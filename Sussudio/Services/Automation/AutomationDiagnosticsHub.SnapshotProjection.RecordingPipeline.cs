using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static RecordingPipelineProjection BuildRecordingPipelineProjection(CaptureHealthSnapshot health)
        => new()
        {
            Encoder = BuildRecordingPipelineEncoderProjection(health),
            Ingest = BuildRecordingPipelineIngestProjection(health),
            VideoQueue = BuildRecordingPipelineVideoQueueProjection(health),
            HardwareQueues = BuildRecordingPipelineHardwareQueuesProjection(health)
        };

    private static RecordingPipelineFlattenedProjection BuildRecordingPipelineFlattenedProjection(
        RecordingPipelineProjection recordingPipeline)
        => new()
        {
            Encoder = BuildRecordingPipelineEncoderFlattenedProjection(recordingPipeline),
            Ingest = BuildRecordingPipelineIngestFlattenedProjection(recordingPipeline),
            VideoQueue = BuildRecordingPipelineVideoQueueFlattenedProjection(recordingPipeline),
            HardwareQueues = BuildRecordingPipelineHardwareQueuesFlattenedProjection(recordingPipeline)
        };

    private readonly record struct RecordingPipelineProjection
    {
        public RecordingPipelineEncoderProjection Encoder { get; init; }
        public RecordingPipelineIngestProjection Ingest { get; init; }
        public RecordingPipelineVideoQueueProjection VideoQueue { get; init; }
        public RecordingPipelineHardwareQueuesProjection HardwareQueues { get; init; }
    }

    private readonly record struct RecordingPipelineFlattenedProjection
    {
        public RecordingPipelineEncoderFlattenedProjection Encoder { get; init; }
        public RecordingPipelineIngestFlattenedProjection Ingest { get; init; }
        public RecordingPipelineVideoQueueFlattenedProjection VideoQueue { get; init; }
        public RecordingPipelineHardwareQueuesFlattenedProjection HardwareQueues { get; init; }
    }
}
