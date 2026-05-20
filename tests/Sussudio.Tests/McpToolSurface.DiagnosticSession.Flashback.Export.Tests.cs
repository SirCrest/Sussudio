using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticSessionFlashbackExportScenarios_OwnExportFlows()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var scenariosText = ReadDiagnosticSessionFlashbackExportScenariosSource();
        var playbackText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExportScenarios.Playback.cs")
            .Replace("\r\n", "\n");
        var playbackValidationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExportScenarios.PlaybackValidation.cs")
            .Replace("\r\n", "\n");
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
        AssertContains(playbackText, "CaptureFlashbackExportPlaybackFrameCountBeforeExportAsync(");
        AssertContains(playbackText, "ValidateFlashbackExportPlaybackAfterExportAsync(");
        AssertContains(playbackText, "ValidateFlashbackExportPlaybackFinalStateAsync(");
        AssertDoesNotContain(playbackText, "playback frame count did not advance during export");
        AssertDoesNotContain(playbackText, "BuildPlaybackCommandHealth(finalSnapshot, baselineSnapshot)");
        AssertContains(playbackValidationText, "private static async Task<long> CaptureFlashbackExportPlaybackFrameCountBeforeExportAsync(");
        AssertContains(playbackValidationText, "flashback export playback: expected Playing before export");
        AssertContains(playbackValidationText, "private static async Task ValidateFlashbackExportPlaybackAfterExportAsync(");
        AssertContains(playbackValidationText, "flashback export playback: playback frame count did not advance during export");
        AssertContains(playbackValidationText, "private static async Task ValidateFlashbackExportPlaybackFinalStateAsync(");
        AssertContains(playbackValidationText, "BuildPlaybackCommandHealth(finalSnapshot, baselineSnapshot)");
        AssertContains(scenariosText, "internal static async Task RunFlashbackRangeExportAsync(");
        AssertContains(scenariosText, "private static async Task<FlashbackSelectionRange?> PrepareFlashbackSelectionRangeAsync(");
        AssertContains(scenariosText, "private static async Task MarkFlashbackSelectionPointAsync(");
        AssertContains(scenariosText, "private readonly record struct FlashbackSelectionRange(");
        AssertContains(scenariosText, "WaitForFlashbackStressBufferReadyAsync(");
        AssertContains(scenariosText, "\"clear-in-out-points\"");
        AssertContains(scenariosText, "\"set-in-point\"");
        AssertContains(scenariosText, "\"set-out-point\"");
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

    internal static Task DiagnosticSessionFlashbackExports_OwnsExportHelpers()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var exportScenariosText = ReadDiagnosticSessionFlashbackExportScenariosSource();
        var stressText = ReadDiagnosticSessionFlashbackStressScenarioSource();
        var exportHelpersText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExports.cs")
            .Replace("\r\n", "\n");
        var audioSwitchText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExports.AudioSwitch.cs")
            .Replace("\r\n", "\n");

        AssertContains(exportHelpersText, "internal static partial class DiagnosticSessionFlashbackExports");
        AssertContains(exportHelpersText, "internal static int? TryParseFlashbackExportSegmentCount(");
        AssertContains(exportHelpersText, "const string marker = \" from \";");
        AssertContains(exportHelpersText, "suffix.Contains(\"segment\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(exportHelpersText, "internal static Dictionary<string, object?> CreateFlashbackExportVerifyPayload(string filePath)");
        AssertContains(exportHelpersText, "[\"verificationProfile\"] = \"flashback-export\"");
        AssertContains(exportHelpersText, "internal static async Task CleanupFlashbackSelectionAsync(");
        AssertContains(exportHelpersText, "\"clear-in-out-points\"");
        AssertContains(exportHelpersText, "\"go-live\"");
        AssertContains(audioSwitchText, "internal static async Task ToggleAudioEnabledDuringFlashbackExportAsync(");
        AssertContains(audioSwitchText, "\"SetAudioEnabled\"");
        AssertContains(exportScenariosText, "using static Sussudio.Tools.DiagnosticSessionFlashbackExports;");
        AssertContains(stressText, "using static Sussudio.Tools.DiagnosticSessionFlashbackExports;");
        AssertDoesNotContain(runnerText, "private static int? TryParseFlashbackExportSegmentCount(");
        AssertDoesNotContain(runnerText, "private static Dictionary<string, object?> CreateFlashbackExportVerifyPayload(");
        AssertDoesNotContain(runnerText, "private static async Task ToggleAudioEnabledDuringFlashbackExportAsync(");
        AssertDoesNotContain(runnerText, "private static async Task CleanupFlashbackSelectionAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackSegments_OwnsSegmentWaitsAndParsing()
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
