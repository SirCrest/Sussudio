using Microsoft.UI.Xaml;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio;

// Window close lifecycle and automation close completion. Recording finalization
// protection lives here because close is the last chance to avoid truncating an
// in-progress recording.
public sealed partial class MainWindow
{
    private int _windowCloseRequested;
    private int _windowCloseCleanupStarted;
    private int _windowCloseRecordingStopInProgress;
    private int _windowCloseAllowedAfterRecordingStop;
    private readonly object _windowCloseCompletionLock = new();
    private TaskCompletionSource<object?>? _windowCloseCompletion;
    private bool _isWindowClosing;

    private void RegisterCloseLifecycle(Microsoft.UI.Windowing.AppWindow appWindow)
        => appWindow.Closing += MainWindow_Closing;

    private async void MainWindow_Closing(
        Microsoft.UI.Windowing.AppWindow sender,
        Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        try
        {
            Logger.Log(
                "WINDOW_CLOSING_TRIGGER " +
                $"requested={Volatile.Read(ref _windowCloseRequested)} " +
                $"isRecording={ViewModel.IsRecording} " +
                $"stack=\n{new System.Diagnostics.StackTrace(true)}");
        }
        catch (Exception logEx)
        {
            System.Diagnostics.Trace.TraceWarning($"WINDOW_CLOSING_TRIGGER log failed: {logEx.Message}");
        }

        if (Volatile.Read(ref _windowCloseCleanupStarted) != 0 ||
            Volatile.Read(ref _windowCloseAllowedAfterRecordingStop) != 0)
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
        Interlocked.Exchange(ref _windowCloseRequested, 0);

        if (Interlocked.Exchange(ref _windowCloseRecordingStopInProgress, 1) != 0)
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

            Interlocked.Exchange(ref _windowCloseAllowedAfterRecordingStop, 1);
            Interlocked.Exchange(ref _windowCloseRequested, 0);
            CompleteWindowCloseRequest();
            RequestWindowClose();
        }
        finally
        {
            Interlocked.Exchange(ref _windowCloseRecordingStopInProgress, 0);
        }
    }

    private async Task<bool> TryStopRecordingBeforeCloseAsync()
    {
        const int StopBudgetMs = 120_000;
        Logger.Log("WINDOW_CLOSE_RECORDING_STOP: recording active, awaiting graceful stop...");
        ViewModel.StatusText = "Stopping recording - please wait...";

        FrameworkElement? shutdownContent = null;
        if (this.Content is FrameworkElement content)
        {
            shutdownContent = content;
            shutdownContent.IsHitTestVisible = false;
            shutdownContent.Opacity = 0.5;
        }

        try
        {
            var stopTask = ViewModel.StopRecordingAndWaitAsync();
            var completed = await Task.WhenAny(stopTask, Task.Delay(StopBudgetMs));
            if (completed == stopTask)
            {
                await stopTask;
                Logger.Log("WINDOW_CLOSE_RECORDING_STOP: recording stopped cleanly.");
                return true;
            }

            Logger.LogFatalBreadcrumb("RECORDING_FINALIZE_TIMEOUT "
                + $"budget_ms={StopBudgetMs}; close cancelled to protect recording.");
            ViewModel.StatusText = "Still saving recording. Close cancelled.";
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            Logger.Log($"WINDOW_CLOSE_RECORDING_STOP: stop failed: {ex.Message}");
            ViewModel.StatusText = $"Close cancelled: recording stop failed ({ex.Message})";
            return false;
        }
        finally
        {
            if (shutdownContent != null &&
                Volatile.Read(ref _windowCloseAllowedAfterRecordingStop) == 0)
            {
                shutdownContent.IsHitTestVisible = true;
                shutdownContent.Opacity = 1;
            }
        }
    }

    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _windowCloseCleanupStarted) != 0)
        {
            return Task.CompletedTask;
        }

        var closeCompletionTask = GetWindowCloseCompletionTask(cancellationToken);

        if (_dispatcherQueue.HasThreadAccess)
        {
            RequestWindowClose();
            return closeCompletionTask;
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
                    completion.TrySetCanceled(cancellationToken);
                    return;
                }

                RequestWindowClose();
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
            if (Volatile.Read(ref _windowCloseCleanupStarted) != 0)
            {
                completion.TrySetResult(null);
            }
            else
            {
                var enqueueFailure = new InvalidOperationException("Failed to enqueue window close action on the UI thread.");
                CompleteWindowCloseRequest(enqueueFailure);
                completion.TrySetException(enqueueFailure);
            }
        }

        return AwaitWindowCloseRequestAsync(completion.Task, closeCompletionTask);
    }

    private Task GetWindowCloseCompletionTask(CancellationToken cancellationToken)
    {
        TaskCompletionSource<object?> completion;
        lock (_windowCloseCompletionLock)
        {
            if (_windowCloseCompletion == null || _windowCloseCompletion.Task.IsCompleted)
            {
                _windowCloseCompletion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            completion = _windowCloseCompletion;
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

    private void CompleteWindowCloseRequest(Exception? exception = null)
    {
        TaskCompletionSource<object?>? completion;
        lock (_windowCloseCompletionLock)
        {
            completion = _windowCloseCompletion;
            _windowCloseCompletion = null;
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

    private void RequestWindowClose()
    {
        if (Volatile.Read(ref _windowCloseCleanupStarted) != 0)
        {
            CompleteWindowCloseRequest();
            return;
        }

        if (Interlocked.Exchange(ref _windowCloseRequested, 1) != 0)
        {
            return;
        }

        try
        {
            Close();
            if (Volatile.Read(ref _windowCloseRecordingStopInProgress) == 0 &&
                !ViewModel.IsRecording &&
                !ViewModel.IsRecordingTransitioning)
            {
                CompleteWindowCloseRequest();
            }
        }
        catch (Exception ex) when (IsCloseAlreadyInProgressException(ex))
        {
            Logger.Log($"Window close already in progress ({ex.GetType().Name}); treating close request as successful.");
            CompleteWindowCloseRequest();
        }
        catch (System.Runtime.InteropServices.COMException ex)
        {
            Logger.Log($"Window.Close COMException (0x{ex.HResult:X8}); using Application.Current.Exit() fallback.");
            CompleteWindowCloseRequest();
            Application.Current.Exit();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Suppressed exception in MainWindow.RequestWindowClose: {ex.Message}");
            Interlocked.Exchange(ref _windowCloseRequested, 0);
            CompleteWindowCloseRequest(ex);
            throw;
        }
    }
    private static bool IsCloseAlreadyInProgressException(Exception ex)
    {
        if (ex is InvalidOperationException && string.IsNullOrWhiteSpace(ex.Message))
        {
            return true;
        }

        var message = ex.Message ?? string.Empty;
        return message.IndexOf("closing", StringComparison.OrdinalIgnoreCase) >= 0 ||
               message.IndexOf("closed", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
