using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Sussudio.Services.Performance;

/// <summary>One validated event copied out of the ring. <see cref="Dur"/> is negative for an instant event.</summary>
public readonly record struct FrameTraceEvent(long Ts, long Dur, long Id, FrameTraceEventKind Kind, int Tid)
{
    public bool IsInstant => Dur < 0;
}

/// <summary>
/// Lock-free ring of fixed 40-byte events. A writer claims a slot with <c>Interlocked.Increment</c>, marks the slot
/// in-progress, fills it, then publishes the slot's stamp (index + 1). A reader keeps a slot only when the stamp it
/// read before copying equals the stamp it reads after, so a slot that is mid-write or was lapped by a newer
/// writer is dropped, never emitted torn. Capacity is a power of two between 2^12 and 2^22.
/// Contract: docs/architecture/performance-telemetry.md section 2.4.
/// </summary>
public sealed class FrameTraceBuffer
{
    public const int MinCapacity = 1 << 12;
    public const int MaxCapacity = 1 << 22;
    public const int DefaultCapacity = 1 << 18;

    // 0 = never written, -1 = write in progress, otherwise (event index + 1).
    private const long StampInProgress = -1;

    private readonly Slot[] _slots;
    private readonly long _mask;
    private long _head;

    public FrameTraceBuffer(int requestedCapacity)
    {
        Capacity = NormalizeCapacity(requestedCapacity);
        _slots = new Slot[Capacity];
        _mask = Capacity - 1;
        StartTimestamp = Stopwatch.GetTimestamp();
        StartedUtc = DateTime.UtcNow;
    }

    /// <summary>Size of one ring event in bytes.</summary>
    public static int EventSizeBytes => Unsafe.SizeOf<Slot>();

    public int Capacity { get; }

    /// <summary>Stopwatch timestamp when the buffer was created (trace start).</summary>
    public long StartTimestamp { get; }

    public DateTime StartedUtc { get; }

    /// <summary>Stopwatch timestamp when the trace was frozen; zero while running.</summary>
    public long StopTimestamp { get; private set; }

    /// <summary>Events claimed since creation, including ones already overwritten.</summary>
    public long Recorded => Volatile.Read(ref _head);

    public long Retained => Math.Min(Recorded, Capacity);

    public long Overwritten => Math.Max(0, Recorded - Capacity);

    /// <summary>Rounds up to a power of two within [MinCapacity, MaxCapacity]; non-positive means the default.</summary>
    public static int NormalizeCapacity(int requested)
    {
        if (requested <= 0)
        {
            return DefaultCapacity;
        }

        if (requested <= MinCapacity)
        {
            return MinCapacity;
        }

        if (requested >= MaxCapacity)
        {
            return MaxCapacity;
        }

        return (int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)requested);
    }

    /// <summary>Records a duration event that began at <paramref name="startTimestamp"/> (Stopwatch ticks).</summary>
    public void Record(FrameTraceEventKind kind, long startTimestamp, long durationTicks, long id) =>
        Write(kind, startTimestamp, durationTicks < 0 ? 0 : durationTicks, id);

    /// <summary>Records an instant event at <paramref name="timestamp"/> (Stopwatch ticks).</summary>
    public void RecordInstant(FrameTraceEventKind kind, long timestamp, long id) =>
        Write(kind, timestamp, -1, id);

    internal void Freeze()
    {
        StopTimestamp = Stopwatch.GetTimestamp();
    }

    /// <summary>Validated events, oldest first. Slots that are mid-write or lapped during the copy are dropped.</summary>
    public IReadOnlyList<FrameTraceEvent> Copy() => Copy(null);

    /// <summary>Test seam: <paramref name="afterFieldsCopied"/> runs between a slot's field copy and its second stamp read.</summary>
    internal List<FrameTraceEvent> Copy(Action<long>? afterFieldsCopied)
    {
        var head = Volatile.Read(ref _head);
        var first = Math.Max(0, head - Capacity);
        var events = new List<FrameTraceEvent>((int)(head - first));
        for (var index = first; index < head; index++)
        {
            ref var slot = ref _slots[(int)(index & _mask)];
            var expected = index + 1;
            if (Volatile.Read(ref slot.Stamp) != expected)
            {
                continue;
            }

            var ts = slot.Ts;
            var dur = slot.Dur;
            var id = slot.Id;
            var kind = slot.Kind;
            var tid = slot.Tid;
            afterFieldsCopied?.Invoke(index);
            Interlocked.MemoryBarrier();
            if (Volatile.Read(ref slot.Stamp) != expected)
            {
                continue;
            }

            events.Add(new FrameTraceEvent(ts, dur, id, (FrameTraceEventKind)kind, tid));
        }

        return events;
    }

    private void Write(FrameTraceEventKind kind, long timestamp, long duration, long id)
    {
        var index = Interlocked.Increment(ref _head) - 1;
        ref var slot = ref _slots[(int)(index & _mask)];

        // Full fence: the in-progress mark must be visible before any field store, or a reader could pair the
        // previous event's stamp with this event's half-written fields.
        Interlocked.Exchange(ref slot.Stamp, StampInProgress);
        slot.Ts = timestamp;
        slot.Dur = duration;
        slot.Id = id;
        slot.Kind = (int)kind;
        slot.Tid = Environment.CurrentManagedThreadId;
        Volatile.Write(ref slot.Stamp, index + 1);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Slot
    {
        public long Stamp;
        public long Ts;
        public long Dur;
        public long Id;
        public int Kind;
        public int Tid;
    }
}
