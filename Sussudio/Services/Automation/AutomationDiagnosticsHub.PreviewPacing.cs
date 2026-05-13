using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PreviewPacingClassification ClassifyPreviewPacing(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        CaptureHealthSnapshot health,
        PreviewRuntimeSnapshot previewRuntime,
        MjpegRecentCounters recentMjpeg,
        PreviewJitterRecentCounters recentPreviewJitter,
        D3DRendererRecentCounters recentRenderer,
        long recentD3DMissedRefreshes,
        long recentD3DStatsFailures,
        long recentD3DFrameLatencyWaitTimeouts)
        => PreviewPacingSlowStageClassifier.Classify(
            new PreviewPacingClassificationInput
            {
                IsPreviewing = viewModelSnapshot.IsPreviewing,
                TargetFrameRate = health.ExpectedFrameRate,
                PreviewCadenceSampleCount = previewRuntime.DisplayCadenceSampleCount,
                PreviewCadenceSampleDurationMs = previewRuntime.DisplayCadenceSampleDurationMs,
                PreviewCadenceExpectedIntervalMs = previewRuntime.DisplayCadenceExpectedIntervalMs,
                PreviewCadenceObservedFps = previewRuntime.DisplayCadenceObservedFps,
                PreviewCadenceOnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,
                PreviewCadenceP99IntervalMs = previewRuntime.DisplayCadenceP99IntervalMs,
                CaptureCadenceSampleCount = health.CaptureCadenceSampleCount,
                CaptureCadenceSampleDurationMs = health.CaptureCadenceSampleDurationMs,
                CaptureExpectedFrameRate = health.ExpectedFrameRate,
                CaptureCadenceOnePercentLowFps = health.CaptureCadenceOnePercentLowFps,
                CaptureCadenceP99IntervalMs = health.CaptureCadenceP99IntervalMs,
                CaptureCadenceSevereGapCount = health.CaptureCadenceSevereGapCount,
                CaptureCadenceEstimatedDroppedFrames = health.CaptureCadenceEstimatedDroppedFrames,
                CaptureCadenceEstimatedDropPercent = health.CaptureCadenceEstimatedDropPercent,
                MjpegPipelineSampleCount = health.MjpegPipelineSampleCount,
                MjpegDecodeP95Ms = health.MjpegDecodeP95Ms,
                MjpegPipelineP95Ms = health.MjpegPipelineP95Ms,
                MjpegPipelineMaxMs = health.MjpegPipelineMaxMs,
                RecentMjpegDropped = recentMjpeg.TotalDropped,
                RecentMjpegFailures = recentMjpeg.Failures,
                MjpegPreviewJitterEnabled = health.MjpegPreviewJitterEnabled,
                RecentPreviewJitterDropped = recentPreviewJitter.Dropped,
                RecentPreviewJitterUnderflows = recentPreviewJitter.Underflows,
                RecentPreviewJitterDeadlineDrops = recentPreviewJitter.DeadlineDrops,
                RecentPreviewJitterScheduleLateCount = recentPreviewJitter.ScheduleLateCount,
                RecentPreviewJitterScheduleLateMs = recentPreviewJitter.ScheduleLateMs,
                MjpegPreviewJitterScheduleLateCount = health.MjpegPreviewJitterScheduleLateCount,
                MjpegPreviewJitterMaxScheduleLateMs = health.MjpegPreviewJitterMaxScheduleLateMs,
                MjpegPreviewJitterLatencyP95Ms = health.MjpegPreviewJitterLatencyP95Ms,
                MjpegPreviewJitterLastDropReason = health.MjpegPreviewJitterLastDropReason,
                RecentRendererSubmitted = Math.Max(recentRenderer.Submitted, recentRenderer.Rendered + recentRenderer.Dropped),
                RecentRendererDropped = recentRenderer.Dropped,
                PreviewD3DPendingFrameCount = previewRuntime.D3DPendingFrameCount,
                PreviewD3DInputUploadCpuP99Ms = previewRuntime.D3DInputUploadCpuP99Ms,
                PreviewD3DRenderSubmitCpuP99Ms = previewRuntime.D3DRenderSubmitCpuP99Ms,
                PreviewD3DPresentCallP99Ms = previewRuntime.D3DPresentCallP99Ms,
                PreviewD3DTotalFrameCpuP99Ms = previewRuntime.D3DTotalFrameCpuP99Ms,
                PreviewD3DFrameLatencyWaitP95Ms = previewRuntime.D3DFrameLatencyWaitP95Ms,
                PreviewD3DFrameLatencyWaitMaxMs = previewRuntime.D3DFrameLatencyWaitMaxMs,
                PreviewD3DFrameLatencyWaitTimeoutCount = previewRuntime.D3DFrameLatencyWaitTimeoutCount,
                RecentD3DFrameLatencyWaitTimeoutCount = recentD3DFrameLatencyWaitTimeouts,
                RecentD3DMissedRefreshes = recentD3DMissedRefreshes,
                RecentD3DStatsFailures = recentD3DStatsFailures,
                PreviewD3DLastDropReason = previewRuntime.D3DLastDropReason,
                VisualCadenceSampleCount = health.VisualCadenceSampleCount,
                VisualCadenceChangeObservedFps = health.VisualCadenceChangeObservedFps,
                VisualCadenceRepeatFramePercent = health.VisualCadenceRepeatFramePercent,
                VisualCadenceLongestRepeatRun = health.VisualCadenceLongestRepeatRun,
                VisualCadenceMotionConfidence = health.VisualCadenceMotionConfidence,
                MjpegPacketHashSampleCount = health.MjpegPacketHashSampleCount,
                MjpegPacketHashInputObservedFps = health.MjpegPacketHashInputObservedFps,
                MjpegPacketHashUniqueObservedFps = health.MjpegPacketHashUniqueObservedFps,
                MjpegPacketHashDuplicateFramePercent = health.MjpegPacketHashDuplicateFramePercent
            });
}
