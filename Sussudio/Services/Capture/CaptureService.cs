using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    private FlashbackBufferManager? _flashbackBufferManager;
    private FlashbackEncoderSink? _flashbackSink;
    private FlashbackExporter? _flashbackExporter;
    private FlashbackPlaybackController? _flashbackPlaybackController;
    private CaptureSettings? _flashbackBackendSettings;
    private volatile bool _flashbackEnabled = true;
    private bool _hasAv1Nvenc;
    private bool _pendingFlashbackSettingsChange;
    private bool _pendingFlashbackEnableAfterRecording;
    private bool _preserveFlashbackSegmentsAfterFailedRecordingFinalize;
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
    private IPreviewFrameSink? _previewFrameSink;
    private RecordingContext? _recordingContext;
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
    // Tracks the most recent codec-downgrade reason logged so we don't spam the
    // FLASHBACK_CODEC_DOWNGRADE message every time CreateFlashbackSessionContext
    // is rebuilt with the same inputs. Reset to null when conditions clear.
    private string? _lastLoggedFlashbackDowngradeReason;
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
    private readonly object _telemetryPollSync = new();
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

    public bool IsFlashbackActive => _flashbackSink != null;
    public TimeSpan FlashbackBufferedDuration => _flashbackBufferManager?.BufferedDuration ?? TimeSpan.Zero;
    public long FlashbackDiskBytes => _flashbackBufferManager?.TotalDiskBytes ?? 0;
    public int FlashbackSegmentCount => _flashbackBufferManager?.SegmentCount ?? 0;
    internal FlashbackPlaybackController? FlashbackPlaybackController => _flashbackPlaybackController;
    internal FlashbackBufferManager? FlashbackBufferManager => _flashbackBufferManager;
    public long FlashbackOutputBytes => _flashbackSink?.OutputBytes ?? 0;
    public long FlashbackTotalBytesWritten => _flashbackBufferManager?.TotalBytesWritten ?? 0;
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

    public Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        => RunTransitionAsync(_sessionState, async transitionToken =>
        {
            if (_isRecording && IsFlashbackRecordingBackendActive() && !enabled)
            {
                Logger.Log("FLASHBACK_DISABLE_BLOCKED reason=recording_active");
                throw new InvalidOperationException("Cannot disable Flashback while Flashback recording is active.");
            }

            if (_flashbackEnabled == enabled)
            {
                if (enabled && (_flashbackSink != null || _isRecording))
                {
                    return;
                }

                if (!enabled &&
                    _flashbackSink == null &&
                    _flashbackBufferManager == null &&
                    _flashbackExporter == null &&
                    _flashbackPlaybackController == null)
                {
                    return;
                }
            }

            _flashbackEnabled = enabled;
            if (!enabled)
            {
                _pendingFlashbackEnableAfterRecording = false;
                await DisposeFlashbackPreviewBackendAsync(transitionToken, purgeSegments: true).ConfigureAwait(false);
                if (!_isVideoPreviewActive && !_isAudioPreviewActive && !_isRecording)
                {
                    await DisposePreviewPipelineAsync(transitionToken, purgeFlashbackSegments: false).ConfigureAwait(false);
                }
                return;
            }

            if (_isRecording)
            {
                _pendingFlashbackEnableAfterRecording = true;
                Logger.Log("FLASHBACK_ENABLE_DEFERRED reason=recording_active");
                return;
            }

            _pendingFlashbackEnableAfterRecording = false;
            if (_unifiedVideoCapture != null && _currentSettings != null)
            {
                try
                {
                    await EnsureFlashbackPreviewBackendAsync(_unifiedVideoCapture, _currentSettings, transitionToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex) when (transitionToken.IsCancellationRequested)
                {
                    _flashbackEnabled = false;
                    _pendingFlashbackEnableAfterRecording = false;
                    if (_flashbackSink != null || _flashbackBufferManager != null || _flashbackExporter != null || _flashbackPlaybackController != null)
                    {
                        await DisposeFlashbackPreviewBackendAsync(CancellationToken.None, purgeSegments: true).ConfigureAwait(false);
                    }
                    Logger.Log($"FLASHBACK_ENABLE_IMMEDIATE_CANCELLED type={ex.GetType().Name} error='{ex.Message}'");
                    throw;
                }
                catch (Exception ex)
                {
                    _flashbackEnabled = false;
                    _pendingFlashbackEnableAfterRecording = false;
                    if (_flashbackSink != null || _flashbackBufferManager != null || _flashbackExporter != null || _flashbackPlaybackController != null)
                    {
                        await DisposeFlashbackPreviewBackendAsync(CancellationToken.None, purgeSegments: true).ConfigureAwait(false);
                    }
                    Logger.Log($"FLASHBACK_ENABLE_IMMEDIATE_FAIL type={ex.GetType().Name} error='{ex.Message}'");
                    throw;
                }
            }
        }, cancellationToken);

    /// <summary>
    /// Updates flashback-specific fields in the active capture settings without
    /// requiring a full session restart. Call before <see cref="RestartFlashbackAsync"/>
    /// so the rebuild uses the latest values.
    /// </summary>
    // REVIEWED 2026-04-07: called from UI thread only; values are independent scalars
    // so a stale read from a background thread produces a slightly-off config, not a crash.
    // RestartFlashbackAsync (which consumes these) acquires _sessionTransitionLock.
    public Task UpdateFlashbackSettingsAsync(
        int bufferMinutes,
        bool gpuDecode,
        CancellationToken cancellationToken = default)
        => RunTransitionAsync(_sessionState, transitionToken =>
        {
            if (_currentSettings != null)
            {
                _currentSettings.FlashbackBufferMinutes = bufferMinutes;
                _currentSettings.FlashbackGpuDecode = gpuDecode;
            }

            if (_flashbackPlaybackController != null)
            {
                _flashbackPlaybackController.GpuDecodeEnabled = gpuDecode;
            }

            if (_isRecording && IsFlashbackRecordingBackendActive())
            {
                _pendingFlashbackSettingsChange = true;
            }

            return Task.CompletedTask;
        }, cancellationToken);

    /// <summary>
    /// Updates encoding-related fields in the active capture settings so that
    /// <see cref="RestartFlashbackAsync"/> picks up the latest bitrate/quality/preset.
    /// </summary>
    // REVIEWED 2026-04-07: same threading rationale as UpdateFlashbackSettings above.
    public void UpdateEncodingSettings(CaptureSettings source)
    {
        if (_currentSettings == null) return;
        _currentSettings.Format = source.Format;
        _currentSettings.Quality = source.Quality;
        _currentSettings.NvencPreset = source.NvencPreset;
        _currentSettings.CustomBitrateMbps = source.CustomBitrateMbps;
        _currentSettings.AudioEnabled = source.AudioEnabled;
        _currentSettings.MicrophoneEnabled = source.MicrophoneEnabled;
        _currentSettings.MicrophoneDeviceId = source.MicrophoneDeviceId;
        _currentSettings.MicrophoneDeviceName = source.MicrophoneDeviceName;
        _currentSettings.FlashbackBufferMinutes = source.FlashbackBufferMinutes;
        _currentSettings.FlashbackGpuDecode = source.FlashbackGpuDecode;
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
                throw new InvalidOperationException("Cannot restart Flashback while Flashback recording is active.");
            }

            await RestartFlashbackCoreAsync(transitionToken).ConfigureAwait(false);
        }, cancellationToken);

    public Task RestartFlashbackAsync(CaptureSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return RunTransitionAsync(_sessionState, async transitionToken =>
        {
            if (_isRecording && IsFlashbackRecordingBackendActive())
            {
                Logger.Log("FLASHBACK_RESTART_BLOCKED reason=recording_active");
                throw new InvalidOperationException("Cannot restart Flashback while Flashback recording is active.");
            }

            UpdateEncodingSettings(settings);
            await RestartFlashbackCoreAsync(transitionToken).ConfigureAwait(false);
        }, cancellationToken);
    }

    private async Task RestartFlashbackCoreAsync(CancellationToken cancellationToken)
    {
        await DisposeFlashbackPreviewBackendAsync(cancellationToken, purgeSegments: true).ConfigureAwait(false);

        var unifiedVideoCapture = _unifiedVideoCapture;
        var settings = _currentSettings;
        if (!_flashbackEnabled || unifiedVideoCapture == null || settings == null)
        {
            Logger.Log($"FLASHBACK_RESTART_TEARDOWN_ONLY enabled={_flashbackEnabled} capture={unifiedVideoCapture != null} settings={settings != null}");
            return;
        }

        await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, cancellationToken).ConfigureAwait(false);
        Logger.Log("FLASHBACK_RESTART_OK");
    }

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

            _currentSettings.Format = format;

            var cycleFailed = false;
            if (_flashbackSink != null)
            {
                try
                {
                    await CycleFlashbackBufferAsync(transitionToken, purgeSegments: true).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex) when (transitionToken.IsCancellationRequested)
                {
                    Logger.Log($"FLASHBACK_FORMAT_CHANGE_CYCLE_CANCELLED format={format} type={ex.GetType().Name} error='{ex.Message}'");
                    throw;
                }
                catch (Exception ex)
                {
                    cycleFailed = true;
                    Logger.Log($"FLASHBACK_FORMAT_CHANGE_CYCLE_FAIL format={format} type={ex.GetType().Name} error='{ex.Message}'");
                }
            }

            if (!cycleFailed)
            {
                Logger.Log($"FLASHBACK_FORMAT_CHANGE_OK format={format}");
            }
        }, cancellationToken);

    /// <summary>
    /// Cycles the flashback encoder when encoder-affecting settings change
    /// (bitrate, quality, preset). Updates <see cref="_currentSettings"/> and
    /// restarts the flashback buffer so new recordings use the updated params.
    /// No-op if not previewing or recording is active.
    /// </summary>
    public Task CycleFlashbackEncoderSettingsAsync(
        VideoQuality? quality = null,
        double? customBitrateMbps = null,
        string? nvencPreset = null,
        CancellationToken cancellationToken = default)
        => RunTransitionAsync(_sessionState, async transitionToken =>
        {
            if (_currentSettings == null) return;

            var changed = false;
            if (quality.HasValue && quality.Value != _currentSettings.Quality)
            {
                _currentSettings.Quality = quality.Value;
                changed = true;
            }
            if (customBitrateMbps.HasValue && Math.Abs(customBitrateMbps.Value - _currentSettings.CustomBitrateMbps) > 0.01)
            {
                _currentSettings.CustomBitrateMbps = customBitrateMbps.Value;
                changed = true;
            }
            if (nvencPreset != null && !string.Equals(nvencPreset, _currentSettings.NvencPreset, StringComparison.OrdinalIgnoreCase))
            {
                _currentSettings.NvencPreset = nvencPreset;
                changed = true;
            }

            if (!changed) return;

            if (_isRecording)
            {
                Logger.Log("FLASHBACK_ENCODER_SETTINGS_CHANGE_BLOCKED reason=recording_active");
                if (IsFlashbackRecordingBackendActive())
                    _pendingFlashbackSettingsChange = true;
                return;
            }

            var cycledBuffer = _flashbackSink != null;
            var cycleFailed = false;
            if (_flashbackSink != null)
            {
                try
                {
                    await CycleFlashbackBufferAsync(transitionToken, purgeSegments: true).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex) when (transitionToken.IsCancellationRequested)
                {
                    Logger.Log($"FLASHBACK_ENCODER_SETTINGS_CHANGE_CYCLE_CANCELLED quality={_currentSettings.Quality} bitrate={_currentSettings.CustomBitrateMbps} preset={_currentSettings.NvencPreset} type={ex.GetType().Name} error='{ex.Message}'");
                    throw;
                }
                catch (Exception ex)
                {
                    cycleFailed = true;
                    Logger.Log($"FLASHBACK_ENCODER_SETTINGS_CHANGE_CYCLE_FAIL quality={_currentSettings.Quality} bitrate={_currentSettings.CustomBitrateMbps} preset={_currentSettings.NvencPreset} type={ex.GetType().Name} error='{ex.Message}'");
                }
            }

            if (!cycleFailed)
            {
                Logger.Log($"FLASHBACK_ENCODER_SETTINGS_CHANGE_OK quality={_currentSettings.Quality} bitrate={_currentSettings.CustomBitrateMbps} preset={_currentSettings.NvencPreset} cycled={cycledBuffer}");
            }
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
        // Snapshot buffer state under the session lock, then release it.
        // PauseEviction (inside ExportFlashbackCoreAsync) protects segment files
        // from deletion — the session lock only needs to be held long enough to
        // read consistent references, not for the entire FFmpeg export.
        FlashbackBufferManager? bufferManager;
        FlashbackEncoderSink? flashbackSink;
        FlashbackExporter? flashbackExporter;
        var sessionLockHeld = false;
        var backendLeaseHeld = false;
        try
        {
            await _sessionTransitionLock.WaitAsync(ct).ConfigureAwait(false);
            sessionLockHeld = true;

            if (_isRecording && IsFlashbackRecordingBackendActive())
            {
                Logger.Log("FLASHBACK_EXPORT_REJECTED reason=flashback_recording_active");
                return FailFlashbackExport(outputPath, "Flashback export is unavailable while Flashback is the active recording backend.");
            }

            await _flashbackBackendLeaseLock.WaitAsync(ct).ConfigureAwait(false);
            backendLeaseHeld = true;
            bufferManager = _flashbackBufferManager;
            flashbackSink = _flashbackSink;
            flashbackExporter = bufferManager != null
                ? _flashbackExporter ??= new FlashbackExporter()
                : _flashbackExporter;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);
            return FailFlashbackExport(outputPath, "Flashback export cancelled.");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_SNAPSHOT_FAIL op=range type={ex.GetType().Name} msg='{ex.Message}'");
            ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);
            throw;
        }
        finally
        {
            ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);
            if (sessionLockHeld)
            {
                ReleaseSemaphoreBestEffort(_sessionTransitionLock, "flashback_export_snapshot_session");
            }
        }

        return await ExportFlashbackCoreAsync(
                TimeSpan.Zero,
                TimeSpan.MaxValue,
                outputPath,
                progress,
                ct,
                snapshotSink: flashbackSink,
                snapshotBufferManager: bufferManager,
                snapshotExporter: flashbackExporter,
                resolveRangeAfterEvictionPaused: manager =>
                {
                    var validStart = manager.ValidStartPts;
                    var bufferedDuration = manager.BufferedDuration;
                    var bufferInPoint = ClampFlashbackBufferPosition(inPoint ?? TimeSpan.Zero, bufferedDuration);
                    var bufferOutPoint = outPoint.HasValue
                        ? ClampFlashbackBufferPosition(outPoint.Value, bufferedDuration)
                        : TimeSpan.MaxValue;
                    var fileInPoint = AddFlashbackPtsOffsetOrMax(bufferInPoint, validStart);
                    var fileOutPoint = AddFlashbackPtsOffsetOrMax(bufferOutPoint, validStart);
                    return fileOutPoint != TimeSpan.MaxValue && fileOutPoint <= fileInPoint
                        ? (false, fileInPoint, fileOutPoint, "Flashback export range is empty or invalid.")
                        : (true, fileInPoint, fileOutPoint, null);
                })
            .ConfigureAwait(false);
    }

    internal async Task<FinalizeResult> ExportFlashbackLastNSecondsAsync(
        double seconds, string outputPath,
        IProgress<ExportProgress>? progress, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            return FailFlashbackExport(outputPath, "Flashback export cancelled.");
        }

        if (!double.IsFinite(seconds) || seconds <= 0 || seconds > TimeSpan.MaxValue.TotalSeconds)
        {
            return FailFlashbackExport(outputPath, "Flashback export duration must be finite, greater than zero, and within TimeSpan range.");
        }

        // Same pattern: snapshot under lock, export outside it.
        FlashbackBufferManager? bufferManager;
        FlashbackEncoderSink? flashbackSink;
        FlashbackExporter? flashbackExporter;
        var sessionLockHeld = false;
        var backendLeaseHeld = false;
        try
        {
            await _sessionTransitionLock.WaitAsync(ct).ConfigureAwait(false);
            sessionLockHeld = true;

            if (_isRecording && IsFlashbackRecordingBackendActive())
            {
                Logger.Log("FLASHBACK_EXPORT_REJECTED reason=flashback_recording_active");
                return FailFlashbackExport(outputPath, "Flashback export is unavailable while Flashback is the active recording backend.");
            }

            await _flashbackBackendLeaseLock.WaitAsync(ct).ConfigureAwait(false);
            backendLeaseHeld = true;
            bufferManager = _flashbackBufferManager;
            flashbackSink = _flashbackSink;
            flashbackExporter = bufferManager != null
                ? _flashbackExporter ??= new FlashbackExporter()
                : _flashbackExporter;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);
            return FailFlashbackExport(outputPath, "Flashback export cancelled.");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_SNAPSHOT_FAIL op=last_n type={ex.GetType().Name} msg='{ex.Message}'");
            ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);
            throw;
        }
        finally
        {
            ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);
            if (sessionLockHeld)
            {
                ReleaseSemaphoreBestEffort(_sessionTransitionLock, "flashback_export_last_n_snapshot_session");
            }
        }

        return await ExportFlashbackCoreAsync(
                TimeSpan.Zero,
                TimeSpan.MaxValue,
                outputPath,
                progress,
                ct,
                snapshotSink: flashbackSink,
                snapshotBufferManager: bufferManager,
                snapshotExporter: flashbackExporter,
                resolveRangeAfterEvictionPaused: manager =>
                {
                    var bufferedDuration = manager.BufferedDuration;
                    var validStart = manager.ValidStartPts;
                    var rangeStart = bufferedDuration.TotalSeconds > seconds
                        ? TimeSpan.FromSeconds(bufferedDuration.TotalSeconds - seconds)
                        : TimeSpan.Zero;
                    var fileInPoint = AddFlashbackPtsOffsetOrMax(rangeStart, validStart);
                    return (true, fileInPoint, TimeSpan.MaxValue, null);
                })
            .ConfigureAwait(false);
    }

    private void ReleaseFlashbackBackendLeaseIfHeld(ref bool backendLeaseHeld)
    {
        if (!backendLeaseHeld)
        {
            return;
        }

        backendLeaseHeld = false;
        ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, "flashback_backend_lease");
    }

    private FinalizeResult FailFlashbackExport(
        string outputPath,
        string statusMessage,
        TimeSpan? inPoint = null,
        TimeSpan? outPoint = null)
    {
        var result = FinalizeResult.Failure(outputPath, statusMessage);
        Logger.Log($"FLASHBACK_EXPORT_REJECTED status='{statusMessage}' output='{outputPath}'");
        RecordRejectedFlashbackExportDiagnostics(outputPath, result, inPoint, outPoint);
        return result;
    }

    // Called from two contexts:
    // (1) Export methods — pass snapshotSink/snapshotBufferManager captured under session lock.
    // (2) FinalizeFlashbackRecordingAsync — runs under session lock, omits snapshots (field reads safe).
    private async Task<FinalizeResult> ExportFlashbackCoreAsync(
        TimeSpan inPoint, TimeSpan outPoint, string outputPath,
        IProgress<ExportProgress>? progress, CancellationToken ct,
        FlashbackEncoderSink? snapshotSink = null,
        FlashbackBufferManager? snapshotBufferManager = null,
        FlashbackExporter? snapshotExporter = null,
        bool requireCompleteLiveEdge = false,
        Func<FlashbackBufferManager, (bool Succeeded, TimeSpan InPoint, TimeSpan OutPoint, string? FailureMessage)>? resolveRangeAfterEvictionPaused = null)
    {
        var flashbackSink = snapshotSink ?? _flashbackSink;
        var bufferManager = snapshotBufferManager ?? _flashbackBufferManager;

        var exportId = 0L;
        var evictionPaused = false;
        var exportOperationLockHeld = false;
        try
        {
            try
            {
                await _flashbackExportOperationLock.WaitAsync(ct).ConfigureAwait(false);
                exportOperationLockHeld = true;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return FailFlashbackExport(outputPath, "Flashback export cancelled.", inPoint, outPoint);
            }

            if (bufferManager == null)
            {
                return FailFlashbackExport(outputPath, "Flashback buffer not active", inPoint, outPoint);
            }

            var exporter = snapshotExporter;
            if (exporter == null)
            {
                exporter = _flashbackExporter ??= new FlashbackExporter();
            }

            // Pause eviction so segments aren't deleted while the exporter reads them.
            // Range-based UI exports resolve relative buffer positions after this pause
            // so queued exports cannot use a stale valid-start snapshot.
            bufferManager.PauseEviction();
            evictionPaused = true;

            if (resolveRangeAfterEvictionPaused != null)
            {
                var resolvedRange = resolveRangeAfterEvictionPaused(bufferManager);
                inPoint = resolvedRange.InPoint;
                outPoint = resolvedRange.OutPoint;
                if (!resolvedRange.Succeeded)
                {
                    return FailFlashbackExport(
                        outputPath,
                        resolvedRange.FailureMessage ?? "Flashback export range is empty or invalid.",
                        inPoint,
                        outPoint);
                }
            }

            exportId = BeginFlashbackExportDiagnostics(inPoint, outPoint, outputPath);
            var diagnosticProgress = CreateFlashbackExportProgressSink(exportId, progress);

            FinalizeResult result;
            IReadOnlyList<string>? segmentPaths = null;
            string? tsPath = null;

            if (flashbackSink != null)
            {
                var forceRotateResult = flashbackSink.ForceRotateForExport(inPoint, outPoint, ct);
                segmentPaths = forceRotateResult.SegmentPaths;
                if (forceRotateResult.Status == FlashbackForceRotateStatus.Failed)
                {
                    var preservedArtifacts = bufferManager.GetValidSegmentPaths(inPoint, outPoint);
                    result = FinalizeResult.Failure(
                        outputPath,
                        "Flashback export failed: live-edge segment rotation failed.",
                        preservedArtifacts);
                    RecordLastFlashbackExportResult(exportId, result);
                    CompleteFlashbackExportDiagnostics(exportId, result);
                    Logger.Log(
                        "FLASHBACK_EXPORT_FORCE_ROTATE_FAILED " +
                        $"preserved_segments={preservedArtifacts.Count} " +
                        $"in_ms={(long)inPoint.TotalMilliseconds} " +
                        $"out_ms={(long)(outPoint == TimeSpan.MaxValue ? -1 : outPoint.TotalMilliseconds)}");
                    return result;
                }

                if (forceRotateResult.Status == FlashbackForceRotateStatus.CommittedPending)
                {
                    var preservedArtifacts = bufferManager.GetValidSegmentPaths(inPoint, outPoint);
                    result = FinalizeResult.Failure(
                        outputPath,
                        requireCompleteLiveEdge
                            ? "Flashback recording finalize failed: live-edge segment was not closed before timeout."
                            : "Flashback export failed: live-edge segment rotation committed but did not complete before timeout.",
                        preservedArtifacts);
                    RecordLastFlashbackExportResult(exportId, result);
                    CompleteFlashbackExportDiagnostics(exportId, result);
                    Logger.Log(
                        "FLASHBACK_EXPORT_FORCE_ROTATE_COMMITTED_PENDING_FAIL " +
                        $"preserved_segments={preservedArtifacts.Count} " +
                        $"in_ms={(long)inPoint.TotalMilliseconds} " +
                        $"out_ms={(long)outPoint.TotalMilliseconds}");
                    return result;
                }

                if (segmentPaths.Count == 0)
                {
                    if (requireCompleteLiveEdge)
                    {
                        var preservedArtifacts = bufferManager.GetValidSegmentPaths(inPoint, outPoint);
                        result = FinalizeResult.Failure(
                            outputPath,
                            "Flashback recording finalize failed: live-edge segment was not closed before timeout.",
                            preservedArtifacts);
                        RecordLastFlashbackExportResult(exportId, result);
                        CompleteFlashbackExportDiagnostics(exportId, result);
                        Logger.Log(
                            "FLASHBACK_RECORDING_EXPORT_INCOMPLETE_FAIL " +
                            $"preserved_segments={preservedArtifacts.Count} " +
                            $"in_ms={(long)inPoint.TotalMilliseconds} " +
                            $"out_ms={(long)outPoint.TotalMilliseconds}");
                        return result;
                    }

                    // ForceRotate timed out (AV1 encoder can be too slow to drain
                    // within the 3-second window). Completed segments before the
                    // active one are already finalized — query them directly.
                    // NOTE: The encoding thread may still be completing the rotation.
                    // This returns only already-completed segments — the live-edge
                    // segment may be missed if it hasn't been finalized yet. This is
                    // acceptable: the previous behavior returned a near-empty file.
                    segmentPaths = bufferManager?.GetValidSegmentPaths(inPoint, outPoint);
                    if (segmentPaths is { Count: > 0 })
                    {
                        Logger.Log($"FLASHBACK_EXPORT_FORCE_ROTATE_FALLBACK reason=force_rotate_timeout segments={segmentPaths.Count} in_ms={(long)inPoint.TotalMilliseconds} out_ms={(long)outPoint.TotalMilliseconds}");
                    }
                    else
                    {
                        segmentPaths = null;
                    }
                }
            }

            // Fallback: single-file export if no segments available
            if (segmentPaths == null)
            {
                tsPath = bufferManager?.ActiveFilePath;
                if (string.IsNullOrWhiteSpace(tsPath))
                {
                    result = FinalizeResult.Failure(outputPath, "Flashback buffer has no active file");
                    RecordLastFlashbackExportResult(exportId, result);
                    CompleteFlashbackExportDiagnostics(exportId, result);
                    return result;
                }

                Logger.Log(
                    "FLASHBACK_EXPORT_ACTIVE_FILE_FALLBACK " +
                    $"path='{tsPath}' in_ms={(long)inPoint.TotalMilliseconds} " +
                    $"out_ms={(long)(outPoint == TimeSpan.MaxValue ? -1 : outPoint.TotalMilliseconds)}");
            }

            var request = new FlashbackExportRequest
            {
                Segments = BuildFlashbackExportSegments(bufferManager, segmentPaths),
                SegmentPaths = segmentPaths,
                InputTsPath = tsPath,
                InPoint = inPoint,
                OutPoint = outPoint,
                OutputPath = outputPath,
            };
            result = await exporter.ExportAsync(request, diagnosticProgress, ct).ConfigureAwait(false);
            RecordLastFlashbackExportResult(exportId, result);
            CompleteFlashbackExportDiagnostics(exportId, result);
            return result;
        }
        catch (Exception ex)
        {
            var statusMessage = ex is OperationCanceledException && ct.IsCancellationRequested
                ? "Flashback export cancelled."
                : ex.Message;
            Logger.Log(
                $"FLASHBACK_EXPORT_CORE_FAIL id={exportId} type={ex.GetType().Name} " +
                $"cancelled={ct.IsCancellationRequested} msg='{statusMessage}'");
            var failure = FinalizeResult.Failure(outputPath, statusMessage);
            if (exportId != 0)
            {
                RecordLastFlashbackExportResult(exportId, failure);
                CompleteFlashbackExportDiagnostics(exportId, failure);
            }
            else
            {
                RecordRejectedFlashbackExportDiagnostics(outputPath, failure, inPoint, outPoint);
            }
            return failure;
        }
        finally
        {
            if (evictionPaused)
            {
                ResumeFlashbackEvictionBestEffort(bufferManager, "flashback_export");
            }
            if (exportOperationLockHeld)
            {
                ReleaseSemaphoreBestEffort(_flashbackExportOperationLock, "flashback_export_operation");
            }
        }
    }

    private static IReadOnlyList<FlashbackExportSegment>? BuildFlashbackExportSegments(
        FlashbackBufferManager? bufferManager,
        IReadOnlyList<string>? segmentPaths)
    {
        if (segmentPaths is not { Count: > 0 })
        {
            return null;
        }

        var segmentInfo = bufferManager?.GetSegmentInfoList()
            .Where(segment => !segment.IsActive)
            .Select(segment => (Key: TryGetFullPath(segment.Path), Segment: segment))
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Key))
            .GroupBy(entry => entry.Key!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Segment, StringComparer.OrdinalIgnoreCase);
        var segments = new List<FlashbackExportSegment>(segmentPaths.Count);
        foreach (var path in segmentPaths)
        {
            var pathKey = TryGetFullPath(path);
            if (segmentInfo != null &&
                pathKey != null &&
                segmentInfo.TryGetValue(pathKey, out var info))
            {
                var startPts = FromSegmentMilliseconds(info.StartPtsMs);
                var endPts = FromSegmentMilliseconds(info.EndPtsMs);
                if (endPts < startPts)
                {
                    endPts = startPts;
                }

                segments.Add(new FlashbackExportSegment
                {
                    Path = path,
                    StartPts = startPts,
                    EndPts = endPts
                });
            }
            else
            {
                segments.Add(new FlashbackExportSegment { Path = path });
            }
        }

        return segments;
    }

    private static string? TryGetFullPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PATH_NORMALIZE_WARN path='{path}' type={ex.GetType().Name} msg='{ex.Message}'");
            return null;
        }
    }

    private static TimeSpan FromSegmentMilliseconds(long milliseconds)
    {
        if (milliseconds <= 0)
        {
            return TimeSpan.Zero;
        }

        return milliseconds >= TimeSpan.MaxValue.TotalMilliseconds
            ? TimeSpan.MaxValue
            : TimeSpan.FromMilliseconds(milliseconds);
    }

    private static TimeSpan ClampFlashbackBufferPosition(TimeSpan position, TimeSpan bufferedDuration)
    {
        if (bufferedDuration <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        if (position < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return position > bufferedDuration ? bufferedDuration : position;
    }

    private static TimeSpan AddFlashbackPtsOffsetOrMax(TimeSpan position, TimeSpan offset)
    {
        if (position == TimeSpan.MaxValue || offset == TimeSpan.MaxValue)
        {
            return TimeSpan.MaxValue;
        }

        if (position < TimeSpan.Zero)
        {
            position = TimeSpan.Zero;
        }

        if (offset <= TimeSpan.Zero)
        {
            return position;
        }

        return position > TimeSpan.MaxValue - offset
            ? TimeSpan.MaxValue
            : position + offset;
    }

    private void RecordLastFlashbackExportResult(long exportId, FinalizeResult result)
    {
        lock (_flashbackExportDiagnosticsLock)
        {
            _lastExportResult = result;
            Volatile.Write(ref _lastFlashbackExportResultId, exportId);
        }
    }

    private long BeginFlashbackExportDiagnostics(TimeSpan inPoint, TimeSpan outPoint, string outputPath)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        lock (_flashbackExportDiagnosticsLock)
        {
            var exportId = Interlocked.Increment(ref _flashbackExportId);
            _flashbackExportActive = true;
            _flashbackExportStatus = "Running";
            _flashbackExportOutputPath = outputPath;
            _flashbackExportStartedUtcUnixMs = now;
            _flashbackExportLastProgressUtcUnixMs = now;
            _flashbackExportCompletedUtcUnixMs = 0;
            _flashbackExportSegmentsProcessed = 0;
            _flashbackExportTotalSegments = 0;
            _flashbackExportPercent = 0;
            _flashbackExportInPointMs = (long)inPoint.TotalMilliseconds;
            _flashbackExportOutPointMs = outPoint == TimeSpan.MaxValue ? -1 : (long)outPoint.TotalMilliseconds;
            _flashbackExportMessage = string.Empty;
            _flashbackExportFailureKind = string.Empty;

            return exportId;
        }
    }

    private void RecordRejectedFlashbackExportDiagnostics(
        string outputPath,
        FinalizeResult result,
        TimeSpan? inPoint = null,
        TimeSpan? outPoint = null)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        lock (_flashbackExportDiagnosticsLock)
        {
            if (_flashbackExportActive)
            {
                _lastExportResult = result;
                Volatile.Write(ref _lastFlashbackExportResultId, 0);
                Logger.Log(
                    "FLASHBACK_EXPORT_REJECTED_DIAGNOSTICS_DEFERRED " +
                    $"active_id={_flashbackExportId} status='{_flashbackExportStatus}' " +
                    $"rejected_status='{result.StatusMessage}' output='{outputPath}'");
                return;
            }

            var exportId = Interlocked.Increment(ref _flashbackExportId);
            _flashbackExportId = exportId;
            _flashbackExportActive = false;
            _flashbackExportStatus = IsFlashbackExportCancelled(result.StatusMessage) ? "Cancelled" : "Failed";
            _flashbackExportOutputPath = outputPath;
            _flashbackExportStartedUtcUnixMs = now;
            _flashbackExportLastProgressUtcUnixMs = now;
            _flashbackExportCompletedUtcUnixMs = now;
            _flashbackExportSegmentsProcessed = 0;
            _flashbackExportTotalSegments = 0;
            _flashbackExportPercent = 0;
            _flashbackExportInPointMs = inPoint.HasValue ? (long)inPoint.Value.TotalMilliseconds : 0;
            _flashbackExportOutPointMs = outPoint.HasValue
                ? outPoint.Value == TimeSpan.MaxValue ? -1 : (long)outPoint.Value.TotalMilliseconds
                : 0;
            _flashbackExportMessage = result.StatusMessage;
            _flashbackExportFailureKind = ClassifyFlashbackExportFailureKind(result.StatusMessage);
            RecordLastFlashbackExportResult(exportId, result);
        }
    }

    private IProgress<ExportProgress> CreateFlashbackExportProgressSink(
        long exportId,
        IProgress<ExportProgress>? innerProgress)
    {
        return new FlashbackExportProgressForwarder(progress =>
        {
            UpdateFlashbackExportProgress(exportId, progress);
            try
            {
                innerProgress?.Report(progress);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_EXPORT_PROGRESS_FORWARD_WARN id={exportId} type={ex.GetType().Name} msg='{ex.Message}'");
            }
        });
    }

    private void UpdateFlashbackExportProgress(long exportId, ExportProgress progress)
    {
        if (Volatile.Read(ref _flashbackExportId) != exportId)
        {
            return;
        }

        lock (_flashbackExportDiagnosticsLock)
        {
            if (_flashbackExportId != exportId || !_flashbackExportActive)
            {
                return;
            }

            var rawTotalSegments = progress.TotalSegments;
            var rawSegmentsProcessed = progress.SegmentsProcessed;
            var rawPercent = progress.Percent;
            var totalSegments = Math.Max(0, rawTotalSegments);
            var segmentsProcessed = Math.Max(0, rawSegmentsProcessed);
            if (totalSegments > 0 && segmentsProcessed > totalSegments)
            {
                segmentsProcessed = totalSegments;
            }

            var percent = double.IsFinite(rawPercent)
                ? Math.Clamp(rawPercent, 0.0, 100.0)
                : 0.0;
            if (rawTotalSegments != totalSegments ||
                rawSegmentsProcessed != segmentsProcessed ||
                !double.IsFinite(rawPercent) ||
                rawPercent != percent)
            {
                Logger.Log(
                    $"FLASHBACK_EXPORT_PROGRESS_NORMALIZED id={exportId} " +
                    $"raw_segments={rawSegmentsProcessed}/{rawTotalSegments} " +
                    $"segments={segmentsProcessed}/{totalSegments} " +
                    $"raw_percent={rawPercent:0.###} percent={percent:0.###}");
            }

            _flashbackExportSegmentsProcessed = segmentsProcessed;
            _flashbackExportTotalSegments = totalSegments;
            _flashbackExportPercent = percent;
            _flashbackExportLastProgressUtcUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }

    private void CompleteFlashbackExportDiagnostics(long exportId, FinalizeResult result)
    {
        if (Volatile.Read(ref _flashbackExportId) != exportId)
        {
            return;
        }

        lock (_flashbackExportDiagnosticsLock)
        {
            if (_flashbackExportId != exportId)
            {
                return;
            }

            _flashbackExportActive = false;
            _flashbackExportStatus = result.Succeeded
                ? "Succeeded"
                : IsFlashbackExportCancelled(result.StatusMessage)
                    ? "Cancelled"
                    : "Failed";
            var completedUtcUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _flashbackExportCompletedUtcUnixMs = completedUtcUnixMs;
            _flashbackExportLastProgressUtcUnixMs = completedUtcUnixMs;
            _flashbackExportMessage = result.StatusMessage;
            _flashbackExportFailureKind = result.Succeeded
                ? string.Empty
                : ClassifyFlashbackExportFailureKind(result.StatusMessage);
            if (result.Succeeded && _flashbackExportPercent < 100)
            {
                _flashbackExportPercent = 100;
            }
        }
    }

    private static bool IsFlashbackExportCancelled(string? statusMessage)
        => statusMessage?.Contains("cancel", StringComparison.OrdinalIgnoreCase) == true;

    internal static string ClassifyFlashbackExportFailureKind(string? statusMessage)
    {
        if (string.IsNullOrWhiteSpace(statusMessage))
        {
            return string.Empty;
        }

        if (IsFlashbackExportCancelled(statusMessage))
        {
            return "Cancelled";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "request is required") ||
            ContainsFlashbackExportFailureText(statusMessage, "duration must be finite"))
        {
            return "InvalidRequest";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "active recording backend"))
        {
            return "UnavailableDuringRecording";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "buffer not active"))
        {
            return "BufferInactive";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "in point") ||
            ContainsFlashbackExportFailureText(statusMessage, "export range"))
        {
            return "InvalidRange";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "output path") ||
            ContainsFlashbackExportFailureText(statusMessage, "output directory") ||
            ContainsFlashbackExportFailureText(statusMessage, "overwrite source"))
        {
            return "InvalidOutputPath";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "operation=avio_open2") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=avformat_alloc_output_context2") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=avformat_new_stream") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=avcodec_parameters_copy") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=av_dict_set") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=avformat_write_header") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=av_interleaved_write_frame") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=av_write_trailer") ||
            ContainsFlashbackExportFailureText(statusMessage, "output file length unavailable") ||
            ContainsFlashbackExportFailureText(statusMessage, "temporary export file was not created") ||
            ContainsFlashbackExportFailureText(statusMessage, "access is denied") ||
            ContainsFlashbackExportFailureText(statusMessage, "permission denied") ||
            ContainsFlashbackExportFailureText(statusMessage, "sharing violation"))
        {
            return "OutputWriteFailed";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "rotation failed"))
        {
            return "ForceRotateFailed";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "live-edge segment"))
        {
            return "IncompleteLiveEdge";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "no segment paths") ||
            ContainsFlashbackExportFailureText(statusMessage, "segment path") ||
            ContainsFlashbackExportFailureText(statusMessage, "segment files") ||
            ContainsFlashbackExportFailureText(statusMessage, "readable segment"))
        {
            return "SegmentUnavailable";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "input file not found") ||
            ContainsFlashbackExportFailureText(statusMessage, "buffer has no active file"))
        {
            return "InputUnavailable";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "operation=avformat_open_input") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=av_read_frame"))
        {
            return "InputReadFailed";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "input context") ||
            ContainsFlashbackExportFailureText(statusMessage, "input had no streams") ||
            ContainsFlashbackExportFailureText(statusMessage, "stream count"))
        {
            return "InvalidInputStream";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "no usable video stream") ||
            ContainsFlashbackExportFailureText(statusMessage, "no segment had complete video parameters") ||
            ContainsFlashbackExportFailureText(statusMessage, "output file is empty") ||
            ContainsFlashbackExportFailureText(statusMessage, "no video packets") ||
            ContainsFlashbackExportFailureText(statusMessage, "no packets"))
        {
            return "NoMediaWritten";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "disposed"))
        {
            return "Disposed";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "timeout") ||
            ContainsFlashbackExportFailureText(statusMessage, "timed out"))
        {
            return "Timeout";
        }

        return "Failed";
    }

    private static bool ContainsFlashbackExportFailureText(string statusMessage, string value)
        => statusMessage.Contains(value, StringComparison.OrdinalIgnoreCase);

    private sealed class FlashbackExportProgressForwarder : IProgress<ExportProgress>
    {
        private readonly Action<ExportProgress> _onProgress;

        public FlashbackExportProgressForwarder(Action<ExportProgress> onProgress)
        {
            _onProgress = onProgress;
        }

        public void Report(ExportProgress value)
            => _onProgress(value);
    }

    private void ScheduleDeferredFlashbackBackendCleanup(
        Task sinkCompletionTask,
        FlashbackBufferManager? bufferManager,
        FlashbackExporter? flashbackExporter,
        string reason,
        bool purgeSegments)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await sinkCompletionTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_BACKEND_DEFERRED_WAIT_WARN reason='{reason}' type={ex.GetType().Name} msg={ex.Message}");
            }
            finally
            {
                if (bufferManager != null)
                {
                    if (purgeSegments)
                    {
                        try
                        {
                            bool lockAcquired = false;
                            if (_flashbackExportOperationLock != null)
                            {
                                Logger.Log($"FLASHBACK_DEFERRED_PURGE_AWAITING_EXPORT_LOCK reason='{reason}'");
                                var lockSw = System.Diagnostics.Stopwatch.StartNew();
                                lockAcquired = await _flashbackExportOperationLock.WaitAsync(TimeSpan.FromSeconds(30), CancellationToken.None).ConfigureAwait(false);
                                lockSw.Stop();
                                if (lockAcquired)
                                    Logger.Log($"FLASHBACK_DEFERRED_PURGE_LOCK_ACQUIRED elapsed_ms={lockSw.ElapsedMilliseconds} reason='{reason}'");
                                else
                                    Logger.Log($"FLASHBACK_DEFERRED_PURGE_EXPORT_LOCK_TIMEOUT — proceeding without lock reason='{reason}'");
                            }

                            try { bufferManager.PurgeAllSegments(); }
                            catch (Exception ex) { Logger.Log($"FLASHBACK_BUFFER_DEFERRED_PURGE_WARN reason='{reason}' type={ex.GetType().Name} msg={ex.Message}"); }
                            finally
                            {
                                if (lockAcquired)
                                {
                                    try { _flashbackExportOperationLock!.Release(); }
                                    catch (Exception ex) { Logger.Log($"FLASHBACK_DEFERRED_PURGE_LOCK_RELEASE_WARN reason='{reason}' type={ex.GetType().Name} msg={ex.Message}"); }
                                }
                            }
                        }
                        catch (Exception ex) { Logger.Log($"FLASHBACK_BUFFER_DEFERRED_PURGE_WARN reason='{reason}' type={ex.GetType().Name} msg={ex.Message}"); }
                    }

                    try { bufferManager.Dispose(); }
                    catch (Exception ex) { Logger.Log($"FLASHBACK_BUFFER_DEFERRED_DISPOSE_WARN reason='{reason}' type={ex.GetType().Name} msg={ex.Message}"); }
                }

                if (flashbackExporter != null)
                {
                    try { flashbackExporter.Dispose(); }
                    catch (Exception ex) { Logger.Log($"FLASHBACK_EXPORTER_DEFERRED_DISPOSE_WARN reason='{reason}' type={ex.GetType().Name} msg={ex.Message}"); }
                }

                Logger.Log($"FLASHBACK_BACKEND_DEFERRED_CLEANUP_OK reason='{reason}'");
            }
        });
    }

    private Task ScheduleDeferredUnifiedVideoCaptureCleanup(
        Task sinkCompletionTask,
        UnifiedVideoCapture unifiedVideoCapture,
        string reason)
    {
        try
        {
            unifiedVideoCapture.SetPreviewSink(null);
        }
        catch (Exception ex)
        {
            Logger.Log($"UNIFIED_VIDEO_DEFERRED_PREVIEW_DETACH_WARN reason='{reason}' type={ex.GetType().Name} msg={ex.Message}");
        }

        return Task.Run(async () =>
        {
            Exception? cleanupFailure = null;
            try
            {
                await sinkCompletionTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"UNIFIED_VIDEO_DEFERRED_WAIT_WARN reason='{reason}' type={ex.GetType().Name} msg={ex.Message}");
            }
            finally
            {
                try
                {
                    await unifiedVideoCapture.StopAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    cleanupFailure ??= ex;
                    Logger.Log($"UNIFIED_VIDEO_DEFERRED_STOP_WARN reason='{reason}' type={ex.GetType().Name} msg={ex.Message}");
                }

                try
                {
                    await unifiedVideoCapture.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    cleanupFailure ??= ex;
                    Logger.Log($"UNIFIED_VIDEO_DEFERRED_DISPOSE_WARN reason='{reason}' type={ex.GetType().Name} msg={ex.Message}");
                }

                Logger.Log($"UNIFIED_VIDEO_DEFERRED_CLEANUP_END reason='{reason}'");

                if (cleanupFailure != null)
                {
                    throw new InvalidOperationException(
                        $"Deferred unified video cleanup failed for reason '{reason}'.",
                        cleanupFailure);
                }
            }
        });
    }

    private void ClearPendingLibAvDrainTaskIfCompletedSuccessfully()
    {
        if (_pendingLibAvDrainTask?.IsCompletedSuccessfully == true)
        {
            _pendingLibAvDrainTask = null;
        }
    }

    private void ThrowIfPendingLibAvDrainTaskBlocksReentry()
    {
        var pendingLibAvDrainTask = _pendingLibAvDrainTask;
        if (pendingLibAvDrainTask == null)
        {
            return;
        }

        if (pendingLibAvDrainTask.IsCompletedSuccessfully)
        {
            _pendingLibAvDrainTask = null;
            return;
        }

        if (pendingLibAvDrainTask.IsFaulted)
        {
            throw new InvalidOperationException(
                "Previous recording backend failed to finalize cleanly. Check the logs and retry.",
                pendingLibAvDrainTask.Exception?.GetBaseException());
        }

        if (pendingLibAvDrainTask.IsCanceled)
        {
            throw new InvalidOperationException("Previous recording backend cleanup was canceled. Check the logs and retry.");
        }

        throw new InvalidOperationException("Previous recording backend is still finalizing. Please wait a moment and try again.");
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
        var controller = _flashbackPlaybackController;
        if (sink == null && controller is { IsDisposed: false, IsInitialized: true })
        {
            controller.PrepareForPreviewDetach();
        }

        _previewFrameSink = sink;
        _unifiedVideoCapture?.SetPreviewSink(sink);
        TryApplySharedPreviewDevice(_unifiedVideoCapture, sink);

        // Late-initialize playback controller if it was created before the renderer
        if (controller is { IsDisposed: false, IsInitialized: false } && sink != null && _unifiedVideoCapture != null)
        {
            controller.Initialize(sink, _unifiedVideoCapture, _wasapiAudioPlayback, _wasapiAudioCapture);
            Logger.Log("FLASHBACK_PLAYBACK_LATE_INIT via SetPreviewFrameSink");
        }
        else if (controller is { IsDisposed: false, IsInitialized: true })
        {
            controller.UpdatePreviewComponents(sink, _unifiedVideoCapture);
        }
    }

    private void CacheMjpegTimingMetrics(UnifiedVideoCapture? unifiedVideoCapture)
    {
        if (unifiedVideoCapture == null)
        {
            return;
        }

        var timingSnapshot = unifiedVideoCapture.GetMjpegPipelineTimingSnapshot();
        _lastMjpegPipelineTimingMetrics = timingSnapshot.Summary;
        _lastFullMjpegPipelineTimingMetrics = timingSnapshot.Details;
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

    private bool IsFlashbackRecordingBackendOwnedByRecording()
        => Volatile.Read(ref _flashbackRecordingStartInProgress) != 0 ||
           Volatile.Read(ref _flashbackRecordingFinalizeInProgress) != 0 ||
           (_isRecording && IsFlashbackRecordingBackendActive());

    private bool ResolveFlashbackSegmentPurge(bool requested, string reason)
    {
        if (!requested)
        {
            return false;
        }

        if (!_preserveFlashbackSegmentsAfterFailedRecordingFinalize)
        {
            return true;
        }

        Logger.Log($"FLASHBACK_SEGMENT_PURGE_BLOCKED reason={reason}");
        return false;
    }

    private void PreserveFlashbackRecoverySegments(string reason)
    {
        _preserveFlashbackSegmentsAfterFailedRecordingFinalize = true;
        Logger.Log($"FLASHBACK_RECOVERY_PRESERVE reason={reason}");
        _flashbackBufferManager?.MarkSessionPreservedForRecovery();
    }

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

    private async Task EnsureFlashbackAudioInputsAsync(
        CaptureSettings settings,
        CancellationToken cancellationToken,
        string reason)
    {
        var audioDeviceId = settings.AudioEnabled
            ? (settings.UseCustomAudioInput ? settings.AudioDeviceId : (_audioDeviceId ?? _currentDevice?.AudioDeviceId))
            : null;

        if (settings.AudioEnabled && _wasapiAudioCapture == null)
        {
            if (!string.IsNullOrWhiteSpace(audioDeviceId))
            {
                WasapiAudioCapture? wasapiCapture = new();
                try
                {
                    await wasapiCapture.InitializeAsync(audioDeviceId, cancellationToken).ConfigureAwait(false);
                    wasapiCapture.AudioLevelUpdated += OnWasapiAudioLevelUpdated;
                    wasapiCapture.CaptureFailed += OnWasapiCaptureFailed;
                    wasapiCapture.Start();
                    _wasapiAudioCapture = wasapiCapture;
                    wasapiCapture = null;
                    _avSyncBaselineDriftMs = double.NaN;
                    Volatile.Write(ref _wasapiAudioCaptureFaulted, false);
                    Volatile.Write(ref _wasapiAudioCaptureFaultMessage, null);
                    Logger.Log($"FLASHBACK_AUDIO_CAPTURE_RESTORED reason='{reason}' device='{audioDeviceId}'");
                }
                finally
                {
                    if (wasapiCapture != null)
                    {
                        wasapiCapture.AudioLevelUpdated -= OnWasapiAudioLevelUpdated;
                        wasapiCapture.CaptureFailed -= OnWasapiCaptureFailed;
                        try { await wasapiCapture.DisposeAsync().ConfigureAwait(false); }
                        catch (Exception disposeEx) { Logger.Log($"FLASHBACK_AUDIO_CAPTURE_RESTORE_DISPOSE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }
                    }
                }
            }
            else
            {
                Logger.Log($"FLASHBACK_AUDIO_CAPTURE_UNAVAILABLE reason='{reason}'");
            }
        }

        AttachFlashbackAudioIfSupported(_wasapiAudioCapture, reason);

        if (_micMonitorEnabled && _microphoneCapture == null && !string.IsNullOrWhiteSpace(_micMonitorDeviceId))
        {
            WasapiAudioCapture? micCapture = new();
            try
            {
                await micCapture.InitializeAsync(_micMonitorDeviceId, cancellationToken).ConfigureAwait(false);
                micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
                micCapture.CaptureFailed += OnWasapiCaptureFailed;
                micCapture.Start();
                _microphoneCapture = micCapture;
                micCapture = null;
                Logger.Log("MIC_MONITOR_START device='" + (_micMonitorDeviceName ?? "?") + "'");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
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
                    catch (Exception disposeEx) { Logger.Log($"MIC_MONITOR_RESTORE_DISPOSE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }
                }
            }
        }

        if (_microphoneCapture != null && _flashbackSink is { MicrophoneEnabled: true } fbSink)
        {
            _microphoneCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
            Logger.Log($"FLASHBACK_MIC_ATTACH_OK reason='{reason}'");
        }
    }

    private FlashbackSessionContext CreateFlashbackSessionContext(
        UnifiedVideoCapture unifiedVideoCapture,
        CaptureSettings settings)
    {
        var isP010 = unifiedVideoCapture.IsP010;
        // Flashback requires real-time hardware encoding. For AV1, fall back to
        // HEVC NVENC if av1_nvenc isn't available (the UI enables AV1 when any
        // AV1 encoder is present, including software-only like libsvtav1).
        var frameRate = unifiedVideoCapture.Fps > 0 ? unifiedVideoCapture.Fps : settings.FrameRate;
        var forceTransportStreamFlashback = UseTransportStreamFlashbackCodec(unifiedVideoCapture, settings, frameRate);
        var codecName = forceTransportStreamFlashback
            ? "hevc_nvenc"
            : settings.Format switch
        {
            RecordingFormat.HevcMp4 => "hevc_nvenc",
            RecordingFormat.Av1Mp4 => _hasAv1Nvenc ? "av1_nvenc" : "hevc_nvenc",
            _ => isP010 ? "hevc_nvenc" : "h264_nvenc" // H264 can't encode P010
        };
        var audioDeviceId = settings.AudioEnabled
            ? (settings.UseCustomAudioInput ? settings.AudioDeviceId : (_audioDeviceId ?? _currentDevice?.AudioDeviceId))
            : null;
        var d3dManager = unifiedVideoCapture.D3DManager;
        // When the software MJPEG decode pipeline is active, frames arrive as CPU NV12
        // buffers (not D3D11 textures). Passing D3D device pointers would cause the
        // encoder to initialize hw_frames, but SendVideoFrame would then feed a software
        // frame into an nvenc context expecting D3D11 textures — crashing in the driver.
        var useGpuEncoding = !unifiedVideoCapture.IsSoftwareMjpegPipelineActive;

        var frameRateParts = ResolveFlashbackSessionFrameRateParts(settings, frameRate);
        frameRate = frameRateParts.EffectiveFrameRate;
        var fpsNum = frameRateParts.Numerator;
        var fpsDen = frameRateParts.Denominator;

        var flashbackNvencPreset = unifiedVideoCapture.IsSoftwareMjpegPipelineActive && frameRate >= 100
            ? "Fast"
            : settings.NvencPreset;

        // Hard rail: HDR must never silently degrade. If the user requested HDR
        // but UVC negotiation did not land on P010, fail the operation rather than
        // allowing SDR data to be encoded as if it were HDR (or vice versa).
        var hdrRequested = HdrOutputPolicy.IsEnabled(settings);
        if (hdrRequested != isP010)
        {
            Logger.Log(
                $"FLASHBACK_HDR_NEGOTIATION_FAIL requested={hdrRequested} negotiated_p010={isP010} resolved_codec={codecName}");
            throw new InvalidOperationException(
                $"Flashback HDR negotiation mismatch: HDR requested={hdrRequested} but UVC negotiated P010={isP010}. " +
                "Operation aborted to prevent silent HDR degradation.");
        }

        // One-shot downgrade visibility. The codec/preset substitutions above happen
        // silently inside the encoder pipeline; without a log line the user has no
        // way to know they did not get the AV1/quality preset they configured. Emit
        // when the resolved reason changes so we don't spam every flashback restart
        // with the identical message.
        var downgradeReason = ResolveFlashbackCodecDowngradeReason(settings, unifiedVideoCapture);
        if (!string.IsNullOrEmpty(downgradeReason) &&
            !string.Equals(downgradeReason, _lastLoggedFlashbackDowngradeReason, StringComparison.Ordinal))
        {
            Logger.Log(
                $"FLASHBACK_CODEC_DOWNGRADE requested_format={settings.Format} resolved_codec={codecName} requested_preset={settings.NvencPreset} resolved_preset={flashbackNvencPreset} frame_rate={frameRate:0.###} software_mjpeg={unifiedVideoCapture.IsSoftwareMjpegPipelineActive} reason='{downgradeReason}'");
            _lastLoggedFlashbackDowngradeReason = downgradeReason;
        }
        else if (string.IsNullOrEmpty(downgradeReason) &&
                 !string.IsNullOrEmpty(_lastLoggedFlashbackDowngradeReason))
        {
            // Capture conditions changed — e.g. user dropped from 120fps to 60fps —
            // and the prior downgrade no longer applies. Surface that too so the log
            // tells a coherent story across reinit cycles.
            Logger.Log(
                $"FLASHBACK_CODEC_DOWNGRADE_CLEARED requested_format={settings.Format} resolved_codec={codecName} resolved_preset={flashbackNvencPreset} frame_rate={frameRate:0.###}");
            _lastLoggedFlashbackDowngradeReason = null;
        }

        return new FlashbackSessionContext
        {
            Width = Math.Max(1, unifiedVideoCapture.Width),
            Height = Math.Max(1, unifiedVideoCapture.Height),
            FrameRate = frameRate,
            FrameRateNumerator = fpsNum,
            FrameRateDenominator = fpsDen,
            CodecName = codecName,
            NvencPreset = flashbackNvencPreset,
            IsP010 = isP010,
            BitRate = settings.GetTargetBitrate(),
            HdrEnabled = hdrRequested,
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

    private static bool UseTransportStreamFlashbackCodec(
        UnifiedVideoCapture unifiedVideoCapture,
        CaptureSettings settings,
        double frameRate)
        =>
            unifiedVideoCapture.IsSoftwareMjpegPipelineActive &&
            frameRate >= 100 &&
            settings.Format == RecordingFormat.Av1Mp4;

    private static (int? Numerator, int? Denominator, double EffectiveFrameRate) ResolveFlashbackSessionFrameRateParts(
        CaptureSettings settings,
        double deliveryFrameRate)
    {
        // Preserve exact rationals only when they describe the actual delivered USB cadence.
        // A source-reported 120000/1001 rate paired with ~120 delivered frames/sec causes A/V
        // drift if we stamp Flashback video against the slower source clock.
        if (!double.IsFinite(deliveryFrameRate) || deliveryFrameRate <= 0)
        {
            return (null, null, deliveryFrameRate);
        }

        if (settings.RequestedFrameRateNumerator is not uint numerator ||
            settings.RequestedFrameRateDenominator is not uint denominator ||
            numerator == 0 ||
            denominator == 0 ||
            numerator > int.MaxValue ||
            denominator > int.MaxValue)
        {
            return (null, null, deliveryFrameRate);
        }

        var rationalFps = numerator / (double)denominator;
        if (!double.IsFinite(rationalFps) || rationalFps <= 0)
        {
            return (null, null, deliveryFrameRate);
        }

        var deltaFps = Math.Abs(rationalFps - deliveryFrameRate);
        var toleranceFps = Math.Max(0.01, deliveryFrameRate * 0.0001);
        if (deltaFps > toleranceFps)
        {
            Logger.Log(
                $"FLASHBACK_FRAME_RATE_RATIONAL_REJECT requested={numerator}/{denominator} " +
                $"rational={rationalFps:0.######} delivery={deliveryFrameRate:0.######} " +
                $"delta={deltaFps:0.######} tolerance={toleranceFps:0.######}");
            return (null, null, deliveryFrameRate);
        }

        Logger.Log(
            $"FLASHBACK_FRAME_RATE_RATIONAL_ACCEPT requested={numerator}/{denominator} " +
            $"delivery={deliveryFrameRate:0.######} effective={rationalFps:0.######}");
        return ((int)numerator, (int)denominator, rationalFps);
    }

    private static string? ResolveFlashbackExportVerificationFormat(
        CaptureSettings? settings,
        UnifiedVideoCapture? unifiedVideoCapture)
    {
        if (settings == null || unifiedVideoCapture == null)
        {
            return settings?.Format.ToString();
        }

        var frameRate = unifiedVideoCapture.Fps > 0 ? unifiedVideoCapture.Fps : settings.FrameRate;
        return UseTransportStreamFlashbackCodec(unifiedVideoCapture, settings, frameRate)
            ? RecordingFormat.HevcMp4.ToString()
            : settings.Format.ToString();
    }

    /// <summary>
    /// Surfaces silent flashback codec/encoder substitutions so the verifier, automation
    /// snapshot, and (eventually) the UI can show what was actually encoded vs what the
    /// user requested. Returns null when user settings are honored as-is.
    /// </summary>
    private static string? ResolveFlashbackCodecDowngradeReason(
        CaptureSettings? settings,
        UnifiedVideoCapture? unifiedVideoCapture)
    {
        if (settings == null || unifiedVideoCapture == null)
        {
            return null;
        }

        var frameRate = unifiedVideoCapture.Fps > 0 ? unifiedVideoCapture.Fps : settings.FrameRate;
        var codecDowngraded = UseTransportStreamFlashbackCodec(unifiedVideoCapture, settings, frameRate);
        var presetCoerced = unifiedVideoCapture.IsSoftwareMjpegPipelineActive
            && frameRate >= 100
            && !string.Equals(settings.NvencPreset, "Fast", StringComparison.OrdinalIgnoreCase);

        if (!codecDowngraded && !presetCoerced)
        {
            return null;
        }

        var parts = new List<string>(2);
        if (codecDowngraded)
        {
            parts.Add($"AV1->HEVC: software MJPEG pipeline at {frameRate:0.#}fps cannot encode AV1 in real time");
        }
        if (presetCoerced)
        {
            parts.Add($"NVENC preset '{settings.NvencPreset}'->'Fast' to keep up with software MJPEG decode at {frameRate:0.#}fps");
        }
        return string.Join("; ", parts);
    }

    private async Task EnsureFlashbackPreviewBackendAsync(
        UnifiedVideoCapture unifiedVideoCapture,
        CaptureSettings settings,
        CancellationToken cancellationToken)
    {
        if (!_flashbackEnabled || _flashbackSink != null)
            return;

        // Cache AV1 NVENC availability on first flashback init (async-safe here)
        if (!_hasAv1Nvenc)
        {
            try
            {
                var support = await FfmpegRuntimeLocator.GetEncoderSupportAsync().ConfigureAwait(false);
                _hasAv1Nvenc = support.HasAv1Nvenc;
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_ENCODER_SUPPORT_PROBE_WARN type={ex.GetType().Name} msg={ex.Message}");
                // Assume unavailable — will fall back to HEVC.
            }
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
        flashbackSink.SetFatalErrorCallback(OnFlashbackBackendFatalError);
        var flashbackExporter = new FlashbackExporter();
        FlashbackPlaybackController? playbackController = null;

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
            playbackController = new FlashbackPlaybackController(bufferManager);
            playbackController.GpuDecodeEnabled = settings.FlashbackGpuDecode;
            if (_previewFrameSink != null && unifiedVideoCapture != null)
            {
                playbackController.Initialize(_previewFrameSink, unifiedVideoCapture, _wasapiAudioPlayback, _wasapiAudioCapture);
            }
            _flashbackPlaybackController = playbackController;
            _flashbackBackendSettings = CloneCaptureSettings(settings);
            _preserveFlashbackSegmentsAfterFailedRecordingFinalize = false;
            ClearLastFlashbackFailure();

            Logger.Log($"FLASHBACK_PREVIEW_INIT_OK session='{bufferManager.SessionId}' controller_initialized={playbackController.IsInitialized}");
        }
        catch (Exception ex)
        {
            var failureToken = ex is OperationCanceledException && cancellationToken.IsCancellationRequested
                ? "FLASHBACK_PREVIEW_INIT_CANCELLED"
                : "FLASHBACK_PREVIEW_INIT_FAIL";
            Logger.Log($"{failureToken} type={ex.GetType().Name} error='{ex.Message}'");
            flashbackSink.FrameEncoded -= OnFlashbackFrameEncoded;
            try { unifiedVideoCapture.SetFlashbackSink(null); }
            catch (Exception detachEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_DETACH_WARN target=video type={detachEx.GetType().Name} msg={detachEx.Message}"); }
            try { _wasapiAudioCapture?.DetachFlashbackSink(); }
            catch (Exception detachEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_DETACH_WARN target=audio type={detachEx.GetType().Name} msg={detachEx.Message}"); }
            try { _microphoneCapture?.SetAudioWriter(null); }
            catch (Exception detachEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_DETACH_WARN target=microphone type={detachEx.GetType().Name} msg={detachEx.Message}"); }
            try { (playbackController ?? _flashbackPlaybackController)?.Dispose(); }
            catch (Exception disposeEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_PLAYBACK_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }
            try { await flashbackSink.DisposeAsync().ConfigureAwait(false); }
            catch (Exception disposeEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_SINK_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }

            var sinkCompletionTask = flashbackSink.EncodingCompletionTask;
            if (!sinkCompletionTask.IsCompleted)
            {
                ScheduleDeferredFlashbackBackendCleanup(
                    sinkCompletionTask,
                    bufferManager,
                    flashbackExporter,
                    reason: "preview_init_rollback",
                    purgeSegments: true);
                bufferManager = null;
                flashbackExporter = null;
            }

            try { flashbackExporter?.Dispose(); }
            catch (Exception disposeEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_EXPORTER_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }

            try { bufferManager?.PurgeAllSegments(); }
            catch (Exception disposeEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_PURGE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }

            try { bufferManager?.Dispose(); }
            catch (Exception disposeEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_BUFFER_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }

            _flashbackSink = null;
            _flashbackBufferManager = null;
            _flashbackExporter = null;
            _flashbackPlaybackController = null;
            _flashbackBackendSettings = null;

            throw;
        }
    }

    private async Task DisposeFlashbackPreviewBackendAsync(
        CancellationToken cancellationToken,
        bool purgeSegments = true,
        bool detachMicrophoneWriter = true)
    {
        await _flashbackBackendLeaseLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var effectivePurgeSegments = ResolveFlashbackSegmentPurge(
                purgeSegments,
                "preview_backend_dispose");
            await DisposeFlashbackPreviewBackendCoreAsync(
                    cancellationToken,
                    effectivePurgeSegments,
                    detachMicrophoneWriter)
                .ConfigureAwait(false);
        }
        finally
        {
            ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, "flashback_preview_backend_dispose");
        }
    }

    private async Task DisposeFlashbackPreviewBackendCoreAsync(
        CancellationToken cancellationToken,
        bool purgeSegments = true,
        bool detachMicrophoneWriter = true)
    {
        var flashbackSink = _flashbackSink;
        var flashbackBufferManager = _flashbackBufferManager;
        var flashbackExporter = _flashbackExporter;
        var flashbackPlaybackController = _flashbackPlaybackController;
        _flashbackPlaybackController = null;

        // Do NOT null the sink/buffer/exporter fields yet; the encoding loop may still be running
        // and code that checks _flashbackSink (e.g. IsFlashbackActive) must see
        // a consistent state until the sink is fully drained and stopped.

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
        if (detachMicrophoneWriter)
        {
            try { _microphoneCapture?.SetAudioWriter(null); }
            catch (Exception ex) { Logger.Log($"FLASHBACK_PREVIEW_DETACH_WARN target=microphone type={ex.GetType().Name} msg={ex.Message}"); }
        }
        try { _wasapiAudioCapture?.DetachFlashbackSink(); }
        catch (Exception ex) { Logger.Log($"FLASHBACK_PREVIEW_DETACH_WARN target=audio type={ex.GetType().Name} msg={ex.Message}"); }
        try { _unifiedVideoCapture?.SetFlashbackSink(null); }
        catch (Exception ex) { Logger.Log($"FLASHBACK_PREVIEW_DETACH_WARN target=video type={ex.GetType().Name} msg={ex.Message}"); }

        Task sinkCompletionTask = Task.CompletedTask;
        if (flashbackSink != null)
        {
            flashbackSink.FrameEncoded -= OnFlashbackFrameEncoded;
            try
            {
                // Once feeds are detached, finish the bounded sink drain even if the
                // caller cancels so service fields never point at a half-torn backend.
                await flashbackSink.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_PREVIEW_STOP_WARN type={ex.GetType().Name} msg={ex.Message}");
            }

            try
            {
                await flashbackSink.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_PREVIEW_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
            }

            sinkCompletionTask = flashbackSink.EncodingCompletionTask;
        }

        // Now that the sink is fully stopped and disposed, clear the fields.
        // Any concurrent reader of _flashbackSink sees either the old (valid)
        // value or null — never a half-disposed object.
        _flashbackSink = null;
        _flashbackBufferManager = null;
        _flashbackExporter = null;
        _flashbackPlaybackController = null;
        _flashbackBackendSettings = null;

        if (!sinkCompletionTask.IsCompleted)
        {
            ScheduleDeferredFlashbackBackendCleanup(
                sinkCompletionTask,
                flashbackBufferManager,
                flashbackExporter,
                reason: purgeSegments ? "preview_backend_dispose_purge" : "preview_backend_dispose",
                purgeSegments: purgeSegments);
            flashbackBufferManager = null;
            flashbackExporter = null;
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (flashbackBufferManager != null)
        {
            if (purgeSegments)
            {
                cancellationToken.ThrowIfCancellationRequested();
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

        Logger.Log($"FLASHBACK_PREVIEW_DISPOSE_OK purge={purgeSegments}");
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
        await _flashbackBackendLeaseLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
        var unifiedVideoCapture = _unifiedVideoCapture;
        var bufferManager = _flashbackBufferManager;
        var oldSink = _flashbackSink;
        var effectivePurgeSegments = ResolveFlashbackSegmentPurge(
            purgeSegments,
            "buffer_cycle");

        if (purgeSegments && !effectivePurgeSegments)
        {
            await DisposeFlashbackPreviewBackendCoreAsync(cancellationToken, purgeSegments: false).ConfigureAwait(false);
            if (_flashbackEnabled && unifiedVideoCapture != null && _currentSettings != null)
            {
                await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, _currentSettings, cancellationToken).ConfigureAwait(false);
                Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=preserve_rebuild new_session=true");
            }
            else
            {
                Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=preserve_rebuild new_session=false reason='disabled_or_no_capture'");
            }
            return;
        }

        // If prerequisites are missing, fall back to full teardown
        if (!_flashbackEnabled || unifiedVideoCapture == null || _currentSettings == null || bufferManager == null || oldSink == null)
        {
            await DisposeFlashbackPreviewBackendCoreAsync(cancellationToken, purgeSegments: effectivePurgeSegments).ConfigureAwait(false);
            if (_flashbackEnabled && unifiedVideoCapture != null && _currentSettings != null)
            {
                await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, _currentSettings, cancellationToken).ConfigureAwait(false);
                Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=full_teardown new_session=true");
            }
            else
            {
                Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=full_teardown new_session=false reason='disabled_or_no_capture'");
            }
            return;
        }

        // Close playback before cycling the sink so active decoders release segment files.
        var oldPlaybackController = _flashbackPlaybackController;
        _flashbackPlaybackController = null;
        var preservedInPoint = !effectivePurgeSegments ? oldPlaybackController?.InPoint : null;
        var preservedOutPoint = !effectivePurgeSegments ? oldPlaybackController?.OutPoint : null;
        if (oldPlaybackController != null)
        {
            try
            {
                oldPlaybackController.GoLive();
                oldPlaybackController.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_PLAYBACK_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
            }
        }

        // Detach audio/video feeds from the old sink
        try { _microphoneCapture?.SetAudioWriter(null); }
        catch (Exception detachEx) { Logger.Log($"FLASHBACK_CYCLE_DETACH_WARN target=microphone type={detachEx.GetType().Name} msg={detachEx.Message}"); }
        try { _wasapiAudioCapture?.DetachFlashbackSink(); }
        catch (Exception detachEx) { Logger.Log($"FLASHBACK_CYCLE_DETACH_WARN target=audio type={detachEx.GetType().Name} msg={detachEx.Message}"); }
        try { unifiedVideoCapture.SetFlashbackSink(null); }
        catch (Exception detachEx) { Logger.Log($"FLASHBACK_CYCLE_DETACH_WARN target=video type={detachEx.GetType().Name} msg={detachEx.Message}"); }
        oldSink.FrameEncoded -= OnFlashbackFrameEncoded;
        var committedCycleToken = CancellationToken.None;

        // Stop and dispose the old sink (leaves buffer manager and segments intact)
        try
        {
            await oldSink.StopAsync(committedCycleToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            Logger.Log($"FLASHBACK_CYCLE_STOP_CANCEL_DEFERRED type={ex.GetType().Name} msg={ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_CYCLE_STOP_WARN type={ex.GetType().Name} msg={ex.Message}");
        }

        try
        {
            await oldSink.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_CYCLE_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
        }

        // From this point on the old sink is no longer a usable backend. Keep
        // cancellation deferred until a replacement is attached or teardown is complete.
        _flashbackSink = null;
        _flashbackBackendSettings = null;

        var oldSinkCompletionTask = oldSink.EncodingCompletionTask;
        if (!oldSinkCompletionTask.IsCompleted)
        {
            Logger.Log("FLASHBACK_CYCLE_DISPOSE_DEFERRED - falling back to full teardown");
            var oldExporter = _flashbackExporter;

            _flashbackSink = null;
            _flashbackBufferManager = null;
            _flashbackExporter = null;
            _flashbackPlaybackController = null;
            _flashbackBackendSettings = null;

            ScheduleDeferredFlashbackBackendCleanup(
                oldSinkCompletionTask,
                bufferManager,
                oldExporter,
                reason: "buffer_cycle_deferred_cleanup",
                purgeSegments: effectivePurgeSegments);

            await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, _currentSettings, committedCycleToken).ConfigureAwait(false);
            Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=deferred_full_rebuild");
            cancellationToken.ThrowIfCancellationRequested();
            return;
        }

        // When the codec/format changed, purge stale segments (incompatible with
        // new encoder) and reset PTS so the new encoder starts fresh from 0.
        // After stop-recording, keep everything — segments, PTS range, and
        // buffer state — so the user can immediately scrub/export DVR history.
        if (effectivePurgeSegments)
        {
            bufferManager.ResetLatestPts();
            bufferManager.PurgeCompletedSegments();

            // If some segments couldn't be deleted (e.g., playback has files locked),
            // fall back to full teardown to avoid mixed-codec segments in the buffer.
            if (bufferManager.SegmentCount > 0)
            {
                Logger.Log($"FLASHBACK_CYCLE_PURGE_INCOMPLETE remaining={bufferManager.SegmentCount} — falling back to full teardown");
                await DisposeFlashbackPreviewBackendCoreAsync(committedCycleToken, purgeSegments: effectivePurgeSegments).ConfigureAwait(false);
                await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, _currentSettings, committedCycleToken).ConfigureAwait(false);
                Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=purge_fallback_rebuild");
                cancellationToken.ThrowIfCancellationRequested();
                return;
            }
        }

        // Ensure the new sink gets a fresh segment file (not the old sink's active path).
        bufferManager.FinalizeActiveSegmentForCycle();

        // Create and start a new encoder sink on the same buffer manager
        var newSink = new FlashbackEncoderSink(bufferManager);
        newSink.SetFatalErrorCallback(OnFlashbackBackendFatalError);
        try
        {
            // When preserving DVR history (no purge), continue PTS from where
            // the old sink left off so new segments don't overlap existing ones.
            var ptsOffset = effectivePurgeSegments ? TimeSpan.Zero : bufferManager.LatestPts;
            await newSink.StartAsync(
                CreateFlashbackSessionContext(unifiedVideoCapture, _currentSettings),
                committedCycleToken,
                ptsBaseOffset: ptsOffset).ConfigureAwait(false);

            newSink.FrameEncoded += OnFlashbackFrameEncoded;
            _flashbackSink = newSink;
            _flashbackBackendSettings = CloneCaptureSettings(_currentSettings);
            ClearLastFlashbackFailure();

            // Reattach feeds
            unifiedVideoCapture.SetFlashbackSink(newSink);
            AttachFlashbackAudioIfSupported(_wasapiAudioCapture, "buffer_cycle");
            if (_microphoneCapture != null && newSink.MicrophoneEnabled)
            {
                _microphoneCapture.SetAudioWriter(samples => newSink.WriteMicrophoneAudioAsync(samples));
                Logger.Log("FLASHBACK_MIC_ATTACH_OK reason='buffer_cycle'");
            }

            var playbackController = new FlashbackPlaybackController(bufferManager);
            playbackController.GpuDecodeEnabled = _currentSettings.FlashbackGpuDecode;
            playbackController.InPoint = preservedInPoint;
            playbackController.OutPoint = preservedOutPoint;
            if (_previewFrameSink != null)
            {
                playbackController.Initialize(_previewFrameSink, unifiedVideoCapture, _wasapiAudioPlayback, _wasapiAudioCapture);
            }
            _flashbackPlaybackController = playbackController;

            Logger.Log($"FLASHBACK_BUFFER_CYCLE_OK mode=sink_only segments={bufferManager.SegmentCount} buffered={bufferManager.BufferedDuration.TotalSeconds:F1}s");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_FAIL type={ex.GetType().Name} error='{ex.Message}' — falling back to full teardown");
            try { newSink.FrameEncoded -= OnFlashbackFrameEncoded; }
            catch (Exception detachEx) { Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_EVENT_DETACH_WARN type={detachEx.GetType().Name} msg={detachEx.Message}"); }
            try { unifiedVideoCapture.SetFlashbackSink(null); }
            catch (Exception detachEx) { Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_DETACH_WARN type={detachEx.GetType().Name} msg={detachEx.Message}"); }
            try { _wasapiAudioCapture?.DetachFlashbackSink(); }
            catch (Exception detachEx) { Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_AUDIO_DETACH_WARN type={detachEx.GetType().Name} msg={detachEx.Message}"); }
            try { _microphoneCapture?.SetAudioWriter(null); }
            catch (Exception detachEx) { Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_MIC_DETACH_WARN type={detachEx.GetType().Name} msg={detachEx.Message}"); }
            try { await newSink.DisposeAsync().ConfigureAwait(false); }
            catch (Exception disposeEx) { Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_DISPOSE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }
            _flashbackSink = null;
            _flashbackBackendSettings = null;

            // Full teardown and rebuild
            await DisposeFlashbackPreviewBackendCoreAsync(committedCycleToken, purgeSegments: effectivePurgeSegments).ConfigureAwait(false);
            await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, _currentSettings, committedCycleToken).ConfigureAwait(false);
            Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=fallback_full_rebuild");
        }

        if (cancellationToken.IsCancellationRequested)
        {
            Logger.Log("FLASHBACK_BUFFER_CYCLE_CANCEL_DEFERRED");
            cancellationToken.ThrowIfCancellationRequested();
        }
        }
        finally
        {
            ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, "flashback_buffer_cycle");
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
        var backendLeaseHeld = false;
        bool outerPauseApplied = false;
        var bufferManager = _flashbackBufferManager;
        try
        {
            await _flashbackBackendLeaseLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            backendLeaseHeld = true;
            bufferManager?.PauseEviction();
            outerPauseApplied = bufferManager != null;

            var endResult = await flashbackSink.EndRecordingAsync(cancellationToken).ConfigureAwait(false);
            if (!endResult.Succeeded)
                return endResult;

            var startPts = flashbackSink.LastRecordingStartPts;
            var endPts = flashbackSink.LastRecordingEndPts;

            var exportResult = await ExportFlashbackCoreAsync(
                    startPts,
                    endPts,
                    outputPath,
                    progress: null,
                    ct: cancellationToken,
                    requireCompleteLiveEdge: true)
                .ConfigureAwait(false);

            exportResult = PreserveFlashbackEndArtifactsOnFailure(exportResult, endResult);
            if (exportResult.Succeeded)
            {
                Logger.Log($"FLASHBACK_RECORDING_EXPORT_OK output='{outputPath}' start_ms={(long)startPts.TotalMilliseconds} end_ms={(long)endPts.TotalMilliseconds} status='{exportResult.StatusMessage}'");
            }
            else
            {
                Logger.Log($"FLASHBACK_RECORDING_EXPORT_FAIL output='{outputPath}' start_ms={(long)startPts.TotalMilliseconds} end_ms={(long)endPts.TotalMilliseconds} status='{exportResult.StatusMessage}'");
            }
            return exportResult;
        }
        finally
        {
            if (outerPauseApplied)
                ResumeFlashbackEvictionBestEffort(bufferManager, "flashback_recording_finalize");
            ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);
        }
    }

    private static FinalizeResult PreserveFlashbackEndArtifactsOnFailure(
        FinalizeResult exportResult,
        FinalizeResult endResult)
    {
        if (exportResult.Succeeded || endResult.PreservedArtifacts.Count == 0)
        {
            return exportResult;
        }

        return FinalizeResult.Failure(
            exportResult.OutputPath,
            exportResult.StatusMessage,
            exportResult.PreservedArtifacts.Concat(endResult.PreservedArtifacts));
    }

    private void OnUnifiedVideoCaptureFatalError(object? sender, Exception ex)
    {
        Logger.Log($"UNIFIED_VIDEO_CAPTURE_FATAL type={ex.GetType().Name} msg={ex.Message}");
        if (_isRecording)
        {
            RecordLastRecordingFailure(ex);
        }

        if (_flashbackSink != null)
        {
            RecordLastFlashbackFailure(ex);
        }

        BeginFatalCaptureCleanup(ex);
    }

    private void OnRecordingBackendFatalError(Exception ex)
    {
        Logger.Log($"RECORDING_BACKEND_FATAL type={ex.GetType().Name} msg={ex.Message}");
        if (_isRecording)
        {
            RecordLastRecordingFailure(ex);
        }

        BeginFatalCaptureCleanup(ex);
    }

    private void OnFlashbackBackendFatalError(Exception ex)
    {
        Logger.Log($"FLASHBACK_BACKEND_FATAL type={ex.GetType().Name} msg={ex.Message}");
        var flashbackIsRecordingBackend = IsFlashbackRecordingBackendOwnedByRecording();
        if (flashbackIsRecordingBackend)
        {
            RecordLastRecordingFailure(ex);
        }

        if (_flashbackSink != null)
        {
            RecordLastFlashbackFailure(ex);
        }

        if (flashbackIsRecordingBackend)
        {
            BeginFatalCaptureCleanup(ex);
            return;
        }

        BeginFlashbackBackendCleanup(ex);
    }

    private void RecordLastRecordingFailure(Exception ex)
    {
        lock (_recordingFailureTelemetryLock)
        {
            _lastRecordingEncodingFailed = true;
            _lastRecordingEncodingFailureType = ex.GetType().Name;
            _lastRecordingEncodingFailureMessage = ex.Message;
        }
    }

    private void RecordLastFlashbackFailure(Exception ex)
    {
        lock (_recordingFailureTelemetryLock)
        {
            _lastFlashbackEncodingFailed = true;
            _lastFlashbackEncodingFailureType = ex.GetType().Name;
            _lastFlashbackEncodingFailureMessage = ex.Message;
        }
    }

    private void ClearLastRecordingFailure()
    {
        lock (_recordingFailureTelemetryLock)
        {
            _lastRecordingEncodingFailed = false;
            _lastRecordingEncodingFailureType = null;
            _lastRecordingEncodingFailureMessage = null;
        }
    }

    private void ClearLastFlashbackFailure()
    {
        lock (_recordingFailureTelemetryLock)
        {
            _lastFlashbackEncodingFailed = false;
            _lastFlashbackEncodingFailureType = null;
            _lastFlashbackEncodingFailureMessage = null;
        }
    }

    private (
        bool RecordingFailed,
        string? RecordingFailureType,
        string? RecordingFailureMessage,
        bool FlashbackFailed,
        string? FlashbackFailureType,
        string? FlashbackFailureMessage) GetLastFailureTelemetry()
    {
        lock (_recordingFailureTelemetryLock)
        {
            return (
                _lastRecordingEncodingFailed,
                _lastRecordingEncodingFailureType,
                _lastRecordingEncodingFailureMessage,
                _lastFlashbackEncodingFailed,
                _lastFlashbackEncodingFailureType,
                _lastFlashbackEncodingFailureMessage);
        }
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
                await _sessionTransitionLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);
                try
                {
                    if (Interlocked.Read(ref _sessionGeneration) != generationAtFault)
                    {
                        Logger.Log("FATAL_CLEANUP_SKIP_STALE reason='session_generation_changed_before_cleanup'");
                        return;
                    }

                    _sessionState = CaptureSessionState.CleaningUp;

                    // Stop the preview renderer before disposing the shared D3D11
                    // device. Same race as the reinit crash: the renderer may be
                    // calling VideoProcessorBlt/Present on the shared device when
                    // cleanup disposes it.
                    try { PreCleanupRequested?.Invoke(); }
                    catch (Exception preEx) { Logger.Log($"PreCleanupRequested handler warning: {preEx.Message}"); }

                    await CleanupCoreAsync(CancellationToken.None).ConfigureAwait(false);
                    _sessionState = CaptureSessionState.Faulted;
                    StatusChanged?.Invoke(this, $"Video capture error: {ex.Message}");
                    ErrorOccurred?.Invoke(this, ex);
                }
                finally
                {
                    ReleaseSemaphoreBestEffort(_sessionTransitionLock, "fatal_capture_cleanup");
                }
            }
            catch (Exception cleanupEx)
            {
                Logger.Log($"Fatal capture cleanup warning: {cleanupEx.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _fatalCleanupInProgress, 0);
            }
        });
    }

    private void BeginFlashbackBackendCleanup(Exception ex)
    {
        if (Volatile.Read(ref _fatalCleanupInProgress) != 0 ||
            Interlocked.Exchange(ref _flashbackCleanupInProgress, 1) != 0)
        {
            return;
        }

        var generationAtFault = Interlocked.Read(ref _sessionGeneration);

        _ = Task.Run(async () =>
        {
            try
            {
                await _sessionTransitionLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);
                try
                {
                    if (Interlocked.Read(ref _sessionGeneration) != generationAtFault)
                    {
                        Logger.Log("FLASHBACK_FATAL_CLEANUP_SKIP_STALE reason='session_generation_changed_before_cleanup'");
                        return;
                    }

                    var preserveDedicatedRecordingMic = _isRecording && !IsFlashbackRecordingBackendActive();
                    await DisposeFlashbackPreviewBackendAsync(
                        CancellationToken.None,
                        purgeSegments: true,
                        detachMicrophoneWriter: !preserveDedicatedRecordingMic).ConfigureAwait(false);

                    StatusChanged?.Invoke(this, $"Flashback error: {ex.Message}");
                }
                finally
                {
                    ReleaseSemaphoreBestEffort(_sessionTransitionLock, "flashback_backend_cleanup");
                }
            }
            catch (Exception cleanupEx)
            {
                Logger.Log($"Flashback backend cleanup warning: {cleanupEx.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _flashbackCleanupInProgress, 0);
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
                try
                {
                    mic.SetAudioWriter(null);
                }
                catch (Exception detachEx)
                {
                    Logger.Log($"MIC_MONITOR_WRITER_DETACH_WARN type={detachEx.GetType().Name} msg={detachEx.Message}");
                }

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
            var previousEnabled = _micMonitorEnabled;
            var previousDeviceId = _micMonitorDeviceId;
            var previousDeviceName = _micMonitorDeviceName;
            WasapiAudioCapture? nextMicCapture = null;
            try
            {
                transitionToken.ThrowIfCancellationRequested();
                if (_isRecording)
                {
                    _micMonitorEnabled = enabled;
                    _micMonitorDeviceId = deviceId;
                    _micMonitorDeviceName = deviceName;
                    Logger.Log("MIC_MONITOR_UPDATE_DEFERRED recording=true");
                    return;
                }

                if (enabled && !_isRecording && _isVideoPreviewActive && !string.IsNullOrWhiteSpace(deviceId))
                {
                    nextMicCapture = new WasapiAudioCapture();
                    await nextMicCapture.InitializeAsync(deviceId, transitionToken).ConfigureAwait(false);
                    nextMicCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
                    nextMicCapture.CaptureFailed += OnWasapiCaptureFailed;
                    nextMicCapture.Start();
                    if (_flashbackSink is { MicrophoneEnabled: true } fbSink)
                    {
                        nextMicCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
                        Logger.Log("FLASHBACK_MIC_ATTACH_OK reason='mic_monitor_update'");
                    }
                }

                await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);

                _micMonitorEnabled = enabled;
                _micMonitorDeviceId = deviceId;
                _micMonitorDeviceName = deviceName;
                _microphoneCapture = nextMicCapture;
                nextMicCapture = null;

                if (_microphoneCapture != null)
                {
                    Logger.Log("MIC_MONITOR_START device='" + (deviceName ?? "?") + "'");
                }
                else
                {
                    MicrophoneAudioLevelUpdated?.Invoke(this, new AudioLevelEventArgs(0, 0, false));
                }
            }
            catch
            {
                _micMonitorEnabled = previousEnabled;
                _micMonitorDeviceId = previousDeviceId;
                _micMonitorDeviceName = previousDeviceName;
                if (nextMicCapture != null)
                {
                    try
                    {
                        nextMicCapture.SetAudioWriter(null);
                        nextMicCapture.AudioLevelUpdated -= OnMicrophoneAudioLevelUpdated;
                        nextMicCapture.CaptureFailed -= OnWasapiCaptureFailed;
                        await nextMicCapture.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("Microphone capture rollback dispose failed: " + ex.Message);
                    }
                }

                throw;
            }
        }, cancellationToken);

    private void OnWasapiCaptureFailed(object? sender, Exception ex)
    {
        var source = ReferenceEquals(sender, _wasapiAudioCapture)
            ? "program"
            : ReferenceEquals(sender, _microphoneCapture)
                ? "microphone"
                : "unknown";

        if (_isRecording)
        {
            Volatile.Write(ref _wasapiAudioCaptureFaulted, true);
            Volatile.Write(ref _wasapiAudioCaptureFaultMessage, $"{source}: {ex.Message}");
        }

        Logger.Log($"WASAPI_CAPTURE_FAILED source={source} type={ex.GetType().Name} hr=0x{ex.HResult:X8} message={ex.Message} recording={_isRecording}");
        var statusPrefix = source == "microphone" ? "Microphone capture error" : "Audio capture error";
        StatusChanged?.Invoke(this, $"{statusPrefix}: {ex.Message}");
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
            var newPlayback = new WasapiAudioPlayback();
            try
            {
                await newPlayback.InitializeAsync(cancellationToken).ConfigureAwait(false);
                newPlayback.SetVolume(0);
                newPlayback.Start();
                _wasapiAudioPlayback = newPlayback;
                Logger.Log("WASAPI audio playback started.");
                newPlayback.SetVolume(_isMonitoringMuted ? 0f : _previewVolume);
                playback = newPlayback;
            }
            catch (Exception ex)
            {
                Logger.Log($"WASAPI_PLAYBACK_START_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
                if (ReferenceEquals(_wasapiAudioPlayback, newPlayback))
                {
                    _wasapiAudioPlayback = null;
                }
                StopWasapiPlaybackBestEffort(newPlayback, "start_fail");
                DisposeWasapiPlaybackBestEffort(newPlayback);
                throw;
            }
        }

        try
        {
            capture.SetPlayback(playback);
        }
        catch (Exception ex)
        {
            Logger.Log($"WASAPI_PLAYBACK_ATTACH_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
            StopWasapiPlayback();
            throw;
        }

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
        SafeClearWasapiCapturePlayback(_wasapiAudioCapture, "stop_playback");
        if (playback != null)
        {
            StopWasapiPlaybackBestEffort(playback, "stop_playback");
            DisposeWasapiPlaybackBestEffort(playback);
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
        SafeClearWasapiCapturePlayback(capture, "detach_capture");
        StopWasapiPlayback();
    }

    private static void SafeClearWasapiCapturePlayback(WasapiAudioCapture? capture, string operation)
    {
        if (capture == null)
        {
            return;
        }

        try
        {
            capture.SetPlayback(null);
        }
        catch (Exception ex)
        {
            Logger.Log($"WASAPI audio playback detach warning op={operation}: {ex.Message}");
        }
    }

    private static void DisposeWasapiPlaybackBestEffort(WasapiAudioPlayback playback)
    {
        try
        {
            playback.Dispose();
            Logger.Log("WASAPI audio playback disposed.");
        }
        catch (Exception ex)
        {
            Logger.Log($"WASAPI audio playback dispose warning: {ex.Message}");
        }
    }

    private static void StopWasapiPlaybackBestEffort(WasapiAudioPlayback playback, string operation)
    {
        try
        {
            playback.Stop();
        }
        catch (Exception ex)
        {
            Logger.Log($"WASAPI audio playback stop warning op={operation}: {ex.Message}");
        }
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

        return d3dSink.CaptureNextFrameAsync(outputPath, cancellationToken);
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
               string.Equals(current.NvencPreset, next.NvencPreset, StringComparison.OrdinalIgnoreCase) &&
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
        await DisposeFlashbackPreviewBackendAsync(
                transitionToken,
                purgeSegments: ResolveFlashbackSegmentPurge(
                    purgeFlashbackSegments,
                    "preview_pipeline_dispose"))
            .ConfigureAwait(false);

        ClearPendingLibAvDrainTaskIfCompletedSuccessfully();

        var unifiedVideoCapture = _unifiedVideoCapture;
        _unifiedVideoCapture = null;
        if (unifiedVideoCapture != null)
        {
            CacheMjpegTimingMetrics(unifiedVideoCapture);
            _lastMfSourceReaderFramesDelivered = unifiedVideoCapture.VideoFramesArrived;
            _lastMfSourceReaderFramesDropped = unifiedVideoCapture.VideoFramesDropped;
            _lastMfSourceReaderNegotiatedFormat = unifiedVideoCapture.NegotiatedFormat;
            DetachUnifiedVideoCapture(unifiedVideoCapture);
            if (_pendingLibAvDrainTask is { IsCompleted: false } pendingLibAvDrainTask)
            {
                _pendingLibAvDrainTask = ScheduleDeferredUnifiedVideoCaptureCleanup(
                    pendingLibAvDrainTask,
                    unifiedVideoCapture,
                    reason: "dispose_preview_pipeline_after_deferred_recording");
            }
            else
            {
                await unifiedVideoCapture.StopAsync().ConfigureAwait(false);
                await unifiedVideoCapture.DisposeAsync().ConfigureAwait(false);
            }
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

            var createdCaptureForAudioPreview = false;
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
                    createdCaptureForAudioPreview = true;
                    _avSyncBaselineDriftMs = double.NaN;
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
            try
            {
                AttachFlashbackAudioIfSupported(_wasapiAudioCapture, "audio_preview_start");
                await StartWasapiPlaybackAsync(transitionToken).ConfigureAwait(false);
            }
            catch
            {
                _isAudioPreviewActive = false;
                if (createdCaptureForAudioPreview)
                {
                    var capture = _wasapiAudioCapture;
                    _wasapiAudioCapture = null;
                    DetachWasapiAudioCapture(capture);
                    if (capture != null)
                    {
                        try
                        {
                            await capture.DisposeAsync().ConfigureAwait(false);
                        }
                        catch (Exception disposeEx)
                        {
                            Logger.Log($"AUDIO_PREVIEW_START_ROLLBACK_DISPOSE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}");
                        }
                    }
                }

                throw;
            }

            StatusChanged?.Invoke(this, "Audio preview started");
        }, cancellationToken);

    public Task StopAudioPreviewAsync(CancellationToken cancellationToken = default)
        => StopAudioPreviewCoreAsync(teardownCapture: false, cancellationToken);

    public Task StopAudioPreviewWithTeardownAsync(CancellationToken cancellationToken = default)
        => StopAudioPreviewCoreAsync(teardownCapture: true, cancellationToken);

    private Task StopAudioPreviewCoreAsync(bool teardownCapture, CancellationToken cancellationToken = default)
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
            var previousDeviceName = _audioDeviceName;

            if (string.Equals(previousDeviceId, audioDeviceId, StringComparison.OrdinalIgnoreCase))
            {
                _audioDeviceName = audioDeviceName;
                return;
            }

            if (_wasapiAudioCapture == null)
            {
                _audioDeviceId = audioDeviceId;
                _audioDeviceName = audioDeviceName;
                return;
            }

            Logger.Log($"Live audio input switch: {audioDeviceName ?? "(card default)"}");

            var activeSink = _isRecording ? _recordingSink : null;
            var oldCapture = _wasapiAudioCapture;
            var committedSwitchToken = CancellationToken.None;

            var resolvedId = audioDeviceId ?? _currentDevice?.AudioDeviceId;
            if (!string.IsNullOrEmpty(resolvedId))
            {
                var newCapture = new WasapiAudioCapture();
                try
                {
                    await newCapture.InitializeAsync(resolvedId, committedSwitchToken).ConfigureAwait(false);
                    newCapture.AudioLevelUpdated += OnWasapiAudioLevelUpdated;
                    newCapture.CaptureFailed += OnWasapiCaptureFailed;
                    newCapture.Start();
                }
                catch
                {
                    _audioDeviceId = previousDeviceId;
                    _audioDeviceName = previousDeviceName;
                    try
                    {
                        newCapture.AudioLevelUpdated -= OnWasapiAudioLevelUpdated;
                        newCapture.CaptureFailed -= OnWasapiCaptureFailed;
                        await newCapture.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"AUDIO_INPUT_SWITCH_NEW_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
                    }

                    throw;
                }

                DetachWasapiAudioCapture(oldCapture);
                _wasapiAudioCapture = newCapture;
                _audioDeviceId = audioDeviceId;
                _audioDeviceName = audioDeviceName;
                Volatile.Write(ref _wasapiAudioCaptureFaulted, false);
                Volatile.Write(ref _wasapiAudioCaptureFaultMessage, null);

                AttachFlashbackAudioIfSupported(newCapture, "audio_input_switch");

                if (activeSink != null && !ReferenceEquals(activeSink, _flashbackSink))
                {
                    newCapture.AttachRecordingSink(activeSink);
                }

                try
                {
                    if (_isAudioPreviewActive)
                    {
                        await StartWasapiPlaybackAsync(committedSwitchToken).ConfigureAwait(false);
                    }

                    Logger.Log($"Audio input switched to: {audioDeviceName ?? resolvedId}");
                }
                finally
                {
                    try
                    {
                        await oldCapture.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"AUDIO_INPUT_SWITCH_OLD_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
                    }
                }
            }
            else
            {
                _audioDeviceId = audioDeviceId;
                _audioDeviceName = audioDeviceName;
                _wasapiAudioCapture = null;
                DetachWasapiAudioCapture(oldCapture);
                try
                {
                    await oldCapture.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Log($"AUDIO_INPUT_SWITCH_OLD_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
                }

                Logger.Log("Audio input cleared — no device available");
                AudioLevelUpdated?.Invoke(this, new AudioLevelEventArgs(0, 0, false));
            }

            if (transitionToken.IsCancellationRequested)
            {
                Logger.Log("AUDIO_INPUT_SWITCH_CANCEL_DEFERRED");
                transitionToken.ThrowIfCancellationRequested();
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
                    transitionToken).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    Logger.Log($"Cleanup stop reported issues: {result.StatusMessage}");
                    if (stoppingFlashbackRecording)
                    {
                        PreserveFlashbackRecoverySegments("cleanup_stop_failed");
                        preserveFlashbackSegmentsAfterFailedRecordingFinalize = true;
                    }
                }
            }
            catch (OperationCanceledException) when (transitionToken.IsCancellationRequested)
            {
                cancellationRequested = true;
                if (stoppingFlashbackRecording)
                {
                    PreserveFlashbackRecoverySegments("cleanup_stop_cancelled");
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

    private async Task<FinalizeResult> StopAndDisposeRecordingBackendAsync(string fallbackStatusMessage, CancellationToken cancellationToken)
    {
        // --- Unified flashback recording path: remux from .ts, cycle buffer ---
        if (IsFlashbackRecordingBackendActive())
        {
            var flashbackSink = _flashbackSink!;
            var fbRecordingContext = _recordingContext;
            var fbOutputPath = fbRecordingContext?.FinalOutputPath ?? (_lastOutputPath ?? string.Empty);

            Volatile.Write(ref _flashbackRecordingFinalizeInProgress, 1);
            _recordingSink = null;
            // Don't null _flashbackSink — it continues for the buffer

            FinalizeResult fbResult;
            OperationCanceledException? flashbackCancellationException = null;
            try
            {
                try
                {
                    fbResult = await FinalizeFlashbackRecordingAsync(flashbackSink, fbRecordingContext, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    Volatile.Write(ref _flashbackRecordingFinalizeInProgress, 0);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                flashbackCancellationException = new OperationCanceledException(cancellationToken);
                fbResult = FinalizeResult.Failure(fbOutputPath, "Flashback recording finalize cancelled.");
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_UNIFIED_RECORDING_FINALIZE_FAIL type={ex.GetType().Name} error='{ex.Message}'");
                fbResult = FinalizeResult.Failure(fbOutputPath, $"Flashback recording finalize failed: {ex.Message}");
            }

            var flashbackVideoCapture = _unifiedVideoCapture;
            var recordingFramesDelivered = 0L;
            var recordingFramesEnqueued = 0L;
            if (flashbackVideoCapture != null)
            {
                flashbackVideoCapture.EndFlashbackRecordingAccounting();
                _lastMfSourceReaderFramesDelivered = flashbackVideoCapture.VideoFramesArrived;
                _lastMfSourceReaderFramesDropped = flashbackVideoCapture.VideoFramesDropped;
                _lastMfSourceReaderNegotiatedFormat = flashbackVideoCapture.NegotiatedFormat;
                recordingFramesDelivered = flashbackVideoCapture.RecordingFramesDelivered;
                recordingFramesEnqueued = flashbackVideoCapture.VideoFramesWrittenToSink;
                Logger.Log(
                    "VIDEO_DIAG flashback_recording_pipeline " +
                    $"source_frames_during_recording={recordingFramesDelivered} " +
                    $"frames_accepted_by_flashback={recordingFramesEnqueued} " +
                    $"pipeline_drops={recordingFramesDelivered - recordingFramesEnqueued}");
            }

            _lastRecordingIntegrity = BuildRecordingIntegritySummary(
                backend: "Flashback",
                recordingActive: false,
                finalizeSucceeded: fbResult.Succeeded,
                finalizeStatus: fbResult.StatusMessage,
                completedUtc: DateTimeOffset.UtcNow,
                sourceFrames: recordingFramesDelivered,
                acceptedFrames: recordingFramesEnqueued,
                counters: CaptureFlashbackRecordingIntegrityCountersSinceBaseline(flashbackSink, flashbackVideoCapture),
                audioCounters: GetRecordingAudioCountersSinceBaseline(
                    CaptureRecordingAudioCounters(_wasapiAudioCapture, flashbackSink, _activeRecordingSettings)));
            _recordingIntegrityCounterBaseline = null;
            _recordingIntegrityAudioBaseline = null;
            LogRecordingIntegritySummary(_lastRecordingIntegrity);

            // If settings changed during recording (format, buffer duration, etc.),
            // do a full restart to apply them. Otherwise just cycle the sink to
            // preserve DVR history.
            try
            {
                if (!fbResult.Succeeded)
                {
                    var hadPendingFlashbackSettingsChange = _pendingFlashbackSettingsChange;
                    _pendingFlashbackSettingsChange = false;
                    PreserveFlashbackRecoverySegments("recording_finalize_failed");
                    Logger.Log(
                        "FLASHBACK_SETTINGS_APPLY_AFTER_RECORDING_DEFERRED " +
                        $"reason=recording_finalize_failed pending_settings={hadPendingFlashbackSettingsChange}");
                }
                else if (_pendingFlashbackSettingsChange)
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                flashbackCancellationException ??= new OperationCanceledException(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_BUFFER_CYCLE_FAIL type={ex.GetType().Name} error='{ex.Message}'");
            }

            _recordingStopwatch.Stop();
            _isRecording = false;
            if (!_isVideoPreviewActive) await StopTelemetryPollAsync().ConfigureAwait(false);
            _recordingContext = null;
            _activeRecordingSettings = null;
            _lastFinalizeStatus = fbResult.StatusMessage;
            _lastFinalizeUtc = DateTimeOffset.UtcNow;
            _lastPreservedArtifacts = fbResult.PreservedArtifacts;

            // Restart mic monitoring if preview is still active
            if (_isVideoPreviewActive && _micMonitorEnabled && !string.IsNullOrWhiteSpace(_micMonitorDeviceId))
            {
                WasapiAudioCapture? micCapture = null;
                try
                {
                    if (_microphoneCapture == null)
                    {
                        micCapture = new WasapiAudioCapture();
                        await micCapture.InitializeAsync(_micMonitorDeviceId, cancellationToken).ConfigureAwait(false);
                        micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
                        micCapture.CaptureFailed += OnWasapiCaptureFailed;
                        micCapture.Start();
                        if (_flashbackSink is { MicrophoneEnabled: true } fbSink)
                        {
                            micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
                        }
                        _microphoneCapture = micCapture;
                        micCapture = null;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    flashbackCancellationException ??= new OperationCanceledException(cancellationToken);
                }
                catch (Exception ex)
                {
                    Logger.Log($"FLASHBACK_MIC_RESTART_WARN type={ex.GetType().Name} error='{ex.Message}'");
                }
                finally
                {
                    if (micCapture != null)
                    {
                        micCapture.AudioLevelUpdated -= OnMicrophoneAudioLevelUpdated;
                        micCapture.CaptureFailed -= OnWasapiCaptureFailed;
                        try { await micCapture.DisposeAsync().ConfigureAwait(false); }
                        catch (Exception disposeEx) { Logger.Log($"FLASHBACK_MIC_RESTART_DISPOSE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }
                    }
                }
            }

            if (fbResult.Succeeded)
            {
                Logger.Log($"FLASHBACK_UNIFIED_RECORDING_STOP_OK output='{fbResult.OutputPath}'");
            }
            else
            {
                Logger.Log($"FLASHBACK_UNIFIED_RECORDING_STOP_FAIL output='{fbResult.OutputPath}'");
            }
            if (flashbackCancellationException != null)
            {
                throw flashbackCancellationException;
            }

            return fbResult;
        }

        // --- Standard LibAvRecordingSink path ---
        var sink = _recordingSink;
        var libAvSink = _libavSink;
        var recordingContext = _recordingContext;
        var fallbackOutputPath = recordingContext?.FinalOutputPath ?? (_lastOutputPath ?? string.Empty);

        _recordingSink = null;
        _libavSink = null;
        _pendingLibAvDrainTask = null;

        var result = FinalizeResult.Success(fallbackOutputPath, fallbackStatusMessage);
        OperationCanceledException? cancellationException = null;

        var unifiedVideoCapture = _unifiedVideoCapture;
        var recordingFramesDeliveredToBoundary = 0L;
        var recordingFramesAcceptedByBoundary = 0L;
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
                if (cancellationException == null && result.Succeeded)
                {
                    result = FinalizeResult.Failure(fallbackOutputPath, $"Unified video recording stop failed: {ex.Message}");
                }
            }
            finally
            {
                // Keep SkipCpuReadback=true — preview uses GPU textures, not CPU bytes.
                // Lock2D is never needed while D3D shared device is active.
            }

            _lastMfSourceReaderFramesDelivered = unifiedVideoCapture.VideoFramesArrived;
            _lastMfSourceReaderFramesDropped = unifiedVideoCapture.VideoFramesDropped;
            _lastMfSourceReaderNegotiatedFormat = unifiedVideoCapture.NegotiatedFormat;
            recordingFramesDeliveredToBoundary = unifiedVideoCapture.RecordingFramesDelivered;
            recordingFramesAcceptedByBoundary = unifiedVideoCapture.VideoFramesWrittenToSink;
            Logger.Log(
                "VIDEO_DIAG mf_source_reader " +
                $"frames_delivered={_lastMfSourceReaderFramesDelivered} " +
                $"frames_dropped={_lastMfSourceReaderFramesDropped} " +
                $"negotiated_format='{_lastMfSourceReaderNegotiatedFormat ?? "unknown"}'");
            Logger.Log(
                "VIDEO_DIAG recording_pipeline " +
                $"source_frames_during_recording={recordingFramesDeliveredToBoundary} " +
                $"frames_enqueued_to_encoder={recordingFramesAcceptedByBoundary} " +
                $"pipeline_drops={recordingFramesDeliveredToBoundary - recordingFramesAcceptedByBoundary}");
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
                var sinkResult = await sink.StopAsync(cancellationToken).ConfigureAwait(false);
                if (result.Succeeded)
                {
                    result = sinkResult;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                cancellationException = new OperationCanceledException(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.Log($"Recording sink stop failed: {ex.Message}");
                if (result.Succeeded)
                {
                    result = FinalizeResult.Failure(fallbackOutputPath, $"Recording stop failed: {ex.Message}");
                }
            }
            finally
            {
                try
                {
                    await sink.DisposeAsync().ConfigureAwait(false);
                    if (libAvSink != null)
                    {
                        var libAvDrainTask = libAvSink.EncodingCompletionTask;
                        if (!libAvDrainTask.IsCompleted)
                        {
                            _pendingLibAvDrainTask = libAvDrainTask;
                        }
                    }
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

        var libAvFinalAudioCounters = libAvSink != null
            ? GetRecordingAudioCountersSinceBaseline(
                CaptureRecordingAudioCounters(_wasapiAudioCapture, libAvSink, _activeRecordingSettings))
            : RecordingAudioIntegrityCounterSnapshot.Disabled;

        if (!_isVideoPreviewActive)
        {
            _unifiedVideoCapture = null;
            if (unifiedVideoCapture != null)
            {
                try
                {
                    CacheMjpegTimingMetrics(unifiedVideoCapture);
                    DetachUnifiedVideoCapture(unifiedVideoCapture);
                    if (_pendingLibAvDrainTask is { IsCompleted: false } pendingLibAvDrainTask)
                    {
                        _pendingLibAvDrainTask = ScheduleDeferredUnifiedVideoCaptureCleanup(
                            pendingLibAvDrainTask,
                            unifiedVideoCapture,
                            reason: "recording_stop_deferred_drain");
                    }
                    else
                    {
                        await unifiedVideoCapture.StopAsync().ConfigureAwait(false);
                        await unifiedVideoCapture.DisposeAsync().ConfigureAwait(false);
                    }
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
            _lastRecordingIntegrity = BuildRecordingIntegritySummary(
                backend: "LibAv",
                recordingActive: false,
                finalizeSucceeded: result.Succeeded,
                finalizeStatus: result.StatusMessage,
                completedUtc: DateTimeOffset.UtcNow,
                sourceFrames: recordingFramesDeliveredToBoundary,
                acceptedFrames: recordingFramesAcceptedByBoundary,
                counters: GetRecordingIntegrityCountersSinceBaseline(CaptureRecordingIntegrityCounters(libAvSink)),
                audioCounters: libAvFinalAudioCounters);
            _recordingIntegrityCounterBaseline = null;
            _recordingIntegrityAudioBaseline = null;
            LogRecordingIntegritySummary(_lastRecordingIntegrity);
        }

        _recordingStopwatch.Stop();
        _isRecording = false;
        if (!_isVideoPreviewActive) await StopTelemetryPollAsync().ConfigureAwait(false);
        _recordingContext = null;
        _activeRecordingSettings = null;
        _mfConvertersDisabled = false;

        if (_pendingFlashbackEnableAfterRecording)
        {
            _pendingFlashbackEnableAfterRecording = false;
            if (_flashbackEnabled && _isVideoPreviewActive && _unifiedVideoCapture != null && _currentSettings != null)
            {
                try
                {
                    await EnsureFlashbackPreviewBackendAsync(_unifiedVideoCapture, _currentSettings, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    cancellationException ??= new OperationCanceledException(cancellationToken);
                    _flashbackEnabled = false;
                    _pendingFlashbackEnableAfterRecording = false;
                    if (_flashbackSink != null || _flashbackBufferManager != null || _flashbackExporter != null || _flashbackPlaybackController != null)
                    {
                        await DisposeFlashbackPreviewBackendAsync(CancellationToken.None, purgeSegments: true).ConfigureAwait(false);
                    }
                    Logger.Log("FLASHBACK_ENABLE_AFTER_RECORDING_CANCELLED");
                }
                catch (Exception ex)
                {
                    _flashbackEnabled = false;
                    _pendingFlashbackEnableAfterRecording = false;
                    if (_flashbackSink != null || _flashbackBufferManager != null || _flashbackExporter != null || _flashbackPlaybackController != null)
                    {
                        await DisposeFlashbackPreviewBackendAsync(CancellationToken.None, purgeSegments: true).ConfigureAwait(false);
                    }
                    Logger.Log($"FLASHBACK_ENABLE_AFTER_RECORDING_FAIL type={ex.GetType().Name} error='{ex.Message}'");
                }
            }
        }

        // Restart mic monitoring if preview is still active
        if (_isVideoPreviewActive && _micMonitorEnabled && !string.IsNullOrWhiteSpace(_micMonitorDeviceId))
        {
            WasapiAudioCapture? micCapture = null;
            try
            {
                micCapture = new WasapiAudioCapture();
                await micCapture.InitializeAsync(_micMonitorDeviceId, cancellationToken).ConfigureAwait(false);
                micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
                micCapture.CaptureFailed += OnWasapiCaptureFailed;
                micCapture.Start();
                if (_flashbackSink is { MicrophoneEnabled: true } fbSink)
                {
                    micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
                    Logger.Log("FLASHBACK_MIC_ATTACH_OK reason='mic_monitor_restart'");
                }
                _microphoneCapture = micCapture;
                micCapture = null;
                Logger.Log("MIC_MONITOR_RESTART device='" + (_micMonitorDeviceName ?? "?") + "'");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                cancellationException ??= new OperationCanceledException(cancellationToken);
            }
            catch (Exception micEx)
            {
                Logger.Log("Mic monitor restart failed (non-fatal): " + micEx.Message);
            }
            finally
            {
                if (micCapture != null)
                {
                    micCapture.AudioLevelUpdated -= OnMicrophoneAudioLevelUpdated;
                    micCapture.CaptureFailed -= OnWasapiCaptureFailed;
                    try { await micCapture.DisposeAsync().ConfigureAwait(false); }
                    catch (Exception disposeEx) { Logger.Log($"MIC_MONITOR_RESTART_DISPOSE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }
                }
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

        var d3dManager = capture.D3DManager;
        if (d3dManager == null)
        {
            return;
        }

        if (!d3dManager.TryCreateDeviceReference(out var sharedDevice, out var reason) || sharedDevice == null)
        {
            Logger.Log($"UNIFIED_VIDEO_SHARED_DEVICE_APPLY_SKIP reason={reason}");
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
        finally
        {
            sharedDevice.Dispose();
        }
    }

    private async Task DisposeTransientRecordingBackendAsync(
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
            if (sink is LibAvRecordingSink libAvSink)
            {
                var libAvDrainTask = libAvSink.EncodingCompletionTask;
                if (!libAvDrainTask.IsCompleted)
                {
                    _pendingLibAvDrainTask = ScheduleDeferredUnifiedVideoCaptureCleanup(
                        libAvDrainTask,
                        unifiedVideoCapture,
                        reason: "recording_start_rollback");
                    unifiedVideoCapture = null;
                }
            }

            try
            {
                if (unifiedVideoCapture != null)
                {
                    await unifiedVideoCapture.StopAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient unified video stop failed during rollback: {ex.Message}");
            }

            try
            {
                if (unifiedVideoCapture != null)
                {
                    await unifiedVideoCapture.DisposeAsync().ConfigureAwait(false);
                }
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
            ReleaseSemaphoreBestEffort(_sessionTransitionLock, "session_transition");
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

    private Task RefreshSourceTelemetryAsync(CancellationToken cancellationToken)
        => RefreshSourceTelemetryAsync(cancellationToken, Volatile.Read(ref _telemetryPollGeneration));

    private async Task RefreshSourceTelemetryAsync(CancellationToken cancellationToken, long pollGeneration)
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

        cancellationToken.ThrowIfCancellationRequested();
        if (pollGeneration != Volatile.Read(ref _telemetryPollGeneration))
        {
            return;
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
        lock (_telemetryPollSync)
        {
            var previousTask = _telemetryPollTask;
            StopTelemetryPollLocked();
            if (previousTask != null && !previousTask.IsCompleted)
            {
                var deferredGeneration = Volatile.Read(ref _telemetryPollGeneration);
                Logger.Log("Telemetry poll start deferred until canceled poll exits");
                _telemetryPollTask = Task.Run(async () =>
                {
                    try
                    {
                        await previousTask.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected while draining a canceled poll.
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Telemetry poll drain failed before restart: {ex.Message}");
                    }

                    lock (_telemetryPollSync)
                    {
                        if (deferredGeneration == Volatile.Read(ref _telemetryPollGeneration))
                        {
                            StartTelemetryPollCoreLocked();
                        }
                    }
                });
                return;
            }

            StartTelemetryPollCoreLocked();
        }
    }

    private void StartTelemetryPollCore()
    {
        lock (_telemetryPollSync)
        {
            StartTelemetryPollCoreLocked();
        }
    }

    private void StartTelemetryPollCoreLocked()
    {
        var generation = Interlocked.Increment(ref _telemetryPollGeneration);
        var cts = new CancellationTokenSource();
        _telemetryPollCts = cts;
        _telemetryPollTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TelemetryPollIntervalMs, cts.Token).ConfigureAwait(false);
                    await RefreshSourceTelemetryAsync(cts.Token, generation).ConfigureAwait(false);
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
        lock (_telemetryPollSync)
        {
            StopTelemetryPollLocked();
        }
    }

    private void StopTelemetryPollLocked()
    {
        Interlocked.Increment(ref _telemetryPollGeneration);
        var cts = _telemetryPollCts;
        _telemetryPollCts = null;
        cts?.Cancel();
        if (_telemetryPollTask?.IsCompleted == true)
        {
            _telemetryPollTask = null;
        }
        // Do not Dispose the CTS here — the poll task may still be checking
        // the token between Cancel and its own exit. Let GC finalize instead of
        // risking ObjectDisposedException in the poll loop's Task.Delay.
    }

    private async Task StopTelemetryPollAsync()
    {
        Task? task;
        lock (_telemetryPollSync)
        {
            task = _telemetryPollTask;
            StopTelemetryPollLocked();
        }
        if (task == null || task.IsCompleted)
        {
            return;
        }

        try
        {
            await task.WaitAsync(TimeSpan.FromMilliseconds(TelemetryPollStopDrainTimeoutMs)).ConfigureAwait(false);
            lock (_telemetryPollSync)
            {
                if (ReferenceEquals(_telemetryPollTask, task))
                {
                    _telemetryPollTask = null;
                }
            }
        }
        catch (TimeoutException)
        {
            Logger.Log($"Telemetry poll drain timed out after {TelemetryPollStopDrainTimeoutMs}ms");
        }
        catch (OperationCanceledException)
        {
            // Expected when the poll loop observes cancellation.
        }
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

    private async Task CleanupForDisposalAsync()
    {
        await _sessionTransitionLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            _sessionState = CaptureSessionState.CleaningUp;
            await CleanupCoreAsync(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            ReleaseSemaphoreBestEffort(_sessionTransitionLock, "dispose_cleanup");
        }
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0) return;
        try
        {
            Task.Run(CleanupForDisposalAsync).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Logger.Log($"CaptureService.Dispose cleanup warning: {ex.Message}");
        }

        DisposeCoordinationLocksBestEffort();
        _sessionState = CaptureSessionState.Disposed;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0) return;
        try
        {
            await CleanupForDisposalAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"CaptureService.DisposeAsync cleanup warning: {ex.Message}");
        }

        DisposeCoordinationLocksBestEffort();
        _sessionState = CaptureSessionState.Disposed;
    }

    private void DisposeCoordinationLocksBestEffort()
    {
        DisposeSemaphoreBestEffort(_sessionTransitionLock, "session_transition");
        DisposeSemaphoreBestEffort(_flashbackBackendLeaseLock, "flashback_backend_lease");
        DisposeSemaphoreBestEffort(_flashbackExportOperationLock, "flashback_export_operation");
    }

    private static void DisposeSemaphoreBestEffort(SemaphoreSlim semaphore, string operation)
    {
        try
        {
            semaphore.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"CAPTURE_SERVICE_SEMAPHORE_DISPOSE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private static void ReleaseSemaphoreBestEffort(SemaphoreSlim semaphore, string operation)
    {
        try
        {
            semaphore.Release();
        }
        catch (Exception ex)
        {
            Logger.Log($"CAPTURE_SERVICE_SEMAPHORE_RELEASE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private static void ResumeFlashbackEvictionBestEffort(FlashbackBufferManager? bufferManager, string operation)
    {
        if (bufferManager == null)
        {
            return;
        }

        try
        {
            bufferManager.ResumeEviction();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EVICTION_RESUME_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }
}
