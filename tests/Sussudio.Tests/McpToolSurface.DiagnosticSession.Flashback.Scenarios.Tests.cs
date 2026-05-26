using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticSessionFlashbackStressScenario_OwnsStressFlow()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var stressText = ReadDiagnosticSessionFlashbackStressScenarioSource();

        AssertContains(stressText, "internal static class DiagnosticSessionFlashbackStressScenario");
        AssertDoesNotContain(stressText, "internal static partial class DiagnosticSessionFlashbackStressScenario");
        AssertContains(stressText, "internal const int FlashbackStressMaxPlaybackPendingCommands = 4;");
        AssertContains(stressText, "internal const int FlashbackStressMaxPlaybackCommandLatencyMs = 750;");
        AssertContains(stressText, "internal const double FlashbackStressPlaybackWarmSeconds = 10.0;");
        AssertContains(stressText, "internal const long FlashbackStressAudioUnavailableFallbackAllowance = 4;");
        AssertContains(stressText, "internal const int FlashbackScrubStressMaxPlaybackPendingCommands = 20;");
        AssertContains(stressText, "internal static async Task RunFlashbackStressAsync(");
        AssertContains(stressText, "ValidateFlashbackStressWarmPlaybackAsync(");
        AssertContains(stressText, "private static async Task VerifyFlashbackStressExportAsync(");
        AssertContains(stressText, "\"flashback-stress-export.mp4\"");
        AssertContains(stressText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(stressText, "flashback stress export verified");
        AssertContains(stressText, "private static async Task ValidateFlashbackStressWarmPlaybackAsync(");
        AssertContains(stressText, "WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertContains(stressText, "\"flashback playback warmed frames=");
        AssertContains(stressText, "private readonly record struct FlashbackStressWarmPlaybackAudioBaseline(");
        AssertContains(stressText, "private readonly record struct FlashbackStressWarmPlaybackAudioDeltas(");
        AssertContains(stressText, "private static FlashbackStressWarmPlaybackAudioBaseline CaptureFlashbackStressWarmPlaybackAudioBaseline(");
        AssertContains(stressText, "private static FlashbackStressWarmPlaybackAudioDeltas CaptureFlashbackStressWarmPlaybackAudioDeltas(");
        AssertContains(stressText, "FlashbackPlaybackAudioMasterUnavailableFallbacks");
        AssertContains(stressText, "FlashbackPlaybackAudioMasterLastFallbackReason");
        AssertContains(stressText, "private static async Task ValidateFlashbackStressCommandDrainAsync(");
        AssertContains(stressText, "BuildPlaybackCommandHealth(lastSnapshot, baselineSnapshot)");
        AssertContains(stressText, "\"flashback stress: playback command queue did not drain within 10s \"");
        AssertContains(stressText, "private readonly record struct FlashbackStressPlaybackDrainResult(");
        AssertContains(stressText, "private static async Task<FlashbackStressPlaybackDrainResult> WaitForFlashbackStressPlaybackCommandDrainAsync(");
        AssertContains(stressText, "GetInt(lastSnapshot, \"FlashbackPlaybackPendingCommands\") == 0");
        AssertContains(stressText, "GetString(lastSnapshot, \"FlashbackPlaybackState\")");
        AssertContains(stressText, "internal static async Task RunFlashbackScrubStressAsync(");
        AssertContains(stressText, "WaitForFlashbackStressBufferReadyAsync(");
        AssertContains(stressText, "new Dictionary<string, object?> { [\"action\"] = \"begin-scrub\", [\"positionMs\"] = 500 }");
        AssertContains(stressText, "private static async Task<int> RunFlashbackScrubStressUpdateBurstAsync(");
        AssertContains(stressText, "new Dictionary<string, object?> { [\"action\"] = \"update-scrub\", [\"positionMs\"] = positions[i] }");
        AssertContains(stressText, "return positions[^1];");
        AssertContains(stressText, "flashback scrub stress: {failedUpdates} update-scrub command(s) failed");
        AssertContains(stressText, "new Dictionary<string, object?> { [\"action\"] = \"end-scrub\", [\"positionMs\"] = finalScrubPositionMs }");
        AssertContains(stressText, "private static async Task ValidateFlashbackScrubStressDrainAsync(");
        AssertContains(stressText, "\"flashback scrub stress: playback did not settle live with an empty queue within 10s \"");
        AssertContains(stressText, "FlashbackScrubStressMaxPlaybackPendingCommands");
        AssertContains(stressText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(stressText, "internal static string? ClassifyFlashbackStressAudioMasterFallbackWarning(");
        AssertContains(stressText, "\"flashback stress: audio-master harmful fallbacks increased during warmed playback \"");
        AssertContains(stressText, "internal static void RegisterSelectedFlashbackStressScenarioTasks(");
        AssertContains(stressText, "1,\n                \"flashback-stress-task\",");
        AssertContains(stressText, "3,\n                \"flashback-scrub-stress-task\",");
        AssertContains(stressText, "RunFlashbackStressAsync(");
        AssertContains(stressText, "RunFlashbackScrubStressAsync(");
        AssertContains(stressText, "sendRawWithConnectRetryAsync");
        AssertContains(stressText, "actions.Add(\"flashback stress started\")");
        AssertContains(stressText, "actions.Add(\"flashback scrub stress started\")");
        AssertContains(startupText, "DiagnosticSessionFlashbackStressScenario.RegisterSelectedFlashbackStressScenarioTasks(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackStressScenario;");
        AssertDoesNotContain(startupText, "RunFlashbackStressAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackScrubStressAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackStressAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackScrubStressAsync(");
        AssertDoesNotContain(runnerText, "private static string? ClassifyFlashbackStressAudioMasterFallbackWarning(");
        AssertDoesNotContain(runnerText, "private const int FlashbackStressMaxPlaybackPendingCommands = 4;");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackStressScenario_ClassifiesAudioMasterFallbacks()
    {
        var assembly = LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        var stressScenarioType = assembly.GetType("Sussudio.Tools.DiagnosticSessionFlashbackStressScenario")
            ?? throw new InvalidOperationException("DiagnosticSessionFlashbackStressScenario type was not found.");
        var classify = stressScenarioType.GetMethod(
                "ClassifyFlashbackStressAudioMasterFallbackWarning",
                BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Audio-master fallback classifier was not found.");

        AssertEqual((string?)null, Invoke(0, 0, 0, 0), "no audio-master fallback warning");
        AssertEqual((string?)null, Invoke(4, 4, 0, 0), "startup unavailable fallback allowance");

        var unavailable = Invoke(5, 5, 0, 0)
            ?? throw new InvalidOperationException("Expected unavailable fallback warning.");
        AssertContains(unavailable, "audio-master unavailable fallbacks exceeded startup allowance");
        AssertContains(unavailable, "unavailableDelta=5");
        AssertContains(unavailable, "allowance=4");
        AssertContains(unavailable, "totalDelta=5");

        var stale = Invoke(2, 0, 1, 0)
            ?? throw new InvalidOperationException("Expected stale fallback warning.");
        AssertContains(stale, "audio-master harmful fallbacks increased during warmed playback");
        AssertContains(stale, "staleDelta=1");
        AssertContains(stale, "driftOutlierDelta=0");

        var driftOutlier = Invoke(2, 0, 0, 1)
            ?? throw new InvalidOperationException("Expected drift-outlier fallback warning.");
        AssertContains(driftOutlier, "audio-master harmful fallbacks increased during warmed playback");
        AssertContains(driftOutlier, "staleDelta=0");
        AssertContains(driftOutlier, "driftOutlierDelta=1");

        var unclassified = Invoke(2, 0, 0, 0)
            ?? throw new InvalidOperationException("Expected unclassified fallback warning.");
        AssertContains(unclassified, "audio-master unclassified fallbacks increased during warmed playback");
        AssertContains(unclassified, "delta=2");

        return Task.CompletedTask;

        string? Invoke(long totalDelta, long unavailableDelta, long staleDelta, long driftOutlierDelta)
            => classify.Invoke(null, new object?[] { totalDelta, unavailableDelta, staleDelta, driftOutlierDelta }) as string;
    }

    internal static Task DiagnosticSessionFlashbackCycleScenarios_OwnCycleFlows()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var cyclesText = ReadDiagnosticSessionFlashbackCycleScenariosSource();

        AssertContains(cyclesText, "internal static class DiagnosticSessionFlashbackCycleScenarios");
        AssertDoesNotContain(cyclesText, "internal static partial class DiagnosticSessionFlashbackCycleScenarios");
        AssertContains(cyclesText, "internal static async Task RunFlashbackRestartCycleAsync(");
        AssertContains(cyclesText, "\"RestartFlashback\"");
        AssertContains(cyclesText, "private static async Task<bool> ValidateFlashbackRestartCycleActiveStateAsync(");
        AssertContains(cyclesText, "FlashbackPlaybackThreadAlive");
        AssertContains(cyclesText, "pending playback commands remained after restart");
        AssertContains(cyclesText, "private static async Task VerifyFlashbackRestartCycleExportAsync(");
        AssertContains(cyclesText, "\"flashback-restart-cycle-export.mp4\"");
        AssertContains(cyclesText, "flashback restart cycle export verified");
        AssertContains(cyclesText, "internal static async Task RunFlashbackEncoderCycleAsync(");
        AssertContains(cyclesText, "var cycledPreset = string.Equals(originalPreset, \"P1\", StringComparison.OrdinalIgnoreCase) ? \"P2\" : \"P1\";");
        AssertContains(cyclesText, "ValidateFlashbackEncoderCycleSnapshot(afterSnapshot, originalFilePath, warnings);");
        AssertContains(cyclesText, "private static void ValidateFlashbackEncoderCycleSnapshot(");
        AssertContains(cyclesText, "post-cycle encoder did not reach readiness frame count");
        AssertContains(cyclesText, "playback state not clean after preset cycle");
        AssertContains(cyclesText, "private static async Task VerifyFlashbackEncoderCycleExportAsync(");
        AssertContains(cyclesText, "\"flashback-encoder-cycle-export.mp4\"");
        AssertContains(cyclesText, "flashback encoder cycle export verified");
        AssertContains(cyclesText, "private static async Task RestoreFlashbackEncoderCyclePresetAsync(");
        AssertContains(cyclesText, "flashback encoder preset restored to");
        AssertContains(cyclesText, "Flashback buffer did not become ready after preset restore");
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
        var flashbackCycleText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.cs")
            .Replace("\r\n", "\n");
        var playbackCycleText = flashbackCycleText;
        var recordingCycleText = flashbackCycleText;

        AssertContains(cyclesText, "internal static class DiagnosticSessionFlashbackPreviewCycleScenarios");
        AssertDoesNotContain(cyclesText, "internal static partial class DiagnosticSessionFlashbackPreviewCycleScenarios");
        AssertContains(cyclesText, "internal static async Task RunFlashbackPreviewCycleAsync(");
        AssertContains(flashbackCycleText, "flashback preview cycle preview stopped");
        AssertContains(flashbackCycleText, "CaptureFlashbackPreviewCycleEncodedFramesBeforeStopAsync(");
        AssertContains(flashbackCycleText, "ValidateFlashbackPreviewCycleStoppedAsync(");
        AssertContains(flashbackCycleText, "ValidateFlashbackPreviewCycleRestartedAsync(");
        AssertContains(flashbackCycleText, "private static async Task<long> CaptureFlashbackPreviewCycleEncodedFramesBeforeStopAsync(");
        AssertContains(flashbackCycleText, "private static async Task<bool> ValidateFlashbackPreviewCycleStoppedAsync(");
        AssertContains(flashbackCycleText, "flashback preview cycle: Flashback frames did not advance while preview was off");
        AssertContains(flashbackCycleText, "private static async Task ValidateFlashbackPreviewCycleRestartedAsync(");
        AssertContains(flashbackCycleText, "VideoFramesFlowing");
        AssertContains(flashbackCycleText, "VerifyFlashbackPreviewCycleExportAsync(");
        AssertContains(flashbackCycleText, "private static async Task VerifyFlashbackPreviewCycleExportAsync(");
        AssertContains(flashbackCycleText, "\"flashback-preview-off-export.mp4\"");
        AssertContains(flashbackCycleText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(flashbackCycleText, "flashback preview cycle export verified");
        AssertContains(cyclesText, "internal static async Task RunFlashbackPlaybackPreviewCycleAsync(");
        AssertContains(playbackCycleText, "flashback playback preview cycle preview stopped during playback");
        AssertContains(playbackCycleText, "CapturePlaybackPreviewCycleFrameCountBeforeStopAsync(");
        AssertContains(playbackCycleText, "ValidatePlaybackPreviewCycleStoppedAsync(");
        AssertContains(playbackCycleText, "ValidatePlaybackPreviewCycleRestartedAsync(");
        AssertContains(playbackCycleText, "private static async Task<long> CapturePlaybackPreviewCycleFrameCountBeforeStopAsync(");
        AssertContains(playbackCycleText, "WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertContains(playbackCycleText, "private static async Task<bool> ValidatePlaybackPreviewCycleStoppedAsync(");
        AssertContains(playbackCycleText, "flashback playback preview cycle: playback did not return live after preview stop");
        AssertContains(playbackCycleText, "private static async Task ValidatePlaybackPreviewCycleRestartedAsync(");
        AssertContains(playbackCycleText, "VideoFramesFlowing");
        AssertContains(playbackCycleText, "VerifyFlashbackPlaybackPreviewCycleExportAsync(");
        AssertContains(playbackCycleText, "private static async Task VerifyFlashbackPlaybackPreviewCycleExportAsync(");
        AssertContains(playbackCycleText, "\"flashback-playback-preview-cycle.mp4\"");
        AssertContains(playbackCycleText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(playbackCycleText, "flashback playback preview cycle export verified");
        AssertContains(cyclesText, "internal static async Task RunFlashbackRecordingPreviewCycleAsync(");
        AssertContains(cyclesText, "flashback recording preview cycle preview stopped");
        AssertContains(recordingCycleText, "CaptureRecordingPreviewCycleCountersBeforeStopAsync(");
        AssertContains(recordingCycleText, "ValidateRecordingPreviewCycleStoppedAsync(");
        AssertContains(recordingCycleText, "ValidateRecordingPreviewCycleRestartedAsync(");
        AssertContains(recordingCycleText, "private readonly record struct RecordingPreviewCycleCounters(");
        AssertContains(recordingCycleText, "private static async Task<RecordingPreviewCycleCounters?> CaptureRecordingPreviewCycleCountersBeforeStopAsync(");
        AssertContains(recordingCycleText, "WaitForFlashbackRecordingReadyAsync(");
        AssertContains(recordingCycleText, "WaitForPreviewActiveAsync(");
        AssertContains(recordingCycleText, "private static async Task<bool> ValidateRecordingPreviewCycleStoppedAsync(");
        AssertContains(recordingCycleText, "flashback recording preview cycle: recording counters did not advance while preview was off");
        AssertContains(recordingCycleText, "private static async Task ValidateRecordingPreviewCycleRestartedAsync(");
        AssertContains(recordingCycleText, "VideoFramesFlowing");
        AssertContains(recordingCycleText, "flashback recording preview cycle: preview frames did not resume");
        AssertDoesNotContain(cyclesText, "internal static bool IsPreviewCycleScenario(");
        AssertContains(cyclesText, "internal static void RegisterSelectedFlashbackPreviewCycleScenarioTasks(");
        AssertContains(cyclesText, "13,\n                \"flashback-preview-cycle-task\",");
        AssertContains(cyclesText, "14,\n                \"flashback-playback-preview-cycle-task\",");
        AssertContains(cyclesText, "15,\n                \"flashback-recording-preview-cycle-task\",");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackPreviewCycleScenarios.Playback.cs")),
            "Flashback playback preview-cycle scenario stays with the preview-cycle scenario family");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackPreviewCycleScenarios.Recording.cs")),
            "Flashback recording preview-cycle scenario stays with the preview-cycle scenario family");
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
        var rejectedExportsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackRejectedExports.cs")
            .Replace("\r\n", "\n");

        AssertContains(rejectedExportsText, "internal static class DiagnosticSessionFlashbackRejectedExports");
        AssertContains(rejectedExportsText, "internal static async Task RunSelectedRejectedExportScenariosAsync(");
        AssertContains(rejectedExportsText, "private static async Task RunFlashbackExportRejectedAsync(");
        AssertContains(rejectedExportsText, "\"flashback-rejected-export.mp4\"");
        AssertContains(rejectedExportsText, "BufferInactive");
        AssertContains(rejectedExportsText, "Flashback buffer not active");
        AssertContains(rejectedExportsText, "private static async Task RunFlashbackRecordingExportRejectedAsync(");
        AssertContains(rejectedExportsText, "\"flashback-recording-rejected-export.mp4\"");
        AssertContains(rejectedExportsText, "UnavailableDuringRecording");
        AssertContains(rejectedExportsText, "recording backend changed after rejected export");
        var dispatchText = ExtractMemberCode(rejectedExportsText, "RunSelectedRejectedExportScenariosAsync");
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

        AssertContains(segmentPlaybackText, "internal static class DiagnosticSessionFlashbackSegmentPlaybackScenarios");
        AssertDoesNotContain(segmentPlaybackText, "internal static partial class DiagnosticSessionFlashbackSegmentPlaybackScenarios");
        AssertContains(segmentPlaybackText, "internal static async Task RunFlashbackSegmentPlaybackAsync(");
        AssertContains(segmentPlaybackText, "flashback segment playback live headroom established");
        AssertContains(segmentPlaybackText, "flashback segment playback started near boundary");
        AssertContains(segmentPlaybackText, "private static async Task<FlashbackSegmentPlaybackTarget?> AcquireFlashbackSegmentPlaybackTargetAsync(");
        AssertContains(segmentPlaybackText, "WaitForFlashbackPlayableCompletedSegmentAsync(");
        AssertContains(segmentPlaybackText, "no playable completed segment became available after recording-assisted rotation");
        AssertContains(segmentPlaybackText, "private static void ValidateFlashbackSegmentPlaybackSnapshot(");
        AssertContains(segmentPlaybackText, "frameCount >= 180");
        AssertContains(segmentPlaybackText, "playback FPS below source-rate target after warm sample");
        AssertContains(segmentPlaybackText, "flashback segment playback: command queue unhealthy");
        AssertContains(segmentPlaybackText, "private static async Task ReturnFlashbackSegmentPlaybackLiveAsync(");
        AssertContains(segmentPlaybackText, "\"go-live\"");
        AssertContains(segmentPlaybackText, "flashback segment playback go-live requested");
        AssertContains(segmentPlaybackText, "flashback segment playback: playback ended in state");
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

    internal static Task DiagnosticSessionFlashbackRecordingSettingsScenarios_OwnDeferredSettingsFlow()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var recordingChecksText = ReadRepoFile("tools/Common/DiagnosticSessionRecordingChecks.cs")
            .Replace("\r\n", "\n");
        var recordingSettingsText = ReadDiagnosticSessionFlashbackRecordingSettingsScenariosSource();

        AssertContains(recordingSettingsText, "internal readonly record struct FlashbackRecordingSettingsDeferredPresetState(");
        AssertContains(recordingSettingsText, "internal static class DiagnosticSessionFlashbackRecordingSettingsScenarios");
        AssertDoesNotContain(recordingSettingsText, "internal static partial class DiagnosticSessionFlashbackRecordingSettingsScenarios");
        AssertContains(recordingSettingsText, "internal static async Task<FlashbackRecordingSettingsDeferredPresetState> RunFlashbackRecordingSettingsDeferredAsync(");
        AssertContains(recordingSettingsText, "flashback recording settings deferred preset changed to");
        AssertContains(recordingSettingsText, "VerifyFlashbackRestartRejectedDuringRecordingAsync(");
        AssertContains(recordingSettingsText, "VerifyFlashbackDisableRejectedDuringRecordingAsync(");
        AssertContains(recordingSettingsText, "VerifyFlashbackRecordingSettingsDeferredStillRecordingAsync(");
        AssertContains(recordingSettingsText, "private static async Task VerifyFlashbackRecordingSettingsCommandRejectedDuringRecordingAsync(");
        AssertContains(recordingSettingsText, "RestartFlashback unexpectedly succeeded during recording");
        AssertContains(recordingSettingsText, "SetFlashbackEnabled(false) unexpectedly succeeded during recording");
        AssertContains(recordingSettingsText, "Flashback recording backend did not remain active after mutations");
        AssertContains(recordingSettingsText, "recording counters did not advance after mutation attempts");
        AssertContains(recordingSettingsText, "internal static async Task VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
        AssertContains(recordingSettingsText, "flashback recording settings deferred post-stop buffer verified");
        AssertContains(recordingSettingsText, "private static async Task RestoreFlashbackRecordingSettingsOriginalPresetAsync(");
        AssertContains(recordingSettingsText, "\"SetPreset\"");
        AssertContains(recordingSettingsText, "flashback recording settings deferred preset restored to");
        AssertContains(recordingSettingsText, "selected preset was not restored");
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

        AssertContains(lifecycleText, "internal static class DiagnosticSessionFlashbackLifecycleScenarios");
        AssertContains(lifecycleText, "internal static void RegisterSelectedFlashbackLifecycleScenarioTask(");
        AssertContains(lifecycleText, "scenarioPlan.RunFlashbackLifecycle");
        AssertContains(lifecycleText, "backgroundTasks.AddScenario(");
        AssertContains(lifecycleText, "2,\n            \"flashback-lifecycle-task\",");
        AssertContains(lifecycleText, "actions.Add(\"flashback lifecycle started\")");
        AssertContains(lifecycleText, "internal static async Task RunFlashbackLifecycleAsync(");
        AssertContains(lifecycleText, "flashback lifecycle pause requested");
        AssertContains(lifecycleText, "flashback lifecycle disabled during playback");
        AssertContains(lifecycleText, "ValidateFlashbackLifecycleDisabledAsync(");
        AssertContains(lifecycleText, "flashback lifecycle re-enabled");
        AssertContains(lifecycleText, "ValidateFlashbackLifecycleReenabledAsync(");
        AssertContains(lifecycleText, "private static async Task ValidateFlashbackLifecycleDisabledAsync(");
        AssertContains(lifecycleText, "flashback lifecycle: playback worker still alive after disable");
        AssertContains(lifecycleText, "flashback lifecycle: pending commands remained after disable");
        AssertContains(lifecycleText, "private static async Task ValidateFlashbackLifecycleReenabledAsync(");
        AssertContains(startupText, "DiagnosticSessionFlashbackLifecycleScenarios.RegisterSelectedFlashbackLifecycleScenarioTask(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackLifecycleScenarios;");
        AssertDoesNotContain(startupText, "RunFlashbackLifecycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackLifecycleAsync(");

        return Task.CompletedTask;
    }
}
