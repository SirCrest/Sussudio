using System.Text.Json;
using Sussudio.Tools;

namespace McpServer.Tools;

public static partial class PerformanceTimelineTools
{
    private static void PopulatePreviewTimelineRow(JsonElement item, TimelineRow row)
    {
        row.VisualCadenceChangeObservedFps = AutomationSnapshotFormatter.GetDouble(item, "VisualCadenceChangeObservedFps");
        row.VisualCadenceRepeatFramePercent = AutomationSnapshotFormatter.GetDouble(item, "VisualCadenceRepeatFramePercent");
        row.VisualCadenceMotionConfidence = AutomationSnapshotFormatter.Get(item, "VisualCadenceMotionConfidence");
        row.MjpegPacketHashInputObservedFps = AutomationSnapshotFormatter.GetDouble(item, "MjpegPacketHashInputObservedFps");
        row.MjpegPacketHashUniqueObservedFps = AutomationSnapshotFormatter.GetDouble(item, "MjpegPacketHashUniqueObservedFps");
        row.MjpegPacketHashDuplicateFramePercent = AutomationSnapshotFormatter.GetDouble(item, "MjpegPacketHashDuplicateFramePercent");
        row.MjpegPreviewJitterEnabled = AutomationSnapshotFormatter.GetBool(item, "MjpegPreviewJitterEnabled");
        row.MjpegPreviewJitterTargetDepth = AutomationSnapshotFormatter.GetInt(item, "MjpegPreviewJitterTargetDepth");
        row.MjpegPreviewJitterMaxDepth = AutomationSnapshotFormatter.GetInt(item, "MjpegPreviewJitterMaxDepth");
        row.MjpegPreviewJitterQueueDepth = AutomationSnapshotFormatter.GetInt(item, "MjpegPreviewJitterQueueDepth");
        row.MjpegPreviewJitterTotalDropped = AutomationSnapshotFormatter.GetLong(item, "MjpegPreviewJitterTotalDropped");
        row.MjpegPreviewJitterDeadlineDropCount = AutomationSnapshotFormatter.GetLong(item, "MjpegPreviewJitterDeadlineDropCount");
        row.MjpegPreviewJitterClearedDropCount = AutomationSnapshotFormatter.GetLong(item, "MjpegPreviewJitterClearedDropCount");
        row.MjpegPreviewJitterUnderflowCount = AutomationSnapshotFormatter.GetLong(item, "MjpegPreviewJitterUnderflowCount");
        row.MjpegPreviewJitterResumeReprimeCount = AutomationSnapshotFormatter.GetLong(item, "MjpegPreviewJitterResumeReprimeCount");
        row.MjpegPreviewJitterLatencyP95Ms = AutomationSnapshotFormatter.GetDouble(item, "MjpegPreviewJitterLatencyP95Ms");
        row.MjpegPreviewJitterLatencyMaxMs = AutomationSnapshotFormatter.GetDouble(item, "MjpegPreviewJitterLatencyMaxMs");
        row.MjpegPreviewJitterLastDropReason = AutomationSnapshotFormatter.Get(item, "MjpegPreviewJitterLastDropReason");
        row.MjpegPreviewJitterLastUnderflowReason = AutomationSnapshotFormatter.Get(item, "MjpegPreviewJitterLastUnderflowReason");
        row.MjpegPreviewJitterLastUnderflowInputAgeMs = AutomationSnapshotFormatter.GetDouble(item, "MjpegPreviewJitterLastUnderflowInputAgeMs");
        row.MjpegPreviewJitterLastUnderflowOutputAgeMs = AutomationSnapshotFormatter.GetDouble(item, "MjpegPreviewJitterLastUnderflowOutputAgeMs");
        row.MjpegPreviewJitterMaxScheduleLateMs = AutomationSnapshotFormatter.GetDouble(item, "MjpegPreviewJitterMaxScheduleLateMs");
        row.MjpegPreviewJitterScheduleLateCount = AutomationSnapshotFormatter.GetLong(item, "MjpegPreviewJitterScheduleLateCount");
        row.PreviewD3DPending = AutomationSnapshotFormatter.GetInt(item, "PreviewD3DPendingFrameCount");
        row.PreviewD3DPresentP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DPresentCallP95Ms");
        row.PreviewD3DTotalP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DTotalFrameCpuP95Ms");
        row.PreviewD3DInputUploadP99Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DInputUploadCpuP99Ms");
        row.PreviewD3DRenderSubmitP99Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DRenderSubmitCpuP99Ms");
        row.PreviewD3DPresentP99Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DPresentCallP99Ms");
        row.PreviewD3DTotalP99Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DTotalFrameCpuP99Ms");
        row.PreviewD3DPipelineP99Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DPipelineLatencyP99Ms");
        row.PreviewD3DPipelineMaxMs = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DPipelineLatencyMaxMs");
        row.PreviewD3DFrameLatencyWaitTimeouts = AutomationSnapshotFormatter.GetLong(item, "PreviewD3DFrameLatencyWaitTimeoutCount");
        row.PreviewD3DFrameLatencyWaitP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DFrameLatencyWaitP95Ms");
        row.PreviewD3DFrameLatencyWaitMaxMs = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DFrameLatencyWaitMaxMs");
        row.PreviewD3DRecentMissed = AutomationSnapshotFormatter.GetLong(item, "PreviewD3DFrameStatsRecentMissedRefreshCount");
        row.PreviewD3DRecentFailures = AutomationSnapshotFormatter.GetLong(item, "PreviewD3DFrameStatsRecentFailureCount");
        row.PreviewD3DSchedulerToPresentMs = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DLastRenderedSchedulerToPresentMs");
        row.PreviewD3DLastPipelineLatencyMs = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DLastRenderedPipelineLatencyMs");
        row.PreviewD3DLastDropReason = AutomationSnapshotFormatter.Get(item, "PreviewD3DLastDropReason");
        row.PreviewPacingLikelySlowStage = AutomationSnapshotFormatter.Get(item, "PreviewPacingLikelySlowStage");
        row.PreviewPacingSlowStageConfidence = AutomationSnapshotFormatter.Get(item, "PreviewPacingSlowStageConfidence");
        row.PreviewPacingSlowStageEvidence = AutomationSnapshotFormatter.Get(item, "PreviewPacingSlowStageEvidence");
    }
}
