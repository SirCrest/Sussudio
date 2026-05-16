static partial class Program
{
    private static void AssertDiagnosticSessionResultBuilderPreviewSchedulerOwnership()
    {
        var analysisText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Analysis.cs")
            .Replace("\r\n", "\n");
        var previewSchedulerText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.PreviewScheduler.cs")
            .Replace("\r\n", "\n");
        var previewSchedulerValidationText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.PreviewSchedulerValidation.cs")
            .Replace("\r\n", "\n");
        var modelsText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Models.cs")
            .Replace("\r\n", "\n");
        var previewResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.PreviewResult.cs")
            .Replace("\r\n", "\n");

        AssertContains(analysisText, "previewScheduler,");
        AssertContains(previewSchedulerText, "private readonly record struct DiagnosticSessionPreviewSchedulerAnalysis(");
        AssertContains(previewSchedulerText, "string LastDropReasonAtEnd,");
        AssertContains(previewSchedulerText, "string LastUnderflowReasonAtEnd,");
        AssertContains(previewSchedulerText, "double LastUnderflowInputAgeMsAtEnd,");
        AssertContains(previewSchedulerText, "double LastUnderflowOutputAgeMsAtEnd");
        AssertContains(previewSchedulerText, "LastDropReasonAtEnd: GetString(lastSnapshot, \"MjpegPreviewJitterLastDropReason\") ?? string.Empty");
        AssertContains(previewSchedulerText, "LastUnderflowReasonAtEnd: GetString(lastSnapshot, \"MjpegPreviewJitterLastUnderflowReason\") ?? string.Empty");
        AssertContains(previewSchedulerText, "LastUnderflowInputAgeMsAtEnd: GetDouble(lastSnapshot, \"MjpegPreviewJitterLastUnderflowInputAgeMs\")");
        AssertContains(previewSchedulerText, "LastUnderflowOutputAgeMsAtEnd: GetDouble(lastSnapshot, \"MjpegPreviewJitterLastUnderflowOutputAgeMs\")");
        AssertContains(previewSchedulerValidationText, "private static void ValidateFlashbackPreviewSchedulerAnalysis(");
        AssertContains(previewSchedulerValidationText, "var previewTargetFps = GetDouble(lastSnapshot, \"ExpectedCaptureFrameRate\");");
        AssertContains(previewSchedulerValidationText, "previewTargetFps = GetDouble(lastSnapshot, \"SelectedExactFrameRate\");");
        AssertContains(previewSchedulerValidationText, "var toleratesPreviewCycleSchedulerSettling =");
        AssertContains(previewSchedulerValidationText, "scenarioPlan.IsPreviewCycleScenario && visualCadenceHealthy");
        AssertContains(previewSchedulerValidationText, "var toleratesSparsePreviewSchedulerDeadlineDrops =");
        AssertContains(previewSchedulerValidationText, "IsSparsePreviewSchedulerDeadlineDropRun(");
        AssertContains(previewSchedulerValidationText, "var toleratesSparseScrubSchedulerTransitions =");
        AssertContains(previewSchedulerValidationText, "scenarioPlan.ToleratesSparsePreviewSchedulerStressTransitions &&");
        AssertContains(previewSchedulerValidationText, "IsSparsePreviewSchedulerStressRun(");
        AssertContains(previewSchedulerValidationText, "ValidateFlashbackPreviewScheduler(");
        AssertContains(modelsText, "DiagnosticSessionPreviewSchedulerAnalysis PreviewScheduler,");
        AssertContains(previewResultText, "var previewScheduler = analysis.PreviewScheduler;");
        AssertContains(previewResultText, "PreviewSchedulerDroppedAtEnd: previewScheduler.DroppedAtEnd");
        AssertContains(previewResultText, "PreviewSchedulerScheduleLateDelta: previewScheduler.ScheduleLateDelta");
        AssertContains(previewResultText, "PreviewSchedulerLastDropReasonAtEnd: previewScheduler.LastDropReasonAtEnd");
        AssertContains(previewResultText, "PreviewSchedulerLastUnderflowReasonAtEnd: previewScheduler.LastUnderflowReasonAtEnd");
        AssertContains(previewResultText, "PreviewSchedulerLastUnderflowInputAgeMsAtEnd: previewScheduler.LastUnderflowInputAgeMsAtEnd");
        AssertContains(previewResultText, "PreviewSchedulerLastUnderflowOutputAgeMsAtEnd: previewScheduler.LastUnderflowOutputAgeMsAtEnd");
        AssertDoesNotContain(modelsText, "long PreviewSchedulerDroppedAtEnd");
        AssertDoesNotContain(modelsText, "double PreviewSchedulerMaxScheduleLateMsObserved");
        AssertDoesNotContain(previewResultText, "analysis.PreviewSchedulerDroppedAtEnd");
        AssertDoesNotContain(previewResultText, "analysis.PreviewSchedulerMaxScheduleLateMsObserved");
        AssertDoesNotContain(previewResultText, "MjpegPreviewJitter");
        AssertDoesNotContain(previewResultText, "var lastSnapshot = analysis.LastSnapshot;");
        AssertDoesNotContain(analysisText, "var previewSchedulerDroppedAtEnd =");
        AssertDoesNotContain(analysisText, "var previewSchedulerMaxScheduleLateMsObserved = samples");
    }
}
