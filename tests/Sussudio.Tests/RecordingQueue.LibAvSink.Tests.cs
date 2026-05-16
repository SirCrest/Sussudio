using System;
using System.Threading.Tasks;

// Tests for recording sink queue limits, drops, and latency accounting.
static partial class Program
{
    private static Task LibAvRecordingSink_StopValidatesFinalOutput()
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

    private static Task RecordingVideoTryEnqueuePaths_DoNotBlockCaptureCallbacks()
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

    private static Task LibAvRecordingSink_NormalDrainLoopInterleavesAudioWithBoundedVideoBatches()
    {
        var libAvSource = ReadLibAvRecordingSinkSource();

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
            libAvSource,
            "private void EncodingLoop(CancellationToken cancellationToken)",
            "    private bool DrainVideoPackets");
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

    private static Task LibAvRecordingSink_EncodingLoopLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.cs")
            .Replace("\r\n", "\n");
        var encodingLoopText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.EncodingLoop.cs")
            .Replace("\r\n", "\n");

        AssertContains(encodingLoopText, "private void EncodingLoop(CancellationToken cancellationToken)");
        AssertContains(encodingLoopText, "private bool DrainVideoPackets(ChannelReader<VideoFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(encodingLoopText, "private bool DrainGpuPackets(ChannelReader<GpuFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(encodingLoopText, "private unsafe bool DrainCudaPackets(ChannelReader<CudaFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(encodingLoopText, "private bool DrainAudioPackets(ChannelReader<AudioSamplePacket> reader)");
        AssertContains(encodingLoopText, "private bool DrainMicrophonePackets(ChannelReader<AudioSamplePacket> reader)");
        AssertDoesNotContain(rootText, "private void EncodingLoop(CancellationToken cancellationToken)");
        AssertDoesNotContain(rootText, "private bool DrainVideoPackets(ChannelReader<VideoFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertDoesNotContain(rootText, "private unsafe bool DrainCudaPackets(ChannelReader<CudaFramePacket> reader, int maxPackets = int.MaxValue)");

        return Task.CompletedTask;
    }

    private static Task LibAvRecordingSink_AudioQueuesLiveInFocusedPartial()
    {
        var queueText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.Queues.cs")
            .Replace("\r\n", "\n");
        var audioQueueText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.AudioQueues.cs")
            .Replace("\r\n", "\n");
        var queueCleanupText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.QueueCleanup.cs")
            .Replace("\r\n", "\n");

        AssertContains(audioQueueText, "public Task WriteAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)");
        AssertContains(audioQueueText, "public Task WriteMicrophoneAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)");
        AssertContains(audioQueueText, "private bool TryEnqueueAudioPacket(Channel<AudioSamplePacket> queue, AudioSamplePacket packet)");
        AssertContains(audioQueueText, "private bool TryEnqueueMicrophonePacket(Channel<AudioSamplePacket> queue, AudioSamplePacket packet)");
        AssertContains(audioQueueText, "private static void ReturnRemainingBuffers(Channel<AudioSamplePacket>? queue, ref int queueDepth)");
        AssertContains(audioQueueText, "private readonly record struct AudioSamplePacket(byte[] Buffer, int Length);");
        AssertContains(queueCleanupText, "private void ReturnRemainingVideoBuffers(Channel<VideoFramePacket>? queue)");
        AssertContains(queueCleanupText, "private static void ReturnRemainingGpuBuffers(Channel<GpuFramePacket>? queue, ref int queueDepth)");
        AssertContains(queueCleanupText, "private static unsafe void ReturnRemainingCudaFrames(Channel<CudaFramePacket>? queue, ref int queueDepth)");
        AssertContains(queueCleanupText, "private static void ReturnVideoPacket(VideoFramePacket packet)");
        AssertDoesNotContain(queueText, "public Task WriteAudioAsync(ReadOnlyMemory<byte> samples, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(queueText, "private bool TryEnqueueAudioPacket(Channel<AudioSamplePacket> queue, AudioSamplePacket packet)");
        AssertDoesNotContain(queueText, "private static void ReturnRemainingBuffers(Channel<AudioSamplePacket>? queue, ref int queueDepth)");
        AssertDoesNotContain(queueText, "private void ReturnRemainingVideoBuffers(Channel<VideoFramePacket>? queue)");
        AssertDoesNotContain(queueText, "private static void ReturnVideoPacket(VideoFramePacket packet)");

        return Task.CompletedTask;
    }

    private static Task LibAvRecordingSink_LifecycleHelpersLiveInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.cs")
            .Replace("\r\n", "\n");
        var diagnosticsText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.Diagnostics.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.Startup.cs")
            .Replace("\r\n", "\n");
        var stopText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.StopLifecycle.cs")
            .Replace("\r\n", "\n");
        var lifetimeText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.Lifetime.cs")
            .Replace("\r\n", "\n");
        var optionsText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.Options.cs")
            .Replace("\r\n", "\n");
        var outputValidationText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.OutputValidation.cs")
            .Replace("\r\n", "\n");

        AssertContains(diagnosticsText, "public long DroppedVideoFrames =>");
        AssertContains(diagnosticsText, "public bool TryGetEncoderAvSyncDrift(out double driftMs, out long correctionSamples)");
        AssertContains(startupText, "public Task StartAsync(RecordingContext context, CancellationToken cancellationToken = default)");
        AssertContains(startupText, "LibAvEncoder.InitializeFFmpeg(requireNativeRuntime: true);");
        AssertContains(startupText, "_encodingTask = Task.Factory.StartNew(");
        AssertContains(startupText, "TaskCreationOptions.LongRunning");
        AssertContains(startupText, "LIBAV_SINK_START output='{context.FinalOutputPath}'");
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
        AssertContains(stopText, "if (context?.HdrPipelineActive == true)");
        AssertContains(stopText, "LIBAV_SINK_STOP output='{outputPath}' bytes={outputBytes}");
        AssertContains(lifetimeText, "public async ValueTask DisposeAsync()");
        AssertContains(lifetimeText, "private void ScheduleDeferredDisposeCleanup(Task encodingTask)");
        AssertContains(optionsText, "private LibAvEncoderOptions CreateOptions(RecordingContext context)");
        AssertContains(optionsText, "SplitEncodeModeParser.ToWireString(context.Settings.SplitEncodeMode)");
        AssertContains(outputValidationText, "private static bool TryValidateStoppedOutputFile(string outputPath, out long outputBytes, out string failureMessage)");
        AssertContains(rootText, "private void CompleteWriter<TPacket>(Channel<TPacket>? channel)");
        AssertContains(rootText, "SignalWork(\"complete_writer\");");
        AssertDoesNotContain(rootText, "public long DroppedVideoFrames =>");
        AssertDoesNotContain(rootText, "public Task StartAsync(RecordingContext context, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(rootText, "public Task<FinalizeResult> StopAsync(CancellationToken cancellationToken = default)");
        AssertDoesNotContain(rootText, "internal Task<FinalizeResult> StopAsync(bool emergency, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(rootText, "private async Task<FinalizeResult> StopCoreAsync(bool emergency, CancellationToken cancellationToken)");
        AssertDoesNotContain(rootText, "public async ValueTask DisposeAsync()");
        AssertDoesNotContain(rootText, "private LibAvEncoderOptions CreateOptions(RecordingContext context)");
        AssertDoesNotContain(rootText, "private static bool TryValidateStoppedOutputFile(string outputPath, out long outputBytes, out string failureMessage)");

        return Task.CompletedTask;
    }

}
