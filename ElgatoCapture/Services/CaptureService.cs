using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using Windows.Storage;

namespace ElgatoCapture.Services;

public class CaptureService : IDisposable, IAsyncDisposable
{
    private readonly SemaphoreSlim _sessionTransitionLock = new(1, 1);
    private readonly ISourceSignalTelemetryProvider _sourceTelemetryProvider;
    private readonly IProcessSupervisor _processSupervisor;
    private readonly RecordingArtifactManager _artifactManager = new();

    private bool _isDisposed;
    private bool _isInitialized;
    private bool _isRecording;
    private bool _isVideoPreviewActive;
    private bool _isAudioPreviewActive;
    private CaptureSessionState _sessionState = CaptureSessionState.Uninitialized;
    private CaptureDevice? _currentDevice;
    private CaptureSettings? _currentSettings;
    private CaptureSettings? _activeRecordingSettings;
    private SourceSignalTelemetrySnapshot _latestSourceTelemetry = SourceSignalTelemetrySnapshot.CreateUnavailable("telemetry-not-started");
    private FFmpegEncoderService? _ffmpegEncoder;
    private IRecordingSink? _recordingSink;
    private WasapiAudioCapture? _wasapiAudioCapture;
    private WasapiAudioPlayback? _wasapiAudioPlayback;
    private bool _wasapiAudioCaptureFaulted;
    private string? _wasapiAudioCaptureFaultMessage;
    private UnifiedVideoCapture? _unifiedVideoCapture;
    private IPreviewFrameSink? _previewFrameSink;
    private RecordingContext? _recordingContext;
    private readonly Stopwatch _recordingStopwatch = new();
    private string? _lastOutputPath;
    private string _lastFinalizeStatus = "None";
    private DateTimeOffset? _lastFinalizeUtc;
    private IReadOnlyList<string> _lastPreservedArtifacts = Array.Empty<string>();
    private bool _lastUsePostMuxAudio;
    private string? _audioDeviceId;
    private string? _audioDeviceName;
    private uint? _actualWidth;
    private uint? _actualHeight;
    private double? _actualFrameRate;
    private string? _actualFrameRateArg;
    private uint? _actualFrameRateNumerator;
    private uint? _actualFrameRateDenominator;
    private string? _actualPixelFormat;
    private string _activeVideoInputPixelFormat = "nv12";
#pragma warning disable CS0649 // Field is never assigned â€” kept for telemetry reflection compatibility
    private long _videoFramesArrived;
#pragma warning restore CS0649
    private long _videoFramesDropped;
    private string? _firstObservedFramePixelFormat;
    private string? _latestObservedFramePixelFormat;
    private string? _latestObservedSurfaceFormat;
    private long _observedP010FrameCount;
    private long _observedNv12FrameCount;
    private long _observedOtherFrameCount;
    private long _observedP010BitDepthSampleCount;
    private double _observedP010Low2BitNonZeroPercent;
    private bool? _observedP010Likely8BitUpscaled;
    private long _lastMfSourceReaderFramesDelivered;
    private long _lastMfSourceReaderFramesDropped;
    private string? _lastMfSourceReaderNegotiatedFormat;
    private CancellationTokenSource? _telemetryPollCts;
    private Task? _telemetryPollTask;
    private const int TelemetryPollIntervalMs = 2000;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<Exception>? ErrorOccurred;
    public event EventHandler<ulong>? FrameCaptured;
    public event EventHandler<AudioLevelEventArgs>? AudioLevelUpdated;
    public event EventHandler<SourceSignalTelemetrySnapshot>? SourceTelemetryUpdated;

    public bool IsRecording => _isRecording;
    public bool IsInitialized => _isInitialized;
    public bool IsVideoPreviewActive => _isVideoPreviewActive;
    public bool IsAudioPreviewActive => _isAudioPreviewActive;
    public CaptureSessionState SessionState => _sessionState;

    public int GetNegotiatedVideoWidth() => _unifiedVideoCapture?.Width ?? 0;
    public int GetNegotiatedVideoHeight() => _unifiedVideoCapture?.Height ?? 0;
    public double GetNegotiatedVideoFps() => _unifiedVideoCapture?.Fps ?? 0;

    public CaptureService() : this(new ProcessSupervisor(), null)
    {
    }

    internal CaptureService(IProcessSupervisor processSupervisor, ISourceSignalTelemetryProvider? sourceSignalTelemetryProvider = null)
    {
        _processSupervisor = processSupervisor;
        _sourceTelemetryProvider = sourceSignalTelemetryProvider ?? CreateDefaultTelemetryProvider();
    }

    private static ISourceSignalTelemetryProvider CreateDefaultTelemetryProvider()
    {
        return new CompositeSourceSignalTelemetryProvider(
            new RticeSourceSignalTelemetryProvider(),
            new KsXuSourceSignalTelemetryProvider(),
            new EgavSourceSignalTelemetryProvider());
    }

    internal void SetPreviewFrameSink(IPreviewFrameSink? sink)
    {
        _previewFrameSink = sink;
        _unifiedVideoCapture?.SetPreviewSink(sink);
        TryApplySharedPreviewDevice(_unifiedVideoCapture, sink);
    }

    private void ConfigureObservedPixelTelemetry(UnifiedVideoCapture unifiedVideoCapture)
    {
        unifiedVideoCapture.SetObservedPixelFormatObserver(OnUnifiedVideoFrameObserved);
    }

    private void OnUnifiedVideoFrameObserved(bool isP010)
    {
        RecordObservedPixelFormat(isP010 ? "P010" : "NV12");
    }

    public RecordingStats GetRecordingStats()
    {
        try
        {
            if (_isRecording && _ffmpegEncoder != null)
            {
                var reported = _ffmpegEncoder.LastReportedOutputBytes;
                if (reported > 0)
                {
                    return new RecordingStats(reported, 0);
                }
            }

            var path = _recordingContext?.VideoOutputPath ?? _lastOutputPath;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return new RecordingStats(0, 0);
            }

            return new RecordingStats(new FileInfo(path).Length, 0);
        }
        catch
        {
            return new RecordingStats(0, 0);
        }
    }

    public SourceSignalTelemetrySnapshot GetLatestSourceTelemetrySnapshot() => _latestSourceTelemetry;

    public CaptureDiagnosticsSnapshot GetDiagnosticsSnapshot()
        => new()
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            SessionState = _sessionState,
            IsRecording = _isRecording,
            RecordingBackend = _isRecording ? "FfmpegPipe" : "None"
        };

    private void ResetObservedPixelTelemetry()
    {
        _firstObservedFramePixelFormat = null;
        _latestObservedFramePixelFormat = null;
        _latestObservedSurfaceFormat = null;
        Interlocked.Exchange(ref _observedP010FrameCount, 0);
        Interlocked.Exchange(ref _observedNv12FrameCount, 0);
        Interlocked.Exchange(ref _observedOtherFrameCount, 0);
        Interlocked.Exchange(ref _observedP010BitDepthSampleCount, 0);
        _observedP010Low2BitNonZeroPercent = 0;
        _observedP010Likely8BitUpscaled = null;
    }

    private static string? NormalizeObservedPixelFormat(string? pixelFormat)
    {
        if (string.IsNullOrWhiteSpace(pixelFormat))
        {
            return null;
        }

        if (pixelFormat.Contains("P010", StringComparison.OrdinalIgnoreCase))
        {
            return "P010";
        }

        if (pixelFormat.Contains("NV12", StringComparison.OrdinalIgnoreCase))
        {
            return "NV12";
        }

        return pixelFormat.Trim().ToUpperInvariant();
    }

    private void OnWasapiAudioLevelUpdated(object? sender, AudioLevelEventArgs e)
    {
        AudioLevelUpdated?.Invoke(this, e);
    }

    private void OnWasapiCaptureFailed(object? sender, Exception ex)
    {
        if (_isRecording)
        {
            Volatile.Write(ref _wasapiAudioCaptureFaulted, true);
            Volatile.Write(ref _wasapiAudioCaptureFaultMessage, ex.Message);
        }

        Logger.Log($"WASAPI_CAPTURE_FAILED type={ex.GetType().Name} hr=0x{ex.HResult:X8} message={ex.Message} recording={_isRecording}");
        StatusChanged?.Invoke(this, $"Audio capture error: {ex.Message}");
        ErrorOccurred?.Invoke(this, ex);
    }

    private async Task StartWasapiPlaybackAsync(CancellationToken cancellationToken)
    {
        var capture = _wasapiAudioCapture;
        if (capture == null)
        {
            return;
        }

        var playback = _wasapiAudioPlayback;
        if (playback == null)
        {
            playback = new WasapiAudioPlayback();
            await playback.InitializeAsync(cancellationToken).ConfigureAwait(false);
            playback.Start();
            _wasapiAudioPlayback = playback;
            Logger.Log("WASAPI audio playback started.");
        }

        capture.SetPlayback(playback);
    }

    private void StopWasapiPlayback()
    {
        var playback = _wasapiAudioPlayback;
        _wasapiAudioPlayback = null;
        _wasapiAudioCapture?.SetPlayback(null);
        if (playback != null)
        {
            try
            {
                playback.Stop();
            }
            catch (Exception ex)
            {
                Logger.Log($"WASAPI audio playback stop warning: {ex.Message}");
            }

            playback.Dispose();
            Logger.Log("WASAPI audio playback disposed.");
        }
    }

    private void DetachWasapiAudioCapture(WasapiAudioCapture? capture)
    {
        if (capture == null)
        {
            StopWasapiPlayback();
            return;
        }

        capture.AudioLevelUpdated -= OnWasapiAudioLevelUpdated;
        capture.CaptureFailed -= OnWasapiCaptureFailed;
        capture.SetPlayback(null);
        StopWasapiPlayback();
    }

    private void RecordObservedPixelFormat(string? pixelFormat, bool incrementAsFrame = true)
    {
        var normalizedFormat = NormalizeObservedPixelFormat(pixelFormat);
        if (string.IsNullOrWhiteSpace(normalizedFormat))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_firstObservedFramePixelFormat))
        {
            _firstObservedFramePixelFormat = normalizedFormat;
        }

        _latestObservedFramePixelFormat = normalizedFormat;
        _latestObservedSurfaceFormat = normalizedFormat;

        if (!incrementAsFrame)
        {
            return;
        }

        if (string.Equals(normalizedFormat, "P010", StringComparison.OrdinalIgnoreCase))
        {
            Interlocked.Increment(ref _observedP010FrameCount);
        }
        else if (string.Equals(normalizedFormat, "NV12", StringComparison.OrdinalIgnoreCase))
        {
            Interlocked.Increment(ref _observedNv12FrameCount);
        }
        else
        {
            Interlocked.Increment(ref _observedOtherFrameCount);
        }
    }

    private void CaptureEncoderRuntimeTelemetry(FFmpegEncoderService encoder)
    {
        Interlocked.Exchange(ref _videoFramesDropped, encoder.DroppedVideoFrames);
        _firstObservedFramePixelFormat = encoder.FirstObservedFramePixelFormat ?? _firstObservedFramePixelFormat;
        _latestObservedFramePixelFormat = encoder.LatestObservedFramePixelFormat ?? _latestObservedFramePixelFormat;
        _latestObservedSurfaceFormat = encoder.LatestObservedSurfaceFormat ?? _latestObservedSurfaceFormat;
        Interlocked.Exchange(ref _observedP010FrameCount, encoder.ObservedP010FrameCount);
        Interlocked.Exchange(ref _observedNv12FrameCount, encoder.ObservedNv12FrameCount);
        Interlocked.Exchange(ref _observedOtherFrameCount, encoder.ObservedOtherFrameCount);
        Interlocked.Exchange(ref _observedP010BitDepthSampleCount, encoder.ObservedP010BitDepthSampleCount);
        _observedP010Low2BitNonZeroPercent = encoder.ObservedP010Low2BitNonZeroPercent;
        _observedP010Likely8BitUpscaled = encoder.ObservedP010Likely8BitUpscaled;
    }

    private (
        string? FirstObservedFramePixelFormat,
        string? LatestObservedFramePixelFormat,
        string? LatestObservedSurfaceFormat,
        long ObservedP010FrameCount,
        long ObservedNv12FrameCount,
        long ObservedOtherFrameCount,
        long ObservedP010BitDepthSampleCount,
        double ObservedP010Low2BitNonZeroPercent,
        bool? ObservedP010Likely8BitUpscaled)
        ResolveObservedFrameTelemetry(FFmpegEncoderService? encoder)
    {
        if (encoder != null)
        {
            CaptureEncoderRuntimeTelemetry(encoder);
        }

        return (
            _firstObservedFramePixelFormat,
            _latestObservedFramePixelFormat,
            _latestObservedSurfaceFormat,
            Math.Max(0, Interlocked.Read(ref _observedP010FrameCount)),
            Math.Max(0, Interlocked.Read(ref _observedNv12FrameCount)),
            Math.Max(0, Interlocked.Read(ref _observedOtherFrameCount)),
            Math.Max(0, Interlocked.Read(ref _observedP010BitDepthSampleCount)),
            _observedP010Low2BitNonZeroPercent,
            _observedP010Likely8BitUpscaled);
    }

    public CaptureRuntimeSnapshot GetRuntimeSnapshot()
    {
        var encoder = _ffmpegEncoder;
        var unifiedVideoCapture = _unifiedVideoCapture;
        var requestedSettings = _activeRecordingSettings ?? _currentSettings;
        var hdrRequested = requestedSettings?.HdrEnabled == true &&
                           requestedSettings.HdrOutputMode == HdrOutputMode.Hdr10Pq;
        var requestedPipelineMode = hdrRequested ? "HDR10-PQ" : "SDR";
        var encoderInputPixelFormat = encoder?.ActiveInputPixelFormat ?? _activeVideoInputPixelFormat;
        var encoderOutputPixelFormat = encoder?.ActiveOutputPixelFormat;
        var encoderVideoCodec = encoder?.ActiveVideoCodec;
        var encoderVideoProfile = encoder?.ActiveVideoProfile;
        var encoderTenBitPipelineConfirmed = encoder?.ActiveTenBitPipelineConfirmed;
        var mfReadwriteDisableConverters = encoder?.MfReadwriteDisableConverters ?? false;
        var negotiatedMediaSubtypeToken = string.Equals(encoderInputPixelFormat, "p010le", StringComparison.OrdinalIgnoreCase)
            ? "P010|MFVideoFormat_P010"
            : "NV12";
        var activePipelineMode = _isRecording
            ? (string.Equals(
                encoderInputPixelFormat,
                "p010le",
                StringComparison.OrdinalIgnoreCase)
                ? "HDR10-PQ"
                : "SDR")
            : requestedPipelineMode;
        var pipelineModeMatched = string.Equals(
            requestedPipelineMode,
            activePipelineMode,
            StringComparison.OrdinalIgnoreCase);
        var pipelineModeStatus = _isRecording
            ? (pipelineModeMatched ? "Active" : "Violation")
            : "Ready";
        var pipelineModeReason = pipelineModeMatched
            ? string.Empty
            : $"Requested pipeline '{requestedPipelineMode}', but active encoder ingress is '{activePipelineMode}' " +
              $"(pixel-format={encoderInputPixelFormat ?? "unknown"}).";
        var hdrOutputActive = _isRecording &&
                              string.Equals(
                                  activePipelineMode,
                                  "HDR10-PQ",
                                  StringComparison.OrdinalIgnoreCase);
        var hdrRequestedButSourceNot10Bit = hdrRequested && _latestSourceTelemetry.IsHdr == false;
        var hdrAutoDowngraded = hdrRequested && _isRecording && !pipelineModeMatched;
        var hdrAutoDowngradeReason = hdrAutoDowngraded
            ? pipelineModeReason
            : string.Empty;
        var hdrDowngradeCode = hdrAutoDowngraded ? "encoder-input-not-p010" : string.Empty;
        var hdrRuntimeState = hdrOutputActive
            ? "Active"
            : hdrRequested
                ? (_isRecording ? "Violation" : "Ready")
                : "Inactive";
        var hdrReadinessReason = hdrOutputActive
            ? string.Empty
            : hdrRequested
                ? (_isRecording
                    ? pipelineModeReason
                    : "HDR requested and will activate when recording starts.")
                : string.Empty;
        var hdrActivationReason = hdrOutputActive
            ? "P010 pipeline is active."
            : hdrRequested
                ? (_isRecording
                    ? "HDR requested but the active recording pipeline is not in HDR mode."
                    : "HDR requested and waiting for recording start.")
                : "HDR not requested.";
        var sourceTelemetryTimestampUtc = _latestSourceTelemetry.TimestampUtc;
        var sourceTelemetryAgeSeconds = ResolveTelemetryAgeSeconds(sourceTelemetryTimestampUtc, DateTimeOffset.UtcNow);
        var sourceTelemetryBackend = ResolveSourceTelemetryBackend(_latestSourceTelemetry);
        var sourceTelemetrySuppressedReason = ResolveSourceTelemetrySuppressedReason(_latestSourceTelemetry);
        var sourceTelemetrySuppressed = !string.IsNullOrWhiteSpace(sourceTelemetrySuppressedReason);
        var sourceTelemetryCircuitState = ResolveSourceTelemetryCircuitState(_latestSourceTelemetry.Availability, sourceTelemetrySuppressed);
        var sourceFrameRateOrigin = ResolveSourceFrameRateOrigin(_latestSourceTelemetry);
        var (telemetryAlignmentStatus, telemetryAlignmentReason) = ResolveTelemetryAlignment(
            requestedSettings,
            _latestSourceTelemetry,
            _actualWidth,
            _actualHeight,
            _actualFrameRate,
            hdrRequested);
        var observedTelemetry = ResolveObservedFrameTelemetry(encoder);
        var observedP010FrameCount = observedTelemetry.ObservedP010FrameCount;
        var observedNv12FrameCount = observedTelemetry.ObservedNv12FrameCount;
        var observedOtherFrameCount = observedTelemetry.ObservedOtherFrameCount;
        var observedP010BitDepthSampleCount = observedTelemetry.ObservedP010BitDepthSampleCount;
        var observedP010Low2BitNonZeroPercent = observedTelemetry.ObservedP010Low2BitNonZeroPercent;
        var observedP010Likely8BitUpscaled = observedTelemetry.ObservedP010Likely8BitUpscaled;
        var observedNonP010FrameCount = observedNv12FrameCount + observedOtherFrameCount;
        var observedFrameCount = observedP010FrameCount + observedNonP010FrameCount;
        var hdrWarmupState = ResolveHdrWarmupState(
            hdrRequested,
            hdrOutputActive,
            _isRecording,
            observedP010FrameCount);
        var requestedReaderSubtype = !string.IsNullOrWhiteSpace(requestedSettings?.RequestedPixelFormat)
            ? requestedSettings!.RequestedPixelFormat
            : hdrRequested
                ? "P010"
                : "NV12";
        var mfSourceReaderFramesDelivered = unifiedVideoCapture?.VideoFramesArrived ?? _lastMfSourceReaderFramesDelivered;
        var mfSourceReaderFramesDropped = unifiedVideoCapture?.VideoFramesDropped ?? _lastMfSourceReaderFramesDropped;
        var mfSourceReaderNegotiatedFormat = unifiedVideoCapture?.NegotiatedFormat ?? _lastMfSourceReaderNegotiatedFormat;
        var negotiatedSubtypeFromSourceReader =
            !string.IsNullOrWhiteSpace(mfSourceReaderNegotiatedFormat) &&
            mfSourceReaderNegotiatedFormat.Contains("P010", StringComparison.OrdinalIgnoreCase)
                ? "P010"
                : !string.IsNullOrWhiteSpace(mfSourceReaderNegotiatedFormat) &&
                  mfSourceReaderNegotiatedFormat.Contains("NV12", StringComparison.OrdinalIgnoreCase)
                    ? "NV12"
                    : "unknown";
        var videoNegotiatedSubtype = unifiedVideoCapture != null
            ? (unifiedVideoCapture.IsP010 ? "P010" : "NV12")
            : negotiatedSubtypeFromSourceReader;
        var hasD3DManager = unifiedVideoCapture?.D3DManager != null;
        var memoryPreference = hasD3DManager ? "Gpu" : "Cpu";
        var readerSourceStreamType = (_isRecording || _isVideoPreviewActive) && unifiedVideoCapture != null
            ? "MfSourceReader"
            : null;
        var previewColorMetadata = (_previewFrameSink as D3D11PreviewRenderer)?.RendererMode ?? "None";
        var muxAttempted = !_isRecording && _lastFinalizeUtc.HasValue && _lastUsePostMuxAudio;
        bool? muxSucceeded = null;
        if (muxAttempted)
        {
            if (_lastFinalizeStatus.Contains("mux failed", StringComparison.OrdinalIgnoreCase))
            {
                muxSucceeded = false;
            }
            else if (_lastFinalizeStatus.Contains("stopped", StringComparison.OrdinalIgnoreCase))
            {
                muxSucceeded = true;
            }
        }

        return new CaptureRuntimeSnapshot
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            IsInitialized = _isInitialized,
            IsRecording = _isRecording,
            IsAudioPreviewActive = _isAudioPreviewActive,
            AudioReaderActive = _wasapiAudioCapture?.IsCapturing ?? false,
            AudioFramesArrived = _wasapiAudioCapture?.AudioFramesArrived ?? 0,
            AudioFramesWrittenToSink = _wasapiAudioCapture?.AudioFramesWrittenToSink ?? 0,
            VideoReaderActive = unifiedVideoCapture != null && (_isVideoPreviewActive || _isRecording),
            IngestVideoFramesArrived = unifiedVideoCapture?.VideoFramesArrived ?? 0,
            IngestVideoFramesWrittenToSink = unifiedVideoCapture?.VideoFramesWrittenToSink ?? 0,
            IngestLastVideoFrameAgeMs = ComputeTickAge(unifiedVideoCapture?.LastVideoFrameArrivedTick ?? 0),
            VideoIngestErrorCount = unifiedVideoCapture?.VideoFramesDropped ?? 0,
            MemoryPreference = memoryPreference,
            VideoRequestedSubtype = requestedReaderSubtype ?? "unknown",
            VideoNegotiatedSubtype = videoNegotiatedSubtype,
            PreviewColorMetadata = previewColorMetadata,
            MfSourceReaderFramesDelivered = mfSourceReaderFramesDelivered,
            MfSourceReaderFramesDropped = mfSourceReaderFramesDropped,
            MfSourceReaderNegotiatedFormat = mfSourceReaderNegotiatedFormat,
            SessionState = _sessionState.ToString(),
            CurrentDeviceId = _currentDevice?.Id,
            CurrentDeviceName = _currentDevice?.Name,
            ActiveAudioDeviceId = _audioDeviceId,
            ActiveAudioDeviceName = _audioDeviceName,
            RequestedWidth = requestedSettings?.Width,
            RequestedHeight = requestedSettings?.Height,
            RequestedFrameRate = requestedSettings?.FrameRate,
            RequestedFrameRateArg = ResolveRequestedFrameRateArg(requestedSettings, _actualFrameRateArg),
            RequestedFrameRateNumerator = requestedSettings?.RequestedFrameRateNumerator,
            RequestedFrameRateDenominator = requestedSettings?.RequestedFrameRateDenominator,
            RequestedPixelFormat = requestedSettings?.RequestedPixelFormat,
            RequestedFormat = requestedSettings?.Format.ToString(),
            RequestedQuality = requestedSettings?.Quality.ToString(),
            RequestedAudioEnabled = requestedSettings?.AudioEnabled,
            RequestedHdrEnabled = requestedSettings?.HdrEnabled,
            RequestedHdrMasteringMetadata =
                !string.IsNullOrWhiteSpace(requestedSettings?.HdrMasterDisplayMetadata) ||
                ((requestedSettings?.HdrMaxCll ?? 0) > 0 && (requestedSettings?.HdrMaxFall ?? 0) > 0),
            HdrOutputActive = hdrOutputActive,
            HdrActivationReason = hdrActivationReason,
            HdrRuntimeState = hdrRuntimeState,
            HdrReadinessReason = hdrReadinessReason,
            HdrWarmupState = hdrWarmupState,
            HdrWarmupRequiredP010Frames = hdrRequested ? 1 : 0,
            HdrWarmupAllowedNonP010Frames = hdrRequested ? 2 : 0,
            HdrWarmupObservedP010Frames = (int)Math.Min(int.MaxValue, observedP010FrameCount),
            HdrWarmupObservedNonP010Frames = (int)Math.Min(int.MaxValue, Math.Max(0L, observedNonP010FrameCount)),
            HdrAutoDowngraded = hdrAutoDowngraded,
            HdrAutoDowngradeReason = hdrAutoDowngradeReason,
            HdrDowngradeCode = hdrDowngradeCode,
            HdrRequestedButSourceNot10Bit = hdrRequestedButSourceNot10Bit,
            RequestedPipelineMode = requestedPipelineMode,
            ActivePipelineMode = activePipelineMode,
            PipelineModeMatched = pipelineModeMatched,
            PipelineModeStatus = pipelineModeStatus,
            PipelineModeReason = pipelineModeReason,
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
            RequestedReaderSubtype = requestedReaderSubtype,
            ReaderSourceStreamType = readerSourceStreamType,
            ReaderSourceSubtype = observedTelemetry.LatestObservedFramePixelFormat ?? _actualPixelFormat,
            FirstObservedFramePixelFormat = observedFrameCount > 0
                ? observedTelemetry.FirstObservedFramePixelFormat
                : null,
            LatestObservedFramePixelFormat = observedFrameCount > 0
                ? observedTelemetry.LatestObservedFramePixelFormat
                : null,
            LatestObservedSurfaceFormat = observedFrameCount > 0
                ? observedTelemetry.LatestObservedSurfaceFormat
                : null,
            ObservedP010FrameCount = observedP010FrameCount,
            ObservedNv12FrameCount = observedNv12FrameCount,
            ObservedOtherFrameCount = observedOtherFrameCount,
            ObservedP010BitDepthSampleCount = observedP010BitDepthSampleCount,
            ObservedP010Low2BitNonZeroPercent = observedP010Low2BitNonZeroPercent,
            ObservedP010Likely8BitUpscaled = observedP010Likely8BitUpscaled,
            EncoderInputPixelFormat = encoderInputPixelFormat,
            EncoderOutputPixelFormat = encoderOutputPixelFormat,
            EncoderVideoCodec = encoderVideoCodec,
            EncoderVideoProfile = encoderVideoProfile,
            EncoderTenBitPipelineConfirmed = encoderTenBitPipelineConfirmed,
            MfReadwriteDisableConverters = mfReadwriteDisableConverters,
            NegotiatedMediaSubtypeToken = negotiatedMediaSubtypeToken,
            DetectedSourceFrameRate = _latestSourceTelemetry.FrameRateExact,
            DetectedSourceFrameRateArg = _latestSourceTelemetry.FrameRateArg,
            SourceFrameRateOrigin = sourceFrameRateOrigin,
            SourceWidth = _latestSourceTelemetry.Width,
            SourceHeight = _latestSourceTelemetry.Height,
            SourceIsHdr = _latestSourceTelemetry.IsHdr,
            RecordingBackend = _isRecording ? "FfmpegPipe" : "None",
            AudioPathMode = requestedSettings?.AudioPathMode.ToString() ?? "None",
            MuxAttempted = muxAttempted,
            MuxSucceeded = muxSucceeded,
            LastOutputPath = _lastOutputPath,
            LastFinalizeStatus = _lastFinalizeStatus,
            LastFinalizeUtc = _lastFinalizeUtc,
            LastPreservedArtifacts = _lastPreservedArtifacts,
            SourceTelemetryAvailability = _latestSourceTelemetry.Availability.ToString(),
            SourceTelemetryOriginDetail = _latestSourceTelemetry.OriginDetail,
            SourceTelemetryConfidence = _latestSourceTelemetry.Confidence.ToString(),
            SourceTelemetryDiagnosticSummary = _latestSourceTelemetry.DiagnosticSummary,
            SourceTelemetryTimestampUtc = sourceTelemetryTimestampUtc,
            SourceTelemetryAgeSeconds = sourceTelemetryAgeSeconds,
            SourceTelemetryBackend = sourceTelemetryBackend,
            SourceTelemetrySuppressed = sourceTelemetrySuppressed,
            SourceTelemetrySuppressedReason = sourceTelemetrySuppressedReason,
            SourceTelemetryCircuitState = sourceTelemetryCircuitState,
            TelemetryAlignmentStatus = telemetryAlignmentStatus,
            TelemetryAlignmentReason = telemetryAlignmentReason
        };
    }

    public VideoSourceProbeResult ProbeVideoSource()
    {
        var unifiedVideoCapture = _unifiedVideoCapture;
        if (unifiedVideoCapture == null)
        {
            return new VideoSourceProbeResult
            {
                SessionActive = false,
                MemoryPreference = "Unknown"
            };
        }

        var subtype = unifiedVideoCapture.IsP010 ? "P010" : "NV12";
        var fps = Math.Round(unifiedVideoCapture.Fps, 3);
        return new VideoSourceProbeResult
        {
            SessionActive = true,
            MemoryPreference = unifiedVideoCapture.IsP010 ? "Auto" : "Cpu",
            CurrentSubtype = subtype,
            CurrentWidth = unifiedVideoCapture.Width,
            CurrentHeight = unifiedVideoCapture.Height,
            CurrentFrameRate = fps,
            P010Available = unifiedVideoCapture.IsP010,
            Nv12Available = !unifiedVideoCapture.IsP010,
            SupportedSubtypes = new[] { subtype },
            TotalFormatCount = 1,
            Formats = new[]
            {
                new VideoSourceFormatEntry
                {
                    Subtype = subtype,
                    Width = unifiedVideoCapture.Width,
                    Height = unifiedVideoCapture.Height,
                    FrameRate = fps,
                    Summary = $"{subtype} {unifiedVideoCapture.Width}x{unifiedVideoCapture.Height}@{fps:0.###}"
                }
            }
        };
    }

    public PreviewColorProbeResult ProbePreviewColor()
    {
        var unifiedVideoCapture = _unifiedVideoCapture;
        var d3dSink = _previewFrameSink as D3D11PreviewRenderer;
        var d3dInputColor = d3dSink?.InputColorSpaceLabel ?? "None";
        var d3dOutputColor = d3dSink?.OutputColorSpaceLabel ?? "None";
        if (unifiedVideoCapture == null)
        {
            return new PreviewColorProbeResult
            {
                SessionActive = false,
                D3DInputColorSpace = d3dInputColor,
                D3DOutputColorSpace = d3dOutputColor
            };
        }

        var subtype = unifiedVideoCapture.IsP010 ? "P010" : "NV12";
        return new PreviewColorProbeResult
        {
            SessionActive = true,
            RendererMode = d3dSink?.RendererMode ?? "None",
            NegotiatedSubtype = subtype,
            SourceWidth = unifiedVideoCapture.Width,
            SourceHeight = unifiedVideoCapture.Height,
            SourceFrameRate = Math.Round(unifiedVideoCapture.Fps, 3),
            D3DInputColorSpace = d3dInputColor,
            D3DOutputColorSpace = d3dOutputColor
        };
    }

    public Task<PreviewFrameCaptureResult> CapturePreviewFrameAsync(string outputPath)
    {
        var d3dSink = _previewFrameSink as D3D11PreviewRenderer;
        if (d3dSink == null || !d3dSink.IsRendering)
        {
            return Task.FromResult(new PreviewFrameCaptureResult
            {
                Succeeded = false,
                Message = "No active preview renderer."
            });
        }

        return d3dSink.CaptureNextFrameAsync(outputPath);
    }

    private static string? ResolveRequestedFrameRateArg(CaptureSettings? settings, string? fallbackArg)
    {
        if (!string.IsNullOrWhiteSpace(settings?.RequestedFrameRateArg))
        {
            return settings.RequestedFrameRateArg;
        }

        if (settings?.RequestedFrameRateNumerator is uint numerator &&
            settings.RequestedFrameRateDenominator is uint denominator &&
            numerator > 0 &&
            denominator > 0)
        {
            return $"{numerator}/{denominator}";
        }

        return fallbackArg;
    }

    private static int? ResolveTelemetryAgeSeconds(DateTimeOffset telemetryTimestampUtc, DateTimeOffset nowUtc)
    {
        var age = nowUtc - telemetryTimestampUtc;
        if (age < TimeSpan.Zero)
        {
            return 0;
        }

        return (int)Math.Floor(age.TotalSeconds);
    }

    private static long ComputeTickAge(long tick)
    {
        if (tick == 0) return -1;
        return Math.Max(0, Environment.TickCount64 - tick);
    }

    private static string ResolveSourceTelemetryBackend(SourceSignalTelemetrySnapshot telemetry)
        => telemetry.Origin switch
        {
            SourceTelemetryOrigin.Egav => "Egav",
            SourceTelemetryOrigin.DeviceFormatFallback => "DeviceFormatFallback",
            SourceTelemetryOrigin.KsXu => "KsXu",
            SourceTelemetryOrigin.Rtice => "Rtice",
            _ => "Unknown"
        };

    private static string ResolveSourceFrameRateOrigin(SourceSignalTelemetrySnapshot telemetry)
    {
        if (!telemetry.FrameRateExact.HasValue || telemetry.FrameRateExact.Value <= 0)
        {
            return "Unknown";
        }

        return telemetry.Origin switch
        {
            SourceTelemetryOrigin.Egav => "SourceTelemetry(Egav)",
            SourceTelemetryOrigin.DeviceFormatFallback => "SourceTelemetry(DeviceFormatFallback)",
            SourceTelemetryOrigin.KsXu => "SourceTelemetry(KsXu)",
            SourceTelemetryOrigin.Rtice => "SourceTelemetry(Rtice)",
            _ => "SourceTelemetry"
        };
    }

    private static string? ResolveSourceTelemetrySuppressedReason(SourceSignalTelemetrySnapshot telemetry)
    {
        if (string.IsNullOrWhiteSpace(telemetry.DiagnosticSummary))
        {
            return null;
        }

        if (telemetry.DiagnosticSummary.Contains("suppressed", StringComparison.OrdinalIgnoreCase) ||
            telemetry.DiagnosticSummary.Contains("disabled", StringComparison.OrdinalIgnoreCase))
        {
            return telemetry.DiagnosticSummary;
        }

        return null;
    }

    private static string ResolveSourceTelemetryCircuitState(
        SourceTelemetryAvailability availability,
        bool telemetrySuppressed)
    {
        if (telemetrySuppressed)
        {
            return "Open";
        }

        return availability switch
        {
            SourceTelemetryAvailability.Unavailable => "Open",
            SourceTelemetryAvailability.Stale => "Open",
            _ => "Closed"
        };
    }

    private static (string Status, string Reason) ResolveTelemetryAlignment(
        CaptureSettings? requestedSettings,
        SourceSignalTelemetrySnapshot telemetry,
        uint? actualWidth,
        uint? actualHeight,
        double? actualFrameRate,
        bool hdrRequested)
    {
        if (telemetry.Availability is SourceTelemetryAvailability.Unknown or SourceTelemetryAvailability.Unavailable)
        {
            return ("Unavailable", telemetry.DiagnosticSummary ?? "Source telemetry unavailable.");
        }

        var expectedWidth = (int?)(requestedSettings?.Width ?? actualWidth);
        var expectedHeight = (int?)(requestedSettings?.Height ?? actualHeight);
        var expectedFrameRate = requestedSettings?.FrameRate ?? actualFrameRate;
        var mismatches = new List<string>();

        if (!telemetry.Width.HasValue || !telemetry.Height.HasValue || !telemetry.FrameRateExact.HasValue)
        {
            return ("Inconclusive", "Telemetry did not include full mode dimensions and frame rate.");
        }

        if (expectedWidth.HasValue && telemetry.Width.Value != expectedWidth.Value)
        {
            mismatches.Add($"width expected {expectedWidth.Value}, observed {telemetry.Width.Value}");
        }

        if (expectedHeight.HasValue && telemetry.Height.Value != expectedHeight.Value)
        {
            mismatches.Add($"height expected {expectedHeight.Value}, observed {telemetry.Height.Value}");
        }

        if (expectedFrameRate.HasValue && Math.Abs(telemetry.FrameRateExact.Value - expectedFrameRate.Value) > 0.75)
        {
            mismatches.Add($"fps expected {expectedFrameRate.Value:0.###}, observed {telemetry.FrameRateExact.Value:0.###}");
        }

        if (telemetry.IsHdr.HasValue && telemetry.IsHdr.Value != hdrRequested)
        {
            mismatches.Add($"hdr expected {hdrRequested}, observed {telemetry.IsHdr.Value}");
        }

        if (mismatches.Count == 0)
        {
            return ("Aligned", "Source telemetry matches requested capture settings.");
        }

        return ("Mismatch", string.Join("; ", mismatches));
    }

    private static string ResolveHdrWarmupState(
        bool hdrRequested,
        bool hdrOutputActive,
        bool isRecording,
        long observedP010Frames)
    {
        if (!hdrRequested)
        {
            return "NotRequested";
        }

        if (hdrOutputActive)
        {
            return "Satisfied";
        }

        if (observedP010Frames > 0)
        {
            return isRecording ? "Partial" : "Pending";
        }

        return isRecording ? "Degraded" : "Pending";
    }

    public CaptureHealthSnapshot GetHealthSnapshot()
    {
        var encoder = _ffmpegEncoder;
        var unifiedVideoCapture = _unifiedVideoCapture;
        var observedTelemetry = ResolveObservedFrameTelemetry(encoder);
        var videoFramesDropped = encoder?.DroppedVideoFrames ?? Interlocked.Read(ref _videoFramesDropped);
        var sourceTelemetrySuppressedReason = ResolveSourceTelemetrySuppressedReason(_latestSourceTelemetry);
        var sourceTelemetrySuppressed = !string.IsNullOrWhiteSpace(sourceTelemetrySuppressedReason);
        var sourceCadence = unifiedVideoCapture?.GetSourceCadenceMetrics()
            ?? default(MfSourceReaderVideoCapture.SourceCadenceMetrics);

        return new CaptureHealthSnapshot
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            SessionState = _sessionState,
            IsRecording = _isRecording,
            RecordingBackend = _isRecording ? "FfmpegPipe" : "None",
            RecordingElapsedMs = _isRecording ? _recordingStopwatch.ElapsedMilliseconds : 0,
            ExpectedFrameRate = _actualFrameRate ?? _currentSettings?.FrameRate ?? 0,
            NegotiatedWidth = _actualWidth,
            NegotiatedHeight = _actualHeight,
            NegotiatedFrameRate = _actualFrameRate,
            NegotiatedFrameRateArg = _actualFrameRateArg,
            NegotiatedFrameRateNumerator = _actualFrameRateNumerator,
            NegotiatedFrameRateDenominator = _actualFrameRateDenominator,
            NegotiatedPixelFormat = _actualPixelFormat,
            RequestedReaderSubtype = _currentSettings?.RequestedPixelFormat,
            ReaderSourceStreamType = (_isRecording || _isVideoPreviewActive) && unifiedVideoCapture != null
                ? "MfSourceReader"
                : null,
            ReaderSourceSubtype = observedTelemetry.LatestObservedFramePixelFormat ?? _actualPixelFormat,
            FirstObservedFramePixelFormat = observedTelemetry.FirstObservedFramePixelFormat,
            LatestObservedFramePixelFormat = observedTelemetry.LatestObservedFramePixelFormat,
            ObservedP010FrameCount = observedTelemetry.ObservedP010FrameCount,
            ObservedNv12FrameCount = observedTelemetry.ObservedNv12FrameCount,
            ObservedOtherFrameCount = observedTelemetry.ObservedOtherFrameCount,
            SourceTelemetryAvailability = _latestSourceTelemetry.Availability,
            SourceTelemetryOrigin = _latestSourceTelemetry.Origin,
            SourceTelemetryConfidence = _latestSourceTelemetry.Confidence,
            SourceTelemetryOriginDetail = _latestSourceTelemetry.OriginDetail,
            SourceTelemetryDiagnosticSummary = _latestSourceTelemetry.DiagnosticSummary,
            SourceTelemetryTimestampUtc = _latestSourceTelemetry.TimestampUtc,
            SourceWidth = _latestSourceTelemetry.Width,
            SourceHeight = _latestSourceTelemetry.Height,
            SourceFrameRateExact = _latestSourceTelemetry.FrameRateExact,
            SourceFrameRateArg = _latestSourceTelemetry.FrameRateArg,
            SourceIsHdr = _latestSourceTelemetry.IsHdr,
            SourceTelemetryBackend = ResolveSourceTelemetryBackend(_latestSourceTelemetry),
            SourceTelemetrySuppressedReason = sourceTelemetrySuppressedReason,
            SourceTelemetrySuppressed = sourceTelemetrySuppressed,
            SourceTelemetryCircuitState = ResolveSourceTelemetryCircuitState(
                _latestSourceTelemetry.Availability,
                sourceTelemetrySuppressed),
            VideoFramesArrived = unifiedVideoCapture?.VideoFramesArrived ?? Interlocked.Read(ref _videoFramesArrived),
            VideoFramesDropped = videoFramesDropped,
            VideoDropsQueueSaturated = encoder?.VideoDropsQueueSaturated ?? 0,
            VideoDropsBacklogEviction = encoder?.VideoDropsBacklogEviction ?? 0,
            AudioDropsQueueSaturated = encoder?.AudioDropsQueueSaturated ?? 0,
            AudioDropsBacklogEviction = encoder?.AudioDropsBacklogEviction ?? 0,
            FfmpegVideoQueueDepth = encoder?.VideoQueueCount ?? 0,
            FfmpegAudioQueueDepth = encoder?.AudioQueueCount ?? 0,
            VideoFramesEnqueued = encoder?.VideoFramesEnqueuedCount ?? 0,
            LastVideoEnqueueAgeMs = ComputeTickAge(encoder?.LastVideoEnqueueTick ?? 0),
            LastVideoWriteAgeMs = ComputeTickAge(encoder?.LastVideoWriteTick ?? 0),
            CaptureCadenceSampleCount = sourceCadence.SampleCount,
            CaptureCadenceObservedFps = sourceCadence.ObservedFps,
            CaptureCadenceExpectedIntervalMs = sourceCadence.ExpectedIntervalMs,
            CaptureCadenceAverageIntervalMs = sourceCadence.AverageIntervalMs,
            CaptureCadenceP95IntervalMs = sourceCadence.P95IntervalMs,
            CaptureCadenceMaxIntervalMs = sourceCadence.MaxIntervalMs,
            CaptureCadenceJitterStdDevMs = sourceCadence.JitterStdDevMs,
            CaptureCadenceSevereGapCount = sourceCadence.SevereGapCount,
            CaptureCadenceEstimatedDroppedFrames = sourceCadence.EstimatedDroppedFrames,
            CaptureCadenceEstimatedDropPercent = sourceCadence.EstimatedDropPercent
        };
    }

    public Task InitializeAsync(CaptureDevice device, CaptureSettings settings, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Initializing, async transitionToken =>
        {
            _currentDevice = device;
            _currentSettings = settings;
            _audioDeviceId = settings.UseCustomAudioInput ? settings.AudioDeviceId : device.AudioDeviceId;
            _audioDeviceName = settings.UseCustomAudioInput ? settings.AudioDeviceName : device.AudioDeviceName;
            _actualWidth = settings.Width;
            _actualHeight = settings.Height;
            _actualFrameRate = settings.FrameRate;
            _actualFrameRateNumerator = settings.RequestedFrameRateNumerator;
            _actualFrameRateDenominator = settings.RequestedFrameRateDenominator;
            _actualFrameRateArg = settings.RequestedFrameRateArg ?? settings.FrameRate.ToString("0.###");
            _actualPixelFormat = settings.RequestedPixelFormat ?? (settings.HdrEnabled ? "P010" : "NV12");
            _activeVideoInputPixelFormat = settings.HdrEnabled ? "p010le" : "nv12";
            _lastUsePostMuxAudio = false;
            Interlocked.Exchange(ref _videoFramesDropped, 0);
            ResetObservedPixelTelemetry();
            _latestSourceTelemetry = BuildFallbackTelemetry();
            await RefreshSourceTelemetryAsync(transitionToken).ConfigureAwait(false);
            _isInitialized = true;
            StatusChanged?.Invoke(this, "Initialized");
        }, cancellationToken);

    public Task StartVideoPreviewAsync(CaptureSettings settings, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Previewing, async transitionToken =>
        {
            EnsureInitialized();
            if (_currentDevice == null) throw new InvalidOperationException("No selected video device is available for preview.");
            if (_isVideoPreviewActive) return;
            transitionToken.ThrowIfCancellationRequested();
            if (_isRecording && _unifiedVideoCapture != null)
            {
                _unifiedVideoCapture.SetPreviewSink(_previewFrameSink);
                TryApplySharedPreviewDevice(_unifiedVideoCapture, _previewFrameSink);
                _isVideoPreviewActive = true;
                StatusChanged?.Invoke(this, "Preview started");
                return;
            }

            var hdrRequested = HdrOutputPolicy.IsEnabled(settings);
            var requireP010 = hdrRequested;
            var audioDeviceId = settings.AudioEnabled
                ? (settings.UseCustomAudioInput ? settings.AudioDeviceId : (_audioDeviceId ?? _currentDevice.AudioDeviceId))
                : null;

            Logger.Log(
                "HDR_REQUEST_STATE scope=preview " +
                $"hdr_toggle={settings.HdrEnabled} " +
                $"require_p010={requireP010} " +
                $"mode={settings.Width}x{settings.Height}@{settings.FrameRate:0.###}");

            UnifiedVideoCapture? unifiedVideoCapture = null;
            WasapiAudioCapture? wasapiCapture = null;
            try
            {
                unifiedVideoCapture = new UnifiedVideoCapture();
                ConfigureObservedPixelTelemetry(unifiedVideoCapture);
                await unifiedVideoCapture.InitializeAsync(
                    _currentDevice.Id,
                    (int)settings.Width,
                    (int)settings.Height,
                    settings.FrameRate,
                    requireP010).ConfigureAwait(false);
                unifiedVideoCapture.SetPreviewSink(_previewFrameSink);
                TryApplySharedPreviewDevice(unifiedVideoCapture, _previewFrameSink);
                unifiedVideoCapture.Start();
                _unifiedVideoCapture = unifiedVideoCapture;
                _lastMfSourceReaderFramesDelivered = 0;
                _lastMfSourceReaderFramesDropped = 0;
                _lastMfSourceReaderNegotiatedFormat = unifiedVideoCapture.NegotiatedFormat;

                _actualWidth = (uint)Math.Max(1, unifiedVideoCapture.Width);
                _actualHeight = (uint)Math.Max(1, unifiedVideoCapture.Height);
                _actualFrameRate = unifiedVideoCapture.Fps > 0 ? unifiedVideoCapture.Fps : settings.FrameRate;
                _actualFrameRateArg = settings.RequestedFrameRateArg ?? _actualFrameRate.Value.ToString("0.###");
                _actualPixelFormat = unifiedVideoCapture.IsP010 ? "P010" : "NV12";
                _activeVideoInputPixelFormat = unifiedVideoCapture.IsP010 ? "p010le" : "nv12";

                if (settings.AudioEnabled)
                {
                    var resolvedAudioDeviceId = audioDeviceId
                        ?? throw new InvalidOperationException("Audio preview is enabled but no audio capture device is available.");
                    wasapiCapture = new WasapiAudioCapture();
                    await wasapiCapture.InitializeAsync(resolvedAudioDeviceId, transitionToken).ConfigureAwait(false);
                    wasapiCapture.AudioLevelUpdated += OnWasapiAudioLevelUpdated;
                    wasapiCapture.CaptureFailed += OnWasapiCaptureFailed;
                    wasapiCapture.Start();
                    _wasapiAudioCapture = wasapiCapture;
                }

                if (_isAudioPreviewActive && _wasapiAudioCapture != null)
                {
                    await StartWasapiPlaybackAsync(transitionToken).ConfigureAwait(false);
                }

                Logger.Log("Preview backend active: IMFSourceReader video + WASAPI audio ingest.");
            }
            catch (Exception ex)
            {
                Logger.Log($"Unified preview start failed: {ex.Message}");
                _unifiedVideoCapture = null;
                if (unifiedVideoCapture != null)
                {
                    try
                    {
                        await unifiedVideoCapture.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception disposeEx)
                    {
                        Logger.Log($"Unified preview rollback dispose warning: {disposeEx.Message}");
                    }
                }

                if (wasapiCapture != null)
                {
                    wasapiCapture.AudioLevelUpdated -= OnWasapiAudioLevelUpdated;
                    wasapiCapture.CaptureFailed -= OnWasapiCaptureFailed;
                }

                var capture = _wasapiAudioCapture ?? wasapiCapture;
                _wasapiAudioCapture = null;
                if (capture != null)
                {
                    DetachWasapiAudioCapture(capture);
                    try
                    {
                        await capture.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception disposeEx)
                    {
                        Logger.Log($"WASAPI capture rollback dispose warning: {disposeEx.Message}");
                    }
                }

                throw;
            }

            _isVideoPreviewActive = true;
            StartTelemetryPoll();
            StatusChanged?.Invoke(this, "Preview started");
        }, cancellationToken);

    public Task StopVideoPreviewAsync(CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Ready, async transitionToken =>
        {
            if (!_isVideoPreviewActive) return;
            transitionToken.ThrowIfCancellationRequested();

            if (_isRecording)
            {
                _unifiedVideoCapture?.SetPreviewSink(null);
            }
            else
            {
                var unifiedVideoCapture = _unifiedVideoCapture;
                _unifiedVideoCapture = null;
                if (unifiedVideoCapture != null)
                {
                    _lastMfSourceReaderFramesDelivered = unifiedVideoCapture.VideoFramesArrived;
                    _lastMfSourceReaderFramesDropped = unifiedVideoCapture.VideoFramesDropped;
                    _lastMfSourceReaderNegotiatedFormat = unifiedVideoCapture.NegotiatedFormat;
                    await unifiedVideoCapture.StopAsync().ConfigureAwait(false);
                    await unifiedVideoCapture.DisposeAsync().ConfigureAwait(false);
                }

                var capture = _wasapiAudioCapture;
                _wasapiAudioCapture = null;
                DetachWasapiAudioCapture(capture);
                if (capture != null)
                {
                    await capture.DisposeAsync().ConfigureAwait(false);
                }
            }

            _isVideoPreviewActive = false;
            if (!_isRecording) StopTelemetryPoll();
            StatusChanged?.Invoke(this, "Preview stopped");
        }, cancellationToken);

    public Task StartRecordingAsync(CaptureSettings settings, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Recording, async transitionToken =>
        {
            EnsureInitialized();
            if (_isRecording)
            {
                return;
            }

            if (_currentDevice == null)
            {
                throw new InvalidOperationException("No selected video device is available for recording.");
            }

            transitionToken.ThrowIfCancellationRequested();

            FFmpegEncoderService? encoder = null;
            IRecordingSink? recordingSink = null;
            WasapiAudioCapture? ownedWasapiAudioCapture = null;
            UnifiedVideoCapture? ownedUnifiedVideoCapture = null;
            var sinkAttachedForAudioOnly = false;
            Volatile.Write(ref _wasapiAudioCaptureFaulted, false);
            Volatile.Write(ref _wasapiAudioCaptureFaultMessage, null);
            try
            {
                encoder = new FFmpegEncoderService();
                encoder.FrameEncoded += (s, count) => FrameCaptured?.Invoke(this, count);
                encoder.IngressViolationDetected += (s, reason) =>
                {
                    ErrorOccurred?.Invoke(this, new InvalidOperationException(reason));
                };

                StorageFolder outputFolder;
                try
                {
                    outputFolder = await StorageFolder.GetFolderFromPathAsync(settings.OutputPath);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Output folder is unavailable: {settings.OutputPath}", ex);
                }

                transitionToken.ThrowIfCancellationRequested();

                var effectiveWidth = _actualWidth ?? settings.Width;
                var effectiveHeight = _actualHeight ?? settings.Height;
                var effectiveFrameRate = _actualFrameRate ?? settings.FrameRate;
                var frameRateArg = ResolveFrameRateArg(settings, effectiveFrameRate);
                await RefreshSourceTelemetryAsync(transitionToken).ConfigureAwait(false);
                var hdrPipelineRequested = HdrOutputPolicy.IsEnabled(settings);
                if (hdrPipelineRequested && _latestSourceTelemetry.IsHdr == false)
                {
                    Logger.Log("HDR requested while source telemetry reports SDR; continuing to request P010 (no silent fallback).");
                }

                var videoInputPixelFormat = hdrPipelineRequested ? "p010le" : "nv12";
                var audioDeviceName = settings.AudioEnabled
                    ? (settings.UseCustomAudioInput ? settings.AudioDeviceName : (_audioDeviceName ?? _currentDevice.AudioDeviceName))
                    : null;
                var audioDeviceId = settings.AudioEnabled
                    ? (settings.UseCustomAudioInput ? settings.AudioDeviceId : (_audioDeviceId ?? _currentDevice.AudioDeviceId))
                    : null;

                var recordingContext = await _artifactManager.CreateContextAsync(
                    outputFolder,
                    settings,
                    usePostMuxAudio: true,
                    audioDeviceName,
                    effectiveFrameRate,
                    frameRateArg,
                    effectiveWidth,
                    effectiveHeight,
                    videoInputPixelFormat).ConfigureAwait(false);

                transitionToken.ThrowIfCancellationRequested();
                var requireP010 = recordingContext.HdrPipelineActive;
                encoder.SetMfReadwriteDisableConverters(requireP010);
                Logger.Log(
                    "HDR_NEGOTIATION " +
                    $"requested_hdr={hdrPipelineRequested} " +
                    $"requested_subtype={(hdrPipelineRequested ? "P010" : "NV12")} " +
                    $"negotiated_pixel_format={_actualPixelFormat ?? "unknown"} " +
                    $"negotiated_subtype_token={(string.Equals(videoInputPixelFormat, "p010le", StringComparison.OrdinalIgnoreCase) ? "P010|MFVideoFormat_P010" : "NV12")} " +
                    $"hdr_static_metadata_requested={(!string.IsNullOrWhiteSpace(settings.HdrMasterDisplayMetadata) || (settings.HdrMaxCll > 0 && settings.HdrMaxFall > 0))} " +
                    $"hdr_master_display_set={(!string.IsNullOrWhiteSpace(settings.HdrMasterDisplayMetadata))} " +
                    $"hdr_max_cll={settings.HdrMaxCll} " +
                    $"hdr_max_fall={settings.HdrMaxFall} " +
                    $"mf_readwrite_disable_converters={(requireP010 ? "true" : "false")} " +
                    $"ffmpeg_ingest_pix_fmt={(string.Equals(videoInputPixelFormat, "p010le", StringComparison.OrdinalIgnoreCase) ? "AV_PIX_FMT_P010LE" : "AV_PIX_FMT_NV12")}");

                recordingSink = new FfmpegRecordingSink(encoder);
                await recordingSink.StartAsync(recordingContext, transitionToken).ConfigureAwait(false);
                transitionToken.ThrowIfCancellationRequested();
                var unifiedVideoCapture = _unifiedVideoCapture;
                if (unifiedVideoCapture == null)
                {
                    ownedUnifiedVideoCapture = new UnifiedVideoCapture();
                    ConfigureObservedPixelTelemetry(ownedUnifiedVideoCapture);
                    await ownedUnifiedVideoCapture.InitializeAsync(
                        _currentDevice.Id,
                        (int)effectiveWidth,
                        (int)effectiveHeight,
                        effectiveFrameRate,
                        requireP010).ConfigureAwait(false);
                    ownedUnifiedVideoCapture.SetPreviewSink(_isVideoPreviewActive ? _previewFrameSink : null);
                    TryApplySharedPreviewDevice(ownedUnifiedVideoCapture, _isVideoPreviewActive ? _previewFrameSink : null);
                    ownedUnifiedVideoCapture.Start();
                    unifiedVideoCapture = ownedUnifiedVideoCapture;
                    _unifiedVideoCapture = ownedUnifiedVideoCapture;
                }
                else if (unifiedVideoCapture.IsP010 != requireP010)
                {
                    throw new InvalidOperationException(
                        $"Recording requires {(requireP010 ? "P010" : "NV12")}, but the active source-reader session negotiated {(unifiedVideoCapture.IsP010 ? "P010" : "NV12")}.");
                }

                TryApplySharedPreviewDevice(unifiedVideoCapture, _isVideoPreviewActive ? _previewFrameSink : null);

                _lastMfSourceReaderFramesDelivered = 0;
                _lastMfSourceReaderFramesDropped = 0;
                _lastMfSourceReaderNegotiatedFormat = unifiedVideoCapture.NegotiatedFormat;
                _actualWidth = (uint)Math.Max(1, unifiedVideoCapture.Width);
                _actualHeight = (uint)Math.Max(1, unifiedVideoCapture.Height);
                _actualFrameRate = unifiedVideoCapture.Fps > 0 ? unifiedVideoCapture.Fps : effectiveFrameRate;
                _actualFrameRateArg = settings.RequestedFrameRateArg ?? _actualFrameRate.Value.ToString("0.###");
                _actualPixelFormat = unifiedVideoCapture.IsP010 ? "P010" : "NV12";

                if (_wasapiAudioCapture == null && settings.AudioEnabled)
                {
                    var resolvedAudioDeviceId = audioDeviceId
                        ?? throw new InvalidOperationException("Recording requires an audio capture device.");
                    ownedWasapiAudioCapture = new WasapiAudioCapture();
                    await ownedWasapiAudioCapture.InitializeAsync(resolvedAudioDeviceId, transitionToken).ConfigureAwait(false);
                    ownedWasapiAudioCapture.AudioLevelUpdated += OnWasapiAudioLevelUpdated;
                    ownedWasapiAudioCapture.CaptureFailed += OnWasapiCaptureFailed;
                    ownedWasapiAudioCapture.Start();
                    _wasapiAudioCapture = ownedWasapiAudioCapture;
                }

                if (_wasapiAudioCapture != null && settings.AudioEnabled)
                {
                    _wasapiAudioCapture.AttachRecordingSink(recordingSink);
                    sinkAttachedForAudioOnly = true;
                    if (_isAudioPreviewActive)
                    {
                        await StartWasapiPlaybackAsync(transitionToken).ConfigureAwait(false);
                    }
                }

                await unifiedVideoCapture.StartRecordingAsync(recordingSink, encoder).ConfigureAwait(false);

                _ffmpegEncoder = encoder;
                _recordingSink = recordingSink;
                _recordingContext = recordingContext;
                _activeRecordingSettings = settings;
                _isRecording = true;
                _activeVideoInputPixelFormat = videoInputPixelFormat;
                Interlocked.Exchange(ref _videoFramesDropped, 0);
                ResetObservedPixelTelemetry();
                _lastOutputPath = recordingContext.FinalOutputPath;
                _lastFinalizeStatus = "Recording";
                _lastFinalizeUtc = null;
                _lastPreservedArtifacts = Array.Empty<string>();
                _lastUsePostMuxAudio = recordingContext.UsePostMuxAudio;
                _recordingStopwatch.Restart();
                StatusChanged?.Invoke(this, "Recording");
                encoder = null;
                recordingSink = null;
                ownedWasapiAudioCapture = null;
                ownedUnifiedVideoCapture = null;
            }
            catch
            {
                if (sinkAttachedForAudioOnly && _wasapiAudioCapture != null)
                {
                    _wasapiAudioCapture.DetachRecordingSink();
                }

                await DisposeTransientRecordingBackendAsync(
                    recordingSink,
                    encoder,
                    ownedWasapiAudioCapture,
                    ownedUnifiedVideoCapture).ConfigureAwait(false);

                if (ownedWasapiAudioCapture != null && ReferenceEquals(_wasapiAudioCapture, ownedWasapiAudioCapture))
                {
                    DetachWasapiAudioCapture(ownedWasapiAudioCapture);
                    _wasapiAudioCapture = null;
                }

                if (ownedUnifiedVideoCapture != null && ReferenceEquals(_unifiedVideoCapture, ownedUnifiedVideoCapture))
                {
                    _lastMfSourceReaderFramesDelivered = ownedUnifiedVideoCapture.VideoFramesArrived;
                    _lastMfSourceReaderFramesDropped = ownedUnifiedVideoCapture.VideoFramesDropped;
                    _lastMfSourceReaderNegotiatedFormat = ownedUnifiedVideoCapture.NegotiatedFormat;
                    _unifiedVideoCapture = null;
                }

                _recordingContext = null;
                _activeRecordingSettings = null;
                _isRecording = false;
                _recordingStopwatch.Reset();
                throw;
            }
        }, cancellationToken);

    public Task StopRecordingAsync(CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Ready, async transitionToken =>
        {
            if (!_isRecording && _recordingSink == null && _ffmpegEncoder == null)
            {
                return;
            }

            var result = await StopAndDisposeRecordingBackendAsync("Stopped", transitionToken).ConfigureAwait(false);
            // Preview continues running on the active source-reader/WASAPI sessions - no resume needed.
            StatusChanged?.Invoke(this, result.StatusMessage);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(result.StatusMessage);
            }
        }, cancellationToken);

    public Task StartAudioPreviewAsync(CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Previewing, async transitionToken =>
        {
            EnsureInitialized();
            transitionToken.ThrowIfCancellationRequested();
            _isAudioPreviewActive = true;

            if (_wasapiAudioCapture != null)
            {
                await StartWasapiPlaybackAsync(transitionToken).ConfigureAwait(false);
            }

            StatusChanged?.Invoke(this, "Audio preview started");
        }, cancellationToken);

    public Task StopAudioPreviewAsync(CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Ready, transitionToken =>
        {
            transitionToken.ThrowIfCancellationRequested();
            _isAudioPreviewActive = false;
            StopWasapiPlayback();
            AudioLevelUpdated?.Invoke(this, new AudioLevelEventArgs(0, 0, false));
            StatusChanged?.Invoke(this, "Audio preview stopped");
            return Task.CompletedTask;
        }, cancellationToken);

    public Task UpdateAudioInputAsync(string? audioDeviceId, string? audioDeviceName, CancellationToken cancellationToken = default)
        => RunTransitionAsync(_sessionState, transitionToken =>
        {
            transitionToken.ThrowIfCancellationRequested();
            _audioDeviceId = audioDeviceId;
            _audioDeviceName = audioDeviceName;
            return Task.CompletedTask;
        }, cancellationToken);

    public Task CleanupAsync(CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.CleaningUp, async transitionToken =>
        {
            var cancellationRequested = false;
            if (_isRecording || _recordingSink != null || _ffmpegEncoder != null)
            {
                try
                {
                    var result = await StopAndDisposeRecordingBackendAsync(
                        "Stopped during cleanup",
                        transitionToken).ConfigureAwait(false);
                    if (!result.Succeeded)
                    {
                        Logger.Log($"Cleanup stop reported issues: {result.StatusMessage}");
                    }
                }
                catch (OperationCanceledException) when (transitionToken.IsCancellationRequested)
                {
                    cancellationRequested = true;
                }
            }

            var unifiedVideoCapture = _unifiedVideoCapture;
            _unifiedVideoCapture = null;
            if (unifiedVideoCapture != null)
            {
                try
                {
                    _lastMfSourceReaderFramesDelivered = unifiedVideoCapture.VideoFramesArrived;
                    _lastMfSourceReaderFramesDropped = unifiedVideoCapture.VideoFramesDropped;
                    _lastMfSourceReaderNegotiatedFormat = unifiedVideoCapture.NegotiatedFormat;
                    await unifiedVideoCapture.StopAsync().ConfigureAwait(false);
                    await unifiedVideoCapture.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Cleanup unified video stop/dispose warning: {ex.Message}");
                }
            }

            var wasapiCapture = _wasapiAudioCapture;
            _wasapiAudioCapture = null;
            DetachWasapiAudioCapture(wasapiCapture);
            if (wasapiCapture != null)
            {
                try
                {
                    await wasapiCapture.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Cleanup WASAPI capture dispose warning: {ex.Message}");
                }
            }

            StopTelemetryPoll();
            _isVideoPreviewActive = false;
            _isAudioPreviewActive = false;
            _isInitialized = false;
            _currentDevice = null;
            _currentSettings = null;
            _activeRecordingSettings = null;
            _recordingContext = null;
            _sessionState = _isDisposed ? CaptureSessionState.Disposed : CaptureSessionState.Uninitialized;

            if (cancellationRequested || transitionToken.IsCancellationRequested)
            {
                transitionToken.ThrowIfCancellationRequested();
            }
        }, cancellationToken);

    private async Task<FinalizeResult> StopAndDisposeRecordingBackendAsync(string fallbackStatusMessage, CancellationToken cancellationToken)
    {
        var sink = _recordingSink;
        var encoder = _ffmpegEncoder;
        var recordingContext = _recordingContext;
        var fallbackOutputPath = recordingContext?.FinalOutputPath ?? (_lastOutputPath ?? string.Empty);

        _recordingSink = null;
        _ffmpegEncoder = null;

        var result = FinalizeResult.Success(fallbackOutputPath, fallbackStatusMessage);
        OperationCanceledException? cancellationException = null;

        var unifiedVideoCapture = _unifiedVideoCapture;
        if (unifiedVideoCapture != null)
        {
            try
            {
                await unifiedVideoCapture.StopRecordingAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                cancellationException = new OperationCanceledException(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.Log($"Unified video recording stop failed: {ex.Message}");
            }

            _lastMfSourceReaderFramesDelivered = unifiedVideoCapture.VideoFramesArrived;
            _lastMfSourceReaderFramesDropped = unifiedVideoCapture.VideoFramesDropped;
            _lastMfSourceReaderNegotiatedFormat = unifiedVideoCapture.NegotiatedFormat;
            var recordingFramesDelivered = unifiedVideoCapture.RecordingFramesDelivered;
            var recordingFramesEnqueued = unifiedVideoCapture.RecordingFramesEnqueued;
            Logger.Log(
                "VIDEO_DIAG mf_source_reader " +
                $"frames_delivered={_lastMfSourceReaderFramesDelivered} " +
                $"frames_dropped={_lastMfSourceReaderFramesDropped} " +
                $"negotiated_format='{_lastMfSourceReaderNegotiatedFormat ?? "unknown"}'");
            Logger.Log(
                "VIDEO_DIAG recording_pipeline " +
                $"source_frames_during_recording={recordingFramesDelivered} " +
                $"frames_enqueued_to_encoder={recordingFramesEnqueued} " +
                $"pipeline_drops={recordingFramesDelivered - recordingFramesEnqueued}");
        }

        if (_wasapiAudioCapture != null)
        {
            try
            {
                _wasapiAudioCapture.DetachRecordingSink();
            }
            catch (Exception ex)
            {
                Logger.Log($"Audio recording sink detach failed: {ex.Message}");
            }
        }

        if (!_isVideoPreviewActive)
        {
            _unifiedVideoCapture = null;
            if (unifiedVideoCapture != null)
            {
                try
                {
                    await unifiedVideoCapture.StopAsync().ConfigureAwait(false);
                    await unifiedVideoCapture.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Unified video capture dispose failed: {ex.Message}");
                    result = FinalizeResult.Failure(fallbackOutputPath, $"Unified video capture dispose failed: {ex.Message}");
                }
            }

            var capture = _wasapiAudioCapture;
            _wasapiAudioCapture = null;
            DetachWasapiAudioCapture(capture);
            if (capture != null)
            {
                try
                {
                    await capture.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Recording WASAPI capture dispose failed: {ex.Message}");
                    result = FinalizeResult.Failure(fallbackOutputPath, $"Recording WASAPI capture dispose failed: {ex.Message}");
                }
            }
        }

        if (sink != null)
        {
            try
            {
                result = await sink.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                cancellationException = new OperationCanceledException(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.Log($"Recording sink stop failed: {ex.Message}");
                result = FinalizeResult.Failure(fallbackOutputPath, $"Recording stop failed: {ex.Message}");
            }
            finally
            {
                try
                {
                    await sink.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Recording sink dispose failed: {ex.Message}");
                    if (cancellationException == null && result.Succeeded)
                    {
                        result = FinalizeResult.Failure(fallbackOutputPath, $"Recording dispose failed: {ex.Message}");
                    }
                }
            }

        }

        var wasapiAudioCaptureFaulted = Volatile.Read(ref _wasapiAudioCaptureFaulted);
        var wasapiAudioCaptureFaultMessage = Volatile.Read(ref _wasapiAudioCaptureFaultMessage);
        Volatile.Write(ref _wasapiAudioCaptureFaulted, false);
        Volatile.Write(ref _wasapiAudioCaptureFaultMessage, null);
        if (wasapiAudioCaptureFaulted && cancellationException == null && result.Succeeded)
        {
            var statusMessage = string.IsNullOrWhiteSpace(wasapiAudioCaptureFaultMessage)
                ? "Recording failed (WASAPI audio capture faulted)."
                : $"Recording failed (WASAPI audio capture faulted: {wasapiAudioCaptureFaultMessage})";
            Logger.Log($"RECORDING_AUDIO_FAULT status='{statusMessage}'");
            result = FinalizeResult.Failure(result.OutputPath, statusMessage);
        }

        // Finalize split recording artifacts (clean up temp files on success, preserve on failure)
        if (recordingContext is { UsePostMuxAudio: true })
        {
            var artifactResult = _artifactManager.FinalizeContext(recordingContext, result.Succeeded);
            if (!artifactResult.Succeeded && result.Succeeded)
            {
                result = artifactResult;
            }
        }

        if (encoder != null)
        {
            CaptureEncoderRuntimeTelemetry(encoder);
            try
            {
                await encoder.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Recording encoder dispose failed: {ex.Message}");
                if (result.Succeeded)
                {
                    result = FinalizeResult.Failure(fallbackOutputPath, $"Recording encoder dispose failed: {ex.Message}");
                }
            }
        }

        _recordingStopwatch.Stop();
        _isRecording = false;
        if (!_isVideoPreviewActive) StopTelemetryPoll();
        _recordingContext = null;
        _activeRecordingSettings = null;
        _lastOutputPath = result.OutputPath;
        _lastFinalizeStatus = result.StatusMessage;
        _lastFinalizeUtc = DateTimeOffset.UtcNow;
        _lastPreservedArtifacts = result.PreservedArtifacts;

        if (cancellationException != null)
        {
            throw cancellationException;
        }

        return result;
    }

    private void TryApplySharedPreviewDevice(UnifiedVideoCapture? capture, IPreviewFrameSink? sink)
    {
        if (capture == null || sink is not D3D11PreviewRenderer renderer)
        {
            return;
        }

        var sharedDevice = capture.D3DManager?.Device;
        if (sharedDevice == null)
        {
            return;
        }

        try
        {
            renderer.SetSharedDevice(sharedDevice);
        }
        catch (Exception ex)
        {
            Logger.Log($"UNIFIED_VIDEO_SHARED_DEVICE_APPLY_WARN type={ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
        }
    }

    private static async Task DisposeTransientRecordingBackendAsync(
        IRecordingSink? sink,
        FFmpegEncoderService? encoder,
        WasapiAudioCapture? wasapiCapture,
        UnifiedVideoCapture? unifiedVideoCapture)
    {
        if (unifiedVideoCapture != null)
        {
            try
            {
                await unifiedVideoCapture.StopAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient unified video stop failed during rollback: {ex.Message}");
            }

            try
            {
                await unifiedVideoCapture.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient unified video dispose failed during rollback: {ex.Message}");
            }
        }

        if (wasapiCapture != null)
        {
            try
            {
                await wasapiCapture.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient WASAPI capture dispose failed during rollback: {ex.Message}");
            }
        }

        if (sink != null)
        {
            try
            {
                await sink.StopAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient recording sink stop failed during rollback: {ex.Message}");
            }

            try
            {
                await sink.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient recording sink dispose failed during rollback: {ex.Message}");
            }
        }

        if (encoder == null)
        {
            return;
        }

        try
        {
            await encoder.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"Transient recording encoder dispose failed during rollback: {ex.Message}");
        }
    }

    private async Task RunTransitionAsync(
        CaptureSessionState transitionState,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _sessionTransitionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _sessionState = transitionState;
            await action(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            _sessionState = ResolveSteadyState();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _sessionState = ResolveSteadyState();
            throw;
        }
        catch (Exception ex)
        {
            _sessionState = CaptureSessionState.Faulted;
            ErrorOccurred?.Invoke(this, ex);
            throw;
        }
        finally
        {
            _sessionTransitionLock.Release();
        }
    }

    private CaptureSessionState ResolveSteadyState()
    {
        if (_isDisposed) return CaptureSessionState.Disposed;
        if (_isRecording) return CaptureSessionState.Recording;
        if (_isVideoPreviewActive || _isAudioPreviewActive) return CaptureSessionState.Previewing;
        return _isInitialized ? CaptureSessionState.Ready : CaptureSessionState.Uninitialized;
    }

    private SourceSignalTelemetrySnapshot BuildFallbackTelemetry()
    {
        return new SourceSignalTelemetrySnapshot
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Availability = SourceTelemetryAvailability.Inconclusive,
            Origin = SourceTelemetryOrigin.DeviceFormatFallback,
            OriginDetail = "CaptureSettingsFallback",
            Confidence = SourceTelemetryConfidence.Low,
            Width = (int?)_actualWidth ?? (int?)_currentSettings?.Width,
            Height = (int?)_actualHeight ?? (int?)_currentSettings?.Height,
            FrameRateExact = _actualFrameRate ?? _currentSettings?.FrameRate,
            FrameRateArg = _actualFrameRateArg ?? _currentSettings?.RequestedFrameRateArg,
            IsHdr = null,
            DiagnosticSummary = "Using capture-format fallback telemetry."
        };
    }

    private async Task RefreshSourceTelemetryAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fallback = BuildFallbackTelemetry();
        SourceSignalTelemetrySnapshot telemetry;
        try
        {
            telemetry = await _sourceTelemetryProvider
                .ReadAsync(_currentDevice, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log($"Source telemetry read failed: {ex.Message}");
            telemetry = SourceSignalTelemetrySnapshot.CreateUnavailable("source-telemetry-exception", ex.Message);
        }

        _latestSourceTelemetry = MergeTelemetryWithFallback(telemetry, fallback);
        SourceTelemetryUpdated?.Invoke(this, _latestSourceTelemetry);
    }

    private void StartTelemetryPoll()
    {
        StopTelemetryPoll();
        var cts = new CancellationTokenSource();
        _telemetryPollCts = cts;
        _telemetryPollTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TelemetryPollIntervalMs, cts.Token).ConfigureAwait(false);
                    await RefreshSourceTelemetryAsync(cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Telemetry poll cycle failed: {ex.Message}");
                }
            }
        }, cts.Token);
    }

    private void StopTelemetryPoll()
    {
        var cts = _telemetryPollCts;
        _telemetryPollCts = null;
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
        }
        _telemetryPollTask = null;
    }

    private static SourceSignalTelemetrySnapshot MergeTelemetryWithFallback(
        SourceSignalTelemetrySnapshot telemetry,
        SourceSignalTelemetrySnapshot fallback)
    {
        return telemetry with
        {
            Width = telemetry.Width ?? fallback.Width,
            Height = telemetry.Height ?? fallback.Height,
            FrameRateExact = telemetry.FrameRateExact ?? fallback.FrameRateExact,
            FrameRateArg = telemetry.FrameRateArg ?? fallback.FrameRateArg,
            IsHdr = telemetry.IsHdr ?? fallback.IsHdr,
            Origin = telemetry.Origin == SourceTelemetryOrigin.Unknown
                ? fallback.Origin
                : telemetry.Origin,
            OriginDetail = string.IsNullOrWhiteSpace(telemetry.OriginDetail) ||
                           string.Equals(telemetry.OriginDetail, "Unknown", StringComparison.OrdinalIgnoreCase)
                ? fallback.OriginDetail
                : telemetry.OriginDetail,
            Confidence = telemetry.Confidence == SourceTelemetryConfidence.Unknown
                ? fallback.Confidence
                : telemetry.Confidence,
            DiagnosticSummary = string.IsNullOrWhiteSpace(telemetry.DiagnosticSummary)
                ? fallback.DiagnosticSummary
                : telemetry.DiagnosticSummary
        };
    }

    private static string ResolveFrameRateArg(CaptureSettings settings, double fallbackFrameRate)
    {
        if (!string.IsNullOrWhiteSpace(settings.RequestedFrameRateArg))
        {
            return settings.RequestedFrameRateArg!;
        }

        if (settings.RequestedFrameRateNumerator.HasValue &&
            settings.RequestedFrameRateDenominator.HasValue &&
            settings.RequestedFrameRateNumerator.Value > 0 &&
            settings.RequestedFrameRateDenominator.Value > 0)
        {
            return $"{settings.RequestedFrameRateNumerator.Value}/{settings.RequestedFrameRateDenominator.Value}";
        }

        return fallbackFrameRate > 0
            ? fallbackFrameRate.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
            : "60";
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Capture not initialized");
        }
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(CaptureService));
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        try
        {
            CleanupAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Logger.Log($"CaptureService.Dispose cleanup warning: {ex.Message}");
        }

        _isDisposed = true;
        _sessionTransitionLock.Dispose();
        _sessionState = CaptureSessionState.Disposed;
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        try
        {
            await CleanupAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"CaptureService.DisposeAsync cleanup warning: {ex.Message}");
        }

        _isDisposed = true;
        _sessionTransitionLock.Dispose();
        _sessionState = CaptureSessionState.Disposed;
    }
}


