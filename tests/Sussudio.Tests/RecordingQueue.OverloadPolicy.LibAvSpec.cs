using System;

// LibAv-specific overload assertions for the recording queue policy harness.
static partial class Program
{
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
}
