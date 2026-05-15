using System.Text;
using static Sussudio.Tools.DiagnosticSessionText;

namespace Sussudio.Tools;

public static partial class DiagnosticSessionResultFormatter
{
    private static void AppendPreviewScheduler(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Preview Scheduler: " +
            $"droppedEnd={result.PreviewSchedulerDroppedAtEnd} " +
            $"droppedDelta={result.PreviewSchedulerDroppedDelta} " +
            $"clearedDropsEnd={result.PreviewSchedulerClearedDropsAtEnd} " +
            $"clearedDropsDelta={result.PreviewSchedulerClearedDropsDelta} " +
            $"deadlineDropsEnd={result.PreviewSchedulerDeadlineDropsAtEnd} " +
            $"deadlineDropsDelta={result.PreviewSchedulerDeadlineDropsDelta} " +
            $"underflowsEnd={result.PreviewSchedulerUnderflowsAtEnd} " +
            $"underflowsDelta={result.PreviewSchedulerUnderflowsDelta} " +
            $"resumeReprimesEnd={result.PreviewSchedulerResumeReprimesAtEnd} " +
            $"resumeReprimesDelta={result.PreviewSchedulerResumeReprimesDelta} " +
            $"lastUnderflowReasonEnd={FormatOptional(result.PreviewSchedulerLastUnderflowReasonAtEnd)} " +
            $"lastUnderflowInputAgeMsEnd={result.PreviewSchedulerLastUnderflowInputAgeMsAtEnd:0.##} " +
            $"lastUnderflowOutputAgeMsEnd={result.PreviewSchedulerLastUnderflowOutputAgeMsAtEnd:0.##} " +
            $"scheduleLateMaxMsObserved={result.PreviewSchedulerMaxScheduleLateMsObserved:0.##} " +
            $"scheduleLateDelta={result.PreviewSchedulerScheduleLateDelta} " +
            $"lastDropReasonEnd={FormatOptional(result.PreviewSchedulerLastDropReasonAtEnd)}");
    }
}
