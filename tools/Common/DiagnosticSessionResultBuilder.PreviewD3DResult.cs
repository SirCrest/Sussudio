namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
    private readonly record struct DiagnosticSessionPreviewD3DResultProjection(
        long PreviewD3DFrameStatsMissedRefreshDelta,
        long PreviewD3DFrameStatsFailureDelta,
        int PreviewD3DMaxRecentSlowFramesObserved,
        string PreviewD3DLatestSlowFrameReason,
        double PreviewD3DLatestSlowFrameOverBudgetMs,
        double PreviewD3DLatestSlowFramePresentIntervalMs,
        double PreviewD3DLatestSlowFrameTotalFrameCpuMs,
        double PreviewD3DLatestSlowFramePresentCallMs,
        int PreviewD3DLatestSlowFramePendingFrameCount,
        double PreviewD3DInputUploadCpuP99MsAtEnd,
        double PreviewD3DInputUploadCpuMaxMsObserved,
        double PreviewD3DRenderSubmitCpuP99MsAtEnd,
        double PreviewD3DRenderSubmitCpuMaxMsObserved,
        double PreviewD3DPresentCallP99MsAtEnd,
        double PreviewD3DPresentCallMaxMsObserved,
        double PreviewD3DTotalFrameCpuP99MsAtEnd,
        double PreviewD3DTotalFrameCpuMaxMsObserved);

    private static DiagnosticSessionPreviewD3DResultProjection BuildPreviewD3DResultProjection(
        DiagnosticSessionResultAnalysis analysis)
    {
        var previewD3DMetrics = analysis.PreviewD3DMetrics;

        return new DiagnosticSessionPreviewD3DResultProjection(
            PreviewD3DFrameStatsMissedRefreshDelta: previewD3DMetrics.MissedRefreshDelta,
            PreviewD3DFrameStatsFailureDelta: previewD3DMetrics.StatsFailureDelta,
            PreviewD3DMaxRecentSlowFramesObserved: previewD3DMetrics.MaxRecentSlowFramesObserved,
            PreviewD3DLatestSlowFrameReason: previewD3DMetrics.LatestSlowFrameReason,
            PreviewD3DLatestSlowFrameOverBudgetMs: previewD3DMetrics.LatestSlowFrameOverBudgetMs,
            PreviewD3DLatestSlowFramePresentIntervalMs: previewD3DMetrics.LatestSlowFramePresentIntervalMs,
            PreviewD3DLatestSlowFrameTotalFrameCpuMs: previewD3DMetrics.LatestSlowFrameTotalFrameCpuMs,
            PreviewD3DLatestSlowFramePresentCallMs: previewD3DMetrics.LatestSlowFramePresentCallMs,
            PreviewD3DLatestSlowFramePendingFrameCount: previewD3DMetrics.LatestSlowFramePendingFrameCount,
            PreviewD3DInputUploadCpuP99MsAtEnd: previewD3DMetrics.InputUploadCpuP99MsAtEnd,
            PreviewD3DInputUploadCpuMaxMsObserved: previewD3DMetrics.InputUploadCpuMaxMsObserved,
            PreviewD3DRenderSubmitCpuP99MsAtEnd: previewD3DMetrics.RenderSubmitCpuP99MsAtEnd,
            PreviewD3DRenderSubmitCpuMaxMsObserved: previewD3DMetrics.RenderSubmitCpuMaxMsObserved,
            PreviewD3DPresentCallP99MsAtEnd: previewD3DMetrics.PresentCallP99MsAtEnd,
            PreviewD3DPresentCallMaxMsObserved: previewD3DMetrics.PresentCallMaxMsObserved,
            PreviewD3DTotalFrameCpuP99MsAtEnd: previewD3DMetrics.TotalFrameCpuP99MsAtEnd,
            PreviewD3DTotalFrameCpuMaxMsObserved: previewD3DMetrics.TotalFrameCpuMaxMsObserved);
    }
}
