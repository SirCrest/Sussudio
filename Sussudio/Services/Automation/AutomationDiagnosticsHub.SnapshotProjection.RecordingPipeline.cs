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

    private readonly record struct RecordingPipelineProjection
    {
        public RecordingPipelineEncoderProjection Encoder { get; init; }
        public RecordingPipelineIngestProjection Ingest { get; init; }
        public RecordingPipelineVideoQueueProjection VideoQueue { get; init; }
        public RecordingPipelineHardwareQueuesProjection HardwareQueues { get; init; }
    }
}
