namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static RecordingPipelineIngestFlattenedProjection BuildRecordingPipelineIngestFlattenedProjection(
        RecordingPipelineProjection recordingPipeline)
        => new()
        {
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
            VideoDropsBacklogEviction = recordingPipeline.VideoDropsBacklogEviction
        };

    private readonly record struct RecordingPipelineIngestFlattenedProjection
    {
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
    }
}
