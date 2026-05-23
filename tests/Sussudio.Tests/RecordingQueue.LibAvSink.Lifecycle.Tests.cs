using System.IO;
using System.Threading.Tasks;

static partial class Program
{
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
        var encodingLoopText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.EncodingLoop.cs")
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

    internal static Task LibAvRecordingSink_EncodingLoopAndPacketDrainsLiveInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.cs")
            .Replace("\r\n", "\n");
        var encodingLoopText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.EncodingLoop.cs")
            .Replace("\r\n", "\n");
        var packetDrainText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.PacketDrain.cs")
            .Replace("\r\n", "\n");

        AssertContains(encodingLoopText, "private void EncodingLoop(CancellationToken cancellationToken)");
        AssertContains(encodingLoopText, "DrainAudioPackets(audioQueue.Reader)");
        AssertContains(encodingLoopText, "DrainCudaPackets(cudaQueue.Reader, CudaDrainBatchLimit)");
        AssertContains(encodingLoopText, "DrainGpuPackets(gpuQueue.Reader, GpuDrainBatchLimit)");
        AssertContains(encodingLoopText, "DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit)");
        AssertContains(packetDrainText, "private bool DrainVideoPackets(ChannelReader<VideoFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(packetDrainText, "private bool DrainGpuPackets(ChannelReader<GpuFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(packetDrainText, "private unsafe bool DrainCudaPackets(ChannelReader<CudaFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(packetDrainText, "private bool DrainAudioPackets(ChannelReader<AudioSamplePacket> reader)");
        AssertContains(packetDrainText, "private bool DrainMicrophonePackets(ChannelReader<AudioSamplePacket> reader)");
        AssertContains(packetDrainText, "Marshal.Release(packet.Texture);");
        AssertContains(packetDrainText, "ffmpeg.av_frame_free(&frame);");
        AssertContains(packetDrainText, "ReturnVideoPacket(packet);");
        AssertContains(packetDrainText, "ReturnBuffer(packet.Buffer);");
        AssertDoesNotContain(encodingLoopText, "private bool DrainVideoPackets(");
        AssertDoesNotContain(encodingLoopText, "private bool DrainGpuPackets(");
        AssertDoesNotContain(encodingLoopText, "private unsafe bool DrainCudaPackets(");
        AssertDoesNotContain(encodingLoopText, "private bool DrainAudioPackets(");
        AssertDoesNotContain(rootText, "private void EncodingLoop(CancellationToken cancellationToken)");
        AssertDoesNotContain(rootText, "private bool DrainVideoPackets(ChannelReader<VideoFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertDoesNotContain(rootText, "private unsafe bool DrainCudaPackets(ChannelReader<CudaFramePacket> reader, int maxPackets = int.MaxValue)");

        return Task.CompletedTask;
    }

    internal static Task LibAvRecordingSink_LifecycleHelpersLiveInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.cs")
            .Replace("\r\n", "\n");
        var diagnosticsText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.Diagnostics.cs")
            .Replace("\r\n", "\n");
        var queueText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.Queues.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.Startup.cs")
            .Replace("\r\n", "\n");
        var videoSessionText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.VideoSession.cs")
            .Replace("\r\n", "\n");
        var stopText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.StopLifecycle.cs")
            .Replace("\r\n", "\n");
        var lifetimeText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.Lifetime.cs")
            .Replace("\r\n", "\n");

        AssertContains(diagnosticsText, "public long DroppedVideoFrames =>");
        AssertContains(diagnosticsText, "public bool TryGetEncoderAvSyncDrift(out double driftMs, out long correctionSamples)");
        AssertContains(startupText, "public Task StartAsync(RecordingContext context, CancellationToken cancellationToken = default)");
        AssertContains(startupText, "LibAvEncoder.InitializeFFmpeg(requireNativeRuntime: true);");
        AssertContains(startupText, "InitializeVideoSessionQueues();");
        AssertContains(startupText, "ResetVideoSessionState(context);");
        AssertContains(startupText, "_encodingTask = Task.Factory.StartNew(");
        AssertContains(startupText, "TaskCreationOptions.LongRunning");
        AssertContains(startupText, "LIBAV_SINK_START output='{context.FinalOutputPath}'");
        AssertContains(startupText, "private LibAvEncoderOptions CreateOptions(RecordingContext context)");
        AssertContains(startupText, "SplitEncodeModeParser.ToWireString(context.Settings.SplitEncodeMode)");
        AssertContains(videoSessionText, "private void InitializeVideoSessionQueues()");
        AssertContains(videoSessionText, "_cudaQueue = Channel.CreateBounded<CudaFramePacket>");
        AssertContains(videoSessionText, "_gpuQueue = Channel.CreateBounded<GpuFramePacket>");
        AssertContains(videoSessionText, "_videoQueue = Channel.CreateBounded<VideoFramePacket>");
        AssertContains(videoSessionText, "LIBAV_SINK_CUDA_QUEUE_INIT capacity=");
        AssertContains(videoSessionText, "LIBAV_SINK_GPU_QUEUE_INIT capacity=");
        AssertContains(videoSessionText, "private void ResetVideoSessionState(RecordingContext context)");
        AssertContains(videoSessionText, "_width = checked((int)context.EffectiveWidth);");
        AssertContains(videoSessionText, "_height = checked((int)context.EffectiveHeight);");
        AssertContains(videoSessionText, "private void ResetVideoSessionMetrics()");
        AssertContains(videoSessionText, "Interlocked.Exchange(ref _videoFramesEnqueued, 0);");
        AssertContains(videoSessionText, "Interlocked.Exchange(ref _gpuFramesEnqueued, 0);");
        AssertContains(videoSessionText, "Interlocked.Exchange(ref _cudaFramesEnqueued, 0);");
        AssertContains(videoSessionText, "Interlocked.Exchange(ref _lastVideoEnqueueTick, 0);");
        AssertContains(videoSessionText, "ResetVideoDiagnostics();");
        AssertContains(videoSessionText, "private void ResetVideoDiagnostics() => _videoLatencyTracker.ResetAll();");
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
        AssertContains(lifetimeText, "public async ValueTask DisposeAsync()");
        AssertContains(lifetimeText, "private void ScheduleDeferredDisposeCleanup(Task encodingTask)");
        AssertContains(rootText, "private void CompleteWriter<TPacket>(Channel<TPacket>? channel)");
        AssertContains(rootText, "SignalWork(\"complete_writer\");");
        AssertDoesNotContain(rootText, "public long DroppedVideoFrames =>");
        AssertDoesNotContain(rootText, "public Task StartAsync(RecordingContext context, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(startupText, "Channel.CreateBounded<VideoFramePacket>");
        AssertDoesNotContain(startupText, "Channel.CreateBounded<GpuFramePacket>");
        AssertDoesNotContain(startupText, "Channel.CreateBounded<CudaFramePacket>");
        AssertDoesNotContain(startupText, "Interlocked.Exchange(ref _videoFramesEnqueued, 0);");
        AssertDoesNotContain(startupText, "Interlocked.Exchange(ref _gpuFramesEnqueued, 0);");
        AssertDoesNotContain(startupText, "Interlocked.Exchange(ref _cudaFramesEnqueued, 0);");
        AssertDoesNotContain(queueText, "private void ResetVideoDiagnostics() => _videoLatencyTracker.ResetAll();");
        AssertDoesNotContain(rootText, "public Task<FinalizeResult> StopAsync(CancellationToken cancellationToken = default)");
        AssertDoesNotContain(rootText, "internal Task<FinalizeResult> StopAsync(bool emergency, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(rootText, "private async Task<FinalizeResult> StopCoreAsync(bool emergency, CancellationToken cancellationToken)");
        AssertDoesNotContain(rootText, "public async ValueTask DisposeAsync()");
        AssertDoesNotContain(rootText, "private LibAvEncoderOptions CreateOptions(RecordingContext context)");
        AssertDoesNotContain(rootText, "private static bool TryValidateStoppedOutputFile(string outputPath, out long outputBytes, out string failureMessage)");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.OutputValidation.cs")),
            "LibAvRecordingSink.OutputValidation.cs folded into LibAvRecordingSink.StopLifecycle.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.Options.cs")),
            "LibAvRecordingSink.Options.cs folded into LibAvRecordingSink.Startup.cs");

        return Task.CompletedTask;
    }
}
