using System;
using System.Threading;
using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private long _lastPreviewJitterTotalDropped;
    private long _lastPreviewJitterUnderflows;
    private long _lastPreviewJitterDeadlineDrops;
    private long _lastPreviewJitterScheduleLateCount;
    private double _lastPreviewJitterLastScheduleLateMs;
    private long _lastPreviewJitterEvalTick;
    private long _lastD3DFramesSubmitted;
    private long _lastD3DFramesRendered;
    private long _lastD3DFramesDropped;
    private long _lastD3DRendererEvalTick;
    private long _lastD3DFrameStatsMissedRefreshes;
    private long _lastD3DFrameStatsFailures;
    private long _lastD3DFrameStatsEvalTick;
    private long _lastD3DFrameLatencyWaitTimeouts;
    private long _lastD3DFrameLatencyWaitEvalTick;
    private long _lastMjpegTotalDropped;
    private long _lastMjpegDecodeFailures;
    private long _lastMjpegEmitFailures;
    private long _lastMjpegCompressedDropsQueueFull;
    private long _lastMjpegEvalTick;
    private long _lastFlashbackDroppedFrames;
    private long _lastFlashbackVideoEncoderDroppedFrames;
    private long _lastFlashbackVideoSequenceGaps;
    private long _lastFlashbackGpuFramesDropped;
    private long _lastFlashbackVideoBackpressureEvents;
    private long _lastFlashbackRecordingEvalTick;

    private PreviewJitterRecentCounters UpdatePreviewJitterRecentCounters(
        CaptureHealthSnapshot health,
        long nowTick)
    {
        var totalDropped = Math.Max(0, health.MjpegPreviewJitterTotalDropped);
        var underflows = Math.Max(0, health.MjpegPreviewJitterUnderflowCount);
        var deadlineDrops = Math.Max(0, health.MjpegPreviewJitterDeadlineDropCount);
        var scheduleLateCount = Math.Max(0, health.MjpegPreviewJitterScheduleLateCount);
        var lastScheduleLateMs = Math.Max(0, health.MjpegPreviewJitterLastScheduleLateMs);
        var previousTick = Interlocked.Exchange(ref _lastPreviewJitterEvalTick, nowTick);
        var previousTotalDropped = Interlocked.Exchange(ref _lastPreviewJitterTotalDropped, totalDropped);
        var previousUnderflows = Interlocked.Exchange(ref _lastPreviewJitterUnderflows, underflows);
        var previousDeadlineDrops = Interlocked.Exchange(ref _lastPreviewJitterDeadlineDrops, deadlineDrops);
        var previousScheduleLateCount = Interlocked.Exchange(ref _lastPreviewJitterScheduleLateCount, scheduleLateCount);
        var previousLastScheduleLateMs = _lastPreviewJitterLastScheduleLateMs;
        _lastPreviewJitterLastScheduleLateMs = lastScheduleLateMs;

        if (previousTick == 0 || nowTick < previousTick)
        {
            return PreviewJitterRecentCounters.Empty;
        }

        var recentScheduleLateCount = Math.Max(0, scheduleLateCount - previousScheduleLateCount);
        return new PreviewJitterRecentCounters(
            Math.Max(0, totalDropped - previousTotalDropped),
            Math.Max(0, underflows - previousUnderflows),
            Math.Max(0, deadlineDrops - previousDeadlineDrops),
            recentScheduleLateCount,
            recentScheduleLateCount > 0 ? Math.Max(0, lastScheduleLateMs) : Math.Max(0, lastScheduleLateMs - previousLastScheduleLateMs));
    }

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

    private D3DRendererRecentCounters UpdateD3DRendererRecentCounters(
        PreviewRuntimeSnapshot previewRuntime,
        long nowTick)
    {
        var submitted = Math.Max(0, previewRuntime.D3DFramesSubmitted);
        var rendered = Math.Max(0, previewRuntime.D3DFramesRendered);
        var dropped = Math.Max(0, previewRuntime.D3DFramesDropped);
        var previousTick = Interlocked.Exchange(ref _lastD3DRendererEvalTick, nowTick);
        var previousSubmitted = Interlocked.Exchange(ref _lastD3DFramesSubmitted, submitted);
        var previousRendered = Interlocked.Exchange(ref _lastD3DFramesRendered, rendered);
        var previousDropped = Interlocked.Exchange(ref _lastD3DFramesDropped, dropped);

        if (previousTick <= 0)
        {
            return D3DRendererRecentCounters.Empty;
        }

        return new D3DRendererRecentCounters(
            Math.Max(0, submitted - previousSubmitted),
            Math.Max(0, rendered - previousRendered),
            Math.Max(0, dropped - previousDropped));
    }

    private (long RecentMissedRefreshes, long RecentFailures) UpdateD3DFrameStatsRecentCounters(
        PreviewRuntimeSnapshot previewRuntime,
        long nowTick)
    {
        var missedRefreshes = Math.Max(0, previewRuntime.D3DFrameStatsMissedRefreshCount);
        var failures = Math.Max(0, previewRuntime.D3DFrameStatsFailureCount);
        var previousTick = Interlocked.Exchange(ref _lastD3DFrameStatsEvalTick, nowTick);
        var previousMissedRefreshes = Interlocked.Exchange(ref _lastD3DFrameStatsMissedRefreshes, missedRefreshes);
        var previousFailures = Interlocked.Exchange(ref _lastD3DFrameStatsFailures, failures);

        if (previousTick == 0 || nowTick < previousTick)
        {
            return (0, 0);
        }

        return (
            Math.Max(0, missedRefreshes - previousMissedRefreshes),
            Math.Max(0, failures - previousFailures));
    }

    private long UpdateD3DFrameLatencyWaitRecentCounters(
        PreviewRuntimeSnapshot previewRuntime,
        long nowTick)
    {
        var timeouts = Math.Max(0, previewRuntime.D3DFrameLatencyWaitTimeoutCount);
        var previousTick = Interlocked.Exchange(ref _lastD3DFrameLatencyWaitEvalTick, nowTick);
        var previousTimeouts = Interlocked.Exchange(ref _lastD3DFrameLatencyWaitTimeouts, timeouts);

        if (previousTick == 0 || nowTick < previousTick)
        {
            return 0;
        }

        return Math.Max(0, timeouts - previousTimeouts);
    }

    private FlashbackRecordingRecentCounters UpdateFlashbackRecordingRecentCounters(
        CaptureHealthSnapshot snapshot,
        long nowTick)
    {
        var droppedFrames = snapshot.FlashbackActive ? Math.Max(0, snapshot.FlashbackDroppedFrames) : 0;
        var encoderDroppedFrames = snapshot.FlashbackActive ? Math.Max(0, snapshot.FlashbackVideoEncoderDroppedFrames) : 0;
        var sequenceGaps = snapshot.FlashbackActive ? Math.Max(0, snapshot.FlashbackVideoSequenceGaps) : 0;
        var gpuFramesDropped = snapshot.FlashbackActive ? Math.Max(0, snapshot.FlashbackGpuFramesDropped) : 0;
        var backpressureEvents = snapshot.FlashbackActive ? Math.Max(0, snapshot.FlashbackVideoBackpressureEvents) : 0;

        var previousTick = Interlocked.Exchange(ref _lastFlashbackRecordingEvalTick, nowTick);
        var previousDroppedFrames = Interlocked.Exchange(ref _lastFlashbackDroppedFrames, droppedFrames);
        var previousEncoderDroppedFrames = Interlocked.Exchange(ref _lastFlashbackVideoEncoderDroppedFrames, encoderDroppedFrames);
        var previousSequenceGaps = Interlocked.Exchange(ref _lastFlashbackVideoSequenceGaps, sequenceGaps);
        var previousGpuFramesDropped = Interlocked.Exchange(ref _lastFlashbackGpuFramesDropped, gpuFramesDropped);
        var previousBackpressureEvents = Interlocked.Exchange(ref _lastFlashbackVideoBackpressureEvents, backpressureEvents);

        if (previousTick == 0 || nowTick < previousTick)
        {
            return FlashbackRecordingRecentCounters.Empty;
        }

        return new FlashbackRecordingRecentCounters(
            Math.Max(0, droppedFrames - previousDroppedFrames),
            Math.Max(0, encoderDroppedFrames - previousEncoderDroppedFrames),
            Math.Max(0, sequenceGaps - previousSequenceGaps),
            Math.Max(0, gpuFramesDropped - previousGpuFramesDropped),
            Math.Max(0, backpressureEvents - previousBackpressureEvents));
    }
}
