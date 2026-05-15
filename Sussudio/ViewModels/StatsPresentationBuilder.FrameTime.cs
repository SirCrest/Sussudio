using System;

namespace Sussudio.ViewModels;

internal static partial class StatsPresentationBuilder
{
    public static StatsFrameTimePresentation BuildFrameTimePresentation(StatsSnapshot snapshot)
    {
        var range = ResolveFrameTimeRange(snapshot.SourceExpectedFps);
        return new StatsFrameTimePresentation(
            SourceText: $"Src {FormatMs(snapshot.SourceP95IntervalMs)} P95 / {FormatMs(snapshot.SourceAvgIntervalMs)} avg",
            VisualText: snapshot.VisualCadenceSamples <= 0
                ? "Crop \u2014"
                : $"Crop {FormatVisualCadenceSummary(snapshot)}",
            PreviewText: $"Preview: {FormatPreviewCadenceSummary(snapshot)}",
            LatencyText: $"Lat {FormatMs(snapshot.PipelineLatencyMs)}",
            StatusText: $"Target {FormatMs(range.ExpectedMs)} | blue=crop changes; green=preview presents | range {FormatMs(range.MinMs)}-{FormatMs(range.MaxMs)}",
            Range: range,
            VisualSamples: snapshot.VisualCadenceRecentChangeIntervalsMs ?? Array.Empty<double>(),
            PreviewSamples: snapshot.PreviewRecentPresentIntervalsMs ?? Array.Empty<double>());
    }

    public static StatsFrameTimeRange ResolveFrameTimeRange(double expectedFps)
    {
        var fps = expectedFps > 0 ? expectedFps : 60.0;
        var lowerFps = Math.Max(1.0, fps * 0.75);
        var upperFps = Math.Max(lowerFps + 1.0, fps * 1.25);
        var minMs = 1000.0 / upperFps;
        var maxMs = 1000.0 / lowerFps;
        return new StatsFrameTimeRange(
            MinMs: minMs,
            MaxMs: maxMs,
            ExpectedMs: 1000.0 / fps);
    }

    private static string FormatPreviewCadenceSummary(StatsSnapshot snapshot)
    {
        if (snapshot.PreviewCadenceSamples <= 0)
        {
            return "\u2014";
        }

        var currentFrameTimeMs = ResolveCurrentPreviewFrameTimeMs(snapshot);
        var currentFrameTime = currentFrameTimeMs > 0
            ? FormatMs(currentFrameTimeMs)
            : "\u2014";
        var onePercentLow = Sanitize(snapshot.PreviewOnePercentLowFps) > 0
            ? $"1% low {FormatFps(snapshot.PreviewOnePercentLowFps)} fps"
            : "1% low \u2014";
        return $"{currentFrameTime} | {onePercentLow}";
    }

    private static double ResolveCurrentPreviewFrameTimeMs(StatsSnapshot snapshot)
    {
        var samples = snapshot.PreviewRecentPresentIntervalsMs;
        if (samples is { Count: > 0 })
        {
            return Sanitize(samples[samples.Count - 1]);
        }

        return Sanitize(snapshot.PreviewAvgIntervalMs);
    }
}
