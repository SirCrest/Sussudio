using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static DiagnosticEvaluation? TryBuildRealtimePreviewDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        PreviewRuntimeSnapshot previewRuntime,
        bool visualCadenceHealthy,
        long recentPreviewUnderflows,
        long recentPreviewDeadlineDrops,
        DiagnosticEvaluationLanes lanes)
    {
        return TryBuildRealtimePreviewSchedulerDiagnosticEvaluation(
                   health,
                   visualCadenceHealthy,
                   recentPreviewUnderflows,
                   recentPreviewDeadlineDrops,
                   lanes) ??
               TryBuildRealtimePreviewRendererDiagnosticEvaluation(lanes) ??
               TryBuildRealtimePreviewPresentDiagnosticEvaluation(
                   previewRuntime,
                   visualCadenceHealthy,
                   lanes);
    }

    private static DiagnosticEvaluation? TryBuildRealtimePreviewRendererDiagnosticEvaluation(
        DiagnosticEvaluationLanes lanes)
    {
        var recentRendererSubmitted = lanes.RecentRendererSubmitted;
        var recentRendererDropPercent = lanes.RecentRendererDropPercent;
        if (recentRendererSubmitted < DiagnosticThresholds.RendererDropWarningMinSamples ||
            recentRendererDropPercent <= DiagnosticThresholds.RendererDropWarningPercent)
        {
            return null;
        }

        return new DiagnosticEvaluation(
            "Warning",
            "renderer",
            "Renderer pacing is the likely preview bottleneck.",
            lanes.Render,
            lanes.Source,
            lanes.Decode,
            lanes.Preview,
            lanes.Render,
            lanes.Present,
            lanes.Recording,
            lanes.Audio);
    }
}
