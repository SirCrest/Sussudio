using System;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private readonly object _presentCadenceLock = new();
    private double[] _presentIntervalWindowMs = new double[1200];
    private int _presentIntervalCount;
    private int _presentIntervalIndex;

    public PresentCadenceMetrics GetPresentCadenceMetrics(double expectedIntervalMs)
    {
        double[] samples;
        lock (_presentCadenceLock)
        {
            if (_presentIntervalCount <= 0)
            {
                return new PresentCadenceMetrics(
                    SampleCount: 0,
                    ObservedFps: 0,
                    ExpectedIntervalMs: expectedIntervalMs,
                    AverageIntervalMs: 0,
                    P95IntervalMs: 0,
                    P99IntervalMs: 0,
                    MaxIntervalMs: 0,
                    OnePercentLowFps: 0,
                    FivePercentLowFps: 0,
                    SampleDurationMs: 0,
                    RecentIntervalsMs: Array.Empty<double>(),
                    JitterStdDevMs: 0,
                    SlowFrameCount: 0,
                    SlowFramePercent: 0);
            }

            samples = new double[_presentIntervalCount];
            for (var i = 0; i < _presentIntervalCount; i++)
            {
                var ringIndex = (_presentIntervalIndex - _presentIntervalCount + i + _presentIntervalWindowMs.Length)
                    % _presentIntervalWindowMs.Length;
                samples[i] = _presentIntervalWindowMs[ringIndex];
            }
        }

        var sampleCount = samples.Length;
        var sum = 0.0;
        var max = 0.0;
        for (var i = 0; i < sampleCount; i++)
        {
            sum += samples[i];
            if (samples[i] > max)
            {
                max = samples[i];
            }
        }

        var average = sum / sampleCount;
        var observedFps = average > double.Epsilon ? 1000.0 / average : 0;
        var targetIntervalMs = expectedIntervalMs > 0 ? expectedIntervalMs : average;
        var slowThresholdMs = targetIntervalMs * 1.6;

        long slowFrameCount = 0;
        var varianceSum = 0.0;
        for (var i = 0; i < sampleCount; i++)
        {
            var delta = samples[i] - average;
            varianceSum += delta * delta;
            if (samples[i] >= slowThresholdMs)
            {
                slowFrameCount++;
            }
        }

        var jitterStdDevMs = Math.Sqrt(varianceSum / sampleCount);
        var sorted = (double[])samples.Clone();
        Array.Sort(sorted);
        var p95Index = (int)Math.Ceiling((sorted.Length - 1) * 0.95);
        var p95IntervalMs = sorted[Math.Clamp(p95Index, 0, sorted.Length - 1)];
        var p99Index = (int)Math.Ceiling((sorted.Length - 1) * 0.99);
        var p99IntervalMs = sorted[Math.Clamp(p99Index, 0, sorted.Length - 1)];
        var onePercentLowFps = p99IntervalMs > double.Epsilon ? 1000.0 / p99IntervalMs : 0;
        var fivePercentLowFps = p95IntervalMs > double.Epsilon ? 1000.0 / p95IntervalMs : 0;
        var slowPercent = slowFrameCount <= 0
            ? 0
            : (double)slowFrameCount / Math.Max(1, sampleCount) * 100.0;

        return new PresentCadenceMetrics(
            SampleCount: sampleCount,
            ObservedFps: observedFps,
            ExpectedIntervalMs: targetIntervalMs,
            AverageIntervalMs: average,
            P95IntervalMs: p95IntervalMs,
            P99IntervalMs: p99IntervalMs,
            MaxIntervalMs: max,
            OnePercentLowFps: onePercentLowFps,
            FivePercentLowFps: fivePercentLowFps,
            SampleDurationMs: sum,
            RecentIntervalsMs: samples,
            JitterStdDevMs: jitterStdDevMs,
            SlowFrameCount: slowFrameCount,
            SlowFramePercent: slowPercent);
    }

    public double[] GetRecentPresentIntervalsMs(int maxSamples)
    {
        lock (_presentCadenceLock)
        {
            return CopyRecentRing(_presentIntervalWindowMs, _presentIntervalCount, _presentIntervalIndex, maxSamples);
        }
    }
}
