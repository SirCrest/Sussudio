using System;
using System.Diagnostics;

namespace Sussudio.Services.Performance;

/// <summary>
/// Times a block that has several exits (returns, throws) without re-indenting it:
/// <c>using var scope = new PerfScope(histogram, kind);</c> records the elapsed time into
/// the histogram and one trace span when the enclosing scope ends, however it ends. A readonly struct,
/// so it allocates nothing and <c>Dispose</c> is a direct call; the cost is two QPC reads, one histogram record and
/// one trace check. Use it for work that runs at most a few times per second or off the frame path; per-frame sites
/// record explicitly beside their own timestamps instead.
/// </summary>
public readonly struct PerfScope : IDisposable
{
    private readonly PerfHistogram? _histogram;
    private readonly FrameTraceEventKind _kind;
    private readonly long _traceId;
    private readonly long _startTick;

    /// <param name="histogram">Histogram that receives the elapsed time, in microseconds.</param>
    /// <param name="kind">Trace stage of the span.</param>
    /// <param name="traceId">Trace id of the span, or -1 when it is not tied to one frame or command.</param>
    public PerfScope(PerfHistogram histogram, FrameTraceEventKind kind, long traceId = -1)
    {
        _histogram = histogram;
        _kind = kind;
        _traceId = traceId;
        _startTick = Stopwatch.GetTimestamp();
    }

    public void Dispose()
    {
        // A default(PerfScope) has no histogram and records nothing.
        if (_histogram is null)
        {
            return;
        }

        var elapsedTicks = Stopwatch.GetTimestamp() - _startTick;
        _histogram.RecordTicks(elapsedTicks);
        FrameTrace.Record(_kind, _startTick, elapsedTicks, _traceId);
    }
}
