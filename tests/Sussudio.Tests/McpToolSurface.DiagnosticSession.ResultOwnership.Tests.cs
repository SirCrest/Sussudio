using System.Threading.Tasks;

static partial class Program
{
    private static Task DiagnosticSessionModels_AreSplitFromRunnerBehavior()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var modelText = ReadDiagnosticSessionModelsSource();

        AssertContains(modelText, "public sealed class DiagnosticSessionOptions");
        AssertContains(modelText, "public sealed partial class DiagnosticSessionResult");
        AssertContains(ReadRepoFile("tools/Common/DiagnosticSessionResult.cs"), "public string SessionId { get; init; } = string.Empty;");
        AssertContains(ReadRepoFile("tools/Common/DiagnosticSessionResult.cs"), "public string[] Warnings { get; set; } = Array.Empty<string>();");
        AssertContains(ReadRepoFile("tools/Common/DiagnosticSessionResult.Capture.cs"), "public string SelectedResolutionAtEnd { get; init; } = string.Empty;");
        AssertContains(ReadRepoFile("tools/Common/DiagnosticSessionResult.Capture.cs"), "public string SourceTelemetrySummaryAtEnd { get; init; } = string.Empty;");
        AssertContains(ReadRepoFile("tools/Common/DiagnosticSessionResult.FlashbackPlayback.cs"), "public int FlashbackPlaybackPendingCommandsAtEnd { get; init; }");
        AssertContains(ReadRepoFile("tools/Common/DiagnosticSessionResult.FlashbackPlayback.cs"), "public bool FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd { get; init; }");
        AssertContains(ReadRepoFile("tools/Common/DiagnosticSessionResult.FlashbackRecording.cs"), "public bool FlashbackRecordingBackendObserved { get; init; }");
        AssertContains(ReadRepoFile("tools/Common/DiagnosticSessionResult.FlashbackExport.cs"), "public string FlashbackExportStatusAtEnd { get; init; } = string.Empty;");
        AssertContains(ReadRepoFile("tools/Common/DiagnosticSessionResult.Preview.cs"), "public long PreviewSchedulerDroppedAtEnd { get; init; }");
        AssertContains(ReadRepoFile("tools/Common/DiagnosticSessionResult.Preview.cs"), "public double VisualCadenceOutputFpsAtEnd { get; init; }");
        AssertContains(ReadRepoFile("tools/Common/DiagnosticSessionResult.Overview.cs"), "public PresentMonProbeResult? PresentMon { get; init; }");
        AssertDoesNotContain(ReadRepoFile("tools/Common/DiagnosticSessionResult.cs"), "public string SelectedResolutionAtEnd");
        AssertDoesNotContain(ReadRepoFile("tools/Common/DiagnosticSessionResult.Capture.cs"), "FlashbackPlayback");
        AssertDoesNotContain(ReadRepoFile("tools/Common/DiagnosticSessionResult.FlashbackPlayback.cs"), "PreviewScheduler");
        AssertDoesNotContain(ReadRepoFile("tools/Common/DiagnosticSessionResult.Preview.cs"), "FlashbackExport");
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


}
