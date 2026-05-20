namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PreviewRuntimeCadenceFlattenedProjection BuildPreviewRuntimeCadenceFlattenedProjection(
        PreviewRuntimeCadenceProjection cadence)
        => new()
        {
            SampleCount = cadence.SampleCount,
            ObservedFps = cadence.ObservedFps,
            ExpectedIntervalMs = cadence.ExpectedIntervalMs,
            AverageIntervalMs = cadence.AverageIntervalMs,
            P95IntervalMs = cadence.P95IntervalMs,
            P99IntervalMs = cadence.P99IntervalMs,
            MaxIntervalMs = cadence.MaxIntervalMs,
            OnePercentLowFps = cadence.OnePercentLowFps,
            FivePercentLowFps = cadence.FivePercentLowFps,
            SampleDurationMs = cadence.SampleDurationMs,
            RecentIntervalsMs = cadence.RecentIntervalsMs,
            JitterStdDevMs = cadence.JitterStdDevMs,
            SlowFrameCount = cadence.SlowFrameCount,
            SlowFramePercent = cadence.SlowFramePercent
        };

    private readonly record struct PreviewRuntimeCadenceFlattenedProjection
    {
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
        public long SlowFrameCount { get; init; }
        public double SlowFramePercent { get; init; }
    }
}
