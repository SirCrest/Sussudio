using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.Win32;
using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.Services.Runtime;

namespace Sussudio.Controllers;

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
/// Owns runtime bootstrap, periodic refresh, and shutdown coordination for
/// the compatibility ViewModel facade.
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

        SetupTimer();
        _context.UpdateDiskSpace();
    }

    public void StopForDispose()
    {
        _timer?.Stop();
        _eventIngressController.Detach();
        _context.DisposeAudioDeviceWatcher();
    }

    private void SetupTimer()
    {
        _timer = _context.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) =>
        {
            var runtimeSnapshot = _context.GetRuntimeSnapshot();

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
    public required Action DisposeCaptureService { get; init; }
    public required Func<Task, int, string, Task> AwaitWithTimeoutAsync { get; init; }
}

/// <summary>
/// Owns bounded teardown policy for the compatibility ViewModel facade.
/// </summary>
internal sealed class MainViewModelDisposalController
{
    private const int DefaultDisposeTimeoutMs = 30000;

    private readonly MainViewModelDisposalControllerContext _context;

    public MainViewModelDisposalController(MainViewModelDisposalControllerContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Dispose()
    {
        var disposeTimeoutMs = GetDisposeTimeoutMs();
        var disposeTask = Task.Run(DisposeCoreAsync);
        var completed = Task.WhenAny(disposeTask, Task.Delay(disposeTimeoutMs)).GetAwaiter().GetResult();
        if (completed != disposeTask)
        {
            Logger.Log($"ViewModel dispose timed out after {disposeTimeoutMs} ms.");
            return;
        }

        try
        {
            disposeTask.GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Logger.Log($"ViewModel dispose failed: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        var disposeTimeoutMs = GetDisposeTimeoutMs();
        var disposeTask = DisposeCoreAsync();
        var completed = await Task.WhenAny(disposeTask, Task.Delay(disposeTimeoutMs)).ConfigureAwait(false);
        if (completed != disposeTask)
        {
            Logger.Log($"ViewModel async dispose timed out after {disposeTimeoutMs} ms.");
            return;
        }

        try
        {
            await disposeTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"ViewModel async dispose failed: {ex.Message}");
        }
    }

    private async Task DisposeCoreAsync()
    {
        if (!_context.TryBeginDispose())
        {
            return;
        }

        _context.CancelActiveFlashbackExport();
        _context.CancelPendingAudioControlWork();
        _context.StopRuntimeForDispose();

        var stepTimeoutMs = EnvironmentHelpers.GetIntFromEnv(
            "SUSSUDIO_VIEWMODEL_DISPOSE_STEP_TIMEOUT_MS",
            DefaultDisposeTimeoutMs,
            1000,
            300000);

        await RunDisposeStepAsync(
            _context.CleanupSessionCoordinatorAsync(),
            stepTimeoutMs,
            "Coordinator cleanup",
            "ViewModel cleanup during dispose failed").ConfigureAwait(false);
        await RunDisposeStepAsync(
            _context.DisposeSessionCoordinatorAsync(),
            stepTimeoutMs,
            "Coordinator dispose",
            "Coordinator dispose failed").ConfigureAwait(false);

        try
        {
            await _context.AwaitWithTimeoutAsync(
                _context.DisposeCaptureServiceAsync(),
                stepTimeoutMs,
                "Capture service dispose").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"Capture service async dispose failed: {ex.Message}");
            _context.DisposeCaptureService();
        }
    }

    private static int GetDisposeTimeoutMs()
        => EnvironmentHelpers.GetIntFromEnv(
            "SUSSUDIO_VIEWMODEL_DISPOSE_TIMEOUT_MS",
            DefaultDisposeTimeoutMs,
            1000,
            300000);

    private async Task RunDisposeStepAsync(
        Task task,
        int timeoutMs,
        string operationName,
        string failureLogPrefix)
    {
        try
        {
            await _context.AwaitWithTimeoutAsync(task, timeoutMs, operationName).ConfigureAwait(false);
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
    public required Action<EventHandler<Exception>> AttachCaptureErrorOccurred { get; init; }
    public required Action<EventHandler<Exception>> DetachCaptureErrorOccurred { get; init; }
    public required Action<Action> AttachCapturePreCleanupRequested { get; init; }
    public required Action<Action> DetachCapturePreCleanupRequested { get; init; }
    public required Action<EventHandler<ulong>> AttachFrameCaptured { get; init; }
    public required Action<EventHandler<ulong>> DetachFrameCaptured { get; init; }
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
    public required Func<Func<Task>[]> GetPreviewRendererStopHandlers { get; init; }
    public required Func<string, Task> ReinitializeDeviceAsync { get; init; }
    public required Func<Func<Task>, string, bool> EnqueueUiOperation { get; init; }
}

/// <summary>
/// Owns runtime event subscriptions and external event ingress for the
/// compatibility ViewModel facade.
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
        _context.AttachCapturePreCleanupRequested(OnCapturePreCleanupRequested);
        _context.AttachFrameCaptured(OnFrameCaptured);
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
        _context.DetachCapturePreCleanupRequested(OnCapturePreCleanupRequested);
        _context.DetachFrameCaptured(OnFrameCaptured);
        _context.DetachAudioLevelUpdated(_context.OnAudioLevelUpdated);
        _context.DetachMicrophoneAudioLevelUpdated(_context.OnMicrophoneAudioLevelUpdated);
        _context.DetachSourceTelemetryUpdated(_context.OnSourceTelemetryUpdated);

        _context.DetachAudioDevicesChanged(_context.OnAudioDevicesChanged);
    }

    private void OnCaptureStatusChanged(object? sender, string status)
    {
        if (!_context.TryEnqueueOnUiThread(() =>
        {
            var runtimeSnapshot = _context.GetRuntimeSnapshot();
            _context.SetStatusText(status);
            _context.UpdateLiveCaptureInfo(runtimeSnapshot);
            _context.UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);
        }))
        {
            Logger.Log($"CAPTURE_STATUS_UI_ENQUEUE_FAILED status='{status}'");
        }
    }

    private void OnCaptureError(object? sender, Exception ex)
    {
        if (!_context.TryEnqueueOnUiThread(() =>
        {
            var runtimeSnapshot = _context.GetRuntimeSnapshot();
            _context.SetStatusText($"Error: {ex.Message}");
            _context.SetIsInitialized(_context.IsCaptureInitialized());
            _context.SetIsPreviewing(_context.IsVideoPreviewActive());
            _context.SetIsRecording(_context.IsCaptureRecording());
            if (!_context.IsPreviewing() && !_context.IsRecording())
            {
                _context.ResetAudioMeter();
            }

            _context.UpdateLiveCaptureInfo(runtimeSnapshot);
            _context.UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);

            // AUDCLNT_E_DEVICE_INVALIDATED (0x88890004) arrives when the audio
            // engine is reset independently of a full system suspend, e.g. monitor
            // power-off, USB hot-unplug, or wake events that don't trigger
            // PowerManager.SystemResuming. Trigger a full rebind so the user does
            // not have to manually re-pick the device. The IsRecording guard inside
            // ReinitializeDeviceAsync (fix #1) prevents this path from running
            // mid-recording; EnqueueUiOperation serializes with any in-flight
            // PowerManager-triggered reinit from OnSystemResuming.
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
                        () => _context.ReinitializeDeviceAsync("audio device invalidated"),
                        "audio device invalidated reinit");
                }
            }
        }))
        {
            Logger.Log($"CAPTURE_ERROR_UI_ENQUEUE_FAILED type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void OnCapturePreCleanupRequested()
    {
        // Fires on a background thread before CaptureService.CleanupAsync disposes
        // the shared D3D11 device. Stop the renderer first to prevent the same race
        // as the reinit crash, where the renderer calls native D3D on a dying device.
        var handlers = _context.GetPreviewRendererStopHandlers();
        foreach (var handler in handlers)
        {
            try { handler().GetAwaiter().GetResult(); }
            catch (Exception ex) { Logger.Log($"PreCleanup renderer stop warning: {ex.Message}"); }
        }
    }

    private void OnFrameCaptured(object? sender, ulong frameCount)
    {
        // Could update frame count display if needed.
    }

    // PowerModeChanged fires on the system thread pool - must not touch UI properties
    // directly. We act only on PowerModes.Resume; Suspend/StatusChange are ignored
    // (Suspend arrives just before the OS freezes the process so there's nothing
    // useful to do, and StatusChange fires on AC/battery transitions which don't
    // affect capture). All UI-state reads happen inside the EnqueueUiOperation
    // lambda, which executes on the DispatcherQueue thread. ReinitializeDeviceAsync's
    // IsRecording guard (fix #1) keeps this safe to call regardless of state.
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
