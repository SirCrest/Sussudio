using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Windows.Storage;
using Sussudio.Services.Audio;
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio.Services.Capture;

// High-level capture orchestrator. It owns the lifetime of video capture,
// WASAPI capture/playback, recording sinks, Flashback backend pieces, source
// telemetry, and the snapshots consumed by automation. CaptureSessionCoordinator
// serializes public transitions; this class enforces the actual resource order.
public partial class CaptureService : IDisposable, IAsyncDisposable
{
    private readonly SemaphoreSlim _sessionTransitionLock = new(1, 1);
    // Lock ordering: acquire _sessionTransitionLock before _flashbackBackendLeaseLock.
    private readonly SemaphoreSlim _flashbackBackendLeaseLock = new(1, 1);
    private readonly ISourceSignalTelemetryProvider _sourceTelemetryProvider;
    private readonly IProcessSupervisor _processSupervisor;
    private readonly RecordingArtifactManager _artifactManager = new();

    private int _isDisposed;
    private bool _isInitialized;
    // REVIEWED 2026-04-07: writes serialized by _sessionTransitionLock;
    // unsync reads from UI thread produce at-worst one-frame-stale value (no crash/corruption).
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
    private readonly FlashbackBackendResources _flashbackBackend = new();
    private FlashbackBufferManager? _flashbackBufferManager
    {
        get => _flashbackBackend.BufferManager;
        set => _flashbackBackend.BufferManager = value;
    }

    private FlashbackEncoderSink? _flashbackSink
    {
        get => _flashbackBackend.Sink;
        set => _flashbackBackend.Sink = value;
    }

    private FlashbackExporter? _flashbackExporter
    {
        get => _flashbackBackend.Exporter;
        set => _flashbackBackend.Exporter = value;
    }

    private FlashbackPlaybackController? _flashbackPlaybackController
    {
        get => _flashbackBackend.PlaybackController;
        set => _flashbackBackend.PlaybackController = value;
    }

    private CaptureSettings? _flashbackBackendSettings
    {
        get => _flashbackBackend.SettingsSnapshot;
        set => _flashbackBackend.SettingsSnapshot = value;
    }

    // Flashback uses a preview-owned continuous encoder when the user is not
    // recording, but can also become the recording backend. These flags track
    // deferred enable/settings changes so recording stop can restore the safe
    // preview backend without mutating capture topology mid-recording.
    private volatile bool _flashbackEnabled = true;
    private bool _hasAv1Nvenc;
    private bool _pendingFlashbackSettingsChange;
    private bool _pendingFlashbackEnableAfterRecording;
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
    private int _flashbackCleanupInProgress;
    private int _flashbackRecordingStartInProgress;
    private int _flashbackRecordingFinalizeInProgress;
    private readonly object _recordingFailureTelemetryLock = new();
    private bool _lastRecordingEncodingFailed;
    private string? _lastRecordingEncodingFailureType;
    private string? _lastRecordingEncodingFailureMessage;
    private bool _lastFlashbackEncodingFailed;
    private string? _lastFlashbackEncodingFailureType;
    private string? _lastFlashbackEncodingFailureMessage;
    private long _sessionGeneration;
    private Task? _pendingLibAvDrainTask;
    private UnifiedVideoCapture? _unifiedVideoCapture;
    private RecordingContext? _recordingContext;

    // Recording finalization state is intentionally retained after stop so the
    // UI, automation, and verifier can explain what happened to the last file
    // even after capture resources have been torn down.
    private readonly Stopwatch _recordingStopwatch = new();
    private string? _lastOutputPath;
    private string _lastFinalizeStatus = "None";
    private DateTimeOffset? _lastFinalizeUtc;
    private IReadOnlyList<string> _lastPreservedArtifacts = Array.Empty<string>();
    private RecordingIntegritySummary _lastRecordingIntegrity = RecordingIntegritySummary.NotStarted;
    private RecordingIntegrityCounterSnapshot? _recordingIntegrityCounterBaseline;
    private RecordingAudioIntegrityCounterSnapshot? _recordingIntegrityAudioBaseline;
    private bool _lastUsePostMuxAudio;
    private FinalizeResult? _lastExportResult;
    private long _lastFlashbackExportResultId;
    private readonly SemaphoreSlim _flashbackExportOperationLock = new(1, 1);
    private readonly object _flashbackExportDiagnosticsLock = new();
    private bool _flashbackExportActive;
    private long _flashbackExportId;
    private string _flashbackExportStatus = "NotStarted";
    private string _flashbackExportOutputPath = string.Empty;
    private long _flashbackExportStartedUtcUnixMs;
    private long _flashbackExportLastProgressUtcUnixMs;
    private long _flashbackExportCompletedUtcUnixMs;
    private int _flashbackExportSegmentsProcessed;
    private int _flashbackExportTotalSegments;
    private double _flashbackExportPercent;
    private long _flashbackExportInPointMs;
    private long _flashbackExportOutPointMs;
    private string _flashbackExportMessage = string.Empty;
    private string _flashbackExportFailureKind = string.Empty;
    private long _flashbackExportForceRotateFallbacks;
    private long _flashbackExportLastForceRotateFallbackUtcUnixMs;
    private int _flashbackExportLastForceRotateFallbackSegments;
    private long _flashbackExportLastForceRotateFallbackInPointMs;
    private long _flashbackExportLastForceRotateFallbackOutPointMs;
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
    private readonly object _telemetryPollSync = new();

    // Telemetry is advisory and read-only: it gates UI choices and diagnostics
    // but must not block capture or recording. Polling has its own generation so
    // stale results from an old device/session cannot overwrite the live state.
    private CancellationTokenSource? _telemetryPollCts;
    private Task? _telemetryPollTask;
    private long _telemetryPollGeneration;
    private const int TelemetryPollIntervalMs = 500;
    private const int TelemetryPollStopDrainTimeoutMs = 750;

    // AV sync drift diagnostics
    private double _avSyncBaselineDriftMs = double.NaN;
    private double _avSyncPrevDriftMs;
    private long _avSyncPrevDriftTick;
    private double _avSyncDriftRateMsPerSec;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<Exception>? ErrorOccurred;
    public event Action? PreCleanupRequested;
    public event EventHandler<ulong>? FrameCaptured;
    public event EventHandler<AudioLevelEventArgs>? AudioLevelUpdated;
    public event EventHandler<AudioLevelEventArgs>? MicrophoneAudioLevelUpdated;
    public event EventHandler<SourceSignalTelemetrySnapshot>? SourceTelemetryUpdated;

    public bool IsRecording => _isRecording;
    public bool IsInitialized => _isInitialized;
    public bool IsVideoPreviewActive => _isVideoPreviewActive;
    public bool IsAudioPreviewActive => _isAudioPreviewActive;
    public CaptureSessionState SessionState => _sessionState;

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
            var previousSettings = _flashbackBackendSettings ?? _currentSettings;
            var flashbackBackendSettingsChanged = _flashbackSink != null &&
                previousSettings != null &&
                !CanReuseFlashbackBackend(previousSettings, settings);
            _currentSettings = settings;

            // Capture mic monitor settings for preview-time metering
            _micMonitorEnabled = settings.MicrophoneEnabled;
            _micMonitorDeviceId = settings.MicrophoneDeviceId;
            _micMonitorDeviceName = settings.MicrophoneDeviceName;

            if (_unifiedVideoCapture != null &&
                !_isRecording &&
                !CanReuseVideoCaptureForPreview(_unifiedVideoCapture, settings))
            {
                Logger.Log("PREVIEW_START recycle_pipeline=1 reason=settings_changed");
                await DisposePreviewPipelineAsync(transitionToken, purgeFlashbackSegments: true).ConfigureAwait(false);
            }

            if (_unifiedVideoCapture != null &&
                !_isRecording &&
                !_flashbackEnabled)
            {
                Logger.Log("PREVIEW_START recycle_pipeline=1 reason=flashback_disabled");
                await DisposePreviewPipelineAsync(transitionToken, purgeFlashbackSegments: false).ConfigureAwait(false);
            }

            if (_unifiedVideoCapture != null &&
                !_isRecording &&
                _flashbackSink != null &&
                flashbackBackendSettingsChanged)
            {
                Logger.Log("PREVIEW_START recycle_flashback=1 reason=flashback_settings_changed");
                await DisposeFlashbackPreviewBackendAsync(transitionToken, purgeSegments: true).ConfigureAwait(false);
            }

            // Fast-path: the capture pipeline is already running (recording active, or
            // flashback backend kept alive across a prior preview toggle). Just reattach
            // the preview renderer — no device re-init, no flashback restart.
            if (_unifiedVideoCapture != null &&
                (_isRecording || _flashbackEnabled))
            {
                // Guard: if an active flashback sink exists but its pixel format no longer
                // matches the freshly negotiated UVC format, the fast path must not silently
                // reuse the wrong backend. Fail hard so the caller tears down and rebuilds.
                if (_flashbackSink?.IsP010 is bool sinkIsP010 &&
                    sinkIsP010 != _unifiedVideoCapture.IsP010)
                {
                    Logger.Log(
                        $"FLASHBACK_FAST_PATH_FORMAT_MISMATCH " +
                        $"existing_p010={sinkIsP010} requested_p010={_unifiedVideoCapture.IsP010}");
                    throw new InvalidOperationException(
                        $"Flashback fast path: pixel-format mismatch — sink was built for " +
                        $"{(sinkIsP010 ? "P010" : "NV12")} but UVC session negotiated " +
                        $"{(_unifiedVideoCapture.IsP010 ? "P010" : "NV12")}. " +
                        "Rebuild the flashback backend with the correct format.");
                }

                Logger.Log($"PREVIEW_START fast_path=1 recording={_isRecording} flashback_alive={_flashbackSink != null}");
                _unifiedVideoCapture.SetPreviewSink(_previewFrameSink);
                TryApplySharedPreviewDevice(_unifiedVideoCapture, _previewFrameSink);
                if (!_isRecording && _flashbackEnabled && _flashbackSink == null)
                {
                    await EnsureFlashbackPreviewBackendAsync(_unifiedVideoCapture, settings, transitionToken).ConfigureAwait(false);
                }
                await EnsureFlashbackAudioInputsAsync(settings, transitionToken, "preview_fast_path").ConfigureAwait(false);
                _isVideoPreviewActive = true;
                // Telemetry may have been stopped via a recording-stop path while preview
                // was off; StartTelemetryPoll is idempotent (stops any prior timer first).
                StartTelemetryPoll();
                StatusChanged?.Invoke(this, "Preview started");
                return;
            }

            ThrowIfPendingLibAvDrainTaskBlocksReentry();

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
                    WasapiAudioCapture? micCapture = null;
                    try
                    {
                        micCapture = new WasapiAudioCapture();
                        await micCapture.InitializeAsync(_micMonitorDeviceId, transitionToken).ConfigureAwait(false);
                        micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
                        micCapture.CaptureFailed += OnWasapiCaptureFailed;
                        micCapture.Start();
                        if (_flashbackSink is { MicrophoneEnabled: true } fbSink)
                        {
                            micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
                            Logger.Log("FLASHBACK_MIC_ATTACH_OK reason='preview_mic_monitor_start'");
                        }
                        _microphoneCapture = micCapture;
                        micCapture = null;
                        Logger.Log("MIC_MONITOR_START device='" + (_micMonitorDeviceName ?? "?") + "'");
                    }
                    catch (OperationCanceledException) when (transitionToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception micEx)
                    {
                        Logger.Log("Mic monitor start failed (non-fatal): " + micEx.Message);
                    }
                    finally
                    {
                        if (micCapture != null)
                        {
                            micCapture.AudioLevelUpdated -= OnMicrophoneAudioLevelUpdated;
                            micCapture.CaptureFailed -= OnWasapiCaptureFailed;
                            try { await micCapture.DisposeAsync().ConfigureAwait(false); }
                            catch (Exception disposeEx) { Logger.Log($"MIC_MONITOR_PREVIEW_START_DISPOSE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }
                        }
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
                var previewStartRollbackToken = CancellationToken.None;
                await DisposeFlashbackPreviewBackendAsync(previewStartRollbackToken).ConfigureAwait(false);
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
        => StopVideoPreviewCoreAsync(teardownPipeline: false, cancellationToken);

    public Task StopVideoPreviewWithTeardownAsync(CancellationToken cancellationToken = default)
        => StopVideoPreviewCoreAsync(teardownPipeline: true, cancellationToken);

    private Task StopVideoPreviewCoreAsync(bool teardownPipeline, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Ready, async transitionToken =>
        {
            if (!_isVideoPreviewActive) return;
            transitionToken.ThrowIfCancellationRequested();

            var commitStoppedState = false;
            Exception? stopFailure = null;
            try
            {
                // Invariant: preview lifecycle must not affect the recording/flashback pipeline.
                // Keep the capture + flashback backend alive across preview toggles unless the
                // caller explicitly requests a full teardown (reinit, shutdown, settings change).
                var keepPipelineAlive = !teardownPipeline &&
                    (_isRecording || (_flashbackEnabled && _flashbackSink != null));

                if (keepPipelineAlive)
                {
                    Logger.Log($"PREVIEW_STOP keep_pipeline_alive=1 recording={_isRecording} flashback_alive={_flashbackSink != null}");
                    _unifiedVideoCapture?.SetPreviewSink(null);
                }
                else
                {
                    await DisposePreviewPipelineAsync(transitionToken, purgeFlashbackSegments: false).ConfigureAwait(false);
                }

                commitStoppedState = true;
            }
            catch (OperationCanceledException) when (transitionToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                stopFailure = ex;
                commitStoppedState = true;
                throw;
            }
            finally
            {
                if (commitStoppedState)
                {
                    _isVideoPreviewActive = false;
                    if (!_isRecording)
                    {
                        try
                        {
                            await StopTelemetryPollAsync().ConfigureAwait(false);
                        }
                        catch (Exception ex) when (stopFailure != null)
                        {
                            Logger.Log($"PREVIEW_STOP_TELEMETRY_WARN type={ex.GetType().Name} msg='{ex.Message}'");
                        }
                    }
                }
            }

            StatusChanged?.Invoke(this, "Preview stopped");
        }, cancellationToken);

    private bool CanReuseVideoCaptureForPreview(UnifiedVideoCapture capture, CaptureSettings settings)
    {
        var hdrRequested = HdrOutputPolicy.IsEnabled(settings);
        return capture.Width == (int)settings.Width &&
               capture.Height == (int)settings.Height &&
               Math.Abs(capture.Fps - settings.FrameRate) < 0.01 &&
               capture.IsP010 == hdrRequested &&
               capture.IsHighFrameRateMjpegMode == settings.UseMjpegHighFrameRateMode;
    }

    private static bool CanReuseFlashbackBackend(CaptureSettings current, CaptureSettings next)
    {
        var currentHdr = HdrOutputPolicy.IsEnabled(current);
        var nextHdr = HdrOutputPolicy.IsEnabled(next);
        if (currentHdr != nextHdr)
        {
            Logger.Log(
                $"FLASHBACK_REUSE_REJECTED reason=hdr_mismatch existing={currentHdr} requested={nextHdr}");
            return false;
        }

        return current.Format == next.Format &&
               current.Quality == next.Quality &&
               Math.Abs(current.CustomBitrateMbps - next.CustomBitrateMbps) < 0.01 &&
               current.NvencPreset == next.NvencPreset &&
               current.SplitEncodeMode == next.SplitEncodeMode &&
               current.AudioEnabled == next.AudioEnabled &&
               current.MicrophoneEnabled == next.MicrophoneEnabled &&
               current.FlashbackBufferMinutes == next.FlashbackBufferMinutes &&
               current.FlashbackGpuDecode == next.FlashbackGpuDecode;
    }

    private static CaptureSettings CloneCaptureSettings(CaptureSettings source)
    {
        return new CaptureSettings
        {
            Width = source.Width,
            Height = source.Height,
            FrameRate = source.FrameRate,
            RequestedFrameRateArg = source.RequestedFrameRateArg,
            RequestedFrameRateNumerator = source.RequestedFrameRateNumerator,
            RequestedFrameRateDenominator = source.RequestedFrameRateDenominator,
            RequestedPixelFormat = source.RequestedPixelFormat,
            Format = source.Format,
            Quality = source.Quality,
            NvencPreset = source.NvencPreset,
            SplitEncodeMode = source.SplitEncodeMode,
            CustomBitrateMbps = source.CustomBitrateMbps,
            HdrEnabled = source.HdrEnabled,
            HdrOutputMode = source.HdrOutputMode,
            HdrNominalPeakNits = source.HdrNominalPeakNits,
            HdrMaxCll = source.HdrMaxCll,
            HdrMaxFall = source.HdrMaxFall,
            HdrMasterDisplayMetadata = source.HdrMasterDisplayMetadata,
            PreviewMode = source.PreviewMode,
            OutputPath = source.OutputPath,
            AudioEnabled = source.AudioEnabled,
            UseCustomAudioInput = source.UseCustomAudioInput,
            AudioDeviceId = source.AudioDeviceId,
            AudioDeviceName = source.AudioDeviceName,
            MicrophoneEnabled = source.MicrophoneEnabled,
            MicrophoneDeviceId = source.MicrophoneDeviceId,
            MicrophoneDeviceName = source.MicrophoneDeviceName,
            AudioPathMode = source.AudioPathMode,
            PipelineOptions = source.PipelineOptions,
            ForceMjpegDecode = source.ForceMjpegDecode,
            FlashbackGpuDecode = source.FlashbackGpuDecode,
            FlashbackBufferMinutes = source.FlashbackBufferMinutes,
            MjpegDecoderCount = source.MjpegDecoderCount
        };
    }

    private async Task DisposePreviewPipelineAsync(
        CancellationToken transitionToken,
        bool purgeFlashbackSegments)
    {
        ClearPendingLibAvDrainTaskIfCompletedSuccessfully();

        var unifiedVideoCapture = _unifiedVideoCapture;
        var videoCaptureCleanupDeferred = false;
        _unifiedVideoCapture = null;
        if (unifiedVideoCapture != null)
        {
            CacheMjpegTimingMetrics(unifiedVideoCapture);
            _lastMfSourceReaderFramesDelivered = unifiedVideoCapture.VideoFramesArrived;
            _lastMfSourceReaderFramesDropped = unifiedVideoCapture.VideoFramesDropped;
            _lastMfSourceReaderNegotiatedFormat = unifiedVideoCapture.NegotiatedFormat;
            DetachUnifiedVideoCapture(unifiedVideoCapture);
            try
            {
                unifiedVideoCapture.SetPreviewSink(null);
                unifiedVideoCapture.SetFlashbackSink(null);
            }
            catch (Exception ex)
            {
                Logger.Log($"PREVIEW_PIPELINE_VIDEO_DETACH_WARN type={ex.GetType().Name} msg='{ex.Message}'");
            }

            if (_pendingLibAvDrainTask is { IsCompleted: false } pendingLibAvDrainTask)
            {
                _pendingLibAvDrainTask = ScheduleDeferredUnifiedVideoCaptureCleanup(
                    pendingLibAvDrainTask,
                    unifiedVideoCapture,
                    reason: "dispose_preview_pipeline_after_deferred_recording");
                videoCaptureCleanupDeferred = true;
            }
            else
            {
                Logger.Log("PREVIEW_PIPELINE_VIDEO_STOP_BEFORE_FLASHBACK_DISPOSE");
                await unifiedVideoCapture.StopAsync().ConfigureAwait(false);
            }
        }

        await DisposeFlashbackPreviewBackendAsync(
                transitionToken,
                purgeSegments: _flashbackBackend.ResolveSegmentPurge(
                    purgeFlashbackSegments,
                    "preview_pipeline_dispose"))
            .ConfigureAwait(false);

        if (unifiedVideoCapture != null && !videoCaptureCleanupDeferred)
        {
            await unifiedVideoCapture.DisposeForPreviewReinitAsync().ConfigureAwait(false);
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
            _currentSettings = settings;
            _micMonitorEnabled = settings.MicrophoneEnabled;
            _micMonitorDeviceId = settings.MicrophoneDeviceId;
            _micMonitorDeviceName = settings.MicrophoneDeviceName;

            LibAvRecordingSink? libAvSink = null;
            IRecordingSink? recordingSink = null;
            WasapiAudioCapture? ownedWasapiAudioCapture = null;
            UnifiedVideoCapture? ownedUnifiedVideoCapture = null;
            RecordingContext? recordingContext = null;
            UnifiedVideoCapture? recordingVideoCapture = null;
            FlashbackEncoderSink? flashbackRecordingStartedSink = null;
            var flashbackRecordingBackendLeaseHeld = false;
            var sinkAttachedForAudioOnly = false;
            Volatile.Write(ref _wasapiAudioCaptureFaulted, false);
            Volatile.Write(ref _wasapiAudioCaptureFaultMessage, null);
            ThrowIfPendingLibAvDrainTaskBlocksReentry();
            try
            {
                if (_flashbackEnabled &&
                    _flashbackSink != null &&
                    !_flashbackSink.CanBeginRecording)
                {
                    Logger.Log(
                        "FLASHBACK_RECORDING_BACKEND_UNUSABLE_FALLBACK " +
                        $"failed={_flashbackSink.EncodingFailed} type={_flashbackSink.EncodingFailureType ?? "None"}");
                    await DisposeFlashbackPreviewBackendAsync(transitionToken, purgeSegments: true).ConfigureAwait(false);
                }

                // --- Unified path: piggyback on existing flashback NVENC session ---
                if (_flashbackEnabled && _flashbackSink != null)
                {
                    // Guard: if the existing flashback sink's pixel format no longer matches the
                    // negotiated UVC format, reject the reuse path so the slow path rebuilds correctly.
                    if (_flashbackSink.IsP010 is bool recSinkIsP010 &&
                        _unifiedVideoCapture != null &&
                        recSinkIsP010 != _unifiedVideoCapture.IsP010)
                    {
                        Logger.Log(
                            $"FLASHBACK_FAST_PATH_FORMAT_MISMATCH " +
                            $"existing_p010={recSinkIsP010} requested_p010={_unifiedVideoCapture.IsP010}");
                        throw new InvalidOperationException(
                            $"Flashback recording fast path: pixel-format mismatch — sink was built for " +
                            $"{(recSinkIsP010 ? "P010" : "NV12")} but UVC session negotiated " +
                            $"{(_unifiedVideoCapture.IsP010 ? "P010" : "NV12")}. " +
                            "Rebuild the flashback backend with the correct format.");
                    }

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
                    recordingContext = fbRecordingContext;

                    // If flashback settings changed while preview was stopped, rebuild
                    // before recording so the retained backend matches the requested file.
                    var flashbackBackendSettingsChanged = _flashbackBackendSettings == null ||
                        !CanReuseFlashbackBackend(_flashbackBackendSettings, settings);
                    var flashbackAudioTopologyChanged =
                        _flashbackSink.AudioEnabled != settings.AudioEnabled ||
                        _flashbackSink.MicrophoneEnabled != settings.MicrophoneEnabled;
                    if (flashbackAudioTopologyChanged)
                    {
                        Logger.Log($"FLASHBACK_RECORDING_TOPOLOGY_MISMATCH_REJECT " +
                            $"audio={settings.AudioEnabled} (was {_flashbackSink.AudioEnabled}) " +
                            $"mic={settings.MicrophoneEnabled} (was {_flashbackSink.MicrophoneEnabled})");
                        EnsureFlashbackRecordingTopologyMatches(
                            _flashbackSink,
                            settings.AudioEnabled,
                            settings.MicrophoneEnabled);
                    }

                    if (flashbackBackendSettingsChanged)
                    {
                        Logger.Log($"FLASHBACK_SETTINGS_MISMATCH_AUTO_RESTART " +
                            $"settings_changed={flashbackBackendSettingsChanged} " +
                            $"audio={settings.AudioEnabled} " +
                            $"mic={settings.MicrophoneEnabled}");

                        await DisposeFlashbackPreviewBackendAsync(transitionToken, purgeSegments: true).ConfigureAwait(false);

                        var uvc = _unifiedVideoCapture;
                        if (uvc != null)
                        {
                            await EnsureFlashbackPreviewBackendAsync(uvc, settings, transitionToken).ConfigureAwait(false);
                        }

                        if (_flashbackSink == null)
                        {
                            throw new InvalidOperationException("Failed to restart flashback backend for updated recording settings.");
                        }
                    }

                    await EnsureFlashbackAudioInputsAsync(settings, transitionToken, "recording_flashback_start").ConfigureAwait(false);
                    await _flashbackBackendLeaseLock.WaitAsync(transitionToken).ConfigureAwait(false);
                    flashbackRecordingBackendLeaseHeld = true;
                    Volatile.Write(ref _flashbackRecordingStartInProgress, 1);
                    try
                    {
                        var activeFlashbackSink = _flashbackSink
                            ?? throw new InvalidOperationException("Flashback backend is not available for recording.");
                        if (!activeFlashbackSink.CanBeginRecording)
                        {
                            throw new InvalidOperationException("Flashback backend is not healthy enough to begin recording.");
                        }

                        if (!activeFlashbackSink.WaitForForceRotateIdle(TimeSpan.FromSeconds(10)))
                        {
                            throw new InvalidOperationException("Flashback backend export rotation did not quiesce before recording start.");
                        }

                        if (!activeFlashbackSink.CanBeginRecording)
                        {
                            throw new InvalidOperationException("Flashback backend became unavailable before recording start.");
                        }

                        flashbackRecordingStartedSink = activeFlashbackSink;
                        _recordingIntegrityCounterBaseline = CaptureRecordingIntegrityCounters(activeFlashbackSink);
                        _recordingIntegrityAudioBaseline = CaptureRecordingAudioCounters(
                            _wasapiAudioCapture,
                            activeFlashbackSink,
                            settings);
                        activeFlashbackSink.BeginRecording(fbRecordingContext.FinalOutputPath);
                        if (activeFlashbackSink.EncodingFailed)
                        {
                            throw new InvalidOperationException(
                                $"Flashback backend failed while starting recording: {activeFlashbackSink.EncodingFailureMessage ?? "unknown error"}");
                        }

                        _unifiedVideoCapture?.BeginFlashbackRecordingAccounting();
                        _recordingSink = activeFlashbackSink;
                        _libavSink = null;
                        _recordingContext = fbRecordingContext;
                        _activeRecordingSettings = settings;
                        ClearLastRecordingFailure();
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
                    finally
                    {
                        Volatile.Write(ref _flashbackRecordingStartInProgress, 0);
                        if (flashbackRecordingBackendLeaseHeld)
                        {
                            flashbackRecordingBackendLeaseHeld = false;
                            ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, "flashback_recording_start");
                        }
                    }
                }

                // --- Standard path: create dedicated LibAvRecordingSink ---
                libAvSink = new LibAvRecordingSink();
                libAvSink.OnEncodingFailed = OnRecordingBackendFatalError;
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

                _recordingIntegrityCounterBaseline = CaptureRecordingIntegrityCounters(activeLibAvSink);
                _recordingIntegrityAudioBaseline = CaptureRecordingAudioCounters(
                    _wasapiAudioCapture,
                    activeLibAvSink,
                    settings);
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
                ClearLastRecordingFailure();
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
                Logger.Log($"CAPTURE_RECORDING_START_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
                RecordLastRecordingFailure(ex);

                if (flashbackRecordingStartedSink != null)
                {
                    try
                    {
                        flashbackRecordingStartedSink.CancelRecordingStartRollback("start_recording_failed");
                    }
                    catch (Exception rollbackEx)
                    {
                        Logger.Log($"FLASHBACK_RECORDING_START_ROLLBACK_WARN type={rollbackEx.GetType().Name} error='{rollbackEx.Message}'");
                    }

                    _unifiedVideoCapture?.EndFlashbackRecordingAccounting();
                    if (ReferenceEquals(_recordingSink, flashbackRecordingStartedSink))
                    {
                        _recordingSink = null;
                    }
                }

                Volatile.Write(ref _flashbackRecordingStartInProgress, 0);
                if (flashbackRecordingBackendLeaseHeld)
                {
                    flashbackRecordingBackendLeaseHeld = false;
                    ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, "flashback_recording_start_fail");
                }

                if (sinkAttachedForAudioOnly && _wasapiAudioCapture != null)
                {
                    _wasapiAudioCapture.DetachRecordingSink();
                }

                await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);

                if (ownedUnifiedVideoCapture != null)
                {
                    DetachUnifiedVideoCapture(ownedUnifiedVideoCapture);
                }

                try
                {
                    await _artifactManager.RollbackAsync(recordingContext).ConfigureAwait(false);
                }
                catch (Exception rollbackEx)
                {
                    Logger.Log($"Recording start rollback cleanup failed: {rollbackEx.Message}");
                }

                try
                {
                    await DisposeTransientRecordingBackendAsync(
                        recordingSink,
                        ownedWasapiAudioCapture,
                        ownedUnifiedVideoCapture).ConfigureAwait(false);
                }
                catch (Exception disposeEx)
                {
                    Logger.Log($"Transient recording backend cleanup failed during start rollback: {disposeEx.Message}");
                }

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
                _recordingIntegrityCounterBaseline = null;
                _recordingIntegrityAudioBaseline = null;
                _isRecording = false;
                _recordingStopwatch.Reset();
                _mfConvertersDisabled = false;
                throw;
            }
        }, cancellationToken);

    // Public path used by normal recording-stop (UI Stop button, automation StopRecording).
    public Task StopRecordingAsync(CancellationToken cancellationToken = default)
        => StopRecordingAsync(emergency: false, cancellationToken);

    // Internal overload used by CaptureSessionCoordinator.StopRecordingForEmergencyAsync.
    // Threads `emergency` through StopAndDisposeRecordingBackendAsync to LibAvRecordingSink
    // so the sink applies EmergencyStopTimeoutMs (5s) instead of StopTimeoutMs (30s) — fits
    // inside App.TryEmergencyStopRecording's 8s wrapper (fix #12).
    internal Task StopRecordingAsync(bool emergency, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Ready, async transitionToken =>
        {
            if (!_isRecording && _recordingSink == null && _libavSink == null)
            {
                return;
            }

            var result = await StopAndDisposeRecordingBackendAsync("Stopped", emergency, transitionToken).ConfigureAwait(false);
            // Preview continues running on the active source-reader/WASAPI sessions - no resume needed.
            StatusChanged?.Invoke(this, result.StatusMessage);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(result.StatusMessage);
            }
        }, cancellationToken);

    public Task CleanupAsync(CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.CleaningUp, CleanupCoreAsync, cancellationToken);

    private async Task CleanupCoreAsync(CancellationToken transitionToken)
    {
        var cancellationRequested = false;
        var preserveFlashbackSegmentsAfterFailedRecordingFinalize = false;
        if (_isRecording || _recordingSink != null || _libavSink != null)
        {
            var stoppingFlashbackRecording = IsFlashbackRecordingBackendActive();
            try
            {
                var result = await StopAndDisposeRecordingBackendAsync(
                    "Stopped during cleanup",
                    emergency: false,
                    transitionToken).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    Logger.Log($"Cleanup stop reported issues: {result.StatusMessage}");
                    if (stoppingFlashbackRecording)
                    {
                        _flashbackBackend.PreserveRecoverySegments("cleanup_stop_failed");
                        preserveFlashbackSegmentsAfterFailedRecordingFinalize = true;
                    }
                }
            }
            catch (OperationCanceledException) when (transitionToken.IsCancellationRequested)
            {
                cancellationRequested = true;
                if (stoppingFlashbackRecording)
                {
                    _flashbackBackend.PreserveRecoverySegments("cleanup_stop_cancelled");
                    preserveFlashbackSegmentsAfterFailedRecordingFinalize = true;
                }
            }
        }

        ClearPendingLibAvDrainTaskIfCompletedSuccessfully();

        try
        {
            if (preserveFlashbackSegmentsAfterFailedRecordingFinalize)
            {
                Logger.Log("FLASHBACK_CLEANUP_PRESERVE_SEGMENTS reason=recording_finalize_failed");
            }

            await DisposeFlashbackPreviewBackendAsync(
                    transitionToken,
                    purgeSegments: !preserveFlashbackSegmentsAfterFailedRecordingFinalize)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_CLEANUP_DISPOSE_WARN type={ex.GetType().Name} msg='{ex.Message}'");
        }

        var pendingLibAvDrainTask = _pendingLibAvDrainTask;
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
                if (pendingLibAvDrainTask is { IsCompleted: false })
                {
                    _pendingLibAvDrainTask = ScheduleDeferredUnifiedVideoCaptureCleanup(
                        pendingLibAvDrainTask,
                        unifiedVideoCapture,
                        reason: "cleanup_after_deferred_recording");
                }
                else
                {
                    await unifiedVideoCapture.StopAsync().ConfigureAwait(false);
                    await unifiedVideoCapture.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_CLEANUP_UNIFIED_VIDEO_WARN type={ex.GetType().Name} msg='{ex.Message}'");
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
                Logger.Log($"FLASHBACK_CLEANUP_WASAPI_WARN type={ex.GetType().Name} msg='{ex.Message}'");
            }
        }

        await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);

        await StopTelemetryPollAsync().ConfigureAwait(false);
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
    }

}
