using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Windows.Storage;
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
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
    private readonly CaptureSessionStateMachine _sessionStateMachine = new();
    private CaptureDevice? _currentDevice;
    private CaptureSettings? _currentSettings;
    private SourceSignalTelemetrySnapshot _latestSourceTelemetry = SourceSignalTelemetrySnapshot.CreateUnavailable("telemetry-not-started");
    private readonly CaptureRecordingBackendResources _recordingBackend = new();
    private readonly FlashbackBackendResources _flashbackBackend = new();

    // Flashback uses a preview-owned continuous encoder when the user is not
    // recording, but can also become the recording backend. These flags track
    // deferred enable/settings changes so recording stop can restore the safe
    // preview backend without mutating capture topology mid-recording.
    private volatile bool _flashbackEnabled = true;
    private bool _hasAv1Nvenc;
    private bool _pendingFlashbackSettingsChange;
    private bool _pendingFlashbackEnableAfterRecording;
    private long _flashbackRecordingStartBytes;
    private readonly PreviewAudioGraphResources _previewAudioGraph = new();

    private string? _micMonitorDeviceId;
    private string? _micMonitorDeviceName;
    private bool _micMonitorEnabled;
    private int _fatalCleanupInProgress;
    private int _flashbackCleanupInProgress;
    private int _flashbackRecordingStartInProgress;
    private int _flashbackRecordingFinalizeInProgress;
    private readonly CaptureVideoPipelineResources _videoPipeline = new();

    private readonly Stopwatch _recordingStopwatch = new();
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
    public CaptureSessionState SessionState => CurrentSessionState;

    private CaptureSessionState CurrentSessionState
        => _sessionStateMachine.State;

    private long CurrentSessionGeneration
        => _sessionStateMachine.Generation;

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
            EnterTransitionState(transitionState);
            await action(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            ResolveSessionSteadyState();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            ResolveSessionSteadyState();
            throw;
        }
        catch (Exception ex)
        {
            EnterFaultedState();
            ErrorOccurred?.Invoke(this, ex);
            throw;
        }
        finally
        {
            ReleaseSemaphoreBestEffort(_sessionTransitionLock, "session_transition");
        }
    }

    private CaptureSessionSteadyStateInputs BuildSteadyStateInputs()
        => new(
            _isDisposed != 0,
            _isRecording,
            _isVideoPreviewActive,
            _isAudioPreviewActive,
            _isInitialized);

    private void EnterTransitionState(CaptureSessionState transitionState)
        => _sessionStateMachine.EnterTransition(transitionState);

    private void EnterCleanupState()
        => _sessionStateMachine.EnterCleanup();

    private void ResolveSessionSteadyState()
        => _sessionStateMachine.ResolveSteadyState(BuildSteadyStateInputs());

    private void EnterFaultedState()
        => _sessionStateMachine.EnterFaulted();

    private void EnterDisposedState()
        => _sessionStateMachine.EnterDisposed();

    private void ResetSessionStateAfterCleanup()
        => _sessionStateMachine.ResetAfterCleanup(_isDisposed != 0);

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
            ResetAvSyncDriftBaseline();
            ResetObservedPixelTelemetry();
            ResetCachedMjpegTimingMetrics();
            _latestSourceTelemetry = BuildFallbackTelemetry();
            await RefreshSourceTelemetryAsync(transitionToken).ConfigureAwait(false);
            TryCorrectFrameRateFromTelemetry();
            _isInitialized = true;
            StatusChanged?.Invoke(this, "Initialized");
        }, cancellationToken);

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

private readonly object _recordingFailureTelemetryLock = new();
    private bool _lastRecordingEncodingFailed;
    private string? _lastRecordingEncodingFailureType;
    private string? _lastRecordingEncodingFailureMessage;
    private bool _lastFlashbackEncodingFailed;
    private string? _lastFlashbackEncodingFailureType;
    private string? _lastFlashbackEncodingFailureMessage;

    public Task CleanupAsync(CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.CleaningUp, CleanupCoreAsync, cancellationToken);

    private async Task CleanupForDisposalAsync()
    {
        await _sessionTransitionLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            EnterCleanupState();
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
        EnterDisposedState();
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
        EnterDisposedState();
    }

    private async Task CleanupCoreAsync(CancellationToken transitionToken)
    {
        var cancellationRequested = false;
        var preserveFlashbackSegmentsAfterFailedRecordingFinalize = false;
        if (_isRecording || _recordingBackend.HasActiveBackend)
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

        _recordingBackend.ClearPendingLibAvDrainIfCompletedSuccessfully();

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

        var pendingLibAvDrainTask = _recordingBackend.PendingLibAvDrainTask;
        var unifiedVideoCapture = _videoPipeline.TakeCapture();
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
                    _recordingBackend.PendingLibAvDrainTask = _videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(
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

        var wasapiCapture = _previewAudioGraph.ProgramCapture;
        _previewAudioGraph.ProgramCapture = null;
        _previewAudioGraph.DetachCapture(
            wasapiCapture,
            OnWasapiAudioLevelUpdated,
            OnWasapiCaptureFailed,
            _flashbackBackend.PlaybackController);
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
        _recordingBackend.ClearContextAndSettings();
        ResetAvSyncDriftBaseline();
        ResetSessionStateAfterCleanup();

        if (cancellationRequested || transitionToken.IsCancellationRequested)
        {
            transitionToken.ThrowIfCancellationRequested();
        }
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

    private void ReleaseFlashbackExportOperationLockIfHeld(ref bool exportOperationLockHeld)
    {
        if (!exportOperationLockHeld)
        {
            return;
        }

        exportOperationLockHeld = false;
        ReleaseSemaphoreBestEffort(_flashbackExportOperationLock, "flashback_export_operation");
    }

    private void DisposeCoordinationLocksBestEffort()
    {
        DisposeSemaphoreBestEffort(_sessionTransitionLock, "session_transition");
        DisposeSemaphoreBestEffort(_flashbackBackendLeaseLock, "flashback_backend_lease");
        DisposeSemaphoreBestEffort(_flashbackExportOperationLock, "flashback_export_operation");
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

    private void OnUnifiedVideoCaptureFatalError(object? sender, Exception ex)
    {
        Logger.Log($"UNIFIED_VIDEO_CAPTURE_FATAL type={ex.GetType().Name} msg={ex.Message}");
        if (_isRecording)
        {
            RecordLastRecordingFailure(ex);
        }

        if (_flashbackBackend.Sink != null)
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

        if (_flashbackBackend.Sink != null)
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

    private void BeginFatalCaptureCleanup(Exception ex)
    {
        if (Interlocked.Exchange(ref _fatalCleanupInProgress, 1) != 0)
        {
            return;
        }

        var generationAtFault = CurrentSessionGeneration;

        _ = Task.Run(async () =>
        {
            try
            {
                await _sessionTransitionLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);
                try
                {
                    if (CurrentSessionGeneration != generationAtFault)
                    {
                        Logger.Log("FATAL_CLEANUP_SKIP_STALE reason='session_generation_changed_before_cleanup'");
                        return;
                    }

                    EnterCleanupState();

                    // Stop the preview renderer before disposing the shared D3D11
                    // device. Same race as the reinit crash: the renderer may be
                    // calling VideoProcessorBlt/Present on the shared device when
                    // cleanup disposes it.
                    try { PreCleanupRequested?.Invoke(); }
                    catch (Exception preEx) { Logger.Log($"PreCleanupRequested handler warning: {preEx.Message}"); }

                    await CleanupCoreAsync(CancellationToken.None).ConfigureAwait(false);
                    EnterFaultedState();
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
        // Centralised TDR guard: if the triggering exception is a GPU device-loss
        // (DXGI_ERROR_DEVICE_REMOVED / _HUNG / _RESET), preserve flashback segments
        // BEFORE entering the async cleanup task. This mirrors the sibling pattern at
        // buffer_cycle_failed / recording_finalize_failed, which both call
        // PreserveRecoverySegments before invoking BeginFlashbackBackendCleanup.
        // PreserveRecoverySegments is idempotent (sets a bool flag), so the
        // double-call from buffer_cycle_failed is harmless. The flag causes
        // ResolveSegmentPurge (inside DisposeFlashbackPreviewBackendAsync) to
        // short-circuit the purge regardless of the purgeSegments argument, and
        // MarkSessionPreservedForRecovery suppresses Directory.Delete in
        // FlashbackBufferManager.Dispose so segment files survive on disk.
        if (IsGpuDeviceLost(ex))
        {
            _flashbackBackend.PreserveRecoverySegments("device_lost");
            Logger.Log($"FLASHBACK_BACKEND_FATAL_DEVICE_LOST type={ex.GetType().Name} preserving_segments=true");
        }

        if (Volatile.Read(ref _fatalCleanupInProgress) != 0 ||
            Interlocked.Exchange(ref _flashbackCleanupInProgress, 1) != 0)
        {
            return;
        }

        var generationAtFault = CurrentSessionGeneration;

        _ = Task.Run(async () =>
        {
            try
            {
                await _sessionTransitionLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);
                try
                {
                    if (CurrentSessionGeneration != generationAtFault)
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

    /// <summary>
    /// Returns true when <paramref name="ex"/> represents a GPU device-removed /
    /// hung / reset condition (TDR). In these cases the flashback rolling buffer
    /// (CPU-resident file data, independent of GPU state) is intact and must NOT
    /// be purged during backend cleanup.
    ///
    /// Covers one level of <see cref="AggregateException"/> unwrap, matching
    /// <c>App.IsRecoverableUnhandled</c>. Deeper unwrap is a separate policy
    /// decision tracked as a deferred follow-up so both classifiers move together.
    ///
    /// DEVICE_HUNG (0x887A0006) is included alongside DEVICE_REMOVED and
    /// DEVICE_RESET because a hung GPU is treated by the driver as a TDR reset:
    /// the OS kills and recreates the device, leaving buffer data intact.
    /// AUDCLNT_E_DEVICE_INVALIDATED and MF_E_* are intentionally excluded -
    /// they are not GPU TDR events and would not flow through this path.
    /// </summary>
    private static bool IsGpuDeviceLost(Exception ex)
    {
        if (ex is AggregateException agg && agg.InnerExceptions.Count == 1 && agg.InnerException is not null)
        {
            ex = agg.InnerException;
        }

        if (ex is COMException com)
        {
            unchecked
            {
                return com.HResult == (int)0x887A0005   // DXGI_ERROR_DEVICE_REMOVED
                    || com.HResult == (int)0x887A0006   // DXGI_ERROR_DEVICE_HUNG
                    || com.HResult == (int)0x887A0007;  // DXGI_ERROR_DEVICE_RESET
            }
        }

        return false;
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
}
