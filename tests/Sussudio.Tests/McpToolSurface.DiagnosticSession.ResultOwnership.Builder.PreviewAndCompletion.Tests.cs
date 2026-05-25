using System.Threading.Tasks;

static partial class Program
{
    private static void AssertDiagnosticSessionResultBuilderPreviewProjectionOwnership()
    {
        var builderText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var flatteningText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Flattening.cs")
            .Replace("\r\n", "\n");
        var projectionSetText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Projections.cs")
            .Replace("\r\n", "\n");
        var previewResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Projections.cs")
            .Replace("\r\n", "\n");
        var previewD3DResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Projections.cs")
            .Replace("\r\n", "\n");
        var analysisText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Analysis.cs")
            .Replace("\r\n", "\n");

        AssertContains(builderText, "return FlattenResultProjectionSet(");
        AssertContains(flatteningText, "private static DiagnosticSessionResult FlattenResultProjectionSet(");
        AssertContains(projectionSetText, "Preview: BuildPreviewResultProjection(analysis)");
        AssertContains(projectionSetText, "PreviewScheduler: BuildPreviewSchedulerResultProjection(analysis)");
        AssertContains(projectionSetText, "PreviewD3D: BuildPreviewD3DResultProjection(analysis)");
        AssertContains(projectionSetText, "PreviewVisualCadence: BuildPreviewVisualCadenceResultProjection(analysis)");
        AssertContains(flatteningText, "var previewResult = resultProjections.Preview;");
        AssertContains(flatteningText, "var previewSchedulerResult = resultProjections.PreviewScheduler;");
        AssertContains(flatteningText, "var previewD3DResult = resultProjections.PreviewD3D;");
        AssertContains(flatteningText, "var previewVisualCadenceResult = resultProjections.PreviewVisualCadence;");
        AssertContains(previewResultText, "private readonly record struct DiagnosticSessionPreviewResultProjection(");
        AssertContains(previewResultText, "private static DiagnosticSessionPreviewResultProjection BuildPreviewResultProjection(");
        AssertContains(previewResultText, "private readonly record struct DiagnosticSessionPreviewSchedulerResultProjection(");
        AssertContains(previewResultText, "private static DiagnosticSessionPreviewSchedulerResultProjection BuildPreviewSchedulerResultProjection(");
        AssertContains(previewD3DResultText, "private readonly record struct DiagnosticSessionPreviewD3DResultProjection(");
        AssertContains(previewD3DResultText, "private static DiagnosticSessionPreviewD3DResultProjection BuildPreviewD3DResultProjection(");
        AssertContains(previewD3DResultText, "var previewD3DMetrics = analysis.PreviewD3DMetrics;");
        AssertContains(previewResultText, "private readonly record struct DiagnosticSessionPreviewVisualCadenceResultProjection(");
        AssertContains(previewResultText, "private static DiagnosticSessionPreviewVisualCadenceResultProjection BuildPreviewVisualCadenceResultProjection(");
        AssertContains(previewResultText, "var visualCadenceMetrics = analysis.VisualCadenceMetrics;");
        AssertContains(previewResultText, "PreviewSchedulerLastDropReasonAtEnd: previewScheduler.LastDropReasonAtEnd");
        AssertContains(previewD3DResultText, "PreviewD3DInputUploadCpuP99MsAtEnd: previewD3DMetrics.InputUploadCpuP99MsAtEnd");
        AssertContains(previewD3DResultText, "PreviewD3DTotalFrameCpuMaxMsObserved: previewD3DMetrics.TotalFrameCpuMaxMsObserved");
        AssertContains(previewResultText, "VisualCadenceOutputFpsAtEnd: visualCadenceMetrics.OutputFpsAtEnd");
        AssertContains(previewResultText, "VisualCadenceLongestRepeatRunAtEnd: visualCadenceMetrics.LongestRepeatRunAtEnd");
        AssertContains(flatteningText, "PreviewD3DInputUploadCpuP99MsAtEnd = previewD3DResult.PreviewD3DInputUploadCpuP99MsAtEnd,");
        AssertContains(flatteningText, "PreviewSchedulerDroppedAtEnd = previewSchedulerResult.PreviewSchedulerDroppedAtEnd,");
        AssertContains(flatteningText, "VisualCadenceOutputFpsAtEnd = previewVisualCadenceResult.VisualCadenceOutputFpsAtEnd,");
        AssertDoesNotContain(flatteningText, "GetString(lastSnapshot, \"MjpegPreviewJitterLastDropReason\")");
        AssertDoesNotContain(analysisText, "private static DiagnosticSessionPreviewSchedulerResultProjection BuildPreviewSchedulerResultProjection(");
        AssertDoesNotContain(analysisText, "PreviewD3DInputUploadCpuP99MsAtEnd");
        AssertDoesNotContain(flatteningText, "PreviewD3DInputUploadCpuP99MsAtEnd = previewResult");
        AssertDoesNotContain(flatteningText, "PreviewD3DInputUploadCpuP99MsAtEnd = previewD3DMetrics");
        AssertDoesNotContain(flatteningText, "VisualCadenceOutputFpsAtEnd = previewResult");
        AssertDoesNotContain(flatteningText, "VisualCadenceOutputFpsAtEnd = visualCadenceMetrics");
    }

    private static void AssertDiagnosticSessionResultBuilderAnalysisWarningsOwnership()
    {
        var analysisText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Analysis.cs")
            .Replace("\r\n", "\n");

        AssertContains(analysisText, "AddFlashbackPlaybackAnalysisWarnings(playbackResultMetrics, warnings);");
        AssertContains(analysisText, "AddFlashbackExportAnalysisWarnings(");
        AssertContains(analysisText, "ValidateFlashbackPreviewSchedulerAnalysis(");
        AssertContains(analysisText, "exportMetrics.ForceRotateFallbacksAtEnd,");
        AssertContains(analysisText, "exportMetrics.ForceRotateFallbacksDelta,");
        AssertContains(analysisText, "exportMetrics.LastForceRotateFallbackSegmentsAtEnd,");
        AssertContains(analysisText, "var toleratesPreviewCycleSchedulerSettling =");
        AssertContains(analysisText, "var toleratesSparsePreviewSchedulerDeadlineDrops =");
        AssertContains(analysisText, "var toleratesSparseScrubSchedulerTransitions =");
        AssertDoesNotContain(analysisText, "var flashbackExportForceRotateFallbacksAtEnd =");
        AssertDoesNotContain(analysisText, "FlashbackExportForceRotateFallbacksAtEnd =");
        AssertContains(analysisText, "private static void AddFlashbackPlaybackAnalysisWarnings(");
        AssertContains(analysisText, "private static void AddFlashbackExportAnalysisWarnings(");
        AssertContains(analysisText, "EvaluateFlashbackWarningsSucceeded(request.ScenarioPlan, warnings)");
        AssertContains(analysisText, "private static bool EvaluateFlashbackWarningsSucceeded(");
        AssertContains(analysisText, "scenarioPlan.UsesFlashbackScenarioWarningPolicy");
        AssertContains(analysisText, "IsToleratedFlashbackScenarioWarning(");
        AssertContains(analysisText, "scenarioPlan.ToleratesSourceSignalHealthWarning");
        AssertContains(analysisText, "scenarioPlan.ToleratesFlashbackForceRotateDrainWarning");
        AssertContains(analysisText, "flashback playback seek forward-decode cap hit during session");
        AssertContains(analysisText, "flashback export used force-rotate partial fallback");
        AssertContains(analysisText, "playbackResultMetrics.SeekForwardDecodeCapHitsDelta <= 0");
        AssertContains(analysisText, "flashbackExportForceRotateFallbacksDelta <= 0");
    }

    internal static Task DiagnosticSessionResultBuilder_DiagnosticHealthVerdictLivesInFocusedPartial()
    {
        var analysisText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Analysis.cs")
            .Replace("\r\n", "\n");
        var healthText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.DiagnosticHealth.cs")
            .Replace("\r\n", "\n");

        AssertContains(analysisText, "var validationOutcome = ValidateAnalysis(");
        AssertContains(analysisText, "var diagnosticHealthSucceeded = AnalyzeDiagnosticHealth(");
        AssertContains(healthText, "private readonly record struct DiagnosticSessionHealthSummary(");
        AssertContains(healthText, "private static DiagnosticSessionHealthSummary BuildDiagnosticHealthSummary(");
        AssertContains(healthText, "request.StoppedRecordingForVerification");
        AssertContains(healthText, "GetString(diagnosticHealthSnapshot, \"DiagnosticHealthStatus\") ?? \"Unknown\"");
        AssertContains(healthText, "private static bool AnalyzeDiagnosticHealth(");
        AssertContains(healthText, "private readonly record struct DiagnosticHealthSourceWarningCounters(");
        AssertContains(healthText, "private static DiagnosticHealthSourceWarningCounters BuildDiagnosticHealthSourceWarningCounters(");
        AssertContains(healthText, "SourceReaderFramesDroppedDelta: GetCounterDelta(lastSnapshot, initialSnapshot, \"MfSourceReaderFramesDropped\")");
        AssertContains(healthText, "VideoIngestErrorsDelta: GetCounterDelta(lastSnapshot, initialSnapshot, \"VideoIngestErrorCount\")");
        AssertContains(healthText, "BuildDiagnosticHealthToleranceVerdict(");
        AssertContains(healthText, "private readonly record struct DiagnosticSessionHealthToleranceVerdict(");
        AssertContains(healthText, "private static DiagnosticSessionHealthToleranceVerdict BuildDiagnosticHealthToleranceVerdict(");
        AssertContains(healthText, "sourceWarningCounters.SourceReaderFramesDroppedDelta");
        AssertContains(healthText, "sourceWarningCounters.VideoIngestErrorsDelta");
        AssertContains(healthText, "BuildSessionDiagnosticHealthObservation(");
        AssertContains(healthText, "IsSparseSourceCaptureCadenceWarningRun(");
        AssertContains(healthText, "IsSparsePreviewSchedulerDeadlineDropRun(");
        AssertContains(healthText, "IsPreviewSchedulerDiagnosticHealthObservation(diagnosticHealthObservation)");
        AssertContains(healthText, "diagnostic health degraded during session");
        AssertContains(healthText, "diagnostic health {tolerance.WarningReason}:");
        AssertContains(healthText, "flashback force-rotate drain warning tolerated for flashback scenario");
        AssertDoesNotContain(analysisText, "private static bool AnalyzeDiagnosticHealth(");
        AssertDoesNotContain(analysisText, "DiagnosticHealthStatus");
        AssertDoesNotContain(analysisText, "DiagnosticLikelyStage");
        AssertDoesNotContain(analysisText, "MfSourceReaderFramesDropped");
        AssertDoesNotContain(analysisText, "VideoIngestErrorCount");
        AssertDoesNotContain(analysisText, "BuildSessionDiagnosticHealthObservation(");
        AssertDoesNotContain(analysisText, "diagnostic health degraded during session");
        AssertDoesNotContain(analysisText, "diagnostic health {toleratedReason}:");

        return Task.CompletedTask;
    }

    private static void AssertDiagnosticSessionResultBuilderSummaryArtifactHandoffOwnership()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var runExecutionText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var completionText = runExecutionText;
        var builderText = ReadDiagnosticSessionResultBuilderSource();

        AssertContains(builderText, "var artifactPaths = await WritePreSummaryAsync(");
        AssertContains(builderText, "SummaryPath = artifactPaths.SummaryPath");
        AssertContains(builderText, "SamplesPath = artifactPaths.SamplesPath");
        AssertContains(builderText, "FrameLedgerPath = artifactPaths.FrameLedgerPath");
        AssertContains(builderText, "TimelinePath = artifactPaths.TimelinePath");
        AssertContains(builderText, "runState.SetStage(\"summary\")");
        AssertContains(builderText, "return await WriteSummaryAsync(result, runState, warnings).ConfigureAwait(false);");
        AssertContains(completionText, "DiagnosticSessionResultBuilder.BuildAndWriteAsync(");
        AssertContains(completionText, "CreateResultBuildRequest(");
        AssertContains(runExecutionText, "RunCompletionPhaseAsync(");
        AssertContains(runExecutionText, "DiagnosticSessionResultBuilder.BuildAndWriteAsync(");
        AssertContains(runExecutionText, "new DiagnosticSessionResultBuildRequest(");
        AssertContains(completionText, "private static DiagnosticSessionResultBuildRequest CreateResultBuildRequest(");
        AssertContains(completionText, "return new DiagnosticSessionResultBuildRequest(");
        AssertContains(completionText, "runBootstrap.ScenarioPlan");
        AssertContains(completionText, "postRunSnapshots.HealthSnapshot");
        AssertDoesNotContain(runnerText, "SetStage(\"result-analysis\")");
        AssertDoesNotContain(runnerText, "var result = new DiagnosticSessionResult");
        AssertDoesNotContain(runnerText, "WriteArtifactBestEffortAsync(\"write-samples\"");
        AssertDoesNotContain(runnerText, "RecordTerminalException(ex, \"summary-write\")");
    }
}
