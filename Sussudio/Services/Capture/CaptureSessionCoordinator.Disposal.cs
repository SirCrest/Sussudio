using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

public sealed partial class CaptureSessionCoordinator
{
    private const int DefaultDisposeDrainTimeoutMs = 15_000;
    private const int DefaultDisposeCancelTimeoutMs = 1_000;

    // REVIEWED 2026-04-07: IDisposable fallback only — MainViewModel.DisposeAsync
    // calls DisposeAsync directly. This sync path is never hit in production.
    public void Dispose()
    {
        if (!TryBeginDispose()) return;
        Task.Run(() => CoreDisposeAsync().AsTask()).GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (!TryBeginDispose()) return;
        await CoreDisposeAsync().ConfigureAwait(false);
    }

    private bool TryBeginDispose()
    {
        lock (_disposeLock)
        {
            if (Volatile.Read(ref _isDisposed)) return false;
            Volatile.Write(ref _isDisposed, true);
        }
        return true;
    }

    private async ValueTask CoreDisposeAsync()
    {
        _queue.Writer.TryComplete();
        var drainTimeoutMs = EnvironmentHelpers.GetIntFromEnv(
            "SUSSUDIO_COORDINATOR_DISPOSE_TIMEOUT_MS",
            DefaultDisposeDrainTimeoutMs,
            1000,
            300000);

        try
        {
            await _workerTask.WaitAsync(TimeSpan.FromMilliseconds(drainTimeoutMs)).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            Logger.Log($"CaptureSessionCoordinator dispose drain timed out after {drainTimeoutMs} ms; canceling worker.");
            CancelWorkerBestEffort();
            await WaitForWorkerCancellationAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            /* Expected during disposal — worker task was cancelled */
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
        }
        finally
        {
            DisposeWorkerCancellationWhenSafe();
        }
    }

    private async Task WaitForWorkerCancellationAsync()
    {
        var cancelTimeoutMs = EnvironmentHelpers.GetIntFromEnv(
            "SUSSUDIO_COORDINATOR_DISPOSE_CANCEL_TIMEOUT_MS",
            DefaultDisposeCancelTimeoutMs,
            100,
            300000);

        try
        {
            await _workerTask.WaitAsync(TimeSpan.FromMilliseconds(cancelTimeoutMs)).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            Logger.Log($"CaptureSessionCoordinator worker cancellation timed out after {cancelTimeoutMs} ms.");
        }
        catch (OperationCanceledException)
        {
            /* Expected during disposal - worker task was cancelled */
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
        }
    }

    private void DisposeWorkerCancellationWhenSafe()
    {
        if (_workerTask.IsCompleted)
        {
            DisposeWorkerCancellationBestEffort("worker_completed");
            return;
        }

        _ = _workerTask.ContinueWith(
            static (_, state) =>
            {
                var cancellation = (CancellationTokenSource)state!;
                try
                {
                    cancellation.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Log($"CAPTURE_COORD_WORKER_CTS_DISPOSE_WARN op=worker_continuation type={ex.GetType().Name} msg='{ex.Message}'");
                }
            },
            _workerCancellation,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private static void DisposeCancellationRegistrationBestEffort(
        CancellationTokenRegistration registration,
        string operation)
    {
        try
        {
            registration.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"CAPTURE_COORD_CANCEL_REG_DISPOSE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void CancelWorkerBestEffort()
    {
        try
        {
            _workerCancellation.Cancel();
        }
        catch (Exception ex)
        {
            Logger.Log($"CAPTURE_COORD_WORKER_CANCEL_WARN type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void DisposeWorkerCancellationBestEffort(string operation)
    {
        try
        {
            _workerCancellation.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"CAPTURE_COORD_WORKER_CTS_DISPOSE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }
}
