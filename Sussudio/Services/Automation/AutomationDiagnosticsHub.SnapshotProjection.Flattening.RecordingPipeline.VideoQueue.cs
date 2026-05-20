namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static RecordingPipelineVideoQueueFlattenedProjection BuildRecordingPipelineVideoQueueFlattenedProjection(
        RecordingPipelineProjection recordingPipeline)
        => new()
        {
            Capacity = recordingPipeline.RecordingVideoQueueCapacity,
            MaxDepth = recordingPipeline.RecordingVideoQueueMaxDepth,
            FramesSubmittedToEncoder = recordingPipeline.RecordingVideoFramesSubmittedToEncoder,
            EncoderPts = recordingPipeline.RecordingVideoEncoderPts,
            EncoderPacketsWritten = recordingPipeline.RecordingVideoEncoderPacketsWritten,
            EncoderDroppedFrames = recordingPipeline.RecordingVideoEncoderDroppedFrames,
            SequenceGaps = recordingPipeline.RecordingVideoSequenceGaps,
            OldestFrameAgeMs = recordingPipeline.RecordingVideoQueueOldestFrameAgeMs,
            LastLatencyMs = recordingPipeline.RecordingVideoQueueLastLatencyMs,
            LatencySampleCount = recordingPipeline.RecordingVideoQueueLatencySampleCount,
            LatencyAvgMs = recordingPipeline.RecordingVideoQueueLatencyAvgMs,
            LatencyP95Ms = recordingPipeline.RecordingVideoQueueLatencyP95Ms,
            LatencyP99Ms = recordingPipeline.RecordingVideoQueueLatencyP99Ms,
            LatencyMaxMs = recordingPipeline.RecordingVideoQueueLatencyMaxMs,
            BackpressureWaitMs = recordingPipeline.RecordingVideoBackpressureWaitMs,
            BackpressureEvents = recordingPipeline.RecordingVideoBackpressureEvents,
            BackpressureLastWaitMs = recordingPipeline.RecordingVideoBackpressureLastWaitMs,
            BackpressureMaxWaitMs = recordingPipeline.RecordingVideoBackpressureMaxWaitMs
        };

    private readonly record struct RecordingPipelineVideoQueueFlattenedProjection
    {
        public int Capacity { get; init; }
        public int MaxDepth { get; init; }
        public long FramesSubmittedToEncoder { get; init; }
        public long EncoderPts { get; init; }
        public long EncoderPacketsWritten { get; init; }
        public long EncoderDroppedFrames { get; init; }
        public long SequenceGaps { get; init; }
        public long OldestFrameAgeMs { get; init; }
        public long LastLatencyMs { get; init; }
        public int LatencySampleCount { get; init; }
        public double LatencyAvgMs { get; init; }
        public double LatencyP95Ms { get; init; }
        public double LatencyP99Ms { get; init; }
        public double LatencyMaxMs { get; init; }
        public long BackpressureWaitMs { get; init; }
        public long BackpressureEvents { get; init; }
        public long BackpressureLastWaitMs { get; init; }
        public long BackpressureMaxWaitMs { get; init; }
    }
}
