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
        var queueText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.Queues.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.Startup.cs")
            .Replace("\r\n", "\n");
        var stopText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.StopLifecycle.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "public long DroppedVideoFrames =>");
        AssertContains(rootText, "public bool TryGetEncoderAvSyncDrift(out double driftMs, out long correctionSamples)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.Diagnostics.cs")),
            "LibAvRecordingSink diagnostics surface lives with the sink root state");
        AssertContains(startupText, "public Task StartAsync(RecordingContext context, CancellationToken cancellationToken = default)");
        AssertContains(startupText, "LibAvEncoder.InitializeFFmpeg(requireNativeRuntime: true);");
        AssertContains(startupText, "InitializeVideoSessionQueues();");
        AssertContains(startupText, "ResetVideoSessionState(context);");
        AssertContains(startupText, "_encodingTask = Task.Factory.StartNew(");
        AssertContains(startupText, "TaskCreationOptions.LongRunning");
        AssertContains(startupText, "LIBAV_SINK_START output='{context.FinalOutputPath}'");
        AssertContains(startupText, "private LibAvEncoderOptions CreateOptions(RecordingContext context)");
        AssertContains(startupText, "SplitEncodeModeParser.ToWireString(context.Settings.SplitEncodeMode)");
        AssertContains(startupText, "private void InitializeVideoSessionQueues()");
        AssertContains(startupText, "_cudaQueue = Channel.CreateBounded<CudaFramePacket>");
        AssertContains(startupText, "_gpuQueue = Channel.CreateBounded<GpuFramePacket>");
        AssertContains(startupText, "_videoQueue = Channel.CreateBounded<VideoFramePacket>");
        AssertContains(startupText, "LIBAV_SINK_CUDA_QUEUE_INIT capacity=");
        AssertContains(startupText, "LIBAV_SINK_GPU_QUEUE_INIT capacity=");
        AssertContains(startupText, "private void ResetVideoSessionState(RecordingContext context)");
        AssertContains(startupText, "_width = checked((int)context.EffectiveWidth);");
        AssertContains(startupText, "_height = checked((int)context.EffectiveHeight);");
        AssertContains(startupText, "private void ResetVideoSessionMetrics()");
        AssertContains(startupText, "Interlocked.Exchange(ref _videoFramesEnqueued, 0);");
        AssertContains(startupText, "Interlocked.Exchange(ref _gpuFramesEnqueued, 0);");
        AssertContains(startupText, "Interlocked.Exchange(ref _cudaFramesEnqueued, 0);");
        AssertContains(startupText, "Interlocked.Exchange(ref _lastVideoEnqueueTick, 0);");
        AssertContains(startupText, "ResetVideoDiagnostics();");
        AssertContains(startupText, "private void ResetVideoDiagnostics() => _videoLatencyTracker.ResetAll();");
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
        AssertDoesNotContain(rootText, "public Task StartAsync(RecordingContext context, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(queueText, "private void ResetVideoDiagnostics() => _videoLatencyTracker.ResetAll();");
        AssertDoesNotContain(rootText, "public Task<FinalizeResult> StopAsync(CancellationToken cancellationToken = default)");
        AssertDoesNotContain(rootText, "internal Task<FinalizeResult> StopAsync(bool emergency, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(rootText, "private async Task<FinalizeResult> StopCoreAsync(bool emergency, CancellationToken cancellationToken)");
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.VideoSession.cs")),
            "LibAvRecordingSink video session startup helpers folded into LibAvRecordingSink.Startup.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvRecordingSink.Lifetime.cs")),
            "LibAvRecordingSink dispose/deferred cleanup lives with the sink root");

        return Task.CompletedTask;
    }
}
