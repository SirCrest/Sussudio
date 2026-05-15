using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace Sussudio.Controllers;

internal readonly record struct WindowCloseLifecycleSnapshot(
    int Requested,
    int CleanupStarted,
    int RecordingStopInProgress,
    int AllowedAfterRecordingStop,
    bool IsClosing);

internal sealed class WindowCloseLifecycleController
{
    private int _closeRequested;
    private int _cleanupStarted;
    private int _recordingStopInProgress;
    private int _allowedAfterRecordingStop;
    private readonly object _completionLock = new();
    private TaskCompletionSource<object?>? _completion;
    private bool _isClosing;

    public bool IsClosing => _isClosing;

    public bool IsCleanupStarted => Volatile.Read(ref _cleanupStarted) != 0;

    public bool IsRecordingStopInProgress => Volatile.Read(ref _recordingStopInProgress) != 0;

    public bool IsAllowedAfterRecordingStop => Volatile.Read(ref _allowedAfterRecordingStop) != 0;

    public WindowCloseLifecycleSnapshot Snapshot => new(
        Volatile.Read(ref _closeRequested),
        Volatile.Read(ref _cleanupStarted),
        Volatile.Read(ref _recordingStopInProgress),
        Volatile.Read(ref _allowedAfterRecordingStop),
        _isClosing);

    public bool TryBeginCleanup()
        => Interlocked.Exchange(ref _cleanupStarted, 1) == 0;

    public void MarkClosing()
        => _isClosing = true;

    public void CompleteRequest(Exception? exception = null)
    {
        TaskCompletionSource<object?>? completion;
        lock (_completionLock)
        {
            completion = _completion;
            _completion = null;
        }

        if (completion == null)
        {
            return;
        }

        if (exception == null)
        {
            completion.TrySetResult(null);
        }
        else
        {
            completion.TrySetException(exception);
        }
    }

    public void ClearRequested()
        => Interlocked.Exchange(ref _closeRequested, 0);

    public bool TryBeginRecordingStop()
        => Interlocked.Exchange(ref _recordingStopInProgress, 1) == 0;

    public void EndRecordingStop()
        => Interlocked.Exchange(ref _recordingStopInProgress, 0);

    public void AllowAfterRecordingStop()
    {
        Interlocked.Exchange(ref _allowedAfterRecordingStop, 1);
        ClearRequested();
    }

    public bool TryMarkRequested()
    {
        if (IsCleanupStarted)
        {
            CompleteRequest();
            return false;
        }

        return Interlocked.Exchange(ref _closeRequested, 1) == 0;
    }

    public void ResetRequestedAfterFailure()
        => ClearRequested();

    public Task CloseAsync(
        DispatcherQueue dispatcherQueue,
        Action requestWindowClose,
        CancellationToken cancellationToken = default)
    {
        if (dispatcherQueue == null)
        {
            throw new ArgumentNullException(nameof(dispatcherQueue));
        }

        if (requestWindowClose == null)
        {
            throw new ArgumentNullException(nameof(requestWindowClose));
        }

        if (IsCleanupStarted)
        {
            return Task.CompletedTask;
        }

        var closeCompletionTask = GetCompletionTask(cancellationToken);

        if (dispatcherQueue.HasThreadAccess)
        {
            requestWindowClose();
            return closeCompletionTask;
        }

        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration registration = default;
        if (cancellationToken.CanBeCanceled)
        {
            registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        }

        var enqueued = dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    completion.TrySetCanceled(cancellationToken);
                    return;
                }

                requestWindowClose();
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
            if (IsCleanupStarted)
            {
                completion.TrySetResult(null);
            }
            else
            {
                var enqueueFailure = new InvalidOperationException("Failed to enqueue window close action on the UI thread.");
                CompleteRequest(enqueueFailure);
                completion.TrySetException(enqueueFailure);
            }
        }

        return AwaitWindowCloseRequestAsync(completion.Task, closeCompletionTask);
    }

    public static bool IsCloseAlreadyInProgressException(Exception ex)
    {
        if (ex is InvalidOperationException && string.IsNullOrWhiteSpace(ex.Message))
        {
            return true;
        }

        var message = ex.Message ?? string.Empty;
        return message.IndexOf("closing", StringComparison.OrdinalIgnoreCase) >= 0 ||
               message.IndexOf("closed", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private Task GetCompletionTask(CancellationToken cancellationToken)
    {
        TaskCompletionSource<object?> completion;
        lock (_completionLock)
        {
            if (_completion == null || _completion.Task.IsCompleted)
            {
                _completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            completion = _completion;
        }

        return cancellationToken.CanBeCanceled
            ? completion.Task.WaitAsync(cancellationToken)
            : completion.Task;
    }

    private static async Task AwaitWindowCloseRequestAsync(Task enqueueTask, Task closeCompletionTask)
    {
        await enqueueTask.ConfigureAwait(false);
        await closeCompletionTask.ConfigureAwait(false);
    }
}
