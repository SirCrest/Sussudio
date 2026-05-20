namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static RecordingPipelineVideoQueueFlattenedProjection BuildRecordingPipelineVideoQueueFlattenedProjection(
        RecordingPipelineProjection recordingPipeline)
        => new()
        {
            Capacity = recordingPipeline.VideoQueue.Capacity,
            MaxDepth = recordingPipeline.VideoQueue.MaxDepth,
            FramesSubmittedToEncoder = recordingPipeline.VideoQueue.FramesSubmittedToEncoder,
            EncoderPts = recordingPipeline.VideoQueue.EncoderPts,
            EncoderPacketsWritten = recordingPipeline.VideoQueue.EncoderPacketsWritten,
            EncoderDroppedFrames = recordingPipeline.VideoQueue.EncoderDroppedFrames,
            SequenceGaps = recordingPipeline.VideoQueue.SequenceGaps,
            OldestFrameAgeMs = recordingPipeline.VideoQueue.OldestFrameAgeMs,
            LastLatencyMs = recordingPipeline.VideoQueue.LastLatencyMs,
            LatencySampleCount = recordingPipeline.VideoQueue.LatencySampleCount,
            LatencyAvgMs = recordingPipeline.VideoQueue.LatencyAvgMs,
            LatencyP95Ms = recordingPipeline.VideoQueue.LatencyP95Ms,
            LatencyP99Ms = recordingPipeline.VideoQueue.LatencyP99Ms,
            LatencyMaxMs = recordingPipeline.VideoQueue.LatencyMaxMs,
            BackpressureWaitMs = recordingPipeline.VideoQueue.BackpressureWaitMs,
            BackpressureEvents = recordingPipeline.VideoQueue.BackpressureEvents,
            BackpressureLastWaitMs = recordingPipeline.VideoQueue.BackpressureLastWaitMs,
            BackpressureMaxWaitMs = recordingPipeline.VideoQueue.BackpressureMaxWaitMs
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
