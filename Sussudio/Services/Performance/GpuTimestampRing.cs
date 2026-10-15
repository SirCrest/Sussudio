using System;
using System.Diagnostics;

namespace Sussudio.Services.Performance;

/// <summary>Outcome of a non-blocking read of one finished GPU timing slot.</summary>
public enum GpuTimestampPoll
{
    /// <summary>The GPU has not produced the result yet (S_FALSE). Try again on a later frame; never wait.</summary>
    NotReady,

    /// <summary>The result was read into the out parameters.</summary>
    Ready,

    /// <summary>The query machinery failed (device removed, driver error). The ring disables itself.</summary>
    Failed,
}

/// <summary>The render pass a GPU timing span brackets. Values index per-path tallies; append, never renumber.</summary>
public enum GpuTimingPath : byte
{
    /// <summary>D3D11 <c>VideoProcessorBlt</c> (the driver's own conversion and scaling work).</summary>
    VideoProcessor = 0,

    /// <summary>The pixel-shader <c>Draw</c> of the NV12 shader path (external D3D textures).</summary>
    Nv12Shader = 1,

    /// <summary>The pixel-shader <c>Draw</c> of the HDR tone-map or passthrough path.</summary>
    HdrShader = 2,
}

/// <summary>
/// The D3D11 side of GPU timing, one query set per slot (a timestamp-disjoint query bracketing a begin and an end
/// timestamp). Kept behind an interface so the ring's bookkeeping and failure handling can be verified without a
/// GPU. Every call happens on the render thread that owns the immediate context.
/// </summary>
public interface IGpuTimestampBackend
{
    /// <summary>Number of query sets the backend created.</summary>
    int SlotCount { get; }

    /// <summary>Opens the slot's disjoint query and records its start timestamp. False when the call failed.</summary>
    bool Begin(int slot);

    /// <summary>Records the slot's end timestamp and closes its disjoint query.</summary>
    void End(int slot);

    /// <summary>
    /// Reads a finished slot without flushing or waiting. On <see cref="GpuTimestampPoll.Ready"/>,
    /// <paramref name="gpuTicks"/> is end minus begin in units of <paramref name="gpuFrequency"/> per second and
    /// <paramref name="disjoint"/> says the GPU clock was unstable during the span (the value is unusable).
    /// </summary>
    GpuTimestampPoll Poll(int slot, out long gpuTicks, out long gpuFrequency, out bool disjoint);
}

/// <summary>
/// Ring of GPU timing slots over an <see cref="IGpuTimestampBackend"/>, owned by the preview render thread.
/// <c>Begin</c> first polls the oldest pending slot (non-blocking), then claims a free slot; <c>End</c> closes it and
/// queues it as pending. Results arrive a few frames late and are recorded then: the duration into
/// <c>preview.gpu_render</c>, and, while a frame trace runs, a <c>PreviewGpuRender</c> span anchored at the CPU
/// timestamp the frame's render pass was submitted (GPU timestamps are in the GPU's own clock domain, not QPC, so
/// only the duration is a GPU measurement; the start is a CPU time) with the frame's arrival tick as its id.
///
/// A span of exactly zero GPU ticks is not a measurement: the GPU's begin and end timestamps were identical, so as far
/// as the queue that wrote them is concerned nothing ran between them (work executed elsewhere or was deferred, which
/// the video-processor path does on some drivers). Such a span is counted in <c>preview.gpu_timing_unmeasured</c> and
/// tallied per <see cref="GpuTimingPath"/>; it is never recorded as a zero-length render, which would pull the
/// histogram's percentiles toward zero. A real but sub-microsecond span is recorded as 1 us so it can never be
/// mistaken for an unmeasured one.
///
/// Rules: never wait (a pending slot that is not ready stays pending, and a ring whose slots are all pending skips
/// the frame and counts it in <c>preview.gpu_timing_skipped</c>); never leave a query open (End on a slot that is
/// not the open one is ignored, and the renderer closes the span in a <c>finally</c>); a disjoint result or an
/// impossible duration is skipped, not recorded; a slot that stays not-ready past <see cref="StaleAfterSeconds"/> is
/// abandoned so one lost result cannot starve the ring; any backend failure disables the ring (it reports
/// <see cref="FailureReason"/> and does nothing more) until the owner disposes and recreates it.
/// Not thread-safe by design: one render thread uses it. Allocation-free after construction.
/// Contract: docs/architecture/performance-telemetry.md section 2.2.1.
/// </summary>
public sealed class GpuTimestampRing
{
    public const int MinSlots = 4;
    public const int MaxSlots = 8;
    public const double StaleAfterSeconds = 1.0;
    private const int PathCount = 3;

    // A span of more than ten seconds is a corrupt value (a driver reporting a bogus frequency), not a render pass.
    private const double MaxPlausibleMicroseconds = 10_000_000;

    private readonly IGpuTimestampBackend _backend;
    private readonly Func<long> _timestamp;
    private readonly long _staleTicks;
    private readonly long[] _frameIds;
    private readonly long[] _submitTimestamps;
    private readonly GpuTimingPath[] _paths;
    private readonly long[] _recordedByPath = new long[PathCount];
    private readonly long[] _unmeasuredByPath = new long[PathCount];
    private int _head;
    private int _tail;
    private int _pending;
    private int _open = -1;
    private string? _failureReason;

    public GpuTimestampRing(IGpuTimestampBackend backend, Func<long>? timestamp = null)
    {
        _backend = backend;
        _timestamp = timestamp ?? Stopwatch.GetTimestamp;
        _staleTicks = (long)(StaleAfterSeconds * Stopwatch.Frequency);
        var slots = Math.Clamp(backend.SlotCount, 1, MaxSlots);
        _frameIds = new long[slots];
        _submitTimestamps = new long[slots];
        _paths = new GpuTimingPath[slots];
    }

    /// <summary>Slots in use by the ring (the backend's count, capped at <see cref="MaxSlots"/>).</summary>
    public int SlotCount => _frameIds.Length;

    /// <summary>Why the ring disabled itself, or null while it works.</summary>
    public string? FailureReason => _failureReason;

    public bool IsFailed => _failureReason is not null;

    /// <summary>Results received and recorded since creation.</summary>
    public long Recorded { get; private set; }

    /// <summary>Spans that reported exactly zero GPU ticks since creation (counted, not recorded).</summary>
    public long Unmeasured { get; private set; }

    /// <summary>Recorded spans whose real duration was under one microsecond and were recorded as 1 us.</summary>
    public long SubMicrosecond { get; private set; }

    /// <summary>Spans of <paramref name="path"/> recorded into the histogram since creation.</summary>
    public long RecordedFor(GpuTimingPath path) => _recordedByPath[PathIndex(path)];

    /// <summary>Spans of <paramref name="path"/> that reported exactly zero GPU ticks since creation.</summary>
    public long UnmeasuredFor(GpuTimingPath path) => _unmeasuredByPath[PathIndex(path)];

    /// <summary>
    /// Opens a timing span for the frame about to draw. Returns the slot to pass to <see cref="End"/>, or -1 when the
    /// frame is not timed (ring failed, every slot pending, or a span is already open).
    /// </summary>
    public int Begin(long frameId, long submitTimestamp, GpuTimingPath path = GpuTimingPath.VideoProcessor)
    {
        if (_failureReason is not null)
        {
            return -1;
        }

        Drain();
        if (_failureReason is not null || _open >= 0)
        {
            return -1;
        }

        if (_pending >= _frameIds.Length)
        {
            PerfMetrics.PreviewGpuTimingSkipped.Increment();
            return -1;
        }

        var slot = _head;
        bool begun;
        try
        {
            begun = _backend.Begin(slot);
        }
        catch (Exception ex)
        {
            Fail("Begin threw " + ex.GetType().Name);
            return -1;
        }

        if (!begun)
        {
            Fail("Begin failed");
            return -1;
        }

        _frameIds[slot] = frameId;
        _submitTimestamps[slot] = submitTimestamp;
        _paths[slot] = path;
        _open = slot;
        return slot;
    }

    /// <summary>Closes the span opened by <see cref="Begin"/>. Ignores -1 and any slot that is not the open one.</summary>
    public void End(int slot)
    {
        if (slot < 0 || slot != _open)
        {
            return;
        }

        _open = -1;
        try
        {
            _backend.End(slot);
        }
        catch (Exception ex)
        {
            Fail("End threw " + ex.GetType().Name);
            return;
        }

        _head = (slot + 1) % _frameIds.Length;
        _pending++;
    }

    /// <summary>
    /// Polls pending slots oldest first and records every finished one, stopping at the first that is not ready.
    /// Called at the start of <see cref="Begin"/>; the owner may also call it to flush results while idle.
    /// </summary>
    public void Drain()
    {
        while (_pending > 0 && _failureReason is null)
        {
            var slot = _tail;
            GpuTimestampPoll poll;
            long gpuTicks;
            long gpuFrequency;
            bool disjoint;
            try
            {
                poll = _backend.Poll(slot, out gpuTicks, out gpuFrequency, out disjoint);
            }
            catch (Exception ex)
            {
                Fail("Poll threw " + ex.GetType().Name);
                return;
            }

            if (poll == GpuTimestampPoll.NotReady)
            {
                if (_timestamp() - _submitTimestamps[slot] > _staleTicks)
                {
                    // The result is not coming: abandon the slot so the ring keeps cycling. The backend may reuse
                    // its queries; D3D11 allows Begin on a query whose previous result was never read.
                    Retire();
                    PerfMetrics.PreviewGpuTimingSkipped.Increment();
                    continue;
                }

                return;
            }

            if (poll == GpuTimestampPoll.Failed)
            {
                Retire();
                Fail("Poll failed");
                return;
            }

            var frameId = _frameIds[slot];
            var submitTimestamp = _submitTimestamps[slot];
            var path = _paths[slot];
            Retire();
            Record(frameId, submitTimestamp, path, gpuTicks, gpuFrequency, disjoint);
        }
    }

    private void Retire()
    {
        _tail = (_tail + 1) % _frameIds.Length;
        _pending--;
    }

    private void Record(long frameId, long submitTimestamp, GpuTimingPath path, long gpuTicks, long gpuFrequency, bool disjoint)
    {
        if (disjoint || gpuFrequency <= 0 || gpuTicks < 0)
        {
            PerfMetrics.PreviewGpuTimingSkipped.Increment();
            return;
        }

        var pathIndex = PathIndex(path);
        if (gpuTicks == 0)
        {
            Unmeasured++;
            _unmeasuredByPath[pathIndex]++;
            PerfMetrics.PreviewGpuTimingUnmeasured.Increment();
            return;
        }

        var exactMicroseconds = gpuTicks * 1_000_000d / gpuFrequency;
        if (exactMicroseconds > MaxPlausibleMicroseconds)
        {
            PerfMetrics.PreviewGpuTimingSkipped.Increment();
            return;
        }

        var microseconds = (long)exactMicroseconds;
        if (microseconds == 0)
        {
            // Real work shorter than the histogram's one-microsecond resolution: record the floor, never 0.
            microseconds = 1;
            SubMicrosecond++;
        }

        Recorded++;
        _recordedByPath[pathIndex]++;
        PerfMetrics.PreviewGpuRender.Record(microseconds);
        if (FrameTrace.IsEnabled)
        {
            var qpcTicks = microseconds * Stopwatch.Frequency / 1_000_000;
            FrameTrace.Record(FrameTraceEventKind.PreviewGpuRender, submitTimestamp, qpcTicks, frameId);
        }
    }

    private static int PathIndex(GpuTimingPath path)
    {
        var index = (int)path;
        return (uint)index < PathCount ? index : 0;
    }

    private void Fail(string reason)
    {
        _failureReason = reason;
        _open = -1;
        _pending = 0;
    }
}
