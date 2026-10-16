using Sussudio.Services.Automation;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of one InvokeUiElement's sequence (UiInvokeSequence: perform on the UI thread, settle, read back),
// written before the request's cancellation was kept away from an action that had run. The fake UI thread behaves like
// WindowUiDispatchController.InvokeAsync: a canceled token cancels the returned task at once, even while the action is
// running on the UI thread, and a queued action whose token is canceled by the time it runs is skipped.
//
//  1. The request is canceled (timeout, shutdown) while the action runs on the UI thread. The action finishes, but the
//     caller saw a cancellation; a caller that retries runs it twice and a toggle flips back. The action's result
//     must be reported.
//  2. The request is canceled during the settle delay or the read-back after the action ran: the same. Both steps run
//     to completion and the result is reported.
//  3. The request is canceled before the UI thread runs the action: the action never runs, and the caller sees the
//     cancellation, never a result for an action that did not happen.
//  4. The action did nothing (not found, rejected): its result comes back without a settle or a read-back.
//  5. The action throws: the exception reaches the caller.
public sealed class UiInvokeSequenceTests
{
    private sealed class FakeUiThread
    {
        private readonly Queue<Action> _queued = new();

        public bool Queue { get; init; }

        /// <summary>False: the cancel lands between the dispatch's own token check and the action.</summary>
        public bool ChecksTokenBeforeRun { get; init; } = true;

        public Task Invoke(Action action, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return Task.FromCanceled(token);
            }

            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var registration = token.Register(() => completion.TrySetCanceled(token));
            void Item()
            {
                try
                {
                    if (ChecksTokenBeforeRun && token.IsCancellationRequested)
                    {
                        completion.TrySetCanceled(token);
                        return;
                    }

                    action();
                    completion.TrySetResult();
                }
                catch (Exception ex)
                {
                    completion.TrySetException(ex);
                }
                finally
                {
                    registration.Dispose();
                }
            }

            if (Queue)
            {
                _queued.Enqueue(Item);
            }
            else
            {
                Item();
            }

            return completion.Task;
        }

        public void RunQueued()
        {
            while (_queued.TryDequeue(out var item))
            {
                item();
            }
        }
    }

    private sealed class Steps
    {
        public int Performed;
        public int Settled;
        public int ReadBack;
        public Action? DuringPerform;
        public Action? DuringSettle;
        public Action? DuringReadBack;
        public string? NotPerformedResult;

        public string Perform()
        {
            DuringPerform?.Invoke();
            Performed++;
            return "attempt";
        }

        public Task Settle(CancellationToken token)
        {
            DuringSettle?.Invoke();
            Settled++;
            return token.IsCancellationRequested ? Task.FromCanceled(token) : Task.CompletedTask;
        }

        public string Read(string attempt)
        {
            DuringReadBack?.Invoke();
            ReadBack++;
            return "invoked after " + attempt;
        }
    }

    private static Task<string> Run(FakeUiThread ui, Steps steps, CancellationToken token)
        => UiInvokeSequence.RunAsync<string, string>(
            ui.Invoke,
            steps.Perform,
            _ => steps.NotPerformedResult,
            steps.Read,
            steps.Settle,
            token);

    [Theory]
    [InlineData("perform")]
    [InlineData("settle")]
    [InlineData("read-back")]
    public async Task ACancelAfterTheActionStartedStillReportsTheAction(string when)
    {
        using var request = new CancellationTokenSource();
        var steps = new Steps();
        switch (when)
        {
            case "perform":
                steps.DuringPerform = request.Cancel;
                break;
            case "settle":
                steps.DuringSettle = request.Cancel;
                break;
            default:
                steps.DuringReadBack = request.Cancel;
                break;
        }

        var result = await Run(new FakeUiThread(), steps, request.Token);

        Assert.Equal("invoked after attempt", result);
        Assert.Equal(1, steps.Performed);
        Assert.Equal(1, steps.Settled);
        Assert.Equal(1, steps.ReadBack);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ACancelBeforeTheUiThreadRanTheActionMeansItNeverRuns(bool dispatchChecksToken)
    {
        using var request = new CancellationTokenSource();
        var ui = new FakeUiThread { Queue = true, ChecksTokenBeforeRun = dispatchChecksToken };
        var steps = new Steps();

        var run = Run(ui, steps, request.Token);
        request.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        ui.RunQueued();
        Assert.Equal(0, steps.Performed);
        Assert.Equal(0, steps.ReadBack);
    }

    [Fact]
    public async Task AnActionThatDidNothingReturnsItsResultWithoutSettlingOrReadingBack()
    {
        var steps = new Steps { NotPerformedResult = "not-found" };

        var result = await Run(new FakeUiThread(), steps, CancellationToken.None);

        Assert.Equal("not-found", result);
        Assert.Equal(0, steps.Settled);
        Assert.Equal(0, steps.ReadBack);
    }

    [Fact]
    public async Task AnActionThatThrowsReachesTheCaller()
    {
        var steps = new Steps { DuringPerform = () => throw new InvalidOperationException("peer refused") };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Run(new FakeUiThread(), steps, CancellationToken.None));

        Assert.Equal("peer refused", error.Message);
        Assert.Equal(0, steps.ReadBack);
    }
}
