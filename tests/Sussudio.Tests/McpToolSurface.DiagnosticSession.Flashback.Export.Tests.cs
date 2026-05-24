using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticSessionFlashbackExportScenarios_OwnExportFlows()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var scenariosText = ReadDiagnosticSessionFlashbackExportScenariosSource();
        var rootText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExportScenarios.cs")
            .Replace("\r\n", "\n");
        var disableDuringExportText = rootText;
        var playbackText = rootText;
        var rangeText = rootText;
        var scenariosTextWithoutSpaces = scenariosText.Replace(" ", string.Empty);

        AssertContains(scenariosText, "internal static partial class DiagnosticSessionFlashbackExportScenarios");
        AssertContains(scenariosText, "internal static async Task RunFlashbackExportConcurrentAsync(");
        AssertContains(scenariosText, "\"flashback-concurrent-a.mp4\"");
        AssertContains(scenariosText, "flashback concurrent exports verified");
        AssertContains(scenariosText, "internal static async Task RunFlashbackDisableDuringExportAsync(");
        AssertContains(scenariosText, "\"flashback-disable-during-export.mp4\"");
        AssertContains(scenariosText, "SendCommandWithConnectRetryAsync(");
        AssertContains(disableDuringExportText, "ValidateFlashbackDisableDuringExportFileAsync(");
        AssertContains(disableDuringExportText, "ValidateFlashbackDisabledAfterExportAsync(");
        AssertContains(disableDuringExportText, "ValidateFlashbackReenabledAfterDisableDuringExportAsync(");
        AssertContains(disableDuringExportText, "private static async Task ValidateFlashbackDisableDuringExportFileAsync(");
        AssertContains(disableDuringExportText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(disableDuringExportText, "private static async Task ValidateFlashbackDisabledAfterExportAsync(");
        AssertContains(disableDuringExportText, "flashback disable during export: pending playback commands remained after disable");
        AssertContains(disableDuringExportText, "private static async Task ValidateFlashbackReenabledAfterDisableDuringExportAsync(");
        AssertContains(scenariosText, "internal static async Task RunFlashbackRotatedExportAsync(");
        AssertContains(scenariosText, "TryParseFlashbackExportSegmentCount(exportMessage)");
        AssertContains(scenariosText, "internal static async Task RunFlashbackExportPlaybackAsync(");
        AssertContains(scenariosText, "flashback export during playback verified");
        AssertContains(playbackText, "CaptureFlashbackExportPlaybackFrameCountBeforeExportAsync(");
        AssertContains(playbackText, "ValidateFlashbackExportPlaybackAfterExportAsync(");
        AssertContains(playbackText, "ValidateFlashbackExportPlaybackFinalStateAsync(");
        AssertContains(playbackText, "private static async Task<long> CaptureFlashbackExportPlaybackFrameCountBeforeExportAsync(");
        AssertContains(playbackText, "flashback export playback: expected Playing before export");
        AssertContains(playbackText, "private static async Task ValidateFlashbackExportPlaybackAfterExportAsync(");
        AssertContains(playbackText, "flashback export playback: playback frame count did not advance during export");
        AssertContains(playbackText, "private static async Task ValidateFlashbackExportPlaybackFinalStateAsync(");
        AssertContains(playbackText, "BuildPlaybackCommandHealth(finalSnapshot, baselineSnapshot)");
        AssertContains(playbackText, "flashback export playback: pending commands remained after go-live");
        AssertContains(scenariosText, "internal static async Task RunFlashbackRangeExportAsync(");
        AssertContains(rangeText, "private static async Task<FlashbackSelectionRange?> PrepareFlashbackSelectionRangeAsync(");
        AssertContains(rangeText, "private readonly record struct FlashbackSelectionRange(");
        AssertContains(rangeText, "WaitForFlashbackStressBufferReadyAsync(");
        AssertContains(rangeText, "private static async Task MarkFlashbackSelectionPointAsync(");
        AssertContains(rangeText, "WaitForFlashbackPlaybackPositionAsync(");
        AssertContains(scenariosText, "\"clear-in-out-points\"");
        AssertContains(scenariosText, "\"set-in-point\"");
        AssertContains(scenariosText, "\"set-out-point\"");
        AssertContains(scenariosText, "[\"useSelectionRange\"] = true");
        AssertContains(scenariosText, "private static void ValidateFlashbackRangeExportResult(");
        AssertContains(scenariosText, "private static async Task ValidateFlashbackRangeExportCleanupAsync(");
        AssertContains(rootText, "internal static void RegisterSelectedFlashbackExportScenarioTasks(");
        AssertContains(rootText, "RegisterFlashbackExportPlaybackTask(");
        AssertContains(rootText, "RegisterFlashbackRangeExportTasks(");
        AssertContains(rootText, "RegisterFlashbackExportCoordinationTasks(");
        AssertContains(rootText, "backgroundTasks.AddScenario(");
        AssertContains(rootText, "private static void RegisterFlashbackExportPlaybackTask(");
        AssertContains(rootText, "6,\n            \"flashback-export-playback-task\",");
        AssertContains(rootText, "flashback export playback started");
        AssertContains(rootText, "private static void RegisterFlashbackRangeExportTasks(");
        AssertContains(rootText, "8,\n                \"flashback-range-export-task\",");
        AssertContains(rootText, "9,\n                \"flashback-range-export-audio-switch-task\",");
        AssertContains(rootText, "flashback range export audio switch started");
        AssertContains(rootText, "private static void RegisterFlashbackExportCoordinationTasks(");
        AssertContains(rootText, "10,\n                \"flashback-export-concurrent-task\",");
        AssertContains(rootText, "11,\n                \"flashback-disable-during-export-task\",");
        AssertContains(rootText, "12,\n                \"flashback-rotated-export-task\",");
        AssertContains(rootText, "flashback rotated export started");
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

        AssertContains(exportHelpersText, "internal static class DiagnosticSessionFlashbackExports");
        AssertDoesNotContain(exportHelpersText, "internal static partial class DiagnosticSessionFlashbackExports");
        AssertContains(exportHelpersText, "internal static int? TryParseFlashbackExportSegmentCount(");
        AssertContains(exportHelpersText, "const string marker = \" from \";");
        AssertContains(exportHelpersText, "suffix.Contains(\"segment\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(exportHelpersText, "internal static Dictionary<string, object?> CreateFlashbackExportVerifyPayload(string filePath)");
        AssertContains(exportHelpersText, "[\"verificationProfile\"] = \"flashback-export\"");
        AssertContains(exportHelpersText, "internal static async Task CleanupFlashbackSelectionAsync(");
        AssertContains(exportHelpersText, "\"clear-in-out-points\"");
        AssertContains(exportHelpersText, "\"go-live\"");
        AssertContains(exportHelpersText, "internal static async Task ToggleAudioEnabledDuringFlashbackExportAsync(");
        AssertContains(exportHelpersText, "\"SetAudioEnabled\"");
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
        var segmentsText = ReadDiagnosticSessionFlashbackSegmentsSource();

        AssertContains(segmentsText, "internal static class DiagnosticSessionFlashbackSegments");
        AssertDoesNotContain(segmentsText, "internal static partial class DiagnosticSessionFlashbackSegments");
        AssertContains(segmentsText, "internal readonly record struct FlashbackSegmentProbe(");
        AssertContains(segmentsText, "internal readonly record struct FlashbackSegmentPlaybackTarget(");
        AssertContains(segmentsText, "internal static async Task<FlashbackSegmentProbe?> WaitForFlashbackCompletedSegmentAsync(");
        AssertContains(segmentsText, "\"FlashbackGetSegments\"");
        AssertContains(segmentsText, "internal static bool TryGetFlashbackSegments(");
        AssertContains(segmentsText, "internal static async Task<FlashbackSegmentPlaybackTarget?> WaitForFlashbackPlayableCompletedSegmentAsync(");
        AssertContains(segmentsText, "const int requiredHeadroomMs = 8_000;");
        AssertContains(segmentsText, "internal static async Task<bool> WaitForFlashbackSegmentPlaybackHeadroomAsync(");
        AssertContains(segmentsText, "data.TryGetProperty(\"Segments\", out var segmentsElement)");
        AssertContains(segmentsText, "sendCommandAsync(\"FlashbackGetSegments\", null, null)");
        AssertContains(segmentsText, "sendCommandAsync(\"GetSnapshot\", null, null)");
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
