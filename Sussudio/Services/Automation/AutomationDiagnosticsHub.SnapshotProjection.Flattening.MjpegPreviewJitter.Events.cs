namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static MjpegPreviewJitterEventFlattenedProjection BuildMjpegPreviewJitterEventFlattenedProjection(
        MjpegPreviewJitterEventProjection events)
        => new()
        {
            LastSelectedPreviewPresentId = events.LastSelectedPreviewPresentId,
            LastSelectedSourceSequenceNumber = events.LastSelectedSourceSequenceNumber,
            LastSelectedQpc = events.LastSelectedQpc,
            LastSelectedSourceLatencyMs = events.LastSelectedSourceLatencyMs,
            LastDroppedSourceSequenceNumber = events.LastDroppedSourceSequenceNumber,
            LastDropQpc = events.LastDropQpc,
            LastDropReason = events.LastDropReason,
            LastUnderflowQpc = events.LastUnderflowQpc,
            LastUnderflowReason = events.LastUnderflowReason,
            LastUnderflowQueueDepth = events.LastUnderflowQueueDepth,
            LastUnderflowInputAgeMs = events.LastUnderflowInputAgeMs,
            LastUnderflowOutputAgeMs = events.LastUnderflowOutputAgeMs,
            LastScheduleLateMs = events.LastScheduleLateMs,
            MaxScheduleLateMs = events.MaxScheduleLateMs,
            ScheduleLateCount = events.ScheduleLateCount
        };

    private readonly record struct MjpegPreviewJitterEventFlattenedProjection
    {
        public long LastSelectedPreviewPresentId { get; init; }
        public long LastSelectedSourceSequenceNumber { get; init; }
        public long LastSelectedQpc { get; init; }
        public double LastSelectedSourceLatencyMs { get; init; }
        public long LastDroppedSourceSequenceNumber { get; init; }
        public long LastDropQpc { get; init; }
        public string LastDropReason { get; init; }
        public long LastUnderflowQpc { get; init; }
        public string LastUnderflowReason { get; init; }
        public int LastUnderflowQueueDepth { get; init; }
        public double LastUnderflowInputAgeMs { get; init; }
        public double LastUnderflowOutputAgeMs { get; init; }
        public double LastScheduleLateMs { get; init; }
        public double MaxScheduleLateMs { get; init; }
        public long ScheduleLateCount { get; init; }
    }
}
