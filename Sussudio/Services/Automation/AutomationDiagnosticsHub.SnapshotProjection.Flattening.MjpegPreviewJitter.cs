namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static MjpegPreviewJitterFlattenedProjection BuildMjpegPreviewJitterFlattenedProjection(
        MjpegPreviewJitterProjection previewJitter)
        => new()
        {
            Enabled = previewJitter.Enabled,
            TargetDepth = previewJitter.TargetDepth,
            MaxDepth = previewJitter.MaxDepth,
            QueueDepth = previewJitter.QueueDepth,
            TotalQueued = previewJitter.TotalQueued,
            TotalSubmitted = previewJitter.TotalSubmitted,
            TotalDropped = previewJitter.TotalDropped,
            UnderflowCount = previewJitter.UnderflowCount,
            ResumeReprimeCount = previewJitter.ResumeReprimeCount,
            InputSampleCount = previewJitter.InputSampleCount,
            InputAvgMs = previewJitter.InputAvgMs,
            InputP95Ms = previewJitter.InputP95Ms,
            InputMaxMs = previewJitter.InputMaxMs,
            OutputSampleCount = previewJitter.OutputSampleCount,
            OutputAvgMs = previewJitter.OutputAvgMs,
            OutputP95Ms = previewJitter.OutputP95Ms,
            OutputMaxMs = previewJitter.OutputMaxMs,
            LatencySampleCount = previewJitter.LatencySampleCount,
            LatencyAvgMs = previewJitter.LatencyAvgMs,
            LatencyP95Ms = previewJitter.LatencyP95Ms,
            LatencyMaxMs = previewJitter.LatencyMaxMs,
            DeadlineDropCount = previewJitter.DeadlineDropCount,
            ClearedDropCount = previewJitter.ClearedDropCount,
            TargetIncreaseCount = previewJitter.TargetIncreaseCount,
            TargetDecreaseCount = previewJitter.TargetDecreaseCount,
            LastSelectedPreviewPresentId = previewJitter.LastSelectedPreviewPresentId,
            LastSelectedSourceSequenceNumber = previewJitter.LastSelectedSourceSequenceNumber,
            LastSelectedQpc = previewJitter.LastSelectedQpc,
            LastSelectedSourceLatencyMs = previewJitter.LastSelectedSourceLatencyMs,
            LastDroppedSourceSequenceNumber = previewJitter.LastDroppedSourceSequenceNumber,
            LastDropQpc = previewJitter.LastDropQpc,
            LastDropReason = previewJitter.LastDropReason,
            LastUnderflowQpc = previewJitter.LastUnderflowQpc,
            LastUnderflowReason = previewJitter.LastUnderflowReason,
            LastUnderflowQueueDepth = previewJitter.LastUnderflowQueueDepth,
            LastUnderflowInputAgeMs = previewJitter.LastUnderflowInputAgeMs,
            LastUnderflowOutputAgeMs = previewJitter.LastUnderflowOutputAgeMs,
            LastScheduleLateMs = previewJitter.LastScheduleLateMs,
            MaxScheduleLateMs = previewJitter.MaxScheduleLateMs,
            ScheduleLateCount = previewJitter.ScheduleLateCount
        };

    private readonly record struct MjpegPreviewJitterFlattenedProjection
    {
        public bool Enabled { get; init; }
        public int TargetDepth { get; init; }
        public int MaxDepth { get; init; }
        public int QueueDepth { get; init; }
        public long TotalQueued { get; init; }
        public long TotalSubmitted { get; init; }
        public long TotalDropped { get; init; }
        public long UnderflowCount { get; init; }
        public long ResumeReprimeCount { get; init; }
        public int InputSampleCount { get; init; }
        public double InputAvgMs { get; init; }
        public double InputP95Ms { get; init; }
        public double InputMaxMs { get; init; }
        public int OutputSampleCount { get; init; }
        public double OutputAvgMs { get; init; }
        public double OutputP95Ms { get; init; }
        public double OutputMaxMs { get; init; }
        public int LatencySampleCount { get; init; }
        public double LatencyAvgMs { get; init; }
        public double LatencyP95Ms { get; init; }
        public double LatencyMaxMs { get; init; }
        public long DeadlineDropCount { get; init; }
        public long ClearedDropCount { get; init; }
        public long TargetIncreaseCount { get; init; }
        public long TargetDecreaseCount { get; init; }
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
