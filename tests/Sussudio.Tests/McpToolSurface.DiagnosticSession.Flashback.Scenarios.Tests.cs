using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticSessionFlashbackCycleScenarios_OwnCycleFlows()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var restartText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackCycleScenarios.Restart.cs")
            .Replace("\r\n", "\n");
        var restartValidationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackCycleScenarios.RestartValidation.cs")
            .Replace("\r\n", "\n");
        var restartExportText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackCycleScenarios.RestartExport.cs")
            .Replace("\r\n", "\n");
        var encoderText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackCycleScenarios.Encoder.cs")
            .Replace("\r\n", "\n");
        var encoderValidationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackCycleScenarios.EncoderValidation.cs")
            .Replace("\r\n", "\n");
        var encoderExportText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackCycleScenarios.EncoderExport.cs")
            .Replace("\r\n", "\n");
        var encoderRestoreText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackCycleScenarios.EncoderRestore.cs")
            .Replace("\r\n", "\n");
        var registrationsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackCycleScenarios.Registrations.cs")
            .Replace("\r\n", "\n");
        var cyclesText = ReadDiagnosticSessionFlashbackCycleScenariosSource();

        AssertContains(restartText, "internal static partial class DiagnosticSessionFlashbackCycleScenarios");
        AssertContains(restartText, "internal static async Task RunFlashbackRestartCycleAsync(");
        AssertContains(restartText, "\"RestartFlashback\"");
        AssertContains(restartText, "ValidateFlashbackRestartCycleActiveStateAsync(");
        AssertContains(restartText, "VerifyFlashbackRestartCycleExportAsync(");
        AssertDoesNotContain(restartText, "FlashbackPlaybackThreadAlive");
        AssertDoesNotContain(restartText, "\"flashback-restart-cycle-export.mp4\"");
        AssertDoesNotContain(restartText, "flashback restart cycle export verified");
        AssertContains(restartValidationText, "private static async Task<bool> ValidateFlashbackRestartCycleActiveStateAsync(");
        AssertContains(restartValidationText, "FlashbackPlaybackThreadAlive");
        AssertContains(restartValidationText, "pending playback commands remained after restart");
        AssertContains(restartExportText, "private static async Task VerifyFlashbackRestartCycleExportAsync(");
        AssertContains(restartExportText, "\"flashback-restart-cycle-export.mp4\"");
        AssertContains(restartExportText, "flashback restart cycle export verified");
        AssertContains(encoderText, "internal static partial class DiagnosticSessionFlashbackCycleScenarios");
        AssertContains(encoderText, "internal static async Task RunFlashbackEncoderCycleAsync(");
        AssertContains(encoderText, "var cycledPreset = string.Equals(originalPreset, \"P1\", StringComparison.OrdinalIgnoreCase) ? \"P2\" : \"P1\";");
        AssertContains(encoderText, "ValidateFlashbackEncoderCycleSnapshot(afterSnapshot, originalFilePath, warnings);");
        AssertContains(encoderText, "VerifyFlashbackEncoderCycleExportAsync(");
        AssertContains(encoderText, "RestoreFlashbackEncoderCyclePresetAsync(");
        AssertDoesNotContain(encoderText, "post-cycle encoder did not reach readiness frame count");
        AssertDoesNotContain(encoderText, "\"flashback-encoder-cycle-export.mp4\"");
        AssertDoesNotContain(encoderText, "flashback encoder preset restored to");
        AssertContains(encoderValidationText, "private static void ValidateFlashbackEncoderCycleSnapshot(");
        AssertContains(encoderValidationText, "post-cycle encoder did not reach readiness frame count");
        AssertContains(encoderValidationText, "playback state not clean after preset cycle");
        AssertContains(encoderExportText, "private static async Task VerifyFlashbackEncoderCycleExportAsync(");
        AssertContains(encoderExportText, "\"flashback-encoder-cycle-export.mp4\"");
        AssertContains(encoderExportText, "flashback encoder cycle export verified");
        AssertContains(encoderRestoreText, "private static async Task RestoreFlashbackEncoderCyclePresetAsync(");
        AssertContains(encoderRestoreText, "flashback encoder preset restored to");
        AssertContains(encoderRestoreText, "Flashback buffer did not become ready after preset restore");
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

    internal static Task DiagnosticSessionFlashbackPreviewCycleScenarios_OwnPreviewCycleFlows()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var cyclesText = ReadDiagnosticSessionFlashbackPreviewCycleScenariosSource();
        var flashbackCycleText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Flashback.cs")
            .Replace("\r\n", "\n");
        var flashbackPreStopText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.FlashbackPreStop.cs")
            .Replace("\r\n", "\n");
        var flashbackStoppedText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.FlashbackStopped.cs")
            .Replace("\r\n", "\n");
        var flashbackRestartValidationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.FlashbackRestartValidation.cs")
            .Replace("\r\n", "\n");
        var flashbackExportText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.FlashbackExport.cs")
            .Replace("\r\n", "\n");
        var playbackCycleText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Playback.cs")
            .Replace("\r\n", "\n");
        var playbackPreStopText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.PlaybackPreStop.cs")
            .Replace("\r\n", "\n");
        var playbackStoppedText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.PlaybackStopped.cs")
            .Replace("\r\n", "\n");
        var playbackRestartText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.PlaybackRestart.cs")
            .Replace("\r\n", "\n");
        var playbackExportText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.PlaybackExport.cs")
            .Replace("\r\n", "\n");
        var recordingCycleText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Recording.cs")
            .Replace("\r\n", "\n");
        var recordingCountersText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.RecordingCounters.cs")
            .Replace("\r\n", "\n");
        var recordingValidationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.RecordingValidation.cs")
            .Replace("\r\n", "\n");
        var recordingRestartValidationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.RecordingRestartValidation.cs")
            .Replace("\r\n", "\n");

        AssertContains(cyclesText, "internal static partial class DiagnosticSessionFlashbackPreviewCycleScenarios");
        AssertContains(cyclesText, "internal static async Task RunFlashbackPreviewCycleAsync(");
        AssertContains(flashbackCycleText, "flashback preview cycle preview stopped");
        AssertContains(flashbackCycleText, "CaptureFlashbackPreviewCycleEncodedFramesBeforeStopAsync(");
        AssertContains(flashbackCycleText, "ValidateFlashbackPreviewCycleStoppedAsync(");
        AssertContains(flashbackCycleText, "ValidateFlashbackPreviewCycleRestartedAsync(");
        AssertDoesNotContain(flashbackCycleText, "Flashback frames did not advance while preview was off");
        AssertDoesNotContain(flashbackCycleText, "VideoFramesFlowing");
        AssertContains(flashbackPreStopText, "private static async Task<long> CaptureFlashbackPreviewCycleEncodedFramesBeforeStopAsync(");
        AssertDoesNotContain(flashbackPreStopText, "Flashback frames did not advance while preview was off");
        AssertContains(flashbackStoppedText, "private static async Task<bool> ValidateFlashbackPreviewCycleStoppedAsync(");
        AssertContains(flashbackStoppedText, "flashback preview cycle: Flashback frames did not advance while preview was off");
        AssertDoesNotContain(flashbackStoppedText, "VideoFramesFlowing");
        AssertContains(flashbackRestartValidationText, "private static async Task ValidateFlashbackPreviewCycleRestartedAsync(");
        AssertContains(flashbackRestartValidationText, "VideoFramesFlowing");
        AssertDoesNotContain(flashbackRestartValidationText, "Flashback frames did not advance while preview was off");
        AssertContains(flashbackCycleText, "VerifyFlashbackPreviewCycleExportAsync(");
        AssertDoesNotContain(flashbackCycleText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(flashbackExportText, "private static async Task VerifyFlashbackPreviewCycleExportAsync(");
        AssertContains(flashbackExportText, "\"flashback-preview-off-export.mp4\"");
        AssertContains(flashbackExportText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(flashbackExportText, "flashback preview cycle export verified");
        AssertContains(cyclesText, "internal static async Task RunFlashbackPlaybackPreviewCycleAsync(");
        AssertContains(playbackCycleText, "flashback playback preview cycle preview stopped during playback");
        AssertContains(playbackCycleText, "CapturePlaybackPreviewCycleFrameCountBeforeStopAsync(");
        AssertContains(playbackCycleText, "ValidatePlaybackPreviewCycleStoppedAsync(");
        AssertContains(playbackCycleText, "ValidatePlaybackPreviewCycleRestartedAsync(");
        AssertDoesNotContain(playbackCycleText, "WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertDoesNotContain(playbackCycleText, "playback did not return live after preview stop");
        AssertDoesNotContain(playbackCycleText, "VideoFramesFlowing");
        AssertContains(playbackPreStopText, "private static async Task<long> CapturePlaybackPreviewCycleFrameCountBeforeStopAsync(");
        AssertContains(playbackPreStopText, "WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertDoesNotContain(playbackPreStopText, "playback did not return live after preview stop");
        AssertContains(playbackStoppedText, "private static async Task<bool> ValidatePlaybackPreviewCycleStoppedAsync(");
        AssertContains(playbackStoppedText, "flashback playback preview cycle: playback did not return live after preview stop");
        AssertDoesNotContain(playbackStoppedText, "VideoFramesFlowing");
        AssertContains(playbackRestartText, "private static async Task ValidatePlaybackPreviewCycleRestartedAsync(");
        AssertContains(playbackRestartText, "VideoFramesFlowing");
        AssertContains(playbackCycleText, "VerifyFlashbackPlaybackPreviewCycleExportAsync(");
        AssertDoesNotContain(playbackCycleText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(playbackExportText, "private static async Task VerifyFlashbackPlaybackPreviewCycleExportAsync(");
        AssertContains(playbackExportText, "\"flashback-playback-preview-cycle.mp4\"");
        AssertContains(playbackExportText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(playbackExportText, "flashback playback preview cycle export verified");
        AssertContains(cyclesText, "internal static async Task RunFlashbackRecordingPreviewCycleAsync(");
        AssertContains(cyclesText, "flashback recording preview cycle preview stopped");
        AssertContains(recordingCycleText, "CaptureRecordingPreviewCycleCountersBeforeStopAsync(");
        AssertContains(recordingCycleText, "ValidateRecordingPreviewCycleStoppedAsync(");
        AssertContains(recordingCycleText, "ValidateRecordingPreviewCycleRestartedAsync(");
        AssertDoesNotContain(recordingCycleText, "WaitForFlashbackRecordingReadyAsync(");
        AssertDoesNotContain(recordingCycleText, "recording counters did not advance while preview was off");
        AssertDoesNotContain(recordingCycleText, "VideoFramesFlowing");
        AssertContains(recordingCountersText, "private readonly record struct RecordingPreviewCycleCounters(");
        AssertContains(recordingCountersText, "private static async Task<RecordingPreviewCycleCounters?> CaptureRecordingPreviewCycleCountersBeforeStopAsync(");
        AssertContains(recordingCountersText, "WaitForFlashbackRecordingReadyAsync(");
        AssertDoesNotContain(recordingCountersText, "recording counters did not advance while preview was off");
        AssertContains(recordingValidationText, "WaitForPreviewActiveAsync(");
        AssertContains(recordingValidationText, "private static async Task<bool> ValidateRecordingPreviewCycleStoppedAsync(");
        AssertContains(recordingValidationText, "flashback recording preview cycle: recording counters did not advance while preview was off");
        AssertDoesNotContain(recordingValidationText, "private readonly record struct RecordingPreviewCycleCounters(");
        AssertDoesNotContain(recordingValidationText, "private static async Task<RecordingPreviewCycleCounters?> CaptureRecordingPreviewCycleCountersBeforeStopAsync(");
        AssertDoesNotContain(recordingValidationText, "private static async Task ValidateRecordingPreviewCycleRestartedAsync(");
        AssertDoesNotContain(recordingValidationText, "VideoFramesFlowing");
        AssertContains(recordingRestartValidationText, "private static async Task ValidateRecordingPreviewCycleRestartedAsync(");
        AssertContains(recordingRestartValidationText, "VideoFramesFlowing");
        AssertContains(recordingRestartValidationText, "flashback recording preview cycle: preview frames did not resume");
        AssertDoesNotContain(recordingRestartValidationText, "recording counters did not advance while preview was off");
        AssertDoesNotContain(cyclesText, "internal static bool IsPreviewCycleScenario(");
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

    internal static Task DiagnosticSessionFlashbackRejectedExports_OwnRejectionFlows()
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

    internal static Task DiagnosticSessionFlashbackSegmentPlaybackScenarios_OwnSegmentPlaybackFlow()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var segmentPlaybackText = ReadDiagnosticSessionFlashbackSegmentPlaybackScenariosSource();
        var segmentPlaybackRootText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.cs")
            .Replace("\r\n", "\n");
        var segmentPlaybackRegistrationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.Registrations.cs")
            .Replace("\r\n", "\n");
        var segmentPlaybackLiveRestoreText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.LiveRestore.cs")
            .Replace("\r\n", "\n");
        var segmentPlaybackTargetText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.Target.cs")
            .Replace("\r\n", "\n");
        var segmentPlaybackValidationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.Validation.cs")
            .Replace("\r\n", "\n");

        AssertContains(segmentPlaybackText, "internal static partial class DiagnosticSessionFlashbackSegmentPlaybackScenarios");
        AssertContains(segmentPlaybackText, "internal static async Task RunFlashbackSegmentPlaybackAsync(");
        AssertContains(segmentPlaybackRootText, "flashback segment playback live headroom established");
        AssertContains(segmentPlaybackRootText, "flashback segment playback started near boundary");
        AssertContains(segmentPlaybackRootText, "AcquireFlashbackSegmentPlaybackTargetAsync(");
        AssertContains(segmentPlaybackRootText, "ValidateFlashbackSegmentPlaybackSnapshot(");
        AssertContains(segmentPlaybackRootText, "ReturnFlashbackSegmentPlaybackLiveAsync(");
        AssertDoesNotContain(segmentPlaybackRootText, "WaitForFlashbackPlayableCompletedSegmentAsync(");
        AssertDoesNotContain(segmentPlaybackRootText, "recording-assisted rotation started");
        AssertDoesNotContain(segmentPlaybackRootText, "frameCount >= 180");
        AssertDoesNotContain(segmentPlaybackRootText, "playback FPS below source-rate target after warm sample");
        AssertDoesNotContain(segmentPlaybackRootText, "internal static void RegisterSelectedFlashbackSegmentPlaybackScenarioTask(");
        AssertDoesNotContain(segmentPlaybackRootText, "\"go-live\"");
        AssertContains(segmentPlaybackTargetText, "private static async Task<FlashbackSegmentPlaybackTarget?> AcquireFlashbackSegmentPlaybackTargetAsync(");
        AssertContains(segmentPlaybackTargetText, "WaitForFlashbackPlayableCompletedSegmentAsync(");
        AssertContains(segmentPlaybackTargetText, "CreateFlashbackCompletedSegmentViaRecordingAsync(");
        AssertContains(segmentPlaybackTargetText, "no playable completed segment became available after recording-assisted rotation");
        AssertContains(segmentPlaybackValidationText, "private static void ValidateFlashbackSegmentPlaybackSnapshot(");
        AssertContains(segmentPlaybackValidationText, "frameCount >= 180");
        AssertContains(segmentPlaybackValidationText, "playback FPS below source-rate target after warm sample");
        AssertContains(segmentPlaybackValidationText, "flashback segment playback: command queue unhealthy");
        AssertContains(segmentPlaybackLiveRestoreText, "private static async Task ReturnFlashbackSegmentPlaybackLiveAsync(");
        AssertContains(segmentPlaybackLiveRestoreText, "\"go-live\"");
        AssertContains(segmentPlaybackLiveRestoreText, "flashback segment playback go-live requested");
        AssertContains(segmentPlaybackLiveRestoreText, "flashback segment playback: playback ended in state");
        AssertContains(segmentPlaybackText, "private static async Task<bool> CreateFlashbackCompletedSegmentViaRecordingAsync(");
        AssertContains(segmentPlaybackText, "recording-assisted rotation started");
        AssertContains(segmentPlaybackText, "private static async Task TryStopRecordingAsync(");
        AssertContains(segmentPlaybackRegistrationText, "internal static void RegisterSelectedFlashbackSegmentPlaybackScenarioTask(");
        AssertContains(segmentPlaybackRegistrationText, "scenarioPlan.RunFlashbackSegmentPlayback");
        AssertContains(segmentPlaybackRegistrationText, "7,\n            \"flashback-segment-playback-task\",");
        AssertContains(segmentPlaybackRegistrationText, "actions.Add(\"flashback segment playback started\")");
        AssertDoesNotContain(segmentPlaybackRegistrationText, "ReturnFlashbackSegmentPlaybackLiveAsync(");
        AssertContains(startupText, "DiagnosticSessionFlashbackSegmentPlaybackScenarios.RegisterSelectedFlashbackSegmentPlaybackScenarioTask(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackSegmentPlaybackScenarios;");
        AssertDoesNotContain(startupText, "RunFlashbackSegmentPlaybackAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackSegmentPlaybackAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<bool> CreateFlashbackCompletedSegmentViaRecordingAsync(");
        AssertDoesNotContain(runnerText, "private static async Task TryStopRecordingAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackRecordingSettingsScenarios_OwnDeferredSettingsFlow()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var recordingChecksText = ReadRepoFile("tools/Common/DiagnosticSessionRecordingChecks.cs")
            .Replace("\r\n", "\n");
        var modelsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.Models.cs")
            .Replace("\r\n", "\n");
        var duringRecordingText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.DuringRecording.cs")
            .Replace("\r\n", "\n");
        var rejectionText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.DuringRecordingRejections.cs")
            .Replace("\r\n", "\n");
        var validationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.DuringRecordingValidation.cs")
            .Replace("\r\n", "\n");
        var postStopText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.PostStop.cs")
            .Replace("\r\n", "\n");
        var postStopRestoreText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.PostStopRestore.cs")
            .Replace("\r\n", "\n");

        AssertContains(modelsText, "internal readonly record struct FlashbackRecordingSettingsDeferredPresetState(");
        AssertContains(duringRecordingText, "internal static partial class DiagnosticSessionFlashbackRecordingSettingsScenarios");
        AssertContains(duringRecordingText, "internal static async Task<FlashbackRecordingSettingsDeferredPresetState> RunFlashbackRecordingSettingsDeferredAsync(");
        AssertContains(duringRecordingText, "flashback recording settings deferred preset changed to");
        AssertContains(duringRecordingText, "VerifyFlashbackRestartRejectedDuringRecordingAsync(");
        AssertContains(duringRecordingText, "VerifyFlashbackDisableRejectedDuringRecordingAsync(");
        AssertContains(duringRecordingText, "VerifyFlashbackRecordingSettingsDeferredStillRecordingAsync(");
        AssertContains(rejectionText, "private static async Task VerifyFlashbackRestartRejectedDuringRecordingAsync(");
        AssertContains(rejectionText, "private static async Task VerifyFlashbackDisableRejectedDuringRecordingAsync(");
        AssertContains(rejectionText, "private static async Task VerifyFlashbackRecordingSettingsCommandRejectedDuringRecordingAsync(");
        AssertContains(rejectionText, "RestartFlashback unexpectedly succeeded during recording");
        AssertContains(rejectionText, "SetFlashbackEnabled(false) unexpectedly succeeded during recording");
        AssertContains(validationText, "private static async Task VerifyFlashbackRecordingSettingsDeferredStillRecordingAsync(");
        AssertContains(validationText, "Flashback recording backend did not remain active after mutations");
        AssertContains(validationText, "recording counters did not advance after mutation attempts");
        AssertContains(postStopText, "internal static partial class DiagnosticSessionFlashbackRecordingSettingsScenarios");
        AssertContains(postStopText, "internal static async Task VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
        AssertContains(postStopText, "flashback recording settings deferred post-stop buffer verified");
        AssertContains(postStopText, "RestoreFlashbackRecordingSettingsOriginalPresetAsync(");
        AssertDoesNotContain(postStopText, "\"SetPreset\"");
        AssertDoesNotContain(postStopText, "flashback recording settings deferred preset restored to");
        AssertContains(postStopRestoreText, "private static async Task RestoreFlashbackRecordingSettingsOriginalPresetAsync(");
        AssertContains(postStopRestoreText, "\"SetPreset\"");
        AssertContains(postStopRestoreText, "flashback recording settings deferred preset restored to");
        AssertContains(postStopRestoreText, "selected preset was not restored");
        AssertDoesNotContain(duringRecordingText, "internal readonly record struct FlashbackRecordingSettingsDeferredPresetState(");
        AssertDoesNotContain(duringRecordingText, "private static async Task VerifyFlashbackRecordingSettingsCommandRejectedDuringRecordingAsync(");
        AssertDoesNotContain(duringRecordingText, "RestartFlashback unexpectedly succeeded during recording");
        AssertDoesNotContain(duringRecordingText, "SetFlashbackEnabled(false) unexpectedly succeeded during recording");
        AssertDoesNotContain(duringRecordingText, "private static async Task VerifyFlashbackRecordingSettingsDeferredStillRecordingAsync(");
        AssertDoesNotContain(rejectionText, "recording counters did not advance after mutation attempts");
        AssertDoesNotContain(validationText, "RestartFlashback unexpectedly succeeded during recording");
        AssertDoesNotContain(postStopText, "internal readonly record struct FlashbackRecordingSettingsDeferredPresetState(");
        AssertContains(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackRecordingSettingsScenarios;");
        AssertContains(startupText, "RunFlashbackRecordingSettingsDeferredAsync(");
        AssertContains(recordingChecksText, "using static Sussudio.Tools.DiagnosticSessionFlashbackRecordingSettingsScenarios;");
        AssertContains(recordingChecksText, "VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<FlashbackRecordingSettingsDeferredPresetState> RunFlashbackRecordingSettingsDeferredAsync(");
        AssertDoesNotContain(runnerText, "private static async Task VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
        AssertDoesNotContain(runnerText, "private readonly record struct FlashbackRecordingSettingsDeferredPresetState(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackLifecycleScenarios_OwnLifecycleFlow()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var lifecycleText = ReadDiagnosticSessionFlashbackLifecycleScenariosSource();
        var lifecycleRootText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackLifecycleScenarios.cs")
            .Replace("\r\n", "\n");
        var lifecycleRegistrationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackLifecycleScenarios.Registrations.cs")
            .Replace("\r\n", "\n");
        var lifecycleValidationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackLifecycleScenarios.Validation.cs")
            .Replace("\r\n", "\n");

        AssertContains(lifecycleText, "internal static partial class DiagnosticSessionFlashbackLifecycleScenarios");
        AssertContains(lifecycleRegistrationText, "internal static void RegisterSelectedFlashbackLifecycleScenarioTask(");
        AssertContains(lifecycleRegistrationText, "scenarioPlan.RunFlashbackLifecycle");
        AssertContains(lifecycleRegistrationText, "backgroundTasks.AddScenario(");
        AssertContains(lifecycleRegistrationText, "2,\n            \"flashback-lifecycle-task\",");
        AssertContains(lifecycleRegistrationText, "actions.Add(\"flashback lifecycle started\")");
        AssertContains(lifecycleRootText, "internal static async Task RunFlashbackLifecycleAsync(");
        AssertContains(lifecycleRootText, "flashback lifecycle pause requested");
        AssertContains(lifecycleRootText, "flashback lifecycle disabled during playback");
        AssertContains(lifecycleRootText, "ValidateFlashbackLifecycleDisabledAsync(");
        AssertContains(lifecycleRootText, "flashback lifecycle re-enabled");
        AssertContains(lifecycleRootText, "ValidateFlashbackLifecycleReenabledAsync(");
        AssertDoesNotContain(lifecycleRootText, "FlashbackPlaybackThreadAlive");
        AssertContains(lifecycleValidationText, "private static async Task ValidateFlashbackLifecycleDisabledAsync(");
        AssertContains(lifecycleValidationText, "flashback lifecycle: playback worker still alive after disable");
        AssertContains(lifecycleValidationText, "flashback lifecycle: pending commands remained after disable");
        AssertContains(lifecycleValidationText, "private static async Task ValidateFlashbackLifecycleReenabledAsync(");
        AssertContains(startupText, "DiagnosticSessionFlashbackLifecycleScenarios.RegisterSelectedFlashbackLifecycleScenarioTask(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackLifecycleScenarios;");
        AssertDoesNotContain(startupText, "RunFlashbackLifecycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackLifecycleAsync(");

        return Task.CompletedTask;
    }
}
