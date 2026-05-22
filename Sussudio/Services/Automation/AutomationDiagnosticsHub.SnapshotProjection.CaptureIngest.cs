using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static CaptureIngestProjection BuildCaptureIngestProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            AudioReaderActive = captureRuntime.AudioReaderActive,
            AudioFramesArrived = captureRuntime.AudioFramesArrived,
            AudioFramesWrittenToSink = captureRuntime.AudioFramesWrittenToSink,
            VideoReaderActive = captureRuntime.VideoReaderActive,
            VideoFramesArrived = captureRuntime.IngestVideoFramesArrived,
            VideoFramesWrittenToSink = captureRuntime.IngestVideoFramesWrittenToSink,
            LastVideoFrameAgeMs = captureRuntime.IngestLastVideoFrameAgeMs,
            VideoIngestErrorCount = captureRuntime.VideoIngestErrorCount,
            MfSourceReaderFramesDelivered = captureRuntime.MfSourceReaderFramesDelivered,
            MfSourceReaderFramesDropped = captureRuntime.MfSourceReaderFramesDropped,
            MfSourceReaderNegotiatedFormat = captureRuntime.MfSourceReaderNegotiatedFormat,
            SourceReaderReadOutstanding = captureRuntime.SourceReaderReadOutstanding,
            SourceReaderReadOutstandingMs = captureRuntime.SourceReaderReadOutstandingMs,
            SourceReaderLastFrameTickMs = captureRuntime.SourceReaderLastFrameTickMs,
            SourceReaderFrameChannelDepth = captureRuntime.SourceReaderFrameChannelDepth
        };

    private readonly record struct CaptureIngestProjection
    {
        public bool AudioReaderActive { get; init; }
        public long AudioFramesArrived { get; init; }
        public long AudioFramesWrittenToSink { get; init; }
        public bool VideoReaderActive { get; init; }
        public long VideoFramesArrived { get; init; }
        public long VideoFramesWrittenToSink { get; init; }
        public long LastVideoFrameAgeMs { get; init; }
        public long VideoIngestErrorCount { get; init; }
        public long MfSourceReaderFramesDelivered { get; init; }
        public long MfSourceReaderFramesDropped { get; init; }
        public string? MfSourceReaderNegotiatedFormat { get; init; }
        public bool SourceReaderReadOutstanding { get; init; }
        public long SourceReaderReadOutstandingMs { get; init; }
        public long SourceReaderLastFrameTickMs { get; init; }
        public int SourceReaderFrameChannelDepth { get; init; }
    }

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
