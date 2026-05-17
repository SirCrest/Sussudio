using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Dispatcher adapters and event fan-out used by view-model partials.
/// </summary>
public partial class MainViewModel
{
    private bool EnqueueUiOperation(Func<Task> operation, string operationName, bool allowDuringDispose = false)
        => _uiDispatchController.Enqueue(operation, operationName, allowDuringDispose);

    private Task ExecuteUiOperationAsync(Func<Task> operation, string operationName)
        => _uiDispatchController.ExecuteAsync(operation, operationName);

    private async Task NotifyPreviewReinitRequestedAsync(string reason)
    {
        var handlers = PreviewReinitRequested;
        if (handlers == null)
        {
            return;
        }

        foreach (Func<string, Task> handler in handlers.GetInvocationList())
        {
            await handler(reason);
        }
    }

    private async Task NotifyRendererStopAsync()
    {
        var handlers = PreviewRendererStopRequested;
        if (handlers == null)
        {
            return;
        }

        foreach (Func<Task> handler in handlers.GetInvocationList())
        {
            await handler();
        }
    }

    private Task InvokeOnUiThreadAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        => _uiDispatchController.InvokeAsync(operation, cancellationToken);

    private Task<T> InvokeOnUiThreadAsync<T>(Func<T> operation, CancellationToken cancellationToken = default)
        => _uiDispatchController.InvokeAsync(operation, cancellationToken);

    private static async Task AwaitWithTimeoutAsync(Task task, int timeoutMs, string operationName)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeoutMs)).ConfigureAwait(false);
        if (completed != task)
        {
            throw new TimeoutException($"{operationName} timed out after {timeoutMs} ms.");
        }

        await task.ConfigureAwait(false);
    }
}
