namespace Sussudio.Services.Automation;

public sealed class PreviewPacingClassificationInput
{
    public bool IsPreviewing { get; init; }
    public double TargetFrameRate { get; init; }
    public int PreviewCadenceSampleCount { get; init; }
    public double PreviewCadenceSampleDurationMs { get; init; }
    public double PreviewCadenceExpectedIntervalMs { get; init; }
    public double PreviewCadenceObservedFps { get; init; }
    public double PreviewCadenceOnePercentLowFps { get; init; }
    public double PreviewCadenceP99IntervalMs { get; init; }
    public int CaptureCadenceSampleCount { get; init; }
    public double CaptureCadenceSampleDurationMs { get; init; }
    public double CaptureExpectedFrameRate { get; init; }
    public double CaptureCadenceOnePercentLowFps { get; init; }
    public double CaptureCadenceP99IntervalMs { get; init; }
    public long CaptureCadenceSevereGapCount { get; init; }
    public long CaptureCadenceEstimatedDroppedFrames { get; init; }
    public double CaptureCadenceEstimatedDropPercent { get; init; }
    public int MjpegPipelineSampleCount { get; init; }
    public double MjpegDecodeP95Ms { get; init; }
    public double MjpegPipelineP95Ms { get; init; }
    public double MjpegPipelineMaxMs { get; init; }
    public long RecentMjpegDropped { get; init; }
    public long RecentMjpegFailures { get; init; }
    public bool MjpegPreviewJitterEnabled { get; init; }
    public long RecentPreviewJitterDropped { get; init; }
    public long RecentPreviewJitterUnderflows { get; init; }
    public long RecentPreviewJitterDeadlineDrops { get; init; }
    public long RecentPreviewJitterScheduleLateCount { get; init; }
    public double RecentPreviewJitterScheduleLateMs { get; init; }
    public long MjpegPreviewJitterScheduleLateCount { get; init; }
    public double MjpegPreviewJitterMaxScheduleLateMs { get; init; }
    public double MjpegPreviewJitterLatencyP95Ms { get; init; }
    public string MjpegPreviewJitterLastDropReason { get; init; } = string.Empty;
    public long RecentRendererSubmitted { get; init; }
    public long RecentRendererDropped { get; init; }
    public int PreviewD3DPendingFrameCount { get; init; }
    public double PreviewD3DInputUploadCpuP99Ms { get; init; }
    public double PreviewD3DRenderSubmitCpuP99Ms { get; init; }
    public double PreviewD3DPresentCallP99Ms { get; init; }
    public double PreviewD3DTotalFrameCpuP99Ms { get; init; }
    public double PreviewD3DFrameLatencyWaitP95Ms { get; init; }
    public double PreviewD3DFrameLatencyWaitMaxMs { get; init; }
    public long PreviewD3DFrameLatencyWaitTimeoutCount { get; init; }
    public long RecentD3DFrameLatencyWaitTimeoutCount { get; init; }
    public long RecentD3DMissedRefreshes { get; init; }
    public long RecentD3DStatsFailures { get; init; }
    public string PreviewD3DLastDropReason { get; init; } = string.Empty;
    public int VisualCadenceSampleCount { get; init; }
    public double VisualCadenceChangeObservedFps { get; init; }
    public double VisualCadenceRepeatFramePercent { get; init; }
    public long VisualCadenceLongestRepeatRun { get; init; }
    public string VisualCadenceMotionConfidence { get; init; } = string.Empty;
    public int MjpegPacketHashSampleCount { get; init; }
    public double MjpegPacketHashInputObservedFps { get; init; }
    public double MjpegPacketHashUniqueObservedFps { get; init; }
    public double MjpegPacketHashDuplicateFramePercent { get; init; }
}

public readonly record struct PreviewPacingClassification(
    string LikelySlowStage,
    string Confidence,
    string Evidence);