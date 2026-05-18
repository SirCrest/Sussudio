using System;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

internal sealed partial class FrameFingerprintCadenceTracker
{
    public readonly record struct Metrics(
        int SampleCount,
        long UniqueFrameCount,
        long DuplicateFrameCount,
        long LongestDuplicateRun,
        double InputObservedFps,
        double UniqueObservedFps,
        double DuplicateFramePercent,
        string LastHash,
        bool LastFrameDuplicate,
        string Pattern,
        double[] RecentInputIntervalsMs,
        double[] RecentUniqueIntervalsMs,
        int[] RecentDuplicateFlags);

    public static Metrics Empty { get; } = new(
        SampleCount: 0,
        UniqueFrameCount: 0,
        DuplicateFrameCount: 0,
        LongestDuplicateRun: 0,
        InputObservedFps: 0,
        UniqueObservedFps: 0,
        DuplicateFramePercent: 0,
        LastHash: string.Empty,
        LastFrameDuplicate: false,
        Pattern: "NoSamples",
        RecentInputIntervalsMs: Array.Empty<double>(),
        RecentUniqueIntervalsMs: Array.Empty<double>(),
        RecentDuplicateFlags: Array.Empty<int>());

    public Metrics GetMetrics(int maxRecentSamples = 180)
    {
        lock (_sync)
        {
            if (_sampleCount <= 0)
            {
                return Empty;
            }

            var inputIntervals = RingBufferHelpers.Copy(_inputIntervalsMs, _inputIntervalCount, _inputIntervalIndex, maxRecentSamples);
            var duplicateFlags = RingBufferHelpers.Copy(_duplicateFlags, _duplicateFlagCount, _duplicateFlagIndex, maxRecentSamples);
            var uniqueIntervals = BuildRecentUniqueIntervals(inputIntervals, duplicateFlags);
            var inputStats = ComputeStats(inputIntervals);
            var uniqueStats = ComputeStats(uniqueIntervals);
            var duplicatePercent = ComputeDuplicatePercent(duplicateFlags);

            return new Metrics(
                SampleCount: (int)Math.Min(int.MaxValue, _sampleCount),
                UniqueFrameCount: _uniqueFrameCount,
                DuplicateFrameCount: _duplicateFrameCount,
                LongestDuplicateRun: _longestDuplicateRun,
                InputObservedFps: inputStats.Average > 0 ? 1000.0 / inputStats.Average : 0,
                UniqueObservedFps: uniqueStats.Average > 0 ? 1000.0 / uniqueStats.Average : 0,
                DuplicateFramePercent: duplicatePercent,
                LastHash: _hasLastHash ? _lastHash.ToString("X16") : string.Empty,
                LastFrameDuplicate: _lastFrameDuplicate,
                Pattern: ResolvePattern(_sampleCount, duplicatePercent, duplicateFlags),
                RecentInputIntervalsMs: inputIntervals,
                RecentUniqueIntervalsMs: uniqueIntervals,
                RecentDuplicateFlags: duplicateFlags);
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

    private static double ComputeDuplicatePercent(int[] duplicateFlags)
    {
        if (duplicateFlags.Length <= 1)
        {
            return 0;
        }

        var duplicates = 0;
        for (var i = 0; i < duplicateFlags.Length; i++)
        {
            duplicates += duplicateFlags[i] != 0 ? 1 : 0;
        }

        return duplicates * 100.0 / duplicateFlags.Length;
    }

    private static double[] BuildRecentUniqueIntervals(double[] inputIntervals, int[] duplicateFlags)
    {
        if (inputIntervals.Length == 0 || duplicateFlags.Length <= 1)
        {
            return Array.Empty<double>();
        }

        var result = new double[Math.Min(inputIntervals.Length, duplicateFlags.Length)];
        var count = 0;
        var accumulatedMs = 0.0;
        var intervalOffset = Math.Max(0, inputIntervals.Length - (duplicateFlags.Length - 1));
        for (var i = 1; i < duplicateFlags.Length; i++)
        {
            var intervalIndex = intervalOffset + i - 1;
            if (intervalIndex >= inputIntervals.Length)
            {
                break;
            }

            accumulatedMs += inputIntervals[intervalIndex];
            if (duplicateFlags[i] != 0)
            {
                continue;
            }

            if (accumulatedMs > 0)
            {
                result[count++] = accumulatedMs;
                accumulatedMs = 0;
            }
        }

        if (accumulatedMs > 0)
        {
            result[count++] = accumulatedMs;
        }

        if (count == 0)
        {
            return Array.Empty<double>();
        }

        Array.Resize(ref result, count);
        return result;
    }

    private static string ResolvePattern(long samples, double duplicatePercent, int[] duplicateFlags)
    {
        if (samples < 8 || duplicateFlags.Length < 8)
        {
            return "WarmingUp";
        }

        if (duplicatePercent <= 0.1)
        {
            return "AllUnique";
        }

        if (duplicatePercent >= 90)
        {
            return "MostlyDuplicate";
        }

        var trailingDuplicateRun = 0;
        for (var i = duplicateFlags.Length - 1; i >= 0 && duplicateFlags[i] != 0; i--)
        {
            trailingDuplicateRun++;
        }

        if (trailingDuplicateRun >= 12)
        {
            return "DuplicateRun";
        }

        var transitions = 0;
        for (var i = 1; i < duplicateFlags.Length; i++)
        {
            if (duplicateFlags[i] != duplicateFlags[i - 1])
            {
                transitions++;
            }
        }

        var transitionRatio = transitions / (double)Math.Max(1, duplicateFlags.Length - 1);
        if (duplicatePercent is >= 40 and <= 60 && transitionRatio >= 0.8)
        {
            return "AlternatingDuplicate";
        }

        return "Mixed";
    }
}
