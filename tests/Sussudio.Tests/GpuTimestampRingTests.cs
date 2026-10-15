using System.Diagnostics;
using Sussudio.Services.Performance;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of the GPU timestamp ring (docs/architecture/performance-telemetry.md sections 2.2.1 and 5): every
// query slot still pending (the frame is skipped, never waited for), disjoint or implausible results, a result that
// never arrives, a backend that fails or throws at any step (device removed), a span left open by an early return,
// a second span opened over the first, results arriving out of step with the frames, ring sizing, and spans of exactly
// zero GPU ticks (counted as unmeasured per render path, never recorded as a zero-length render). The D3D11
// queries are a fake backend; the static PerfMetrics are read as deltas. The frame-trace span is not asserted here:
// the trace recorder is a process-wide singleton other tests assert is off.
public sealed class GpuTimestampRingTests
{
    private sealed class FakeBackend : IGpuTimestampBackend
    {
        public FakeBackend(int slots = 6) => SlotCount = slots;

        public int SlotCount { get; }

        public bool BeginSucceeds { get; set; } = true;

        public bool ThrowOnBegin { get; set; }

        public bool ThrowOnEnd { get; set; }

        public bool ThrowOnPoll { get; set; }

        public int BeginCalls { get; private set; }

        public int EndCalls { get; private set; }

        public int PollCalls { get; private set; }

        public Dictionary<int, (GpuTimestampPoll Poll, long Ticks, long Frequency, bool Disjoint)> Results { get; } = new();

        public bool Begin(int slot)
        {
            BeginCalls++;
            if (ThrowOnBegin)
            {
                throw new InvalidOperationException("device removed");
            }

            return BeginSucceeds;
        }

        public void End(int slot)
        {
            EndCalls++;
            if (ThrowOnEnd)
            {
                throw new InvalidOperationException("device removed");
            }
        }

        public GpuTimestampPoll Poll(int slot, out long gpuTicks, out long gpuFrequency, out bool disjoint)
        {
            PollCalls++;
            if (ThrowOnPoll)
            {
                throw new InvalidOperationException("device removed");
            }

            if (Results.TryGetValue(slot, out var result))
            {
                Results.Remove(slot);
                gpuTicks = result.Ticks;
                gpuFrequency = result.Frequency;
                disjoint = result.Disjoint;
                return result.Poll;
            }

            gpuTicks = 0;
            gpuFrequency = 0;
            disjoint = false;
            return GpuTimestampPoll.NotReady;
        }

        public void Ready(int slot, long ticks, long frequency = 1_000_000, bool disjoint = false) =>
            Results[slot] = (GpuTimestampPoll.Ready, ticks, frequency, disjoint);
    }

    private readonly record struct Mark(long Recorded, double SumMicroseconds, long Skipped, long Unmeasured)
    {
        public static Mark Now()
        {
            var snapshot = PerfMetrics.PreviewGpuRender.Snapshot();
            return new Mark(
                snapshot.Count,
                snapshot.Sum,
                PerfMetrics.PreviewGpuTimingSkipped.Read(),
                PerfMetrics.PreviewGpuTimingUnmeasured.Read());
        }
    }

    private static void Frame(GpuTimestampRing ring, long id, long timestamp = 0)
    {
        var slot = ring.Begin(id, timestamp);
        Assert.True(slot >= 0, "expected a free slot");
        ring.End(slot);
    }

    [Fact]
    public void AFinishedSlotIsRecordedInMicrosecondsOnALaterFrame()
    {
        var backend = new FakeBackend();
        var ring = new GpuTimestampRing(backend, () => 0);
        var before = Mark.Now();

        Frame(ring, id: 1);
        backend.Ready(slot: 0, ticks: 500, frequency: 1_000_000);
        Frame(ring, id: 2);

        var after = Mark.Now();
        Assert.Equal(1, after.Recorded - before.Recorded);
        Assert.Equal(500, after.SumMicroseconds - before.SumMicroseconds);
        Assert.Equal(1, ring.Recorded);
        Assert.False(ring.IsFailed);
    }

    [Fact]
    public void ResultsAreRecordedOldestFirstAndTheRingWrapsAroundManyFrames()
    {
        var backend = new FakeBackend(slots: 4);
        var ring = new GpuTimestampRing(backend, () => 0);
        var before = Mark.Now();

        for (var frame = 0; frame < 40; frame++)
        {
            Frame(ring, id: frame);
            backend.Ready(slot: frame % 4, ticks: 100 + frame);
        }

        ring.Drain();
        var after = Mark.Now();
        Assert.Equal(40, after.Recorded - before.Recorded);
        Assert.Equal(0, after.Skipped - before.Skipped);
        Assert.Equal(40 * 100 + (39 * 40 / 2), after.SumMicroseconds - before.SumMicroseconds);
    }

    [Fact]
    public void WhenEverySlotIsPendingTheFrameIsSkippedWithoutWaitingOrOpeningAQuery()
    {
        var backend = new FakeBackend(slots: 4);
        var ring = new GpuTimestampRing(backend, () => 0);
        for (var i = 0; i < 4; i++)
        {
            Frame(ring, id: i);
        }

        var before = Mark.Now();
        var beginCalls = backend.BeginCalls;

        // GPU is behind: nothing is ready. The renderer must get -1 back immediately and never touch the backend's Begin.
        Assert.Equal(-1, ring.Begin(frameId: 99, submitTimestamp: 0));
        Assert.Equal(-1, ring.Begin(frameId: 100, submitTimestamp: 0));
        Assert.Equal(beginCalls, backend.BeginCalls);
        Assert.Equal(2, Mark.Now().Skipped - before.Skipped);

        backend.Ready(slot: 0, ticks: 10);
        Assert.True(ring.Begin(frameId: 101, submitTimestamp: 0) >= 0);
    }

    [Fact]
    public void ADisjointResultAndImplausibleValuesAreSkippedNotRecorded()
    {
        var backend = new FakeBackend(slots: 8);
        var ring = new GpuTimestampRing(backend, () => 0);
        var before = Mark.Now();

        Frame(ring, 1);
        backend.Ready(0, ticks: 500, disjoint: true);
        Frame(ring, 2);
        backend.Ready(1, ticks: 500, frequency: 0);
        Frame(ring, 3);
        backend.Ready(2, ticks: -7);
        Frame(ring, 4);
        backend.Ready(3, ticks: long.MaxValue / 4, frequency: 1_000_000);
        Frame(ring, 5);
        backend.Ready(4, ticks: 250);
        ring.Drain();

        var after = Mark.Now();
        Assert.Equal(1, after.Recorded - before.Recorded);
        Assert.Equal(250, after.SumMicroseconds - before.SumMicroseconds);
        Assert.Equal(4, after.Skipped - before.Skipped);
        Assert.False(ring.IsFailed);
    }

    [Fact]
    public void AZeroTickSpanIsCountedAsUnmeasuredAndNeverRecordedAsAZeroLengthRender()
    {
        var backend = new FakeBackend(slots: 8);
        var ring = new GpuTimestampRing(backend, () => 0);
        var before = Mark.Now();

        Frame(ring, 1);
        backend.Ready(0, ticks: 0);
        Frame(ring, 2);
        backend.Ready(1, ticks: 400);
        Frame(ring, 3);
        backend.Ready(2, ticks: 0);
        Frame(ring, 4);
        ring.Drain();

        var after = Mark.Now();
        Assert.Equal(2, after.Unmeasured - before.Unmeasured);
        Assert.Equal(1, after.Recorded - before.Recorded);
        Assert.Equal(400, after.SumMicroseconds - before.SumMicroseconds);
        Assert.Equal(0, after.Skipped - before.Skipped);
        Assert.Equal(2, ring.Unmeasured);
        Assert.Equal(1, ring.Recorded);
        Assert.False(ring.IsFailed);
    }

    [Fact]
    public void ARealSubMicrosecondSpanIsRecordedAsOneMicrosecondSoItIsNeverMistakenForUnmeasured()
    {
        var backend = new FakeBackend();
        var ring = new GpuTimestampRing(backend, () => 0);
        var before = Mark.Now();

        // 300 ticks at 1 GHz is 0.3 us of real work.
        Frame(ring, 1);
        backend.Ready(0, ticks: 300, frequency: 1_000_000_000);
        ring.Drain();

        var after = Mark.Now();
        Assert.Equal(1, after.Recorded - before.Recorded);
        Assert.Equal(1, after.SumMicroseconds - before.SumMicroseconds);
        Assert.Equal(0, after.Unmeasured - before.Unmeasured);
        Assert.Equal(1, ring.SubMicrosecond);
    }

    [Fact]
    public void ADisjointZeroTickSpanIsSkippedNotCountedAsUnmeasured()
    {
        var backend = new FakeBackend();
        var ring = new GpuTimestampRing(backend, () => 0);
        var before = Mark.Now();

        Frame(ring, 1);
        backend.Ready(0, ticks: 0, disjoint: true);
        ring.Drain();

        var after = Mark.Now();
        Assert.Equal(1, after.Skipped - before.Skipped);
        Assert.Equal(0, after.Unmeasured - before.Unmeasured);
        Assert.Equal(0, ring.Unmeasured);
    }

    [Fact]
    public void RecordedAndUnmeasuredSpansAreTalliedPerRenderPath()
    {
        var backend = new FakeBackend(slots: 8);
        var ring = new GpuTimestampRing(backend, () => 0);

        void Timed(GpuTimingPath path, long ticks, int slot)
        {
            var opened = ring.Begin(frameId: slot, submitTimestamp: 0, path);
            Assert.Equal(slot, opened);
            ring.End(opened);
            backend.Ready(slot, ticks);
        }

        Timed(GpuTimingPath.VideoProcessor, ticks: 0, slot: 0);
        Timed(GpuTimingPath.VideoProcessor, ticks: 0, slot: 1);
        Timed(GpuTimingPath.VideoProcessor, ticks: 90, slot: 2);
        Timed(GpuTimingPath.Nv12Shader, ticks: 250, slot: 3);
        Timed(GpuTimingPath.HdrShader, ticks: 0, slot: 4);
        ring.Drain();

        Assert.Equal(1, ring.RecordedFor(GpuTimingPath.VideoProcessor));
        Assert.Equal(2, ring.UnmeasuredFor(GpuTimingPath.VideoProcessor));
        Assert.Equal(1, ring.RecordedFor(GpuTimingPath.Nv12Shader));
        Assert.Equal(0, ring.UnmeasuredFor(GpuTimingPath.Nv12Shader));
        Assert.Equal(0, ring.RecordedFor(GpuTimingPath.HdrShader));
        Assert.Equal(1, ring.UnmeasuredFor(GpuTimingPath.HdrShader));
        Assert.Equal(2, ring.Recorded);
        Assert.Equal(3, ring.Unmeasured);
    }

    [Fact]
    public void AResultThatNeverArrivesIsAbandonedAfterAboutASecondSoTheRingKeepsCycling()
    {
        var now = 0L;
        var backend = new FakeBackend(slots: 4);
        var ring = new GpuTimestampRing(backend, () => now);
        for (var i = 0; i < 4; i++)
        {
            Frame(ring, id: i, timestamp: now);
        }

        var before = Mark.Now();

        // Just under a second: still pending, ring still full, no skip is charged for abandoning.
        now = (long)(0.9 * Stopwatch.Frequency);
        Assert.Equal(-1, ring.Begin(10, now));
        var afterFull = Mark.Now();
        Assert.Equal(1, afterFull.Skipped - before.Skipped);

        // Past a second: the lost slots are retired and the frame gets a slot.
        now = (long)((GpuTimestampRing.StaleAfterSeconds + 0.5) * Stopwatch.Frequency);
        var slot = ring.Begin(11, now);
        Assert.True(slot >= 0);
        Assert.Equal(4, Mark.Now().Skipped - afterFull.Skipped);
        Assert.False(ring.IsFailed);
    }

    [Fact]
    public void ABackendThatRefusesToBeginFailsTheRingAndItIsNeverCalledAgain()
    {
        var backend = new FakeBackend { BeginSucceeds = false };
        var ring = new GpuTimestampRing(backend, () => 0);

        Assert.Equal(-1, ring.Begin(1, 0));
        Assert.True(ring.IsFailed);
        Assert.Equal("Begin failed", ring.FailureReason);

        Assert.Equal(-1, ring.Begin(2, 0));
        Assert.Equal(1, backend.BeginCalls);
    }

    [Theory]
    [InlineData("begin")]
    [InlineData("end")]
    [InlineData("poll")]
    public void ABackendThatThrowsAtAnyStepFailsTheRingInsteadOfEscapingToTheRenderThread(string step)
    {
        var backend = new FakeBackend { ThrowOnBegin = step == "begin" };
        var ring = new GpuTimestampRing(backend, () => 0);

        if (step == "begin")
        {
            Assert.Equal(-1, ring.Begin(1, 0));
        }
        else
        {
            var slot = ring.Begin(1, 0);
            Assert.True(slot >= 0);
            backend.ThrowOnEnd = step == "end";
            backend.ThrowOnPoll = step == "poll";
            ring.End(slot);
            ring.Drain();
        }

        Assert.True(ring.IsFailed);
        Assert.Contains("threw", ring.FailureReason);

        // A failed ring is inert: no further backend calls, no exception, no open span.
        var calls = backend.BeginCalls + backend.EndCalls + backend.PollCalls;
        Assert.Equal(-1, ring.Begin(2, 0));
        ring.End(0);
        ring.Drain();
        Assert.Equal(calls, backend.BeginCalls + backend.EndCalls + backend.PollCalls);
    }

    [Fact]
    public void AFailedPollReportingTheDeviceGoneDisablesTheRingAndDropsPendingFrames()
    {
        var backend = new FakeBackend();
        var ring = new GpuTimestampRing(backend, () => 0);
        Frame(ring, 1);
        Frame(ring, 2);
        backend.Results[0] = (GpuTimestampPoll.Failed, 0, 0, false);
        var before = Mark.Now();

        ring.Drain();

        Assert.True(ring.IsFailed);
        Assert.Equal("Poll failed", ring.FailureReason);
        Assert.Equal(before.Recorded, Mark.Now().Recorded);
        Assert.Equal(-1, ring.Begin(3, 0));
    }

    [Fact]
    public void ClosingASlotThatIsNotTheOpenOneOrNoSlotAtAllDoesNothing()
    {
        var backend = new FakeBackend();
        var ring = new GpuTimestampRing(backend, () => 0);

        ring.End(-1);
        ring.End(3);
        Assert.Equal(0, backend.EndCalls);

        var slot = ring.Begin(1, 0);
        ring.End(slot + 1);
        Assert.Equal(0, backend.EndCalls);

        ring.End(slot);
        ring.End(slot);
        Assert.Equal(1, backend.EndCalls);
    }

    [Fact]
    public void ASecondSpanCannotBeOpenedOverAnOpenOneSoNoQueryIsLeftDangling()
    {
        var backend = new FakeBackend();
        var ring = new GpuTimestampRing(backend, () => 0);

        var first = ring.Begin(1, 0);
        Assert.True(first >= 0);
        Assert.Equal(-1, ring.Begin(2, 0));
        Assert.Equal(1, backend.BeginCalls);

        // Closing the first span (the renderer does it in a finally) frees the ring for the next frame.
        ring.End(first);
        Assert.True(ring.Begin(3, 0) >= 0);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(6, 6)]
    [InlineData(8, 8)]
    [InlineData(100, GpuTimestampRing.MaxSlots)]
    public void TheRingNeverUsesMoreSlotsThanItsCapOrFewerThanOne(int backendSlots, int expected)
    {
        var ring = new GpuTimestampRing(new FakeBackend(backendSlots), () => 0);
        Assert.Equal(expected, ring.SlotCount);
    }
}
