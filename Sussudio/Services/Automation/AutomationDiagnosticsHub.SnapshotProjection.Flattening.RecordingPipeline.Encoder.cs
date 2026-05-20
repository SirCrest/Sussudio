namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static RecordingPipelineEncoderFlattenedProjection BuildRecordingPipelineEncoderFlattenedProjection(
        RecordingPipelineProjection recordingPipeline)
        => new()
        {
            VideoFramesEnqueued = recordingPipeline.EncoderVideoFramesEnqueued,
            VideoFramesEncoded = recordingPipeline.EncoderVideoFramesEncoded,
            LastEnqueueAgeMs = recordingPipeline.EncoderLastEnqueueAgeMs,
            LastWriteAgeMs = recordingPipeline.EncoderLastWriteAgeMs,
            EncodingFailed = recordingPipeline.RecordingEncodingFailed,
            EncodingFailureType = recordingPipeline.RecordingEncodingFailureType,
            EncodingFailureMessage = recordingPipeline.RecordingEncodingFailureMessage
        };

    private readonly record struct RecordingPipelineEncoderFlattenedProjection
    {
        public long VideoFramesEnqueued { get; init; }
        public long VideoFramesEncoded { get; init; }
        public long LastEnqueueAgeMs { get; init; }
        public long LastWriteAgeMs { get; init; }
        public bool EncodingFailed { get; init; }
        public string? EncodingFailureType { get; init; }
        public string? EncodingFailureMessage { get; init; }
    }
}
