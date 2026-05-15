using System.Threading.Tasks;

static partial class Program
{
    private static Task DiagnosticSessionResultBuilder_OwnsSummaryConstruction()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var analysisText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Analysis.cs")
            .Replace("\r\n", "\n");
        var previewSchedulerText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.PreviewScheduler.cs")
            .Replace("\r\n", "\n");
        var previewSchedulerValidationText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.PreviewSchedulerValidation.cs")
            .Replace("\r\n", "\n");
        var modelsText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Models.cs")
            .Replace("\r\n", "\n");
        var resultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Result.cs")
            .Replace("\r\n", "\n");
        var overviewResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.OverviewResult.cs")
            .Replace("\r\n", "\n");
        var flashbackWarningsText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.FlashbackWarnings.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackResult.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackCommandsResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackCommandsResult.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackCadenceResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackCadenceResult.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackDecodeResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackDecodeResult.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackAudioMasterResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackAudioMasterResult.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackStagesResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackStagesResult.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.FlashbackRecordingResult.cs")
            .Replace("\r\n", "\n");
        var flashbackExportResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.FlashbackExportResult.cs")
            .Replace("\r\n", "\n");
        var captureResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.CaptureResult.cs")
            .Replace("\r\n", "\n");
        var previewD3DResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.PreviewD3DResult.cs")
            .Replace("\r\n", "\n");
        var previewVisualCadenceResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.PreviewVisualCadenceResult.cs")
            .Replace("\r\n", "\n");
        var previewResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.PreviewResult.cs")
            .Replace("\r\n", "\n");
        var builderText = ReadDiagnosticSessionResultBuilderSource();

        AssertContains(builderText, "internal static partial class DiagnosticSessionResultBuilder");
        AssertContains(builderText, "internal static async Task<DiagnosticSessionResult> BuildAndWriteAsync(");
        AssertContains(builderText, "private static DiagnosticSessionResult CreateResult(");
        AssertContains(builderText, "private static DiagnosticSessionPreviewSchedulerAnalysis BuildPreviewSchedulerAnalysis(");
        AssertContains(builderText, "private readonly record struct DiagnosticSessionPreviewSchedulerAnalysis(");
        AssertContains(builderText, "internal sealed record DiagnosticSessionResultBuildRequest(");
        AssertContains(builderText, "runState.SetStage(\"result-analysis\")");
        AssertContains(builderText, "var result = new DiagnosticSessionResult");
        AssertContains(builderText, "var previewScheduler = BuildPreviewSchedulerAnalysis(initialSnapshot, lastSnapshot, samples);");
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
        AssertContains(resultText, "var overviewResult = BuildOverviewResultProjection(request, runState, analysis);");
        AssertContains(resultText, "Success = overviewResult.Success,");
        AssertContains(overviewResultText, "private readonly record struct DiagnosticSessionOverviewResultProjection(");
        AssertContains(overviewResultText, "private static DiagnosticSessionOverviewResultProjection BuildOverviewResultProjection(");
        AssertContains(overviewResultText, "var verificationSucceeded = request.Verification.HasValue");
        AssertContains(overviewResultText, "Success: DetermineDiagnosticSessionSuccess(request, runState, analysis, verificationSucceeded)");
        AssertContains(overviewResultText, "ProcessCpuPercentAtEnd: GetDouble(lastSnapshot, \"ProcessCpuPercent\")");
        AssertContains(overviewResultText, "ProcessCpuMaxPercentObserved: analysis.ProcessCpuMaxPercentObserved");
        AssertContains(overviewResultText, "RecordingVerificationMessage: request.Verification.HasValue");
        AssertContains(overviewResultText, "PresentMon: request.PresentMon");
        AssertContains(overviewResultText, "private static bool DetermineDiagnosticSessionSuccess(");
        AssertContains(overviewResultText, "request.CommandFailureCount == 0 &&");
        AssertContains(overviewResultText, "runState.TerminalException is null &&");
        AssertContains(overviewResultText, "analysis.DiagnosticHealthSucceeded &&");
        AssertContains(overviewResultText, "(request.PresentMon is null || request.PresentMon.Success) &&");
        AssertContains(overviewResultText, "(!verificationSucceeded.HasValue || verificationSucceeded.Value) &&");
        AssertContains(overviewResultText, "analysis.FlashbackWarningsSucceeded");
        AssertDoesNotContain(resultText, "request.CommandFailureCount == 0 &&");
        AssertDoesNotContain(resultText, "ProcessCpuPercentAtEnd = GetDouble(lastSnapshot");
        AssertDoesNotContain(resultText, "RecordingVerificationMessage = request.Verification.HasValue");
        AssertContains(resultText, "var captureResult = BuildCaptureResultProjection(analysis);");
        AssertContains(captureResultText, "private readonly record struct DiagnosticSessionCaptureResultProjection(");
        AssertContains(captureResultText, "private static DiagnosticSessionCaptureResultProjection BuildCaptureResultProjection(");
        AssertContains(captureResultText, "SelectedResolutionAtEnd: GetString(lastSnapshot, \"SelectedResolution\") ?? string.Empty");
        AssertContains(captureResultText, "SourceWidthAtEnd: (int)(GetNullableLong(lastSnapshot, \"SourceWidth\") ?? 0)");
        AssertContains(captureResultText, "SourceTelemetrySummaryAtEnd: GetString(lastSnapshot, \"SourceTelemetrySummaryText\") ?? string.Empty");
        AssertDoesNotContain(resultText, "SelectedResolutionAtEnd = GetString(lastSnapshot");
        AssertDoesNotContain(resultText, "SourceWidthAtEnd = (int)(GetNullableLong");
        AssertDoesNotContain(resultText, "SourceTelemetrySummaryAtEnd = GetString(lastSnapshot");
        AssertContains(resultText, "var flashbackPlaybackResult = BuildFlashbackPlaybackResultProjection(analysis);");
        AssertContains(resultText, "var flashbackPlaybackCommandsResult = flashbackPlaybackResult.CommandsResult;");
        AssertContains(resultText, "var flashbackPlaybackCadenceResult = flashbackPlaybackResult.CadenceResult;");
        AssertContains(resultText, "var flashbackPlaybackDecodeResult = flashbackPlaybackResult.DecodeResult;");
        AssertContains(resultText, "var flashbackPlaybackAudioMasterResult = flashbackPlaybackResult.AudioMasterResult;");
        AssertContains(resultText, "var flashbackPlaybackStagesResult = flashbackPlaybackResult.StagesResult;");
        AssertContains(flashbackPlaybackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackResultProjection(");
        AssertContains(flashbackPlaybackResultText, "private static DiagnosticSessionFlashbackPlaybackResultProjection BuildFlashbackPlaybackResultProjection(");
        AssertContains(flashbackPlaybackResultText, "CommandsResult: commandsResult");
        AssertContains(flashbackPlaybackResultText, "CadenceResult: cadenceResult");
        AssertContains(flashbackPlaybackResultText, "DecodeResult: decodeResult");
        AssertContains(flashbackPlaybackResultText, "AudioMasterResult: audioMasterResult");
        AssertContains(flashbackPlaybackResultText, "StagesResult: stagesResult");
        AssertContains(flashbackPlaybackCommandsResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackCommandsResultProjection(");
        AssertContains(flashbackPlaybackCommandsResultText, "private static DiagnosticSessionFlashbackPlaybackCommandsResultProjection BuildFlashbackPlaybackCommandsResultProjection(");
        AssertContains(flashbackPlaybackCommandsResultText, "FlashbackPlaybackPendingCommandsAtEnd: playbackResultMetrics.PendingCommandsAtEnd");
        AssertContains(flashbackPlaybackCommandsResultText, "FlashbackPlaybackMaxCommandQueueLatencyCommandObserved: playbackResultMetrics.MaxCommandQueueLatencyCommandObserved");
        AssertContains(flashbackPlaybackCommandsResultText, "FlashbackPlaybackLastCommandFailureAtEnd: playbackResultMetrics.LastCommandFailureAtEnd");
        AssertContains(flashbackPlaybackCommandsResultText, "FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd: playbackResultMetrics.LastCommandFailureUtcUnixMsAtEnd");
        AssertContains(flashbackPlaybackCadenceResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackCadenceResultProjection(");
        AssertContains(flashbackPlaybackCadenceResultText, "private static DiagnosticSessionFlashbackPlaybackCadenceResultProjection BuildFlashbackPlaybackCadenceResultProjection(");
        AssertContains(flashbackPlaybackCadenceResultText, "FlashbackPlaybackMinOnePercentLowFpsObserved: playbackSessionMetrics.MinOnePercentLowFpsObserved");
        AssertContains(flashbackPlaybackCadenceResultText, "FlashbackPlaybackMinOnePercentLowDecodeP99Ms: playbackSessionMetrics.MinOnePercentLowDecodeP99Ms");
        AssertContains(flashbackPlaybackCadenceResultText, "FlashbackPlaybackDroppedFramesDelta: playbackSessionMetrics.DroppedFramesDelta");
        AssertContains(flashbackPlaybackDecodeResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackDecodeResultProjection(");
        AssertContains(flashbackPlaybackDecodeResultText, "private static DiagnosticSessionFlashbackPlaybackDecodeResultProjection BuildFlashbackPlaybackDecodeResultProjection(");
        AssertContains(flashbackPlaybackDecodeResultText, "FlashbackPlaybackMaxDecodePhaseAtEnd: playbackResultMetrics.MaxDecodePhaseAtEnd");
        AssertContains(flashbackPlaybackDecodeResultText, "FlashbackPlaybackMaxDecodeP99MsObserved: playbackSessionMetrics.MaxDecodeP99MsObserved");
        AssertContains(flashbackPlaybackDecodeResultText, "FlashbackPlaybackMaxDecodePositionMsObserved: playbackSessionMetrics.MaxDecodePositionMsObserved");
        AssertContains(flashbackPlaybackAudioMasterResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackAudioMasterResultProjection(");
        AssertContains(flashbackPlaybackAudioMasterResultText, "private static DiagnosticSessionFlashbackPlaybackAudioMasterResultProjection BuildFlashbackPlaybackAudioMasterResultProjection(");
        AssertContains(flashbackPlaybackAudioMasterResultText, "FlashbackPlaybackAudioMasterFallbacksAtEnd: playbackResultMetrics.AudioMasterFallbacksAtEnd");
        AssertContains(flashbackPlaybackAudioMasterResultText, "FlashbackPlaybackMaxAudioMasterFallbacksObserved: playbackSessionMetrics.MaxAudioMasterFallbacksObserved");
        AssertContains(flashbackPlaybackAudioMasterResultText, "FlashbackPlaybackMaxAbsAvDriftMsObserved: playbackSessionMetrics.MaxAbsAvDriftMsObserved");
        AssertContains(flashbackPlaybackStagesResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackStagesResultProjection(");
        AssertContains(flashbackPlaybackStagesResultText, "private static DiagnosticSessionFlashbackPlaybackStagesResultProjection BuildFlashbackPlaybackStagesResultProjection(");
        AssertContains(flashbackPlaybackStagesResultText, "FlashbackPlaybackSubmitFailuresDelta: playbackSessionMetrics.SubmitFailuresDelta");
        AssertContains(flashbackPlaybackStagesResultText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta: playbackResultMetrics.SeekForwardDecodeCapHitsDelta");
        AssertContains(flashbackPlaybackStagesResultText, "FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd: playbackResultMetrics.LastSeekHitForwardDecodeCapAtEnd");
        AssertContains(resultText, "FlashbackPlaybackPendingCommandsAtEnd = flashbackPlaybackCommandsResult.FlashbackPlaybackPendingCommandsAtEnd,");
        AssertContains(resultText, "FlashbackPlaybackDroppedFramesDelta = flashbackPlaybackCadenceResult.FlashbackPlaybackDroppedFramesDelta,");
        AssertContains(resultText, "FlashbackPlaybackMaxDecodePhaseAtEnd = flashbackPlaybackDecodeResult.FlashbackPlaybackMaxDecodePhaseAtEnd,");
        AssertContains(resultText, "FlashbackPlaybackAudioMasterFallbacksAtEnd = flashbackPlaybackAudioMasterResult.FlashbackPlaybackAudioMasterFallbacksAtEnd,");
        AssertContains(resultText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta = flashbackPlaybackStagesResult.FlashbackPlaybackSeekForwardDecodeCapHitsDelta,");
        AssertDoesNotContain(flashbackPlaybackResultText, "FlashbackPlaybackPendingCommandsAtEnd: playbackResultMetrics.PendingCommandsAtEnd");
        AssertDoesNotContain(flashbackPlaybackResultText, "FlashbackPlaybackMinOnePercentLowFpsObserved: playbackSessionMetrics.MinOnePercentLowFpsObserved");
        AssertDoesNotContain(flashbackPlaybackResultText, "FlashbackPlaybackMaxDecodePhaseAtEnd: playbackResultMetrics.MaxDecodePhaseAtEnd");
        AssertDoesNotContain(flashbackPlaybackResultText, "FlashbackPlaybackAudioMasterFallbacksAtEnd: playbackResultMetrics.AudioMasterFallbacksAtEnd");
        AssertDoesNotContain(flashbackPlaybackResultText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta: playbackResultMetrics.SeekForwardDecodeCapHitsDelta");
        AssertDoesNotContain(resultText, "FlashbackPlaybackPendingCommandsAtEnd = playbackResultMetrics");
        AssertDoesNotContain(resultText, "FlashbackPlaybackMinOnePercentLowFpsObserved = playbackSessionMetrics");
        AssertDoesNotContain(resultText, "FlashbackPlaybackMaxDecodePhaseAtEnd = playbackResultMetrics");
        AssertDoesNotContain(resultText, "FlashbackPlaybackAudioMasterFallbacksAtEnd = playbackResultMetrics");
        AssertDoesNotContain(resultText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta = playbackResultMetrics");
        AssertContains(resultText, "var flashbackRecordingResult = BuildFlashbackRecordingResultProjection(analysis);");
        AssertContains(flashbackRecordingResultText, "private readonly record struct DiagnosticSessionFlashbackRecordingResultProjection(");
        AssertContains(flashbackRecordingResultText, "private static DiagnosticSessionFlashbackRecordingResultProjection BuildFlashbackRecordingResultProjection(");
        AssertContains(flashbackRecordingResultText, "FlashbackRecordingBackendObserved: recordingMetrics.BackendObserved");
        AssertContains(flashbackRecordingResultText, "FlashbackRecordingIntegrityQueueDroppedFramesDelta: recordingMetrics.IntegrityQueueDroppedFramesDelta");
        AssertDoesNotContain(resultText, "FlashbackRecordingBackendObserved = recordingMetrics");
        AssertDoesNotContain(resultText, "FlashbackRecordingIntegrityQueueDroppedFramesDelta = recordingMetrics");
        AssertContains(resultText, "var flashbackExportResult = BuildFlashbackExportResultProjection(analysis);");
        AssertContains(flashbackExportResultText, "private readonly record struct DiagnosticSessionFlashbackExportResultProjection(");
        AssertContains(flashbackExportResultText, "private static DiagnosticSessionFlashbackExportResultProjection BuildFlashbackExportResultProjection(");
        AssertContains(flashbackExportResultText, "FlashbackExportObserved: exportMetrics.Observed");
        AssertContains(flashbackExportResultText, "FlashbackExportForceRotateFallbacksDelta: exportMetrics.ForceRotateFallbacksDelta");
        AssertContains(flashbackExportResultText, "LastExportSuccessAtEnd: exportMetrics.LastSuccessAtEnd");
        AssertContains(flashbackExportResultText, "FlashbackExportMaxThroughputBytesPerSecObserved: exportMetrics.MaxThroughputBytesPerSecObserved");
        AssertDoesNotContain(resultText, "FlashbackExportObserved = exportMetrics");
        AssertDoesNotContain(resultText, "FlashbackExportForceRotateFallbacksAtEnd = flashbackExportForceRotateFallbacksAtEnd");
        AssertDoesNotContain(resultText, "LastExportSuccessAtEnd = exportMetrics");
        AssertContains(resultText, "var previewResult = BuildPreviewResultProjection(analysis);");
        AssertContains(resultText, "var previewD3DResult = BuildPreviewD3DResultProjection(analysis);");
        AssertContains(resultText, "var previewVisualCadenceResult = BuildPreviewVisualCadenceResultProjection(analysis);");
        AssertContains(previewResultText, "private readonly record struct DiagnosticSessionPreviewResultProjection(");
        AssertContains(previewResultText, "private static DiagnosticSessionPreviewResultProjection BuildPreviewResultProjection(");
        AssertContains(previewD3DResultText, "private readonly record struct DiagnosticSessionPreviewD3DResultProjection(");
        AssertContains(previewD3DResultText, "private static DiagnosticSessionPreviewD3DResultProjection BuildPreviewD3DResultProjection(");
        AssertContains(previewD3DResultText, "var previewD3DMetrics = analysis.PreviewD3DMetrics;");
        AssertContains(previewVisualCadenceResultText, "private readonly record struct DiagnosticSessionPreviewVisualCadenceResultProjection(");
        AssertContains(previewVisualCadenceResultText, "private static DiagnosticSessionPreviewVisualCadenceResultProjection BuildPreviewVisualCadenceResultProjection(");
        AssertContains(previewVisualCadenceResultText, "var visualCadenceMetrics = analysis.VisualCadenceMetrics;");
        AssertContains(previewResultText, "PreviewSchedulerLastDropReasonAtEnd: previewScheduler.LastDropReasonAtEnd");
        AssertContains(previewD3DResultText, "PreviewD3DInputUploadCpuP99MsAtEnd: previewD3DMetrics.InputUploadCpuP99MsAtEnd");
        AssertContains(previewD3DResultText, "PreviewD3DTotalFrameCpuMaxMsObserved: previewD3DMetrics.TotalFrameCpuMaxMsObserved");
        AssertContains(previewVisualCadenceResultText, "VisualCadenceOutputFpsAtEnd: visualCadenceMetrics.OutputFpsAtEnd");
        AssertContains(previewVisualCadenceResultText, "VisualCadenceLongestRepeatRunAtEnd: visualCadenceMetrics.LongestRepeatRunAtEnd");
        AssertContains(resultText, "PreviewD3DInputUploadCpuP99MsAtEnd = previewD3DResult.PreviewD3DInputUploadCpuP99MsAtEnd,");
        AssertContains(resultText, "VisualCadenceOutputFpsAtEnd = previewVisualCadenceResult.VisualCadenceOutputFpsAtEnd,");
        AssertDoesNotContain(resultText, "GetString(lastSnapshot, \"MjpegPreviewJitterLastDropReason\")");
        AssertDoesNotContain(previewResultText, "previewD3DMetrics");
        AssertDoesNotContain(previewResultText, "PreviewD3DInputUploadCpuP99MsAtEnd");
        AssertDoesNotContain(previewResultText, "analysis.VisualCadenceMetrics");
        AssertDoesNotContain(previewResultText, "VisualCadenceOutputFpsAtEnd");
        AssertDoesNotContain(resultText, "PreviewD3DInputUploadCpuP99MsAtEnd = previewResult");
        AssertDoesNotContain(resultText, "PreviewD3DInputUploadCpuP99MsAtEnd = previewD3DMetrics");
        AssertDoesNotContain(resultText, "VisualCadenceOutputFpsAtEnd = previewResult");
        AssertDoesNotContain(resultText, "VisualCadenceOutputFpsAtEnd = visualCadenceMetrics");
        AssertContains(analysisText, "AddFlashbackPlaybackAnalysisWarnings(playbackResultMetrics, warnings);");
        AssertContains(analysisText, "AddFlashbackExportAnalysisWarnings(");
        AssertContains(analysisText, "ValidateFlashbackPreviewSchedulerAnalysis(");
        AssertContains(analysisText, "exportMetrics.ForceRotateFallbacksAtEnd,");
        AssertContains(analysisText, "exportMetrics.ForceRotateFallbacksDelta,");
        AssertContains(analysisText, "exportMetrics.LastForceRotateFallbackSegmentsAtEnd,");
        AssertDoesNotContain(analysisText, "var toleratesPreviewCycleSchedulerSettling =");
        AssertDoesNotContain(analysisText, "var toleratesSparsePreviewSchedulerDeadlineDrops =");
        AssertDoesNotContain(analysisText, "var toleratesSparseScrubSchedulerTransitions =");
        AssertDoesNotContain(analysisText, "var flashbackExportForceRotateFallbacksAtEnd =");
        AssertDoesNotContain(analysisText, "FlashbackExportForceRotateFallbacksAtEnd,");
        AssertDoesNotContain(analysisText, "flashback playback seek forward-decode cap hit during session");
        AssertDoesNotContain(analysisText, "flashback export used force-rotate partial fallback");
        AssertContains(flashbackWarningsText, "private static void AddFlashbackPlaybackAnalysisWarnings(");
        AssertContains(flashbackWarningsText, "private static void AddFlashbackExportAnalysisWarnings(");
        AssertContains(flashbackWarningsText, "flashback playback seek forward-decode cap hit during session");
        AssertContains(flashbackWarningsText, "flashback export used force-rotate partial fallback");
        AssertContains(builderText, "var artifactPaths = await WritePreSummaryAsync(");
        AssertContains(builderText, "SummaryPath = artifactPaths.SummaryPath");
        AssertContains(builderText, "SamplesPath = artifactPaths.SamplesPath");
        AssertContains(builderText, "FrameLedgerPath = artifactPaths.FrameLedgerPath");
        AssertContains(builderText, "TimelinePath = artifactPaths.TimelinePath");
        AssertContains(builderText, "runState.SetStage(\"summary\")");
        AssertContains(builderText, "return await WriteAsync(result, runState, warnings).ConfigureAwait(false);");
        AssertContains(runnerText, "DiagnosticSessionResultBuilder.BuildAndWriteAsync(");
        AssertContains(runnerText, "new DiagnosticSessionResultBuildRequest(");
        AssertDoesNotContain(runnerText, "SetStage(\"result-analysis\")");
        AssertDoesNotContain(runnerText, "var result = new DiagnosticSessionResult");
        AssertDoesNotContain(runnerText, "WriteArtifactBestEffortAsync(\"write-samples\"");
        AssertDoesNotContain(runnerText, "RecordTerminalException(ex, \"summary-write\")");
        AssertDoesNotContain(analysisText, "var previewSchedulerDroppedAtEnd =");
        AssertDoesNotContain(analysisText, "var previewSchedulerMaxScheduleLateMsObserved = samples");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionResultBuilder_DiagnosticHealthVerdictLivesInFocusedPartial()
    {
        var analysisText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Analysis.cs")
            .Replace("\r\n", "\n");
        var healthText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.DiagnosticHealth.cs")
            .Replace("\r\n", "\n");

        AssertContains(analysisText, "var diagnosticHealthSucceeded = AnalyzeDiagnosticHealth(");
        AssertContains(healthText, "private static bool AnalyzeDiagnosticHealth(");
        AssertContains(healthText, "BuildSessionDiagnosticHealthObservation(");
        AssertContains(healthText, "IsSparseSourceCaptureCadenceWarningRun(");
        AssertContains(healthText, "IsSparsePreviewSchedulerDeadlineDropRun(");
        AssertContains(healthText, "IsPreviewSchedulerDiagnosticHealthObservation(diagnosticHealthObservation)");
        AssertContains(healthText, "diagnostic health degraded during session");
        AssertContains(healthText, "diagnostic health {toleratedReason}:");
        AssertContains(healthText, "flashback force-rotate drain warning tolerated for flashback scenario");
        AssertDoesNotContain(analysisText, "BuildSessionDiagnosticHealthObservation(");
        AssertDoesNotContain(analysisText, "diagnostic health degraded during session");
        AssertDoesNotContain(analysisText, "diagnostic health {toleratedReason}:");

        return Task.CompletedTask;
    }
}
