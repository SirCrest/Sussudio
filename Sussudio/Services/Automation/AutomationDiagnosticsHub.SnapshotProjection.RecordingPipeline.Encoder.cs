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
}
