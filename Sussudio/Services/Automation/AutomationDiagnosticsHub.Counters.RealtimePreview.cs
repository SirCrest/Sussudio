using System;
using System.Threading;
using Sussudio.Models;

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
}
