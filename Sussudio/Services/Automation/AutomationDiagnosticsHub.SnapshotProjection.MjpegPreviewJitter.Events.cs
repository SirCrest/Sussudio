using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static MjpegPreviewJitterEventProjection BuildMjpegPreviewJitterEventProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            LastSelectedPreviewPresentId = health.MjpegPreviewJitterLastSelectedPreviewPresentId,
            LastSelectedSourceSequenceNumber = health.MjpegPreviewJitterLastSelectedSourceSequenceNumber,
            LastSelectedQpc = health.MjpegPreviewJitterLastSelectedQpc,
            LastSelectedSourceLatencyMs = health.MjpegPreviewJitterLastSelectedSourceLatencyMs,
            LastDroppedSourceSequenceNumber = health.MjpegPreviewJitterLastDroppedSourceSequenceNumber,
            LastDropQpc = health.MjpegPreviewJitterLastDropQpc,
            LastDropReason = health.MjpegPreviewJitterLastDropReason,
            LastUnderflowQpc = health.MjpegPreviewJitterLastUnderflowQpc,
            LastUnderflowReason = health.MjpegPreviewJitterLastUnderflowReason,
            LastUnderflowQueueDepth = health.MjpegPreviewJitterLastUnderflowQueueDepth,
            LastUnderflowInputAgeMs = health.MjpegPreviewJitterLastUnderflowInputAgeMs,
            LastUnderflowOutputAgeMs = health.MjpegPreviewJitterLastUnderflowOutputAgeMs,
            LastScheduleLateMs = health.MjpegPreviewJitterLastScheduleLateMs,
            MaxScheduleLateMs = health.MjpegPreviewJitterMaxScheduleLateMs,
            ScheduleLateCount = health.MjpegPreviewJitterScheduleLateCount
        };

    private readonly record struct MjpegPreviewJitterEventProjection
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
