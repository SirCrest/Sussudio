using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using System.Threading;
using Sussudio.Controllers;

namespace Sussudio;

// Window close lifecycle and automation close completion. Recording
// finalization side effects live in WindowCloseRecordingFinalizationController.
public sealed partial class MainWindow
{
    private readonly WindowCloseLifecycleController _windowCloseLifecycleController = new();
    private readonly WindowCloseRecordingFinalizationController _windowCloseRecordingFinalizationController = new();
    private WindowCloseRequestController _windowCloseRequestController = null!;
    private bool _isWindowClosing => _windowCloseLifecycleController.IsClosing;

    private void InitializeWindowCloseRequestController()
    {
        _windowCloseRequestController = new WindowCloseRequestController(new WindowCloseRequestControllerContext
        {
            LifecycleController = _windowCloseLifecycleController,
            CloseWindow = Close,
            ExitApplication = () => Application.Current.Exit(),
            IsRecording = () => ViewModel.IsRecording,
            IsRecordingTransitioning = () => ViewModel.IsRecordingTransitioning
        });
    }

    private void RegisterCloseLifecycle(Microsoft.UI.Windowing.AppWindow appWindow)
        => appWindow.Closing += MainWindow_Closing;

    private async void MainWindow_Closing(
        Microsoft.UI.Windowing.AppWindow sender,
        Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        try
        {
            var snapshot = _windowCloseLifecycleController.Snapshot;
            Logger.Log(
                "WINDOW_CLOSING_TRIGGER " +
                $"requested={snapshot.Requested} " +
                $"isRecording={ViewModel.IsRecording} " +
                $"stack=\n{new System.Diagnostics.StackTrace(true)}");
        }
        catch (Exception logEx)
        {
            System.Diagnostics.Trace.TraceWarning($"WINDOW_CLOSING_TRIGGER log failed: {logEx.Message}");
        }

        if (_windowCloseLifecycleController.IsCleanupStarted ||
            _windowCloseLifecycleController.IsAllowedAfterRecordingStop)
        {
            CompleteWindowCloseRequest();
            return;
        }

        if (!ViewModel.IsRecording && !ViewModel.IsRecordingTransitioning)
        {
            CompleteWindowCloseRequest();
            return;
        }

        args.Cancel = true;
        _windowCloseLifecycleController.ClearRequested();

        if (!_windowCloseLifecycleController.TryBeginRecordingStop())
        {
            Logger.Log("WINDOW_CLOSE_RECORDING_STOP: close already waiting for recording stop.");
            return;
        }

        try
        {
            var stopped = await TryStopRecordingBeforeCloseAsync();
            if (!stopped)
            {
                CompleteWindowCloseRequest(new InvalidOperationException(ViewModel.StatusText));
                return;
            }

            _windowCloseLifecycleController.AllowAfterRecordingStop();
            CompleteWindowCloseRequest();
            RequestWindowClose();
        }
        finally
        {
            _windowCloseLifecycleController.EndRecordingStop();
        }
    }

    private Task<bool> TryStopRecordingBeforeCloseAsync()
        => _windowCloseRecordingFinalizationController.StopBeforeCloseAsync(
            ViewModel,
            Content as FrameworkElement,
            () => _windowCloseLifecycleController.IsAllowedAfterRecordingStop);

    public Task CloseAsync(CancellationToken cancellationToken = default)
        => _windowCloseLifecycleController.CloseAsync(_dispatcherQueue, RequestWindowClose, cancellationToken);

    private void CompleteWindowCloseRequest(Exception? exception = null)
        => _windowCloseLifecycleController.CompleteRequest(exception);

    private void RequestWindowClose()
        => _windowCloseRequestController.RequestClose();
}
