using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PreviewRuntimeCadenceProjection BuildPreviewRuntimeCadenceProjection(
        PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            SampleCount = previewRuntime.DisplayCadenceSampleCount,
            ObservedFps = previewRuntime.DisplayCadenceObservedFps,
            ExpectedIntervalMs = previewRuntime.DisplayCadenceExpectedIntervalMs,
            AverageIntervalMs = previewRuntime.DisplayCadenceAverageIntervalMs,
            P95IntervalMs = previewRuntime.DisplayCadenceP95IntervalMs,
            P99IntervalMs = previewRuntime.DisplayCadenceP99IntervalMs,
            MaxIntervalMs = previewRuntime.DisplayCadenceMaxIntervalMs,
            OnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,
            FivePercentLowFps = previewRuntime.DisplayCadenceFivePercentLowFps,
            SampleDurationMs = previewRuntime.DisplayCadenceSampleDurationMs,
            RecentIntervalsMs = previewRuntime.DisplayCadenceRecentIntervalsMs,
            JitterStdDevMs = previewRuntime.DisplayCadenceJitterStdDevMs,
            SlowFrameCount = previewRuntime.DisplayCadenceSlowFrameCount,
            SlowFramePercent = previewRuntime.DisplayCadenceSlowFramePercent
        };

    private readonly record struct PreviewRuntimeCadenceProjection
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
