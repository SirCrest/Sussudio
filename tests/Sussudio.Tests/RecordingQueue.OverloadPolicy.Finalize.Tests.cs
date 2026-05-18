using System;
using System.Threading.Tasks;

// Tests for recording backend finalize, cleanup, and Flashback recovery boundaries.
static partial class Program
{
    private static Task RecordingBackendFinalizeAndCleanup_PreservesFlashbackBoundaries()
    {
        var sources = ReadRecordingQueueOverloadPolicySources();
        var flashbackBackendSource = sources.FlashbackBackendSource;
        var captureServiceSource = sources.CaptureServiceSource;
        var captureHealthSnapshotRootSource = sources.CaptureHealthSnapshotRootSource;
        var captureSnapshotsSource = sources.CaptureSnapshotsSource;
        var unifiedVideoCaptureSource = sources.UnifiedVideoCaptureSource;
        var microphoneMonitorText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.MicrophoneMonitor.cs")
            .Replace("\r\n", "\n");
        var libAvPreviewRestoreText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvPreviewRestore.cs")
            .Replace("\r\n", "\n");
        var stopRecordingBackendRouter = ExtractSourceBlock(
            captureServiceSource,
            "private async Task<FinalizeResult> StopAndDisposeRecordingBackendAsync",
            "private async Task<FinalizeResult> StopAndDisposeFlashbackRecordingBackendAsync");
        var flashbackStopRecordingBackend = ExtractSourceBlock(
            captureServiceSource,
            "private async Task<FinalizeResult> StopAndDisposeFlashbackRecordingBackendAsync",
            "private async Task<FinalizeResult> StopAndDisposeLibAvRecordingBackendAsync");
        var libAvStopRecordingBackend = ExtractSourceBlock(
            captureServiceSource,
            "private async Task<FinalizeResult> StopAndDisposeLibAvRecordingBackendAsync",
            "private async Task<FinalizeResult> FinalizeFlashbackRecordingAsync");

        AssertContains(stopRecordingBackendRouter, "IsFlashbackRecordingBackendActive()");
        AssertContains(stopRecordingBackendRouter, "StopAndDisposeFlashbackRecordingBackendAsync(cancellationToken)");
        AssertContains(stopRecordingBackendRouter, "StopAndDisposeLibAvRecordingBackendAsync(fallbackStatusMessage, emergency, cancellationToken)");
        AssertDoesNotContain(stopRecordingBackendRouter, "OperationCanceledException? flashbackCancellationException = null;");
        AssertDoesNotContain(stopRecordingBackendRouter, "var sink = _recordingSink;");
        AssertContains(flashbackStopRecordingBackend, "OperationCanceledException? flashbackCancellationException = null;");
        AssertContains(flashbackStopRecordingBackend, "fbResult = FinalizeResult.Failure(fbOutputPath, \"Flashback recording finalize cancelled.\");");
        AssertContains(flashbackStopRecordingBackend, "if (cancellationToken.IsCancellationRequested && IsFlashbackFinalizeCancellationResult(fbResult))");
        AssertContains(flashbackStopRecordingBackend, "flashbackCancellationException ??= new OperationCanceledException(cancellationToken);");
        AssertContains(flashbackStopRecordingBackend, "FLASHBACK_UNIFIED_RECORDING_FINALIZE_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(flashbackStopRecordingBackend, "ReconcileFlashbackBackendAfterRecordingFinalizeAsync(");
        AssertContains(flashbackStopRecordingBackend, "FLASHBACK_BUFFER_CYCLE_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(flashbackStopRecordingBackend, "RecordLastFlashbackFailure(ex);");
        AssertContains(flashbackStopRecordingBackend, "_flashbackBackend.PreserveRecoverySegments(\"buffer_cycle_failed\");");
        AssertContains(flashbackStopRecordingBackend, "BeginFlashbackBackendCleanup(ex);");
        AssertContains(flashbackStopRecordingBackend, "FLASHBACK_MIC_RESTART_WARN type={ex.GetType().Name} error='{ex.Message}'");
        AssertDoesNotContain(flashbackStopRecordingBackend, "libAvSink.StopAsync(emergency, cancellationToken)");
        AssertContains(libAvStopRecordingBackend, "var detachedBackend = _recordingBackend.DetachLibAvBackend();");
        AssertContains(libAvStopRecordingBackend, "await unifiedVideoCapture.StopRecordingAsync()");
        AssertContains(libAvStopRecordingBackend, "? await libAvSink.StopAsync(emergency, cancellationToken).ConfigureAwait(false)");
        AssertContains(libAvStopRecordingBackend, "RestoreLibAvPreviewFeaturesAfterRecordingAsync(");
        AssertContains(libAvStopRecordingBackend, "PublishRecordingFinalizedOutcome(result, updateOutputPath: true);");
        AssertDoesNotContain(libAvStopRecordingBackend, "FinalizeFlashbackRecordingAsync(");
        AssertOccursBefore(
            libAvStopRecordingBackend,
            "RestoreLibAvPreviewFeaturesAfterRecordingAsync(",
            "PublishRecordingFinalizedOutcome(result, updateOutputPath: true);");
        AssertOccursBefore(
            flashbackStopRecordingBackend,
            "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)",
            "FLASHBACK_UNIFIED_RECORDING_FINALIZE_FAIL");
        AssertOccursBefore(
            flashbackStopRecordingBackend,
            "if (cancellationToken.IsCancellationRequested && IsFlashbackFinalizeCancellationResult(fbResult))",
            "_lastRecordingIntegrity = BuildRecordingIntegritySummary(");
        AssertOccursBefore(
            flashbackStopRecordingBackend,
            "fbResult = FinalizeResult.Failure(fbOutputPath, \"Flashback recording finalize cancelled.\");",
            "_recordingStopwatch.Stop();");
        AssertOccursBefore(
            flashbackStopRecordingBackend,
            "PublishRecordingFinalizedOutcome(fbResult, updateOutputPath: false);",
            "throw flashbackCancellationException;");
        var postFinalizeCycle = ExtractSourceBlock(
            flashbackStopRecordingBackend,
            "private async Task<OperationCanceledException?> ReconcileFlashbackBackendAfterRecordingFinalizeAsync",
            "        return cancellationException;");
        AssertContains(postFinalizeCycle, "cancellationException ??= new OperationCanceledException(cancellationToken);");
        AssertOccursBefore(
            postFinalizeCycle,
            "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)",
            "FLASHBACK_BUFFER_CYCLE_FAIL");
        AssertOccursBefore(
            postFinalizeCycle,
            "FLASHBACK_BUFFER_CYCLE_FAIL",
            "RecordLastFlashbackFailure(ex);");
        AssertOccursBefore(
            postFinalizeCycle,
            "RecordLastFlashbackFailure(ex);",
            "BeginFlashbackBackendCleanup(ex);");

        AssertFlashbackAndLibAvMicrophoneRestartPolicies(
            flashbackStopRecordingBackend,
            microphoneMonitorText,
            libAvPreviewRestoreText);
        AssertFlashbackBackendCleanupPolicies(captureServiceSource, flashbackBackendSource);
        AssertRecordingQueueHealthSnapshotTelemetry(
            captureServiceSource,
            captureHealthSnapshotRootSource,
            captureSnapshotsSource,
            unifiedVideoCaptureSource);

        return Task.CompletedTask;
    }

    private static void AssertFlashbackAndLibAvMicrophoneRestartPolicies(
        string flashbackStopRecordingBackend,
        string microphoneMonitorText,
        string libAvPreviewRestoreText)
    {
        var flashbackMicMonitorRestart = ExtractSourceBlock(
            flashbackStopRecordingBackend,
            "// Restart mic monitoring if preview is still active",
            "if (fbResult.Succeeded)");
        AssertContains(flashbackMicMonitorRestart, "await RestartMicrophoneMonitorAfterRecordingAsync(");
        AssertContains(flashbackMicMonitorRestart, "OnlyWhenMissing: true,");
        AssertContains(flashbackMicMonitorRestart, "FlashbackAttachReason: null,");
        AssertContains(flashbackMicMonitorRestart, "RestartLogEvent: null,");
        AssertContains(flashbackMicMonitorRestart, "DisposeWarningEvent: \"FLASHBACK_MIC_RESTART_DISPOSE_WARN\"");
        AssertContains(flashbackMicMonitorRestart, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)");
        AssertContains(flashbackMicMonitorRestart, "flashbackCancellationException ??= new OperationCanceledException(cancellationToken);");
        AssertContains(flashbackMicMonitorRestart, "FLASHBACK_MIC_RESTART_WARN type={ex.GetType().Name} error='{ex.Message}'");
        AssertDoesNotContain(flashbackMicMonitorRestart, "WasapiAudioCapture? micCapture = null;");
        AssertDoesNotContain(flashbackMicMonitorRestart, "micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;");
        AssertContains(microphoneMonitorText, "private async Task RestartMicrophoneMonitorAfterRecordingAsync(");
        AssertContains(microphoneMonitorText, "if (options.OnlyWhenMissing && _microphoneCapture != null)");
        AssertContains(microphoneMonitorText, "FLASHBACK_MIC_ATTACH_OK reason='{options.FlashbackAttachReason}'");
        AssertContains(microphoneMonitorText, "Logger.Log($\"{options.DisposeWarningEvent} type={disposeEx.GetType().Name} msg={disposeEx.Message}\");");
        AssertOccursBefore(
            microphoneMonitorText,
            "micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));",
            "_microphoneCapture = micCapture;");

        AssertContains(libAvPreviewRestoreText, "private async Task<OperationCanceledException?> RestorePendingFlashbackEnableAfterLibAvRecordingAsync(");
        AssertContains(libAvPreviewRestoreText, "if (!_pendingFlashbackEnableAfterRecording)");
        AssertContains(libAvPreviewRestoreText, "_pendingFlashbackEnableAfterRecording = false;");
        AssertContains(libAvPreviewRestoreText, "await EnsureFlashbackPreviewBackendAsync(_unifiedVideoCapture, _currentSettings, cancellationToken)");
        AssertContains(libAvPreviewRestoreText, "FLASHBACK_ENABLE_AFTER_RECORDING_CANCELLED");
        AssertContains(libAvPreviewRestoreText, "FLASHBACK_ENABLE_AFTER_RECORDING_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        var standardMicMonitorRestart = ExtractSourceBlock(
            libAvPreviewRestoreText,
            "private async Task<OperationCanceledException?> RestartStandardMicrophoneMonitorAfterLibAvRecordingAsync",
            "        return cancellationException;");
        AssertContains(standardMicMonitorRestart, "await RestartMicrophoneMonitorAfterRecordingAsync(");
        AssertContains(standardMicMonitorRestart, "OnlyWhenMissing: false,");
        AssertContains(standardMicMonitorRestart, "FlashbackAttachReason: \"mic_monitor_restart\",");
        AssertContains(standardMicMonitorRestart, "RestartLogEvent: \"MIC_MONITOR_RESTART\",");
        AssertContains(standardMicMonitorRestart, "DisposeWarningEvent: \"MIC_MONITOR_RESTART_DISPOSE_WARN\"");
        AssertContains(standardMicMonitorRestart, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)");
        AssertContains(standardMicMonitorRestart, "cancellationException ??= new OperationCanceledException(cancellationToken);");
        AssertContains(standardMicMonitorRestart, "Mic monitor restart failed (non-fatal): ");
        AssertDoesNotContain(standardMicMonitorRestart, "WasapiAudioCapture? micCapture = null;");
        AssertContains(microphoneMonitorText, "Logger.Log($\"{options.RestartLogEvent} device='\" + (_micMonitorDeviceName ?? \"?\") + \"'\");");
    }

    private static void AssertFlashbackBackendCleanupPolicies(string captureServiceSource, string flashbackBackendSource)
    {
        AssertContains(captureServiceSource, "private static bool IsFlashbackFinalizeCancellationResult(FinalizeResult result)");
        AssertContains(captureServiceSource, "string.Equals(result.StatusMessage, \"Flashback export cancelled.\", StringComparison.Ordinal)");
        AssertContains(captureServiceSource, "string.Equals(result.StatusMessage, \"Flashback recording finalize cancelled.\", StringComparison.Ordinal)");
        AssertContains(captureServiceSource, "private void PublishRecordingStartedOutcome(string finalOutputPath)");
        AssertContains(captureServiceSource, "private void PublishRecordingFinalizedOutcome(FinalizeResult result, bool updateOutputPath)");
        AssertContains(captureServiceSource, "PublishRecordingStartedOutcome(fbRecordingContext.FinalOutputPath);");
        AssertContains(captureServiceSource, "PublishRecordingStartedOutcome(rollback.RecordingContext.FinalOutputPath);");
        AssertContains(captureServiceSource, "PublishRecordingFinalizedOutcome(fbResult, updateOutputPath: false);");
        AssertContains(captureServiceSource, "PublishRecordingFinalizedOutcome(result, updateOutputPath: true);");
        var disposeFlashbackPreviewBackendCore = ExtractSourceBlock(
            captureServiceSource,
            "private async Task DisposeFlashbackPreviewBackendCoreAsync",
            "private FlashbackPreviewBackendDisposalRequest CreateFlashbackPreviewBackendDisposalRequest");
        AssertContains(disposeFlashbackPreviewBackendCore, "_flashbackBackend.DisposePreviewBackendAsync(request)");
        var disposeFlashbackPreviewBackendResources = ExtractSourceBlock(
            flashbackBackendSource,
            "public async Task DisposePreviewBackendAsync",
            "public void ScheduleDeferredArtifactCleanup");
        AssertOccursBefore(disposeFlashbackPreviewBackendResources, "request.CancellationToken.ThrowIfCancellationRequested();", "CleanupArtifactsAfterExportAsync(");
        AssertOccursBefore(disposeFlashbackPreviewBackendResources, "TakePlaybackController()", "flashbackPlaybackController.GoLive();");
        AssertContains(disposeFlashbackPreviewBackendResources, "DetachProducers(");
        AssertContains(disposeFlashbackPreviewBackendResources, "\"FLASHBACK_PREVIEW_DETACH_WARN\"");
        AssertContains(disposeFlashbackPreviewBackendResources, "await flashbackSink.StopAsync(CancellationToken.None).ConfigureAwait(false);");
        AssertOccursBefore(disposeFlashbackPreviewBackendResources, "DetachProducers(", "await flashbackSink.StopAsync(CancellationToken.None).ConfigureAwait(false);");
        AssertOccursBefore(disposeFlashbackPreviewBackendResources, "Clear();", "request.CancellationToken.ThrowIfCancellationRequested();");
        AssertOccursBefore(disposeFlashbackPreviewBackendResources, "ScheduleDeferredArtifactCleanup(", "request.CancellationToken.ThrowIfCancellationRequested();");
        AssertContains(disposeFlashbackPreviewBackendResources, "var cleanupCompleted = await CleanupArtifactsAfterExportAsync(");
        AssertContains(disposeFlashbackPreviewBackendResources, "ScheduleDeferredArtifactCleanup(\n                Task.Delay(TimeSpan.FromSeconds(1)),");
        var deferredFlashbackBackendCleanup = ExtractSourceBlock(
            captureServiceSource,
            "private void ScheduleDeferredFlashbackBackendCleanup",
            "private async Task<bool> CleanupFlashbackBackendArtifactsAfterExportAsync");
        AssertContains(deferredFlashbackBackendCleanup, "FlashbackBackendArtifactCleanupRequest request,");
        AssertContains(deferredFlashbackBackendCleanup, "_flashbackBackend.ScheduleDeferredArtifactCleanup(");
        AssertContains(deferredFlashbackBackendCleanup, "WaitForFlashbackBackendCleanupExportLockAsync");
        AssertContains(deferredFlashbackBackendCleanup, "ReleaseFlashbackBackendCleanupExportLock");

        var deferredFlashbackBackendResourcesCleanup = ExtractSourceBlock(
            flashbackBackendSource,
            "public void ScheduleDeferredArtifactCleanup",
            "public async Task<bool> CleanupArtifactsAfterExportAsync");
        AssertContains(deferredFlashbackBackendResourcesCleanup, "FlashbackBackendArtifactCleanupRequest request,");
        AssertContains(deferredFlashbackBackendResourcesCleanup, "CleanupArtifactsAfterExportAsync(");
        AssertContains(deferredFlashbackBackendResourcesCleanup, "if (cleanupCompleted)");
        AssertContains(deferredFlashbackBackendResourcesCleanup, "FLASHBACK_BACKEND_DEFERRED_CLEANUP_OK reason='{request.Reason}' attempt={attempt}");
        AssertContains(deferredFlashbackBackendResourcesCleanup, "else if (attempt < 3)");
        AssertContains(deferredFlashbackBackendResourcesCleanup, "FLASHBACK_BACKEND_DEFERRED_CLEANUP_RETRY reason='{request.Reason}' attempt={attempt} next_attempt={nextAttempt}");
        AssertContains(deferredFlashbackBackendResourcesCleanup, "Task.Delay(TimeSpan.FromSeconds(5))");
        AssertContains(deferredFlashbackBackendResourcesCleanup, "FLASHBACK_BACKEND_DEFERRED_CLEANUP_GIVE_UP reason='{request.Reason}' attempt={attempt} preserve_segments=true");
        var flashbackBackendArtifactCleanup = ExtractSourceBlock(
            flashbackBackendSource,
            "public async Task<bool> CleanupArtifactsAfterExportAsync",
            "public async Task<FlashbackPlaybackController> StartPreviewBackendAsync");
        AssertContains(flashbackBackendArtifactCleanup, "FlashbackBackendArtifactCleanupRequest request,");
        AssertContains(captureServiceSource, "WaitAsync(\n            TimeSpan.FromSeconds(30),\n            CancellationToken.None)");
        AssertContains(flashbackBackendArtifactCleanup, "acquireExportOperationLockAsync()");
        AssertOccursBefore(flashbackBackendArtifactCleanup, "request.FlashbackExporter.Dispose();", "request.BufferManager.PurgeAllSegments();");
        AssertOccursBefore(flashbackBackendArtifactCleanup, "request.FlashbackExporter.Dispose();", "request.BufferManager.Dispose();");
        AssertOccursBefore(flashbackBackendArtifactCleanup, "acquireExportOperationLockAsync()", "request.FlashbackExporter.Dispose();");
        AssertOccursBefore(flashbackBackendArtifactCleanup, "acquireExportOperationLockAsync()", "request.BufferManager.PurgeAllSegments();");
    }

}
