namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static SourceReaderFlattenedProjection BuildSourceReaderFlattenedProjection(
        AudioAndIngestProjection audioAndIngest)
        => new()
        {
            FramesDelivered = audioAndIngest.MfSourceReaderFramesDelivered,
            FramesDropped = audioAndIngest.MfSourceReaderFramesDropped,
            NegotiatedFormat = audioAndIngest.MfSourceReaderNegotiatedFormat,
            ReadOutstanding = audioAndIngest.SourceReaderReadOutstanding,
            ReadOutstandingMs = audioAndIngest.SourceReaderReadOutstandingMs,
            LastFrameTickMs = audioAndIngest.SourceReaderLastFrameTickMs,
            FrameChannelDepth = audioAndIngest.SourceReaderFrameChannelDepth
        };

    private readonly record struct SourceReaderFlattenedProjection
    {
        public long FramesDelivered { get; init; }
        public long FramesDropped { get; init; }
        public string? NegotiatedFormat { get; init; }
        public bool ReadOutstanding { get; init; }
        public long ReadOutstandingMs { get; init; }
        public long LastFrameTickMs { get; init; }
        public int FrameChannelDepth { get; init; }
    }
}
