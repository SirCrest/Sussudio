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
        AssertContains(captureServiceSource, "flashbackSink.SetFatalErrorCallback(OnFlashbackBackendFatalError)");
        AssertContains(captureServiceSource, "newSink.SetFatalErrorCallback(OnFlashbackBackendFatalError)");
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
        AssertContains(captureServiceSource, "_flashbackBackend.ClearRecoveryPreserve();");
        AssertContains(captureServiceSource, "FLASHBACK_PREVIEW_ROLLBACK_PURGE_WARN");
        AssertContains(captureServiceSource, "flashbackSink.FrameEncoded -= OnFlashbackFrameEncoded;");
        AssertContains(captureServiceSource, "FLASHBACK_PREVIEW_ROLLBACK_PLAYBACK_WARN");
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
            "Logger.Log($\"CAPTURE_RECORDING_START_FAIL",
            "}, cancellationToken);");
        AssertContains(startRecordingFailure, "RecordLastRecordingFailure(ex);");
        AssertContains(startRecordingFailure, "Recording start rollback cleanup failed");
        AssertContains(startRecordingFailure, "Transient recording backend cleanup failed during start rollback");
        AssertOccursBefore(
            startRecordingFailure,
            "RecordLastRecordingFailure(ex);",
            "await _artifactManager.RollbackAsync(recordingContext)");
        AssertDoesNotContain(captureServiceSource, "System.Diagnostics.Trace.TraceWarning($\"Suppressed exception in CaptureService.StartRecordingAsync");
        AssertContains(captureServiceSource, "FLASHBACK_BUFFER_CYCLE_OK mode=preserve_rebuild");
        AssertContains(captureServiceSource, "FLASHBACK_BUFFER_CLEANUP_PURGE_WARN");
        AssertDoesNotContain(captureServiceSource, "FLASHBACK_BUFFER_DEFERRED_PURGE_SKIP");
        AssertContains(captureServiceSource, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)\n            {\n                throw;\n            }");
        var stopRecordingBackend = ExtractSourceBlock(
            captureServiceSource,
            "private async Task<FinalizeResult> StopAndDisposeRecordingBackendAsync",
            "private async Task DisposeTransientRecordingBackendAsync");
        var microphoneMonitorText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.MicrophoneMonitor.cs")
            .Replace("\r\n", "\n");
        AssertContains(stopRecordingBackend, "OperationCanceledException? flashbackCancellationException = null;");
        AssertContains(stopRecordingBackend, "fbResult = FinalizeResult.Failure(fbOutputPath, \"Flashback recording finalize cancelled.\");");
        AssertContains(stopRecordingBackend, "if (cancellationToken.IsCancellationRequested && IsFlashbackFinalizeCancellationResult(fbResult))");
        AssertContains(stopRecordingBackend, "flashbackCancellationException ??= new OperationCanceledException(cancellationToken);");
        AssertContains(stopRecordingBackend, "FLASHBACK_UNIFIED_RECORDING_FINALIZE_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(stopRecordingBackend, "FLASHBACK_BUFFER_CYCLE_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(stopRecordingBackend, "RecordLastFlashbackFailure(ex);");
        AssertContains(stopRecordingBackend, "_flashbackBackend.PreserveRecoverySegments(\"buffer_cycle_failed\");");
        AssertContains(stopRecordingBackend, "BeginFlashbackBackendCleanup(ex);");
        AssertContains(stopRecordingBackend, "FLASHBACK_MIC_RESTART_WARN type={ex.GetType().Name} error='{ex.Message}'");
        AssertOccursBefore(
            stopRecordingBackend,
            "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)",
            "FLASHBACK_UNIFIED_RECORDING_FINALIZE_FAIL");
        AssertOccursBefore(
            stopRecordingBackend,
            "if (cancellationToken.IsCancellationRequested && IsFlashbackFinalizeCancellationResult(fbResult))",
            "_lastRecordingIntegrity = BuildRecordingIntegritySummary(");
        AssertOccursBefore(
            stopRecordingBackend,
            "fbResult = FinalizeResult.Failure(fbOutputPath, \"Flashback recording finalize cancelled.\");",
            "_recordingStopwatch.Stop();");
        AssertOccursBefore(
            stopRecordingBackend,
            "_lastPreservedArtifacts = fbResult.PreservedArtifacts;",
            "throw flashbackCancellationException;");
        var postFinalizeCycle = ExtractSourceBlock(
            stopRecordingBackend,
            "// If settings changed during recording (format, buffer duration, etc.),",
            "_recordingStopwatch.Stop();");
        AssertContains(postFinalizeCycle, "flashbackCancellationException ??= new OperationCanceledException(cancellationToken);");
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
            stopRecordingBackend,
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
        var standardMicMonitorRestart = ExtractSourceBlock(
            stopRecordingBackend,
            "var wasapiAudioCaptureFaulted = Volatile.Read(ref _wasapiAudioCaptureFaulted);",
            "_lastOutputPath = result.OutputPath;");
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
            "private async Task CycleFlashbackBufferAsync");
        AssertOccursBefore(disposeFlashbackPreviewBackendCore, "cancellationToken.ThrowIfCancellationRequested();", "CleanupFlashbackBackendArtifactsAfterExportAsync(");
        AssertOccursBefore(disposeFlashbackPreviewBackendCore, "_flashbackBackend.TakePlaybackController()", "flashbackPlaybackController.GoLive();");
        AssertContains(disposeFlashbackPreviewBackendCore, "_flashbackBackend.DetachProducers(");
        AssertContains(disposeFlashbackPreviewBackendCore, "\"FLASHBACK_PREVIEW_DETACH_WARN\"");
        AssertContains(disposeFlashbackPreviewBackendCore, "await flashbackSink.StopAsync(CancellationToken.None).ConfigureAwait(false);");
        AssertOccursBefore(disposeFlashbackPreviewBackendCore, "_flashbackBackend.DetachProducers(", "await flashbackSink.StopAsync(CancellationToken.None).ConfigureAwait(false);");
        AssertOccursBefore(disposeFlashbackPreviewBackendCore, "_flashbackBackend.Clear();", "cancellationToken.ThrowIfCancellationRequested();");
        AssertOccursBefore(disposeFlashbackPreviewBackendCore, "ScheduleDeferredFlashbackBackendCleanup(", "cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(disposeFlashbackPreviewBackendCore, "var cleanupCompleted = await CleanupFlashbackBackendArtifactsAfterExportAsync(");
        AssertContains(disposeFlashbackPreviewBackendCore, "ScheduleDeferredFlashbackBackendCleanup(\n                Task.Delay(TimeSpan.FromSeconds(1)),");
        var deferredFlashbackBackendCleanup = ExtractSourceBlock(
            captureServiceSource,
            "private void ScheduleDeferredFlashbackBackendCleanup",
            "private Task ScheduleDeferredUnifiedVideoCaptureCleanup");
        AssertContains(deferredFlashbackBackendCleanup, "FlashbackBackendArtifactCleanupRequest request,");
        AssertContains(deferredFlashbackBackendCleanup, "CleanupFlashbackBackendArtifactsAfterExportAsync(");
        AssertContains(deferredFlashbackBackendCleanup, "if (cleanupCompleted)");
        AssertContains(deferredFlashbackBackendCleanup, "FLASHBACK_BACKEND_DEFERRED_CLEANUP_OK reason='{request.Reason}' attempt={attempt}");
        AssertContains(deferredFlashbackBackendCleanup, "else if (attempt < 3)");
        AssertContains(deferredFlashbackBackendCleanup, "FLASHBACK_BACKEND_DEFERRED_CLEANUP_RETRY reason='{request.Reason}' attempt={attempt} next_attempt={nextAttempt}");
        AssertContains(deferredFlashbackBackendCleanup, "Task.Delay(TimeSpan.FromSeconds(5))");
        AssertContains(deferredFlashbackBackendCleanup, "FLASHBACK_BACKEND_DEFERRED_CLEANUP_GIVE_UP reason='{request.Reason}' attempt={attempt} preserve_segments=true");
        var flashbackBackendArtifactCleanup = ExtractSourceBlock(
            captureServiceSource,
            "private async Task<bool> CleanupFlashbackBackendArtifactsAfterExportAsync",
            "private Task ScheduleDeferredUnifiedVideoCaptureCleanup");
        AssertContains(flashbackBackendArtifactCleanup, "FlashbackBackendArtifactCleanupRequest request,");
        AssertContains(flashbackBackendArtifactCleanup, "WaitAsync(TimeSpan.FromSeconds(30), CancellationToken.None)");
        AssertOccursBefore(flashbackBackendArtifactCleanup, "request.FlashbackExporter.Dispose();", "request.BufferManager.PurgeAllSegments();");
        AssertOccursBefore(flashbackBackendArtifactCleanup, "request.FlashbackExporter.Dispose();", "request.BufferManager.Dispose();");
        AssertOccursBefore(flashbackBackendArtifactCleanup, "WaitAsync(TimeSpan.FromSeconds(30), CancellationToken.None)", "request.FlashbackExporter.Dispose();");
        AssertOccursBefore(flashbackBackendArtifactCleanup, "WaitAsync(TimeSpan.FromSeconds(30), CancellationToken.None)", "request.BufferManager.PurgeAllSegments();");
        var cycleFlashbackBuffer = ExtractSourceBlock(
            captureServiceSource,
            "private async Task CycleFlashbackBufferAsync",
            "private async Task<FinalizeResult> FinalizeFlashbackRecordingAsync");
        AssertContains(cycleFlashbackBuffer, "var committedCycleToken = CancellationToken.None;");
        AssertContains(cycleFlashbackBuffer, "FLASHBACK_CYCLE_STOP_CANCEL_DEFERRED");
        AssertContains(cycleFlashbackBuffer, "FLASHBACK_BUFFER_CYCLE_CANCEL_DEFERRED");
        AssertDoesNotContain(cycleFlashbackBuffer, "cancellationToken: cancellationToken");
        AssertOccursBefore(
            cycleFlashbackBuffer,
            "await oldSink.DisposeAsync().ConfigureAwait(false);",
            "_flashbackBackend.ClearSinkAndSettings();");
        AssertContains(cycleFlashbackBuffer, "var oldPlaybackController = _flashbackBackend.TakePlaybackController();");
        AssertContains(cycleFlashbackBuffer, "oldPlaybackController.GoLive();");
        AssertContains(cycleFlashbackBuffer, "oldPlaybackController.Dispose();");
        AssertOccursBefore(
            cycleFlashbackBuffer,
            "oldPlaybackController.Dispose();",
            "bufferManager.PurgeCompletedSegments();");
        AssertOccursBefore(
            cycleFlashbackBuffer,
            "oldPlaybackController.Dispose();",
            "_flashbackBackend.DetachProducers(");
        AssertContains(cycleFlashbackBuffer, "_flashbackBackend.DetachProducers(");
        AssertContains(cycleFlashbackBuffer, "\"FLASHBACK_CYCLE_DETACH_WARN\"");
        var cycleNewSinkStart = ExtractSourceBlock(
            cycleFlashbackBuffer,
            "var newSink = new FlashbackEncoderSink(bufferManager);",
            "finally");
        AssertContains(cycleNewSinkStart, "committedCycleToken,");
        AssertContains(cycleNewSinkStart, "FLASHBACK_BUFFER_CYCLE_CANCEL_DEFERRED");
        AssertContains(cycleNewSinkStart, "newSink.FrameEncoded -= OnFlashbackFrameEncoded;");
        AssertContains(cycleNewSinkStart, "unifiedVideoCapture.SetFlashbackSink(null);");
        AssertContains(cycleNewSinkStart, "_wasapiAudioCapture?.DetachFlashbackSink();");
        AssertContains(cycleNewSinkStart, "_microphoneCapture?.SetAudioWriter(null);");
        AssertContains(cycleNewSinkStart, "var playbackController = new FlashbackPlaybackController(bufferManager);");
        AssertContains(cycleNewSinkStart, "playbackController.GpuDecodeEnabled = _currentSettings.FlashbackGpuDecode;");
        AssertContains(cycleNewSinkStart, "playbackController.Initialize(_previewFrameSink, unifiedVideoCapture, _wasapiAudioPlayback, _wasapiAudioCapture);");
        AssertContains(cycleNewSinkStart, "_flashbackPlaybackController = playbackController;");
        AssertContains(cycleNewSinkStart, "FLASHBACK_CYCLE_NEW_SINK_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(cycleNewSinkStart, "FLASHBACK_CYCLE_NEW_SINK_DETACH_WARN");
        AssertContains(captureServiceSource, "request.PurgeSegments");
        AssertContains(captureServiceSource, "new FlashbackPreviewBackendDisposalRequest(");
        AssertContains(captureServiceSource, "new FlashbackBackendArtifactCleanupRequest(");
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
        AssertContains(captureServiceSource, "FLASHBACK_PREVIEW_ROLLBACK_DETACH_WARN target=video");
        AssertContains(captureServiceSource, "FLASHBACK_PREVIEW_ROLLBACK_DETACH_WARN target=audio");
        AssertContains(captureServiceSource, "FLASHBACK_PREVIEW_ROLLBACK_DETACH_WARN target=microphone");
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
