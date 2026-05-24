using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;

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

internal sealed class WindowAppClosingControllerContext
{
    public required WindowCloseLifecycleController LifecycleController { get; init; }
    public required Func<bool> IsRecording { get; init; }
    public required Func<bool> IsRecordingTransitioning { get; init; }
    public required Func<string> GetStatusText { get; init; }
    public required Func<Task<bool>> StopRecordingBeforeCloseAsync { get; init; }
    public required Action RequestWindowClose { get; init; }
}

internal sealed class WindowAppClosingController
{
    private readonly WindowAppClosingControllerContext _context;

    public WindowAppClosingController(WindowAppClosingControllerContext context)
    {
        _context = context;
    }

    public async Task HandleClosingAsync(AppWindowClosingEventArgs args)
    {
        LogWindowClosingTrigger();

        if (_context.LifecycleController.IsCleanupStarted ||
            _context.LifecycleController.IsAllowedAfterRecordingStop)
        {
            _context.LifecycleController.CompleteRequest();
            return;
        }

        if (!_context.IsRecording() && !_context.IsRecordingTransitioning())
        {
            _context.LifecycleController.CompleteRequest();
            return;
        }

        args.Cancel = true;
        _context.LifecycleController.ClearRequested();

        if (!_context.LifecycleController.TryBeginRecordingStop())
        {
            Logger.Log("WINDOW_CLOSE_RECORDING_STOP: close already waiting for recording stop.");
            return;
        }

        try
        {
            var stopped = await _context.StopRecordingBeforeCloseAsync();
            if (!stopped)
            {
                _context.LifecycleController.CompleteRequest(new InvalidOperationException(_context.GetStatusText()));
                return;
            }

            _context.LifecycleController.AllowAfterRecordingStop();
            _context.LifecycleController.CompleteRequest();
            _context.RequestWindowClose();
        }
        finally
        {
            _context.LifecycleController.EndRecordingStop();
        }
    }

    private void LogWindowClosingTrigger()
    {
        try
        {
            var snapshot = _context.LifecycleController.Snapshot;
            Logger.Log(
                "WINDOW_CLOSING_TRIGGER " +
                $"requested={snapshot.Requested} " +
                $"isRecording={_context.IsRecording()} " +
                $"stack=\n{new System.Diagnostics.StackTrace(true)}");
        }
        catch (Exception logEx)
        {
            System.Diagnostics.Trace.TraceWarning($"WINDOW_CLOSING_TRIGGER log failed: {logEx.Message}");
        }
    }
}
