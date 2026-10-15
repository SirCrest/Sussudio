using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Sussudio.Services.Performance;

/// <summary>
/// One validated event copied out of the ring. <see cref="Dur"/> is negative for an instant event. <see cref="Arg"/>
/// is an optional 16-bit stage argument (zero when absent): the drop-reason code on a preview drop, the worker
/// number plus one on an MJPEG decode.
/// </summary>
public readonly record struct FrameTraceEvent(long Ts, long Dur, long Id, FrameTraceEventKind Kind, int Tid, int Arg = 0)
{
    public bool IsInstant => Dur < 0;
}

/// <summary>
/// Where a chunked read of the ring stands: the next event index to look at and the exclusive end captured when the
/// read began. Copy the struct to read the same range again (a dump walks the ring twice).
/// </summary>
public struct FrameTraceReadCursor
{
    internal long Next;
    internal long End;

    /// <summary>True once every event index in the captured range has been looked at.</summary>
    public readonly bool IsDone => Next >= End;
}

/// <summary>
/// Lock-free ring of fixed 40-byte events. A writer claims an index with <c>Interlocked.Increment</c>, then takes
/// ownership of the slot by compare-exchanging its stamp from the last published value to in-progress; only the
/// winner stores fields, so a writer that was preempted across a whole ring wrap can never land stale field stores
/// on a newer event (it finds the slot lapped or busy and drops its own event instead). The owner fills the slot and
/// publishes the stamp (index + 1). A reader keeps a slot only when the stamp it read before copying equals the
/// stamp it reads after, so a slot that is mid-write or was lapped is dropped, never emitted torn.
/// Capacity is a power of two between 2^12 and 2^22. Reads are chunked (<see cref="ReadChunk(ref FrameTraceReadCursor, Span{FrameTraceEvent})"/>)
/// so a dump never materialises the ring. Contract: docs/architecture/performance-telemetry.md section 2.4.
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
    private long _abandoned;

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

    /// <summary>Stopwatch timestamp when the trace started (buffer creation or the last <see cref="Restart"/>).</summary>
    public long StartTimestamp { get; private set; }

    public DateTime StartedUtc { get; private set; }

    /// <summary>Stopwatch timestamp when the trace was frozen; zero while running.</summary>
    public long StopTimestamp { get; private set; }

    /// <summary>Events claimed since creation, including ones already overwritten.</summary>
    public long Recorded => Volatile.Read(ref _head);

    public long Retained => Math.Min(Recorded, Capacity);

    public long Overwritten => Math.Max(0, Recorded - Capacity);

    /// <summary>
    /// Events a writer gave up on because its slot was already owned by a newer lap or by a writer that was mid-write
    /// (a writer preempted for a whole ring wrap). They are counted in <see cref="Recorded"/> but never stored; the
    /// figure is almost always zero.
    /// </summary>
    public long Abandoned => Volatile.Read(ref _abandoned);

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
    public void Record(FrameTraceEventKind kind, long startTimestamp, long durationTicks, long id, ushort arg = 0) =>
        Write(kind, startTimestamp, durationTicks < 0 ? 0 : durationTicks, id, arg);

    /// <summary>Records an instant event at <paramref name="timestamp"/> (Stopwatch ticks).</summary>
    public void RecordInstant(FrameTraceEventKind kind, long timestamp, long id, ushort arg = 0) =>
        Write(kind, timestamp, -1, id, arg);

    internal void Freeze()
    {
        StopTimestamp = Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Re-arms a frozen buffer for a new trace without allocating a new ring: clears every slot (so no stale stamp can
    /// match a new event index), resets the head and the start times. Only for a buffer that is no longer recording and
    /// is not being read; the recorder guarantees both. The ring of a default trace is 10.5 MB and up to 168 MB, which
    /// would otherwise land on the large object heap on every start.
    /// </summary>
    internal void Restart()
    {
        Array.Clear(_slots);
        StopTimestamp = 0;
        StartTimestamp = Stopwatch.GetTimestamp();
        StartedUtc = DateTime.UtcNow;
        Volatile.Write(ref _abandoned, 0);
        Volatile.Write(ref _head, 0);
    }

    /// <summary>Captures the range a chunked read will walk: the newest <see cref="Capacity"/> event indices.</summary>
    public FrameTraceReadCursor BeginRead()
    {
        var head = Volatile.Read(ref _head);
        return new FrameTraceReadCursor { Next = Math.Max(0, head - Capacity), End = head };
    }

    /// <summary>
    /// Copies validated events, oldest first, into <paramref name="destination"/> until it is full or the range is
    /// exhausted, and returns how many it wrote; zero means the cursor is done. Slots that are mid-write or were lapped
    /// during the copy are skipped, never emitted torn. Allocation free.
    /// </summary>
    public int ReadChunk(ref FrameTraceReadCursor cursor, Span<FrameTraceEvent> destination) =>
        ReadChunk(ref cursor, destination, null);

    /// <summary>Test seam: <paramref name="afterFieldsCopied"/> runs between a slot's field copy and its second stamp read.</summary>
    internal int ReadChunk(ref FrameTraceReadCursor cursor, Span<FrameTraceEvent> destination, Action<long>? afterFieldsCopied)
    {
        var written = 0;
        var index = cursor.Next;
        var end = cursor.End;
        while (index < end && written < destination.Length)
        {
            var current = index++;
            ref var slot = ref _slots[(int)(current & _mask)];
            var expected = current + 1;
            if (Volatile.Read(ref slot.Stamp) != expected)
            {
                continue;
            }

            var ts = slot.Ts;
            var dur = slot.Dur;
            var id = slot.Id;
            var kind = slot.Kind;
            var arg = slot.Arg;
            var tid = slot.Tid;
            afterFieldsCopied?.Invoke(current);
            Interlocked.MemoryBarrier();
            if (Volatile.Read(ref slot.Stamp) != expected)
            {
                continue;
            }

            destination[written++] = new FrameTraceEvent(ts, dur, id, (FrameTraceEventKind)kind, tid, arg);
        }

        cursor.Next = index;
        return written;
    }

    private void Write(FrameTraceEventKind kind, long timestamp, long duration, long id, ushort arg) =>
        WriteClaimed(Interlocked.Increment(ref _head) - 1, kind, timestamp, duration, id, arg);

    /// <summary>
    /// Stores an event at an already-claimed index. Separate from <see cref="Write"/> only so a test can replay the
    /// writer that was preempted between claiming its index and storing the event, which cannot be provoked reliably
    /// with real threads.
    /// </summary>
    internal void WriteClaimed(long index, FrameTraceEventKind kind, long timestamp, long duration, long id, ushort arg)
    {
        ref var slot = ref _slots[(int)(index & _mask)];

        // Take ownership of the slot: the compare-exchange (a full fence) swaps the last published stamp for the
        // in-progress mark, so the mark is visible before any field store and only one writer fills the slot at a
        // time. A published stamp above this index means a newer lap already owns the slot; an in-progress mark means
        // another writer is mid-write (an older lap that was preempted, or a newer one). Either way this event is
        // the stale one: drop it rather than store fields a reader could pair with the other writer's stamp.
        var observed = Volatile.Read(ref slot.Stamp);
        if (observed < 0
            || observed > index
            || Interlocked.CompareExchange(ref slot.Stamp, StampInProgress, observed) != observed)
        {
            Interlocked.Increment(ref _abandoned);
            return;
        }

        slot.Ts = timestamp;
        slot.Dur = duration;
        slot.Id = id;
        slot.Kind = (ushort)kind;
        slot.Arg = arg;
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
        public ushort Kind;
        public ushort Arg;
        public int Tid;
    }
}
