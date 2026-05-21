using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticSessionModels_AreSplitFromRunnerBehavior()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var modelText = ReadDiagnosticSessionModelsSource();
        var resultText = ReadRepoFile("tools/Common/DiagnosticSessionResult.cs");
        var overviewResultText = ReadRepoFile("tools/Common/DiagnosticSessionResult.Overview.cs");
        var captureSourceResultText = ReadRepoFile("tools/Common/DiagnosticSessionResult.CaptureSource.cs");
        var previewCadenceResultText = ReadRepoFile("tools/Common/DiagnosticSessionResult.PreviewCadence.cs");
        var previewSchedulerResultText = ReadRepoFile("tools/Common/DiagnosticSessionResult.PreviewScheduler.cs");
        var previewD3DResultText = ReadRepoFile("tools/Common/DiagnosticSessionResult.PreviewD3D.cs");
        var previewVisualCadenceResultText = ReadRepoFile("tools/Common/DiagnosticSessionResult.PreviewVisualCadence.cs");
        var playbackCommandsResultText = ReadRepoFile("tools/Common/DiagnosticSessionResult.FlashbackPlayback.Commands.cs");
        var playbackCadenceResultText = ReadRepoFile("tools/Common/DiagnosticSessionResult.FlashbackPlayback.Cadence.cs");
        var playbackOnePercentLowResultText = ReadRepoFile("tools/Common/DiagnosticSessionResult.FlashbackPlayback.OnePercentLow.cs");
        var playbackDecodeResultText = ReadRepoFile("tools/Common/DiagnosticSessionResult.FlashbackPlayback.Decode.cs");
        var playbackAudioMasterResultText = ReadRepoFile("tools/Common/DiagnosticSessionResult.FlashbackPlayback.AudioMaster.cs");
        var playbackStageResultText = ReadRepoFile("tools/Common/DiagnosticSessionResult.FlashbackPlayback.Stage.cs");
        var flashbackRecordingResultText = ReadRepoFile("tools/Common/DiagnosticSessionResult.FlashbackRecording.cs");
        var flashbackExportResultText = ReadRepoFile("tools/Common/DiagnosticSessionResult.FlashbackExport.cs");

        AssertContains(modelText, "public sealed class DiagnosticSessionOptions");
        AssertContains(modelText, "public sealed partial class DiagnosticSessionResult");
        AssertContains(resultText, "public string SessionId { get; init; } = string.Empty;");
        AssertContains(resultText, "public string[] Warnings { get; set; } = Array.Empty<string>();");
        AssertContains(overviewResultText, "// End-of-run overview.");
        AssertContains(overviewResultText, "public double ProcessCpuPercentAtEnd { get; init; }");
        AssertContains(overviewResultText, "public PresentMonProbeResult? PresentMon { get; init; }");
        AssertContains(captureSourceResultText, "// Capture/source summary.");
        AssertContains(captureSourceResultText, "public string SelectedResolutionAtEnd { get; init; } = string.Empty;");
        AssertContains(captureSourceResultText, "public string SourceTelemetrySummaryAtEnd { get; init; } = string.Empty;");
        AssertContains(playbackCommandsResultText, "// Flashback playback command queue summary.");
        AssertContains(playbackCommandsResultText, "public int FlashbackPlaybackPendingCommandsAtEnd { get; init; }");
        AssertContains(playbackCadenceResultText, "// Flashback playback cadence and frame-delivery summary.");
        AssertContains(playbackCadenceResultText, "public double FlashbackPlaybackObservedFpsAtEnd { get; init; }");
        AssertContains(playbackOnePercentLowResultText, "// Flashback playback 1% low sample-window summary.");
        AssertContains(playbackOnePercentLowResultText, "public double FlashbackPlaybackOnePercentLowFpsAtEnd { get; init; }");
        AssertContains(playbackDecodeResultText, "// Flashback playback decode timing summary.");
        AssertContains(playbackDecodeResultText, "public double FlashbackPlaybackDecodeP99MsAtEnd { get; init; }");
        AssertContains(playbackAudioMasterResultText, "// Flashback playback audio-master summary.");
        AssertContains(playbackAudioMasterResultText, "public long FlashbackPlaybackAudioMasterFallbacksAtEnd { get; init; }");
        AssertContains(playbackStageResultText, "// Flashback playback stage and seek summary.");
        AssertContains(playbackStageResultText, "public long FlashbackPlaybackSubmitFailuresAtEnd { get; init; }");
        AssertContains(playbackStageResultText, "public bool FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd { get; init; }");
        AssertContains(flashbackRecordingResultText, "// Flashback recording summary.");
        AssertContains(flashbackRecordingResultText, "public bool FlashbackRecordingBackendObserved { get; init; }");
        AssertContains(flashbackExportResultText, "// Flashback export summary.");
        AssertContains(flashbackExportResultText, "public string FlashbackExportStatusAtEnd { get; init; } = string.Empty;");
        AssertContains(previewCadenceResultText, "// Preview cadence summary.");
        AssertContains(previewCadenceResultText, "public double PreviewCadenceOnePercentLowFpsAtEnd { get; init; }");
        AssertContains(previewSchedulerResultText, "// Preview scheduler and jitter-buffer summary.");
        AssertContains(previewSchedulerResultText, "public long PreviewSchedulerDroppedAtEnd { get; init; }");
        AssertContains(previewD3DResultText, "// Preview D3D frame-stat and CPU timing summary.");
        AssertContains(previewD3DResultText, "public double PreviewD3DInputUploadCpuP99MsAtEnd { get; init; }");
        AssertContains(previewVisualCadenceResultText, "// Preview visual-cadence summary.");
        AssertContains(previewVisualCadenceResultText, "public double VisualCadenceOutputFpsAtEnd { get; init; }");
        AssertDoesNotContain(resultText, "public double ProcessCpuPercentAtEnd");
        AssertDoesNotContain(resultText, "public PresentMonProbeResult? PresentMon");
        AssertDoesNotContain(resultText, "public string SelectedResolutionAtEnd");
        AssertDoesNotContain(resultText, "public string SourceTelemetrySummaryAtEnd");
        AssertDoesNotContain(resultText, "public int FlashbackPlaybackPendingCommandsAtEnd");
        AssertDoesNotContain(resultText, "public bool FlashbackRecordingBackendObserved");
        AssertDoesNotContain(resultText, "public string FlashbackExportStatusAtEnd");
        AssertDoesNotContain(resultText, "public long PreviewSchedulerDroppedAtEnd");
        AssertDoesNotContain(resultText, "public double VisualCadenceOutputFpsAtEnd");
        AssertDoesNotContain(flashbackRecordingResultText, "FlashbackPlayback");
        AssertDoesNotContain(flashbackRecordingResultText, "PreviewScheduler");
        AssertDoesNotContain(flashbackRecordingResultText, "FlashbackExportStatusAtEnd");
        AssertDoesNotContain(flashbackExportResultText, "FlashbackPlayback");
        AssertDoesNotContain(flashbackExportResultText, "PreviewScheduler");
        AssertDoesNotContain(flashbackExportResultText, "FlashbackRecordingBackendObserved");
        AssertDoesNotContain(captureSourceResultText, "FlashbackPlayback");
        AssertDoesNotContain(captureSourceResultText, "PreviewScheduler");
        AssertDoesNotContain(overviewResultText, "FlashbackPlayback");
        AssertDoesNotContain(overviewResultText, "PreviewScheduler");
        AssertDoesNotContain(playbackCommandsResultText, "PreviewScheduler");
        AssertDoesNotContain(playbackCadenceResultText, "PreviewScheduler");
        AssertDoesNotContain(playbackOnePercentLowResultText, "PreviewScheduler");
        AssertDoesNotContain(playbackDecodeResultText, "PreviewScheduler");
        AssertDoesNotContain(playbackAudioMasterResultText, "PreviewScheduler");
        AssertDoesNotContain(playbackStageResultText, "PreviewScheduler");
        AssertDoesNotContain(playbackCommandsResultText, "FlashbackPlaybackDecodeP99MsAtEnd");
        AssertDoesNotContain(playbackCadenceResultText, "FlashbackPlaybackPendingCommandsAtEnd");
        AssertDoesNotContain(playbackCadenceResultText, "FlashbackPlaybackOnePercentLowFpsAtEnd");
        AssertDoesNotContain(playbackOnePercentLowResultText, "FlashbackPlaybackPendingCommandsAtEnd");
        AssertDoesNotContain(playbackOnePercentLowResultText, "FlashbackPlaybackDecodeP99MsAtEnd");
        AssertDoesNotContain(playbackDecodeResultText, "FlashbackPlaybackAudioMasterFallbacksAtEnd");
        AssertDoesNotContain(playbackAudioMasterResultText, "FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd");
        AssertDoesNotContain(playbackStageResultText, "FlashbackPlaybackAudioMasterFallbacksAtEnd");
        AssertDoesNotContain(previewCadenceResultText, "FlashbackPlayback");
        AssertDoesNotContain(previewSchedulerResultText, "FlashbackPlayback");
        AssertDoesNotContain(previewD3DResultText, "FlashbackPlayback");
        AssertDoesNotContain(previewVisualCadenceResultText, "FlashbackPlayback");
        AssertDoesNotContain(previewCadenceResultText, "PreviewScheduler");
        AssertDoesNotContain(previewSchedulerResultText, "PreviewD3DInputUploadCpuP99MsAtEnd");
        AssertDoesNotContain(previewD3DResultText, "VisualCadenceOutputFpsAtEnd");
        AssertDoesNotContain(previewVisualCadenceResultText, "PreviewD3DInputUploadCpuP99MsAtEnd");
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
