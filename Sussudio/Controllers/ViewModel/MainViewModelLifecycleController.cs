using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.Win32;
using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.Services.Flashback;
using Sussudio.Services.Runtime;

namespace Sussudio.Controllers;

internal readonly record struct MainViewModelCaptureSelectionSnapshot(
    CaptureDevice? SelectedDevice,
    MediaFormat[] AvailableFormats,
    KeyValuePair<string, MediaFormat[]>[] ResolutionToFormats,
    ResolutionOption[] AvailableResolutions,
    FrameRateOption[] AvailableFrameRates,
    string[] AvailableVideoFormats,
    string? SelectedResolution,
    uint? AutoResolvedWidth,
    uint? AutoResolvedHeight,
    double SelectedFrameRate,
    double? AutoResolvedFrameRate,
    string SelectedVideoFormat,
    int MjpegDecoderCount,
    MediaFormat? SelectedFormat,
    bool IsHdrAvailable,
    bool IsHdrEnabled,
    bool IsAutoFrameRateSelected,
    double? SelectedFriendlyFrameRate,
    double? SelectedExactFrameRate,
    string? SelectedExactFrameRateArg,
    string DisabledResolutionReason,
    string DisabledFrameRateReason,
    string HdrResolutionSupportHint,
    string[] AvailableRecordingFormats,
    string SelectedRecordingFormat,
    SourceSignalTelemetrySnapshot LatestSourceTelemetry,
    int? SourceWidth,
    int? SourceHeight,
    bool? SourceIsHdr,
    string SourceTelemetryAvailability,
    string SourceTelemetryOriginDetail,
    string SourceTelemetryConfidence,
    string? SourceTelemetryDiagnosticSummary,
    DateTimeOffset? SourceTelemetryTimestampUtc,
    double? DetectedSourceFrameRate,
    string? DetectedSourceFrameRateArg,
    string SourceFrameRateOrigin,
    string SourceTelemetrySummaryText,
    string SourceTargetSummaryText,
    bool HasUserOverriddenResolutionForCurrentMode,
    bool HasUserOverriddenFrameRateForCurrentMode,
    bool PendingSdrAutoSelectionForDeviceChange,
    int? PendingSdrAutoFriendlyFrameRateBucket,
    bool ForceSourceAutoRetarget,
    string? LastKnownResolutionKey,
    string? LastSourceModeKey,
    bool PendingModeOptionsRefresh)
{
    public bool MatchesSelectionState(MainViewModelCaptureSelectionSnapshot other)
        => ReferenceEquals(SelectedDevice, other.SelectedDevice) &&
           string.Equals(SelectedResolution, other.SelectedResolution, StringComparison.Ordinal) &&
           AutoResolvedWidth == other.AutoResolvedWidth &&
           AutoResolvedHeight == other.AutoResolvedHeight &&
           AreEqual(SelectedFrameRate, other.SelectedFrameRate) &&
           AreNullableEqual(AutoResolvedFrameRate, other.AutoResolvedFrameRate) &&
           string.Equals(SelectedVideoFormat, other.SelectedVideoFormat, StringComparison.Ordinal) &&
           MjpegDecoderCount == other.MjpegDecoderCount &&
           Equals(SelectedFormat, other.SelectedFormat) &&
           IsHdrAvailable == other.IsHdrAvailable &&
           IsHdrEnabled == other.IsHdrEnabled &&
           IsAutoFrameRateSelected == other.IsAutoFrameRateSelected &&
           AreNullableEqual(SelectedFriendlyFrameRate, other.SelectedFriendlyFrameRate) &&
           AreNullableEqual(SelectedExactFrameRate, other.SelectedExactFrameRate) &&
           string.Equals(SelectedExactFrameRateArg, other.SelectedExactFrameRateArg, StringComparison.Ordinal) &&
           string.Equals(DisabledResolutionReason, other.DisabledResolutionReason, StringComparison.Ordinal) &&
           string.Equals(DisabledFrameRateReason, other.DisabledFrameRateReason, StringComparison.Ordinal) &&
           string.Equals(HdrResolutionSupportHint, other.HdrResolutionSupportHint, StringComparison.Ordinal) &&
           string.Equals(SelectedRecordingFormat, other.SelectedRecordingFormat, StringComparison.Ordinal) &&
           HasUserOverriddenResolutionForCurrentMode == other.HasUserOverriddenResolutionForCurrentMode &&
           HasUserOverriddenFrameRateForCurrentMode == other.HasUserOverriddenFrameRateForCurrentMode &&
           PendingSdrAutoSelectionForDeviceChange == other.PendingSdrAutoSelectionForDeviceChange &&
           PendingSdrAutoFriendlyFrameRateBucket == other.PendingSdrAutoFriendlyFrameRateBucket &&
           ForceSourceAutoRetarget == other.ForceSourceAutoRetarget &&
           string.Equals(LastKnownResolutionKey, other.LastKnownResolutionKey, StringComparison.Ordinal) &&
           string.Equals(LastSourceModeKey, other.LastSourceModeKey, StringComparison.Ordinal) &&
           PendingModeOptionsRefresh == other.PendingModeOptionsRefresh;

    private static bool AreNullableEqual(double? left, double? right)
        => left.HasValue == right.HasValue && (!left.HasValue || AreEqual(left.Value, right!.Value));

    private static bool AreEqual(double left, double right)
        => Math.Abs(left - right) < 0.0001;
}

internal sealed class MainViewModelRuntimeLifecycleControllerContext
{
    public required Func<MainViewModelRuntimeEventIngressController> CreateEventIngressController { get; init; }
    public required Func<DispatcherQueueTimer> CreateTimer { get; init; }
    public required Func<CaptureRuntimeSnapshot> GetRuntimeSnapshot { get; init; }
    public required Func<SourceSignalTelemetrySnapshot> GetLatestSourceTelemetrySnapshot { get; init; }
    public required Action<SourceSignalTelemetrySnapshot> SetLatestSourceTelemetrySnapshot { get; init; }
    public required Action<SourceSignalTelemetrySnapshot, bool> ApplySourceTelemetrySnapshot { get; init; }
    public required Action UpdateHdrRuntimeStatusFromCaptureWithoutSnapshot { get; init; }
    public required Action<CaptureRuntimeSnapshot> UpdateHdrRuntimeStatusFromCaptureWithSnapshot { get; init; }
    public required Action UpdateLiveCaptureInfoWithoutSnapshot { get; init; }
    public required Action<CaptureRuntimeSnapshot> UpdateLiveCaptureInfoWithSnapshot { get; init; }
    public required Action ResetLiveCaptureInfo { get; init; }
    public required Action UpdateDiskSpace { get; init; }
    public required Action RefreshSourceTelemetrySummaryAge { get; init; }
    public required Func<bool> IsRecording { get; init; }
    public required Func<bool> IsPreviewing { get; init; }
    public required Func<bool> IsFlashbackActive { get; init; }
    public required Func<TimeSpan> GetRecordingElapsed { get; init; }
    public required Action<string> SetRecordingTime { get; init; }
    public required Action UpdateRecordingStats { get; init; }
    public required Action UpdateFlashbackBitrate { get; init; }
    public required Action UpdateFlashbackHealthStatus { get; init; }
    public required Action StopFlashbackHealthPresentation { get; init; }
    public required Action DisposeAudioDeviceWatcher { get; init; }

    public void UpdateLiveCaptureInfo(CaptureRuntimeSnapshot snapshot)
        => UpdateLiveCaptureInfoWithSnapshot(snapshot);

    public void UpdateHdrRuntimeStatusFromCapture(CaptureRuntimeSnapshot snapshot)
        => UpdateHdrRuntimeStatusFromCaptureWithSnapshot(snapshot);

    public void UpdateLiveCaptureInfo()
        => UpdateLiveCaptureInfoWithoutSnapshot();

    public void UpdateHdrRuntimeStatusFromCapture()
        => UpdateHdrRuntimeStatusFromCaptureWithoutSnapshot();
}

/// <summary>
/// Starts runtime services, refreshes UI state periodically, and coordinates shutdown.
/// </summary>
internal sealed class MainViewModelRuntimeLifecycleController
{
    private readonly MainViewModelRuntimeLifecycleControllerContext _context;
    private readonly MainViewModelRuntimeEventIngressController _eventIngressController;
    private DispatcherQueueTimer? _timer;

    public MainViewModelRuntimeLifecycleController(MainViewModelRuntimeLifecycleControllerContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _eventIngressController = _context.CreateEventIngressController();
    }

    public void Start()
        => _eventIngressController.Attach();

    public void InitializePresentation()
    {
        var latestSourceTelemetry = _context.GetLatestSourceTelemetrySnapshot();
        _context.SetLatestSourceTelemetrySnapshot(latestSourceTelemetry);
        _context.ApplySourceTelemetrySnapshot(latestSourceTelemetry, false);
        _context.UpdateHdrRuntimeStatusFromCapture();
        _context.UpdateLiveCaptureInfo();
        _context.UpdateFlashbackHealthStatus();

        SetupTimer();
        _context.UpdateDiskSpace();
    }

    public void StopForDispose()
    {
        _timer?.Stop();
        _context.StopFlashbackHealthPresentation();
        _eventIngressController.Detach();
        _context.DisposeAudioDeviceWatcher();
    }

    public void CompleteDispose()
        => _eventIngressController.DetachCleanupHandoff();

    private void SetupTimer()
    {
        _timer = _context.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) =>
        {
            var runtimeSnapshot = _context.GetRuntimeSnapshot();
            _context.UpdateFlashbackHealthStatus();

            if (_context.IsRecording())
            {
                _context.SetRecordingTime(_context.GetRecordingElapsed().ToString(@"hh\:mm\:ss"));
                _context.UpdateRecordingStats();
            }

            if (!_context.IsRecording() && _context.IsFlashbackActive())
            {
                _context.UpdateFlashbackBitrate();
            }

            if (_context.IsPreviewing() || _context.IsRecording())
            {
                _context.UpdateLiveCaptureInfo(runtimeSnapshot);
            }
            else
            {
                _context.ResetLiveCaptureInfo();
            }

            _context.UpdateDiskSpace();
            _context.RefreshSourceTelemetrySummaryAge();
            _context.UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);
        };
        _timer.Start();
    }
}

internal sealed class MainViewModelDisposalControllerContext
{
    public required Func<bool> TryBeginDispose { get; init; }
    public required Action CancelActiveFlashbackExport { get; init; }
    public required Action CancelPendingAudioControlWork { get; init; }
    public required Action StopRuntimeForDispose { get; init; }
    public required Func<Task> CleanupSessionCoordinatorAsync { get; init; }
    public required Func<Task> DisposeSessionCoordinatorAsync { get; init; }
    public required Func<Task> DisposeCaptureServiceAsync { get; init; }
    public required Action CompleteRuntimeDispose { get; init; }
    public required Func<Task, int, string, Task> AwaitWithTimeoutAsync { get; init; }
}

/// <summary>
/// Owns service disposal while callers use a bounded wait for shutdown.
/// </summary>
internal sealed class MainViewModelDisposalController
{
    private const int DefaultDisposeTimeoutMs = 30000;

    private readonly MainViewModelDisposalControllerContext _context;
    private readonly object _disposalLock = new();
    private Task? _disposalTask;

    public MainViewModelDisposalController(MainViewModelDisposalControllerContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Dispose()
        => _context.AwaitWithTimeoutAsync(
            GetOrStartDisposalTask(), GetDisposeTimeoutMs(), "ViewModel disposal").GetAwaiter().GetResult();

    public async ValueTask DisposeAsync()
        => await _context.AwaitWithTimeoutAsync(
            GetOrStartDisposalTask(), GetDisposeTimeoutMs(), "ViewModel disposal").ConfigureAwait(false);

    private Task GetOrStartDisposalTask()
    {
        lock (_disposalLock)
        {
            // A timed-out caller leaves the actual operation owned here. A later
            // caller joins it; only a completed failure can start a retry.
            if (_disposalTask == null || _disposalTask.IsFaulted || _disposalTask.IsCanceled)
            {
                _disposalTask = Task.Run(DisposeCoreAsync);
            }

            return _disposalTask;
        }
    }

    private async Task DisposeCoreAsync()
    {
        if (_context.TryBeginDispose())
        {
            _context.CancelActiveFlashbackExport();
            _context.CancelPendingAudioControlWork();
            _context.StopRuntimeForDispose();
        }

        await RunDisposeStepAsync(
            _context.CleanupSessionCoordinatorAsync,
            "ViewModel cleanup during dispose failed").ConfigureAwait(false);
        await RunDisposeStepAsync(
            _context.DisposeSessionCoordinatorAsync,
            "Coordinator dispose failed").ConfigureAwait(false);

        await _context.DisposeCaptureServiceAsync().ConfigureAwait(false);
        _context.CompleteRuntimeDispose();
    }

    private static int GetDisposeTimeoutMs()
        => EnvironmentHelpers.GetIntFromEnv(
            "SUSSUDIO_VIEWMODEL_DISPOSE_TIMEOUT_MS",
            DefaultDisposeTimeoutMs,
            1000,
            300000);

    private static async Task RunDisposeStepAsync(Func<Task> operation, string failureLogPrefix)
    {
        try
        {
            await operation().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"{failureLogPrefix}: {ex.Message}");
        }
    }
}

internal sealed class MainViewModelRuntimeEventIngressControllerContext
{
    public required Action<EventHandler<DeviceService.DeviceFormatProbeCompletedEventArgs>> AttachFormatProbeCompleted { get; init; }
    public required Action<EventHandler<DeviceService.DeviceFormatProbeCompletedEventArgs>> DetachFormatProbeCompleted { get; init; }
    public required EventHandler<DeviceService.DeviceFormatProbeCompletedEventArgs> OnDeviceFormatProbeCompleted { get; init; }
    public required Action<EventHandler<string>> AttachCaptureStatusChanged { get; init; }
    public required Action<EventHandler<string>> DetachCaptureStatusChanged { get; init; }
    public required Action<EventHandler<CaptureErrorEventArgs>> AttachCaptureErrorOccurred { get; init; }
    public required Action<EventHandler<CaptureErrorEventArgs>> DetachCaptureErrorOccurred { get; init; }
    public required Func<CaptureErrorOrigin, bool> IsCaptureErrorCurrent { get; init; }
    public required Func<CaptureErrorOrigin, Task> RecoverCaptureErrorAsync { get; init; }
    public required Action<Action<FlashbackPlaybackStateChange>> AttachFlashbackPlaybackStateChanged { get; init; }
    public required Action<Action<FlashbackPlaybackStateChange>> DetachFlashbackPlaybackStateChanged { get; init; }
    public required Action<FlashbackPlaybackStateChange> OnFlashbackPlaybackStateChanged { get; init; }
    public required Action UpdateFlashbackHealthStatus { get; init; }
    public required Action<Func<CancellationToken, Task>> AttachCapturePreCleanupRequested { get; init; }
    public required Action<Func<CancellationToken, Task>> DetachCapturePreCleanupRequested { get; init; }
    public required Action<EventHandler<AudioLevelEventArgs>> AttachAudioLevelUpdated { get; init; }
    public required Action<EventHandler<AudioLevelEventArgs>> DetachAudioLevelUpdated { get; init; }
    public required EventHandler<AudioLevelEventArgs> OnAudioLevelUpdated { get; init; }
    public required Action<EventHandler<AudioLevelEventArgs>> AttachMicrophoneAudioLevelUpdated { get; init; }
    public required Action<EventHandler<AudioLevelEventArgs>> DetachMicrophoneAudioLevelUpdated { get; init; }
    public required EventHandler<AudioLevelEventArgs> OnMicrophoneAudioLevelUpdated { get; init; }
    public required Action<EventHandler<SourceSignalTelemetrySnapshot>> AttachSourceTelemetryUpdated { get; init; }
    public required Action<EventHandler<SourceSignalTelemetrySnapshot>> DetachSourceTelemetryUpdated { get; init; }
    public required EventHandler<SourceSignalTelemetrySnapshot> OnSourceTelemetryUpdated { get; init; }
    public required Action<Action> AttachAudioDevicesChanged { get; init; }
    public required Action<Action> DetachAudioDevicesChanged { get; init; }
    public required Action OnAudioDevicesChanged { get; init; }
    public required Func<Action, bool> TryEnqueueOnUiThread { get; init; }
    public required Func<CaptureRuntimeSnapshot> GetRuntimeSnapshot { get; init; }
    public required Action<string> SetStatusText { get; init; }
    public required Action<CaptureRuntimeSnapshot> UpdateLiveCaptureInfo { get; init; }
    public required Action<CaptureRuntimeSnapshot> UpdateHdrRuntimeStatusFromCapture { get; init; }
    public required Action<bool> SetIsInitialized { get; init; }
    public required Func<bool> IsCaptureInitialized { get; init; }
    public required Func<bool> IsInitialized { get; init; }
    public required Action<bool> SetIsPreviewing { get; init; }
    public required Func<bool> IsVideoPreviewActive { get; init; }
    public required Func<bool> IsPreviewing { get; init; }
    public required Action<bool> SetIsRecording { get; init; }
    public required Func<bool> IsCaptureRecording { get; init; }
    public required Func<bool> IsRecording { get; init; }
    public required Action ResetAudioMeter { get; init; }
    public required Func<Task> NotifyRendererStopAsync { get; init; }
    public required Func<Func<Task>, CancellationToken, Task> InvokeOnUiThreadAsync { get; init; }
    public required Func<string, Task> ReinitializeDeviceAsync { get; init; }
    public required Func<Func<Task>, string, bool> EnqueueUiOperation { get; init; }
}

/// <summary>
/// Subscribes to runtime and system events and schedules UI updates on the dispatcher.
/// </summary>
internal sealed class MainViewModelRuntimeEventIngressController
{
    private readonly MainViewModelRuntimeEventIngressControllerContext _context;

    public MainViewModelRuntimeEventIngressController(MainViewModelRuntimeEventIngressControllerContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Attach()
    {
        _context.AttachFormatProbeCompleted(_context.OnDeviceFormatProbeCompleted);

        _context.AttachCaptureStatusChanged(OnCaptureStatusChanged);
        _context.AttachCaptureErrorOccurred(OnCaptureError);
        _context.AttachFlashbackPlaybackStateChanged(_context.OnFlashbackPlaybackStateChanged);
        _context.AttachCapturePreCleanupRequested(OnCapturePreCleanupRequested);
        _context.AttachAudioLevelUpdated(_context.OnAudioLevelUpdated);
        _context.AttachMicrophoneAudioLevelUpdated(_context.OnMicrophoneAudioLevelUpdated);
        _context.AttachSourceTelemetryUpdated(_context.OnSourceTelemetryUpdated);

        // SystemEvents.PowerModeChanged is the managed desktop wake signal used
        // to recover capture after sleep or hibernate resume.
        SystemEvents.PowerModeChanged += OnSystemPowerModeChanged;

        _context.AttachAudioDevicesChanged(_context.OnAudioDevicesChanged);
    }

    public void Detach()
    {
        _context.DetachFormatProbeCompleted(_context.OnDeviceFormatProbeCompleted);

        SystemEvents.PowerModeChanged -= OnSystemPowerModeChanged;

        _context.DetachCaptureStatusChanged(OnCaptureStatusChanged);
        _context.DetachCaptureErrorOccurred(OnCaptureError);
        _context.DetachFlashbackPlaybackStateChanged(_context.OnFlashbackPlaybackStateChanged);
        _context.DetachAudioLevelUpdated(_context.OnAudioLevelUpdated);
        _context.DetachMicrophoneAudioLevelUpdated(_context.OnMicrophoneAudioLevelUpdated);
        _context.DetachSourceTelemetryUpdated(_context.OnSourceTelemetryUpdated);

        _context.DetachAudioDevicesChanged(_context.OnAudioDevicesChanged);
    }

    public void DetachCleanupHandoff()
        => _context.DetachCapturePreCleanupRequested(OnCapturePreCleanupRequested);

    private void OnCaptureStatusChanged(object? sender, string status)
    {
        if (!_context.TryEnqueueOnUiThread(() =>
        {
            var runtimeSnapshot = _context.GetRuntimeSnapshot();
            _context.SetStatusText(status);
            _context.UpdateFlashbackHealthStatus();
            _context.UpdateLiveCaptureInfo(runtimeSnapshot);
            _context.UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);
        }))
        {
            Logger.Log($"CAPTURE_STATUS_UI_ENQUEUE_FAILED status='{status}'");
        }
    }

    private void OnCaptureError(object? sender, CaptureErrorEventArgs error)
    {
        var ex = error.Exception;
        if (!_context.TryEnqueueOnUiThread(() =>
        {
            if (!_context.IsCaptureErrorCurrent(error.Origin))
            {
                return;
            }

            var runtimeSnapshot = _context.GetRuntimeSnapshot();
            _context.SetStatusText($"Error: {ex.Message}");
            _context.UpdateFlashbackHealthStatus();
            _context.SetIsInitialized(_context.IsCaptureInitialized());
            _context.SetIsPreviewing(_context.IsVideoPreviewActive());
            _context.SetIsRecording(_context.IsCaptureRecording());
            if (!_context.IsPreviewing() && !_context.IsRecording())
            {
                _context.ResetAudioMeter();
            }

            _context.UpdateLiveCaptureInfo(runtimeSnapshot);
            _context.UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);

            // An audio device can be invalidated without a system resume event.
            // Reopen capture automatically while preview is active and recording
            // is stopped. Recovery checks the same origin after its asynchronous waits.
            unchecked
            {
                const int AudclntDeviceInvalidated = (int)0x88890004;
                if (ex is COMException comEx &&
                    comEx.HResult == AudclntDeviceInvalidated &&
                    _context.IsPreviewing() &&
                    !_context.IsRecording())
                {
                    Logger.Log("AUDCLNT_E_DEVICE_INVALIDATED received \u2014 scheduling audio rebind.");
                    _context.EnqueueUiOperation(
                        () => _context.RecoverCaptureErrorAsync(error.Origin),
                        "audio device invalidated reinit");
                }
            }
        }))
        {
            Logger.Log($"CAPTURE_ERROR_UI_ENQUEUE_FAILED type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private Task OnCapturePreCleanupRequested(CancellationToken admissionToken)
        => _context.InvokeOnUiThreadAsync(_context.NotifyRendererStopAsync, admissionToken);

    // PowerModeChanged fires on the system thread pool - must not touch UI properties
    // directly. We act only on PowerModes.Resume; Suspend/StatusChange are ignored
    // (Suspend arrives just before the OS freezes the process so there's nothing
    // useful to do, and StatusChange fires on AC/battery transitions which don't
    // affect capture). All UI-state reads happen inside the EnqueueUiOperation
    // lambda, which executes on the DispatcherQueue thread. ReinitializeDeviceAsync's
    // IsRecording guard skips reinitialization while a recording is active.
    private void OnSystemPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode != PowerModes.Resume)
        {
            return;
        }

        Logger.Log("SYSTEM_RESUMING_EVENT received \u2014 scheduling capture rebind if previewing.");
        _context.EnqueueUiOperation(() =>
        {
            if (!_context.IsPreviewing() || !_context.IsInitialized() || _context.IsRecording())
            {
                Logger.Log(
                    $"SYSTEM_RESUMING_REINIT_SKIP previewing={_context.IsPreviewing()} " +
                    $"initialized={_context.IsInitialized()} recording={_context.IsRecording()}");
                return Task.CompletedTask;
            }

            Logger.Log("SYSTEM_RESUMING_REINIT_SCHEDULED");
            return _context.ReinitializeDeviceAsync("system resume");
        }, "system resume reinit");
    }
}

/// <summary>
/// Capture commands and UI callbacks used to start, stop, and reinitialize preview.
/// </summary>
internal sealed class MainViewModelPreviewLifecycleControllerContext
{
    public required CaptureSessionCoordinator SessionCoordinator { get; init; }
    public required Func<CaptureSettings> BuildCaptureSettings { get; init; }
    public required Func<Func<Task>, CancellationToken, Task> InvokeOnUiThreadAsync { get; init; }
    public required Func<CancellationToken, Task<PreviewAudioVolumeOperation>> RampPreviewVolumeDownForStopAsync { get; init; }
    public required Action<PreviewAudioVolumeOperation> RestorePreviewVolumeAfterStopFailed { get; init; }
    public required Func<MainViewModelPreviewLifecycleController, MainViewModelPreviewReinitializeController> CreateReinitializeController { get; init; }
    public required Func<CaptureDevice?> SelectedDevice { get; init; }
    public required Action<CaptureDevice?> SetSelectedDevice { get; init; }
    public required Func<MainViewModelCaptureSelectionSnapshot> CaptureSelectionSnapshot { get; init; }
    public required Func<MainViewModelCaptureSelectionSnapshot, MainViewModelCaptureSelectionSnapshot, bool> RestoreCaptureSelectionSnapshotIfUnchanged { get; init; }
    public required Func<bool> IsInitialized { get; init; }
    public required Action<bool> SetIsInitialized { get; init; }
    public required Func<bool> IsPreviewing { get; init; }
    public required Action<bool> SetIsPreviewing { get; init; }
    public required Func<bool> IsPreviewReinitializing { get; init; }
    public required Func<bool> IsRecording { get; init; }
    public required Func<bool> ShouldStartAudioPreview { get; init; }
    public required Func<bool> IsAudioPreviewActive { get; init; }
    public required Action<string> SetStatusText { get; init; }
    public required Action RaisePreviewStartRequested { get; init; }
    public required Action RaisePreviewStopRequested { get; init; }
    public required Action ApplyLatestSourceTelemetryForPreviewStart { get; init; }
}

/// <summary>
/// Starts and stops preview while keeping UI state and selected capture settings in sync.
/// </summary>
internal sealed class MainViewModelPreviewLifecycleController
{
    private readonly MainViewModelPreviewLifecycleControllerContext _context;
    private readonly MainViewModelPreviewReinitializeController _previewReinitializeController;

    public MainViewModelPreviewLifecycleController(MainViewModelPreviewLifecycleControllerContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _previewReinitializeController = _context.CreateReinitializeController(this);
    }

    public void CancelPendingPreviewRestart()
        => _previewReinitializeController.CancelPendingPreviewRestart();

    public bool IsReinitializeAdmitted => _previewReinitializeController.IsReinitializeAdmitted;

    public async Task InitializeDeviceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var selectedDevice = _context.SelectedDevice()
                ?? throw new InvalidOperationException("No capture device selected.");
            _context.SetStatusText("Initializing device...");
            var settings = _context.BuildCaptureSettings();
            Logger.Log(
                $"CAPTURE_INIT device='{selectedDevice.Name}' id='{selectedDevice.Id}' format={settings.Format} {settings.Width}x{settings.Height}@{settings.FrameRate} hdr={settings.HdrEnabled} audio={settings.AudioEnabled}");

            await _context.SessionCoordinator.InitializeAsync(selectedDevice, settings, cancellationToken);

            _context.SetIsInitialized(true);
            _context.SetStatusText("Device ready");
            Logger.Log("CAPTURE_INIT_READY");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _context.SetStatusText("Device initialization canceled");
            _context.SetIsInitialized(false);
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            _context.SetStatusText($"Failed to initialize: {ex.Message}");
            _context.SetIsInitialized(false);
            throw;
        }
    }

    public async Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (userInitiated)
        {
            _previewReinitializeController.ResetPendingPreviewRestartCancellation();
        }

        _context.RaisePreviewStartRequested();
        Logger.Log($"PREVIEW_START requested initialized={_context.IsInitialized()} audio={_context.ShouldStartAudioPreview()}");

        if (!_context.IsInitialized())
        {
            await InitializeDeviceAsync(cancellationToken);
        }

        var settings = _context.BuildCaptureSettings();
        await _context.SessionCoordinator.StartVideoPreviewAsync(settings, cancellationToken).ConfigureAwait(true);

        _context.SetIsPreviewing(true);
        _context.SetStatusText("Preview starting...");

        if (_context.ShouldStartAudioPreview())
        {
            await _context.SessionCoordinator.StartAudioPreviewAsync(cancellationToken);
        }

        _context.ApplyLatestSourceTelemetryForPreviewStart();
        Logger.Log($"PREVIEW_START_READY audio={_context.ShouldStartAudioPreview()}");
    }

    public Task SetPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return _context.InvokeOnUiThreadAsync(async () =>
        {
            if (!enabled && _context.IsPreviewReinitializing())
            {
                CancelPendingPreviewRestart();
                if (!_context.IsPreviewing())
                {
                    return;
                }
            }

            if (enabled == _context.IsPreviewing())
            {
                return;
            }

            if (enabled)
            {
                await StartPreviewAsync(userInitiated: true, cancellationToken);
            }
            else
            {
                await StopPreviewAsync(userInitiated: true, teardownPipeline: false, cancellationToken);
            }
        }, cancellationToken);
    }

    public async Task ApplySelectedDeviceAsync(CaptureDevice device, CancellationToken cancellationToken = default)
        => await ApplySelectedDeviceWithResultAsync(device, cancellationToken).ConfigureAwait(true);

    public async Task<bool> ApplySelectedDeviceWithResultAsync(CaptureDevice device, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_context.IsRecording())
        {
            _context.SetStatusText("Stop recording before switching capture devices.");
            return false;
        }

        var selectedDevice = _context.SelectedDevice();
        if (selectedDevice != null &&
            string.Equals(selectedDevice.Id, device.Id, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        Logger.Log($"DEVICE_APPLY_REQUEST device='{device.Name}' id='{device.Id}' preview={_context.IsPreviewing()} initialized={_context.IsInitialized()}");
        var rollback = _context.CaptureSelectionSnapshot();
        _context.SetSelectedDevice(device);
        var attempted = _context.CaptureSelectionSnapshot();

        if (_context.IsPreviewing())
        {
            var reinitialized = await ReinitializeDeviceWithResultAsync("device selection apply").ConfigureAwait(true);
            if (!reinitialized)
            {
                _context.RestoreCaptureSelectionSnapshotIfUnchanged(rollback, attempted);
            }

            return reinitialized;
        }

        _context.SetIsInitialized(false);
        _context.SetStatusText($"Selected device: {device.Name}");
        return true;
    }

    public async Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (userInitiated && _context.IsPreviewReinitializing())
        {
            CancelPendingPreviewRestart();
        }

        PreviewAudioVolumeOperation? volumeOperation = null;
        if (userInitiated && !_context.IsPreviewReinitializing() && _context.IsAudioPreviewActive())
        {
            volumeOperation = await _context.RampPreviewVolumeDownForStopAsync(cancellationToken);
        }

        try
        {
            await StopPreviewCoreAsync(teardownPipeline, cancellationToken);
        }
        catch
        {
            if (volumeOperation is { } operation)
            {
                _context.RestorePreviewVolumeAfterStopFailed(operation);
            }
            throw;
        }
    }

    private async Task StopPreviewCoreAsync(bool teardownPipeline, CancellationToken cancellationToken)
    {
        _context.RaisePreviewStopRequested();
        var commitStoppedState = false;
        try
        {
            if (teardownPipeline)
            {
                await _context.SessionCoordinator.StopVideoPreviewWithTeardownAsync(cancellationToken);
            }
            else
            {
                await _context.SessionCoordinator.StopVideoPreviewAsync(cancellationToken);
            }

            commitStoppedState = true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            commitStoppedState = true;
            throw;
        }
        finally
        {
            if (commitStoppedState)
            {
                _context.SetIsPreviewing(false);
            }
        }

        if (_context.IsAudioPreviewActive())
        {
            if (teardownPipeline)
            {
                await _context.SessionCoordinator.StopAudioPreviewWithTeardownAsync(cancellationToken);
            }
            else
            {
                await _context.SessionCoordinator.StopAudioPreviewAsync(cancellationToken);
            }
        }

        if (!_context.IsPreviewReinitializing())
        {
            _context.SetStatusText("Preview stopped");
        }
    }

    public Task ReinitializeDeviceAsync(string reason)
        => _previewReinitializeController.ReinitializeDeviceAsync(reason);

    public Task<bool> ReinitializeDeviceWithResultAsync(string reason)
        => _previewReinitializeController.ReinitializeDeviceWithResultAsync(reason);

    public Task RecoverCaptureErrorAsync(CaptureErrorOrigin origin)
        => _previewReinitializeController.RecoverCaptureErrorAsync(origin);
}

/// <summary>
/// Capture state, cleanup tasks, and callbacks used when reinitializing preview.
/// </summary>
internal sealed class MainViewModelPreviewReinitializeControllerContext
{
    public required Func<CaptureErrorOrigin, bool> IsCaptureErrorCurrent { get; init; }
    public required Func<CaptureDevice?> SelectedDevice { get; init; }
    public required Func<MediaFormat?> SelectedFormat { get; init; }
    public required Func<bool> IsRecording { get; init; }
    public required Func<bool> IsRecordingTransitioning { get; init; }
    public required Func<bool> IsInitialized { get; init; }
    public required Action<bool> SetIsInitialized { get; init; }
    public required Func<bool> IsPreviewing { get; init; }
    public required Action<bool> SetIsPreviewing { get; init; }
    public required Func<bool> IsPreviewReinitializing { get; init; }
    public required Action<bool> SetIsPreviewReinitializing { get; init; }
    public required Action<string> SetStatusText { get; init; }
    public required int PreviewReinitializeDebounceMs { get; init; }
    public required Func<Task?> PendingFlashbackCycleTask { get; init; }
    public required int FlashbackCycleBeforeReinitializeTimeoutMs { get; init; }
    public required Func<Task, int, string, Task> AwaitWithTimeoutAsync { get; init; }
    public required Action<Task> ClearPendingFlashbackCycleIfSameAndCompleted { get; init; }
    public required Func<string, Task> NotifyPreviewReinitRequestedAsync { get; init; }
    public required Func<Task> NotifyRendererStopAsync { get; init; }
    public required Func<Task?> PendingDeferredCaptureCleanupTask { get; init; }
    public required int ReinitDeviceBusyCleanupTimeoutMs { get; init; }
}

/// <summary>
/// Debounces preview reinitialization and waits for pending cleanup before reopening capture.
/// </summary>
internal sealed class MainViewModelPreviewReinitializeController
{
    private readonly MainViewModelPreviewReinitializeControllerContext _context;
    private readonly MainViewModelPreviewLifecycleController _previewLifecycleController;
    private readonly SemaphoreSlim _previewReinitializeGate = new(1, 1);
    private int _previewReinitializeGeneration;
    private bool _cancelPreviewRestartAfterReinitialize;

    // UI-thread admission spans every reinitialize, including device-ready state
    // without preview. Recording start checks this before its first await.
    public bool IsReinitializeAdmitted { get; private set; }

    public MainViewModelPreviewReinitializeController(
        MainViewModelPreviewReinitializeControllerContext context,
        MainViewModelPreviewLifecycleController previewLifecycleController)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _previewLifecycleController = previewLifecycleController ?? throw new ArgumentNullException(nameof(previewLifecycleController));
    }

    public void CancelPendingPreviewRestart()
    {
        if (_context.IsPreviewReinitializing())
        {
            _cancelPreviewRestartAfterReinitialize = true;
        }
    }

    public void ResetPendingPreviewRestartCancellation()
    {
        _cancelPreviewRestartAfterReinitialize = false;
    }

    public async Task ReinitializeDeviceAsync(string reason)
        => await ReinitializeDeviceCoreAsync(reason, treatCoalescedAsSuccess: true).ConfigureAwait(true);

    public async Task<bool> ReinitializeDeviceWithResultAsync(string reason)
        => await ReinitializeDeviceCoreAsync(reason, treatCoalescedAsSuccess: false).ConfigureAwait(true);

    public async Task RecoverCaptureErrorAsync(CaptureErrorOrigin origin)
        => await ReinitializeDeviceCoreAsync("audio device invalidated", treatCoalescedAsSuccess: true, origin).ConfigureAwait(true);

    private bool IsCaptureErrorCurrent(CaptureErrorOrigin? origin)
        => origin is null || _context.IsCaptureErrorCurrent(origin.Value);

    private async Task<bool> ReinitializeDeviceCoreAsync(
        string reason,
        bool treatCoalescedAsSuccess,
        CaptureErrorOrigin? errorOrigin = null)
    {
        // An obsolete recovery must not supersede an unrelated settings request.
        if (!IsCaptureErrorCurrent(errorOrigin))
        {
            return false;
        }

        if (_context.SelectedDevice() == null || _context.SelectedFormat() == null)
        {
            return false;
        }

        if (_context.IsRecording())
        {
            Logger.Log($"REINIT_REJECTED_RECORDING reason='{reason}' - stop recording before changing capture settings.");
            _context.SetStatusText("Stop recording before changing capture settings.");
            return false;
        }

        var reinitializeGeneration = Interlocked.Increment(ref _previewReinitializeGeneration);
        await Task.Delay(_context.PreviewReinitializeDebounceMs).ConfigureAwait(true);
        if (!IsCaptureErrorCurrent(errorOrigin))
        {
            return false;
        }

        if (Volatile.Read(ref _previewReinitializeGeneration) != reinitializeGeneration)
        {
            Logger.Log($"REINIT_COALESCED reason='{reason}' generation={reinitializeGeneration}");
            return treatCoalescedAsSuccess;
        }

        var pendingCycle = _context.PendingFlashbackCycleTask();
        if (pendingCycle != null)
        {
            try
            {
                await _context.AwaitWithTimeoutAsync(
                    pendingCycle,
                    _context.FlashbackCycleBeforeReinitializeTimeoutMs,
                    "Flashback encoder settings cycle before reinitialize").ConfigureAwait(true);
            }
            catch (TimeoutException ex)
            {
                if (!IsCaptureErrorCurrent(errorOrigin))
                {
                    return false;
                }

                Logger.Log($"REINIT_WAIT_FLASHBACK_CYCLE_TIMEOUT reason={reason} timeoutMs={_context.FlashbackCycleBeforeReinitializeTimeoutMs}");
                _context.SetStatusText($"Failed to apply format: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Log($"REINIT_WAIT_FLASHBACK_CYCLE_FAULT reason={reason} type={ex.GetType().Name} msg='{ex.Message}'");
            }

            if (!IsCaptureErrorCurrent(errorOrigin))
            {
                return false;
            }

            _context.ClearPendingFlashbackCycleIfSameAndCompleted(pendingCycle);
        }

        await _previewReinitializeGate.WaitAsync().ConfigureAwait(true);
        try
        {
            if (!IsCaptureErrorCurrent(errorOrigin))
            {
                return false;
            }

            // A newer request can arrive while the Flashback cycle or another restart owns the gate.
            if (Volatile.Read(ref _previewReinitializeGeneration) != reinitializeGeneration)
            {
                Logger.Log($"REINIT_COALESCED reason='{reason}' generation={reinitializeGeneration}");
                return treatCoalescedAsSuccess;
            }

            // Recording can start while debounce, Flashback, or a previous
            // reinitialize yields. Admit neither teardown nor a competing start.
            if (_context.IsRecording() || _context.IsRecordingTransitioning())
            {
                Logger.Log($"REINIT_REJECTED_RECORDING reason='{reason}' - recording became active while waiting.");
                _context.SetStatusText("Stop recording before changing capture settings.");
                return false;
            }

            IsReinitializeAdmitted = true;
            return await ReinitializeAdmittedRequestAsync(reason).ConfigureAwait(true);
        }
        finally
        {
            IsReinitializeAdmitted = false;
            _previewReinitializeGate.Release();
        }
    }

    private async Task<bool> ReinitializeAdmittedRequestAsync(string reason)
    {
        var shouldRestartPreview = _context.IsPreviewing();
        var success = false;
        try
        {
            _context.SetStatusText("Applying new settings...");
            Logger.Log($"=== Reinitializing device ({reason}) ===");

            if (shouldRestartPreview)
            {
                _context.SetIsPreviewReinitializing(true);
                ResetPendingPreviewRestartCancellation();
                await _context.NotifyPreviewReinitRequestedAsync(reason);
                await _context.NotifyRendererStopAsync();
            }

            if (_context.IsInitialized())
            {
                await _previewLifecycleController.StopPreviewAsync(userInitiated: false, teardownPipeline: true, CancellationToken.None);
            }

            _context.SetIsInitialized(false);
            success = await TryInitializeAndRestartPreviewAsync(
                reason,
                shouldRestartPreview,
                logFatalBreadcrumbs: true,
                reportSuccessfulPreview: true).ConfigureAwait(true);
        }
        catch (PreviewRendererReinitStopTimeoutException ex)
        {
            Logger.LogException(ex);
            Logger.Log($"REINIT_ABORT_RENDERER_STOP_TIMEOUT reason='{reason}' msg='{ex.InnerException?.Message ?? ex.Message}'");
            _context.SetStatusText($"Failed to apply format: {ex.Message}");
            success = false;
        }
        catch (Exception ex) when (IsDeviceBusyException(ex) && shouldRestartPreview)
        {
            Logger.LogException(ex);
            Logger.Log($"REINIT_DEVICE_BUSY reason='{reason}' hr=0x{ex.HResult:X8} — awaiting deferred cleanup then retrying");
            var retried = false;
            try
            {
                var pendingCleanup = _context.PendingDeferredCaptureCleanupTask();
                if (pendingCleanup is { IsCompleted: false })
                {
                    await Task.WhenAny(pendingCleanup, Task.Delay(_context.ReinitDeviceBusyCleanupTimeoutMs)).ConfigureAwait(true);
                }

                for (var attempt = 1; attempt <= 2; attempt++)
                {
                    Logger.Log($"REINIT_DEVICE_BUSY_RETRY attempt={attempt} reason='{reason}'");
                    try
                    {
                        success = await TryInitializeAndRestartPreviewAsync(
                            reason,
                            shouldRestartPreview,
                            logFatalBreadcrumbs: false,
                            reportSuccessfulPreview: true).ConfigureAwait(true);
                        if (success)
                        {
                            retried = true;
                            break;
                        }
                    }
                    catch (Exception retryEx)
                    {
                        Logger.Log($"REINIT_DEVICE_BUSY_RETRY_FAIL attempt={attempt} reason='{reason}' type={retryEx.GetType().Name} hr=0x{retryEx.HResult:X8} msg='{retryEx.Message}'");
                    }
                }

                if (!retried)
                {
                    await CleanupFailedPreviewRestartAsync(reason).ConfigureAwait(true);
                    var recoveryOutcome = "fail";
                    try
                    {
                        await TryInitializeAndRestartPreviewAsync(
                            reason,
                            shouldRestartPreview,
                            logFatalBreadcrumbs: false,
                            reportSuccessfulPreview: false).ConfigureAwait(true);

                        recoveryOutcome = _context.IsPreviewing() ? "ok" : "fail";
                    }
                    catch (Exception recoveryEx)
                    {
                        Logger.Log($"REINIT_RECOVERY_RESTART_FAULT reason='{reason}' type={recoveryEx.GetType().Name} msg='{recoveryEx.Message}'");
                    }

                    Logger.Log($"REINIT_RECOVERY_RESTART outcome={recoveryOutcome} reason='{reason}'");
                    _context.SetStatusText($"Failed to apply format: {ex.Message}");
                    success = false;
                }
            }
            catch (Exception outerEx)
            {
                Logger.Log($"REINIT_DEVICE_BUSY_OUTER_FAULT reason='{reason}' type={outerEx.GetType().Name} msg='{outerEx.Message}'");
                _context.SetStatusText($"Failed to apply format: {ex.Message}");
                success = false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            if (shouldRestartPreview)
            {
                await CleanupFailedPreviewRestartAsync(reason).ConfigureAwait(true);
            }

            _context.SetStatusText($"Failed to apply format: {ex.Message}");
            success = false;
        }
        finally
        {
            ResetPendingPreviewRestartCancellation();
            if (shouldRestartPreview)
            {
                _context.SetIsPreviewReinitializing(false);
            }
        }

        return success;
    }

    private async Task<bool> TryInitializeAndRestartPreviewAsync(
        string reason,
        bool shouldRestartPreview,
        bool logFatalBreadcrumbs,
        bool reportSuccessfulPreview)
    {
        if (logFatalBreadcrumbs)
        {
            Logger.LogFatalBreadcrumb($"REINIT phase=init_device reason={reason}");
        }

        await _previewLifecycleController.InitializeDeviceAsync().ConfigureAwait(true);

        if (logFatalBreadcrumbs)
        {
            Logger.LogFatalBreadcrumb($"REINIT phase=init_device_done reason={reason}");
        }

        var previewRestartCanceled = _cancelPreviewRestartAfterReinitialize;
        if (_context.IsInitialized() && shouldRestartPreview && !previewRestartCanceled)
        {
            if (logFatalBreadcrumbs)
            {
                Logger.LogFatalBreadcrumb($"REINIT phase=start_preview reason={reason}");
            }

            await _previewLifecycleController.StartPreviewAsync(userInitiated: false).ConfigureAwait(true);

            if (logFatalBreadcrumbs)
            {
                Logger.LogFatalBreadcrumb($"REINIT phase=start_preview_done reason={reason}");
            }
        }

        var success =
            _context.IsInitialized() &&
            (!shouldRestartPreview || previewRestartCanceled || _context.IsPreviewing());
        if (success && shouldRestartPreview && !previewRestartCanceled && reportSuccessfulPreview)
        {
            var selectedFormat = _context.SelectedFormat()!;
            _context.SetStatusText($"Preview: {selectedFormat.Width}x{selectedFormat.Height}@{selectedFormat.FrameRate}fps");
        }

        return success;
    }

    private static bool IsDeviceBusyException(Exception? ex)
    {
        unchecked
        {
            for (var current = ex; current is not null; current = current.InnerException)
            {
                if (current.HResult == (int)0xC00D36E6 || current.HResult == (int)0x80070001)
                {
                    return true;
                }

                // Defensive: some MF failures surface the HRESULT only in the message text.
                var message = current.Message;
                if (!string.IsNullOrEmpty(message)
                    && (message.Contains("0xC00D36E6", StringComparison.OrdinalIgnoreCase)
                        || message.Contains("0x80070001", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }
    }

    private async Task CleanupFailedPreviewRestartAsync(string reason)
    {
        try
        {
            Logger.Log($"REINIT_FAILED_CLEANUP reason='{reason}' previewing={_context.IsPreviewing()} initialized={_context.IsInitialized()}");
            await _previewLifecycleController.StopPreviewAsync(
                    userInitiated: false,
                    teardownPipeline: true,
                    CancellationToken.None)
                .ConfigureAwait(true);
        }
        catch (Exception cleanupEx)
        {
            Logger.Log($"REINIT_FAILED_CLEANUP_FAULT reason='{reason}' type={cleanupEx.GetType().Name} msg='{cleanupEx.Message}'");
        }
        finally
        {
            _context.SetIsPreviewing(false);
            _context.SetIsInitialized(false);
        }
    }
}

/// <summary>
/// Recording commands and UI state used to serialize recording start and stop requests.
/// </summary>
internal sealed class MainViewModelRecordingTransitionControllerContext
{
    public required Func<bool> IsRecording { get; init; }
    public required Action<bool> SetIsRecording { get; init; }
    public required Func<bool> IsInitialized { get; init; }
    public required Func<bool> HasSelectedDevice { get; init; }
    public required Func<string> GetStatusText { get; init; }
    public required Action<string> SetStatusText { get; init; }
    public required Action<bool> SetIsRecordingTransitioning { get; init; }
    public required Func<Func<Task>, CancellationToken, Task> InvokeOnUiThreadAsync { get; init; }
    public required Func<CaptureSettings> BuildCaptureSettings { get; init; }
    public required Func<CaptureSettings, CancellationToken, Task> StartRecordingAsync { get; init; }
    public required Func<CancellationToken, Task> StopRecordingAsync { get; init; }
    public required Func<bool> GetSessionIsRecording { get; init; }
    public required Action RestartRecordingStopwatch { get; init; }
    public required Action StopRecordingStopwatch { get; init; }
    public required Action ClearRecordingBitrateSamples { get; init; }
    public required Action<string> SetRecordingSizeInfo { get; init; }
    public required Action<string> SetRecordingBitrateInfo { get; init; }
    public required Func<string> GetRecordingTime { get; init; }
}

/// <summary>
/// Serializes recording start and stop requests and updates the UI after each transition.
/// </summary>
internal sealed class MainViewModelRecordingTransitionController
{
    private readonly MainViewModelRecordingTransitionControllerContext _context;
    private readonly MainViewModelPreviewLifecycleController _previewLifecycleController;
    private int _recordingToggleInProgress;
    // Holds the in-flight ToggleRecordingAsync task so the window-close path can
    // observe (and await) an already-running stop instead of short-circuiting on
    // the CAS gate. Cleared by the transition completion continuation.
    private volatile Task? _activeRecordingToggleTask;
    private int _activeRecordingTransitionTarget = -1;

    public MainViewModelRecordingTransitionController(
        MainViewModelRecordingTransitionControllerContext context,
        MainViewModelPreviewLifecycleController previewLifecycleController)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _previewLifecycleController = previewLifecycleController ?? throw new ArgumentNullException(nameof(previewLifecycleController));
    }

    public Task ToggleRecordingAsync()
        => SetRecordingDesiredStateAsync(!_context.IsRecording());

    public Task SetRecordingDesiredStateAsync(bool enabled, CancellationToken cancellationToken = default)
        => _context.InvokeOnUiThreadAsync(
            () => SetRecordingDesiredStateOnUiThreadAsync(enabled, cancellationToken),
            cancellationToken);

    /// <summary>
    /// Waits for any active recording toggle, then stops recording if it is still running.
    /// </summary>
    public Task StopRecordingAndWaitAsync(CancellationToken cancellationToken = default)
        => _context.InvokeOnUiThreadAsync(
            () => SetRecordingDesiredStateOnUiThreadAsync(enabled: false, cancellationToken),
            cancellationToken);

    private Task BeginRecordingTransitionAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        if (enabled == _context.IsRecording())
        {
            return Task.CompletedTask;
        }

        if (enabled && _previewLifecycleController.IsReinitializeAdmitted)
        {
            const string message = "Wait for capture settings to finish applying before starting recording.";
            _context.SetStatusText(message);
            throw new InvalidOperationException(message);
        }

        if (Interlocked.CompareExchange(ref _recordingToggleInProgress, 1, 0) != 0)
        {
            Logger.Log("Recording transition rejected: operation already in progress.");
            throw new InvalidOperationException("Recording transition already in progress.");
        }

        var task = RecordingTransitionInnerAsync(enabled, cancellationToken);
        Volatile.Write(ref _activeRecordingTransitionTarget, enabled ? 1 : 0);
        _activeRecordingToggleTask = task;
        _ = task.ContinueWith(completed =>
        {
            if (ReferenceEquals(_activeRecordingToggleTask, completed))
            {
                _activeRecordingToggleTask = null;
                Volatile.Write(ref _activeRecordingTransitionTarget, -1);
            }
        }, TaskScheduler.Default);

        return task;
    }

    private async Task SetRecordingDesiredStateOnUiThreadAsync(bool enabled, CancellationToken cancellationToken)
    {
        var inFlight = _activeRecordingToggleTask;
        if (inFlight != null && !inFlight.IsCompleted)
        {
            var inFlightTarget = Volatile.Read(ref _activeRecordingTransitionTarget);
            Exception? transitionError = null;
            try
            {
                await inFlight;
            }
            catch (OperationCanceledException ex)
            {
                transitionError = ex;
                Logger.Log($"Recording transition wait canceled: {ex.Message}");
            }
            catch (Exception ex)
            {
                transitionError = ex;
                Logger.Log($"Recording transition wait faulted: {ex.Message}");
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (transitionError is OperationCanceledException transitionCanceled && inFlightTarget == (enabled ? 1 : 0))
            {
                throw transitionCanceled;
            }

            if (transitionError != null && inFlightTarget == (enabled ? 1 : 0))
            {
                throw new InvalidOperationException("Recording transition failed.", transitionError);
            }

            if (_context.IsRecording() == enabled)
            {
                return;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (_context.IsRecording() == enabled)
        {
            return;
        }

        await BeginRecordingTransitionAsync(enabled, cancellationToken);
        if (_context.IsRecording() != enabled)
        {
            throw new InvalidOperationException(
                $"Recording transition did not reach requested state: requested={enabled}, actual={_context.IsRecording()}.");
        }
    }

    private async Task RecordingTransitionInnerAsync(bool enabled, CancellationToken cancellationToken)
    {
        try
        {
            _context.SetIsRecordingTransitioning(true);
            _context.SetStatusText(enabled ? "Starting recording..." : "Finalizing recording...");

            if (enabled)
            {
                await StartRecordingAsync(cancellationToken);
            }
            else
            {
                await StopRecordingAsync(cancellationToken);
            }

            if (_context.IsRecording() != enabled)
            {
                throw new InvalidOperationException(
                    $"Recording transition did not reach requested state: requested={enabled}, actual={_context.IsRecording()}.");
            }
        }
        finally
        {
            _context.SetIsRecordingTransitioning(false);
            Interlocked.Exchange(ref _recordingToggleInProgress, 0);
        }
    }

    private async Task StartRecordingAsync(CancellationToken cancellationToken = default)
    {
        if (!_context.HasSelectedDevice())
        {
            _context.SetStatusText("No device selected");
            throw new InvalidOperationException(_context.GetStatusText());
        }

        if (!_context.IsInitialized())
        {
            await _previewLifecycleController.InitializeDeviceAsync(cancellationToken);
        }

        try
        {
            var settings = _context.BuildCaptureSettings();
            await _context.StartRecordingAsync(settings, cancellationToken);

            _context.SetIsRecording(true);
            _context.RestartRecordingStopwatch();
            _context.ClearRecordingBitrateSamples();
            _context.SetRecordingSizeInfo("0 B");
            _context.SetRecordingBitrateInfo("--");
            _context.SetStatusText("Recording...");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _context.SetIsRecording(_context.GetSessionIsRecording());
            _context.SetStatusText("Recording start canceled");
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            _context.SetIsRecording(_context.GetSessionIsRecording());
            _context.SetStatusText($"Recording failed: {ex.Message}");
            throw;
        }
    }

    private async Task StopRecordingAsync(CancellationToken cancellationToken = default)
    {
        // UX: Freeze the timer immediately when the user requests stop (finalization can take seconds).
        // Keep IsRecording true until the stop transition completes so the button remains in "Stop" state.
        _context.StopRecordingStopwatch();
        _context.SetStatusText("Finalizing recording...");

        try
        {
            await _context.StopRecordingAsync(cancellationToken);
            _context.SetIsRecording(false);
            _context.SetStatusText($"Recording saved ({_context.GetRecordingTime()})");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _context.SetIsRecording(_context.GetSessionIsRecording());
            _context.SetStatusText("Stop recording canceled");
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            _context.SetIsRecording(_context.GetSessionIsRecording());
            _context.SetStatusText($"Recording failed: {ex.Message}");
            throw;
        }
    }
}
