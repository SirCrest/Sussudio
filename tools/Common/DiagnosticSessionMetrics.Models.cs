namespace Sussudio.Tools;

internal readonly record struct PlaybackCommandHealth(
    long Dropped,
    long Skipped,
    long SubmitFailures,
    long CoalescedScrub,
    long CoalescedSeek,
    long NonCoalescedDropped);

internal sealed class SourceCadenceSessionMetrics
{
    public long MaxSevereGapCountObserved { get; set; }
    public long MaxEstimatedDroppedFramesObserved { get; set; }
    public double MaxDropPercentObserved { get; set; }
}

internal sealed class PreviewCadenceSessionMetrics
{
    public double OnePercentLowFpsAtEnd { get; init; }
    public double MinOnePercentLowFpsObserved { get; set; } = double.PositiveInfinity;
}

internal sealed class VisualCadenceSessionMetrics
{
    public double OutputFpsAtEnd { get; init; }
    public double ChangeFpsAtEnd { get; init; }
    public double MinChangeFpsObserved { get; set; } = double.PositiveInfinity;
    public double RepeatPercentAtEnd { get; init; }
    public double MaxRepeatPercentObserved { get; set; }
    public long RepeatFramesAtEnd { get; init; }
    public long LongestRepeatRunAtEnd { get; init; }
}

internal sealed class PreviewD3DMetrics
{
    public long MissedRefreshDelta { get; init; }
    public long StatsFailureDelta { get; init; }
    public int MaxRecentSlowFramesObserved { get; set; }
    public string LatestSlowFrameReason { get; set; } = string.Empty;
    public double LatestSlowFrameOverBudgetMs { get; set; }
    public double LatestSlowFramePresentIntervalMs { get; set; }
    public double LatestSlowFrameTotalFrameCpuMs { get; set; }
    public double LatestSlowFramePresentCallMs { get; set; }
    public int LatestSlowFramePendingFrameCount { get; set; }
    public double InputUploadCpuP99MsAtEnd { get; init; }
    public double InputUploadCpuMaxMsObserved { get; set; }
    public double RenderSubmitCpuP99MsAtEnd { get; init; }
    public double RenderSubmitCpuMaxMsObserved { get; set; }
    public double PresentCallP99MsAtEnd { get; init; }
    public double PresentCallMaxMsObserved { get; set; }
    public double TotalFrameCpuP99MsAtEnd { get; init; }
    public double TotalFrameCpuMaxMsObserved { get; set; }
}
