using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.Media.Audio;
using Windows.Media.Render;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using WinRT;

namespace ElgatoCapture.Services;

// COM interface for accessing raw audio buffer bytes
[ComImport]
[Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
unsafe interface IMemoryBufferByteAccess
{
    void GetBuffer(out byte* buffer, out uint capacity);
}

public class CaptureService : IDisposable, IAsyncDisposable
{
    private enum RecordingBackend
    {
        None,
        Ffmpeg,
        AviWriter,
        MediaCaptureFallback
    }

    private MediaCapture? _mediaCapture;
    private MediaFrameReader? _frameReader;
    private MediaFrameReader? _recordingFrameReader; // Separate reader for recording
    private StorageFile? _recordingFile;
    private IRecordingSink? _recordingSink;
    private RecordingContext? _recordingContext;
    private bool _isRecording;
    private bool _isInitialized;
    private readonly object _lockObject = new();
    private bool _isDisposed;
    private readonly SemaphoreSlim _sessionTransitionLock = new(1, 1);
    private volatile CaptureSessionState _sessionState = CaptureSessionState.Uninitialized;
    private volatile RecordingBackend _recordingBackend = RecordingBackend.None;
    private readonly IProcessSupervisor _processSupervisor;
    private readonly RecordingArtifactManager _artifactManager = new();

    // FFmpeg encoder for CFR output
    private FFmpegEncoderService? _ffmpegEncoder;

    // Frame conversion pipeline - decouples GPU copy from format conversion
    private Channel<SoftwareBitmap>? _conversionQueue;
    private Task? _conversionWorkerTask;
    private CancellationTokenSource? _conversionCancellation;
    private RecordingPipelineOptions _activePipelineOptions = new();
    private int _conversionQueueCapacity = 8;
    private int _conversionQueueDepth;
    private const string DefaultVideoInputPixelFormat = "nv12";
    private const string HdrVideoInputPixelFormat = "p010le";
    private readonly Stopwatch _recordingStopwatch = new();
    private long _videoFramesArrived;
    private long _videoFramesQueued;
    private long _videoFramesDropped;
    private long _videoFramesConverted;
    private long _videoFramesEnqueued;
    private long _videoFramesDirectNv12;
    private long _videoFramesConvertedNv12;
    private long _lastPipelineLogMs;
    private long _lastFrameArrivalMs;
    private long _lastConversionIdleLogMs;
    private long _lastEncoderNotReadyLogMs;
    private long _lastHealthSnapshotLogMs;
    private int _loggedFirstFrameArrival;
    private int _loggedFirstFrameQueued;
    private int _loggedFirstFrameConverted;
    private int _loggedFirstFrameEnqueued;
    private long _videoFramesDroppedFromBacklog;
    private readonly object _captureCadenceLock = new();
    private readonly double[] _captureFrameIntervalWindowMs = new double[600];
    private int _captureFrameIntervalCount;
    private int _captureFrameIntervalIndex;
    private long _captureLastArrivalTick;

    private AudioGraph? _recordingAudioGraph; // Separate graph for recording audio capture
    private AudioFrameOutputNode? _audioFrameOutputNode;
    private AudioDeviceInputNode? _recordingAudioInputNode; // Store for proper disposal
    private AudioEncodingProperties? _recordingAudioFormat;
    private long _audioFrameIndex;
    private long _audioClipFrameCount;
    private long _lastAudioLogTick;
    private long _lastAudioClipLogTick;
    private string? _detectedAudioSubtype; // Track format for consistency detection
    private FileStream? _audioFileStream;
    private Channel<byte[]>? _audioFileWriteQueue;
    private CancellationTokenSource? _audioFileWriteCancellation;
    private Task? _audioFileWriteTask;
    private long _audioBytesWritten;
    private long _audioFileWriteDropped;
    private string? _audioTempPath;
    private string? _finalOutputPath;
    private string? _lastOutputPath;
    private string _lastFinalizeStatus = "None";
    private DateTimeOffset? _lastFinalizeUtc;
    private IReadOnlyList<string> _lastPreservedArtifacts = Array.Empty<string>();
    private bool _postMuxAudioEnabled;
    private string _activeAudioPathMode = "None";
    private bool _muxAttempted;
    private bool? _muxSucceeded;
    private string? _ffmpegPathForMux;
    private const int RecordingAudioSampleRate = 48000;
    private const short RecordingAudioChannels = 2;
    private const short RecordingAudioBitsPerSample = 32;
    private const int AudioWriteQueueCapacity = 512;
    private const int MuxTimeoutMs = 45_000;
    private const int ConversionDrainTimeoutMs = 5000;
    private const int ConversionCancelGraceMs = 2000;
    private long _lastAudioLevelUpdateTick;
    private const int ComInteropErrorLogIntervalMs = 2000;
    private long _lastComInteropErrorLogTick;
    private long _suppressedComInteropErrorCount;

    // Audio preview
    private AudioGraph? _audioGraph;
    private AudioDeviceInputNode? _audioInputNode;
    private AudioDeviceOutputNode? _audioOutputNode;
    private AudioFrameOutputNode? _previewAudioFrameOutputNode;
    private AudioEncodingProperties? _previewAudioFormat;
    private bool _isAudioPreviewActive;
    private string? _audioDeviceId;
    private string? _audioDeviceName;

    // Stored device info for reinitialization
    private CaptureDevice? _currentDevice;
    private CaptureSettings? _currentSettings;
    private CaptureSettings? _activeRecordingSettings;
    private CaptureSettings? _lastRecordingSettings;
    private double? _actualFrameRate;
    private string? _actualFrameRateArg;
    private uint? _actualFrameRateNumerator;
    private uint? _actualFrameRateDenominator;
    private uint? _actualWidth;
    private uint? _actualHeight;
    private string? _actualPixelFormat;
    private string _activeVideoInputPixelFormat = DefaultVideoInputPixelFormat;
    private bool _hdrOutputActive;
    private string _hdrActivationReason = "HDR inactive";
    private bool _lastRecordingHdrOutputActive;
    private string _lastHdrActivationReason = "HDR inactive";
    private string? _recordingReaderSourceStreamType;
    private string? _recordingReaderSourceSubtype;
    private string? _recordingReaderRequestedSubtype;
    private string? _firstObservedFramePixelFormat;
    private string? _latestObservedFramePixelFormat;
    private long _observedP010FrameCount;
    private long _observedNv12FrameCount;
    private long _observedOtherFrameCount;
    private bool _hdrAutoDowngraded;
    private string _hdrAutoDowngradeReason = string.Empty;
    private bool _hdrSourceNot10BitDetected;
    private bool _recordingFrameHandlerAttached;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<Exception>? ErrorOccurred;
    public event EventHandler<ulong>? FrameCaptured;
    public event EventHandler<AudioLevelEventArgs>? AudioLevelUpdated;

    public bool IsRecording => _isRecording;
    public bool IsInitialized => _isInitialized;
    public bool IsAudioPreviewActive => _isAudioPreviewActive;
    public MediaCapture? MediaCapture => _mediaCapture;
    public CaptureSessionState SessionState => _sessionState;

    public CaptureService() : this(new ProcessSupervisor())
    {
    }

    internal CaptureService(IProcessSupervisor processSupervisor)
    {
        _processSupervisor = processSupervisor ?? throw new ArgumentNullException(nameof(processSupervisor));
    }

    private readonly record struct CaptureCadenceMetrics(
        int SampleCount,
        double ObservedFps,
        double ExpectedIntervalMs,
        double AverageIntervalMs,
        double P95IntervalMs,
        double MaxIntervalMs,
        double JitterStdDevMs,
        long SevereGapCount,
        long EstimatedDroppedFrames,
        double EstimatedDropPercent);

    private CaptureSessionState ResolveSteadyState()
    {
        if (_isDisposed)
        {
            return CaptureSessionState.Disposed;
        }

        if (_isRecording)
        {
            return CaptureSessionState.Recording;
        }

        if (_isInitialized)
        {
            return _isAudioPreviewActive
                ? CaptureSessionState.Previewing
                : CaptureSessionState.Ready;
        }

        return CaptureSessionState.Uninitialized;
    }

    private async Task RunSessionTransitionAsync(
        string operationName,
        CaptureSessionState transitionState,
        Func<Task> action,
        bool allowWhenDisposed = false)
    {
        var sessionLockTimeoutMs = GetIntFromEnv(
            "ELGATOCAPTURE_SESSION_TRANSITION_TIMEOUT_MS",
            60000,
            1000,
            300000);
        if (!await _sessionTransitionLock.WaitAsync(sessionLockTimeoutMs))
        {
            throw new TimeoutException(
                $"Session transition lock wait timed out after {sessionLockTimeoutMs} ms for {operationName}.");
        }

        var previousState = _sessionState;
        try
        {
            if (_isDisposed && !allowWhenDisposed)
            {
                throw new ObjectDisposedException(nameof(CaptureService));
            }

            _sessionState = transitionState;
            Logger.Log($"Session transition start: {operationName} ({previousState} -> {transitionState})");
            await action();
            if (_sessionState == transitionState)
            {
                _sessionState = ResolveSteadyState();
            }
            Logger.Log($"Session transition complete: {operationName} => {_sessionState}");
        }
        catch
        {
            if (_sessionState != CaptureSessionState.Disposed)
            {
                _sessionState = CaptureSessionState.Faulted;
            }
            throw;
        }
        finally
        {
            _sessionTransitionLock.Release();
        }
    }

    public RecordingStats GetRecordingStats()
    {
        long videoBytes = 0;
        long audioBytes = 0;

        try
        {
            var videoPath = _recordingFile?.Path;
            if (!string.IsNullOrEmpty(videoPath) && File.Exists(videoPath))
            {
                videoBytes = new FileInfo(videoPath).Length;
            }
        }
        catch
        {
            videoBytes = 0;
        }

        try
        {
            if (_postMuxAudioEnabled && !string.IsNullOrEmpty(_audioTempPath) && File.Exists(_audioTempPath))
            {
                audioBytes = new FileInfo(_audioTempPath).Length;
            }
        }
        catch
        {
            audioBytes = 0;
        }

        return new RecordingStats(videoBytes, audioBytes);
    }

    public CaptureDiagnosticsSnapshot GetDiagnosticsSnapshot()
    {
        var health = GetHealthSnapshot();
        return new CaptureDiagnosticsSnapshot
        {
            TimestampUtc = health.TimestampUtc,
            SessionState = health.SessionState,
            IsRecording = health.IsRecording,
            RecordingBackend = health.RecordingBackend,
            AudioPathMode = health.AudioPathMode,
            MuxResult = health.MuxResult,
            RecordingElapsedMs = health.RecordingElapsedMs,
            LastFrameArrivalMs = health.LastFrameArrivalMs,
            EstimatedPipelineLatencyMs = health.EstimatedPipelineLatencyMs,
            ExpectedFrameRate = health.ExpectedFrameRate,
            NegotiatedWidth = health.NegotiatedWidth,
            NegotiatedHeight = health.NegotiatedHeight,
            NegotiatedFrameRate = health.NegotiatedFrameRate,
            NegotiatedFrameRateArg = health.NegotiatedFrameRateArg,
            NegotiatedFrameRateNumerator = health.NegotiatedFrameRateNumerator,
            NegotiatedFrameRateDenominator = health.NegotiatedFrameRateDenominator,
            NegotiatedPixelFormat = health.NegotiatedPixelFormat,
            RequestedReaderSubtype = health.RequestedReaderSubtype,
            ReaderSourceStreamType = health.ReaderSourceStreamType,
            ReaderSourceSubtype = health.ReaderSourceSubtype,
            FirstObservedFramePixelFormat = health.FirstObservedFramePixelFormat,
            LatestObservedFramePixelFormat = health.LatestObservedFramePixelFormat,
            ObservedP010FrameCount = health.ObservedP010FrameCount,
            ObservedNv12FrameCount = health.ObservedNv12FrameCount,
            ObservedOtherFrameCount = health.ObservedOtherFrameCount,
            HdrAutoDowngraded = health.HdrAutoDowngraded,
            HdrAutoDowngradeReason = health.HdrAutoDowngradeReason,
            CaptureCadenceSampleCount = health.CaptureCadenceSampleCount,
            CaptureCadenceObservedFps = health.CaptureCadenceObservedFps,
            CaptureCadenceExpectedIntervalMs = health.CaptureCadenceExpectedIntervalMs,
            CaptureCadenceAverageIntervalMs = health.CaptureCadenceAverageIntervalMs,
            CaptureCadenceP95IntervalMs = health.CaptureCadenceP95IntervalMs,
            CaptureCadenceMaxIntervalMs = health.CaptureCadenceMaxIntervalMs,
            CaptureCadenceJitterStdDevMs = health.CaptureCadenceJitterStdDevMs,
            CaptureCadenceSevereGapCount = health.CaptureCadenceSevereGapCount,
            CaptureCadenceEstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames,
            CaptureCadenceEstimatedDropPercent = health.CaptureCadenceEstimatedDropPercent,
            ConversionQueueDepth = health.ConversionQueueDepth,
            FfmpegVideoQueueDepth = health.FfmpegVideoQueueDepth,
            FfmpegAudioQueueDepth = health.FfmpegAudioQueueDepth,
            VideoFramesArrived = health.VideoFramesArrived,
            VideoFramesQueued = health.VideoFramesQueued,
            VideoFramesDropped = health.VideoFramesDropped,
            VideoFramesDroppedBacklog = health.VideoFramesDroppedBacklog,
            VideoFramesConverted = health.VideoFramesConverted,
            VideoFramesEnqueued = health.VideoFramesEnqueued,
            VideoDropsQueueSaturated = health.VideoDropsQueueSaturated,
            VideoDropsBacklogEviction = health.VideoDropsBacklogEviction,
            AudioDropsQueueSaturated = health.AudioDropsQueueSaturated,
            AudioDropsBacklogEviction = health.AudioDropsBacklogEviction,
            AudioChunksDropped = health.AudioChunksDropped
        };
    }

    public CaptureRuntimeSnapshot GetRuntimeSnapshot()
    {
        var requestedSettings = _recordingContext?.Settings ?? _activeRecordingSettings ?? _currentSettings ?? _lastRecordingSettings;
        var outputPath = _lastOutputPath
            ?? _recordingContext?.FinalOutputPath
            ?? _recordingFile?.Path;
        var requestedFrameRateArg = requestedSettings?.RequestedFrameRateArg;
        if (string.IsNullOrWhiteSpace(requestedFrameRateArg) &&
            requestedSettings?.RequestedFrameRateNumerator.HasValue == true &&
            requestedSettings?.RequestedFrameRateDenominator.HasValue == true &&
            requestedSettings.RequestedFrameRateNumerator.Value > 0 &&
            requestedSettings.RequestedFrameRateDenominator.Value > 0)
        {
            var requestedNumerator = requestedSettings.RequestedFrameRateNumerator.Value;
            var requestedDenominator = requestedSettings.RequestedFrameRateDenominator.Value;
            requestedFrameRateArg = $"{requestedNumerator}/{requestedDenominator}";
        }
        if (string.IsNullOrWhiteSpace(requestedFrameRateArg) &&
            requestedSettings != null &&
            requestedSettings.FrameRate > 0)
        {
            var requestedFrameRate = requestedSettings.FrameRate;
            requestedFrameRateArg = requestedFrameRate.ToString("0.###", CultureInfo.InvariantCulture);
        }

        return new CaptureRuntimeSnapshot
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            IsInitialized = _isInitialized,
            IsRecording = _isRecording,
            IsAudioPreviewActive = _isAudioPreviewActive,
            SessionState = _sessionState.ToString(),
            CurrentDeviceId = _currentDevice?.Id,
            CurrentDeviceName = _currentDevice?.Name,
            ActiveAudioDeviceId = _audioDeviceId,
            ActiveAudioDeviceName = _audioDeviceName,
            RequestedWidth = requestedSettings?.Width,
            RequestedHeight = requestedSettings?.Height,
            RequestedFrameRate = requestedSettings?.FrameRate,
            RequestedFrameRateArg = requestedFrameRateArg,
            RequestedFrameRateNumerator = requestedSettings?.RequestedFrameRateNumerator,
            RequestedFrameRateDenominator = requestedSettings?.RequestedFrameRateDenominator,
            RequestedPixelFormat = requestedSettings?.RequestedPixelFormat,
            RequestedFormat = requestedSettings?.Format.ToString(),
            RequestedQuality = requestedSettings?.Quality.ToString(),
            RequestedAudioEnabled = requestedSettings?.AudioEnabled,
            RequestedHdrEnabled = requestedSettings?.HdrEnabled,
            RequestedHdrMasteringMetadata = requestedSettings != null &&
                                           (!string.IsNullOrWhiteSpace(requestedSettings.HdrMasterDisplayMetadata) ||
                                            (requestedSettings.HdrMaxCll > 0 && requestedSettings.HdrMaxFall > 0)),
            HdrOutputActive = _isRecording ? _hdrOutputActive : _lastRecordingHdrOutputActive,
            HdrActivationReason = _isRecording ? _hdrActivationReason : _lastHdrActivationReason,
            HdrAutoDowngraded = _hdrAutoDowngraded,
            HdrAutoDowngradeReason = _hdrAutoDowngradeReason,
            HdrRequestedButSourceNot10Bit = _hdrSourceNot10BitDetected,
            RequestedOutputPath = requestedSettings?.OutputPath,
            ActualWidth = _actualWidth,
            ActualHeight = _actualHeight,
            ActualFrameRate = _actualFrameRate,
            ActualFrameRateArg = _actualFrameRateArg,
            NegotiatedWidth = _actualWidth,
            NegotiatedHeight = _actualHeight,
            NegotiatedFrameRate = _actualFrameRate,
            NegotiatedFrameRateArg = _actualFrameRateArg,
            NegotiatedFrameRateNumerator = _actualFrameRateNumerator,
            NegotiatedFrameRateDenominator = _actualFrameRateDenominator,
            NegotiatedPixelFormat = _actualPixelFormat,
            RequestedReaderSubtype = _recordingReaderRequestedSubtype,
            ReaderSourceStreamType = _recordingReaderSourceStreamType,
            ReaderSourceSubtype = _recordingReaderSourceSubtype,
            FirstObservedFramePixelFormat = _firstObservedFramePixelFormat,
            LatestObservedFramePixelFormat = _latestObservedFramePixelFormat,
            ObservedP010FrameCount = Interlocked.Read(ref _observedP010FrameCount),
            ObservedNv12FrameCount = Interlocked.Read(ref _observedNv12FrameCount),
            ObservedOtherFrameCount = Interlocked.Read(ref _observedOtherFrameCount),
            EncoderInputPixelFormat = _activeVideoInputPixelFormat,
            EncoderOutputPixelFormat = _ffmpegEncoder?.ActiveOutputPixelFormat,
            DetectedSourceFrameRate = _actualFrameRate,
            DetectedSourceFrameRateArg = _actualFrameRateArg,
            SourceFrameRateOrigin = _actualFrameRate.HasValue ? "NegotiatedDeviceFormat" : "Unknown",
            RecordingBackend = _recordingBackend.ToString(),
            AudioPathMode = _activeAudioPathMode,
            MuxAttempted = _muxAttempted,
            MuxSucceeded = _muxSucceeded,
            LastOutputPath = outputPath,
            LastFinalizeStatus = _lastFinalizeStatus,
            LastFinalizeUtc = _lastFinalizeUtc,
            LastPreservedArtifacts = _lastPreservedArtifacts
        };
    }

    public CaptureHealthSnapshot GetHealthSnapshot()
    {
        var encoder = _ffmpegEncoder;
        var elapsedMs = _recordingStopwatch.IsRunning ? _recordingStopwatch.ElapsedMilliseconds : 0;
        var lastArrivalMs = Interlocked.Read(ref _lastFrameArrivalMs);
        var estimatedLatencyMs = (elapsedMs > 0 && lastArrivalMs > 0)
            ? Math.Max(0, elapsedMs - lastArrivalMs)
            : 0;
        var expectedFrameRate =
            _actualFrameRate ??
            _activeRecordingSettings?.FrameRate ??
            _currentSettings?.FrameRate ??
            _lastRecordingSettings?.FrameRate ??
            0;
        var cadence = GetCaptureCadenceMetrics(expectedFrameRate);

        var muxResult = _muxAttempted
            ? (_muxSucceeded == true ? "Succeeded" : "Failed")
            : "NotAttempted";

        return new CaptureHealthSnapshot
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            SessionState = SessionState,
            IsRecording = _isRecording,
            RecordingBackend = _recordingBackend.ToString(),
            AudioPathMode = _activeAudioPathMode,
            MuxResult = muxResult,
            RecordingElapsedMs = elapsedMs,
            LastFrameArrivalMs = lastArrivalMs,
            EstimatedPipelineLatencyMs = estimatedLatencyMs,
            ExpectedFrameRate = expectedFrameRate,
            NegotiatedWidth = _actualWidth,
            NegotiatedHeight = _actualHeight,
            NegotiatedFrameRate = _actualFrameRate,
            NegotiatedFrameRateArg = _actualFrameRateArg,
            NegotiatedFrameRateNumerator = _actualFrameRateNumerator,
            NegotiatedFrameRateDenominator = _actualFrameRateDenominator,
            NegotiatedPixelFormat = _actualPixelFormat,
            RequestedReaderSubtype = _recordingReaderRequestedSubtype,
            ReaderSourceStreamType = _recordingReaderSourceStreamType,
            ReaderSourceSubtype = _recordingReaderSourceSubtype,
            FirstObservedFramePixelFormat = _firstObservedFramePixelFormat,
            LatestObservedFramePixelFormat = _latestObservedFramePixelFormat,
            ObservedP010FrameCount = Interlocked.Read(ref _observedP010FrameCount),
            ObservedNv12FrameCount = Interlocked.Read(ref _observedNv12FrameCount),
            ObservedOtherFrameCount = Interlocked.Read(ref _observedOtherFrameCount),
            HdrAutoDowngraded = _hdrAutoDowngraded,
            HdrAutoDowngradeReason = _hdrAutoDowngradeReason,
            CaptureCadenceSampleCount = cadence.SampleCount,
            CaptureCadenceObservedFps = cadence.ObservedFps,
            CaptureCadenceExpectedIntervalMs = cadence.ExpectedIntervalMs,
            CaptureCadenceAverageIntervalMs = cadence.AverageIntervalMs,
            CaptureCadenceP95IntervalMs = cadence.P95IntervalMs,
            CaptureCadenceMaxIntervalMs = cadence.MaxIntervalMs,
            CaptureCadenceJitterStdDevMs = cadence.JitterStdDevMs,
            CaptureCadenceSevereGapCount = cadence.SevereGapCount,
            CaptureCadenceEstimatedDroppedFrames = cadence.EstimatedDroppedFrames,
            CaptureCadenceEstimatedDropPercent = cadence.EstimatedDropPercent,
            ConversionQueueDepth = Volatile.Read(ref _conversionQueueDepth),
            FfmpegVideoQueueDepth = encoder?.VideoQueueCount ?? 0,
            FfmpegAudioQueueDepth = encoder?.AudioQueueCount ?? 0,
            VideoFramesArrived = Interlocked.Read(ref _videoFramesArrived),
            VideoFramesQueued = Interlocked.Read(ref _videoFramesQueued),
            VideoFramesDropped = Interlocked.Read(ref _videoFramesDropped),
            VideoFramesDroppedBacklog = Interlocked.Read(ref _videoFramesDroppedFromBacklog),
            VideoFramesConverted = Interlocked.Read(ref _videoFramesConverted),
            VideoFramesEnqueued = Interlocked.Read(ref _videoFramesEnqueued),
            VideoDropsQueueSaturated = encoder?.VideoDropsQueueSaturated ?? 0,
            VideoDropsBacklogEviction = encoder?.VideoDropsBacklogEviction ?? 0,
            AudioDropsQueueSaturated = encoder?.AudioDropsQueueSaturated ?? 0,
            AudioDropsBacklogEviction = encoder?.AudioDropsBacklogEviction ?? 0,
            AudioChunksDropped = Interlocked.Read(ref _audioFileWriteDropped)
        };
    }

    public async Task UpdateAudioInputAsync(string? audioDeviceId, string? audioDeviceName)
    {
        _audioDeviceId = audioDeviceId;
        _audioDeviceName = audioDeviceName;
        Logger.Log($"Audio input updated: {audioDeviceName ?? "(none)"}");

        if (_isAudioPreviewActive)
        {
            await StopAudioPreviewAsync();
            if (!string.IsNullOrEmpty(_audioDeviceId))
            {
                await StartAudioPreviewAsync();
            }
        }
    }

    public Task InitializeAsync(CaptureDevice device, CaptureSettings settings)
        => RunSessionTransitionAsync(
            nameof(InitializeAsync),
            CaptureSessionState.Initializing,
            () => InitializeCoreAsync(device, settings));

    private async Task InitializeCoreAsync(CaptureDevice device, CaptureSettings settings)
    {
        await CleanupCoreAsync();

        // Store device and settings for potential reinitialization during recording
        _currentDevice = device;
        _currentSettings = settings;
        _actualFrameRate = null;
        _actualFrameRateArg = null;
        _actualFrameRateNumerator = null;
        _actualFrameRateDenominator = null;
        _actualWidth = null;
        _actualHeight = null;
        _actualPixelFormat = null;
        _activeVideoInputPixelFormat = DefaultVideoInputPixelFormat;
        _hdrOutputActive = false;
        _hdrActivationReason = "HDR inactive";
        _lastRecordingHdrOutputActive = false;
        _lastHdrActivationReason = "HDR inactive";
        _recordingReaderSourceStreamType = null;
        _recordingReaderSourceSubtype = null;
        _recordingReaderRequestedSubtype = null;
        _firstObservedFramePixelFormat = null;
        _latestObservedFramePixelFormat = null;
        _hdrAutoDowngraded = false;
        _hdrAutoDowngradeReason = string.Empty;
        _hdrSourceNot10BitDetected = false;
        _recordingFrameHandlerAttached = false;
        Interlocked.Exchange(ref _observedP010FrameCount, 0);
        Interlocked.Exchange(ref _observedNv12FrameCount, 0);
        Interlocked.Exchange(ref _observedOtherFrameCount, 0);

        _mediaCapture = new MediaCapture();

        var selectedAudioId = settings.UseCustomAudioInput ? settings.AudioDeviceId : device.AudioDeviceId;
        var selectedAudioName = settings.UseCustomAudioInput ? settings.AudioDeviceName : device.AudioDeviceName;
        Logger.Log($"Audio enabled: {settings.AudioEnabled}");
        Logger.Log($"Audio input source: {(settings.UseCustomAudioInput ? "Custom" : "Device")}");
        Logger.Log($"Audio device ID: {selectedAudioId ?? "(none)"}");
        Logger.Log($"Audio device name: {selectedAudioName ?? "(none)"}");

        // Initialize MediaCapture with AudioAndVideo mode from the start if audio is available
        // This avoids needing to reinitialize when recording starts, making it seamless
        // AudioGraph handles audio PREVIEW (to speakers), MediaCapture handles audio RECORDING
        var initSettings = new MediaCaptureInitializationSettings
        {
            VideoDeviceId = device.Id
        };

        if (settings.AudioEnabled && settings.UseCustomAudioInput)
        {
            _audioDeviceId = settings.AudioDeviceId;
            _audioDeviceName = settings.AudioDeviceName;
            initSettings.StreamingCaptureMode = StreamingCaptureMode.Video;
            Logger.Log("✓ Using custom audio input (AudioGraph only)");
            Logger.Log($"  Audio device: {settings.AudioDeviceName ?? "(none)"}");
        }
        else if (settings.AudioEnabled && !string.IsNullOrEmpty(device.AudioDeviceId))
        {
            _audioDeviceId = device.AudioDeviceId;
            _audioDeviceName = device.AudioDeviceName;
            initSettings.AudioDeviceId = device.AudioDeviceId;
            initSettings.StreamingCaptureMode = StreamingCaptureMode.AudioAndVideo;
            Logger.Log($"✓ Initializing with AudioAndVideo mode for seamless recording");
            Logger.Log($"  Audio device: {device.AudioDeviceName}");
            Logger.Log($"  (AudioGraph handles preview, MediaCapture handles recording)");
        }
        else if (settings.AudioEnabled)
        {
            _audioDeviceId = null;
            _audioDeviceName = null;
            initSettings.StreamingCaptureMode = StreamingCaptureMode.Video;
            Logger.Log($"✗ Audio enabled but no audio device available - using Video mode");
        }
        else
        {
            _audioDeviceId = null;
            _audioDeviceName = null;
            initSettings.StreamingCaptureMode = StreamingCaptureMode.Video;
            Logger.Log("Audio disabled by user - using Video mode");
        }

        Logger.Log($"Streaming mode: {initSettings.StreamingCaptureMode}");

        _mediaCapture.Failed += MediaCapture_Failed;

        try
        {
            await _mediaCapture.InitializeAsync(initSettings);

            // Set the video format on the device
            Logger.Log($"=== Setting device format to {settings.Width}x{settings.Height}@{settings.FrameRate}fps ===");
            var videoController = _mediaCapture.VideoDeviceController;
            var availableProperties = videoController.GetAvailableMediaStreamProperties(MediaStreamType.VideoRecord);

            var videoProperties = availableProperties.OfType<VideoEncodingProperties>().ToList();
            Logger.Log($"Device has {availableProperties.Count} available formats");
            if (!string.IsNullOrWhiteSpace(settings.RequestedPixelFormat))
            {
                Logger.Log($"Requested pixel format: {settings.RequestedPixelFormat}");
            }

            if (settings.RequestedFrameRateNumerator.HasValue &&
                settings.RequestedFrameRateDenominator.HasValue &&
                settings.RequestedFrameRateNumerator.Value > 0 &&
                settings.RequestedFrameRateDenominator.Value > 0)
            {
                var reqNumerator = settings.RequestedFrameRateNumerator.Value;
                var reqDenominator = settings.RequestedFrameRateDenominator.Value;
                Logger.Log($"Requested frame-rate rational: {reqNumerator}/{reqDenominator}");
            }

            var sizeCandidates = videoProperties
                .Where(p => p.Width == settings.Width && p.Height == settings.Height)
                .ToList();
            var frameRateCandidates = sizeCandidates
                .Where(p => IsFrameRateClose(p, settings.FrameRate))
                .ToList();

            if (settings.RequestedFrameRateNumerator.HasValue &&
                settings.RequestedFrameRateDenominator.HasValue &&
                settings.RequestedFrameRateNumerator.Value > 0 &&
                settings.RequestedFrameRateDenominator.Value > 0)
            {
                var requestedNumerator = settings.RequestedFrameRateNumerator.Value;
                var requestedDenominator = settings.RequestedFrameRateDenominator.Value;
                var exactFrameRateCandidates = sizeCandidates
                    .Where(p => p.FrameRate.Numerator == requestedNumerator && p.FrameRate.Denominator == requestedDenominator)
                    .ToList();
                if (exactFrameRateCandidates.Count > 0)
                {
                    frameRateCandidates = exactFrameRateCandidates;
                }
                else
                {
                    Logger.Log(
                        $"Requested exact frame-rate {requestedNumerator}/{requestedDenominator} was unavailable; " +
                        $"falling back to closest fps match near {settings.FrameRate:0.###}.");
                }
            }

            if (settings.HdrEnabled)
            {
                var hdrCandidates = frameRateCandidates
                    .Where(p => IsHdrSubtype(p.Subtype))
                    .ToList();
                if (hdrCandidates.Count == 0)
                {
                    Logger.Log(
                        $"No HDR-capable subtype is available for {settings.Width}x{settings.Height}@{settings.FrameRate:0.###}.");
                    throw new InvalidOperationException(
                        $"HDR mode is not available for {settings.Width}x{settings.Height}@{settings.FrameRate:0.###}.");
                }

                frameRateCandidates = hdrCandidates;
            }

            var matchingFormat = frameRateCandidates
                .OrderBy(p => GetSubtypePreferenceRank(p.Subtype, settings.RequestedPixelFormat, settings.HdrEnabled))
                .ThenBy(p => Math.Abs(ResolveFrameRate(p.FrameRate.Numerator, p.FrameRate.Denominator, settings.FrameRate) - settings.FrameRate))
                .FirstOrDefault();

            if (matchingFormat != null)
            {
                var fps = ResolveFrameRate(
                    matchingFormat.FrameRate.Numerator,
                    matchingFormat.FrameRate.Denominator,
                    settings.FrameRate);
                Logger.Log($"Found matching format: {matchingFormat.Width}x{matchingFormat.Height}@{fps:0.###}fps ({matchingFormat.Subtype})");
                if (Math.Abs(fps - settings.FrameRate) > 0.01)
                {
                    Logger.Log($"Requested FPS {settings.FrameRate:F3} differs from device FPS {fps:F3}. Using device FPS for FFmpeg.");
                }
                await videoController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoRecord, matchingFormat);
                Logger.Log("Device format set successfully");

                if (videoController.GetMediaStreamProperties(MediaStreamType.VideoRecord) is VideoEncodingProperties appliedProps)
                {
                    SetNegotiatedVideoFormat(appliedProps, settings.FrameRate);
                    var appliedFps = ResolveFrameRate(
                        appliedProps.FrameRate.Numerator,
                        appliedProps.FrameRate.Denominator,
                        settings.FrameRate);
                    Logger.Log($"Applied video-record format: {appliedProps.Width}x{appliedProps.Height}@{appliedFps:0.###}fps ({appliedProps.Subtype})");
                }
                else
                {
                    SetNegotiatedVideoFormat(matchingFormat, settings.FrameRate);
                }
            }
            else
            {
                Logger.Log($"No matching format found for {settings.Width}x{settings.Height}@{settings.FrameRate:0.###}fps");
                Logger.Log("Available formats:");
                foreach (var prop in videoProperties.Take(10))
                {
                    var fps = ResolveFrameRate(prop.FrameRate.Numerator, prop.FrameRate.Denominator, settings.FrameRate);
                    Logger.Log($"  - {prop.Width}x{prop.Height}@{fps:0.###}fps ({prop.Subtype})");
                }

                if (settings.HdrEnabled)
                {
                    throw new InvalidOperationException(
                        $"HDR mode is not available for {settings.Width}x{settings.Height}@{settings.FrameRate:0.###}.");
                }

                if (videoController.GetMediaStreamProperties(MediaStreamType.VideoRecord) is VideoEncodingProperties currentProps)
                {
                    var fps = ResolveFrameRate(
                        currentProps.FrameRate.Numerator,
                        currentProps.FrameRate.Denominator,
                        settings.FrameRate);
                    SetNegotiatedVideoFormat(currentProps, settings.FrameRate);
                    Logger.Log($"Using current device format for FFmpeg: {currentProps.Width}x{currentProps.Height}@{fps:0.###}fps ({currentProps.Subtype})");
                }
            }

            _isInitialized = true;
            _sessionState = CaptureSessionState.Ready;
            StatusChanged?.Invoke(this, "Initialized");
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
            throw;
        }
    }

    private void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
    {
        ErrorOccurred?.Invoke(this, new Exception($"MediaCapture failed: {errorEventArgs.Message}"));
    }

    public Task StartRecordingAsync(CaptureSettings settings)
        => RunSessionTransitionAsync(
            nameof(StartRecordingAsync),
            CaptureSessionState.Recording,
            () => StartRecordingCoreAsync(settings));

    private async Task StartRecordingCoreAsync(CaptureSettings settings)
    {
        if (!_isInitialized || _mediaCapture == null || _currentDevice == null)
        {
            throw new InvalidOperationException("Capture not initialized");
        }

        if (_isRecording)
        {
            return;
        }

        try
        {
            Logger.Log("=== Starting recording (seamless - no reinitialization needed) ===");
            Logger.LogEvent("CAP-REC-START", $"format={settings.Format} path={settings.OutputPath}");
            Logger.Log($"Audio preview active: {_isAudioPreviewActive}");
            _currentSettings = settings;
            _activeRecordingSettings = settings;
            _lastFinalizeStatus = "Recording";
            _lastFinalizeUtc = DateTimeOffset.UtcNow;
            _lastPreservedArtifacts = Array.Empty<string>();
            _recordingReaderSourceStreamType = null;
            _recordingReaderSourceSubtype = null;
            _recordingReaderRequestedSubtype = null;
            _firstObservedFramePixelFormat = null;
            _latestObservedFramePixelFormat = null;
            _hdrAutoDowngraded = false;
            _hdrAutoDowngradeReason = string.Empty;
            _hdrSourceNot10BitDetected = false;
            _recordingFrameHandlerAttached = false;
            Interlocked.Exchange(ref _observedP010FrameCount, 0);
            Interlocked.Exchange(ref _observedNv12FrameCount, 0);
            Interlocked.Exchange(ref _observedOtherFrameCount, 0);
            var canCaptureAudio = settings.AudioEnabled && !string.IsNullOrEmpty(_audioDeviceId);
            var isCompressedFormat = settings.Format != RecordingFormat.UncompressedAvi;
            var useNamedPipeAudio = isCompressedFormat &&
                                    canCaptureAudio &&
                                    settings.AudioPathMode == AudioPathMode.NamedPipeExperimental;
            var effectiveFrameRate = _actualFrameRate ?? settings.FrameRate;
            var frameRateArg = !string.IsNullOrWhiteSpace(_actualFrameRateArg)
                ? _actualFrameRateArg
                : effectiveFrameRate.ToString("0.###", CultureInfo.InvariantCulture);
            var effectiveWidth = _actualWidth ?? settings.Width;
            var effectiveHeight = _actualHeight ?? settings.Height;
            var useHdrVideoPipeline = IsHdrOutputEnabled(settings);
            var videoInputPixelFormat = useHdrVideoPipeline
                ? HdrVideoInputPixelFormat
                : DefaultVideoInputPixelFormat;

            if (settings.HdrEnabled &&
                settings.HdrOutputMode == HdrOutputMode.Hdr10Pq &&
                !useHdrVideoPipeline)
            {
                throw new InvalidOperationException(
                    "HDR was requested, but HDR output is disabled by environment override. " +
                    "Unset ELGATOCAPTURE_HDR_OUTPUT_FORCE_OFF (or set ELGATOCAPTURE_HDR_OUTPUT_ENABLED=1).");
            }

            if (useHdrVideoPipeline && !IsHdrSubtype(_actualPixelFormat))
            {
                throw new InvalidOperationException(
                    "HDR recording requires an HDR-capable negotiated device pixel format, " +
                    $"but got '{_actualPixelFormat ?? "unknown"}'.");
            }

            _activeVideoInputPixelFormat = videoInputPixelFormat;
            _hdrOutputActive = useHdrVideoPipeline;
            _hdrActivationReason = useHdrVideoPipeline
                ? $"Active (requested={settings.HdrEnabled}, mode={settings.HdrOutputMode}, negotiated={_actualPixelFormat ?? "unknown"})"
                : settings.HdrEnabled && settings.HdrOutputMode == HdrOutputMode.Hdr10Pq
                    ? "Inactive (requested HDR10 but override disabled pipeline)"
                    : "Inactive (HDR toggle or mode is off)";
            if (useHdrVideoPipeline)
            {
                Logger.Log("HDR output pipeline enabled: requesting 10-bit P010 input for encoder.");
            }

            _muxAttempted = false;
            _muxSucceeded = null;
            _postMuxAudioEnabled = isCompressedFormat && canCaptureAudio && !useNamedPipeAudio;
            _activeAudioPathMode = !settings.AudioEnabled || !canCaptureAudio
                ? "Disabled"
                : !isCompressedFormat
                    ? "EmbeddedCapture"
                    : useNamedPipeAudio
                        ? AudioPathMode.NamedPipeExperimental.ToString()
                        : AudioPathMode.PostMuxDefault.ToString();
            Logger.Log($"Audio in recording: {(canCaptureAudio ? "Yes" : "No")}");
            Logger.Log($"Audio path mode requested: {settings.AudioPathMode}");

            var folder = await StorageFolder.GetFolderFromPathAsync(settings.OutputPath);
            var sinkAudioDevice = isCompressedFormat && canCaptureAudio && !_postMuxAudioEnabled
                ? _audioDeviceName
                : null;
            _recordingContext = await _artifactManager.CreateContextAsync(
                folder,
                settings,
                _postMuxAudioEnabled,
                sinkAudioDevice,
                effectiveFrameRate,
                frameRateArg,
                effectiveWidth,
                effectiveHeight,
                videoInputPixelFormat);

            _recordingFile = await StorageFile.GetFileFromPathAsync(_recordingContext.VideoOutputPath);
            _finalOutputPath = _recordingContext.UsePostMuxAudio ? _recordingContext.FinalOutputPath : null;
            _audioTempPath = _recordingContext.AudioTempPath;
            _lastOutputPath = _recordingContext.UsePostMuxAudio
                ? _recordingContext.FinalOutputPath
                : _recordingContext.VideoOutputPath;

            if (_recordingContext.UsePostMuxAudio)
            {
                if (!string.IsNullOrWhiteSpace(_audioTempPath))
                {
                    StartAudioCaptureFile(_audioTempPath);
                }

                Logger.Log("Post-mux audio enabled");
                Logger.Log($"Video temp file: {_recordingContext.VideoOutputPath}");
                Logger.Log($"Audio temp file: {_audioTempPath}");
                Logger.Log($"Final output file: {_recordingContext.FinalOutputPath}");
            }
            else if (useNamedPipeAudio)
            {
                Logger.Log("Named-pipe audio path enabled (experimental mode)");
            }

            if (settings.Format == RecordingFormat.UncompressedAvi)
            {
                await StartUncompressedRecordingAsync();
            }
            else
            {
                await StartCompressedRecordingAsync(settings);
            }

            _isRecording = true;
            _sessionState = CaptureSessionState.Recording;
            StatusChanged?.Invoke(this, "Recording");
            Logger.Log("✓ Recording started successfully");
        }
        catch (Exception ex)
        {
            try
            {
                await CleanupRecordingResourcesOnErrorAsync();
            }
            catch (Exception cleanupEx)
            {
                Logger.Log($"Start-recording rollback failed: {cleanupEx.Message}");
            }

            _recordingContext = null;
            _recordingFile = null;
            _recordingBackend = RecordingBackend.None;
            _postMuxAudioEnabled = false;
            _audioTempPath = null;
            _finalOutputPath = null;
            _activeRecordingSettings = null;
            _lastFinalizeStatus = $"StartFailed: {ex.Message}";
            _lastFinalizeUtc = DateTimeOffset.UtcNow;
            _lastPreservedArtifacts = Array.Empty<string>();

            ErrorOccurred?.Invoke(this, ex);
            throw;
        }
    }

    private async Task StartCompressedRecordingAsync(CaptureSettings settings)
    {
        if (_mediaCapture == null || _recordingFile == null) return;

        _recordingBackend = RecordingBackend.Ffmpeg;
        var startupStopwatch = System.Diagnostics.Stopwatch.StartNew();
        Logger.Log("=== Starting FFmpeg-based recording for CFR output ===");
        ResetPipelineStats();
        _recordingStopwatch.Restart();
        Logger.LogVerbose("Recording pipeline stopwatch started");

        // Initialize FFmpeg encoder
        _ffmpegEncoder = new FFmpegEncoderService();
        _ffmpegEncoder.StatusChanged += (s, msg) => Logger.Log($"[FFmpegEncoder] {msg}");
        _ffmpegEncoder.ErrorOccurred += (s, err) => Logger.Log($"[FFmpegEncoder] ERROR: {err}");
        _ffmpegEncoder.FrameEncoded += (s, count) => FrameCaptured?.Invoke(this, count);
        _ffmpegPathForMux = _ffmpegEncoder.FfmpegPath;
        _recordingSink = new FfmpegRecordingSink(_ffmpegEncoder);

        // Determine if we're using audio
        // Audio is captured via AudioGraph and piped to FFmpeg via named pipe
        // This gives us full timestamp control (both streams start at 0)
        string? audioDevice = null;
        var captureAudio = settings.AudioEnabled && !string.IsNullOrEmpty(_audioDeviceName);
        if (_postMuxAudioEnabled && captureAudio)
        {
            Logger.Log("Audio will be captured to WAV and muxed after recording");
        }
        else if (captureAudio)
        {
            audioDevice = _audioDeviceName;
            Logger.Log("Audio will be captured via AudioGraph and piped to FFmpeg");

            // Prepare audio queue BEFORE starting encoder
            // This allows audio samples to buffer while FFmpeg is starting up
            _ffmpegEncoder.PrepareAudioQueue();
        }

        try
        {
            // Initialize conversion pipeline
            _conversionQueueCapacity = ResolveConversionQueueCapacity(settings);
            _conversionQueue = Channel.CreateBounded<SoftwareBitmap>(new BoundedChannelOptions(_conversionQueueCapacity)
            {
                SingleReader = _activePipelineOptions.VideoDropPolicy != VideoFrameDropPolicy.DropOldest,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            });
            Interlocked.Exchange(ref _conversionQueueDepth, 0);
            _conversionCancellation = new CancellationTokenSource();
            _conversionWorkerTask = Task.Run(() => RunConversionWorkerAsync(_conversionCancellation.Token));
            Logger.Log(
                $"Frame conversion pipeline initialized (queue size: {_conversionQueueCapacity}, " +
                $"targetLatencyMs={_activePipelineOptions.TargetVideoLatencyMs}, dropPolicy={_activePipelineOptions.VideoDropPolicy})");

            // Set up audio capture FIRST - start buffering before FFmpeg starts
            // This ensures audio samples are queued during FFmpeg's ~4 second probe phase
            if (captureAudio)
            {
                Logger.LogVerbose($"Audio capture setup starting at {startupStopwatch.ElapsedMilliseconds} ms");
                await SetupRecordingAudioCaptureAsync();
                Logger.LogVerbose($"Audio capture setup complete at {startupStopwatch.ElapsedMilliseconds} ms");
                Logger.Log("Audio capture started - buffering while FFmpeg initializes");
            }

            // Start FFmpeg encoder through sink with contextualized contract.
            var context = _recordingContext ?? throw new InvalidOperationException("Recording context not initialized.");
            if (_postMuxAudioEnabled && captureAudio)
            {
                var ffmpegSettings = new CaptureSettings
                {
                    Width = settings.Width,
                    Height = settings.Height,
                    FrameRate = settings.FrameRate,
                    RequestedFrameRateArg = settings.RequestedFrameRateArg,
                    RequestedFrameRateNumerator = settings.RequestedFrameRateNumerator,
                    RequestedFrameRateDenominator = settings.RequestedFrameRateDenominator,
                    RequestedPixelFormat = settings.RequestedPixelFormat,
                    Format = settings.Format,
                    Quality = settings.Quality,
                    CustomBitrateMbps = settings.CustomBitrateMbps,
                    HdrEnabled = settings.HdrEnabled,
                    HdrOutputMode = settings.HdrOutputMode,
                    HdrNominalPeakNits = settings.HdrNominalPeakNits,
                    HdrMaxCll = settings.HdrMaxCll,
                    HdrMaxFall = settings.HdrMaxFall,
                    HdrMasterDisplayMetadata = settings.HdrMasterDisplayMetadata,
                    OutputPath = settings.OutputPath,
                    AudioEnabled = false,
                    UseCustomAudioInput = settings.UseCustomAudioInput,
                    AudioDeviceId = settings.AudioDeviceId,
                    AudioDeviceName = settings.AudioDeviceName,
                    AudioPathMode = settings.AudioPathMode,
                    PipelineOptions = settings.PipelineOptions
                };

                context = new RecordingContext
                {
                    Settings = ffmpegSettings,
                    VideoOutputPath = context.VideoOutputPath,
                    FinalOutputPath = context.FinalOutputPath,
                    AudioTempPath = context.AudioTempPath,
                    UsePostMuxAudio = context.UsePostMuxAudio,
                    AudioDeviceName = null,
                    EffectiveFrameRate = context.EffectiveFrameRate,
                    FrameRateArg = context.FrameRateArg,
                    EffectiveWidth = context.EffectiveWidth,
                    EffectiveHeight = context.EffectiveHeight,
                    VideoInputPixelFormat = context.VideoInputPixelFormat,
                    HdrPipelineActive = context.HdrPipelineActive
                };
            }
            else if (!string.IsNullOrWhiteSpace(audioDevice) && !string.Equals(context.AudioDeviceName, audioDevice, StringComparison.Ordinal))
            {
                context = new RecordingContext
                {
                    Settings = context.Settings,
                    VideoOutputPath = context.VideoOutputPath,
                    FinalOutputPath = context.FinalOutputPath,
                    AudioTempPath = context.AudioTempPath,
                    UsePostMuxAudio = context.UsePostMuxAudio,
                    AudioDeviceName = audioDevice,
                    EffectiveFrameRate = context.EffectiveFrameRate,
                    FrameRateArg = context.FrameRateArg,
                    EffectiveWidth = context.EffectiveWidth,
                    EffectiveHeight = context.EffectiveHeight,
                    VideoInputPixelFormat = context.VideoInputPixelFormat,
                    HdrPipelineActive = context.HdrPipelineActive
                };
            }

            // Probe and start the recording frame reader before FFmpeg launch so we can
            // verify the real incoming frame format and auto-downgrade HDR if needed.
            await SetupRecordingFrameReaderAsync(settings, attachFrameHandler: false);
            Logger.LogVerbose($"Recording frame reader warm-up completed at {startupStopwatch.ElapsedMilliseconds} ms");

            if (!_hdrOutputActive &&
                string.Equals(context.VideoInputPixelFormat, HdrVideoInputPixelFormat, StringComparison.OrdinalIgnoreCase))
            {
                context = CreateHdrDowngradedRecordingContext(context);
            }

            _recordingContext = context;
            _activeVideoInputPixelFormat = context.VideoInputPixelFormat;
            await _recordingSink.StartAsync(context);
            Logger.LogVerbose($"FFmpeg StartEncodingAsync returned at {startupStopwatch.ElapsedMilliseconds} ms");

            AttachRecordingFrameReaderHandler();
            Logger.LogVerbose($"Recording frame reader attached at {startupStopwatch.ElapsedMilliseconds} ms");

            Logger.Log("FFmpeg recording started - frames will be piped to encoder");
        }
        catch
        {
            throw;
        }
    }

    private async Task SetupRecordingFrameReaderAsync(CaptureSettings settings, bool attachFrameHandler)
    {
        if (_mediaCapture == null)
        {
            return;
        }

        _recordingReaderRequestedSubtype = null;
        _firstObservedFramePixelFormat = null;
        _latestObservedFramePixelFormat = null;

        if (_recordingFrameReader != null)
        {
            try
            {
                if (_recordingFrameHandlerAttached)
                {
                    _recordingFrameReader.FrameArrived -= RecordingFrameReader_FrameArrived;
                    _recordingFrameHandlerAttached = false;
                }
                await _recordingFrameReader.StopAsync();
            }
            catch (Exception ex)
            {
                Logger.Log($"Recording frame reader cleanup warning: {ex.Message}");
            }
            finally
            {
                _recordingFrameReader.Dispose();
                _recordingFrameReader = null;
            }
        }

        var firstFrameTimeoutMs = GetIntFromEnv(
            "ELGATOCAPTURE_RECORDING_FIRST_FRAME_TIMEOUT_MS",
            defaultValue: 5000,
            minValue: 500,
            maxValue: 30000);

        var hdrRequested = _hdrOutputActive &&
                           settings.HdrEnabled &&
                           settings.HdrOutputMode == HdrOutputMode.Hdr10Pq;

        if (await TrySetupRecordingFrameReaderCoreAsync(hdrRequested, firstFrameTimeoutMs, attachFrameHandler).ConfigureAwait(false))
        {
            return;
        }

        if (hdrRequested)
        {
            var downgradeReason =
                $"HDR source verification failed: expected true 10-bit P010 frames but observed '{_firstObservedFramePixelFormat ?? "unknown"}'. " +
                "Switched to SDR for this recording.";
            AutoDowngradeHdr(downgradeReason);
            _recordingReaderRequestedSubtype = null;
            _recordingReaderSourceStreamType = null;
            _recordingReaderSourceSubtype = null;
            _firstObservedFramePixelFormat = null;
            _latestObservedFramePixelFormat = null;
            if (await TrySetupRecordingFrameReaderCoreAsync(hdrRequested: false, firstFrameTimeoutMs, attachFrameHandler).ConfigureAwait(false))
            {
                return;
            }
        }

        throw new InvalidOperationException("Unable to acquire recording frame source with live video frames");
    }

    private async Task<bool> TrySetupRecordingFrameReaderCoreAsync(bool hdrRequested, int firstFrameTimeoutMs, bool attachFrameHandler)
    {
        if (_mediaCapture == null)
        {
            return false;
        }

        var candidateSources = _mediaCapture.FrameSources.Values
            .Where(fs =>
                fs.Info.SourceKind == MediaFrameSourceKind.Color &&
                (fs.Info.MediaStreamType == MediaStreamType.VideoRecord ||
                 fs.Info.MediaStreamType == MediaStreamType.VideoPreview))
            .OrderBy(fs => fs.Info.MediaStreamType == MediaStreamType.VideoRecord ? 0 : 1)
            .ThenBy(fs => fs.Info.Id, StringComparer.Ordinal)
            .ToList();

        if (candidateSources.Count == 0)
        {
            candidateSources = _mediaCapture.FrameSources.Values
                .Where(fs => fs.Info.SourceKind == MediaFrameSourceKind.Color)
                .OrderBy(fs => fs.Info.Id, StringComparer.Ordinal)
                .ToList();
        }

        if (candidateSources.Count == 0)
        {
            throw new InvalidOperationException("No color frame sources available for recording");
        }

        Logger.Log(
            $"Recording frame source candidates: {candidateSources.Count} (first-frame timeout: {firstFrameTimeoutMs} ms, hdrRequested={hdrRequested})");
        foreach (var source in candidateSources)
        {
            Logger.Log($"  Candidate source: id={source.Info.Id}, stream={source.Info.MediaStreamType}, kind={source.Info.SourceKind}");
        }

        foreach (var source in candidateSources)
        {
            MediaFrameReader? candidateReader = null;
            try
            {
                string? requestedSubtype = null;
                if (hdrRequested)
                {
                    if (source.Info.MediaStreamType != MediaStreamType.VideoRecord)
                    {
                        Logger.Log($"Skipping non-VideoRecord source for HDR: {source.Info.Id}");
                        continue;
                    }

                    requestedSubtype = ResolveHdrReaderSubtype(source);
                    if (string.IsNullOrWhiteSpace(requestedSubtype))
                    {
                        Logger.Log($"Skipping source without HDR-compatible subtype: {source.Info.Id}");
                        continue;
                    }

                    _recordingReaderRequestedSubtype ??= requestedSubtype;

                    candidateReader = await _mediaCapture.CreateFrameReaderAsync(source, requestedSubtype);
                }
                else
                {
                    candidateReader = await _mediaCapture.CreateFrameReaderAsync(source);
                }

                if (candidateReader == null)
                {
                    Logger.Log($"Failed to create recording frame reader for source {source.Info.Id}");
                    continue;
                }

                var warmupResult = await TryStartReaderAndAwaitFirstFrameAsync(
                    candidateReader,
                    source,
                    firstFrameTimeoutMs).ConfigureAwait(false);
                if (!warmupResult.Success)
                {
                    continue;
                }

                if (hdrRequested)
                {
                    var sourceSubtype = warmupResult.FrameSubtype ?? requestedSubtype ?? source.CurrentFormat?.Subtype;
                    var pixelFormat = warmupResult.FramePixelFormat;
                    var isTrue10Bit = pixelFormat.HasValue && pixelFormat.Value == BitmapPixelFormat.P010;
                    if (!isTrue10Bit)
                    {
                        Logger.Log(
                            $"HDR source rejected: source={source.Info.Id}, stream={source.Info.MediaStreamType}, " +
                            $"subtype={sourceSubtype ?? "unknown"}, observedPixelFormat={pixelFormat?.ToString() ?? "unknown"}");
                        continue;
                    }
                }

                _recordingFrameReader = candidateReader;
                _recordingReaderSourceStreamType = source.Info.MediaStreamType.ToString();
                _recordingReaderSourceSubtype = warmupResult.FrameSubtype ??
                    source.CurrentFormat?.Subtype ??
                    source.SupportedFormats.FirstOrDefault()?.Subtype;
                var warmupPixelFormat = warmupResult.FramePixelFormat?.ToString();
                _firstObservedFramePixelFormat ??= warmupPixelFormat;
                _latestObservedFramePixelFormat = warmupPixelFormat;
                if (attachFrameHandler)
                {
                    AttachRecordingFrameReaderHandler();
                }

                Logger.Log(
                    $"Recording frame reader ready: id={source.Info.Id}, stream={source.Info.MediaStreamType}, " +
                    $"subtype={_recordingReaderSourceSubtype ?? "unknown"}, firstPixelFormat={_firstObservedFramePixelFormat ?? "unknown"}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"Recording frame source rejected ({source.Info.Id}): {ex.Message}");
            }
            finally
            {
                if (candidateReader != null && !ReferenceEquals(candidateReader, _recordingFrameReader))
                {
                    try
                    {
                        await candidateReader.StopAsync();
                    }
                    catch
                    {
                        // Best effort cleanup.
                    }

                    candidateReader.Dispose();
                }
            }
        }

        return false;
    }

    private void AttachRecordingFrameReaderHandler()
    {
        if (_recordingFrameReader == null || _recordingFrameHandlerAttached)
        {
            return;
        }

        _recordingFrameReader.FrameArrived += RecordingFrameReader_FrameArrived;
        _recordingFrameHandlerAttached = true;
    }

    private static string? ResolveHdrReaderSubtype(MediaFrameSource source)
    {
        var preferred = source.SupportedFormats
            .FirstOrDefault(format =>
                !string.IsNullOrWhiteSpace(format.Subtype) &&
                format.Subtype.Contains("P010", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(preferred?.Subtype))
        {
            return preferred.Subtype;
        }

        var hdrFallback = source.SupportedFormats
            .FirstOrDefault(format =>
                !string.IsNullOrWhiteSpace(format.Subtype) &&
                format.Subtype.Contains("HDR", StringComparison.OrdinalIgnoreCase));
        return hdrFallback?.Subtype;
    }

    private void AutoDowngradeHdr(string reason)
    {
        _hdrOutputActive = false;
        _activeVideoInputPixelFormat = DefaultVideoInputPixelFormat;
        _hdrAutoDowngraded = true;
        _hdrAutoDowngradeReason = reason;
        _hdrSourceNot10BitDetected = true;
        _hdrActivationReason = $"AutoDowngradedToSdr ({reason})";
        Logger.Log($"HDR AUTO-DOWNGRADE: {reason}");
        StatusChanged?.Invoke(this, $"HDR disabled for this recording. {reason}");
    }

    private RecordingContext CreateHdrDowngradedRecordingContext(RecordingContext context)
    {
        if (_activeRecordingSettings == null)
        {
            return context;
        }

        var downgradedSettings = new CaptureSettings
        {
            Width = context.Settings.Width,
            Height = context.Settings.Height,
            FrameRate = context.Settings.FrameRate,
            RequestedFrameRateArg = context.Settings.RequestedFrameRateArg,
            RequestedFrameRateNumerator = context.Settings.RequestedFrameRateNumerator,
            RequestedFrameRateDenominator = context.Settings.RequestedFrameRateDenominator,
            RequestedPixelFormat = context.Settings.RequestedPixelFormat,
            Format = context.Settings.Format,
            Quality = context.Settings.Quality,
            CustomBitrateMbps = context.Settings.CustomBitrateMbps,
            HdrEnabled = false,
            HdrOutputMode = HdrOutputMode.Off,
            HdrNominalPeakNits = context.Settings.HdrNominalPeakNits,
            HdrMaxCll = context.Settings.HdrMaxCll,
            HdrMaxFall = context.Settings.HdrMaxFall,
            HdrMasterDisplayMetadata = context.Settings.HdrMasterDisplayMetadata,
            OutputPath = context.Settings.OutputPath,
            AudioEnabled = context.Settings.AudioEnabled,
            UseCustomAudioInput = context.Settings.UseCustomAudioInput,
            AudioDeviceId = context.Settings.AudioDeviceId,
            AudioDeviceName = context.Settings.AudioDeviceName,
            AudioPathMode = context.Settings.AudioPathMode,
            PipelineOptions = context.Settings.PipelineOptions
        };

        return new RecordingContext
        {
            Settings = downgradedSettings,
            VideoOutputPath = context.VideoOutputPath,
            FinalOutputPath = context.FinalOutputPath,
            AudioTempPath = context.AudioTempPath,
            UsePostMuxAudio = context.UsePostMuxAudio,
            AudioDeviceName = context.AudioDeviceName,
            EffectiveFrameRate = context.EffectiveFrameRate,
            FrameRateArg = context.FrameRateArg,
            EffectiveWidth = context.EffectiveWidth,
            EffectiveHeight = context.EffectiveHeight,
            VideoInputPixelFormat = DefaultVideoInputPixelFormat,
            HdrPipelineActive = false
        };
    }

    private static int GetIntFromEnv(string variableName, int defaultValue, int minValue, int maxValue)
    {
        var rawValue = Environment.GetEnvironmentVariable(variableName);
        if (int.TryParse(rawValue, out var parsedValue))
        {
            return Math.Clamp(parsedValue, minValue, maxValue);
        }

        return defaultValue;
    }

    private static bool GetBoolFromEnv(string variableName, bool defaultValue)
    {
        var rawValue = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return defaultValue;
        }

        if (bool.TryParse(rawValue, out var parsedBool))
        {
            return parsedBool;
        }

        if (int.TryParse(rawValue, out var parsedInt))
        {
            return parsedInt != 0;
        }

        return defaultValue;
    }

    private static double ResolveFrameRate(uint numerator, uint denominator, double fallback)
    {
        if (numerator > 0 && denominator > 0)
        {
            return (double)numerator / denominator;
        }

        return fallback;
    }

    private static bool IsFrameRateClose(VideoEncodingProperties properties, double requestedFrameRate, double tolerance = 0.01)
    {
        var fps = ResolveFrameRate(properties.FrameRate.Numerator, properties.FrameRate.Denominator, requestedFrameRate);
        return Math.Abs(fps - requestedFrameRate) <= tolerance;
    }

    private static bool IsHdrSubtype(string? subtype)
        => !string.IsNullOrWhiteSpace(subtype) &&
           (subtype.Contains("P010", StringComparison.OrdinalIgnoreCase) ||
            subtype.Contains("HDR", StringComparison.OrdinalIgnoreCase));

    private static int GetSubtypePreferenceRank(string? subtype, string? requestedPixelFormat, bool hdrRequested)
    {
        if (!string.IsNullOrWhiteSpace(requestedPixelFormat) &&
            !string.IsNullOrWhiteSpace(subtype) &&
            string.Equals(subtype, requestedPixelFormat, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (hdrRequested)
        {
            if (!string.IsNullOrWhiteSpace(subtype) &&
                subtype.Contains("P010", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (IsHdrSubtype(subtype))
            {
                return 2;
            }

            return 100;
        }

        return 10 + MediaFormat.GetPixelFormatPriority(subtype);
    }

    private void SetNegotiatedVideoFormat(VideoEncodingProperties properties, double fallbackFrameRate)
    {
        _actualWidth = properties.Width;
        _actualHeight = properties.Height;
        _actualPixelFormat = properties.Subtype;

        var numerator = properties.FrameRate.Numerator;
        var denominator = properties.FrameRate.Denominator;
        if (numerator > 0 && denominator > 0)
        {
            _actualFrameRateNumerator = numerator;
            _actualFrameRateDenominator = denominator;
            _actualFrameRateArg = $"{numerator}/{denominator}";
            _actualFrameRate = (double)numerator / denominator;
            return;
        }

        _actualFrameRateNumerator = null;
        _actualFrameRateDenominator = null;
        _actualFrameRateArg = null;
        _actualFrameRate = fallbackFrameRate;
    }

    private static bool IsHdrOutputEnabled(CaptureSettings settings)
    {
        return HdrOutputPolicy.IsEnabled(settings);
    }

    private readonly record struct FrameWarmupResult(
        bool Success,
        string? FrameSubtype,
        BitmapPixelFormat? FramePixelFormat);

    private static async Task<FrameWarmupResult> TryStartReaderAndAwaitFirstFrameAsync(
        MediaFrameReader reader,
        MediaFrameSource source,
        int firstFrameTimeoutMs)
    {
        try
        {
            var startResult = await reader.StartAsync();
            Logger.Log($"Recording frame reader start status ({source.Info.Id}): {startResult}");
            if (startResult != MediaFrameReaderStartStatus.Success)
            {
                return default;
            }

            var deadline = Stopwatch.GetTimestamp() +
                (long)(firstFrameTimeoutMs / 1000.0 * Stopwatch.Frequency);
            while (Stopwatch.GetTimestamp() <= deadline)
            {
                using var frame = reader.TryAcquireLatestFrame();
                var videoFrame = frame?.VideoMediaFrame;
                if (videoFrame != null)
                {
                    string? frameSubtype = source.CurrentFormat?.Subtype;
                    BitmapPixelFormat? framePixelFormat = null;

                    if (videoFrame.SoftwareBitmap != null)
                    {
                        framePixelFormat = videoFrame.SoftwareBitmap.BitmapPixelFormat;
                    }
                    else if (videoFrame.Direct3DSurface != null)
                    {
                        using var copied = await SoftwareBitmap.CreateCopyFromSurfaceAsync(
                            videoFrame.Direct3DSurface,
                            BitmapAlphaMode.Ignore);
                        framePixelFormat = copied.BitmapPixelFormat;
                    }

                    Logger.Log(
                        $"Recording frame reader warm-up succeeded for source {source.Info.Id} " +
                        $"(subtype={frameSubtype ?? "unknown"}, pixelFormat={framePixelFormat?.ToString() ?? "unknown"})");
                    return new FrameWarmupResult(
                        Success: true,
                        FrameSubtype: frameSubtype,
                        FramePixelFormat: framePixelFormat);
                }

                await Task.Delay(15).ConfigureAwait(false);
            }

            Logger.Log(
                $"Recording frame reader warm-up timeout for source {source.Info.Id} " +
                $"({source.Info.MediaStreamType}) after {firstFrameTimeoutMs} ms");
            return default;
        }
        catch (ObjectDisposedException)
        {
            return default;
        }
    }

    private async void RecordingFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        // Capture local reference to avoid race condition
        var conversionQueue = _conversionQueue;
        if (conversionQueue == null || _conversionCancellation?.IsCancellationRequested == true) return;

        using var frame = sender.TryAcquireLatestFrame();
        if (frame?.VideoMediaFrame == null) return;

        try
        {
            SoftwareBitmap? softwareBitmap = null;
            var frameIndex = Interlocked.Increment(ref _videoFramesArrived);
            var nowMs = _recordingStopwatch.ElapsedMilliseconds;
            TrackCaptureFrameArrivalCadence();
            var lastArrivalMs = Interlocked.Exchange(ref _lastFrameArrivalMs, nowMs);
            if (Logger.VerboseEnabled && frameIndex == 1)
            {
                Logger.LogVerbose($"First recording frame arrived at {nowMs} ms");
            }

            // Try to get SoftwareBitmap from frame
            if (frame.VideoMediaFrame.SoftwareBitmap != null)
            {
                // Already in CPU memory - create copy to extend lifetime beyond frame disposal
                var copyStartTicks = Logger.VerboseEnabled ? Stopwatch.GetTimestamp() : 0;
                softwareBitmap = SoftwareBitmap.Copy(frame.VideoMediaFrame.SoftwareBitmap);
                if (Logger.VerboseEnabled && copyStartTicks != 0)
                {
                    var copyMs = (Stopwatch.GetTimestamp() - copyStartTicks) * 1000.0 / Stopwatch.Frequency;
                    if (frameIndex == 1 || copyMs >= 5)
                    {
                        Logger.LogVerbose($"SoftwareBitmap copy took {copyMs:0.00} ms");
                    }
                }
            }
            else if (frame.VideoMediaFrame.Direct3DSurface != null)
            {
                // GPU copy MUST happen here (Direct3DSurface lifetime tied to frame)
                // Use BitmapAlphaMode.Ignore because YUY2/NV12 formats don't have alpha
                var copyStartTicks = Logger.VerboseEnabled ? Stopwatch.GetTimestamp() : 0;
                softwareBitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(
                    frame.VideoMediaFrame.Direct3DSurface,
                    BitmapAlphaMode.Ignore);
                if (Logger.VerboseEnabled && copyStartTicks != 0)
                {
                    var copyMs = (Stopwatch.GetTimestamp() - copyStartTicks) * 1000.0 / Stopwatch.Frequency;
                    if (frameIndex == 1 || copyMs >= 5)
                    {
                        Logger.LogVerbose($"Direct3DSurface copy took {copyMs:0.00} ms");
                    }
                }
            }

            if (softwareBitmap == null) return;
            _firstObservedFramePixelFormat ??= softwareBitmap.BitmapPixelFormat.ToString();
            _latestObservedFramePixelFormat = softwareBitmap.BitmapPixelFormat.ToString();
            switch (softwareBitmap.BitmapPixelFormat)
            {
                case BitmapPixelFormat.P010:
                    Interlocked.Increment(ref _observedP010FrameCount);
                    break;
                case BitmapPixelFormat.Nv12:
                    Interlocked.Increment(ref _observedNv12FrameCount);
                    break;
                default:
                    Interlocked.Increment(ref _observedOtherFrameCount);
                    break;
            }

            if (_hdrOutputActive && softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.P010)
            {
                _hdrSourceNot10BitDetected = true;
                if (!_hdrAutoDowngraded && string.IsNullOrWhiteSpace(_hdrAutoDowngradeReason))
                {
                    _hdrAutoDowngradeReason =
                        $"HDR requested but observed runtime frame format {softwareBitmap.BitmapPixelFormat} instead of P010.";
                }
            }

            if (Logger.VerboseEnabled && frameIndex == 1)
            {
                var relTime = frame.SystemRelativeTime?.TotalMilliseconds;
                var relTimeText = relTime.HasValue ? relTime.Value.ToString("0.###", CultureInfo.InvariantCulture) : "n/a";
                Logger.LogVerbose($"First frame details: {softwareBitmap.PixelWidth}x{softwareBitmap.PixelHeight}, fmt={softwareBitmap.BitmapPixelFormat}, relTimeMs={relTimeText}");
            }

            // Queue the unconverted frame (YUY2/NV12) for the conversion worker.
            // Keep this queue latency-bounded and prefer fresh frames under load.
            if (!TryEnqueueConversionFrame(conversionQueue, softwareBitmap))
            {
                Interlocked.Increment(ref _videoFramesDropped);
                Logger.Log("Warning: Conversion queue saturated, dropping newest frame");
                softwareBitmap.Dispose();
            }
            else
            {
                var queuedCount = Interlocked.Increment(ref _videoFramesQueued);
                if (Logger.VerboseEnabled && Interlocked.Exchange(ref _loggedFirstFrameQueued, 1) == 0)
                {
                    Logger.LogVerbose($"First frame queued at {_recordingStopwatch.ElapsedMilliseconds} ms (queueCount={Volatile.Read(ref _conversionQueueDepth)})");
                }

                if (Logger.VerboseEnabled && lastArrivalMs > 0 && queuedCount % 120 == 0)
                {
                    var delta = nowMs - lastArrivalMs;
                    Logger.LogVerbose($"Frame arrival cadence: frame={queuedCount}, delta={delta} ms");
                }
            }

            LogPipelineStatsIfNeeded();
            LogHealthSnapshotIfNeeded();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
        }
    }

    private async Task RunConversionWorkerAsync(CancellationToken cancellationToken)
    {
        Logger.Log("Frame conversion worker started");

        try
        {
            // Get queue reference once (doesn't change)
            var queue = _conversionQueue;
            if (queue == null) return;

            while (!cancellationToken.IsCancellationRequested)
            {
                SoftwareBitmap? sourceBitmap = null;

                try
                {
                    if (!queue.Reader.TryRead(out sourceBitmap))
                    {
                        if (queue.Reader.Completion.IsCompleted)
                        {
                            break;
                        }

                        if (Logger.VerboseEnabled)
                        {
                            var nowMs = _recordingStopwatch.ElapsedMilliseconds;
                            var lastIdle = Interlocked.Read(ref _lastConversionIdleLogMs);
                            if (nowMs - lastIdle >= 1000 &&
                                Interlocked.CompareExchange(ref _lastConversionIdleLogMs, nowMs, lastIdle) == lastIdle)
                            {
                                Logger.LogVerbose($"Conversion worker idle: no frames (queueCount={Volatile.Read(ref _conversionQueueDepth)})");
                            }
                        }
                        await Task.Delay(100, cancellationToken);
                        continue;
                    }
                    Interlocked.Decrement(ref _conversionQueueDepth);

                    // Get sink/encoder references each iteration (may change during startup/shutdown).
                    var sink = _recordingSink;
                    var encoder = _ffmpegEncoder;
                    var sinkUnavailable = sink == null;
                    var ffmpegUnavailable = _recordingBackend == RecordingBackend.Ffmpeg &&
                        (encoder == null || !encoder.IsEncoding);
                    if (sinkUnavailable || ffmpegUnavailable)
                    {
                        // Sink unavailable: drop frame to keep latency bounded.
                        Interlocked.Increment(ref _videoFramesDropped);
                        sourceBitmap.Dispose();
                        sourceBitmap = null;

                        if (Logger.VerboseEnabled)
                        {
                            var nowMs = _recordingStopwatch.ElapsedMilliseconds;
                            var lastNotReady = Interlocked.Read(ref _lastEncoderNotReadyLogMs);
                            if (nowMs - lastNotReady >= 1000 &&
                                Interlocked.CompareExchange(ref _lastEncoderNotReadyLogMs, nowMs, lastNotReady) == lastNotReady)
                            {
                                Logger.LogVerbose("Conversion worker: sink unavailable, dropping frame");
                            }
                        }
                        continue;
                    }

                    // Convert to the encoder input pixel format (NV12 for SDR, P010 for HDR10).
                    var targetPixelFormat = ResolveTargetVideoPixelFormat();
                    SoftwareBitmap convertedFrame;
                    if (sourceBitmap.BitmapPixelFormat == targetPixelFormat)
                    {
                        if (targetPixelFormat == BitmapPixelFormat.Nv12)
                        {
                            Interlocked.Increment(ref _videoFramesDirectNv12);
                        }

                        convertedFrame = sourceBitmap;
                    }
                    else
                    {
                        var convertStartTicks = Logger.VerboseEnabled ? Stopwatch.GetTimestamp() : 0;
                        convertedFrame = SoftwareBitmap.Convert(sourceBitmap, targetPixelFormat, BitmapAlphaMode.Ignore);
                        if (Logger.VerboseEnabled && convertStartTicks != 0)
                        {
                            var convertMs = (Stopwatch.GetTimestamp() - convertStartTicks) * 1000.0 / Stopwatch.Frequency;
                            if (convertMs >= 5)
                            {
                                Logger.LogVerbose($"SoftwareBitmap.Convert to {targetPixelFormat} took {convertMs:0.00} ms");
                            }
                        }
                        if (targetPixelFormat == BitmapPixelFormat.Nv12)
                        {
                            Interlocked.Increment(ref _videoFramesConvertedNv12);
                        }

                        sourceBitmap.Dispose(); // Dispose unconverted frame
                    }

                    // Send converted frame through the active sink contract.
                    var enqueueStartTicks = Logger.VerboseEnabled ? Stopwatch.GetTimestamp() : 0;
                    await sink!.WriteVideoAsync(convertedFrame, cancellationToken);
                    if (Logger.VerboseEnabled && enqueueStartTicks != 0)
                    {
                        var enqueueMs = (Stopwatch.GetTimestamp() - enqueueStartTicks) * 1000.0 / Stopwatch.Frequency;
                        if (enqueueMs >= 5)
                        {
                            Logger.LogVerbose($"Sink WriteVideoAsync took {enqueueMs:0.00} ms");
                        }
                    }

                    // Dispose converted frame (FFmpeg has copied the data)
                    convertedFrame.Dispose();

                    var convertedCount = Interlocked.Increment(ref _videoFramesConverted);
                    var enqueuedCount = Interlocked.Increment(ref _videoFramesEnqueued);
                    if (Logger.VerboseEnabled && Interlocked.Exchange(ref _loggedFirstFrameConverted, 1) == 0)
                    {
                        Logger.LogVerbose($"First frame converted at {_recordingStopwatch.ElapsedMilliseconds} ms");
                    }
                    if (Logger.VerboseEnabled && Interlocked.Exchange(ref _loggedFirstFrameEnqueued, 1) == 0)
                    {
                        Logger.LogVerbose($"First frame enqueued to FFmpeg at {_recordingStopwatch.ElapsedMilliseconds} ms");
                    }
                    if (Logger.VerboseEnabled && convertedCount % 120 == 0)
                    {
                        Logger.LogVerbose($"Conversion progress: converted={convertedCount}, enqueued={enqueuedCount}");
                    }

                    LogPipelineStatsIfNeeded();
                    LogHealthSnapshotIfNeeded();
                }
                catch (OperationCanceledException)
                {
                    sourceBitmap?.Dispose();
                    break;
                }
                catch (Exception ex)
                {
                    sourceBitmap?.Dispose();
                    Logger.LogException(ex);
                }
            }
        }
        finally
        {
            Logger.Log("Frame conversion worker stopped");
        }
    }

    private void TrackCaptureFrameArrivalCadence()
    {
        var nowTick = Stopwatch.GetTimestamp();
        var previousTick = Interlocked.Exchange(ref _captureLastArrivalTick, nowTick);
        if (previousTick <= 0)
        {
            return;
        }

        var intervalMs = (nowTick - previousTick) * 1000.0 / Stopwatch.Frequency;
        if (intervalMs <= 0 || intervalMs > 5000)
        {
            return;
        }

        lock (_captureCadenceLock)
        {
            _captureFrameIntervalWindowMs[_captureFrameIntervalIndex] = intervalMs;
            _captureFrameIntervalIndex = (_captureFrameIntervalIndex + 1) % _captureFrameIntervalWindowMs.Length;
            if (_captureFrameIntervalCount < _captureFrameIntervalWindowMs.Length)
            {
                _captureFrameIntervalCount++;
            }
        }
    }

    private BitmapPixelFormat ResolveTargetVideoPixelFormat()
    {
        return string.Equals(_activeVideoInputPixelFormat, HdrVideoInputPixelFormat, StringComparison.OrdinalIgnoreCase)
            ? BitmapPixelFormat.P010
            : BitmapPixelFormat.Nv12;
    }

    private CaptureCadenceMetrics GetCaptureCadenceMetrics(double expectedFrameRate)
    {
        double[] samples;
        lock (_captureCadenceLock)
        {
            if (_captureFrameIntervalCount <= 0)
            {
                var expectedInterval = expectedFrameRate > 0 ? 1000.0 / expectedFrameRate : 0;
                return new CaptureCadenceMetrics(
                    SampleCount: 0,
                    ObservedFps: 0,
                    ExpectedIntervalMs: expectedInterval,
                    AverageIntervalMs: 0,
                    P95IntervalMs: 0,
                    MaxIntervalMs: 0,
                    JitterStdDevMs: 0,
                    SevereGapCount: 0,
                    EstimatedDroppedFrames: 0,
                    EstimatedDropPercent: 0);
            }

            samples = new double[_captureFrameIntervalCount];
            for (var i = 0; i < _captureFrameIntervalCount; i++)
            {
                var ringIndex = (_captureFrameIntervalIndex - _captureFrameIntervalCount + i + _captureFrameIntervalWindowMs.Length)
                    % _captureFrameIntervalWindowMs.Length;
                samples[i] = _captureFrameIntervalWindowMs[ringIndex];
            }
        }

        var sampleCount = samples.Length;
        if (sampleCount == 0)
        {
            return default;
        }

        var sum = 0.0;
        var max = 0.0;
        for (var i = 0; i < sampleCount; i++)
        {
            sum += samples[i];
            if (samples[i] > max)
            {
                max = samples[i];
            }
        }

        var average = sum / sampleCount;
        var observedFps = average > double.Epsilon ? 1000.0 / average : 0;
        var expectedIntervalMs = expectedFrameRate > 0 ? 1000.0 / expectedFrameRate : average;
        var severeGapThresholdMs = expectedIntervalMs * 2.25;

        var varianceSum = 0.0;
        long severeGaps = 0;
        long estimatedDropped = 0;
        for (var i = 0; i < sampleCount; i++)
        {
            var delta = samples[i] - average;
            varianceSum += delta * delta;

            var interval = samples[i];
            if (interval >= severeGapThresholdMs)
            {
                severeGaps++;
            }

            if (expectedIntervalMs > double.Epsilon)
            {
                var missingFrames = (long)Math.Floor((interval + expectedIntervalMs * 0.20) / expectedIntervalMs) - 1;
                if (missingFrames > 0)
                {
                    estimatedDropped += missingFrames;
                }
            }
        }

        var jitterStdDevMs = Math.Sqrt(varianceSum / sampleCount);
        var sorted = (double[])samples.Clone();
        Array.Sort(sorted);
        var p95Index = (int)Math.Ceiling((sorted.Length - 1) * 0.95);
        var p95IntervalMs = sorted[Math.Clamp(p95Index, 0, sorted.Length - 1)];
        var dropPercent = estimatedDropped <= 0
            ? 0
            : (double)estimatedDropped / Math.Max(1, sampleCount + estimatedDropped) * 100.0;

        return new CaptureCadenceMetrics(
            SampleCount: sampleCount,
            ObservedFps: observedFps,
            ExpectedIntervalMs: expectedIntervalMs,
            AverageIntervalMs: average,
            P95IntervalMs: p95IntervalMs,
            MaxIntervalMs: max,
            JitterStdDevMs: jitterStdDevMs,
            SevereGapCount: severeGaps,
            EstimatedDroppedFrames: estimatedDropped,
            EstimatedDropPercent: dropPercent);
    }

    private void ResetPipelineStats()
    {
        Interlocked.Exchange(ref _videoFramesArrived, 0);
        Interlocked.Exchange(ref _videoFramesQueued, 0);
        Interlocked.Exchange(ref _videoFramesDropped, 0);
        Interlocked.Exchange(ref _videoFramesDroppedFromBacklog, 0);
        Interlocked.Exchange(ref _videoFramesConverted, 0);
        Interlocked.Exchange(ref _videoFramesEnqueued, 0);
        Interlocked.Exchange(ref _videoFramesDirectNv12, 0);
        Interlocked.Exchange(ref _videoFramesConvertedNv12, 0);
        Interlocked.Exchange(ref _lastPipelineLogMs, 0);
        Interlocked.Exchange(ref _lastFrameArrivalMs, 0);
        Interlocked.Exchange(ref _lastConversionIdleLogMs, 0);
        Interlocked.Exchange(ref _lastEncoderNotReadyLogMs, 0);
        Interlocked.Exchange(ref _lastHealthSnapshotLogMs, 0);
        Interlocked.Exchange(ref _loggedFirstFrameArrival, 0);
        Interlocked.Exchange(ref _loggedFirstFrameQueued, 0);
        Interlocked.Exchange(ref _loggedFirstFrameConverted, 0);
        Interlocked.Exchange(ref _loggedFirstFrameEnqueued, 0);
        Interlocked.Exchange(ref _conversionQueueDepth, 0);
        Interlocked.Exchange(ref _captureLastArrivalTick, 0);
        lock (_captureCadenceLock)
        {
            Array.Clear(_captureFrameIntervalWindowMs, 0, _captureFrameIntervalWindowMs.Length);
            _captureFrameIntervalCount = 0;
            _captureFrameIntervalIndex = 0;
        }
    }

    private void LogPipelineStatsIfNeeded()
    {
        if (!Logger.VerboseEnabled || !_recordingStopwatch.IsRunning)
        {
            return;
        }

        var nowMs = _recordingStopwatch.ElapsedMilliseconds;
        var last = Interlocked.Read(ref _lastPipelineLogMs);
        if (nowMs - last < 1000)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _lastPipelineLogMs, nowMs, last) != last)
        {
            return;
        }

        var encoder = _ffmpegEncoder;
        var conversionQueueCount = Volatile.Read(ref _conversionQueueDepth);
        var videoQueueCount = encoder?.VideoQueueCount ?? 0;
        var audioQueueCount = encoder?.AudioQueueCount ?? 0;
        var arrived = Interlocked.Read(ref _videoFramesArrived);
        var queued = Interlocked.Read(ref _videoFramesQueued);
        var dropped = Interlocked.Read(ref _videoFramesDropped);
        var droppedBacklog = Interlocked.Read(ref _videoFramesDroppedFromBacklog);
        var converted = Interlocked.Read(ref _videoFramesConverted);
        var enqueued = Interlocked.Read(ref _videoFramesEnqueued);
        var directNv12 = Interlocked.Read(ref _videoFramesDirectNv12);
        var convertedNv12 = Interlocked.Read(ref _videoFramesConvertedNv12);

        Logger.LogVerbose(
            $"Pipeline: t={nowMs}ms arrived={arrived} queued={queued} dropped={dropped} converted={converted} " +
            $"enqueued={enqueued} nv12Direct={directNv12} nv12Converted={convertedNv12} droppedBacklog={droppedBacklog} " +
            $"convQ={conversionQueueCount} ffmpegVQ={videoQueueCount} ffmpegAQ={audioQueueCount}");
    }

    private void LogHealthSnapshotIfNeeded()
    {
        if (!_recordingStopwatch.IsRunning)
        {
            return;
        }

        var nowMs = _recordingStopwatch.ElapsedMilliseconds;
        var last = Interlocked.Read(ref _lastHealthSnapshotLogMs);
        if (nowMs - last < 5000)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _lastHealthSnapshotLogMs, nowMs, last) != last)
        {
            return;
        }

        Logger.LogStructured("CaptureHealth", GetHealthSnapshot());
    }

    private int ResolveConversionQueueCapacity(CaptureSettings settings)
    {
        _activePipelineOptions = settings.PipelineOptions ?? new RecordingPipelineOptions();
        var frameRate = _actualFrameRate ?? settings.FrameRate;
        return _activePipelineOptions.ResolveVideoQueueCapacity(frameRate);
    }

    private bool TryEnqueueConversionFrame(Channel<SoftwareBitmap> queue, SoftwareBitmap frame)
    {
        if (_conversionCancellation?.IsCancellationRequested == true)
        {
            return false;
        }

        var capacity = Math.Max(1, _conversionQueueCapacity);
        if (_activePipelineOptions.VideoDropPolicy == VideoFrameDropPolicy.DropNewest &&
            Volatile.Read(ref _conversionQueueDepth) >= capacity)
        {
            return false;
        }

        if (queue.Writer.TryWrite(frame))
        {
            var nextDepth = Interlocked.Increment(ref _conversionQueueDepth);
            if (nextDepth > capacity)
            {
                Interlocked.Exchange(ref _conversionQueueDepth, capacity);
            }

            return true;
        }

        if (_activePipelineOptions.VideoDropPolicy == VideoFrameDropPolicy.DropOldest &&
            queue.Reader.TryRead(out var evictedFrame))
        {
            Interlocked.Decrement(ref _conversionQueueDepth);
            Interlocked.Increment(ref _videoFramesDropped);
            var backlogDrops = Interlocked.Increment(ref _videoFramesDroppedFromBacklog);
            if (backlogDrops == 1 || backlogDrops % 60 == 0)
            {
                Logger.Log($"Video backlog drop: {backlogDrops} frame(s) evicted from conversion queue");
            }

            evictedFrame.Dispose();

            if (queue.Writer.TryWrite(frame))
            {
                var depthAfterWrite = Interlocked.Increment(ref _conversionQueueDepth);
                if (depthAfterWrite > capacity)
                {
                    Interlocked.Exchange(ref _conversionQueueDepth, capacity);
                }

                return true;
            }
        }

        return false;
    }

    private void DrainConversionQueueFrames(Channel<SoftwareBitmap>? queue)
    {
        if (queue == null)
        {
            return;
        }

        while (queue.Reader.TryRead(out var frame))
        {
            Interlocked.Decrement(ref _conversionQueueDepth);
            frame.Dispose();
        }

        if (Volatile.Read(ref _conversionQueueDepth) < 0)
        {
            Interlocked.Exchange(ref _conversionQueueDepth, 0);
        }
    }

    private async Task StopConversionWorkerAsync(bool cancelIfStalled)
    {
        if (_conversionQueue != null)
        {
            _conversionQueue.Writer.TryComplete();
        }

        if (_conversionWorkerTask == null)
        {
            return;
        }

        var workerTask = _conversionWorkerTask;
        var completedInTime = await Task.WhenAny(workerTask, Task.Delay(ConversionDrainTimeoutMs)) == workerTask;
        if (!completedInTime && cancelIfStalled)
        {
            Logger.Log($"Conversion worker drain exceeded {ConversionDrainTimeoutMs} ms; canceling worker");
            _conversionCancellation?.Cancel();
            await Task.WhenAny(workerTask, Task.Delay(ConversionCancelGraceMs));
        }

        try
        {
            await workerTask;
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation fallback is used.
        }
        catch (Exception ex)
        {
            Logger.Log($"Conversion worker error during shutdown: {ex.Message}");
        }
        finally
        {
            _conversionWorkerTask = null;
        }
    }

    private async Task SetupRecordingAudioCaptureAsync()
    {
        if (string.IsNullOrEmpty(_audioDeviceId)) return;

        try
        {
            Logger.Log("=== Setting up audio capture for FFmpeg recording ===");

            // Create a separate AudioGraph for recording (the preview graph outputs to speakers)
            var graphSettings = new AudioGraphSettings(AudioRenderCategory.Media)
            {
                QuantumSizeSelectionMode = QuantumSizeSelectionMode.SystemDefault,
                // Request PCM16; AudioGraph may still deliver Float32 frames
                EncodingProperties = AudioEncodingProperties.CreatePcm(48000, 2, 16)
            };

            var graphResult = await AudioGraph.CreateAsync(graphSettings);
            if (graphResult.Status != AudioGraphCreationStatus.Success)
            {
                Logger.Log($"Failed to create recording AudioGraph: {graphResult.Status}");
                ErrorOccurred?.Invoke(this, new Exception($"Failed to create recording AudioGraph: {graphResult.Status}"));
                CleanupRecordingAudioGraph();
                return;
            }

            _recordingAudioGraph = graphResult.Graph;
            var graphFormat = _recordingAudioGraph.EncodingProperties;
            Logger.Log($"Recording audio graph format: {graphFormat.Subtype}, {graphFormat.BitsPerSample} bits, {graphFormat.SampleRate} Hz, {graphFormat.ChannelCount} ch");
            _audioFrameIndex = 0;
            _audioClipFrameCount = 0;
            _lastAudioLogTick = 0;
            _lastAudioClipLogTick = 0;
            _detectedAudioSubtype = null; // Reset for format change detection

            // Create frame output node (captures audio samples)
            // Request PCM16 output; actual frame bytes are detected at runtime
            var outputFormat = AudioEncodingProperties.CreatePcm(48000, 2, 16);
            _audioFrameOutputNode = _recordingAudioGraph.CreateFrameOutputNode(outputFormat);
            _recordingAudioFormat = _audioFrameOutputNode.EncodingProperties;
            Logger.Log($"Recording audio output format: {_recordingAudioFormat.Subtype}, {_recordingAudioFormat.BitsPerSample} bits, {_recordingAudioFormat.SampleRate} Hz, {_recordingAudioFormat.ChannelCount} ch");
            _recordingAudioGraph.QuantumStarted += RecordingAudioGraph_QuantumStarted;

            // Find and connect the audio input device
            var audioDevices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(
                Windows.Devices.Enumeration.DeviceClass.AudioCapture);
            var captureDevice = audioDevices.FirstOrDefault(d => d.Id == _audioDeviceId);

            if (captureDevice == null)
            {
                Logger.Log($"Audio capture device not found: {_audioDeviceId}");
                ErrorOccurred?.Invoke(this, new Exception($"Audio capture device not found for recording"));
                CleanupRecordingAudioGraph();
                return;
            }

            var inputResult = await _recordingAudioGraph.CreateDeviceInputNodeAsync(
                Windows.Media.Capture.MediaCategory.Media,
                _recordingAudioGraph.EncodingProperties,
                captureDevice);
            Logger.Log("Recording audio input node created (default audio processing)");

            if (inputResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                Logger.Log($"Failed to create audio input node: {inputResult.Status}");
                ErrorOccurred?.Invoke(this, new Exception($"Failed to create audio input node: {inputResult.Status}"));
                CleanupRecordingAudioGraph();
                return;
            }

            // Store and connect input to frame output
            _recordingAudioInputNode = inputResult.DeviceInputNode;
            var inputFormat = _recordingAudioInputNode.EncodingProperties;
            Logger.Log($"Recording audio input format: {inputFormat.Subtype}, {inputFormat.BitsPerSample} bits, {inputFormat.SampleRate} Hz, {inputFormat.ChannelCount} ch");
            _recordingAudioInputNode.AddOutgoingConnection(_audioFrameOutputNode);

            // Start the graph
            _recordingAudioGraph.Start();
            Logger.Log("Recording audio graph started");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            CleanupRecordingAudioGraph();
        }
    }

    private void CleanupRecordingAudioGraph()
    {
        try
        {
            if (_recordingAudioGraph != null)
            {
                _recordingAudioGraph.QuantumStarted -= RecordingAudioGraph_QuantumStarted;
                _recordingAudioGraph.Stop();
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Error stopping recording audio graph: {ex.Message}");
        }

        try { _recordingAudioInputNode?.Dispose(); }
        catch (Exception ex) { Logger.Log($"Error disposing recording audio input node: {ex.Message}"); }
        _recordingAudioInputNode = null;

        try { _audioFrameOutputNode?.Dispose(); }
        catch (Exception ex) { Logger.Log($"Error disposing recording audio output node: {ex.Message}"); }
        _audioFrameOutputNode = null;

        try { _recordingAudioGraph?.Dispose(); }
        catch (Exception ex) { Logger.Log($"Error disposing recording audio graph: {ex.Message}"); }
        _recordingAudioGraph = null;
        _recordingAudioFormat = null;
    }

    private void StartAudioCaptureFile(string path)
    {
        try
        {
            _audioFileStream?.Dispose();
            _audioFileStream = new FileStream(
                path,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.Read,
                bufferSize: 128 * 1024,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);

            Interlocked.Exchange(ref _audioBytesWritten, 0);
            Interlocked.Exchange(ref _audioFileWriteDropped, 0);
            WriteWavHeader(_audioFileStream, 0);

            _audioFileWriteQueue = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(AudioWriteQueueCapacity)
            {
                SingleReader = false,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            });
            _audioFileWriteCancellation?.Dispose();
            _audioFileWriteCancellation = new CancellationTokenSource();
            _audioFileWriteTask = Task.Run(() => RunAudioFileWriterAsync(_audioFileWriteCancellation.Token));
            Logger.Log($"Audio capture file created: {path}");
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to create audio capture file: {ex.Message}");
        }
    }

    private async Task StopAudioCaptureFileWriterAsync(bool finalizeHeader, bool cancelPendingWrites)
    {
        var queue = _audioFileWriteQueue;
        if (queue != null)
        {
            try { queue.Writer.TryComplete(); }
            catch (Exception ex) { Logger.Log($"Audio queue completion failed: {ex.Message}"); }
        }

        if (cancelPendingWrites)
        {
            try { _audioFileWriteCancellation?.Cancel(); }
            catch (Exception ex) { Logger.Log($"Audio writer cancellation failed: {ex.Message}"); }
        }

        if (_audioFileWriteTask != null)
        {
            try
            {
                await _audioFileWriteTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested.
            }
            catch (Exception ex)
            {
                Logger.Log($"Audio writer task failed: {ex.Message}");
            }
            _audioFileWriteTask = null;
        }

        _audioFileWriteQueue = null;

        _audioFileWriteCancellation?.Dispose();
        _audioFileWriteCancellation = null;

        if (_audioFileStream == null)
        {
            return;
        }

        try
        {
            if (finalizeHeader)
            {
                var bytesWritten = Interlocked.Read(ref _audioBytesWritten);
                WriteWavHeader(_audioFileStream, bytesWritten);
            }

            await _audioFileStream.FlushAsync();
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to finalize audio file: {ex.Message}");
        }
        finally
        {
            _audioFileStream.Dispose();
            _audioFileStream = null;
        }
    }

    private async Task RunAudioFileWriterAsync(CancellationToken cancellationToken)
    {
        var queue = _audioFileWriteQueue;
        var stream = _audioFileStream;
        if (queue == null || stream == null)
        {
            return;
        }

        try
        {
            while (true)
            {
                var hasPayload = await queue.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false);
                if (!hasPayload)
                {
                    break;
                }

                while (queue.Reader.TryRead(out var payload))
                {
                    if (payload == null || payload.Length == 0)
                    {
                        continue;
                    }

                    await stream.WriteAsync(payload, 0, payload.Length, cancellationToken).ConfigureAwait(false);
                    Interlocked.Add(ref _audioBytesWritten, payload.Length);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when recording stops quickly during shutdown.
        }
        catch (Exception ex)
        {
            Logger.Log($"Audio file writer failed: {ex.Message}");
        }
    }

    private unsafe void QueueAudioBytesForFile(byte* data, int byteCount)
    {
        if (byteCount <= 0)
        {
            return;
        }

        var copy = new byte[byteCount];
        fixed (byte* outputBytes = copy)
        {
            System.Buffer.MemoryCopy(data, outputBytes, byteCount, byteCount);
        }
        QueueAudioBytesForFile(copy, cloneInput: false);
    }

    private void QueueAudioBytesForFile(byte[] data)
    {
        QueueAudioBytesForFile(data, cloneInput: true);
    }

    private void QueueAudioBytesForFile(byte[] data, bool cloneInput)
    {
        if (data.Length == 0)
        {
            return;
        }

        var queue = _audioFileWriteQueue;
        if (queue == null || queue.Reader.Completion.IsCompleted)
        {
            return;
        }

        var payload = data;
        if (cloneInput)
        {
            payload = new byte[data.Length];
            System.Buffer.BlockCopy(data, 0, payload, 0, data.Length);
        }

        if (!queue.Writer.TryWrite(payload))
        {
            if (queue.Reader.TryRead(out _))
            {
                var backlogDropped = Interlocked.Increment(ref _audioFileWriteDropped);
                if (backlogDropped == 1 || backlogDropped % 120 == 0)
                {
                    Logger.Log($"Audio write backlog saturated, dropped oldest chunks: {backlogDropped}");
                }
            }

            if (!queue.Writer.TryWrite(payload))
            {
                var dropped = Interlocked.Increment(ref _audioFileWriteDropped);
                if (dropped == 1 || dropped % 120 == 0)
                {
                    Logger.Log($"Audio write backlog saturated, dropped chunks: {dropped}");
                }
            }
        }
    }

    private void WriteWavHeader(FileStream stream, long dataBytes)
    {
        if (dataBytes < 0)
        {
            dataBytes = 0;
        }

        var blockAlign = (short)(RecordingAudioChannels * (RecordingAudioBitsPerSample / 8));
        var byteRate = RecordingAudioSampleRate * blockAlign;
        var sampleFrames = blockAlign > 0 ? dataBytes / blockAlign : 0;
        var riffSize = 4L + (8 + 28) + (8 + 16) + (8 + dataBytes); // RF64 + ds64 + fmt + data

        stream.Seek(0, SeekOrigin.Begin);
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

        // Always emit RF64 so recordings beyond 4GB remain valid for post-mux workflows.
        writer.Write(Encoding.ASCII.GetBytes("RF64"));
        writer.Write(uint.MaxValue); // RF64 placeholder (actual size in ds64)
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));

        writer.Write(Encoding.ASCII.GetBytes("ds64"));
        writer.Write((uint)28); // ds64 payload size
        writer.Write((ulong)riffSize);
        writer.Write((ulong)dataBytes);
        writer.Write((ulong)sampleFrames);
        writer.Write((uint)0); // table length

        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write((uint)16);
        writer.Write((ushort)3); // IEEE float
        writer.Write((ushort)RecordingAudioChannels);
        writer.Write((uint)RecordingAudioSampleRate);
        writer.Write((uint)byteRate);
        writer.Write((ushort)blockAlign);
        writer.Write((ushort)RecordingAudioBitsPerSample);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(uint.MaxValue); // RF64 placeholder (actual size in ds64)
        writer.Flush();
    }

    private async Task<bool> MuxAudioIntoVideoAsync(string videoPath, string audioPath, string outputPath)
    {
        try
        {
            if (!File.Exists(videoPath))
            {
                Logger.Log($"Mux failed: video file not found: {videoPath}");
                return false;
            }

            if (!File.Exists(audioPath))
            {
                Logger.Log($"Mux failed: audio file not found: {audioPath}");
                return false;
            }

            var ffmpegPath = string.IsNullOrWhiteSpace(_ffmpegPathForMux) ? "ffmpeg.exe" : _ffmpegPathForMux;
            var args = $"-y -i \"{videoPath}\" -i \"{audioPath}\" -c:v copy -c:a aac -b:a 320k -shortest -movflags +faststart \"{outputPath}\"";

            Logger.Log($"Muxing audio into video: {outputPath}");
            var result = await _processSupervisor.RunAsync(new ProcessSpec
            {
                FileName = ffmpegPath,
                Arguments = args,
                TimeoutMs = MuxTimeoutMs,
                WorkingDirectory = Path.GetDirectoryName(outputPath)
            });

            if (!result.Started)
            {
                var reason = result.StartException?.Message ?? "process could not start";
                Logger.Log($"Mux failed: {reason}");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(result.StdOut))
            {
                foreach (var line in result.StdOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    Logger.Log($"[FFmpeg Mux][stdout] {line}");
                }
            }

            if (!string.IsNullOrWhiteSpace(result.StdErr))
            {
                foreach (var line in result.StdErr.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    Logger.Log($"[FFmpeg Mux][stderr] {line}");
                }
            }

            if (result.TimedOut)
            {
                Logger.Log($"Mux failed: ffmpeg timed out after {MuxTimeoutMs} ms");
                return false;
            }

            if (result.ExitCode != 0)
            {
                Logger.Log($"Mux failed: ffmpeg exited with code {result.ExitCode}");
                return false;
            }

            Logger.Log("Mux completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log($"Mux failed: {ex.Message}");
            return false;
        }
    }

    private async Task CleanupRecordingResourcesOnErrorAsync()
    {
        await StopConversionWorkerAsync(cancelIfStalled: true);

        if (_conversionQueue != null)
        {
            DrainConversionQueueFrames(_conversionQueue);
            _conversionQueue = null;
        }
        Interlocked.Exchange(ref _conversionQueueDepth, 0);

        _conversionCancellation?.Dispose();
        _conversionCancellation = null;

        if (_recordingFrameReader != null)
        {
            try
            {
                if (_recordingFrameHandlerAttached)
                {
                    _recordingFrameReader.FrameArrived -= RecordingFrameReader_FrameArrived;
                    _recordingFrameHandlerAttached = false;
                }
                await _recordingFrameReader.StopAsync();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error stopping recording frame reader after failure: {ex.Message}");
            }
            finally
            {
                _recordingFrameReader.Dispose();
                _recordingFrameReader = null;
            }
        }
        _recordingReaderRequestedSubtype = null;
        _firstObservedFramePixelFormat = null;
        _latestObservedFramePixelFormat = null;

        CleanupRecordingAudioGraph();

        if (_recordingSink != null)
        {
            try { await _recordingSink.StopAsync(); }
            catch (Exception ex) { Logger.Log($"Error stopping recording sink after failure: {ex.Message}"); }
            try { await _recordingSink.DisposeAsync(); }
            catch (Exception ex) { Logger.Log($"Error disposing recording sink after failure: {ex.Message}"); }
            _recordingSink = null;
        }

        if (_postMuxAudioEnabled)
        {
            await StopAudioCaptureFileWriterAsync(finalizeHeader: false, cancelPendingWrites: true);
        }

        try
        {
            await _artifactManager.RollbackAsync(_recordingContext);
        }
        catch (Exception ex)
        {
            Logger.Log($"Error rolling back recording artifacts: {ex.Message}");
        }

        _recordingContext = null;
        _recordingBackend = RecordingBackend.None;
        _postMuxAudioEnabled = false;
        _audioTempPath = null;
        _finalOutputPath = null;
        _recordingFile = null;
        _ffmpegPathForMux = null;
        if (_ffmpegEncoder != null)
        {
            try { _ffmpegEncoder.Dispose(); }
            catch (Exception ex) { Logger.Log($"Error disposing FFmpeg after failure: {ex.Message}"); }
            _ffmpegEncoder = null;
        }
    }

    private void RecordingAudioGraph_QuantumStarted(AudioGraph sender, object args)
    {
        // Capture local references to avoid race conditions with StopRecordingAsync
        var sink = _recordingSink;
        var outputNode = _audioFrameOutputNode;
        var usePostMux = _postMuxAudioEnabled;

        // Don't check IsEncoding here - we want to buffer audio samples while FFmpeg is starting up
        // The sink handles accepting/rejecting queued samples while the backend is starting.
        if (outputNode == null) return;
        if (!usePostMux && sink == null) return;

        try
        {
            using var frame = outputNode.GetFrame();
            if (frame == null) return;

            using (var buffer = frame.LockBuffer(AudioBufferAccessMode.Read))
            using (var reference = buffer.CreateReference())
            {
                // Get the raw audio bytes
                unsafe
                {
                    byte* dataInBytes;
                    uint capacityInBytes;
                    var byteAccess = reference.As<IMemoryBufferByteAccess>();
                    byteAccess.GetBuffer(out dataInBytes, out capacityInBytes);

                    var byteCount = (int)Math.Min(buffer.Length, capacityInBytes);
                    if (byteCount > 0)
                    {
                        var audioFormat = _recordingAudioFormat;
                        var declaredSubtype = audioFormat?.Subtype;
                        var declaredBits = audioFormat?.BitsPerSample ?? 16;
                        var channelCount = audioFormat?.ChannelCount ?? 0u;
                        var sampleRate = audioFormat?.SampleRate ?? 0u;
                        var samplesPerQuantum = sender.SamplesPerQuantum;
                        var effectiveSubtype = declaredSubtype;
                        var bitsPerSample = declaredBits;
                        string? formatOverride = null;

                        // Pre-calculate expected byte counts (used for format detection and logging)
                        var expectedBytesPcm16 = channelCount > 0 && samplesPerQuantum > 0
                            ? (long)samplesPerQuantum * channelCount * 2 : 0;
                        var expectedBytes32 = channelCount > 0 && samplesPerQuantum > 0
                            ? (long)samplesPerQuantum * channelCount * 4 : 0;

                        // Trust the BYTE COUNT over the declared format - Windows audio APIs often lie about format
                        // If we get 32-bit sized data, treat it as Float32 regardless of what format claims
                        if (expectedBytes32 > 0 && byteCount == expectedBytes32 && bitsPerSample != 32)
                        {
                            // Data is 32-bit sized - treat as Float32 (most common case for AudioGraph)
                            effectiveSubtype = MediaEncodingSubtypes.Float;
                            bitsPerSample = 32;
                            formatOverride = $"Override to Float32 based on byte count (declared={declaredSubtype}/{declaredBits}, byteCount={byteCount}, expected16={expectedBytesPcm16}, expected32={expectedBytes32})";
                        }
                        else if (expectedBytesPcm16 > 0 && byteCount == expectedBytesPcm16 && bitsPerSample != 16)
                        {
                            // Data is 16-bit sized - treat as PCM16
                            effectiveSubtype = MediaEncodingSubtypes.Pcm;
                            bitsPerSample = 16;
                            formatOverride = $"Override to PCM16 based on byte count (declared={declaredSubtype}/{declaredBits}, byteCount={byteCount}, expected16={expectedBytesPcm16}, expected32={expectedBytes32})";
                        }
                        var declaredMatches = !string.IsNullOrEmpty(declaredSubtype) &&
                                              string.Equals(declaredSubtype, effectiveSubtype, StringComparison.OrdinalIgnoreCase);
                        var bitsMatch = declaredBits == bitsPerSample;
                        var formatMatches = declaredMatches && bitsMatch && string.IsNullOrWhiteSpace(formatOverride);
                        var frameIndex = Interlocked.Increment(ref _audioFrameIndex);
                        var isFirstFrame = frameIndex == 1;
                        var nowTick = Environment.TickCount64;
                        var shouldLog = nowTick - Interlocked.Read(ref _lastAudioLogTick) >= 1000;

                        // First-frame comprehensive logging for debugging audio issues
                        if (isFirstFrame)
                        {
                            Logger.Log($"=== Audio Format Detection (first frame) ===");
                            Logger.Log($"Declared format: subtype={declaredSubtype}, bits={declaredBits}, Hz={sampleRate}, ch={channelCount}");
                            Logger.Log($"Buffer: byteCount={byteCount}, samplesPerQuantum={samplesPerQuantum}");
                            if (expectedBytesPcm16 > 0)
                            {
                                Logger.Log($"Expected PCM16 bytes: {expectedBytesPcm16}, Expected 32-bit bytes: {expectedBytes32}");
                            }
                            if (!string.IsNullOrWhiteSpace(formatOverride))
                            {
                                Logger.Log($"AUDIO FORMAT OVERRIDE: {formatOverride}");
                            }
                            Logger.Log($"Effective format: subtype={effectiveSubtype}, bits={bitsPerSample}");

                            // Warn about unexpected byte counts
                            if (expectedBytesPcm16 > 0 && byteCount != expectedBytesPcm16 && byteCount != expectedBytes32)
                            {
                                Logger.Log($"WARNING: Audio byte count {byteCount} doesn't match expected PCM16 ({expectedBytesPcm16}) or 32-bit ({expectedBytes32})");
                                Logger.Log($"This may indicate audio format detection issues - check audio output for corruption");
                            }

                            _detectedAudioSubtype = effectiveSubtype;
                        }
                        else if (_detectedAudioSubtype != effectiveSubtype)
                        {
                            // Format changed mid-recording - this is unusual and worth logging
                            Logger.Log($"WARNING: Audio format changed from {_detectedAudioSubtype} to {effectiveSubtype} at frame {frameIndex}");
                            _detectedAudioSubtype = effectiveSubtype;
                        }

                        double rms = 0;
                        double peak = 0;
                        double minSample = 0;
                        double maxSample = 0;
                        double meanSample = 0;
                        bool clipped = false;
                        var bytesPerFrame = 0;
                        if (channelCount > 0 && bitsPerSample > 0 && bitsPerSample % 8 == 0)
                        {
                            bytesPerFrame = (int)(bitsPerSample / 8) * (int)channelCount;
                        }

                        if (effectiveSubtype == MediaEncodingSubtypes.Float)
                        {
                            // Pass through Float32 samples (f32le)
                            var sampleCount = byteCount / sizeof(float);
                            if (sampleCount <= 0) return;

                            var inputSamples = (float*)dataInBytes;
                            double sumSquares = 0;
                            double sum = 0;
                            float peakAbs = 0;
                            float minVal = float.MaxValue;
                            float maxVal = float.MinValue;
                            for (var i = 0; i < sampleCount; i++)
                            {
                                var rawSample = inputSamples[i];
                                if (rawSample < minVal) minVal = rawSample;
                                if (rawSample > maxVal) maxVal = rawSample;
                                var abs = Math.Abs(rawSample);
                                if (abs > peakAbs) peakAbs = abs;
                                if (abs > 1.0f) clipped = true;
                                sumSquares += rawSample * rawSample;
                                sum += rawSample;
                            }
                            peak = peakAbs;
                            rms = Math.Sqrt(sumSquares / sampleCount);
                            minSample = minVal;
                            maxSample = maxVal;
                            meanSample = sum / sampleCount;

                            if (usePostMux)
                            {
                                QueueAudioBytesForFile(dataInBytes, byteCount);
                            }
                            else
                            {
                                var audioData = new byte[byteCount];
                                fixed (byte* outputBytes = audioData)
                                {
                                    System.Buffer.MemoryCopy(dataInBytes, outputBytes, byteCount, byteCount);
                                }
                                QueueAudioSamplesToSink(sink, audioData);
                            }
                        }
                        else if (effectiveSubtype == MediaEncodingSubtypes.Pcm && bitsPerSample == 32)
                        {
                            // Convert 32-bit PCM to Float32 (f32le)
                            var sampleCount = byteCount / sizeof(int);
                            if (sampleCount <= 0) return;
                            var audioData = new byte[sampleCount * sizeof(float)];
                            fixed (byte* outputBytes = audioData)
                            {
                                var inputSamples = (int*)dataInBytes;
                                var outputSamples = (float*)outputBytes;
                                double sumSquares = 0;
                                double sum = 0;
                                float peakAbs = 0;
                                float minVal = float.MaxValue;
                                float maxVal = float.MinValue;
                                const float divisor = 2147483648f;
                                for (var i = 0; i < sampleCount; i++)
                                {
                                    var rawSample = inputSamples[i] / divisor;
                                    outputSamples[i] = rawSample;
                                    if (rawSample < minVal) minVal = rawSample;
                                    if (rawSample > maxVal) maxVal = rawSample;
                                    var abs = Math.Abs(rawSample);
                                    if (abs > peakAbs) peakAbs = abs;
                                    if (abs > 1.0f) clipped = true;
                                    sumSquares += rawSample * rawSample;
                                    sum += rawSample;
                                }
                                peak = peakAbs;
                                rms = Math.Sqrt(sumSquares / sampleCount);
                                minSample = minVal;
                                maxSample = maxVal;
                                meanSample = sum / sampleCount;
                            }
                            if (usePostMux)
                            {
                                QueueAudioBytesForFile(audioData);
                            }
                            else
                            {
                                QueueAudioSamplesToSink(sink, audioData);
                            }
                        }
                        else
                        {
                            // Convert PCM16 (or other) to Float32 (f32le)
                            var sampleCount = byteCount / sizeof(short);
                            if (sampleCount <= 0) return;
                            var audioData = new byte[sampleCount * sizeof(float)];
                            fixed (byte* outputBytes = audioData)
                            {
                                var inputSamples = (short*)dataInBytes;
                                var outputSamples = (float*)outputBytes;
                                double sumSquares = 0;
                                double sum = 0;
                                float peakAbs = 0;
                                float minVal = float.MaxValue;
                                float maxVal = float.MinValue;
                                const float divisor = 32768f;
                                for (var i = 0; i < sampleCount; i++)
                                {
                                    var rawSample = inputSamples[i] / divisor;
                                    outputSamples[i] = rawSample;
                                    if (rawSample < minVal) minVal = rawSample;
                                    if (rawSample > maxVal) maxVal = rawSample;
                                    var abs = Math.Abs(rawSample);
                                    if (abs > peakAbs) peakAbs = abs;
                                    if (abs > 1.0f) clipped = true;
                                    sumSquares += rawSample * rawSample;
                                    sum += rawSample;
                                }
                                peak = peakAbs;
                                rms = Math.Sqrt(sumSquares / sampleCount);
                                minSample = minVal;
                                maxSample = maxVal;
                                meanSample = sum / sampleCount;
                            }
                            if (usePostMux)
                            {
                                QueueAudioBytesForFile(audioData);
                            }
                            else
                            {
                                QueueAudioSamplesToSink(sink, audioData);
                            }
                        }

                        if (clipped && formatMatches)
                        {
                            var clipCount = Interlocked.Increment(ref _audioClipFrameCount);
                            if (clipCount == 1 || nowTick - Interlocked.Read(ref _lastAudioClipLogTick) >= 1000)
                            {
                                Interlocked.Exchange(ref _lastAudioClipLogTick, nowTick);
                                Logger.Log($"Audio clipping detected (frame {frameIndex}). Peak={peak:0.000} RMS={rms:0.000} bytes={byteCount} subtype={effectiveSubtype} bits={bitsPerSample}");
                            }
                        }
                        else if (clipped && !formatMatches)
                        {
                            var clipCount = Interlocked.Read(ref _audioClipFrameCount);
                            if (clipCount == 0 || nowTick - Interlocked.Read(ref _lastAudioClipLogTick) >= 1000)
                            {
                                Interlocked.Exchange(ref _lastAudioClipLogTick, nowTick);
                                Logger.Log($"Audio over 0 dBFS detected but format mismatch (frame {frameIndex}). Peak={peak:0.000} RMS={rms:0.000} declared={declaredSubtype}/{declaredBits} effective={effectiveSubtype}/{bitsPerSample}");
                            }
                        }

                        if (shouldLog)
                        {
                            Interlocked.Exchange(ref _lastAudioLogTick, nowTick);
                            var sampleCount = bytesPerFrame > 0 ? byteCount / bytesPerFrame : 0;
                            var formatSummary = $"subtype={effectiveSubtype} bits={bitsPerSample} Hz={sampleRate} ch={channelCount} bytes={byteCount}";
                            var sampleSummary = bytesPerFrame > 0
                                ? $" frames={sampleCount} bytesPerFrame={bytesPerFrame} samplesPerQuantum={sender.SamplesPerQuantum}"
                                : " frames=? bytesPerFrame=0";
                            Logger.Log($"Audio stats (frame {frameIndex}): Peak={peak:0.000} RMS={rms:0.000} mean={meanSample:0.000} min={minSample:0.000} max={maxSample:0.000} {formatSummary}{sampleSummary} clipFrames={Interlocked.Read(ref _audioClipFrameCount)}");
                            if (!string.IsNullOrWhiteSpace(formatOverride))
                            {
                                Logger.Log($"Audio format override: declared={declaredSubtype}/{declaredBits} effective={effectiveSubtype}/{bitsPerSample} {formatOverride}");
                            }
                            if (bytesPerFrame > 0 && byteCount % bytesPerFrame != 0)
                            {
                                Logger.Log($"Audio format mismatch: byteCount={byteCount} is not aligned to bytesPerFrame={bytesPerFrame} (subtype={effectiveSubtype} bits={bitsPerSample} ch={channelCount})");
                            }
                        }

                        if (AudioLevelUpdated != null)
                        {
                            var lastTick = Interlocked.Read(ref _lastAudioLevelUpdateTick);
                            if (nowTick - lastTick >= 50 &&
                                Interlocked.CompareExchange(ref _lastAudioLevelUpdateTick, nowTick, lastTick) == lastTick)
                            {
                                var peakLevel = Math.Clamp(peak, 0.0, 1.0);
                                var rmsLevel = Math.Clamp(rms, 0.0, 1.0);
                                var clipForUi = formatMatches && clipped;
                                AudioLevelUpdated?.Invoke(this, new AudioLevelEventArgs(peakLevel, rmsLevel, clipForUi));
                            }
                        }
                    }
                }
            }
        }
        catch (ObjectDisposedException)
        {
            // Output node was disposed during cleanup - this is expected
        }
        catch (Exception ex)
        {
            LogComInteropAwareError("Audio frame error", ex);
        }
    }

    private void QueueAudioSamplesToSink(IRecordingSink? sink, byte[] audioData)
    {
        if (sink == null || audioData.Length == 0)
        {
            return;
        }

        var writeTask = sink.WriteAudioAsync(audioData);
        if (!writeTask.IsCompletedSuccessfully)
        {
            _ = writeTask.ContinueWith(
                t => Logger.Log($"Audio sink write failed: {t.Exception?.GetBaseException().Message}"),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }
    }

    private static bool IsComInteropDisabledException(Exception ex)
        => ex is NotSupportedException &&
           ex.Message.Contains("Built-in COM has been disabled", StringComparison.OrdinalIgnoreCase);

    private void LogComInteropAwareError(string prefix, Exception ex)
    {
        if (!IsComInteropDisabledException(ex))
        {
            Logger.Log($"{prefix}: {ex.Message}");
            return;
        }

        var nowTick = Environment.TickCount64;
        var lastTick = Interlocked.Read(ref _lastComInteropErrorLogTick);
        if (nowTick - lastTick < ComInteropErrorLogIntervalMs ||
            Interlocked.CompareExchange(ref _lastComInteropErrorLogTick, nowTick, lastTick) != lastTick)
        {
            Interlocked.Increment(ref _suppressedComInteropErrorCount);
            return;
        }

        var suppressed = Interlocked.Exchange(ref _suppressedComInteropErrorCount, 0);
        var suffix = suppressed > 0 ? $" (suppressed repeats: {suppressed})" : string.Empty;
        Logger.Log($"{prefix}: {ex.Message}{suffix}");
    }

    private void PreviewAudioGraph_QuantumStarted(AudioGraph sender, object args)
    {
        if (_isRecording)
        {
            return;
        }

        var outputNode = _previewAudioFrameOutputNode;
        if (outputNode == null) return;

        try
        {
            using var frame = outputNode.GetFrame();
            if (frame == null) return;

            using (var buffer = frame.LockBuffer(AudioBufferAccessMode.Read))
            using (var reference = buffer.CreateReference())
            {
                unsafe
                {
                    byte* dataInBytes;
                    uint capacityInBytes;
                    var byteAccess = reference.As<IMemoryBufferByteAccess>();
                    byteAccess.GetBuffer(out dataInBytes, out capacityInBytes);

                    var byteCount = (int)Math.Min(buffer.Length, capacityInBytes);
                    if (byteCount <= 0)
                    {
                        return;
                    }

                    var audioFormat = _previewAudioFormat;
                    var declaredSubtype = audioFormat?.Subtype;
                    var declaredBits = audioFormat?.BitsPerSample ?? 16;
                    var channelCount = audioFormat?.ChannelCount ?? 0u;
                    var samplesPerQuantum = sender.SamplesPerQuantum;
                    var effectiveSubtype = declaredSubtype;
                    var bitsPerSample = declaredBits;

                    var expectedBytesPcm16 = channelCount > 0 && samplesPerQuantum > 0
                        ? (long)samplesPerQuantum * channelCount * 2 : 0;
                    var expectedBytes32 = channelCount > 0 && samplesPerQuantum > 0
                        ? (long)samplesPerQuantum * channelCount * 4 : 0;

                    if (expectedBytes32 > 0 && byteCount == expectedBytes32 && bitsPerSample != 32)
                    {
                        effectiveSubtype = MediaEncodingSubtypes.Float;
                        bitsPerSample = 32;
                    }
                    else if (expectedBytesPcm16 > 0 && byteCount == expectedBytesPcm16 && bitsPerSample != 16)
                    {
                        effectiveSubtype = MediaEncodingSubtypes.Pcm;
                        bitsPerSample = 16;
                    }

                    var declaredMatches = !string.IsNullOrEmpty(declaredSubtype) &&
                                          string.Equals(declaredSubtype, effectiveSubtype, StringComparison.OrdinalIgnoreCase);
                    var bitsMatch = declaredBits == bitsPerSample;
                    var formatMatches = declaredMatches && bitsMatch;

                    double rms = 0;
                    double peak = 0;
                    bool clipped = false;

                    if (effectiveSubtype == MediaEncodingSubtypes.Float)
                    {
                        var sampleCount = byteCount / sizeof(float);
                        if (sampleCount <= 0) return;

                        var inputSamples = (float*)dataInBytes;
                        double sumSquares = 0;
                        float peakAbs = 0;
                        for (var i = 0; i < sampleCount; i++)
                        {
                            var rawSample = inputSamples[i];
                            var abs = Math.Abs(rawSample);
                            if (abs > peakAbs) peakAbs = abs;
                            if (abs > 1.0f) clipped = true;
                            sumSquares += rawSample * rawSample;
                        }
                        peak = peakAbs;
                        rms = Math.Sqrt(sumSquares / sampleCount);
                    }
                    else if (effectiveSubtype == MediaEncodingSubtypes.Pcm && bitsPerSample == 32)
                    {
                        var sampleCount = byteCount / sizeof(int);
                        if (sampleCount <= 0) return;

                        var inputSamples = (int*)dataInBytes;
                        const float divisor = 2147483648f;
                        double sumSquares = 0;
                        float peakAbs = 0;
                        for (var i = 0; i < sampleCount; i++)
                        {
                            var rawSample = inputSamples[i] / divisor;
                            var abs = Math.Abs(rawSample);
                            if (abs > peakAbs) peakAbs = abs;
                            if (abs > 1.0f) clipped = true;
                            sumSquares += rawSample * rawSample;
                        }
                        peak = peakAbs;
                        rms = Math.Sqrt(sumSquares / sampleCount);
                    }
                    else
                    {
                        var sampleCount = byteCount / sizeof(short);
                        if (sampleCount <= 0) return;

                        var inputSamples = (short*)dataInBytes;
                        const float divisor = 32768f;
                        double sumSquares = 0;
                        float peakAbs = 0;
                        for (var i = 0; i < sampleCount; i++)
                        {
                            var rawSample = inputSamples[i] / divisor;
                            var abs = Math.Abs(rawSample);
                            if (abs > peakAbs) peakAbs = abs;
                            if (abs > 1.0f) clipped = true;
                            sumSquares += rawSample * rawSample;
                        }
                        peak = peakAbs;
                        rms = Math.Sqrt(sumSquares / sampleCount);
                    }

                    if (AudioLevelUpdated != null)
                    {
                        var nowTick = Environment.TickCount64;
                        var lastTick = Interlocked.Read(ref _lastAudioLevelUpdateTick);
                        if (nowTick - lastTick >= 50 &&
                            Interlocked.CompareExchange(ref _lastAudioLevelUpdateTick, nowTick, lastTick) == lastTick)
                        {
                            var peakLevel = Math.Clamp(peak, 0.0, 1.0);
                            var rmsLevel = Math.Clamp(rms, 0.0, 1.0);
                            var clipForUi = formatMatches && clipped;
                            AudioLevelUpdated?.Invoke(this, new AudioLevelEventArgs(peakLevel, rmsLevel, clipForUi));
                        }
                    }
                }
            }
        }
        catch (ObjectDisposedException)
        {
            // Output node was disposed during cleanup - this is expected
        }
        catch (Exception ex)
        {
            LogComInteropAwareError("Audio preview frame error", ex);
        }
    }

    private async Task StartUncompressedRecordingAsync()
    {
        if (_mediaCapture == null || _recordingFile == null || _recordingContext == null)
        {
            return;
        }

        _recordingBackend = RecordingBackend.None;

        // Find a frame source from the already-initialized MediaCapture
        var videoSourceInfo = _mediaCapture.FrameSources.Values
            .Select(fs => fs.Info)
            .FirstOrDefault(info =>
                info.MediaStreamType == MediaStreamType.VideoRecord &&
                info.SourceKind == MediaFrameSourceKind.Color);

        if (videoSourceInfo == null)
        {
            _recordingBackend = RecordingBackend.MediaCaptureFallback;
            _recordingSink = new MediaCaptureFallbackSink(_mediaCapture);
            await _recordingSink.StartAsync(_recordingContext);
            Logger.Log("Started AVI fallback recording with standard API (CFR mode)");
            return;
        }

        // Initialize AVI writer for true uncompressed
        _recordingBackend = RecordingBackend.AviWriter;
        _recordingSink = new AviRecordingSink();
        await _recordingSink.StartAsync(_recordingContext);

        var frameSource = _mediaCapture.FrameSources[videoSourceInfo.Id];
        _frameReader = await _mediaCapture.CreateFrameReaderAsync(frameSource);
        _frameReader.FrameArrived += FrameReader_FrameArrived;

        await _frameReader.StartAsync();
    }

    private ulong _frameCount;

    private async void FrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        // Capture local reference to avoid race condition with StopRecordingAsync.
        var sink = _recordingSink;

        using var frame = sender.TryAcquireLatestFrame();
        if (frame?.VideoMediaFrame?.SoftwareBitmap == null) return;

        var softwareBitmap = frame.VideoMediaFrame.SoftwareBitmap;
        SoftwareBitmap? convertedBitmap = null;

        try
        {
            // Convert to BGRA8 if needed
            if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8)
            {
                convertedBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8);
                softwareBitmap = convertedBitmap;
            }

            if (sink != null)
            {
                await sink.WriteVideoAsync(softwareBitmap);
            }
            _frameCount++;
            FrameCaptured?.Invoke(this, _frameCount);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
        }
        finally
        {
            convertedBitmap?.Dispose();
        }
    }

    public Task StopRecordingAsync()
        => RunSessionTransitionAsync(
            nameof(StopRecordingAsync),
            CaptureSessionState.Ready,
            StopRecordingCoreAsync);

    private async Task StopRecordingCoreAsync()
    {
        if (!_isRecording) return;

        try
        {
            Logger.Log("=== Stopping recording ===");
            Logger.LogEvent("CAP-REC-STOP", $"backend={_recordingBackend}");
            var shouldPostMux = _postMuxAudioEnabled &&
                                _recordingContext?.UsePostMuxAudio == true &&
                                !string.IsNullOrEmpty(_recordingContext.AudioTempPath) &&
                                !string.IsNullOrEmpty(_recordingContext.FinalOutputPath);
            var stopStatus = "Stopped";
            var tempVideoPath = _recordingContext?.VideoOutputPath ?? _recordingFile?.Path;
            var audioTempPath = _recordingContext?.AudioTempPath ?? _audioTempPath;
            var finalOutputPath = _recordingContext?.FinalOutputPath ?? _finalOutputPath;
            var muxFailureReason = (string?)null;
            var muxed = true;
            var encoderDropped = _ffmpegEncoder?.DroppedVideoFrames ?? 0;

            // Stop recording frame reader (FFmpeg recording)
            if (_recordingFrameReader != null)
            {
                if (_recordingFrameHandlerAttached)
                {
                    _recordingFrameReader.FrameArrived -= RecordingFrameReader_FrameArrived;
                    _recordingFrameHandlerAttached = false;
                }
                await _recordingFrameReader.StopAsync();
                _recordingFrameReader.Dispose();
                _recordingFrameReader = null;
                Logger.Log("Recording frame reader stopped");
            }

            // Stop recording audio graph
            if (_recordingAudioGraph != null)
            {
                CleanupRecordingAudioGraph();
                Logger.Log("Recording audio graph stopped");
            }

            // Stop conversion worker
            if (_conversionQueue != null)
            {
                Logger.Log("Waiting for conversion queue to drain...");
            }
            await StopConversionWorkerAsync(cancelIfStalled: true);

            // Dispose conversion queue and remaining frames
            if (_conversionQueue != null)
            {
                DrainConversionQueueFrames(_conversionQueue);
                _conversionQueue = null;
            }
            Interlocked.Exchange(ref _conversionQueueDepth, 0);

            _conversionCancellation?.Dispose();
            _conversionCancellation = null;

            // Legacy: Stop AVI frame reader
            if (_frameReader != null)
            {
                _frameReader.FrameArrived -= FrameReader_FrameArrived;
                await _frameReader.StopAsync();
                _frameReader.Dispose();
                _frameReader = null;
            }

            FinalizeResult sinkResult = FinalizeResult.Success(finalOutputPath ?? string.Empty, "Stopped");
            if (_recordingSink != null)
            {
                sinkResult = await _recordingSink.StopAsync();
                await _recordingSink.DisposeAsync();
                _recordingSink = null;
            }
            _ffmpegEncoder = null;

            if (_recordingStopwatch.IsRunning)
            {
                if (Logger.VerboseEnabled)
                {
                    var durationMs = _recordingStopwatch.ElapsedMilliseconds;
                    Logger.LogVerbose(
                        $"Pipeline summary: durationMs={durationMs} arrived={Interlocked.Read(ref _videoFramesArrived)} " +
                        $"queued={Interlocked.Read(ref _videoFramesQueued)} dropped={Interlocked.Read(ref _videoFramesDropped)} " +
                        $"droppedBacklog={Interlocked.Read(ref _videoFramesDroppedFromBacklog)} " +
                        $"converted={Interlocked.Read(ref _videoFramesConverted)} enqueued={Interlocked.Read(ref _videoFramesEnqueued)} " +
                        $"nv12Direct={Interlocked.Read(ref _videoFramesDirectNv12)} nv12Converted={Interlocked.Read(ref _videoFramesConvertedNv12)} " +
                        $"ffmpegDropped={encoderDropped}");
                }
                _recordingStopwatch.Stop();
            }

            if (_postMuxAudioEnabled)
            {
                await StopAudioCaptureFileWriterAsync(finalizeHeader: true, cancelPendingWrites: false);
                var droppedAudioChunks = Interlocked.Read(ref _audioFileWriteDropped);
                if (droppedAudioChunks > 0)
                {
                    Logger.Log($"Audio writer dropped {droppedAudioChunks} queued chunk(s) under storage pressure");
                }

                if (!sinkResult.Succeeded)
                {
                    _muxAttempted = false;
                    _muxSucceeded = false;
                    muxed = false;
                    muxFailureReason = sinkResult.StatusMessage;
                }
                else if (shouldPostMux && !string.IsNullOrWhiteSpace(tempVideoPath) &&
                    !string.IsNullOrWhiteSpace(audioTempPath) && !string.IsNullOrWhiteSpace(finalOutputPath))
                {
                    _muxAttempted = true;
                    muxed = await MuxAudioIntoVideoAsync(tempVideoPath, audioTempPath, finalOutputPath);
                    _muxSucceeded = muxed;
                    muxFailureReason = muxed ? null : "ffmpeg mux execution failed";
                }
                else
                {
                    _muxAttempted = true;
                    _muxSucceeded = false;
                    muxed = false;
                    muxFailureReason = "missing mux input artifacts";
                }
            }

            var finalizeResult = sinkResult;
            if (_recordingContext != null)
            {
                if (_recordingContext.UsePostMuxAudio)
                {
                    var artifactResult = _artifactManager.FinalizeContext(_recordingContext, muxed, muxFailureReason);
                    finalizeResult = sinkResult.Succeeded
                        ? artifactResult
                        : FinalizeResult.Failure(
                            artifactResult.OutputPath,
                            sinkResult.StatusMessage,
                            artifactResult.PreservedArtifacts);
                }
                else if (sinkResult.Succeeded)
                {
                    finalizeResult = _artifactManager.FinalizeContext(_recordingContext, muxSucceeded: true);
                }
            }
            stopStatus = finalizeResult.StatusMessage;
            _lastFinalizeStatus = finalizeResult.StatusMessage;
            _lastFinalizeUtc = DateTimeOffset.UtcNow;
            _lastOutputPath = !string.IsNullOrWhiteSpace(finalizeResult.OutputPath)
                ? finalizeResult.OutputPath
                : (finalOutputPath ?? tempVideoPath ?? _recordingFile?.Path);
            _lastPreservedArtifacts = finalizeResult.PreservedArtifacts.ToArray();
            _lastRecordingSettings = _activeRecordingSettings ?? _currentSettings;
            _activeRecordingSettings = null;

            if (!finalizeResult.Succeeded)
            {
                ErrorOccurred?.Invoke(this, new Exception(finalizeResult.StatusMessage));
                Logger.Log($"Recording finalization failed: {finalizeResult.StatusMessage}");
                if (finalizeResult.PreservedArtifacts.Count > 0)
                {
                    Logger.Log($"Preserved artifacts: {string.Join(", ", finalizeResult.PreservedArtifacts)}");
                }
            }

            _lastRecordingHdrOutputActive = _hdrOutputActive;
            _lastHdrActivationReason = _hdrActivationReason;
            _isRecording = false;
            _hdrOutputActive = false;
            _hdrActivationReason = "HDR inactive";
            _frameCount = 0;
            if (_isInitialized)
            {
                _sessionState = CaptureSessionState.Ready;
            }

            Logger.Log($"Recording stopped. Audio preview status: {(_isAudioPreviewActive ? "still active" : "inactive")}");
            Logger.LogStructured("CaptureDiagnostics", GetDiagnosticsSnapshot());
            StatusChanged?.Invoke(this, stopStatus);

            _recordingBackend = RecordingBackend.None;
            _recordingContext = null;
            _recordingFile = null;
            _postMuxAudioEnabled = false;
            _audioTempPath = null;
            _finalOutputPath = null;
            _activeVideoInputPixelFormat = DefaultVideoInputPixelFormat;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
            throw;
        }
    }

    public Task StartAudioPreviewAsync()
        => RunSessionTransitionAsync(
            nameof(StartAudioPreviewAsync),
            CaptureSessionState.Previewing,
            StartAudioPreviewCoreAsync);

    private async Task StartAudioPreviewCoreAsync()
    {
        if (string.IsNullOrEmpty(_audioDeviceId))
        {
            Logger.Log("Cannot start audio preview - no audio device configured");
            return;
        }

        if (_isAudioPreviewActive)
        {
            Logger.Log("Audio preview already active");
            return;
        }

        try
        {
            Logger.Log("=== Starting Audio Preview ===");
            Logger.Log($"Audio device ID: {_audioDeviceId}");

            // Create AudioGraph settings
            // Use SystemDefault for stable audio - LowestLatency can cause skipping under CPU load
            var graphSettings = new AudioGraphSettings(AudioRenderCategory.Media);
            graphSettings.QuantumSizeSelectionMode = QuantumSizeSelectionMode.SystemDefault;

            var graphResult = await AudioGraph.CreateAsync(graphSettings);

            if (graphResult.Status != AudioGraphCreationStatus.Success)
            {
                Logger.Log($"Failed to create AudioGraph: {graphResult.Status}");
                ErrorOccurred?.Invoke(this, new Exception($"Failed to create AudioGraph: {graphResult.Status}"));
                return;
            }

            _audioGraph = graphResult.Graph;
            Logger.Log($"AudioGraph created: {_audioGraph.EncodingProperties.SampleRate}Hz, {_audioGraph.EncodingProperties.ChannelCount} channels, {_audioGraph.EncodingProperties.BitsPerSample} bits");

            // Create output node (default audio device - speakers/headphones)
            var outputResult = await _audioGraph.CreateDeviceOutputNodeAsync();
            if (outputResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                Logger.Log($"Failed to create output node: {outputResult.Status}");
                ErrorOccurred?.Invoke(this, new Exception($"Failed to create audio output node: {outputResult.Status}"));
                _audioGraph?.Dispose();
                _audioGraph = null;
                return;
            }

            _audioOutputNode = outputResult.DeviceOutputNode;
            Logger.Log("Audio output node created successfully");

            // Find the audio capture device
            var audioDevices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(
                Windows.Devices.Enumeration.DeviceClass.AudioCapture);

            var captureDevice = audioDevices.FirstOrDefault(d => d.Id == _audioDeviceId);
            if (captureDevice == null)
            {
                Logger.Log($"Audio capture device not found: {_audioDeviceId}");
                ErrorOccurred?.Invoke(this, new Exception("Audio capture device not found"));
                await StopAudioPreviewCoreAsync();
                return;
            }

            Logger.Log($"Found audio capture device: {captureDevice.Name}");

            // Create input node from the capture device
            // AudioGraph handles all format conversion internally
            var inputResult = await _audioGraph.CreateDeviceInputNodeAsync(
                Windows.Media.Capture.MediaCategory.Media,
                _audioGraph.EncodingProperties,
                captureDevice);

            if (inputResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                Logger.Log($"Failed to create input node: {inputResult.Status}, ExtendedError: {inputResult.ExtendedError?.Message}");
                ErrorOccurred?.Invoke(this, new Exception($"Failed to create audio input node: {inputResult.Status}"));
                await StopAudioPreviewCoreAsync();
                return;
            }

            _audioInputNode = inputResult.DeviceInputNode;
            Logger.Log("Audio input node created successfully");

            // Create a frame output node for meter levels
            var previewOutputFormat = AudioEncodingProperties.CreatePcm(48000, 2, 16);
            _previewAudioFrameOutputNode = _audioGraph.CreateFrameOutputNode(previewOutputFormat);
            _previewAudioFormat = _previewAudioFrameOutputNode.EncodingProperties;
            _audioGraph.QuantumStarted += PreviewAudioGraph_QuantumStarted;
            Logger.Log($"Preview audio output format: {_previewAudioFormat.Subtype}, {_previewAudioFormat.BitsPerSample} bits, {_previewAudioFormat.SampleRate} Hz, {_previewAudioFormat.ChannelCount} ch");

            // Connect input to output nodes - AudioGraph handles format conversion
            _audioInputNode.AddOutgoingConnection(_previewAudioFrameOutputNode);
            _audioInputNode.AddOutgoingConnection(_audioOutputNode);
            Logger.Log("Audio nodes connected");

            // Start the graph
            _audioGraph.Start();

            _isAudioPreviewActive = true;
            if (_isInitialized && !_isRecording)
            {
                _sessionState = CaptureSessionState.Previewing;
            }
            Logger.Log("Audio preview started successfully");
            StatusChanged?.Invoke(this, "Audio preview active");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            ErrorOccurred?.Invoke(this, ex);
            await StopAudioPreviewCoreAsync();
        }
    }

    public Task StopAudioPreviewAsync()
        => RunSessionTransitionAsync(
            nameof(StopAudioPreviewAsync),
            CaptureSessionState.Ready,
            StopAudioPreviewCoreAsync);

    private async Task StopAudioPreviewCoreAsync()
    {
        if (!_isAudioPreviewActive)
        {
            return;
        }

        try
        {
            Logger.Log("=== Stopping Audio Preview ===");

            if (_audioInputNode != null)
            {
                _audioInputNode.Dispose();
                _audioInputNode = null;
            }

            if (_previewAudioFrameOutputNode != null)
            {
                _previewAudioFrameOutputNode.Dispose();
                _previewAudioFrameOutputNode = null;
                _previewAudioFormat = null;
            }

            if (_audioOutputNode != null)
            {
                _audioOutputNode.Dispose();
                _audioOutputNode = null;
            }

            if (_audioGraph != null)
            {
                _audioGraph.QuantumStarted -= PreviewAudioGraph_QuantumStarted;
                _audioGraph.Stop();
                _audioGraph.Dispose();
                _audioGraph = null;
            }

            _isAudioPreviewActive = false;
            if (_isInitialized && !_isRecording)
            {
                _sessionState = CaptureSessionState.Ready;
            }
            Logger.Log("Audio preview stopped");
            StatusChanged?.Invoke(this, "Audio preview stopped");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            ErrorOccurred?.Invoke(this, ex);
        }
    }

    public Task CleanupAsync()
        => RunSessionTransitionAsync(
            nameof(CleanupAsync),
            CaptureSessionState.CleaningUp,
            CleanupCoreAsync,
            allowWhenDisposed: true);

    private async Task CleanupCoreAsync()
    {
        if (_isRecording)
        {
            await StopRecordingCoreAsync();
        }
        else if (_recordingSink != null)
        {
            try
            {
                await _recordingSink.StopAsync();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error stopping recording sink during cleanup: {ex.Message}");
            }

            try
            {
                await _recordingSink.DisposeAsync();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error disposing recording sink during cleanup: {ex.Message}");
            }

            _recordingSink = null;
            _ffmpegEncoder = null;
            _recordingBackend = RecordingBackend.None;
        }

        if (_postMuxAudioEnabled)
        {
            await StopAudioCaptureFileWriterAsync(finalizeHeader: false, cancelPendingWrites: true);
            await _artifactManager.RollbackAsync(_recordingContext);
            _postMuxAudioEnabled = false;
        }

        if (_isAudioPreviewActive)
        {
            await StopAudioPreviewCoreAsync();
        }

        if (_mediaCapture != null)
        {
            _mediaCapture.Failed -= MediaCapture_Failed;
            _mediaCapture.Dispose();
            _mediaCapture = null;
        }

        _isInitialized = false;
        _recordingContext = null;
        _recordingFile = null;
        _audioTempPath = null;
        _finalOutputPath = null;
        _activeRecordingSettings = null;
        _recordingReaderSourceStreamType = null;
        _recordingReaderSourceSubtype = null;
        _recordingReaderRequestedSubtype = null;
        _firstObservedFramePixelFormat = null;
        _latestObservedFramePixelFormat = null;
        if (!_isDisposed)
        {
            _sessionState = CaptureSessionState.Uninitialized;
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _sessionState = CaptureSessionState.Disposed;

        // Attempt graceful asynchronous cleanup first so active recordings finalize.
        try
        {
            var cleanupTimeoutMs = GetIntFromEnv(
                "ELGATOCAPTURE_DISPOSE_CLEANUP_TIMEOUT_MS",
                30000,
                1000,
                300000);
            var cleanupTask = Task.Run(CleanupAsync);
            var completed = Task.WhenAny(cleanupTask, Task.Delay(cleanupTimeoutMs)).GetAwaiter().GetResult();
            if (completed != cleanupTask)
            {
                Logger.Log($"Graceful cleanup during dispose timed out after {cleanupTimeoutMs} ms.");
            }
            else
            {
                cleanupTask.GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Graceful cleanup during dispose failed: {ex.Message}");
        }

        // Fallback synchronous cleanup for any remaining resources.
        try
        {
            // Stop and dispose audio graphs synchronously
            try
            {
                _recordingAudioGraph?.Stop();
                _recordingAudioInputNode?.Dispose();
                _audioFrameOutputNode?.Dispose();
                _recordingAudioGraph?.Dispose();
                _recordingAudioFormat = null;
            }
            catch (Exception ex) { Logger.Log($"Error stopping recording audio graph: {ex.Message}"); }

            try
            {
                if (_audioGraph != null)
                {
                    _audioGraph.QuantumStarted -= PreviewAudioGraph_QuantumStarted;
                }
                _audioGraph?.Stop();
                _audioInputNode?.Dispose();
                _previewAudioFrameOutputNode?.Dispose();
                _audioOutputNode?.Dispose();
                _audioGraph?.Dispose();
            }
            catch (Exception ex) { Logger.Log($"Error stopping audio preview graph: {ex.Message}"); }

            // Dispose FFmpeg encoder
            try
            {
                _recordingSink?.Dispose();
                _ffmpegEncoder?.Dispose();
            }
            catch (Exception ex) { Logger.Log($"Error disposing recording sink/encoder: {ex.Message}"); }

            // Dispose frame readers
            try
            {
                _frameReader?.Dispose();
                _recordingFrameReader?.Dispose();
            }
            catch (Exception ex) { Logger.Log($"Error disposing frame readers: {ex.Message}"); }

            // Dispose MediaCapture last
            try
            {
                _mediaCapture?.Dispose();
            }
            catch (Exception ex) { Logger.Log($"Error disposing MediaCapture: {ex.Message}"); }

            _recordingSink = null;
            _ffmpegEncoder = null;
            _recordingContext = null;
            _recordingBackend = RecordingBackend.None;
            _isRecording = false;
            _hdrOutputActive = false;
            _lastRecordingHdrOutputActive = false;
            _hdrActivationReason = "HDR inactive";
            _lastHdrActivationReason = "HDR inactive";
            _recordingReaderSourceStreamType = null;
            _recordingReaderSourceSubtype = null;
            _recordingReaderRequestedSubtype = null;
            _firstObservedFramePixelFormat = null;
            _latestObservedFramePixelFormat = null;
            _recordingFrameHandlerAttached = false;
            _isInitialized = false;
            _activeRecordingSettings = null;

            Logger.Log("CaptureService disposed");
        }
        catch (Exception ex)
        {
            Logger.Log($"Error during CaptureService disposal: {ex.Message}");
        }
        finally
        {
            _sessionTransitionLock.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        var cleanupTimeoutMs = GetIntFromEnv(
            "ELGATOCAPTURE_DISPOSE_CLEANUP_TIMEOUT_MS",
            30000,
            1000,
            300000);
        var cleanupTask = CleanupAsync();
        var completed = await Task.WhenAny(cleanupTask, Task.Delay(cleanupTimeoutMs));
        if (completed != cleanupTask)
        {
            Logger.Log($"CaptureService async dispose cleanup timed out after {cleanupTimeoutMs} ms.");
        }
        else
        {
            await cleanupTask;
        }

        _sessionState = CaptureSessionState.Disposed;
        _sessionTransitionLock.Dispose();
        Logger.Log("CaptureService disposed (async)");
    }
}

// Simple AVI writer for uncompressed frames
internal class AviWriter : IDisposable
{
    private readonly IRandomAccessStream _stream;
    private readonly DataWriter _writer;
    private readonly uint _width;
    private readonly uint _height;
    private readonly uint _frameRate;
    private readonly List<uint> _frameOffsets = new();
    private uint _frameCount;
    private ulong _moviStartPosition;
    private readonly byte[] _frameBuffer;

    public AviWriter(IRandomAccessStream stream, uint width, uint height, uint frameRate)
    {
        _stream = stream;
        _writer = new DataWriter(stream);
        _width = width;
        _height = height;
        _frameRate = frameRate;
        _frameBuffer = new byte[width * height * 4]; // BGRA
    }

    public async Task WriteHeaderAsync()
    {
        // Simplified AVI header - will be updated on finalize
        // RIFF header
        WriteString("RIFF");
        _writer.WriteUInt32(0); // File size placeholder
        WriteString("AVI ");

        // hdrl LIST
        WriteString("LIST");
        _writer.WriteUInt32(0); // hdrl size placeholder
        WriteString("hdrl");

        // avih (main AVI header)
        WriteString("avih");
        _writer.WriteUInt32(56); // avih size
        _writer.WriteUInt32(1000000 / _frameRate); // microseconds per frame
        _writer.WriteUInt32(0); // max bytes per sec
        _writer.WriteUInt32(0); // padding granularity
        _writer.WriteUInt32(0x10); // flags (AVIF_HASINDEX)
        _writer.WriteUInt32(0); // total frames placeholder
        _writer.WriteUInt32(0); // initial frames
        _writer.WriteUInt32(1); // streams
        _writer.WriteUInt32(_width * _height * 4); // suggested buffer size
        _writer.WriteUInt32(_width);
        _writer.WriteUInt32(_height);
        _writer.WriteUInt32(0); // reserved
        _writer.WriteUInt32(0);
        _writer.WriteUInt32(0);
        _writer.WriteUInt32(0);

        // strl LIST (stream list)
        WriteString("LIST");
        _writer.WriteUInt32(116); // strl size
        WriteString("strl");

        // strh (stream header)
        WriteString("strh");
        _writer.WriteUInt32(56); // strh size
        WriteString("vids"); // fccType
        WriteString("DIB "); // fccHandler (uncompressed)
        _writer.WriteUInt32(0); // flags
        _writer.WriteUInt16(0); // priority
        _writer.WriteUInt16(0); // language
        _writer.WriteUInt32(0); // initial frames
        _writer.WriteUInt32(1); // scale
        _writer.WriteUInt32(_frameRate); // rate
        _writer.WriteUInt32(0); // start
        _writer.WriteUInt32(0); // length placeholder
        _writer.WriteUInt32(_width * _height * 4); // suggested buffer
        _writer.WriteUInt32(0); // quality
        _writer.WriteUInt32(0); // sample size
        _writer.WriteInt16(0); // left
        _writer.WriteInt16(0); // top
        _writer.WriteInt16((short)_width); // right
        _writer.WriteInt16((short)_height); // bottom

        // strf (stream format - BITMAPINFOHEADER)
        WriteString("strf");
        _writer.WriteUInt32(40); // strf size
        _writer.WriteUInt32(40); // biSize
        _writer.WriteInt32((int)_width);
        _writer.WriteInt32(-(int)_height); // negative for top-down
        _writer.WriteUInt16(1); // planes
        _writer.WriteUInt16(32); // bits per pixel
        _writer.WriteUInt32(0); // compression (BI_RGB)
        _writer.WriteUInt32(_width * _height * 4); // image size
        _writer.WriteInt32(0);
        _writer.WriteInt32(0);
        _writer.WriteUInt32(0);
        _writer.WriteUInt32(0);

        // movi LIST
        WriteString("LIST");
        _writer.WriteUInt32(0); // movi size placeholder
        WriteString("movi");

        await _writer.StoreAsync();
        _moviStartPosition = _stream.Position;
    }

    private void WriteString(string s)
    {
        foreach (char c in s)
        {
            _writer.WriteByte((byte)c);
        }
    }

    public async Task WriteFrameAsync(SoftwareBitmap bitmap)
    {
        var buffer = new Windows.Storage.Streams.Buffer((uint)_frameBuffer.Length);
        bitmap.CopyToBuffer(buffer);

        // Get the raw pixel data
        var reader = DataReader.FromBuffer(buffer);
        reader.ReadBytes(_frameBuffer);

        // Write frame chunk
        _frameOffsets.Add((uint)(_stream.Position - 4));
        WriteString("00dc"); // video chunk
        _writer.WriteUInt32((uint)_frameBuffer.Length);
        _writer.WriteBytes(_frameBuffer);
        _frameCount++;

        await _writer.StoreAsync();
    }

    public async Task FinalizeAsync()
    {
        // Write index
        var indexStart = _stream.Position;
        WriteString("idx1");
        _writer.WriteUInt32((uint)(_frameCount * 16));

        for (int i = 0; i < _frameOffsets.Count; i++)
        {
            WriteString("00dc");
            _writer.WriteUInt32(0x10); // AVIIF_KEYFRAME
            _writer.WriteUInt32(_frameOffsets[i] - (uint)_moviStartPosition);
            _writer.WriteUInt32((uint)_frameBuffer.Length);
        }

        await _writer.StoreAsync();

        // Update file size in RIFF header
        var fileSize = _stream.Position;
        _stream.Seek(4);
        var sizeWriter = new DataWriter(_stream);
        sizeWriter.WriteUInt32((uint)(fileSize - 8));
        await sizeWriter.StoreAsync();

        await _stream.FlushAsync();
    }

    public void Dispose()
    {
        _writer.Dispose();
        _stream.Dispose();
    }
}

