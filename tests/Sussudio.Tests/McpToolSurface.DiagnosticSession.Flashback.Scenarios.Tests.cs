using System.Threading.Tasks;

static partial class Program
{
    private static Task DiagnosticSessionFlashbackCycleScenarios_OwnCycleFlows()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var cyclesText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackCycleScenarios.cs")
            .Replace("\r\n", "\n");

        AssertContains(cyclesText, "internal static class DiagnosticSessionFlashbackCycleScenarios");
        AssertContains(cyclesText, "internal static async Task RunFlashbackRestartCycleAsync(");
        AssertContains(cyclesText, "\"RestartFlashback\"");
        AssertContains(cyclesText, "\"flashback-restart-cycle-export.mp4\"");
        AssertContains(cyclesText, "flashback restart cycle export verified");
        AssertContains(cyclesText, "internal static async Task RunFlashbackEncoderCycleAsync(");
        AssertContains(cyclesText, "var cycledPreset = string.Equals(originalPreset, \"P1\", StringComparison.OrdinalIgnoreCase) ? \"P2\" : \"P1\";");
        AssertContains(cyclesText, "\"flashback-encoder-cycle-export.mp4\"");
        AssertContains(cyclesText, "flashback encoder preset restored to");
        AssertContains(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackCycleScenarios;");
        AssertContains(startupText, "RunFlashbackRestartCycleAsync(");
        AssertContains(startupText, "RunFlashbackEncoderCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRestartCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackEncoderCycleAsync(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionFlashbackPreviewCycleScenarios_OwnPreviewCycleFlows()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var cyclesText = ReadDiagnosticSessionFlashbackPreviewCycleScenariosSource();

        AssertContains(cyclesText, "internal static partial class DiagnosticSessionFlashbackPreviewCycleScenarios");
        AssertContains(cyclesText, "internal static async Task RunFlashbackPreviewCycleAsync(");
        AssertContains(cyclesText, "\"flashback-preview-off-export.mp4\"");
        AssertContains(cyclesText, "flashback preview cycle export verified");
        AssertContains(cyclesText, "internal static async Task RunFlashbackPlaybackPreviewCycleAsync(");
        AssertContains(cyclesText, "flashback playback preview cycle preview stopped during playback");
        AssertContains(cyclesText, "flashback playback preview cycle export verified");
        AssertContains(cyclesText, "internal static async Task RunFlashbackRecordingPreviewCycleAsync(");
        AssertContains(cyclesText, "flashback recording preview cycle preview stopped");
        AssertContains(cyclesText, "internal static bool IsPreviewCycleScenario(");
        AssertContains(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackPreviewCycleScenarios;");
        AssertContains(startupText, "RunFlashbackPreviewCycleAsync(");
        AssertContains(startupText, "RunFlashbackPlaybackPreviewCycleAsync(");
        AssertContains(startupText, "RunFlashbackRecordingPreviewCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackPreviewCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackPlaybackPreviewCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRecordingPreviewCycleAsync(");
        AssertDoesNotContain(runnerText, "private static bool IsPreviewCycleScenario(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionFlashbackRejectedExports_OwnRejectionFlows()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var rejectedText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackRejectedExports.cs")
            .Replace("\r\n", "\n");

        AssertContains(rejectedText, "internal static class DiagnosticSessionFlashbackRejectedExports");
        AssertContains(rejectedText, "internal static async Task RunSelectedRejectedExportScenariosAsync(");
        AssertContains(rejectedText, "internal static async Task RunFlashbackExportRejectedAsync(");
        AssertContains(rejectedText, "\"flashback-rejected-export.mp4\"");
        AssertContains(rejectedText, "BufferInactive");
        AssertContains(rejectedText, "Flashback buffer not active");
        AssertContains(rejectedText, "internal static async Task RunFlashbackRecordingExportRejectedAsync(");
        AssertContains(rejectedText, "\"flashback-recording-rejected-export.mp4\"");
        AssertContains(rejectedText, "UnavailableDuringRecording");
        AssertContains(rejectedText, "recording backend changed after rejected export");
        var dispatchText = ExtractMemberCode(rejectedText, "RunSelectedRejectedExportScenariosAsync");
        AssertContains(dispatchText, "scenarioPlan.RunFlashbackExportRejected");
        AssertContains(dispatchText, "scenarioPlan.RunFlashbackRecordingExportRejected");
        AssertOccursBefore(dispatchText, "RunFlashbackExportRejectedAsync(", "RunFlashbackRecordingExportRejectedAsync(");
        AssertContains(runnerText, "DiagnosticSessionFlashbackRejectedExports.RunSelectedRejectedExportScenariosAsync(");
        AssertDoesNotContain(runnerText, "using static Sussudio.Tools.DiagnosticSessionFlashbackRejectedExports;");
        AssertDoesNotContain(runnerText, "RunFlashbackExportRejectedAsync(");
        AssertDoesNotContain(runnerText, "RunFlashbackRecordingExportRejectedAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackExportRejectedAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRecordingExportRejectedAsync(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionFlashbackSegmentPlaybackScenarios_OwnSegmentPlaybackFlow()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var segmentPlaybackText = ReadDiagnosticSessionFlashbackSegmentPlaybackScenariosSource();

        AssertContains(segmentPlaybackText, "internal static partial class DiagnosticSessionFlashbackSegmentPlaybackScenarios");
        AssertContains(segmentPlaybackText, "internal static async Task RunFlashbackSegmentPlaybackAsync(");
        AssertContains(segmentPlaybackText, "flashback segment playback live headroom established");
        AssertContains(segmentPlaybackText, "flashback segment playback started near boundary");
        AssertContains(segmentPlaybackText, "frameCount >= 180");
        AssertContains(segmentPlaybackText, "playback FPS below source-rate target after warm sample");
        AssertContains(segmentPlaybackText, "private static async Task<bool> CreateFlashbackCompletedSegmentViaRecordingAsync(");
        AssertContains(segmentPlaybackText, "recording-assisted rotation started");
        AssertContains(segmentPlaybackText, "private static async Task TryStopRecordingAsync(");
        AssertContains(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackSegmentPlaybackScenarios;");
        AssertContains(startupText, "RunFlashbackSegmentPlaybackAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackSegmentPlaybackAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<bool> CreateFlashbackCompletedSegmentViaRecordingAsync(");
        AssertDoesNotContain(runnerText, "private static async Task TryStopRecordingAsync(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionFlashbackRecordingSettingsScenarios_OwnDeferredSettingsFlow()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var recordingChecksText = ReadRepoFile("tools/Common/DiagnosticSessionRecordingChecks.cs")
            .Replace("\r\n", "\n");
        var settingsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.cs")
            .Replace("\r\n", "\n");

        AssertContains(settingsText, "internal readonly record struct FlashbackRecordingSettingsDeferredPresetState(");
        AssertContains(settingsText, "internal static class DiagnosticSessionFlashbackRecordingSettingsScenarios");
        AssertContains(settingsText, "internal static async Task<FlashbackRecordingSettingsDeferredPresetState> RunFlashbackRecordingSettingsDeferredAsync(");
        AssertContains(settingsText, "flashback recording settings deferred preset changed to");
        AssertContains(settingsText, "RestartFlashback unexpectedly succeeded during recording");
        AssertContains(settingsText, "SetFlashbackEnabled(false) unexpectedly succeeded during recording");
        AssertContains(settingsText, "internal static async Task VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
        AssertContains(settingsText, "flashback recording settings deferred post-stop buffer verified");
        AssertContains(settingsText, "flashback recording settings deferred preset restored to");
        AssertContains(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackRecordingSettingsScenarios;");
        AssertContains(startupText, "RunFlashbackRecordingSettingsDeferredAsync(");
        AssertContains(recordingChecksText, "using static Sussudio.Tools.DiagnosticSessionFlashbackRecordingSettingsScenarios;");
        AssertContains(recordingChecksText, "VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<FlashbackRecordingSettingsDeferredPresetState> RunFlashbackRecordingSettingsDeferredAsync(");
        AssertDoesNotContain(runnerText, "private static async Task VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
        AssertDoesNotContain(runnerText, "private readonly record struct FlashbackRecordingSettingsDeferredPresetState(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionFlashbackLifecycleScenarios_OwnLifecycleFlow()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var lifecycleText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackLifecycleScenarios.cs")
            .Replace("\r\n", "\n");

        AssertContains(lifecycleText, "internal static class DiagnosticSessionFlashbackLifecycleScenarios");
        AssertContains(lifecycleText, "internal static async Task RunFlashbackLifecycleAsync(");
        AssertContains(lifecycleText, "flashback lifecycle pause requested");
        AssertContains(lifecycleText, "flashback lifecycle disabled during playback");
        AssertContains(lifecycleText, "flashback lifecycle: playback worker still alive after disable");
        AssertContains(lifecycleText, "flashback lifecycle: pending commands remained after disable");
        AssertContains(lifecycleText, "flashback lifecycle re-enabled");
        AssertContains(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackLifecycleScenarios;");
        AssertContains(startupText, "RunFlashbackLifecycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackLifecycleAsync(");

        return Task.CompletedTask;
    }
}
