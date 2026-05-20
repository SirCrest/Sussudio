namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static CaptureCadenceFlattenedProjection BuildCaptureCadenceFlattenedProjection(
        CaptureCadenceProjection captureCadence)
        => new()
        {
            ExpectedFrameRate = captureCadence.ExpectedFrameRate,
            SampleCount = captureCadence.SampleCount,
            ObservedFps = captureCadence.ObservedFps,
            ExpectedIntervalMs = captureCadence.ExpectedIntervalMs,
            AverageIntervalMs = captureCadence.AverageIntervalMs,
            P95IntervalMs = captureCadence.P95IntervalMs,
            P99IntervalMs = captureCadence.P99IntervalMs,
            MaxIntervalMs = captureCadence.MaxIntervalMs,
            OnePercentLowFps = captureCadence.OnePercentLowFps,
            FivePercentLowFps = captureCadence.FivePercentLowFps,
            SampleDurationMs = captureCadence.SampleDurationMs,
            RecentIntervalsMs = captureCadence.RecentIntervalsMs,
            JitterStdDevMs = captureCadence.JitterStdDevMs,
            SevereGapCount = captureCadence.SevereGapCount,
            EstimatedDroppedFrames = captureCadence.EstimatedDroppedFrames,
            EstimatedDropPercent = captureCadence.EstimatedDropPercent
        };

    private readonly record struct CaptureCadenceFlattenedProjection
    {
        public double ExpectedFrameRate { get; init; }
        public int SampleCount { get; init; }
        public double ObservedFps { get; init; }
        public double ExpectedIntervalMs { get; init; }
        public double AverageIntervalMs { get; init; }
        public double P95IntervalMs { get; init; }
        public double P99IntervalMs { get; init; }
        public double MaxIntervalMs { get; init; }
        public double OnePercentLowFps { get; init; }
        public double FivePercentLowFps { get; init; }
        public double SampleDurationMs { get; init; }
        public double[] RecentIntervalsMs { get; init; }
        public double JitterStdDevMs { get; init; }
        public long SevereGapCount { get; init; }
        public long EstimatedDroppedFrames { get; init; }
        public double EstimatedDropPercent { get; init; }
    }
}
