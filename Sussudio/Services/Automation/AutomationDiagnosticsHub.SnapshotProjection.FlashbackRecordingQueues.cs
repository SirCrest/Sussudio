using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackRecordingQueuesProjection BuildFlashbackRecordingQueuesProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            VideoQueueCapacity = health.FlashbackVideoQueueCapacity,
            VideoQueueMaxDepth = health.FlashbackVideoQueueMaxDepth,
            VideoFramesSubmittedToEncoder = health.FlashbackVideoFramesSubmittedToEncoder,
            VideoEncoderPts = health.FlashbackVideoEncoderPts,
            VideoEncoderPacketsWritten = health.FlashbackVideoEncoderPacketsWritten,
            VideoEncoderDroppedFrames = health.FlashbackVideoEncoderDroppedFrames,
            VideoSequenceGaps = health.FlashbackVideoSequenceGaps,
            VideoQueueRejectedFrames = health.FlashbackVideoQueueRejectedFrames,
            VideoQueueLastRejectReason = health.FlashbackVideoQueueLastRejectReason,
            VideoQueueOldestFrameAgeMs = health.FlashbackVideoQueueOldestFrameAgeMs,
            VideoQueueLastLatencyMs = health.FlashbackVideoQueueLastLatencyMs,
            VideoQueueLatencySampleCount = health.FlashbackVideoQueueLatencySampleCount,
            VideoQueueLatencyAvgMs = health.FlashbackVideoQueueLatencyAvgMs,
            VideoQueueLatencyP95Ms = health.FlashbackVideoQueueLatencyP95Ms,
            VideoQueueLatencyP99Ms = health.FlashbackVideoQueueLatencyP99Ms,
            VideoQueueLatencyMaxMs = health.FlashbackVideoQueueLatencyMaxMs,
            VideoBackpressureWaitMs = health.FlashbackVideoBackpressureWaitMs,
            VideoBackpressureEvents = health.FlashbackVideoBackpressureEvents,
            VideoBackpressureLastWaitMs = health.FlashbackVideoBackpressureLastWaitMs,
            VideoBackpressureMaxWaitMs = health.FlashbackVideoBackpressureMaxWaitMs,
            GpuQueueDepth = health.FlashbackGpuQueueDepth,
            GpuQueueCapacity = health.FlashbackGpuQueueCapacity,
            GpuQueueMaxDepth = health.FlashbackGpuQueueMaxDepth,
            GpuFramesEnqueued = health.FlashbackGpuFramesEnqueued,
            GpuFramesDropped = health.FlashbackGpuFramesDropped,
            GpuQueueRejectedFrames = health.FlashbackGpuQueueRejectedFrames,
            GpuQueueLastRejectReason = health.FlashbackGpuQueueLastRejectReason,
            VideoQueueDepth = health.FlashbackVideoQueueDepth,
            AudioQueueDepth = health.FlashbackAudioQueueDepth,
            AudioQueueCapacity = health.FlashbackAudioQueueCapacity
        };

    private readonly record struct FlashbackRecordingQueuesProjection
    {
        public int VideoQueueCapacity { get; init; }
        public int VideoQueueMaxDepth { get; init; }
        public long VideoFramesSubmittedToEncoder { get; init; }
        public long VideoEncoderPts { get; init; }
        public long VideoEncoderPacketsWritten { get; init; }
        public long VideoEncoderDroppedFrames { get; init; }
        public long VideoSequenceGaps { get; init; }
        public long VideoQueueRejectedFrames { get; init; }
        public string VideoQueueLastRejectReason { get; init; }
        public long VideoQueueOldestFrameAgeMs { get; init; }
        public long VideoQueueLastLatencyMs { get; init; }
        public int VideoQueueLatencySampleCount { get; init; }
        public double VideoQueueLatencyAvgMs { get; init; }
        public double VideoQueueLatencyP95Ms { get; init; }
        public double VideoQueueLatencyP99Ms { get; init; }
        public double VideoQueueLatencyMaxMs { get; init; }
        public long VideoBackpressureWaitMs { get; init; }
        public long VideoBackpressureEvents { get; init; }
        public long VideoBackpressureLastWaitMs { get; init; }
        public long VideoBackpressureMaxWaitMs { get; init; }
        public int GpuQueueDepth { get; init; }
        public int GpuQueueCapacity { get; init; }
        public int GpuQueueMaxDepth { get; init; }
        public long GpuFramesEnqueued { get; init; }
        public long GpuFramesDropped { get; init; }
        public long GpuQueueRejectedFrames { get; init; }
        public string GpuQueueLastRejectReason { get; init; }
        public int VideoQueueDepth { get; init; }
        public int AudioQueueDepth { get; init; }
        public int AudioQueueCapacity { get; init; }
    }

    private static FlashbackRecordingQueuesFlattenedProjection BuildFlashbackRecordingQueuesFlattenedProjection(
        FlashbackRecordingQueuesProjection queues)
        => new()
        {
            VideoQueueCapacity = queues.VideoQueueCapacity,
            VideoQueueMaxDepth = queues.VideoQueueMaxDepth,
            VideoFramesSubmittedToEncoder = queues.VideoFramesSubmittedToEncoder,
            VideoEncoderPts = queues.VideoEncoderPts,
            VideoEncoderPacketsWritten = queues.VideoEncoderPacketsWritten,
            VideoEncoderDroppedFrames = queues.VideoEncoderDroppedFrames,
            VideoSequenceGaps = queues.VideoSequenceGaps,
            VideoQueueRejectedFrames = queues.VideoQueueRejectedFrames,
            VideoQueueLastRejectReason = queues.VideoQueueLastRejectReason,
            VideoQueueOldestFrameAgeMs = queues.VideoQueueOldestFrameAgeMs,
            VideoQueueLastLatencyMs = queues.VideoQueueLastLatencyMs,
            VideoQueueLatencySampleCount = queues.VideoQueueLatencySampleCount,
            VideoQueueLatencyAvgMs = queues.VideoQueueLatencyAvgMs,
            VideoQueueLatencyP95Ms = queues.VideoQueueLatencyP95Ms,
            VideoQueueLatencyP99Ms = queues.VideoQueueLatencyP99Ms,
            VideoQueueLatencyMaxMs = queues.VideoQueueLatencyMaxMs,
            VideoBackpressureWaitMs = queues.VideoBackpressureWaitMs,
            VideoBackpressureEvents = queues.VideoBackpressureEvents,
            VideoBackpressureLastWaitMs = queues.VideoBackpressureLastWaitMs,
            VideoBackpressureMaxWaitMs = queues.VideoBackpressureMaxWaitMs,
            GpuQueueDepth = queues.GpuQueueDepth,
            GpuQueueCapacity = queues.GpuQueueCapacity,
            GpuQueueMaxDepth = queues.GpuQueueMaxDepth,
            GpuFramesEnqueued = queues.GpuFramesEnqueued,
            GpuFramesDropped = queues.GpuFramesDropped,
            GpuQueueRejectedFrames = queues.GpuQueueRejectedFrames,
            GpuQueueLastRejectReason = queues.GpuQueueLastRejectReason,
            VideoQueueDepth = queues.VideoQueueDepth,
            AudioQueueDepth = queues.AudioQueueDepth,
            AudioQueueCapacity = queues.AudioQueueCapacity
        };

    private readonly record struct FlashbackRecordingQueuesFlattenedProjection
    {
        public int VideoQueueCapacity { get; init; }
        public int VideoQueueMaxDepth { get; init; }
        public long VideoFramesSubmittedToEncoder { get; init; }
        public long VideoEncoderPts { get; init; }
        public long VideoEncoderPacketsWritten { get; init; }
        public long VideoEncoderDroppedFrames { get; init; }
        public long VideoSequenceGaps { get; init; }
        public long VideoQueueRejectedFrames { get; init; }
        public string VideoQueueLastRejectReason { get; init; }
        public long VideoQueueOldestFrameAgeMs { get; init; }
        public long VideoQueueLastLatencyMs { get; init; }
        public int VideoQueueLatencySampleCount { get; init; }
        public double VideoQueueLatencyAvgMs { get; init; }
        public double VideoQueueLatencyP95Ms { get; init; }
        public double VideoQueueLatencyP99Ms { get; init; }
        public double VideoQueueLatencyMaxMs { get; init; }
        public long VideoBackpressureWaitMs { get; init; }
        public long VideoBackpressureEvents { get; init; }
        public long VideoBackpressureLastWaitMs { get; init; }
        public long VideoBackpressureMaxWaitMs { get; init; }
        public int GpuQueueDepth { get; init; }
        public int GpuQueueCapacity { get; init; }
        public int GpuQueueMaxDepth { get; init; }
        public long GpuFramesEnqueued { get; init; }
        public long GpuFramesDropped { get; init; }
        public long GpuQueueRejectedFrames { get; init; }
        public string GpuQueueLastRejectReason { get; init; }
        public int VideoQueueDepth { get; init; }
        public int AudioQueueDepth { get; init; }
        public int AudioQueueCapacity { get; init; }
    }
}
