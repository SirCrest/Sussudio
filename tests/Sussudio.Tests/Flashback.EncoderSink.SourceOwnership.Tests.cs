using System.Threading.Tasks;

static partial class Program
{
    internal static Task FlashbackEncoderSink_StartupLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Startup.cs")
            .Replace("\r\n", "\n");
        var startupQueuesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.StartupQueues.cs")
            .Replace("\r\n", "\n");
        var startupRollbackText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.StartupRollback.cs")
            .Replace("\r\n", "\n");
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(startupText, "public Task StartAsync(FlashbackSessionContext context, CancellationToken cancellationToken = default, TimeSpan ptsBaseOffset = default)");
        AssertContains(startupText, "ValidateSessionContext(context);");
        AssertContains(startupText, "var tsPath = _bufferManager.AcquireSegmentPath(out var startupGeneratedSegment);");
        AssertContains(startupText, "InitializeStartupQueues(sessionContext);");
        AssertContains(startupText, "_encodingTask = Task.Factory.StartNew(");
        AssertContains(startupText, "RollBackStartFailure(ex, startupGeneratedSegmentPath);");
        AssertDoesNotContain(startupText, "Channel.CreateBounded<");
        AssertDoesNotContain(startupText, "FLASHBACK_SINK_START_FAIL");
        AssertDoesNotContain(startupText, "_bufferManager.AbandonGeneratedSegmentPath(startupGeneratedSegmentPath, restoreActivePath: null);");

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
        AssertContains(docsText, "FlashbackEncoderSink.StartupQueues.cs");
        AssertContains(docsText, "FlashbackEncoderSink.StartupRollback.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_RootHelpersLiveInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var startupPolicyText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.StartupPolicy.cs")
            .Replace("\r\n", "\n");
        var diagnosticsResetText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.DiagnosticsReset.cs")
            .Replace("\r\n", "\n");
        var recordingAccountingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.RecordingAccounting.cs")
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

        AssertContains(recordingAccountingText, "private static long ToNonNegativeLongSaturated(double value)");
        AssertContains(recordingAccountingText, "private static long NonNegativeByteDelta(long currentBytes, long startBytes)");
        AssertContains(recordingAccountingText, "private static TimeSpan NonNegativeDuration(TimeSpan end, TimeSpan start)");
        AssertContains(recordingAccountingText, "private static (TimeSpan StartPts, TimeSpan EndPts) ResumeEvictionBestEffort(");
        AssertContains(recordingAccountingText, "FLASHBACK_SINK_EVICTION_RESUME_WARN");

        AssertContains(docsText, "FlashbackEncoderSink.StartupPolicy.cs");
        AssertContains(docsText, "FlashbackEncoderSink.DiagnosticsReset.cs");
        AssertContains(docsText, "FlashbackEncoderSink.RecordingAccounting.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_PacketDrainLivesInFocusedPartial()
    {
        var loopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingLoop.cs").Replace("\r\n", "\n");
        var packetDrainVideoText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.PacketDrain.Video.cs").Replace("\r\n", "\n");
        var packetDrainAudioText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.PacketDrain.Audio.cs").Replace("\r\n", "\n");
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

        AssertContains(packetDrainVideoText, "private bool DrainVideoPackets(ChannelReader<VideoFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(packetDrainVideoText, "private bool DrainGpuPackets(ChannelReader<GpuFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(packetDrainVideoText, "MfSourceReaderVideoCapture.GetFrameSizeBytes");
        AssertContains(packetDrainVideoText, "OnVideoFrameEncoded();");
        AssertDoesNotContain(packetDrainVideoText, "private bool DrainAudioPackets(");

        AssertContains(packetDrainAudioText, "private bool DrainAudioPackets(ChannelReader<AudioSamplePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(packetDrainAudioText, "private bool DrainMicrophonePackets(ChannelReader<AudioSamplePacket> reader, int maxPackets = int.MaxValue)");
        AssertDoesNotContain(packetDrainAudioText, "private bool DrainVideoPackets(");

        AssertContains(encodingProgressText, "private void OnVideoFrameEncoded()");
        AssertContains(encodingProgressText, "private TimeSpan ResolveEncoderPts()");
        AssertContains(encodingProgressText, "_bufferManager.UpdateLatestPts(pts);");
        AssertContains(encodingProgressText, "FrameEncoded?.Invoke(this, encoded);");
        AssertContains(docsText, "FlashbackEncoderSink.PacketDrain.Video.cs");
        AssertContains(docsText, "FlashbackEncoderSink.PacketDrain.Audio.cs");
        AssertContains(docsText, "FlashbackEncoderSink.EncodingProgress.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_QueueCleanupLivesInFocusedPartial()
    {
        var queuesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Queues.cs")
            .Replace("\r\n", "\n");
        var audioQueueSubmissionText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.AudioQueueSubmission.cs")
            .Replace("\r\n", "\n");
        var videoQueueSubmissionText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.VideoQueueSubmission.cs")
            .Replace("\r\n", "\n");
        var videoQueueGuardsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.VideoQueueSubmission.Guards.cs")
            .Replace("\r\n", "\n");
        var videoQueueWritersText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.VideoQueueSubmission.Writers.cs")
            .Replace("\r\n", "\n");
        var videoQueueRejectionsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.VideoQueueSubmission.Rejections.cs")
            .Replace("\r\n", "\n");
        var queueCleanupText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.QueueCleanup.cs")
            .Replace("\r\n", "\n");
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(videoQueueSubmissionText, "private VideoEnqueueResult TryEnqueueVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertContains(videoQueueSubmissionText, "private VideoEnqueueResult TryEnqueueGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)");
        AssertContains(videoQueueSubmissionText, "TryWriteVideoPacket(queue, packet)");
        AssertContains(videoQueueSubmissionText, "TryWriteGpuPacket(queue, packet)");
        AssertContains(videoQueueSubmissionText, "TrackVideoQueueRejected(\"queue_full\");");
        AssertContains(videoQueueSubmissionText, "TrackGpuQueueRejected(\"queue_full\");");
        AssertDoesNotContain(videoQueueSubmissionText, "private string? GetVideoEnqueueRejectReason(bool isGpu)");
        AssertDoesNotContain(videoQueueSubmissionText, "private string? GetVideoInputRejectReason(");
        AssertDoesNotContain(videoQueueSubmissionText, "private string? GetGpuInputRejectReason(");
        AssertDoesNotContain(videoQueueSubmissionText, "private bool TryWriteVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertDoesNotContain(videoQueueSubmissionText, "private bool TryWriteGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)");
        AssertDoesNotContain(videoQueueSubmissionText, "private void TrackVideoQueueRejected(string reason)");
        AssertDoesNotContain(videoQueueSubmissionText, "private void TrackGpuQueueRejected(string reason)");

        AssertContains(videoQueueGuardsText, "private string? GetVideoEnqueueRejectReason(bool isGpu)");
        AssertContains(videoQueueGuardsText, "private string? GetVideoInputRejectReason(Channel<VideoFramePacket>? queue, int expectedSize, bool dataIsEmpty)");
        AssertContains(videoQueueGuardsText, "private string? GetGpuInputRejectReason(Channel<GpuFramePacket>? queue, IntPtr texture)");
        AssertContains(videoQueueGuardsText, "return \"force_rotate_draining\";");
        AssertContains(videoQueueGuardsText, "? $\"encoding_failed:{failure.GetType().Name}\"");
        AssertContains(videoQueueGuardsText, "return dataIsEmpty ? \"data_empty\" : null;");
        AssertContains(videoQueueGuardsText, "return texture == IntPtr.Zero ? \"null_texture\" : null;");
        AssertDoesNotContain(videoQueueGuardsText, "private bool TryWriteVideoPacket(");
        AssertDoesNotContain(videoQueueGuardsText, "private void TrackVideoQueueRejected(");

        AssertContains(videoQueueWritersText, "private bool TryWriteVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertContains(videoQueueWritersText, "private bool TryWriteGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)");
        AssertContains(videoQueueWritersText, "AtomicMax.Update(ref _videoQueueMaxDepth, depth);");
        AssertContains(videoQueueWritersText, "AtomicMax.Update(ref _gpuQueueMaxDepth, depth);");
        AssertContains(videoQueueWritersText, "DecrementQueueDepth(ref _videoQueueDepth, \"video_write_failed\");");
        AssertContains(videoQueueWritersText, "DecrementQueueDepth(ref _gpuQueueDepth, \"gpu_write_failed\");");
        AssertDoesNotContain(videoQueueWritersText, "private string? GetVideoEnqueueRejectReason(");
        AssertDoesNotContain(videoQueueWritersText, "private void TrackVideoQueueRejected(");

        AssertContains(videoQueueRejectionsText, "private void TrackVideoQueueRejected(string reason)");
        AssertContains(videoQueueRejectionsText, "private void TrackGpuQueueRejected(string reason)");
        AssertContains(videoQueueRejectionsText, "FLASHBACK_SINK_VIDEO_QUEUE_REJECT");
        AssertContains(videoQueueRejectionsText, "FLASHBACK_SINK_GPU_QUEUE_REJECT");
        AssertContains(videoQueueRejectionsText, "total == 1 || total % 30 == 0");
        AssertDoesNotContain(videoQueueRejectionsText, "private bool TryWriteVideoPacket(");
        AssertDoesNotContain(videoQueueRejectionsText, "private string? GetVideoEnqueueRejectReason(");

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

        AssertDoesNotContain(queuesText, "private void ReturnAllRemainingQueuedBuffers()");
        AssertDoesNotContain(queuesText, "private void ReturnRemainingBuffers(Channel<VideoFramePacket>? queue, ref int queueDepth)");
        AssertDoesNotContain(queuesText, "private static void ReturnRemainingBuffers(Channel<AudioSamplePacket>? queue, ref int queueDepth)");
        AssertDoesNotContain(queuesText, "private static void ReturnRemainingGpuBuffers(Channel<GpuFramePacket>? queue, ref int queueDepth)");
        AssertContains(queuesText, "private void CompleteWriter<TPacket>(Channel<TPacket>? channel)");
        AssertContains(queuesText, "private void SignalWork(string operation)");
        AssertContains(queuesText, "private bool WaitForCancellation(TimeSpan timeout)");
        AssertContains(queuesText, "private void FailEncoding(Exception ex)");
        AssertContains(queuesText, "private static void DecrementQueueDepth(ref int target, string queueName)");
        AssertDoesNotContain(queuesText, "private bool TryEnqueueAudioPacket(");
        AssertDoesNotContain(queuesText, "private static bool TryWriteAudioPacket(");
        AssertDoesNotContain(queuesText, "private static bool IsForceRotateQueueGuarded(");
        AssertDoesNotContain(queuesText, "private void ResetVideoDiagnostics()");

        AssertContains(audioQueueSubmissionText, "private static bool IsForceRotateQueueGuarded(int queueDepth, int queueCapacity)");
        AssertContains(audioQueueSubmissionText, "queueDepth >= Math.Ceiling(queueCapacity * ForceRotateQueueGuardRatio)");
        AssertContains(audioQueueSubmissionText, "private bool TryEnqueueAudioPacket(");
        AssertContains(audioQueueSubmissionText, "Volatile.Read(ref _forceRotateDraining)");
        AssertContains(audioQueueSubmissionText, "TryWriteAudioPacket(queue, packet, ref queueDepth, \"audio\")");
        AssertContains(audioQueueSubmissionText, "TryWriteAudioPacket(queue, packet, ref queueDepth, \"audio_after_evict\")");
        AssertContains(audioQueueSubmissionText, "FLASHBACK_SINK_AUDIO_EVICT_PTS");
        AssertContains(audioQueueSubmissionText, "private static bool TryWriteAudioPacket(");
        AssertContains(audioQueueSubmissionText, "DecrementQueueDepth(ref queueDepth, $\"{queueName}_write_failed\");");
        AssertDoesNotContain(audioQueueSubmissionText, "private void CompleteWriter<TPacket>");
        AssertDoesNotContain(audioQueueSubmissionText, "private void FailEncoding(Exception ex)");
        AssertContains(docsText, "FlashbackEncoderSink.VideoQueueSubmission.Guards.cs");
        AssertContains(docsText, "FlashbackEncoderSink.VideoQueueSubmission.Writers.cs");
        AssertContains(docsText, "FlashbackEncoderSink.VideoQueueSubmission.Rejections.cs");
        AssertContains(docsText, "FlashbackEncoderSink.AudioQueueSubmission.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_ForceRotateRequestsLiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var forceRotateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotate.cs")
            .Replace("\r\n", "\n");
        var forceRotateRequestsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotateRequests.cs")
            .Replace("\r\n", "\n");
        var forceRotateLifecycleText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotateLifecycle.cs")
            .Replace("\r\n", "\n");
        var forceRotateRequestText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotateRequest.cs")
            .Replace("\r\n", "\n");
        var forceRotateExecutionText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotateExecution.cs")
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
        AssertContains(forceRotateRequestsText, "public FlashbackForceRotateResult ForceRotateForExport(");
        AssertContains(forceRotateRequestsText, "private const int ForceRotateCommittedGraceMs = 1_000;");
        AssertContains(forceRotateRequestsText, "var request = new ForceRotateRequest();");
        AssertContains(forceRotateRequestsText, "TryCancelForceRotate(request)");
        AssertDoesNotContain(forceRotateRequestsText, "private sealed class ForceRotateRequest");
        AssertDoesNotContain(forceRotateRequestsText, "private void CompletePendingForceRotateWithEmptyResult()");
        AssertContains(forceRotateLifecycleText, "private bool TryCancelForceRotate(ForceRotateRequest request)");
        AssertContains(forceRotateLifecycleText, "private void CompletePendingForceRotateWithEmptyResult()");
        AssertContains(forceRotateLifecycleText, "private static bool ShouldAbortForceRotateDrain(");
        AssertContains(forceRotateRequestText, "private sealed class ForceRotateRequest");
        AssertContains(forceRotateRequestText, "public bool TryBeginCommit()");
        AssertContains(forceRotateRequestText, "public bool TryCancel()");
        AssertContains(forceRotateRequestText, "public void Complete(IReadOnlyList<string> paths)");
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
        AssertDoesNotContain(forceRotateText, "public FlashbackForceRotateResult ForceRotateForExport(");
        AssertDoesNotContain(forceRotateText, "private sealed class ForceRotateRequest");
        AssertDoesNotContain(forceRotateText, "private const int ForceRotateCommittedGraceMs = 1_000;");
        AssertDoesNotContain(forceRotateText, "private bool ProcessPendingForceRotate(");
        AssertContains(docsText, "FlashbackEncoderSink.ForceRotateLifecycle.cs");
        AssertContains(docsText, "FlashbackEncoderSink.ForceRotateRequest.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_StopAndDisposeLifecyclesStaySplit()
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

    internal static Task FlashbackEncoderSink_ProducerInputsLiveInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var videoInputsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Inputs.Video.cs")
            .Replace("\r\n", "\n");
        var audioInputsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Inputs.Audio.cs")
            .Replace("\r\n", "\n");
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(videoInputsText, "public bool TryEnqueueRawVideoFrame(ReadOnlySpan<byte> data, int expectedSize)");
        AssertContains(videoInputsText, "bool IRawVideoFrameLeaseTryEncoder.TryEnqueueRawVideoFrame(PooledVideoFrameLease frame)");
        AssertContains(videoInputsText, "public bool TryEnqueueGpuVideoFrame(IntPtr d3d11Texture2D, int subresourceIndex)");
        AssertContains(videoInputsText, "MfSourceReaderVideoCapture.GetFrameSizeBytes");
        AssertContains(videoInputsText, "Marshal.AddRef(d3d11Texture2D);");
        AssertContains(videoInputsText, "TrackVideoQueueRejected(rejectReason);");
        AssertContains(videoInputsText, "TrackGpuQueueRejected(rejectReason);");
        AssertDoesNotContain(videoInputsText, "WriteAudioAsync");
        AssertDoesNotContain(videoInputsText, "EnqueueMicrophoneSamples");

        AssertContains(audioInputsText, "public void EnqueueAudioSamples(ReadOnlyMemory<byte> samples)");
        AssertContains(audioInputsText, "public void EnqueueMicrophoneSamples(ReadOnlyMemory<byte> samples)");
        AssertContains(audioInputsText, "public Task WriteAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)");
        AssertContains(audioInputsText, "public Task WriteMicrophoneAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)");
        AssertContains(audioInputsText, "Hot WASAPI callback path: copy/enqueue only, never await or block.");
        AssertContains(audioInputsText, "TryValidateAudioPacketLength(samples.Length, \"audio\")");
        AssertContains(audioInputsText, "TryValidateAudioPacketLength(samples.Length, \"microphone\")");
        AssertDoesNotContain(audioInputsText, "TryEnqueueRawVideoFrame");
        AssertDoesNotContain(audioInputsText, "TryEnqueueGpuVideoFrame");

        AssertDoesNotContain(rootText, "public bool TryEnqueueRawVideoFrame(ReadOnlySpan<byte> data, int expectedSize)");
        AssertDoesNotContain(rootText, "public void EnqueueAudioSamples(ReadOnlyMemory<byte> samples)");
        AssertContains(docsText, "FlashbackEncoderSink.Inputs.Video.cs");
        AssertContains(docsText, "FlashbackEncoderSink.Inputs.Audio.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_RuntimeStateLivesInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var countersText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.RuntimeState.Counters.cs")
            .Replace("\r\n", "\n");
        var queueMetricsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.RuntimeState.QueueMetrics.cs")
            .Replace("\r\n", "\n");
        var statusText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.RuntimeState.Status.cs")
            .Replace("\r\n", "\n");
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(countersText, "public event EventHandler<long>? FrameEncoded;");
        AssertContains(countersText, "public long DroppedVideoFrames =>");
        AssertContains(countersText, "public long VideoFramesSubmittedToEncoder =>");
        AssertContains(countersText, "public long SegmentRotationFailures =>");
        AssertDoesNotContain(countersText, "public void SetFatalErrorCallback");
        AssertDoesNotContain(countersText, "public (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) VideoQueueLatencyMetrics");

        AssertContains(queueMetricsText, "public int VideoQueueCount =>");
        AssertContains(queueMetricsText, "public long VideoQueueRejectedFrames =>");
        AssertContains(queueMetricsText, "public (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) VideoQueueLatencyMetrics");
        AssertContains(queueMetricsText, "public long GpuQueueRejectedFrames =>");
        AssertDoesNotContain(queueMetricsText, "public bool EncodingFailed =>");
        AssertDoesNotContain(queueMetricsText, "public string? CodecName =>");

        AssertContains(statusText, "public bool EncodingFailed =>");
        AssertContains(statusText, "public void SetFatalErrorCallback(Action<Exception>? callback)");
        AssertContains(statusText, "public string? CodecName =>");
        AssertContains(statusText, "public bool? IsP010 =>");
        AssertContains(statusText, "internal Task EncodingCompletionTask =>");
        AssertDoesNotContain(statusText, "public long DroppedVideoFrames =>");
        AssertDoesNotContain(statusText, "public int VideoQueueCount =>");

        AssertDoesNotContain(rootText, "public event EventHandler<long>? FrameEncoded;");
        AssertContains(docsText, "FlashbackEncoderSink.RuntimeState.Counters.cs");
        AssertContains(docsText, "FlashbackEncoderSink.RuntimeState.QueueMetrics.cs");
        AssertContains(docsText, "FlashbackEncoderSink.RuntimeState.Status.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_RecordingLifecycleLivesInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var recordingStateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Recording.State.cs")
            .Replace("\r\n", "\n");
        var recordingStartText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Recording.Start.cs")
            .Replace("\r\n", "\n");
        var recordingEndText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Recording.End.cs")
            .Replace("\r\n", "\n");
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(recordingStateText, "public TimeSpan LastRecordingStartPts { get; private set; }");
        AssertContains(recordingStateText, "public TimeSpan LastRecordingEndPts { get; private set; }");
        AssertContains(recordingStateText, "public bool IsRecordingActive =>");
        AssertContains(recordingStateText, "public bool CanBeginRecording");
        AssertContains(recordingStateText, "!_bufferManager.IsSessionPreservedForRecovery");
        AssertDoesNotContain(recordingStateText, "public void BeginRecording(");
        AssertDoesNotContain(recordingStateText, "public async Task<FinalizeResult> EndRecordingAsync");

        AssertContains(recordingStartText, "Task IRecordingSink.StartAsync(RecordingContext context, CancellationToken cancellationToken)");
        AssertContains(recordingStartText, "public void BeginRecording(string outputPath)");
        AssertContains(recordingStartText, "Cannot begin recording: flashback export rotation is still draining.");
        AssertContains(recordingStartText, "_bufferManager.PauseEviction();");
        AssertContains(recordingStartText, "public void CancelRecordingStartRollback(string reason)");
        AssertContains(recordingStartText, "ResumeEvictionBestEffort(_bufferManager, \"recording_start_rollback\")");
        AssertDoesNotContain(recordingStartText, "public bool CanBeginRecording");
        AssertDoesNotContain(recordingStartText, "public async Task<FinalizeResult> EndRecordingAsync");

        AssertContains(recordingEndText, "public async Task<FinalizeResult> EndRecordingAsync(CancellationToken cancellationToken)");
        AssertContains(recordingEndText, "FLASHBACK_RECORDING_END_REJECTED");
        AssertContains(recordingEndText, "FLASHBACK_RECORDING_FAIL");
        AssertContains(recordingEndText, "ResumeEvictionBestEffort(_bufferManager, \"recording_end\")");
        AssertContains(recordingEndText, "FLASHBACK_RECORDING_READY");
        AssertDoesNotContain(recordingEndText, "public void BeginRecording(");
        AssertDoesNotContain(recordingEndText, "public bool CanBeginRecording");

        AssertDoesNotContain(rootText, "public void BeginRecording(string outputPath)");
        AssertContains(docsText, "FlashbackEncoderSink.Recording.State.cs");
        AssertContains(docsText, "FlashbackEncoderSink.Recording.Start.cs");
        AssertContains(docsText, "FlashbackEncoderSink.Recording.End.cs");

        return Task.CompletedTask;
    }

    internal static Task FlashbackEncoderSink_OptionsHelpersLiveInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var optionsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Options.cs")
            .Replace("\r\n", "\n");
        var sessionContextText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.SessionContext.cs")
            .Replace("\r\n", "\n");
        var fileSessionText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.FileSessionHelpers.cs")
            .Replace("\r\n", "\n");
        var packetBuffersText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.PacketBuffers.cs")
            .Replace("\r\n", "\n");
        var packetTypesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.PacketTypes.cs")
            .Replace("\r\n", "\n");
        var audioInputsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Inputs.Audio.cs")
            .Replace("\r\n", "\n");
        var docsText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n") + "\n" +
            ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(optionsText, "private static LibAvEncoderOptions CreateOptions(FlashbackSessionContext context, string outputPath)");
        AssertContains(optionsText, "internal static string GetSegmentExtension(string codecName)");
        AssertContains(optionsText, "private static (int? Numerator, int? Denominator) ResolveSessionFrameRateParts(int? numerator, int? denominator)");
        AssertDoesNotContain(optionsText, "private static FlashbackSessionContext CreateSessionContext");
        AssertDoesNotContain(optionsText, "private readonly record struct VideoFramePacket");
        AssertDoesNotContain(optionsText, "private static byte[] GetBuffer");

        AssertContains(sessionContextText, "private static FlashbackSessionContext CreateSessionContext(RecordingContext context)");
        AssertContains(sessionContextText, "private static (int? Numerator, int? Denominator) ResolveFrameRateParts(string frameRateArg)");
        AssertContains(sessionContextText, "private static string MapCodecName(RecordingFormat format)");
        AssertContains(sessionContextText, "SplitEncodeModeParser.ToWireString(context.Settings.SplitEncodeMode)");

        AssertContains(fileSessionText, "private static long GetFileSize(string path)");
        AssertContains(fileSessionText, "private static string CreateSessionId()");
        AssertContains(fileSessionText, "FLASHBACK_SINK_FILE_SIZE_WARN");

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

        AssertContains(audioInputsText, "private static long GetSampleCount(int byteLength)");
        AssertContains(audioInputsText, "private static bool TryValidateAudioPacketLength(int byteLength, string source)");
        AssertDoesNotContain(rootText, "private static FlashbackSessionContext CreateSessionContext");
        AssertDoesNotContain(rootText, "private static byte[] GetBuffer");
        AssertContains(docsText, "FlashbackEncoderSink.SessionContext.cs");
        AssertContains(docsText, "FlashbackEncoderSink.PacketBuffers.cs");
        AssertContains(docsText, "FlashbackEncoderSink.PacketTypes.cs");

        return Task.CompletedTask;
    }
}
