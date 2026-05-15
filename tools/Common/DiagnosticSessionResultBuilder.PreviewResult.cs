using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
    private readonly record struct DiagnosticSessionPreviewResultProjection(
        double PreviewCadenceOnePercentLowFpsAtEnd,
        double PreviewCadenceMinOnePercentLowFpsObserved,
        long PreviewSchedulerDroppedAtEnd,
        long PreviewSchedulerDeadlineDropsAtEnd,
        long PreviewSchedulerClearedDropsAtEnd,
        long PreviewSchedulerUnderflowsAtEnd,
        long PreviewSchedulerResumeReprimesAtEnd,
        long PreviewSchedulerDroppedDelta,
        long PreviewSchedulerDeadlineDropsDelta,
        long PreviewSchedulerClearedDropsDelta,
        long PreviewSchedulerUnderflowsDelta,
        long PreviewSchedulerResumeReprimesDelta,
        string PreviewSchedulerLastDropReasonAtEnd,
        string PreviewSchedulerLastUnderflowReasonAtEnd,
        double PreviewSchedulerLastUnderflowInputAgeMsAtEnd,
        double PreviewSchedulerLastUnderflowOutputAgeMsAtEnd,
        double PreviewSchedulerMaxScheduleLateMsObserved,
        long PreviewSchedulerScheduleLateDelta,
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
        double PreviewD3DTotalFrameCpuMaxMsObserved,
        double VisualCadenceOutputFpsAtEnd,
        double VisualCadenceChangeFpsAtEnd,
        double VisualCadenceMinChangeFpsObserved,
        double VisualCadenceRepeatPercentAtEnd,
        double VisualCadenceMaxRepeatPercentObserved,
        long VisualCadenceRepeatFramesAtEnd,
        long VisualCadenceLongestRepeatRunAtEnd);

    private static DiagnosticSessionPreviewResultProjection BuildPreviewResultProjection(
        DiagnosticSessionResultAnalysis analysis)
    {
        var lastSnapshot = analysis.LastSnapshot;
        var previewCadenceMetrics = analysis.PreviewCadenceMetrics;
        var previewD3DMetrics = analysis.PreviewD3DMetrics;
        var visualCadenceMetrics = analysis.VisualCadenceMetrics;

        return new DiagnosticSessionPreviewResultProjection(
            PreviewCadenceOnePercentLowFpsAtEnd: previewCadenceMetrics.OnePercentLowFpsAtEnd,
            PreviewCadenceMinOnePercentLowFpsObserved: previewCadenceMetrics.MinOnePercentLowFpsObserved,
            PreviewSchedulerDroppedAtEnd: analysis.PreviewSchedulerDroppedAtEnd,
            PreviewSchedulerDeadlineDropsAtEnd: analysis.PreviewSchedulerDeadlineDropsAtEnd,
            PreviewSchedulerClearedDropsAtEnd: analysis.PreviewSchedulerClearedDropsAtEnd,
            PreviewSchedulerUnderflowsAtEnd: analysis.PreviewSchedulerUnderflowsAtEnd,
            PreviewSchedulerResumeReprimesAtEnd: analysis.PreviewSchedulerResumeReprimesAtEnd,
            PreviewSchedulerDroppedDelta: analysis.PreviewSchedulerDroppedDelta,
            PreviewSchedulerDeadlineDropsDelta: analysis.PreviewSchedulerDeadlineDropsDelta,
            PreviewSchedulerClearedDropsDelta: analysis.PreviewSchedulerClearedDropsDelta,
            PreviewSchedulerUnderflowsDelta: analysis.PreviewSchedulerUnderflowsDelta,
            PreviewSchedulerResumeReprimesDelta: analysis.PreviewSchedulerResumeReprimesDelta,
            PreviewSchedulerLastDropReasonAtEnd: GetString(lastSnapshot, "MjpegPreviewJitterLastDropReason") ?? string.Empty,
            PreviewSchedulerLastUnderflowReasonAtEnd: GetString(lastSnapshot, "MjpegPreviewJitterLastUnderflowReason") ?? string.Empty,
            PreviewSchedulerLastUnderflowInputAgeMsAtEnd: GetDouble(lastSnapshot, "MjpegPreviewJitterLastUnderflowInputAgeMs"),
            PreviewSchedulerLastUnderflowOutputAgeMsAtEnd: GetDouble(lastSnapshot, "MjpegPreviewJitterLastUnderflowOutputAgeMs"),
            PreviewSchedulerMaxScheduleLateMsObserved: analysis.PreviewSchedulerMaxScheduleLateMsObserved,
            PreviewSchedulerScheduleLateDelta: analysis.PreviewSchedulerScheduleLateDelta,
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
            PreviewD3DTotalFrameCpuMaxMsObserved: previewD3DMetrics.TotalFrameCpuMaxMsObserved,
            VisualCadenceOutputFpsAtEnd: visualCadenceMetrics.OutputFpsAtEnd,
            VisualCadenceChangeFpsAtEnd: visualCadenceMetrics.ChangeFpsAtEnd,
            VisualCadenceMinChangeFpsObserved: visualCadenceMetrics.MinChangeFpsObserved,
            VisualCadenceRepeatPercentAtEnd: visualCadenceMetrics.RepeatPercentAtEnd,
            VisualCadenceMaxRepeatPercentObserved: visualCadenceMetrics.MaxRepeatPercentObserved,
            VisualCadenceRepeatFramesAtEnd: visualCadenceMetrics.RepeatFramesAtEnd,
            VisualCadenceLongestRepeatRunAtEnd: visualCadenceMetrics.LongestRepeatRunAtEnd);
    }
}
