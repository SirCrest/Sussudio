using System;
using System.Threading.Tasks;

// Tests for recording sink queue limits, drops, and latency accounting.
static partial class Program
{
    private static string ReadLibAvRecordingSinkSource()
    {
        var parts = new[]
        {
            ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.Startup.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.StopLifecycle.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.Queueing.cs").Replace("\r\n", "\n")
        };

        return string.Join("\n", parts);
    }

    private static string ReadUnifiedVideoCaptureSource()
    {
        var parts = new[]
        {
            ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.FrameIngress.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.Lifecycle.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.SinkFanout.cs").Replace("\r\n", "\n")
        };

        return string.Join("\n", parts);
    }

    private static string ExtractSourceBlock(string source, string startToken, string endToken)
    {
        var normalizedSource = NormalizeLineEndings(source);
        var normalizedStartToken = NormalizeLineEndings(startToken);
        var normalizedEndToken = NormalizeLineEndings(endToken);
        var start = normalizedSource.IndexOf(normalizedStartToken, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException($"Assertion failed: expected source to contain '{startToken}'.");
        }

        var end = normalizedSource.IndexOf(normalizedEndToken, start + normalizedStartToken.Length, StringComparison.Ordinal);
        if (end < 0)
        {
            throw new InvalidOperationException($"Assertion failed: expected source after '{startToken}' to contain '{endToken}'.");
        }

        return normalizedSource[start..end];
    }

    internal static Task RecordingVideoQueues_FailExplicitlyInsteadOfEvictingFrames()
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
        AssertOccursBefore(captureServiceSource, "controller.PrepareForPreviewDetach();", "_videoPipeline.SetPreviewFrameSink(sink);");
        AssertContains(captureServiceSource, "controller.UpdatePreviewComponents(sink, unifiedVideoCapture);");
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
            "var flashbackBackendSettingsChanged = _flashbackBackend.SettingsSnapshot == null",
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
        AssertContains(captureServiceSource, "SafeClearCapturePlayback(ProgramCapture, \"stop_playback\")");
        AssertContains(captureServiceSource, "SafeClearCapturePlayback(capture, \"detach_capture\")");
        AssertContains(captureServiceSource, "private static void DisposePlaybackBestEffort(WasapiAudioPlayback playback)");
        AssertContains(captureServiceSource, "StopPlaybackBestEffort(newPlayback, \"start_fail\")");
        AssertContains(captureServiceSource, "WASAPI_PLAYBACK_START_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
        AssertContains(captureServiceSource, "WASAPI_PLAYBACK_ATTACH_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
        AssertContains(captureServiceSource, "StopPlayback(flashbackPlaybackController);\n            throw;");
        AssertContains(captureServiceSource, "if (ReferenceEquals(Playback, newPlayback))");
        AssertContains(captureServiceSource, "private static void StopPlaybackBestEffort(WasapiAudioPlayback playback, string operation)");
        AssertContains(captureServiceSource, "WASAPI audio playback dispose warning");
        AssertDoesNotContain(captureServiceSource, "_previewAudioGraph.ProgramCapture?.SetPlayback(null);");
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
        return Task.CompletedTask;
    }

    internal static Task RecordingBackendFlashbackBufferCycle_PreservesPolicies()
    {
        var sources = ReadRecordingQueueOverloadPolicySources();
        var bufferCycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackControls.cs")
            .Replace("\r\n", "\n");
        var finalizeBackendText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs")
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

    private readonly record struct RecordingQueueOverloadPolicySources(
        string LibAvSource,
        string FlashbackSource,
        string FlashbackBackendSource,
        string FlashbackBufferSource,
        string FlashbackCleanupSource,
        string CaptureServiceSource,
        string CaptureHealthSnapshotRootSource,
        string CaptureSnapshotsSource,
        string UnifiedVideoCaptureSource,
        string RecordingContractsSource);

    private static RecordingQueueOverloadPolicySources ReadRecordingQueueOverloadPolicySources()
    {
        var libAvSource = ReadLibAvRecordingSinkSource();
        var flashbackSource = ReadFlashbackEncoderSinkSource();
        var flashbackBackendSource = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.cs");
        var flashbackBufferSource = ReadFlashbackBufferManagerSource();
        var flashbackCleanupSource = ReadRepoFile("Sussudio/Services/Flashback/FlashbackStartupCacheCleanup.cs");
        var captureServiceSource = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Cleanup.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackControls.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs")
            + "\n" + ReadCaptureServiceAudioSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Failures.cs")
            + "\n" + ReadCaptureServicePreviewLifecycleSource()
            + "\n" + ReadCaptureServiceFlashbackOrchestrationSource()
            + "\n" + ReadCaptureServiceRecordingFinalizationSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportDiagnostics.cs");
        var captureHealthSnapshotRootSource = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs");
        var captureSnapshotsSource = captureHealthSnapshotRootSource
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs");
        var unifiedVideoCaptureSource = ReadUnifiedVideoCaptureSource();
        var recordingContractsSource = ReadRepoFile("Sussudio/Services/Contracts/RecordingContracts.cs");

        return new RecordingQueueOverloadPolicySources(
            libAvSource,
            flashbackSource,
            flashbackBackendSource,
            flashbackBufferSource,
            flashbackCleanupSource,
            captureServiceSource,
            captureHealthSnapshotRootSource,
            captureSnapshotsSource,
            unifiedVideoCaptureSource,
            recordingContractsSource);
    }

    private static void AssertLibAvRecordingQueueOverloadPolicy(string libAvSource, string recordingContractsSource)
    {
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
    }

    private static void AssertFlashbackRecordingQueueOverloadPolicy(string flashbackSource)
    {
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
        AssertContains(flashbackSource, "if (!RotateSegment(currentPts))\n                {\n                    localRequest.CompleteEmpty();\n                    return true;\n                }");
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
        AssertOccursBefore(flashbackSource, "if (_ownsBufferManager)\n        {\n            _bufferManager.PurgeAllSegments();", "_encoder.Dispose();");
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
    }

    private static void AssertFlashbackBufferRecoveryPolicy(
        string flashbackSource,
        string flashbackBufferSource,
        string flashbackCleanupSource)
    {
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
            "private static bool TryWriteAudioPacket");
        AssertOccursBefore(flashbackVideoEnqueue, "GetVideoEnqueueRejectReason(isGpu: false)", "TryWriteVideoPacket(queue, packet)");
        AssertOccursBefore(flashbackGpuEnqueue, "GetVideoEnqueueRejectReason(isGpu: true)", "TryWriteGpuPacket(queue, packet)");
        AssertOccursBefore(flashbackAudioEnqueue, "Volatile.Read(ref _forceRotateDraining)", "TryWriteAudioPacket(queue, packet, ref queueDepth, \"audio\")");
        AssertContains(flashbackVideoEnqueue, "var rejectReason = GetVideoEnqueueRejectReason(isGpu: false);");
        AssertContains(flashbackVideoEnqueue, "TrackVideoQueueRejected(rejectReason);");
        AssertContains(flashbackGpuEnqueue, "var rejectReason = GetVideoEnqueueRejectReason(isGpu: true);");
        AssertContains(flashbackGpuEnqueue, "TrackGpuQueueRejected(rejectReason);");
        AssertContains(flashbackAudioEnqueue, "if (_disposed ||");
        AssertContains(flashbackAudioEnqueue, "!_started ||");
        AssertContains(flashbackGpuEnqueue, "lock (_videoQueueSync)");
        AssertContains(flashbackAudioEnqueue, "lock (_videoQueueSync)");
    }

    private static void AssertRecordingQueueHealthSnapshotTelemetry(
        string captureServiceSource,
        string captureHealthSnapshotRootSource,
        string captureSnapshotsSource,
        string unifiedVideoCaptureSource)
    {
        AssertContains(unifiedVideoCaptureSource, "encoder is IRawVideoFrameTryEncoder");
        AssertContains(unifiedVideoCaptureSource, "leaseEncoder is IRawVideoFrameLeaseTryEncoder");
        AssertContains(unifiedVideoCaptureSource, "encoder is IGpuVideoFrameTryEncoder");
        AssertContains(unifiedVideoCaptureSource, "BeginFlashbackRecordingAccounting");
        AssertContains(unifiedVideoCaptureSource, "RecordFlashbackRecordingAccounting");
        AssertContains(unifiedVideoCaptureSource, "sink.IsRecordingActive");
        AssertContains(unifiedVideoCaptureSource, "if (accepted)");
        AssertContains(unifiedVideoCaptureSource, "public MjpegPipelineTimingSnapshot GetMjpegPipelineTimingSnapshot()");
        AssertContains(unifiedVideoCaptureSource, "private static MjpegPipelineTimingMetrics CreateMjpegPipelineTimingSummary");
        AssertContains(captureServiceSource, "var timingSnapshot = capture?.GetMjpegPipelineTimingSnapshot();");
        AssertContains(captureServiceSource, "RecordLastRecordingFailure");
        AssertContains(captureServiceSource, "RecordLastFlashbackFailure");
        AssertContains(captureServiceSource, "ClearLastRecordingFailure");
        AssertContains(captureServiceSource, "ClearLastFlashbackFailure");
        AssertContains(captureSnapshotsSource, "GetLastFailureTelemetry");
        AssertContains(captureSnapshotsSource, "IsFlashbackRecordingBackendOwnedByRecording()");
        AssertContains(captureHealthSnapshotRootSource, "var mjpegHealth = CaptureMjpegHealthSnapshotFields(unifiedVideoCapture);");
        AssertContains(captureSnapshotsSource, "var timingSnapshot = _videoPipeline.GetMjpegTimingSnapshot(unifiedVideoCapture);");
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
        AssertContains(captureSnapshotsSource, "RecordingCudaQueueDepth = recordingHealth.CudaQueueDepth");
        AssertContains(captureSnapshotsSource, "RecordingCudaFramesDropped = recordingHealth.CudaFramesDropped");
        AssertContains(captureSnapshotsSource, "sink?.CudaQueueCount ?? 0");
        AssertContains(captureSnapshotsSource, "sink?.CudaFramesDropped ?? 0");
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

        var sharedFormatterSource = global::Sussudio.Tests.RuntimeContractSource.ReadAutomationSnapshotFormatterSource();
        var ssctlFormatterSource = global::Sussudio.Tests.RuntimeContractSource.ReadSsctlSnapshotFormatterSource();
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
            "StopAndDisposeOldSinkForBufferCycleAsync(",
            "ClearSinkAndSettings();");
        AssertContains(backendCycleFlashbackBuffer, "await oldSink.DisposeAsync().ConfigureAwait(false);");
        AssertContains(backendCycleFlashbackBuffer, "var oldPlaybackController = TakePlaybackController();");
        AssertContains(backendCycleFlashbackBuffer, "oldPlaybackController.GoLive();");
        AssertContains(backendCycleFlashbackBuffer, "oldPlaybackController.Dispose();");
        AssertOccursBefore(
            backendCycleFlashbackBuffer,
            "DisposePlaybackForBufferCycle(",
            "bufferManager.PurgeCompletedSegments();");
        AssertOccursBefore(
            backendCycleFlashbackBuffer,
            "DisposePlaybackForBufferCycle(",
            "DetachOldSinkProducersForBufferCycle(");
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

    internal static Task RecordingBackendFinalizeAndCleanup_PreservesFlashbackBoundaries()
    {
        var sources = ReadRecordingQueueOverloadPolicySources();
        var flashbackBackendSource = sources.FlashbackBackendSource;
        var captureServiceSource = sources.CaptureServiceSource;
        var captureHealthSnapshotRootSource = sources.CaptureHealthSnapshotRootSource;
        var captureSnapshotsSource = sources.CaptureSnapshotsSource;
        var unifiedVideoCaptureSource = sources.UnifiedVideoCaptureSource;
        var microphoneMonitorText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.AudioPreviewLifecycle.cs")
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
            "private readonly record struct LibAvFinalizeStepResult");

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
            libAvStopRecordingBackend);
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
        string libAvStopRecordingBackend)
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
        AssertContains(microphoneMonitorText, "if (options.OnlyWhenMissing && _previewAudioGraph.MicrophoneCapture != null)");
        AssertContains(microphoneMonitorText, "FLASHBACK_MIC_ATTACH_OK reason='{options.FlashbackAttachReason}'");
        AssertContains(microphoneMonitorText, "Logger.Log($\"{options.DisposeWarningEvent} type={disposeEx.GetType().Name} msg={disposeEx.Message}\");");
        AssertOccursBefore(
            microphoneMonitorText,
            "micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));",
            "_previewAudioGraph.MicrophoneCapture = micCapture;");

        AssertContains(libAvStopRecordingBackend, "private async Task<OperationCanceledException?> RestorePendingFlashbackEnableAfterLibAvRecordingAsync(");
        AssertContains(libAvStopRecordingBackend, "if (!_pendingFlashbackEnableAfterRecording)");
        AssertContains(libAvStopRecordingBackend, "_pendingFlashbackEnableAfterRecording = false;");
        AssertContains(libAvStopRecordingBackend, "await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, cancellationToken)");
        AssertContains(libAvStopRecordingBackend, "FLASHBACK_ENABLE_AFTER_RECORDING_CANCELLED");
        AssertContains(libAvStopRecordingBackend, "FLASHBACK_ENABLE_AFTER_RECORDING_FAIL type={ex.GetType().Name} error='{ex.Message}'");
        var standardMicMonitorRestart = ExtractSourceBlock(
            libAvStopRecordingBackend,
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
