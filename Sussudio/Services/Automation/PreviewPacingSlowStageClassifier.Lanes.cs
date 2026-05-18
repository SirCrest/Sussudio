using System;

namespace Sussudio.Services.Automation;

public static partial class PreviewPacingSlowStageClassifier
{
    private static bool TryClassifySourceCapture(
        PreviewPacingClassificationInput input,
        bool sourceSampleReady,
        double targetFps,
        out PreviewPacingClassification classification)
    {
        if (IsSourceCaptureSuspect(input, sourceSampleReady, targetFps))
        {
            classification = new PreviewPacingClassification(
                "SourceCapture",
                input.CaptureCadenceEstimatedDroppedFrames > 0 || input.CaptureCadenceSevereGapCount > 0 ? "High" : "Medium",
                Format(
                    "capture1pct={0:0.##}fps p99={1:0.##}ms drops={2} gaps={3} dropPct={4:0.###} preview1pct={5:0.##}fps.",
                    input.CaptureCadenceOnePercentLowFps,
                    input.CaptureCadenceP99IntervalMs,
                    input.CaptureCadenceEstimatedDroppedFrames,
                    input.CaptureCadenceSevereGapCount,
                    input.CaptureCadenceEstimatedDropPercent,
                    input.PreviewCadenceOnePercentLowFps));
            return true;
        }

        classification = default;
        return false;
    }

    private static bool TryClassifyVisualDuplicateOrLowMotion(
        PreviewPacingClassificationInput input,
        double targetFps,
        out PreviewPacingClassification classification)
    {
        if (IsVisualDuplicateOrLowMotionSuspect(input, targetFps))
        {
            classification = new PreviewPacingClassification(
                "VisualDuplicateOrLowMotion",
                "Medium",
                Format(
                    "visualChange={0:0.##}fps repeat={1:0.###}% longestRun={2} confidence={3}; mjpgInput={4:0.##}fps unique={5:0.##}fps dup={6:0.###}%.",
                    input.VisualCadenceChangeObservedFps,
                    input.VisualCadenceRepeatFramePercent,
                    input.VisualCadenceLongestRepeatRun,
                    string.IsNullOrWhiteSpace(input.VisualCadenceMotionConfidence) ? "Unknown" : input.VisualCadenceMotionConfidence,
                    input.MjpegPacketHashInputObservedFps,
                    input.MjpegPacketHashUniqueObservedFps,
                    input.MjpegPacketHashDuplicateFramePercent));
            return true;
        }

        classification = default;
        return false;
    }

    private static bool TryClassifyMjpegDecode(
        PreviewPacingClassificationInput input,
        double targetFrameMs,
        out PreviewPacingClassification classification)
    {
        if (IsMjpegDecodeSuspect(input, targetFrameMs))
        {
            classification = new PreviewPacingClassification(
                "MjpegDecode",
                input.RecentMjpegDropped > 0 || input.RecentMjpegFailures > 0 ? "High" : "Medium",
                Format(
                    "mjpegDecodeP95={0:0.##}ms pipelineP95={1:0.##}ms pipelineMax={2:0.##}ms recentDropped={3} recentFailures={4}.",
                    input.MjpegDecodeP95Ms,
                    input.MjpegPipelineP95Ms,
                    input.MjpegPipelineMaxMs,
                    input.RecentMjpegDropped,
                    input.RecentMjpegFailures));
            return true;
        }

        classification = default;
        return false;
    }

    private static bool TryClassifyPreviewJitterScheduler(
        PreviewPacingClassificationInput input,
        double targetFrameMs,
        out PreviewPacingClassification classification)
    {
        if (IsPreviewJitterSuspect(input, targetFrameMs))
        {
            classification = new PreviewPacingClassification(
                "PreviewJitterScheduler",
                input.RecentPreviewJitterDeadlineDrops > 0 ||
                input.RecentPreviewJitterDropped > 0
                    ? "High"
                    : "Medium",
                Format(
                    "jitter recentDrops={0} deadlineDrops={1} underflows={2} recentScheduleLate={3}/{4:0.##}ms lifetimeScheduleLate={5} maxScheduleLate={6:0.##}ms latencyP95={7:0.##}ms lastDrop={8}.",
                    input.RecentPreviewJitterDropped,
                    input.RecentPreviewJitterDeadlineDrops,
                    input.RecentPreviewJitterUnderflows,
                    input.RecentPreviewJitterScheduleLateCount,
                    input.RecentPreviewJitterScheduleLateMs,
                    input.MjpegPreviewJitterScheduleLateCount,
                    input.MjpegPreviewJitterMaxScheduleLateMs,
                    input.MjpegPreviewJitterLatencyP95Ms,
                    string.IsNullOrWhiteSpace(input.MjpegPreviewJitterLastDropReason) ? "none" : input.MjpegPreviewJitterLastDropReason));
            return true;
        }

        classification = default;
        return false;
    }

    private static bool TryClassifyCompositorMiss(
        PreviewPacingClassificationInput input,
        double targetFrameMs,
        out PreviewPacingClassification classification)
    {
        if (input.RecentD3DMissedRefreshes > 0 || input.RecentD3DStatsFailures > 0)
        {
            classification = new PreviewPacingClassification(
                "CompositorMiss",
                "High",
                Format(
                    "dxgiRecentMissed={0} dxgiRecentFailures={1} previewP99={2:0.##}ms target={3:0.###}ms.",
                    input.RecentD3DMissedRefreshes,
                    input.RecentD3DStatsFailures,
                    input.PreviewCadenceP99IntervalMs,
                    targetFrameMs));
            return true;
        }

        classification = default;
        return false;
    }

    private static bool TryClassifyRenderSubmit(
        PreviewPacingClassificationInput input,
        out PreviewPacingClassification classification)
    {
        var rendererDropPercent = CalculatePercent(input.RecentRendererDropped, input.RecentRendererSubmitted);
        if (rendererDropPercent > RendererDropWarningPercent ||
            input.PreviewD3DPendingFrameCount > 1 ||
            input.RecentRendererDropped > 0 && !string.IsNullOrWhiteSpace(input.PreviewD3DLastDropReason))
        {
            classification = new PreviewPacingClassification(
                "RenderSubmit",
                rendererDropPercent > RendererDropWarningPercent ? "High" : "Medium",
                Format(
                    "rendererDropped={0}/{1} ({2:0.###}%) pending={3} lastDrop={4}.",
                    input.RecentRendererDropped,
                    input.RecentRendererSubmitted,
                    rendererDropPercent,
                    input.PreviewD3DPendingFrameCount,
                    string.IsNullOrWhiteSpace(input.PreviewD3DLastDropReason) ? "none" : input.PreviewD3DLastDropReason));
            return true;
        }

        classification = default;
        return false;
    }

    private static bool IsSourceCaptureSuspect(
        PreviewPacingClassificationInput input,
        bool sourceSampleReady,
        double targetFps)
    {
        if (input.CaptureCadenceEstimatedDroppedFrames > 0 ||
            input.CaptureCadenceSevereGapCount > 0 ||
            input.CaptureCadenceEstimatedDropPercent > 0.1)
        {
            return true;
        }

        var sourceTarget = input.CaptureExpectedFrameRate > 0 ? input.CaptureExpectedFrameRate : targetFps;
        return sourceSampleReady && IsOnePercentLowDegraded(input.CaptureCadenceOnePercentLowFps, sourceTarget);
    }

    private static bool IsVisualDuplicateOrLowMotionSuspect(
        PreviewPacingClassificationInput input,
        double targetFps)
    {
        var visualReady = input.VisualCadenceSampleCount >= Math.Max(60, (int)Math.Round(targetFps));
        if (visualReady &&
            input.VisualCadenceChangeObservedFps > 0 &&
            input.VisualCadenceChangeObservedFps < targetFps * 0.90 &&
            (input.VisualCadenceRepeatFramePercent >= VisualRepeatWarningPercent ||
             input.VisualCadenceLongestRepeatRun > 2))
        {
            return true;
        }

        return input.MjpegPacketHashSampleCount >= Math.Max(60, (int)Math.Round(targetFps)) &&
               input.MjpegPacketHashInputObservedFps >= targetFps * 0.90 &&
               input.MjpegPacketHashUniqueObservedFps > 0 &&
               input.MjpegPacketHashUniqueObservedFps < targetFps * 0.90 &&
               input.MjpegPacketHashDuplicateFramePercent >= MjpegDuplicateWarningPercent;
    }

    private static bool IsMjpegDecodeSuspect(PreviewPacingClassificationInput input, double targetFrameMs)
    {
        if (input.RecentMjpegDropped > 0 || input.RecentMjpegFailures > 0)
        {
            return true;
        }

        if (input.MjpegPipelineSampleCount <= 0)
        {
            return false;
        }

        return input.MjpegPipelineP95Ms > targetFrameMs * 0.90 ||
               input.MjpegDecodeP95Ms > targetFrameMs * 0.65 ||
               input.MjpegPipelineMaxMs > targetFrameMs * 1.50;
    }

    private static bool IsPreviewJitterSuspect(PreviewPacingClassificationInput input, double targetFrameMs)
    {
        if (!input.MjpegPreviewJitterEnabled)
        {
            return false;
        }

        if (input.RecentPreviewJitterDropped > 0 ||
            input.RecentPreviewJitterDeadlineDrops > 0 ||
            input.RecentPreviewJitterUnderflows > 3)
        {
            return true;
        }

        return input.RecentPreviewJitterScheduleLateCount > 0 &&
               input.RecentPreviewJitterScheduleLateMs > Math.Max(1.0, targetFrameMs * 0.25);
    }

    private static double CalculatePercent(long count, long total)
        => total > 0 ? Math.Max(0.0, count) * 100.0 / total : 0.0;
}
