using System.IO;
using System.Linq;

static partial class Program
{
    private static readonly string[] CaptureServiceFlashbackOrchestrationFiles =
    {
        "Sussudio/Services/Capture/CaptureService.FlashbackControls.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackAudioInputs.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackPreviewBackend.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackPreviewBackendDisposal.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackBufferCycle.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackBufferSettings.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackEncoderSettings.cs"
    };

    private static readonly string[] CaptureServiceRecordingFinalizationFiles =
    {
        "Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashbackBackend.cs",
        "Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvBackend.cs",
        "Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvResources.cs",
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
        "Sussudio/Services/Capture/CaptureService.RecordingIntegrity.Models.cs",
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

    private static Task CaptureService_FlashbackOrchestrationLivesInFocusedPartials()
    {
        var controlsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackControls.cs");
        var audioInputsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackAudioInputs.cs");
        var previewBackendText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackPreviewBackend.cs");
        var disposalText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackPreviewBackendDisposal.cs");
        var bufferCycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackBufferCycle.cs");
        var bufferSettingsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackBufferSettings.cs");
        var encoderSettingsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackEncoderSettings.cs");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");
        var backendResourcesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.PreviewDisposal.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.ArtifactCleanup.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.BufferCycle.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.Startup.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.cs");

        AssertContains(controlsText, "public Task RestartFlashbackAsync(");
        AssertContains(controlsText, "private async Task RestartFlashbackCoreAsync(");
        AssertContains(audioInputsText, "private async Task EnsureFlashbackAudioInputsAsync(");
        AssertContains(previewBackendText, "private async Task EnsureFlashbackPreviewBackendAsync(");
        AssertContains(disposalText, "private async Task DisposeFlashbackPreviewBackendAsync(");
        AssertContains(disposalText, "private async Task DisposeFlashbackPreviewBackendCoreAsync(");
        AssertContains(disposalText, "CreateFlashbackPreviewBackendDisposalRequest(");
        AssertContains(backendResourcesText, "internal readonly record struct FlashbackPreviewBackendDisposalRequest(");
        AssertContains(backendResourcesText, "public async Task DisposePreviewBackendAsync(");
        AssertContains(bufferCycleText, "private async Task CycleFlashbackBufferAsync(");
        AssertContains(bufferCycleText, "_flashbackBackend.CycleSinkOnlyAsync(");
        AssertContains(bufferSettingsText, "public Task UpdateFlashbackSettingsAsync(");
        AssertContains(bufferSettingsText, "_currentSettings.FlashbackBufferMinutes = bufferMinutes;");
        AssertContains(bufferSettingsText, "_flashbackPlaybackController.GpuDecodeEnabled = gpuDecode;");
        AssertDoesNotContain(bufferSettingsText, "FLASHBACK_FORMAT_CHANGE_");
        AssertDoesNotContain(bufferSettingsText, "FLASHBACK_ENCODER_SETTINGS_CHANGE_");
        AssertContains(encoderSettingsText, "private void UpdateEncodingSettings(CaptureSettings source)");
        AssertContains(encoderSettingsText, "public Task UpdateRecordingFormatAsync(");
        AssertContains(encoderSettingsText, "public Task CycleFlashbackEncoderSettingsAsync(");
        AssertContains(encoderSettingsText, "var previousSettings = CloneCaptureSettings(_currentSettings);");
        AssertContains(encoderSettingsText, "FLASHBACK_FORMAT_CHANGE_ROLLBACK");
        AssertContains(encoderSettingsText, "FLASHBACK_ENCODER_SETTINGS_CHANGE_ROLLBACK");
        AssertDoesNotContain(encoderSettingsText, "GpuDecodeEnabled = gpuDecode;");
        AssertContains(agentMapText, "CaptureService.FlashbackBufferSettings.cs");
        AssertContains(agentMapText, "CaptureService.FlashbackEncoderSettings.cs");
        AssertDoesNotContain(agentMapText, "CaptureService.FlashbackSettingsControls.cs");
        AssertContains(cleanupPlanText, "CaptureService.FlashbackBufferSettings.cs");
        AssertContains(cleanupPlanText, "CaptureService.FlashbackEncoderSettings.cs");
        AssertDoesNotContain(cleanupPlanText, "CaptureService.FlashbackSettingsControls.cs");
        AssertContains(backendResourcesText, "public async Task<FlashbackBufferCycleResult> CycleSinkOnlyAsync(");
        AssertDoesNotContain(controlsText, "private async Task EnsureFlashbackAudioInputsAsync(");
        AssertDoesNotContain(controlsText, "private async Task EnsureFlashbackPreviewBackendAsync(");
        AssertDoesNotContain(controlsText, "private async Task DisposeFlashbackPreviewBackendAsync(");
        AssertDoesNotContain(controlsText, "private async Task CycleFlashbackBufferAsync(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackSettingsControls.cs")),
            "old broad Flashback settings controls file removed");

        return Task.CompletedTask;
    }

    private static Task CaptureService_RecordingFinalizationLivesInFocusedPartials()
    {
        var stopLifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStopLifecycle.cs");
        var flashbackBackendFinalizationText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashbackBackend.cs");
        var libAvBackendFinalizationText =
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvBackend.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvResources.cs");
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
        AssertContains(libAvBackendFinalizationText, "RestoreLibAvPreviewFeaturesAfterRecordingAsync(");
        AssertContains(libAvBackendFinalizationText, "PublishRecordingFinalizedOutcome(result, updateOutputPath: true);");
        AssertDoesNotContain(libAvBackendFinalizationText, "if (_pendingFlashbackEnableAfterRecording)");
        AssertContains(libAvPreviewRestoreText, "private async Task<OperationCanceledException?> RestoreLibAvPreviewFeaturesAfterRecordingAsync(");
        AssertContains(libAvPreviewRestoreText, "private async Task<OperationCanceledException?> RestorePendingFlashbackEnableAfterLibAvRecordingAsync(");
        AssertContains(libAvPreviewRestoreText, "private async Task<OperationCanceledException?> RestartStandardMicrophoneMonitorAfterLibAvRecordingAsync(");
        AssertContains(libAvPreviewRestoreText, "if (!_pendingFlashbackEnableAfterRecording)");
        AssertContains(libAvPreviewRestoreText, "await EnsureFlashbackPreviewBackendAsync(_unifiedVideoCapture, _currentSettings, cancellationToken)");
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
