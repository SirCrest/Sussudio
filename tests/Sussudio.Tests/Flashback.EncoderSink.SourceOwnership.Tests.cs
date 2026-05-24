using System.Threading.Tasks;

static partial class Program
{
    internal static Task FlashbackEncoderSink_StartupLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Startup.cs")
            .Replace("\r\n", "\n");
        var startupQueuesText = startupText;
        var startupRollbackText = startupText;
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(startupText, "public Task StartAsync(FlashbackSessionContext context, CancellationToken cancellationToken = default, TimeSpan ptsBaseOffset = default)");
        AssertContains(startupText, "ValidateSessionContext(context);");
        AssertContains(startupText, "var tsPath = _bufferManager.AcquireSegmentPath(out var startupGeneratedSegment);");
        AssertContains(startupText, "InitializeStartupQueues(sessionContext);");
        AssertContains(startupText, "_encodingTask = Task.Factory.StartNew(");
        AssertContains(startupText, "RollBackStartFailure(ex, startupGeneratedSegmentPath);");

        AssertContains(startupQueuesText, "private void InitializeStartupQueues(FlashbackSessionContext sessionContext)");
        AssertContains(startupQueuesText, "Channel.CreateBounded<GpuFramePacket>");
        AssertContains(startupQueuesText, "Channel.CreateBounded<VideoFramePacket>");
        AssertContains(startupQueuesText, "Channel.CreateBounded<AudioSamplePacket>");
        AssertContains(startupQueuesText, "FLASHBACK_SINK_WARN_CPU_ENCODING");
        AssertContains(startupQueuesText, "FLASHBACK_SINK_GPU_QUEUE_INIT");

        AssertContains(startupRollbackText, "private void RollBackStartFailure(Exception ex, string? startupGeneratedSegmentPath)");
        AssertContains(startupRollbackText, "FLASHBACK_SINK_START_FAIL");
        AssertContains(startupRollbackText, "CompleteWriter(_videoQueue);");
        AssertContains(startupRollbackText, "DisposeCtsBestEffort(_cts, \"start_fail\");");
        AssertContains(startupRollbackText, "DisposeEncoderBestEffort(\"start_fail\");");
        AssertContains(startupRollbackText, "_bufferManager.AbandonGeneratedSegmentPath(startupGeneratedSegmentPath, restoreActivePath: null);");

        AssertDoesNotContain(rootText, "public Task StartAsync(FlashbackSessionContext context, CancellationToken cancellationToken = default, TimeSpan ptsBaseOffset = default)");
        AssertDoesNotContain(rootText, "FLASHBACK_SINK_START_FAIL");
        AssertContains(docsText, "FlashbackEncoderSink.Startup.cs");
        AssertContains(docsText, "startup queue allocation");
        AssertContains(docsText, "start-failure rollback");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_RootHelpersLiveInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var startupPolicyText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Startup.cs")
            .Replace("\r\n", "\n");
        var diagnosticsResetText = startupPolicyText;
        var runtimeStateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.RuntimeState.cs")
            .Replace("\r\n", "\n");
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(rootText, "public FlashbackEncoderSink(FlashbackBufferOptions? options = null)");
        AssertContains(rootText, "public FlashbackEncoderSink(FlashbackBufferManager bufferManager)");
        AssertDoesNotContain(rootText, "private static int ResolveVideoQueueCapacity");
        AssertDoesNotContain(rootText, "private void ResetEncodingCounters()");
        AssertDoesNotContain(rootText, "private static long NonNegativeByteDelta");

        AssertContains(startupPolicyText, "private static int ResolveVideoQueueCapacity(FlashbackSessionContext context, bool useHardwareFrames)");
        AssertContains(startupPolicyText, "private static bool IsHighResolutionFrame(FlashbackSessionContext context)");
        AssertContains(startupPolicyText, "private static double ResolveSessionFrameRate(double frameRate)");
        AssertContains(startupPolicyText, "private static void ValidateSessionContext(FlashbackSessionContext context)");

        AssertContains(diagnosticsResetText, "private void ResetEncodingCounters()");
        AssertContains(diagnosticsResetText, "ResetVideoDiagnostics();");
        AssertContains(diagnosticsResetText, "private void ResetVideoDiagnostics()");
        AssertContains(diagnosticsResetText, "Interlocked.Exchange(ref _segmentStartBytes, 0);");

        AssertContains(runtimeStateText, "private static long ToNonNegativeLongSaturated(double value)");
        AssertContains(runtimeStateText, "private static long NonNegativeByteDelta(long currentBytes, long startBytes)");
        AssertContains(runtimeStateText, "private static TimeSpan NonNegativeDuration(TimeSpan end, TimeSpan start)");
        AssertContains(runtimeStateText, "private static (TimeSpan StartPts, TimeSpan EndPts) ResumeEvictionBestEffort(");
        AssertContains(runtimeStateText, "FLASHBACK_SINK_EVICTION_RESUME_WARN");

        AssertContains(docsText, "FlashbackEncoderSink.Startup.cs");
        AssertContains(docsText, "session validation");
        AssertContains(docsText, "startup metric/counter reset");
        AssertContains(docsText, "FlashbackEncoderSink.RuntimeState.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_PacketDrainLivesInFocusedPartial()
    {
        var loopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingLoop.cs").Replace("\r\n", "\n");
        var packetDrainText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.PacketDrain.cs").Replace("\r\n", "\n");
        var encodingProgressText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingProgress.cs").Replace("\r\n", "\n");
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(loopText, "private void EncodingLoop(CancellationToken cancellationToken)");
        AssertContains(loopText, "DrainAudioPackets(audioQueue.Reader)");
        AssertContains(loopText, "DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit)");
        AssertContains(loopText, "var finalPts = ResolveEncoderPts();");
        AssertDoesNotContain(loopText, "private bool DrainVideoPackets(");
        AssertDoesNotContain(loopText, "private bool DrainGpuPackets(");
        AssertDoesNotContain(loopText, "private TimeSpan ResolveEncoderPts()");
        AssertDoesNotContain(loopText, "private void OnVideoFrameEncoded()");

        AssertContains(packetDrainText, "private bool DrainVideoPackets(ChannelReader<VideoFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(packetDrainText, "private bool DrainGpuPackets(ChannelReader<GpuFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(packetDrainText, "MfSourceReaderVideoCapture.GetFrameSizeBytes");
        AssertContains(packetDrainText, "OnVideoFrameEncoded();");
        AssertContains(packetDrainText, "private bool DrainAudioPackets(ChannelReader<AudioSamplePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(packetDrainText, "private bool DrainMicrophonePackets(ChannelReader<AudioSamplePacket> reader, int maxPackets = int.MaxValue)");

        AssertContains(encodingProgressText, "private void OnVideoFrameEncoded()");
        AssertContains(encodingProgressText, "private TimeSpan ResolveEncoderPts()");
        AssertContains(encodingProgressText, "private bool RotateSegment(TimeSpan currentPts)");
        AssertContains(encodingProgressText, "_bufferManager.UpdateLatestPts(pts);");
        AssertContains(encodingProgressText, "FrameEncoded?.Invoke(this, encoded);");
        AssertContains(encodingProgressText, "FLASHBACK_SINK_ROTATE");
        AssertContains(encodingProgressText, "FLASHBACK_SINK_ROTATE_FAIL");
        AssertContains(docsText, "FlashbackEncoderSink.PacketDrain.cs");
        AssertContains(docsText, "FlashbackEncoderSink.EncodingProgress.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_QueueCleanupLivesInFocusedPartial()
    {
        var queuesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Queues.cs")
            .Replace("\r\n", "\n");
        var videoQueueSubmissionText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.VideoQueueSubmission.cs")
            .Replace("\r\n", "\n");
        var queueCleanupText = queuesText;
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(videoQueueSubmissionText, "private VideoEnqueueResult TryEnqueueVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertContains(videoQueueSubmissionText, "private VideoEnqueueResult TryEnqueueGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)");
        AssertContains(videoQueueSubmissionText, "TryWriteVideoPacket(queue, packet)");
        AssertContains(videoQueueSubmissionText, "TryWriteGpuPacket(queue, packet)");
        AssertContains(videoQueueSubmissionText, "TrackVideoQueueRejected(\"queue_full\");");
        AssertContains(videoQueueSubmissionText, "TrackGpuQueueRejected(\"queue_full\");");
        AssertContains(videoQueueSubmissionText, "private string? GetVideoEnqueueRejectReason(bool isGpu)");
        AssertContains(videoQueueSubmissionText, "private string? GetVideoInputRejectReason(Channel<VideoFramePacket>? queue, int expectedSize, bool dataIsEmpty)");
        AssertContains(videoQueueSubmissionText, "private string? GetGpuInputRejectReason(Channel<GpuFramePacket>? queue, IntPtr texture)");
        AssertContains(videoQueueSubmissionText, "return \"force_rotate_draining\";");
        AssertContains(videoQueueSubmissionText, "? $\"encoding_failed:{failure.GetType().Name}\"");
        AssertContains(videoQueueSubmissionText, "return dataIsEmpty ? \"data_empty\" : null;");
        AssertContains(videoQueueSubmissionText, "return texture == IntPtr.Zero ? \"null_texture\" : null;");
        AssertContains(videoQueueSubmissionText, "private bool TryWriteVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertContains(videoQueueSubmissionText, "private bool TryWriteGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)");
        AssertContains(videoQueueSubmissionText, "AtomicMax.Update(ref _videoQueueMaxDepth, depth);");
        AssertContains(videoQueueSubmissionText, "AtomicMax.Update(ref _gpuQueueMaxDepth, depth);");
        AssertContains(videoQueueSubmissionText, "DecrementQueueDepth(ref _videoQueueDepth, \"video_write_failed\");");
        AssertContains(videoQueueSubmissionText, "DecrementQueueDepth(ref _gpuQueueDepth, \"gpu_write_failed\");");
        AssertContains(videoQueueSubmissionText, "private void TrackVideoQueueRejected(string reason)");
        AssertContains(videoQueueSubmissionText, "private void TrackGpuQueueRejected(string reason)");
        AssertContains(videoQueueSubmissionText, "FLASHBACK_SINK_VIDEO_QUEUE_REJECT");
        AssertContains(videoQueueSubmissionText, "FLASHBACK_SINK_GPU_QUEUE_REJECT");
        AssertContains(videoQueueSubmissionText, "total == 1 || total % 30 == 0");
        AssertContains(videoQueueSubmissionText, "private static bool IsForceRotateQueueGuarded(int queueDepth, int queueCapacity)");
        AssertContains(videoQueueSubmissionText, "queueDepth >= Math.Ceiling(queueCapacity * ForceRotateQueueGuardRatio)");
        AssertContains(videoQueueSubmissionText, "private bool TryEnqueueAudioPacket(");
        AssertContains(videoQueueSubmissionText, "Volatile.Read(ref _forceRotateDraining)");
        AssertContains(videoQueueSubmissionText, "TryWriteAudioPacket(queue, packet, ref queueDepth, \"audio\")");
        AssertContains(videoQueueSubmissionText, "TryWriteAudioPacket(queue, packet, ref queueDepth, \"audio_after_evict\")");
        AssertContains(videoQueueSubmissionText, "FLASHBACK_SINK_AUDIO_EVICT_PTS");
        AssertContains(videoQueueSubmissionText, "private static bool TryWriteAudioPacket(");
        AssertContains(videoQueueSubmissionText, "DecrementQueueDepth(ref queueDepth, $\"{queueName}_write_failed\");");

        AssertDoesNotContain(queuesText, "private VideoEnqueueResult TryEnqueueVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertDoesNotContain(queuesText, "private VideoEnqueueResult TryEnqueueGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)");
        AssertDoesNotContain(queuesText, "private string? GetVideoEnqueueRejectReason(bool isGpu)");
        AssertDoesNotContain(queuesText, "private string? GetVideoInputRejectReason(");
        AssertDoesNotContain(queuesText, "private string? GetGpuInputRejectReason(");
        AssertDoesNotContain(queuesText, "private bool TryWriteVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertDoesNotContain(queuesText, "private bool TryWriteGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)");
        AssertDoesNotContain(queuesText, "private void TrackVideoQueueRejected(string reason)");
        AssertDoesNotContain(queuesText, "private void TrackGpuQueueRejected(string reason)");

        AssertContains(queueCleanupText, "private void ReturnAllRemainingQueuedBuffers()");
        AssertContains(queueCleanupText, "private void ReturnRemainingBuffers(Channel<VideoFramePacket>? queue, ref int queueDepth)");
        AssertContains(queueCleanupText, "private static void ReturnRemainingBuffers(Channel<AudioSamplePacket>? queue, ref int queueDepth)");
        AssertContains(queueCleanupText, "private static void ReturnRemainingGpuBuffers(Channel<GpuFramePacket>? queue, ref int queueDepth)");
        AssertContains(queueCleanupText, "ReturnVideoPacketBestEffort(packet);");
        AssertContains(queueCleanupText, "_videoLatencyTracker.ClearEnqueueTicksUnderLock();");
        AssertContains(queueCleanupText, "ReturnBuffer(packet.Buffer);");
        AssertContains(queueCleanupText, "ReleaseGpuTextureBestEffort(packet.Texture);");
        AssertContains(queueCleanupText, "Interlocked.Exchange(ref queueDepth, 0);");

        AssertContains(queuesText, "private void ReturnAllRemainingQueuedBuffers()");
        AssertContains(queuesText, "private void ReturnRemainingBuffers(Channel<VideoFramePacket>? queue, ref int queueDepth)");
        AssertContains(queuesText, "private static void ReturnRemainingBuffers(Channel<AudioSamplePacket>? queue, ref int queueDepth)");
        AssertContains(queuesText, "private static void ReturnRemainingGpuBuffers(Channel<GpuFramePacket>? queue, ref int queueDepth)");
        AssertContains(queuesText, "private void CompleteWriter<TPacket>(Channel<TPacket>? channel)");
        AssertContains(queuesText, "private void SignalWork(string operation)");
        AssertContains(queuesText, "private bool WaitForCancellation(TimeSpan timeout)");
        AssertContains(queuesText, "private void FailEncoding(Exception ex)");
        AssertContains(queuesText, "private static void DecrementQueueDepth(ref int target, string queueName)");
        AssertDoesNotContain(queuesText, "private bool TryEnqueueAudioPacket(");
        AssertDoesNotContain(queuesText, "private static bool TryWriteAudioPacket(");
        AssertDoesNotContain(queuesText, "private static bool IsForceRotateQueueGuarded(");
        AssertDoesNotContain(queuesText, "private void ResetVideoDiagnostics()");

        AssertContains(docsText, "FlashbackEncoderSink.VideoQueueSubmission.cs");
        foreach (var removedFile in new[]
        {
            "FlashbackEncoderSink.VideoQueueSubmission.Guards.cs",
            "FlashbackEncoderSink.VideoQueueSubmission.Writers.cs",
            "FlashbackEncoderSink.VideoQueueSubmission.Rejections.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", removedFile)),
                $"{removedFile} folded into FlashbackEncoderSink.VideoQueueSubmission.cs");
        }
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.AudioQueueSubmission.cs")),
            "FlashbackEncoderSink.AudioQueueSubmission.cs folded into FlashbackEncoderSink.VideoQueueSubmission.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_ForceRotateLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var forceRotateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotate.cs")
            .Replace("\r\n", "\n");
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(forceRotateText, "public bool IsForceRotateActive =>");
        AssertContains(forceRotateText, "public bool IsForceRotateRequested =>");
        AssertContains(forceRotateText, "public bool IsForceRotateDraining =>");
        AssertContains(forceRotateText, "public bool WaitForForceRotateIdle(TimeSpan timeout)");
        AssertContains(forceRotateText, "private bool _forceRotateRequested;");
        AssertContains(forceRotateText, "private volatile ForceRotateRequest? _forceRotateRequest;");
        AssertContains(forceRotateText, "private TimeSpan _forceRotateInPoint;");
        AssertContains(forceRotateText, "private TimeSpan _forceRotateOutPoint;");
        AssertContains(forceRotateText, "private bool _forceRotateDraining;");
        AssertContains(forceRotateText, "public FlashbackForceRotateResult ForceRotateForExport(");
        AssertContains(forceRotateText, "private const int ForceRotateCommittedGraceMs = 1_000;");
        AssertContains(forceRotateText, "var request = new ForceRotateRequest();");
        AssertContains(forceRotateText, "TryCancelForceRotate(request)");
        AssertContains(forceRotateText, "private bool TryCancelForceRotate(ForceRotateRequest request)");
        AssertContains(forceRotateText, "private void CompletePendingForceRotateWithEmptyResult()");
        AssertContains(forceRotateText, "private static bool ShouldAbortForceRotateDrain(");
        AssertContains(forceRotateText, "private sealed class ForceRotateRequest");
        AssertContains(forceRotateText, "public bool TryBeginCommit()");
        AssertContains(forceRotateText, "public bool TryCancel()");
        AssertContains(forceRotateText, "public void Complete(IReadOnlyList<string> paths)");
        AssertContains(forceRotateText, "private bool ProcessPendingForceRotate(");
        AssertContains(forceRotateText, "Volatile.Write(ref _forceRotateDraining, true);");
        AssertContains(forceRotateText, "while (DrainAudioPackets(audioQueue.Reader, AudioDrainBatchLimit))");
        AssertContains(forceRotateText, "while (DrainMicrophonePackets(microphoneQueue.Reader, AudioDrainBatchLimit))");
        AssertContains(forceRotateText, "while (DrainGpuPackets(gpuQueue.Reader, GpuDrainBatchLimit))");
        AssertContains(forceRotateText, "while (DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit))");
        AssertContains(forceRotateText, "if (!localRequest.TryBeginCommit())");
        AssertContains(forceRotateText, "if (!RotateSegment(currentPts))");
        AssertContains(forceRotateText, "localRequest.Complete(_bufferManager.GetValidSegmentPaths(localIn, localOut));");
        AssertDoesNotContain(rootText, "public FlashbackForceRotateResult ForceRotateForExport(");
        AssertDoesNotContain(rootText, "public bool IsForceRotateActive =>");
        AssertDoesNotContain(rootText, "public bool WaitForForceRotateIdle(TimeSpan timeout)");
        AssertDoesNotContain(rootText, "private bool _forceRotateRequested;");
        AssertDoesNotContain(rootText, "private TimeSpan _forceRotateInPoint;");
        AssertDoesNotContain(rootText, "private TimeSpan _forceRotateOutPoint;");
        AssertDoesNotContain(rootText, "private bool _forceRotateDraining;");
        AssertDoesNotContain(rootText, "private sealed class ForceRotateRequest");
        AssertDoesNotContain(rootText, "private const int ForceRotateCommittedGraceMs = 1_000;");
        AssertContains(docsText, "FlashbackEncoderSink.ForceRotate.cs");
        foreach (var removedFile in new[]
        {
            "FlashbackEncoderSink.ForceRotateRequests.cs",
            "FlashbackEncoderSink.ForceRotateExecution.cs",
            "FlashbackEncoderSink.ForceRotateLifecycle.cs",
            "FlashbackEncoderSink.ForceRotateRequest.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", removedFile)),
                $"{removedFile} folded into FlashbackEncoderSink.ForceRotate.cs");
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_StopAndDisposeLifecyclesShareShutdownOwner()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var disposeLifecycleText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.DisposeLifecycle.cs")
            .Replace("\r\n", "\n");
        var lifetimeText = disposeLifecycleText;

        AssertContains(lifetimeText, "public Task<FinalizeResult> StopAsync(CancellationToken cancellationToken = default)");
        AssertContains(lifetimeText, "private async Task<FinalizeResult> StopCoreAsync(CancellationToken cancellationToken)");
        AssertContains(lifetimeText, "FLASHBACK_SINK_STOP_DRAIN_TIMEOUT");
        AssertContains(lifetimeText, "FLASHBACK_SINK_STOP_FAIL");
        AssertContains(disposeLifecycleText, "public void Dispose()");
        AssertContains(disposeLifecycleText, "public async ValueTask DisposeAsync()");
        AssertContains(disposeLifecycleText, "private void ScheduleDeferredDisposeCleanup(Task encodingTask)");
        AssertContains(disposeLifecycleText, "private void FinalizeDisposeCore()");
        AssertContains(disposeLifecycleText, "private void CancelEncodingCts(string operation)");
        AssertContains(disposeLifecycleText, "private void DisposeEncoderBestEffort(string operation)");
        AssertDoesNotContain(rootText, "public Task<FinalizeResult> StopAsync(CancellationToken cancellationToken = default)");
        AssertDoesNotContain(rootText, "public async ValueTask DisposeAsync()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.Lifetime.cs")),
            "FlashbackEncoderSink.Lifetime.cs folded into FlashbackEncoderSink.DisposeLifecycle.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_ProducerInputsLiveInCohesivePartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var inputsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Inputs.cs")
            .Replace("\r\n", "\n");
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(inputsText, "public bool TryEnqueueRawVideoFrame(ReadOnlySpan<byte> data, int expectedSize)");
        AssertContains(inputsText, "bool IRawVideoFrameLeaseTryEncoder.TryEnqueueRawVideoFrame(PooledVideoFrameLease frame)");
        AssertContains(inputsText, "public bool TryEnqueueGpuVideoFrame(IntPtr d3d11Texture2D, int subresourceIndex)");
        AssertContains(inputsText, "MfSourceReaderVideoCapture.GetFrameSizeBytes");
        AssertContains(inputsText, "Marshal.AddRef(d3d11Texture2D);");
        AssertContains(inputsText, "TrackVideoQueueRejected(rejectReason);");
        AssertContains(inputsText, "TrackGpuQueueRejected(rejectReason);");
        AssertContains(inputsText, "public void EnqueueAudioSamples(ReadOnlyMemory<byte> samples)");
        AssertContains(inputsText, "public void EnqueueMicrophoneSamples(ReadOnlyMemory<byte> samples)");
        AssertContains(inputsText, "public Task WriteAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)");
        AssertContains(inputsText, "public Task WriteMicrophoneAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)");
        AssertContains(inputsText, "Hot WASAPI callback path: copy/enqueue only, never await or block.");
        AssertContains(inputsText, "TryValidateAudioPacketLength(samples.Length, \"audio\")");
        AssertContains(inputsText, "TryValidateAudioPacketLength(samples.Length, \"microphone\")");

        AssertDoesNotContain(rootText, "public bool TryEnqueueRawVideoFrame(ReadOnlySpan<byte> data, int expectedSize)");
        AssertDoesNotContain(rootText, "public void EnqueueAudioSamples(ReadOnlyMemory<byte> samples)");
        AssertContains(docsText, "FlashbackEncoderSink.Inputs.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.Inputs.Video.cs")),
            "FlashbackEncoderSink video producer inputs folded into FlashbackEncoderSink.Inputs.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.Inputs.Audio.cs")),
            "FlashbackEncoderSink audio producer inputs folded into FlashbackEncoderSink.Inputs.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_RuntimeStateLivesInCohesivePartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var runtimeStateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.RuntimeState.cs")
            .Replace("\r\n", "\n");
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(runtimeStateText, "public event EventHandler<long>? FrameEncoded;");
        AssertContains(runtimeStateText, "public long DroppedVideoFrames =>");
        AssertContains(runtimeStateText, "public long VideoFramesSubmittedToEncoder =>");
        AssertContains(runtimeStateText, "public long SegmentRotationFailures =>");
        AssertContains(runtimeStateText, "public int VideoQueueCount =>");
        AssertContains(runtimeStateText, "public long VideoQueueRejectedFrames =>");
        AssertContains(runtimeStateText, "public (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) VideoQueueLatencyMetrics");
        AssertContains(runtimeStateText, "public long GpuQueueRejectedFrames =>");
        AssertContains(runtimeStateText, "public bool EncodingFailed =>");
        AssertContains(runtimeStateText, "public void SetFatalErrorCallback(Action<Exception>? callback)");
        AssertContains(runtimeStateText, "public string? CodecName =>");
        AssertContains(runtimeStateText, "public bool? IsP010 =>");
        AssertContains(runtimeStateText, "private static long ToNonNegativeLongSaturated(double value)");
        AssertContains(runtimeStateText, "private static long NonNegativeByteDelta(long currentBytes, long startBytes)");
        AssertContains(runtimeStateText, "private static TimeSpan NonNegativeDuration(TimeSpan end, TimeSpan start)");
        AssertContains(runtimeStateText, "private static (TimeSpan StartPts, TimeSpan EndPts) ResumeEvictionBestEffort(");
        AssertContains(runtimeStateText, "internal Task EncodingCompletionTask =>");

        AssertDoesNotContain(rootText, "public event EventHandler<long>? FrameEncoded;");
        AssertContains(docsText, "FlashbackEncoderSink.RuntimeState.cs");
        foreach (var removedFile in new[]
        {
            "FlashbackEncoderSink.RuntimeState.Counters.cs",
            "FlashbackEncoderSink.RuntimeState.QueueMetrics.cs",
            "FlashbackEncoderSink.RuntimeState.Status.cs",
            "FlashbackEncoderSink.RecordingAccounting.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", removedFile)),
                $"{removedFile} folded into FlashbackEncoderSink.RuntimeState.cs");
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_RecordingLifecycleLivesInCohesivePartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var recordingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Recording.cs")
            .Replace("\r\n", "\n");
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(recordingText, "public TimeSpan LastRecordingStartPts { get; private set; }");
        AssertContains(recordingText, "public TimeSpan LastRecordingEndPts { get; private set; }");
        AssertContains(recordingText, "public bool IsRecordingActive =>");
        AssertContains(recordingText, "public bool CanBeginRecording");
        AssertContains(recordingText, "!_bufferManager.IsSessionPreservedForRecovery");
        AssertContains(recordingText, "Task IRecordingSink.StartAsync(RecordingContext context, CancellationToken cancellationToken)");
        AssertContains(recordingText, "public void BeginRecording(string outputPath)");
        AssertContains(recordingText, "Cannot begin recording: flashback export rotation is still draining.");
        AssertContains(recordingText, "_bufferManager.PauseEviction();");
        AssertContains(recordingText, "public void CancelRecordingStartRollback(string reason)");
        AssertContains(recordingText, "ResumeEvictionBestEffort(_bufferManager, \"recording_start_rollback\")");
        AssertContains(recordingText, "public async Task<FinalizeResult> EndRecordingAsync(CancellationToken cancellationToken)");
        AssertContains(recordingText, "FLASHBACK_RECORDING_END_REJECTED");
        AssertContains(recordingText, "FLASHBACK_RECORDING_FAIL");
        AssertContains(recordingText, "ResumeEvictionBestEffort(_bufferManager, \"recording_end\")");
        AssertContains(recordingText, "FLASHBACK_RECORDING_READY");

        AssertDoesNotContain(rootText, "public void BeginRecording(string outputPath)");
        AssertContains(docsText, "FlashbackEncoderSink.Recording.cs");
        foreach (var removedFile in new[]
        {
            "FlashbackEncoderSink.Recording.State.cs",
            "FlashbackEncoderSink.Recording.Start.cs",
            "FlashbackEncoderSink.Recording.End.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", removedFile)),
                $"{removedFile} folded into FlashbackEncoderSink.Recording.cs");
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_OptionsHelpersLiveInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Startup.cs")
            .Replace("\r\n", "\n");
        var optionsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Options.cs")
            .Replace("\r\n", "\n");
        var sessionContextText = optionsText;
        var queuesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Queues.cs")
            .Replace("\r\n", "\n");
        var packetBuffersText = queuesText;
        var packetTypesText = packetBuffersText;
        var inputsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Inputs.cs")
            .Replace("\r\n", "\n");
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(optionsText, "private static LibAvEncoderOptions CreateOptions(FlashbackSessionContext context, string outputPath)");
        AssertContains(optionsText, "internal static string GetSegmentExtension(string codecName)");
        AssertContains(optionsText, "private static (int? Numerator, int? Denominator) ResolveSessionFrameRateParts(int? numerator, int? denominator)");
        AssertContains(optionsText, "private static FlashbackSessionContext CreateSessionContext(RecordingContext context)");
        AssertContains(optionsText, "private static (int? Numerator, int? Denominator) ResolveFrameRateParts(string frameRateArg)");
        AssertContains(optionsText, "private static string MapCodecName(RecordingFormat format)");
        AssertDoesNotContain(optionsText, "private readonly record struct VideoFramePacket");
        AssertDoesNotContain(optionsText, "private static byte[] GetBuffer");

        AssertContains(sessionContextText, "private static FlashbackSessionContext CreateSessionContext(RecordingContext context)");
        AssertContains(sessionContextText, "private static (int? Numerator, int? Denominator) ResolveFrameRateParts(string frameRateArg)");
        AssertContains(sessionContextText, "private static string MapCodecName(RecordingFormat format)");
        AssertContains(sessionContextText, "SplitEncodeModeParser.ToWireString(context.Settings.SplitEncodeMode)");

        AssertContains(startupText, "private static string CreateSessionId()");
        AssertContains(startupText, "DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()");

        AssertContains(packetBuffersText, "private static byte[] GetBuffer(int size)");
        AssertContains(packetBuffersText, "private static void ReturnBuffer(byte[] buffer)");
        AssertContains(packetBuffersText, "private static void ReturnVideoPacket(VideoFramePacket packet)");
        AssertContains(packetBuffersText, "private static void ReturnVideoPacketBestEffort(VideoFramePacket packet)");
        AssertContains(packetBuffersText, "private static void ReleaseGpuTextureBestEffort(IntPtr texture)");
        AssertContains(packetBuffersText, "ArrayPool<byte>.Shared.Rent(size)");
        AssertContains(packetBuffersText, "Marshal.Release(texture);");

        AssertContains(packetTypesText, "private readonly record struct VideoFramePacket");
        AssertContains(packetTypesText, "private enum VideoEnqueueResult");
        AssertContains(packetTypesText, "private readonly record struct AudioSamplePacket");
        AssertContains(packetTypesText, "private readonly record struct GpuFramePacket");

        AssertContains(inputsText, "private static long GetSampleCount(int byteLength)");
        AssertContains(inputsText, "private static bool TryValidateAudioPacketLength(int byteLength, string source)");
        AssertDoesNotContain(rootText, "private static FlashbackSessionContext CreateSessionContext");
        AssertDoesNotContain(rootText, "private static byte[] GetBuffer");
        AssertContains(docsText, "FlashbackEncoderSink.Options.cs");
        AssertContains(docsText, "recording-to-Flashback session mapping");
        AssertContains(docsText, "generated session ID formatting");
        AssertContains(docsText, "FlashbackEncoderSink.Queues.cs");
        AssertContains(docsText, "packet DTOs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackEncoderSink.PacketBuffers.cs")),
            "FlashbackEncoderSink.PacketBuffers.cs folded into FlashbackEncoderSink.Queues.cs");

        return Task.CompletedTask;
    }
}
