using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio;

// UI-thread dispatching helpers shared by MainWindow automation, event
// handlers, and controller adapters.
public sealed partial class MainWindow
{
    private Task InvokeOnUiThreadAsync(Action action, CancellationToken cancellationToken = default)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            action();
            return Task.CompletedTask;
        }

        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration registration = default;
        if (cancellationToken.CanBeCanceled)
        {
            registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        }

        var enqueued = _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    CompleteWindowCloseRequest(new OperationCanceledException(cancellationToken));
                    completion.TrySetCanceled(cancellationToken);
                    return;
                }

                action();
                completion.TrySetResult(null);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
            finally
            {
                registration.Dispose();
            }
        });

        if (!enqueued)
        {
            registration.Dispose();
            completion.TrySetException(new InvalidOperationException("Failed to enqueue window action on the UI thread."));
        }

        return completion.Task;
    }

    private Task InvokeOnUiThreadAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            return action();
        }

        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration registration = default;
        if (cancellationToken.CanBeCanceled)
        {
            registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        }

        var enqueued = _dispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    completion.TrySetCanceled(cancellationToken);
                    return;
                }

                await action().ConfigureAwait(true);
                completion.TrySetResult(null);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
            finally
            {
                registration.Dispose();
            }
        });

        if (!enqueued)
        {
            registration.Dispose();
            completion.TrySetException(new InvalidOperationException("Failed to enqueue window action on the UI thread."));
        }

        return completion.Task;
    }

    private async Task RunUiEventHandlerAsync(Func<Task> operation, string operationName)
    {
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            ViewModel.StatusText = $"{operationName} failed: {ex.Message}";
        }
    }
}
