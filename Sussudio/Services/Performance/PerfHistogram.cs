using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading;

namespace Sussudio.Services.Performance;

/// <summary>
/// Lock-free log-linear histogram of non-negative <see cref="long"/> values (microseconds for time, bytes or
/// percent otherwise). Values 0-63 have exact buckets; above that each power of two is split into 32 sub-buckets,
/// so a bucket is at most 1/32 of its lower bound wide and its midpoint is within 1.6 % of any value it holds.
/// Recording never throws, never allocates and is safe from any number of threads.
/// Contract: docs/architecture/performance-telemetry.md section 2.1.
/// </summary>
public sealed class PerfHistogram
{
    /// <summary>Values below this have one exact bucket each.</summary>
    public const int ExactBucketCount = 64;

    /// <summary>Sub-buckets per power of two above the exact range (log2 of the count is <see cref="SubBucketBits"/>).</summary>
    public const int SubBucketCount = 32;

    private const int SubBucketBits = 5;
    private const int FirstLogOctave = 6;   // 2^6 = 64, the first value without an exact bucket.
    private const int LastLogOctave = 31;   // 2^31 .. 2^32-1 is the last octave.

    /// <summary>Total number of buckets: 64 exact plus 26 octaves of 32 sub-buckets.</summary>
    public const int BucketCount = ExactBucketCount + ((LastLogOctave - FirstLogOctave) + 1) * SubBucketCount;

    /// <summary>Largest value with its own bucket; larger values clamp into the top bucket.</summary>
    public const long MaxRecordableValue = (1L << 32) - 1;

    private static readonly double TicksToMicroseconds = 1_000_000d / Stopwatch.Frequency;

    private readonly long[] _buckets = new long[BucketCount];
    private long _sum;
    private long _max;

    /// <summary>Records one value. Negative values clamp to zero, values above the range clamp into the top bucket.</summary>
    public void Record(long value)
    {
        if (value < 0)
        {
            value = 0;
        }

        // Max keeps the true value even when the bucket clamps, so an outlier beyond the range stays visible.
        if (value > Volatile.Read(ref _max))
        {
            RaiseMax(value);
        }

        var clamped = value > MaxRecordableValue ? MaxRecordableValue : value;
        Interlocked.Increment(ref _buckets[BucketIndex(clamped)]);
        Interlocked.Add(ref _sum, clamped);
    }

    /// <summary>Records a Stopwatch tick duration, converted to microseconds. Negative durations record as zero.</summary>
    public void RecordTicks(long ticks)
    {
        if (ticks <= 0)
        {
            Record(0);
            return;
        }

        // Double multiply keeps full precision for any Stopwatch.Frequency and cannot overflow for realistic spans.
        Record((long)(ticks * TicksToMicroseconds));
    }

    /// <summary>Records the Stopwatch ticks elapsed since <paramref name="startTimestamp"/> as microseconds.</summary>
    public void RecordElapsedSince(long startTimestamp)
    {
        RecordTicks(Stopwatch.GetTimestamp() - startTimestamp);
    }

    /// <summary>
    /// Copies the counts. Writers racing the copy may land in it or not; every bucket, the total and the maximum
    /// only ever grow, so successive snapshots never go backwards.
    /// </summary>
    public PerfHistogramSnapshot Snapshot()
    {
        var buckets = new long[BucketCount];
        long count = 0;
        for (var i = 0; i < buckets.Length; i++)
        {
            var value = Volatile.Read(ref _buckets[i]);
            buckets[i] = value;
            count += value;
        }

        return new PerfHistogramSnapshot(buckets, count, Volatile.Read(ref _sum), Volatile.Read(ref _max), maxIsApproximate: false);
    }

    /// <summary>Bucket index for a value; negatives map to 0 and values above the range to the top bucket.</summary>
    public static int BucketIndex(long value)
    {
        if (value < ExactBucketCount)
        {
            return value < 0 ? 0 : (int)value;
        }

        if (value > MaxRecordableValue)
        {
            return BucketCount - 1;
        }

        var octave = 63 - BitOperations.LeadingZeroCount((ulong)value);
        var sub = (int)((value >> (octave - SubBucketBits)) & (SubBucketCount - 1));
        return ExactBucketCount + (octave - FirstLogOctave) * SubBucketCount + sub;
    }

    /// <summary>Smallest value that lands in the bucket.</summary>
    public static long BucketLowerBound(int index)
    {
        if (index < ExactBucketCount)
        {
            return index;
        }

        var offset = index - ExactBucketCount;
        var octave = offset / SubBucketCount + FirstLogOctave;
        var sub = offset % SubBucketCount;
        return (1L << octave) + ((long)sub << (octave - SubBucketBits));
    }

    /// <summary>Number of distinct values the bucket holds.</summary>
    public static long BucketWidth(int index)
    {
        if (index < ExactBucketCount)
        {
            return 1;
        }

        var octave = (index - ExactBucketCount) / SubBucketCount + FirstLogOctave;
        return 1L << (octave - SubBucketBits);
    }

    private void RaiseMax(long value)
    {
        var current = Volatile.Read(ref _max);
        while (value > current)
        {
            var observed = Interlocked.CompareExchange(ref _max, value, current);
            if (observed == current)
            {
                return;
            }

            current = observed;
        }
    }
}

/// <summary>
/// Immutable copy of a <see cref="PerfHistogram"/> (or the difference of two copies). Percentiles resolve to the
/// midpoint of the bucket holding the requested rank.
/// </summary>
public sealed class PerfHistogramSnapshot
{
    public static readonly PerfHistogramSnapshot Empty =
        new(new long[PerfHistogram.BucketCount], 0, 0, 0, maxIsApproximate: false);

    private readonly long[] _buckets;

    internal PerfHistogramSnapshot(long[] buckets, long count, long sum, long max, bool maxIsApproximate)
    {
        _buckets = buckets;
        Count = count;
        Sum = sum;
        Max = max;
        MaxIsApproximate = maxIsApproximate;
    }

    public long Count { get; }

    /// <summary>Sum of recorded values (each clamped to the histogram range so it cannot overflow).</summary>
    public long Sum { get; }

    /// <summary>Largest recorded value. Exact for a lifetime snapshot, a bucket upper bound for a delta.</summary>
    public long Max { get; }

    /// <summary>True when <see cref="Max"/> is a bucket bound (delta snapshots) rather than an observed value.</summary>
    public bool MaxIsApproximate { get; }

    public double Mean => Count == 0 ? 0 : (double)Sum / Count;

    /// <summary>Value at the given percentile (0-100), or 0 when the snapshot is empty.</summary>
    public double Percentile(double percent)
    {
        if (Count == 0)
        {
            return 0;
        }

        var fraction = double.IsNaN(percent) ? 0 : Math.Clamp(percent, 0, 100) / 100d;
        var rank = (long)Math.Ceiling(fraction * Count);
        if (rank < 1)
        {
            rank = 1;
        }
        else if (rank > Count)
        {
            rank = Count;
        }

        long cumulative = 0;
        for (var i = 0; i < _buckets.Length; i++)
        {
            cumulative += _buckets[i];
            if (cumulative >= rank)
            {
                return PerfHistogram.BucketLowerBound(i) + (PerfHistogram.BucketWidth(i) - 1) / 2d;
            }
        }

        // Unreachable when counts are consistent; the last populated bucket is the honest fallback.
        return Max;
    }

    /// <summary>
    /// Per-bucket difference against an earlier snapshot of the same histogram, clamped at zero so a baseline that is
    /// newer than this snapshot yields an empty result instead of negative counts. The result's max is the upper
    /// bound of its highest non-empty bucket, never above this snapshot's lifetime max, and is flagged approximate.
    /// </summary>
    public PerfHistogramSnapshot Subtract(PerfHistogramSnapshot baseline)
    {
        var delta = new long[_buckets.Length];
        long count = 0;
        var highest = -1;
        for (var i = 0; i < delta.Length; i++)
        {
            var difference = _buckets[i] - baseline._buckets[i];
            if (difference > 0)
            {
                delta[i] = difference;
                count += difference;
                highest = i;
            }
        }

        var sum = Sum - baseline.Sum;
        if (sum < 0 || count == 0)
        {
            sum = 0;
        }

        long max = 0;
        if (highest >= 0)
        {
            max = PerfHistogram.BucketLowerBound(highest) + PerfHistogram.BucketWidth(highest) - 1;
            if (max > Max)
            {
                max = Max;
            }
        }

        return new PerfHistogramSnapshot(delta, count, sum, max, maxIsApproximate: true);
    }
}
