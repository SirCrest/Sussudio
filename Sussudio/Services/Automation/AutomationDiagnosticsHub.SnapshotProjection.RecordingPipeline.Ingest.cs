using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static RecordingPipelineIngestProjection BuildRecordingPipelineIngestProjection(CaptureHealthSnapshot health)
        => new()
        {
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
            VideoDropsBacklogEviction = health.VideoDropsBacklogEviction
        };

    private static RecordingPipelineIngestFlattenedProjection BuildRecordingPipelineIngestFlattenedProjection(
        RecordingPipelineProjection recordingPipeline)
        => new()
        {
            ConversionQueueDepth = recordingPipeline.Ingest.ConversionQueueDepth,
            FfmpegVideoQueueDepth = recordingPipeline.Ingest.FfmpegVideoQueueDepth,
            FfmpegAudioQueueDepth = recordingPipeline.Ingest.FfmpegAudioQueueDepth,
            VideoFramesArrived = recordingPipeline.Ingest.VideoFramesArrived,
            VideoFramesQueued = recordingPipeline.Ingest.VideoFramesQueued,
            VideoFramesDropped = recordingPipeline.Ingest.VideoFramesDropped,
            VideoFramesDroppedBacklog = recordingPipeline.Ingest.VideoFramesDroppedBacklog,
            VideoFramesConverted = recordingPipeline.Ingest.VideoFramesConverted,
            VideoFramesEnqueued = recordingPipeline.Ingest.VideoFramesEnqueued,
            VideoDropsQueueSaturated = recordingPipeline.Ingest.VideoDropsQueueSaturated,
            VideoDropsBacklogEviction = recordingPipeline.Ingest.VideoDropsBacklogEviction
        };

    private readonly record struct RecordingPipelineIngestProjection
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
