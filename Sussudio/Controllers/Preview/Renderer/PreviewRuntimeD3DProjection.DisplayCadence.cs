using System;

namespace Sussudio.Controllers;

internal sealed partial class PreviewRuntimeD3DProjection
{
    public int DisplayCadenceSampleCount { get; private set; }
    public double DisplayCadenceObservedFps { get; private set; }
    public double DisplayCadenceExpectedIntervalMs { get; private set; }
    public double DisplayCadenceAverageIntervalMs { get; private set; }
    public double DisplayCadenceP95IntervalMs { get; private set; }
    public double DisplayCadenceP99IntervalMs { get; private set; }
    public double DisplayCadenceMaxIntervalMs { get; private set; }
    public double DisplayCadenceOnePercentLowFps { get; private set; }
    public double DisplayCadenceFivePercentLowFps { get; private set; }
    public double DisplayCadenceSampleDurationMs { get; private set; }
    public double[] DisplayCadenceRecentIntervalsMs { get; private set; } = Array.Empty<double>();
    public double DisplayCadenceJitterStdDevMs { get; private set; }
    public long DisplayCadenceSlowFrameCount { get; private set; }
    public double DisplayCadenceSlowFramePercent { get; private set; }

    private void ApplyDisplayCadence(PreviewRuntimeD3DDisplayCadence displayCadence)
    {
        DisplayCadenceSampleCount = displayCadence.SampleCount;
        DisplayCadenceObservedFps = displayCadence.ObservedFps;
        DisplayCadenceExpectedIntervalMs = displayCadence.ExpectedIntervalMs;
        DisplayCadenceAverageIntervalMs = displayCadence.AverageIntervalMs;
        DisplayCadenceP95IntervalMs = displayCadence.P95IntervalMs;
        DisplayCadenceP99IntervalMs = displayCadence.P99IntervalMs;
        DisplayCadenceMaxIntervalMs = displayCadence.MaxIntervalMs;
        DisplayCadenceOnePercentLowFps = displayCadence.OnePercentLowFps;
        DisplayCadenceFivePercentLowFps = displayCadence.FivePercentLowFps;
        DisplayCadenceSampleDurationMs = displayCadence.SampleDurationMs;
        DisplayCadenceRecentIntervalsMs = displayCadence.RecentIntervalsMs;
        DisplayCadenceJitterStdDevMs = displayCadence.JitterStdDevMs;
        DisplayCadenceSlowFrameCount = displayCadence.SlowFrameCount;
        DisplayCadenceSlowFramePercent = displayCadence.SlowFramePercent;
    }
}
