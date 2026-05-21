namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
    private readonly record struct DiagnosticSessionPreviewSchedulerResultProjection(
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
        long PreviewSchedulerScheduleLateDelta);

    private static DiagnosticSessionPreviewSchedulerResultProjection BuildPreviewSchedulerResultProjection(
        DiagnosticSessionResultAnalysis analysis)
    {
        var previewScheduler = analysis.PreviewScheduler;

        return new DiagnosticSessionPreviewSchedulerResultProjection(
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
            PreviewSchedulerScheduleLateDelta: previewScheduler.ScheduleLateDelta);
    }
}
