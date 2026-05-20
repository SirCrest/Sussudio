using System;
using System.Threading;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private long _lastMjpegTotalDropped;
    private long _lastMjpegDecodeFailures;
    private long _lastMjpegEmitFailures;
    private long _lastMjpegCompressedDropsQueueFull;
    private long _lastMjpegEvalTick;

    private MjpegRecentCounters UpdateMjpegRecentCounters(
        CaptureHealthSnapshot health,
        long nowTick)
    {
        var totalDropped = Math.Max(0, health.MjpegTotalDropped);
        var decodeFailures = Math.Max(0, health.MjpegDecodeFailures);
        var emitFailures = Math.Max(0, health.MjpegEmitFailures);
        var compressedQueueDrops = Math.Max(0, health.MjpegCompressedDropsQueueFull);
        var previousTick = Interlocked.Exchange(ref _lastMjpegEvalTick, nowTick);
        var previousTotalDropped = Interlocked.Exchange(ref _lastMjpegTotalDropped, totalDropped);
        var previousDecodeFailures = Interlocked.Exchange(ref _lastMjpegDecodeFailures, decodeFailures);
        var previousEmitFailures = Interlocked.Exchange(ref _lastMjpegEmitFailures, emitFailures);
        var previousCompressedQueueDrops = Interlocked.Exchange(ref _lastMjpegCompressedDropsQueueFull, compressedQueueDrops);

        if (previousTick == 0 || nowTick < previousTick)
        {
            return MjpegRecentCounters.Empty;
        }

        return new MjpegRecentCounters(
            Math.Max(0, totalDropped - previousTotalDropped),
            Math.Max(0, decodeFailures - previousDecodeFailures),
            Math.Max(0, emitFailures - previousEmitFailures),
            Math.Max(0, compressedQueueDrops - previousCompressedQueueDrops));
    }
}
