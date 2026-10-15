using System.Globalization;
using System.Text;
using System.Text.Json;
using Sussudio.Tools.Performance;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of the frame-trace analyzer (docs/architecture/performance-telemetry.md sections 2.4 and 7).
// Written before the analyzer, per the testing policy. Every case below is a way the analyzer could lie or crash:
//
//  1. Input that is not a trace: empty, whitespace, not JSON, JSON with the wrong root, no traceEvents array.
//     Must be a FrameTraceFormatException with a message, never another exception type and never a half result.
//  2. A truncated dump (a crash or a copy in progress): cut anywhere, including just before the closing brace.
//     Same contract as 1; the metadata block comes last, so a missing tail must not look like a valid file.
//  3. A valid trace with no events: zero frames, no NaN or divide-by-zero, JSON and text still render.
//  4. Stage names from a newer app: tolerated, listed, never fatal.
//  5. Frames that are missing stages (dropped by the renderer, or cut by ring wraparound): counted and reported
//     per stage, never silently removed from the frame total.
//  6. id = -1 events and ambient stages (GC, UI stall, automation) must never become frames, even if an
//     ambient event carries a positive id (the GC pause records a collection count today).
//  7. A single frame: no division by zero in percentiles or cadence, no hitch from one sample.
//  8. Events with unusable fields (ts as a string, missing name): skipped and counted, the rest still analyzed.
//  9. A dump without the metadata block, or ids that are sequence numbers rather than QPC ticks: the arrival
//     time falls back to the frame's earliest event and the report says so, instead of printing latencies that
//     are billions of milliseconds.
// 10. Arrival math at realistic QPC magnitudes (1e14 ticks): done in integer ticks, not by subtracting doubles.
// 11. Duplicate events for one frame and stage: durations are summed, the frame is not duplicated.
// 12. Hitch attribution: the overlapping GC pause, marker, the stage that grew most, and a late arrival must each
//     be named when they are the cause; aggregate stages (pipeline, render_total) must not win the "grew most".
// 13. A token larger than the reader's first buffer (a long marker): the chunked reader must grow, not fail.
// 14. Instants that are drops: counted and tied to their frame; the frame is still reported.
// Added with the slice 2a dump (args.reason on preview.drop, args.arg on any event, capture.sample_time):
// 15. A drop with args.reason: counted per reason, and the slowest-frame entry names the reason. A drop from an older
//     dump has no reason: still counted, no reasons listed, no invented "unknown" bucket.
// 16. A reason that is not a string (a number, an object) or is blank: ignored without skipping the event.
// 17. An open-ended reason vocabulary: past 32 distinct reasons per drop name the extras fold into "other", so the
//     report and its JSON stay bounded however hostile the dump is. Long reasons are truncated.
// 18. args.arg on an event (written when non-zero): tolerated and never mistaken for the frame id.
// 19. telemetry.hub_refresh carries id -1: ambient, never a frame, even many of them.
// 20. A dump with frames but no capture.sample_time (an older app): ingress latency is absent and the report says
//     why, instead of printing nothing.
// 21. The app's own telemetry refresh (telemetry.hub_refresh) overlapping a hitch is named as a cause when it is long
//     enough to matter, and a sub-millisecond one is not blamed.
public sealed class FrameTraceAnalyzerTests
{
    private const long Frequency = 10_000_000;
    private const long Origin = 100_000_000_000_000; // 1e14 ticks, about 115 days of QPC at 10 MHz

    private static long Ticks(double ms) => Origin + (long)Math.Round(ms * Frequency / 1000d);

    // ExtraArgs is a raw JSON fragment appended to args after id (for example "\"reason\":\"stale\"").
    private sealed record Ev(string Name, string Phase, double TsUs, double DurUs, int Tid, long? Id, string? Text = null, string? ExtraArgs = null);

    // Builds the same Chrome Trace Event JSON FrameTraceRecorder writes, in integer ticks, so the tests state
    // times in milliseconds and the analyzer has to recover them from `id` and `ts`.
    private sealed class TraceBuilder
    {
        private readonly List<Ev> _events = new();
        public bool IncludeMetadata { get; set; } = true;
        public long Overwritten { get; set; }

        public void Span(string name, double startMs, double durMs, long? id, int tid = 1)
            => _events.Add(new Ev(name, "X", startMs * 1000d, durMs * 1000d, tid, id));

        public void Instant(string name, double atMs, long? id, int tid = 1, string? extraArgs = null)
            => _events.Add(new Ev(name, "i", atMs * 1000d, 0, tid, id, null, extraArgs));

        public void Marker(double atMs, string text)
            => _events.Add(new Ev("marker", "i", atMs * 1000d, 0, 0, null, text));

        public void Raw(string json) => _rawEvents.Add(json);

        private readonly List<string> _rawEvents = new();

        // A frame arriving at `arrivalMs`: read -> deliver -> queue -> upload -> submit -> present, plus the
        // aggregate pipeline span. Every stage can be overridden to model a slow stage or a missing one.
        public long Frame(
            double arrivalMs,
            double queueMs = 1.0,
            double submitMs = 0.5,
            bool present = true,
            double presentDelayMs = 0,
            double? sampleOffsetMs = null,
            double readMs = 2.0)
        {
            var id = Ticks(arrivalMs);
            Span("capture.read", arrivalMs - readMs, readMs, id);
            Span("capture.deliver", arrivalMs, 0.3, id);
            Span("preview.queue_dwell", arrivalMs + 0.5, queueMs, id);
            var cursor = arrivalMs + 0.5 + queueMs;
            Span("preview.input_upload", cursor, 0.4, id);
            cursor += 0.4;
            Span("preview.render_submit", cursor, submitMs, id);
            cursor += submitMs + presentDelayMs;
            if (present)
            {
                Span("preview.present", cursor, 0.1, id);
                Span("preview.pipeline", arrivalMs, cursor + 0.1 - arrivalMs, id);
            }

            if (sampleOffsetMs is { } offset)
            {
                Instant("capture.sample_time", arrivalMs - offset, id);
            }

            return id;
        }

        public string Build()
        {
            var json = new StringBuilder();
            json.Append("{\"displayTimeUnit\":\"ms\",\"traceEvents\":[");
            json.Append("{\"name\":\"process_name\",\"ph\":\"M\",\"pid\":1,\"tid\":0,\"args\":{\"name\":\"Sussudio\"}}");
            json.Append(",{\"name\":\"thread_name\",\"ph\":\"M\",\"pid\":1,\"tid\":1,\"args\":{\"name\":\"preview.render\"}}");
            foreach (var e in _events)
            {
                json.Append(",{\"name\":").Append(JsonSerializer.Serialize(e.Name));
                json.Append(",\"cat\":\"x\",\"ph\":\"").Append(e.Phase).Append('"');
                if (e.Phase == "i")
                {
                    json.Append(",\"s\":\"t\"");
                }

                json.Append(",\"ts\":").Append(e.TsUs.ToString("R", CultureInfo.InvariantCulture));
                if (e.Phase == "X")
                {
                    json.Append(",\"dur\":").Append(e.DurUs.ToString("R", CultureInfo.InvariantCulture));
                }

                json.Append(",\"pid\":1,\"tid\":").Append(e.Tid.ToString(CultureInfo.InvariantCulture));
                json.Append(",\"args\":{");
                if (e.Text is not null)
                {
                    json.Append("\"text\":").Append(JsonSerializer.Serialize(e.Text));
                }
                else
                {
                    json.Append("\"id\":").Append((e.Id ?? -1).ToString(CultureInfo.InvariantCulture));
                    if (e.ExtraArgs is not null)
                    {
                        json.Append(',').Append(e.ExtraArgs);
                    }
                }

                json.Append("}}");
            }

            foreach (var raw in _rawEvents)
            {
                json.Append(',').Append(raw);
            }

            json.Append(']');
            if (IncludeMetadata)
            {
                json.Append(",\"sussudio\":{\"qpcFrequency\":").Append(Frequency.ToString(CultureInfo.InvariantCulture));
                json.Append(",\"originQpc\":").Append(Origin.ToString(CultureInfo.InvariantCulture));
                json.Append(",\"startedUtc\":\"2026-10-14T20:00:00.0000000Z\",\"capacity\":262144");
                json.Append(",\"recorded\":").Append(_events.Count.ToString(CultureInfo.InvariantCulture));
                json.Append(",\"retained\":").Append(_events.Count.ToString(CultureInfo.InvariantCulture));
                json.Append(",\"overwritten\":").Append(Overwritten.ToString(CultureInfo.InvariantCulture));
                json.Append(",\"running\":false}");
            }

            json.Append('}');
            return json.ToString();
        }
    }

    private static TraceBuilder Steady(int frames, double intervalMs = 1000d / 60d)
    {
        var trace = new TraceBuilder();
        for (var i = 0; i < frames; i++)
        {
            trace.Frame(100 + i * intervalMs);
        }

        return trace;
    }

    private static TraceStageStats Stage(FrameTraceAnalysis analysis, string name)
        => Assert.Single(analysis.Stages, stage => stage.Stage == name);

    [Fact]
    public void ASteadyTraceReportsStageStatsLatenciesAndNoHitches()
    {
        var analysis = FrameTraceAnalyzer.AnalyzeJson(Steady(120).Build());

        Assert.Equal(120, analysis.Frames.Total);
        Assert.Equal(0, analysis.Frames.MissingStages);
        Assert.Equal(0, analysis.Trace.SkippedEvents);

        var queue = Stage(analysis, "preview.queue_dwell");
        Assert.Equal(120, queue.Frames);
        Assert.Equal(1.0, queue.P50Ms, 3);
        Assert.Equal(1.0, queue.MaxMs, 3);

        // The queue stage ends 0.5 + 1.0 ms after arrival.
        Assert.Equal(1.5, queue.EndLatencyP50Ms, 3);

        var endToEnd = Assert.Single(analysis.Derived, d => d.Name == "arrival_to_present");
        Assert.Equal(120, endToEnd.Count);
        // read(2.0 before) is irrelevant; present starts at 0.5+1.0+0.4+0.5 = 2.4 and lasts 0.1.
        Assert.Equal(2.5, endToEnd.P50Ms, 3);

        Assert.NotNull(analysis.PresentCadence);
        Assert.Equal("preview.present", analysis.PresentCadence!.Stage);
        Assert.Equal(119, analysis.PresentCadence.Intervals);
        Assert.Equal(1000d / 60d, analysis.PresentCadence.MedianMs, 2);
        Assert.Equal(0, analysis.PresentCadence.HitchCount);
    }

    [Fact]
    public void ArrivalLatencyIsRecoveredInIntegerTicksAtRealisticQpcMagnitudes()
    {
        var trace = new TraceBuilder();
        var id = Ticks(1000);
        trace.Span("capture.read", 998, 2, id);
        trace.Span("preview.present", 1007.4, 0.1, id); // ends 7.5 ms after arrival
        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());

        var endToEnd = Assert.Single(analysis.Derived, d => d.Name == "arrival_to_present");
        Assert.Equal(7.5, endToEnd.P50Ms, 3);
        Assert.Equal(7.5, Stage(analysis, "preview.present").EndLatencyP50Ms, 3);
    }

    [Fact]
    public void AGcPauseAndMarkerOverlappingAPresentHitchAreNamed()
    {
        var trace = new TraceBuilder();
        for (var i = 0; i < 100; i++)
        {
            // Frame 50 is presented 14 ms late (still ahead of frame 51) while a GC pause covers it.
            trace.Frame(100 + i * 16.667, presentDelayMs: i == 50 ? 14 : 0);
        }

        trace.Span("gc.pause", 100 + 50 * 16.667 + 5, 25, null, tid: 7);
        trace.Marker(100 + 50 * 16.667 + 10, "variant B run 2 measure");
        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());

        var cadence = analysis.PresentCadence!;
        Assert.True(cadence.HitchCount >= 1);
        var hitch = cadence.Hitches[0];
        Assert.True(hitch.IntervalMs > 28);
        Assert.Contains(hitch.Attributions, a => a.Kind == "event" && a.Name == "gc.pause");
        Assert.Contains(hitch.Attributions, a => a.Kind == "event" && a.Name == "marker" && a.Detail.Contains("variant B run 2"));

        var gc = Assert.Single(analysis.AmbientEvents, e => e.Name == "gc.pause");
        Assert.Equal(1, gc.Count);
        Assert.Equal(25, gc.MaxMs, 3);

        Assert.Contains("gc.pause", analysis.FormatSummary());
    }

    [Fact]
    public void AHubRefreshOverlappingAHitchIsNamedButAVeryShortOneIsNot()
    {
        var trace = new TraceBuilder();
        for (var i = 0; i < 100; i++)
        {
            trace.Frame(100 + i * 16.667, presentDelayMs: i == 50 ? 14 : 0);
        }

        // The app's own telemetry refresh: a long one inside the hitch is a cause, a sub-millisecond one is noise.
        trace.Span("telemetry.hub_refresh", 100 + 50 * 16.667 + 2, 9, -1, tid: 8);
        trace.Span("telemetry.hub_refresh", 100 + 50 * 16.667 + 12, 0.4, -1, tid: 8);
        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());

        var hitch = analysis.PresentCadence!.Hitches[0];
        var named = Assert.Single(hitch.Attributions, a => a.Name == "telemetry.hub_refresh");
        Assert.True(named.Ms >= 8, "overlap " + named.Ms.ToString(CultureInfo.InvariantCulture));
        Assert.Equal(2, Assert.Single(analysis.AmbientEvents, e => e.Name == "telemetry.hub_refresh").Count);
    }

    [Fact]
    public void TheStageThatGrewMostIsNamedAndAggregateStagesNeverWin()
    {
        var trace = new TraceBuilder();
        for (var i = 0; i < 100; i++)
        {
            // Frame 70 spends 12 ms in render_submit (median 0.5 ms). preview.pipeline grows by the same amount
            // because it spans the whole path, but it is an aggregate and must not be the named cause.
            trace.Frame(100 + i * 16.667, submitMs: i == 70 ? 12 : 0.5);
        }

        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());
        var hitch = analysis.PresentCadence!.Hitches[0];

        var stage = Assert.Single(hitch.Attributions, a => a.Kind == "stage");
        Assert.Equal("preview.render_submit", stage.Name);
        Assert.Equal(11.5, stage.Ms, 1);
        Assert.Equal("stage", hitch.Attributions.OrderByDescending(a => a.Ms).First().Kind);
        Assert.Contains("preview.render_submit", hitch.LikelyCause);
        Assert.DoesNotContain(hitch.Attributions, a => a.Name == "preview.pipeline");

        var slow = analysis.SlowestFrames[0];
        Assert.Equal("preview.render_submit", slow.DominantStage);
    }

    [Fact]
    public void ALateArrivalIsAttributedToTheSourceNotToAStage()
    {
        var trace = new TraceBuilder();
        for (var i = 0; i < 100; i++)
        {
            // From frame 40 on, everything arrives 45 ms late once: one long gap between arrivals.
            var arrival = 100 + i * 16.667 + (i >= 40 ? 45 : 0);
            trace.Frame(arrival);
        }

        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());
        var hitch = analysis.PresentCadence!.Hitches[0];

        Assert.Equal("arrival", hitch.Attributions.OrderByDescending(a => a.Ms).First().Kind);
        Assert.NotNull(hitch.ArrivalIntervalMs);
        Assert.True(hitch.ArrivalIntervalMs > 55);
        Assert.Contains("arrival", hitch.LikelyCause);
    }

    // Seen live: a 27 ms present interval whose source frame arrived 26.5 ms after the previous one was blamed on a
    // 44 ms automation command that ran on another thread at the same time. Overlap is not excess: the renderer cannot
    // present a frame that has not arrived, whatever else ran meanwhile.
    [Theory]
    [InlineData("automation.command")]
    [InlineData("ui.dispatcher_latency")]
    [InlineData("telemetry.hub_refresh")]
    public void ALateArrivalThatExplainsTheHitchOutranksAnEventThatMerelyOverlapsIt(string eventName)
    {
        var trace = LateArrivalAtFrame40(lateByMs: 12);
        trace.Span(eventName, 100 + 39 * 16.667 - 5, 40, -1, tid: 9);
        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());

        var hitch = analysis.PresentCadence!.Hitches[0];
        Assert.Contains("late arrival", hitch.LikelyCause);
        var overlapping = Assert.Single(hitch.Attributions, a => a.Name == eventName);
        Assert.True(overlapping.Ms > hitch.Attributions.Single(a => a.Kind == "arrival").Ms, "the event must overlap more than the arrival excess for this case to mean anything");
    }

    // A GC pause stops every managed thread, the capture loop included, so it can be why the frame arrived late.
    [Fact]
    public void AGcPauseOverlappingALateArrivalIsStillNamed()
    {
        var trace = LateArrivalAtFrame40(lateByMs: 12);
        trace.Span("gc.pause", 100 + 39 * 16.667 + 2, 20, -1, tid: 9);
        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());

        Assert.Contains("gc.pause", analysis.PresentCadence!.Hitches[0].LikelyCause);
    }

    // The arrival gap clears the attribution threshold but covers well under the interval's excess (the frame also
    // waited 14 ms after submit), so the event that covers the whole interval stays the named cause.
    [Fact]
    public void AnArrivalGapThatExplainsLittleOfTheHitchDoesNotOutrankTheEvent()
    {
        var trace = new TraceBuilder();
        for (var i = 0; i < 100; i++)
        {
            trace.Frame(100 + i * 16.667 + (i >= 40 ? 10 : 0), presentDelayMs: i == 40 ? 14 : 0);
        }

        trace.Span("automation.command", 100 + 39 * 16.667 - 5, 60, -1, tid: 9);
        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());

        var hitch = analysis.PresentCadence!.Hitches[0];
        Assert.Contains(hitch.Attributions, a => a.Kind == "arrival");
        Assert.Contains("automation.command", hitch.LikelyCause);
    }

    // With nothing on the frame's own path to blame, an overlapping event is named exactly as before.
    [Fact]
    public void AnOverlappingEventIsNamedWhenNoArrivalGapOrStageExplainsTheHitch()
    {
        var trace = new TraceBuilder();
        for (var i = 0; i < 100; i++)
        {
            trace.Frame(100 + i * 16.667, presentDelayMs: i == 50 ? 14 : 0);
        }

        trace.Span("automation.command", 100 + 50 * 16.667, 30, -1, tid: 9);
        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());

        Assert.Contains("automation.command", analysis.PresentCadence!.Hitches[0].LikelyCause);
    }

    private static TraceBuilder LateArrivalAtFrame40(double lateByMs)
    {
        var trace = new TraceBuilder();
        for (var i = 0; i < 100; i++)
        {
            trace.Frame(100 + i * 16.667 + (i >= 40 ? lateByMs : 0));
        }

        return trace;
    }

    [Fact]
    public void FramesMissingStagesAreCountedAndNotDropped()
    {
        var trace = new TraceBuilder();
        for (var i = 0; i < 60; i++)
        {
            var arrival = 100 + i * 16.667;
            var id = trace.Frame(arrival, present: i is < 10 or > 14);
            if (i is 10 or 11)
            {
                trace.Instant("preview.drop", arrival + 3, id);
            }
        }

        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());

        Assert.Equal(60, analysis.Frames.Total);
        Assert.Equal(5, analysis.Frames.MissingStages);
        Assert.Equal(55, analysis.Frames.Complete);
        Assert.Equal(2, analysis.Frames.WithDrop);
        Assert.Equal(5, Stage(analysis, "preview.present").MissingFrames);
        Assert.Equal(0, Stage(analysis, "capture.read").MissingFrames);
        Assert.Equal(2, Assert.Single(analysis.Drops, d => d.Name == "preview.drop").Count);
    }

    // Failure modes of the slowest-frames dominant stage. The total is measured from arrival, and capture.read (the wait
    // for the device, ~one frame period at steady state) ends at arrival, so it must not win by being long: it is
    // listed as pre-arrival instead. A stage that straddles arrival counts only the part after it, and a trace without
    // QPC metadata places arrival at the earliest span, which makes the read part of the interval again.
    [Fact]
    public void AStageThatEndedAtArrivalIsListedAsPreArrivalAndNeverNamedDominant()
    {
        var trace = new TraceBuilder();
        for (var i = 0; i < 60; i++)
        {
            // A 16.7 ms read before every frame; frame 30 then spends 4 ms in its queue.
            trace.Frame(100 + i * 16.667, queueMs: i == 30 ? 4 : 1.0, readMs: 16.7);
        }

        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build(), new FrameTraceAnalysisOptions { TopFrames = 3 });

        var worst = analysis.SlowestFrames[0];
        Assert.Equal("arrival-to-present", worst.RankedBy);
        Assert.Equal("preview.queue_dwell", worst.DominantStage);
        Assert.Equal(new[] { "capture.read" }, worst.PreArrivalStages);
        Assert.True(worst.TotalMs < 16.7, "the 16.7 ms read is not part of arrival-to-present: " + worst.TotalMs.ToString(CultureInfo.InvariantCulture));
        Assert.Contains(worst.Stages, stage => stage.Stage == "capture.read" && stage.StartMs < 0);

        var text = analysis.FormatSummary();
        Assert.Contains("dominant preview.queue_dwell", text);
        Assert.Contains("before arrival (not in the total): capture.read 16.7", text);
    }

    [Fact]
    public void AStageStraddlingArrivalCountsOnlyItsPartAfterArrival()
    {
        var trace = new TraceBuilder();
        for (var i = 0; i < 40; i++)
        {
            var arrival = 100 + i * 16.667;
            var id = trace.Frame(arrival, queueMs: 2.0);
            // 4 ms long, but only 1 ms of it after arrival: the 2 ms queue wait is what dominates the frame.
            trace.Span("capture.straddle", arrival - 3, 4, id);
        }

        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build(), new FrameTraceAnalysisOptions { TopFrames = 1 });

        var frame = analysis.SlowestFrames[0];
        Assert.Equal("preview.queue_dwell", frame.DominantStage);
        Assert.DoesNotContain("capture.straddle", frame.PreArrivalStages!);
    }

    // With the GPU lanes carrying the frame id, the encoders' spans join the frame. They run on their own thread, so a
    // long encode that finishes after the present cannot be what delayed the present and must not be named dominant.
    [Fact]
    public void AnEncoderStageThatEndedAfterThePresentIsListedAfterPresentAndNeverNamedDominant()
    {
        var trace = new TraceBuilder();
        for (var i = 0; i < 60; i++)
        {
            var arrival = 100 + i * 16.667;
            var id = trace.Frame(arrival, queueMs: i == 30 ? 4 : 1.0);
            // An 8 ms encode that starts with the frame and ends after its present (a normal frame is presented ~2.5 ms in;
            // the slow one, with its 4 ms queue wait, ~5.5 ms in).
            trace.Span("flashback.encode_submit", arrival + 0.4, 8.0, id, tid: 2);
        }

        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build(), new FrameTraceAnalysisOptions { TopFrames = 3 });

        var worst = analysis.SlowestFrames[0];
        Assert.Equal("arrival-to-present", worst.RankedBy);
        Assert.Equal("preview.queue_dwell", worst.DominantStage);
        Assert.Equal(new[] { "flashback.encode_submit" }, worst.AfterPresentStages);
        Assert.Contains(worst.Stages, stage => stage.Stage == "flashback.encode_submit");

        var text = analysis.FormatSummary();
        Assert.Contains("after present (not in the total): flashback.encode_submit 8.0", text);
    }

    [Fact]
    public void AnEncoderStageThatFinishedBeforeThePresentStillCountsTowardTheDominantStage()
    {
        var trace = new TraceBuilder();
        for (var i = 0; i < 40; i++)
        {
            var arrival = 100 + i * 16.667;
            // The present is delayed 6 ms; a 4 ms encode that finishes inside that wait is on the frame's path.
            var id = trace.Frame(arrival, presentDelayMs: i == 20 ? 6 : 0);
            trace.Span("flashback.encode_submit", arrival + 0.4, i == 20 ? 4.0 : 0.2, id, tid: 2);
        }

        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build(), new FrameTraceAnalysisOptions { TopFrames = 1 });

        var worst = analysis.SlowestFrames[0];
        Assert.Equal("flashback.encode_submit", worst.DominantStage);
        Assert.Empty(worst.AfterPresentStages!);
    }

    [Fact]
    public void WithoutQpcMetadataTheReadIsInsideTheIntervalAndCanBeDominant()
    {
        var trace = new TraceBuilder { IncludeMetadata = false };
        for (var i = 0; i < 30; i++)
        {
            trace.Frame(100 + i * 16.667, readMs: 16.7);
        }

        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build(), new FrameTraceAnalysisOptions { TopFrames = 1 });

        var frame = analysis.SlowestFrames[0];
        Assert.Equal("earliest-event", analysis.Trace.ArrivalSource);
        Assert.Equal("capture.read", frame.DominantStage);
        Assert.Empty(frame.PreArrivalStages!);
    }

    [Fact]
    public void ADroppedFrameStaysInTheSlowestListWithItsMissingStages()
    {
        var trace = new TraceBuilder();
        for (var i = 0; i < 40; i++)
        {
            var arrival = 100 + i * 16.667;
            var id = trace.Frame(arrival, present: i != 20, queueMs: i == 20 ? 200 : 1.0);
            if (i == 20)
            {
                trace.Instant("preview.drop", arrival + 201, id);
            }
        }

        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build(), new FrameTraceAnalysisOptions { TopFrames = 3 });

        var worst = analysis.SlowestFrames[0];
        Assert.Equal("lifetime", worst.RankedBy);
        Assert.True(worst.Dropped);
        Assert.Contains("preview.present", worst.MissingStages);
        Assert.Equal("preview.queue_dwell", worst.DominantStage);
    }

    private static string ReasonArg(string reason) => "\"reason\":" + JsonSerializer.Serialize(reason);

    [Fact]
    public void DropReasonsAreCountedPerReasonAndNamedOnTheSlowFrame()
    {
        var trace = new TraceBuilder();
        for (var i = 0; i < 40; i++)
        {
            var arrival = 100 + i * 16.667;
            var id = trace.Frame(arrival, present: i is not (20 or 25 or 30), queueMs: i == 20 ? 200 : 1.0);
            if (i == 20)
            {
                trace.Instant("preview.drop", arrival + 201, id, extraArgs: ReasonArg("renderer-backlog"));
            }
            else if (i is 25 or 30)
            {
                trace.Instant("preview.drop", arrival + 3, id, extraArgs: ReasonArg("stale"));
            }
        }

        // One drop from a build that did not write reasons yet.
        trace.Instant("preview.drop", 900, Ticks(100 + 3 * 16.667));

        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build(), new FrameTraceAnalysisOptions { TopFrames = 3 });

        var drops = Assert.Single(analysis.Drops, d => d.Name == "preview.drop");
        Assert.Equal(4, drops.Count);
        Assert.Equal(2, Assert.Single(drops.Reasons, r => r.Reason == "stale").Count);
        Assert.Equal(1, Assert.Single(drops.Reasons, r => r.Reason == "renderer-backlog").Count);
        Assert.Equal(3, drops.Reasons.Sum(r => r.Count));

        var worst = analysis.SlowestFrames[0];
        Assert.True(worst.Dropped);
        Assert.Equal("renderer-backlog", worst.DropReason);
        Assert.Contains("renderer-backlog", analysis.FormatSummary());
        Assert.Contains("(no reason recorded)", analysis.FormatSummary());

        using var json = JsonDocument.Parse(analysis.ToJson());
        Assert.True(json.RootElement.GetProperty("drops")[0].GetProperty("reasons").GetArrayLength() >= 2);
    }

    [Fact]
    public void DropsFromADumpWithoutReasonsAreCountedWithNoReasonBuckets()
    {
        var trace = new TraceBuilder();
        for (var i = 0; i < 30; i++)
        {
            var arrival = 100 + i * 16.667;
            var id = trace.Frame(arrival);
            if (i is 5 or 6)
            {
                trace.Instant("preview.drop", arrival + 2, id);
            }
        }

        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());

        var drops = Assert.Single(analysis.Drops);
        Assert.Equal(2, drops.Count);
        Assert.Empty(drops.Reasons);
        Assert.DoesNotContain("(no reason recorded)", analysis.FormatSummary());
        Assert.All(analysis.SlowestFrames, frame => Assert.Null(frame.DropReason));
    }

    [Theory]
    [InlineData("\"reason\":5")]
    [InlineData("\"reason\":{\"a\":1}")]
    [InlineData("\"reason\":[\"stale\"]")]
    [InlineData("\"reason\":true")]
    [InlineData("\"reason\":null")]
    [InlineData("\"reason\":\"\"")]
    [InlineData("\"reason\":\"   \"")]
    public void AReasonThatIsNotAUsableStringIsIgnoredWithoutSkippingTheDrop(string extraArgs)
    {
        var trace = new TraceBuilder();
        for (var i = 0; i < 20; i++)
        {
            var arrival = 100 + i * 16.667;
            var id = trace.Frame(arrival);
            if (i == 7)
            {
                trace.Instant("preview.drop", arrival + 2, id, extraArgs: extraArgs);
            }
        }

        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());

        Assert.Equal(0, analysis.Trace.SkippedEvents);
        var drops = Assert.Single(analysis.Drops);
        Assert.Equal(1, drops.Count);
        Assert.Empty(drops.Reasons);
        Assert.Equal(1, analysis.Frames.WithDrop);
    }

    [Fact]
    public void MoreThanThirtyTwoDistinctReasonsFoldIntoOtherAndLongReasonsAreTruncated()
    {
        var trace = new TraceBuilder();

        // The long reason comes first so it is kept (truncated) rather than folded into "other".
        var longId = trace.Frame(50);
        var longReason = new string('x', 500);
        trace.Instant("preview.drop", 52, longId, extraArgs: ReasonArg(longReason));
        trace.Instant("preview.drop", 53, longId, extraArgs: ReasonArg(longReason));

        for (var i = 0; i < 40; i++)
        {
            var arrival = 100 + i * 16.667;
            var id = trace.Frame(arrival);
            trace.Instant("preview.drop", arrival + 2, id, extraArgs: ReasonArg("reason-" + i.ToString(CultureInfo.InvariantCulture)));
        }

        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());

        var drops = Assert.Single(analysis.Drops);
        Assert.Equal(42, drops.Count);
        Assert.Equal(2, Assert.Single(drops.Reasons, r => r.Reason == new string('x', 64)).Count);
        // 32 distinct reasons are kept (the long one plus reason-0..reason-30), then the nine extras share one
        // "other" bucket; no count is lost.
        Assert.True(drops.Reasons.Count <= 33, "reasons stay bounded: " + drops.Reasons.Count.ToString(CultureInfo.InvariantCulture));
        Assert.Equal(9, Assert.Single(drops.Reasons, r => r.Reason == "other").Count);
        Assert.Equal(42, drops.Reasons.Sum(r => r.Count));
        Assert.All(drops.Reasons, r => Assert.True(r.Reason.Length <= 64));
    }

    [Fact]
    public void AnArgFieldOnAnEventIsToleratedAndNeverTheFrameId()
    {
        var trace = Steady(20);
        var id = Ticks(100);
        trace.Instant("preview.drop", 104, id, extraArgs: "\"arg\":9999999999999," + ReasonArg("stale"));
        trace.Instant("capture.sample_time", 98, id, extraArgs: "\"arg\":3");
        trace.Raw("{\"name\":\"preview.render_submit\",\"cat\":\"x\",\"ph\":\"X\",\"ts\":100500,\"dur\":10,\"pid\":1,\"tid\":1,\"args\":{\"arg\":42,\"id\":"
            + id.ToString(CultureInfo.InvariantCulture) + "}}");

        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());

        Assert.Equal(0, analysis.Trace.SkippedEvents);
        Assert.Equal(20, analysis.Frames.Total);
        Assert.Equal(1, analysis.Frames.WithDrop);
        Assert.Equal("qpc-id", analysis.Trace.ArrivalSource);
    }

    [Fact]
    public void HubRefreshSpansWithIdMinusOneAreAmbientAndNeverFrames()
    {
        var trace = Steady(20);
        for (var i = 0; i < 25; i++)
        {
            trace.Span("telemetry.hub_refresh", 120 + i * 100, 2.5, -1, tid: 3);
        }

        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());

        Assert.Equal(20, analysis.Frames.Total);
        var hub = Assert.Single(analysis.AmbientEvents, e => e.Name == "telemetry.hub_refresh");
        Assert.Equal(25, hub.Count);
        Assert.Equal(2.5, hub.MaxMs, 3);
        Assert.DoesNotContain(analysis.Stages, stage => stage.Stage == "telemetry.hub_refresh");
    }

    [Fact]
    public void FramesWithoutAnySampleTimeEventGetAWarningNamingTheMissingEvent()
    {
        var withoutSampleTime = FrameTraceAnalyzer.AnalyzeJson(Steady(30).Build());
        Assert.DoesNotContain(withoutSampleTime.Derived, d => d.Name == "ingress_to_visible");
        Assert.Contains(withoutSampleTime.Warnings, w => w.Contains("capture.sample_time", StringComparison.Ordinal));

        var trace = new TraceBuilder();
        for (var i = 0; i < 30; i++)
        {
            trace.Frame(100 + i * 16.667, sampleOffsetMs: 4);
        }

        var withSampleTime = FrameTraceAnalyzer.AnalyzeJson(trace.Build());
        Assert.Contains(withSampleTime.Derived, d => d.Name == "ingress_to_visible");
        Assert.DoesNotContain(withSampleTime.Warnings, w => w.Contains("capture.sample_time", StringComparison.Ordinal));
    }

    [Fact]
    public void UnknownStageNamesAreToleratedAndListed()
    {
        var trace = Steady(30);
        var id = Ticks(100);
        trace.Span("future.stage_x", 100.2, 3.0, id);
        trace.Span("kind-99", 101, 1.0, id);
        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());

        Assert.Equal(30, analysis.Frames.Total);
        var future = Stage(analysis, "future.stage_x");
        Assert.Equal(1, future.Frames);
        Assert.Equal(3.0, future.MaxMs, 3);
        Assert.Contains(analysis.Stages, stage => stage.Stage == "kind-99");
        // Rare unknown stages are not "expected" on every frame, so they do not make other frames incomplete.
        Assert.Equal(0, analysis.Frames.MissingStages);
    }

    [Fact]
    public void IdMinusOneEventsNeverFormFrames()
    {
        var trace = Steady(20);
        trace.Span("ui.dispatcher_latency", 150, 60, null);
        trace.Span("automation.command", 160, 4, null);
        trace.Span("some.future_ambient", 170, 2, -1);
        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());

        Assert.Equal(20, analysis.Frames.Total);
        Assert.Contains(analysis.AmbientEvents, e => e.Name == "ui.dispatcher_latency" && e.Count == 1 && e.MaxMs > 59.9);
        Assert.Contains(analysis.AmbientEvents, e => e.Name == "automation.command");
        Assert.Contains(analysis.AmbientEvents, e => e.Name == "some.future_ambient");
        Assert.DoesNotContain(analysis.Stages, stage => stage.Stage == "ui.dispatcher_latency");
    }

    [Fact]
    public void AmbientStagesAreNeverFramesEvenWhenTheyCarryAPositiveId()
    {
        var trace = Steady(20);
        // The GC pause event records the gen-0 collection count as its id today.
        trace.Span("gc.pause", 140, 8, 7);
        trace.Span("gc.pause", 180, 9, 8);
        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());

        Assert.Equal(20, analysis.Frames.Total);
        Assert.Equal(2, Assert.Single(analysis.AmbientEvents, e => e.Name == "gc.pause").Count);
        Assert.DoesNotContain(analysis.Stages, stage => stage.Stage == "gc.pause");
        Assert.Empty(analysis.Warnings.Where(w => w.Contains("QPC", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void ASingleFrameTraceCompletesWithoutHitchesOrNaN()
    {
        var trace = new TraceBuilder();
        trace.Frame(100);
        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());

        Assert.Equal(1, analysis.Frames.Total);
        var queue = Stage(analysis, "preview.queue_dwell");
        Assert.Equal(queue.P50Ms, queue.P99Ms);
        Assert.Equal(queue.P50Ms, queue.MaxMs);
        Assert.Single(analysis.SlowestFrames);
        Assert.True(analysis.PresentCadence is null || analysis.PresentCadence.HitchCount == 0);
        using var json = JsonDocument.Parse(analysis.ToJson());
        Assert.Equal(JsonValueKind.Object, json.RootElement.ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(analysis.FormatSummary()));
    }

    [Fact]
    public void AnEmptyTraceIsValidAndReportsNothingWithoutDividingByZero()
    {
        var analysis = FrameTraceAnalyzer.AnalyzeJson(new TraceBuilder().Build());

        Assert.Equal(0, analysis.Frames.Total);
        Assert.Empty(analysis.SlowestFrames);
        Assert.Empty(analysis.Stages);
        Assert.Null(analysis.PresentCadence);
        Assert.NotEmpty(analysis.Warnings);
        using var json = JsonDocument.Parse(analysis.ToJson());
        Assert.False(string.IsNullOrWhiteSpace(analysis.FormatSummary()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \r\n  ")]
    [InlineData("this is not json")]
    [InlineData("[1,2,3]")]
    [InlineData("42")]
    [InlineData("{\"foo\":1}")]
    [InlineData("{\"traceEvents\":{}}")]
    [InlineData("{\"traceEvents\":[1,2,3],\"sussudio\":{}}x")]
    public void InputThatIsNotATraceIsAFormatExceptionWithAMessage(string content)
    {
        var error = Assert.Throws<FrameTraceFormatException>(() => FrameTraceAnalyzer.AnalyzeJson(content));
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
    }

    [Theory]
    [InlineData(0.05)]
    [InlineData(0.30)]
    [InlineData(0.50)]
    [InlineData(0.75)]
    [InlineData(0.97)]
    public void ATruncatedDumpIsAFormatExceptionAtAnyCutPoint(double fraction)
    {
        var json = Steady(80).Build();
        var cut = json.Substring(0, (int)(json.Length * fraction));

        var error = Assert.Throws<FrameTraceFormatException>(() => FrameTraceAnalyzer.AnalyzeJson(cut));
        Assert.Contains("truncated", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ADumpMissingOnlyItsLastCharacterIsStillATruncatedDump()
    {
        var json = Steady(10).Build();
        Assert.Throws<FrameTraceFormatException>(() => FrameTraceAnalyzer.AnalyzeJson(json[..^1]));
    }

    [Fact]
    public void EventsWithUnusableFieldsAreSkippedAndCountedWhileTheRestIsAnalyzed()
    {
        var trace = Steady(30);
        trace.Raw("{\"name\":\"preview.present\",\"ph\":\"X\",\"ts\":\"soon\",\"dur\":1,\"tid\":1,\"args\":{\"id\":5}}");
        trace.Raw("{\"ph\":\"X\",\"ts\":10,\"dur\":1,\"tid\":1,\"args\":{\"id\":5}}");
        trace.Raw("{\"name\":\"preview.present\",\"ph\":\"X\",\"ts\":10,\"dur\":\"long\",\"tid\":1,\"args\":{\"id\":5}}");
        trace.Raw("\"a string where an event should be\"");
        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());

        Assert.Equal(4, analysis.Trace.SkippedEvents);
        Assert.Equal(30, analysis.Frames.Total);
    }

    [Fact]
    public void AnEventWithoutArgsIsTreatedAsHavingNoFrame()
    {
        var trace = Steady(10);
        trace.Raw("{\"name\":\"preview.present\",\"ph\":\"X\",\"ts\":5000,\"dur\":10,\"tid\":1}");
        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());

        Assert.Equal(10, analysis.Frames.Total);
        Assert.Contains(analysis.AmbientEvents, e => e.Name == "preview.present");
    }

    [Fact]
    public void AMissingMetadataBlockFallsBackToTheEarliestEventAndSaysSo()
    {
        var trace = Steady(30);
        trace.IncludeMetadata = false;
        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());

        Assert.Equal(30, analysis.Frames.Total);
        Assert.Contains(analysis.Warnings, w => w.Contains("sussudio", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("earliest-event", analysis.Trace.ArrivalSource);
        Assert.Equal(1.0, Stage(analysis, "preview.queue_dwell").P50Ms, 3);
        // End to end is measured from the capture.read start (2 ms before arrival) in this mode, so it is the
        // present end (2.5 ms after arrival) plus 2 ms, not a huge number.
        var endToEnd = Assert.Single(analysis.Derived, d => d.Name == "arrival_to_present");
        Assert.InRange(endToEnd.P50Ms, 4.4, 4.6);
    }

    [Fact]
    public void IdsThatAreSequenceNumbersFallBackToTheEarliestEventPerFrame()
    {
        var trace = new TraceBuilder();
        for (var i = 0; i < 30; i++)
        {
            var start = 100 + i * 16.667;
            trace.Span("capture.read", start, 2, i);
            trace.Span("preview.present", start + 4, 0.1, i);
        }

        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());

        Assert.Equal(30, analysis.Frames.Total);
        Assert.Equal("earliest-event", analysis.Trace.ArrivalSource);
        Assert.Contains(analysis.Warnings, w => w.Contains("QPC", StringComparison.OrdinalIgnoreCase));
        var endToEnd = Assert.Single(analysis.Derived, d => d.Name == "arrival_to_present");
        Assert.InRange(endToEnd.P50Ms, 4.0, 4.2);
    }

    [Fact]
    public void DuplicateEventsForOneFrameAndStageAreSummedNotDuplicated()
    {
        var trace = new TraceBuilder();
        for (var i = 0; i < 20; i++)
        {
            var id = trace.Frame(100 + i * 16.667);
            if (i == 5)
            {
                trace.Span("preview.queue_dwell", 100 + i * 16.667 + 6, 2.0, id);
            }
        }

        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());

        Assert.Equal(20, analysis.Frames.Total);
        var queue = Stage(analysis, "preview.queue_dwell");
        Assert.Equal(20, queue.Frames);
        Assert.Equal(3.0, queue.MaxMs, 3);
    }

    [Fact]
    public void SlowestFramesAreWorstFirstCappedAtTopAndCarryTheirStageBreakdown()
    {
        var trace = new TraceBuilder();
        var queues = new[] { 1, 1, 9, 1, 5, 1, 12, 1, 1, 3 };
        for (var i = 0; i < queues.Length; i++)
        {
            trace.Frame(100 + i * 16.667, queueMs: queues[i]);
        }

        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build(), new FrameTraceAnalysisOptions { TopFrames = 3 });

        Assert.Equal(3, analysis.SlowestFrames.Count);
        Assert.Equal("arrival-to-present", analysis.SlowestFrames[0].RankedBy);
        Assert.True(analysis.SlowestFrames[0].TotalMs >= analysis.SlowestFrames[1].TotalMs);
        Assert.True(analysis.SlowestFrames[1].TotalMs >= analysis.SlowestFrames[2].TotalMs);
        Assert.Equal(Ticks(100 + 6 * 16.667), analysis.SlowestFrames[0].Id);
        var breakdown = analysis.SlowestFrames[0].Stages;
        Assert.Contains(breakdown, s => s.Stage == "preview.queue_dwell" && Math.Abs(s.DurationMs - 12) < 0.001);
        Assert.Contains(breakdown, s => s.Stage == "preview.present");
        Assert.Equal("preview.queue_dwell", analysis.SlowestFrames[0].DominantStage);
    }

    [Fact]
    public void IngressToVisibleUsesTheCaptureSampleTimeInstantWhenOneExists()
    {
        var trace = new TraceBuilder();
        for (var i = 0; i < 40; i++)
        {
            // Half of the frames carry a sample-time instant 5 ms before arrival.
            trace.Frame(100 + i * 16.667, sampleOffsetMs: i % 2 == 0 ? 5 : null);
        }

        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());

        var ingress = Assert.Single(analysis.Derived, d => d.Name == "ingress_to_visible");
        Assert.Equal(20, ingress.Count);
        // Estimated visible is the end of preview.pipeline (2.5 ms after arrival) minus the sample time.
        Assert.Equal(7.5, ingress.P50Ms, 3);
        Assert.DoesNotContain(analysis.Stages, stage => stage.Stage == "capture.sample_time");
    }

    [Fact]
    public void ALongMarkerLargerThanTheReaderBufferStillParses()
    {
        var trace = Steady(10);
        trace.Marker(150, new string('m', 300_000));
        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());

        Assert.Equal(10, analysis.Frames.Total);
        Assert.Contains(analysis.AmbientEvents, e => e.Name == "marker" && e.Count == 1);
    }

    [Fact]
    public void ARingThatWrappedIsReportedBecauseEarlyFramesMayBeIncomplete()
    {
        var trace = Steady(10);
        trace.Overwritten = 1234;
        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());

        Assert.Equal(1234, analysis.Trace.Overwritten);
        Assert.Contains(analysis.Warnings, w => w.Contains("wrap", StringComparison.OrdinalIgnoreCase) || w.Contains("overwritten", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AnalyzeFileReadsAPathAndReportsAMissingFileAsAFormatException()
    {
        var directory = Path.Combine(Path.GetTempPath(), "sussudio-trace-analyzer-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "steady.trace.json");
            File.WriteAllText(path, Steady(12).Build(), new UTF8Encoding(false));
            Assert.Equal(12, FrameTraceAnalyzer.AnalyzeFile(path).Frames.Total);

            var error = Assert.Throws<FrameTraceFormatException>(() => FrameTraceAnalyzer.AnalyzeFile(Path.Combine(directory, "absent.json")));
            Assert.Contains("absent.json", error.Message);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void TheJsonOutputCarriesTheSameFindingsAsTheTypedResult()
    {
        var trace = new TraceBuilder();
        for (var i = 0; i < 100; i++)
        {
            trace.Frame(100 + i * 16.667, presentDelayMs: i == 50 ? 14 : 0);
        }

        trace.Span("gc.pause", 100 + 50 * 16.667 + 5, 25, null);
        var analysis = FrameTraceAnalyzer.AnalyzeJson(trace.Build());
        using var json = JsonDocument.Parse(analysis.ToJson());
        var root = json.RootElement;

        Assert.Equal(100, root.GetProperty("frames").GetProperty("total").GetInt32());
        Assert.True(root.GetProperty("presentCadence").GetProperty("hitchCount").GetInt32() >= 1);
        var firstHitch = root.GetProperty("presentCadence").GetProperty("hitches")[0];
        Assert.Contains(
            firstHitch.GetProperty("attributions").EnumerateArray(),
            a => a.GetProperty("name").GetString() == "gc.pause");
    }
}
