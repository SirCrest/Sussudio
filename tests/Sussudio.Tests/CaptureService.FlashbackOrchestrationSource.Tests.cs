using System.IO;
using System.Linq;

static partial class Program
{
    private static readonly string[] CaptureServiceFlashbackOrchestrationFiles =
    {
        "Sussudio/Services/Capture/CaptureService.FlashbackState.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackEnable.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackRestart.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackAudioInputs.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackPreviewBackend.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackBufferCycle.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackBufferSettings.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackRecordingFormat.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackEncoderSettings.cs"
    };

    private static readonly string[] CaptureServiceRecordingFinalizationFiles =
    {
        "Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashbackBackend.cs",
        "Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvBackend.cs",
        "Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvPreviewRestore.cs",
        "Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashback.cs",
        "Sussudio/Services/Capture/CaptureService.RecordingOutcomeState.cs"
    };

    private static readonly string[] CaptureServicePreviewLifecycleFiles =
    {
        "Sussudio/Services/Capture/CaptureService.PreviewStart.cs",
        "Sussudio/Services/Capture/CaptureService.PreviewAudioGraph.cs",
        "Sussudio/Services/Capture/CaptureService.PreviewStop.cs",
        "Sussudio/Services/Capture/CaptureService.PreviewReuse.cs",
        "Sussudio/Services/Capture/CaptureService.PreviewDisposal.cs",
        "Sussudio/Services/Capture/CaptureVideoPipelineResources.cs",
        "Sussudio/Services/Capture/CaptureService.VideoPipelineLifecycle.cs"
    };

    private static readonly string[] CaptureServiceRecordingIntegrityFiles =
    {
        "Sussudio/Services/Capture/CaptureService.RecordingIntegrity.cs",
        "Sussudio/Services/Capture/CaptureService.RecordingIntegrity.Summary.cs",
        "Sussudio/Services/Capture/CaptureService.RecordingIntegrity.Counters.cs",
        "Sussudio/Services/Capture/CaptureService.RecordingIntegrity.Audio.cs"
    };

    private static string ReadCaptureServiceFlashbackOrchestrationSource()
        => string.Join(
            "\n",
            CaptureServiceFlashbackOrchestrationFiles.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));

    private static string ReadCaptureServiceRecordingFinalizationSource()
        => string.Join(
            "\n",
            CaptureServiceRecordingFinalizationFiles.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));

    private static string ReadCaptureServicePreviewLifecycleSource()
        => string.Join(
            "\n",
            CaptureServicePreviewLifecycleFiles.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));

    private static string ReadCaptureServiceRecordingIntegritySource()
        => string.Join(
            "\n",
            CaptureServiceRecordingIntegrityFiles.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));

    private static string ReadCaptureServiceFlashbackOrchestrationCodeWithoutCommentsOrStrings()
        => string.Join(
            "\n",
            CaptureServiceFlashbackOrchestrationFiles.Select(ReadRepoCodeWithoutCommentsOrStrings));

    private static string ReadCaptureServicePreviewLifecycleCodeWithoutCommentsOrStrings()
        => string.Join(
            "\n",
            CaptureServicePreviewLifecycleFiles.Select(ReadRepoCodeWithoutCommentsOrStrings));

    internal static Task CaptureService_FlashbackOrchestrationLivesInFocusedPartials()
    {
        var flashbackStateText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackState.cs");
        var flashbackEnableText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackEnable.cs");
        var flashbackRestartText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRestart.cs");
        var audioInputsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackAudioInputs.cs");
        var previewBackendText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackPreviewBackend.cs");
        var bufferCycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackBufferCycle.cs");
        var bufferSettingsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackBufferSettings.cs");
        var recordingFormatText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRecordingFormat.cs");
        var encoderSettingsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackEncoderSettings.cs");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");
        var backendResourcesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.PreviewDisposal.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.ArtifactCleanup.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.BufferCycle.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.BufferCycle.Lifecycle.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.Startup.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.Startup.Rollback.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.RecordingFinalize.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.Producers.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.cs");

        AssertContains(flashbackStateText, "public bool IsFlashbackActive => _flashbackBackend.Sink != null;");
        AssertContains(flashbackStateText, "internal IReadOnlyList<FlashbackSegmentInfo> GetFlashbackSegments()");
        AssertContains(flashbackEnableText, "public Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(flashbackEnableText, "FLASHBACK_ENABLE_DEFERRED");
        AssertContains(flashbackRestartText, "public Task RestartFlashbackAsync(");
        AssertContains(flashbackRestartText, "private async Task RestartFlashbackCoreAsync(");
        AssertContains(flashbackRestartText, "UpdateEncodingSettings(settings);");
        AssertContains(audioInputsText, "private async Task EnsureFlashbackAudioInputsAsync(");
        AssertContains(previewBackendText, "private async Task EnsureFlashbackPreviewBackendAsync(");
        AssertContains(previewBackendText, "private async Task DisposeFlashbackPreviewBackendAsync(");
        AssertContains(previewBackendText, "private async Task DisposeFlashbackPreviewBackendCoreAsync(");
        AssertContains(previewBackendText, "CreateFlashbackPreviewBackendDisposalRequest(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackPreviewBackendDisposal.cs")),
            "old Flashback preview backend disposal partial removed");
        AssertContains(backendResourcesText, "internal readonly record struct FlashbackPreviewBackendDisposalRequest(");
        AssertContains(backendResourcesText, "public async Task DisposePreviewBackendAsync(");
        AssertContains(bufferCycleText, "private async Task CycleFlashbackBufferAsync(");
        AssertContains(bufferCycleText, "_flashbackBackend.CycleSinkOnlyAsync(");
        AssertContains(bufferSettingsText, "public Task UpdateFlashbackSettingsAsync(");
        AssertContains(bufferSettingsText, "_currentSettings.FlashbackBufferMinutes = bufferMinutes;");
        AssertContains(bufferSettingsText, "_flashbackBackend.PlaybackController.GpuDecodeEnabled = gpuDecode;");
        AssertDoesNotContain(bufferSettingsText, "FLASHBACK_FORMAT_CHANGE_");
        AssertDoesNotContain(bufferSettingsText, "FLASHBACK_ENCODER_SETTINGS_CHANGE_");
        AssertContains(recordingFormatText, "public Task UpdateRecordingFormatAsync(");
        AssertContains(recordingFormatText, "var previousSettings = CloneCaptureSettings(_currentSettings);");
        AssertContains(recordingFormatText, "FLASHBACK_FORMAT_CHANGE_ROLLBACK");
        AssertDoesNotContain(recordingFormatText, "FLASHBACK_ENCODER_SETTINGS_CHANGE_");
        AssertContains(encoderSettingsText, "private void UpdateEncodingSettings(CaptureSettings source)");
        AssertContains(encoderSettingsText, "public Task CycleFlashbackEncoderSettingsAsync(");
        AssertContains(encoderSettingsText, "var previousSettings = CloneCaptureSettings(_currentSettings);");
        AssertContains(encoderSettingsText, "FLASHBACK_ENCODER_SETTINGS_CHANGE_ROLLBACK");
        AssertDoesNotContain(encoderSettingsText, "GpuDecodeEnabled = gpuDecode;");
        AssertDoesNotContain(encoderSettingsText, "FLASHBACK_FORMAT_CHANGE_");
        AssertContains(agentMapText, "CaptureService.FlashbackState.cs");
        AssertContains(agentMapText, "CaptureService.FlashbackEnable.cs");
        AssertContains(agentMapText, "CaptureService.FlashbackRestart.cs");
        AssertContains(agentMapText, "CaptureService.FlashbackBufferSettings.cs");
        AssertContains(agentMapText, "CaptureService.FlashbackRecordingFormat.cs");
        AssertContains(agentMapText, "CaptureService.FlashbackEncoderSettings.cs");
        AssertDoesNotContain(agentMapText, "CaptureService.FlashbackControls.cs");
        AssertDoesNotContain(agentMapText, "CaptureService.FlashbackSettingsControls.cs");
        AssertContains(cleanupPlanText, "CaptureService.FlashbackState.cs");
        AssertContains(cleanupPlanText, "CaptureService.FlashbackEnable.cs");
        AssertContains(cleanupPlanText, "CaptureService.FlashbackRestart.cs");
        AssertContains(cleanupPlanText, "CaptureService.FlashbackBufferSettings.cs");
        AssertContains(cleanupPlanText, "CaptureService.FlashbackRecordingFormat.cs");
        AssertContains(cleanupPlanText, "CaptureService.FlashbackEncoderSettings.cs");
        AssertDoesNotContain(cleanupPlanText, "CaptureService.FlashbackControls.cs");
        AssertDoesNotContain(cleanupPlanText, "CaptureService.FlashbackSettingsControls.cs");
        AssertContains(agentMapText, "FlashbackBackendResources.BufferCycle.Lifecycle.cs");
        AssertContains(cleanupPlanText, "FlashbackBackendResources.BufferCycle.Lifecycle.cs");
        AssertContains(agentMapText, "FlashbackBackendResources.Startup.Rollback.cs");
        AssertContains(cleanupPlanText, "FlashbackBackendResources.Startup.Rollback.cs");
        AssertContains(agentMapText, "FlashbackBackendResources.RecordingFinalize.cs");
        AssertContains(cleanupPlanText, "FlashbackBackendResources.RecordingFinalize.cs");
        AssertContains(agentMapText, "FlashbackBackendResources.Producers.cs");
        AssertContains(cleanupPlanText, "FlashbackBackendResources.Producers.cs");
        AssertContains(backendResourcesText, "private FlashbackBufferCyclePlaybackState DisposePlaybackForBufferCycle(");
        AssertContains(backendResourcesText, "private static async Task StopAndDisposeOldSinkForBufferCycleAsync(");
        AssertContains(backendResourcesText, "private async Task<bool> TryStartReplacementSinkForBufferCycleAsync(");
        AssertContains(backendResourcesText, "private static async Task CleanupFailedReplacementSinkForBufferCycleAsync(");
        AssertContains(backendResourcesText, "public async Task<FlashbackBufferCycleResult> CycleSinkOnlyAsync(");
        AssertContains(backendResourcesText, "public async Task<FlashbackPlaybackController> StartPreviewBackendAsync(");
        AssertContains(backendResourcesText, "private async Task RollBackPreviewBackendStartAsync(");
        AssertContains(backendResourcesText, "FLASHBACK_PREVIEW_ROLLBACK_DETACH_WARN");
        AssertContains(backendResourcesText, "preview_init_rollback");
        AssertContains(backendResourcesText, "public async Task<FinalizeResult> FinalizeRecordingAsync(");
        AssertContains(backendResourcesText, "private static FinalizeResult PreserveEndArtifactsOnFailure(");
        AssertContains(backendResourcesText, "public void AttachProducers(FlashbackProducerAttachRequest request)");
        AssertContains(backendResourcesText, "public void DetachProducers(FlashbackProducerDetachRequest request)");
        AssertDoesNotContain(flashbackEnableText, "private async Task RestartFlashbackCoreAsync(");
        AssertDoesNotContain(flashbackRestartText, "public Task SetFlashbackEnabledAsync(");
        AssertDoesNotContain(flashbackStateText, "RunTransitionAsync(CurrentSessionState,");
        AssertDoesNotContain(flashbackEnableText, "private async Task EnsureFlashbackAudioInputsAsync(");
        AssertDoesNotContain(flashbackEnableText, "private async Task EnsureFlashbackPreviewBackendAsync(");
        AssertDoesNotContain(flashbackEnableText, "private async Task DisposeFlashbackPreviewBackendAsync(");
        AssertDoesNotContain(flashbackEnableText, "private async Task CycleFlashbackBufferAsync(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackControls.cs")),
            "old broad Flashback controls file removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackSettingsControls.cs")),
            "old broad Flashback settings controls file removed");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RecordingFinalizationLivesInFocusedPartials()
    {
        var stopLifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStopLifecycle.cs");
        var flashbackBackendFinalizationText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashbackBackend.cs");
        var libAvBackendFinalizationText =
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvBackend.cs");
        var libAvPreviewRestoreText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvPreviewRestore.cs");
        var flashbackFinalizationText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashback.cs");
        var outcomeStateText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingOutcomeState.cs");

        AssertContains(stopLifecycleText, "private async Task<FinalizeResult> StopAndDisposeRecordingBackendAsync(");
        AssertContains(stopLifecycleText, "StopAndDisposeFlashbackRecordingBackendAsync(cancellationToken)");
        AssertContains(stopLifecycleText, "StopAndDisposeLibAvRecordingBackendAsync(fallbackStatusMessage, emergency, cancellationToken)");
        AssertContains(flashbackBackendFinalizationText, "private async Task<FinalizeResult> StopAndDisposeFlashbackRecordingBackendAsync(");
        AssertContains(flashbackBackendFinalizationText, "FLASHBACK_UNIFIED_RECORDING_FINALIZE_FAIL");
        AssertContains(flashbackBackendFinalizationText, "ReconcileFlashbackBackendAfterRecordingFinalizeAsync(");
        AssertContains(flashbackBackendFinalizationText, "PublishRecordingFinalizedOutcome(fbResult, updateOutputPath: false);");
        AssertContains(flashbackBackendFinalizationText, "private async Task<OperationCanceledException?> ReconcileFlashbackBackendAfterRecordingFinalizeAsync(");
        AssertContains(flashbackBackendFinalizationText, "_flashbackBackend.PreserveRecoverySegments(\"recording_finalize_failed\");");
        AssertContains(flashbackBackendFinalizationText, "FLASHBACK_SETTINGS_APPLY_AFTER_RECORDING_DEFERRED");
        AssertContains(flashbackBackendFinalizationText, "FLASHBACK_SETTINGS_APPLY_AFTER_RECORDING");
        AssertContains(flashbackBackendFinalizationText, "await CycleFlashbackBufferAsync(cancellationToken)");
        AssertContains(flashbackBackendFinalizationText, "FLASHBACK_BUFFER_CYCLE_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(flashbackBackendFinalizationText, "BeginFlashbackBackendCleanup(ex);");
        AssertOccursBefore(flashbackBackendFinalizationText, "LogRecordingIntegritySummary(_lastRecordingIntegrity);", "ReconcileFlashbackBackendAfterRecordingFinalizeAsync(");
        AssertOccursBefore(flashbackBackendFinalizationText, "ReconcileFlashbackBackendAfterRecordingFinalizeAsync(", "PublishRecordingFinalizedOutcome(fbResult, updateOutputPath: false);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeFlashbackBackendReconcile.cs")),
            "old Flashback backend reconcile partial removed");
        AssertContains(libAvBackendFinalizationText, "private async Task<FinalizeResult> StopAndDisposeLibAvRecordingBackendAsync(");
        AssertContains(libAvBackendFinalizationText, "StopUnifiedVideoRecordingForLibAvFinalizeAsync(");
        AssertContains(libAvBackendFinalizationText, "DetachLibAvRecordingAudioBeforeSinkStopAsync(");
        AssertContains(libAvBackendFinalizationText, "StopAndDisposeLibAvSinkForFinalizeAsync(");
        AssertContains(libAvBackendFinalizationText, "DisposeIdleLibAvPreviewResourcesAfterRecordingAsync(");
        AssertContains(libAvBackendFinalizationText, "FoldLibAvAudioFaultIntoFinalizeResult(");
        AssertContains(libAvBackendFinalizationText, "PublishLibAvRecordingIntegrity(");
        AssertContains(libAvBackendFinalizationText, "CompleteLibAvRecordingFinalizeStateAsync(");
        AssertContains(libAvBackendFinalizationText, "var sinkResult = libAvSink != null");
        AssertContains(libAvBackendFinalizationText, "_videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(libAvBackendFinalizationText, "reason: \"recording_stop_deferred_drain\"");
        AssertContains(libAvBackendFinalizationText, "_previewAudioGraph.DetachCapture(");
        AssertContains(libAvBackendFinalizationText, "Recording WASAPI capture dispose failed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeLibAvResources.cs")),
            "old broad LibAv resource finalization partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeLibAvVideoBoundary.cs")),
            "old LibAv video-boundary finalization partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeLibAvSink.cs")),
            "old LibAv sink finalization partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingFinalizeLibAvIdlePreview.cs")),
            "old LibAv idle-preview finalization partial removed");
        AssertContains(libAvBackendFinalizationText, "RestoreLibAvPreviewFeaturesAfterRecordingAsync(");
        AssertContains(libAvBackendFinalizationText, "PublishRecordingFinalizedOutcome(result, updateOutputPath: true);");
        AssertDoesNotContain(libAvBackendFinalizationText, "if (_pendingFlashbackEnableAfterRecording)");
        AssertContains(libAvPreviewRestoreText, "private async Task<OperationCanceledException?> RestoreLibAvPreviewFeaturesAfterRecordingAsync(");
        AssertContains(libAvPreviewRestoreText, "private async Task<OperationCanceledException?> RestorePendingFlashbackEnableAfterLibAvRecordingAsync(");
        AssertContains(libAvPreviewRestoreText, "private async Task<OperationCanceledException?> RestartStandardMicrophoneMonitorAfterLibAvRecordingAsync(");
        AssertContains(libAvPreviewRestoreText, "if (!_pendingFlashbackEnableAfterRecording)");
        AssertContains(libAvPreviewRestoreText, "await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, cancellationToken)");
        AssertContains(libAvPreviewRestoreText, "FLASHBACK_ENABLE_AFTER_RECORDING_CANCELLED");
        AssertContains(libAvPreviewRestoreText, "FLASHBACK_ENABLE_AFTER_RECORDING_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(libAvPreviewRestoreText, "OnlyWhenMissing: false,");
        AssertContains(libAvPreviewRestoreText, "FlashbackAttachReason: \"mic_monitor_restart\",");
        AssertContains(libAvPreviewRestoreText, "RestartLogEvent: \"MIC_MONITOR_RESTART\",");
        AssertContains(flashbackFinalizationText, "private async Task<FinalizeResult> FinalizeFlashbackRecordingAsync(");
        AssertContains(flashbackFinalizationText, "private static bool IsFlashbackFinalizeCancellationResult(FinalizeResult result)");
        AssertContains(flashbackFinalizationText, "private sealed class FlashbackRecordingBoundarySnapshot");
        AssertContains(flashbackFinalizationText, "private void CaptureFlashbackRecordingBoundarySnapshot(");
        AssertContains(flashbackFinalizationText, "if (recordingBoundary.Captured)");
        AssertContains(flashbackFinalizationText, "flashbackVideoCapture.EndFlashbackRecordingAccounting();");
        AssertContains(flashbackFinalizationText, "recordingBoundary.Counters = CaptureFlashbackRecordingIntegrityCountersSinceBaseline");
        AssertContains(flashbackFinalizationText, "recordingBoundary.AudioCounters = GetRecordingAudioCountersSinceBaseline(");
        AssertContains(flashbackFinalizationText, "recordingBoundary.Captured = true;");
        AssertContains(outcomeStateText, "private void PublishRecordingStartedOutcome(string finalOutputPath)");
        AssertContains(outcomeStateText, "private void PublishRecordingFinalizedOutcome(FinalizeResult result, bool updateOutputPath)");
        AssertDoesNotContain(stopLifecycleText, "private sealed class FlashbackRecordingBoundarySnapshot");
        AssertDoesNotContain(stopLifecycleText, "private void CaptureFlashbackRecordingBoundarySnapshot(");
        AssertDoesNotContain(stopLifecycleText, "Unified video recording stop failed");
        AssertDoesNotContain(stopLifecycleText, "FLASHBACK_UNIFIED_RECORDING_FINALIZE_FAIL");
        AssertDoesNotContain(stopLifecycleText, "_lastOutputPath = result.OutputPath;");
        AssertDoesNotContain(stopLifecycleText, "_lastFinalizeStatus = result.StatusMessage;");
        AssertDoesNotContain(stopLifecycleText, "_lastPreservedArtifacts = result.PreservedArtifacts;");

        return Task.CompletedTask;
    }
}
