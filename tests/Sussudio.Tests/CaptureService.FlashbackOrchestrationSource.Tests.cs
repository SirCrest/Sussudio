using System.Linq;

static partial class Program
{
    private static readonly string[] CaptureServiceFlashbackOrchestrationFiles =
    {
        "Sussudio/Services/Capture/CaptureService.FlashbackControls.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackAudioInputs.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackPreviewBackend.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackPreviewBackendDisposal.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackBufferCycle.cs"
    };

    private static readonly string[] CaptureServiceRecordingFinalizationFiles =
    {
        "Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashbackBackend.cs",
        "Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashbackBackendReconcile.cs",
        "Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvBackend.cs",
        "Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvPreviewRestore.cs",
        "Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashback.cs",
        "Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashbackBoundary.cs",
        "Sussudio/Services/Capture/CaptureService.RecordingOutcomeState.cs"
    };

    private static readonly string[] CaptureServiceAudioFiles =
    {
        "Sussudio/Services/Capture/CaptureService.Audio.cs",
        "Sussudio/Services/Capture/CaptureService.AudioPreviewLifecycle.cs",
        "Sussudio/Services/Capture/CaptureService.AudioInputSwitching.cs",
        "Sussudio/Services/Capture/CaptureService.MicrophoneMonitor.cs",
        "Sussudio/Services/Capture/CaptureService.WasapiPlayback.cs"
    };

    private static readonly string[] CaptureServicePreviewLifecycleFiles =
    {
        "Sussudio/Services/Capture/CaptureService.PreviewStart.cs",
        "Sussudio/Services/Capture/CaptureService.PreviewAudioGraph.cs",
        "Sussudio/Services/Capture/CaptureService.PreviewStop.cs",
        "Sussudio/Services/Capture/CaptureService.PreviewReuse.cs",
        "Sussudio/Services/Capture/CaptureService.PreviewDisposal.cs"
    };

    private static readonly string[] CaptureServiceRecordingIntegrityFiles =
    {
        "Sussudio/Services/Capture/CaptureService.RecordingIntegrity.cs",
        "Sussudio/Services/Capture/CaptureService.RecordingIntegrity.Models.cs",
        "Sussudio/Services/Capture/CaptureService.RecordingIntegrity.Summary.cs",
        "Sussudio/Services/Capture/CaptureService.RecordingIntegrity.Counters.cs",
        "Sussudio/Services/Capture/CaptureService.RecordingIntegrity.Audio.cs",
        "Sussudio/Services/Capture/CaptureService.RecordingIntegrity.Logging.cs"
    };

    private static string ReadCaptureServiceFlashbackOrchestrationSource()
        => string.Join(
            "\n",
            CaptureServiceFlashbackOrchestrationFiles.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));

    private static string ReadCaptureServiceRecordingFinalizationSource()
        => string.Join(
            "\n",
            CaptureServiceRecordingFinalizationFiles.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));

    private static string ReadCaptureServiceAudioSource()
        => string.Join(
            "\n",
            CaptureServiceAudioFiles.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));

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

    private static string ReadCaptureServiceAudioCodeWithoutCommentsOrStrings()
        => string.Join(
            "\n",
            CaptureServiceAudioFiles.Select(ReadRepoCodeWithoutCommentsOrStrings));

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
        var backendResourcesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.ArtifactCleanup.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.cs");

        AssertContains(controlsText, "public Task RestartFlashbackAsync(");
        AssertContains(controlsText, "private async Task RestartFlashbackCoreAsync(");
        AssertContains(audioInputsText, "private async Task EnsureFlashbackAudioInputsAsync(");
        AssertContains(previewBackendText, "private async Task EnsureFlashbackPreviewBackendAsync(");
        AssertContains(disposalText, "private async Task DisposeFlashbackPreviewBackendAsync(");
        AssertContains(disposalText, "private readonly record struct FlashbackPreviewBackendDisposalRequest(");
        AssertContains(disposalText, "private async Task DisposeFlashbackPreviewBackendCoreAsync(");
        AssertContains(bufferCycleText, "private async Task CycleFlashbackBufferAsync(");
        AssertContains(bufferCycleText, "_flashbackBackend.CycleSinkOnlyAsync(");
        AssertContains(backendResourcesText, "public async Task<FlashbackBufferCycleResult> CycleSinkOnlyAsync(");
        AssertDoesNotContain(controlsText, "private async Task EnsureFlashbackAudioInputsAsync(");
        AssertDoesNotContain(controlsText, "private async Task EnsureFlashbackPreviewBackendAsync(");
        AssertDoesNotContain(controlsText, "private async Task DisposeFlashbackPreviewBackendAsync(");
        AssertDoesNotContain(controlsText, "private async Task CycleFlashbackBufferAsync(");

        return Task.CompletedTask;
    }

    private static Task CaptureService_AudioOwnershipLivesInFocusedPartials()
    {
        var audioText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Audio.cs");
        var audioPreviewText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.AudioPreviewLifecycle.cs");
        var audioInputSwitchingText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.AudioInputSwitching.cs");
        var microphoneText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.MicrophoneMonitor.cs");
        var playbackText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.WasapiPlayback.cs");

        AssertContains(audioText, "public void SetPreviewVolume(");
        AssertContains(audioText, "public void SetMonitoringMuted(");
        AssertContains(audioText, "private void OnWasapiAudioLevelUpdated(");
        AssertContains(audioText, "private void OnWasapiCaptureFailed(");
        AssertDoesNotContain(audioText, "public Task StartAudioPreviewAsync(");
        AssertDoesNotContain(audioText, "public Task UpdateAudioInputAsync(");
        AssertDoesNotContain(audioText, "public Task UpdateMicrophoneMonitorAsync(");
        AssertDoesNotContain(audioText, "private async Task StartWasapiPlaybackAsync(");

        AssertContains(audioPreviewText, "public Task StartAudioPreviewAsync(");
        AssertContains(audioPreviewText, "public Task StopAudioPreviewAsync(");
        AssertContains(audioPreviewText, "public Task StopAudioPreviewWithTeardownAsync(");
        AssertContains(audioPreviewText, "private Task StopAudioPreviewCoreAsync(");
        AssertDoesNotContain(audioPreviewText, "public Task UpdateAudioInputAsync(");
        AssertDoesNotContain(audioPreviewText, "public Task UpdateMicrophoneMonitorAsync(");
        AssertDoesNotContain(audioPreviewText, "private async Task StartWasapiPlaybackAsync(");

        AssertContains(audioInputSwitchingText, "public Task UpdateAudioInputAsync(");
        AssertContains(audioInputSwitchingText, "Logger.Log($\"Live audio input switch:");
        AssertContains(audioInputSwitchingText, "Logger.Log(\"AUDIO_INPUT_SWITCH_CANCEL_DEFERRED\");");
        AssertDoesNotContain(audioInputSwitchingText, "public Task StartAudioPreviewAsync(");
        AssertDoesNotContain(audioInputSwitchingText, "public Task UpdateMicrophoneMonitorAsync(");
        AssertDoesNotContain(audioInputSwitchingText, "private async Task StartWasapiPlaybackAsync(");

        AssertContains(microphoneText, "public Task UpdateMicrophoneMonitorAsync(");
        AssertContains(microphoneText, "private async Task DisposeMicrophoneCaptureAsync()");
        AssertContains(microphoneText, "private void OnMicrophoneAudioLevelUpdated(");
        AssertContains(microphoneText, "private async Task RestartMicrophoneMonitorAfterRecordingAsync(");
        AssertContains(microphoneText, "private readonly record struct MicrophoneMonitorRestartOptions(");

        AssertContains(playbackText, "private async Task StartWasapiPlaybackAsync(");
        AssertContains(playbackText, "private void StopWasapiPlayback()");
        AssertContains(playbackText, "private void DetachWasapiAudioCapture(");
        AssertContains(playbackText, "private static void SafeClearWasapiCapturePlayback(");
        AssertContains(playbackText, "private static void DisposeWasapiPlaybackBestEffort(");

        return Task.CompletedTask;
    }

    private static Task CaptureService_MicrophoneRestartAfterRecordingLivesInMicrophoneMonitorPartial()
    {
        var finalizationText = ReadCaptureServiceRecordingFinalizationSource()
            .Replace("\r\n", "\n");
        var microphoneText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.MicrophoneMonitor.cs")
            .Replace("\r\n", "\n");

        AssertContains(microphoneText, "private readonly record struct MicrophoneMonitorRestartOptions(");
        AssertContains(microphoneText, "private async Task RestartMicrophoneMonitorAfterRecordingAsync(");
        AssertContains(microphoneText, "new WasapiAudioCapture()");
        AssertContains(microphoneText, "micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;");
        AssertContains(microphoneText, "micCapture.CaptureFailed += OnWasapiCaptureFailed;");
        AssertContains(microphoneText, "micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));");
        AssertContains(microphoneText, "FLASHBACK_MIC_ATTACH_OK reason='{options.FlashbackAttachReason}'");
        AssertContains(microphoneText, "Logger.Log($\"{options.RestartLogEvent} device='\" + (_micMonitorDeviceName ?? \"?\") + \"'\");");
        AssertContains(microphoneText, "Logger.Log($\"{options.DisposeWarningEvent} type={disposeEx.GetType().Name} msg={disposeEx.Message}\");");
        AssertOccursBefore(
            microphoneText,
            "micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));",
            "_microphoneCapture = micCapture;");

        AssertContains(finalizationText, "await RestartMicrophoneMonitorAfterRecordingAsync(");
        AssertContains(finalizationText, "OnlyWhenMissing: true,");
        AssertContains(finalizationText, "DisposeWarningEvent: \"FLASHBACK_MIC_RESTART_DISPOSE_WARN\"");
        AssertContains(finalizationText, "OnlyWhenMissing: false,");
        AssertContains(finalizationText, "FlashbackAttachReason: \"mic_monitor_restart\",");
        AssertContains(finalizationText, "RestartLogEvent: \"MIC_MONITOR_RESTART\",");
        AssertContains(finalizationText, "DisposeWarningEvent: \"MIC_MONITOR_RESTART_DISPOSE_WARN\"");
        AssertDoesNotContain(finalizationText, "WasapiAudioCapture? micCapture = null;");
        AssertDoesNotContain(finalizationText, "micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;");
        AssertDoesNotContain(finalizationText, "micCapture.CaptureFailed += OnWasapiCaptureFailed;");

        return Task.CompletedTask;
    }

    private static Task CaptureService_RecordingFinalizationLivesInFocusedPartials()
    {
        var stopLifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStopLifecycle.cs");
        var flashbackBackendFinalizationText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashbackBackend.cs");
        var flashbackBackendReconcileText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashbackBackendReconcile.cs");
        var libAvBackendFinalizationText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvBackend.cs");
        var libAvPreviewRestoreText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvPreviewRestore.cs");
        var flashbackFinalizationText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashback.cs");
        var flashbackBoundaryText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashbackBoundary.cs");
        var outcomeStateText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingOutcomeState.cs");

        AssertContains(stopLifecycleText, "private async Task<FinalizeResult> StopAndDisposeRecordingBackendAsync(");
        AssertContains(stopLifecycleText, "StopAndDisposeFlashbackRecordingBackendAsync(cancellationToken)");
        AssertContains(stopLifecycleText, "StopAndDisposeLibAvRecordingBackendAsync(fallbackStatusMessage, emergency, cancellationToken)");
        AssertContains(flashbackBackendFinalizationText, "private async Task<FinalizeResult> StopAndDisposeFlashbackRecordingBackendAsync(");
        AssertContains(flashbackBackendFinalizationText, "FLASHBACK_UNIFIED_RECORDING_FINALIZE_FAIL");
        AssertContains(flashbackBackendFinalizationText, "ReconcileFlashbackBackendAfterRecordingFinalizeAsync(");
        AssertContains(flashbackBackendFinalizationText, "PublishRecordingFinalizedOutcome(fbResult, updateOutputPath: false);");
        AssertDoesNotContain(flashbackBackendFinalizationText, "FLASHBACK_SETTINGS_APPLY_AFTER_RECORDING");
        AssertContains(flashbackBackendReconcileText, "private async Task<OperationCanceledException?> ReconcileFlashbackBackendAfterRecordingFinalizeAsync(");
        AssertContains(flashbackBackendReconcileText, "_flashbackBackend.PreserveRecoverySegments(\"recording_finalize_failed\");");
        AssertContains(flashbackBackendReconcileText, "FLASHBACK_SETTINGS_APPLY_AFTER_RECORDING_DEFERRED");
        AssertContains(flashbackBackendReconcileText, "FLASHBACK_SETTINGS_APPLY_AFTER_RECORDING");
        AssertContains(flashbackBackendReconcileText, "await CycleFlashbackBufferAsync(cancellationToken)");
        AssertContains(flashbackBackendReconcileText, "FLASHBACK_BUFFER_CYCLE_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(flashbackBackendReconcileText, "BeginFlashbackBackendCleanup(ex);");
        AssertContains(libAvBackendFinalizationText, "private async Task<FinalizeResult> StopAndDisposeLibAvRecordingBackendAsync(");
        AssertContains(libAvBackendFinalizationText, "var sinkResult = libAvSink != null");
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
        AssertContains(flashbackBoundaryText, "private sealed class FlashbackRecordingBoundarySnapshot");
        AssertContains(flashbackBoundaryText, "private void CaptureFlashbackRecordingBoundarySnapshot(");
        AssertContains(flashbackBoundaryText, "if (recordingBoundary.Captured)");
        AssertContains(flashbackBoundaryText, "flashbackVideoCapture.EndFlashbackRecordingAccounting();");
        AssertContains(flashbackBoundaryText, "recordingBoundary.Counters = CaptureFlashbackRecordingIntegrityCountersSinceBaseline");
        AssertContains(flashbackBoundaryText, "recordingBoundary.AudioCounters = GetRecordingAudioCountersSinceBaseline(");
        AssertContains(flashbackBoundaryText, "recordingBoundary.Captured = true;");
        AssertDoesNotContain(flashbackFinalizationText, "private sealed class FlashbackRecordingBoundarySnapshot");
        AssertDoesNotContain(flashbackFinalizationText, "private void CaptureFlashbackRecordingBoundarySnapshot(");
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
