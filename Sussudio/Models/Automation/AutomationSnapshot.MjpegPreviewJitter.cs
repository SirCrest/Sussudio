namespace Sussudio.Models;

public sealed partial class AutomationSnapshot
{
    public bool MjpegPreviewJitterEnabled { get; init; }
    public int MjpegPreviewJitterTargetDepth { get; init; }
    public int MjpegPreviewJitterMaxDepth { get; init; }
    public int MjpegPreviewJitterQueueDepth { get; init; }
    public long MjpegPreviewJitterTotalQueued { get; init; }
    public long MjpegPreviewJitterTotalSubmitted { get; init; }
    public long MjpegPreviewJitterTotalDropped { get; init; }
    public long MjpegPreviewJitterUnderflowCount { get; init; }
    public long MjpegPreviewJitterResumeReprimeCount { get; init; }
    public int MjpegPreviewJitterInputSampleCount { get; init; }
    public double MjpegPreviewJitterInputAvgMs { get; init; }
    public double MjpegPreviewJitterInputP95Ms { get; init; }
    public double MjpegPreviewJitterInputMaxMs { get; init; }
    public int MjpegPreviewJitterOutputSampleCount { get; init; }
    public double MjpegPreviewJitterOutputAvgMs { get; init; }
    public double MjpegPreviewJitterOutputP95Ms { get; init; }
    public double MjpegPreviewJitterOutputMaxMs { get; init; }
    public int MjpegPreviewJitterLatencySampleCount { get; init; }
    public double MjpegPreviewJitterLatencyAvgMs { get; init; }
    public double MjpegPreviewJitterLatencyP95Ms { get; init; }
    public double MjpegPreviewJitterLatencyMaxMs { get; init; }
    public long MjpegPreviewJitterDeadlineDropCount { get; init; }
    public long MjpegPreviewJitterClearedDropCount { get; init; }
    public long MjpegPreviewJitterTargetIncreaseCount { get; init; }
    public long MjpegPreviewJitterTargetDecreaseCount { get; init; }
    public long MjpegPreviewJitterLastSelectedPreviewPresentId { get; init; }
    public long MjpegPreviewJitterLastSelectedSourceSequenceNumber { get; init; }
    public long MjpegPreviewJitterLastSelectedQpc { get; init; }
    public double MjpegPreviewJitterLastSelectedSourceLatencyMs { get; init; }
    public long MjpegPreviewJitterLastDroppedSourceSequenceNumber { get; init; }
    public long MjpegPreviewJitterLastDropQpc { get; init; }
    public string MjpegPreviewJitterLastDropReason { get; init; } = string.Empty;
    public long MjpegPreviewJitterLastUnderflowQpc { get; init; }
    public string MjpegPreviewJitterLastUnderflowReason { get; init; } = string.Empty;
    public int MjpegPreviewJitterLastUnderflowQueueDepth { get; init; }
    public double MjpegPreviewJitterLastUnderflowInputAgeMs { get; init; }
    public double MjpegPreviewJitterLastUnderflowOutputAgeMs { get; init; }
    public double MjpegPreviewJitterLastScheduleLateMs { get; init; }
    public double MjpegPreviewJitterMaxScheduleLateMs { get; init; }
    public long MjpegPreviewJitterScheduleLateCount { get; init; }
}
