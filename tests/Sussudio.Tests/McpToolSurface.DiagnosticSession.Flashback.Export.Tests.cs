using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticSessionFlashbackExportScenarios_OwnExportFlows()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var scenariosText = ReadDiagnosticSessionFlashbackExportScenariosSource();
        var registrationsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExportScenarios.Registrations.cs")
            .Replace("\r\n", "\n");
        var playbackRegistrationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExportScenarios.Registrations.Playback.cs")
            .Replace("\r\n", "\n");
        var rangeRegistrationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExportScenarios.Registrations.Range.cs")
            .Replace("\r\n", "\n");
        var coordinationRegistrationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExportScenarios.Registrations.Coordination.cs")
            .Replace("\r\n", "\n");
        var disableDuringExportText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExportScenarios.DisableDuringExport.cs")
            .Replace("\r\n", "\n");
        var disableDuringExportValidationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExportScenarios.DisableDuringExportValidation.cs")
            .Replace("\r\n", "\n");
        var playbackText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExportScenarios.Playback.cs")
            .Replace("\r\n", "\n");
        var playbackPreExportText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExportScenarios.PlaybackPreExport.cs")
            .Replace("\r\n", "\n");
        var playbackPostExportText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExportScenarios.PlaybackPostExport.cs")
            .Replace("\r\n", "\n");
        var playbackFinalStateText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExportScenarios.PlaybackFinalState.cs")
            .Replace("\r\n", "\n");
        var rangeSelectionText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExportScenarios.RangeSelection.cs")
            .Replace("\r\n", "\n");
        var rangeSelectionMarkersText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExportScenarios.RangeSelection.Markers.cs")
            .Replace("\r\n", "\n");
        var rangeSelectionModelsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExportScenarios.RangeSelection.Models.cs")
            .Replace("\r\n", "\n");
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
        AssertDoesNotContain(disableDuringExportText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertDoesNotContain(disableDuringExportText, "playback worker still alive after disable");
        AssertContains(disableDuringExportValidationText, "private static async Task ValidateFlashbackDisableDuringExportFileAsync(");
        AssertContains(disableDuringExportValidationText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(disableDuringExportValidationText, "private static async Task ValidateFlashbackDisabledAfterExportAsync(");
        AssertContains(disableDuringExportValidationText, "flashback disable during export: pending playback commands remained after disable");
        AssertContains(disableDuringExportValidationText, "private static async Task ValidateFlashbackReenabledAfterDisableDuringExportAsync(");
        AssertContains(scenariosText, "internal static async Task RunFlashbackRotatedExportAsync(");
        AssertContains(scenariosText, "TryParseFlashbackExportSegmentCount(exportMessage)");
        AssertContains(scenariosText, "internal static async Task RunFlashbackExportPlaybackAsync(");
        AssertContains(scenariosText, "flashback export during playback verified");
        AssertContains(playbackText, "CaptureFlashbackExportPlaybackFrameCountBeforeExportAsync(");
        AssertContains(playbackText, "ValidateFlashbackExportPlaybackAfterExportAsync(");
        AssertContains(playbackText, "ValidateFlashbackExportPlaybackFinalStateAsync(");
        AssertDoesNotContain(playbackText, "playback frame count did not advance during export");
        AssertDoesNotContain(playbackText, "BuildPlaybackCommandHealth(finalSnapshot, baselineSnapshot)");
        AssertContains(playbackPreExportText, "private static async Task<long> CaptureFlashbackExportPlaybackFrameCountBeforeExportAsync(");
        AssertContains(playbackPreExportText, "flashback export playback: expected Playing before export");
        AssertDoesNotContain(playbackPreExportText, "playback frame count did not advance during export");
        AssertContains(playbackPostExportText, "private static async Task ValidateFlashbackExportPlaybackAfterExportAsync(");
        AssertContains(playbackPostExportText, "flashback export playback: playback frame count did not advance during export");
        AssertDoesNotContain(playbackPostExportText, "BuildPlaybackCommandHealth(finalSnapshot, baselineSnapshot)");
        AssertContains(playbackFinalStateText, "private static async Task ValidateFlashbackExportPlaybackFinalStateAsync(");
        AssertContains(playbackFinalStateText, "BuildPlaybackCommandHealth(finalSnapshot, baselineSnapshot)");
        AssertContains(playbackFinalStateText, "flashback export playback: pending commands remained after go-live");
        AssertContains(scenariosText, "internal static async Task RunFlashbackRangeExportAsync(");
        AssertContains(rangeSelectionText, "private static async Task<FlashbackSelectionRange?> PrepareFlashbackSelectionRangeAsync(");
        AssertContains(rangeSelectionText, "WaitForFlashbackStressBufferReadyAsync(");
        AssertContains(rangeSelectionMarkersText, "private static async Task MarkFlashbackSelectionPointAsync(");
        AssertContains(rangeSelectionMarkersText, "WaitForFlashbackPlaybackPositionAsync(");
        AssertContains(rangeSelectionModelsText, "private readonly record struct FlashbackSelectionRange(");
        AssertDoesNotContain(rangeSelectionText, "WaitForFlashbackPlaybackPositionAsync(");
        AssertContains(scenariosText, "\"clear-in-out-points\"");
        AssertContains(scenariosText, "\"set-in-point\"");
        AssertContains(scenariosText, "\"set-out-point\"");
        AssertContains(scenariosText, "[\"useSelectionRange\"] = true");
        AssertContains(scenariosText, "private static void ValidateFlashbackRangeExportResult(");
        AssertContains(scenariosText, "private static async Task ValidateFlashbackRangeExportCleanupAsync(");
        AssertContains(registrationsText, "internal static void RegisterSelectedFlashbackExportScenarioTasks(");
        AssertContains(registrationsText, "RegisterFlashbackExportPlaybackTask(");
        AssertContains(registrationsText, "RegisterFlashbackRangeExportTasks(");
        AssertContains(registrationsText, "RegisterFlashbackExportCoordinationTasks(");
        AssertDoesNotContain(registrationsText, "backgroundTasks.AddScenario(");
        AssertContains(playbackRegistrationText, "private static void RegisterFlashbackExportPlaybackTask(");
        AssertContains(playbackRegistrationText, "6,\n            \"flashback-export-playback-task\",");
        AssertContains(playbackRegistrationText, "flashback export playback started");
        AssertDoesNotContain(playbackRegistrationText, "flashback-range-export-task");
        AssertContains(rangeRegistrationText, "private static void RegisterFlashbackRangeExportTasks(");
        AssertContains(rangeRegistrationText, "8,\n                \"flashback-range-export-task\",");
        AssertContains(rangeRegistrationText, "9,\n                \"flashback-range-export-audio-switch-task\",");
        AssertContains(rangeRegistrationText, "flashback range export audio switch started");
        AssertDoesNotContain(rangeRegistrationText, "flashback-export-concurrent-task");
        AssertContains(coordinationRegistrationText, "private static void RegisterFlashbackExportCoordinationTasks(");
        AssertContains(coordinationRegistrationText, "10,\n                \"flashback-export-concurrent-task\",");
        AssertContains(coordinationRegistrationText, "11,\n                \"flashback-disable-during-export-task\",");
        AssertContains(coordinationRegistrationText, "12,\n                \"flashback-rotated-export-task\",");
        AssertContains(coordinationRegistrationText, "flashback rotated export started");
        AssertDoesNotContain(coordinationRegistrationText, "flashback-range-export-task");
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
        var completedWaitsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSegments.CompletedWaits.cs")
            .Replace("\r\n", "\n");
        var playbackTargetWaitsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSegments.PlaybackTargetWaits.cs")
            .Replace("\r\n", "\n");
        var playbackHeadroomWaitsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSegments.PlaybackHeadroomWaits.cs")
            .Replace("\r\n", "\n");
        var segmentModelsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSegments.Models.cs")
            .Replace("\r\n", "\n");
        var segmentParsingText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSegments.Parsing.cs")
            .Replace("\r\n", "\n");
        var segmentsText = ReadDiagnosticSessionFlashbackSegmentsSource();
        var segmentWaitsText = completedWaitsText + "\n" + playbackTargetWaitsText + "\n" + playbackHeadroomWaitsText;

        AssertContains(segmentsText, "internal static partial class DiagnosticSessionFlashbackSegments");
        AssertContains(segmentModelsText, "internal readonly record struct FlashbackSegmentProbe(");
        AssertContains(segmentModelsText, "internal readonly record struct FlashbackSegmentPlaybackTarget(");
        AssertContains(completedWaitsText, "internal static async Task<FlashbackSegmentProbe?> WaitForFlashbackCompletedSegmentAsync(");
        AssertContains(completedWaitsText, "\"FlashbackGetSegments\"");
        AssertContains(segmentParsingText, "internal static bool TryGetFlashbackSegments(");
        AssertContains(playbackTargetWaitsText, "internal static async Task<FlashbackSegmentPlaybackTarget?> WaitForFlashbackPlayableCompletedSegmentAsync(");
        AssertContains(playbackTargetWaitsText, "const int requiredHeadroomMs = 8_000;");
        AssertContains(playbackHeadroomWaitsText, "internal static async Task<bool> WaitForFlashbackSegmentPlaybackHeadroomAsync(");
        AssertContains(playbackHeadroomWaitsText, "const int requiredHeadroomMs = 8_000;");
        AssertContains(segmentParsingText, "data.TryGetProperty(\"Segments\", out var segmentsElement)");
        AssertDoesNotContain(segmentWaitsText, "internal readonly record struct FlashbackSegmentProbe(");
        AssertDoesNotContain(segmentWaitsText, "segments.Add(new FlashbackSegmentProbe(");
        AssertDoesNotContain(completedWaitsText, "GetSnapshot");
        AssertDoesNotContain(playbackHeadroomWaitsText, "FlashbackGetSegments");
        AssertDoesNotContain(segmentModelsText, "TryGetFlashbackSegments(");
        AssertDoesNotContain(segmentParsingText, "WaitForFlashbackPlayableCompletedSegmentAsync(");
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
