using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class WindowUiDispatchControllerContext
{
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required MainViewModel ViewModel { get; init; }
    public required Action<Exception?> CompleteWindowCloseRequest { get; init; }
}

internal sealed class WindowUiDispatchController
{
    private readonly WindowUiDispatchControllerContext _context;

    public WindowUiDispatchController(WindowUiDispatchControllerContext context)
    {
        _context = context;
    }

    public Task InvokeAsync(Action action, CancellationToken cancellationToken = default)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        if (_context.DispatcherQueue.HasThreadAccess)
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

        var enqueued = _context.DispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _context.CompleteWindowCloseRequest(new OperationCanceledException(cancellationToken));
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

    public Task InvokeAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        if (_context.DispatcherQueue.HasThreadAccess)
        {
            return action();
        }

        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration registration = default;
        if (cancellationToken.CanBeCanceled)
        {
            registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        }

        var enqueued = _context.DispatcherQueue.TryEnqueue(async () =>
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

    public async Task<TResult> InvokeWithRetryAsync<TResult>(
        Func<TResult> action,
        string enqueueFailureMessage,
        CancellationToken cancellationToken = default)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (enqueueFailureMessage == null)
        {
            throw new ArgumentNullException(nameof(enqueueFailureMessage));
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (_context.DispatcherQueue.HasThreadAccess)
        {
            return action();
        }

        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var completion = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationTokenRegistration registration = default;
            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(() =>
                {
                    completion.TrySetCanceled(cancellationToken);
                });
            }

            var enqueued = _context.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        completion.TrySetCanceled(cancellationToken);
                        return;
                    }

                    completion.TrySetResult(action());
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

            if (enqueued)
            {
                return await completion.Task.ConfigureAwait(false);
            }

            registration.Dispose();
            if (attempt >= maxAttempts)
            {
                break;
            }

            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException(enqueueFailureMessage);
    }

    public async Task RunUiEventHandlerAsync(Func<Task> operation, string operationName)
    {
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            _context.ViewModel.StatusText = $"{operationName} failed: {ex.Message}";
        }
    }
}

internal sealed class MainViewModelUiDispatchControllerContext
{
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required Func<bool> IsDisposing { get; init; }
    public required Action<string> Log { get; init; }
    public required Action<Exception> LogException { get; init; }
    public required Action<string> SetStatusText { get; init; }
}

internal sealed class MainViewModelUiDispatchController
{
    private readonly MainViewModelUiDispatchControllerContext _context;

    public MainViewModelUiDispatchController(MainViewModelUiDispatchControllerContext context)
    {
        _context = context;
    }

    public bool Enqueue(Func<Task> operation, string operationName, bool allowDuringDispose = false)
    {
        if (!allowDuringDispose && _context.IsDisposing())
        {
            _context.Log($"UI_OPERATION_SKIP op='{operationName}' reason=disposing");
            return false;
        }

        var enqueued = _context.DispatcherQueue.TryEnqueue(() =>
        {
            if (!allowDuringDispose && _context.IsDisposing())
            {
                _context.Log($"UI_OPERATION_SKIP op='{operationName}' reason=disposing_after_enqueue");
                return;
            }

            _ = ExecuteAsync(operation, operationName);
        });
        if (!enqueued)
        {
            _context.Log($"UI_OPERATION_ENQUEUE_FAILED op='{operationName}'");
        }

        return enqueued;
    }

    public async Task ExecuteAsync(Func<Task> operation, string operationName)
    {
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            _context.LogException(ex);
            _context.SetStatusText($"{operationName} failed: {ex.Message}");
        }
    }

    public Task InvokeAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        if (_context.DispatcherQueue.HasThreadAccess)
        {
            return operation();
        }

        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration registration = default;
        if (cancellationToken.CanBeCanceled)
        {
            registration = cancellationToken.Register(() =>
            {
                completion.TrySetCanceled(cancellationToken);
            });
        }

        var enqueued = _context.DispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                registration.Dispose();
                registration = default;

                if (cancellationToken.IsCancellationRequested)
                {
                    completion.TrySetCanceled(cancellationToken);
                    return;
                }

                await operation();
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
            _context.Log("INVOKE_UI_OPERATION_ENQUEUE_FAILED kind=async");
            completion.TrySetException(new InvalidOperationException("Failed to enqueue UI operation."));
        }

        return completion.Task;
    }

    public Task<T> InvokeAsync<T>(Func<T> operation, CancellationToken cancellationToken = default)
    {
        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<T>(cancellationToken);
        }

        if (_context.DispatcherQueue.HasThreadAccess)
        {
            return Task.FromResult(operation());
        }

        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration registration = default;
        if (cancellationToken.CanBeCanceled)
        {
            registration = cancellationToken.Register(() =>
            {
                completion.TrySetCanceled(cancellationToken);
            });
        }

        var enqueued = _context.DispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                registration.Dispose();
                registration = default;

                if (cancellationToken.IsCancellationRequested)
                {
                    completion.TrySetCanceled(cancellationToken);
                    return;
                }

                completion.TrySetResult(operation());
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
            _context.Log("INVOKE_UI_OPERATION_ENQUEUE_FAILED kind=value");
            completion.TrySetException(new InvalidOperationException("Failed to enqueue UI operation."));
        }

        return completion.Task;
    }
}
