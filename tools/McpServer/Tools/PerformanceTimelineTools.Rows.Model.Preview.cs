namespace McpServer.Tools;

public static partial class PerformanceTimelineTools
{
    private sealed partial class TimelineRow
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
        public int PreviewD3DPending { get; init; }
        public double PreviewD3DPresentP95Ms { get; init; }
        public double PreviewD3DTotalP95Ms { get; init; }
        public double PreviewD3DInputUploadP99Ms { get; init; }
        public double PreviewD3DRenderSubmitP99Ms { get; init; }
        public double PreviewD3DPresentP99Ms { get; init; }
        public double PreviewD3DTotalP99Ms { get; init; }
        public double PreviewD3DPipelineP99Ms { get; init; }
        public double PreviewD3DPipelineMaxMs { get; init; }
        public long PreviewD3DFrameLatencyWaitTimeouts { get; init; }
        public double PreviewD3DFrameLatencyWaitP95Ms { get; init; }
        public double PreviewD3DFrameLatencyWaitMaxMs { get; init; }
        public long PreviewD3DRecentMissed { get; init; }
        public long PreviewD3DRecentFailures { get; init; }
        public double PreviewD3DSchedulerToPresentMs { get; init; }
        public double PreviewD3DLastPipelineLatencyMs { get; init; }
        public string PreviewD3DLastDropReason { get; init; } = string.Empty;
        public string PreviewPacingLikelySlowStage { get; init; } = string.Empty;
        public string PreviewPacingSlowStageConfidence { get; init; } = string.Empty;
        public string PreviewPacingSlowStageEvidence { get; init; } = string.Empty;
    }
}
