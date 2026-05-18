using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackEncoderSink_ForceRotateDrainingRejectsVideoAndGpuEnqueues()
    {
        var sinkType = RequireType("Sussudio.Services.Flashback.FlashbackEncoderSink");
        var optionsType = RequireType("Sussudio.Models.FlashbackBufferOptions");
        var ctor = sinkType.GetConstructor(new[] { optionsType })
            ?? throw new InvalidOperationException("FlashbackEncoderSink(FlashbackBufferOptions) constructor not found.");
        var sink = ctor.Invoke(new object?[] { null })!;
        var rejectReason = sinkType.GetMethod("GetVideoEnqueueRejectReason", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetVideoEnqueueRejectReason not found.");

        try
        {
            SetPrivateField(sink, "_started", true);
            SetPrivateField(sink, "_forceRotateDraining", true);

            AssertEqual("force_rotate_draining", rejectReason.Invoke(sink, new object[] { false }) as string, "Force-rotate draining rejects CPU video");
            AssertEqual("force_rotate_draining", rejectReason.Invoke(sink, new object[] { true }) as string, "Force-rotate draining rejects GPU video");

            SetPrivateField(sink, "_forceRotateDraining", false);
            AssertEqual<string?>(null, rejectReason.Invoke(sink, new object[] { false }) as string, "CPU video accepted after force-rotate drain clears");
            AssertEqual<string?>(null, rejectReason.Invoke(sink, new object[] { true }) as string, "GPU video accepted after force-rotate drain clears");
        }
        finally
        {
            (sink as IDisposable)?.Dispose();
        }

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_DisposeResetsGpuQueueDepth()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

        AssertContains(sourceText, "ReturnRemainingGpuBuffers(_gpuQueue, ref _gpuQueueDepth);");
        AssertContains(sourceText, "private static void ReturnRemainingGpuBuffers(Channel<GpuFramePacket>? queue, ref int queueDepth)");
        AssertContains(sourceText, "Interlocked.Exchange(ref queueDepth, 0);");
        AssertContains(sourceText, "var timeoutFailure = new TimeoutException(\"Flashback encode drain timed out while stopping.\");");
        AssertContains(sourceText, "_encodingFailure ??= timeoutFailure;");
        AssertContains(sourceText, "_encodingFailure ??= ex;");
        AssertContains(sourceText, "CancelEncodingCts(\"dispose\");");
        AssertContains(sourceText, "CancelEncodingCts(\"stop_timeout\");");
        AssertContains(sourceText, "private void CancelEncodingCts(string operation)");
        AssertContains(sourceText, "FLASHBACK_SINK_CANCEL_WARN");
        AssertContains(sourceText, "DisposeCtsBestEffort(_cts, \"start_fail\");");
        AssertContains(sourceText, "DisposeCtsBestEffort(_cts, \"finalize_dispose\");");
        AssertContains(sourceText, "DisposeWorkAvailableBestEffort(\"finalize_dispose\");");
        AssertContains(sourceText, "DisposeEncoderBestEffort(\"start_fail\");");
        AssertContains(sourceText, "DisposeEncoderBestEffort(\"finalize_dispose\");");
        AssertContains(sourceText, "DisposeEncoderBestEffort(\"encoding_loop_fatal\");");
        AssertContains(sourceText, "FLASHBACK_SINK_CTS_DISPOSE_WARN");
        AssertContains(sourceText, "FLASHBACK_SINK_WORK_SIGNAL_DISPOSE_WARN");
        AssertContains(sourceText, "private void SignalWork(string operation)");
        AssertContains(sourceText, "FLASHBACK_SINK_WORK_SIGNAL_SKIPPED");
        AssertContains(sourceText, "SignalWork(\"force_rotate_idle\");");
        AssertContains(sourceText, "SignalWork(\"force_rotate_request\");");
        AssertEqual(1, sourceText.Split("_workAvailable.Set();", StringSplitOptions.None).Length - 1, "All work-signal wakeups go through SignalWork");
        AssertContains(sourceText, "FLASHBACK_SINK_ENCODER_DISPOSE_WARN");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_SINK_BUFFER_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}\");");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_RECORDING_FAIL type={failure.GetType().Name} error='{failure.Message}'\");");
        AssertContains(sourceText, "ReturnVideoPacketBestEffort(packet);");
        AssertContains(sourceText, "ReleaseGpuTextureBestEffort(packet.Texture);");
        AssertContains(sourceText, "FLASHBACK_SINK_RETURN_VIDEO_PACKET_WARN");
        AssertContains(sourceText, "FLASHBACK_SINK_RELEASE_GPU_PACKET_WARN");
        AssertContains(sourceText, "public long VideoQueueRejectedFrames => Interlocked.Read(ref _videoQueueRejectedFrames);");
        AssertContains(sourceText, "public string? LastVideoQueueRejectReason => Volatile.Read(ref _lastVideoQueueRejectReason);");
        AssertContains(sourceText, "public long GpuQueueRejectedFrames => Interlocked.Read(ref _gpuQueueRejectedFrames);");
        AssertContains(sourceText, "public string? LastGpuQueueRejectReason => Volatile.Read(ref _lastGpuQueueRejectReason);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _videoQueueRejectedFrames, 0);");
        AssertContains(sourceText, "Volatile.Write(ref _lastVideoQueueRejectReason, null);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _gpuQueueRejectedFrames, 0);");
        AssertContains(sourceText, "Volatile.Write(ref _lastGpuQueueRejectReason, null);");
        AssertContains(sourceText, "private const double ForceRotateQueueGuardRatio = 0.65;");
        AssertContains(sourceText, "private string? GetVideoEnqueueRejectReason(bool isGpu)");
        AssertContains(sourceText, "private string? GetVideoInputRejectReason(Channel<VideoFramePacket>? queue, int expectedSize, bool dataIsEmpty)");
        AssertContains(sourceText, "private string? GetGpuInputRejectReason(Channel<GpuFramePacket>? queue, IntPtr texture)");
        AssertContains(sourceText, "return \"force_rotate_draining\";");
        AssertDoesNotContain(sourceText, "return \"force_rotate_queue_guard\";");
        AssertContains(sourceText, "private static bool IsForceRotateQueueGuarded(int queueDepth, int queueCapacity)");
        AssertContains(sourceText, "queueDepth >= Math.Ceiling(queueCapacity * ForceRotateQueueGuardRatio)");
        AssertContains(sourceText, "return \"cancelled\";");
        AssertContains(sourceText, "return \"disposed\";");
        AssertContains(sourceText, "return \"not_started\";");
        AssertContains(sourceText, "return \"queue_null\";");
        AssertContains(sourceText, "return \"invalid_expected_size\";");
        AssertContains(sourceText, "return dataIsEmpty ? \"data_empty\" : null;");
        AssertContains(sourceText, "return texture == IntPtr.Zero ? \"null_texture\" : null;");
        AssertContains(sourceText, "TrackGpuQueueRejected(\"invalid_subresource\");");
        AssertContains(sourceText, "? $\"encoding_failed:{failure.GetType().Name}\"");
        AssertContains(sourceText, "private void TrackVideoQueueRejected(string reason)");
        AssertContains(sourceText, "private void TrackGpuQueueRejected(string reason)");
        AssertContains(sourceText, "FLASHBACK_SINK_VIDEO_QUEUE_REJECT");
        AssertContains(sourceText, "FLASHBACK_SINK_GPU_QUEUE_REJECT");
        AssertContains(sourceText, "total == 1 || total % 30 == 0");
        AssertContains(sourceText, "private bool TryWriteVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertContains(sourceText, "var depth = Interlocked.Increment(ref _videoQueueDepth);\n        if (queue.Writer.TryWrite(packet))");
        AssertContains(sourceText, "AtomicMax.Update(ref _videoQueueMaxDepth, depth);");
        AssertContains(sourceText, "DecrementQueueDepth(ref _videoQueueDepth, \"video_write_failed\");");
        AssertContains(sourceText, "private bool TryWriteGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)");
        AssertContains(sourceText, "var depth = Interlocked.Increment(ref _gpuQueueDepth);\n        if (queue.Writer.TryWrite(packet))");
        AssertContains(sourceText, "AtomicMax.Update(ref _gpuQueueMaxDepth, depth);");
        AssertContains(sourceText, "DecrementQueueDepth(ref _gpuQueueDepth, \"gpu_write_failed\");");
        AssertContains(sourceText, "private static bool TryWriteAudioPacket(");
        AssertContains(sourceText, "Interlocked.Increment(ref queueDepth);\n        if (queue.Writer.TryWrite(packet))");
        AssertContains(sourceText, "DecrementQueueDepth(ref queueDepth, $\"{queueName}_write_failed\");");
        AssertContains(sourceText, "TryWriteAudioPacket(queue, packet, ref queueDepth, \"audio\")");
        AssertContains(sourceText, "TryWriteAudioPacket(queue, packet, ref queueDepth, \"audio_after_evict\")");
        AssertContains(sourceText, "private static void DecrementQueueDepth(ref int target, string queueName)");
        AssertContains(sourceText, "var current = Volatile.Read(ref target);");
        AssertContains(sourceText, "if (current <= 0)");
        AssertContains(sourceText, "if (Interlocked.CompareExchange(ref target, current - 1, current) == current)");
        AssertContains(sourceText, "FLASHBACK_SINK_QUEUE_DEPTH_UNDERFLOW");
        AssertContains(sourceText, "DecrementQueueDepth(ref _videoQueueDepth, \"video\");");
        AssertContains(sourceText, "DecrementQueueDepth(ref _gpuQueueDepth, \"gpu\");");
        AssertContains(sourceText, "DecrementQueueDepth(ref _audioQueueDepth, \"audio\");");
        AssertContains(sourceText, "DecrementQueueDepth(ref _microphoneQueueDepth, \"microphone\");");
        AssertDoesNotContain(sourceText, "private bool WaitForBackpressureRetryCancellation()");
        AssertDoesNotContain(sourceText, "=> WaitForCancellation(TimeSpan.FromMilliseconds(1));");
        AssertContains(sourceText, "private bool WaitForCancellation(TimeSpan timeout)");
        AssertContains(sourceText, "return cts.Token.WaitHandle.WaitOne(timeout);");
        AssertContains(sourceText, "catch (ObjectDisposedException)\n        {\n            return true;\n        }");
        AssertDoesNotContain(sourceText, "if (WaitForBackpressureRetryCancellation())");
        AssertContains(sourceText, "TrackVideoQueueRejected(\"queue_full\");");
        AssertContains(sourceText, "TrackGpuQueueRejected(\"queue_full\");");
        AssertDoesNotContain(sourceText, "FLASHBACK_SINK_VIDEO_BACKPRESSURE_DROP");
        AssertDoesNotContain(sourceText, "FLASHBACK_SINK_GPU_BACKPRESSURE_DROP");
        AssertDoesNotContain(sourceText, "FailEncoding(overloadFailure);");
        AssertDoesNotContain(sourceText, "Flashback recording video queue overloaded after");
        AssertDoesNotContain(sourceText, "Flashback GPU recording queue overloaded after");
        AssertContains(sourceText, "if (WaitForCancellation(TimeSpan.FromMilliseconds(10)))\n            {\n                return false;\n            }");
        AssertDoesNotContain(sourceText, "var depth = Interlocked.Decrement(ref target);");
        AssertDoesNotContain(sourceText, "Interlocked.Exchange(ref target, 0);\n        Logger.Log($\"FLASHBACK_SINK_QUEUE_DEPTH_UNDERFLOW");
        AssertDoesNotContain(sourceText, "Interlocked.Decrement(ref _videoQueueDepth)");
        AssertDoesNotContain(sourceText, "Interlocked.Decrement(ref _gpuQueueDepth)");
        AssertDoesNotContain(sourceText, "AtomicMax.Update(ref _videoQueueMaxDepth, Interlocked.Increment(ref _videoQueueDepth))");
        AssertDoesNotContain(sourceText, "AtomicMax.Update(ref _gpuQueueMaxDepth, Interlocked.Increment(ref _gpuQueueDepth))");
        AssertDoesNotContain(sourceText, "queue.Writer.TryWrite(packet))\n        {\n            Interlocked.Increment(ref queueDepth);");
        AssertDoesNotContain(sourceText, "Marshal.Release(packet.Texture);");

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_AudioPacketsAreValidatedBeforeRent()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

        AssertContains(sourceText, "private const int AudioInputBlockAlignBytes = 2 * sizeof(float);");
        AssertContains(sourceText, "private const int MaxAudioPacketBytes = 4 * 1024 * 1024;");
        AssertContains(sourceText, "if (!TryValidateAudioPacketLength(samples.Length, \"audio\"))");
        AssertContains(sourceText, "if (!TryValidateAudioPacketLength(samples.Length, \"microphone\"))");
        AssertContains(sourceText, "private static bool TryValidateAudioPacketLength(int byteLength, string source)");
        AssertContains(sourceText, "if (byteLength <= 0 || byteLength > MaxAudioPacketBytes)");
        AssertContains(sourceText, "FLASHBACK_SINK_AUDIO_PACKET_REJECT source={source} reason=size");
        AssertContains(sourceText, "if (byteLength % AudioInputBlockAlignBytes != 0)");
        AssertContains(sourceText, "FLASHBACK_SINK_AUDIO_PACKET_REJECT source={source} reason=alignment");
        AssertContains(sourceText, "return byteLength > 0 ? byteLength / AudioInputBlockAlignBytes : 0;");
        AssertDoesNotContain(sourceText, "const int inputBlockAlign = 2 * sizeof(float);");

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_NormalDrainLoopInterleavesAudioWithBoundedVideoBatches()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

        AssertContains(sourceText, "private const int VideoDrainBatchLimit = 24;");
        AssertContains(sourceText, "private const int GpuDrainBatchLimit = 16;");
        AssertContains(sourceText, "DrainGpuPackets(gpuQueue.Reader, GpuDrainBatchLimit)");
        AssertContains(sourceText, "DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit)");
        AssertContains(sourceText, "private bool DrainVideoPackets(ChannelReader<VideoFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(sourceText, "while (drainedCount < maxPackets)");
        AssertContains(sourceText, "private bool DrainGpuPackets(ChannelReader<GpuFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(sourceText, "while (drainedCount < maxPackets && reader.TryRead(out var packet))");

        var loopBlock = ExtractTextBetween(
            sourceText,
            "private void EncodingLoop(CancellationToken cancellationToken)",
            "    private bool DrainVideoPackets");
        AssertOccursBefore(loopBlock, "DrainAudioPackets(audioQueue.Reader)", "DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit)");
        AssertOccursBefore(loopBlock, "DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit)", "// Audio AGAIN");
        var secondAudioDrainBlock = ExtractTextBetween(
            loopBlock,
            "// Audio AGAIN",
            "// Handle force-rotate requests");
        AssertContains(secondAudioDrainBlock, "DrainAudioPackets(audioQueue.Reader)");

        return Task.CompletedTask;
    }

}
