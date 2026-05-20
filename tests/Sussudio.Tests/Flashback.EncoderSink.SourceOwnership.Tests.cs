using System.Threading.Tasks;

static partial class Program
{
    internal static Task FlashbackEncoderSink_StartupLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Startup.cs")
            .Replace("\r\n", "\n");

        AssertContains(startupText, "public Task StartAsync(FlashbackSessionContext context, CancellationToken cancellationToken = default, TimeSpan ptsBaseOffset = default)");
        AssertContains(startupText, "ValidateSessionContext(context);");
        AssertContains(startupText, "var tsPath = _bufferManager.AcquireSegmentPath(out var startupGeneratedSegment);");
        AssertContains(startupText, "_encodingTask = Task.Factory.StartNew(");
        AssertContains(startupText, "FLASHBACK_SINK_START_FAIL");
        AssertContains(startupText, "_bufferManager.AbandonGeneratedSegmentPath(startupGeneratedSegmentPath, restoreActivePath: null);");
        AssertDoesNotContain(rootText, "public Task StartAsync(FlashbackSessionContext context, CancellationToken cancellationToken = default, TimeSpan ptsBaseOffset = default)");
        AssertDoesNotContain(rootText, "FLASHBACK_SINK_START_FAIL");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_PacketDrainLivesInFocusedPartial()
    {
        var loopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingLoop.cs").Replace("\r\n", "\n");
        var packetDrainText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.PacketDrain.cs").Replace("\r\n", "\n");

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
        AssertContains(packetDrainText, "private bool DrainAudioPackets(ChannelReader<AudioSamplePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(packetDrainText, "private bool DrainMicrophonePackets(ChannelReader<AudioSamplePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(packetDrainText, "private void OnVideoFrameEncoded()");
        AssertContains(packetDrainText, "private TimeSpan ResolveEncoderPts()");
        AssertContains(packetDrainText, "MfSourceReaderVideoCapture.GetFrameSizeBytes");
        AssertContains(packetDrainText, "FrameEncoded?.Invoke(this, encoded);");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_QueueCleanupLivesInFocusedPartial()
    {
        var queuesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Queues.cs")
            .Replace("\r\n", "\n");
        var videoQueueSubmissionText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.VideoQueueSubmission.cs")
            .Replace("\r\n", "\n");
        var queueCleanupText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.QueueCleanup.cs")
            .Replace("\r\n", "\n");

        AssertContains(videoQueueSubmissionText, "private VideoEnqueueResult TryEnqueueVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertContains(videoQueueSubmissionText, "private VideoEnqueueResult TryEnqueueGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)");
        AssertContains(videoQueueSubmissionText, "private string? GetVideoEnqueueRejectReason(bool isGpu)");
        AssertContains(videoQueueSubmissionText, "private bool TryWriteVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertContains(videoQueueSubmissionText, "private bool TryWriteGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)");
        AssertContains(videoQueueSubmissionText, "TrackVideoQueueRejected(\"queue_full\");");
        AssertContains(videoQueueSubmissionText, "TrackGpuQueueRejected(\"queue_full\");");
        AssertDoesNotContain(queuesText, "private VideoEnqueueResult TryEnqueueVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertDoesNotContain(queuesText, "private VideoEnqueueResult TryEnqueueGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)");
        AssertDoesNotContain(queuesText, "private bool TryWriteVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertDoesNotContain(queuesText, "private bool TryWriteGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)");

        AssertContains(queueCleanupText, "private void ReturnAllRemainingQueuedBuffers()");
        AssertContains(queueCleanupText, "private void ReturnRemainingBuffers(Channel<VideoFramePacket>? queue, ref int queueDepth)");
        AssertContains(queueCleanupText, "private static void ReturnRemainingBuffers(Channel<AudioSamplePacket>? queue, ref int queueDepth)");
        AssertContains(queueCleanupText, "private static void ReturnRemainingGpuBuffers(Channel<GpuFramePacket>? queue, ref int queueDepth)");
        AssertContains(queueCleanupText, "ReturnVideoPacketBestEffort(packet);");
        AssertContains(queueCleanupText, "_videoLatencyTracker.ClearEnqueueTicksUnderLock();");
        AssertContains(queueCleanupText, "ReturnBuffer(packet.Buffer);");
        AssertContains(queueCleanupText, "ReleaseGpuTextureBestEffort(packet.Texture);");
        AssertContains(queueCleanupText, "Interlocked.Exchange(ref queueDepth, 0);");

        AssertDoesNotContain(queuesText, "private void ReturnAllRemainingQueuedBuffers()");
        AssertDoesNotContain(queuesText, "private void ReturnRemainingBuffers(Channel<VideoFramePacket>? queue, ref int queueDepth)");
        AssertDoesNotContain(queuesText, "private static void ReturnRemainingBuffers(Channel<AudioSamplePacket>? queue, ref int queueDepth)");
        AssertDoesNotContain(queuesText, "private static void ReturnRemainingGpuBuffers(Channel<GpuFramePacket>? queue, ref int queueDepth)");
        AssertContains(queuesText, "private void CompleteWriter<TPacket>(Channel<TPacket>? channel)");
        AssertContains(queuesText, "private void SignalWork(string operation)");
        AssertContains(queuesText, "private bool WaitForCancellation(TimeSpan timeout)");
        AssertContains(queuesText, "private void FailEncoding(Exception ex)");
        AssertContains(queuesText, "private bool TryEnqueueAudioPacket(");
        AssertContains(queuesText, "private static bool TryWriteAudioPacket(");

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_ForceRotateRequestLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var forceRotateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotate.cs")
            .Replace("\r\n", "\n");
        var forceRotateExecutionText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotateExecution.cs")
            .Replace("\r\n", "\n");

        AssertContains(forceRotateText, "public FlashbackForceRotateResult ForceRotateForExport(");
        AssertContains(forceRotateText, "public bool IsForceRotateActive =>");
        AssertContains(forceRotateText, "public bool IsForceRotateRequested =>");
        AssertContains(forceRotateText, "public bool IsForceRotateDraining =>");
        AssertContains(forceRotateText, "public bool WaitForForceRotateIdle(TimeSpan timeout)");
        AssertContains(forceRotateText, "private bool _forceRotateRequested;");
        AssertContains(forceRotateText, "private volatile ForceRotateRequest? _forceRotateRequest;");
        AssertContains(forceRotateText, "private TimeSpan _forceRotateInPoint;");
        AssertContains(forceRotateText, "private TimeSpan _forceRotateOutPoint;");
        AssertContains(forceRotateText, "private bool _forceRotateDraining;");
        AssertContains(forceRotateText, "private sealed class ForceRotateRequest");
        AssertContains(forceRotateText, "private const int ForceRotateCommittedGraceMs = 1_000;");
        AssertContains(forceRotateText, "private static bool ShouldAbortForceRotateDrain(");
        AssertContains(forceRotateExecutionText, "private bool ProcessPendingForceRotate(");
        AssertContains(forceRotateExecutionText, "Volatile.Write(ref _forceRotateDraining, true);");
        AssertContains(forceRotateExecutionText, "while (DrainAudioPackets(audioQueue.Reader, AudioDrainBatchLimit))");
        AssertContains(forceRotateExecutionText, "while (DrainMicrophonePackets(microphoneQueue.Reader, AudioDrainBatchLimit))");
        AssertContains(forceRotateExecutionText, "while (DrainGpuPackets(gpuQueue.Reader, GpuDrainBatchLimit))");
        AssertContains(forceRotateExecutionText, "while (DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit))");
        AssertContains(forceRotateExecutionText, "if (!localRequest.TryBeginCommit())");
        AssertContains(forceRotateExecutionText, "if (!RotateSegment(currentPts))");
        AssertContains(forceRotateExecutionText, "localRequest.Complete(_bufferManager.GetValidSegmentPaths(localIn, localOut));");
        AssertDoesNotContain(rootText, "public FlashbackForceRotateResult ForceRotateForExport(");
        AssertDoesNotContain(rootText, "public bool IsForceRotateActive =>");
        AssertDoesNotContain(rootText, "public bool WaitForForceRotateIdle(TimeSpan timeout)");
        AssertDoesNotContain(rootText, "private bool _forceRotateRequested;");
        AssertDoesNotContain(rootText, "private TimeSpan _forceRotateInPoint;");
        AssertDoesNotContain(rootText, "private TimeSpan _forceRotateOutPoint;");
        AssertDoesNotContain(rootText, "private bool _forceRotateDraining;");
        AssertDoesNotContain(rootText, "private sealed class ForceRotateRequest");
        AssertDoesNotContain(rootText, "private const int ForceRotateCommittedGraceMs = 1_000;");
        AssertDoesNotContain(forceRotateText, "private bool ProcessPendingForceRotate(");

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_StopAndDisposeLifecyclesStaySplit()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var lifetimeText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Lifetime.cs")
            .Replace("\r\n", "\n");
        var disposeLifecycleText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.DisposeLifecycle.cs")
            .Replace("\r\n", "\n");

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
        AssertDoesNotContain(lifetimeText, "public async ValueTask DisposeAsync()");
        AssertDoesNotContain(lifetimeText, "private void FinalizeDisposeCore()");
        AssertDoesNotContain(disposeLifecycleText, "private async Task<FinalizeResult> StopCoreAsync(CancellationToken cancellationToken)");
        AssertDoesNotContain(rootText, "public Task<FinalizeResult> StopAsync(CancellationToken cancellationToken = default)");
        AssertDoesNotContain(rootText, "public async ValueTask DisposeAsync()");

        return Task.CompletedTask;
    }
}
