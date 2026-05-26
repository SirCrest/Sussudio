using System.Threading.Tasks;

static partial class Program
{
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
        var validationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackValidation.cs")
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
        AssertDoesNotContain(executionText, "using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;");
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
}
