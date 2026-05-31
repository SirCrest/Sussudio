using System.Threading.Tasks;

static partial class Program
{
    private static string ReadDiagnosticSessionResultBuilderAnalysisSource()
    {
        var builderText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        return ExtractTextBetween(
            builderText,
            "private sealed record DiagnosticSessionResultAnalysis(",
            "internal sealed record DiagnosticSessionResultBuildRequest(");
    }

    internal static Task DiagnosticSessionModels_AreSplitFromRunnerBehavior()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var modelText = ReadDiagnosticSessionModelsSource();
        var resultText = ReadRepoFile("tools/Common/DiagnosticSessionResult.cs");

        AssertContains(modelText, "public sealed class DiagnosticSessionOptions");
        AssertContains(modelText, "public sealed class DiagnosticSessionResult");
        AssertDoesNotContain(modelText, "public sealed partial class DiagnosticSessionResult");
        AssertContains(resultText, "public string SessionId { get; init; } = string.Empty;");
        AssertContains(resultText, "public string[] Warnings { get; set; } = Array.Empty<string>();");
        AssertContains(resultText, "// End-of-run overview.");
        AssertContains(resultText, "public double ProcessCpuPercentAtEnd { get; init; }");
        AssertContains(resultText, "public PresentMonProbeResult? PresentMon { get; init; }");
        AssertContains(resultText, "// Capture/source summary.");
        AssertContains(resultText, "public string SelectedResolutionAtEnd { get; init; } = string.Empty;");
        AssertContains(resultText, "public string SourceTelemetrySummaryAtEnd { get; init; } = string.Empty;");
        AssertContains(resultText, "// Flashback playback command queue summary.");
        AssertContains(resultText, "public int FlashbackPlaybackPendingCommandsAtEnd { get; init; }");
        AssertContains(resultText, "// Flashback playback cadence and frame-delivery summary.");
        AssertContains(resultText, "public double FlashbackPlaybackObservedFpsAtEnd { get; init; }");
        AssertContains(resultText, "// Flashback playback 1% low sample-window summary.");
        AssertContains(resultText, "public double FlashbackPlaybackOnePercentLowFpsAtEnd { get; init; }");
        AssertContains(resultText, "// Flashback playback decode timing summary.");
        AssertContains(resultText, "public double FlashbackPlaybackDecodeP99MsAtEnd { get; init; }");
        AssertContains(resultText, "// Flashback playback audio-master summary.");
        AssertContains(resultText, "public long FlashbackPlaybackAudioMasterFallbacksAtEnd { get; init; }");
        AssertContains(resultText, "// Flashback playback stage and seek summary.");
        AssertContains(resultText, "public long FlashbackPlaybackSubmitFailuresAtEnd { get; init; }");
        AssertContains(resultText, "public bool FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd { get; init; }");
        AssertContains(resultText, "// Flashback recording summary.");
        AssertContains(resultText, "public bool FlashbackRecordingBackendObserved { get; init; }");
        AssertContains(resultText, "// Flashback export summary.");
        AssertContains(resultText, "public string FlashbackExportStatusAtEnd { get; init; } = string.Empty;");
        AssertContains(resultText, "// Preview cadence summary.");
        AssertContains(resultText, "public double PreviewCadenceOnePercentLowFpsAtEnd { get; init; }");
        AssertContains(resultText, "// Preview visual-cadence summary.");
        AssertContains(resultText, "public double VisualCadenceOutputFpsAtEnd { get; init; }");
        AssertContains(resultText, "// Preview scheduler and jitter-buffer summary.");
        AssertContains(resultText, "public long PreviewSchedulerDroppedAtEnd { get; init; }");
        AssertContains(resultText, "// Preview D3D frame-stat and CPU timing summary.");
        AssertContains(resultText, "public double PreviewD3DInputUploadCpuP99MsAtEnd { get; init; }");
        AssertContains(modelText, "public sealed class DiagnosticSessionSample");
        AssertContains(modelText, "public string TerminalState { get; set; }");
        AssertContains(modelText, "public JsonElement Snapshot { get; init; }");
        AssertContains(runnerText, "public static class DiagnosticSessionRunner");
        AssertContains(runnerText, "public static async Task<DiagnosticSessionResult> RunAsync(");
        AssertContains(runnerText, "private static async Task<DiagnosticSessionResult> RunCompletionPhaseAsync(");
        AssertDoesNotContain(runnerText, "public sealed class DiagnosticSessionResult");
        AssertDoesNotContain(runnerText, "public sealed class DiagnosticSessionOptions");
        AssertDoesNotContain(runnerText, "public sealed class DiagnosticSessionSample");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionOptionalTextFormatter_OwnsSharedFormattingHelpers()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var formatterText = ReadDiagnosticSessionResultFormatterSource();
        var formatterRootText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.cs")
            .Replace("\r\n", "\n");
        var validationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSupport.cs")
            .Replace("\r\n", "\n");

        AssertContains(formatterRootText, "internal static class DiagnosticSessionOptionalTextFormatter");
        AssertContains(formatterRootText, "internal static string FormatOptional(string value)");
        AssertContains(formatterRootText, "string.IsNullOrWhiteSpace(value) ? \"none\" : value");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionOptionalTextFormatter.cs")), "Optional diagnostic text formatting stays folded into DiagnosticSessionResultFormatter.cs");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;");
        AssertContains(formatterText, "using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;");
        AssertContains(validationText, "using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;");
        AssertDoesNotContain(runnerText, "private static string FormatOptional(");
        AssertDoesNotContain(validationText, "private static string FormatOptional(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionJsonArtifacts_OwnsJsonWritingAndResponseExtractionSplit()
    {
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var initialSnapshotText = ReadDiagnosticSessionRunContextSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var responseJsonText = initialSnapshotText;

        AssertContains(builderText, "internal static class DiagnosticSessionJsonArtifacts");
        AssertContains(builderText, "internal static JsonElement CreateEmptyJsonObject()");
        AssertContains(builderText, "internal static async Task WriteJsonAsync<T>(");
        AssertContains(responseJsonText, "internal static class DiagnosticSessionAutomationResponseJson");
        AssertContains(responseJsonText, "internal static bool TryGetSnapshot(");
        AssertContains(responseJsonText, "internal static bool TryGetVerification(");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionResultArtifacts.cs")), "Result artifact helpers stay folded into DiagnosticSessionResultBuilder.cs");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionAutomationResponseJson.cs")), "Automation response JSON helpers stay folded into DiagnosticSessionRunContext.cs");
        AssertContains(initialSnapshotText, "using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;");
        AssertContains(initialSnapshotText, "using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;");
        AssertDoesNotContain(builderText, "TryGetSnapshot(");
        AssertDoesNotContain(builderText, "TryGetVerification(");
        AssertDoesNotContain(executionText, "using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;");
        AssertContains(executionText, "using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;");
        AssertDoesNotContain(executionText, "private static async Task WriteJsonAsync<T>(");
        AssertDoesNotContain(executionText, "private static bool TryGetSnapshot(");
        AssertDoesNotContain(executionText, "private static bool TryGetVerification(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionResultBuilder_OwnsSummaryWriteFailures()
    {
        var builderText = ReadDiagnosticSessionResultBuilderSource();

        AssertContains(builderText, "private static async Task<DiagnosticSessionResult> WriteSummaryAsync(");
        AssertContains(builderText, "await WriteJsonAsync(result.SummaryPath, result, CancellationToken.None)");
        AssertContains(builderText, "runState.RecordTerminalException(ex, \"summary-write\")");
        AssertContains(builderText, "result.Success = false;");
        AssertContains(builderText, "result.CompletedUtc = DateTimeOffset.UtcNow;");
        AssertContains(builderText, "result.TerminalState = runState.GetTerminalState();");
        AssertContains(builderText, "result.LastStage = runState.GetResultLastStage();");
        AssertContains(builderText, "result.Warnings = warnings.ToArray();");
        AssertContains(builderText, "runState.SetStage(\"summary-written\")");
        AssertContains(builderText, "WriteSummaryAsync(result, runState, warnings)");
        AssertDoesNotContain(builderText, "using static Sussudio.Tools.DiagnosticSessionSummaryWriter;");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionResultArtifacts_OwnPreSummaryWrites()
    {
        var builderText = ReadDiagnosticSessionResultBuilderSource();

        AssertContains(builderText, "internal static class DiagnosticSessionResultArtifacts");
        AssertContains(builderText, "internal static async Task<DiagnosticSessionResultArtifactPaths> WritePreSummaryAsync(");
        AssertContains(builderText, "internal readonly record struct DiagnosticSessionResultArtifactPaths(");
        AssertContains(builderText, "SummaryPath: Path.Combine(outputDirectory, \"summary.json\")");
        AssertContains(builderText, "SamplesPath: Path.Combine(outputDirectory, \"samples.json\")");
        AssertContains(builderText, "FrameLedgerPath: Path.Combine(outputDirectory, \"frame-ledger.json\")");
        AssertContains(builderText, "TimelinePath: Path.Combine(outputDirectory, \"timeline.json\")");
        AssertContains(builderText, "private static object BuildFrameLedgerTrace(");
        AssertContains(builderText, "using static Sussudio.Tools.AutomationSnapshotFormatter;");
        AssertContains(builderText, "runState.WriteArtifactBestEffortAsync(\"write-samples\", paths.SamplesPath, samples)");
        AssertContains(builderText, "runState.WriteArtifactBestEffortAsync(\"write-frame-ledger\", paths.FrameLedgerPath, BuildFrameLedgerTrace(sessionId, samples))");
        AssertContains(builderText, "runState.WriteArtifactBestEffortAsync(\"write-timeline\", paths.TimelinePath, timeline)");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionResultArtifacts;");
        AssertContains(builderText, "WritePreSummaryAsync(");
        AssertDoesNotContain(builderText, "Path.Combine(request.OutputDirectory, \"samples.json\")");
        AssertDoesNotContain(builderText, "BuildFrameLedgerTrace(request.SessionId, samples)");

        return Task.CompletedTask;
    }


    internal static Task DiagnosticSessionResultFormatter_OwnsFormattedSummaryText()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var formatterText = ReadDiagnosticSessionResultFormatterSource();
        var formatterRootText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.cs")
            .Replace("\r\n", "\n");

        AssertContains(formatterRootText, "public static class DiagnosticSessionResultFormatter");
        AssertContains(formatterRootText, "public static string Format(DiagnosticSessionResult result)");
        AssertContains(formatterRootText, "AppendOverview(builder, result);");
        AssertContains(formatterRootText, "AppendCaptureMode(builder, result);");
        AssertContains(formatterRootText, "AppendRecordingVerification(builder, result);");
        AssertContains(formatterRootText, "AppendPresentMon(builder, result);");
        AssertContains(formatterRootText, "AppendProcessPerformance(builder, result);");
        AssertContains(formatterRootText, "private static void AppendOverview(");
        AssertContains(formatterRootText, "== Diagnostic Session:");
        AssertContains(formatterRootText, "private static void AppendCaptureMode(");
        AssertContains(formatterRootText, "\"Capture Mode: \"");
        AssertContains(formatterRootText, "private static string FormatFrameRate(");
        AssertContains(formatterRootText, "CultureInfo.InvariantCulture");
        AssertContains(formatterRootText, "private static void AppendRecordingVerification(");
        AssertContains(formatterRootText, "\"Recording Verification: ");
        AssertContains(formatterRootText, "private static void AppendPresentMon(");
        AssertContains(formatterRootText, "\"PresentMon: ");
        AssertContains(formatterRootText, "private static void AppendProcessPerformance(");
        AssertContains(formatterRootText, "\"Process Perf: \"");
        AssertContains(formatterRootText, "private static void AppendFlashbackSections(");
        AssertContains(formatterRootText, "AppendFlashbackPlaybackCommands(builder, result);");
        AssertContains(formatterRootText, "AppendFlashbackRecording(builder, result);");
        AssertContains(formatterRootText, "AppendFlashbackExport(builder, result);");
        AssertContains(formatterRootText, "private static void AppendFlashbackPlaybackCommands(");
        AssertContains(formatterRootText, "\"Flashback Playback Commands: \"");
        AssertContains(formatterRootText, "private static void AppendFlashbackPlaybackStages(");
        AssertContains(formatterRootText, "\"Flashback Playback Stages: \"");
        AssertContains(formatterRootText, "private static void AppendFlashbackRecording(");
        AssertContains(formatterRootText, "\"Flashback Recording: \"");
        AssertContains(formatterRootText, "private static void AppendFlashbackExport(");
        AssertContains(formatterRootText, "\"Flashback Export: \"");
        AssertContains(formatterRootText, "\"Flashback Playback Perf: \"");
        AssertContains(formatterRootText, "private static void AppendPreviewSections(");
        AssertContains(formatterRootText, "AppendPreviewScheduler(builder, result);");
        AssertContains(formatterRootText, "AppendPreviewD3DPerformance(builder, result);");
        AssertContains(formatterRootText, "AppendPreviewD3DCpuTiming(builder, result);");
        AssertContains(formatterRootText, "AppendPreviewVisualCadence(builder, result);");
        AssertContains(formatterRootText, "private static void AppendPreviewScheduler(");
        AssertContains(formatterRootText, "\"Preview Scheduler: \"");
        AssertContains(formatterRootText, "FormatOptional(result.PreviewSchedulerLastUnderflowReasonAtEnd)");
        AssertContains(formatterRootText, "private static void AppendPreviewD3DPerformance(");
        AssertContains(formatterRootText, "\"Preview D3D Perf: \"");
        AssertContains(formatterRootText, "private static void AppendPreviewD3DCpuTiming(");
        AssertContains(formatterRootText, "\"Preview D3D CPU Timing: \"");
        AssertContains(formatterRootText, "private static void AppendPreviewVisualCadence(");
        AssertContains(formatterRootText, "\"Preview Visual Cadence: \"");
        AssertContains(formatterText, "private static void AppendFlashbackSections(");
        AssertContains(formatterText, "private static void AppendPreviewSections(");
        AssertContains(formatterText, "private static void AppendArtifacts(");
        AssertContains(formatterText, "\"Flashback Playback Perf: \"");
        AssertContains(formatterRootText, "FormatOptional(result.FlashbackPlaybackMaxCommandQueueLatencyCommandObserved)");
        AssertContains(formatterRootText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta");
        AssertContains(formatterRootText, "FlashbackRecordingVideoFramesSubmittedDelta");
        AssertContains(formatterRootText, "FlashbackExportForceRotateFallbacksDelta");
        AssertContains(formatterRootText, "FormatBytes(result.FlashbackExportMaxOutputBytesObserved)");
        AssertContains(formatterRootText, "BuildFlashbackPlaybackCadencePerformanceText(result)");
        AssertContains(formatterRootText, "BuildFlashbackPlaybackAudioMasterPerformanceText(result)");
        AssertContains(formatterRootText, "BuildFlashbackPlaybackSubmitPerformanceText(result)");
        AssertContains(formatterRootText, "submitFailuresDelta={result.FlashbackPlaybackSubmitFailuresDelta}");
        AssertContains(formatterRootText, "private static string BuildFlashbackPlaybackCadencePerformanceText(");
        AssertContains(formatterRootText, "BuildFlashbackPlaybackOnePercentLowPerformanceText(result)");
        AssertContains(formatterRootText, "droppedFramesDelta={result.FlashbackPlaybackDroppedFramesDelta}");
        AssertContains(formatterRootText, "private static string BuildFlashbackPlaybackOnePercentLowPerformanceText(");
        AssertContains(formatterRootText, "onePercentLowMinAvDriftMs={result.FlashbackPlaybackMinOnePercentLowAvDriftMs:0.##}");
        AssertContains(formatterRootText, "onePercentLowMinAudioFallbacks={result.FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks}");
        AssertContains(formatterRootText, "private static string BuildFlashbackPlaybackAudioMasterPerformanceText(");
        AssertContains(formatterRootText, "FormatOptional(result.FlashbackPlaybackAudioMasterLastFallbackReasonAtEnd)");
        AssertContains(formatterRootText, "absAvDriftMsMax={result.FlashbackPlaybackMaxAbsAvDriftMsObserved:0.##}");
        AssertContains(formatterRootText, "private static void AppendFlashbackPlaybackDecode(");
        AssertContains(formatterRootText, "\"Flashback Playback Decode: \"");
        AssertContains(formatterRootText, "FormatOptional(result.PreviewD3DLatestSlowFrameReason)");
        AssertContains(formatterRootText, "PreviewD3DInputUploadCpuP99MsAtEnd");
        AssertContains(formatterRootText, "VisualCadenceLongestRepeatRunAtEnd");
        AssertContains(runnerText, "return DiagnosticSessionResultFormatter.Format(result);");
        AssertDoesNotContain(runnerText, "== Diagnostic Session:");
        AssertDoesNotContain(runnerText, "\"Flashback Playback Perf: \"");
        AssertDoesNotContain(runnerText, "private static string FormatFrameRate(");

        return Task.CompletedTask;
    }

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

    private static void AssertDiagnosticSessionResultBuilderPreviewProjectionOwnership()
    {
        var builderText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var flatteningText = ExtractMemberCode(builderText, "FlattenResultProjectionSet");
        var projectionSetText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Projections.cs")
            .Replace("\r\n", "\n");
        var previewResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Projections.cs")
            .Replace("\r\n", "\n");
        var previewD3DResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Projections.cs")
            .Replace("\r\n", "\n");
        var analysisText = ReadDiagnosticSessionResultBuilderAnalysisSource();

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
        var analysisText = ReadDiagnosticSessionResultBuilderAnalysisSource();

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

    internal static Task DiagnosticSessionResultBuilder_DiagnosticHealthVerdictLivesWithAnalysis()
    {
        var analysisText = ReadDiagnosticSessionResultBuilderAnalysisSource();
        var healthText = analysisText;

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
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionResultBuilder.DiagnosticHealth.cs")),
            "diagnostic health verdict helpers folded into analysis owner");
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

    private static void AssertDiagnosticSessionResultBuilderCoreOwnership()
    {
        var builderText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var flatteningText = ExtractMemberCode(builderText, "FlattenResultProjectionSet");
        var projectionSetText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Projections.cs")
            .Replace("\r\n", "\n");
        var resultBuildRequestText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var analysisText = ReadDiagnosticSessionResultBuilderAnalysisSource();
        var diagnosticHealthText = analysisText;

        AssertContains(builderText, "internal static partial class DiagnosticSessionResultBuilder");
        AssertContains(builderText, "internal static async Task<DiagnosticSessionResult> BuildAndWriteAsync(");
        AssertContains(builderText, "private static DiagnosticSessionResult CreateResult(");
        AssertContains(flatteningText, "private static DiagnosticSessionResult FlattenResultProjectionSet(");
        AssertContains(projectionSetText, "private static DiagnosticSessionResultProjectionSet BuildResultProjectionSet(");
        AssertContains(projectionSetText, "private readonly record struct DiagnosticSessionResultProjectionSet(");
        AssertContains(analysisText, "private static DiagnosticSessionPreviewSchedulerAnalysis BuildPreviewSchedulerAnalysis(");
        AssertContains(analysisText, "private readonly record struct DiagnosticSessionPreviewSchedulerAnalysis(");
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
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionResultBuilder.Analysis.cs")),
            "diagnostic-session analysis folded into DiagnosticSessionResultBuilder.cs");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionResultBuilder.DiagnosticHealth.cs")),
            "diagnostic health verdict helpers folded into DiagnosticSessionResultBuilder.cs");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionResultBuilder.FlashbackPlaybackResult.cs")),
            "Flashback playback result projection folded into DiagnosticSessionResultBuilder.Projections.cs");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionResultBuilder.Flattening.cs")),
            "final result DTO flattening folded into DiagnosticSessionResultBuilder.cs");
        AssertDoesNotContain(flatteningText, "private static DiagnosticSessionResultProjectionSet BuildResultProjectionSet(");
        AssertDoesNotContain(builderText, "private static DiagnosticSessionResultProjectionSet BuildResultProjectionSet(");
        AssertDoesNotContain(builderText, "private readonly record struct DiagnosticSessionResultProjectionSet(");
        AssertContains(builderText, "return new DiagnosticSessionResult\n        {");
        AssertContains(analysisText, "IsToleratedFlashbackScenarioWarning(");
    }

    private static void AssertDiagnosticSessionResultBuilderPreviewSchedulerOwnership()
    {
        var analysisText = ReadDiagnosticSessionResultBuilderAnalysisSource();
        var previewResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Projections.cs")
            .Replace("\r\n", "\n");

        AssertContains(analysisText, "previewScheduler,");
        AssertContains(analysisText, "private readonly record struct DiagnosticSessionPreviewSchedulerAnalysis(");
        AssertContains(analysisText, "string LastDropReasonAtEnd,");
        AssertContains(analysisText, "string LastUnderflowReasonAtEnd,");
        AssertContains(analysisText, "double LastUnderflowInputAgeMsAtEnd,");
        AssertContains(analysisText, "double LastUnderflowOutputAgeMsAtEnd");
        AssertContains(analysisText, "LastDropReasonAtEnd: GetString(lastSnapshot, \"MjpegPreviewJitterLastDropReason\") ?? string.Empty");
        AssertContains(analysisText, "LastUnderflowReasonAtEnd: GetString(lastSnapshot, \"MjpegPreviewJitterLastUnderflowReason\") ?? string.Empty");
        AssertContains(analysisText, "LastUnderflowInputAgeMsAtEnd: GetDouble(lastSnapshot, \"MjpegPreviewJitterLastUnderflowInputAgeMs\")");
        AssertContains(analysisText, "LastUnderflowOutputAgeMsAtEnd: GetDouble(lastSnapshot, \"MjpegPreviewJitterLastUnderflowOutputAgeMs\")");
        AssertContains(analysisText, "private static void ValidateFlashbackPreviewSchedulerAnalysis(");
        AssertContains(analysisText, "var previewTargetFps = GetDouble(lastSnapshot, \"ExpectedCaptureFrameRate\");");
        AssertContains(analysisText, "previewTargetFps = GetDouble(lastSnapshot, \"SelectedExactFrameRate\");");
        AssertContains(analysisText, "var toleratesPreviewCycleSchedulerSettling =");
        AssertContains(analysisText, "scenarioPlan.IsPreviewCycleScenario && visualCadenceHealthy");
        AssertContains(analysisText, "var toleratesSparsePreviewSchedulerDeadlineDrops =");
        AssertContains(analysisText, "IsSparsePreviewSchedulerDeadlineDropRun(");
        AssertContains(analysisText, "var toleratesSparseScrubSchedulerTransitions =");
        AssertContains(analysisText, "scenarioPlan.ToleratesSparsePreviewSchedulerStressTransitions &&");
        AssertContains(analysisText, "IsSparsePreviewSchedulerStressRun(");
        AssertContains(analysisText, "ValidateFlashbackPreviewScheduler(");
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
        AssertDoesNotContain(analysisText, "var previewSchedulerDroppedAtEnd =");
        AssertDoesNotContain(analysisText, "var previewSchedulerMaxScheduleLateMsObserved = samples");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionResultBuilder.PreviewScheduler.cs")),
            "preview scheduler analysis folded into DiagnosticSessionResultBuilder.cs");
    }

    private static void AssertDiagnosticSessionResultBuilderOverviewAndCaptureProjectionOwnership()
    {
        var builderText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var flatteningText = ExtractMemberCode(builderText, "FlattenResultProjectionSet");
        var projectionSetText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Projections.cs")
            .Replace("\r\n", "\n");
        var analysisText = ReadDiagnosticSessionResultBuilderAnalysisSource();
        var overviewResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Projections.cs")
            .Replace("\r\n", "\n");
        var captureResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Projections.cs")
            .Replace("\r\n", "\n");

        AssertContains(builderText, "var resultProjections = BuildResultProjectionSet(request, runState, analysis);");
        AssertContains(builderText, "return FlattenResultProjectionSet(");
        AssertContains(builderText, "return new DiagnosticSessionResult\n        {");
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

    private static void AssertDiagnosticSessionResultBuilderFlashbackProjectionOwnership()
    {
        var builderText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var flatteningText = ExtractMemberCode(builderText, "FlattenResultProjectionSet");
        var projectionSetText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Projections.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackResultText = projectionSetText;
        var flashbackRecordingResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Projections.cs")
            .Replace("\r\n", "\n");
        var flashbackExportResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Projections.cs")
            .Replace("\r\n", "\n");

        AssertContains(builderText, "return FlattenResultProjectionSet(");
        AssertContains(flatteningText, "private static DiagnosticSessionResult FlattenResultProjectionSet(");
        AssertContains(projectionSetText, "FlashbackPlayback: BuildFlashbackPlaybackResultProjection(analysis)");
        AssertContains(flatteningText, "var flashbackPlaybackResult = resultProjections.FlashbackPlayback;");
        AssertContains(flatteningText, "var flashbackPlaybackCommandsResult = flashbackPlaybackResult.CommandsResult;");
        AssertContains(flatteningText, "var flashbackPlaybackCadenceResult = flashbackPlaybackResult.CadenceResult;");
        AssertContains(flatteningText, "var flashbackPlaybackOnePercentLowResult = flashbackPlaybackResult.OnePercentLowResult;");
        AssertContains(flatteningText, "var flashbackPlaybackDecodeResult = flashbackPlaybackResult.DecodeResult;");
        AssertContains(flatteningText, "var flashbackPlaybackAudioMasterResult = flashbackPlaybackResult.AudioMasterResult;");
        AssertContains(flatteningText, "var flashbackPlaybackStagesResult = flashbackPlaybackResult.StagesResult;");
        AssertContains(flashbackPlaybackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackResultProjection(");
        AssertContains(flashbackPlaybackResultText, "private static DiagnosticSessionFlashbackPlaybackResultProjection BuildFlashbackPlaybackResultProjection(");
        AssertContains(flashbackPlaybackResultText, "CommandsResult: commandsResult");
        AssertContains(flashbackPlaybackResultText, "CadenceResult: cadenceResult");
        AssertContains(flashbackPlaybackResultText, "OnePercentLowResult: onePercentLowResult");
        AssertContains(flashbackPlaybackResultText, "DecodeResult: decodeResult");
        AssertContains(flashbackPlaybackResultText, "AudioMasterResult: audioMasterResult");
        AssertContains(flashbackPlaybackResultText, "StagesResult: stagesResult");
        AssertContains(flashbackPlaybackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackCommandsResultProjection(");
        AssertContains(flashbackPlaybackResultText, "private static DiagnosticSessionFlashbackPlaybackCommandsResultProjection BuildFlashbackPlaybackCommandsResultProjection(");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackPendingCommandsAtEnd: playbackResultMetrics.PendingCommandsAtEnd");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackMaxCommandQueueLatencyCommandObserved: playbackResultMetrics.MaxCommandQueueLatencyCommandObserved");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackLastCommandFailureAtEnd: playbackResultMetrics.LastCommandFailureAtEnd");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd: playbackResultMetrics.LastCommandFailureUtcUnixMsAtEnd");
        AssertContains(flashbackPlaybackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackCadenceResultProjection(");
        AssertContains(flashbackPlaybackResultText, "private static DiagnosticSessionFlashbackPlaybackCadenceResultProjection BuildFlashbackPlaybackCadenceResultProjection(");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackDroppedFramesDelta: playbackSessionMetrics.DroppedFramesDelta");
        AssertContains(flashbackPlaybackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackOnePercentLowResultProjection(");
        AssertContains(flashbackPlaybackResultText, "private static DiagnosticSessionFlashbackPlaybackOnePercentLowResultProjection BuildFlashbackPlaybackOnePercentLowResultProjection(");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackMinOnePercentLowFpsObserved: playbackSessionMetrics.MinOnePercentLowFpsObserved");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackMinOnePercentLowDecodeP99Ms: playbackSessionMetrics.MinOnePercentLowDecodeP99Ms");
        AssertContains(flashbackPlaybackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackDecodeResultProjection(");
        AssertContains(flashbackPlaybackResultText, "private static DiagnosticSessionFlashbackPlaybackDecodeResultProjection BuildFlashbackPlaybackDecodeResultProjection(");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackMaxDecodePhaseAtEnd: playbackResultMetrics.MaxDecodePhaseAtEnd");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackMaxDecodeP99MsObserved: playbackSessionMetrics.MaxDecodeP99MsObserved");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackMaxDecodePositionMsObserved: playbackSessionMetrics.MaxDecodePositionMsObserved");
        AssertContains(flashbackPlaybackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackAudioMasterResultProjection(");
        AssertContains(flashbackPlaybackResultText, "private static DiagnosticSessionFlashbackPlaybackAudioMasterResultProjection BuildFlashbackPlaybackAudioMasterResultProjection(");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackAudioMasterFallbacksAtEnd: playbackResultMetrics.AudioMasterFallbacksAtEnd");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackMaxAudioMasterFallbacksObserved: playbackSessionMetrics.MaxAudioMasterFallbacksObserved");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackMaxAbsAvDriftMsObserved: playbackSessionMetrics.MaxAbsAvDriftMsObserved");
        AssertContains(flashbackPlaybackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackStagesResultProjection(");
        AssertContains(flashbackPlaybackResultText, "private static DiagnosticSessionFlashbackPlaybackStagesResultProjection BuildFlashbackPlaybackStagesResultProjection(");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackSubmitFailuresDelta: playbackSessionMetrics.SubmitFailuresDelta");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta: playbackResultMetrics.SeekForwardDecodeCapHitsDelta");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd: playbackResultMetrics.LastSeekHitForwardDecodeCapAtEnd");
        AssertDoesNotContain(builderText, "private readonly record struct DiagnosticSessionFlashbackPlaybackResultProjection(");
        AssertContains(flatteningText, "FlashbackPlaybackPendingCommandsAtEnd = flashbackPlaybackCommandsResult.FlashbackPlaybackPendingCommandsAtEnd,");
        AssertContains(flatteningText, "FlashbackPlaybackDroppedFramesDelta = flashbackPlaybackCadenceResult.FlashbackPlaybackDroppedFramesDelta,");
        AssertContains(flatteningText, "FlashbackPlaybackMinOnePercentLowFpsObserved = flashbackPlaybackOnePercentLowResult.FlashbackPlaybackMinOnePercentLowFpsObserved,");
        AssertContains(flatteningText, "FlashbackPlaybackMaxDecodePhaseAtEnd = flashbackPlaybackDecodeResult.FlashbackPlaybackMaxDecodePhaseAtEnd,");
        AssertContains(flatteningText, "FlashbackPlaybackAudioMasterFallbacksAtEnd = flashbackPlaybackAudioMasterResult.FlashbackPlaybackAudioMasterFallbacksAtEnd,");
        AssertContains(flatteningText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta = flashbackPlaybackStagesResult.FlashbackPlaybackSeekForwardDecodeCapHitsDelta,");
        AssertDoesNotContain(builderText, "FlashbackPlaybackPendingCommandsAtEnd: playbackResultMetrics.PendingCommandsAtEnd");
        AssertDoesNotContain(builderText, "FlashbackPlaybackMinOnePercentLowFpsObserved: playbackSessionMetrics.MinOnePercentLowFpsObserved");
        AssertDoesNotContain(builderText, "FlashbackPlaybackMaxDecodePhaseAtEnd: playbackResultMetrics.MaxDecodePhaseAtEnd");
        AssertDoesNotContain(builderText, "FlashbackPlaybackAudioMasterFallbacksAtEnd: playbackResultMetrics.AudioMasterFallbacksAtEnd");
        AssertDoesNotContain(builderText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta: playbackResultMetrics.SeekForwardDecodeCapHitsDelta");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionResultBuilder.FlashbackPlaybackResult.cs")),
            "Flashback playback result projection folded into projection-set owner");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackPendingCommandsAtEnd = playbackResultMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackMinOnePercentLowFpsObserved = playbackSessionMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackMinOnePercentLowFpsObserved = flashbackPlaybackCadenceResult");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackMaxDecodePhaseAtEnd = playbackResultMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackAudioMasterFallbacksAtEnd = playbackResultMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta = playbackResultMetrics");
        AssertContains(projectionSetText, "FlashbackRecording: BuildFlashbackRecordingResultProjection(analysis)");
        AssertContains(flatteningText, "var flashbackRecordingResult = resultProjections.FlashbackRecording;");
        AssertContains(flashbackRecordingResultText, "private readonly record struct DiagnosticSessionFlashbackRecordingResultProjection(");
        AssertContains(flashbackRecordingResultText, "private static DiagnosticSessionFlashbackRecordingResultProjection BuildFlashbackRecordingResultProjection(");
        AssertContains(flashbackRecordingResultText, "FlashbackRecordingBackendObserved: recordingMetrics.BackendObserved");
        AssertContains(flashbackRecordingResultText, "FlashbackRecordingIntegrityQueueDroppedFramesDelta: recordingMetrics.IntegrityQueueDroppedFramesDelta");
        AssertDoesNotContain(flatteningText, "FlashbackRecordingBackendObserved = recordingMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackRecordingIntegrityQueueDroppedFramesDelta = recordingMetrics");
        AssertContains(projectionSetText, "FlashbackExport: BuildFlashbackExportResultProjection(analysis)");
        AssertContains(flatteningText, "var flashbackExportResult = resultProjections.FlashbackExport;");
        AssertContains(flashbackExportResultText, "private readonly record struct DiagnosticSessionFlashbackExportResultProjection(");
        AssertContains(flashbackExportResultText, "private static DiagnosticSessionFlashbackExportResultProjection BuildFlashbackExportResultProjection(");
        AssertContains(flashbackExportResultText, "FlashbackExportObserved: exportMetrics.Observed");
        AssertContains(flashbackExportResultText, "FlashbackExportForceRotateFallbacksDelta: exportMetrics.ForceRotateFallbacksDelta");
        AssertContains(flashbackExportResultText, "LastExportSuccessAtEnd: exportMetrics.LastSuccessAtEnd");
        AssertContains(flashbackExportResultText, "FlashbackExportMaxThroughputBytesPerSecObserved: exportMetrics.MaxThroughputBytesPerSecObserved");
        AssertDoesNotContain(flatteningText, "FlashbackExportObserved = exportMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackExportForceRotateFallbacksAtEnd = flashbackExportForceRotateFallbacksAtEnd");
        AssertDoesNotContain(flatteningText, "LastExportSuccessAtEnd = exportMetrics");
    }
}
