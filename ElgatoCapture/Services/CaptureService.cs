using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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
    private MediaCapture? _mediaCapture;
    private MediaFrameReader? _frameReader;
    private MediaFrameReader? _recordingFrameReader; // Separate reader for recording
    private StorageFile? _recordingFile;
    private AviWriter? _aviWriter;
    private bool _isRecording;
    private bool _isInitialized;
    private readonly object _lockObject = new();
    private bool _isDisposed;

    // FFmpeg encoder for CFR output
    private FFmpegEncoderService? _ffmpegEncoder;

    // Frame conversion pipeline - decouples GPU copy from format conversion
    private BlockingCollection<SoftwareBitmap>? _conversionQueue;
    private Task? _conversionWorkerTask;
    private CancellationTokenSource? _conversionCancellation;
    private const int ConversionQueueSize = 360; // ~6 seconds at 60fps (feeds NVENC)
    private const string VideoInputPixelFormat = "nv12";
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
    private int _loggedFirstFrameArrival;
    private int _loggedFirstFrameQueued;
    private int _loggedFirstFrameConverted;
    private int _loggedFirstFrameEnqueued;

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
    private readonly object _audioFileLock = new();
    private long _audioBytesWritten;
    private string? _audioTempPath;
    private string? _finalOutputPath;
    private bool _postMuxAudioEnabled;
    private string? _ffmpegPathForMux;
    private const int RecordingAudioSampleRate = 48000;
    private const short RecordingAudioChannels = 2;
    private const short RecordingAudioBitsPerSample = 32;
    private long _lastAudioLevelUpdateTick;

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
    private double? _actualFrameRate;
    private string? _actualFrameRateArg;
    private uint? _actualWidth;
    private uint? _actualHeight;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<Exception>? ErrorOccurred;
    public event EventHandler<ulong>? FrameCaptured;
    public event EventHandler<AudioLevelEventArgs>? AudioLevelUpdated;

    public bool IsRecording => _isRecording;
    public bool IsInitialized => _isInitialized;
    public bool IsAudioPreviewActive => _isAudioPreviewActive;
    public MediaCapture? MediaCapture => _mediaCapture;

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

    public async Task InitializeAsync(CaptureDevice device, CaptureSettings settings)
    {
        await CleanupAsync();

        // Store device and settings for potential reinitialization during recording
        _currentDevice = device;
        _currentSettings = settings;
        _actualFrameRate = null;
        _actualFrameRateArg = null;
        _actualWidth = null;
        _actualHeight = null;

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

            Logger.Log($"Device has {availableProperties.Count} available formats");

            var matchingFormat = availableProperties
                .OfType<VideoEncodingProperties>()
                .FirstOrDefault(p =>
                    p.Width == settings.Width &&
                    p.Height == settings.Height &&
                    Math.Abs((double)p.FrameRate.Numerator / p.FrameRate.Denominator - settings.FrameRate) < 1);

            if (matchingFormat != null)
            {
                var fps = (double)matchingFormat.FrameRate.Numerator / matchingFormat.FrameRate.Denominator;
                _actualFrameRate = fps;
                _actualFrameRateArg = $"{matchingFormat.FrameRate.Numerator}/{matchingFormat.FrameRate.Denominator}";
                _actualWidth = matchingFormat.Width;
                _actualHeight = matchingFormat.Height;
                Logger.Log($"✓ Found matching format: {matchingFormat.Width}x{matchingFormat.Height}@{fps:F1}fps ({matchingFormat.Subtype})");
                if (Math.Abs(fps - settings.FrameRate) > 0.01)
                {
                    Logger.Log($"Requested FPS {settings.FrameRate:F3} differs from device FPS {fps:F3}. Using device FPS for FFmpeg.");
                }
                await videoController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoRecord, matchingFormat);
                Logger.Log($"✓ Device format set successfully");
            }
            else
            {
                Logger.Log($"✗ No matching format found for {settings.Width}x{settings.Height}@{settings.FrameRate}fps");
                Logger.Log("Available formats:");
                foreach (var prop in availableProperties.OfType<VideoEncodingProperties>().Take(10))
                {
                    var fps = (double)prop.FrameRate.Numerator / prop.FrameRate.Denominator;
                    Logger.Log($"  - {prop.Width}x{prop.Height}@{fps:F1}fps ({prop.Subtype})");
                }

                if (videoController.GetMediaStreamProperties(MediaStreamType.VideoRecord) is VideoEncodingProperties currentProps)
                {
                    var fps = currentProps.FrameRate.Denominator > 0
                        ? (double)currentProps.FrameRate.Numerator / currentProps.FrameRate.Denominator
                        : settings.FrameRate;
                    _actualFrameRate = fps;
                    _actualFrameRateArg = currentProps.FrameRate.Denominator > 0
                        ? $"{currentProps.FrameRate.Numerator}/{currentProps.FrameRate.Denominator}"
                        : null;
                    _actualWidth = currentProps.Width;
                    _actualHeight = currentProps.Height;
                    Logger.Log($"Using current device format for FFmpeg: {currentProps.Width}x{currentProps.Height}@{fps:F1}fps ({currentProps.Subtype})");
                }
            }

            _isInitialized = true;
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

    public async Task StartRecordingAsync(CaptureSettings settings)
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
            Logger.Log($"Audio preview active: {_isAudioPreviewActive}");
            var canCaptureAudio = settings.AudioEnabled && !string.IsNullOrEmpty(_audioDeviceId);
            Logger.Log($"Audio in recording: {(canCaptureAudio ? "Yes" : "No")}");

            var folder = await StorageFolder.GetFolderFromPathAsync(settings.OutputPath);
            var outputFile = await folder.CreateFileAsync(
                settings.GetOutputFileName(),
                CreationCollisionOption.GenerateUniqueName);

            _postMuxAudioEnabled = settings.Format != RecordingFormat.UncompressedAvi && canCaptureAudio;
            _finalOutputPath = null;
            _audioTempPath = null;

            if (_postMuxAudioEnabled)
            {
                _finalOutputPath = outputFile.Path;
                var baseName = Path.GetFileNameWithoutExtension(outputFile.Path);
                var extension = Path.GetExtension(outputFile.Path);
                var tempVideoFile = await folder.CreateFileAsync(
                    $"{baseName}_video{extension}",
                    CreationCollisionOption.GenerateUniqueName);
                var tempAudioFile = await folder.CreateFileAsync(
                    $"{baseName}_audio.wav",
                    CreationCollisionOption.GenerateUniqueName);

                _recordingFile = tempVideoFile;
                _audioTempPath = tempAudioFile.Path;
                StartAudioCaptureFile(_audioTempPath);

                Logger.Log("Post-mux audio enabled");
                Logger.Log($"Video temp file: {_recordingFile.Path}");
                Logger.Log($"Audio temp file: {_audioTempPath}");
                Logger.Log($"Final output file: {_finalOutputPath}");
            }
            else
            {
                _recordingFile = outputFile;
            }

            if (settings.Format == RecordingFormat.UncompressedAvi)
            {
                await StartUncompressedRecordingAsync(settings);
            }
            else
            {
                await StartCompressedRecordingAsync(settings);
            }

            _isRecording = true;
            StatusChanged?.Invoke(this, "Recording");
            Logger.Log("✓ Recording started successfully");
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
            throw;
        }
    }

    private async Task StartCompressedRecordingAsync(CaptureSettings settings)
    {
        if (_mediaCapture == null || _recordingFile == null) return;

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
            _conversionQueue = new BlockingCollection<SoftwareBitmap>(ConversionQueueSize);
            _conversionCancellation = new CancellationTokenSource();
            _conversionWorkerTask = Task.Run(() => RunConversionWorkerAsync(_conversionCancellation.Token));
            Logger.Log($"Frame conversion pipeline initialized (queue size: {ConversionQueueSize})");

            // Set up audio capture FIRST - start buffering before FFmpeg starts
            // This ensures audio samples are queued during FFmpeg's ~4 second probe phase
            if (captureAudio)
            {
                Logger.LogVerbose($"Audio capture setup starting at {startupStopwatch.ElapsedMilliseconds} ms");
                await SetupRecordingAudioCaptureAsync();
                Logger.LogVerbose($"Audio capture setup complete at {startupStopwatch.ElapsedMilliseconds} ms");
                Logger.Log("Audio capture started - buffering while FFmpeg initializes");
            }

            // Start FFmpeg encoder with output path and audio device name
            // (FFmpeg will create named pipe for audio if audioDevice is specified)
            var effectiveFrameRate = _actualFrameRate ?? settings.FrameRate;
            var frameRateArg = !string.IsNullOrWhiteSpace(_actualFrameRateArg)
                ? _actualFrameRateArg
                : effectiveFrameRate.ToString("0.###", CultureInfo.InvariantCulture);
            var effectiveWidth = _actualWidth ?? settings.Width;
            var effectiveHeight = _actualHeight ?? settings.Height;
            var ffmpegSettings = settings;
            if (_postMuxAudioEnabled && captureAudio)
            {
                ffmpegSettings = new CaptureSettings
                {
                    Width = settings.Width,
                    Height = settings.Height,
                    FrameRate = settings.FrameRate,
                    Format = settings.Format,
                    Quality = settings.Quality,
                    CustomBitrateMbps = settings.CustomBitrateMbps,
                    HdrEnabled = settings.HdrEnabled,
                    OutputPath = settings.OutputPath,
                    AudioEnabled = false
                };
            }

            await _ffmpegEncoder.StartEncodingAsync(
                ffmpegSettings,
                _recordingFile.Path,
                audioDevice,
                effectiveFrameRate,
                frameRateArg,
                effectiveWidth,
                effectiveHeight,
                VideoInputPixelFormat);
            Logger.LogVerbose($"FFmpeg StartEncodingAsync returned at {startupStopwatch.ElapsedMilliseconds} ms");

            // Set up frame reader for recording (uses existing MediaCapture)
            await SetupRecordingFrameReaderAsync(settings);
            Logger.LogVerbose($"Recording frame reader started at {startupStopwatch.ElapsedMilliseconds} ms");

            Logger.Log("FFmpeg recording started - frames will be piped to encoder");
        }
        catch
        {
            await CleanupRecordingResourcesOnErrorAsync();
            throw;
        }
    }

    private async Task SetupRecordingFrameReaderAsync(CaptureSettings settings)
    {
        if (_mediaCapture == null) return;

        // Find video frame source
        var frameSourceGroups = await MediaFrameSourceGroup.FindAllAsync();
        MediaFrameSourceGroup? matchingGroup = null;
        MediaFrameSourceInfo? videoSourceInfo = null;

        foreach (var group in frameSourceGroups)
        {
            var videoSource = group.SourceInfos.FirstOrDefault(si =>
                si.MediaStreamType == MediaStreamType.VideoRecord &&
                si.SourceKind == MediaFrameSourceKind.Color);

            if (videoSource != null)
            {
                matchingGroup = group;
                videoSourceInfo = videoSource;
                break;
            }
        }

        if (matchingGroup == null || videoSourceInfo == null)
        {
            Logger.Log("WARNING: Could not find frame source for FFmpeg recording");
            // Try to use VideoPreview stream instead
            var previewSources = _mediaCapture.FrameSources.Values
                .Where(fs => fs.Info.MediaStreamType == MediaStreamType.VideoPreview ||
                             fs.Info.MediaStreamType == MediaStreamType.VideoRecord)
                .ToList();

            if (previewSources.Count > 0)
            {
                Logger.Log($"Using alternative frame source: {previewSources[0].Info.MediaStreamType}");
                _recordingFrameReader = await _mediaCapture.CreateFrameReaderAsync(previewSources[0]);
            }
            else
            {
                throw new InvalidOperationException("No suitable frame source available for FFmpeg recording");
            }
        }
        else
        {
            // Use the identified frame source
            if (_mediaCapture.FrameSources.TryGetValue(videoSourceInfo.Id, out var frameSource))
            {
                _recordingFrameReader = await _mediaCapture.CreateFrameReaderAsync(frameSource);
            }
            else
            {
                // Fallback: try any available frame source
                var anySource = _mediaCapture.FrameSources.Values.FirstOrDefault();
                if (anySource != null)
                {
                    _recordingFrameReader = await _mediaCapture.CreateFrameReaderAsync(anySource);
                }
                else
                {
                    throw new InvalidOperationException("No frame sources available");
                }
            }
        }

        if (_recordingFrameReader != null)
        {
            _recordingFrameReader.FrameArrived += RecordingFrameReader_FrameArrived;
            var result = await _recordingFrameReader.StartAsync();
            Logger.Log($"Recording frame reader started: {result}");
        }
    }

    private async void RecordingFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        // Capture local reference to avoid race condition
        var conversionQueue = _conversionQueue;
        if (conversionQueue == null || conversionQueue.IsAddingCompleted) return;

        using var frame = sender.TryAcquireLatestFrame();
        if (frame?.VideoMediaFrame == null) return;

        try
        {
            SoftwareBitmap? softwareBitmap = null;
            var frameIndex = Interlocked.Increment(ref _videoFramesArrived);
            var nowMs = _recordingStopwatch.ElapsedMilliseconds;
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
            if (Logger.VerboseEnabled && frameIndex == 1)
            {
                var relTime = frame.SystemRelativeTime?.TotalMilliseconds;
                var relTimeText = relTime.HasValue ? relTime.Value.ToString("0.###", CultureInfo.InvariantCulture) : "n/a";
                Logger.LogVerbose($"First frame details: {softwareBitmap.PixelWidth}x{softwareBitmap.PixelHeight}, fmt={softwareBitmap.BitmapPixelFormat}, relTimeMs={relTimeText}");
            }

            // Queue the unconverted frame (YUY2/NV12) - worker thread will convert to BGRA8
            // This is fast - just queuing a reference, not copying pixels
            if (!conversionQueue.TryAdd(softwareBitmap, 10))
            {
                // Conversion queue full - worker thread is lagging
                Interlocked.Increment(ref _videoFramesDropped);
                Logger.Log($"Warning: Conversion queue full, dropping frame");
                softwareBitmap.Dispose();
            }
            else
            {
                var queuedCount = Interlocked.Increment(ref _videoFramesQueued);
                if (Logger.VerboseEnabled && Interlocked.Exchange(ref _loggedFirstFrameQueued, 1) == 0)
                {
                    Logger.LogVerbose($"First frame queued at {_recordingStopwatch.ElapsedMilliseconds} ms (queueCount={conversionQueue.Count})");
                }

                if (Logger.VerboseEnabled && lastArrivalMs > 0 && queuedCount % 120 == 0)
                {
                    var delta = nowMs - lastArrivalMs;
                    Logger.LogVerbose($"Frame arrival cadence: frame={queuedCount}, delta={delta} ms");
                }
            }

            LogPipelineStatsIfNeeded();
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
                    // Wait for unconverted frame (blocks if queue empty)
                    if (!queue.TryTake(out sourceBitmap, 100, cancellationToken))
                    {
                        if (Logger.VerboseEnabled)
                        {
                            var nowMs = _recordingStopwatch.ElapsedMilliseconds;
                            var lastIdle = Interlocked.Read(ref _lastConversionIdleLogMs);
                            if (nowMs - lastIdle >= 1000 &&
                                Interlocked.CompareExchange(ref _lastConversionIdleLogMs, nowMs, lastIdle) == lastIdle)
                            {
                                Logger.LogVerbose($"Conversion worker idle: no frames (queueCount={queue.Count})");
                            }
                        }
                        continue;
                    }

                    // Get encoder reference each iteration (might change during startup)
                    var encoder = _ffmpegEncoder;
                    if (encoder == null || !encoder.IsEncoding)
                    {
                        // Encoder not ready yet - requeue frame and wait
                        if (Logger.VerboseEnabled)
                        {
                            var nowMs = _recordingStopwatch.ElapsedMilliseconds;
                            var lastNotReady = Interlocked.Read(ref _lastEncoderNotReadyLogMs);
                            if (nowMs - lastNotReady >= 1000 &&
                                Interlocked.CompareExchange(ref _lastEncoderNotReadyLogMs, nowMs, lastNotReady) == lastNotReady)
                            {
                                Logger.LogVerbose("Conversion worker: encoder not ready, requeueing frame");
                            }
                        }
                        if (!queue.TryAdd(sourceBitmap, 10, cancellationToken))
                        {
                            Interlocked.Increment(ref _videoFramesDropped);
                            sourceBitmap.Dispose();
                        }
                        await Task.Delay(10, cancellationToken);
                        continue;
                    }

                    // Convert to NV12 for NVENC-friendly input (reduces conversion overhead vs BGRA)
                    SoftwareBitmap nv12Frame;
                    if (sourceBitmap.BitmapPixelFormat == BitmapPixelFormat.Nv12)
                    {
                        Interlocked.Increment(ref _videoFramesDirectNv12);
                        nv12Frame = sourceBitmap;
                    }
                    else
                    {
                        var convertStartTicks = Logger.VerboseEnabled ? Stopwatch.GetTimestamp() : 0;
                        nv12Frame = SoftwareBitmap.Convert(sourceBitmap, BitmapPixelFormat.Nv12, BitmapAlphaMode.Ignore);
                        if (Logger.VerboseEnabled && convertStartTicks != 0)
                        {
                            var convertMs = (Stopwatch.GetTimestamp() - convertStartTicks) * 1000.0 / Stopwatch.Frequency;
                            if (convertMs >= 5)
                            {
                                Logger.LogVerbose($"SoftwareBitmap.Convert to NV12 took {convertMs:0.00} ms");
                            }
                        }
                        Interlocked.Increment(ref _videoFramesConvertedNv12);
                        sourceBitmap.Dispose(); // Dispose unconverted frame
                    }

                    // Send converted frame to FFmpeg encoder
                    var enqueueStartTicks = Logger.VerboseEnabled ? Stopwatch.GetTimestamp() : 0;
                    encoder.EnqueueVideoFrame(nv12Frame);
                    if (Logger.VerboseEnabled && enqueueStartTicks != 0)
                    {
                        var enqueueMs = (Stopwatch.GetTimestamp() - enqueueStartTicks) * 1000.0 / Stopwatch.Frequency;
                        if (enqueueMs >= 5)
                        {
                            Logger.LogVerbose($"EnqueueVideoFrame took {enqueueMs:0.00} ms");
                        }
                    }

                    // Dispose converted frame (FFmpeg has copied the data)
                    nv12Frame.Dispose();

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

    private void ResetPipelineStats()
    {
        Interlocked.Exchange(ref _videoFramesArrived, 0);
        Interlocked.Exchange(ref _videoFramesQueued, 0);
        Interlocked.Exchange(ref _videoFramesDropped, 0);
        Interlocked.Exchange(ref _videoFramesConverted, 0);
        Interlocked.Exchange(ref _videoFramesEnqueued, 0);
        Interlocked.Exchange(ref _videoFramesDirectNv12, 0);
        Interlocked.Exchange(ref _videoFramesConvertedNv12, 0);
        Interlocked.Exchange(ref _lastPipelineLogMs, 0);
        Interlocked.Exchange(ref _lastFrameArrivalMs, 0);
        Interlocked.Exchange(ref _lastConversionIdleLogMs, 0);
        Interlocked.Exchange(ref _lastEncoderNotReadyLogMs, 0);
        Interlocked.Exchange(ref _loggedFirstFrameArrival, 0);
        Interlocked.Exchange(ref _loggedFirstFrameQueued, 0);
        Interlocked.Exchange(ref _loggedFirstFrameConverted, 0);
        Interlocked.Exchange(ref _loggedFirstFrameEnqueued, 0);
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
        var conversionQueueCount = _conversionQueue?.Count ?? 0;
        var videoQueueCount = encoder?.VideoQueueCount ?? 0;
        var audioQueueCount = encoder?.AudioQueueCount ?? 0;
        var arrived = Interlocked.Read(ref _videoFramesArrived);
        var queued = Interlocked.Read(ref _videoFramesQueued);
        var dropped = Interlocked.Read(ref _videoFramesDropped);
        var converted = Interlocked.Read(ref _videoFramesConverted);
        var enqueued = Interlocked.Read(ref _videoFramesEnqueued);
        var directNv12 = Interlocked.Read(ref _videoFramesDirectNv12);
        var convertedNv12 = Interlocked.Read(ref _videoFramesConvertedNv12);

        Logger.LogVerbose(
            $"Pipeline: t={nowMs}ms arrived={arrived} queued={queued} dropped={dropped} converted={converted} " +
            $"enqueued={enqueued} nv12Direct={directNv12} nv12Converted={convertedNv12} " +
            $"convQ={conversionQueueCount} ffmpegVQ={videoQueueCount} ffmpegAQ={audioQueueCount}");
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
            lock (_audioFileLock)
            {
                _audioFileStream?.Dispose();
                _audioFileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
                _audioBytesWritten = 0;
                WriteWavHeader(_audioFileStream, 0);
            }
            Logger.Log($"Audio capture file created: {path}");
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to create audio capture file: {ex.Message}");
        }
    }

    private void FinalizeAudioCaptureFile()
    {
        lock (_audioFileLock)
        {
            if (_audioFileStream == null)
            {
                return;
            }

            try
            {
                WriteWavHeader(_audioFileStream, _audioBytesWritten);
                _audioFileStream.Flush();
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
    }

    private unsafe void WriteAudioBytesToFile(byte* data, int byteCount)
    {
        if (byteCount <= 0)
        {
            return;
        }

        lock (_audioFileLock)
        {
            if (_audioFileStream == null)
            {
                return;
            }

            try
            {
                var span = new ReadOnlySpan<byte>(data, byteCount);
                _audioFileStream.Write(span);
                _audioBytesWritten += byteCount;
            }
            catch (Exception ex)
            {
                Logger.Log($"Audio file write failed: {ex.Message}");
            }
        }
    }

    private void WriteAudioBytesToFile(byte[] data)
    {
        if (data.Length == 0)
        {
            return;
        }

        lock (_audioFileLock)
        {
            if (_audioFileStream == null)
            {
                return;
            }

            try
            {
                _audioFileStream.Write(data, 0, data.Length);
                _audioBytesWritten += data.Length;
            }
            catch (Exception ex)
            {
                Logger.Log($"Audio file write failed: {ex.Message}");
            }
        }
    }

    private void WriteWavHeader(FileStream stream, long dataBytes)
    {
        if (dataBytes < 0)
        {
            dataBytes = 0;
        }

        if (dataBytes > uint.MaxValue)
        {
            Logger.Log($"WARNING: Audio data exceeds WAV 4GB limit ({dataBytes} bytes). File may be invalid.");
        }

        var blockAlign = (short)(RecordingAudioChannels * (RecordingAudioBitsPerSample / 8));
        var byteRate = RecordingAudioSampleRate * blockAlign;
        var riffSize = 4 + (8 + 16) + (8 + dataBytes);

        stream.Seek(0, SeekOrigin.Begin);
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write((uint)riffSize);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write((uint)16);
        writer.Write((ushort)3); // IEEE float
        writer.Write((ushort)RecordingAudioChannels);
        writer.Write((uint)RecordingAudioSampleRate);
        writer.Write((uint)byteRate);
        writer.Write((ushort)blockAlign);
        writer.Write((ushort)RecordingAudioBitsPerSample);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write((uint)dataBytes);
        writer.Flush();
    }

    private void CleanupPostMuxFiles()
    {
        FinalizeAudioCaptureFile();

        TryDeleteFile(_audioTempPath);
        _audioTempPath = null;

        if (_postMuxAudioEnabled && _recordingFile != null)
        {
            TryDeleteFile(_recordingFile.Path);
        }
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
            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                Logger.Log("Mux failed: could not start ffmpeg process");
                return false;
            }

            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(stderr))
            {
                foreach (var line in stderr.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    Logger.Log($"[FFmpeg Mux] {line}");
                }
            }

            if (process.ExitCode != 0)
            {
                Logger.Log($"Mux failed: ffmpeg exited with code {process.ExitCode}");
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

    private static void TryDeleteFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to delete temp file '{path}': {ex.Message}");
        }
    }

    private async Task CleanupRecordingResourcesOnErrorAsync()
    {
        if (_conversionQueue != null)
        {
            _conversionQueue.CompleteAdding();
        }

        if (_conversionCancellation != null)
        {
            _conversionCancellation.Cancel();
        }

        if (_conversionWorkerTask != null)
        {
            try { await _conversionWorkerTask; }
            catch (Exception ex) { Logger.Log($"Conversion worker error during cleanup: {ex.Message}"); }
            _conversionWorkerTask = null;
        }

        if (_conversionQueue != null)
        {
            foreach (var frame in _conversionQueue)
            {
                frame?.Dispose();
            }
            _conversionQueue.Dispose();
            _conversionQueue = null;
        }

        _conversionCancellation?.Dispose();
        _conversionCancellation = null;

        CleanupRecordingAudioGraph();

        if (_ffmpegEncoder != null)
        {
            try { await _ffmpegEncoder.StopEncodingAsync(); }
            catch (Exception ex) { Logger.Log($"Error stopping FFmpeg after failure: {ex.Message}"); }
            try { _ffmpegEncoder.Dispose(); }
            catch (Exception ex) { Logger.Log($"Error disposing FFmpeg after failure: {ex.Message}"); }
            _ffmpegEncoder = null;
        }

        if (_postMuxAudioEnabled)
        {
            CleanupPostMuxFiles();
        }
    }

    private void RecordingAudioGraph_QuantumStarted(AudioGraph sender, object args)
    {
        // Capture local references to avoid race conditions with StopRecordingAsync
        var encoder = _ffmpegEncoder;
        var outputNode = _audioFrameOutputNode;
        var usePostMux = _postMuxAudioEnabled;

        // Don't check IsEncoding here - we want to buffer audio samples while FFmpeg is starting up
        // The EnqueueAudioSamples method will handle rejecting samples if queue isn't ready
        if (outputNode == null) return;
        if (!usePostMux && encoder == null) return;
        var encoderInstance = encoder!;

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
                                WriteAudioBytesToFile(dataInBytes, byteCount);
                            }
                            else
                            {
                                var audioData = new byte[byteCount];
                                fixed (byte* outputBytes = audioData)
                                {
                                    System.Buffer.MemoryCopy(dataInBytes, outputBytes, byteCount, byteCount);
                                }
                                encoderInstance.EnqueueAudioSamples(audioData);
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
                                WriteAudioBytesToFile(audioData);
                            }
                            else
                            {
                                encoderInstance.EnqueueAudioSamples(audioData);
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
                                WriteAudioBytesToFile(audioData);
                            }
                            else
                            {
                                encoderInstance.EnqueueAudioSamples(audioData);
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
            // Log to file instead of just Debug for visibility
            Logger.Log($"Audio frame error: {ex.Message}");
        }
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
            Logger.Log($"Audio preview frame error: {ex.Message}");
        }
    }

    private async Task StartUncompressedRecordingAsync(CaptureSettings settings)
    {
        if (_mediaCapture == null || _recordingFile == null) return;

        // Find a frame source from the already-initialized MediaCapture
        var videoSourceInfo = _mediaCapture.FrameSources.Values
            .Select(fs => fs.Info)
            .FirstOrDefault(info =>
                info.MediaStreamType == MediaStreamType.VideoRecord &&
                info.SourceKind == MediaFrameSourceKind.Color);

        if (videoSourceInfo == null)
        {
            // Fallback to using compressed recording for uncompressed - use very high bitrate
            var profile = MediaEncodingProfile.CreateAvi(VideoEncodingQuality.HD1080p);
            if (profile.Video != null)
            {
                profile.Video.Width = settings.Width;
                profile.Video.Height = settings.Height;
                profile.Video.FrameRate.Numerator = (uint)settings.FrameRate;
                profile.Video.FrameRate.Denominator = 1;
                profile.Video.Bitrate = 200_000_000; // 200 Mbps for near-lossless
            }

            if (!settings.AudioEnabled)
            {
                profile.Audio = null;
            }

            // Use standard recording API for CFR output
            await _mediaCapture.StartRecordToStorageFileAsync(profile, _recordingFile);
            Logger.Log("Started AVI fallback recording with standard API (CFR mode)");
            return;
        }

        // Initialize AVI writer for true uncompressed
        _aviWriter = new AviWriter(
            await _recordingFile.OpenAsync(FileAccessMode.ReadWrite),
            settings.Width,
            settings.Height,
            (uint)settings.FrameRate);

        var frameSource = _mediaCapture.FrameSources[videoSourceInfo.Id];
        _frameReader = await _mediaCapture.CreateFrameReaderAsync(frameSource);
        _frameReader.FrameArrived += FrameReader_FrameArrived;

        await _frameReader.StartAsync();
    }

    private ulong _frameCount;

    private async void FrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        // Capture local reference to avoid race condition with StopRecordingAsync
        var aviWriter = _aviWriter;

        using var frame = sender.TryAcquireLatestFrame();
        if (frame?.VideoMediaFrame?.SoftwareBitmap == null) return;

        var softwareBitmap = frame.VideoMediaFrame.SoftwareBitmap;

        try
        {
            // Convert to BGRA8 if needed
            if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8)
            {
                softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8);
            }

            if (aviWriter != null)
            {
                await aviWriter.WriteFrameAsync(softwareBitmap);
            }
            _frameCount++;
            FrameCaptured?.Invoke(this, _frameCount);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
        }
    }

    public async Task StopRecordingAsync()
    {
        if (!_isRecording) return;

        try
        {
            Logger.Log("=== Stopping recording ===");
            var shouldPostMux = _postMuxAudioEnabled &&
                                !string.IsNullOrEmpty(_finalOutputPath) &&
                                !string.IsNullOrEmpty(_audioTempPath);
            var tempVideoPath = _recordingFile?.Path;
            var audioTempPath = _audioTempPath;
            var finalOutputPath = _finalOutputPath;

            // Stop recording frame reader (FFmpeg recording)
            if (_recordingFrameReader != null)
            {
                _recordingFrameReader.FrameArrived -= RecordingFrameReader_FrameArrived;
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
                _conversionQueue.CompleteAdding(); // Signal no more frames coming
                Logger.Log("Waiting for conversion queue to drain...");
            }

            if (_conversionCancellation != null)
            {
                _conversionCancellation.Cancel();
            }

            if (_conversionWorkerTask != null)
            {
                await _conversionWorkerTask;
                _conversionWorkerTask = null;
            }

            // Dispose conversion queue and remaining frames
            if (_conversionQueue != null)
            {
                foreach (var frame in _conversionQueue)
                {
                    frame?.Dispose();
                }
                _conversionQueue.Dispose();
                _conversionQueue = null;
            }

            _conversionCancellation?.Dispose();
            _conversionCancellation = null;

            // Stop FFmpeg encoder
            long encoderDropped = 0;
            if (_ffmpegEncoder != null)
            {
                encoderDropped = _ffmpegEncoder.DroppedVideoFrames;
                await _ffmpegEncoder.StopEncodingAsync();
                _ffmpegEncoder.Dispose();
                _ffmpegEncoder = null;
                Logger.Log("FFmpeg encoder stopped");
            }

            // Legacy: Stop AVI frame reader
            if (_frameReader != null)
            {
                _frameReader.FrameArrived -= FrameReader_FrameArrived;
                await _frameReader.StopAsync();
                _frameReader.Dispose();
                _frameReader = null;
            }

            // Legacy: Finalize AVI writer
            if (_aviWriter != null)
            {
                await _aviWriter.FinalizeAsync();
                _aviWriter.Dispose();
                _aviWriter = null;
            }

            if (_recordingStopwatch.IsRunning)
            {
                if (Logger.VerboseEnabled)
                {
                    var durationMs = _recordingStopwatch.ElapsedMilliseconds;
                    Logger.LogVerbose(
                        $"Pipeline summary: durationMs={durationMs} arrived={Interlocked.Read(ref _videoFramesArrived)} " +
                        $"queued={Interlocked.Read(ref _videoFramesQueued)} dropped={Interlocked.Read(ref _videoFramesDropped)} " +
                        $"converted={Interlocked.Read(ref _videoFramesConverted)} enqueued={Interlocked.Read(ref _videoFramesEnqueued)} " +
                        $"nv12Direct={Interlocked.Read(ref _videoFramesDirectNv12)} nv12Converted={Interlocked.Read(ref _videoFramesConvertedNv12)} " +
                        $"ffmpegDropped={encoderDropped}");
                }
                _recordingStopwatch.Stop();
            }

            if (_postMuxAudioEnabled)
            {
                FinalizeAudioCaptureFile();

                if (shouldPostMux && !string.IsNullOrWhiteSpace(tempVideoPath) &&
                    !string.IsNullOrWhiteSpace(audioTempPath) && !string.IsNullOrWhiteSpace(finalOutputPath))
                {
                    var muxed = await MuxAudioIntoVideoAsync(tempVideoPath, audioTempPath, finalOutputPath);
                    if (muxed)
                    {
                        TryDeleteFile(tempVideoPath);
                        TryDeleteFile(audioTempPath);
                    }
                }

                _postMuxAudioEnabled = false;
                _audioTempPath = null;
                _finalOutputPath = null;
            }

            _isRecording = false;
            _frameCount = 0;

            Logger.Log($"✓ Recording stopped. Audio preview status: {(_isAudioPreviewActive ? "still active" : "inactive")}");
            StatusChanged?.Invoke(this, "Stopped");
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
            throw;
        }
    }

    public async Task StartAudioPreviewAsync()
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
                await StopAudioPreviewAsync();
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
                await StopAudioPreviewAsync();
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
            Logger.Log("✓ Audio preview started successfully");
            StatusChanged?.Invoke(this, "Audio preview active");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            ErrorOccurred?.Invoke(this, ex);
            await StopAudioPreviewAsync();
        }
    }

    public async Task StopAudioPreviewAsync()
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
            Logger.Log("Audio preview stopped");
            StatusChanged?.Invoke(this, "Audio preview stopped");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            ErrorOccurred?.Invoke(this, ex);
        }
    }

    public async Task CleanupAsync()
    {
        if (_isRecording)
        {
            await StopRecordingAsync();
        }

        if (_isAudioPreviewActive)
        {
            await StopAudioPreviewAsync();
        }

        if (_mediaCapture != null)
        {
            _mediaCapture.Failed -= MediaCapture_Failed;
            _mediaCapture.Dispose();
            _mediaCapture = null;
        }

        _isInitialized = false;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        // Don't block with GetAwaiter().GetResult() - it can deadlock on UI thread
        // Instead, do synchronous cleanup for critical resources
        try
        {
            _isRecording = false;
            _isInitialized = false;

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
                _ffmpegEncoder?.Dispose();
            }
            catch (Exception ex) { Logger.Log($"Error disposing FFmpeg encoder: {ex.Message}"); }

            // Dispose frame readers
            try
            {
                _frameReader?.Dispose();
                _recordingFrameReader?.Dispose();
            }
            catch (Exception ex) { Logger.Log($"Error disposing frame readers: {ex.Message}"); }

            // Dispose AVI writer
            try
            {
                _aviWriter?.Dispose();
            }
            catch (Exception ex) { Logger.Log($"Error disposing AVI writer: {ex.Message}"); }

            // Dispose MediaCapture last
            try
            {
                _mediaCapture?.Dispose();
            }
            catch (Exception ex) { Logger.Log($"Error disposing MediaCapture: {ex.Message}"); }

            Logger.Log("CaptureService disposed");
        }
        catch (Exception ex)
        {
            Logger.Log($"Error during CaptureService disposal: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        await CleanupAsync();
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
