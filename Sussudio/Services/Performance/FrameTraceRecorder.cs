using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace Sussudio.Services.Performance;

public sealed record FrameTraceStatus(
    bool Running,
    bool HasBuffer,
    long Recorded,
    long Retained,
    long Overwritten,
    int Capacity,
    DateTime? StartedUtc,
    double DurationMs);

public readonly record struct FrameTraceStartResult(bool Success, string? Error, int Capacity);

/// <summary>
/// Owns the active (or last frozen) <see cref="FrameTraceBuffer"/>. Off by default: while no trace is running every
/// record call is one volatile read and a return. <c>Start</c> allocates a ring, <c>Stop</c> freezes it (kept until
/// the next start so it can still be dumped), and <c>TryWriteChromeTrace</c> writes Chrome Trace Event JSON that
/// Perfetto and chrome://tracing open. Writers that already passed the enabled check when <c>Stop</c> runs may land
/// one last event in the frozen buffer; that is harmless and bounded. They can never land it in the next trace, even
/// when that trace reuses the ring: a writer records with the generation it read together with the buffer, and the
/// ring drops an event whose generation has ended.
/// </summary>
public sealed class FrameTraceRecorder
{
    // Events per chunk of a dump walk: 1,024 x 40 bytes = 40 KB, well under the large object heap threshold.
    private const int DumpChunkEvents = 1024;

    // A dump hands its JSON to the stream whenever this much is pending. Utf8JsonWriter never flushes by itself, so
    // without this a full ring's hundreds of megabytes of JSON would sit in its buffer until the end.
    private const int DumpFlushBytes = 32 * 1024;

    private readonly object _gate = new();
    private readonly PerfMarkerLog _markers;
    private readonly PerfThreadNames _threads;
    private volatile ActiveTrace? _active;
    private FrameTraceBuffer? _frozen;

    // Dumps currently walking a ring (guarded by _gate). A ring is reused for the next trace only when this is zero.
    private int _dumpsInProgress;

    public FrameTraceRecorder(PerfMarkerLog markers, PerfThreadNames threads)
    {
        _markers = markers;
        _threads = threads;
    }

    /// <summary>
    /// Raised, outside the lock and on the calling thread, after a trace started or stopped (the value of
    /// <see cref="IsEnabled"/> changed). Demand-driven sensors subscribe so none polls while no trace runs.
    /// Handlers must be quick and must not throw.
    /// </summary>
    public event Action? Changed;

    /// <summary>True while a trace is recording. One volatile read.</summary>
    public bool IsEnabled => _active is not null;

    public void Record(FrameTraceEventKind kind, long startTimestamp, long durationTicks, long id, ushort arg = 0)
    {
        var trace = _active;
        if (trace is null)
        {
            return;
        }

        trace.Buffer.RecordForTrace(trace.Generation, kind, startTimestamp, durationTicks, id, arg);
    }

    public void RecordInstant(FrameTraceEventKind kind, long timestamp, long id, ushort arg = 0)
    {
        var trace = _active;
        if (trace is null)
        {
            return;
        }

        trace.Buffer.RecordInstantForTrace(trace.Generation, kind, timestamp, id, arg);
    }

    /// <summary>
    /// Starts a trace with the requested capacity (rounded and clamped). Rejected while one is running. The ring of the
    /// previous (frozen) trace is reused when its capacity is the same and no dump is reading it, so repeated traces do
    /// not each allocate a multi-megabyte array on the large object heap.
    /// </summary>
    public FrameTraceStartResult Start(int requestedCapacity)
    {
        FrameTraceStartResult result;
        lock (_gate)
        {
            if (_active is { } running)
            {
                return new FrameTraceStartResult(false, "A frame trace is already running; stop it before starting another.", running.Buffer.Capacity);
            }

            FrameTraceBuffer buffer;
            if (_frozen is { } previous
                && _dumpsInProgress == 0
                && previous.Capacity == FrameTraceBuffer.NormalizeCapacity(requestedCapacity))
            {
                previous.Restart();
                buffer = previous;
            }
            else
            {
                buffer = new FrameTraceBuffer(requestedCapacity);
            }

            _frozen = null;
            _active = new ActiveTrace(buffer, buffer.Generation);
            result = new FrameTraceStartResult(true, null, buffer.Capacity);
        }

        RaiseChanged();
        return result;
    }

    /// <summary>Freezes the running trace. Harmless when nothing is running.</summary>
    public FrameTraceStatus Stop()
    {
        FrameTraceStatus status;
        var stopped = false;
        lock (_gate)
        {
            if (_active is { } running)
            {
                running.Buffer.Freeze();
                _frozen = running.Buffer;
                _active = null;
                stopped = true;
            }

            status = StatusLocked();
        }

        if (stopped)
        {
            RaiseChanged();
        }

        return status;
    }

    public FrameTraceStatus GetStatus()
    {
        lock (_gate)
        {
            return StatusLocked();
        }
    }

    /// <summary>
    /// Writes Chrome Trace Event JSON for the running or last frozen trace. Names, not numbers: every event carries
    /// its stage name, category and the frame's arrival timestamp in <c>args.id</c> (-1 for events with no frame);
    /// a non-zero stage arg is written as <c>args.arg</c> and a preview drop also names it in <c>args.reason</c>;
    /// thread names and agent markers are emitted as metadata and instant events. A thread that exited keeps its role
    /// with a "(retired)" suffix. Returns false (and writes nothing) when no trace has been started.
    /// The ring is streamed, never copied: two walks in fixed chunks through one pooled buffer (the first finds the
    /// time origin and the threads that appear, the second writes, handing the stream about 32 KB at a time), so a dump
    /// allocates a few tens of kilobytes however large the ring is. <paramref name="cancellationToken"/> is checked once
    /// per chunk of each walk: a canceled dump throws <see cref="OperationCanceledException"/>, releases the ring and
    /// leaves a partial trace in <paramref name="stream"/> for the caller to discard.
    /// </summary>
    public bool TryWriteChromeTrace(Stream stream, out string? error, CancellationToken cancellationToken = default)
    {
        FrameTraceBuffer? buffer;
        lock (_gate)
        {
            buffer = _active?.Buffer ?? _frozen;
            if (buffer is not null)
            {
                _dumpsInProgress++;
            }
        }

        if (buffer is null)
        {
            error = "No frame trace has been started.";
            return false;
        }

        try
        {
            WriteChromeTrace(buffer, stream, cancellationToken);
        }
        finally
        {
            lock (_gate)
            {
                _dumpsInProgress--;
            }
        }

        error = null;
        return true;
    }

    private void WriteChromeTrace(FrameTraceBuffer buffer, Stream stream, CancellationToken cancellationToken)
    {
        var chunk = ArrayPool<FrameTraceEvent>.Shared.Rent(DumpChunkEvents);
        try
        {
            var chunkSpan = chunk.AsSpan(0, DumpChunkEvents);

            // Both walks cover the same index range, captured once. A running trace keeps writing meanwhile: a slot
            // lapped between the walks is dropped by the second one, and one that only published after the first
            // walk skipped it is dropped below if it predates the origin.
            var range = buffer.BeginRead();

            // Walk 1: the earliest timestamp (the origin of the trace's time axis) and which threads appear.
            var origin = buffer.StartTimestamp;
            var tids = new SortedSet<int>();
            var cursor = range;
            int count;
            while ((count = buffer.ReadChunk(ref cursor, chunkSpan)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                for (var i = 0; i < count; i++)
                {
                    ref readonly var e = ref chunk[i];
                    if (e.Ts < origin)
                    {
                        origin = e.Ts;
                    }

                    tids.Add(e.Tid);
                }
            }

            var stopTimestamp = buffer.StopTimestamp;
            var markers = _markers.Snapshot()
                .Where(m => m.Timestamp >= buffer.StartTimestamp && (stopTimestamp == 0 || m.Timestamp <= stopTimestamp))
                .ToList();
            var threadNames = _threads.Snapshot();

            var pid = Environment.ProcessId;
            var toMicroseconds = 1_000_000d / Stopwatch.Frequency;
            long retained = 0;
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
            {
                writer.WriteStartObject();
                writer.WriteString("displayTimeUnit", "ms");
                writer.WriteStartArray("traceEvents");

                WriteMetadata(writer, "process_name", pid, 0, "Sussudio");
                foreach (var tid in tids)
                {
                    if (threadNames.TryGetValue(tid, out var name))
                    {
                        WriteMetadata(writer, "thread_name", pid, tid, name);
                    }
                }

                // Walk 2: the events.
                cursor = range;
                while ((count = buffer.ReadChunk(ref cursor, chunkSpan)) > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    for (var i = 0; i < count; i++)
                    {
                        ref readonly var e = ref chunk[i];
                        if (e.Ts < origin)
                        {
                            continue;
                        }

                        WriteEvent(writer, e, pid, origin, toMicroseconds);
                        retained++;
                        if (writer.BytesPending >= DumpFlushBytes)
                        {
                            writer.Flush();
                        }
                    }
                }

                foreach (var marker in markers)
                {
                    writer.WriteStartObject();
                    writer.WriteString("name", "marker");
                    writer.WriteString("cat", "marker");
                    writer.WriteString("ph", "i");
                    writer.WriteString("s", "g");
                    writer.WriteNumber("ts", (marker.Timestamp - origin) * toMicroseconds);
                    writer.WriteNumber("pid", pid);
                    writer.WriteNumber("tid", 0);
                    writer.WriteStartObject("args");
                    writer.WriteString("text", marker.Text);
                    writer.WriteString("timestampUtc", marker.TimestampUtc.ToString("O"));
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();

                writer.WriteStartObject("sussudio");
                writer.WriteNumber("qpcFrequency", Stopwatch.Frequency);
                writer.WriteNumber("originQpc", origin);
                writer.WriteString("startedUtc", buffer.StartedUtc.ToString("O"));
                writer.WriteNumber("capacity", buffer.Capacity);
                writer.WriteNumber("recorded", buffer.Recorded);
                writer.WriteNumber("retained", retained);
                writer.WriteNumber("overwritten", buffer.Overwritten);
                writer.WriteNumber("abandoned", buffer.Abandoned);
                writer.WriteBoolean("running", stopTimestamp == 0);
                writer.WriteEndObject();

                writer.WriteEndObject();
            }
        }
        finally
        {
            ArrayPool<FrameTraceEvent>.Shared.Return(chunk);
        }
    }

    private static void WriteEvent(Utf8JsonWriter writer, in FrameTraceEvent e, int pid, long origin, double toMicroseconds)
    {
        writer.WriteStartObject();
        writer.WriteString("name", FrameTraceEventKinds.Name(e.Kind));
        writer.WriteString("cat", FrameTraceEventKinds.Category(e.Kind));
        if (e.IsInstant)
        {
            writer.WriteString("ph", "i");
            writer.WriteString("s", "t");
            writer.WriteNumber("ts", (e.Ts - origin) * toMicroseconds);
        }
        else
        {
            writer.WriteString("ph", "X");
            writer.WriteNumber("ts", (e.Ts - origin) * toMicroseconds);
            writer.WriteNumber("dur", e.Dur * toMicroseconds);
        }

        writer.WriteNumber("pid", pid);
        writer.WriteNumber("tid", e.Tid);
        writer.WriteStartObject("args");
        writer.WriteNumber("id", e.Id);
        if (e.Arg != 0)
        {
            writer.WriteNumber("arg", e.Arg);
            if (e.Kind == FrameTraceEventKind.PreviewDrop)
            {
                writer.WriteString("reason", PreviewDropReasons.Name((PreviewDropReason)e.Arg));
            }
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private void RaiseChanged()
    {
        // A subscriber must never turn a trace command into a failure; sensors report their own faults.
        try
        {
            Changed?.Invoke();
        }
        catch (Exception)
        {
            // Intentionally swallowed: the trace start or stop itself already succeeded.
        }
    }

    private FrameTraceStatus StatusLocked()
    {
        var buffer = _active?.Buffer ?? _frozen;
        if (buffer is null)
        {
            return new FrameTraceStatus(false, false, 0, 0, 0, 0, null, 0);
        }

        var end = buffer.StopTimestamp != 0 ? buffer.StopTimestamp : Stopwatch.GetTimestamp();
        var durationMs = Math.Max(0, end - buffer.StartTimestamp) * 1000d / Stopwatch.Frequency;
        return new FrameTraceStatus(
            _active is not null,
            true,
            buffer.Recorded,
            buffer.Retained,
            buffer.Overwritten,
            buffer.Capacity,
            buffer.StartedUtc,
            durationMs);
    }

    // The running trace as one reference, so a writer reads the buffer and its generation in a single volatile read.
    private sealed record ActiveTrace(FrameTraceBuffer Buffer, int Generation);

    private static void WriteMetadata(Utf8JsonWriter writer, string kind, int pid, int tid, string name)
    {
        writer.WriteStartObject();
        writer.WriteString("name", kind);
        writer.WriteString("ph", "M");
        writer.WriteNumber("pid", pid);
        writer.WriteNumber("tid", tid);
        writer.WriteStartObject("args");
        writer.WriteString("name", name);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }
}

/// <summary>
/// Process-wide flight recorder. Hot paths call <c>if (FrameTrace.IsEnabled) FrameTrace.Record(...)</c> (or just
/// <see cref="Record"/>, which repeats the check); a disabled trace costs one volatile read.
/// </summary>
public static class FrameTrace
{
    public static readonly FrameTraceRecorder Recorder = new(PerfMarkers.Log, PerfThreadNames.Default);

    public static bool IsEnabled => Recorder.IsEnabled;

    public static void Record(FrameTraceEventKind kind, long startTimestamp, long durationTicks, long id, ushort arg = 0) =>
        Recorder.Record(kind, startTimestamp, durationTicks, id, arg);

    public static void RecordInstant(FrameTraceEventKind kind, long timestamp, long id, ushort arg = 0) =>
        Recorder.RecordInstant(kind, timestamp, id, arg);

    /// <summary>
    /// Labels the calling thread in trace dumps (called by <c>HotThreads.Register</c>). Keep the returned entry and
    /// pass it to <see cref="RetireThreadName"/> when the thread's role ends.
    /// </summary>
    public static ThreadNameEntry NameCurrentThread(string name) =>
        PerfThreadNames.Default.Set(Environment.CurrentManagedThreadId, name);

    /// <summary>Ends a thread label so the id, which the runtime recycles, does not keep naming unrelated work.</summary>
    public static void RetireThreadName(int managedThreadId, ThreadNameEntry entry) =>
        PerfThreadNames.Default.Retire(managedThreadId, entry);
}
