using System;

namespace Sussudio.Services.Automation;

public static partial class PreviewPacingSlowStageClassifier
{
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
}
