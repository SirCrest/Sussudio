using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationDiagnosticsFlashbackExportProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
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

    internal static Task AutomationDiagnosticsFlashbackRecordingProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var flashbackRecordingProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingStartupCacheProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.StartupCache.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingQueuesProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecordingQueues.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingRuntimeProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.Runtime.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingBackendProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.Backend.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingEncoderProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.Encoder.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var flashbackRecording = BuildFlashbackRecordingProjection(captureRuntime, health);");
        AssertContains(snapshotFlatteningText, "var flashbackRecordingFlattening = BuildFlashbackRecordingFlattenedProjection(flashbackRecording);");
        AssertContains(snapshotFlatteningText, "FlashbackEncodingFailed = flashbackRecordingFlattening.EncodingFailed,");
        AssertContains(snapshotFlatteningText, "FlashbackStartupCacheOverBudget = flashbackRecordingFlattening.StartupCache.OverBudget,");
        AssertContains(snapshotFlatteningText, "FlashbackVideoQueueCapacity = flashbackRecordingFlattening.Queues.VideoQueueCapacity,");
        AssertContains(snapshotFlatteningText, "FlashbackGpuQueueLastRejectReason = flashbackRecordingFlattening.Queues.GpuQueueLastRejectReason,");
        AssertContains(snapshotFlatteningText, "FlashbackActive = flashbackRecordingFlattening.Runtime.Active,");
        AssertContains(snapshotFlatteningText, "FlashbackBackendSettingsStale = flashbackRecordingFlattening.Backend.SettingsStale,");
        AssertContains(snapshotFlatteningText, "FlashbackExportVerificationFormat = flashbackRecordingFlattening.Backend.ExportVerificationFormat,");
        AssertContains(snapshotFlatteningText, "EncoderCodecName = flashbackRecordingFlattening.Encoder.CodecName,");
        AssertContains(snapshotFlatteningText, "FlashbackAudioQueueCapacity = flashbackRecordingFlattening.Queues.AudioQueueCapacity,");
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
        AssertContains(flashbackRecordingProjectionText, "var queues = BuildFlashbackRecordingQueuesProjection(health);");
        AssertContains(flashbackRecordingProjectionText, "var runtime = BuildFlashbackRecordingRuntimeProjection(health);");
        AssertContains(flashbackRecordingProjectionText, "var backend = BuildFlashbackRecordingBackendProjection(captureRuntime, health);");
        AssertContains(flashbackRecordingProjectionText, "var encoder = BuildFlashbackRecordingEncoderProjection(health);");
        AssertContains(flashbackRecordingProjectionText, "StartupCache = startupCache,");
        AssertContains(flashbackRecordingProjectionText, "Queues = queues,");
        AssertContains(flashbackRecordingProjectionText, "Runtime = runtime,");
        AssertContains(flashbackRecordingProjectionText, "Backend = backend,");
        AssertContains(flashbackRecordingProjectionText, "Encoder = encoder");
        AssertContains(flashbackRecordingProjectionText, "EncodingFailed = health.FlashbackEncodingFailed,");
        AssertContains(flashbackRecordingProjectionText, "private readonly record struct FlashbackRecordingProjection");
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingFlattenedProjection BuildFlashbackRecordingFlattenedProjection(");
        AssertContains(flashbackRecordingProjectionText, "FlashbackRecordingProjection flashbackRecording");
        AssertContains(flashbackRecordingProjectionText, "EncodingFailed = flashbackRecording.EncodingFailed,");
        AssertContains(flashbackRecordingProjectionText, "StartupCache = BuildFlashbackRecordingStartupCacheFlattenedProjection(flashbackRecording.StartupCache),");
        AssertContains(flashbackRecordingProjectionText, "Queues = BuildFlashbackRecordingQueuesFlattenedProjection(flashbackRecording.Queues),");
        AssertContains(flashbackRecordingProjectionText, "Runtime = BuildFlashbackRecordingRuntimeFlattenedProjection(flashbackRecording.Runtime),");
        AssertContains(flashbackRecordingProjectionText, "Backend = BuildFlashbackRecordingBackendFlattenedProjection(flashbackRecording.Backend),");
        AssertContains(flashbackRecordingProjectionText, "Encoder = BuildFlashbackRecordingEncoderFlattenedProjection(flashbackRecording.Encoder)");
        AssertContains(flashbackRecordingProjectionText, "private readonly record struct FlashbackRecordingFlattenedProjection");
        AssertContains(flashbackRecordingProjectionText, "public FlashbackRecordingStartupCacheFlattenedProjection StartupCache { get; init; }");
        AssertContains(flashbackRecordingProjectionText, "public FlashbackRecordingQueuesFlattenedProjection Queues { get; init; }");
        AssertContains(flashbackRecordingProjectionText, "public FlashbackRecordingRuntimeFlattenedProjection Runtime { get; init; }");
        AssertContains(flashbackRecordingProjectionText, "public FlashbackRecordingBackendFlattenedProjection Backend { get; init; }");
        AssertContains(flashbackRecordingProjectionText, "public FlashbackRecordingEncoderFlattenedProjection Encoder { get; init; }");
        AssertContains(flashbackRecordingProjectionText, "public FlashbackRecordingRuntimeProjection Runtime { get; init; }");
        AssertContains(flashbackRecordingProjectionText, "public FlashbackRecordingBackendProjection Backend { get; init; }");
        AssertContains(flashbackRecordingProjectionText, "public FlashbackRecordingEncoderProjection Encoder { get; init; }");
        AssertDoesNotContain(flashbackRecordingProjectionText, "Active = health.FlashbackActive,");
        AssertDoesNotContain(flashbackRecordingProjectionText, "ExportVerificationFormat = captureRuntime.FlashbackExportVerificationFormat ?? health.FlashbackExportVerificationFormat,");
        AssertDoesNotContain(flashbackRecordingProjectionText, "CodecDowngradeReason = captureRuntime.FlashbackCodecDowngradeReason ?? health.FlashbackCodecDowngradeReason,");
        AssertDoesNotContain(flashbackRecordingProjectionText, "EncoderCodecName = health.EncoderCodecName,");
        AssertDoesNotContain(flashbackRecordingProjectionText, "StartupCacheOverBudget = health.FlashbackStartupCacheOverBudget,");
        AssertDoesNotContain(flashbackRecordingProjectionText, "VideoQueueCapacity = health.FlashbackVideoQueueCapacity,");
        AssertDoesNotContain(flashbackRecordingProjectionText, "GpuQueueLastRejectReason = health.FlashbackGpuQueueLastRejectReason,");
        AssertDoesNotContain(flashbackRecordingProjectionText, "AudioQueueCapacity = health.FlashbackAudioQueueCapacity");
        AssertDoesNotContain(flashbackRecordingProjectionText, "private static FlashbackRecordingStartupCacheProjection BuildFlashbackRecordingStartupCacheProjection(");
        AssertDoesNotContain(flashbackRecordingProjectionText, "private static FlashbackRecordingRuntimeProjection BuildFlashbackRecordingRuntimeProjection(");
        AssertDoesNotContain(flashbackRecordingProjectionText, "private static FlashbackRecordingBackendProjection BuildFlashbackRecordingBackendProjection(");
        AssertDoesNotContain(flashbackRecordingProjectionText, "private static FlashbackRecordingEncoderProjection BuildFlashbackRecordingEncoderProjection(");
        AssertContains(flashbackRecordingStartupCacheProjectionText, "private static FlashbackRecordingStartupCacheProjection BuildFlashbackRecordingStartupCacheProjection(");
        AssertContains(flashbackRecordingStartupCacheProjectionText, "TempDriveFreeBytes = health.FlashbackTempDriveFreeBytes,");
        AssertContains(flashbackRecordingStartupCacheProjectionText, "OverBudget = health.FlashbackStartupCacheOverBudget");
        AssertContains(flashbackRecordingStartupCacheProjectionText, "private readonly record struct FlashbackRecordingStartupCacheProjection");
        AssertContains(flashbackRecordingStartupCacheProjectionText, "private static FlashbackRecordingStartupCacheFlattenedProjection BuildFlashbackRecordingStartupCacheFlattenedProjection(");
        AssertContains(flashbackRecordingStartupCacheProjectionText, "OverBudget = startupCache.OverBudget");
        AssertContains(flashbackRecordingStartupCacheProjectionText, "private readonly record struct FlashbackRecordingStartupCacheFlattenedProjection");
        AssertContains(flashbackRecordingQueuesProjectionText, "private static FlashbackRecordingQueuesProjection BuildFlashbackRecordingQueuesProjection(");
        AssertContains(flashbackRecordingQueuesProjectionText, "VideoQueueCapacity = health.FlashbackVideoQueueCapacity,");
        AssertContains(flashbackRecordingQueuesProjectionText, "GpuQueueLastRejectReason = health.FlashbackGpuQueueLastRejectReason,");
        AssertContains(flashbackRecordingQueuesProjectionText, "AudioQueueCapacity = health.FlashbackAudioQueueCapacity");
        AssertContains(flashbackRecordingQueuesProjectionText, "private readonly record struct FlashbackRecordingQueuesProjection");
        AssertContains(flashbackRecordingQueuesProjectionText, "private static FlashbackRecordingQueuesFlattenedProjection BuildFlashbackRecordingQueuesFlattenedProjection(");
        AssertContains(flashbackRecordingQueuesProjectionText, "VideoQueueCapacity = queues.VideoQueueCapacity,");
        AssertContains(flashbackRecordingQueuesProjectionText, "GpuQueueLastRejectReason = queues.GpuQueueLastRejectReason,");
        AssertContains(flashbackRecordingQueuesProjectionText, "AudioQueueCapacity = queues.AudioQueueCapacity");
        AssertContains(flashbackRecordingQueuesProjectionText, "private readonly record struct FlashbackRecordingQueuesFlattenedProjection");
        AssertContains(flashbackRecordingRuntimeProjectionText, "private static FlashbackRecordingRuntimeProjection BuildFlashbackRecordingRuntimeProjection(");
        AssertContains(flashbackRecordingRuntimeProjectionText, "Active = health.FlashbackActive,");
        AssertContains(flashbackRecordingRuntimeProjectionText, "GpuEncoding = health.FlashbackGpuEncoding");
        AssertContains(flashbackRecordingRuntimeProjectionText, "private readonly record struct FlashbackRecordingRuntimeProjection");
        AssertContains(flashbackRecordingRuntimeProjectionText, "private static FlashbackRecordingRuntimeFlattenedProjection BuildFlashbackRecordingRuntimeFlattenedProjection(");
        AssertContains(flashbackRecordingRuntimeProjectionText, "FlashbackRecordingRuntimeProjection runtime");
        AssertContains(flashbackRecordingRuntimeProjectionText, "Active = runtime.Active,");
        AssertContains(flashbackRecordingRuntimeProjectionText, "GpuEncoding = runtime.GpuEncoding");
        AssertContains(flashbackRecordingRuntimeProjectionText, "private readonly record struct FlashbackRecordingRuntimeFlattenedProjection");
        AssertContains(flashbackRecordingBackendProjectionText, "private static FlashbackRecordingBackendProjection BuildFlashbackRecordingBackendProjection(");
        AssertContains(flashbackRecordingBackendProjectionText, "ExportVerificationFormat = captureRuntime.FlashbackExportVerificationFormat ?? health.FlashbackExportVerificationFormat,");
        AssertContains(flashbackRecordingBackendProjectionText, "CodecDowngradeReason = captureRuntime.FlashbackCodecDowngradeReason ?? health.FlashbackCodecDowngradeReason");
        AssertContains(flashbackRecordingBackendProjectionText, "private readonly record struct FlashbackRecordingBackendProjection");
        AssertContains(flashbackRecordingBackendProjectionText, "private static FlashbackRecordingBackendFlattenedProjection BuildFlashbackRecordingBackendFlattenedProjection(");
        AssertContains(flashbackRecordingBackendProjectionText, "FlashbackRecordingBackendProjection backend");
        AssertContains(flashbackRecordingBackendProjectionText, "SettingsStale = backend.SettingsStale,");
        AssertContains(flashbackRecordingBackendProjectionText, "ExportVerificationFormat = backend.ExportVerificationFormat,");
        AssertContains(flashbackRecordingBackendProjectionText, "CodecDowngradeReason = backend.CodecDowngradeReason");
        AssertContains(flashbackRecordingBackendProjectionText, "private readonly record struct FlashbackRecordingBackendFlattenedProjection");
        AssertContains(flashbackRecordingEncoderProjectionText, "private static FlashbackRecordingEncoderProjection BuildFlashbackRecordingEncoderProjection(");
        AssertContains(flashbackRecordingEncoderProjectionText, "CodecName = health.EncoderCodecName,");
        AssertContains(flashbackRecordingEncoderProjectionText, "FrameRateDenominator = health.EncoderFrameRateDenominator");
        AssertContains(flashbackRecordingEncoderProjectionText, "private readonly record struct FlashbackRecordingEncoderProjection");
        AssertContains(flashbackRecordingEncoderProjectionText, "private static FlashbackRecordingEncoderFlattenedProjection BuildFlashbackRecordingEncoderFlattenedProjection(");
        AssertContains(flashbackRecordingEncoderProjectionText, "FlashbackRecordingEncoderProjection encoder");
        AssertContains(flashbackRecordingEncoderProjectionText, "CodecName = encoder.CodecName,");
        AssertContains(flashbackRecordingEncoderProjectionText, "FrameRateDenominator = encoder.FrameRateDenominator");
        AssertContains(flashbackRecordingEncoderProjectionText, "private readonly record struct FlashbackRecordingEncoderFlattenedProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsFlashbackPlaybackProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var flashbackPlaybackFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackPlayback.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackFlatteningAudioMasterText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackPlayback.AudioMaster.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackFlatteningTimingText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackPlayback.Timing.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackFlatteningDecodeText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackPlayback.Decode.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackFlatteningCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackPlayback.Commands.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackAudioMasterProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.AudioMaster.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackTimingProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.Timing.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackDecodeProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.Decode.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackCommandProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.Commands.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var flashbackPlayback = BuildFlashbackPlaybackProjection(health);");
        AssertContains(snapshotFlatteningText, "var flashbackPlaybackFlattening = BuildFlashbackPlaybackFlattenedProjection(flashbackPlayback);");
        AssertContains(snapshotFlatteningText, "FlashbackPlaybackState = flashbackPlaybackFlattening.State,");
        AssertContains(snapshotFlatteningText, "FlashbackPlaybackAudioMasterFallbacks = flashbackPlaybackFlattening.AudioMaster.Fallbacks,");
        AssertContains(snapshotFlatteningText, "FlashbackPlaybackTargetFps = flashbackPlaybackFlattening.Timing.TargetFps,");
        AssertContains(snapshotFlatteningText, "FlashbackPlaybackMaxDecodePhase = flashbackPlaybackFlattening.Decode.MaxPhase,");
        AssertContains(snapshotFlatteningText, "FlashbackPlaybackLastCommandFailure = flashbackPlaybackFlattening.Commands.LastFailure,");
        AssertContains(flashbackPlaybackFlatteningText, "private static FlashbackPlaybackFlattenedProjection BuildFlashbackPlaybackFlattenedProjection(");
        AssertContains(flashbackPlaybackFlatteningText, "FlashbackPlaybackProjection flashbackPlayback");
        AssertContains(flashbackPlaybackFlatteningText, "State = flashbackPlayback.State,");
        AssertContains(flashbackPlaybackFlatteningText, "AudioMaster = BuildFlashbackPlaybackAudioMasterFlattenedProjection(flashbackPlayback.AudioMaster),");
        AssertContains(flashbackPlaybackFlatteningText, "Timing = BuildFlashbackPlaybackTimingFlattenedProjection(flashbackPlayback.Timing),");
        AssertContains(flashbackPlaybackFlatteningText, "Decode = BuildFlashbackPlaybackDecodeFlattenedProjection(flashbackPlayback.Decode),");
        AssertContains(flashbackPlaybackFlatteningText, "Commands = BuildFlashbackPlaybackCommandFlattenedProjection(flashbackPlayback.Commands)");
        AssertContains(flashbackPlaybackFlatteningText, "private readonly record struct FlashbackPlaybackFlattenedProjection");
        AssertContains(flashbackPlaybackFlatteningText, "public FlashbackPlaybackAudioMasterFlattenedProjection AudioMaster { get; init; }");
        AssertContains(flashbackPlaybackFlatteningText, "public FlashbackPlaybackTimingFlattenedProjection Timing { get; init; }");
        AssertContains(flashbackPlaybackFlatteningText, "public FlashbackPlaybackDecodeFlattenedProjection Decode { get; init; }");
        AssertContains(flashbackPlaybackFlatteningText, "public FlashbackPlaybackCommandFlattenedProjection Commands { get; init; }");
        AssertContains(flashbackPlaybackFlatteningAudioMasterText, "private static FlashbackPlaybackAudioMasterFlattenedProjection BuildFlashbackPlaybackAudioMasterFlattenedProjection(");
        AssertContains(flashbackPlaybackFlatteningAudioMasterText, "Fallbacks = audioMaster.Fallbacks,");
        AssertContains(flashbackPlaybackFlatteningAudioMasterText, "LastFallbackReason = audioMaster.LastFallbackReason,");
        AssertContains(flashbackPlaybackFlatteningAudioMasterText, "private readonly record struct FlashbackPlaybackAudioMasterFlattenedProjection");
        AssertContains(flashbackPlaybackFlatteningTimingText, "private static FlashbackPlaybackTimingFlattenedProjection BuildFlashbackPlaybackTimingFlattenedProjection(");
        AssertContains(flashbackPlaybackFlatteningTimingText, "FlashbackPlaybackTimingProjection timing");
        AssertContains(flashbackPlaybackFlatteningTimingText, "TargetFps = timing.TargetFps,");
        AssertContains(flashbackPlaybackFlatteningTimingText, "PtsCadenceMismatchCount = timing.PtsCadenceMismatchCount,");
        AssertContains(flashbackPlaybackFlatteningTimingText, "AvDriftMs = timing.AvDriftMs");
        AssertContains(flashbackPlaybackFlatteningTimingText, "private readonly record struct FlashbackPlaybackTimingFlattenedProjection");
        AssertContains(flashbackPlaybackFlatteningDecodeText, "private static FlashbackPlaybackDecodeFlattenedProjection BuildFlashbackPlaybackDecodeFlattenedProjection(");
        AssertContains(flashbackPlaybackFlatteningDecodeText, "MaxPhase = decode.MaxPhase,");
        AssertContains(flashbackPlaybackFlatteningDecodeText, "MaxPositionMs = decode.MaxPositionMs");
        AssertContains(flashbackPlaybackFlatteningDecodeText, "private readonly record struct FlashbackPlaybackDecodeFlattenedProjection");
        AssertContains(flashbackPlaybackFlatteningCommandsText, "private static FlashbackPlaybackCommandFlattenedProjection BuildFlashbackPlaybackCommandFlattenedProjection(");
        AssertContains(flashbackPlaybackFlatteningCommandsText, "LastFailure = commands.LastFailure");
        AssertContains(flashbackPlaybackFlatteningCommandsText, "private readonly record struct FlashbackPlaybackCommandFlattenedProjection");
        AssertDoesNotContain(flashbackPlaybackFlatteningText, "AudioMasterFallbacks = flashbackPlayback.AudioMaster.Fallbacks,");
        AssertDoesNotContain(flashbackPlaybackFlatteningText, "MaxDecodePhase = flashbackPlayback.Decode.MaxPhase,");
        AssertDoesNotContain(flashbackPlaybackFlatteningText, "LastCommandFailure = flashbackPlayback.Commands.LastFailure");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackState = health.FlashbackPlaybackState,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackTargetFps = health.FlashbackPlaybackTargetFps,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackLastCommandFailure = health.FlashbackPlaybackLastCommandFailure,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackState = flashbackPlayback.State,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackTargetFps = flashbackPlayback.TargetFps,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackMaxDecodePhase = flashbackPlayback.Decode.MaxPhase,");
        AssertDoesNotContain(snapshotFlatteningText, "FlashbackPlaybackLastCommandFailure = flashbackPlayback.Commands.LastFailure,");

        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackProjection BuildFlashbackPlaybackProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackProjectionText, "var audioMaster = BuildFlashbackPlaybackAudioMasterProjection(health);");
        AssertContains(flashbackPlaybackProjectionText, "var timing = BuildFlashbackPlaybackTimingProjection(health);");
        AssertContains(flashbackPlaybackProjectionText, "var decode = BuildFlashbackPlaybackDecodeProjection(health);");
        AssertContains(flashbackPlaybackProjectionText, "var commands = BuildFlashbackPlaybackCommandProjection(health);");
        AssertContains(flashbackPlaybackProjectionText, "State = health.FlashbackPlaybackState,");
        AssertContains(flashbackPlaybackProjectionText, "AudioMaster = audioMaster,");
        AssertContains(flashbackPlaybackProjectionText, "Timing = timing,");
        AssertContains(flashbackPlaybackProjectionText, "Decode = decode,");
        AssertContains(flashbackPlaybackProjectionText, "Commands = commands");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "TargetFps = health.FlashbackPlaybackTargetFps,");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "AudioMasterFallbacks = audioMaster.Fallbacks,");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "AudioMasterFallbacks = health.FlashbackPlaybackAudioMasterFallbacks,");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "PtsCadenceMismatchCount = health.FlashbackPlaybackPtsCadenceMismatchCount,");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "AvDriftMs = health.FlashbackAvDriftMs");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "MaxDecodePhase = decode.MaxPhase,");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "MaxDecodePhase = health.FlashbackPlaybackMaxDecodePhase,");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "LastCommandFailure = commands.LastFailure");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "LastCommandFailure = health.FlashbackPlaybackLastCommandFailure");
        AssertContains(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackProjection");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "private static FlashbackPlaybackAudioMasterProjection BuildFlashbackPlaybackAudioMasterProjection(");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "private static FlashbackPlaybackTimingProjection BuildFlashbackPlaybackTimingProjection(");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "private static FlashbackPlaybackDecodeProjection BuildFlashbackPlaybackDecodeProjection(");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "private static FlashbackPlaybackCommandProjection BuildFlashbackPlaybackCommandProjection(");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackAudioMasterProjection");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackTimingProjection");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackDecodeProjection");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackCommandProjection");

        AssertContains(flashbackPlaybackAudioMasterProjectionText, "private static FlashbackPlaybackAudioMasterProjection BuildFlashbackPlaybackAudioMasterProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackAudioMasterProjectionText, "Fallbacks = health.FlashbackPlaybackAudioMasterFallbacks,");
        AssertContains(flashbackPlaybackAudioMasterProjectionText, "LastFallbackReason = health.FlashbackPlaybackAudioMasterLastFallbackReason,");
        AssertContains(flashbackPlaybackAudioMasterProjectionText, "private readonly record struct FlashbackPlaybackAudioMasterProjection");

        AssertContains(flashbackPlaybackTimingProjectionText, "private static FlashbackPlaybackTimingProjection BuildFlashbackPlaybackTimingProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackTimingProjectionText, "TargetFps = health.FlashbackPlaybackTargetFps,");
        AssertContains(flashbackPlaybackTimingProjectionText, "PtsCadenceMismatchCount = health.FlashbackPlaybackPtsCadenceMismatchCount,");
        AssertContains(flashbackPlaybackTimingProjectionText, "AvDriftMs = health.FlashbackAvDriftMs");
        AssertContains(flashbackPlaybackTimingProjectionText, "private readonly record struct FlashbackPlaybackTimingProjection");

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
