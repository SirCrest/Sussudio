using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Sussudio.Controllers;
using Sussudio.Models;

namespace Sussudio;

// MainWindow shell-control adapter. Controllers own dispatch policy, window
// automation behavior, and close lifecycle state; this partial keeps the XAML
// and IAutomationWindowControl entry points in one place.
public sealed partial class MainWindow
{
    private WindowUiDispatchController? _windowUiDispatchController;
    private WindowAutomationController _windowAutomationController = null!;
    private readonly WindowCloseLifecycleController _windowCloseLifecycleController = new();
    private readonly WindowCloseRecordingFinalizationController _windowCloseRecordingFinalizationController = new();
    private WindowCloseRequestController _windowCloseRequestController = null!;
    private WindowAppClosingController _windowAppClosingController = null!;
    private bool _isWindowClosing => _windowCloseLifecycleController.IsClosing;

    private WindowUiDispatchController WindowUiDispatchController =>
        _windowUiDispatchController ??= new WindowUiDispatchController(
            new WindowUiDispatchControllerContext
            {
                DispatcherQueue = _dispatcherQueue,
                ViewModel = ViewModel,
                CompleteWindowCloseRequest = CompleteWindowCloseRequest
            });

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

    private void InitializeWindowAutomationController()
    {
        _windowAutomationController = new WindowAutomationController(
            new WindowAutomationControllerContext
            {
                DispatcherQueue = _dispatcherQueue,
                ViewModel = ViewModel,
                GetAppWindow = GetAppWindow,
                GetWindowHandle = () => _hwnd,
                InvokeOnUiThreadAsync = InvokeOnUiThreadAsync
            });
    }

    private void RegisterCloseLifecycle(Microsoft.UI.Windowing.AppWindow appWindow)
        => appWindow.Closing += MainWindow_Closing;

    private async void MainWindow_Closing(
        Microsoft.UI.Windowing.AppWindow sender,
        Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
        => await _windowAppClosingController.HandleClosingAsync(args);

    private Task InvokeOnUiThreadAsync(Action action, CancellationToken cancellationToken = default)
        => WindowUiDispatchController.InvokeAsync(action, cancellationToken);

    private Task InvokeOnUiThreadAsync(Func<Task> action, CancellationToken cancellationToken = default)
        => WindowUiDispatchController.InvokeAsync(action, cancellationToken);

    private Task RunUiEventHandlerAsync(Func<Task> operation, string operationName)
        => WindowUiDispatchController.RunUiEventHandlerAsync(operation, operationName);

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

    public Task MinimizeAsync(CancellationToken cancellationToken = default)
        => _windowAutomationController.MinimizeAsync(cancellationToken);

    public Task MaximizeAsync(CancellationToken cancellationToken = default)
        => _windowAutomationController.MaximizeAsync(cancellationToken);

    public Task RestoreAsync(CancellationToken cancellationToken = default)
        => _windowAutomationController.RestoreAsync(cancellationToken);

    public Task OpenRecordingsFolderAsync(CancellationToken cancellationToken = default)
        => _windowAutomationController.OpenRecordingsFolderAsync(cancellationToken);

    public Task MoveToAsync(int x, int y, CancellationToken cancellationToken = default)
        => _windowAutomationController.MoveToAsync(x, y, cancellationToken);

    public Task ResizeToAsync(int width, int height, CancellationToken cancellationToken = default)
        => _windowAutomationController.ResizeToAsync(width, height, cancellationToken);

    public Task SnapToRegionAsync(AutomationWindowAction region, CancellationToken cancellationToken = default)
        => _windowAutomationController.SnapToRegionAsync(region, cancellationToken);
}
