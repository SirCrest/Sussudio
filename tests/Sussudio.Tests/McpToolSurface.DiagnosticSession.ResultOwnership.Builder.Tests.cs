using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticSessionResultBuilder_OwnsSummaryConstruction()
    {
        AssertDiagnosticSessionResultBuilderCoreOwnership();
        AssertDiagnosticSessionResultBuilderPreviewSchedulerOwnership();
        AssertDiagnosticSessionResultBuilderOverviewAndCaptureProjectionOwnership();
        AssertDiagnosticSessionResultBuilderFlashbackProjectionOwnership();
        AssertDiagnosticSessionResultBuilderPreviewProjectionOwnership();
        AssertDiagnosticSessionResultBuilderAnalysisWarningsOwnership();
        AssertDiagnosticSessionResultBuilderSummaryArtifactHandoffOwnership();

        return Task.CompletedTask;
    }

    private static void AssertDiagnosticSessionResultBuilderCoreOwnership()
    {
        var builderText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var flatteningText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Flattening.cs")
            .Replace("\r\n", "\n");
        var projectionSetText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Projections.cs")
            .Replace("\r\n", "\n");
        var resultBuildRequestText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var analysisText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Analysis.cs")
            .Replace("\r\n", "\n");
        var diagnosticHealthText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.DiagnosticHealth.cs")
            .Replace("\r\n", "\n");
        var previewSchedulerText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.PreviewScheduler.cs")
            .Replace("\r\n", "\n");

        AssertContains(builderText, "internal static partial class DiagnosticSessionResultBuilder");
        AssertContains(builderText, "internal static async Task<DiagnosticSessionResult> BuildAndWriteAsync(");
        AssertContains(builderText, "private static DiagnosticSessionResult CreateResult(");
        AssertContains(flatteningText, "private static DiagnosticSessionResult FlattenResultProjectionSet(");
        AssertContains(projectionSetText, "private static DiagnosticSessionResultProjectionSet BuildResultProjectionSet(");
        AssertContains(projectionSetText, "private readonly record struct DiagnosticSessionResultProjectionSet(");
        AssertContains(previewSchedulerText, "private static DiagnosticSessionPreviewSchedulerAnalysis BuildPreviewSchedulerAnalysis(");
        AssertContains(previewSchedulerText, "private readonly record struct DiagnosticSessionPreviewSchedulerAnalysis(");
        AssertContains(resultBuildRequestText, "internal sealed record DiagnosticSessionResultBuildRequest(");
        AssertContains(analysisText, "private sealed record DiagnosticSessionResultAnalysis(");
        AssertContains(projectionSetText, "DiagnosticSessionOverviewResultProjection Overview,");
        AssertContains(projectionSetText, "DiagnosticSessionPreviewVisualCadenceResultProjection PreviewVisualCadence");
        AssertContains(builderText, "runState.SetStage(\"result-analysis\")");
        AssertContains(builderText, "return FlattenResultProjectionSet(");
        AssertContains(flatteningText, "return new DiagnosticSessionResult\n        {");
        AssertContains(builderText, "var resultProjections = BuildResultProjectionSet(request, runState, analysis);");
        AssertContains(analysisText, "var healthSummary = BuildDiagnosticHealthSummary(request, lastSnapshot);");
        AssertContains(analysisText, "healthSummary.Snapshot,");
        AssertContains(analysisText, "healthSummary,");
        AssertContains(analysisText, "var previewScheduler = BuildPreviewSchedulerAnalysis(initialSnapshot, lastSnapshot, samples);");
        AssertContains(analysisText, "var validationOutcome = ValidateAnalysis(");
        AssertContains(diagnosticHealthText, "private readonly record struct DiagnosticSessionHealthSummary(");
        AssertContains(diagnosticHealthText, "private static DiagnosticSessionHealthSummary BuildDiagnosticHealthSummary(");
        AssertContains(diagnosticHealthText, "private readonly record struct DiagnosticHealthSourceWarningCounters(");
        AssertContains(diagnosticHealthText, "private static DiagnosticHealthSourceWarningCounters BuildDiagnosticHealthSourceWarningCounters(");
        AssertContains(diagnosticHealthText, "var tolerance = BuildDiagnosticHealthToleranceVerdict(");
        AssertContains(diagnosticHealthText, "tolerance.IsTolerated");
        AssertContains(diagnosticHealthText, "tolerance.WarningReason");
        AssertContains(diagnosticHealthText, "private readonly record struct DiagnosticSessionHealthToleranceVerdict(");
        AssertContains(diagnosticHealthText, "private static DiagnosticSessionHealthToleranceVerdict BuildDiagnosticHealthToleranceVerdict(");
        AssertContains(diagnosticHealthText, "var sourceWarningCounters = BuildDiagnosticHealthSourceWarningCounters(initialSnapshot, lastSnapshot);");
        AssertContains(diagnosticHealthText, "IsSparseSourceCaptureCadenceWarningRun(");
        AssertContains(diagnosticHealthText, "IsSparsePreviewSchedulerDeadlineDropRun(");
        AssertContains(analysisText, "private readonly record struct DiagnosticSessionAnalysisValidationOutcome(");
        AssertContains(analysisText, "private static DiagnosticSessionAnalysisValidationOutcome ValidateAnalysis(");
        AssertContains(analysisText, "ValidateFlashbackPlaybackSession(");
        AssertContains(analysisText, "ValidateCleanupLifecycleRestored(");
        AssertContains(analysisText, "ValidateFlashbackPreviewSchedulerAnalysis(");
        AssertContains(analysisText, "AnalyzeDiagnosticHealth(");
        AssertContains(analysisText, "EvaluateFlashbackWarningsSucceeded(request.ScenarioPlan, warnings)");
        AssertContains(analysisText, "private static bool EvaluateFlashbackWarningsSucceeded(");
        AssertContains(analysisText, "IsToleratedFlashbackScenarioWarning(");
        AssertDoesNotContain(analysisText, "MfSourceReaderFramesDropped");
        AssertDoesNotContain(analysisText, "VideoIngestErrorCount");
        AssertDoesNotContain(flatteningText, "private static DiagnosticSessionResultProjectionSet BuildResultProjectionSet(");
        AssertDoesNotContain(builderText, "private static DiagnosticSessionResultProjectionSet BuildResultProjectionSet(");
        AssertDoesNotContain(builderText, "private readonly record struct DiagnosticSessionResultProjectionSet(");
        AssertDoesNotContain(builderText, "return new DiagnosticSessionResult\n        {");
        AssertContains(analysisText, "IsToleratedFlashbackScenarioWarning(");
    }

    private static void AssertDiagnosticSessionResultBuilderPreviewSchedulerOwnership()
    {
        var analysisText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Analysis.cs")
            .Replace("\r\n", "\n");
        var previewSchedulerText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.PreviewScheduler.cs")
            .Replace("\r\n", "\n");
        var previewResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Projections.cs")
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
        AssertContains(previewSchedulerText, "private static void ValidateFlashbackPreviewSchedulerAnalysis(");
        AssertContains(previewSchedulerText, "var previewTargetFps = GetDouble(lastSnapshot, \"ExpectedCaptureFrameRate\");");
        AssertContains(previewSchedulerText, "previewTargetFps = GetDouble(lastSnapshot, \"SelectedExactFrameRate\");");
        AssertContains(previewSchedulerText, "var toleratesPreviewCycleSchedulerSettling =");
        AssertContains(previewSchedulerText, "scenarioPlan.IsPreviewCycleScenario && visualCadenceHealthy");
        AssertContains(previewSchedulerText, "var toleratesSparsePreviewSchedulerDeadlineDrops =");
        AssertContains(previewSchedulerText, "IsSparsePreviewSchedulerDeadlineDropRun(");
        AssertContains(previewSchedulerText, "var toleratesSparseScrubSchedulerTransitions =");
        AssertContains(previewSchedulerText, "scenarioPlan.ToleratesSparsePreviewSchedulerStressTransitions &&");
        AssertContains(previewSchedulerText, "IsSparsePreviewSchedulerStressRun(");
        AssertContains(previewSchedulerText, "ValidateFlashbackPreviewScheduler(");
        AssertContains(analysisText, "DiagnosticSessionPreviewSchedulerAnalysis PreviewScheduler,");
        AssertContains(previewResultText, "private readonly record struct DiagnosticSessionPreviewSchedulerResultProjection(");
        AssertContains(previewResultText, "private static DiagnosticSessionPreviewSchedulerResultProjection BuildPreviewSchedulerResultProjection(");
        AssertContains(previewResultText, "var previewScheduler = analysis.PreviewScheduler;");
        AssertContains(previewResultText, "PreviewSchedulerDroppedAtEnd: previewScheduler.DroppedAtEnd");
        AssertContains(previewResultText, "PreviewSchedulerScheduleLateDelta: previewScheduler.ScheduleLateDelta");
        AssertContains(previewResultText, "PreviewSchedulerLastDropReasonAtEnd: previewScheduler.LastDropReasonAtEnd");
        AssertContains(previewResultText, "PreviewSchedulerLastUnderflowReasonAtEnd: previewScheduler.LastUnderflowReasonAtEnd");
        AssertContains(previewResultText, "PreviewSchedulerLastUnderflowInputAgeMsAtEnd: previewScheduler.LastUnderflowInputAgeMsAtEnd");
        AssertContains(previewResultText, "PreviewSchedulerLastUnderflowOutputAgeMsAtEnd: previewScheduler.LastUnderflowOutputAgeMsAtEnd");
        AssertDoesNotContain(analysisText, "long PreviewSchedulerDroppedAtEnd");
        AssertDoesNotContain(analysisText, "double PreviewSchedulerMaxScheduleLateMsObserved");
        AssertDoesNotContain(previewSchedulerText, "private readonly record struct DiagnosticSessionPreviewSchedulerResultProjection(");
        AssertDoesNotContain(previewSchedulerText, "private static DiagnosticSessionPreviewSchedulerResultProjection BuildPreviewSchedulerResultProjection(");
        AssertDoesNotContain(previewSchedulerText, "analysis.PreviewSchedulerDroppedAtEnd");
        AssertDoesNotContain(previewSchedulerText, "analysis.PreviewSchedulerMaxScheduleLateMsObserved");
        AssertDoesNotContain(analysisText, "var previewSchedulerDroppedAtEnd =");
        AssertDoesNotContain(analysisText, "var previewSchedulerMaxScheduleLateMsObserved = samples");
    }

    private static void AssertDiagnosticSessionResultBuilderOverviewAndCaptureProjectionOwnership()
    {
        var builderText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var flatteningText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Flattening.cs")
            .Replace("\r\n", "\n");
        var projectionSetText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Projections.cs")
            .Replace("\r\n", "\n");
        var analysisText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Analysis.cs")
            .Replace("\r\n", "\n");
        var overviewResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Projections.cs")
            .Replace("\r\n", "\n");
        var captureResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Projections.cs")
            .Replace("\r\n", "\n");

        AssertContains(builderText, "var resultProjections = BuildResultProjectionSet(request, runState, analysis);");
        AssertContains(builderText, "return FlattenResultProjectionSet(");
        AssertDoesNotContain(builderText, "return new DiagnosticSessionResult\n        {");
        AssertContains(flatteningText, "private static DiagnosticSessionResult FlattenResultProjectionSet(");
        AssertContains(projectionSetText, "Overview: BuildOverviewResultProjection(request, runState, analysis)");
        AssertContains(flatteningText, "var overviewResult = resultProjections.Overview;");
        AssertContains(flatteningText, "Success = overviewResult.Success,");
        AssertContains(overviewResultText, "private readonly record struct DiagnosticSessionOverviewResultProjection(");
        AssertContains(overviewResultText, "private static DiagnosticSessionOverviewResultProjection BuildOverviewResultProjection(");
        AssertContains(overviewResultText, "var verificationSucceeded = request.Verification.HasValue");
        AssertContains(overviewResultText, "Success: DetermineDiagnosticSessionSuccess(request, runState, analysis, verificationSucceeded)");
        AssertContains(overviewResultText, "ProcessCpuPercentAtEnd: GetDouble(lastSnapshot, \"ProcessCpuPercent\")");
        AssertContains(overviewResultText, "var processCpuMaxPercentObserved = GetProcessCpuMaxPercentObserved(request.Samples, lastSnapshot);");
        AssertContains(overviewResultText, "ProcessCpuMaxPercentObserved: processCpuMaxPercentObserved");
        AssertContains(overviewResultText, "private static double GetProcessCpuMaxPercentObserved(");
        AssertContains(overviewResultText, ".Select(sample => GetDouble(sample.Snapshot, \"ProcessCpuPercent\"))");
        AssertContains(overviewResultText, ".Append(GetDouble(lastSnapshot, \"ProcessCpuPercent\"))");
        AssertContains(overviewResultText, "RecordingVerificationMessage: request.Verification.HasValue");
        AssertContains(overviewResultText, "PresentMon: request.PresentMon");
        AssertContains(overviewResultText, "private static bool DetermineDiagnosticSessionSuccess(");
        AssertContains(overviewResultText, "request.CommandFailureCount == 0 &&");
        AssertContains(overviewResultText, "runState.TerminalException is null &&");
        AssertContains(overviewResultText, "analysis.DiagnosticHealthSucceeded &&");
        AssertContains(overviewResultText, "(request.PresentMon is null || request.PresentMon.Success) &&");
        AssertContains(overviewResultText, "(!verificationSucceeded.HasValue || verificationSucceeded.Value) &&");
        AssertContains(overviewResultText, "analysis.FlashbackWarningsSucceeded");
        AssertDoesNotContain(flatteningText, "request.CommandFailureCount == 0 &&");
        AssertDoesNotContain(flatteningText, "ProcessCpuPercentAtEnd = GetDouble(lastSnapshot");
        AssertDoesNotContain(flatteningText, "RecordingVerificationMessage = request.Verification.HasValue");
        AssertDoesNotContain(analysisText, "ProcessCpuMaxPercentObserved");
        AssertDoesNotContain(overviewResultText, "analysis.ProcessCpuMaxPercentObserved");
        AssertContains(projectionSetText, "Capture: BuildCaptureResultProjection(analysis)");
        AssertContains(flatteningText, "var captureResult = resultProjections.Capture;");
        AssertContains(captureResultText, "private readonly record struct DiagnosticSessionCaptureResultProjection(");
        AssertContains(captureResultText, "private static DiagnosticSessionCaptureResultProjection BuildCaptureResultProjection(");
        AssertContains(captureResultText, "SelectedResolutionAtEnd: GetString(lastSnapshot, \"SelectedResolution\") ?? string.Empty");
        AssertContains(captureResultText, "SourceWidthAtEnd: (int)(GetNullableLong(lastSnapshot, \"SourceWidth\") ?? 0)");
        AssertContains(captureResultText, "SourceTelemetrySummaryAtEnd: GetString(lastSnapshot, \"SourceTelemetrySummaryText\") ?? string.Empty");
        AssertDoesNotContain(flatteningText, "SelectedResolutionAtEnd = GetString(lastSnapshot");
        AssertDoesNotContain(flatteningText, "SourceWidthAtEnd = (int)(GetNullableLong");
        AssertDoesNotContain(flatteningText, "SourceTelemetrySummaryAtEnd = GetString(lastSnapshot");
    }
}
