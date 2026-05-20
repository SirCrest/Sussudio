namespace Sussudio.Models;

public sealed partial class PerformanceTimelineEntry
{
    public double VisualCadenceChangeObservedFps { get; init; }
    public double VisualCadenceRepeatFramePercent { get; init; }
    public string VisualCadenceMotionConfidence { get; init; } = string.Empty;
    public double MjpegPacketHashInputObservedFps { get; init; }
    public double MjpegPacketHashUniqueObservedFps { get; init; }
    public double MjpegPacketHashDuplicateFramePercent { get; init; }
    public bool MjpegPreviewJitterEnabled { get; init; }
    public int MjpegPreviewJitterTargetDepth { get; init; }
    public int MjpegPreviewJitterMaxDepth { get; init; }
    public int MjpegPreviewJitterQueueDepth { get; init; }
    public long MjpegPreviewJitterTotalDropped { get; init; }
    public long MjpegPreviewJitterDeadlineDropCount { get; init; }
    public long MjpegPreviewJitterClearedDropCount { get; init; }
    public long MjpegPreviewJitterUnderflowCount { get; init; }
    public long MjpegPreviewJitterResumeReprimeCount { get; init; }
    public double MjpegPreviewJitterLatencyP95Ms { get; init; }
    public double MjpegPreviewJitterLatencyMaxMs { get; init; }
    public string MjpegPreviewJitterLastDropReason { get; init; } = string.Empty;
    public string MjpegPreviewJitterLastUnderflowReason { get; init; } = string.Empty;
    public double MjpegPreviewJitterLastUnderflowInputAgeMs { get; init; }
    public double MjpegPreviewJitterLastUnderflowOutputAgeMs { get; init; }
    public double MjpegPreviewJitterMaxScheduleLateMs { get; init; }
    public long MjpegPreviewJitterScheduleLateCount { get; init; }
    public int PreviewD3DPendingFrameCount { get; init; }
    public double PreviewD3DPresentCallP95Ms { get; init; }
    public double PreviewD3DTotalFrameCpuP95Ms { get; init; }
    public double PreviewD3DInputUploadCpuP99Ms { get; init; }
    public double PreviewD3DRenderSubmitCpuP99Ms { get; init; }
    public double PreviewD3DPresentCallP99Ms { get; init; }
    public double PreviewD3DTotalFrameCpuP99Ms { get; init; }
    public double PreviewD3DPipelineLatencyP95Ms { get; init; }
    public double PreviewD3DPipelineLatencyP99Ms { get; init; }
    public double PreviewD3DPipelineLatencyMaxMs { get; init; }
    public long PreviewD3DFrameLatencyWaitTimeoutCount { get; init; }
    public double PreviewD3DFrameLatencyWaitP95Ms { get; init; }
    public double PreviewD3DFrameLatencyWaitMaxMs { get; init; }
    public long PreviewD3DFrameStatsRecentMissedRefreshCount { get; init; }
    public long PreviewD3DFrameStatsRecentFailureCount { get; init; }
    public double PreviewD3DLastRenderedSchedulerToPresentMs { get; init; }
    public double PreviewD3DLastRenderedPipelineLatencyMs { get; init; }
    public string PreviewD3DLastDropReason { get; init; } = string.Empty;
    public string PreviewPacingLikelySlowStage { get; init; } = "Unknown";
    public string PreviewPacingSlowStageConfidence { get; init; } = "None";
    public string PreviewPacingSlowStageEvidence { get; init; } = string.Empty;
}
