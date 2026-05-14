using System;
using System.Threading.Tasks;

// Tests for recording sink queue limits, drops, and latency accounting.
static partial class Program
{
    private static Task RecordingVideoQueues_FailExplicitlyInsteadOfEvictingFrames()
    {
        var libAvSource = ReadLibAvRecordingSinkSource();
        var flashbackSource = ReadFlashbackEncoderSinkSource();
        var flashbackBackendSource = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.cs");
        var flashbackBufferSource = ReadFlashbackBufferManagerSource();
        var flashbackCleanupSource = ReadRepoFile("Sussudio/Services/Flashback/FlashbackStartupCacheCleanup.cs");
        var captureServiceSource = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Cleanup.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackControls.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Audio.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.DeferredCleanup.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Failures.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewPipeline.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackOrchestration.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeRecord.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingRollback.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportOperations.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportDiagnostics.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportPlanning.cs");
        var captureSnapshotsSource = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs");
        var unifiedVideoCaptureSource = ReadUnifiedVideoCaptureSource();
        var recordingContractsSource = ReadRepoFile("Sussudio/Services/Recording/RecordingContracts.cs")
            + "\n"
            + ReadRepoFile("Sussudio/Services/Contracts/RecordingContracts.cs");

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

        AssertContains(libAvSource, "LibAv recording video queue overloaded");
        AssertDoesNotContain(libAvSource, "QueueBackpressureTimeoutMs");
        AssertDoesNotContain(libAvSource, "Thread.Sleep(");
        AssertDoesNotContain(libAvSource, "backpressure_retry");
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
        AssertContains(libAvSource, "public (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) VideoQueueLatencyMetrics");
        AssertContains(libAvSource, "public double VideoQueueLatencyP95Ms");
        AssertContains(libAvSource, "public double VideoQueueLatencyP99Ms");
        AssertContains(libAvSource, "public long VideoBackpressureWaitMs");
        AssertContains(libAvSource, "public long VideoBackpressureEvents");
        AssertDoesNotContain(libAvSource, "_videoLatencyTracker.RecordBackpressure(backpressureStartTick");
        AssertContains(libAvSource, "_videoLatencyTracker.TrackEnqueueUnderLock(packet.EnqueueTick)");
        AssertContains(libAvSource, "_videoLatencyTracker.TrackDequeueUnderLock(packet.EnqueueTick)");
        AssertContains(libAvSource, "_videoLatencyTracker.RecordPacketDequeued(packet.EnqueueTick, packet.SequenceNumber)");
        AssertContains(libAvSource, "private bool TryWriteVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertContains(libAvSource, "var depth = Interlocked.Increment(ref _videoQueueDepth);\n        if (queue.Writer.TryWrite(packet))");
        AssertContains(libAvSource, "AtomicMax.Update(ref _videoQueueMaxDepth, depth);");
        AssertContains(libAvSource, "DecrementQueueDepth(ref _videoQueueDepth, \"video_write_failed\");");
        AssertContains(libAvSource, "public int GpuQueueMaxDepth");
        AssertContains(libAvSource, "public int CudaQueueMaxDepth");
        AssertContains(libAvSource, "private bool TryWriteGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)");
        AssertContains(libAvSource, "var depth = Interlocked.Increment(ref _gpuQueueDepth);\n        if (queue.Writer.TryWrite(packet))");
        AssertContains(libAvSource, "AtomicMax.Update(ref _gpuQueueMaxDepth, depth);");
        AssertContains(libAvSource, "DecrementQueueDepth(ref _gpuQueueDepth, \"gpu_write_failed\");");
        AssertContains(libAvSource, "private bool TryWriteCudaPacket(Channel<CudaFramePacket> queue, CudaFramePacket packet)");
        AssertContains(libAvSource, "var depth = Interlocked.Increment(ref _cudaQueueDepth);\n        if (queue.Writer.TryWrite(packet))");
        AssertContains(libAvSource, "AtomicMax.Update(ref _cudaQueueMaxDepth, depth);");
        AssertContains(libAvSource, "DecrementQueueDepth(ref _cudaQueueDepth, \"cuda_write_failed\");");
        AssertContains(libAvSource, "private static bool TryWriteAudioPacket(");
        AssertContains(libAvSource, "DecrementQueueDepth(ref queueDepth, $\"{queueName}_write_failed\");");
        AssertContains(libAvSource, "private static void DecrementQueueDepth(ref int target, string queueName)");
        AssertContains(libAvSource, "LIBAV_SINK_QUEUE_DEPTH_UNDERFLOW");
        AssertContains(libAvSource, "private void SignalWork(string operation)");
        AssertContains(libAvSource, "LIBAV_SINK_WORK_SIGNAL_SKIPPED");
        AssertContains(libAvSource, "SignalWork(\"complete_writer\");");
        AssertEqual(1, libAvSource.Split("_workAvailable.Release();", StringSplitOptions.None).Length - 1, "All LibAv work-signal wakeups go through SignalWork");
        AssertContains(libAvSource, "ReturnRemainingGpuBuffers(_gpuQueue, ref _gpuQueueDepth);");
        AssertContains(libAvSource, "ReturnRemainingCudaFrames(_cudaQueue, ref _cudaQueueDepth);");
        AssertDoesNotContain(libAvSource, "AtomicMax.Update(ref _videoQueueMaxDepth, Interlocked.Increment(ref _videoQueueDepth))");
        AssertDoesNotContain(libAvSource, "AtomicMax.Update(ref _gpuQueueMaxDepth, Interlocked.Increment(ref _gpuQueueDepth))");
        AssertDoesNotContain(libAvSource, "AtomicMax.Update(ref _cudaQueueMaxDepth, Interlocked.Increment(ref _cudaQueueDepth))");
        AssertDoesNotContain(libAvSource, "Interlocked.Decrement(ref _videoQueueDepth)");
        AssertDoesNotContain(libAvSource, "Interlocked.Decrement(ref _gpuQueueDepth)");
        AssertDoesNotContain(libAvSource, "Interlocked.Decrement(ref _cudaQueueDepth)");
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
        AssertDoesNotContain(flashbackSource, "QueueBackpressureTimeoutMs");
        AssertDoesNotContain(flashbackSource, "WaitForBackpressureRetryCancellation");
        AssertDoesNotContain(flashbackSource, "FLASHBACK_SINK_VIDEO_BACKPRESSURE_DROP");
        AssertDoesNotContain(flashbackSource, "FLASHBACK_SINK_GPU_BACKPRESSURE_DROP");
        AssertContains(flashbackSource, "var p010FrameSize = MfSourceReaderVideoCapture.GetFrameSizeBytes(_width, _height, isP010: true)");
        AssertContains(flashbackSource, "VideoFramePacket.Frame(buffer, expectedSize, enqueueTick, isP010)");
        AssertContains(flashbackSource, "MfSourceReaderVideoCapture.GetFrameSizeBytes(w, h, packet.IsP010)");
        AssertContains(flashbackSource, "lease.PixelFormat == PooledVideoPixelFormat.P010");
        AssertContains(flashbackSource, "FLASHBACK_SINK_VIDEO_OVERLOAD");
        AssertContains(flashbackSource, "FLASHBACK_SINK_GPU_OVERLOAD");
        AssertContains(flashbackSource, "_onFatalError?.Invoke");
        AssertDoesNotContain(flashbackSource, "catch { /* Callback must not mask the original error */ }");
        AssertContains(flashbackSource, "Logger.Log($\"FLASHBACK_SINK_FATAL_CALLBACK_FAIL type={callbackEx.GetType().Name} msg={callbackEx.Message}\");");
        AssertContains(flashbackSource, "private void OnVideoFrameEncoded()\n    {\n        if (_disposed)\n        {\n            return;\n        }");
        AssertContains(flashbackSource, "if (!_disposed && Volatile.Read(ref _recordingActive) == 1)");
        AssertContains(flashbackSource, "public bool EncodingFailed");
        AssertContains(flashbackSource, "public string? EncodingFailureMessage");
        AssertContains(flashbackSource, "public bool CanBeginRecording");
        AssertContains(flashbackSource, "public bool IsRecordingActive");
        AssertContains(flashbackSource, "Volatile.Read(ref _recordingActive) == 0");
        AssertContains(flashbackSource, "!_bufferManager.IsSessionPreservedForRecovery");
        AssertContains(flashbackSource, "Cannot begin recording: flashback recording is already active.");
        AssertContains(flashbackSource, "Cannot begin recording: flashback session is preserved for recovery.");
        AssertOccursBefore(flashbackSource, "Cannot begin recording: flashback recording is already active.", "_bufferManager.PauseEviction();");
        AssertOccursBefore(flashbackSource, "Cannot begin recording: flashback session is preserved for recovery.", "_bufferManager.PauseEviction();");
        AssertOccursBefore(flashbackSource, "_bufferManager.PauseEviction();", "Volatile.Write(ref _recordingActive, 1);");
        AssertContains(flashbackSource, "public bool IsForceRotateActive");
        AssertContains(flashbackSource, "public bool IsForceRotateRequested");
        AssertContains(flashbackSource, "public bool IsForceRotateDraining");
        AssertContains(flashbackSource, "WaitForForceRotateIdle");
        AssertContains(flashbackSource, "CompletePendingForceRotateWithEmptyResult");
        AssertContains(flashbackSource, "ForceRotateRequest? supersededRequest;");
        AssertContains(flashbackSource, "supersededRequest = _forceRotateRequest;");
        AssertContains(flashbackSource, "FLASHBACK_SINK_FORCE_ROTATE_SUPERSEDED");
        AssertContains(flashbackSource, "supersededRequest.TryCancel();");
        AssertContains(flashbackSource, "if (!RotateSegment(currentPts))\n                            {\n                                localRequest.CompleteEmpty();\n                                madeProgress = true;\n                                continue;\n                            }");
        AssertContains(flashbackSource, "private bool RotateSegment(TimeSpan currentPts)");
        AssertContains(flashbackSource, "return true;\n        }\n        catch (Exception ex)");
        AssertContains(flashbackSource, "Logger.Log($\"FLASHBACK_SINK_ROTATE_FAIL type={ex.GetType().Name} msg={ex.Message}\");\n            return false;");
        AssertContains(flashbackSource, "TryCancelForceRotate(request)");
        AssertContains(flashbackSource, "ReferenceEquals(_forceRotateRequest, request)");
        AssertContains(flashbackSource, "cancelled={cancelled}");
        AssertContains(flashbackSource, "_forceRotateRequested = false;");
        AssertContains(flashbackSource, "Volatile.Write(ref _forceRotateDraining, false);");
        AssertContains(flashbackSource, "CancelEncodingCts(\"stop_timeout\");\n                CompletePendingForceRotateWithEmptyResult();\n                Logger.Log(\"FLASHBACK_SINK_STOP_DRAIN_TIMEOUT\");");
        AssertContains(flashbackSource, "Cannot begin recording: flashback export rotation is still draining.");
        AssertContains(flashbackSource, "if (_ownsBufferManager)");
        AssertOccursBefore(flashbackSource, "if (_ownsBufferManager)\n            {\n                _bufferManager.PurgeAllSegments();", "_encoder.Dispose();");
        AssertContains(flashbackSource, "CancelRecordingStartRollback");
        AssertContains(flashbackSource, "var wasRecording = Interlocked.Exchange(ref _recordingActive, 0) != 0");
        AssertContains(flashbackSource, "if (!wasRecording)\n        {\n            const string message = \"Flashback recording was not active.\";");
        AssertContains(flashbackSource, "FLASHBACK_RECORDING_END_REJECTED");
        AssertContains(flashbackSource, "finally");
        AssertContains(flashbackSource, "ResumeEvictionBestEffort(_bufferManager, \"recording_end\")");
        AssertContains(flashbackSource, "ResumeEvictionBestEffort(_bufferManager, \"recording_start_rollback\")");
        AssertContains(flashbackSource, "if (Interlocked.Exchange(ref _recordingActive, 0) != 0)\n        {\n            ResumeEvictionBestEffort(_bufferManager, \"dispose\");\n        }");
        AssertContains(flashbackSource, "_gpuEncodingEnabled = false;\n        _audioEnabled = false;\n        _microphoneEnabled = false;\n        _sessionContext = null;\n        _width = 0;\n        _height = 0;\n        _tsFilePath = null;\n        _recordingOutputPath = string.Empty;\n        _segmentStartPts = TimeSpan.Zero;\n        _segmentDuration = TimeSpan.Zero;\n        _ptsBaseOffset = TimeSpan.Zero;\n        Interlocked.Exchange(ref _segmentStartBytes, 0);");
        AssertContains(flashbackSource, "FLASHBACK_SINK_EVICTION_RESUME_WARN");
        AssertContains(flashbackSource, "if (LastRecordingEndPts < LastRecordingStartPts)\n                {\n                    LastRecordingEndPts = _bufferManager.LatestPts;\n                    if (LastRecordingEndPts < LastRecordingStartPts)\n                    {\n                        LastRecordingEndPts = LastRecordingStartPts;\n                    }\n                }");
        AssertContains(flashbackSource, "Cannot begin recording: flashback encoder is not running.");
        AssertContains(flashbackSource, "public int VideoQueueMaxDepth");
        AssertContains(flashbackSource, "public long VideoFramesSubmittedToEncoder");
        AssertContains(flashbackSource, "public long VideoEncoderPacketsWritten");
        AssertContains(flashbackSource, "public long VideoSequenceGaps");
        AssertContains(flashbackSource, "public long VideoQueueOldestFrameAgeMs");
        AssertContains(flashbackSource, "public (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) VideoQueueLatencyMetrics");
        AssertContains(flashbackSource, "public double VideoQueueLatencyP95Ms");
        AssertContains(flashbackSource, "public double VideoQueueLatencyP99Ms");
        AssertContains(flashbackSource, "public long VideoBackpressureWaitMs");
        AssertContains(flashbackSource, "public long VideoBackpressureEvents");
        AssertDoesNotContain(flashbackSource, "_videoLatencyTracker.RecordBackpressure(backpressureStartTick");
        AssertContains(flashbackSource, "_videoLatencyTracker.TrackEnqueueUnderLock(packet.EnqueueTick)");
        AssertContains(flashbackSource, "_videoLatencyTracker.TrackDequeueUnderLock(packet.EnqueueTick)");
        AssertContains(flashbackSource, "_videoLatencyTracker.RecordPacketDequeued(packet.EnqueueTick, packet.SequenceNumber)");
        AssertContains(flashbackSource, "public int GpuQueueMaxDepth");
        AssertContains(flashbackSource, "IRawVideoFrameTryEncoder");
        AssertContains(flashbackSource, "IGpuVideoFrameTryEncoder");
        AssertContains(flashbackSource, "public bool TryEnqueueRawVideoFrame");
        AssertContains(flashbackSource, "public bool TryEnqueueGpuVideoFrame");
        AssertContains(flashbackSource, "VideoEnqueueResult.Rejected");
        AssertContains(flashbackSource, "TryEnqueueGpuPacket");
        AssertContains(flashbackSource, "Volatile.Read(ref _forceRotateDraining)");
        AssertContains(flashbackSource, "Volatile.Read(ref _encodingFailure) != null");
        AssertContains(flashbackSource, "var maxFrameSize = Math.Max(nv12FrameSize, p010FrameSize);");
        AssertContains(flashbackSource, "var matchesConfiguredFrameSize =\n            expectedSize == nv12FrameSize ||\n            (p010FrameSize > 0 && expectedSize == p010FrameSize);");
        AssertContains(flashbackSource, "if (maxFrameSize <= 0 || !matchesConfiguredFrameSize)");
        AssertContains(flashbackSource, "FLASHBACK_SINK_VIDEO_FRAME_INVALID_SIZE expected={expectedSize} max={maxFrameSize}");
        AssertContains(flashbackSource, "if (expectedSize <= 0)\n        {\n            Logger.Log($\"FLASHBACK_SINK_VIDEO_FRAME_INVALID_SIZE expected={expectedSize} actual={frame.Width}x{frame.Height}\");\n            frame.Dispose();\n            return false;\n        }");
        AssertContains(flashbackSource, "if (subresourceIndex < 0)\n        {\n            TrackGpuQueueRejected(\"invalid_subresource\");\n            Logger.Log($\"FLASHBACK_SINK_GPU_FRAME_INVALID_SUBRESOURCE subresource={subresourceIndex}\");\n            return false;\n        }");
        AssertOccursBefore(flashbackSource, "FLASHBACK_SINK_GPU_FRAME_INVALID_SUBRESOURCE", "Marshal.AddRef(d3d11Texture2D);");
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
        AssertContains(captureSnapshotsSource, "var mjpegTimingSnapshot = unifiedVideoCapture?.GetMjpegPipelineTimingSnapshot();");
        AssertDoesNotContain(captureSnapshotsSource, "unifiedVideoCapture?.GetMjpegPipelineTimingMetrics()");
        AssertDoesNotContain(captureSnapshotsSource, "unifiedVideoCapture?.GetFullMjpegPipelineTimingMetrics()");
        AssertContains(captureSnapshotsSource, "var flashbackVideoQueueLatencyMetrics = fbSink?.VideoQueueLatencyMetrics");
        AssertContains(captureSnapshotsSource, "var activeRecordingVideoQueueLatencyMetrics = sink?.VideoQueueLatencyMetrics");
        AssertDoesNotContain(captureSnapshotsSource, "var flashbackIsRecordingBackend = _isRecording && IsFlashbackRecordingBackendActive()");
        AssertContains(captureSnapshotsSource, "RecordingEncodingFailureMessage");
        AssertContains(captureSnapshotsSource, "RecordingVideoFramesSubmittedToEncoder = activeRecordingVideoFramesSubmitted");
        AssertContains(captureSnapshotsSource, "RecordingVideoQueueLatencyP95Ms = activeRecordingVideoQueueLatencyMetrics.P95Ms");
        AssertContains(captureSnapshotsSource, "RecordingVideoQueueLatencyP99Ms = activeRecordingVideoQueueLatencyMetrics.P99Ms");
        AssertContains(captureSnapshotsSource, "RecordingVideoQueueOldestFrameAgeMs = activeRecordingVideoQueueOldestFrameAgeMs");
        AssertContains(captureSnapshotsSource, "RecordingVideoBackpressureWaitMs = activeRecordingVideoBackpressureWaitMs");
        AssertContains(captureSnapshotsSource, "FlashbackVideoEncoderPacketsWritten = fbSink?.VideoEncoderPacketsWritten");
        AssertContains(captureSnapshotsSource, "FlashbackVideoSequenceGaps = fbSink?.VideoSequenceGaps");
        AssertContains(captureSnapshotsSource, "FlashbackVideoQueueOldestFrameAgeMs = fbSink?.VideoQueueOldestFrameAgeMs");
        AssertContains(captureSnapshotsSource, "FlashbackVideoQueueLatencyP99Ms = flashbackVideoQueueLatencyMetrics.P99Ms");
        AssertContains(captureSnapshotsSource, "FlashbackVideoBackpressureWaitMs = fbSink?.VideoBackpressureWaitMs");
        AssertContains(captureSnapshotsSource, "FatalCleanupInProgress = fatalCleanupInProgress");
        AssertContains(captureSnapshotsSource, "FlashbackCleanupInProgress = flashbackCleanupInProgress");
        AssertContains(captureSnapshotsSource, "FlashbackForceRotateActive = fbSink?.IsForceRotateActive");
        AssertContains(captureSnapshotsSource, "FlashbackForceRotateRequested = fbSink?.IsForceRotateRequested");
        AssertContains(captureSnapshotsSource, "FlashbackForceRotateDraining = fbSink?.IsForceRotateDraining");
        AssertContains(captureSnapshotsSource, "FlashbackEncodingFailureMessage");
        AssertContains(captureSnapshotsSource, "FlashbackStartupCacheBytes = bufMgr?.StartupCacheBytes");
        AssertContains(captureSnapshotsSource, "FlashbackTempDriveFreeBytes = bufMgr?.TempDriveAvailableFreeBytes");
        var sharedFormatterSource = ReadAutomationSnapshotFormatterSource();
        var ssctlFormatterSource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.cs");
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

    private static Task CaptureService_RecordingLifecycleLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(rootText, "public Task StartRecordingAsync(");
        AssertDoesNotContain(rootText, "public Task StopRecordingAsync(");
        AssertContains(lifecycleText, "public Task StartRecordingAsync(");
        AssertContains(lifecycleText, "public Task StopRecordingAsync(");
        AssertContains(lifecycleText, "internal Task StopRecordingAsync(bool emergency");
        AssertContains(lifecycleText, "FLASHBACK_UNIFIED_RECORDING_START");
        AssertContains(lifecycleText, "await recordingSink.StartAsync(recordingContext, transitionToken)");
        AssertContains(lifecycleText, "await StopAndDisposeRecordingBackendAsync(\"Stopped\", emergency, transitionToken)");

        return Task.CompletedTask;
    }

    private static Task CaptureService_RecordingRollbackLivesInFocusedPartial()
    {
        var finalizationText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeRecord.cs")
            .Replace("\r\n", "\n");
        var rollbackText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingRollback.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(finalizationText, "private async Task DisposeTransientRecordingBackendAsync(");
        AssertContains(rollbackText, "private async Task DisposeTransientRecordingBackendAsync(");
        AssertContains(rollbackText, "Transient recording sink stop failed during rollback");
        AssertContains(rollbackText, "Transient unified video dispose failed during rollback");
        AssertContains(rollbackText, "ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(rollbackText, "reason: \"recording_start_rollback\"");

        return Task.CompletedTask;
    }

}
