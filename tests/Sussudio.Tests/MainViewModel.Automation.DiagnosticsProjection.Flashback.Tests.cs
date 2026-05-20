using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationDiagnosticsFlashbackExportProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var flashbackExportFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackExport.cs")
            .Replace("\r\n", "\n");
        var flashbackExportProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackExport.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var flashbackExport = BuildFlashbackExportProjection(health);");
        AssertContains(snapshotProjectionText, "var flashbackExportLastResult = BuildFlashbackExportLastResultProjection(health);");
        AssertContains(snapshotFlatteningText, "var flashbackExportFlattening = BuildFlashbackExportFlattenedProjection(");
        AssertContains(snapshotFlatteningText, "FlashbackExportActive = flashbackExportFlattening.Active,");
        AssertContains(snapshotFlatteningText, "FlashbackExportPercent = flashbackExportFlattening.Percent,");
        AssertContains(snapshotFlatteningText, "FlashbackExportLastForceRotateFallbackSegments = flashbackExportFlattening.LastForceRotateFallbackSegments,");
        AssertContains(snapshotFlatteningText, "LastExportId = flashbackExportFlattening.LastExportId,");
        AssertContains(snapshotFlatteningText, "LastExportMessage = flashbackExportFlattening.LastExportMessage");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackExportActive = health.FlashbackExportActive,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackExportActive = flashbackExport.Active,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackExportPercent = health.FlashbackExportPercent,");
        AssertDoesNotContain(snapshotFlatteningText, "LastExportId = health.LastExportId,");
        AssertDoesNotContain(snapshotFlatteningText, "LastExportId = flashbackExportLastResult.LastExportId,");
        AssertDoesNotContain(snapshotFlatteningText, "LastExportId = flashbackExport.LastExportId,");

        AssertContains(flashbackExportFlatteningText, "private static FlashbackExportFlattenedProjection BuildFlashbackExportFlattenedProjection(");
        AssertContains(flashbackExportFlatteningText, "Active = flashbackExport.Active,");
        AssertContains(flashbackExportFlatteningText, "Percent = flashbackExport.Percent,");
        AssertContains(flashbackExportFlatteningText, "LastForceRotateFallbackSegments = flashbackExport.LastForceRotateFallbackSegments,");
        AssertContains(flashbackExportFlatteningText, "LastExportId = lastResult.LastExportId,");
        AssertContains(flashbackExportFlatteningText, "LastExportMessage = lastResult.LastExportMessage");
        AssertContains(flashbackExportFlatteningText, "private readonly record struct FlashbackExportFlattenedProjection");

        AssertContains(flashbackExportProjectionText, "private static FlashbackExportProjection BuildFlashbackExportProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackExportProjectionText, "Active = health.FlashbackExportActive,");
        AssertContains(flashbackExportProjectionText, "Percent = health.FlashbackExportPercent,");
        AssertContains(flashbackExportProjectionText, "LastForceRotateFallbackSegments = health.FlashbackExportLastForceRotateFallbackSegments,");
        AssertContains(flashbackExportProjectionText, "private readonly record struct FlashbackExportProjection");
        AssertDoesNotContain(flashbackExportProjectionText, "LastExportId = flashbackExport.LastExportId,");
        AssertContains(flashbackExportProjectionText, "private static FlashbackExportLastResultProjection BuildFlashbackExportLastResultProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackExportProjectionText, "LastExportId = health.LastExportId,");
        AssertContains(flashbackExportProjectionText, "LastExportMessage = health.LastExportMessage");
        AssertContains(flashbackExportProjectionText, "private readonly record struct FlashbackExportLastResultProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsFlashbackRecordingProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackRecording.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingQueuesProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecordingQueues.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var flashbackRecording = BuildFlashbackRecordingProjection(captureRuntime, health);");
        AssertContains(snapshotFlatteningText, "var flashbackRecordingFlattening = BuildFlashbackRecordingFlattenedProjection(flashbackRecording);");
        AssertContains(snapshotFlatteningText, "FlashbackEncodingFailed = flashbackRecordingFlattening.EncodingFailed,");
        AssertContains(snapshotFlatteningText, "FlashbackStartupCacheOverBudget = flashbackRecordingFlattening.StartupCacheOverBudget,");
        AssertContains(snapshotFlatteningText, "FlashbackVideoQueueCapacity = flashbackRecordingFlattening.VideoQueueCapacity,");
        AssertContains(snapshotFlatteningText, "FlashbackGpuQueueLastRejectReason = flashbackRecordingFlattening.GpuQueueLastRejectReason,");
        AssertContains(snapshotFlatteningText, "FlashbackActive = flashbackRecordingFlattening.Active,");
        AssertContains(snapshotFlatteningText, "FlashbackBackendSettingsStale = flashbackRecordingFlattening.BackendSettingsStale,");
        AssertContains(snapshotFlatteningText, "FlashbackExportVerificationFormat = flashbackRecordingFlattening.ExportVerificationFormat,");
        AssertContains(snapshotFlatteningText, "EncoderCodecName = flashbackRecordingFlattening.EncoderCodecName,");
        AssertContains(snapshotFlatteningText, "FlashbackAudioQueueCapacity = flashbackRecordingFlattening.AudioQueueCapacity,");
        AssertContains(flashbackRecordingFlatteningText, "private static FlashbackRecordingFlattenedProjection BuildFlashbackRecordingFlattenedProjection(");
        AssertContains(flashbackRecordingFlatteningText, "FlashbackRecordingProjection flashbackRecording");
        AssertContains(flashbackRecordingFlatteningText, "EncodingFailed = flashbackRecording.EncodingFailed,");
        AssertContains(flashbackRecordingFlatteningText, "StartupCacheOverBudget = flashbackRecording.StartupCache.OverBudget,");
        AssertContains(flashbackRecordingFlatteningText, "VideoQueueCapacity = flashbackRecording.Queues.VideoQueueCapacity,");
        AssertContains(flashbackRecordingFlatteningText, "GpuQueueLastRejectReason = flashbackRecording.Queues.GpuQueueLastRejectReason,");
        AssertContains(flashbackRecordingFlatteningText, "AudioQueueCapacity = flashbackRecording.Queues.AudioQueueCapacity");
        AssertContains(flashbackRecordingFlatteningText, "private readonly record struct FlashbackRecordingFlattenedProjection");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackEncodingFailed = health.FlashbackEncodingFailed,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackVideoQueueCapacity = health.FlashbackVideoQueueCapacity,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackGpuQueueLastRejectReason = health.FlashbackGpuQueueLastRejectReason,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackActive = health.FlashbackActive,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackExportVerificationFormat = captureRuntime.FlashbackExportVerificationFormat ?? health.FlashbackExportVerificationFormat,");
        AssertDoesNotContain(snapshotFlatteningText, "EncoderCodecName = health.EncoderCodecName,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackEncodingFailed = flashbackRecording.EncodingFailed,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackStartupCacheOverBudget = flashbackRecording.StartupCache.OverBudget,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackVideoQueueCapacity = flashbackRecording.Queues.VideoQueueCapacity,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackGpuQueueLastRejectReason = flashbackRecording.Queues.GpuQueueLastRejectReason,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackActive = flashbackRecording.Active,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackAudioQueueCapacity = flashbackRecording.Queues.AudioQueueCapacity,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackStartupCacheOverBudget = flashbackRecording.StartupCacheOverBudget,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackStartupCacheOverBudget = health.FlashbackStartupCacheOverBudget,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackVideoQueueCapacity = flashbackRecording.VideoQueueCapacity,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackGpuQueueLastRejectReason = flashbackRecording.GpuQueueLastRejectReason,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackAudioQueueCapacity = flashbackRecording.AudioQueueCapacity,");

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
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingStartupCacheProjection BuildFlashbackRecordingStartupCacheProjection(");
        AssertContains(flashbackRecordingProjectionText, "TempDriveFreeBytes = health.FlashbackTempDriveFreeBytes,");
        AssertContains(flashbackRecordingProjectionText, "OverBudget = health.FlashbackStartupCacheOverBudget");
        AssertContains(flashbackRecordingProjectionText, "private readonly record struct FlashbackRecordingStartupCacheProjection");
        AssertContains(flashbackRecordingQueuesProjectionText, "private static FlashbackRecordingQueuesProjection BuildFlashbackRecordingQueuesProjection(");
        AssertContains(flashbackRecordingQueuesProjectionText, "VideoQueueCapacity = health.FlashbackVideoQueueCapacity,");
        AssertContains(flashbackRecordingQueuesProjectionText, "GpuQueueLastRejectReason = health.FlashbackGpuQueueLastRejectReason,");
        AssertContains(flashbackRecordingQueuesProjectionText, "AudioQueueCapacity = health.FlashbackAudioQueueCapacity");
        AssertContains(flashbackRecordingQueuesProjectionText, "private readonly record struct FlashbackRecordingQueuesProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsFlashbackPlaybackProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackPlayback.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackAudioMasterProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.AudioMaster.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackDecodeProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.Decode.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackCommandProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.Commands.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var flashbackPlayback = BuildFlashbackPlaybackProjection(health);");
        AssertContains(snapshotFlatteningText, "var flashbackPlaybackFlattening = BuildFlashbackPlaybackFlattenedProjection(flashbackPlayback);");
        AssertContains(snapshotFlatteningText, "FlashbackPlaybackState = flashbackPlaybackFlattening.State,");
        AssertContains(snapshotFlatteningText, "FlashbackPlaybackTargetFps = flashbackPlaybackFlattening.TargetFps,");
        AssertContains(snapshotFlatteningText, "FlashbackPlaybackMaxDecodePhase = flashbackPlaybackFlattening.MaxDecodePhase,");
        AssertContains(snapshotFlatteningText, "FlashbackPlaybackLastCommandFailure = flashbackPlaybackFlattening.LastCommandFailure,");
        AssertContains(flashbackPlaybackFlatteningText, "private static FlashbackPlaybackFlattenedProjection BuildFlashbackPlaybackFlattenedProjection(");
        AssertContains(flashbackPlaybackFlatteningText, "FlashbackPlaybackProjection flashbackPlayback");
        AssertContains(flashbackPlaybackFlatteningText, "State = flashbackPlayback.State,");
        AssertContains(flashbackPlaybackFlatteningText, "AudioMasterFallbacks = flashbackPlayback.AudioMaster.Fallbacks,");
        AssertContains(flashbackPlaybackFlatteningText, "MaxDecodePhase = flashbackPlayback.Decode.MaxPhase,");
        AssertContains(flashbackPlaybackFlatteningText, "LastCommandFailure = flashbackPlayback.Commands.LastFailure");
        AssertContains(flashbackPlaybackFlatteningText, "private readonly record struct FlashbackPlaybackFlattenedProjection");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackState = health.FlashbackPlaybackState,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackTargetFps = health.FlashbackPlaybackTargetFps,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackLastCommandFailure = health.FlashbackPlaybackLastCommandFailure,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackState = flashbackPlayback.State,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackTargetFps = flashbackPlayback.TargetFps,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackMaxDecodePhase = flashbackPlayback.Decode.MaxPhase,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackLastCommandFailure = flashbackPlayback.Commands.LastFailure,");

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
        AssertDoesNotContain(flashbackPlaybackProjectionText, "private static FlashbackPlaybackAudioMasterProjection BuildFlashbackPlaybackAudioMasterProjection(");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "private static FlashbackPlaybackDecodeProjection BuildFlashbackPlaybackDecodeProjection(");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "private static FlashbackPlaybackCommandProjection BuildFlashbackPlaybackCommandProjection(");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackAudioMasterProjection");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackDecodeProjection");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackCommandProjection");

        AssertContains(flashbackPlaybackAudioMasterProjectionText, "private static FlashbackPlaybackAudioMasterProjection BuildFlashbackPlaybackAudioMasterProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackAudioMasterProjectionText, "Fallbacks = health.FlashbackPlaybackAudioMasterFallbacks,");
        AssertContains(flashbackPlaybackAudioMasterProjectionText, "LastFallbackReason = health.FlashbackPlaybackAudioMasterLastFallbackReason,");
        AssertContains(flashbackPlaybackAudioMasterProjectionText, "private readonly record struct FlashbackPlaybackAudioMasterProjection");

        AssertContains(flashbackPlaybackDecodeProjectionText, "private static FlashbackPlaybackDecodeProjection BuildFlashbackPlaybackDecodeProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackDecodeProjectionText, "SeekForwardDecodeCapHits = health.FlashbackPlaybackSeekForwardDecodeCapHits,");
        AssertContains(flashbackPlaybackDecodeProjectionText, "MaxPhase = health.FlashbackPlaybackMaxDecodePhase,");
        AssertContains(flashbackPlaybackDecodeProjectionText, "MaxPositionMs = health.FlashbackPlaybackMaxDecodePositionMs");
        AssertContains(flashbackPlaybackDecodeProjectionText, "private readonly record struct FlashbackPlaybackDecodeProjection");

        AssertContains(flashbackPlaybackCommandProjectionText, "private static FlashbackPlaybackCommandProjection BuildFlashbackPlaybackCommandProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackCommandProjectionText, "ThreadAlive = health.FlashbackPlaybackThreadAlive,");
        AssertContains(flashbackPlaybackCommandProjectionText, "LastFailure = health.FlashbackPlaybackLastCommandFailure");
        AssertContains(flashbackPlaybackCommandProjectionText, "private readonly record struct FlashbackPlaybackCommandProjection");

        return Task.CompletedTask;
    }
}
