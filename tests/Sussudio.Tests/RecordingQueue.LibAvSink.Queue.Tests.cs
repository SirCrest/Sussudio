using System;
using System.Threading.Tasks;

// Tests for recording sink queue limits, drops, and latency accounting.
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

    private static Task LibAvRecordingSink_VideoQueueSubmissionLivesInFocusedPartial()
    {
        var queueText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.Queues.cs")
            .Replace("\r\n", "\n");
        var videoSubmissionText = ReadRepoFile("Sussudio/Services/Recording/LibAvRecordingSink.VideoQueueSubmission.cs")
            .Replace("\r\n", "\n");

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
        AssertDoesNotContain(queueText, "private VideoEnqueueResult TryEnqueueVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertDoesNotContain(queueText, "private VideoEnqueueResult TryEnqueueGpuPacket(Channel<GpuFramePacket> queue, GpuFramePacket packet)");
        AssertDoesNotContain(queueText, "private unsafe VideoEnqueueResult TryEnqueueCudaPacket(Channel<CudaFramePacket> queue, CudaFramePacket packet)");
        AssertDoesNotContain(queueText, "private bool TryWriteVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)");
        AssertDoesNotContain(queueText, "private readonly record struct VideoFramePacket");

        return Task.CompletedTask;
    }
}
