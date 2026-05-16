using System.Linq;

static partial class Program
{
    private static readonly string[] CaptureServiceFlashbackOrchestrationFiles =
    {
        "Sussudio/Services/Capture/CaptureService.FlashbackOrchestration.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackAudioInputs.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackPreviewBackend.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackPreviewBackendDisposal.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackBufferCycle.cs"
    };

    private static readonly string[] CaptureServiceRecordingFinalizationFiles =
    {
        "Sussudio/Services/Capture/CaptureService.RecordingFinalizeRecord.cs",
        "Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashback.cs",
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

    private static Task CaptureService_FlashbackOrchestrationLivesInFocusedPartials()
    {
        var orchestrationText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackOrchestration.cs");
        var audioInputsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackAudioInputs.cs");
        var previewBackendText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackPreviewBackend.cs");
        var disposalText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackPreviewBackendDisposal.cs");
        var bufferCycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackBufferCycle.cs");

        AssertContains(orchestrationText, "private async Task RestartFlashbackCoreAsync(");
        AssertContains(audioInputsText, "private async Task EnsureFlashbackAudioInputsAsync(");
        AssertContains(previewBackendText, "private async Task EnsureFlashbackPreviewBackendAsync(");
        AssertContains(disposalText, "private async Task DisposeFlashbackPreviewBackendAsync(");
        AssertContains(disposalText, "private readonly record struct FlashbackPreviewBackendDisposalRequest(");
        AssertContains(disposalText, "private async Task DisposeFlashbackPreviewBackendCoreAsync(");
        AssertContains(bufferCycleText, "private async Task CycleFlashbackBufferAsync(");
        AssertDoesNotContain(orchestrationText, "private async Task EnsureFlashbackAudioInputsAsync(");
        AssertDoesNotContain(orchestrationText, "private async Task EnsureFlashbackPreviewBackendAsync(");
        AssertDoesNotContain(orchestrationText, "private async Task DisposeFlashbackPreviewBackendAsync(");
        AssertDoesNotContain(orchestrationText, "private async Task CycleFlashbackBufferAsync(");

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
        var finalizationText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeRecord.cs")
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
        var recordFinalizationText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeRecord.cs");
        var flashbackFinalizationText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashback.cs");
        var outcomeStateText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingOutcomeState.cs");

        AssertContains(recordFinalizationText, "private async Task<FinalizeResult> StopAndDisposeRecordingBackendAsync(");
        AssertContains(flashbackFinalizationText, "private async Task<FinalizeResult> FinalizeFlashbackRecordingAsync(");
        AssertContains(flashbackFinalizationText, "private sealed class FlashbackRecordingBoundarySnapshot");
        AssertContains(flashbackFinalizationText, "private void CaptureFlashbackRecordingBoundarySnapshot(");
        AssertContains(flashbackFinalizationText, "private static bool IsFlashbackFinalizeCancellationResult(FinalizeResult result)");
        AssertContains(outcomeStateText, "private void PublishRecordingStartedOutcome(string finalOutputPath)");
        AssertContains(outcomeStateText, "private void PublishRecordingFinalizedOutcome(FinalizeResult result, bool updateOutputPath)");
        AssertDoesNotContain(recordFinalizationText, "private sealed class FlashbackRecordingBoundarySnapshot");
        AssertDoesNotContain(recordFinalizationText, "private void CaptureFlashbackRecordingBoundarySnapshot(");
        AssertDoesNotContain(recordFinalizationText, "_lastOutputPath = result.OutputPath;");
        AssertDoesNotContain(recordFinalizationText, "_lastFinalizeStatus = result.StatusMessage;");
        AssertDoesNotContain(recordFinalizationText, "_lastPreservedArtifacts = result.PreservedArtifacts;");

        return Task.CompletedTask;
    }
}
