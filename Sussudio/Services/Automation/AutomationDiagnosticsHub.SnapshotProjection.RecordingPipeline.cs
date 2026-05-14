using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static RecordingPipelineProjection BuildRecordingPipelineProjection(CaptureHealthSnapshot health)
        => new()
        {
            EncoderVideoFramesEnqueued = health.VideoFramesEnqueued,
            EncoderVideoFramesEncoded = health.VideoFramesConverted,
            EncoderLastEnqueueAgeMs = health.LastVideoEnqueueAgeMs,
            EncoderLastWriteAgeMs = health.LastVideoWriteAgeMs,
            ConversionQueueDepth = health.ConversionQueueDepth,
            FfmpegVideoQueueDepth = health.FfmpegVideoQueueDepth,
            FfmpegAudioQueueDepth = health.FfmpegAudioQueueDepth,
            VideoFramesArrived = health.VideoFramesArrived,
            VideoFramesQueued = health.VideoFramesQueued,
            VideoFramesDropped = health.VideoFramesDropped,
            VideoFramesDroppedBacklog = health.VideoFramesDroppedBacklog,
            VideoFramesConverted = health.VideoFramesConverted,
            VideoFramesEnqueued = health.VideoFramesEnqueued,
            VideoDropsQueueSaturated = health.VideoDropsQueueSaturated,
            VideoDropsBacklogEviction = health.VideoDropsBacklogEviction,
            RecordingEncodingFailed = health.RecordingEncodingFailed,
            RecordingEncodingFailureType = health.RecordingEncodingFailureType,
            RecordingEncodingFailureMessage = health.RecordingEncodingFailureMessage,
            RecordingVideoQueueCapacity = health.RecordingVideoQueueCapacity,
            RecordingVideoQueueMaxDepth = health.RecordingVideoQueueMaxDepth,
            RecordingVideoFramesSubmittedToEncoder = health.RecordingVideoFramesSubmittedToEncoder,
            RecordingVideoEncoderPts = health.RecordingVideoEncoderPts,
            RecordingVideoEncoderPacketsWritten = health.RecordingVideoEncoderPacketsWritten,
            RecordingVideoEncoderDroppedFrames = health.RecordingVideoEncoderDroppedFrames,
            RecordingVideoSequenceGaps = health.RecordingVideoSequenceGaps,
            RecordingVideoQueueOldestFrameAgeMs = health.RecordingVideoQueueOldestFrameAgeMs,
            RecordingVideoQueueLastLatencyMs = health.RecordingVideoQueueLastLatencyMs,
            RecordingVideoQueueLatencySampleCount = health.RecordingVideoQueueLatencySampleCount,
            RecordingVideoQueueLatencyAvgMs = health.RecordingVideoQueueLatencyAvgMs,
            RecordingVideoQueueLatencyP95Ms = health.RecordingVideoQueueLatencyP95Ms,
            RecordingVideoQueueLatencyP99Ms = health.RecordingVideoQueueLatencyP99Ms,
            RecordingVideoQueueLatencyMaxMs = health.RecordingVideoQueueLatencyMaxMs,
            RecordingVideoBackpressureWaitMs = health.RecordingVideoBackpressureWaitMs,
            RecordingVideoBackpressureEvents = health.RecordingVideoBackpressureEvents,
            RecordingVideoBackpressureLastWaitMs = health.RecordingVideoBackpressureLastWaitMs,
            RecordingVideoBackpressureMaxWaitMs = health.RecordingVideoBackpressureMaxWaitMs,
            RecordingGpuQueueDepth = health.RecordingGpuQueueDepth,
            RecordingGpuQueueCapacity = health.RecordingGpuQueueCapacity,
            RecordingGpuQueueMaxDepth = health.RecordingGpuQueueMaxDepth,
            RecordingGpuFramesEnqueued = health.RecordingGpuFramesEnqueued,
            RecordingGpuFramesDropped = health.RecordingGpuFramesDropped,
            RecordingCudaQueueDepth = health.RecordingCudaQueueDepth,
            RecordingCudaQueueCapacity = health.RecordingCudaQueueCapacity,
            RecordingCudaQueueMaxDepth = health.RecordingCudaQueueMaxDepth,
            RecordingCudaFramesEnqueued = health.RecordingCudaFramesEnqueued,
            RecordingCudaFramesDropped = health.RecordingCudaFramesDropped
        };

    private readonly record struct RecordingPipelineProjection
    {
        public long EncoderVideoFramesEnqueued { get; init; }
        public long EncoderVideoFramesEncoded { get; init; }
        public long EncoderLastEnqueueAgeMs { get; init; }
        public long EncoderLastWriteAgeMs { get; init; }
        public int ConversionQueueDepth { get; init; }
        public int FfmpegVideoQueueDepth { get; init; }
        public int FfmpegAudioQueueDepth { get; init; }
        public long VideoFramesArrived { get; init; }
        public long VideoFramesQueued { get; init; }
        public long VideoFramesDropped { get; init; }
        public long VideoFramesDroppedBacklog { get; init; }
        public long VideoFramesConverted { get; init; }
        public long VideoFramesEnqueued { get; init; }
        public long VideoDropsQueueSaturated { get; init; }
        public long VideoDropsBacklogEviction { get; init; }
        public bool RecordingEncodingFailed { get; init; }
        public string? RecordingEncodingFailureType { get; init; }
        public string? RecordingEncodingFailureMessage { get; init; }
        public int RecordingVideoQueueCapacity { get; init; }
        public int RecordingVideoQueueMaxDepth { get; init; }
        public long RecordingVideoFramesSubmittedToEncoder { get; init; }
        public long RecordingVideoEncoderPts { get; init; }
        public long RecordingVideoEncoderPacketsWritten { get; init; }
        public long RecordingVideoEncoderDroppedFrames { get; init; }
        public long RecordingVideoSequenceGaps { get; init; }
        public long RecordingVideoQueueOldestFrameAgeMs { get; init; }
        public long RecordingVideoQueueLastLatencyMs { get; init; }
        public int RecordingVideoQueueLatencySampleCount { get; init; }
        public double RecordingVideoQueueLatencyAvgMs { get; init; }
        public double RecordingVideoQueueLatencyP95Ms { get; init; }
        public double RecordingVideoQueueLatencyP99Ms { get; init; }
        public double RecordingVideoQueueLatencyMaxMs { get; init; }
        public long RecordingVideoBackpressureWaitMs { get; init; }
        public long RecordingVideoBackpressureEvents { get; init; }
        public long RecordingVideoBackpressureLastWaitMs { get; init; }
        public long RecordingVideoBackpressureMaxWaitMs { get; init; }
        public int RecordingGpuQueueDepth { get; init; }
        public int RecordingGpuQueueCapacity { get; init; }
        public int RecordingGpuQueueMaxDepth { get; init; }
        public long RecordingGpuFramesEnqueued { get; init; }
        public long RecordingGpuFramesDropped { get; init; }
        public int RecordingCudaQueueDepth { get; init; }
        public int RecordingCudaQueueCapacity { get; init; }
        public int RecordingCudaQueueMaxDepth { get; init; }
        public long RecordingCudaFramesEnqueued { get; init; }
        public long RecordingCudaFramesDropped { get; init; }
    }
}
