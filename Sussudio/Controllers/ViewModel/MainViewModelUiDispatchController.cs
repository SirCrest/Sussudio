using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace Sussudio.Controllers;

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
