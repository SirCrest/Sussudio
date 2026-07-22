using System;
using System.Buffers.Binary;
using System.Diagnostics;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

// Hashes compressed/source frame bytes to detect duplicate cadence patterns
// before decode. It is intentionally approximate: the goal is fast evidence of
// repeated packets, not cryptographic identity.
internal sealed class FrameFingerprintCadenceTracker
{
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

    private void AddFlagSample(int value) =>
        RingBufferHelpers.Add(_duplicateFlags, ref _duplicateFlagCount, ref _duplicateFlagIndex, value);

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

    private static void AddTimingSample(double[] window, ref int count, ref int index, double value)
    {
        if (!double.IsFinite(value) || value < 0 || value > 5000)
        {
            return;
        }

        RingBufferHelpers.Add(window, ref count, ref index, value);
    }

    private static double ElapsedMs(long startTick, long endTick)
        => (endTick - startTick) * 1000.0 / Stopwatch.Frequency;

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
        return (sum / values.Length, PercentileHelpers.FromSorted(sorted, 0.95));
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

// Samples luma from rendered frames to estimate what the viewer actually sees.
// This detects repeated visual output even when source frames keep arriving,
// which is why it complements source-reader and renderer-submit counters.
internal sealed class VisualCadenceTracker
{
    private const int DefaultSampleColumns = 640;
    private const int DefaultSampleRows = 360;
    private const int WindowSize = 720;

    private readonly object _sync = new();
    private readonly int _sampleColumns;
    private readonly int _sampleRows;
    private readonly int _sampleSize;
    private readonly double _cropLeft;
    private readonly double _cropTop;
    private readonly double _cropWidth;
    private readonly double _cropHeight;
    private byte[] _lastSample;
    private byte[] _currentSample;
    private readonly double[] _outputIntervalsMs = new double[WindowSize];
    private readonly double[] _changeIntervalsMs = new double[WindowSize];
    private readonly double[] _deltaWindow = new double[WindowSize];

    private int _outputIntervalCount;
    private int _outputIntervalIndex;
    private int _changeIntervalCount;
    private int _changeIntervalIndex;
    private int _deltaCount;
    private int _deltaIndex;
    private bool _hasLastSample;
    private long _lastOutputTick;
    private long _lastChangeTick;
    private long _sampleCount;
    private long _changedFrameCount;
    private long _repeatFrameCount;
    private long _currentRepeatRun;
    private long _longestRepeatRun;
    private double _lastDelta;
    private int _lastSampleLength;
    private int _lastBytesPerLuma;

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

    private readonly record struct LumaSample(int Length, double ChangedPixels);

    public VisualCadenceTracker(
        int sampleColumns = DefaultSampleColumns,
        int sampleRows = DefaultSampleRows,
        double cropLeft = 0,
        double cropTop = 0,
        double cropWidth = 1,
        double cropHeight = 1)
    {
        _sampleColumns = Math.Max(1, sampleColumns);
        _sampleRows = Math.Max(1, sampleRows);
        _sampleSize = _sampleColumns * _sampleRows;
        _cropLeft = Math.Clamp(cropLeft, 0, 0.999);
        _cropTop = Math.Clamp(cropTop, 0, 0.999);
        _cropWidth = Math.Clamp(cropWidth, 0.001, 1.0 - _cropLeft);
        _cropHeight = Math.Clamp(cropHeight, 0.001, 1.0 - _cropTop);
        _lastSample = new byte[_sampleSize * 2];
        _currentSample = new byte[_sampleSize * 2];
    }

    public void Reset()
    {
        lock (_sync)
        {
            Array.Clear(_lastSample, 0, _lastSample.Length);
            Array.Clear(_currentSample, 0, _currentSample.Length);
            Array.Clear(_outputIntervalsMs, 0, _outputIntervalsMs.Length);
            Array.Clear(_changeIntervalsMs, 0, _changeIntervalsMs.Length);
            Array.Clear(_deltaWindow, 0, _deltaWindow.Length);
            _outputIntervalCount = 0;
            _outputIntervalIndex = 0;
            _changeIntervalCount = 0;
            _changeIntervalIndex = 0;
            _deltaCount = 0;
            _deltaIndex = 0;
            _hasLastSample = false;
            _lastOutputTick = 0;
            _lastChangeTick = 0;
            _sampleCount = 0;
            _changedFrameCount = 0;
            _repeatFrameCount = 0;
            _currentRepeatRun = 0;
            _longestRepeatRun = 0;
            _lastDelta = 0;
            _lastSampleLength = 0;
            _lastBytesPerLuma = 0;
        }
    }

    public void RecordFrame(
        ReadOnlySpan<byte> frame,
        int width,
        int height,
        PooledVideoPixelFormat pixelFormat,
        long timestampTick = 0)
    {
        if (frame.IsEmpty || width <= 0 || height <= 0)
        {
            return;
        }

        var bytesPerLuma = pixelFormat == PooledVideoPixelFormat.P010 ? 2 : 1;
        var requiredLumaBytes = width * height * bytesPerLuma;
        if (requiredLumaBytes <= 0 || frame.Length < requiredLumaBytes)
        {
            if (frame.Length >= width * height)
            {
                bytesPerLuma = 1;
                requiredLumaBytes = width * height;
            }
            else
            {
                return;
            }
        }

        var nowTick = timestampTick > 0 ? timestampTick : Stopwatch.GetTimestamp();
        lock (_sync)
        {
            var sample = SampleLumaAndCompare(frame, width, height, bytesPerLuma, _currentSample, _hasLastSample ? _lastSample : null);
            var sampleLength = sample.Length;
            if (sampleLength <= 0)
            {
                return;
            }

            if (_lastOutputTick > 0)
            {
                AddTimingSample(_outputIntervalsMs, ref _outputIntervalCount, ref _outputIntervalIndex, ElapsedMs(_lastOutputTick, nowTick));
            }

            _lastOutputTick = nowTick;
            _sampleCount++;

            if (!_hasLastSample)
            {
                PromoteCurrentSample(sampleLength, bytesPerLuma);
                _hasLastSample = true;
                _changedFrameCount++;
                _lastChangeTick = nowTick;
                return;
            }

            var sampleShapeChanged = sampleLength != _lastSampleLength || bytesPerLuma != _lastBytesPerLuma;
            var delta = sampleShapeChanged
                ? Math.Max(
                    sampleLength / Math.Max(1, bytesPerLuma),
                    _lastSampleLength / Math.Max(1, _lastBytesPerLuma))
                : sample.ChangedPixels;
            _lastDelta = delta;
            AddValueSample(_deltaWindow, ref _deltaCount, ref _deltaIndex, delta);

            if (delta > 0)
            {
                if (_lastChangeTick > 0)
                {
                    AddTimingSample(_changeIntervalsMs, ref _changeIntervalCount, ref _changeIntervalIndex, ElapsedMs(_lastChangeTick, nowTick));
                }

                _lastChangeTick = nowTick;
                _changedFrameCount++;
                _currentRepeatRun = 0;
                PromoteCurrentSample(sampleLength, bytesPerLuma);
                return;
            }

            _repeatFrameCount++;
            _currentRepeatRun++;
            if (_currentRepeatRun > _longestRepeatRun)
            {
                _longestRepeatRun = _currentRepeatRun;
            }
        }
    }

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

    private LumaSample SampleLumaAndCompare(
        ReadOnlySpan<byte> frame,
        int width,
        int height,
        int bytesPerLuma,
        byte[] destination,
        byte[]? previous)
    {
        var cropX = Math.Clamp((int)Math.Round(width * _cropLeft), 0, Math.Max(0, width - 1));
        var cropY = Math.Clamp((int)Math.Round(height * _cropTop), 0, Math.Max(0, height - 1));
        var cropWidth = Math.Clamp((int)Math.Round(width * _cropWidth), 1, width - cropX);
        var cropHeight = Math.Clamp((int)Math.Round(height * _cropHeight), 1, height - cropY);
        var sampleWidth = Math.Min(_sampleColumns, cropWidth);
        var sampleHeight = Math.Min(_sampleRows, cropHeight);
        var sampleX = cropX + Math.Max(0, (cropWidth - sampleWidth) / 2);
        var sampleY = cropY + Math.Max(0, (cropHeight - sampleHeight) / 2);
        var index = 0;
        var changed = 0;
        for (var row = 0; row < sampleHeight; row++)
        {
            var y = sampleY + row;
            var rowOffset = y * width * bytesPerLuma;
            for (var col = 0; col < sampleWidth; col++)
            {
                var x = sampleX + col;
                var lumaOffset = rowOffset + x * bytesPerLuma;
                var changedPixel = false;
                var luma = frame[lumaOffset];
                destination[index] = luma;
                if (previous != null && previous[index] != luma)
                {
                    changedPixel = true;
                }

                index++;
                if (bytesPerLuma == 2)
                {
                    var secondLuma = lumaOffset + 1 < frame.Length
                        ? frame[lumaOffset + 1]
                        : (byte)0;
                    destination[index] = secondLuma;
                    if (previous != null && previous[index] != secondLuma)
                    {
                        changedPixel = true;
                    }

                    index++;
                }

                if (changedPixel)
                {
                    changed++;
                }
            }
        }

        return new LumaSample(index, changed);
    }

    private void PromoteCurrentSample(int sampleLength, int bytesPerLuma)
    {
        var oldLast = _lastSample;
        _lastSample = _currentSample;
        _currentSample = oldLast;
        _lastSampleLength = sampleLength;
        _lastBytesPerLuma = bytesPerLuma;
    }

    private static void AddTimingSample(double[] window, ref int count, ref int index, double value)
    {
        if (!double.IsFinite(value) || value < 0 || value > 5000)
        {
            return;
        }

        RingBufferHelpers.Add(window, ref count, ref index, value);
    }

    private static void AddValueSample(double[] window, ref int count, ref int index, double value)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            return;
        }

        RingBufferHelpers.Add(window, ref count, ref index, value);
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
        return (sum / values.Length, PercentileHelpers.FromSorted(sorted, 0.95));
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

    private static double ElapsedMs(long startTick, long endTick)
        => (endTick - startTick) * 1000.0 / Stopwatch.Frequency;
}
