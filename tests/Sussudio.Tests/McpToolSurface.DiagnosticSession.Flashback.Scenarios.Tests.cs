using System.Threading.Tasks;

static partial class Program
{
    private static Task DiagnosticSessionFlashbackCycleScenarios_OwnCycleFlows()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var restartText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackCycleScenarios.Restart.cs")
            .Replace("\r\n", "\n");
        var encoderText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackCycleScenarios.Encoder.cs")
            .Replace("\r\n", "\n");
        var registrationsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackCycleScenarios.Registrations.cs")
            .Replace("\r\n", "\n");
        var cyclesText = ReadDiagnosticSessionFlashbackCycleScenariosSource();

        AssertContains(restartText, "internal static partial class DiagnosticSessionFlashbackCycleScenarios");
        AssertContains(restartText, "internal static async Task RunFlashbackRestartCycleAsync(");
        AssertContains(restartText, "\"RestartFlashback\"");
        AssertContains(restartText, "\"flashback-restart-cycle-export.mp4\"");
        AssertContains(restartText, "flashback restart cycle export verified");
        AssertContains(encoderText, "internal static partial class DiagnosticSessionFlashbackCycleScenarios");
        AssertContains(encoderText, "internal static async Task RunFlashbackEncoderCycleAsync(");
        AssertContains(encoderText, "var cycledPreset = string.Equals(originalPreset, \"P1\", StringComparison.OrdinalIgnoreCase) ? \"P2\" : \"P1\";");
        AssertContains(encoderText, "\"flashback-encoder-cycle-export.mp4\"");
        AssertContains(encoderText, "flashback encoder preset restored to");
        AssertContains(registrationsText, "internal static partial class DiagnosticSessionFlashbackCycleScenarios");
        AssertDoesNotContain(registrationsText, "internal static async Task RunFlashbackRestartCycleAsync(");
        AssertDoesNotContain(registrationsText, "internal static async Task RunFlashbackEncoderCycleAsync(");
        AssertContains(cyclesText, "internal static void RegisterSelectedFlashbackCycleScenarioTasks(");
        AssertContains(cyclesText, "4,\n                \"flashback-restart-cycle-task\",");
        AssertContains(cyclesText, "5,\n                \"flashback-encoder-cycle-task\",");
        AssertContains(startupText, "DiagnosticSessionFlashbackCycleScenarios.RegisterSelectedFlashbackCycleScenarioTasks(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackCycleScenarios;");
        AssertDoesNotContain(startupText, "RunFlashbackRestartCycleAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackEncoderCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRestartCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackEncoderCycleAsync(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionFlashbackPreviewCycleScenarios_OwnPreviewCycleFlows()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var cyclesText = ReadDiagnosticSessionFlashbackPreviewCycleScenariosSource();
        var flashbackCycleText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Flashback.cs")
            .Replace("\r\n", "\n");
        var flashbackExportText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.FlashbackExport.cs")
            .Replace("\r\n", "\n");
        var playbackCycleText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Playback.cs")
            .Replace("\r\n", "\n");
        var playbackExportText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.PlaybackExport.cs")
            .Replace("\r\n", "\n");

        AssertContains(cyclesText, "internal static partial class DiagnosticSessionFlashbackPreviewCycleScenarios");
        AssertContains(cyclesText, "internal static async Task RunFlashbackPreviewCycleAsync(");
        AssertContains(flashbackCycleText, "flashback preview cycle preview stopped");
        AssertContains(flashbackCycleText, "VerifyFlashbackPreviewCycleExportAsync(");
        AssertDoesNotContain(flashbackCycleText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(flashbackExportText, "private static async Task VerifyFlashbackPreviewCycleExportAsync(");
        AssertContains(flashbackExportText, "\"flashback-preview-off-export.mp4\"");
        AssertContains(flashbackExportText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(flashbackExportText, "flashback preview cycle export verified");
        AssertContains(cyclesText, "internal static async Task RunFlashbackPlaybackPreviewCycleAsync(");
        AssertContains(playbackCycleText, "flashback playback preview cycle preview stopped during playback");
        AssertContains(playbackCycleText, "VerifyFlashbackPlaybackPreviewCycleExportAsync(");
        AssertDoesNotContain(playbackCycleText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(playbackExportText, "private static async Task VerifyFlashbackPlaybackPreviewCycleExportAsync(");
        AssertContains(playbackExportText, "\"flashback-playback-preview-cycle.mp4\"");
        AssertContains(playbackExportText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(playbackExportText, "flashback playback preview cycle export verified");
        AssertContains(cyclesText, "internal static async Task RunFlashbackRecordingPreviewCycleAsync(");
        AssertContains(cyclesText, "flashback recording preview cycle preview stopped");
        AssertContains(cyclesText, "internal static bool IsPreviewCycleScenario(");
        AssertContains(cyclesText, "internal static void RegisterSelectedFlashbackPreviewCycleScenarioTasks(");
        AssertContains(cyclesText, "13,\n                \"flashback-preview-cycle-task\",");
        AssertContains(cyclesText, "14,\n                \"flashback-playback-preview-cycle-task\",");
        AssertContains(cyclesText, "15,\n                \"flashback-recording-preview-cycle-task\",");
        AssertContains(startupText, "DiagnosticSessionFlashbackPreviewCycleScenarios.RegisterSelectedFlashbackPreviewCycleScenarioTasks(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackPreviewCycleScenarios;");
        AssertDoesNotContain(startupText, "RunFlashbackPreviewCycleAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackPlaybackPreviewCycleAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackRecordingPreviewCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackPreviewCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackPlaybackPreviewCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRecordingPreviewCycleAsync(");
        AssertDoesNotContain(runnerText, "private static bool IsPreviewCycleScenario(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionFlashbackRejectedExports_OwnRejectionFlows()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var rejectedRootText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackRejectedExports.cs")
            .Replace("\r\n", "\n");
        var inactiveRejectedText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackRejectedExports.Inactive.cs")
            .Replace("\r\n", "\n");
        var recordingRejectedText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackRejectedExports.Recording.cs")
            .Replace("\r\n", "\n");

        AssertContains(rejectedRootText, "internal static partial class DiagnosticSessionFlashbackRejectedExports");
        AssertContains(rejectedRootText, "internal static async Task RunSelectedRejectedExportScenariosAsync(");
        AssertDoesNotContain(rejectedRootText, "internal static async Task RunFlashbackExportRejectedAsync(");
        AssertDoesNotContain(rejectedRootText, "internal static async Task RunFlashbackRecordingExportRejectedAsync(");
        AssertContains(inactiveRejectedText, "internal static partial class DiagnosticSessionFlashbackRejectedExports");
        AssertContains(inactiveRejectedText, "internal static async Task RunFlashbackExportRejectedAsync(");
        AssertContains(inactiveRejectedText, "\"flashback-rejected-export.mp4\"");
        AssertContains(inactiveRejectedText, "BufferInactive");
        AssertContains(inactiveRejectedText, "Flashback buffer not active");
        AssertContains(recordingRejectedText, "internal static partial class DiagnosticSessionFlashbackRejectedExports");
        AssertContains(recordingRejectedText, "internal static async Task RunFlashbackRecordingExportRejectedAsync(");
        AssertContains(recordingRejectedText, "\"flashback-recording-rejected-export.mp4\"");
        AssertContains(recordingRejectedText, "UnavailableDuringRecording");
        AssertContains(recordingRejectedText, "recording backend changed after rejected export");
        var dispatchText = ExtractMemberCode(rejectedRootText, "RunSelectedRejectedExportScenariosAsync");
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
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var segmentPlaybackText = ReadDiagnosticSessionFlashbackSegmentPlaybackScenariosSource();
        var segmentPlaybackRootText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.cs")
            .Replace("\r\n", "\n");
        var segmentPlaybackValidationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.Validation.cs")
            .Replace("\r\n", "\n");

        AssertContains(segmentPlaybackText, "internal static partial class DiagnosticSessionFlashbackSegmentPlaybackScenarios");
        AssertContains(segmentPlaybackText, "internal static async Task RunFlashbackSegmentPlaybackAsync(");
        AssertContains(segmentPlaybackRootText, "flashback segment playback live headroom established");
        AssertContains(segmentPlaybackRootText, "flashback segment playback started near boundary");
        AssertContains(segmentPlaybackRootText, "ValidateFlashbackSegmentPlaybackSnapshot(");
        AssertDoesNotContain(segmentPlaybackRootText, "frameCount >= 180");
        AssertDoesNotContain(segmentPlaybackRootText, "playback FPS below source-rate target after warm sample");
        AssertContains(segmentPlaybackValidationText, "private static void ValidateFlashbackSegmentPlaybackSnapshot(");
        AssertContains(segmentPlaybackValidationText, "frameCount >= 180");
        AssertContains(segmentPlaybackValidationText, "playback FPS below source-rate target after warm sample");
        AssertContains(segmentPlaybackValidationText, "flashback segment playback: command queue unhealthy");
        AssertContains(segmentPlaybackText, "private static async Task<bool> CreateFlashbackCompletedSegmentViaRecordingAsync(");
        AssertContains(segmentPlaybackText, "recording-assisted rotation started");
        AssertContains(segmentPlaybackText, "private static async Task TryStopRecordingAsync(");
        AssertContains(segmentPlaybackText, "internal static void RegisterSelectedFlashbackSegmentPlaybackScenarioTask(");
        AssertContains(segmentPlaybackText, "scenarioPlan.RunFlashbackSegmentPlayback");
        AssertContains(segmentPlaybackText, "7,\n            \"flashback-segment-playback-task\",");
        AssertContains(segmentPlaybackText, "actions.Add(\"flashback segment playback started\")");
        AssertContains(startupText, "DiagnosticSessionFlashbackSegmentPlaybackScenarios.RegisterSelectedFlashbackSegmentPlaybackScenarioTask(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackSegmentPlaybackScenarios;");
        AssertDoesNotContain(startupText, "RunFlashbackSegmentPlaybackAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackSegmentPlaybackAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<bool> CreateFlashbackCompletedSegmentViaRecordingAsync(");
        AssertDoesNotContain(runnerText, "private static async Task TryStopRecordingAsync(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionFlashbackRecordingSettingsScenarios_OwnDeferredSettingsFlow()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var recordingChecksText = ReadRepoFile("tools/Common/DiagnosticSessionRecordingChecks.cs")
            .Replace("\r\n", "\n");
        var modelsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.Models.cs")
            .Replace("\r\n", "\n");
        var duringRecordingText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.DuringRecording.cs")
            .Replace("\r\n", "\n");
        var postStopText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.PostStop.cs")
            .Replace("\r\n", "\n");

        AssertContains(modelsText, "internal readonly record struct FlashbackRecordingSettingsDeferredPresetState(");
        AssertContains(duringRecordingText, "internal static partial class DiagnosticSessionFlashbackRecordingSettingsScenarios");
        AssertContains(duringRecordingText, "internal static async Task<FlashbackRecordingSettingsDeferredPresetState> RunFlashbackRecordingSettingsDeferredAsync(");
        AssertContains(duringRecordingText, "flashback recording settings deferred preset changed to");
        AssertContains(duringRecordingText, "RestartFlashback unexpectedly succeeded during recording");
        AssertContains(duringRecordingText, "SetFlashbackEnabled(false) unexpectedly succeeded during recording");
        AssertContains(postStopText, "internal static partial class DiagnosticSessionFlashbackRecordingSettingsScenarios");
        AssertContains(postStopText, "internal static async Task VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
        AssertContains(postStopText, "flashback recording settings deferred post-stop buffer verified");
        AssertContains(postStopText, "flashback recording settings deferred preset restored to");
        AssertDoesNotContain(modelsText, "RunFlashbackRecordingSettingsDeferredAsync(");
        AssertDoesNotContain(modelsText, "VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
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
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var lifecycleText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackLifecycleScenarios.cs")
            .Replace("\r\n", "\n");

        AssertContains(lifecycleText, "internal static class DiagnosticSessionFlashbackLifecycleScenarios");
        AssertContains(lifecycleText, "internal static void RegisterSelectedFlashbackLifecycleScenarioTask(");
        AssertContains(lifecycleText, "scenarioPlan.RunFlashbackLifecycle");
        AssertContains(lifecycleText, "backgroundTasks.AddScenario(");
        AssertContains(lifecycleText, "2,\n            \"flashback-lifecycle-task\",");
        AssertContains(lifecycleText, "actions.Add(\"flashback lifecycle started\")");
        AssertContains(lifecycleText, "internal static async Task RunFlashbackLifecycleAsync(");
        AssertContains(lifecycleText, "flashback lifecycle pause requested");
        AssertContains(lifecycleText, "flashback lifecycle disabled during playback");
        AssertContains(lifecycleText, "flashback lifecycle: playback worker still alive after disable");
        AssertContains(lifecycleText, "flashback lifecycle: pending commands remained after disable");
        AssertContains(lifecycleText, "flashback lifecycle re-enabled");
        AssertContains(startupText, "DiagnosticSessionFlashbackLifecycleScenarios.RegisterSelectedFlashbackLifecycleScenarioTask(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackLifecycleScenarios;");
        AssertDoesNotContain(startupText, "RunFlashbackLifecycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackLifecycleAsync(");

        return Task.CompletedTask;
    }
}
