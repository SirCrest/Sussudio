using System.Threading.Tasks;

static partial class Program
{
    internal static Task RecordingBackendFlashbackBufferCycle_PreservesPolicies()
    {
        var sources = ReadRecordingQueueOverloadPolicySources();
        var bufferCycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackBufferCycle.cs")
            .Replace("\r\n", "\n");
        var finalizeBackendText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashbackBackend.cs")
            .Replace("\r\n", "\n");

        AssertContains(bufferCycleText, "private async Task CycleFlashbackBufferAsync(");
        AssertContains(bufferCycleText, "_flashbackBackend.CycleSinkOnlyAsync(");
        AssertDoesNotContain(finalizeBackendText, "private async Task CycleFlashbackBufferAsync(");
        AssertDoesNotContain(finalizeBackendText, "public async Task<FlashbackBufferCycleResult> CycleSinkOnlyAsync(");
        AssertFlashbackBufferCyclePolicies(
            sources.CaptureServiceSource,
            sources.FlashbackBackendSource);

        return Task.CompletedTask;
    }

    private static void AssertFlashbackBufferCyclePolicies(string captureServiceSource, string flashbackBackendSource)
    {
        var cycleFlashbackBuffer = ExtractSourceBlock(
            captureServiceSource,
            "private async Task CycleFlashbackBufferAsync",
            "private async Task<FinalizeResult> FinalizeFlashbackRecordingAsync");
        var backendCycleFlashbackBuffer = ExtractSourceBlock(
            flashbackBackendSource,
            "public async Task<FlashbackBufferCycleResult> CycleSinkOnlyAsync",
            "private async Task RollBackPreviewBackendStartAsync");
        AssertContains(cycleFlashbackBuffer, "var committedCycleToken = CancellationToken.None;");
        AssertContains(backendCycleFlashbackBuffer, "FLASHBACK_CYCLE_STOP_CANCEL_DEFERRED");
        AssertContains(backendCycleFlashbackBuffer, "FLASHBACK_BUFFER_CYCLE_CANCEL_DEFERRED");
        AssertDoesNotContain(cycleFlashbackBuffer, "cancellationToken: cancellationToken");
        AssertOccursBefore(
            backendCycleFlashbackBuffer,
            "await oldSink.DisposeAsync().ConfigureAwait(false);",
            "ClearSinkAndSettings();");
        AssertContains(backendCycleFlashbackBuffer, "var oldPlaybackController = TakePlaybackController();");
        AssertContains(backendCycleFlashbackBuffer, "oldPlaybackController.GoLive();");
        AssertContains(backendCycleFlashbackBuffer, "oldPlaybackController.Dispose();");
        AssertOccursBefore(
            backendCycleFlashbackBuffer,
            "oldPlaybackController.Dispose();",
            "bufferManager.PurgeCompletedSegments();");
        AssertOccursBefore(
            backendCycleFlashbackBuffer,
            "oldPlaybackController.Dispose();",
            "DetachProducers(");
        AssertContains(backendCycleFlashbackBuffer, "DetachProducers(");
        AssertContains(backendCycleFlashbackBuffer, "\"FLASHBACK_CYCLE_DETACH_WARN\"");
        var cycleNewSinkStart = backendCycleFlashbackBuffer;
        AssertContains(cycleNewSinkStart, "committedCycleToken,");
        AssertContains(cycleNewSinkStart, "AttachProducers(");
        AssertContains(cycleNewSinkStart, "new FlashbackProducerAttachRequest(");
        AssertContains(cycleNewSinkStart, "\"buffer_cycle\"");
        AssertContains(cycleNewSinkStart, "FLASHBACK_BUFFER_CYCLE_CANCEL_DEFERRED");
        AssertContains(cycleNewSinkStart, "newSink.FrameEncoded -= request.FrameEncodedHandler;");
        AssertContains(cycleNewSinkStart, "request.VideoCapture.SetFlashbackSink(null);");
        AssertContains(cycleNewSinkStart, "request.AudioCapture?.DetachFlashbackSink();");
        AssertContains(cycleNewSinkStart, "request.MicrophoneCapture?.SetAudioWriter(null);");
        AssertContains(cycleNewSinkStart, "new FlashbackPlaybackController(bufferManager)");
        AssertContains(cycleNewSinkStart, "GpuDecodeEnabled = request.Settings.FlashbackGpuDecode");
        AssertContains(cycleNewSinkStart, "request.PreviewFrameSink");
        AssertContains(cycleNewSinkStart, "PlaybackController = playbackController;");
        AssertContains(cycleNewSinkStart, "FLASHBACK_CYCLE_NEW_SINK_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(cycleNewSinkStart, "FLASHBACK_CYCLE_NEW_SINK_DETACH_WARN");
        AssertContains(flashbackBackendSource, "request.PurgeSegments");
        AssertContains(captureServiceSource, "new FlashbackPreviewBackendDisposalRequest(");
        AssertContains(flashbackBackendSource, "new FlashbackBackendArtifactCleanupRequest(");
        AssertContains(captureServiceSource, "effectivePurgeSegments,");
        AssertContains(captureServiceSource, "!activeFlashbackSink.CanBeginRecording");
        AssertContains(captureServiceSource, "_flashbackRecordingStartInProgress");
        AssertContains(captureServiceSource, "_flashbackRecordingFinalizeInProgress");
        AssertContains(captureServiceSource, "IsFlashbackRecordingBackendOwnedByRecording");
        AssertContains(captureServiceSource, "Volatile.Write(ref _flashbackRecordingStartInProgress, 1)");
        AssertContains(captureServiceSource, "Volatile.Write(ref _flashbackRecordingFinalizeInProgress, 1)");
        AssertContains(captureServiceSource, "Volatile.Write(ref _flashbackRecordingFinalizeInProgress, 0)");
        AssertContains(captureServiceSource, "await _flashbackBackendLeaseLock.WaitAsync(transitionToken)");
        AssertContains(captureServiceSource, "BeginFlashbackRecordingAccounting");
        AssertContains(captureServiceSource, "EndFlashbackRecordingAccounting");
        AssertContains(captureServiceSource, "CancelRecordingStartRollback");
        AssertContains(captureServiceSource, "FLASHBACK_RECORDING_START_ROLLBACK_WARN type={rollbackEx.GetType().Name} error='{rollbackEx.Message}'");
        AssertContains(captureServiceSource, "var failureToken = ex is OperationCanceledException && cancellationToken.IsCancellationRequested");
        AssertContains(captureServiceSource, "FLASHBACK_PREVIEW_INIT_CANCELLED");
        AssertContains(captureServiceSource, "FLASHBACK_PREVIEW_INIT_FAIL");
        AssertContains(captureServiceSource, "Logger.Log($\"{failureToken} type={ex.GetType().Name} error='{ex.Message}'\")");
        AssertContains(flashbackBackendSource, "new FlashbackProducerDetachRequest(");
        AssertContains(flashbackBackendSource, "\"FLASHBACK_PREVIEW_ROLLBACK_DETACH_WARN\"");
        AssertContains(flashbackBackendSource, "Logger.Log($\"{request.WarningToken} target=video");
        AssertContains(flashbackBackendSource, "Logger.Log($\"{request.WarningToken} target=audio");
        AssertContains(flashbackBackendSource, "Logger.Log($\"{request.WarningToken} target=microphone");
        AssertContains(captureServiceSource, "MIC_MONITOR_WRITER_DETACH_WARN");
        AssertOccursBefore(captureServiceSource, "MIC_MONITOR_WRITER_DETACH_WARN", "await mic.DisposeAsync().ConfigureAwait(false);");
        AssertContains(captureServiceSource, "VIDEO_DIAG flashback_recording_pipeline");
        AssertContains(captureServiceSource, "BeginFlashbackBackendCleanup");
        AssertContains(captureServiceSource, "detachMicrophoneWriter: !preserveDedicatedRecordingMic");
        AssertContains(captureServiceSource, "recordingContext = fbRecordingContext");
        AssertDoesNotContain(captureServiceSource, "SetFatalErrorCallback(OnRecordingBackendFatalError)");
    }
}
