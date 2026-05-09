namespace Sussudio.Services.Automation;

internal readonly record struct DiagnosticEvaluation(
    string HealthStatus,
    string LikelyStage,
    string Summary,
    string Evidence,
    string SourceLane,
    string DecodeLane,
    string PreviewLane,
    string RenderLane,
    string PresentLane,
    string RecordingLane,
    string AudioLane);

internal readonly record struct PerformanceEvaluation(double Score, bool PerfectionMet, string Summary);

internal readonly record struct FlashbackRecordingRecentCounters(
    long DroppedFrames,
    long EncoderDroppedFrames,
    long SequenceGaps,
    long GpuFramesDropped,
    long BackpressureEvents)
{
    public static FlashbackRecordingRecentCounters Empty { get; } = new(0, 0, 0, 0, 0);
}

internal readonly record struct D3DRendererRecentCounters(
    long Submitted,
    long Rendered,
    long Dropped)
{
    public static D3DRendererRecentCounters Empty { get; } = new(0, 0, 0);
}

internal readonly record struct MjpegRecentCounters(
    long TotalDropped,
    long DecodeFailures,
    long EmitFailures,
    long CompressedQueueDrops)
{
    public static MjpegRecentCounters Empty { get; } = new(0, 0, 0, 0);

    public long Failures => DecodeFailures + EmitFailures + CompressedQueueDrops;
}
