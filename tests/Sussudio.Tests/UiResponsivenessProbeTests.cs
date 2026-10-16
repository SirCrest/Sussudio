using System.Diagnostics;
using Sussudio.Services.Performance;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of the UI responsiveness probe (docs/architecture/performance-telemetry.md sections 2.8 and 5):
// a stalled dispatcher must never accumulate a backlog (one probe outstanding), an enqueue that fails or throws must
// not wedge the probe or kill the app, the stall counter and the latency histogram must agree on the thresholds,
// Stop must be prompt and harmless when repeated or when never started, and a restart must work. The dispatcher is
// a fake delegate; the static PerfMetrics are read as deltas because they are process-wide.
public sealed class UiResponsivenessProbeTests
{
    private sealed class FakeClock
    {
        public long Now;

        public long Read() => Interlocked.Read(ref Now);

        public static long Ms(double milliseconds) => (long)(milliseconds * Stopwatch.Frequency / 1000d);
    }

    private sealed class FakeDispatcher
    {
        public List<Action> Queue { get; } = new();

        public bool Accepts { get; set; } = true;

        public bool Throws { get; set; }

        public int EnqueueCalls { get; private set; }

        public bool Enqueue(Action action)
        {
            EnqueueCalls++;
            if (Throws)
            {
                throw new InvalidOperationException("dispatcher shutting down");
            }

            if (!Accepts)
            {
                return false;
            }

            Queue.Add(action);
            return true;
        }

        public void RunNext()
        {
            var action = Queue[0];
            Queue.RemoveAt(0);
            action();
        }
    }

    private readonly record struct MetricMark(long Samples, long Stalls)
    {
        public static MetricMark Now() =>
            new(PerfMetrics.UiDispatcherLatency.Snapshot().Count, PerfMetrics.UiStall50Ms.Read());
    }

    [Fact]
    public void AtMostOneProbeIsOutstandingSoAStalledDispatcherGetsNoBacklog()
    {
        var clock = new FakeClock();
        var dispatcher = new FakeDispatcher();
        var probe = new UiResponsivenessProbe(dispatcher.Enqueue, clock.Read);

        probe.Tick();
        probe.Tick();
        probe.Tick();

        Assert.Single(dispatcher.Queue);
        Assert.Equal(1, dispatcher.EnqueueCalls);
        var stats = probe.Stats;
        Assert.Equal(1, stats.Enqueued);
        Assert.Equal(2, stats.SkippedWhileBusy);
        Assert.Equal(0, stats.Completed);

        // Once the UI thread catches up, probing resumes.
        dispatcher.RunNext();
        probe.Tick();
        Assert.Single(dispatcher.Queue);
        Assert.Equal(2, probe.Stats.Enqueued);
    }

    [Fact]
    public void ARejectedEnqueueIsCountedAndTheNextTickTriesAgain()
    {
        var dispatcher = new FakeDispatcher { Accepts = false };
        var probe = new UiResponsivenessProbe(dispatcher.Enqueue, new FakeClock().Read);

        probe.Tick();
        probe.Tick();
        Assert.Equal(2, dispatcher.EnqueueCalls);
        Assert.Equal(2, probe.Stats.EnqueueFailures);
        Assert.Equal(0, probe.Stats.SkippedWhileBusy);

        dispatcher.Accepts = true;
        probe.Tick();
        Assert.Single(dispatcher.Queue);
        Assert.Equal(1, probe.Stats.Enqueued);
    }

    [Fact]
    public void AnEnqueueThatThrowsIsCountedAndDoesNotEscapeOrWedgeTheProbe()
    {
        var dispatcher = new FakeDispatcher { Throws = true };
        var probe = new UiResponsivenessProbe(dispatcher.Enqueue, new FakeClock().Read);

        probe.Tick();
        probe.Tick();
        Assert.Equal(2, probe.Stats.EnqueueFailures);

        dispatcher.Throws = false;
        probe.Tick();
        Assert.Equal(1, probe.Stats.Enqueued);
    }

    [Fact]
    public void WaitsAreRecordedAndOnlyWaitsOverFiftyMillisecondsCountAsStalls()
    {
        var clock = new FakeClock();
        var dispatcher = new FakeDispatcher();
        var probe = new UiResponsivenessProbe(dispatcher.Enqueue, clock.Read);
        var before = MetricMark.Now();

        foreach (var waitMs in new[] { 1d, 16d, 49d })
        {
            probe.Tick();
            clock.Now += FakeClock.Ms(waitMs);
            dispatcher.RunNext();
        }

        var quiet = MetricMark.Now();
        Assert.Equal(3, quiet.Samples - before.Samples);
        Assert.Equal(0, quiet.Stalls - before.Stalls);

        foreach (var waitMs in new[] { 51d, 400d })
        {
            probe.Tick();
            clock.Now += FakeClock.Ms(waitMs);
            dispatcher.RunNext();
        }

        var stalled = MetricMark.Now();
        Assert.Equal(5, stalled.Samples - before.Samples);
        Assert.Equal(2, stalled.Stalls - before.Stalls);
        Assert.Equal(5, probe.Stats.Completed);
    }

    [Fact]
    public void AClockThatMovesBackwardsRecordsAZeroWaitInsteadOfThrowing()
    {
        var clock = new FakeClock { Now = FakeClock.Ms(1000) };
        var dispatcher = new FakeDispatcher();
        var probe = new UiResponsivenessProbe(dispatcher.Enqueue, clock.Read);
        var before = MetricMark.Now();

        probe.Tick();
        clock.Now -= FakeClock.Ms(10);
        dispatcher.RunNext();

        var after = MetricMark.Now();
        Assert.Equal(1, after.Samples - before.Samples);
        Assert.Equal(0, after.Stalls - before.Stalls);
    }

    [Fact]
    public void StopIsHarmlessWhenNeverStartedAndWhenRepeated()
    {
        var probe = new UiResponsivenessProbe(new FakeDispatcher().Enqueue, new FakeClock().Read);
        probe.Stop();
        probe.Stop();
        Assert.False(probe.IsRunning);
    }

    [Fact]
    public void StartIsIdempotentStopIsPromptAndTheProbeRestarts()
    {
        // The fake dispatcher runs the probe at once on the probe thread, so completed counts rise without a UI thread.
        var probe = new UiResponsivenessProbe(
            action =>
            {
                action();
                return true;
            },
            periodMs: 5);

        Assert.True(probe.Start());
        Assert.False(probe.Start());
        Assert.True(probe.IsRunning);
        SpinWait.SpinUntil(() => probe.Stats.Completed >= 3, TimeSpan.FromSeconds(10));
        Assert.True(probe.Stats.Completed >= 3, "the probe thread never ran the dispatcher callback");

        var stopwatch = Stopwatch.StartNew();
        probe.Stop();
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), "Stop was not prompt");
        Assert.False(probe.IsRunning);

        var frozen = probe.Stats.Enqueued;
        Thread.Sleep(60);
        Assert.Equal(frozen, probe.Stats.Enqueued);

        Assert.True(probe.Start());
        var resumedFrom = probe.Stats.Completed;
        SpinWait.SpinUntil(() => probe.Stats.Completed > resumedFrom, TimeSpan.FromSeconds(10));
        Assert.True(probe.Stats.Completed > resumedFrom, "the probe did not resume after a restart");
        probe.Stop();
    }

    [Fact]
    public void DescribeStatesWhetherTheProbeRuns()
    {
        var probe = new UiResponsivenessProbe(
            action =>
            {
                action();
                return true;
            },
            periodMs: 5);

        Assert.Equal("ui_probe: stopped", probe.Describe());
        probe.Start();
        Assert.StartsWith("ui_probe: running", probe.Describe());
        probe.Stop();
        Assert.Equal("ui_probe: stopped", probe.Describe());
    }
}
