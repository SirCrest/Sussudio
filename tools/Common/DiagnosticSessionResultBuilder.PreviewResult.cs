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
        var previewCadenceMetrics = analysis.PreviewCadenceMetrics;
        var previewScheduler = analysis.PreviewScheduler;
        var visualCadenceMetrics = analysis.VisualCadenceMetrics;

        return new DiagnosticSessionPreviewResultProjection(
            PreviewCadenceOnePercentLowFpsAtEnd: previewCadenceMetrics.OnePercentLowFpsAtEnd,
            PreviewCadenceMinOnePercentLowFpsObserved: previewCadenceMetrics.MinOnePercentLowFpsObserved,
            PreviewSchedulerDroppedAtEnd: previewScheduler.DroppedAtEnd,
            PreviewSchedulerDeadlineDropsAtEnd: previewScheduler.DeadlineDropsAtEnd,
            PreviewSchedulerClearedDropsAtEnd: previewScheduler.ClearedDropsAtEnd,
            PreviewSchedulerUnderflowsAtEnd: previewScheduler.UnderflowsAtEnd,
            PreviewSchedulerResumeReprimesAtEnd: previewScheduler.ResumeReprimesAtEnd,
            PreviewSchedulerDroppedDelta: previewScheduler.DroppedDelta,
            PreviewSchedulerDeadlineDropsDelta: previewScheduler.DeadlineDropsDelta,
            PreviewSchedulerClearedDropsDelta: previewScheduler.ClearedDropsDelta,
            PreviewSchedulerUnderflowsDelta: previewScheduler.UnderflowsDelta,
            PreviewSchedulerResumeReprimesDelta: previewScheduler.ResumeReprimesDelta,
            PreviewSchedulerLastDropReasonAtEnd: previewScheduler.LastDropReasonAtEnd,
            PreviewSchedulerLastUnderflowReasonAtEnd: previewScheduler.LastUnderflowReasonAtEnd,
            PreviewSchedulerLastUnderflowInputAgeMsAtEnd: previewScheduler.LastUnderflowInputAgeMsAtEnd,
            PreviewSchedulerLastUnderflowOutputAgeMsAtEnd: previewScheduler.LastUnderflowOutputAgeMsAtEnd,
            PreviewSchedulerMaxScheduleLateMsObserved: previewScheduler.MaxScheduleLateMsObserved,
            PreviewSchedulerScheduleLateDelta: previewScheduler.ScheduleLateDelta,
            VisualCadenceOutputFpsAtEnd: visualCadenceMetrics.OutputFpsAtEnd,
            VisualCadenceChangeFpsAtEnd: visualCadenceMetrics.ChangeFpsAtEnd,
            VisualCadenceMinChangeFpsObserved: visualCadenceMetrics.MinChangeFpsObserved,
            VisualCadenceRepeatPercentAtEnd: visualCadenceMetrics.RepeatPercentAtEnd,
            VisualCadenceMaxRepeatPercentObserved: visualCadenceMetrics.MaxRepeatPercentObserved,
            VisualCadenceRepeatFramesAtEnd: visualCadenceMetrics.RepeatFramesAtEnd,
            VisualCadenceLongestRepeatRunAtEnd: visualCadenceMetrics.LongestRepeatRunAtEnd);
    }
}
