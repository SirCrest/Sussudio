namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static CaptureIngestFlattenedProjection BuildCaptureIngestFlattenedProjection(
        AudioAndIngestProjection audioAndIngest)
        => new()
        {
            AudioReaderActive = audioAndIngest.AudioReaderActive,
            AudioFramesArrived = audioAndIngest.AudioFramesArrived,
            AudioFramesWrittenToSink = audioAndIngest.AudioFramesWrittenToSink,
            VideoReaderActive = audioAndIngest.VideoReaderActive,
            VideoFramesArrived = audioAndIngest.IngestVideoFramesArrived,
            VideoFramesWrittenToSink = audioAndIngest.IngestVideoFramesWrittenToSink,
            LastVideoFrameAgeMs = audioAndIngest.IngestLastVideoFrameAgeMs,
            VideoIngestErrorCount = audioAndIngest.VideoIngestErrorCount
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
