using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static RecordingIntegrityVideoProjection BuildRecordingIntegrityVideoProjection(
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            SourceFrames = captureRuntime.RecordingIntegritySourceFrames,
            AcceptedFrames = captureRuntime.RecordingIntegrityAcceptedFrames,
            PipelineDroppedFrames = captureRuntime.RecordingIntegrityPipelineDroppedFrames,
            QueueDroppedFrames = captureRuntime.RecordingIntegrityQueueDroppedFrames,
            SubmittedFrames = captureRuntime.RecordingIntegritySubmittedFrames,
            EncodedFrames = captureRuntime.RecordingIntegrityEncodedFrames,
            PacketsWritten = captureRuntime.RecordingIntegrityPacketsWritten,
            EncoderDroppedFrames = captureRuntime.RecordingIntegrityEncoderDroppedFrames,
            SequenceGaps = captureRuntime.RecordingIntegritySequenceGaps
        };

    private readonly record struct RecordingIntegrityVideoProjection
    {
        public long SourceFrames { get; init; }
        public long AcceptedFrames { get; init; }
        public long PipelineDroppedFrames { get; init; }
        public long QueueDroppedFrames { get; init; }
        public long SubmittedFrames { get; init; }
        public long EncodedFrames { get; init; }
        public long PacketsWritten { get; init; }
        public long EncoderDroppedFrames { get; init; }
        public long SequenceGaps { get; init; }
    }
}
