using System.Threading.Tasks;

static partial class Program
{
    private static Task DiagnosticSessionFlashbackExportScenarios_OwnExportFlows()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var scenariosText = ReadDiagnosticSessionFlashbackExportScenariosSource();
        var scenariosTextWithoutSpaces = scenariosText.Replace(" ", string.Empty);

        AssertContains(scenariosText, "internal static partial class DiagnosticSessionFlashbackExportScenarios");
        AssertContains(scenariosText, "internal static async Task RunFlashbackExportConcurrentAsync(");
        AssertContains(scenariosText, "\"flashback-concurrent-a.mp4\"");
        AssertContains(scenariosText, "flashback concurrent exports verified");
        AssertContains(scenariosText, "internal static async Task RunFlashbackDisableDuringExportAsync(");
        AssertContains(scenariosText, "\"flashback-disable-during-export.mp4\"");
        AssertContains(scenariosText, "SendCommandWithConnectRetryAsync(");
        AssertContains(scenariosText, "internal static async Task RunFlashbackRotatedExportAsync(");
        AssertContains(scenariosText, "TryParseFlashbackExportSegmentCount(exportMessage)");
        AssertContains(scenariosText, "internal static async Task RunFlashbackExportPlaybackAsync(");
        AssertContains(scenariosText, "flashback export during playback verified");
        AssertContains(scenariosText, "internal static async Task RunFlashbackRangeExportAsync(");
        AssertContains(scenariosText, "[\"useSelectionRange\"] = true");
        AssertContains(scenariosText, "private static void ValidateFlashbackRangeExportResult(");
        AssertContains(scenariosText, "private static async Task ValidateFlashbackRangeExportCleanupAsync(");
        AssertContains(scenariosText, "internal static void RegisterSelectedFlashbackExportScenarioTasks(");
        AssertContains(scenariosText, "backgroundTasks.AddScenario(");
        AssertContains(scenariosTextWithoutSpaces, "6,\n\"flashback-export-playback-task\",");
        AssertContains(scenariosTextWithoutSpaces, "8,\n\"flashback-range-export-task\",");
        AssertContains(scenariosTextWithoutSpaces, "9,\n\"flashback-range-export-audio-switch-task\",");
        AssertContains(scenariosTextWithoutSpaces, "10,\n\"flashback-export-concurrent-task\",");
        AssertContains(scenariosTextWithoutSpaces, "11,\n\"flashback-disable-during-export-task\",");
        AssertContains(scenariosTextWithoutSpaces, "12,\n\"flashback-rotated-export-task\",");
        AssertContains(scenariosText, "\"flashback-range-export-task\"");
        AssertContains(scenariosText, "actions.Add(\"flashback concurrent export started\")");
        AssertContains(startupText, "DiagnosticSessionFlashbackExportScenarios.RegisterSelectedFlashbackExportScenarioTasks(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackExportScenarios;");
        AssertDoesNotContain(startupText, "RunFlashbackExportConcurrentAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackDisableDuringExportAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackRotatedExportAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackExportPlaybackAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackRangeExportAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackExportConcurrentAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackDisableDuringExportAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRotatedExportAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackExportPlaybackAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRangeExportAsync(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionFlashbackExports_OwnsExportHelpers()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var exportScenariosText = ReadDiagnosticSessionFlashbackExportScenariosSource();
        var stressText = ReadDiagnosticSessionFlashbackStressScenarioSource();
        var segmentCountText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExports.SegmentCount.cs")
            .Replace("\r\n", "\n");
        var verifyPayloadText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExports.VerifyPayload.cs")
            .Replace("\r\n", "\n");
        var audioSwitchText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExports.AudioSwitch.cs")
            .Replace("\r\n", "\n");
        var selectionCleanupText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExports.SelectionCleanup.cs")
            .Replace("\r\n", "\n");

        AssertContains(segmentCountText, "internal static partial class DiagnosticSessionFlashbackExports");
        AssertContains(segmentCountText, "internal static int? TryParseFlashbackExportSegmentCount(");
        AssertContains(segmentCountText, "const string marker = \" from \";");
        AssertContains(segmentCountText, "suffix.Contains(\"segment\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(verifyPayloadText, "internal static Dictionary<string, object?> CreateFlashbackExportVerifyPayload(string filePath)");
        AssertContains(verifyPayloadText, "[\"verificationProfile\"] = \"flashback-export\"");
        AssertContains(audioSwitchText, "internal static async Task ToggleAudioEnabledDuringFlashbackExportAsync(");
        AssertContains(audioSwitchText, "\"SetAudioEnabled\"");
        AssertContains(selectionCleanupText, "internal static async Task CleanupFlashbackSelectionAsync(");
        AssertContains(selectionCleanupText, "\"clear-in-out-points\"");
        AssertContains(selectionCleanupText, "\"go-live\"");
        AssertContains(exportScenariosText, "using static Sussudio.Tools.DiagnosticSessionFlashbackExports;");
        AssertContains(stressText, "using static Sussudio.Tools.DiagnosticSessionFlashbackExports;");
        AssertDoesNotContain(runnerText, "private static int? TryParseFlashbackExportSegmentCount(");
        AssertDoesNotContain(runnerText, "private static Dictionary<string, object?> CreateFlashbackExportVerifyPayload(");
        AssertDoesNotContain(runnerText, "private static async Task ToggleAudioEnabledDuringFlashbackExportAsync(");
        AssertDoesNotContain(runnerText, "private static async Task CleanupFlashbackSelectionAsync(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionFlashbackSegments_OwnsSegmentWaitsAndParsing()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var exportScenariosText = ReadDiagnosticSessionFlashbackExportScenariosSource();
        var segmentPlaybackText = ReadDiagnosticSessionFlashbackSegmentPlaybackScenariosSource();
        var segmentsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSegments.cs")
            .Replace("\r\n", "\n");

        AssertContains(segmentsText, "internal static class DiagnosticSessionFlashbackSegments");
        AssertContains(segmentsText, "internal readonly record struct FlashbackSegmentProbe(");
        AssertContains(segmentsText, "internal readonly record struct FlashbackSegmentPlaybackTarget(");
        AssertContains(segmentsText, "internal static async Task<FlashbackSegmentProbe?> WaitForFlashbackCompletedSegmentAsync(");
        AssertContains(segmentsText, "internal static bool TryGetFlashbackSegments(");
        AssertContains(segmentsText, "internal static async Task<FlashbackSegmentPlaybackTarget?> WaitForFlashbackPlayableCompletedSegmentAsync(");
        AssertContains(segmentsText, "internal static async Task<bool> WaitForFlashbackSegmentPlaybackHeadroomAsync(");
        AssertContains(segmentsText, "\"FlashbackGetSegments\"");
        AssertContains(segmentsText, "data.TryGetProperty(\"Segments\", out var segmentsElement)");
        AssertContains(segmentsText, "const int requiredHeadroomMs = 8_000;");
        AssertContains(exportScenariosText, "using static Sussudio.Tools.DiagnosticSessionFlashbackSegments;");
        AssertContains(segmentPlaybackText, "using static Sussudio.Tools.DiagnosticSessionFlashbackSegments;");
        AssertDoesNotContain(runnerText, "private readonly record struct FlashbackSegmentProbe(");
        AssertDoesNotContain(runnerText, "private readonly record struct FlashbackSegmentPlaybackTarget(");
        AssertDoesNotContain(runnerText, "private static async Task<FlashbackSegmentProbe?> WaitForFlashbackCompletedSegmentAsync(");
        AssertDoesNotContain(runnerText, "private static bool TryGetFlashbackSegments(");
        AssertDoesNotContain(runnerText, "private static async Task<FlashbackSegmentPlaybackTarget?> WaitForFlashbackPlayableCompletedSegmentAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<bool> WaitForFlashbackSegmentPlaybackHeadroomAsync(");

        return Task.CompletedTask;
    }
}
