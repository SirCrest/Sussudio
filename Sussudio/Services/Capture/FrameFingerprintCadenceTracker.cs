using System;
using System.Buffers.Binary;
using System.Diagnostics;

namespace ElgatoCapture.Services.Capture;

internal sealed class FrameFingerprintCadenceTracker
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

    private const int WindowSize = 720;
    private const ulong HashOffset = 14695981039346656037UL;
    private const ulong HashPrime = 1099511628211UL;
    private const int HashRegionBytes = 4096;

    private readonly object _sync = new();
    private readonly double[] _inputIntervalsMs = new double[WindowSize];
    private readonly double[] _uniqueIntervalsMs = new double[WindowSize];
    private readonly int[] _duplicateFlags = new int[WindowSize];

    private int _inputIntervalCount;
    private int _inputIntervalIndex;
    private int _uniqueIntervalCount;
    private int _uniqueIntervalIndex;
    private int _duplicateFlagCount;
    private int _duplicateFlagIndex;
    private bool _hasLastHash;
    private ulong _lastHash;
    private long _lastInputTick;
    private long _lastUniqueTick;
    private long _sampleCount;
    private long _uniqueFrameCount;
    private long _duplicateFrameCount;
    private long _currentDuplicateRun;
    private long _longestDuplicateRun;
    private bool _lastFrameDuplicate;

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

    public void Reset()
    {
        lock (_sync)
        {
            Array.Clear(_inputIntervalsMs, 0, _inputIntervalsMs.Length);
            Array.Clear(_uniqueIntervalsMs, 0, _uniqueIntervalsMs.Length);
            Array.Clear(_duplicateFlags, 0, _duplicateFlags.Length);
            _inputIntervalCount = 0;
            _inputIntervalIndex = 0;
            _uniqueIntervalCount = 0;
            _uniqueIntervalIndex = 0;
            _duplicateFlagCount = 0;
            _duplicateFlagIndex = 0;
            _hasLastHash = false;
            _lastHash = 0;
            _lastInputTick = 0;
            _lastUniqueTick = 0;
            _sampleCount = 0;
            _uniqueFrameCount = 0;
            _duplicateFrameCount = 0;
            _currentDuplicateRun = 0;
            _longestDuplicateRun = 0;
            _lastFrameDuplicate = false;
        }
    }

    public void RecordFrame(ulong hash, long timestampTick = 0)
    {
        var nowTick = timestampTick > 0 ? timestampTick : Stopwatch.GetTimestamp();
        lock (_sync)
        {
            if (_lastInputTick > 0)
            {
                AddTimingSample(_inputIntervalsMs, ref _inputIntervalCount, ref _inputIntervalIndex, ElapsedMs(_lastInputTick, nowTick));
            }

            _lastInputTick = nowTick;
            _sampleCount++;

            var duplicate = _hasLastHash && hash == _lastHash;
            _lastFrameDuplicate = duplicate;
            AddFlagSample(duplicate ? 1 : 0);

            if (duplicate)
            {
                _duplicateFrameCount++;
                _currentDuplicateRun++;
                if (_currentDuplicateRun > _longestDuplicateRun)
                {
                    _longestDuplicateRun = _currentDuplicateRun;
                }

                return;
            }

            if (_lastUniqueTick > 0)
            {
                AddTimingSample(_uniqueIntervalsMs, ref _uniqueIntervalCount, ref _uniqueIntervalIndex, ElapsedMs(_lastUniqueTick, nowTick));
            }

            _lastUniqueTick = nowTick;
            _lastHash = hash;
            _hasLastHash = true;
            _uniqueFrameCount++;
            _currentDuplicateRun = 0;
        }
    }

    public Metrics GetMetrics(int maxRecentSamples = 180)
    {
        lock (_sync)
        {
            if (_sampleCount <= 0)
            {
                return Empty;
            }

            var inputIntervals = CopyRing(_inputIntervalsMs, _inputIntervalCount, _inputIntervalIndex, maxRecentSamples);
            var duplicateFlags = CopyRing(_duplicateFlags, _duplicateFlagCount, _duplicateFlagIndex, maxRecentSamples);
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

    public static ulong ComputeHash(ReadOnlySpan<byte> data)
    {
        var hash = HashOffset ^ (uint)data.Length;
        if (data.Length <= HashRegionBytes * 3)
        {
            return HashBytes(hash, data);
        }

        hash = HashBytes(hash, data.Slice(0, HashRegionBytes));
        var middleStart = Math.Max(HashRegionBytes, (data.Length / 2) - (HashRegionBytes / 2));
        hash = HashBytes(hash, data.Slice(middleStart, HashRegionBytes));
        hash = HashBytes(hash, data.Slice(data.Length - HashRegionBytes, HashRegionBytes));
        return hash;
    }

    private static ulong HashBytes(ulong initialHash, ReadOnlySpan<byte> data)
    {
        var hash = initialHash;
        var offset = 0;
        while (offset + sizeof(ulong) <= data.Length)
        {
            hash ^= BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(offset, sizeof(ulong)));
            hash *= HashPrime;
            offset += sizeof(ulong);
        }

        while (offset < data.Length)
        {
            hash ^= data[offset];
            hash *= HashPrime;
            offset++;
        }

        return hash;
    }

    private void AddFlagSample(int value)
    {
        _duplicateFlags[_duplicateFlagIndex] = value;
        _duplicateFlagIndex = (_duplicateFlagIndex + 1) % _duplicateFlags.Length;
        if (_duplicateFlagCount < _duplicateFlags.Length)
        {
            _duplicateFlagCount++;
        }
    }

    private static void AddTimingSample(double[] window, ref int count, ref int index, double value)
    {
        if (!double.IsFinite(value) || value < 0 || value > 5000)
        {
            return;
        }

        window[index] = value;
        index = (index + 1) % window.Length;
        if (count < window.Length)
        {
            count++;
        }
    }

    private static double[] CopyRing(double[] window, int count, int index, int maxCount)
    {
        var take = Math.Min(Math.Max(0, maxCount), count);
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

    private static int[] CopyRing(int[] window, int count, int index, int maxCount)
    {
        var take = Math.Min(Math.Max(0, maxCount), count);
        if (take <= 0)
        {
            return Array.Empty<int>();
        }

        var result = new int[take];
        var start = (index - take + window.Length) % window.Length;
        for (var i = 0; i < take; i++)
        {
            result[i] = window[(start + i) % window.Length];
        }

        return result;
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

    private static double ElapsedMs(long startTick, long endTick)
        => (endTick - startTick) * 1000.0 / Stopwatch.Frequency;
}
