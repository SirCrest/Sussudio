using System;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

internal sealed partial class VisualCadenceTracker
{
    public readonly record struct Metrics(
        int SampleCount,
        long ChangedFrameCount,
        long RepeatFrameCount,
        long LongestRepeatRun,
        double OutputObservedFps,
        double ChangeObservedFps,
        double RepeatFramePercent,
        double LastDelta,
        double AverageDelta,
        double P95Delta,
        double MotionScore,
        string MotionConfidence,
        double[] RecentOutputIntervalsMs,
        double[] RecentChangeIntervalsMs);

    public static Metrics Empty { get; } = new(
        SampleCount: 0,
        ChangedFrameCount: 0,
        RepeatFrameCount: 0,
        LongestRepeatRun: 0,
        OutputObservedFps: 0,
        ChangeObservedFps: 0,
        RepeatFramePercent: 0,
        LastDelta: 0,
        AverageDelta: 0,
        P95Delta: 0,
        MotionScore: 0,
        MotionConfidence: "NoSamples",
        RecentOutputIntervalsMs: Array.Empty<double>(),
        RecentChangeIntervalsMs: Array.Empty<double>());

    public Metrics GetMetrics(int maxRecentIntervals = 180)
    {
        lock (_sync)
        {
            if (_sampleCount <= 0)
            {
                return Empty;
            }

            var outputIntervals = RingBufferHelpers.Copy(_outputIntervalsMs, _outputIntervalCount, _outputIntervalIndex, maxRecentIntervals);
            var changeIntervals = RingBufferHelpers.Copy(_changeIntervalsMs, _changeIntervalCount, _changeIntervalIndex, maxRecentIntervals);
            var deltas = RingBufferHelpers.Copy(_deltaWindow, _deltaCount, _deltaIndex, WindowSize);
            var deltaStats = ComputeStats(deltas);
            var outputStats = ComputeStats(outputIntervals);
            var changeStats = ComputeStats(changeIntervals);
            var repeatPercent = _sampleCount > 1
                ? _repeatFrameCount * 100.0 / Math.Max(1, _sampleCount - 1)
                : 0;
            var motionScore = Math.Clamp(deltaStats.Average / Math.Max(1, _sampleSize) * 100.0, 0.0, 100.0);
            var motionConfidence = ResolveMotionConfidence(_sampleCount, deltaStats.Average, repeatPercent, changeIntervals.Length);

            return new Metrics(
                SampleCount: (int)Math.Min(int.MaxValue, _sampleCount),
                ChangedFrameCount: _changedFrameCount,
                RepeatFrameCount: _repeatFrameCount,
                LongestRepeatRun: _longestRepeatRun,
                OutputObservedFps: outputStats.Average > 0 ? 1000.0 / outputStats.Average : 0,
                ChangeObservedFps: changeStats.Average > 0 ? 1000.0 / changeStats.Average : 0,
                RepeatFramePercent: repeatPercent,
                LastDelta: _lastDelta,
                AverageDelta: deltaStats.Average,
                P95Delta: deltaStats.P95,
                MotionScore: motionScore,
                MotionConfidence: motionConfidence,
                RecentOutputIntervalsMs: outputIntervals,
                RecentChangeIntervalsMs: changeIntervals);
        }
    }

    private static (double Average, double P95) ComputeStats(double[] values)
    {
        if (values.Length == 0)
        {
            return (0, 0);
        }

        var sum = 0.0;
        for (var i = 0; i < values.Length; i++)
        {
            sum += values[i];
        }

        var sorted = (double[])values.Clone();
        Array.Sort(sorted);
        var p95Index = Math.Clamp((int)Math.Ceiling((sorted.Length - 1) * 0.95), 0, sorted.Length - 1);
        return (sum / values.Length, sorted[p95Index]);
    }

    private static string ResolveMotionConfidence(long samples, double averageDelta, double repeatPercent, int changeIntervalCount)
    {
        if (samples < 8)
        {
            return "WarmingUp";
        }

        if (changeIntervalCount < 4 || averageDelta <= 0 || repeatPercent > 90)
        {
            return "LowMotion";
        }

        if (averageDelta >= 1024 && repeatPercent < 35)
        {
            return "HighMotion";
        }

        return "ModerateMotion";
    }
}
