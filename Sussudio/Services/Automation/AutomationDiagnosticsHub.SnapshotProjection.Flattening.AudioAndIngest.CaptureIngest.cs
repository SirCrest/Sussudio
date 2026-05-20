namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static CaptureIngestFlattenedProjection BuildCaptureIngestFlattenedProjection(
        CaptureIngestProjection ingest)
        => new()
        {
            AudioReaderActive = ingest.AudioReaderActive,
            AudioFramesArrived = ingest.AudioFramesArrived,
            AudioFramesWrittenToSink = ingest.AudioFramesWrittenToSink,
            VideoReaderActive = ingest.VideoReaderActive,
            VideoFramesArrived = ingest.VideoFramesArrived,
            VideoFramesWrittenToSink = ingest.VideoFramesWrittenToSink,
            LastVideoFrameAgeMs = ingest.LastVideoFrameAgeMs,
            VideoIngestErrorCount = ingest.VideoIngestErrorCount
        };

    private readonly record struct CaptureIngestFlattenedProjection
    {
        public bool AudioReaderActive { get; init; }
        public long AudioFramesArrived { get; init; }
        public long AudioFramesWrittenToSink { get; init; }
        public bool VideoReaderActive { get; init; }
        public long VideoFramesArrived { get; init; }
        public long VideoFramesWrittenToSink { get; init; }
        public long LastVideoFrameAgeMs { get; init; }
        public long VideoIngestErrorCount { get; init; }
    }
}
