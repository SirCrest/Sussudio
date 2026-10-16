using System.Diagnostics;
using Sussudio.Services.Performance;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of the lock-free log-linear histogram (docs/architecture/performance-telemetry.md
// section 2.1 and 5): boundary values, concurrent writers, snapshots taken during writes,
// empty reads, precision and delta arithmetic.
public sealed class PerfHistogramTests
{
    [Fact]
    public void NegativeValuesClampToZeroAndNeverThrow()
    {
        var histogram = new PerfHistogram();
        histogram.Record(-1);
        histogram.Record(long.MinValue);
        histogram.Record(0);

        var snapshot = histogram.Snapshot();
        Assert.Equal(3, snapshot.Count);
        Assert.Equal(0, snapshot.Sum);
        Assert.Equal(0, snapshot.Max);
        Assert.Equal(0, snapshot.Percentile(50));
        Assert.Equal(0, snapshot.Percentile(100));
    }

    [Fact]
    public void ValuesBelowSixtyFourHaveExactBuckets()
    {
        var histogram = new PerfHistogram();
        for (var value = 0; value < 64; value++)
        {
            histogram.Record(value);
        }

        var snapshot = histogram.Snapshot();
        Assert.Equal(64, snapshot.Count);
        Assert.Equal(63, snapshot.Max);
        Assert.False(snapshot.MaxIsApproximate);
        // Rank-ordered values 0..63, so the 50th percentile is rank 32 = value 31.
        Assert.Equal(31, snapshot.Percentile(50));
        Assert.Equal(63, snapshot.Percentile(100));
        Assert.Equal(0, snapshot.Percentile(0));
    }

    [Fact]
    public void SixtyThreeAndSixtyFourLandInDifferentBucketsWithoutOverlap()
    {
        Assert.Equal(63, PerfHistogram.BucketIndex(63));
        Assert.Equal(64, PerfHistogram.BucketIndex(64));
        Assert.Equal(64, PerfHistogram.BucketIndex(65));
        Assert.Equal(65, PerfHistogram.BucketIndex(66));

        var histogram = new PerfHistogram();
        histogram.Record(63);
        histogram.Record(64);
        var snapshot = histogram.Snapshot();
        Assert.Equal(63, snapshot.Percentile(50));
        // 64 shares a two-wide bucket with 65; its midpoint is 64.5.
        Assert.Equal(64.5, snapshot.Percentile(100));
    }

    [Fact]
    public void BucketBoundsTileTheRangeWithoutGapsOrOverlap()
    {
        long expectedLower = 0;
        for (var index = 0; index < PerfHistogram.BucketCount; index++)
        {
            Assert.Equal(expectedLower, PerfHistogram.BucketLowerBound(index));
            var width = PerfHistogram.BucketWidth(index);
            Assert.True(width >= 1);
            if (index >= PerfHistogram.ExactBucketCount)
            {
                // The bucket's width is at most one thirty-second of its lower bound.
                Assert.True(width * 32 <= expectedLower, $"bucket {index} is too wide");
            }

            expectedLower += width;
        }

        Assert.Equal(PerfHistogram.MaxRecordableValue + 1, expectedLower);
    }

    [Fact]
    public void LongMaxValueClampsIntoTheTopBucketAndStillUpdatesMax()
    {
        var histogram = new PerfHistogram();
        histogram.Record(long.MaxValue);
        histogram.Record(PerfHistogram.MaxRecordableValue + 1);

        var snapshot = histogram.Snapshot();
        Assert.Equal(2, snapshot.Count);
        Assert.Equal(long.MaxValue, snapshot.Max);
        Assert.True(snapshot.Sum > 0, "sum must not overflow negative");
        var top = snapshot.Percentile(100);
        Assert.InRange(top, PerfHistogram.MaxRecordableValue * 0.98, PerfHistogram.MaxRecordableValue);
    }

    [Fact]
    public void EmptyHistogramReportsZeroEverywhere()
    {
        var snapshot = new PerfHistogram().Snapshot();
        Assert.Equal(0, snapshot.Count);
        Assert.Equal(0, snapshot.Sum);
        Assert.Equal(0, snapshot.Max);
        Assert.Equal(0, snapshot.Mean);
        Assert.Equal(0, snapshot.Percentile(50));
        Assert.Equal(0, snapshot.Percentile(99.9));
        Assert.Equal(0, PerfHistogramSnapshot.Empty.Percentile(50));
    }

    [Fact]
    public void ConcurrentWritersLoseNoCounts()
    {
        var histogram = new PerfHistogram();
        const int threads = 8;
        const int perThread = 200_000;
        Parallel.For(0, threads, t =>
        {
            for (var i = 0; i < perThread; i++)
            {
                histogram.Record((i % 5000) + t);
            }
        });

        var snapshot = histogram.Snapshot();
        Assert.Equal(threads * (long)perThread, snapshot.Count);
        long expectedSum = 0;
        for (var t = 0; t < threads; t++)
        {
            for (var i = 0; i < perThread; i++)
            {
                expectedSum += (i % 5000) + t;
            }
        }

        Assert.Equal(expectedSum, snapshot.Sum);
        Assert.Equal(5006, snapshot.Max);
    }

    [Fact]
    public async Task SnapshotsDuringWritesNeverThrowAndNeverGoBackwards()
    {
        var histogram = new PerfHistogram();
        using var stop = new CancellationTokenSource();
        var writers = Enumerable.Range(0, 4).Select(w => Task.Run(() =>
        {
            var value = w;
            while (!stop.IsCancellationRequested)
            {
                histogram.Record(value++ % 100_000);
            }
        })).ToArray();

        long previousCount = 0;
        long previousSum = 0;
        long previousMax = 0;
        var deadline = Stopwatch.StartNew();
        while (deadline.ElapsedMilliseconds < 300)
        {
            var snapshot = histogram.Snapshot();
            Assert.True(snapshot.Count >= previousCount, "count went backwards");
            Assert.True(snapshot.Max >= previousMax, "max went backwards");
            Assert.True(snapshot.Sum >= previousSum, "sum went backwards");
            _ = snapshot.Percentile(99);
            previousCount = snapshot.Count;
            previousSum = snapshot.Sum;
            previousMax = snapshot.Max;
        }

        stop.Cancel();
        await Task.WhenAll(writers);
    }

    // Failure modes for the sum: a writer that lands between the bucket copy and the sum read puts its value in the sum
    // but not the count, and one paused between its bucket increment and its sum add puts it in the count but not the
    // sum. Either makes Mean wrong, and one outlier in a short delta can make it wildly wrong. Writers here record one
    // value with an exact bucket, so any snapshot whose sum and count come from one cut has Sum == value * Count.
    [Fact]
    public async Task SnapshotsTakenDuringWritesHaveASumThatMatchesTheirCount()
    {
        const long value = 7;
        var histogram = new PerfHistogram();
        using var stop = new CancellationTokenSource();
        var writers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            while (!stop.IsCancellationRequested)
            {
                histogram.Record(value);
            }
        })).ToArray();

        var previous = histogram.Snapshot();
        var deadline = Stopwatch.StartNew();
        while (deadline.ElapsedMilliseconds < 300)
        {
            var snapshot = histogram.Snapshot();
            Assert.Equal(value * snapshot.Count, snapshot.Sum);
            var delta = snapshot.Subtract(previous);
            Assert.Equal(value * delta.Count, delta.Sum);
            previous = snapshot;
        }

        stop.Cancel();
        await Task.WhenAll(writers);
    }

    // Under writers that never pause a snapshot may not get a quiet moment; its sum must then still stay inside the
    // range its own buckets allow, so the mean is off by at most the bucket width instead of by a whole outlier.
    [Fact]
    public async Task UnderConstantWritesTheMeanStaysInsideTheBucketOfTheOnlyValueRecorded()
    {
        const long value = 1_000_000;
        var bucket = PerfHistogram.BucketIndex(value);
        var lower = PerfHistogram.BucketLowerBound(bucket);
        var upper = lower + PerfHistogram.BucketWidth(bucket) - 1;
        var histogram = new PerfHistogram();
        using var stop = new CancellationTokenSource();
        var writers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            while (!stop.IsCancellationRequested)
            {
                histogram.Record(value);
            }
        })).ToArray();

        var previous = histogram.Snapshot();
        var deadline = Stopwatch.StartNew();
        while (deadline.ElapsedMilliseconds < 300)
        {
            var snapshot = histogram.Snapshot();
            Assert.InRange(snapshot.Sum, lower * snapshot.Count, upper * snapshot.Count);
            var delta = snapshot.Subtract(previous);
            Assert.InRange(delta.Sum, lower * delta.Count, upper * delta.Count);
            previous = snapshot;
        }

        stop.Cancel();
        await Task.WhenAll(writers);
    }

    // Failure modes for the max, which a writer updates apart from the buckets: one that raises it before marking its
    // record begun can be caught between the two by a snapshot that then accepts its copy as coherent, and one that
    // raises it after the copy reaches a snapshot that gave up waiting for a quiet moment. Either reports a max that no
    // bucket in the snapshot holds. The writer here climbs the buckets, so nearly every record is a new max in a new
    // bucket, and every snapshot's max must lie inside its own highest non-empty bucket, or be zero when it has none.
    [Fact]
    public async Task SnapshotsTakenDuringWritesHaveAMaxInsideTheirHighestBucket()
    {
        var current = new PerfHistogram();
        using var stop = new CancellationTokenSource();
        var writer = Task.Run(() =>
        {
            while (!stop.IsCancellationRequested)
            {
                var histogram = new PerfHistogram();
                Volatile.Write(ref current, histogram);
                for (var bucket = 0; bucket < PerfHistogram.BucketCount; bucket++)
                {
                    histogram.Record(PerfHistogram.BucketLowerBound(bucket));
                }
            }
        });

        var deadline = Stopwatch.StartNew();
        while (deadline.ElapsedMilliseconds < 300)
        {
            var snapshot = Volatile.Read(ref current).Snapshot();
            if (snapshot.Count == 0)
            {
                Assert.Equal(0, snapshot.Max);
                continue;
            }

            var highest = PerfHistogram.BucketIndex((long)snapshot.Percentile(100));
            var lower = PerfHistogram.BucketLowerBound(highest);
            Assert.InRange(snapshot.Max, lower, lower + PerfHistogram.BucketWidth(highest) - 1);
        }

        stop.Cancel();
        await writer;
    }

    [Theory]
    [InlineData(1, 10_000)]
    [InlineData(100, 5_000_000)]
    [InlineData(1_000, 1_000_000_000)]
    public void PercentilesStayWithinOnePointSixPercentOnAUniformDistribution(long minimum, long maximum)
    {
        var histogram = new PerfHistogram();
        const int samples = 100_000;
        var values = new long[samples];
        var random = new Random(12345);
        for (var i = 0; i < samples; i++)
        {
            values[i] = minimum + (long)(random.NextDouble() * (maximum - minimum));
            histogram.Record(values[i]);
        }

        Array.Sort(values);
        var snapshot = histogram.Snapshot();
        foreach (var percent in new[] { 50.0, 90.0, 99.0, 99.9 })
        {
            var rank = (int)Math.Ceiling(percent / 100.0 * samples);
            var exact = values[Math.Clamp(rank, 1, samples) - 1];
            var reported = snapshot.Percentile(percent);
            var tolerance = Math.Max(1.0, exact * 0.016);
            Assert.True(
                Math.Abs(reported - exact) <= tolerance,
                $"p{percent}: exact {exact}, reported {reported}, tolerance {tolerance}");
        }
    }

    [Fact]
    public void DeltaOfTwoSnapshotsContainsOnlyTheIntervalAndNeverGoesNegative()
    {
        var histogram = new PerfHistogram();
        for (var i = 0; i < 1000; i++)
        {
            histogram.Record(100);
        }

        var baseline = histogram.Snapshot();
        for (var i = 0; i < 500; i++)
        {
            histogram.Record(5_000);
        }

        var delta = histogram.Snapshot().Subtract(baseline);
        Assert.Equal(500, delta.Count);
        Assert.Equal(500L * 5_000, delta.Sum);
        Assert.True(delta.MaxIsApproximate);
        // The delta max is the upper bound of the highest non-empty bucket, never below the true max.
        Assert.InRange(delta.Max, 5_000, 5_000 * 1.04);
        Assert.InRange(delta.Percentile(50), 5_000 * 0.984, 5_000 * 1.016);

        var empty = histogram.Snapshot().Subtract(histogram.Snapshot());
        Assert.Equal(0, empty.Count);
        Assert.Equal(0, empty.Max);
    }

    [Fact]
    public void DeltaMaxIsClampedToTheLifetimeMax()
    {
        var histogram = new PerfHistogram();
        histogram.Record(1_000);
        var baseline = histogram.Snapshot();
        // 1_001 shares a bucket whose upper bound is above the true maximum.
        histogram.Record(1_001);
        var delta = histogram.Snapshot().Subtract(baseline);
        Assert.Equal(1, delta.Count);
        Assert.True(delta.Max <= 1_001);
        Assert.True(delta.Max >= 1_001);
    }

    [Fact]
    public void SubtractingANewerBaselineClampsToZeroInsteadOfGoingNegative()
    {
        var histogram = new PerfHistogram();
        histogram.Record(10);
        var older = histogram.Snapshot();
        histogram.Record(10);
        histogram.Record(20);
        var newer = histogram.Snapshot();

        // Reversed operands model a window restarted between the two reads.
        var reversed = older.Subtract(newer);
        Assert.Equal(0, reversed.Count);
        Assert.Equal(0, reversed.Sum);
        Assert.Equal(0, reversed.Percentile(50));
    }

    [Fact]
    public void TickHelpersConvertStopwatchTicksToMicroseconds()
    {
        var histogram = new PerfHistogram();
        var oneMillisecondInTicks = Stopwatch.Frequency / 1000;
        histogram.RecordTicks(oneMillisecondInTicks);
        histogram.RecordTicks(-5);
        var snapshot = histogram.Snapshot();
        Assert.Equal(2, snapshot.Count);
        Assert.InRange(snapshot.Max, 999, 1001);

        var start = Stopwatch.GetTimestamp();
        Thread.Sleep(5);
        histogram.RecordElapsedSince(start);
        Assert.InRange(histogram.Snapshot().Max, 4_000, 500_000);
    }

    [Fact]
    public void CounterAddsAndIncrementsAtomicallyAcrossThreads()
    {
        var counter = new PerfCounter();
        Parallel.For(0, 8, _ =>
        {
            for (var i = 0; i < 100_000; i++)
            {
                counter.Increment();
            }

            counter.Add(5);
        });

        Assert.Equal(800_040, counter.Read());
    }
}
