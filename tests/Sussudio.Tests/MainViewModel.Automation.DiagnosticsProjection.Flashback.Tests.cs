using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationDiagnosticsFlashbackExportProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var flashbackExportProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackExport.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var flashbackExport = BuildFlashbackExportProjection(health);");
        AssertContains(snapshotProjectionText, "FlashbackExportActive = flashbackExport.Active,");
        AssertContains(snapshotProjectionText, "FlashbackExportPercent = flashbackExport.Percent,");
        AssertContains(snapshotProjectionText, "FlashbackExportLastForceRotateFallbackSegments = flashbackExport.LastForceRotateFallbackSegments,");
        AssertContains(snapshotProjectionText, "LastExportId = flashbackExport.LastExportId,");
        AssertContains(snapshotProjectionText, "LastExportMessage = flashbackExport.LastExportMessage");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackExportActive = health.FlashbackExportActive,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackExportPercent = health.FlashbackExportPercent,");
        AssertDoesNotContain(snapshotProjectionText, "LastExportId = health.LastExportId,");

        AssertContains(flashbackExportProjectionText, "private static FlashbackExportProjection BuildFlashbackExportProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackExportProjectionText, "Active = health.FlashbackExportActive,");
        AssertContains(flashbackExportProjectionText, "Percent = health.FlashbackExportPercent,");
        AssertContains(flashbackExportProjectionText, "LastForceRotateFallbackSegments = health.FlashbackExportLastForceRotateFallbackSegments,");
        AssertContains(flashbackExportProjectionText, "LastExportId = health.LastExportId,");
        AssertContains(flashbackExportProjectionText, "private readonly record struct FlashbackExportProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsFlashbackRecordingProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingStartupCacheProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecordingStartupCache.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingQueuesProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecordingQueues.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var flashbackRecording = BuildFlashbackRecordingProjection(captureRuntime, health);");
        AssertContains(snapshotProjectionText, "FlashbackEncodingFailed = flashbackRecording.EncodingFailed,");
        AssertContains(snapshotProjectionText, "FlashbackStartupCacheOverBudget = flashbackRecording.StartupCache.OverBudget,");
        AssertContains(snapshotProjectionText, "FlashbackVideoQueueCapacity = flashbackRecording.Queues.VideoQueueCapacity,");
        AssertContains(snapshotProjectionText, "FlashbackGpuQueueLastRejectReason = flashbackRecording.Queues.GpuQueueLastRejectReason,");
        AssertContains(snapshotProjectionText, "FlashbackActive = flashbackRecording.Active,");
        AssertContains(snapshotProjectionText, "FlashbackBackendSettingsStale = flashbackRecording.BackendSettingsStale,");
        AssertContains(snapshotProjectionText, "FlashbackExportVerificationFormat = flashbackRecording.ExportVerificationFormat,");
        AssertContains(snapshotProjectionText, "EncoderCodecName = flashbackRecording.EncoderCodecName,");
        AssertContains(snapshotProjectionText, "FlashbackAudioQueueCapacity = flashbackRecording.Queues.AudioQueueCapacity,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackEncodingFailed = health.FlashbackEncodingFailed,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackVideoQueueCapacity = health.FlashbackVideoQueueCapacity,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackGpuQueueLastRejectReason = health.FlashbackGpuQueueLastRejectReason,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackActive = health.FlashbackActive,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackExportVerificationFormat = captureRuntime.FlashbackExportVerificationFormat ?? health.FlashbackExportVerificationFormat,");
        AssertDoesNotContain(snapshotProjectionText, "EncoderCodecName = health.EncoderCodecName,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackStartupCacheOverBudget = flashbackRecording.StartupCacheOverBudget,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackStartupCacheOverBudget = health.FlashbackStartupCacheOverBudget,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackVideoQueueCapacity = flashbackRecording.VideoQueueCapacity,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackGpuQueueLastRejectReason = flashbackRecording.GpuQueueLastRejectReason,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackAudioQueueCapacity = flashbackRecording.AudioQueueCapacity,");

        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingProjection BuildFlashbackRecordingProjection(");
        AssertContains(flashbackRecordingProjectionText, "CaptureRuntimeSnapshot captureRuntime,");
        AssertContains(flashbackRecordingProjectionText, "var startupCache = BuildFlashbackRecordingStartupCacheProjection(health);");
        AssertContains(flashbackRecordingProjectionText, "StartupCache = startupCache,");
        AssertContains(flashbackRecordingProjectionText, "var queues = BuildFlashbackRecordingQueuesProjection(health);");
        AssertContains(flashbackRecordingProjectionText, "Queues = queues,");
        AssertContains(flashbackRecordingProjectionText, "EncodingFailed = health.FlashbackEncodingFailed,");
        AssertContains(flashbackRecordingProjectionText, "ExportVerificationFormat = captureRuntime.FlashbackExportVerificationFormat ?? health.FlashbackExportVerificationFormat,");
        AssertContains(flashbackRecordingProjectionText, "CodecDowngradeReason = captureRuntime.FlashbackCodecDowngradeReason ?? health.FlashbackCodecDowngradeReason,");
        AssertContains(flashbackRecordingProjectionText, "private readonly record struct FlashbackRecordingProjection");
        AssertDoesNotContain(flashbackRecordingProjectionText, "StartupCacheOverBudget = health.FlashbackStartupCacheOverBudget,");
        AssertDoesNotContain(flashbackRecordingProjectionText, "VideoQueueCapacity = health.FlashbackVideoQueueCapacity,");
        AssertDoesNotContain(flashbackRecordingProjectionText, "GpuQueueLastRejectReason = health.FlashbackGpuQueueLastRejectReason,");
        AssertDoesNotContain(flashbackRecordingProjectionText, "AudioQueueCapacity = health.FlashbackAudioQueueCapacity");
        AssertContains(flashbackRecordingStartupCacheProjectionText, "private static FlashbackRecordingStartupCacheProjection BuildFlashbackRecordingStartupCacheProjection(");
        AssertContains(flashbackRecordingStartupCacheProjectionText, "TempDriveFreeBytes = health.FlashbackTempDriveFreeBytes,");
        AssertContains(flashbackRecordingStartupCacheProjectionText, "OverBudget = health.FlashbackStartupCacheOverBudget");
        AssertContains(flashbackRecordingStartupCacheProjectionText, "private readonly record struct FlashbackRecordingStartupCacheProjection");
        AssertContains(flashbackRecordingQueuesProjectionText, "private static FlashbackRecordingQueuesProjection BuildFlashbackRecordingQueuesProjection(");
        AssertContains(flashbackRecordingQueuesProjectionText, "VideoQueueCapacity = health.FlashbackVideoQueueCapacity,");
        AssertContains(flashbackRecordingQueuesProjectionText, "GpuQueueLastRejectReason = health.FlashbackGpuQueueLastRejectReason,");
        AssertContains(flashbackRecordingQueuesProjectionText, "AudioQueueCapacity = health.FlashbackAudioQueueCapacity");
        AssertContains(flashbackRecordingQueuesProjectionText, "private readonly record struct FlashbackRecordingQueuesProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsFlashbackPlaybackProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackAudioMasterProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlaybackAudioMaster.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackDecodeProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlaybackDecode.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackCommandsProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlaybackCommands.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var flashbackPlayback = BuildFlashbackPlaybackProjection(health);");
        AssertContains(snapshotProjectionText, "FlashbackPlaybackState = flashbackPlayback.State,");
        AssertContains(snapshotProjectionText, "FlashbackPlaybackTargetFps = flashbackPlayback.TargetFps,");
        AssertContains(snapshotProjectionText, "FlashbackPlaybackMaxDecodePhase = flashbackPlayback.Decode.MaxPhase,");
        AssertContains(snapshotProjectionText, "FlashbackPlaybackLastCommandFailure = flashbackPlayback.Commands.LastFailure,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackPlaybackState = health.FlashbackPlaybackState,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackPlaybackTargetFps = health.FlashbackPlaybackTargetFps,");
        AssertDoesNotContain(snapshotProjectionText, "FlashbackPlaybackLastCommandFailure = health.FlashbackPlaybackLastCommandFailure,");

        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackProjection BuildFlashbackPlaybackProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackProjectionText, "var audioMaster = BuildFlashbackPlaybackAudioMasterProjection(health);");
        AssertContains(flashbackPlaybackProjectionText, "var decode = BuildFlashbackPlaybackDecodeProjection(health);");
        AssertContains(flashbackPlaybackProjectionText, "var commands = BuildFlashbackPlaybackCommandProjection(health);");
        AssertContains(flashbackPlaybackProjectionText, "State = health.FlashbackPlaybackState,");
        AssertContains(flashbackPlaybackProjectionText, "AudioMaster = audioMaster,");
        AssertContains(flashbackPlaybackProjectionText, "TargetFps = health.FlashbackPlaybackTargetFps,");
        AssertContains(flashbackPlaybackProjectionText, "Decode = decode,");
        AssertContains(flashbackPlaybackProjectionText, "Commands = commands");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "AudioMasterFallbacks = audioMaster.Fallbacks,");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "AudioMasterFallbacks = health.FlashbackPlaybackAudioMasterFallbacks,");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "MaxDecodePhase = decode.MaxPhase,");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "MaxDecodePhase = health.FlashbackPlaybackMaxDecodePhase,");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "LastCommandFailure = commands.LastFailure");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "LastCommandFailure = health.FlashbackPlaybackLastCommandFailure");
        AssertContains(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackProjection");

        AssertContains(flashbackPlaybackAudioMasterProjectionText, "private static FlashbackPlaybackAudioMasterProjection BuildFlashbackPlaybackAudioMasterProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackAudioMasterProjectionText, "Fallbacks = health.FlashbackPlaybackAudioMasterFallbacks,");
        AssertContains(flashbackPlaybackAudioMasterProjectionText, "LastFallbackReason = health.FlashbackPlaybackAudioMasterLastFallbackReason,");
        AssertContains(flashbackPlaybackAudioMasterProjectionText, "private readonly record struct FlashbackPlaybackAudioMasterProjection");

        AssertContains(flashbackPlaybackDecodeProjectionText, "private static FlashbackPlaybackDecodeProjection BuildFlashbackPlaybackDecodeProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackDecodeProjectionText, "SeekForwardDecodeCapHits = health.FlashbackPlaybackSeekForwardDecodeCapHits,");
        AssertContains(flashbackPlaybackDecodeProjectionText, "MaxPhase = health.FlashbackPlaybackMaxDecodePhase,");
        AssertContains(flashbackPlaybackDecodeProjectionText, "MaxPositionMs = health.FlashbackPlaybackMaxDecodePositionMs");
        AssertContains(flashbackPlaybackDecodeProjectionText, "private readonly record struct FlashbackPlaybackDecodeProjection");

        AssertContains(flashbackPlaybackCommandsProjectionText, "private static FlashbackPlaybackCommandProjection BuildFlashbackPlaybackCommandProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackCommandsProjectionText, "ThreadAlive = health.FlashbackPlaybackThreadAlive,");
        AssertContains(flashbackPlaybackCommandsProjectionText, "LastFailure = health.FlashbackPlaybackLastCommandFailure");
        AssertContains(flashbackPlaybackCommandsProjectionText, "private readonly record struct FlashbackPlaybackCommandProjection");

        return Task.CompletedTask;
    }
}
