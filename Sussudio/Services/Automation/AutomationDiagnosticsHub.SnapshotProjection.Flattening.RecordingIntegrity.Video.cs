namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static RecordingIntegrityVideoFlattenedProjection BuildRecordingIntegrityVideoFlattenedProjection(
        RecordingIntegrityVideoProjection video)
        => new()
        {
            SourceFrames = video.SourceFrames,
            AcceptedFrames = video.AcceptedFrames,
            PipelineDroppedFrames = video.PipelineDroppedFrames,
            QueueDroppedFrames = video.QueueDroppedFrames,
            SubmittedFrames = video.SubmittedFrames,
            EncodedFrames = video.EncodedFrames,
            PacketsWritten = video.PacketsWritten,
            EncoderDroppedFrames = video.EncoderDroppedFrames,
            SequenceGaps = video.SequenceGaps
        };

    private readonly record struct RecordingIntegrityVideoFlattenedProjection
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
