using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using Windows.Storage;

namespace ElgatoCapture.Services;

public partial class CaptureService : IDisposable, IAsyncDisposable
{
    private readonly SemaphoreSlim _sessionTransitionLock = new(1, 1);
    private readonly ISourceSignalTelemetryProvider _sourceTelemetryProvider;
    private readonly IProcessSupervisor _processSupervisor;
    private readonly RecordingArtifactManager _artifactManager = new();

    private int _isDisposed;
    private bool _isInitialized;
    private bool _isRecording;
    private bool _isVideoPreviewActive;
    private bool _isAudioPreviewActive;
    private CaptureSessionState _sessionState = CaptureSessionState.Uninitialized;
    private CaptureDevice? _currentDevice;
    private CaptureSettings? _currentSettings;
    private CaptureSettings? _activeRecordingSettings;
    private SourceSignalTelemetrySnapshot _latestSourceTelemetry = SourceSignalTelemetrySnapshot.CreateUnavailable("telemetry-not-started");
    private LibAvRecordingSink? _libavSink;
    private IRecordingSink? _recordingSink;
    private FlashbackBufferManager? _flashbackBufferManager;
    private FlashbackEncoderSink? _flashbackSink;
    private FlashbackExporter? _flashbackExporter;
    private FlashbackPlaybackController? _flashbackPlaybackController;
    private volatile bool _flashbackEnabled = true;
    private bool _hasAv1Nvenc;
    private bool _pendingFlashbackSettingsChange;
    private long _flashbackRecordingStartBytes;
    private WasapiAudioCapture? _wasapiAudioCapture;
    private WasapiAudioCapture? _microphoneCapture;
    private string? _micMonitorDeviceId;
    private string? _micMonitorDeviceName;
    private bool _micMonitorEnabled;
    private WasapiAudioPlayback? _wasapiAudioPlayback;
    private float _previewVolume = 1.0f;
    private bool _isMonitoringMuted;
    private bool _wasapiAudioCaptureFaulted;
    private string? _wasapiAudioCaptureFaultMessage;
    private int _fatalCleanupInProgress;
    private long _sessionGeneration;
    private UnifiedVideoCapture? _unifiedVideoCapture;
    private IPreviewFrameSink? _previewFrameSink;
    private RecordingContext? _recordingContext;
    private readonly Stopwatch _recordingStopwatch = new();
    private string? _lastOutputPath;
    private string _lastFinalizeStatus = "None";
    private DateTimeOffset? _lastFinalizeUtc;
    private IReadOnlyList<string> _lastPreservedArtifacts = Array.Empty<string>();
    private bool _lastUsePostMuxAudio;
    private FinalizeResult? _lastExportResult;
    private string? _audioDeviceId;
    private string? _audioDeviceName;
    private bool _mfConvertersDisabled;
    private uint? _actualWidth;
    private uint? _actualHeight;
    private double? _actualFrameRate;
    private string? _actualFrameRateArg;
    private uint? _actualFrameRateNumerator;
    private uint? _actualFrameRateDenominator;
    private string? _actualPixelFormat;
    private string _activeVideoInputPixelFormat = "nv12";
    private long _videoFramesDropped;
    private string? _firstObservedFramePixelFormat;
    private string? _latestObservedFramePixelFormat;
    private string? _latestObservedSurfaceFormat;
    private long _observedP010FrameCount;
    private long _observedNv12FrameCount;
    private long _observedOtherFrameCount;
    private long _lastMfSourceReaderFramesDelivered;
    private long _lastMfSourceReaderFramesDropped;
    private string? _lastMfSourceReaderNegotiatedFormat;
    private UnifiedVideoCapture.MjpegPipelineTimingMetrics _lastMjpegPipelineTimingMetrics;
    private ParallelMjpegDecodePipeline.PipelineTimingMetrics? _lastFullMjpegPipelineTimingMetrics;
    private CancellationTokenSource? _telemetryPollCts;
    private Task? _telemetryPollTask;
    private const int TelemetryPollIntervalMs = 500;

    // AV sync drift diagnostics
    private double _avSyncBaselineDriftMs = double.NaN;
    private double _avSyncPrevDriftMs;
    private long _avSyncPrevDriftTick;
    private double _avSyncDriftRateMsPerSec;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<Exception>? ErrorOccurred;
    public event EventHandler<ulong>? FrameCaptured;
    public event EventHandler<AudioLevelEventArgs>? AudioLevelUpdated;
    public event EventHandler<AudioLevelEventArgs>? MicrophoneAudioLevelUpdated;
    public event EventHandler<SourceSignalTelemetrySnapshot>? SourceTelemetryUpdated;

    public bool IsRecording => _isRecording;
    public bool IsInitialized => _isInitialized;
    public bool IsVideoPreviewActive => _isVideoPreviewActive;
    public bool IsAudioPreviewActive => _isAudioPreviewActive;
    public CaptureSessionState SessionState => _sessionState;

    public bool IsFlashbackActive => _flashbackSink != null;
    public TimeSpan FlashbackBufferedDuration => _flashbackBufferManager?.BufferedDuration ?? TimeSpan.Zero;
    public long FlashbackDiskBytes => _flashbackBufferManager?.TotalDiskBytes ?? 0;
    public int FlashbackSegmentCount => _flashbackBufferManager?.SegmentCount ?? 0;
    internal FlashbackPlaybackController? FlashbackPlaybackController => _flashbackPlaybackController;
    internal FlashbackBufferManager? FlashbackBufferManager => _flashbackBufferManager;
    public long FlashbackOutputBytes => _flashbackSink?.OutputBytes ?? 0;
    public long FlashbackTotalBytesWritten => _flashbackSink?.TotalBytesWritten ?? 0;
    public string? EncoderCodecName => _flashbackSink?.CodecName;
    public uint EncoderTargetBitRate => _flashbackSink?.TargetBitRate ?? 0;
    public int EncoderWidth => _flashbackSink?.EncoderWidth ?? 0;
    public int EncoderHeight => _flashbackSink?.EncoderHeight ?? 0;
    public double EncoderFrameRate => _flashbackSink?.EncoderFrameRate ?? 0;
    public FinalizeResult? LastExportResult => _lastExportResult;

    internal IReadOnlyList<FlashbackSegmentInfo> GetFlashbackSegments()
    {
        return _flashbackBufferManager?.GetSegmentInfoList()
            ?? Array.Empty<FlashbackSegmentInfo>();
    }

    public void SetFlashbackEnabled(bool enabled)
    {
        if (_isRecording && IsFlashbackRecordingBackendActive() && !enabled)
        {
            Logger.Log("FLASHBACK_DISABLE_BLOCKED reason=recording_active");
            return;
        }
        _flashbackEnabled = enabled;
    }

    /// <summary>
    /// Updates flashback-specific fields in the active capture settings without
    /// requiring a full session restart. Call before <see cref="RestartFlashbackAsync"/>
    /// so the rebuild uses the latest values.
    /// </summary>
    public void UpdateFlashbackSettings(int bufferMinutes, bool gpuDecode)
    {
        if (_currentSettings != null)
        {
            _currentSettings.FlashbackBufferMinutes = bufferMinutes;
            _currentSettings.FlashbackGpuDecode = gpuDecode;
        }
        if (_isRecording && IsFlashbackRecordingBackendActive())
            _pendingFlashbackSettingsChange = true;
    }

    /// <summary>
    /// Updates encoding-related fields in the active capture settings so that
    /// <see cref="RestartFlashbackAsync"/> picks up the latest bitrate/quality/preset.
    /// </summary>
    public void UpdateEncodingSettings(CaptureSettings source)
    {
        if (_currentSettings == null) return;
        _currentSettings.CustomBitrateMbps = source.CustomBitrateMbps;
        _currentSettings.AudioEnabled = source.AudioEnabled;
        _currentSettings.MicrophoneEnabled = source.MicrophoneEnabled;
        // If a flashback-backed recording is active, the restart will be deferred —
        // flag it so the stop-recording path knows to do a full rebuild.
        if (_isRecording && IsFlashbackRecordingBackendActive())
            _pendingFlashbackSettingsChange = true;
    }

    /// <summary>
    /// Tears down the running flashback encoder and buffer, then rebuilds
    /// with current settings. Purges all existing segments because encoding
    /// parameters (bitrate, codec, etc.) may have changed.
    /// </summary>
    public Task RestartFlashbackAsync(CancellationToken cancellationToken = default)
        => RunTransitionAsync(_sessionState, async transitionToken =>
        {
            if (_isRecording && IsFlashbackRecordingBackendActive())
            {
                Logger.Log("FLASHBACK_RESTART_BLOCKED reason=recording_active");
                return;
            }

            Logger.Log("FLASHBACK_RESTART_BEGIN");
            await DisposeFlashbackPreviewBackendAsync(transitionToken, purgeSegments: true).ConfigureAwait(false);

            var unifiedVideoCapture = _unifiedVideoCapture;
            var settings = _currentSettings;
            if (!_flashbackEnabled || unifiedVideoCapture == null || settings == null)
            {
                Logger.Log($"FLASHBACK_RESTART_TEARDOWN_ONLY enabled={_flashbackEnabled} capture={unifiedVideoCapture != null} settings={settings != null}");
                return;
            }

            await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, transitionToken).ConfigureAwait(false);
            Logger.Log("FLASHBACK_RESTART_DONE");
        }, cancellationToken);

    /// <summary>
    /// Updates the recording format and cycles the flashback encoder so the buffer
    /// uses the new codec.  No-op if not previewing or if a recording is active.
    /// </summary>
    public Task UpdateRecordingFormatAsync(RecordingFormat format, CancellationToken cancellationToken = default)
        => RunTransitionAsync(_sessionState, async transitionToken =>
        {
            if (_currentSettings == null || format == _currentSettings.Format)
                return;

            if (_isRecording)
            {
                Logger.Log($"FLASHBACK_FORMAT_CHANGE_BLOCKED reason=recording_active format={format}");
                _currentSettings.Format = format;
                if (IsFlashbackRecordingBackendActive())
                    _pendingFlashbackSettingsChange = true;
                return;
            }

            Logger.Log($"FLASHBACK_FORMAT_CHANGE_BEGIN old={_currentSettings.Format} new={format}");
            _currentSettings.Format = format;

            if (_flashbackSink != null)
            {
                try
                {
                    await CycleFlashbackBufferAsync(transitionToken, purgeSegments: true).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Log($"FLASHBACK_FORMAT_CHANGE_CYCLE_FAIL error='{ex.Message}'");
                }
            }

            Logger.Log($"FLASHBACK_FORMAT_CHANGE_DONE format={format}");
        }, cancellationToken);

    public void SetPreviewVolume(float volume)
    {
        _previewVolume = Math.Clamp(volume, 0f, 1f);
        if (!_isMonitoringMuted)
        {
            var playback = _wasapiAudioPlayback;
            playback?.SetVolume(_previewVolume);
        }
    }

    public void SetMonitoringMuted(bool muted)
    {
        _isMonitoringMuted = muted;
        var playback = _wasapiAudioPlayback;
        playback?.SetVolume(muted ? 0f : _previewVolume);
    }

    internal async Task<FinalizeResult> ExportFlashbackRangeAsync(
        TimeSpan? inPoint, TimeSpan? outPoint, string outputPath,
        IProgress<ExportProgress>? progress, CancellationToken ct)
    {
        // M5: Acquire session lock so a concurrent recording stop can't cycle the buffer
        await _sessionTransitionLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var bufferManager = _flashbackBufferManager;
            if (bufferManager == null)
                return FinalizeResult.Failure(outputPath, "Flashback buffer not active");

            var validStart = bufferManager.ValidStartPts;
            var fileInPoint = (inPoint ?? TimeSpan.Zero) + validStart;
            var fileOutPoint = outPoint.HasValue ? outPoint.Value + validStart : TimeSpan.MaxValue;
            return await ExportFlashbackCoreAsync(fileInPoint, fileOutPoint, outputPath, progress, ct).ConfigureAwait(false);
        }
        finally
        {
            _sessionTransitionLock.Release();
        }
    }

    internal async Task<FinalizeResult> ExportFlashbackLastNSecondsAsync(
        double seconds, string outputPath,
        IProgress<ExportProgress>? progress, CancellationToken ct)
    {
        // M5: Acquire session lock so a concurrent recording stop can't cycle the buffer
        await _sessionTransitionLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var bufferManager = _flashbackBufferManager;
            if (bufferManager == null)
                return FinalizeResult.Failure(outputPath, "Flashback buffer not active");

            var bufferedDuration = bufferManager.BufferedDuration;
            var validStart = bufferManager.ValidStartPts;
            var rangeStart = bufferedDuration.TotalSeconds > seconds
                ? TimeSpan.FromSeconds(bufferedDuration.TotalSeconds - seconds)
                : TimeSpan.Zero;
            var fileInPoint = rangeStart + validStart;
            return await ExportFlashbackCoreAsync(fileInPoint, TimeSpan.MaxValue, outputPath, progress, ct).ConfigureAwait(false);
        }
        finally
        {
            _sessionTransitionLock.Release();
        }
    }

    private async Task<FinalizeResult> ExportFlashbackCoreAsync(
        TimeSpan inPoint, TimeSpan outPoint, string outputPath,
        IProgress<ExportProgress>? progress, CancellationToken ct)
    {
        var flashbackSink = _flashbackSink;
        var bufferManager = _flashbackBufferManager;
        var exporter = _flashbackExporter ??= new FlashbackExporter();

        // Pause eviction so segments aren't deleted while the exporter reads them
        bufferManager?.PauseEviction();
        try
        {
            FinalizeResult result;
            IReadOnlyList<string>? segmentPaths = null;
            string? tsPath = null;

            if (flashbackSink != null)
            {
                segmentPaths = flashbackSink.ForceRotateForExport(inPoint, outPoint);
                if (segmentPaths.Count == 0)
                    segmentPaths = null;
            }

            // Fallback: single-file export if no segments available
            if (segmentPaths == null)
            {
                tsPath = bufferManager?.ActiveFilePath;
                if (string.IsNullOrWhiteSpace(tsPath))
                {
                    result = FinalizeResult.Failure(outputPath, "Flashback buffer has no active file");
                    _lastExportResult = result;
                    return result;
                }
            }

            var request = new FlashbackExportRequest
            {
                SegmentPaths = segmentPaths,
                InputTsPath = tsPath,
                InPoint = inPoint,
                OutPoint = outPoint,
                OutputPath = outputPath,
            };
            result = await exporter.ExportAsync(request, progress, ct).ConfigureAwait(false);
            _lastExportResult = result;
            return result;
        }
        finally
        {
            bufferManager?.ResumeEviction();
        }
    }

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
        return new NativeXuAtCommandProvider();
    }

    internal void SetPreviewFrameSink(IPreviewFrameSink? sink)
    {
        _previewFrameSink = sink;
        _unifiedVideoCapture?.SetPreviewSink(sink);
        TryApplySharedPreviewDevice(_unifiedVideoCapture, sink);

        // Late-initialize playback controller if it was created before the renderer
        var controller = _flashbackPlaybackController;
        if (controller != null && !controller.IsInitialized && sink != null && _unifiedVideoCapture != null)
        {
            controller.Initialize(sink, _unifiedVideoCapture, _wasapiAudioPlayback, _wasapiAudioCapture);
            Logger.Log("FLASHBACK_PLAYBACK_LATE_INIT via SetPreviewFrameSink");
        }
    }

    private void CacheMjpegTimingMetrics(UnifiedVideoCapture? unifiedVideoCapture)
    {
        if (unifiedVideoCapture == null)
        {
            return;
        }

        _lastMjpegPipelineTimingMetrics = unifiedVideoCapture.GetMjpegPipelineTimingMetrics();
        _lastFullMjpegPipelineTimingMetrics = unifiedVideoCapture.GetFullMjpegPipelineTimingMetrics();
    }

    private void ResetCachedMjpegTimingMetrics()
    {
        _lastMjpegPipelineTimingMetrics = default;
        _lastFullMjpegPipelineTimingMetrics = null;
    }

    internal ParallelMjpegDecodePipeline.PipelineTimingMetrics? GetMjpegPipelineTimingDetails()
    {
        return _unifiedVideoCapture?.GetFullMjpegPipelineTimingMetrics() ?? _lastFullMjpegPipelineTimingMetrics;
    }

    private void AttachUnifiedVideoCapture(UnifiedVideoCapture unifiedVideoCapture)
    {
        unifiedVideoCapture.FatalErrorOccurred += OnUnifiedVideoCaptureFatalError;
        unifiedVideoCapture.SetPixelFormatDetectedCallback(fmt => RecordObservedPixelFormat(fmt));
    }

    private void DetachUnifiedVideoCapture(UnifiedVideoCapture? unifiedVideoCapture)
    {
        if (unifiedVideoCapture == null)
        {
            return;
        }

        unifiedVideoCapture.FatalErrorOccurred -= OnUnifiedVideoCaptureFatalError;
        unifiedVideoCapture.SetPixelFormatDetectedCallback(null);
    }

    private bool IsFlashbackRecordingBackendActive()
        => _flashbackSink != null &&
           ReferenceEquals(_recordingSink, _flashbackSink);

    private void AttachFlashbackAudioIfSupported(WasapiAudioCapture? capture, string reason)
    {
        var flashbackSink = _flashbackSink;
        if (capture == null || flashbackSink == null)
            return;

        if (!flashbackSink.AudioEnabled)
        {
            Logger.Log($"FLASHBACK_AUDIO_ATTACH_SKIPPED reason='{reason}' sink_audio_enabled=false");
            return;
        }

        capture.AttachFlashbackSink(flashbackSink);
        Logger.Log($"FLASHBACK_AUDIO_ATTACH_OK reason='{reason}'");
    }

    private FlashbackSessionContext CreateFlashbackSessionContext(
        UnifiedVideoCapture unifiedVideoCapture,
        CaptureSettings settings)
    {
        var isP010 = unifiedVideoCapture.IsP010;
        // Flashback requires real-time hardware encoding. For AV1, fall back to
        // HEVC NVENC if av1_nvenc isn't available (the UI enables AV1 when any
        // AV1 encoder is present, including software-only like libsvtav1).
        var codecName = settings.Format switch
        {
            RecordingFormat.HevcMp4 => "hevc_nvenc",
            RecordingFormat.Av1Mp4 => _hasAv1Nvenc ? "av1_nvenc" : "hevc_nvenc",
            _ => isP010 ? "hevc_nvenc" : "h264_nvenc" // H264 can't encode P010
        };
        var audioDeviceId = settings.AudioEnabled
            ? (settings.UseCustomAudioInput ? settings.AudioDeviceId : (_audioDeviceId ?? _currentDevice?.AudioDeviceId))
            : null;
        var frameRate = unifiedVideoCapture.Fps > 0 ? unifiedVideoCapture.Fps : settings.FrameRate;
        var d3dManager = unifiedVideoCapture.D3DManager;
        // When the software MJPEG decode pipeline is active, frames arrive as CPU NV12
        // buffers (not D3D11 textures). Passing D3D device pointers would cause the
        // encoder to initialize hw_frames, but SendVideoFrame would then feed a software
        // frame into an nvenc context expecting D3D11 textures — crashing in the driver.
        var useGpuEncoding = !unifiedVideoCapture.IsSoftwareMjpegPipelineActive;

        // NTSC rational frame rate correction — DISABLED.
        // The HDMI source outputs 119.88fps but the capture card delivers at ~120fps
        // over USB (its own clock). Using NTSC time_base (1001/120000) makes video PTS
        // run ~1ms/sec ahead of audio, causing progressive A/V drift. The driver rate
        // matches the actual frame delivery rate, so use it as-is for correct sync.
        //
        // The original comment claimed integer rates cause "NVENC ticks_per_frame=2"
        // drift — that was a misdiagnosis. The actual issue was probesize starvation
        // in FlashbackDecoder (fixed: probesize increased from 32KB to 256KB).
        //
        // TODO: Revisit when we can measure the true source rate vs delivery rate and
        // handle the discrepancy properly (e.g., detect duplicate frames from the
        // capture card, or use drift correction in the encoder).
        int? fpsNum = null;
        int? fpsDen = null;
        // if (_actualFrameRateNumerator.HasValue && _actualFrameRateDenominator is > 1)
        // {
        //     fpsNum = (int)_actualFrameRateNumerator.Value;
        //     fpsDen = (int)_actualFrameRateDenominator.Value;
        // }
        // else
        // {
        //     var telemetry = _latestSourceTelemetry;
        //     if (telemetry.HasFrameRate && telemetry.FrameRateExact.HasValue)
        //     {
        //         var bucket = (int)Math.Round(frameRate, MidpointRounding.AwayFromZero);
        //         if (bucket > 0)
        //         {
        //             var expectedNtsc = bucket * 1000.0 / 1001.0;
        //             if (Math.Abs(telemetry.FrameRateExact.Value - expectedNtsc) <= 0.15)
        //             {
        //                 fpsNum = bucket * 1000;
        //                 fpsDen = 1001;
        //                 frameRate = (double)fpsNum.Value / fpsDen.Value;
        //             }
        //         }
        //     }
        // }

        return new FlashbackSessionContext
        {
            Width = Math.Max(1, unifiedVideoCapture.Width),
            Height = Math.Max(1, unifiedVideoCapture.Height),
            FrameRate = frameRate,
            FrameRateNumerator = fpsNum,
            FrameRateDenominator = fpsDen,
            CodecName = codecName,
            IsP010 = isP010,
            BitRate = settings.GetTargetBitrate(),
            HdrEnabled = isP010,
            IsFullRangeInput = unifiedVideoCapture.IsHighFrameRateMjpegMode,
            HdrMasterDisplayMetadata = settings.HdrMasterDisplayMetadata,
            HdrMaxCll = settings.HdrMaxCll,
            HdrMaxFall = settings.HdrMaxFall,
            D3D11DevicePtr = useGpuEncoding ? (d3dManager?.Device?.NativePointer ?? IntPtr.Zero) : IntPtr.Zero,
            D3D11DeviceContextPtr = useGpuEncoding ? (d3dManager?.ImmediateContext?.NativePointer ?? IntPtr.Zero) : IntPtr.Zero,
            AudioEnabled = settings.AudioEnabled && !string.IsNullOrWhiteSpace(audioDeviceId),
            MicrophoneEnabled = settings.MicrophoneEnabled && !string.IsNullOrWhiteSpace(settings.MicrophoneDeviceId)
        };
    }

    private async Task EnsureFlashbackPreviewBackendAsync(
        UnifiedVideoCapture unifiedVideoCapture,
        CaptureSettings settings,
        CancellationToken cancellationToken)
    {
        if (!_flashbackEnabled || _flashbackSink != null)
            return;

        Logger.Log("FLASHBACK_PREVIEW_INIT_BEGIN");

        // Cache AV1 NVENC availability on first flashback init (async-safe here)
        if (!_hasAv1Nvenc)
        {
            try
            {
                var support = await FfmpegRuntimeLocator.GetEncoderSupportAsync().ConfigureAwait(false);
                _hasAv1Nvenc = support.HasAv1Nvenc;
            }
            catch { /* Assume unavailable — will fall back to HEVC */ }
        }

        var bufferMinutes = settings.FlashbackBufferMinutes > 0 ? settings.FlashbackBufferMinutes : 5;
        var bufferDuration = TimeSpan.FromMinutes(bufferMinutes);
        // Segment duration must be shorter than buffer duration so completed segments
        // can be evicted. Use half the buffer, clamped to [0.5, 5] minutes.
        // - Lower bound 0.5min: for 1-min buffer, ensures at least 1 completed segment
        //   exists before the buffer fills (2 segments × 0.5min = 1min).
        // - Upper bound 5min: for large buffers (15-30min), keeps eviction granular
        //   so users don't lose 15min of history in one eviction step.
        var segmentDuration = TimeSpan.FromMinutes(Math.Clamp(bufferMinutes / 2.0, 0.5, 5.0));
        var bufferManager = new FlashbackBufferManager(new FlashbackBufferOptions
        {
            BufferDuration = bufferDuration,
            SegmentDuration = segmentDuration
        });
        bufferManager.Initialize(Guid.NewGuid().ToString("N"));
        var flashbackSink = new FlashbackEncoderSink(bufferManager);
        var flashbackExporter = new FlashbackExporter();

        try
        {
            // Wait until both video and audio are confirmed flowing before starting
            // the encoder. This eliminates the startup transient where audio PTS races
            // ahead of video PTS (~840ms) because WASAPI starts before the source reader.
            var deadline = Environment.TickCount64 + 5000;
            while (Environment.TickCount64 < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var videoReady = unifiedVideoCapture.VideoFramesArrived > 0;
                var audioReady = _wasapiAudioCapture == null || _wasapiAudioCapture.CaptureCallbackCount > 0;
                if (videoReady && audioReady)
                    break;
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
            Logger.Log(
                $"FLASHBACK_PREVIEW_READINESS video_frames={unifiedVideoCapture.VideoFramesArrived} " +
                $"audio_callbacks={_wasapiAudioCapture?.CaptureCallbackCount ?? -1}");

            await flashbackSink.StartAsync(
                CreateFlashbackSessionContext(unifiedVideoCapture, settings),
                cancellationToken).ConfigureAwait(false);
            flashbackSink.FrameEncoded += OnFlashbackFrameEncoded;
            unifiedVideoCapture.SetFlashbackSink(flashbackSink);
            // Set _flashbackSink BEFORE AttachFlashbackAudioIfSupported — it reads the field
            _flashbackBufferManager = bufferManager;
            _flashbackSink = flashbackSink;
            _flashbackExporter = flashbackExporter;
            AttachFlashbackAudioIfSupported(_wasapiAudioCapture, "preview_backend_start");
            if (_microphoneCapture != null && flashbackSink.MicrophoneEnabled)
            {
                _microphoneCapture.SetAudioWriter(samples => flashbackSink.WriteMicrophoneAudioAsync(samples));
                Logger.Log("FLASHBACK_MIC_ATTACH_OK reason='preview_backend_start'");
            }

            // Create playback controller for timeline scrubbing/playback
            var playbackController = new FlashbackPlaybackController(bufferManager);
            playbackController.GpuDecodeEnabled = settings.FlashbackGpuDecode;
            if (_previewFrameSink != null && unifiedVideoCapture != null)
            {
                playbackController.Initialize(_previewFrameSink, unifiedVideoCapture, _wasapiAudioPlayback, _wasapiAudioCapture);
            }
            _flashbackPlaybackController = playbackController;

            Logger.Log($"FLASHBACK_PREVIEW_INIT_OK session='{bufferManager.SessionId}' controller_initialized={playbackController.IsInitialized}");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PREVIEW_INIT_FAIL error='{ex.Message}'");
            unifiedVideoCapture.SetFlashbackSink(null);
            _wasapiAudioCapture?.DetachFlashbackSink();
            try { flashbackSink.Dispose(); }
            catch (Exception disposeEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_SINK_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }

            try { flashbackExporter.Dispose(); }
            catch (Exception disposeEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_EXPORTER_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }

            try { bufferManager.Dispose(); }
            catch (Exception disposeEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_BUFFER_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }

            _flashbackSink = null;
            _flashbackBufferManager = null;
            _flashbackExporter = null;
            _flashbackPlaybackController = null;

            throw;
        }
    }

    private async Task DisposeFlashbackPreviewBackendAsync(
        CancellationToken cancellationToken,
        bool purgeSegments = true)
    {
        var flashbackSink = _flashbackSink;
        var flashbackBufferManager = _flashbackBufferManager;
        var flashbackExporter = _flashbackExporter;
        var flashbackPlaybackController = _flashbackPlaybackController;

        // Do NOT null the fields yet — the encoding loop may still be running
        // and code that checks _flashbackSink (e.g. IsFlashbackActive) must see
        // a consistent state until the sink is fully drained and stopped.

        Logger.Log($"FLASHBACK_PREVIEW_DISPOSE_BEGIN purge={purgeSegments} has_sink={flashbackSink != null} has_buffer={flashbackBufferManager != null} has_controller={flashbackPlaybackController != null}");

        if (flashbackPlaybackController != null)
        {
            try
            {
                flashbackPlaybackController.GoLive();
                flashbackPlaybackController.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_PLAYBACK_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
            }
        }

        // Detach feeds first — stops new frames from entering the sink
        _microphoneCapture?.SetAudioWriter(null);
        _wasapiAudioCapture?.DetachFlashbackSink();
        _unifiedVideoCapture?.SetFlashbackSink(null);

        if (flashbackSink != null)
        {
            flashbackSink.FrameEncoded -= OnFlashbackFrameEncoded;
            try
            {
                // StopAsync waits for the encoding loop to fully drain and exit
                await flashbackSink.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_PREVIEW_STOP_WARN type={ex.GetType().Name} msg={ex.Message}");
            }

            try
            {
                flashbackSink.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_PREVIEW_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
            }
        }

        // Now that the sink is fully stopped and disposed, clear the fields.
        // Any concurrent reader of _flashbackSink sees either the old (valid)
        // value or null — never a half-disposed object.
        _flashbackSink = null;
        _flashbackBufferManager = null;
        _flashbackExporter = null;
        _flashbackPlaybackController = null;

        if (flashbackBufferManager != null)
        {
            if (purgeSegments)
            {
                try { flashbackBufferManager.PurgeAllSegments(); }
                catch (Exception ex) { Logger.Log($"FLASHBACK_BUFFER_PURGE_WARN type={ex.GetType().Name} msg={ex.Message}"); }
            }

            try { flashbackBufferManager.Dispose(); }
            catch (Exception ex) { Logger.Log($"FLASHBACK_BUFFER_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}"); }
        }

        if (flashbackExporter != null)
        {
            try { flashbackExporter.Dispose(); }
            catch (Exception ex) { Logger.Log($"FLASHBACK_EXPORTER_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}"); }
        }

        Logger.Log("FLASHBACK_PREVIEW_DISPOSE_END");
    }

    /// <summary>
    /// Cycles the flashback encoder sink after recording stops.
    /// Preserves the buffer manager and its segments so DVR rewind history
    /// survives across recordings. Only the encoder sink is torn down and
    /// replaced; the buffer manager continues accumulating segments.
    /// Falls back to full teardown+rebuild if sink-only cycle fails.
    /// </summary>
    private async Task CycleFlashbackBufferAsync(CancellationToken cancellationToken, bool purgeSegments = false)
    {
        var unifiedVideoCapture = _unifiedVideoCapture;
        var bufferManager = _flashbackBufferManager;
        var oldSink = _flashbackSink;

        // If prerequisites are missing, fall back to full teardown
        if (!_flashbackEnabled || unifiedVideoCapture == null || _currentSettings == null || bufferManager == null || oldSink == null)
        {
            Logger.Log("FLASHBACK_BUFFER_CYCLE_BEGIN mode=full_teardown");
            await DisposeFlashbackPreviewBackendAsync(cancellationToken, purgeSegments: true).ConfigureAwait(false);
            if (_flashbackEnabled && unifiedVideoCapture != null && _currentSettings != null)
            {
                await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, _currentSettings, cancellationToken).ConfigureAwait(false);
                Logger.Log("FLASHBACK_BUFFER_CYCLE_DONE new_session=true");
            }
            else
            {
                Logger.Log("FLASHBACK_BUFFER_CYCLE_DONE new_session=false (flashback disabled or no capture)");
            }
            return;
        }

        Logger.Log($"FLASHBACK_BUFFER_CYCLE_BEGIN mode=sink_only segments={bufferManager.SegmentCount} buffered={bufferManager.BufferedDuration.TotalSeconds:F1}s");

        // Detach audio/video feeds from the old sink
        _microphoneCapture?.SetAudioWriter(null);
        _wasapiAudioCapture?.DetachFlashbackSink();
        unifiedVideoCapture.SetFlashbackSink(null);
        oldSink.FrameEncoded -= OnFlashbackFrameEncoded;

        // Stop and dispose the old sink (leaves buffer manager and segments intact)
        try
        {
            await oldSink.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_CYCLE_STOP_WARN type={ex.GetType().Name} msg={ex.Message}");
        }

        try
        {
            oldSink.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_CYCLE_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
        }

        // When the codec/format changed, purge stale segments (incompatible with
        // new encoder) and reset PTS so the new encoder starts fresh from 0.
        // After stop-recording, keep everything — segments, PTS range, and
        // buffer state — so the user can immediately scrub/export DVR history.
        if (purgeSegments)
        {
            bufferManager.ResetLatestPts();
            bufferManager.PurgeCompletedSegments();

            // If some segments couldn't be deleted (e.g., playback has files locked),
            // fall back to full teardown to avoid mixed-codec segments in the buffer.
            if (bufferManager.SegmentCount > 0)
            {
                Logger.Log($"FLASHBACK_CYCLE_PURGE_INCOMPLETE remaining={bufferManager.SegmentCount} — falling back to full teardown");
                await DisposeFlashbackPreviewBackendAsync(cancellationToken, purgeSegments: true).ConfigureAwait(false);
                await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, _currentSettings, cancellationToken).ConfigureAwait(false);
                Logger.Log("FLASHBACK_BUFFER_CYCLE_DONE mode=purge_fallback_rebuild");
                return;
            }
        }

        // Ensure the new sink gets a fresh segment file (not the old sink's active path).
        bufferManager.FinalizeActiveSegmentForCycle();

        // Create and start a new encoder sink on the same buffer manager
        var newSink = new FlashbackEncoderSink(bufferManager);
        try
        {
            // When preserving DVR history (no purge), continue PTS from where
            // the old sink left off so new segments don't overlap existing ones.
            var ptsOffset = purgeSegments ? TimeSpan.Zero : bufferManager.LatestPts;
            await newSink.StartAsync(
                CreateFlashbackSessionContext(unifiedVideoCapture, _currentSettings),
                cancellationToken,
                ptsBaseOffset: ptsOffset).ConfigureAwait(false);

            newSink.FrameEncoded += OnFlashbackFrameEncoded;
            _flashbackSink = newSink;

            // Reattach feeds
            unifiedVideoCapture.SetFlashbackSink(newSink);
            AttachFlashbackAudioIfSupported(_wasapiAudioCapture, "buffer_cycle");
            if (_microphoneCapture != null && newSink.MicrophoneEnabled)
            {
                _microphoneCapture.SetAudioWriter(samples => newSink.WriteMicrophoneAudioAsync(samples));
                Logger.Log("FLASHBACK_MIC_ATTACH_OK reason='buffer_cycle'");
            }

            Logger.Log($"FLASHBACK_BUFFER_CYCLE_DONE mode=sink_only segments={bufferManager.SegmentCount} buffered={bufferManager.BufferedDuration.TotalSeconds:F1}s");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_FAIL error='{ex.Message}' — falling back to full teardown");
            try { newSink.Dispose(); } catch { /* Best-effort: dispose during error recovery must not mask the cycle failure */ }
            _flashbackSink = null;

            // Full teardown and rebuild
            await DisposeFlashbackPreviewBackendAsync(cancellationToken, purgeSegments: true).ConfigureAwait(false);
            await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, _currentSettings, cancellationToken).ConfigureAwait(false);
            Logger.Log("FLASHBACK_BUFFER_CYCLE_DONE mode=fallback_full_rebuild");
        }
    }

    private void OnFlashbackFrameEncoded(object? sender, long frameCount)
    {
        if (!IsFlashbackRecordingBackendActive())
            return;

        FrameCaptured?.Invoke(this, unchecked((ulong)Math.Max(0L, frameCount)));
    }

    private void ValidateFlashbackRecordingCapabilities(
        FlashbackEncoderSink flashbackSink,
        bool requiresHdmiAudio,
        bool requiresMicrophone)
    {
        if (requiresHdmiAudio && !flashbackSink.AudioEnabled)
            throw new InvalidOperationException(
                "Flashback recording cannot include HDMI audio because the active flashback session was started without audio.");

        if (requiresMicrophone && !flashbackSink.MicrophoneEnabled)
            throw new InvalidOperationException(
                "Flashback recording cannot include microphone audio because the active flashback session was started without microphone support.");
    }

    private static void EnsureFlashbackRecordingTopologyMatches(
        FlashbackEncoderSink flashbackSink,
        bool audioEnabled,
        bool microphoneEnabled)
    {
        if (flashbackSink.AudioEnabled == audioEnabled &&
            flashbackSink.MicrophoneEnabled == microphoneEnabled)
            return;

        throw new InvalidOperationException(
            "Flashback recording settings changed after preview start. " +
            $"Restart preview so flashback can reopen with audio={audioEnabled} microphone={microphoneEnabled} " +
            $"(current audio={flashbackSink.AudioEnabled} microphone={flashbackSink.MicrophoneEnabled}).");
    }

    private async Task<FinalizeResult> FinalizeFlashbackRecordingAsync(
        FlashbackEncoderSink flashbackSink,
        RecordingContext? recordingContext,
        CancellationToken cancellationToken)
    {
        var outputPath = recordingContext?.FinalOutputPath ?? string.Empty;

        // H3: Pause eviction BEFORE EndRecordingAsync to close the window where
        // eviction could delete segments between EndRecording (which resumes eviction
        // internally) and ExportFlashbackCoreAsync (which pauses it again).
        // With ref-counted eviction, the nested Pause from ExportFlashbackCoreAsync is safe.
        // M2: Track whether we actually paused so the finally block doesn't decrement past zero
        // if PauseEviction was never reached (e.g. bufferManager is null).
        bool outerPauseApplied = false;
        var bufferManager = _flashbackBufferManager;
        try
        {
            bufferManager?.PauseEviction();
            outerPauseApplied = true;

            var endResult = await flashbackSink.EndRecordingAsync(cancellationToken).ConfigureAwait(false);
            if (!endResult.Succeeded)
                return endResult;

            var startPts = flashbackSink.LastRecordingStartPts;
            var endPts = flashbackSink.LastRecordingEndPts;

            Logger.Log($"FLASHBACK_RECORDING_EXPORT_BEGIN output='{outputPath}' start_ms={(long)startPts.TotalMilliseconds} end_ms={(long)endPts.TotalMilliseconds}");

            var exportResult = await ExportFlashbackCoreAsync(startPts, endPts, outputPath, progress: null, ct: cancellationToken)
                .ConfigureAwait(false);

            Logger.Log($"FLASHBACK_RECORDING_EXPORT_DONE succeeded={exportResult.Succeeded} status='{exportResult.StatusMessage}'");
            return exportResult;
        }
        finally
        {
            if (outerPauseApplied)
                bufferManager?.ResumeEviction();
        }
    }

    private void OnUnifiedVideoCaptureFatalError(object? sender, Exception ex)
    {
        Logger.Log($"UNIFIED_VIDEO_CAPTURE_FATAL type={ex.GetType().Name} msg={ex.Message}");
        BeginFatalCaptureCleanup(ex);
    }

    private void BeginFatalCaptureCleanup(Exception ex)
    {
        if (Interlocked.Exchange(ref _fatalCleanupInProgress, 1) != 0)
        {
            return;
        }

        var generationAtFault = Interlocked.Read(ref _sessionGeneration);

        _ = Task.Run(async () =>
        {
            try
            {
                await CleanupAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception cleanupEx)
            {
                Logger.Log($"Fatal capture cleanup warning: {cleanupEx.Message}");
            }
            finally
            {
                // Only overwrite session state if no new session has started
                // during cleanup — a new RunTransitionAsync increments the
                // generation, so a mismatch means our Faulted write is stale.
                if (Interlocked.Read(ref _sessionGeneration) == generationAtFault)
                {
                    _sessionState = CaptureSessionState.Faulted;
                }
                else
                {
                    Logger.Log("FATAL_CLEANUP_SKIP_FAULTED reason='session_generation_changed'");
                }

                StatusChanged?.Invoke(this, $"Video capture error: {ex.Message}");
                ErrorOccurred?.Invoke(this, ex);
                Interlocked.Exchange(ref _fatalCleanupInProgress, 0);
            }
        });
    }

    public SourceSignalTelemetrySnapshot GetLatestSourceTelemetrySnapshot() => _latestSourceTelemetry;

    private void ResetObservedPixelTelemetry()
    {
        _firstObservedFramePixelFormat = null;
        _latestObservedFramePixelFormat = null;
        _latestObservedSurfaceFormat = null;
        Interlocked.Exchange(ref _observedP010FrameCount, 0);
        Interlocked.Exchange(ref _observedNv12FrameCount, 0);
        Interlocked.Exchange(ref _observedOtherFrameCount, 0);
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

    private void OnMicrophoneAudioLevelUpdated(object? sender, AudioLevelEventArgs e)
    {
        MicrophoneAudioLevelUpdated?.Invoke(this, e);
    }

    private async Task DisposeMicrophoneCaptureAsync()
    {
        var mic = _microphoneCapture;
        _microphoneCapture = null;
        if (mic != null)
        {
            try
            {
                mic.SetAudioWriter(null);
                mic.AudioLevelUpdated -= OnMicrophoneAudioLevelUpdated;
                mic.CaptureFailed -= OnWasapiCaptureFailed;
                await mic.DisposeAsync().ConfigureAwait(false);
                Logger.Log("MIC_MONITOR_STOP");
            }
            catch (Exception ex)
            {
                Logger.Log("Microphone capture dispose failed: " + ex.Message);
            }
        }
    }

    public Task UpdateMicrophoneMonitorAsync(bool enabled, string? deviceId, string? deviceName, CancellationToken cancellationToken = default)
        => RunTransitionAsync(_sessionState, async transitionToken =>
        {
            _micMonitorEnabled = enabled;
            _micMonitorDeviceId = deviceId;
            _micMonitorDeviceName = deviceName;

            await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);

            if (enabled && !_isRecording && _isVideoPreviewActive && !string.IsNullOrWhiteSpace(deviceId))
            {
                var micCapture = new WasapiAudioCapture();
                await micCapture.InitializeAsync(deviceId, transitionToken).ConfigureAwait(false);
                micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
                micCapture.CaptureFailed += OnWasapiCaptureFailed;
                micCapture.Start();
                _microphoneCapture = micCapture;
                if (_flashbackSink is { MicrophoneEnabled: true } fbSink)
                {
                    micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
                    Logger.Log("FLASHBACK_MIC_ATTACH_OK reason='mic_monitor_update'");
                }
                Logger.Log("MIC_MONITOR_START device='" + (deviceName ?? "?") + "'");
            }
            else
            {
                MicrophoneAudioLevelUpdated?.Invoke(this, new AudioLevelEventArgs(0, 0, false));
            }
        }, cancellationToken);

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
            playback.SetVolume(0);
            playback.Start();
            _wasapiAudioPlayback = playback;
            Logger.Log("WASAPI audio playback started.");
            playback.SetVolume(_isMonitoringMuted ? 0f : _previewVolume);
        }

        capture.SetPlayback(playback);

        // Update flashback controller with audio components (they weren't available
        // during flashback init because WASAPI starts after flashback).
        var controller = _flashbackPlaybackController;
        controller?.UpdateAudioComponents(playback, capture);
    }

    private void StopWasapiPlayback()
    {
        var fbController = _flashbackPlaybackController;
        fbController?.UpdateAudioComponents(null, null);
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

    private void CaptureEncoderRuntimeTelemetry(LibAvRecordingSink? sink)
    {
        if (sink == null)
        {
            return;
        }

        Interlocked.Exchange(ref _videoFramesDropped, sink.DroppedVideoFrames);
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

    public Task<PreviewFrameCaptureResult> CapturePreviewFrameAsync(string outputPath, CancellationToken cancellationToken = default)
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

        cancellationToken.ThrowIfCancellationRequested();
        return d3dSink.CaptureNextFrameAsync(outputPath);
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
            _avSyncBaselineDriftMs = double.NaN;
            ResetObservedPixelTelemetry();
            ResetCachedMjpegTimingMetrics();
            _latestSourceTelemetry = BuildFallbackTelemetry();
            await RefreshSourceTelemetryAsync(transitionToken).ConfigureAwait(false);
            TryCorrectFrameRateFromTelemetry();
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

            // Capture mic monitor settings for preview-time metering
            _micMonitorEnabled = settings.MicrophoneEnabled;
            _micMonitorDeviceId = settings.MicrophoneDeviceId;
            _micMonitorDeviceName = settings.MicrophoneDeviceName;

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
            var useMjpegHighFrameRateMode = settings.UseMjpegHighFrameRateMode;
            var audioDeviceId = settings.AudioEnabled
                ? (settings.UseCustomAudioInput ? settings.AudioDeviceId : (_audioDeviceId ?? _currentDevice.AudioDeviceId))
                : null;

            Logger.Log(
                "HDR_REQUEST_STATE scope=preview " +
                $"hdr_toggle={settings.HdrEnabled} " +
                $"require_p010={requireP010} " +
                $"mjpeg_hfr={useMjpegHighFrameRateMode} " +
                $"mode={settings.Width}x{settings.Height}@{settings.FrameRate:0.###}");

            UnifiedVideoCapture? unifiedVideoCapture = null;
            WasapiAudioCapture? wasapiCapture = null;
            try
            {
                Logger.LogFatalBreadcrumb($"PREVIEW_START phase=create_uvc");
                unifiedVideoCapture = new UnifiedVideoCapture();
                AttachUnifiedVideoCapture(unifiedVideoCapture);
                Logger.LogFatalBreadcrumb($"PREVIEW_START phase=init_uvc {(int)settings.Width}x{(int)settings.Height}@{settings.FrameRate:0.###} p010={requireP010} pxfmt={settings.RequestedPixelFormat} mjpeg_hfr={useMjpegHighFrameRateMode}");
                await unifiedVideoCapture.InitializeAsync(
                    _currentDevice.Id,
                    (int)settings.Width,
                    (int)settings.Height,
                    settings.FrameRate,
                    requireP010,
                    settings.RequestedPixelFormat,
                    useMjpegHighFrameRateMode,
                    settings.MjpegDecoderCount).ConfigureAwait(false);
                Logger.LogFatalBreadcrumb($"PREVIEW_START phase=init_done");
                unifiedVideoCapture.SetPreviewSink(_previewFrameSink);
                TryApplySharedPreviewDevice(unifiedVideoCapture, _previewFrameSink);
                Logger.LogFatalBreadcrumb($"PREVIEW_START phase=starting");
                unifiedVideoCapture.Start();
                Logger.LogFatalBreadcrumb($"PREVIEW_START phase=started");
                // Skip Lock2D by default — preview uses GPU textures via SubmitTexture,
                // never CPU bytes. Lock2D causes GPU pipeline stalls (~5% cadence drops
                // at 120fps, worse at 4K). The existing guards (hasTexture, !frameData.IsEmpty)
                // handle the rare fallback case where GPU texture extraction fails.
                if (unifiedVideoCapture.D3DManager != null)
                {
                    unifiedVideoCapture.SetSkipCpuReadback(true);
                }
                _unifiedVideoCapture = unifiedVideoCapture;
                _lastMfSourceReaderFramesDelivered = 0;
                _lastMfSourceReaderFramesDropped = 0;
                _lastMfSourceReaderNegotiatedFormat = unifiedVideoCapture.NegotiatedFormat;

                _actualWidth = (uint)Math.Max(1, unifiedVideoCapture.Width);
                _actualHeight = (uint)Math.Max(1, unifiedVideoCapture.Height);
                _actualFrameRateNumerator = settings.RequestedFrameRateNumerator;
                _actualFrameRateDenominator = settings.RequestedFrameRateDenominator;
                _actualFrameRate = _actualFrameRateNumerator.HasValue && _actualFrameRateDenominator is > 0
                    ? (double)_actualFrameRateNumerator.Value / _actualFrameRateDenominator.Value
                    : unifiedVideoCapture.Fps > 0 ? unifiedVideoCapture.Fps : settings.FrameRate;
                _actualFrameRateArg = ResolveFrameRateArg(settings, _actualFrameRate ?? settings.FrameRate);
                _actualPixelFormat = unifiedVideoCapture.NativeInputFormat ?? (unifiedVideoCapture.IsP010 ? "P010" : "NV12");
                _activeVideoInputPixelFormat = unifiedVideoCapture.IsP010 ? "p010le" : "nv12";
                TryCorrectFrameRateFromTelemetry();

                if (settings.AudioEnabled && !string.IsNullOrWhiteSpace(audioDeviceId))
                {
                    wasapiCapture = new WasapiAudioCapture();
                    await wasapiCapture.InitializeAsync(audioDeviceId, transitionToken).ConfigureAwait(false);
                    wasapiCapture.AudioLevelUpdated += OnWasapiAudioLevelUpdated;
                    wasapiCapture.CaptureFailed += OnWasapiCaptureFailed;
                    wasapiCapture.Start();
                    _wasapiAudioCapture = wasapiCapture;
                }
                else if (settings.AudioEnabled)
                {
                    Logger.Log("Audio preview requested but no audio capture device is available; continuing with video-only preview.");
                }

                if (_isAudioPreviewActive && _wasapiAudioCapture != null)
                {
                    await StartWasapiPlaybackAsync(transitionToken).ConfigureAwait(false);
                }

                Logger.Log(
                    _wasapiAudioCapture != null
                        ? "Preview backend active: IMFSourceReader video + WASAPI audio ingest."
                        : "Preview backend active: IMFSourceReader video only (no audio capture endpoint).");

                // Start mic monitoring if enabled (metering only, no recording sink)
                if (_micMonitorEnabled && !string.IsNullOrWhiteSpace(_micMonitorDeviceId))
                {
                    try
                    {
                        var micCapture = new WasapiAudioCapture();
                        await micCapture.InitializeAsync(_micMonitorDeviceId, transitionToken).ConfigureAwait(false);
                        micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
                        micCapture.CaptureFailed += OnWasapiCaptureFailed;
                        micCapture.Start();
                        _microphoneCapture = micCapture;
                        if (_flashbackSink is { MicrophoneEnabled: true } fbSink)
                        {
                            micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
                            Logger.Log("FLASHBACK_MIC_ATTACH_OK reason='preview_mic_monitor_start'");
                        }
                        Logger.Log("MIC_MONITOR_START device='" + (_micMonitorDeviceName ?? "?") + "'");
                    }
                    catch (Exception micEx)
                    {
                        Logger.Log("Mic monitor start failed (non-fatal): " + micEx.Message);
                    }
                }

                // Start flashback AFTER all preview components are running.
                // This eliminates the ~840ms A/V sync drift caused by WASAPI audio
                // flowing before the source reader delivers its first video frame.
                await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, transitionToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Unified preview start failed: {ex.Message}");
                await DisposeFlashbackPreviewBackendAsync(transitionToken).ConfigureAwait(false);
                _unifiedVideoCapture = null;
                if (unifiedVideoCapture != null)
                {
                    DetachUnifiedVideoCapture(unifiedVideoCapture);
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
                await DisposeFlashbackPreviewBackendAsync(transitionToken).ConfigureAwait(false);

                var unifiedVideoCapture = _unifiedVideoCapture;
                _unifiedVideoCapture = null;
                if (unifiedVideoCapture != null)
                {
                    CacheMjpegTimingMetrics(unifiedVideoCapture);
                    _lastMfSourceReaderFramesDelivered = unifiedVideoCapture.VideoFramesArrived;
                    _lastMfSourceReaderFramesDropped = unifiedVideoCapture.VideoFramesDropped;
                    _lastMfSourceReaderNegotiatedFormat = unifiedVideoCapture.NegotiatedFormat;
                    DetachUnifiedVideoCapture(unifiedVideoCapture);
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

                await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);
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

            LibAvRecordingSink? libAvSink = null;
            IRecordingSink? recordingSink = null;
            WasapiAudioCapture? ownedWasapiAudioCapture = null;
            UnifiedVideoCapture? ownedUnifiedVideoCapture = null;
            RecordingContext? recordingContext = null;
            UnifiedVideoCapture? recordingVideoCapture = null;
            var sinkAttachedForAudioOnly = false;
            Volatile.Write(ref _wasapiAudioCaptureFaulted, false);
            Volatile.Write(ref _wasapiAudioCaptureFaultMessage, null);
            try
            {
                // --- Unified path: piggyback on existing flashback NVENC session ---
                if (_flashbackEnabled && _flashbackSink != null)
                {
                    StorageFolder fbOutputFolder;
                    try
                    {
                        fbOutputFolder = await StorageFolder.GetFolderFromPathAsync(settings.OutputPath);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Output folder is unavailable: {settings.OutputPath}", ex);
                    }

                    transitionToken.ThrowIfCancellationRequested();

                    var fbEffectiveFrameRate = _unifiedVideoCapture?.Fps > 0 ? _unifiedVideoCapture.Fps : settings.FrameRate;
                    var fbRecordingContext = await _artifactManager.CreateContextAsync(
                        fbOutputFolder,
                        new RecordingContextRequest
                        {
                            Settings = settings,
                            UsePostMuxAudio = false,
                            AudioDeviceName = settings.AudioEnabled
                                ? (settings.UseCustomAudioInput ? settings.AudioDeviceName : (_audioDeviceName ?? _currentDevice.AudioDeviceName))
                                : null,
                            MicrophoneDeviceName = settings.MicrophoneEnabled ? settings.MicrophoneDeviceName : null,
                            EffectiveFrameRate = fbEffectiveFrameRate,
                            FrameRateArg = ResolveFrameRateArg(settings, fbEffectiveFrameRate),
                            EffectiveWidth = _actualWidth ?? settings.Width,
                            EffectiveHeight = _actualHeight ?? settings.Height,
                            VideoInputPixelFormat = _unifiedVideoCapture?.IsP010 == true ? "p010le" : "nv12",
                            IsFullRangeInput = _unifiedVideoCapture?.IsSoftwareMjpegPipelineActive == true,
                            GpuHandles = GpuPipelineHandles.None
                        }).ConfigureAwait(false);

                    // If audio/microphone topology changed since flashback was started,
                    // auto-restart the flashback backend to match the new settings.
                    if (_flashbackSink.AudioEnabled != settings.AudioEnabled ||
                        _flashbackSink.MicrophoneEnabled != settings.MicrophoneEnabled)
                    {
                        Logger.Log($"FLASHBACK_TOPOLOGY_MISMATCH_AUTO_RESTART " +
                            $"audio={settings.AudioEnabled} (was {_flashbackSink.AudioEnabled}) " +
                            $"mic={settings.MicrophoneEnabled} (was {_flashbackSink.MicrophoneEnabled})");

                        await DisposeFlashbackPreviewBackendAsync(transitionToken, purgeSegments: true).ConfigureAwait(false);

                        var uvc = _unifiedVideoCapture;
                        if (uvc != null)
                        {
                            await EnsureFlashbackPreviewBackendAsync(uvc, settings, transitionToken).ConfigureAwait(false);
                        }

                        if (_flashbackSink == null)
                        {
                            throw new InvalidOperationException("Failed to restart flashback backend for new audio topology.");
                        }
                    }

                    _flashbackSink.BeginRecording(fbRecordingContext.FinalOutputPath);
                    _recordingSink = _flashbackSink;
                    _libavSink = null;
                    _recordingContext = fbRecordingContext;
                    _activeRecordingSettings = settings;
                    _isRecording = true;
                    _flashbackRecordingStartBytes = _flashbackBufferManager?.TotalBytesWritten ?? 0;
                    _lastOutputPath = fbRecordingContext.FinalOutputPath;
                    _lastFinalizeStatus = "Recording";
                    _lastFinalizeUtc = null;
                    _lastPreservedArtifacts = Array.Empty<string>();
                    _recordingStopwatch.Restart();
                    StatusChanged?.Invoke(this, "Recording");
                    Logger.Log($"FLASHBACK_UNIFIED_RECORDING_START output='{fbRecordingContext.FinalOutputPath}'");
                    return;
                }

                // --- Standard path: create dedicated LibAvRecordingSink ---
                libAvSink = new LibAvRecordingSink();
                libAvSink.FrameEncoded += (s, count) => FrameCaptured?.Invoke(this, unchecked((ulong)Math.Max(0L, count)));
                recordingSink = libAvSink;

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
                await RefreshSourceTelemetryAsync(transitionToken).ConfigureAwait(false);
                TryCorrectFrameRateFromTelemetry();
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

                var requireP010 = string.Equals(videoInputPixelFormat, "p010le", StringComparison.OrdinalIgnoreCase);
                var useMjpegHighFrameRateMode = settings.UseMjpegHighFrameRateMode;
                var unifiedVideoCapture = _unifiedVideoCapture;
                if (unifiedVideoCapture == null)
                {
                    ownedUnifiedVideoCapture = new UnifiedVideoCapture();
                    AttachUnifiedVideoCapture(ownedUnifiedVideoCapture);
                    await ownedUnifiedVideoCapture.InitializeAsync(
                        _currentDevice.Id,
                        (int)effectiveWidth,
                        (int)effectiveHeight,
                        effectiveFrameRate,
                        requireP010,
                        settings.RequestedPixelFormat,
                        useMjpegHighFrameRateMode,
                        settings.MjpegDecoderCount).ConfigureAwait(false);
                    ownedUnifiedVideoCapture.SetPreviewSink(_isVideoPreviewActive ? _previewFrameSink : null);
                    TryApplySharedPreviewDevice(ownedUnifiedVideoCapture, _isVideoPreviewActive ? _previewFrameSink : null);
                    unifiedVideoCapture = ownedUnifiedVideoCapture;
                    _unifiedVideoCapture = ownedUnifiedVideoCapture;
                }
                else if (unifiedVideoCapture.IsP010 != requireP010)
                {
                    throw new InvalidOperationException(
                        $"Recording requires {(requireP010 ? "P010" : "NV12")}, but the active source-reader session negotiated {(unifiedVideoCapture.IsP010 ? "P010" : "NV12")}.");
                }
                else if (unifiedVideoCapture.IsHighFrameRateMjpegMode != useMjpegHighFrameRateMode)
                {
                    throw new InvalidOperationException(
                        $"Recording requested mjpeg_hfr={useMjpegHighFrameRateMode}, but the active preview session is mjpeg_hfr={unifiedVideoCapture.IsHighFrameRateMjpegMode}.");
                }

                recordingVideoCapture = unifiedVideoCapture;
                TryApplySharedPreviewDevice(unifiedVideoCapture, _isVideoPreviewActive ? _previewFrameSink : null);

                var isMjpegMode = recordingVideoCapture.IsSoftwareMjpegPipelineActive;
                var d3dManager = unifiedVideoCapture.D3DManager;
                var recordingWidth = (uint)Math.Max(1, unifiedVideoCapture.Width);
                var recordingHeight = (uint)Math.Max(1, unifiedVideoCapture.Height);
                var recordingFrameRate = unifiedVideoCapture.Fps > 0 ? unifiedVideoCapture.Fps : effectiveFrameRate;
                var frameRateArg = ResolveFrameRateArg(settings, recordingFrameRate);
                IntPtr cudaHwDeviceCtxPtr = IntPtr.Zero;
                IntPtr cudaHwFramesCtxPtr = IntPtr.Zero;

                recordingContext = await _artifactManager.CreateContextAsync(
                    outputFolder,
                    new RecordingContextRequest
                    {
                        Settings = settings,
                        UsePostMuxAudio = false,
                        AudioDeviceName = audioDeviceName,
                        MicrophoneDeviceName = settings.MicrophoneEnabled ? settings.MicrophoneDeviceName : null,
                        EffectiveFrameRate = recordingFrameRate,
                        FrameRateArg = frameRateArg,
                        EffectiveWidth = recordingWidth,
                        EffectiveHeight = recordingHeight,
                        VideoInputPixelFormat = videoInputPixelFormat,
                        IsFullRangeInput = isMjpegMode,
                        GpuHandles = new GpuPipelineHandles(
                            isMjpegMode ? IntPtr.Zero : (d3dManager?.Device.NativePointer ?? IntPtr.Zero),
                            isMjpegMode ? IntPtr.Zero : (d3dManager?.ImmediateContext.NativePointer ?? IntPtr.Zero),
                            cudaHwDeviceCtxPtr,
                            cudaHwFramesCtxPtr)
                    }).ConfigureAwait(false);

                transitionToken.ThrowIfCancellationRequested();
                _mfConvertersDisabled = requireP010 || isMjpegMode;
                Logger.Log(
                    "HDR_NEGOTIATION " +
                    $"requested_hdr={hdrPipelineRequested} " +
                    $"requested_subtype={(hdrPipelineRequested ? "P010" : "NV12")} " +
                    $"requested_source_subtype={settings.RequestedPixelFormat ?? (hdrPipelineRequested ? "P010" : "NV12")} " +
                    $"mjpeg_hfr={useMjpegHighFrameRateMode} " +
                    $"negotiated_pixel_format={(unifiedVideoCapture.IsP010 ? "P010" : "NV12")} " +
                    $"negotiated_subtype_token={(string.Equals(videoInputPixelFormat, "p010le", StringComparison.OrdinalIgnoreCase) ? "P010|MFVideoFormat_P010" : "NV12")} " +
                    $"hdr_static_metadata_requested={(!string.IsNullOrWhiteSpace(settings.HdrMasterDisplayMetadata) || (settings.HdrMaxCll > 0 && settings.HdrMaxFall > 0))} " +
                    $"hdr_master_display_set={(!string.IsNullOrWhiteSpace(settings.HdrMasterDisplayMetadata))} " +
                    $"hdr_max_cll={settings.HdrMaxCll} " +
                    $"hdr_max_fall={settings.HdrMaxFall} " +
                    $"mf_readwrite_disable_converters={(_mfConvertersDisabled ? "true" : "false")} " +
                    $"libav_ingest_pix_fmt={(string.Equals(videoInputPixelFormat, "p010le", StringComparison.OrdinalIgnoreCase) ? "AV_PIX_FMT_P010LE" : "AV_PIX_FMT_NV12")}");

                await recordingSink.StartAsync(recordingContext, transitionToken).ConfigureAwait(false);
                transitionToken.ThrowIfCancellationRequested();

                _lastMfSourceReaderFramesDelivered = 0;
                _lastMfSourceReaderFramesDropped = 0;
                _lastMfSourceReaderNegotiatedFormat = unifiedVideoCapture.NegotiatedFormat;
                _actualWidth = (uint)Math.Max(1, unifiedVideoCapture.Width);
                _actualHeight = (uint)Math.Max(1, unifiedVideoCapture.Height);
                _actualFrameRateNumerator = settings.RequestedFrameRateNumerator;
                _actualFrameRateDenominator = settings.RequestedFrameRateDenominator;
                _actualFrameRate = _actualFrameRateNumerator.HasValue && _actualFrameRateDenominator is > 0
                    ? (double)_actualFrameRateNumerator.Value / _actualFrameRateDenominator.Value
                    : unifiedVideoCapture.Fps > 0 ? unifiedVideoCapture.Fps : effectiveFrameRate;
                _actualFrameRateArg = ResolveFrameRateArg(settings, _actualFrameRate ?? effectiveFrameRate);
                _actualPixelFormat = unifiedVideoCapture.NativeInputFormat ?? (unifiedVideoCapture.IsP010 ? "P010" : "NV12");
                TryCorrectFrameRateFromTelemetry();

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

                var activeLibAvSink = libAvSink
                    ?? throw new InvalidOperationException("Recording requires an active LibAv sink.");

                // Dispose preview-time mic monitor — recording creates its own with sink
                await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);

                if (settings.MicrophoneEnabled && !string.IsNullOrWhiteSpace(settings.MicrophoneDeviceId))
                {
                    var micSink = activeLibAvSink; // capture stable reference — libAvSink is nulled on success path
                    var micCapture = new WasapiAudioCapture();
                    await micCapture.InitializeAsync(settings.MicrophoneDeviceId, transitionToken).ConfigureAwait(false);
                    micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
                    micCapture.CaptureFailed += OnWasapiCaptureFailed;
                    micCapture.SetAudioWriter(samples => micSink.WriteMicrophoneAudioAsync(samples));
                    micCapture.Start();
                    _microphoneCapture = micCapture;
                    Logger.Log("MICROPHONE_CAPTURE_START device='" + settings.MicrophoneDeviceName + "'");
                }

                IGpuVideoFrameEncoder? gpuEncoder =
                    (!isMjpegMode && activeLibAvSink.GpuEncodingEnabled)
                        ? activeLibAvSink
                        : null;

                await unifiedVideoCapture.StartRecordingAsync(recordingSink, activeLibAvSink, gpuEncoder).ConfigureAwait(false);
                if (gpuEncoder != null)
                {
                    Logger.Log("GPU_RECORDING_ACTIVE gpu_encoder=active");
                }

                if (ownedUnifiedVideoCapture != null)
                {
                    ownedUnifiedVideoCapture.Start();
                }

                _libavSink = libAvSink;
                _recordingSink = recordingSink;
                _recordingContext = recordingContext;
                _activeRecordingSettings = settings;
                _isRecording = true;
                _activeVideoInputPixelFormat = videoInputPixelFormat;
                Interlocked.Exchange(ref _videoFramesDropped, 0);
                ResetObservedPixelTelemetry();
                RecordObservedPixelFormat(recordingContext.HdrPipelineActive ? "P010" : "NV12", incrementAsFrame: false);
                _lastOutputPath = recordingContext.FinalOutputPath;
                _lastFinalizeStatus = "Recording";
                _lastFinalizeUtc = null;
                _lastPreservedArtifacts = Array.Empty<string>();
                _lastUsePostMuxAudio = recordingContext.UsePostMuxAudio;
                _recordingStopwatch.Restart();
                StatusChanged?.Invoke(this, "Recording");
                libAvSink = null;
                recordingSink = null;
                ownedWasapiAudioCapture = null;
                ownedUnifiedVideoCapture = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"Suppressed exception in CaptureService.StartRecordingAsync: {ex.Message}");

                if (sinkAttachedForAudioOnly && _wasapiAudioCapture != null)
                {
                    _wasapiAudioCapture.DetachRecordingSink();
                }

                await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);

                if (ownedUnifiedVideoCapture != null)
                {
                    DetachUnifiedVideoCapture(ownedUnifiedVideoCapture);
                }

                await _artifactManager.RollbackAsync(recordingContext).ConfigureAwait(false);
                await DisposeTransientRecordingBackendAsync(
                    recordingSink,
                    ownedWasapiAudioCapture,
                    ownedUnifiedVideoCapture).ConfigureAwait(false);

                if (ownedWasapiAudioCapture != null && ReferenceEquals(_wasapiAudioCapture, ownedWasapiAudioCapture))
                {
                    DetachWasapiAudioCapture(ownedWasapiAudioCapture);
                    _wasapiAudioCapture = null;
                }

                if (ownedUnifiedVideoCapture != null && ReferenceEquals(_unifiedVideoCapture, ownedUnifiedVideoCapture))
                {
                    CacheMjpegTimingMetrics(ownedUnifiedVideoCapture);
                    _lastMfSourceReaderFramesDelivered = ownedUnifiedVideoCapture.VideoFramesArrived;
                    _lastMfSourceReaderFramesDropped = ownedUnifiedVideoCapture.VideoFramesDropped;
                    _lastMfSourceReaderNegotiatedFormat = ownedUnifiedVideoCapture.NegotiatedFormat;
                    _unifiedVideoCapture = null;
                }

                _recordingContext = null;
                _activeRecordingSettings = null;
                _isRecording = false;
                _recordingStopwatch.Reset();
                _mfConvertersDisabled = false;
                throw;
            }
        }, cancellationToken);

    public Task StopRecordingAsync(CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Ready, async transitionToken =>
        {
            if (!_isRecording && _recordingSink == null && _libavSink == null)
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

            // Create WASAPI capture if it wasn't started with the preview (audio was disabled at start)
            if (_wasapiAudioCapture == null && _currentDevice != null)
            {
                var audioId = _audioDeviceId ?? _currentDevice.AudioDeviceId;
                if (!string.IsNullOrEmpty(audioId))
                {
                    Logger.Log($"Late-starting WASAPI audio capture for device {audioId}");
                    var wasapiCapture = new WasapiAudioCapture();
                    await wasapiCapture.InitializeAsync(audioId, transitionToken).ConfigureAwait(false);
                    wasapiCapture.AudioLevelUpdated += OnWasapiAudioLevelUpdated;
                    wasapiCapture.CaptureFailed += OnWasapiCaptureFailed;
                    wasapiCapture.Start();
                    _wasapiAudioCapture = wasapiCapture;
                    Volatile.Write(ref _wasapiAudioCaptureFaulted, false);
                    Volatile.Write(ref _wasapiAudioCaptureFaultMessage, null);
                }
                else
                {
                    Logger.Log("Audio preview requested but no audio capture device is available.");
                }
            }

            if (_wasapiAudioCapture == null)
            {
                _isAudioPreviewActive = false;
                StatusChanged?.Invoke(this, "Audio preview unavailable");
                return;
            }

            _isAudioPreviewActive = true;
            if (_wasapiAudioCapture != null)
            {
                await StartWasapiPlaybackAsync(transitionToken).ConfigureAwait(false);
            }

            StatusChanged?.Invoke(this, "Audio preview started");
        }, cancellationToken);

    public Task StopAudioPreviewAsync(bool teardownCapture = false, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Ready, async transitionToken =>
        {
            transitionToken.ThrowIfCancellationRequested();
            _isAudioPreviewActive = false;
            StopWasapiPlayback();

            if (teardownCapture && !_isRecording)
            {
                var capture = _wasapiAudioCapture;
                _wasapiAudioCapture = null;
                DetachWasapiAudioCapture(capture);
                if (capture != null)
                {
                    Logger.Log("Tearing down WASAPI audio capture (audio disabled)");
                    await capture.DisposeAsync().ConfigureAwait(false);
                }
            }

            AudioLevelUpdated?.Invoke(this, new AudioLevelEventArgs(0, 0, false));
            StatusChanged?.Invoke(this, "Audio preview stopped");
        }, cancellationToken);

    public Task UpdateAudioInputAsync(string? audioDeviceId, string? audioDeviceName, CancellationToken cancellationToken = default)
        => RunTransitionAsync(_sessionState, async transitionToken =>
        {
            transitionToken.ThrowIfCancellationRequested();
            var previousDeviceId = _audioDeviceId;
            _audioDeviceId = audioDeviceId;
            _audioDeviceName = audioDeviceName;

            if (string.Equals(previousDeviceId, audioDeviceId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (_wasapiAudioCapture == null)
            {
                return;
            }

            Logger.Log($"Live audio input switch: {audioDeviceName ?? "(card default)"}");

            var activeSink = _isRecording ? _recordingSink : null;
            if (activeSink != null)
            {
                _wasapiAudioCapture.DetachRecordingSink();
            }

            var oldCapture = _wasapiAudioCapture;
            _wasapiAudioCapture = null;
            DetachWasapiAudioCapture(oldCapture);
            await oldCapture.DisposeAsync().ConfigureAwait(false);

            var resolvedId = audioDeviceId ?? _currentDevice?.AudioDeviceId;
            if (!string.IsNullOrEmpty(resolvedId))
            {
                var newCapture = new WasapiAudioCapture();
                await newCapture.InitializeAsync(resolvedId, transitionToken).ConfigureAwait(false);
                newCapture.AudioLevelUpdated += OnWasapiAudioLevelUpdated;
                newCapture.CaptureFailed += OnWasapiCaptureFailed;
                newCapture.Start();
                _wasapiAudioCapture = newCapture;
                Volatile.Write(ref _wasapiAudioCaptureFaulted, false);
                Volatile.Write(ref _wasapiAudioCaptureFaultMessage, null);

                AttachFlashbackAudioIfSupported(newCapture, "audio_input_switch");

                if (_isAudioPreviewActive)
                {
                    await StartWasapiPlaybackAsync(transitionToken).ConfigureAwait(false);
                }

                if (activeSink != null)
                {
                    newCapture.AttachRecordingSink(activeSink);
                }

                Logger.Log($"Audio input switched to: {audioDeviceName ?? resolvedId}");
            }
            else
            {
                Logger.Log("Audio input cleared — no device available");
                AudioLevelUpdated?.Invoke(this, new AudioLevelEventArgs(0, 0, false));
            }
        }, cancellationToken);

    public Task CleanupAsync(CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.CleaningUp, async transitionToken =>
        {
            var cancellationRequested = false;
            if (_isRecording || _recordingSink != null || _libavSink != null)
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
                    CacheMjpegTimingMetrics(unifiedVideoCapture);
                    _lastMfSourceReaderFramesDelivered = unifiedVideoCapture.VideoFramesArrived;
                    _lastMfSourceReaderFramesDropped = unifiedVideoCapture.VideoFramesDropped;
                    _lastMfSourceReaderNegotiatedFormat = unifiedVideoCapture.NegotiatedFormat;
                    DetachUnifiedVideoCapture(unifiedVideoCapture);
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

            await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);

            try
            {
                await DisposeFlashbackPreviewBackendAsync(transitionToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Cleanup flashback dispose warning: {ex.Message}");
            }

            StopTelemetryPoll();
            _isVideoPreviewActive = false;
            _isAudioPreviewActive = false;
            _isInitialized = false;
            _currentDevice = null;
            _currentSettings = null;
            _activeRecordingSettings = null;
            _recordingContext = null;
            _avSyncBaselineDriftMs = double.NaN;
            _sessionState = _isDisposed != 0 ? CaptureSessionState.Disposed : CaptureSessionState.Uninitialized;

            if (cancellationRequested || transitionToken.IsCancellationRequested)
            {
                transitionToken.ThrowIfCancellationRequested();
            }
        }, cancellationToken);

    private async Task<FinalizeResult> StopAndDisposeRecordingBackendAsync(string fallbackStatusMessage, CancellationToken cancellationToken)
    {
        // --- Unified flashback recording path: remux from .ts, cycle buffer ---
        if (IsFlashbackRecordingBackendActive())
        {
            var flashbackSink = _flashbackSink!;
            var fbRecordingContext = _recordingContext;
            var fbOutputPath = fbRecordingContext?.FinalOutputPath ?? (_lastOutputPath ?? string.Empty);

            _recordingSink = null;
            // Don't null _flashbackSink — it continues for the buffer

            Logger.Log("FLASHBACK_UNIFIED_RECORDING_STOP_BEGIN");
            FinalizeResult fbResult;
            try
            {
                fbResult = await FinalizeFlashbackRecordingAsync(flashbackSink, fbRecordingContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_UNIFIED_RECORDING_FINALIZE_FAIL error='{ex.Message}'");
                fbResult = FinalizeResult.Failure(fbOutputPath, $"Flashback recording finalize failed: {ex.Message}");
            }

            // If settings changed during recording (format, buffer duration, etc.),
            // do a full restart to apply them. Otherwise just cycle the sink to
            // preserve DVR history.
            try
            {
                if (_pendingFlashbackSettingsChange)
                {
                    _pendingFlashbackSettingsChange = false;
                    Logger.Log("FLASHBACK_SETTINGS_APPLY_AFTER_RECORDING");
                    await DisposeFlashbackPreviewBackendAsync(cancellationToken, purgeSegments: true).ConfigureAwait(false);
                    if (_flashbackEnabled && _unifiedVideoCapture != null && _currentSettings != null)
                        await EnsureFlashbackPreviewBackendAsync(_unifiedVideoCapture, _currentSettings, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await CycleFlashbackBufferAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_BUFFER_CYCLE_FAIL error='{ex.Message}'");
            }

            _recordingStopwatch.Stop();
            _isRecording = false;
            if (!_isVideoPreviewActive) StopTelemetryPoll();
            _recordingContext = null;
            _activeRecordingSettings = null;
            _lastFinalizeStatus = fbResult.StatusMessage;
            _lastFinalizeUtc = DateTimeOffset.UtcNow;
            _lastPreservedArtifacts = fbResult.PreservedArtifacts;

            // Restart mic monitoring if preview is still active
            if (_isVideoPreviewActive && _micMonitorEnabled && !string.IsNullOrWhiteSpace(_micMonitorDeviceId))
            {
                try
                {
                    if (_microphoneCapture == null)
                    {
                        var micCapture = new WasapiAudioCapture();
                        await micCapture.InitializeAsync(_micMonitorDeviceId, cancellationToken).ConfigureAwait(false);
                        micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
                        micCapture.CaptureFailed += OnWasapiCaptureFailed;
                        micCapture.Start();
                        _microphoneCapture = micCapture;
                        if (_flashbackSink is { MicrophoneEnabled: true } fbSink)
                        {
                            micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"FLASHBACK_MIC_RESTART_WARN error='{ex.Message}'");
                }
            }

            Logger.Log($"FLASHBACK_UNIFIED_RECORDING_STOP_DONE succeeded={fbResult.Succeeded} output='{fbResult.OutputPath}'");
            return fbResult;
        }

        // --- Standard LibAvRecordingSink path ---
        var sink = _recordingSink;
        var libAvSink = _libavSink;
        var recordingContext = _recordingContext;
        var fallbackOutputPath = recordingContext?.FinalOutputPath ?? (_lastOutputPath ?? string.Empty);

        _recordingSink = null;
        _libavSink = null;

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
            finally
            {
                // Keep SkipCpuReadback=true — preview uses GPU textures, not CPU bytes.
                // Lock2D is never needed while D3D shared device is active.
            }

            _lastMfSourceReaderFramesDelivered = unifiedVideoCapture.VideoFramesArrived;
            _lastMfSourceReaderFramesDropped = unifiedVideoCapture.VideoFramesDropped;
            _lastMfSourceReaderNegotiatedFormat = unifiedVideoCapture.NegotiatedFormat;
            var recordingFramesDelivered = unifiedVideoCapture.RecordingFramesDelivered;
            var recordingFramesEnqueued = unifiedVideoCapture.VideoFramesWrittenToSink;
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

        await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);

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

        if (!_isVideoPreviewActive)
        {
            _unifiedVideoCapture = null;
            if (unifiedVideoCapture != null)
            {
                try
                {
                    CacheMjpegTimingMetrics(unifiedVideoCapture);
                    DetachUnifiedVideoCapture(unifiedVideoCapture);
                    await unifiedVideoCapture.StopAsync().ConfigureAwait(false);
                    await unifiedVideoCapture.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Unified video capture dispose failed: {ex.Message}");
                    if (cancellationException == null && result.Succeeded)
                    {
                        result = FinalizeResult.Failure(fallbackOutputPath, $"Unified video capture dispose failed: {ex.Message}");
                    }
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
                    if (cancellationException == null && result.Succeeded)
                    {
                        result = FinalizeResult.Failure(fallbackOutputPath, $"Recording WASAPI capture dispose failed: {ex.Message}");
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

        if (libAvSink != null)
        {
            CaptureEncoderRuntimeTelemetry(libAvSink);
        }

        _recordingStopwatch.Stop();
        _isRecording = false;
        if (!_isVideoPreviewActive) StopTelemetryPoll();
        _recordingContext = null;
        _activeRecordingSettings = null;
        _mfConvertersDisabled = false;

        // Restart mic monitoring if preview is still active
        if (_isVideoPreviewActive && _micMonitorEnabled && !string.IsNullOrWhiteSpace(_micMonitorDeviceId))
        {
            try
            {
                var micCapture = new WasapiAudioCapture();
                await micCapture.InitializeAsync(_micMonitorDeviceId, cancellationToken).ConfigureAwait(false);
                micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
                micCapture.CaptureFailed += OnWasapiCaptureFailed;
                micCapture.Start();
                _microphoneCapture = micCapture;
                if (_flashbackSink is { MicrophoneEnabled: true } fbSink)
                {
                    micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
                    Logger.Log("FLASHBACK_MIC_ATTACH_OK reason='mic_monitor_restart'");
                }
                Logger.Log("MIC_MONITOR_RESTART device='" + (_micMonitorDeviceName ?? "?") + "'");
            }
            catch (Exception micEx)
            {
                Logger.Log("Mic monitor restart failed (non-fatal): " + micEx.Message);
            }
        }

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

        // MJPEG (JPEG) uses full-range YCbCr (0-255). Tell the VP to use
        // YcbcrFullG22LeftP709 instead of the default studio-range color space.
        renderer.FullRangeInput = capture.IsHighFrameRateMjpegMode;

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
        WasapiAudioCapture? wasapiCapture,
        UnifiedVideoCapture? unifiedVideoCapture)
    {
        if (unifiedVideoCapture != null)
        {
            try
            {
                await unifiedVideoCapture.StopRecordingAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient unified video recording stop failed during rollback: {ex.Message}");
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
            Interlocked.Increment(ref _sessionGeneration);
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
        if (_isDisposed != 0) return CaptureSessionState.Disposed;
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

    /// <summary>
    /// When the driver reports integer frame rates (e.g. 120/1 for MJPG) but source
    /// telemetry confirms NTSC timing (e.g. vfreq=11987 → 119.88fps), override the
    /// actual frame rate to the correct NTSC rational. This affects recording metadata,
    /// cadence tracking, and UI display.
    /// </summary>
    private void TryCorrectFrameRateFromTelemetry()
    {
        if (_actualFrameRateDenominator is not null and not 1)
            return; // Already fractional — no correction needed.

        var telemetry = _latestSourceTelemetry;
        if (!telemetry.HasFrameRate || !telemetry.FrameRateExact.HasValue)
            return;

        // Check if telemetry reports an NTSC rate (x000/1001 family).
        // NativeXu vfreq is in 0.01Hz: 11987 → 119.87Hz ≈ 120000/1001.
        var telemetryFps = telemetry.FrameRateExact.Value;
        var friendlyBucket = (int)Math.Round(_actualFrameRate ?? 0, MidpointRounding.AwayFromZero);
        if (friendlyBucket <= 0)
            return;

        var expectedNtscFps = friendlyBucket * 1000.0 / 1001.0;
        if (Math.Abs(telemetryFps - expectedNtscFps) > 0.15)
            return; // Telemetry doesn't match NTSC pattern for this bucket.

        var ntscNumerator = (uint)(friendlyBucket * 1000);
        const uint ntscDenominator = 1001;
        var correctedFps = (double)ntscNumerator / ntscDenominator;

        Logger.Log(
            $"FRAMERATE_NTSC_CORRECTION driver={_actualFrameRateNumerator}/{_actualFrameRateDenominator} " +
            $"telemetry={telemetryFps:0.###} corrected={ntscNumerator}/{ntscDenominator} ({correctedFps:0.######})");

        _actualFrameRate = correctedFps;
        _actualFrameRateNumerator = ntscNumerator;
        _actualFrameRateDenominator = ntscDenominator;
        _actualFrameRateArg = $"{ntscNumerator}/{ntscDenominator}";
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
            VideoFormat = telemetry.VideoFormat ?? fallback.VideoFormat,
            Colorimetry = telemetry.Colorimetry ?? fallback.Colorimetry,
            Quantization = telemetry.Quantization ?? fallback.Quantization,
            HdrTransferFunction = telemetry.HdrTransferFunction ?? fallback.HdrTransferFunction,
            HdrTransferCode = telemetry.HdrTransferCode ?? fallback.HdrTransferCode,
            Firmware = telemetry.Firmware ?? fallback.Firmware,
            AudioFormat = telemetry.AudioFormat ?? fallback.AudioFormat,
            AudioSampleRate = telemetry.AudioSampleRate ?? fallback.AudioSampleRate,
            InputSource = telemetry.InputSource ?? fallback.InputSource,
            UsbHostProtocol = telemetry.UsbHostProtocol ?? fallback.UsbHostProtocol,
            HdcpMode = telemetry.HdcpMode ?? fallback.HdcpMode,
            HdcpVersion = telemetry.HdcpVersion ?? fallback.HdcpVersion,
            RxTxHdcpVersion = telemetry.RxTxHdcpVersion ?? fallback.RxTxHdcpVersion,
            RawTimingHex = telemetry.RawTimingHex ?? fallback.RawTimingHex,
            DetailEntries = telemetry.DetailEntries.Count > 0
                ? telemetry.DetailEntries
                : fallback.DetailEntries,
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
        if (_isDisposed != 0)
        {
            throw new ObjectDisposedException(nameof(CaptureService));
        }
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0) return;
        try
        {
            Task.Run(() => CleanupAsync(CancellationToken.None)).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Logger.Log($"CaptureService.Dispose cleanup warning: {ex.Message}");
        }

        _sessionTransitionLock.Dispose();
        _sessionState = CaptureSessionState.Disposed;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0) return;
        try
        {
            await CleanupAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"CaptureService.DisposeAsync cleanup warning: {ex.Message}");
        }

        _sessionTransitionLock.Dispose();
        _sessionState = CaptureSessionState.Disposed;
    }
}


