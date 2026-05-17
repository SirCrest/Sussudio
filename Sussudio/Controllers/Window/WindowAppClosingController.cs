using System;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;

namespace Sussudio.Controllers;

internal sealed class WindowAppClosingControllerContext
{
    public required WindowCloseLifecycleController LifecycleController { get; init; }
    public required Func<bool> IsRecording { get; init; }
    public required Func<bool> IsRecordingTransitioning { get; init; }
    public required Func<string> GetStatusText { get; init; }
    public required Func<Task<bool>> StopRecordingBeforeCloseAsync { get; init; }
    public required Action RequestWindowClose { get; init; }
}

internal sealed class WindowAppClosingController
{
    private readonly WindowAppClosingControllerContext _context;

    public WindowAppClosingController(WindowAppClosingControllerContext context)
    {
        _context = context;
    }

    public async Task HandleClosingAsync(AppWindowClosingEventArgs args)
    {
        LogWindowClosingTrigger();

        if (_context.LifecycleController.IsCleanupStarted ||
            _context.LifecycleController.IsAllowedAfterRecordingStop)
        {
            _context.LifecycleController.CompleteRequest();
            return;
        }

        if (!_context.IsRecording() && !_context.IsRecordingTransitioning())
        {
            _context.LifecycleController.CompleteRequest();
            return;
        }

        args.Cancel = true;
        _context.LifecycleController.ClearRequested();

        if (!_context.LifecycleController.TryBeginRecordingStop())
        {
            Logger.Log("WINDOW_CLOSE_RECORDING_STOP: close already waiting for recording stop.");
            return;
        }

        try
        {
            var stopped = await _context.StopRecordingBeforeCloseAsync();
            if (!stopped)
            {
                _context.LifecycleController.CompleteRequest(new InvalidOperationException(_context.GetStatusText()));
                return;
            }

            _context.LifecycleController.AllowAfterRecordingStop();
            _context.LifecycleController.CompleteRequest();
            _context.RequestWindowClose();
        }
        finally
        {
            _context.LifecycleController.EndRecordingStop();
        }
    }

    private void LogWindowClosingTrigger()
    {
        try
        {
            var snapshot = _context.LifecycleController.Snapshot;
            Logger.Log(
                "WINDOW_CLOSING_TRIGGER " +
                $"requested={snapshot.Requested} " +
                $"isRecording={_context.IsRecording()} " +
                $"stack=\n{new System.Diagnostics.StackTrace(true)}");
        }
        catch (Exception logEx)
        {
            System.Diagnostics.Trace.TraceWarning($"WINDOW_CLOSING_TRIGGER log failed: {logEx.Message}");
        }
    }
}
