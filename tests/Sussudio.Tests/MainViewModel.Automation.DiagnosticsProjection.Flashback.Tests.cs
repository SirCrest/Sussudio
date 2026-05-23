using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationDiagnosticsFlashbackExportProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
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

        AssertContains(flashbackExportProjectionText, "private static FlashbackExportFlattenedProjection BuildFlashbackExportFlattenedProjection(");
        AssertContains(flashbackExportProjectionText, "Active = flashbackExport.Active,");
        AssertContains(flashbackExportProjectionText, "Percent = flashbackExport.Percent,");
        AssertContains(flashbackExportProjectionText, "LastForceRotateFallbackSegments = flashbackExport.LastForceRotateFallbackSegments,");
        AssertContains(flashbackExportProjectionText, "LastExportId = lastResult.LastExportId,");
        AssertContains(flashbackExportProjectionText, "LastExportMessage = lastResult.LastExportMessage");
        AssertContains(flashbackExportProjectionText, "private readonly record struct FlashbackExportFlattenedProjection");

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
        var flashbackRecordingQueuesProjectionText = flashbackRecordingProjectionText;

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
        AssertDoesNotContain(flashbackRecordingProjectionText, "StartupCacheOverBudget = health.FlashbackStartupCacheOverBudget,");
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingStartupCacheProjection BuildFlashbackRecordingStartupCacheProjection(");
        AssertContains(flashbackRecordingProjectionText, "TempDriveFreeBytes = health.FlashbackTempDriveFreeBytes,");
        AssertContains(flashbackRecordingProjectionText, "OverBudget = health.FlashbackStartupCacheOverBudget");
        AssertContains(flashbackRecordingProjectionText, "private readonly record struct FlashbackRecordingStartupCacheProjection");
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingStartupCacheFlattenedProjection BuildFlashbackRecordingStartupCacheFlattenedProjection(");
        AssertContains(flashbackRecordingProjectionText, "OverBudget = startupCache.OverBudget");
        AssertContains(flashbackRecordingProjectionText, "private readonly record struct FlashbackRecordingStartupCacheFlattenedProjection");
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
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingRuntimeProjection BuildFlashbackRecordingRuntimeProjection(");
        AssertContains(flashbackRecordingProjectionText, "Active = health.FlashbackActive,");
        AssertContains(flashbackRecordingProjectionText, "GpuEncoding = health.FlashbackGpuEncoding");
        AssertContains(flashbackRecordingProjectionText, "private readonly record struct FlashbackRecordingRuntimeProjection");
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingRuntimeFlattenedProjection BuildFlashbackRecordingRuntimeFlattenedProjection(");
        AssertContains(flashbackRecordingProjectionText, "FlashbackRecordingRuntimeProjection runtime");
        AssertContains(flashbackRecordingProjectionText, "Active = runtime.Active,");
        AssertContains(flashbackRecordingProjectionText, "GpuEncoding = runtime.GpuEncoding");
        AssertContains(flashbackRecordingProjectionText, "private readonly record struct FlashbackRecordingRuntimeFlattenedProjection");
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingBackendProjection BuildFlashbackRecordingBackendProjection(");
        AssertContains(flashbackRecordingProjectionText, "ExportVerificationFormat = captureRuntime.FlashbackExportVerificationFormat ?? health.FlashbackExportVerificationFormat,");
        AssertContains(flashbackRecordingProjectionText, "CodecDowngradeReason = captureRuntime.FlashbackCodecDowngradeReason ?? health.FlashbackCodecDowngradeReason");
        AssertContains(flashbackRecordingProjectionText, "private readonly record struct FlashbackRecordingBackendProjection");
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingBackendFlattenedProjection BuildFlashbackRecordingBackendFlattenedProjection(");
        AssertContains(flashbackRecordingProjectionText, "FlashbackRecordingBackendProjection backend");
        AssertContains(flashbackRecordingProjectionText, "SettingsStale = backend.SettingsStale,");
        AssertContains(flashbackRecordingProjectionText, "ExportVerificationFormat = backend.ExportVerificationFormat,");
        AssertContains(flashbackRecordingProjectionText, "CodecDowngradeReason = backend.CodecDowngradeReason");
        AssertContains(flashbackRecordingProjectionText, "private readonly record struct FlashbackRecordingBackendFlattenedProjection");
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingEncoderProjection BuildFlashbackRecordingEncoderProjection(");
        AssertContains(flashbackRecordingProjectionText, "CodecName = health.EncoderCodecName,");
        AssertContains(flashbackRecordingProjectionText, "FrameRateDenominator = health.EncoderFrameRateDenominator");
        AssertContains(flashbackRecordingProjectionText, "private readonly record struct FlashbackRecordingEncoderProjection");
        AssertContains(flashbackRecordingProjectionText, "private static FlashbackRecordingEncoderFlattenedProjection BuildFlashbackRecordingEncoderFlattenedProjection(");
        AssertContains(flashbackRecordingProjectionText, "FlashbackRecordingEncoderProjection encoder");
        AssertContains(flashbackRecordingProjectionText, "CodecName = encoder.CodecName,");
        AssertContains(flashbackRecordingProjectionText, "FrameRateDenominator = encoder.FrameRateDenominator");
        AssertContains(flashbackRecordingProjectionText, "private readonly record struct FlashbackRecordingEncoderFlattenedProjection");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsFlashbackPlaybackProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var flashbackPlaybackProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var flashbackPlayback = BuildFlashbackPlaybackProjection(health);");
        AssertContains(snapshotFlatteningText, "var flashbackPlaybackFlattening = BuildFlashbackPlaybackFlattenedProjection(flashbackPlayback);");
        AssertContains(snapshotFlatteningText, "FlashbackPlaybackState = flashbackPlaybackFlattening.State,");
        AssertContains(snapshotFlatteningText, "FlashbackPlaybackAudioMasterFallbacks = flashbackPlaybackFlattening.AudioMaster.Fallbacks,");
        AssertContains(snapshotFlatteningText, "FlashbackPlaybackTargetFps = flashbackPlaybackFlattening.Timing.TargetFps,");
        AssertContains(snapshotFlatteningText, "FlashbackPlaybackMaxDecodePhase = flashbackPlaybackFlattening.Decode.MaxPhase,");
        AssertContains(snapshotFlatteningText, "FlashbackPlaybackLastCommandFailure = flashbackPlaybackFlattening.Commands.LastFailure,");
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
        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackFlattenedProjection BuildFlashbackPlaybackFlattenedProjection(");
        AssertContains(flashbackPlaybackProjectionText, "FlashbackPlaybackProjection flashbackPlayback");
        AssertContains(flashbackPlaybackProjectionText, "State = flashbackPlayback.State,");
        AssertContains(flashbackPlaybackProjectionText, "AudioMaster = BuildFlashbackPlaybackAudioMasterFlattenedProjection(flashbackPlayback.AudioMaster),");
        AssertContains(flashbackPlaybackProjectionText, "Timing = BuildFlashbackPlaybackTimingFlattenedProjection(flashbackPlayback.Timing),");
        AssertContains(flashbackPlaybackProjectionText, "Decode = BuildFlashbackPlaybackDecodeFlattenedProjection(flashbackPlayback.Decode),");
        AssertContains(flashbackPlaybackProjectionText, "Commands = BuildFlashbackPlaybackCommandFlattenedProjection(flashbackPlayback.Commands)");
        AssertContains(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackFlattenedProjection");
        AssertContains(flashbackPlaybackProjectionText, "public FlashbackPlaybackAudioMasterFlattenedProjection AudioMaster { get; init; }");
        AssertContains(flashbackPlaybackProjectionText, "public FlashbackPlaybackTimingFlattenedProjection Timing { get; init; }");
        AssertContains(flashbackPlaybackProjectionText, "public FlashbackPlaybackDecodeFlattenedProjection Decode { get; init; }");
        AssertContains(flashbackPlaybackProjectionText, "public FlashbackPlaybackCommandFlattenedProjection Commands { get; init; }");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "AudioMasterFallbacks = flashbackPlayback.AudioMaster.Fallbacks,");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "MaxDecodePhase = flashbackPlayback.Decode.MaxPhase,");
        AssertDoesNotContain(flashbackPlaybackProjectionText, "LastCommandFailure = flashbackPlayback.Commands.LastFailure");
        AssertContains(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackProjection");
        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackAudioMasterProjection BuildFlashbackPlaybackAudioMasterProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackProjectionText, "Fallbacks = health.FlashbackPlaybackAudioMasterFallbacks,");
        AssertContains(flashbackPlaybackProjectionText, "LastFallbackReason = health.FlashbackPlaybackAudioMasterLastFallbackReason,");
        AssertContains(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackAudioMasterProjection");
        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackAudioMasterFlattenedProjection BuildFlashbackPlaybackAudioMasterFlattenedProjection(");
        AssertContains(flashbackPlaybackProjectionText, "Fallbacks = audioMaster.Fallbacks,");
        AssertContains(flashbackPlaybackProjectionText, "LastFallbackReason = audioMaster.LastFallbackReason,");
        AssertContains(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackAudioMasterFlattenedProjection");

        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackTimingProjection BuildFlashbackPlaybackTimingProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackProjectionText, "TargetFps = health.FlashbackPlaybackTargetFps,");
        AssertContains(flashbackPlaybackProjectionText, "PtsCadenceMismatchCount = health.FlashbackPlaybackPtsCadenceMismatchCount,");
        AssertContains(flashbackPlaybackProjectionText, "AvDriftMs = health.FlashbackAvDriftMs");
        AssertContains(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackTimingProjection");
        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackTimingFlattenedProjection BuildFlashbackPlaybackTimingFlattenedProjection(");
        AssertContains(flashbackPlaybackProjectionText, "FlashbackPlaybackTimingProjection timing");
        AssertContains(flashbackPlaybackProjectionText, "TargetFps = timing.TargetFps,");
        AssertContains(flashbackPlaybackProjectionText, "PtsCadenceMismatchCount = timing.PtsCadenceMismatchCount,");
        AssertContains(flashbackPlaybackProjectionText, "AvDriftMs = timing.AvDriftMs");
        AssertContains(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackTimingFlattenedProjection");

        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackDecodeProjection BuildFlashbackPlaybackDecodeProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackProjectionText, "SeekForwardDecodeCapHits = health.FlashbackPlaybackSeekForwardDecodeCapHits,");
        AssertContains(flashbackPlaybackProjectionText, "MaxPhase = health.FlashbackPlaybackMaxDecodePhase,");
        AssertContains(flashbackPlaybackProjectionText, "MaxPositionMs = health.FlashbackPlaybackMaxDecodePositionMs");
        AssertContains(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackDecodeProjection");
        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackDecodeFlattenedProjection BuildFlashbackPlaybackDecodeFlattenedProjection(");
        AssertContains(flashbackPlaybackProjectionText, "MaxPhase = decode.MaxPhase,");
        AssertContains(flashbackPlaybackProjectionText, "MaxPositionMs = decode.MaxPositionMs");
        AssertContains(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackDecodeFlattenedProjection");

        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackCommandProjection BuildFlashbackPlaybackCommandProjection(CaptureHealthSnapshot health)");
        AssertContains(flashbackPlaybackProjectionText, "ThreadAlive = health.FlashbackPlaybackThreadAlive,");
        AssertContains(flashbackPlaybackProjectionText, "LastFailure = health.FlashbackPlaybackLastCommandFailure");
        AssertContains(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackCommandProjection");
        AssertContains(flashbackPlaybackProjectionText, "private static FlashbackPlaybackCommandFlattenedProjection BuildFlashbackPlaybackCommandFlattenedProjection(");
        AssertContains(flashbackPlaybackProjectionText, "LastFailure = commands.LastFailure");
        AssertContains(flashbackPlaybackProjectionText, "private readonly record struct FlashbackPlaybackCommandFlattenedProjection");

        return Task.CompletedTask;
    }
}
