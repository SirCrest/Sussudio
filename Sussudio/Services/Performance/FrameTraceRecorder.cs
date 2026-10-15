using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

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
/// one last event in the frozen buffer; that is harmless and bounded.
/// </summary>
public sealed class FrameTraceRecorder
{
    private readonly object _gate = new();
    private readonly PerfMarkerLog _markers;
    private readonly PerfThreadNames _threads;
    private volatile FrameTraceBuffer? _active;
    private FrameTraceBuffer? _frozen;

    public FrameTraceRecorder(PerfMarkerLog markers, PerfThreadNames threads)
    {
        _markers = markers;
        _threads = threads;
    }

    /// <summary>True while a trace is recording. One volatile read.</summary>
    public bool IsEnabled => _active is not null;

    public void Record(FrameTraceEventKind kind, long startTimestamp, long durationTicks, long id, ushort arg = 0)
    {
        var buffer = _active;
        if (buffer is null)
        {
            return;
        }

        buffer.Record(kind, startTimestamp, durationTicks, id, arg);
    }

    public void RecordInstant(FrameTraceEventKind kind, long timestamp, long id, ushort arg = 0)
    {
        var buffer = _active;
        if (buffer is null)
        {
            return;
        }

        buffer.RecordInstant(kind, timestamp, id, arg);
    }

    /// <summary>Starts a trace with the requested capacity (rounded and clamped). Rejected while one is running.</summary>
    public FrameTraceStartResult Start(int requestedCapacity)
    {
        lock (_gate)
        {
            if (_active is { } running)
            {
                return new FrameTraceStartResult(false, "A frame trace is already running; stop it before starting another.", running.Capacity);
            }

            var buffer = new FrameTraceBuffer(requestedCapacity);
            _frozen = null;
            _active = buffer;
            return new FrameTraceStartResult(true, null, buffer.Capacity);
        }
    }

    /// <summary>Freezes the running trace. Harmless when nothing is running.</summary>
    public FrameTraceStatus Stop()
    {
        lock (_gate)
        {
            if (_active is { } running)
            {
                running.Freeze();
                _frozen = running;
                _active = null;
            }

            return StatusLocked();
        }
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
    /// thread names and agent markers are emitted as metadata and instant events. Returns false (and writes nothing) when no trace has been started.
    /// </summary>
    public bool TryWriteChromeTrace(Stream stream, out string? error)
    {
        FrameTraceBuffer? buffer;
        lock (_gate)
        {
            buffer = _active ?? _frozen;
        }

        if (buffer is null)
        {
            error = "No frame trace has been started.";
            return false;
        }

        var events = buffer.Copy();
        var stopTimestamp = buffer.StopTimestamp;
        var markers = _markers.Snapshot()
            .Where(m => m.Timestamp >= buffer.StartTimestamp && (stopTimestamp == 0 || m.Timestamp <= stopTimestamp))
            .ToList();
        var threadNames = _threads.Snapshot();

        var origin = buffer.StartTimestamp;
        foreach (var e in events)
        {
            if (e.Ts < origin)
            {
                origin = e.Ts;
            }
        }

        var pid = Environment.ProcessId;
        var toMicroseconds = 1_000_000d / Stopwatch.Frequency;
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WriteString("displayTimeUnit", "ms");
            writer.WriteStartArray("traceEvents");

            WriteMetadata(writer, "process_name", pid, 0, "Sussudio");
            foreach (var tid in events.Select(e => e.Tid).Distinct().OrderBy(t => t))
            {
                if (threadNames.TryGetValue(tid, out var name))
                {
                    WriteMetadata(writer, "thread_name", pid, tid, name);
                }
            }

            foreach (var e in events)
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
            writer.WriteNumber("retained", events.Count);
            writer.WriteNumber("overwritten", buffer.Overwritten);
            writer.WriteBoolean("running", stopTimestamp == 0);
            writer.WriteEndObject();

            writer.WriteEndObject();
        }

        error = null;
        return true;
    }

    private FrameTraceStatus StatusLocked()
    {
        var buffer = _active ?? _frozen;
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

    /// <summary>Labels the calling thread in trace dumps (called by <c>HotThreads.Register</c>).</summary>
    public static void NameCurrentThread(string name) =>
        PerfThreadNames.Default.Set(Environment.CurrentManagedThreadId, name);
}
