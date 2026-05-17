using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

// Window close lifecycle and automation close completion. Recording
// finalization side effects live in WindowCloseRecordingFinalizationController.
public sealed partial class MainWindow
{
    private readonly WindowCloseLifecycleController _windowCloseLifecycleController = new();
    private readonly WindowCloseRecordingFinalizationController _windowCloseRecordingFinalizationController = new();
    private WindowCloseRequestController _windowCloseRequestController = null!;
    private WindowAppClosingController _windowAppClosingController = null!;
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

        _windowAppClosingController = new WindowAppClosingController(new WindowAppClosingControllerContext
        {
            LifecycleController = _windowCloseLifecycleController,
            IsRecording = () => ViewModel.IsRecording,
            IsRecordingTransitioning = () => ViewModel.IsRecordingTransitioning,
            GetStatusText = () => ViewModel.StatusText,
            StopRecordingBeforeCloseAsync = TryStopRecordingBeforeCloseAsync,
            RequestWindowClose = RequestWindowClose
        });
    }

    private void RegisterCloseLifecycle(Microsoft.UI.Windowing.AppWindow appWindow)
        => appWindow.Closing += MainWindow_Closing;

    private async void MainWindow_Closing(
        Microsoft.UI.Windowing.AppWindow sender,
        Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
        => await _windowAppClosingController.HandleClosingAsync(args);

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
