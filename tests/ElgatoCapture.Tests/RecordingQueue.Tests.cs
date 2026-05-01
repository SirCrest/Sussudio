using System;
using System.Threading.Tasks;

static partial class Program
{
    private static Task RecordingVideoQueues_FailExplicitlyInsteadOfEvictingFrames()
    {
        var libAvSource = ReadRepoFile("ElgatoCapture/Services/Recording/LibAvRecordingSink.cs");
        var flashbackSource = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackEncoderSink.cs");
        var flashbackBufferSource = ReadRepoFile("ElgatoCapture/Services/Flashback/FlashbackBufferManager.cs");
        var captureServiceSource = ReadRepoFile("ElgatoCapture/Services/Capture/CaptureService.cs");
        var captureSnapshotsSource = ReadRepoFile("ElgatoCapture/Services/Capture/CaptureService.Snapshots.cs");
        var unifiedVideoCaptureSource = ReadRepoFile("ElgatoCapture/Services/Capture/UnifiedVideoCapture.cs");
        var recordingContractsSource = ReadRepoFile("ElgatoCapture/Services/Recording/RecordingContracts.cs");

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

        AssertContains(libAvSource, "LibAv recording video queue overloaded");
        AssertContains(libAvSource, "QueueBackpressureTimeoutMs = 250");
        AssertContains(libAvSource, "Thread.Sleep(1)");
        AssertContains(libAvSource, "after {QueueBackpressureTimeoutMs}ms backpressure");
        AssertContains(libAvSource, "LIBAV_SINK_VIDEO_OVERLOAD");
        AssertContains(libAvSource, "LIBAV_SINK_FATAL");
        AssertContains(libAvSource, "OnEncodingFailed?.Invoke");
        AssertContains(libAvSource, "public bool EncodingFailed");
        AssertContains(libAvSource, "public string? EncodingFailureMessage");
        AssertContains(libAvSource, "public int VideoQueueMaxDepth");
        AssertContains(libAvSource, "public long VideoFramesSubmittedToEncoder");
        AssertContains(libAvSource, "public long VideoEncoderPacketsWritten");
        AssertContains(libAvSource, "public long VideoSequenceGaps");
        AssertContains(libAvSource, "public long VideoQueueOldestFrameAgeMs");
        AssertContains(libAvSource, "public double VideoQueueLatencyP95Ms");
        AssertContains(libAvSource, "public long VideoBackpressureWaitMs");
        AssertContains(libAvSource, "public long VideoBackpressureEvents");
        AssertContains(libAvSource, "RecordVideoBackpressure(backpressureStartTick, Environment.TickCount64)");
        AssertContains(libAvSource, "TrackQueuedVideoTick(packet.EnqueueTick)");
        AssertContains(libAvSource, "RemoveQueuedVideoTick(packet.EnqueueTick)");
        AssertContains(libAvSource, "RecordVideoPacketDequeued(packet)");
        AssertContains(libAvSource, "public int GpuQueueMaxDepth");
        AssertContains(libAvSource, "public int CudaQueueMaxDepth");
        AssertContains(recordingContractsSource, "IRawVideoFrameTryEncoder");
        AssertContains(recordingContractsSource, "IGpuVideoFrameTryEncoder");
        AssertContains(libAvSource, "IRawVideoFrameTryEncoder");
        AssertContains(libAvSource, "IGpuVideoFrameTryEncoder");
        AssertContains(libAvSource, "public bool TryEnqueueRawVideoFrame");
        AssertContains(libAvSource, "public bool TryEnqueueGpuVideoFrame");
        AssertContains(libAvSource, "VideoEnqueueResult.Rejected");
        AssertContains(libAvSource, "TryEnqueueGpuPacket");
        AssertContains(libAvSource, "TryEnqueueCudaPacket");
        AssertContains(libAvSource, "LibAv GPU recording queue overloaded");
        AssertContains(libAvSource, "LibAv CUDA recording queue overloaded");
        AssertContains(libAvSource, "if (!_started");
        AssertContains(libAvSource, "Volatile.Read(ref _encodingFailure) != null");
        AssertContains(flashbackSource, "Flashback recording video queue overloaded");
        AssertContains(flashbackSource, "QueueBackpressureTimeoutMs = 250");
        AssertContains(flashbackSource, "Thread.Sleep(1)");
        AssertContains(flashbackSource, "after {QueueBackpressureTimeoutMs}ms backpressure");
        AssertContains(flashbackSource, "var p010FrameSize = MfSourceReaderVideoCapture.GetFrameSizeBytes(_width, _height, isP010: true)");
        AssertContains(flashbackSource, "VideoFramePacket.Frame(buffer, expectedSize, enqueueTick, isP010)");
        AssertContains(flashbackSource, "MfSourceReaderVideoCapture.GetFrameSizeBytes(w, h, packet.IsP010)");
        AssertContains(flashbackSource, "lease.PixelFormat == PooledVideoPixelFormat.P010");
        AssertContains(flashbackSource, "Flashback GPU recording queue overloaded");
        AssertContains(flashbackSource, "FLASHBACK_SINK_VIDEO_OVERLOAD");
        AssertContains(flashbackSource, "FLASHBACK_SINK_FATAL");
        AssertContains(flashbackSource, "_onFatalError?.Invoke");
        AssertDoesNotContain(flashbackSource, "catch { /* Callback must not mask the original error */ }");
        AssertContains(flashbackSource, "Logger.Log($\"FLASHBACK_SINK_FATAL_CALLBACK_FAIL type={callbackEx.GetType().Name} msg={callbackEx.Message}\");");
        AssertContains(flashbackSource, "public bool EncodingFailed");
        AssertContains(flashbackSource, "public string? EncodingFailureMessage");
        AssertContains(flashbackSource, "public bool CanBeginRecording");
        AssertContains(flashbackSource, "public bool IsRecordingActive");
        AssertContains(flashbackSource, "Volatile.Read(ref _recordingActive) == 0");
        AssertContains(flashbackSource, "Cannot begin recording: flashback recording is already active.");
        AssertOccursBefore(flashbackSource, "Cannot begin recording: flashback recording is already active.", "_bufferManager.PauseEviction();");
        AssertOccursBefore(flashbackSource, "_bufferManager.PauseEviction();", "Volatile.Write(ref _recordingActive, 1);");
        AssertContains(flashbackSource, "public bool IsForceRotateActive");
        AssertContains(flashbackSource, "WaitForForceRotateIdle");
        AssertContains(flashbackSource, "CompletePendingForceRotateWithEmptyResult");
        AssertContains(flashbackSource, "TaskCompletionSource<IReadOnlyList<string>>? supersededTcs;");
        AssertContains(flashbackSource, "supersededTcs = _forceRotateTcs;");
        AssertContains(flashbackSource, "FLASHBACK_SINK_FORCE_ROTATE_SUPERSEDED");
        AssertContains(flashbackSource, "supersededTcs.TrySetResult(Array.Empty<string>())");
        AssertContains(flashbackSource, "if (!RotateSegment(currentPts))\n                            {\n                                localTcs?.TrySetResult(Array.Empty<string>());\n                                madeProgress = true;\n                                continue;\n                            }");
        AssertContains(flashbackSource, "private bool RotateSegment(TimeSpan currentPts)");
        AssertContains(flashbackSource, "return true;\n        }\n        catch (Exception ex)");
        AssertContains(flashbackSource, "Logger.Log($\"FLASHBACK_SINK_ROTATE_FAIL type={ex.GetType().Name} msg={ex.Message}\");\n            return false;");
        AssertContains(flashbackSource, "TryCancelPendingForceRotate(tcs)");
        AssertContains(flashbackSource, "ReferenceEquals(_forceRotateTcs, requestTcs)");
        AssertContains(flashbackSource, "cleared_pending={clearedPending}");
        AssertContains(flashbackSource, "_forceRotateRequested = false;");
        AssertContains(flashbackSource, "Volatile.Write(ref _forceRotateDraining, false);");
        AssertContains(flashbackSource, "Cannot begin recording: flashback export rotation is still draining.");
        AssertContains(flashbackSource, "if (_ownsBufferManager)");
        AssertOccursBefore(flashbackSource, "if (_ownsBufferManager)\n            {\n                _bufferManager.PurgeAllSegments();", "_encoder.Dispose();");
        AssertContains(flashbackSource, "CancelRecordingStartRollback");
        AssertContains(flashbackSource, "var wasRecording = Interlocked.Exchange(ref _recordingActive, 0) != 0");
        AssertContains(flashbackSource, "if (!wasRecording)\n        {\n            const string message = \"Flashback recording was not active.\";");
        AssertContains(flashbackSource, "FLASHBACK_RECORDING_END_REJECTED");
        AssertContains(flashbackSource, "finally");
        AssertContains(flashbackSource, "_bufferManager.ResumeEviction()");
        AssertContains(flashbackSource, "if (LastRecordingEndPts < LastRecordingStartPts)\n                {\n                    LastRecordingEndPts = _bufferManager.LatestPts;\n                    if (LastRecordingEndPts < LastRecordingStartPts)\n                    {\n                        LastRecordingEndPts = LastRecordingStartPts;\n                    }\n                }");
        AssertContains(flashbackSource, "Cannot begin recording: flashback encoder is not running.");
        AssertContains(flashbackSource, "public int VideoQueueMaxDepth");
        AssertContains(flashbackSource, "public long VideoFramesSubmittedToEncoder");
        AssertContains(flashbackSource, "public long VideoEncoderPacketsWritten");
        AssertContains(flashbackSource, "public long VideoSequenceGaps");
        AssertContains(flashbackSource, "public long VideoQueueOldestFrameAgeMs");
        AssertContains(flashbackSource, "public double VideoQueueLatencyP95Ms");
        AssertContains(flashbackSource, "public long VideoBackpressureWaitMs");
        AssertContains(flashbackSource, "public long VideoBackpressureEvents");
        AssertContains(flashbackSource, "RecordVideoBackpressure(backpressureStartTick, Environment.TickCount64)");
        AssertContains(flashbackSource, "TrackQueuedVideoTick(packet.EnqueueTick)");
        AssertContains(flashbackSource, "RemoveQueuedVideoTick(packet.EnqueueTick)");
        AssertContains(flashbackSource, "RecordVideoPacketDequeued(packet)");
        AssertContains(flashbackSource, "public int GpuQueueMaxDepth");
        AssertContains(flashbackSource, "IRawVideoFrameTryEncoder");
        AssertContains(flashbackSource, "IGpuVideoFrameTryEncoder");
        AssertContains(flashbackSource, "public bool TryEnqueueRawVideoFrame");
        AssertContains(flashbackSource, "public bool TryEnqueueGpuVideoFrame");
        AssertContains(flashbackSource, "VideoEnqueueResult.Rejected");
        AssertContains(flashbackSource, "TryEnqueueGpuPacket");
        AssertContains(flashbackSource, "Volatile.Read(ref _forceRotateDraining)");
        AssertContains(flashbackSource, "Volatile.Read(ref _encodingFailure) != null");
        var flashbackBufferDispose = ExtractSourceBlock(
            flashbackBufferSource,
            "public void Dispose()",
            "private void EvictOldestSegments()");
        AssertDoesNotContain(flashbackBufferDispose, "PurgeAllSegments()");
        AssertContains(flashbackBufferSource, "RecoveryPreserveMarkerFileName");
        AssertContains(flashbackBufferSource, "MarkSessionPreservedForRecovery");
        AssertContains(flashbackBufferSource, "FLASHBACK_STALE_SESSION_PRESERVE_SKIP");
        AssertContains(flashbackBufferSource, "File.Exists(Path.Combine(fullPath, RecoveryPreserveMarkerFileName))");
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
        AssertOccursBefore(flashbackVideoEnqueue, "Volatile.Read(ref _forceRotateDraining)", "queue.Writer.TryWrite(packet)");
        AssertOccursBefore(flashbackGpuEnqueue, "Volatile.Read(ref _forceRotateDraining)", "queue.Writer.TryWrite(packet)");
        AssertOccursBefore(flashbackAudioEnqueue, "Volatile.Read(ref _forceRotateDraining)", "queue.Writer.TryWrite(packet)");
        AssertContains(flashbackGpuEnqueue, "lock (_videoQueueSync)");
        AssertContains(flashbackAudioEnqueue, "lock (_videoQueueSync)");
        AssertContains(captureServiceSource, "libAvSink.OnEncodingFailed = OnRecordingBackendFatalError");
        AssertContains(captureServiceSource, "flashbackSink.SetFatalErrorCallback(OnFlashbackBackendFatalError)");
        AssertContains(captureServiceSource, "newSink.SetFatalErrorCallback(OnFlashbackBackendFatalError)");
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
        AssertContains(captureServiceSource, "_preserveFlashbackSegmentsAfterFailedRecordingFinalize");
        AssertContains(captureServiceSource, "PreserveFlashbackRecoverySegments");
        AssertContains(captureServiceSource, "MarkSessionPreservedForRecovery");
        AssertContains(captureServiceSource, "FLASHBACK_RECOVERY_PRESERVE");
        AssertContains(captureServiceSource, "FLASHBACK_PREVIEW_ROLLBACK_PURGE_WARN");
        AssertContains(captureServiceSource, "flashbackSink.FrameEncoded -= OnFlashbackFrameEncoded;");
        AssertContains(captureServiceSource, "FLASHBACK_PREVIEW_ROLLBACK_PLAYBACK_WARN");
        AssertContains(captureServiceSource, "ResolveFlashbackSegmentPurge");
        AssertContains(captureServiceSource, "FLASHBACK_SEGMENT_PURGE_BLOCKED");
        AssertContains(captureServiceSource, "WaitForForceRotateIdle(TimeSpan.FromSeconds(10))");
        AssertContains(captureServiceSource, "Flashback backend export rotation did not quiesce before recording start.");
        AssertContains(captureServiceSource, "bool requireCompleteLiveEdge = false");
        AssertContains(captureServiceSource, "requireCompleteLiveEdge: true");
        AssertContains(captureServiceSource, "FLASHBACK_RECORDING_EXPORT_INCOMPLETE_FAIL");
        AssertContains(captureServiceSource, "live-edge segment was not closed before timeout");
        AssertContains(captureServiceSource, "FLASHBACK_SETTINGS_APPLY_AFTER_RECORDING_DEFERRED");
        AssertOccursBefore(captureServiceSource, "if (!fbResult.Succeeded)", "else if (_pendingFlashbackSettingsChange)");
        AssertContains(captureServiceSource, "preserveFlashbackSegmentsAfterFailedRecordingFinalize");
        AssertContains(captureServiceSource, "FLASHBACK_CLEANUP_PRESERVE_SEGMENTS");
        AssertContains(captureServiceSource, "purgeSegments: !preserveFlashbackSegmentsAfterFailedRecordingFinalize");
        AssertContains(captureServiceSource, "FLASHBACK_BUFFER_CYCLE_OK mode=preserve_rebuild");
        AssertContains(captureServiceSource, "FLASHBACK_BUFFER_DEFERRED_PURGE_WARN");
        AssertContains(captureServiceSource, "FLASHBACK_BUFFER_DEFERRED_PURGE_SKIP");
        AssertContains(captureServiceSource, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)\n            {\n                throw;\n            }");
        var stopRecordingBackend = ExtractSourceBlock(
            captureServiceSource,
            "private async Task<FinalizeResult> StopAndDisposeRecordingBackendAsync",
            "private async Task DisposeTransientRecordingBackendAsync");
        AssertContains(stopRecordingBackend, "OperationCanceledException? flashbackCancellationException = null;");
        AssertContains(stopRecordingBackend, "fbResult = FinalizeResult.Failure(fbOutputPath, \"Flashback recording finalize cancelled.\");");
        AssertOccursBefore(
            stopRecordingBackend,
            "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)",
            "FLASHBACK_UNIFIED_RECORDING_FINALIZE_FAIL");
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
        var disposeFlashbackPreviewBackendCore = ExtractSourceBlock(
            captureServiceSource,
            "private async Task DisposeFlashbackPreviewBackendCoreAsync",
            "private async Task CycleFlashbackBufferAsync");
        AssertOccursBefore(disposeFlashbackPreviewBackendCore, "cancellationToken.ThrowIfCancellationRequested();", "flashbackBufferManager.PurgeAllSegments();");
        var cycleFlashbackBuffer = ExtractSourceBlock(
            captureServiceSource,
            "private async Task CycleFlashbackBufferAsync",
            "private void OnFlashbackFrameEncoded");
        AssertOccursBefore(
            cycleFlashbackBuffer,
            "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)",
            "FLASHBACK_CYCLE_STOP_WARN");
        AssertContains(cycleFlashbackBuffer, "cancellationToken: cancellationToken");
        var cycleNewSinkStart = ExtractSourceBlock(
            cycleFlashbackBuffer,
            "var newSink = new FlashbackEncoderSink(bufferManager);",
            "finally");
        AssertOccursBefore(
            cycleNewSinkStart,
            "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)",
            "FLASHBACK_CYCLE_NEW_SINK_FAIL");
        AssertContains(cycleNewSinkStart, "newSink.FrameEncoded -= OnFlashbackFrameEncoded;");
        AssertContains(cycleNewSinkStart, "unifiedVideoCapture.SetFlashbackSink(null);");
        AssertContains(cycleNewSinkStart, "_wasapiAudioCapture?.DetachFlashbackSink();");
        AssertContains(cycleNewSinkStart, "_microphoneCapture?.SetAudioWriter(null);");
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
        AssertContains(captureServiceSource, "RecordLastRecordingFailure");
        AssertContains(captureServiceSource, "RecordLastFlashbackFailure");
        AssertContains(captureServiceSource, "ClearLastRecordingFailure");
        AssertContains(captureServiceSource, "ClearLastFlashbackFailure");
        AssertContains(captureSnapshotsSource, "GetLastFailureTelemetry");
        AssertContains(captureSnapshotsSource, "IsFlashbackRecordingBackendOwnedByRecording()");
        AssertDoesNotContain(captureSnapshotsSource, "var flashbackIsRecordingBackend = _isRecording && IsFlashbackRecordingBackendActive()");
        AssertContains(captureSnapshotsSource, "RecordingEncodingFailureMessage");
        AssertContains(captureSnapshotsSource, "RecordingVideoFramesSubmittedToEncoder = activeRecordingVideoFramesSubmitted");
        AssertContains(captureSnapshotsSource, "RecordingVideoQueueLatencyP95Ms = activeRecordingVideoQueueLatencyP95Ms");
        AssertContains(captureSnapshotsSource, "RecordingVideoQueueOldestFrameAgeMs = activeRecordingVideoQueueOldestFrameAgeMs");
        AssertContains(captureSnapshotsSource, "RecordingVideoBackpressureWaitMs = activeRecordingVideoBackpressureWaitMs");
        AssertContains(captureSnapshotsSource, "FlashbackVideoEncoderPacketsWritten = fbSink?.VideoEncoderPacketsWritten");
        AssertContains(captureSnapshotsSource, "FlashbackVideoSequenceGaps = fbSink?.VideoSequenceGaps");
        AssertContains(captureSnapshotsSource, "FlashbackVideoQueueOldestFrameAgeMs = fbSink?.VideoQueueOldestFrameAgeMs");
        AssertContains(captureSnapshotsSource, "FlashbackVideoBackpressureWaitMs = fbSink?.VideoBackpressureWaitMs");
        AssertContains(captureSnapshotsSource, "FlashbackEncodingFailureMessage");
        AssertContains(captureSnapshotsSource, "FlashbackStartupCacheBytes = bufMgr?.StartupCacheBytes");
        AssertContains(captureSnapshotsSource, "FlashbackTempDriveFreeBytes = bufMgr?.TempDriveAvailableFreeBytes");
        var sharedFormatterSource = ReadRepoFile("tools/Common/AutomationSnapshotFormatter.cs");
        var ecctlFormatterSource = ReadRepoFile("tools/ecctl/Formatters.cs");
        var mcpAppStateSource = ReadRepoFile("tools/McpServer/Tools/AppStateTools.cs");
        AssertContains(sharedFormatterSource, "FlashbackEncodingFailed");
        AssertContains(sharedFormatterSource, "FlashbackStartupCacheBytes");
        AssertContains(ecctlFormatterSource, "FlashbackEncodingFailed");
        AssertContains(ecctlFormatterSource, "FlashbackStartupCacheBytes");
        AssertContains(mcpAppStateSource, "FormatSnapshot(response, includeFlashback: true)");
        AssertOccursBefore(
            sharedFormatterSource,
            "var flashbackFailed = Get(snapshot, \"FlashbackEncodingFailed\", \"false\");",
            "builder.AppendLine(\"== Flashback ==\");");
        AssertOccursBefore(
            ecctlFormatterSource,
            "var flashbackFailed = AutomationSnapshotFormatter.Get(snapshot, \"FlashbackEncodingFailed\", \"false\");",
            "builder.AppendLine(\"== Flashback ==\");");

        return Task.CompletedTask;
    }

    private static string ExtractSourceBlock(string source, string startToken, string endToken)
    {
        var start = source.IndexOf(startToken, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException($"Assertion failed: expected source to contain '{startToken}'.");
        }

        var end = source.IndexOf(endToken, start + startToken.Length, StringComparison.Ordinal);
        if (end < 0)
        {
            throw new InvalidOperationException($"Assertion failed: expected source after '{startToken}' to contain '{endToken}'.");
        }

        return source[start..end];
    }

}
