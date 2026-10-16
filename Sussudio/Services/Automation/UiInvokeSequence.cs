using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.Services.Automation;

/// <summary>
/// The order of one InvokeUiElement: perform the action on the UI thread, let the UI settle, then read the element back
/// on the UI thread. The WinUI work is the bridge's (UiAutomationBridge); this owns the sequence.
///
/// The request's cancellation (its timeout, app shutdown) stops an action only before the UI thread starts it. Once it
/// has started it finishes, and a cancellation reported for it would tell the caller it did not run: a caller that
/// retries runs it twice and a toggle flips back. So the steps after it ignore the cancellation and the result says
/// what was done. The pipe server sends that result even after the request timed out.
/// </summary>
internal static class UiInvokeSequence
{
    private const int Pending = 0;
    private const int Started = 1;
    private const int Abandoned = 2;

    /// <param name="onUiThread">Runs an action on the UI thread (the window's UI dispatch).</param>
    /// <param name="perform">The action, on the UI thread.</param>
    /// <param name="notPerformed">The final result when the action did nothing (not found, rejected), else null.</param>
    /// <param name="readBack">The result after the action, on the UI thread.</param>
    /// <param name="settle">The wait for bindings, animations and flyouts to settle.</param>
    public static async Task<TResult> RunAsync<TAttempt, TResult>(
        Func<Action, CancellationToken, Task> onUiThread,
        Func<TAttempt> perform,
        Func<TAttempt, TResult?> notPerformed,
        Func<TAttempt, TResult> readBack,
        Func<CancellationToken, Task> settle,
        CancellationToken cancellationToken)
        where TAttempt : class
        where TResult : class
    {
        TAttempt? attempt = null;
        var stage = Pending;
        var performed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            await onUiThread(
                () =>
                {
                    // A request abandoned before this ran must not act late.
                    if (Interlocked.CompareExchange(ref stage, Started, Pending) != Pending)
                    {
                        return;
                    }

                    try
                    {
                        attempt = perform();
                    }
                    finally
                    {
                        performed.TrySetResult();
                    }
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // The UI dispatch cancels its task as soon as the token fires, even while the action is running. Not started
            // yet: it never will. Started: wait for it to finish and report it.
            if (Interlocked.CompareExchange(ref stage, Abandoned, Pending) == Pending)
            {
                throw;
            }

            await performed.Task.ConfigureAwait(false);
            if (attempt is null)
            {
                // The action threw on the UI thread; nothing was done to report.
                throw;
            }
        }

        if (notPerformed(attempt!) is { } early)
        {
            return early;
        }

        await settle(CancellationToken.None).ConfigureAwait(false);
        TResult? result = null;
        await onUiThread(() => result = readBack(attempt!), CancellationToken.None).ConfigureAwait(false);
        return result!;
    }
}
