using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static MjpegPreviewJitterTimingProjection BuildMjpegPreviewJitterTimingProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            InputSampleCount = health.MjpegPreviewJitterInputSampleCount,
            InputAvgMs = health.MjpegPreviewJitterInputAvgMs,
            InputP95Ms = health.MjpegPreviewJitterInputP95Ms,
            InputMaxMs = health.MjpegPreviewJitterInputMaxMs,
            OutputSampleCount = health.MjpegPreviewJitterOutputSampleCount,
            OutputAvgMs = health.MjpegPreviewJitterOutputAvgMs,
            OutputP95Ms = health.MjpegPreviewJitterOutputP95Ms,
            OutputMaxMs = health.MjpegPreviewJitterOutputMaxMs,
            LatencySampleCount = health.MjpegPreviewJitterLatencySampleCount,
            LatencyAvgMs = health.MjpegPreviewJitterLatencyAvgMs,
            LatencyP95Ms = health.MjpegPreviewJitterLatencyP95Ms,
            LatencyMaxMs = health.MjpegPreviewJitterLatencyMaxMs
        };

    private readonly record struct MjpegPreviewJitterTimingProjection
    {
        public int InputSampleCount { get; init; }
        public double InputAvgMs { get; init; }
        public double InputP95Ms { get; init; }
        public double InputMaxMs { get; init; }
        public int OutputSampleCount { get; init; }
        public double OutputAvgMs { get; init; }
        public double OutputP95Ms { get; init; }
        public double OutputMaxMs { get; init; }
        public int LatencySampleCount { get; init; }
        public double LatencyAvgMs { get; init; }
        public double LatencyP95Ms { get; init; }
        public double LatencyMaxMs { get; init; }
    }

    private static MjpegPreviewJitterTimingFlattenedProjection BuildMjpegPreviewJitterTimingFlattenedProjection(
        MjpegPreviewJitterTimingProjection timing)
        => new()
        {
            InputSampleCount = timing.InputSampleCount,
            InputAvgMs = timing.InputAvgMs,
            InputP95Ms = timing.InputP95Ms,
            InputMaxMs = timing.InputMaxMs,
            OutputSampleCount = timing.OutputSampleCount,
            OutputAvgMs = timing.OutputAvgMs,
            OutputP95Ms = timing.OutputP95Ms,
            OutputMaxMs = timing.OutputMaxMs,
            LatencySampleCount = timing.LatencySampleCount,
            LatencyAvgMs = timing.LatencyAvgMs,
            LatencyP95Ms = timing.LatencyP95Ms,
            LatencyMaxMs = timing.LatencyMaxMs
        };

    private readonly record struct MjpegPreviewJitterTimingFlattenedProjection
    {
        public int InputSampleCount { get; init; }
        public double InputAvgMs { get; init; }
        public double InputP95Ms { get; init; }
        public double InputMaxMs { get; init; }
        public int OutputSampleCount { get; init; }
        public double OutputAvgMs { get; init; }
        public double OutputP95Ms { get; init; }
        public double OutputMaxMs { get; init; }
        public int LatencySampleCount { get; init; }
        public double LatencyAvgMs { get; init; }
        public double LatencyP95Ms { get; init; }
        public double LatencyMaxMs { get; init; }
    }
}
