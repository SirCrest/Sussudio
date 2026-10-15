using System.Globalization;

namespace Sussudio.Tools.Performance;

// Turns the per-frame accumulators the reader folded into the typed analysis. Design rules:
//   - Arrival time is the frame's QPC id placed on the trace axis with integer ticks (id - originQpc) and only then
//     converted to microseconds; when the dump has no usable metadata, or the ids are not QPC ticks, it is the frame's
//     earliest span and the report says so.
//   - A frame is never removed for being incomplete. A stage most frames have is "expected"; frames without it are
//     counted per stage and in total, and dropped frames still rank by lifetime.
//   - Aggregate spans (pipeline, render_total) cover other stages, so they are reported but never named as the stage
//     that grew most or the dominant stage.
//   - A slow frame's total is measured from its arrival, so a stage that ended by arrival (capture.read, the wait for
//     the device) is outside it: it is listed as pre-arrival and never named dominant. A stage that straddles arrival
//     counts only its part after it.
internal static class FrameTraceAnalysisBuilder
{
    private const double PlausibleArrivalSlackUs = 60_000_000; // an id further than a minute from its events is not a QPC arrival
    private const double ExpectedStageFraction = 0.5;
    private const double MinStageAttributionMs = 1.0;
    private const double MinAmbientAttributionMs = 2.0;
    private const int MaxEventAttributions = 6;
    private const int MaxMarkerAttributions = 3;
    private const int MaxMarkerDetailChars = 160;
    private const double PreArrivalToleranceUs = 5; // a span ending within 5 us after arrival ended at arrival (separately taken timestamps)
    private const string PresentStage = "preview.present";
    private const string PipelineStage = "preview.pipeline";

    public static FrameTraceAnalysis Build(FrameTraceCapture capture, FrameTraceAnalysisOptions options)
    {
        var warnings = new List<string>();
        var frames = capture.Frames.Values.ToList();
        var arrivalSource = ResolveArrival(capture, frames, warnings, out var fallbackFrames);
        frames.Sort(static (a, b) =>
        {
            var byArrival = a.ArrivalUs.CompareTo(b.ArrivalUs);
            return byArrival != 0 ? byArrival : a.Id.CompareTo(b.Id);
        });

        var stageCount = capture.StageNames.Count;
        var durations = new List<double>[stageCount];
        var endLatencies = new List<double>[stageCount];
        for (var s = 0; s < stageCount; s++)
        {
            durations[s] = new List<double>();
            endLatencies[s] = new List<double>();
        }

        foreach (var frame in frames)
        {
            for (var s = 0; s < stageCount; s++)
            {
                if (!frame.Has(s))
                {
                    continue;
                }

                durations[s].Add(frame.DurUs[s] / 1000d);
                endLatencies[s].Add((frame.EndUs[s] - frame.ArrivalUs) / 1000d);
            }
        }

        var expected = new bool[stageCount];
        var medianDuration = new double[stageCount];
        var stages = new List<TraceStageStats>(stageCount);
        for (var s = 0; s < stageCount; s++)
        {
            durations[s].Sort();
            endLatencies[s].Sort();
            expected[s] = frames.Count > 0 && durations[s].Count >= frames.Count * ExpectedStageFraction;
            medianDuration[s] = PerfMath.Percentile(durations[s], 0.5);
            stages.Add(new TraceStageStats(
                capture.StageNames[s],
                capture.StageAggregate[s],
                durations[s].Count,
                expected[s] ? frames.Count - durations[s].Count : 0,
                PerfMath.Round(PerfMath.Mean(durations[s])),
                PerfMath.Round(medianDuration[s]),
                PerfMath.Round(PerfMath.Percentile(durations[s], 0.9)),
                PerfMath.Round(PerfMath.Percentile(durations[s], 0.99)),
                PerfMath.Round(durations[s].Count == 0 ? 0 : durations[s][^1]),
                PerfMath.Round(PerfMath.Percentile(endLatencies[s], 0.5)),
                PerfMath.Round(PerfMath.Percentile(endLatencies[s], 0.99))));
        }

        stages.Sort(static (a, b) =>
        {
            var byEnd = a.EndLatencyP50Ms.CompareTo(b.EndLatencyP50Ms);
            return byEnd != 0 ? byEnd : string.CompareOrdinal(a.Stage, b.Stage);
        });

        var missingFrames = 0;
        var withDrop = 0;
        foreach (var frame in frames)
        {
            for (var s = 0; s < stageCount; s++)
            {
                if (expected[s] && !frame.Has(s))
                {
                    missingFrames++;
                    break;
                }
            }

            if (frame.Drops > 0)
            {
                withDrop++;
            }
        }

        var presentIndex = capture.StageIndexByName.TryGetValue(PresentStage, out var present) ? present : -1;
        var pipelineIndex = capture.StageIndexByName.TryGetValue(PipelineStage, out var pipeline) ? pipeline : -1;

        var derived = BuildDerived(frames, presentIndex, pipelineIndex);
        var cadence = BuildCadence(capture, frames, options, stageCount, medianDuration, presentIndex);
        var slowest = BuildSlowest(capture, frames, options, stageCount, expected, presentIndex);

        AddWarnings(capture, frames.Count, frames.Any(frame => frame.HasSample), warnings);

        var info = new FrameTraceInfo(
            capture.ParsedEvents,
            capture.SkippedEvents,
            capture.HasMetadata,
            capture.HasMetadata ? capture.QpcFrequency : 0,
            capture.HasMetadata ? capture.OriginQpc : 0,
            capture.StartedUtc,
            capture.Capacity,
            capture.Recorded,
            capture.Retained,
            capture.Overwritten,
            capture.Running,
            arrivalSource,
            fallbackFrames,
            capture.ParsedEvents == 0 ? 0 : PerfMath.Round((capture.MaxUs - capture.MinUs) / 1000d));

        var ambient = capture.Ambient
            .Select(pair => new TraceAmbientStats(
                pair.Key,
                pair.Value.Count,
                PerfMath.Round(pair.Value.TotalUs / 1000d),
                PerfMath.Round(pair.Value.MaxUs / 1000d)))
            .OrderByDescending(stat => stat.TotalMs)
            .ThenBy(stat => stat.Name, StringComparer.Ordinal)
            .ToList();
        var drops = capture.Drops
            .Select(pair => new TraceDropStats(
                pair.Key,
                pair.Value,
                capture.DropReasons.TryGetValue(pair.Key, out var reasons)
                    ? reasons
                        .Select(reason => new TraceDropReason(reason.Key, reason.Value))
                        .OrderByDescending(reason => reason.Count)
                        .ThenBy(reason => reason.Reason, StringComparer.Ordinal)
                        .ToList()
                    : new List<TraceDropReason>()))
            .OrderByDescending(stat => stat.Count)
            .ThenBy(stat => stat.Name, StringComparer.Ordinal)
            .ToList();

        return new FrameTraceAnalysis(
            info,
            new FrameTraceFrameCounts(frames.Count, frames.Count - missingFrames, missingFrames, withDrop),
            stages,
            derived,
            cadence,
            slowest,
            ambient,
            drops,
            warnings);
    }

    private static string ResolveArrival(FrameTraceCapture capture, List<FrameAcc> frames, List<string> warnings, out int fallbackFrames)
    {
        fallbackFrames = 0;
        if (frames.Count == 0)
        {
            return "none";
        }

        var useIds = false;
        if (capture.HasMetadata)
        {
            var plausible = 0;
            foreach (var frame in frames)
            {
                frame.ArrivalIdUs = (frame.Id - capture.OriginQpc) * 1_000_000d / capture.QpcFrequency;
                frame.IdPlausible = Math.Abs(frame.ArrivalIdUs - FirstEventUs(frame)) <= PlausibleArrivalSlackUs;
                if (frame.IdPlausible)
                {
                    plausible++;
                }
            }

            useIds = plausible * 2 >= frames.Count;
        }

        foreach (var frame in frames)
        {
            if (useIds && frame.IdPlausible)
            {
                frame.ArrivalUs = frame.ArrivalIdUs;
            }
            else
            {
                frame.ArrivalUs = FirstEventUs(frame);
                if (useIds)
                {
                    fallbackFrames++;
                }
            }
        }

        if (!capture.HasMetadata)
        {
            warnings.Add(
                "The dump has no usable \"sussudio\" metadata block (qpcFrequency and originQpc), so arrival is taken from each "
                + "frame's earliest event instead of its QPC id: latencies start at the capture.read call.");
        }
        else if (!useIds)
        {
            warnings.Add(
                "Event ids do not look like QPC timestamps (sequence numbers from an older build?), so arrival is taken from each "
                + "frame's earliest event instead: latencies start at the capture.read call.");
        }
        else if (fallbackFrames > 0)
        {
            warnings.Add(
                $"{fallbackFrames.ToString(CultureInfo.InvariantCulture)} frame(s) had ids that do not look like QPC timestamps; "
                + "their arrival is taken from their earliest event.");
        }

        return useIds ? "qpc-id" : "earliest-event";
    }

    private static double FirstEventUs(FrameAcc frame)
        => double.IsFinite(frame.FirstSpanUs) ? frame.FirstSpanUs : frame.FirstAnyUs;

    private static void AddWarnings(FrameTraceCapture capture, int frameCount, bool anyFrameHasSampleTime, List<string> warnings)
    {
        if (frameCount > 0 && !anyFrameHasSampleTime)
        {
            warnings.Add(
                "No frame has a capture.sample_time event, so ingress_to_visible and ingress_to_arrival are not reported "
                + "(the capture clock is not in the performance-counter domain, or the app predates that event).");
        }

        if (frameCount == 0)
        {
            warnings.Add(
                "The trace contains no frame events. Was the trace started (perf trace start) before the preview ran, "
                + "and was capture delivering frames while it recorded?");
        }

        if (capture.SkippedEvents > 0)
        {
            warnings.Add($"{capture.SkippedEvents.ToString(CultureInfo.InvariantCulture)} event(s) had unusable fields and were skipped.");
        }

        if (capture.Overwritten > 0)
        {
            warnings.Add(
                $"The ring wrapped: {capture.Overwritten.ToString(CultureInfo.InvariantCulture)} event(s) were overwritten, so the earliest "
                + "frames may be incomplete. Start the trace with a larger --capacity to keep the whole interval.");
        }

        if (capture.Running == true)
        {
            warnings.Add("The trace was still running when it was dumped; the newest frames may be incomplete.");
        }

        if (capture.AmbientTruncated)
        {
            warnings.Add(
                $"More than {FrameTraceCapture.MaxRetainedAmbientEvents.ToString(CultureInfo.InvariantCulture)} ambient events were seen; "
                + "per-name totals are exact but hitch attribution only considers the earliest ones.");
        }
    }

    private static List<TraceDerivedStats> BuildDerived(List<FrameAcc> frames, int presentIndex, int pipelineIndex)
    {
        var arrivalToPresent = new List<double>();
        var ingressToVisible = new List<double>();
        var ingressToArrival = new List<double>();
        foreach (var frame in frames)
        {
            var presented = presentIndex >= 0 && frame.Has(presentIndex);
            if (presented)
            {
                arrivalToPresent.Add((frame.EndUs[presentIndex] - frame.ArrivalUs) / 1000d);
            }

            if (!frame.HasSample)
            {
                continue;
            }

            ingressToArrival.Add((frame.ArrivalUs - frame.SampleUs) / 1000d);
            if (pipelineIndex >= 0 && frame.Has(pipelineIndex))
            {
                ingressToVisible.Add((frame.EndUs[pipelineIndex] - frame.SampleUs) / 1000d);
            }
            else if (presented)
            {
                ingressToVisible.Add((frame.EndUs[presentIndex] - frame.SampleUs) / 1000d);
            }
        }

        var derived = new List<TraceDerivedStats>();
        AddDerived(derived, "arrival_to_present", "Frame arrival to the end of preview.present.", arrivalToPresent);
        AddDerived(derived, "ingress_to_visible", "Source sample time (capture.sample_time) to the estimated visible time.", ingressToVisible);
        AddDerived(derived, "ingress_to_arrival", "Source sample time to frame arrival (capture and driver latency).", ingressToArrival);
        return derived;
    }

    private static void AddDerived(List<TraceDerivedStats> derived, string name, string description, List<double> values)
    {
        if (values.Count == 0)
        {
            return;
        }

        values.Sort();
        derived.Add(new TraceDerivedStats(
            name,
            description,
            values.Count,
            PerfMath.Round(PerfMath.Mean(values)),
            PerfMath.Round(PerfMath.Percentile(values, 0.5)),
            PerfMath.Round(PerfMath.Percentile(values, 0.9)),
            PerfMath.Round(PerfMath.Percentile(values, 0.99)),
            PerfMath.Round(values[^1])));
    }

    private static TracePresentCadence? BuildCadence(
        FrameTraceCapture capture,
        List<FrameAcc> frames,
        FrameTraceAnalysisOptions options,
        int stageCount,
        double[] medianDuration,
        int presentIndex)
    {
        if (presentIndex < 0)
        {
            return null;
        }

        var presents = new List<(double Us, int Frame)>();
        for (var i = 0; i < frames.Count; i++)
        {
            if (frames[i].Has(presentIndex))
            {
                presents.Add((frames[i].StartUs[presentIndex], i));
            }
        }

        if (presents.Count < 2)
        {
            return null;
        }

        presents.Sort(static (a, b) => a.Us.CompareTo(b.Us));
        var intervals = new double[presents.Count - 1];
        for (var k = 0; k < intervals.Length; k++)
        {
            intervals[k] = (presents[k + 1].Us - presents[k].Us) / 1000d;
        }

        var sorted = (double[])intervals.Clone();
        Array.Sort(sorted);
        var median = PerfMath.Percentile(sorted, 0.5);
        var threshold = Math.Max(median * options.HitchFactor, median + options.HitchMinExcessMs);

        var arrivalIntervals = new double[frames.Count];
        var sortedArrivals = new List<double>(Math.Max(0, frames.Count - 1));
        for (var i = 1; i < frames.Count; i++)
        {
            arrivalIntervals[i] = (frames[i].ArrivalUs - frames[i - 1].ArrivalUs) / 1000d;
            sortedArrivals.Add(arrivalIntervals[i]);
        }

        sortedArrivals.Sort();
        var medianArrival = PerfMath.Percentile(sortedArrivals, 0.5);

        var hitchIntervals = new List<int>();
        for (var k = 0; k < intervals.Length; k++)
        {
            if (intervals[k] > threshold)
            {
                hitchIntervals.Add(k);
            }
        }

        var worst = hitchIntervals
            .OrderByDescending(k => intervals[k])
            .ThenBy(k => k)
            .Take(Math.Max(0, options.MaxHitches))
            .ToList();
        var hitches = new List<TraceHitch>(worst.Count);
        foreach (var k in worst)
        {
            var previous = frames[presents[k].Frame];
            var lateIndex = presents[k + 1].Frame;
            var late = frames[lateIndex];
            double? arrivalInterval = lateIndex > 0 ? arrivalIntervals[lateIndex] : null;
            var attributions = Attribute(
                capture,
                late,
                presents[k].Us,
                presents[k + 1].Us,
                stageCount,
                medianDuration,
                arrivalInterval,
                medianArrival,
                options);
            hitches.Add(new TraceHitch(
                late.Id,
                previous.Id,
                PerfMath.Round((presents[k + 1].Us - capture.MinUs) / 1000d),
                PerfMath.Round(intervals[k]),
                arrivalInterval is { } value ? PerfMath.Round(value) : null,
                DescribeCause(attributions),
                attributions));
        }

        return new TracePresentCadence(
            PresentStage,
            intervals.Length,
            PerfMath.Round(median),
            PerfMath.Round(PerfMath.Percentile(sorted, 0.99)),
            PerfMath.Round(sorted[^1]),
            PerfMath.Round(threshold),
            hitchIntervals.Count,
            hitches);
    }

    private static List<TraceAttribution> Attribute(
        FrameTraceCapture capture,
        FrameAcc late,
        double windowStartUs,
        double windowEndUs,
        int stageCount,
        double[] medianDuration,
        double? arrivalIntervalMs,
        double medianArrivalMs,
        FrameTraceAnalysisOptions options)
    {
        var attributions = new List<TraceAttribution>();

        // The stage that grew most on the late frame. Aggregates cover other stages and would always win.
        var bestStage = -1;
        var bestGrowth = MinStageAttributionMs;
        for (var s = 0; s < stageCount; s++)
        {
            if (capture.StageAggregate[s] || !late.Has(s))
            {
                continue;
            }

            var growth = (late.DurUs[s] / 1000d) - medianDuration[s];
            if (growth >= bestGrowth)
            {
                bestGrowth = growth;
                bestStage = s;
            }
        }

        if (bestStage >= 0)
        {
            attributions.Add(new TraceAttribution(
                "stage",
                capture.StageNames[bestStage],
                PerfMath.Round(bestGrowth),
                $"{PerfMath.Fixed(late.DurUs[bestStage] / 1000d, 1)} ms on this frame, median {PerfMath.Fixed(medianDuration[bestStage], 1)} ms"));
        }

        // A source that delivered this frame late shows as a long gap between arrivals, not as any stage.
        if (arrivalIntervalMs is { } gap
            && gap > medianArrivalMs * options.HitchFactor
            && gap - medianArrivalMs >= options.HitchMinExcessMs)
        {
            attributions.Add(new TraceAttribution(
                "arrival",
                "capture.arrival_gap",
                PerfMath.Round(gap - medianArrivalMs),
                $"{PerfMath.Fixed(gap, 1)} ms between source frames, median {PerfMath.Fixed(medianArrivalMs, 1)} ms"));
        }

        // Ambient events that overlap the interval: the long ones by size, markers by position.
        var events = new List<TraceAttribution>();
        var markers = new List<TraceAttribution>();
        foreach (var ambient in capture.AmbientEvents)
        {
            if (ambient.StartUs > windowEndUs || ambient.EndUs < windowStartUs)
            {
                continue;
            }

            if (ambient.Name == "marker")
            {
                markers.Add(new TraceAttribution("event", "marker", 0, Truncate(ambient.Text ?? string.Empty)));
                continue;
            }

            var durationMs = Math.Max(0d, ambient.EndUs - ambient.StartUs) / 1000d;
            if (!IsAttributable(ambient.Name, durationMs))
            {
                continue;
            }

            var overlapMs = Math.Max(0d, Math.Min(ambient.EndUs, windowEndUs) - Math.Max(ambient.StartUs, windowStartUs)) / 1000d;
            events.Add(new TraceAttribution(
                "event",
                ambient.Name,
                PerfMath.Round(overlapMs),
                $"{PerfMath.Fixed(durationMs, 1)} ms event, {PerfMath.Fixed(overlapMs, 1)} ms inside the interval"));
        }

        attributions.AddRange(events.OrderByDescending(a => a.Ms).Take(MaxEventAttributions));
        attributions.AddRange(markers.Take(MaxMarkerAttributions));
        return attributions.OrderByDescending(a => a.Ms).ToList();
    }

    private static bool IsAttributable(string name, double durationMs)
        => name is "gc.pause" or "ui.dispatcher_latency" or "automation.command" or "flashback.segment_rotation"
            || (durationMs >= MinAmbientAttributionMs && !name.StartsWith("audio.", StringComparison.Ordinal));

    private static string Truncate(string text)
        => text.Length <= MaxMarkerDetailChars ? text : text[..MaxMarkerDetailChars] + "...";

    private static string DescribeCause(List<TraceAttribution> attributions)
    {
        var top = attributions.FirstOrDefault(a => a.Ms > 0);
        if (top is null)
        {
            return "no stage grew, no arrival gap and no ambient event overlapped: the delay is outside the traced stages";
        }

        return top.Kind switch
        {
            "stage" => $"{top.Name} ran {PerfMath.Fixed(top.Ms, 1)} ms longer than its median",
            "arrival" => $"late arrival: {top.Detail}",
            _ => $"{top.Name} overlapped the interval ({PerfMath.Fixed(top.Ms, 1)} ms)",
        };
    }

    private static List<TraceSlowFrame> BuildSlowest(
        FrameTraceCapture capture,
        List<FrameAcc> frames,
        FrameTraceAnalysisOptions options,
        int stageCount,
        bool[] expected,
        int presentIndex)
    {
        var take = Math.Clamp(options.TopFrames, 0, 1000);
        if (take == 0 || frames.Count == 0)
        {
            return new List<TraceSlowFrame>();
        }

        var ranked = new List<(FrameAcc Frame, double TotalMs, bool Presented)>(frames.Count);
        foreach (var frame in frames)
        {
            var presented = presentIndex >= 0 && frame.Has(presentIndex);
            var end = presented ? frame.EndUs[presentIndex] : frame.LastEndUs;
            ranked.Add((frame, (end - frame.ArrivalUs) / 1000d, presented));
        }

        ranked.Sort(static (a, b) =>
        {
            var byTotal = b.TotalMs.CompareTo(a.TotalMs);
            return byTotal != 0 ? byTotal : a.Frame.Id.CompareTo(b.Frame.Id);
        });

        var slowest = new List<TraceSlowFrame>(Math.Min(take, ranked.Count));
        foreach (var (frame, totalMs, presented) in ranked.Take(take))
        {
            var stages = new List<TraceFrameStage>();
            var missing = new List<string>();
            var preArrival = new List<string>();
            string? dominant = null;
            var dominantMs = double.NegativeInfinity;
            for (var s = 0; s < stageCount; s++)
            {
                if (!frame.Has(s))
                {
                    if (expected[s])
                    {
                        missing.Add(capture.StageNames[s]);
                    }

                    continue;
                }

                var durationMs = frame.DurUs[s] / 1000d;
                stages.Add(new TraceFrameStage(
                    capture.StageNames[s],
                    PerfMath.Round((frame.StartUs[s] - frame.ArrivalUs) / 1000d),
                    PerfMath.Round(durationMs),
                    PerfMath.Round((frame.EndUs[s] - frame.ArrivalUs) / 1000d)));
                if (frame.EndUs[s] <= frame.ArrivalUs + PreArrivalToleranceUs)
                {
                    preArrival.Add(capture.StageNames[s]);
                    continue;
                }

                var insideMs = (frame.EndUs[s] - Math.Max(frame.StartUs[s], frame.ArrivalUs)) / 1000d;
                if (!capture.StageAggregate[s] && insideMs > dominantMs)
                {
                    dominantMs = insideMs;
                    dominant = capture.StageNames[s];
                }
            }

            stages.Sort(static (a, b) => a.StartMs.CompareTo(b.StartMs));
            slowest.Add(new TraceSlowFrame(
                frame.Id,
                PerfMath.Round((frame.ArrivalUs - capture.MinUs) / 1000d),
                PerfMath.Round(totalMs),
                presented ? "arrival-to-present" : "lifetime",
                frame.Drops > 0,
                missing,
                dominant,
                stages,
                frame.DropReason,
                preArrival));
        }

        return slowest;
    }
}
