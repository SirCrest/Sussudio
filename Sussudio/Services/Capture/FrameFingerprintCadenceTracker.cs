using System;
using System.Buffers.Binary;
using System.Diagnostics;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

// Hashes compressed/source frame bytes to detect duplicate cadence patterns
// before decode. It is intentionally approximate: the goal is fast evidence of
// repeated packets, not cryptographic identity.
internal sealed partial class FrameFingerprintCadenceTracker
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
}
