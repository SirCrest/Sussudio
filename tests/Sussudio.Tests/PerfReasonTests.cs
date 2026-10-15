using Sussudio.Services.Performance;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of the fixed reason tables (docs/architecture/performance-telemetry.md sections 2.2 and 5): the
// renderer and jitter buffer pass open-ended strings, so counters must stay bounded (unknown, null and empty collapse
// into "other"), the "{x}:stale" form must map to "stale", every entry needs a registered wire-named counter, and a
// bad code must clamp instead of throwing. Trace args must also survive the ring and the dump.
public sealed class PerfReasonTests
{
    [Theory]
    [InlineData("renderer-stopped", PreviewDropReason.RendererStopped)]
    [InlineData("renderer-backlog", PreviewDropReason.RendererBacklog)]
    [InlineData("explicit-drain", PreviewDropReason.ExplicitDrain)]
    [InlineData("live-preview-suppressed", PreviewDropReason.LivePreviewSuppressed)]
    [InlineData("live-preview-resumed", PreviewDropReason.LivePreviewResumed)]
    [InlineData("shared-device-reset", PreviewDropReason.SharedDeviceReset)]
    [InlineData("stale-generation", PreviewDropReason.StaleGeneration)]
    [InlineData("stale", PreviewDropReason.Stale)]
    [InlineData("render-skipped", PreviewDropReason.RenderSkipped)]
    [InlineData("render-failed", PreviewDropReason.RenderFailed)]
    [InlineData("stale-before-render", PreviewDropReason.StaleBeforeRender)]
    [InlineData("renderer-exit", PreviewDropReason.RendererExit)]
    [InlineData("renderer-stop", PreviewDropReason.RendererStop)]
    [InlineData("device-lost", PreviewDropReason.DeviceLost)]
    [InlineData("live-preview-suppressed:stale", PreviewDropReason.Stale)]
    [InlineData("anything:stale", PreviewDropReason.Stale)]
    [InlineData("something-new", PreviewDropReason.Other)]
    [InlineData("stale:not-a-suffix", PreviewDropReason.Other)]
    [InlineData("", PreviewDropReason.Other)]
    [InlineData(null, PreviewDropReason.Other)]
    public void PreviewDropReasonStringsMapIntoTheFixedTable(string? reason, PreviewDropReason expected)
    {
        Assert.Equal(expected, PreviewDropReasons.Classify(reason));
    }

    [Fact]
    public void EveryPreviewDropEntryHasARegisteredWireNamedCounterAndUnknownReasonsShareOther()
    {
        foreach (var reason in Enum.GetValues<PreviewDropReason>().Where(r => r != PreviewDropReason.None))
        {
            var name = PreviewDropReasons.Name(reason);
            Assert.Contains(PerfMetrics.Registry.Catalog, info => info.Name == "preview.drop." + name);
        }

        Assert.Equal("none", PreviewDropReasons.Name(PreviewDropReason.None));

        var before = PerfMetrics.PreviewDrop("never-seen-1").Read();
        Assert.Same(PerfMetrics.PreviewDrop("never-seen-1"), PerfMetrics.PreviewDrop("never-seen-2"));
        PerfMetrics.PreviewDrop("never-seen-3").Increment();
        Assert.Equal(before + 1, PerfMetrics.PreviewDrop("never-seen-4").Read());
        Assert.Same(PerfMetrics.PreviewDrop("other"), PerfMetrics.PreviewDrop("never-seen-1"));
    }

    [Fact]
    public void ReasonCounterCodesOutOfRangeClampToTheLastEntry()
    {
        var last = PerfMetrics.PreviewDrops.Counter(PerfMetrics.PreviewDrops.Count);
        Assert.Same(last, PerfMetrics.PreviewDrops.Counter(0));
        Assert.Same(last, PerfMetrics.PreviewDrops.Counter(-3));
        Assert.Same(last, PerfMetrics.PreviewDrops.Counter(10_000));
    }

    [Theory]
    [InlineData("queue-full", JitterDropReason.QueueFull)]
    [InlineData("late-sequence", JitterDropReason.LateSequence)]
    [InlineData("missing-sequence", JitterDropReason.MissingSequence)]
    [InlineData("submit-failed", JitterDropReason.SubmitFailed)]
    [InlineData("hard-deadline", JitterDropReason.HardDeadline)]
    [InlineData("soft-deadline", JitterDropReason.SoftDeadline)]
    [InlineData("cleared", JitterDropReason.Cleared)]
    [InlineData("suppressed", JitterDropReason.Suppressed)]
    [InlineData("whatever", JitterDropReason.Other)]
    [InlineData(null, JitterDropReason.Other)]
    public void JitterDropReasonStringsMapIntoTheFixedTable(string? reason, JitterDropReason expected)
    {
        Assert.Equal(expected, JitterReasons.ClassifyDrop(reason));
        Assert.Contains(PerfMetrics.Registry.Catalog, info => info.Name == "preview.jitter_drop." + JitterReasons.Name(expected));
    }

    [Theory]
    [InlineData("empty-queue", JitterUnderflowReason.EmptyQueue)]
    [InlineData("waiting-for-sequence", JitterUnderflowReason.WaitingForSequence)]
    [InlineData("selection-blocked", JitterUnderflowReason.SelectionBlocked)]
    [InlineData("resume-reprime", JitterUnderflowReason.ResumeReprime)]
    [InlineData("whatever", JitterUnderflowReason.Other)]
    [InlineData(null, JitterUnderflowReason.Other)]
    public void JitterUnderflowReasonStringsMapIntoTheFixedTable(string? reason, JitterUnderflowReason expected)
    {
        Assert.Equal(expected, JitterReasons.ClassifyUnderflow(reason));
        Assert.Contains(PerfMetrics.Registry.Catalog, info => info.Name == "preview.jitter_underflow." + JitterReasons.Name(expected));
    }

    [Fact]
    public void TraceArgSurvivesTheRingAndNeverBleedsIntoTheKind()
    {
        var buffer = new FrameTraceBuffer(1 << 12);
        buffer.RecordInstant(FrameTraceEventKind.PreviewDrop, 100, 42, (ushort)PreviewDropReason.DeviceLost);
        buffer.Record(FrameTraceEventKind.MjpegDecode, 200, 50, 43, 3);
        buffer.Record(FrameTraceEventKind.HubRefresh, 300, 10, -1);
        buffer.RecordInstant(FrameTraceEventKind.CaptureSampleTime, 400, 44, ushort.MaxValue);

        var events = buffer.Copy();
        Assert.Equal(4, events.Count);
        Assert.Equal((FrameTraceEventKind.PreviewDrop, (int)PreviewDropReason.DeviceLost), (events[0].Kind, events[0].Arg));
        Assert.True(events[0].IsInstant);
        Assert.Equal((FrameTraceEventKind.MjpegDecode, 3), (events[1].Kind, events[1].Arg));
        Assert.Equal((FrameTraceEventKind.HubRefresh, 0), (events[2].Kind, events[2].Arg));
        Assert.Equal((FrameTraceEventKind.CaptureSampleTime, (int)ushort.MaxValue), (events[3].Kind, events[3].Arg));
        Assert.Equal(40, FrameTraceBuffer.EventSizeBytes);
    }

    [Fact]
    public void DumpOmitsAZeroArgAndNamesAPreviewDropReason()
    {
        var recorder = new FrameTraceRecorder(new PerfMarkerLog(), new PerfThreadNames());
        Assert.True(recorder.Start(1 << 12).Success);
        recorder.RecordInstant(FrameTraceEventKind.PreviewDrop, 1000, 7, (ushort)PreviewDropReason.RendererBacklog);
        recorder.Record(FrameTraceEventKind.CaptureRead, 900, 20, 7);
        recorder.Stop();

        using var stream = new MemoryStream();
        Assert.True(recorder.TryWriteChromeTrace(stream, out var error), error);
        using var document = System.Text.Json.JsonDocument.Parse(stream.ToArray());
        var drop = document.RootElement.GetProperty("traceEvents").EnumerateArray()
            .Single(e => e.GetProperty("name").GetString() == "preview.drop");
        var dropArgs = drop.GetProperty("args");
        Assert.Equal((int)PreviewDropReason.RendererBacklog, dropArgs.GetProperty("arg").GetInt32());
        Assert.Equal("renderer-backlog", dropArgs.GetProperty("reason").GetString());

        var read = document.RootElement.GetProperty("traceEvents").EnumerateArray()
            .Single(e => e.GetProperty("name").GetString() == "capture.read");
        Assert.False(read.GetProperty("args").TryGetProperty("arg", out _));
    }
}
