namespace Sussudio.Tools;

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
