using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static DiagnosticEvaluation? TryBuildRealtimePreviewSchedulerDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        bool visualCadenceHealthy,
        long recentPreviewUnderflows,
        long recentPreviewDeadlineDrops,
        DiagnosticEvaluationLanes lanes)
    {
        var previewSubmitFailed = string.Equals(
            health.MjpegPreviewJitterLastDropReason,
            "submit-failed",
            StringComparison.OrdinalIgnoreCase);
        if (!previewSubmitFailed &&
            (recentPreviewDeadlineDrops <= 0 || visualCadenceHealthy) &&
            recentPreviewUnderflows <= 3)
        {
            return null;
        }

        return new DiagnosticEvaluation(
            "Warning",
            "preview_scheduler",
            previewSubmitFailed
                ? "Preview scheduler failed to submit frames."
                : "Preview scheduler is skipping stale or missing frames.",
            lanes.Preview,
            lanes.Source,
            lanes.Decode,
            lanes.Preview,
            lanes.Render,
            lanes.Present,
            lanes.Recording,
            lanes.Audio);
    }
}
