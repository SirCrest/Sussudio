using System.Diagnostics;
using Sussudio.Services.Performance;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of the read-loop to renderer sample-time hand-off (docs/architecture/performance-telemetry.md
// sections 2.6 and 5): a miss for a key that was never recorded, empty-entry and non-positive keys, set-associative
// retention (four frames per set survive, the fifth evicts the smallest arrival tick without leaking its sample),
// re-recording a key in place, and a reader racing the writer.
public sealed class PerfIngressSamplesTests
{
    [Fact]
    public void RecordedFrameIsFoundByItsArrivalTick()
    {
        const long arrival = 777_000_001;
        PerfIngressSamples.Record(arrival, 776_000_123);

        Assert.True(PerfIngressSamples.TryGet(arrival, out var sample));
        Assert.Equal(776_000_123, sample);
    }

    [Fact]
    public void UnrecordedAndNonPositiveKeysNeverMatchAnEmptySlot()
    {
        Assert.False(PerfIngressSamples.TryGet(0, out var zero));
        Assert.Equal(0, zero);
        Assert.False(PerfIngressSamples.TryGet(-5, out _));
        Assert.False(PerfIngressSamples.TryGet(123_456_789_012, out _));

        // Recording a non-positive key is ignored rather than corrupting a slot.
        PerfIngressSamples.Record(0, 99);
        PerfIngressSamples.Record(-1, 99);
        Assert.False(PerfIngressSamples.TryGet(0, out _));
    }

    // The table is process-wide and never cleared, so an earlier test's entries stay in it. The writer evicts the
    // smallest arrival tick in a set, so a test whose keys are smaller than a leftover would evict its own keys.
    // Each test therefore takes a key base above everything recorded before it (real QPC ticks are far below 1e18).
    private static long _nextKeyBase = 1_000_000_000_000_000_000;

    private static long NextKeyBase() => Interlocked.Add(ref _nextKeyBase, 1_000_000_000);

    private static long[] KeysInOneSet(int count, long start)
    {
        var keys = new long[count];
        var set = PerfIngressSamples.SetIndex(start);
        keys[0] = start;
        var next = start;
        for (var i = 1; i < count; i++)
        {
            do
            {
                next++;
            }
            while (PerfIngressSamples.SetIndex(next) != set);
            keys[i] = next;
        }

        return keys;
    }

    [Fact]
    public void FourFramesThatShareASetAreAllRetained()
    {
        // A direct-mapped ledger lost every one of these but the last; the tail frames are the ones that stay in
        // flight longest, so losing them biases p99.9 and max low.
        var keys = KeysInOneSet(PerfIngressSamples.Ways, NextKeyBase());
        for (var i = 0; i < keys.Length; i++)
        {
            PerfIngressSamples.Record(keys[i], 1_000 + i);
        }

        for (var i = 0; i < keys.Length; i++)
        {
            Assert.True(PerfIngressSamples.TryGet(keys[i], out var sample), $"way {i} was lost");
            Assert.Equal(1_000 + i, sample);
        }
    }

    [Fact]
    public void AFifthFrameInASetEvictsTheOldestAndNeverLeaksItsSample()
    {
        var keys = KeysInOneSet(PerfIngressSamples.Ways + 1, NextKeyBase());
        for (var i = 0; i < keys.Length; i++)
        {
            PerfIngressSamples.Record(keys[i], 2_000 + i);
        }

        Assert.False(PerfIngressSamples.TryGet(keys[0], out var leaked));
        Assert.Equal(0, leaked);
        for (var i = 1; i < keys.Length; i++)
        {
            Assert.True(PerfIngressSamples.TryGet(keys[i], out var sample), $"key {i} should survive");
            Assert.Equal(2_000 + i, sample);
        }
    }

    [Fact]
    public void EvictionTakesTheSmallestArrivalTickNotTheLeastRecentlyWritten()
    {
        // Written out of order: the oldest frame (smallest tick) is written second, so a first-written or
        // round-robin policy would evict the wrong entry.
        var keys = KeysInOneSet(PerfIngressSamples.Ways + 1, NextKeyBase());
        PerfIngressSamples.Record(keys[3], 33);
        PerfIngressSamples.Record(keys[0], 30);
        PerfIngressSamples.Record(keys[2], 32);
        PerfIngressSamples.Record(keys[1], 31);
        PerfIngressSamples.Record(keys[4], 34);

        Assert.False(PerfIngressSamples.TryGet(keys[0], out _));
        Assert.True(PerfIngressSamples.TryGet(keys[1], out var s1));
        Assert.Equal(31, s1);
        Assert.True(PerfIngressSamples.TryGet(keys[2], out var s2));
        Assert.Equal(32, s2);
        Assert.True(PerfIngressSamples.TryGet(keys[3], out var s3));
        Assert.Equal(33, s3);
        Assert.True(PerfIngressSamples.TryGet(keys[4], out var s4));
        Assert.Equal(34, s4);
    }

    [Fact]
    public void RecordingAFrameAgainReplacesItInPlaceAndDoesNotCostAWay()
    {
        var keys = KeysInOneSet(PerfIngressSamples.Ways, NextKeyBase());
        PerfIngressSamples.Record(keys[0], 1);
        PerfIngressSamples.Record(keys[0], 2);
        PerfIngressSamples.Record(keys[0], 3);
        for (var i = 1; i < keys.Length; i++)
        {
            PerfIngressSamples.Record(keys[i], 10 + i);
        }

        Assert.True(PerfIngressSamples.TryGet(keys[0], out var first));
        Assert.Equal(3, first);
        for (var i = 1; i < keys.Length; i++)
        {
            Assert.True(PerfIngressSamples.TryGet(keys[i], out var sample));
            Assert.Equal(10 + i, sample);
        }
    }

    [Fact]
    public void FramesInFlightAcrossManySetsAreRetainedThroughAHundredNewerFrames()
    {
        // 60 fps over about 1.7 s of a slow frame's life: a direct-mapped table lost roughly one in ten of these at
        // 100 newer frames; with four ways a set needs four newer arrivals to evict, so none are lost here.
        var start = NextKeyBase();
        const int total = 2_000;
        const int inFlight = 100;
        for (var i = 0; i < total; i++)
        {
            PerfIngressSamples.Record(start + i * 166_667L, i + 1);
        }

        var lost = 0;
        for (var i = total - inFlight; i < total; i++)
        {
            if (!PerfIngressSamples.TryGet(start + i * 166_667L, out var sample) || sample != i + 1)
            {
                lost++;
            }
        }

        Assert.Equal(0, lost);
    }

    [Fact]
    public void ReaderRacingTheWriterSeesAMissOrTheMatchingPairNeverAForeignSample()
    {
        // The sample is a pure function of the key, so any hit that disagrees is a torn or leaked pair. The writer
        // cycles through a bounded key range (more keys than slots, so slots keep being evicted and refilled) and the
        // reader probes the same range; an unbounded writer would move past every key before the reader asked for it.
        const long baseTick = 9_000_000_000;
        const int keyRange = 3000;
        var stop = false;
        var mismatches = 0L;
        var hits = 0L;

        var writer = new Thread(() =>
        {
            for (long i = 1; !Volatile.Read(ref stop); i = i % keyRange + 1)
            {
                var key = baseTick + i * 16_667;
                PerfIngressSamples.Record(key, key * 3);
            }
        }) { IsBackground = true };

        var reader = new Thread(() =>
        {
            var random = new Random(1);
            while (!Volatile.Read(ref stop))
            {
                var key = baseTick + random.Next(1, keyRange + 1) * 16_667L;
                if (PerfIngressSamples.TryGet(key, out var sample))
                {
                    Interlocked.Increment(ref hits);
                    if (sample != key * 3)
                    {
                        Interlocked.Increment(ref mismatches);
                    }
                }
            }
        }) { IsBackground = true };

        writer.Start();
        reader.Start();
        var watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < 400)
        {
            Thread.Sleep(20);
        }

        Volatile.Write(ref stop, true);
        Assert.True(writer.Join(TimeSpan.FromSeconds(5)));
        Assert.True(reader.Join(TimeSpan.FromSeconds(5)));

        Assert.Equal(0, Interlocked.Read(ref mismatches));
        Assert.True(Interlocked.Read(ref hits) > 0, "the race should have produced at least one hit to check");
    }
}
