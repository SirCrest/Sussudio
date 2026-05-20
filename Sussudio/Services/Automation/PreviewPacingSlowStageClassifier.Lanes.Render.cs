using System;

namespace Sussudio.Services.Automation;

public static partial class PreviewPacingSlowStageClassifier
{
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

    private static double CalculatePercent(long count, long total)
        => total > 0 ? Math.Max(0.0, count) * 100.0 / total : 0.0;
}
