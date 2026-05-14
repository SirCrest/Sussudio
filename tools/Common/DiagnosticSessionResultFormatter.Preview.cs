using System.Text;
using static Sussudio.Tools.DiagnosticSessionText;

namespace Sussudio.Tools;

public static partial class DiagnosticSessionResultFormatter
{
    private static void AppendPreviewSections(StringBuilder builder, DiagnosticSessionResult result)
    {
        AppendPreviewScheduler(builder, result);
        AppendPreviewD3DPerformance(builder, result);
        AppendPreviewD3DCpuTiming(builder, result);
        AppendPreviewVisualCadence(builder, result);
    }

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

    private static void AppendPreviewD3DPerformance(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Preview D3D Perf: " +
            $"onePercentLowFpsEnd={result.PreviewCadenceOnePercentLowFpsAtEnd:0.##} " +
            $"onePercentLowFpsMin={result.PreviewCadenceMinOnePercentLowFpsObserved:0.##} " +
            $"missedRefreshDelta={result.PreviewD3DFrameStatsMissedRefreshDelta} " +
            $"statsFailureDelta={result.PreviewD3DFrameStatsFailureDelta} " +
            $"maxRecentSlowFrames={result.PreviewD3DMaxRecentSlowFramesObserved} " +
            $"latestSlowReason={FormatOptional(result.PreviewD3DLatestSlowFrameReason)} " +
            $"overBudgetMs={result.PreviewD3DLatestSlowFrameOverBudgetMs:0.##} " +
            $"presentIntervalMs={result.PreviewD3DLatestSlowFramePresentIntervalMs:0.##} " +
            $"totalFrameCpuMs={result.PreviewD3DLatestSlowFrameTotalFrameCpuMs:0.##} " +
            $"presentCallMs={result.PreviewD3DLatestSlowFramePresentCallMs:0.##} " +
            $"pending={result.PreviewD3DLatestSlowFramePendingFrameCount}");
    }

    private static void AppendPreviewD3DCpuTiming(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Preview D3D CPU Timing: " +
            $"inputUploadP99End={result.PreviewD3DInputUploadCpuP99MsAtEnd:0.##} " +
            $"inputUploadMaxObserved={result.PreviewD3DInputUploadCpuMaxMsObserved:0.##} " +
            $"renderSubmitP99End={result.PreviewD3DRenderSubmitCpuP99MsAtEnd:0.##} " +
            $"renderSubmitMaxObserved={result.PreviewD3DRenderSubmitCpuMaxMsObserved:0.##} " +
            $"presentCallP99End={result.PreviewD3DPresentCallP99MsAtEnd:0.##} " +
            $"presentCallMaxObserved={result.PreviewD3DPresentCallMaxMsObserved:0.##} " +
            $"totalFrameP99End={result.PreviewD3DTotalFrameCpuP99MsAtEnd:0.##} " +
            $"totalFrameMaxObserved={result.PreviewD3DTotalFrameCpuMaxMsObserved:0.##}");
    }

    private static void AppendPreviewVisualCadence(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Preview Visual Cadence: " +
            $"outputFpsEnd={result.VisualCadenceOutputFpsAtEnd:0.##} " +
            $"changeFpsEnd={result.VisualCadenceChangeFpsAtEnd:0.##} " +
            $"changeFpsMin={result.VisualCadenceMinChangeFpsObserved:0.##} " +
            $"repeatPctEnd={result.VisualCadenceRepeatPercentAtEnd:0.###} " +
            $"repeatPctMax={result.VisualCadenceMaxRepeatPercentObserved:0.###} " +
            $"repeatFramesEnd={result.VisualCadenceRepeatFramesAtEnd} " +
            $"longestRepeatRunEnd={result.VisualCadenceLongestRepeatRunAtEnd}");
    }
}
