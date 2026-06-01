using System;
using System.Runtime.InteropServices;
using System.Threading;
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

/// <summary>
/// Graph-built ports consumed by the preview lifecycle controller.
/// </summary>
internal sealed class MainViewModelPreviewLifecycleControllerContext
{
    public required CaptureSessionCoordinator SessionCoordinator { get; init; }
    public required Func<CaptureSettings> BuildCaptureSettings { get; init; }
    public required Func<Func<Task>, CancellationToken, Task> InvokeOnUiThreadAsync { get; init; }
    public required Func<CancellationToken, Task> RampPreviewVolumeDownForStopAsync { get; init; }
    public required Func<MainViewModelPreviewLifecycleController, MainViewModelPreviewReinitializeController> CreateReinitializeController { get; init; }
    public required Func<CaptureDevice?> SelectedDevice { get; init; }
    public required Action<CaptureDevice> SetSelectedDevice { get; init; }
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
/// Owns UI-facing preview lifecycle operations behind the MainViewModel compatibility facade.
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

    public async Task InitializeDeviceAsync(CancellationToken cancellationToken = default)
    {
        var selectedDevice = _context.SelectedDevice();
        if (selectedDevice == null)
        {
            Logger.Log("ERROR: SelectedDevice is NULL");
            return;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
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

        if (_context.IsInitialized())
        {
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
        else
        {
            Logger.Log("Cannot start preview - device not initialized");
            _context.SetStatusText("Cannot start preview - device not initialized");
        }
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
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_context.IsRecording())
        {
            _context.SetStatusText("Stop recording before switching capture devices.");
            return;
        }

        var selectedDevice = _context.SelectedDevice();
        if (selectedDevice != null &&
            string.Equals(selectedDevice.Id, device.Id, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Logger.Log($"DEVICE_APPLY_REQUEST device='{device.Name}' id='{device.Id}' preview={_context.IsPreviewing()} initialized={_context.IsInitialized()}");
        _context.SetSelectedDevice(device);

        if (_context.IsPreviewing())
        {
            await ReinitializeDeviceAsync("device selection apply").ConfigureAwait(true);
            return;
        }

        _context.SetIsInitialized(false);
        _context.SetStatusText($"Selected device: {device.Name}");
    }

    public async Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (userInitiated && _context.IsPreviewReinitializing())
        {
            CancelPendingPreviewRestart();
        }

        if (userInitiated && !_context.IsPreviewReinitializing() && _context.IsAudioPreviewActive())
        {
            await _context.RampPreviewVolumeDownForStopAsync(cancellationToken);
        }

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
}

/// <summary>
/// Graph-built ports consumed by the preview reinitialize transaction controller.
/// </summary>
internal sealed class MainViewModelPreviewReinitializeControllerContext
{
    public required Func<CaptureDevice?> SelectedDevice { get; init; }
    public required Func<MediaFormat?> SelectedFormat { get; init; }
    public required Func<bool> IsRecording { get; init; }
    public required Func<bool> IsInitialized { get; init; }
    public required Action<bool> SetIsInitialized { get; init; }
    public required Func<bool> IsPreviewing { get; init; }
    public required Func<bool> IsPreviewReinitializing { get; init; }
    public required Action<bool> SetIsPreviewReinitializing { get; init; }
    public required Action<string> SetStatusText { get; init; }
    public required Func<bool> CancelPreviewRestartAfterReinitialize { get; init; }
    public required Action<bool> SetCancelPreviewRestartAfterReinitialize { get; init; }
    public required Func<int> IncrementReinitializeGeneration { get; init; }
    public required Func<int> ReadReinitializeGeneration { get; init; }
    public required int PreviewReinitializeDebounceMs { get; init; }
    public required Func<Task?> PendingFlashbackCycleTask { get; init; }
    public required int FlashbackCycleBeforeReinitializeTimeoutMs { get; init; }
    public required Func<Task, int, string, Task> AwaitWithTimeoutAsync { get; init; }
    public required Action<Task> ClearPendingFlashbackCycleIfSameAndCompleted { get; init; }
    public required Func<Task> WaitReinitializeGateAsync { get; init; }
    public required Action ReleaseReinitializeGate { get; init; }
    public required Func<string, Task> NotifyPreviewReinitRequestedAsync { get; init; }
    public required Func<Task> NotifyRendererStopAsync { get; init; }
}

/// <summary>
/// Owns the debounced preview reinitialization transaction for the compatibility ViewModel facade.
/// </summary>
internal sealed class MainViewModelPreviewReinitializeController
{
    private readonly MainViewModelPreviewReinitializeControllerContext _context;
    private readonly MainViewModelPreviewLifecycleController _previewLifecycleController;

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
            _context.SetCancelPreviewRestartAfterReinitialize(true);
        }
    }

    public void ResetPendingPreviewRestartCancellation()
    {
        _context.SetCancelPreviewRestartAfterReinitialize(false);
    }

    public async Task ReinitializeDeviceAsync(string reason)
    {
        if (_context.SelectedDevice() == null || _context.SelectedFormat() == null)
        {
            return;
        }

        if (_context.IsRecording())
        {
            Logger.Log($"REINIT_REJECTED_RECORDING reason='{reason}' - stop recording before changing capture settings.");
            _context.SetStatusText("Stop recording before changing capture settings.");
            return;
        }

        var reinitializeGeneration = _context.IncrementReinitializeGeneration();
        await Task.Delay(_context.PreviewReinitializeDebounceMs).ConfigureAwait(true);
        if (_context.ReadReinitializeGeneration() != reinitializeGeneration)
        {
            Logger.Log($"REINIT_COALESCED reason='{reason}' generation={reinitializeGeneration}");
            return;
        }

        var pendingCycle = _context.PendingFlashbackCycleTask();
        if (pendingCycle != null)
        {
            try
            {
                await _context.AwaitWithTimeoutAsync(
                    pendingCycle,
                    _context.FlashbackCycleBeforeReinitializeTimeoutMs,
                    "Flashback encoder settings cycle before reinitialize").ConfigureAwait(false);
            }
            catch (TimeoutException ex)
            {
                Logger.Log($"REINIT_WAIT_FLASHBACK_CYCLE_TIMEOUT reason={reason} timeoutMs={_context.FlashbackCycleBeforeReinitializeTimeoutMs}");
                _context.SetStatusText($"Failed to apply format: {ex.Message}");
                return;
            }
            catch (Exception ex)
            {
                Logger.Log($"REINIT_WAIT_FLASHBACK_CYCLE_FAULT reason={reason} type={ex.GetType().Name} msg='{ex.Message}'");
            }

            _context.ClearPendingFlashbackCycleIfSameAndCompleted(pendingCycle);
        }

        await _context.WaitReinitializeGateAsync();
        var shouldRestartPreview = _context.IsPreviewing();
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

            if (_context.IsPreviewing())
            {
                await _previewLifecycleController.StopPreviewAsync(userInitiated: false, teardownPipeline: true, CancellationToken.None);
            }

            _context.SetIsInitialized(false);
            Logger.LogFatalBreadcrumb($"REINIT phase=init_device reason={reason}");
            await _previewLifecycleController.InitializeDeviceAsync();
            Logger.LogFatalBreadcrumb($"REINIT phase=init_device_done reason={reason}");

            if (_context.IsInitialized() && shouldRestartPreview && !_context.CancelPreviewRestartAfterReinitialize())
            {
                Logger.LogFatalBreadcrumb($"REINIT phase=start_preview reason={reason}");
                await _previewLifecycleController.StartPreviewAsync(userInitiated: false);
                Logger.LogFatalBreadcrumb($"REINIT phase=start_preview_done reason={reason}");

                var selectedFormat = _context.SelectedFormat()!;
                _context.SetStatusText($"Preview: {selectedFormat.Width}x{selectedFormat.Height}@{selectedFormat.FrameRate}fps");
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            _context.SetStatusText($"Failed to apply format: {ex.Message}");
        }
        finally
        {
            ResetPendingPreviewRestartCancellation();
            if (shouldRestartPreview)
            {
                _context.SetIsPreviewReinitializing(false);
            }

            _context.ReleaseReinitializeGate();
        }
    }
}

/// <summary>
/// Graph-built ports consumed by the recording transition controller.
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
/// Owns UI-facing recording start/stop transition serialization and state repair.
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
    /// Graceful-stop entry point for callers that must NOT short-circuit on the
    /// toggle CAS gate. If a toggle is in flight, await it; afterwards, if still
    /// recording, initiate a fresh stop.
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
            _context.SetStatusText(enabled ? "Starting recording..." : "Stopping recording...");

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
            if (!_context.IsInitialized())
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(_context.GetStatusText())
                        ? "Device failed to initialize."
                        : _context.GetStatusText());
            }
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
            _context.SetStatusText($"Stop recording failed: {ex.Message}");
            throw;
        }
    }
}
