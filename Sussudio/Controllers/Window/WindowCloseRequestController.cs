using System;
using System.Runtime.InteropServices;

namespace Sussudio.Controllers;

internal sealed class WindowCloseRequestControllerContext
{
    public required WindowCloseLifecycleController LifecycleController { get; init; }
    public required Action CloseWindow { get; init; }
    public required Action ExitApplication { get; init; }
    public required Func<bool> IsRecording { get; init; }
    public required Func<bool> IsRecordingTransitioning { get; init; }
}

internal sealed class WindowCloseRequestController
{
    private readonly WindowCloseRequestControllerContext _context;

    public WindowCloseRequestController(WindowCloseRequestControllerContext context)
    {
        _context = context;
    }

    public void RequestClose()
    {
        if (!_context.LifecycleController.TryMarkRequested())
        {
            return;
        }

        try
        {
            _context.CloseWindow();
            if (!_context.LifecycleController.IsRecordingStopInProgress &&
                !_context.IsRecording() &&
                !_context.IsRecordingTransitioning())
            {
                _context.LifecycleController.CompleteRequest();
            }
        }
        catch (Exception ex) when (WindowCloseLifecycleController.IsCloseAlreadyInProgressException(ex))
        {
            Logger.Log($"Window close already in progress ({ex.GetType().Name}); treating close request as successful.");
            _context.LifecycleController.CompleteRequest();
        }
        catch (COMException ex)
        {
            Logger.Log($"Window.Close COMException (0x{ex.HResult:X8}); using Application.Current.Exit() fallback.");
            _context.LifecycleController.CompleteRequest();
            _context.ExitApplication();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Suppressed exception in MainWindow.RequestWindowClose: {ex.Message}");
            _context.LifecycleController.ResetRequestedAfterFailure();
            _context.LifecycleController.CompleteRequest(ex);
            throw;
        }
    }
}
