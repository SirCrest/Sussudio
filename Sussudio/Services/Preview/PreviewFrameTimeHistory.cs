using System;
using System.Diagnostics;
using System.Threading;

namespace Sussudio.Services.Preview;

[Flags]
internal enum PreviewFrameTimeSampleFlags
{
    None = 0,
    GapBefore = 1,
    CountForPresentCadence = 2
}

internal readonly record struct PreviewFrameTimeSample(
    long Epoch,
    long Sequence,
    long TimestampQpc,
    double IntervalMs,
    PreviewFrameTimeSampleFlags Flags)
{
    public bool IsCadenceSample =>
        (Flags & PreviewFrameTimeSampleFlags.CountForPresentCadence) != 0 &&
        double.IsFinite(IntervalMs) && IntervalMs > 0;
}

internal readonly record struct PreviewFrameTimeCursor(long Epoch, long Sequence);

internal readonly record struct PreviewFrameTimeHistoryRead(
    int Count,
    PreviewFrameTimeCursor Cursor,
    bool GapBeforeFirst,
    long LastTimestampQpc);

// The owner synchronizes reads and writes. Entries contain measurements only;
// keeping this history never retains a capture frame, texture, or sample lease.
internal sealed class PreviewFrameTimeHistory
{
    public const int DefaultCapacity = 4096;
    public const double WindowSeconds = 10;

    private static long _nextEpoch;
    private readonly PreviewFrameTimeSample[] _samples;
    private readonly long _windowTicks;
    private int _start;
    private int _count;
    private long _epoch;
    private long _sequence;
    private long _lastTimestampQpc;

    public PreviewFrameTimeHistory(int capacity = DefaultCapacity, long? frequency = null)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        Frequency = frequency ?? Stopwatch.Frequency;
        if (Frequency <= 0 || Frequency > long.MaxValue / (long)WindowSeconds)
            throw new ArgumentOutOfRangeException(nameof(frequency));
        _samples = new PreviewFrameTimeSample[capacity];
        _windowTicks = Frequency * (long)WindowSeconds;
        Reset();
    }

    public long Frequency { get; }
    public int Count => _count;
    public int Capacity => _samples.Length;

    public void Reset()
    {
        _epoch = Interlocked.Increment(ref _nextEpoch);
        _start = 0;
        _count = 0;
        _sequence = 0;
        _lastTimestampQpc = 0;
    }

    public PreviewFrameTimeSample Append(long timestampQpc, double intervalMs,
        PreviewFrameTimeSampleFlags flags = PreviewFrameTimeSampleFlags.CountForPresentCadence)
    {
        if (timestampQpc <= 0) return default;
        if (_lastTimestampQpc > 0 && timestampQpc < _lastTimestampQpc)
        {
            Reset();
            flags |= PreviewFrameTimeSampleFlags.GapBefore;
        }
        if (!double.IsFinite(intervalMs) || intervalMs <= 0)
        {
            intervalMs = 0;
            flags = (flags | PreviewFrameTimeSampleFlags.GapBefore) &
                ~PreviewFrameTimeSampleFlags.CountForPresentCadence;
        }

        var oldestAllowed = timestampQpc - _windowTicks;
        while (_count > 0 && _samples[_start].TimestampQpc < oldestAllowed)
            RemoveOldest();
        if (_count == _samples.Length) RemoveOldest();

        var sample = new PreviewFrameTimeSample(_epoch, ++_sequence, timestampQpc, intervalMs, flags);
        _samples[(_start + _count) % _samples.Length] = sample;
        _count++;
        _lastTimestampQpc = timestampQpc;
        return sample;
    }

    public PreviewFrameTimeHistoryRead CopyAfter(
        PreviewFrameTimeCursor cursor, Span<PreviewFrameTimeSample> destination)
    {
        var changedEpoch = cursor.Epoch != _epoch;
        if (_count == 0)
            return new(0, new(_epoch, _sequence), changedEpoch, _lastTimestampQpc);

        var oldestSequence = _samples[_start].Sequence;
        var lostSamples = !changedEpoch &&
            (cursor.Sequence < oldestSequence - 1 || cursor.Sequence > _sequence);
        var afterSequence = changedEpoch || lostSamples ? oldestSequence - 1 : cursor.Sequence;
        var available = (int)Math.Min(_count, _sequence - afterSequence);
        var count = Math.Min(destination.Length, available);
        var offset = (int)(afterSequence - oldestSequence + 1);
        for (var i = 0; i < count; i++)
            destination[i] = _samples[(_start + offset + i) % _samples.Length];

        return new(count, new(_epoch, afterSequence + count), changedEpoch || lostSamples, _lastTimestampQpc);
    }

    private void RemoveOldest()
    {
        _start = (_start + 1) % _samples.Length;
        _count--;
    }
}
