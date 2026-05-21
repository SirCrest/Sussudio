using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PerformanceTimelineCoreProjection BuildPerformanceTimelineCoreProjection(
        AutomationSnapshot snapshot)
        => new(
            TimestampUtc: snapshot.TimestampUtc,
            CaptureFps: snapshot.CaptureCadenceObservedFps,
            PreviewFps: snapshot.PreviewCadenceObservedFps,
            VideoQueueDepth: snapshot.FfmpegVideoQueueDepth,
            VideoDrops: snapshot.VideoDropsQueueSaturated,
            CaptureCadenceAverageMs: snapshot.CaptureCadenceAverageIntervalMs,
            CaptureCadenceP95Ms: snapshot.CaptureCadenceP95IntervalMs,
            CaptureCadenceP99Ms: snapshot.CaptureCadenceP99IntervalMs,
            CaptureCadenceMaxMs: snapshot.CaptureCadenceMaxIntervalMs,
            CaptureCadenceOnePercentLowFps: snapshot.CaptureCadenceOnePercentLowFps,
            CaptureCadenceFivePercentLowFps: snapshot.CaptureCadenceFivePercentLowFps);

    private readonly record struct PerformanceTimelineCoreProjection(
        DateTimeOffset TimestampUtc,
        double CaptureFps,
        double PreviewFps,
        int VideoQueueDepth,
        long VideoDrops,
        double CaptureCadenceAverageMs,
        double CaptureCadenceP95Ms,
        double CaptureCadenceP99Ms,
        double CaptureCadenceMaxMs,
        double CaptureCadenceOnePercentLowFps,
        double CaptureCadenceFivePercentLowFps);
}
