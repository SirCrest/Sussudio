namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static SourceReaderFlattenedProjection BuildSourceReaderFlattenedProjection(
        CaptureIngestProjection ingest)
        => new()
        {
            FramesDelivered = ingest.MfSourceReaderFramesDelivered,
            FramesDropped = ingest.MfSourceReaderFramesDropped,
            NegotiatedFormat = ingest.MfSourceReaderNegotiatedFormat,
            ReadOutstanding = ingest.SourceReaderReadOutstanding,
            ReadOutstandingMs = ingest.SourceReaderReadOutstandingMs,
            LastFrameTickMs = ingest.SourceReaderLastFrameTickMs,
            FrameChannelDepth = ingest.SourceReaderFrameChannelDepth
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
