using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static RecordingPipelineEncoderProjection BuildRecordingPipelineEncoderProjection(CaptureHealthSnapshot health)
        => new()
        {
            VideoFramesEnqueued = health.VideoFramesEnqueued,
            VideoFramesEncoded = health.VideoFramesConverted,
            LastEnqueueAgeMs = health.LastVideoEnqueueAgeMs,
            LastWriteAgeMs = health.LastVideoWriteAgeMs,
            EncodingFailed = health.RecordingEncodingFailed,
            EncodingFailureType = health.RecordingEncodingFailureType,
            EncodingFailureMessage = health.RecordingEncodingFailureMessage
        };

    private static RecordingPipelineEncoderFlattenedProjection BuildRecordingPipelineEncoderFlattenedProjection(
        RecordingPipelineProjection recordingPipeline)
        => new()
        {
            VideoFramesEnqueued = recordingPipeline.Encoder.VideoFramesEnqueued,
            VideoFramesEncoded = recordingPipeline.Encoder.VideoFramesEncoded,
            LastEnqueueAgeMs = recordingPipeline.Encoder.LastEnqueueAgeMs,
            LastWriteAgeMs = recordingPipeline.Encoder.LastWriteAgeMs,
            EncodingFailed = recordingPipeline.Encoder.EncodingFailed,
            EncodingFailureType = recordingPipeline.Encoder.EncodingFailureType,
            EncodingFailureMessage = recordingPipeline.Encoder.EncodingFailureMessage
        };

    private readonly record struct RecordingPipelineEncoderProjection
    {
        public long VideoFramesEnqueued { get; init; }
        public long VideoFramesEncoded { get; init; }
        public long LastEnqueueAgeMs { get; init; }
        public long LastWriteAgeMs { get; init; }
        public bool EncodingFailed { get; init; }
        public string? EncodingFailureType { get; init; }
        public string? EncodingFailureMessage { get; init; }
    }

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
