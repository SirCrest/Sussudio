using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Sussudio.Controllers;

internal sealed class WindowShutdownCleanupControllerContext
{
    public required WindowCloseLifecycleController LifecycleController { get; init; }

    public required Func<bool> IsRecording { get; init; }

    public required Func<bool> IsPreviewing { get; init; }

    public required Action CancelNativeShellRevealAfterFirstFrame { get; init; }

    public required Action CompleteWindowCloseRequest { get; init; }

    public required Action DetachMeterActivationHandlers { get; init; }

    public required Action StopTimers { get; init; }

    public required Action StopStatsOverlay { get; init; }

    public required Action StopRecordingVisuals { get; init; }

    public required Action DetachMainContentSizeChanged { get; init; }

    public required Action DetachViewModelEventHandlers { get; init; }

    public required Action StopPreviewForShutdown { get; init; }

    public required Action ResetPreviewStartupTracking { get; init; }

    public required Func<Task> StopRecordingAfterClosedBestEffortAsync { get; init; }

    public required Func<ValueTask> DisposeAutomationHostAsync { get; init; }

    public required Action DisposeNvmlMonitor { get; init; }

    public required Func<ValueTask> DisposeViewModelAsync { get; init; }
}

internal sealed class WindowShutdownCleanupController
{
    private readonly WindowShutdownCleanupControllerContext _context;

    public WindowShutdownCleanupController(WindowShutdownCleanupControllerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task RunAsync()
    {
        _context.CancelNativeShellRevealAfterFirstFrame();

        if (!_context.LifecycleController.TryBeginCleanup())
        {
            return;
        }

        LogWindowClosedTrigger();

        _context.CompleteWindowCloseRequest();
        _context.LifecycleController.MarkClosing();
        _context.DetachMeterActivationHandlers();
        _context.StopTimers();
        _context.StopStatsOverlay();
        _context.StopRecordingVisuals();
        _context.DetachMainContentSizeChanged();
        _context.DetachViewModelEventHandlers();

        try
        {
            _context.StopPreviewForShutdown();
            _context.ResetPreviewStartupTracking();
        }
        catch (Exception ex)
        {
            Logger.Log($"Preview shutdown cleanup failed: {ex.Message}");
        }

        await _context.StopRecordingAfterClosedBestEffortAsync().ConfigureAwait(false);
        await _context.DisposeAutomationHostAsync().ConfigureAwait(false);

        _context.DisposeNvmlMonitor();

        try
        {
            await _context.DisposeViewModelAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"ViewModel dispose during window close failed: {ex.Message}");
        }
    }

    private void LogWindowClosedTrigger()
    {
        try
        {
            var snapshot = _context.LifecycleController.Snapshot;
            Logger.Log(
                "WINDOW_CLOSED_TRIGGER " +
                $"requested={snapshot.Requested} " +
                $"recordingStopInProgress={snapshot.RecordingStopInProgress} " +
                $"allowedAfterRecordingStop={snapshot.AllowedAfterRecordingStop} " +
                $"isRecording={_context.IsRecording()} " +
                $"isPreviewing={_context.IsPreviewing()} " +
                $"stack=\n{new StackTrace(true)}");
        }
        catch (Exception logEx)
        {
            Trace.TraceWarning($"WINDOW_CLOSED_TRIGGER log failed: {logEx.Message}");
        }
    }
}
