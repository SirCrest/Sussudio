using System.Diagnostics;
using Sussudio.Services.Performance;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of the read-loop to renderer sample-time hand-off (docs/architecture/performance-telemetry.md
// sections 2.6 and 5): a miss for a key that was never recorded, empty-slot and non-positive keys, a colliding newer
// frame evicting an older one without ever leaking its sample, and a reader racing the writer.
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

    [Fact]
    public void NewerFrameInTheSameSlotEvictsTheOlderOneWithoutLeakingItsSample()
    {
        const long older = 5_000_000_011;
        var slot = PerfIngressSamples.SlotIndex(older);
        long newer = older + 1;
        while (PerfIngressSamples.SlotIndex(newer) != slot)
        {
            newer++;
        }

        PerfIngressSamples.Record(older, 111);
        PerfIngressSamples.Record(newer, 222);

        Assert.False(PerfIngressSamples.TryGet(older, out var leaked));
        Assert.Equal(0, leaked);
        Assert.True(PerfIngressSamples.TryGet(newer, out var sample));
        Assert.Equal(222, sample);
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
