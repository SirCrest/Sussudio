using System;
using System.Threading.Tasks;

// Tests for recording sink queue limits, drops, and latency accounting.
static partial class Program
{
    private static Task RecordingVideoQueues_FailExplicitlyInsteadOfEvictingFrames()
    {
        var sources = ReadRecordingQueueOverloadPolicySources();
        var libAvSource = sources.LibAvSource;
        var flashbackSource = sources.FlashbackSource;
        var flashbackBackendSource = sources.FlashbackBackendSource;
        var flashbackBufferSource = sources.FlashbackBufferSource;
        var flashbackCleanupSource = sources.FlashbackCleanupSource;
        var captureServiceSource = sources.CaptureServiceSource;
        var captureHealthSnapshotRootSource = sources.CaptureHealthSnapshotRootSource;
        var captureSnapshotsSource = sources.CaptureSnapshotsSource;
        var unifiedVideoCaptureSource = sources.UnifiedVideoCaptureSource;
        var recordingContractsSource = sources.RecordingContractsSource;

        AssertDoesNotContain(libAvSource, "LIBAV_SINK_BURST_EVICT");
        AssertDoesNotContain(flashbackSource, "FLASHBACK_SINK_BURST_EVICT");
        AssertDoesNotContain(libAvSource, "LIBAV_SINK_VIDEO_DROP");
        AssertDoesNotContain(flashbackSource, "FLASHBACK_SINK_VIDEO_DROP");
        AssertDoesNotContain(libAvSource, "_videoSkipsBeforeNextPacket");
        AssertDoesNotContain(flashbackSource, "_videoSkipsBeforeNextPacket");
        AssertDoesNotContain(libAvSource, "SkipRawVideoFrame");
        AssertDoesNotContain(flashbackSource, "SkipRawVideoFrame");
        AssertDoesNotContain(libAvSource, "VideoFramePacket.Skip");
        AssertDoesNotContain(flashbackSource, "VideoFramePacket.Skip");
        AssertDoesNotContain(libAvSource, "_encoder.SkipVideoFrame");
        AssertDoesNotContain(flashbackSource, "_encoder.SkipVideoFrame");
        AssertDoesNotContain(libAvSource, "Interlocked.Add(ref _videoDropsBacklogEviction");
        AssertDoesNotContain(flashbackSource, "Interlocked.Add(ref _videoDropsBacklogEviction");
        AssertContains(captureServiceSource, "FLASHBACK_ENCODER_SUPPORT_PROBE_WARN");
        AssertDoesNotContain(captureServiceSource, "catch { /* Assume unavailable");

        AssertLibAvRecordingQueueOverloadPolicy(libAvSource, recordingContractsSource);
        AssertFlashbackRecordingQueueOverloadPolicy(flashbackSource);
        AssertFlashbackBufferRecoveryPolicy(flashbackSource, flashbackBufferSource, flashbackCleanupSource);
        AssertContains(captureServiceSource, "libAvSink.OnEncodingFailed = OnRecordingBackendFatalError");
        AssertContains(captureServiceSource, "OnFlashbackBackendFatalError,");
        AssertContains(flashbackBackendSource, "flashbackSink.SetFatalErrorCallback(request.FatalErrorCallback)");
        AssertContains(flashbackBackendSource, "newSink.SetFatalErrorCallback(request.FatalErrorCallback)");
        AssertContains(captureServiceSource, "if (sink == null && controller is { IsDisposed: false, IsInitialized: true })");
        AssertContains(captureServiceSource, "controller.PrepareForPreviewDetach();");
        AssertOccursBefore(captureServiceSource, "controller.PrepareForPreviewDetach();", "_unifiedVideoCapture?.SetPreviewSink(sink);");
        AssertContains(captureServiceSource, "controller.UpdatePreviewComponents(sink, _unifiedVideoCapture);");
        AssertContains(captureServiceSource, "FLASHBACK_PLAYBACK_LATE_INIT via SetPreviewFrameSink");
        AssertContains(captureServiceSource, "private void OnFlashbackBackendFatalError");
        AssertContains(captureServiceSource, "FLASHBACK_RECORDING_BACKEND_UNUSABLE_FALLBACK");
        AssertContains(captureServiceSource, "FLASHBACK_EXPORT_REJECTED reason=flashback_recording_active");
        AssertContains(captureServiceSource, "Flashback export is unavailable while Flashback is the active recording backend.");
        AssertContains(captureServiceSource, "FLASHBACK_DISABLE_BLOCKED reason=recording_active");
        AssertContains(captureServiceSource, "Cannot disable Flashback while Flashback recording is active.");
        AssertContains(captureServiceSource, "FLASHBACK_RESTART_BLOCKED reason=recording_active");
        AssertContains(captureServiceSource, "Cannot restart Flashback while Flashback recording is active.");
        var restartFlashbackWithSettings = ExtractSourceBlock(
            captureServiceSource,
            "public Task RestartFlashbackAsync(CaptureSettings settings",
            "private async Task RestartFlashbackCoreAsync");
        AssertOccursBefore(
            restartFlashbackWithSettings,
            "if (_isRecording && IsFlashbackRecordingBackendActive())",
            "UpdateEncodingSettings(settings);");
        var restartFlashbackCore = ExtractSourceBlock(
            captureServiceSource,
            "private async Task RestartFlashbackCoreAsync",
            "    private async Task EnsureFlashbackAudioInputsAsync");
        AssertContains(restartFlashbackCore, "var committedRestartToken = CancellationToken.None;");
        AssertContains(restartFlashbackCore, "await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, committedRestartToken).ConfigureAwait(false);");
        AssertContains(restartFlashbackCore, "Logger.Log(\"FLASHBACK_RESTART_OK\");\n        cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(captureServiceSource, "_flashbackBackend.PreserveRecoverySegments");
        AssertContains(flashbackBackendSource, "MarkSessionPreservedForRecovery");
        AssertContains(flashbackBackendSource, "FLASHBACK_RECOVERY_PRESERVE");
        AssertContains(flashbackBackendSource, "ClearRecoveryPreserve();");
        AssertContains(flashbackBackendSource, "FLASHBACK_PREVIEW_ROLLBACK_PURGE_WARN");
        AssertContains(flashbackBackendSource, "flashbackSink.FrameEncoded -= request.FrameEncodedHandler;");
        AssertContains(flashbackBackendSource, "FLASHBACK_PREVIEW_ROLLBACK_PLAYBACK_WARN");
        AssertContains(captureServiceSource, "_flashbackBackend.ResolveSegmentPurge");
        AssertContains(flashbackBackendSource, "FLASHBACK_SEGMENT_PURGE_BLOCKED");
        AssertContains(captureServiceSource, "WaitForForceRotateIdle(TimeSpan.FromSeconds(10))");
        AssertContains(captureServiceSource, "Flashback backend export rotation did not quiesce before recording start.");
        var flashbackRecordingStartMismatch = ExtractSourceBlock(
            captureServiceSource,
            "var flashbackBackendSettingsChanged = _flashbackBackendSettings == null",
            "await EnsureFlashbackAudioInputsAsync(settings, transitionToken, \"recording_flashback_start\")");
        AssertContains(flashbackRecordingStartMismatch, "FLASHBACK_RECORDING_TOPOLOGY_MISMATCH_REJECT");
        AssertContains(flashbackRecordingStartMismatch, "EnsureFlashbackRecordingTopologyMatches(");
        AssertOccursBefore(
            flashbackRecordingStartMismatch,
            "EnsureFlashbackRecordingTopologyMatches(",
            "await DisposeFlashbackPreviewBackendAsync(transitionToken, purgeSegments: true)");
        AssertContains(captureServiceSource, "bool requireCompleteLiveEdge = false");
        AssertContains(captureServiceSource, "requireCompleteLiveEdge: true");
        AssertContains(captureServiceSource, "FLASHBACK_RECORDING_EXPORT_INCOMPLETE_FAIL");
        AssertContains(captureServiceSource, "live-edge segment was not closed before timeout");
        AssertContains(flashbackBackendSource, "PreserveEndArtifactsOnFailure(exportResult, endResult);");
        AssertContains(flashbackBackendSource, "private static FinalizeResult PreserveEndArtifactsOnFailure(");
        AssertContains(flashbackBackendSource, "exportResult.PreservedArtifacts.Concat(endResult.PreservedArtifacts)");
        AssertOccursBefore(flashbackBackendSource, "PreserveEndArtifactsOnFailure(exportResult, endResult);", "FLASHBACK_RECORDING_EXPORT_OK");
        AssertContains(captureServiceSource, "FLASHBACK_SETTINGS_APPLY_AFTER_RECORDING_DEFERRED");
        var flashbackFailedFinalizeSettingsBranch = ExtractTextBetween(
            captureServiceSource,
            "if (!fbResult.Succeeded)",
            "else if (_pendingFlashbackSettingsChange)");
        AssertContains(flashbackFailedFinalizeSettingsBranch, "var hadPendingFlashbackSettingsChange = _pendingFlashbackSettingsChange;");
        AssertContains(flashbackFailedFinalizeSettingsBranch, "_pendingFlashbackSettingsChange = false;");
        AssertContains(flashbackFailedFinalizeSettingsBranch, "pending_settings={hadPendingFlashbackSettingsChange}");
        AssertOccursBefore(captureServiceSource, "if (!fbResult.Succeeded)", "else if (_pendingFlashbackSettingsChange)");
        AssertContains(captureServiceSource, "preserveFlashbackSegmentsAfterFailedRecordingFinalize");
        AssertContains(captureServiceSource, "FLASHBACK_CLEANUP_PRESERVE_SEGMENTS");
        AssertContains(captureServiceSource, "purgeSegments: !preserveFlashbackSegmentsAfterFailedRecordingFinalize");
        AssertContains(captureServiceSource, "FLASHBACK_CLEANUP_DISPOSE_WARN type={ex.GetType().Name} msg='{ex.Message}'");
        AssertContains(captureServiceSource, "FLASHBACK_CLEANUP_UNIFIED_VIDEO_WARN type={ex.GetType().Name} msg='{ex.Message}'");
        AssertContains(captureServiceSource, "FLASHBACK_CLEANUP_WASAPI_WARN type={ex.GetType().Name} msg='{ex.Message}'");
        AssertContains(captureServiceSource, "SafeClearWasapiCapturePlayback(_wasapiAudioCapture, \"stop_playback\")");
        AssertContains(captureServiceSource, "SafeClearWasapiCapturePlayback(capture, \"detach_capture\")");
        AssertContains(captureServiceSource, "private static void DisposeWasapiPlaybackBestEffort(WasapiAudioPlayback playback)");
        AssertContains(captureServiceSource, "StopWasapiPlaybackBestEffort(newPlayback, \"start_fail\")");
        AssertContains(captureServiceSource, "WASAPI_PLAYBACK_START_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
        AssertContains(captureServiceSource, "WASAPI_PLAYBACK_ATTACH_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
        AssertContains(captureServiceSource, "StopWasapiPlayback();\n            throw;");
        AssertContains(captureServiceSource, "if (ReferenceEquals(_wasapiAudioPlayback, newPlayback))");
        AssertContains(captureServiceSource, "private static void StopWasapiPlaybackBestEffort(WasapiAudioPlayback playback, string operation)");
        AssertContains(captureServiceSource, "WASAPI audio playback dispose warning");
        AssertDoesNotContain(captureServiceSource, "_wasapiAudioCapture?.SetPlayback(null);");
        AssertDoesNotContain(captureServiceSource, "capture.SetPlayback(null);\n        StopWasapiPlayback();");
        AssertContains(captureServiceSource, "CAPTURE_RECORDING_START_FAIL");
        var startRecordingFailure = ExtractSourceBlock(
            captureServiceSource,
            "private async Task RollbackRecordingStartAsync",
            "private async Task DisposeTransientRecordingBackendAsync");
        AssertContains(startRecordingFailure, "RecordLastRecordingFailure(ex);");
        AssertContains(startRecordingFailure, "Recording start rollback cleanup failed");
        AssertContains(startRecordingFailure, "Transient recording backend cleanup failed during start rollback");
        AssertOccursBefore(
            startRecordingFailure,
            "RecordLastRecordingFailure(ex);",
            "await _artifactManager.RollbackAsync(rollback.RecordingContext)");
        AssertDoesNotContain(captureServiceSource, "System.Diagnostics.Trace.TraceWarning($\"Suppressed exception in CaptureService.StartRecordingAsync");
        AssertContains(captureServiceSource, "FLASHBACK_BUFFER_CYCLE_OK mode=preserve_rebuild");
        AssertContains(captureServiceSource, "_flashbackBackend.CycleSinkOnlyAsync(");
        AssertContains(flashbackBackendSource, "FLASHBACK_BUFFER_CLEANUP_PURGE_WARN");
        AssertDoesNotContain(captureServiceSource, "FLASHBACK_BUFFER_DEFERRED_PURGE_SKIP");
        AssertContains(captureServiceSource, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)\n            {\n                throw;\n            }");
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
        var microphoneMonitorText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.MicrophoneMonitor.cs")
            .Replace("\r\n", "\n");
        var libAvPreviewRestoreText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvPreviewRestore.cs")
            .Replace("\r\n", "\n");
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
        AssertContains(libAvStopRecordingBackend, "var sink = _recordingSink;");
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
        AssertContains(captureServiceSource, "private static bool IsFlashbackFinalizeCancellationResult(FinalizeResult result)");
        AssertContains(captureServiceSource, "string.Equals(result.StatusMessage, \"Flashback export cancelled.\", StringComparison.Ordinal)");
        AssertContains(captureServiceSource, "string.Equals(result.StatusMessage, \"Flashback recording finalize cancelled.\", StringComparison.Ordinal)");
        AssertContains(captureServiceSource, "private void PublishRecordingStartedOutcome(string finalOutputPath)");
        AssertContains(captureServiceSource, "private void PublishRecordingFinalizedOutcome(FinalizeResult result, bool updateOutputPath)");
        AssertContains(captureServiceSource, "PublishRecordingStartedOutcome(fbRecordingContext.FinalOutputPath);");
        AssertContains(captureServiceSource, "PublishRecordingStartedOutcome(rollback.RecordingContext.FinalOutputPath);");
        AssertContains(captureServiceSource, "PublishRecordingFinalizedOutcome(fbResult, updateOutputPath: false);");
        AssertContains(captureServiceSource, "PublishRecordingFinalizedOutcome(result, updateOutputPath: true);");
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
            "private Task ScheduleDeferredUnifiedVideoCaptureCleanup");
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
        AssertRecordingQueueHealthSnapshotTelemetry(
            captureServiceSource,
            captureHealthSnapshotRootSource,
            captureSnapshotsSource,
            unifiedVideoCaptureSource);

        return Task.CompletedTask;
    }

}
