using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private static FlashbackQueueHealthSnapshotFields CaptureFlashbackQueueHealthSnapshotFields(
        FlashbackEncoderSink? fbSink,
        (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) videoQueueLatencyMetrics)
        => new(
            fbSink?.VideoQueueCount ?? 0,
            fbSink?.AudioQueueCount ?? 0,
            fbSink?.AudioQueueCapacityPackets ?? 0,
            fbSink?.IsForceRotateActive ?? false,
            fbSink?.IsForceRotateRequested ?? false,
            fbSink?.IsForceRotateDraining ?? false,
            fbSink?.VideoQueueCapacityFrames ?? 0,
            fbSink?.VideoQueueMaxDepth ?? 0,
            fbSink?.VideoFramesSubmittedToEncoder ?? 0,
            fbSink?.VideoEncoderPts ?? 0,
            fbSink?.VideoEncoderPacketsWritten ?? 0,
            fbSink?.VideoEncoderDroppedFrames ?? 0,
            fbSink?.VideoSequenceGaps ?? 0,
            fbSink?.VideoQueueRejectedFrames ?? 0,
            fbSink?.LastVideoQueueRejectReason ?? string.Empty,
            fbSink?.VideoQueueOldestFrameAgeMs ?? 0,
            fbSink?.LastVideoQueueLatencyMs ?? 0,
            videoQueueLatencyMetrics,
            fbSink?.VideoBackpressureWaitMs ?? 0,
            fbSink?.VideoBackpressureEvents ?? 0,
            fbSink?.LastVideoBackpressureWaitMs ?? 0,
            fbSink?.MaxVideoBackpressureWaitMs ?? 0,
            fbSink?.GpuQueueCount ?? 0,
            fbSink?.GpuQueueCapacityFrames ?? 0,
            fbSink?.GpuQueueMaxDepth ?? 0,
            fbSink?.GpuFramesEnqueued ?? 0,
            fbSink?.GpuFramesDropped ?? 0,
            fbSink?.GpuQueueRejectedFrames ?? 0,
            fbSink?.LastGpuQueueRejectReason ?? string.Empty);

    private readonly record struct FlashbackQueueHealthSnapshotFields(
        int VideoQueueDepth,
        int AudioQueueDepth,
        int AudioQueueCapacity,
        bool ForceRotateActive,
        bool ForceRotateRequested,
        bool ForceRotateDraining,
        int VideoQueueCapacity,
        int VideoQueueMaxDepth,
        long VideoFramesSubmittedToEncoder,
        long VideoEncoderPts,
        long VideoEncoderPacketsWritten,
        long VideoEncoderDroppedFrames,
        long VideoSequenceGaps,
        long VideoQueueRejectedFrames,
        string VideoQueueLastRejectReason,
        long VideoQueueOldestFrameAgeMs,
        long VideoQueueLastLatencyMs,
        (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) VideoQueueLatencyMetrics,
        long VideoBackpressureWaitMs,
        long VideoBackpressureEvents,
        long VideoBackpressureLastWaitMs,
        long VideoBackpressureMaxWaitMs,
        int GpuQueueDepth,
        int GpuQueueCapacity,
        int GpuQueueMaxDepth,
        long GpuFramesEnqueued,
        long GpuFramesDropped,
        long GpuQueueRejectedFrames,
        string GpuQueueLastRejectReason);
}
