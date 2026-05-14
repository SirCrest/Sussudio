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
        var flashbackBufferDispose = ExtractSourceBlock(
            flashbackBufferSource,
            "public void Dispose()",
            "private void ThrowIfDisposed()");
        AssertDoesNotContain(flashbackBufferDispose, "PurgeAllSegments()");
        AssertContains(flashbackBufferSource, "RecoveryPreserveMarkerFileName");
        AssertContains(flashbackBufferSource, "MarkSessionPreservedForRecovery");
        AssertContains(flashbackBufferSource, "public bool IsSessionPreservedForRecovery");
        AssertContains(flashbackBufferSource, "private bool _preserveSessionForRecovery;");
        AssertContains(flashbackBufferSource, "private bool IsSessionPreservedForRecoveryUnsafe()");
        AssertContains(flashbackBufferSource, "FLASHBACK_BUFFER_PURGE_SKIP reason=recovery_preserved");
        AssertContains(flashbackBufferSource, "FLASHBACK_BUFFER_DISPOSE_PRESERVE_RECOVERY");
        AssertContains(flashbackCleanupSource, "FLASHBACK_STALE_SESSION_PRESERVE_SKIP");
        AssertContains(flashbackCleanupSource, "File.Exists(Path.Combine(fullPath, RecoveryPreserveMarkerFileName))");
        AssertContains(flashbackBufferSource, "DeleteFileForEviction(oldest.Path, oldest.SizeBytes, \"valid_window\")");
        AssertContains(flashbackBufferSource, "DeleteFileForEviction(oldest.Path, oldest.SizeBytes, \"disk_budget\")");
        AssertContains(flashbackBufferSource, "private static bool DeleteEvictedFile");
        AssertContains(flashbackBufferSource, "FLASHBACK_BUFFER_EVICT_DELETE_WARN");
        AssertContains(flashbackBufferSource, "FLASHBACK_BUFFER_SEGMENT_EVICT_DELETED");
        AssertContains(flashbackBufferSource, "public void MarkActiveSegmentStart(string path, TimeSpan startPts)");
        AssertContains(flashbackSource, "_bufferManager.MarkActiveSegmentStart(tsPath, _segmentStartPts);");
        AssertContains(flashbackSource, "_bufferManager.MarkActiveSegmentStart(newPath, _segmentStartPts);");
        var flashbackVideoEnqueue = ExtractSourceBlock(
            flashbackSource,
            "private VideoEnqueueResult TryEnqueueVideoPacket",
            "private VideoEnqueueResult TryEnqueueGpuPacket");
        var flashbackGpuEnqueue = ExtractSourceBlock(
            flashbackSource,
            "private VideoEnqueueResult TryEnqueueGpuPacket",
            "private void FailEncoding");
        var flashbackAudioEnqueue = ExtractSourceBlock(
            flashbackSource,
            "private bool TryEnqueueAudioPacket",
            "private static void ReturnRemainingBuffers");
        AssertOccursBefore(flashbackVideoEnqueue, "GetVideoEnqueueRejectReason(isGpu: false)", "TryWriteVideoPacket(queue, packet)");
        AssertOccursBefore(flashbackGpuEnqueue, "GetVideoEnqueueRejectReason(isGpu: true)", "TryWriteGpuPacket(queue, packet)");
        AssertOccursBefore(flashbackAudioEnqueue, "Volatile.Read(ref _forceRotateDraining)", "TryWriteAudioPacket(queue, packet, ref queueDepth, \"audio\")");
        AssertContains(flashbackVideoEnqueue, "var rejectReason = GetVideoEnqueueRejectReason(isGpu: false);");
        AssertContains(flashbackVideoEnqueue, "TrackVideoQueueRejected(rejectReason);");
        AssertContains(flashbackGpuEnqueue, "var rejectReason = GetVideoEnqueueRejectReason(isGpu: true);");
        AssertContains(flashbackGpuEnqueue, "TrackGpuQueueRejected(rejectReason);");
        AssertContains(flashbackAudioEnqueue, "if (_disposed ||\n            !_started ||");
        AssertContains(flashbackGpuEnqueue, "lock (_videoQueueSync)");
        AssertContains(flashbackAudioEnqueue, "lock (_videoQueueSync)");
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
            "public Task StopRecordingAsync");
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
        AssertContains(flashbackMicMonitorRestart, "WasapiAudioCapture? micCapture = null;");
        AssertContains(flashbackMicMonitorRestart, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)");
        AssertContains(flashbackMicMonitorRestart, "flashbackCancellationException ??= new OperationCanceledException(cancellationToken);");
        AssertContains(flashbackMicMonitorRestart, "FLASHBACK_MIC_RESTART_DISPOSE_WARN");
        AssertOccursBefore(
            flashbackMicMonitorRestart,
            "micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));",
            "_microphoneCapture = micCapture;");
        AssertContains(flashbackMicMonitorRestart, "_microphoneCapture = micCapture;\n                        micCapture = null;");
        AssertContains(captureServiceSource, "private static bool IsFlashbackFinalizeCancellationResult(FinalizeResult result)");
        AssertContains(captureServiceSource, "string.Equals(result.StatusMessage, \"Flashback export cancelled.\", StringComparison.Ordinal)");
        AssertContains(captureServiceSource, "string.Equals(result.StatusMessage, \"Flashback recording finalize cancelled.\", StringComparison.Ordinal)");
        var standardMicMonitorRestart = ExtractSourceBlock(
            stopRecordingBackend,
            "var wasapiAudioCaptureFaulted = Volatile.Read(ref _wasapiAudioCaptureFaulted);",
            "_lastOutputPath = result.OutputPath;");
        AssertContains(standardMicMonitorRestart, "WasapiAudioCapture? micCapture = null;");
        AssertContains(standardMicMonitorRestart, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)");
        AssertContains(standardMicMonitorRestart, "cancellationException ??= new OperationCanceledException(cancellationToken);");
        AssertContains(standardMicMonitorRestart, "MIC_MONITOR_RESTART_DISPOSE_WARN");
        AssertOccursBefore(
            standardMicMonitorRestart,
            "micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));",
            "_microphoneCapture = micCapture;");
        AssertContains(standardMicMonitorRestart, "_microphoneCapture = micCapture;\n                micCapture = null;");
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
        AssertContains(deferredFlashbackBackendCleanup, "CleanupFlashbackBackendArtifactsAfterExportAsync(");
        AssertContains(deferredFlashbackBackendCleanup, "if (cleanupCompleted)");
        AssertContains(deferredFlashbackBackendCleanup, "FLASHBACK_BACKEND_DEFERRED_CLEANUP_OK reason='{reason}' attempt={attempt}");
        AssertContains(deferredFlashbackBackendCleanup, "else if (attempt < 3)");
        AssertContains(deferredFlashbackBackendCleanup, "FLASHBACK_BACKEND_DEFERRED_CLEANUP_RETRY reason='{reason}' attempt={attempt} next_attempt={nextAttempt}");
        AssertContains(deferredFlashbackBackendCleanup, "Task.Delay(TimeSpan.FromSeconds(5))");
        AssertContains(deferredFlashbackBackendCleanup, "FLASHBACK_BACKEND_DEFERRED_CLEANUP_GIVE_UP reason='{reason}' attempt={attempt} preserve_segments=true");
        var flashbackBackendArtifactCleanup = ExtractSourceBlock(
            captureServiceSource,
            "private async Task<bool> CleanupFlashbackBackendArtifactsAfterExportAsync",
            "private Task ScheduleDeferredUnifiedVideoCaptureCleanup");
        AssertContains(flashbackBackendArtifactCleanup, "WaitAsync(TimeSpan.FromSeconds(30), CancellationToken.None)");
        AssertOccursBefore(flashbackBackendArtifactCleanup, "flashbackExporter.Dispose();", "bufferManager.PurgeAllSegments();");
        AssertOccursBefore(flashbackBackendArtifactCleanup, "flashbackExporter.Dispose();", "bufferManager.Dispose();");
        AssertOccursBefore(flashbackBackendArtifactCleanup, "WaitAsync(TimeSpan.FromSeconds(30), CancellationToken.None)", "flashbackExporter.Dispose();");
        AssertOccursBefore(flashbackBackendArtifactCleanup, "WaitAsync(TimeSpan.FromSeconds(30), CancellationToken.None)", "bufferManager.PurgeAllSegments();");
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
        AssertContains(captureServiceSource, "purgeSegments: purgeSegments");
        AssertContains(captureServiceSource, "purgeSegments: effectivePurgeSegments");
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
        AssertContains(unifiedVideoCaptureSource, "encoder is IRawVideoFrameTryEncoder");
        AssertContains(unifiedVideoCaptureSource, "leaseEncoder is IRawVideoFrameLeaseTryEncoder");
        AssertContains(unifiedVideoCaptureSource, "encoder is IGpuVideoFrameTryEncoder");
        AssertContains(unifiedVideoCaptureSource, "BeginFlashbackRecordingAccounting");
        AssertContains(unifiedVideoCaptureSource, "RecordFlashbackRecordingAccounting");
        AssertContains(unifiedVideoCaptureSource, "sink.IsRecordingActive");
        AssertContains(unifiedVideoCaptureSource, "if (accepted)");
        AssertContains(unifiedVideoCaptureSource, "public MjpegPipelineTimingSnapshot GetMjpegPipelineTimingSnapshot()");
        AssertContains(unifiedVideoCaptureSource, "private static MjpegPipelineTimingMetrics CreateMjpegPipelineTimingSummary");
        AssertContains(captureServiceSource, "var timingSnapshot = unifiedVideoCapture.GetMjpegPipelineTimingSnapshot();");
        AssertContains(captureServiceSource, "RecordLastRecordingFailure");
        AssertContains(captureServiceSource, "RecordLastFlashbackFailure");
        AssertContains(captureServiceSource, "ClearLastRecordingFailure");
        AssertContains(captureServiceSource, "ClearLastFlashbackFailure");
        AssertContains(captureSnapshotsSource, "GetLastFailureTelemetry");
        AssertContains(captureSnapshotsSource, "IsFlashbackRecordingBackendOwnedByRecording()");
        AssertContains(captureHealthSnapshotRootSource, "var mjpegHealth = CaptureMjpegHealthSnapshotFields(unifiedVideoCapture);");
        AssertDoesNotContain(captureHealthSnapshotRootSource, "GetMjpegPipelineTimingSnapshot()");
        AssertContains(captureSnapshotsSource, "var timingSnapshot = unifiedVideoCapture?.GetMjpegPipelineTimingSnapshot();");
        AssertContains(captureSnapshotsSource, "private MjpegHealthSnapshotFields CaptureMjpegHealthSnapshotFields(");
        AssertDoesNotContain(captureSnapshotsSource, "unifiedVideoCapture?.GetMjpegPipelineTimingMetrics()");
        AssertDoesNotContain(captureSnapshotsSource, "unifiedVideoCapture?.GetFullMjpegPipelineTimingMetrics()");
        AssertContains(captureSnapshotsSource, "var flashbackVideoQueueLatencyMetrics = fbSink?.VideoQueueLatencyMetrics");
        AssertContains(captureSnapshotsSource, "sink?.VideoQueueLatencyMetrics ??");
        AssertDoesNotContain(captureSnapshotsSource, "var flashbackIsRecordingBackend = _isRecording && IsFlashbackRecordingBackendActive()");
        AssertContains(captureSnapshotsSource, "RecordingEncodingFailureMessage");
        AssertContains(captureSnapshotsSource, "RecordingVideoFramesSubmittedToEncoder = recordingHealth.VideoFramesSubmitted");
        AssertContains(captureSnapshotsSource, "RecordingVideoQueueLatencyP95Ms = recordingHealth.VideoQueueLatencyMetrics.P95Ms");
        AssertContains(captureSnapshotsSource, "RecordingVideoQueueLatencyP99Ms = recordingHealth.VideoQueueLatencyMetrics.P99Ms");
        AssertContains(captureSnapshotsSource, "RecordingVideoQueueOldestFrameAgeMs = recordingHealth.VideoQueueOldestFrameAgeMs");
        AssertContains(captureSnapshotsSource, "RecordingVideoBackpressureWaitMs = recordingHealth.VideoBackpressureWaitMs");
        AssertContains(captureSnapshotsSource, "fbSink?.VideoEncoderPacketsWritten ?? 0");
        AssertContains(captureSnapshotsSource, "fbSink?.VideoSequenceGaps ?? 0");
        AssertContains(captureSnapshotsSource, "fbSink?.VideoQueueOldestFrameAgeMs ?? 0");
        AssertContains(captureSnapshotsSource, "FlashbackVideoQueueLatencyP99Ms = flashbackQueues.VideoQueueLatencyMetrics.P99Ms");
        AssertContains(captureSnapshotsSource, "fbSink?.VideoBackpressureWaitMs ?? 0");
        AssertContains(captureSnapshotsSource, "FatalCleanupInProgress = fatalCleanupInProgress");
        AssertContains(captureSnapshotsSource, "FlashbackCleanupInProgress = flashbackCleanupInProgress");
        AssertContains(captureSnapshotsSource, "fbSink?.IsForceRotateActive ?? false");
        AssertContains(captureSnapshotsSource, "fbSink?.IsForceRotateRequested ?? false");
        AssertContains(captureSnapshotsSource, "fbSink?.IsForceRotateDraining ?? false");
        AssertContains(captureSnapshotsSource, "FlashbackEncodingFailureMessage");
        AssertContains(captureSnapshotsSource, "FlashbackStartupCacheBytes = flashbackBuffer.StartupCacheBytes");
        AssertContains(captureSnapshotsSource, "bufMgr?.StartupCacheBytes ?? 0");
        AssertContains(captureSnapshotsSource, "FlashbackTempDriveFreeBytes = flashbackBuffer.TempDriveFreeBytes");
        AssertContains(captureSnapshotsSource, "bufMgr?.TempDriveAvailableFreeBytes ?? 0");
        var sharedFormatterSource = ReadAutomationSnapshotFormatterSource();
        var ssctlFormatterSource = ReadSsctlSnapshotFormatterSource();
        var mcpAppStateSource = ReadRepoFile("tools/McpServer/Tools/AppStateTools.cs");
        AssertContains(sharedFormatterSource, "FlashbackEncodingFailed");
        AssertContains(sharedFormatterSource, "FlashbackStartupCacheBytes");
        AssertContains(sharedFormatterSource, "FlashbackCleanupInProgress");
        AssertContains(sharedFormatterSource, "FlashbackForceRotateActive");
        AssertContains(sharedFormatterSource, "FlashbackForceRotateRequested");
        AssertContains(sharedFormatterSource, "FlashbackForceRotateDraining");
        AssertContains(ssctlFormatterSource, "FlashbackEncodingFailed");
        AssertContains(ssctlFormatterSource, "FlashbackStartupCacheBytes");
        AssertContains(ssctlFormatterSource, "FlashbackCleanupInProgress");
        AssertContains(ssctlFormatterSource, "FlashbackForceRotateActive");
        AssertContains(ssctlFormatterSource, "FlashbackForceRotateRequested");
        AssertContains(ssctlFormatterSource, "FlashbackForceRotateDraining");
        AssertContains(mcpAppStateSource, "FormatSnapshot(response, includeFlashback: true)");
        AssertOccursBefore(
            sharedFormatterSource,
            "var flashbackFailed = Get(snapshot, \"FlashbackEncodingFailed\", \"false\");",
            "builder.AppendLine(\"== Flashback ==\");");
        AssertOccursBefore(
            ssctlFormatterSource,
            "var flashbackFailed = AutomationSnapshotFormatter.Get(snapshot, \"FlashbackEncodingFailed\", \"false\");",
            "builder.AppendLine(\"== Flashback ==\");");

        return Task.CompletedTask;
    }

}
