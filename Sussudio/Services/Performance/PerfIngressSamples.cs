using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Sussudio.Services.Performance;

/// <summary>
/// Hand-off of each frame's Media Foundation sample time (as a QPC timestamp) from the capture read thread to the
/// preview renderer, keyed by the frame's arrival QPC timestamp, the same identity every pipeline stage already
/// carries. It exists so <c>preview.ingress_to_visible</c> can be recorded at present without threading a new field
/// through every delegate, frame type and queue between the read loop and the renderer.
/// One writer (the read loop), any reader. The table is 4-way set-associative: 256 sets of four 16-byte entries
/// (arrival, sample), one 64-byte cache line per set. A frame hashes to one set; the writer takes an empty way, or
/// replaces the OLDEST entry in the set (the smallest arrival tick), and the reader probes all four ways. A direct-mapped
/// table loses an entry whenever any later frame hashes to its slot, so the frames that stay in flight longest, the
/// tail that p99.9 and max are about, were the ones most likely to miss. With four ways a frame is evicted only after
/// four newer frames land in its set while it is still in flight. A lookup never returns another frame's sample, and a
/// miss records nothing (the renderer counts it as <c>preview.ingress_lookup_miss</c>). Written only while the ingress
/// clock is the QPC domain. Control flow never depends on it.
/// </summary>
public static class PerfIngressSamples
{
    private const int SetBits = 8;
    private const int SetCount = 1 << SetBits;
    internal const int Ways = 4;

    // Entries are (arrival, sample) pairs of longs; a set is Ways pairs (64 bytes).
    private const int LongsPerEntry = 2;
    private const int LongsPerSet = Ways * LongsPerEntry;
    private const int CacheLineLongs = 8;

    // Pinned (the pinned object heap never moves) so the address used to align the first set to a cache line stays
    // valid. A managed array is only 8-byte aligned, so one spare set of longs is allocated and the first set
    // starts at the first 64-byte boundary inside it; otherwise a set could straddle two lines.
    private static readonly long[] Cells = GC.AllocateArray<long>(SetCount * LongsPerSet + CacheLineLongs, pinned: true);
    private static readonly int Origin = CacheLineAlignedOrigin(Cells);

    /// <summary>Publishes the sample time for a frame. An arrival of zero or less is ignored (it marks an empty entry).</summary>
    public static void Record(long arrivalTick, long sampleQpcTick)
    {
        if (arrivalTick <= 0)
        {
            return;
        }

        var set = SetStart(arrivalTick);

        // Victim: the entry already holding this key (a frame recorded twice replaces itself), otherwise the entry
        // with the smallest arrival tick. An empty entry holds 0, so it always loses to a live one and an empty way
        // is used before anything is evicted. Arrival ticks grow with time, so the smallest is the oldest.
        var victim = set;
        var oldest = long.MaxValue;
        for (var way = 0; way < Ways; way++)
        {
            var index = set + way * LongsPerEntry;
            var arrival = Volatile.Read(ref Cells[index]);
            if (arrival == arrivalTick)
            {
                victim = index;
                break;
            }

            if (arrival < oldest)
            {
                oldest = arrival;
                victim = index;
            }
        }

        // Invalidate first (full fence) so a reader that matched the old key cannot pair it with the new sample.
        Interlocked.Exchange(ref Cells[victim], 0);
        Cells[victim + 1] = sampleQpcTick;
        Volatile.Write(ref Cells[victim], arrivalTick);
    }

    /// <summary>Looks up the sample time recorded for a frame; false when it was never recorded or was evicted.</summary>
    public static bool TryGet(long arrivalTick, out long sampleQpcTick)
    {
        sampleQpcTick = 0;
        if (arrivalTick <= 0)
        {
            return false;
        }

        var set = SetStart(arrivalTick);
        for (var way = 0; way < Ways; way++)
        {
            var index = set + way * LongsPerEntry;
            if (Volatile.Read(ref Cells[index]) != arrivalTick)
            {
                continue;
            }

            var sample = Cells[index + 1];
            Interlocked.MemoryBarrier();
            if (Volatile.Read(ref Cells[index]) != arrivalTick)
            {
                // Evicted or rewritten while it was read: a key lives in at most one way, so this is a miss.
                return false;
            }

            sampleQpcTick = sample;
            return true;
        }

        return false;
    }

    internal static int SetIndex(long arrivalTick) =>
        (int)(((ulong)arrivalTick * 0x9E3779B97F4A7C15UL) >> (64 - SetBits));

    private static int SetStart(long arrivalTick) => Origin + SetIndex(arrivalTick) * LongsPerSet;

    private static int CacheLineAlignedOrigin(long[] cells)
    {
        var address = (long)Marshal.UnsafeAddrOfPinnedArrayElement(cells, 0);
        return (int)(((64 - (address & 63)) & 63) >> 3);
    }
}
