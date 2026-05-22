using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticSessionModels_AreSplitFromRunnerBehavior()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var modelText = ReadDiagnosticSessionModelsSource();
        var resultText = ReadRepoFile("tools/Common/DiagnosticSessionResult.cs");

        AssertContains(modelText, "public sealed class DiagnosticSessionOptions");
        AssertContains(modelText, "public sealed partial class DiagnosticSessionResult");
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
        AssertContains(runnerText, "public static Task<DiagnosticSessionResult> RunAsync(");
        AssertContains(runnerText, "internal static async Task<DiagnosticSessionResult> RunAsync(");
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
        var validationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackValidation.Preview.cs")
            .Replace("\r\n", "\n");
        var textHelpersText = ReadRepoFile("tools/Common/DiagnosticSessionOptionalTextFormatter.cs")
            .Replace("\r\n", "\n");

        AssertContains(textHelpersText, "internal static class DiagnosticSessionOptionalTextFormatter");
        AssertContains(textHelpersText, "internal static string FormatOptional(string value)");
        AssertContains(textHelpersText, "string.IsNullOrWhiteSpace(value) ? \"none\" : value");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;");
        AssertContains(formatterText, "using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;");
        AssertContains(validationText, "using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;");
        AssertDoesNotContain(runnerText, "private static string FormatOptional(");
        AssertDoesNotContain(formatterText, "private static string FormatOptional(");
        AssertDoesNotContain(validationText, "private static string FormatOptional(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionJsonArtifacts_OwnsJsonWritingAndResponseExtractionSplit()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var initialSnapshotText = ReadRepoFile("tools/Common/DiagnosticSessionInitialSnapshot.cs")
            .Replace("\r\n", "\n");
        var jsonArtifactsText = ReadRepoFile("tools/Common/DiagnosticSessionJsonArtifacts.cs")
            .Replace("\r\n", "\n");
        var responseJsonText = ReadRepoFile("tools/Common/DiagnosticSessionAutomationResponseJson.cs")
            .Replace("\r\n", "\n");

        AssertContains(jsonArtifactsText, "internal static class DiagnosticSessionJsonArtifacts");
        AssertContains(jsonArtifactsText, "internal static JsonElement CreateEmptyJsonObject()");
        AssertContains(jsonArtifactsText, "internal static async Task WriteJsonAsync<T>(");
        AssertContains(responseJsonText, "internal static class DiagnosticSessionAutomationResponseJson");
        AssertContains(responseJsonText, "internal static bool TryGetSnapshot(");
        AssertContains(responseJsonText, "internal static bool TryGetVerification(");
        AssertContains(initialSnapshotText, "using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;");
        AssertContains(initialSnapshotText, "using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;");
        AssertDoesNotContain(jsonArtifactsText, "BuildFrameLedgerTrace(");
        AssertDoesNotContain(jsonArtifactsText, "TryGetSnapshot(");
        AssertDoesNotContain(jsonArtifactsText, "TryGetVerification(");
        AssertDoesNotContain(runnerText, "using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;");
        AssertDoesNotContain(runnerText, "using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;");
        AssertDoesNotContain(runnerText, "private static async Task WriteJsonAsync<T>(");
        AssertDoesNotContain(runnerText, "private static bool TryGetSnapshot(");
        AssertDoesNotContain(runnerText, "private static bool TryGetVerification(");

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
        var artifactsText = ReadRepoFile("tools/Common/DiagnosticSessionResultArtifacts.cs")
            .Replace("\r\n", "\n");

        AssertContains(artifactsText, "internal static class DiagnosticSessionResultArtifacts");
        AssertContains(artifactsText, "internal static async Task<DiagnosticSessionResultArtifactPaths> WritePreSummaryAsync(");
        AssertContains(artifactsText, "internal readonly record struct DiagnosticSessionResultArtifactPaths(");
        AssertContains(artifactsText, "SummaryPath: Path.Combine(outputDirectory, \"summary.json\")");
        AssertContains(artifactsText, "SamplesPath: Path.Combine(outputDirectory, \"samples.json\")");
        AssertContains(artifactsText, "FrameLedgerPath: Path.Combine(outputDirectory, \"frame-ledger.json\")");
        AssertContains(artifactsText, "TimelinePath: Path.Combine(outputDirectory, \"timeline.json\")");
        AssertContains(artifactsText, "private static object BuildFrameLedgerTrace(");
        AssertContains(artifactsText, "using static Sussudio.Tools.AutomationSnapshotFormatter;");
        AssertContains(artifactsText, "runState.WriteArtifactBestEffortAsync(\"write-samples\", paths.SamplesPath, samples)");
        AssertContains(artifactsText, "runState.WriteArtifactBestEffortAsync(\"write-frame-ledger\", paths.FrameLedgerPath, BuildFrameLedgerTrace(sessionId, samples))");
        AssertContains(artifactsText, "runState.WriteArtifactBestEffortAsync(\"write-timeline\", paths.TimelinePath, timeline)");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionResultArtifacts;");
        AssertDoesNotContain(artifactsText, "using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;");
        AssertContains(builderText, "WritePreSummaryAsync(");
        AssertDoesNotContain(builderText, "Path.Combine(request.OutputDirectory, \"samples.json\")");
        AssertDoesNotContain(builderText, "BuildFrameLedgerTrace(request.SessionId, samples)");

        return Task.CompletedTask;
    }
}
