using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    public void Start()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AutomationDiagnosticsHub));
        }

        if (_loopTask != null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _loopTask = Task.Run(() => RunLoopAsync(_cts.Token));
        AddEvent(DiagnosticsSeverity.Info, DiagnosticsCategory.System, "Diagnostics hub started.");
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_loopTask == null)
        {
            return;
        }

        _cts?.Cancel();
        var loopTask = _loopTask;
        _loopTask = null;
        var autoVerificationTask = _autoVerificationTask;
        _autoVerificationTask = null;
        Interlocked.Exchange(ref _autoVerificationScheduled, 0);

        try
        {
            await Task.WhenAny(loopTask, Task.Delay(5000, cancellationToken)).ConfigureAwait(false);
            if (autoVerificationTask != null)
            {
                await Task.WhenAny(autoVerificationTask, Task.Delay(5000, cancellationToken)).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            /* Expected during shutdown — stop/dispose requested while awaiting loop tasks */
        }

        _cts?.Dispose();
        _cts = null;
        AddEvent(DiagnosticsSeverity.Info, DiagnosticsCategory.System, "Diagnostics hub stopped.");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Never block the UI thread on a Task that may itself need the UI thread
        // to make progress (StopAsync awaits items that may have dispatched back).
        // Task.Run breaks the ambient SynchronizationContext so StopAsync can
        // complete without re-entering a captured UI dispatcher.  The budget is
        // 12 s: StopAsync has two consecutive 5 s Task.WhenAny waits internally
        // (loopTask + autoVerificationTask), so 12 s covers both with margin.
        // Callers that need deterministic teardown should call DisposeAsync.
        var stoppedCleanly = false;
        try
        {
            var stop = Task.Run(() => StopAsync());
            if (stop.Wait(TimeSpan.FromSeconds(12)))
            {
                stoppedCleanly = true;
            }
            else
            {
                Logger.Log("DIAGHUB_DISPOSE_TIMEOUT msg='StopAsync did not complete within 12 s; abandoning'");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"DIAGHUB_DISPOSE_FAULT type={ex.GetType().Name} msg='{ex.Message}'");
        }

        if (stoppedCleanly)
        {
            _currentProcess.Dispose();
        }
        else
        {
            // StopAsync did not complete within the budget; the abandoned RunLoopAsync
            // may still call _currentProcess.Refresh() / WorkingSet64. Disposing the
            // handle here would race with those reads and produce ObjectDisposedException
            // churn on the loop thread. Skip the dispose; the kernel reclaims the
            // process handle when the host process exits (Dispose is only invoked from
            // teardown paths, so the leak is bounded).
            Logger.Log("DIAGHUB_DISPOSE_SKIPPED_PROCESS_HANDLE reason=stop_timeout");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await StopAsync().ConfigureAwait(false);
        _currentProcess.Dispose();
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RefreshSnapshotAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                /* Expected during shutdown — exit the refresh loop */
                break;
            }
            catch (Exception ex)
            {
                AddEvent(DiagnosticsSeverity.Error, DiagnosticsCategory.System, $"Diagnostics refresh failed: {ex.Message}");
            }

            try
            {
                await Task.Delay(PollIntervalMs, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                /* Expected during shutdown — exit the refresh loop */
                break;
            }
        }
    }
}
