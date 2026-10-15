using System.Runtime.InteropServices;
using System.Threading;

namespace Sussudio.Services.Performance;

/// <summary>
/// Hand-off of each frame's Media Foundation sample time (as a QPC timestamp) from the capture read thread to the
/// preview renderer, keyed by the frame's arrival QPC timestamp, the same identity every pipeline stage already
/// carries. It exists so <c>preview.ingress_to_visible</c> can be recorded at present without threading a new field
/// through every delegate, frame type and queue between the read loop and the renderer.
/// One writer (the read loop), any reader. A direct-mapped table of 1,024 slots is far larger than the frames in
/// flight; a slot overwritten by a newer frame makes the older frame's lookup miss (the sample is simply not
/// recorded), and a lookup never returns another frame's sample. Written only while the ingress clock is the QPC
/// domain. Control flow never depends on it.
/// </summary>
public static class PerfIngressSamples
{
    private const int SlotCountBits = 10;
    private const int SlotCount = 1 << SlotCountBits;

    private static readonly Slot[] Slots = new Slot[SlotCount];

    /// <summary>Publishes the sample time for a frame. An arrival of zero or less is ignored (it marks an empty slot).</summary>
    public static void Record(long arrivalTick, long sampleQpcTick)
    {
        if (arrivalTick <= 0)
        {
            return;
        }

        ref var slot = ref Slots[SlotIndex(arrivalTick)];

        // Invalidate first (full fence) so a reader that matched the old key cannot pair it with the new sample.
        Interlocked.Exchange(ref slot.Arrival, 0);
        slot.Sample = sampleQpcTick;
        Volatile.Write(ref slot.Arrival, arrivalTick);
    }

    /// <summary>Looks up the sample time recorded for a frame; false when it was never recorded or was overwritten.</summary>
    public static bool TryGet(long arrivalTick, out long sampleQpcTick)
    {
        sampleQpcTick = 0;
        if (arrivalTick <= 0)
        {
            return false;
        }

        ref var slot = ref Slots[SlotIndex(arrivalTick)];
        if (Volatile.Read(ref slot.Arrival) != arrivalTick)
        {
            return false;
        }

        var sample = slot.Sample;
        Interlocked.MemoryBarrier();
        if (Volatile.Read(ref slot.Arrival) != arrivalTick)
        {
            return false;
        }

        sampleQpcTick = sample;
        return true;
    }

    internal static int SlotIndex(long arrivalTick) =>
        (int)(((ulong)arrivalTick * 0x9E3779B97F4A7C15UL) >> (64 - SlotCountBits));

    [StructLayout(LayoutKind.Sequential)]
    private struct Slot
    {
        public long Arrival;
        public long Sample;
    }
}
