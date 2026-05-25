using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Sussudio.Models;
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
