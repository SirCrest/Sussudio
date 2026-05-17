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
}
