using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task RecordingVideoTryEnqueuePaths_DoNotBlockCaptureCallbacks()
    {
        var libAvSource = ReadLibAvRecordingSinkSource();
        var flashbackSource = ReadFlashbackEncoderSinkSource();

        var libAvVideoEnqueue = ExtractSourceBlock(
            libAvSource,
            "private VideoEnqueueResult TryEnqueueVideoPacket",
            "private VideoEnqueueResult TryEnqueueGpuPacket");
        var libAvGpuEnqueue = ExtractSourceBlock(
            libAvSource,
            "private VideoEnqueueResult TryEnqueueGpuPacket",
            "private unsafe VideoEnqueueResult TryEnqueueCudaPacket");
        var libAvCudaEnqueue = ExtractSourceBlock(
            libAvSource,
            "private unsafe VideoEnqueueResult TryEnqueueCudaPacket",
            "private bool TryWriteVideoPacket");
        var flashbackVideoEnqueue = ExtractSourceBlock(
            flashbackSource,
            "private VideoEnqueueResult TryEnqueueVideoPacket",
            "private VideoEnqueueResult TryEnqueueGpuPacket");
        var flashbackGpuEnqueue = ExtractSourceBlock(
            flashbackSource,
            "private VideoEnqueueResult TryEnqueueGpuPacket",
            "private string? GetVideoEnqueueRejectReason");

        AssertDoesNotContain(libAvVideoEnqueue, "while (true)");
        AssertDoesNotContain(libAvGpuEnqueue, "while (true)");
        AssertDoesNotContain(libAvCudaEnqueue, "while (true)");
        AssertDoesNotContain(flashbackVideoEnqueue, "while (true)");
        AssertDoesNotContain(flashbackGpuEnqueue, "while (true)");
        AssertDoesNotContain(libAvSource, "Thread.Sleep(");
        AssertDoesNotContain(libAvSource, "backpressure_retry");
        AssertDoesNotContain(flashbackSource, "WaitForBackpressureRetryCancellation");
        AssertDoesNotContain(flashbackVideoEnqueue, "TimeSpan.FromMilliseconds(1)");
        AssertDoesNotContain(flashbackGpuEnqueue, "TimeSpan.FromMilliseconds(1)");

        AssertContains(libAvVideoEnqueue, "FailEncoding(overloadFailure);");
        AssertContains(libAvVideoEnqueue, "ReturnVideoPacket(packet);");
        AssertContains(libAvGpuEnqueue, "FailEncoding(new InvalidOperationException(");
        AssertContains(libAvGpuEnqueue, "Marshal.Release(packet.Texture);");
        AssertContains(libAvCudaEnqueue, "FailEncoding(new InvalidOperationException(");
        AssertContains(libAvCudaEnqueue, "ffmpeg.av_frame_free(&overloadedFrame);");
        AssertContains(flashbackVideoEnqueue, "TrackVideoQueueRejected(\"queue_full\");");
        AssertContains(flashbackGpuEnqueue, "TrackGpuQueueRejected(\"queue_full\");");

        return Task.CompletedTask;
    }

    internal static Task LibAvRecordingSink_QueueingOwnsProducerAdmissionAndCleanup()
    {
        var queueingText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.Queueing.cs")
            .Replace("\r\n", "\n");
        var queueText = queueingText;
        var videoSubmissionText = queueingText;

        AssertContains(queueText, "public Task WriteAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)");
        AssertContains(queueText, "public Task WriteMicrophoneAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)");
        AssertContains(queueText, "private bool TryEnqueueAudioPacket(Channel<AudioSamplePacket> queue, AudioSamplePacket packet)");
        AssertContains(queueText, "private bool TryEnqueueMicrophonePacket(Channel<AudioSamplePacket> queue, AudioSamplePacket packet)");
        AssertContains(queueText, "private static void ReturnRemainingBuffers(Channel<AudioSamplePacket>? queue, ref int queueDepth)");
        AssertContains(queueText, "private readonly record struct AudioSamplePacket(byte[] Buffer, int Length);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.AudioQueues.cs")),
            "LibAvRecordingSink audio queue surface folded into shared queue owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.QueueCleanup.cs")),
            "LibAvRecordingSink queue cleanup lives with video queue submission and packet ownership");
        AssertContains(videoSubmissionText, "private void ReturnRemainingVideoBuffers(Channel<VideoFramePacket>? queue)");
        AssertContains(videoSubmissionText, "private static void ReturnRemainingGpuBuffers(Channel<GpuFramePacket>? queue, ref int queueDepth)");
        AssertContains(videoSubmissionText, "private static unsafe void ReturnRemainingCudaFrames(Channel<CudaFramePacket>? queue, ref int queueDepth)");
        AssertContains(videoSubmissionText, "private static void ReturnVideoPacket(VideoFramePacket packet)");
        AssertContains(queueText, "public bool TryEnqueueGpuVideoFrame(IntPtr d3d11Texture2D, int subresourceIndex)");
        AssertContains(queueText, "public unsafe void EnqueueCudaVideoFrame(AVFrame* cudaFrame)");
        AssertContains(queueText, "public bool TryEnqueueRawVideoFrame(ReadOnlySpan<byte> data, int expectedSize)");
        AssertContains(queueText, "bool IRawVideoFrameLeaseTryEncoder.TryEnqueueRawVideoFrame(PooledVideoFrameLease frame)");
        AssertContains(videoSubmissionText, "private VideoEnqueueResult TryEnqueueVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertContains(videoSubmissionText, "private VideoEnqueueResult TryEnqueueGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)");
        AssertContains(videoSubmissionText, "private unsafe VideoEnqueueResult TryEnqueueCudaPacket(Channel<CudaFramePacket> queue, CudaFramePacket packet)");
        AssertContains(videoSubmissionText, "private bool TryWriteVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertContains(videoSubmissionText, "private bool TryWriteGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)");
        AssertContains(videoSubmissionText, "private bool TryWriteCudaPacket(Channel<CudaFramePacket> queue, CudaFramePacket packet)");
        AssertContains(videoSubmissionText, "private readonly record struct VideoFramePacket");
        AssertContains(videoSubmissionText, "private enum VideoEnqueueResult");
        AssertContains(videoSubmissionText, "private readonly record struct GpuFramePacket");
        AssertContains(videoSubmissionText, "private readonly record struct CudaFramePacket");
        AssertContains(videoSubmissionText, "internal sealed class VideoQueueLatencyTracker");
        AssertContains(videoSubmissionText, "public void TrackEnqueueUnderLock(long enqueueTick)");
        AssertContains(videoSubmissionText, "public (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) GetMetrics()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.Queues.cs")),
            "LibAvRecordingSink.Queues.cs folded into LibAvRecordingSink.Queueing.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.VideoQueueSubmission.cs")),
            "LibAvRecordingSink.VideoQueueSubmission.cs folded into LibAvRecordingSink.Queueing.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "VideoQueueLatencyTracker.cs")),
            "shared video queue latency tracker folded into the recording queueing owner");

        return Task.CompletedTask;
    }

    internal static Task LibAvRecordingSink_StopValidatesFinalOutput()
    {
        var libAvSource = ReadLibAvRecordingSinkSource();

        AssertContains(libAvSource, "private static bool TryValidateStoppedOutputFile(string outputPath, out long outputBytes, out string failureMessage)");
        AssertContains(libAvSource, "if (!TryValidateStoppedOutputFile(outputPath, out var outputBytes, out var outputFailure))\n        {\n            Logger.Log($\"LIBAV_SINK_STOP_OUTPUT_INVALID output='{outputPath}' reason='{outputFailure}'\");\n            return FinalizeResult.Failure(outputPath, $\"Stopped (output file invalid: {outputFailure})\");\n        }");
        AssertOccursBefore(libAvSource, "TryValidateStoppedOutputFile(outputPath, out var outputBytes, out var outputFailure)", "if (context?.HdrPipelineActive == true)");
        AssertContains(libAvSource, "failureMessage = \"output file is missing\";");
        AssertContains(libAvSource, "failureMessage = \"output file is empty\";");
        AssertContains(libAvSource, "LIBAV_SINK_STOP_OUTPUT_VALIDATE_WARN");
        AssertContains(libAvSource, "LIBAV_SINK_STOP output='{outputPath}' bytes={outputBytes}");

        return Task.CompletedTask;
    }

    internal static Task LibAvRecordingSink_NormalDrainLoopInterleavesAudioWithBoundedVideoBatches()
    {
        var libAvSource = ReadLibAvRecordingSinkSource();
        var encodingLoopText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.cs")
            .Replace("\r\n", "\n");

        AssertContains(libAvSource, "private const int VideoDrainBatchLimit = 24;");
        AssertContains(libAvSource, "private const int AudioDrainBatchLimit = 128;");
        AssertContains(libAvSource, "private const int GpuDrainBatchLimit = 16;");
        AssertContains(libAvSource, "private const int CudaDrainBatchLimit = 16;");
        AssertContains(libAvSource, "DrainCudaPackets(cudaQueue.Reader, CudaDrainBatchLimit)");
        AssertContains(libAvSource, "DrainGpuPackets(gpuQueue.Reader, GpuDrainBatchLimit)");
        AssertContains(libAvSource, "DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit)");
        AssertContains(libAvSource, "private bool DrainVideoPackets(ChannelReader<VideoFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(libAvSource, "private bool DrainGpuPackets(ChannelReader<GpuFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(libAvSource, "private unsafe bool DrainCudaPackets(ChannelReader<CudaFramePacket> reader, int maxPackets = int.MaxValue)");

        var loopBlock = ExtractSourceBlock(
            encodingLoopText,
            "private void EncodingLoop(CancellationToken cancellationToken)",
            "            _encoder.FlushAndClose();");
        AssertOccursBefore(loopBlock, "DrainAudioPackets(audioQueue.Reader)", "DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit)");
        AssertOccursBefore(loopBlock, "DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit)", "// Audio again catches samples");

        var secondAudioDrainBlock = ExtractSourceBlock(
            loopBlock,
            "// Audio again catches samples",
            "if (videoQueue.Reader.Completion.IsCompleted");
        AssertContains(secondAudioDrainBlock, "DrainAudioPackets(audioQueue.Reader)");
        AssertContains(secondAudioDrainBlock, "DrainMicrophonePackets(microphoneQueue.Reader)");

        return Task.CompletedTask;
    }

    internal static Task LibAvRecordingSink_EncodingLoopAndPacketDrainsLiveWithSinkRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "private void EncodingLoop(CancellationToken cancellationToken)");
        AssertContains(rootText, "DrainAudioPackets(audioQueue.Reader)");
        AssertContains(rootText, "DrainCudaPackets(cudaQueue.Reader, CudaDrainBatchLimit)");
        AssertContains(rootText, "DrainGpuPackets(gpuQueue.Reader, GpuDrainBatchLimit)");
        AssertContains(rootText, "DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.EncodingLoop.cs")),
            "LibAvRecordingSink encoding loop stays folded into the sink root");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.PacketDrain.cs")),
            "LibAvRecordingSink packet drains stay folded into the sink root with the encoding loop");
        AssertContains(rootText, "private bool DrainVideoPackets(ChannelReader<VideoFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(rootText, "private bool DrainGpuPackets(ChannelReader<GpuFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(rootText, "private unsafe bool DrainCudaPackets(ChannelReader<CudaFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(rootText, "private bool DrainAudioPackets(ChannelReader<AudioSamplePacket> reader)");
        AssertContains(rootText, "private bool DrainMicrophonePackets(ChannelReader<AudioSamplePacket> reader)");
        AssertContains(rootText, "Marshal.Release(packet.Texture);");
        AssertContains(rootText, "ffmpeg.av_frame_free(&frame);");
        AssertContains(rootText, "ReturnVideoPacket(packet);");
        AssertContains(rootText, "ReturnBuffer(packet.Buffer);");

        return Task.CompletedTask;
    }

    internal static Task LibAvRecordingSink_LifecycleHelpersLiveWithTheirOwners()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.cs")
            .Replace("\r\n", "\n");
        var queueText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.Queueing.cs")
            .Replace("\r\n", "\n");
        var stopText = rootText;

        AssertContains(rootText, "public long DroppedVideoFrames =>");
        AssertContains(rootText, "public bool TryGetEncoderAvSyncDrift(out double driftMs, out long correctionSamples)");
        AssertContains(rootText, "public Task StartAsync(RecordingContext context, CancellationToken cancellationToken = default)");
        AssertContains(rootText, "LibAvEncoder.InitializeFFmpeg(requireNativeRuntime: true);");
        AssertContains(rootText, "InitializeVideoSessionQueues();");
        AssertContains(rootText, "ResetVideoSessionState(context);");
        AssertContains(rootText, "_encodingTask = Task.Factory.StartNew(");
        AssertContains(rootText, "TaskCreationOptions.LongRunning");
        AssertContains(rootText, "LIBAV_SINK_START output='{context.FinalOutputPath}'");
        AssertContains(rootText, "private LibAvEncoderOptions CreateOptions(RecordingContext context)");
        AssertContains(rootText, "SplitEncodeModeParser.ToWireString(context.Settings.SplitEncodeMode)");
        AssertContains(rootText, "private void InitializeVideoSessionQueues()");
        AssertContains(rootText, "_cudaQueue = Channel.CreateBounded<CudaFramePacket>");
        AssertContains(rootText, "_gpuQueue = Channel.CreateBounded<GpuFramePacket>");
        AssertContains(rootText, "_videoQueue = Channel.CreateBounded<VideoFramePacket>");
        AssertContains(rootText, "LIBAV_SINK_CUDA_QUEUE_INIT capacity=");
        AssertContains(rootText, "LIBAV_SINK_GPU_QUEUE_INIT capacity=");
        AssertContains(rootText, "private void ResetVideoSessionState(RecordingContext context)");
        AssertContains(rootText, "_width = checked((int)context.EffectiveWidth);");
        AssertContains(rootText, "_height = checked((int)context.EffectiveHeight);");
        AssertContains(rootText, "private void ResetVideoSessionMetrics()");
        AssertContains(rootText, "Interlocked.Exchange(ref _videoFramesEnqueued, 0);");
        AssertContains(rootText, "Interlocked.Exchange(ref _gpuFramesEnqueued, 0);");
        AssertContains(rootText, "Interlocked.Exchange(ref _cudaFramesEnqueued, 0);");
        AssertContains(rootText, "Interlocked.Exchange(ref _lastVideoEnqueueTick, 0);");
        AssertContains(rootText, "ResetVideoDiagnostics();");
        AssertContains(rootText, "private void ResetVideoDiagnostics() => _videoLatencyTracker.ResetAll();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.Diagnostics.cs")),
            "LibAvRecordingSink diagnostics surface lives with the sink root state");
        AssertContains(stopText, "public Task<FinalizeResult> StopAsync(CancellationToken cancellationToken = default)");
        AssertContains(stopText, "=> StopCoreAsync(emergency: false, cancellationToken);");
        AssertContains(stopText, "internal Task<FinalizeResult> StopAsync(bool emergency, CancellationToken cancellationToken = default)");
        AssertContains(stopText, "=> StopCoreAsync(emergency, cancellationToken);");
        AssertContains(stopText, "private async Task<FinalizeResult> StopCoreAsync(bool emergency, CancellationToken cancellationToken)");
        AssertContains(stopText, "var drainTimeoutMs = emergency ? EmergencyStopTimeoutMs : StopTimeoutMs;");
        AssertContains(stopText, "_cts?.Cancel();");
        AssertContains(stopText, "LIBAV_SINK_STOP_DRAIN_FLUSH_SKIPPED reason=encoder_task_still_running");
        AssertContains(stopText, "return FinalizeResult.Failure(outputPath, \"Stopped (libav encode drain timed out; emergency flush attempted)\");");
        AssertContains(stopText, "TryValidateStoppedOutputFile(outputPath, out var outputBytes, out var outputFailure)");
        AssertContains(stopText, "private static bool TryValidateStoppedOutputFile(string outputPath, out long outputBytes, out string failureMessage)");
        AssertContains(stopText, "if (context?.HdrPipelineActive == true)");
        AssertContains(stopText, "LIBAV_SINK_STOP output='{outputPath}' bytes={outputBytes}");
        AssertContains(rootText, "public async ValueTask DisposeAsync()");
        AssertContains(rootText, "private void ScheduleDeferredDisposeCleanup(Task encodingTask)");
        AssertContains(rootText, "private void CompleteWriter<TPacket>(Channel<TPacket>? channel)");
        AssertContains(rootText, "SignalWork(\"complete_writer\");");
        AssertDoesNotContain(queueText, "private void ResetVideoDiagnostics() => _videoLatencyTracker.ResetAll();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.StopLifecycle.cs")),
            "LibAvRecordingSink stop/finalize lifecycle lives with the sink root lifecycle");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.OutputValidation.cs")),
            "LibAvRecordingSink.OutputValidation.cs folded into the sink root lifecycle");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.Options.cs")),
            "LibAvRecordingSink.Options.cs folded into the sink startup owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.VideoSession.cs")),
            "LibAvRecordingSink video session startup helpers folded into the sink root");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.Startup.cs")),
            "LibAvRecordingSink startup shell folded into the sink root with encoding-loop lifecycle");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.Lifetime.cs")),
            "LibAvRecordingSink dispose/deferred cleanup lives with the sink root");

        return Task.CompletedTask;
    }
}
