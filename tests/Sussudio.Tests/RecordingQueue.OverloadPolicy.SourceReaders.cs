// Source readers for recording sink queue limits, drops, and latency accounting tests.
static partial class Program
{
    private readonly record struct RecordingQueueOverloadPolicySources(
        string LibAvSource,
        string FlashbackSource,
        string FlashbackBackendSource,
        string FlashbackBufferSource,
        string FlashbackCleanupSource,
        string CaptureServiceSource,
        string CaptureHealthSnapshotRootSource,
        string CaptureSnapshotsSource,
        string UnifiedVideoCaptureSource,
        string RecordingContractsSource);

    private static RecordingQueueOverloadPolicySources ReadRecordingQueueOverloadPolicySources()
    {
        var libAvSource = ReadLibAvRecordingSinkSource();
        var flashbackSource = ReadFlashbackEncoderSinkSource();
        var flashbackBackendSource = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.PreviewDisposal.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.ArtifactCleanup.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.BufferCycle.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.BufferCycle.Lifecycle.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.Startup.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.cs");
        var flashbackBufferSource = ReadFlashbackBufferManagerSource();
        var flashbackCleanupSource = ReadRepoFile("Sussudio/Services/Flashback/FlashbackStartupCacheCleanup.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackStartupSessionCacheBudget.cs");
        var captureServiceSource = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStartContext.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStartFlashback.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStartLibAv.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStartLibAv.VideoCapture.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStartLibAv.AudioInputs.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStopLifecycle.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Cleanup.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackState.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackEnable.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRestart.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackBufferSettings.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRecordingFormat.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackEncoderSettings.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRecording.SessionContext.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRecording.FrameRate.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRecording.SnapshotCompatibility.cs")
            + "\n" + ReadCaptureServiceAudioSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.DeferredCleanup.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.VideoPipelineLifecycle.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Failures.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FailureCleanup.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackBackendFailureCleanup.cs")
            + "\n" + ReadCaptureServicePreviewLifecycleSource()
            + "\n" + ReadCaptureServiceFlashbackOrchestrationSource()
            + "\n" + ReadCaptureServiceRecordingFinalizationSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingRollback.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportOperations.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportBackendSnapshot.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportRangeResolution.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportForceRotate.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportRequestPreparation.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportDiagnostics.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportProgress.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportPlanning.cs");
        var captureHealthSnapshotRootSource = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs");
        var captureSnapshotsSource = captureHealthSnapshotRootSource
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotCaptureCadence.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotMjpeg.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssemblyFields.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotRecording.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotRecordingActiveBackend.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackBackend.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackBackend.Queues.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotSourceTelemetry.cs");
        var unifiedVideoCaptureSource = ReadUnifiedVideoCaptureSource();
        var recordingContractsSource = ReadRepoFile("Sussudio/Services/Recording/RecordingContracts.cs")
            + "\n"
            + ReadRepoFile("Sussudio/Services/Contracts/RecordingContracts.cs");

        return new RecordingQueueOverloadPolicySources(
            libAvSource,
            flashbackSource,
            flashbackBackendSource,
            flashbackBufferSource,
            flashbackCleanupSource,
            captureServiceSource,
            captureHealthSnapshotRootSource,
            captureSnapshotsSource,
            unifiedVideoCaptureSource,
            recordingContractsSource);
    }
}
