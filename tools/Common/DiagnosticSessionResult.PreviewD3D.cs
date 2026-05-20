namespace Sussudio.Tools;

public sealed partial class DiagnosticSessionResult
{
    // Preview D3D frame-stat and CPU timing summary.
    public long PreviewD3DFrameStatsMissedRefreshDelta { get; init; }
    public long PreviewD3DFrameStatsFailureDelta { get; init; }
    public int PreviewD3DMaxRecentSlowFramesObserved { get; init; }
    public string PreviewD3DLatestSlowFrameReason { get; init; } = string.Empty;
    public double PreviewD3DLatestSlowFrameOverBudgetMs { get; init; }
    public double PreviewD3DLatestSlowFramePresentIntervalMs { get; init; }
    public double PreviewD3DLatestSlowFrameTotalFrameCpuMs { get; init; }
    public double PreviewD3DLatestSlowFramePresentCallMs { get; init; }
    public int PreviewD3DLatestSlowFramePendingFrameCount { get; init; }
    public double PreviewD3DInputUploadCpuP99MsAtEnd { get; init; }
    public double PreviewD3DInputUploadCpuMaxMsObserved { get; init; }
    public double PreviewD3DRenderSubmitCpuP99MsAtEnd { get; init; }
    public double PreviewD3DRenderSubmitCpuMaxMsObserved { get; init; }
    public double PreviewD3DPresentCallP99MsAtEnd { get; init; }
    public double PreviewD3DPresentCallMaxMsObserved { get; init; }
    public double PreviewD3DTotalFrameCpuP99MsAtEnd { get; init; }
    public double PreviewD3DTotalFrameCpuMaxMsObserved { get; init; }
}
