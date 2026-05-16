using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Controllers;

namespace Sussudio;

// UI-thread dispatching helpers shared by MainWindow automation, event
// handlers, and controller adapters. The controller owns enqueue/error policy;
// this partial keeps the private MainWindow adapter names stable for callers.
public sealed partial class MainWindow
{
    private WindowUiDispatchController? _windowUiDispatchController;

    private WindowUiDispatchController WindowUiDispatchController =>
        _windowUiDispatchController ??= new WindowUiDispatchController(
            new WindowUiDispatchControllerContext
            {
                DispatcherQueue = _dispatcherQueue,
                ViewModel = ViewModel,
                CompleteWindowCloseRequest = CompleteWindowCloseRequest
            });

    private Task InvokeOnUiThreadAsync(Action action, CancellationToken cancellationToken = default)
        => WindowUiDispatchController.InvokeAsync(action, cancellationToken);

    private Task InvokeOnUiThreadAsync(Func<Task> action, CancellationToken cancellationToken = default)
        => WindowUiDispatchController.InvokeAsync(action, cancellationToken);

    private Task RunUiEventHandlerAsync(Func<Task> operation, string operationName)
        => WindowUiDispatchController.RunUiEventHandlerAsync(operation, operationName);
}
