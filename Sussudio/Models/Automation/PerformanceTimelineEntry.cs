using System;

namespace Sussudio.Models;

public sealed partial class PerformanceTimelineEntry
{
    public DateTimeOffset TimestampUtc { get; init; }
    public double CaptureFps { get; init; }
    public double PreviewFps { get; init; }
    public int VideoQueueDepth { get; init; }
    public long VideoDrops { get; init; }
    public double CaptureCadenceAverageMs { get; init; }
    public double CaptureCadenceP95Ms { get; init; }
    public double CaptureCadenceP99Ms { get; init; }
    public double CaptureCadenceMaxMs { get; init; }
    public double CaptureCadenceOnePercentLowFps { get; init; }
    public double CaptureCadenceFivePercentLowFps { get; init; }
    public double PreviewCadenceAverageMs { get; init; }
    public double PreviewCadenceP95Ms { get; init; }
    public double PreviewCadenceP99Ms { get; init; }
    public double PreviewCadenceMaxMs { get; init; }
    public double PreviewCadenceOnePercentLowFps { get; init; }
    public double PreviewCadenceFivePercentLowFps { get; init; }
    public double PreviewCadenceSlowFramePercent { get; init; }
}
