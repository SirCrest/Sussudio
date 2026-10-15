using System.Globalization;
using System.Text;

namespace Sussudio.Tools.Performance;

// Plain-text rendering of a FrameTraceAnalysis for a terminal. It only prints what the typed result already holds.
internal static class FrameTraceAnalysisFormatter
{
    private const int StageNameWidth = 30;

    public static string Format(FrameTraceAnalysis analysis)
    {
        var text = new StringBuilder();
        var trace = analysis.Trace;
        var frames = analysis.Frames;

        text.AppendLine("Frame trace analysis");
        text.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"  {trace.ParsedEvents} events over {PerfMath.Fixed(trace.DurationMs / 1000d, 2)} s, {trace.SkippedEvents} skipped; "
            + $"ring {(trace.Capacity?.ToString(CultureInfo.InvariantCulture) ?? "unknown")} events, {trace.Overwritten} overwritten; arrival from {trace.ArrivalSource}"));
        text.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"  frames {frames.Total}: {frames.Complete} complete, {frames.MissingStages} missing stages, {frames.WithDrop} with a drop"));

        if (analysis.Stages.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("Stages (ms; end = from frame arrival to the end of the stage)");
            text.AppendLine($"  {"stage".PadRight(StageNameWidth)} {"frames",7} {"missing",7} {"p50",8} {"p99",8} {"max",8} {"end p50",8} {"end p99",8}");
            foreach (var stage in analysis.Stages)
            {
                var name = stage.Aggregate ? stage.Stage + " *" : stage.Stage;
                text.AppendLine(string.Create(
                    CultureInfo.InvariantCulture,
                    $"  {name.PadRight(StageNameWidth)} {stage.Frames,7} {stage.MissingFrames,7} {PerfMath.Fixed(stage.P50Ms),8} {PerfMath.Fixed(stage.P99Ms),8} {PerfMath.Fixed(stage.MaxMs),8} {PerfMath.Fixed(stage.EndLatencyP50Ms),8} {PerfMath.Fixed(stage.EndLatencyP99Ms),8}"));
            }

            if (analysis.Stages.Any(stage => stage.Aggregate))
            {
                text.AppendLine("  * aggregate span: covers other stages and is never named as the cause");
            }
        }

        if (analysis.Derived.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("Derived latency (ms)");
            foreach (var derived in analysis.Derived)
            {
                text.AppendLine(string.Create(
                    CultureInfo.InvariantCulture,
                    $"  {derived.Name.PadRight(StageNameWidth)} n={derived.Count} mean {PerfMath.Fixed(derived.MeanMs)} p50 {PerfMath.Fixed(derived.P50Ms)} p99 {PerfMath.Fixed(derived.P99Ms)} max {PerfMath.Fixed(derived.MaxMs)}"));
            }
        }

        if (analysis.PresentCadence is { } cadence)
        {
            text.AppendLine();
            text.AppendLine(string.Create(
                CultureInfo.InvariantCulture,
                $"Present cadence ({cadence.Stage}): {cadence.Intervals} intervals, median {PerfMath.Fixed(cadence.MedianMs)} ms, "
                + $"p99 {PerfMath.Fixed(cadence.P99Ms)} ms, max {PerfMath.Fixed(cadence.MaxMs)} ms"));
            var hitchSuffix = cadence.HitchCount > cadence.Hitches.Count ? $", worst {cadence.Hitches.Count} listed" : string.Empty;
            text.AppendLine($"  hitches (interval over {PerfMath.Fixed(cadence.HitchThresholdMs)} ms): {cadence.HitchCount}{hitchSuffix}");
            foreach (var hitch in cadence.Hitches)
            {
                text.AppendLine(string.Create(
                    CultureInfo.InvariantCulture,
                    $"  at {PerfMath.Fixed(hitch.AtMs / 1000d, 3)} s: {PerfMath.Fixed(hitch.IntervalMs, 1)} ms before frame {hitch.FrameId}"));
                text.AppendLine("    likely cause: " + hitch.LikelyCause);
                foreach (var attribution in hitch.Attributions)
                {
                    text.AppendLine($"    - {attribution.Kind} {attribution.Name}: {attribution.Detail}");
                }
            }
        }

        if (analysis.SlowestFrames.Count > 0)
        {
            text.AppendLine();
            text.AppendLine($"Slowest frames (ranked by {analysis.SlowestFrames[0].RankedBy}, worst first)");
            foreach (var frame in analysis.SlowestFrames)
            {
                var dropped = frame.Dropped ? ", dropped" : string.Empty;
                var dominant = frame.DominantStage is null ? string.Empty : $", dominant {frame.DominantStage}";
                var missing = frame.MissingStages.Count == 0 ? string.Empty : $", missing {string.Join(", ", frame.MissingStages)}";
                text.AppendLine(
                    $"  frame {frame.Id} at {PerfMath.Fixed(frame.ArrivalMs / 1000d, 3)} s: {PerfMath.Fixed(frame.TotalMs, 1)} ms ({frame.RankedBy}){dropped}{dominant}{missing}");
                text.AppendLine("    " + string.Join("  ", frame.Stages.Select(stage =>
                    string.Create(CultureInfo.InvariantCulture, $"{stage.Stage} {PerfMath.Fixed(stage.DurationMs, 1)}"))));
            }
        }

        if (analysis.AmbientEvents.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("Events outside frames (GC, UI stalls, automation, markers)");
            foreach (var ambient in analysis.AmbientEvents)
            {
                text.AppendLine(string.Create(
                    CultureInfo.InvariantCulture,
                    $"  {ambient.Name.PadRight(StageNameWidth)} count {ambient.Count} total {PerfMath.Fixed(ambient.TotalMs)} ms max {PerfMath.Fixed(ambient.MaxMs)} ms"));
            }
        }

        if (analysis.Drops.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("Drops");
            foreach (var drop in analysis.Drops)
            {
                text.AppendLine(string.Create(CultureInfo.InvariantCulture, $"  {drop.Name.PadRight(StageNameWidth)} {drop.Count}"));
            }
        }

        if (analysis.Warnings.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("Warnings");
            foreach (var warning in analysis.Warnings)
            {
                text.AppendLine("  - " + warning);
            }
        }

        return text.ToString();
    }
}
