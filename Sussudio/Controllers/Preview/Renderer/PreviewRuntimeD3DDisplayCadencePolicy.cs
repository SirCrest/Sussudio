using System;
using Sussudio.Services.Preview;

namespace Sussudio.Controllers;

internal readonly record struct PreviewRuntimeD3DDisplayCadence(
    int SampleCount,
    double ObservedFps,
    double ExpectedIntervalMs,
    double AverageIntervalMs,
    double P95IntervalMs,
    double P99IntervalMs,
    double MaxIntervalMs,
    double OnePercentLowFps,
    double FivePercentLowFps,
    double SampleDurationMs,
    double[] RecentIntervalsMs,
    double JitterStdDevMs,
    long SlowFrameCount,
    double SlowFramePercent);

internal static class PreviewRuntimeD3DDisplayCadencePolicy
{
    public static PreviewRuntimeD3DDisplayCadence Evaluate(
        D3D11PreviewRenderer? d3d,
        double previewMinPresentationIntervalMs)
    {
        var displayCadence = d3d?.GetPresentCadenceMetrics(previewMinPresentationIntervalMs);

        return new PreviewRuntimeD3DDisplayCadence(
            SampleCount: displayCadence?.SampleCount ?? 0,
            ObservedFps: displayCadence?.ObservedFps ?? 0,
            ExpectedIntervalMs: displayCadence?.ExpectedIntervalMs ?? 0,
            AverageIntervalMs: displayCadence?.AverageIntervalMs ?? 0,
            P95IntervalMs: displayCadence?.P95IntervalMs ?? 0,
            P99IntervalMs: displayCadence?.P99IntervalMs ?? 0,
            MaxIntervalMs: displayCadence?.MaxIntervalMs ?? 0,
            OnePercentLowFps: displayCadence?.OnePercentLowFps ?? 0,
            FivePercentLowFps: displayCadence?.FivePercentLowFps ?? 0,
            SampleDurationMs: displayCadence?.SampleDurationMs ?? 0,
            RecentIntervalsMs: displayCadence?.RecentIntervalsMs ?? Array.Empty<double>(),
            JitterStdDevMs: displayCadence?.JitterStdDevMs ?? 0,
            SlowFrameCount: displayCadence?.SlowFrameCount ?? 0,
            SlowFramePercent: displayCadence?.SlowFramePercent ?? 0);
    }
}
