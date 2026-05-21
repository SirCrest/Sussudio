using System;
using System.Diagnostics;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private static double[] CopyRecentRing(double[] window, int count, int index, int maxSamples)
    {
        var take = Math.Min(Math.Max(0, maxSamples), count);
        if (take <= 0)
        {
            return Array.Empty<double>();
        }

        var result = new double[take];
        var start = (index - take + window.Length) % window.Length;
        for (var i = 0; i < take; i++)
        {
            result[i] = window[(start + i) % window.Length];
        }

        return result;
    }

    private static CpuStageTimingMetrics SummarizeCpuStageTiming(double[] samples)
    {
        if (samples.Length == 0)
        {
            return new CpuStageTimingMetrics(0, 0, 0, 0, 0);
        }

        Array.Sort(samples);
        var sum = 0.0;
        var max = 0.0;
        for (var i = 0; i < samples.Length; i++)
        {
            sum += samples[i];
            if (samples[i] > max)
            {
                max = samples[i];
            }
        }

        var p95Index = (int)Math.Ceiling((samples.Length - 1) * 0.95);
        var p99Index = (int)Math.Ceiling((samples.Length - 1) * 0.99);
        return new CpuStageTimingMetrics(
            samples.Length,
            sum / samples.Length,
            samples[Math.Clamp(p95Index, 0, samples.Length - 1)],
            samples[Math.Clamp(p99Index, 0, samples.Length - 1)],
            max);
    }

    private static double TicksToMs(long ticks)
        => ticks <= 0 ? 0 : ticks * 1000.0 / Stopwatch.Frequency;

    private static bool IsValidRenderCpuStageMs(double value)
        => value >= 0 && value <= 5000 && !double.IsNaN(value) && !double.IsInfinity(value);
}
