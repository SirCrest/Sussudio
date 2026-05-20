namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static RecordingPipelineFlattenedProjection BuildRecordingPipelineFlattenedProjection(
        RecordingPipelineProjection recordingPipeline)
        => new()
        {
            EncoderVideoFramesEnqueued = recordingPipeline.EncoderVideoFramesEnqueued,
            EncoderVideoFramesEncoded = recordingPipeline.EncoderVideoFramesEncoded,
            EncoderLastEnqueueAgeMs = recordingPipeline.EncoderLastEnqueueAgeMs,
            EncoderLastWriteAgeMs = recordingPipeline.EncoderLastWriteAgeMs,
            ConversionQueueDepth = recordingPipeline.ConversionQueueDepth,
            FfmpegVideoQueueDepth = recordingPipeline.FfmpegVideoQueueDepth,
            FfmpegAudioQueueDepth = recordingPipeline.FfmpegAudioQueueDepth,
            VideoFramesArrived = recordingPipeline.VideoFramesArrived,
            VideoFramesQueued = recordingPipeline.VideoFramesQueued,
            VideoFramesDropped = recordingPipeline.VideoFramesDropped,
            VideoFramesDroppedBacklog = recordingPipeline.VideoFramesDroppedBacklog,
            VideoFramesConverted = recordingPipeline.VideoFramesConverted,
            VideoFramesEnqueued = recordingPipeline.VideoFramesEnqueued,
            VideoDropsQueueSaturated = recordingPipeline.VideoDropsQueueSaturated,
            VideoDropsBacklogEviction = recordingPipeline.VideoDropsBacklogEviction,
            RecordingEncodingFailed = recordingPipeline.RecordingEncodingFailed,
            RecordingEncodingFailureType = recordingPipeline.RecordingEncodingFailureType,
            RecordingEncodingFailureMessage = recordingPipeline.RecordingEncodingFailureMessage,
            RecordingVideoQueueCapacity = recordingPipeline.RecordingVideoQueueCapacity,
            RecordingVideoQueueMaxDepth = recordingPipeline.RecordingVideoQueueMaxDepth,
            RecordingVideoFramesSubmittedToEncoder = recordingPipeline.RecordingVideoFramesSubmittedToEncoder,
            RecordingVideoEncoderPts = recordingPipeline.RecordingVideoEncoderPts,
            RecordingVideoEncoderPacketsWritten = recordingPipeline.RecordingVideoEncoderPacketsWritten,
            RecordingVideoEncoderDroppedFrames = recordingPipeline.RecordingVideoEncoderDroppedFrames,
            RecordingVideoSequenceGaps = recordingPipeline.RecordingVideoSequenceGaps,
            RecordingVideoQueueOldestFrameAgeMs = recordingPipeline.RecordingVideoQueueOldestFrameAgeMs,
            RecordingVideoQueueLastLatencyMs = recordingPipeline.RecordingVideoQueueLastLatencyMs,
            RecordingVideoQueueLatencySampleCount = recordingPipeline.RecordingVideoQueueLatencySampleCount,
            RecordingVideoQueueLatencyAvgMs = recordingPipeline.RecordingVideoQueueLatencyAvgMs,
            RecordingVideoQueueLatencyP95Ms = recordingPipeline.RecordingVideoQueueLatencyP95Ms,
            RecordingVideoQueueLatencyP99Ms = recordingPipeline.RecordingVideoQueueLatencyP99Ms,
            RecordingVideoQueueLatencyMaxMs = recordingPipeline.RecordingVideoQueueLatencyMaxMs,
            RecordingVideoBackpressureWaitMs = recordingPipeline.RecordingVideoBackpressureWaitMs,
            RecordingVideoBackpressureEvents = recordingPipeline.RecordingVideoBackpressureEvents,
            RecordingVideoBackpressureLastWaitMs = recordingPipeline.RecordingVideoBackpressureLastWaitMs,
            RecordingVideoBackpressureMaxWaitMs = recordingPipeline.RecordingVideoBackpressureMaxWaitMs,
            RecordingGpuQueueDepth = recordingPipeline.RecordingGpuQueueDepth,
            RecordingGpuQueueCapacity = recordingPipeline.RecordingGpuQueueCapacity,
            RecordingGpuQueueMaxDepth = recordingPipeline.RecordingGpuQueueMaxDepth,
            RecordingGpuFramesEnqueued = recordingPipeline.RecordingGpuFramesEnqueued,
            RecordingGpuFramesDropped = recordingPipeline.RecordingGpuFramesDropped,
            RecordingCudaQueueDepth = recordingPipeline.RecordingCudaQueueDepth,
            RecordingCudaQueueCapacity = recordingPipeline.RecordingCudaQueueCapacity,
            RecordingCudaQueueMaxDepth = recordingPipeline.RecordingCudaQueueMaxDepth,
            RecordingCudaFramesEnqueued = recordingPipeline.RecordingCudaFramesEnqueued,
            RecordingCudaFramesDropped = recordingPipeline.RecordingCudaFramesDropped
        };

    private readonly record struct RecordingPipelineFlattenedProjection
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
