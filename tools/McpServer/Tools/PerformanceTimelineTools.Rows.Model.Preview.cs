namespace McpServer.Tools;

public static partial class PerformanceTimelineTools
{
    private sealed partial class TimelineRow
    {
        public double VisualCadenceChangeObservedFps { get; set; }
        public double VisualCadenceRepeatFramePercent { get; set; }
        public string VisualCadenceMotionConfidence { get; set; } = string.Empty;
        public double MjpegPacketHashInputObservedFps { get; set; }
        public double MjpegPacketHashUniqueObservedFps { get; set; }
        public double MjpegPacketHashDuplicateFramePercent { get; set; }
        public bool MjpegPreviewJitterEnabled { get; set; }
        public int MjpegPreviewJitterTargetDepth { get; set; }
        public int MjpegPreviewJitterMaxDepth { get; set; }
        public int MjpegPreviewJitterQueueDepth { get; set; }
        public long MjpegPreviewJitterTotalDropped { get; set; }
        public long MjpegPreviewJitterDeadlineDropCount { get; set; }
        public long MjpegPreviewJitterClearedDropCount { get; set; }
        public long MjpegPreviewJitterUnderflowCount { get; set; }
        public long MjpegPreviewJitterResumeReprimeCount { get; set; }
        public double MjpegPreviewJitterLatencyP95Ms { get; set; }
        public double MjpegPreviewJitterLatencyMaxMs { get; set; }
        public string MjpegPreviewJitterLastDropReason { get; set; } = string.Empty;
        public string MjpegPreviewJitterLastUnderflowReason { get; set; } = string.Empty;
        public double MjpegPreviewJitterLastUnderflowInputAgeMs { get; set; }
        public double MjpegPreviewJitterLastUnderflowOutputAgeMs { get; set; }
        public double MjpegPreviewJitterMaxScheduleLateMs { get; set; }
        public long MjpegPreviewJitterScheduleLateCount { get; set; }
        public int PreviewD3DPending { get; set; }
        public double PreviewD3DPresentP95Ms { get; set; }
        public double PreviewD3DTotalP95Ms { get; set; }
        public double PreviewD3DInputUploadP99Ms { get; set; }
        public double PreviewD3DRenderSubmitP99Ms { get; set; }
        public double PreviewD3DPresentP99Ms { get; set; }
        public double PreviewD3DTotalP99Ms { get; set; }
        public double PreviewD3DPipelineP99Ms { get; set; }
        public double PreviewD3DPipelineMaxMs { get; set; }
        public long PreviewD3DFrameLatencyWaitTimeouts { get; set; }
        public double PreviewD3DFrameLatencyWaitP95Ms { get; set; }
        public double PreviewD3DFrameLatencyWaitMaxMs { get; set; }
        public long PreviewD3DRecentMissed { get; set; }
        public long PreviewD3DRecentFailures { get; set; }
        public double PreviewD3DSchedulerToPresentMs { get; set; }
        public double PreviewD3DLastPipelineLatencyMs { get; set; }
        public string PreviewD3DLastDropReason { get; set; } = string.Empty;
        public string PreviewPacingLikelySlowStage { get; set; } = string.Empty;
        public string PreviewPacingSlowStageConfidence { get; set; } = string.Empty;
        public string PreviewPacingSlowStageEvidence { get; set; } = string.Empty;
    }
}
