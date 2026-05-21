using System.Text.Json;
using static Sussudio.Tools.DiagnosticSessionHealthTolerances;
using static Sussudio.Tools.DiagnosticSessionMetrics;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
    private readonly record struct DiagnosticSessionHealthToleranceVerdict(
        bool IsTolerated,
        bool SparseSourceCaptureCadenceWarning,
        bool SparsePreviewSchedulerDeadlineDropRun,
        string WarningReason);

    private static DiagnosticSessionHealthToleranceVerdict BuildDiagnosticHealthToleranceVerdict(
        JsonElement initialSnapshot,
        JsonElement lastSnapshot,
        DiagnosticHealthObservation diagnosticHealthObservation,
        DiagnosticSessionScenarioPlan scenarioPlan,
        SourceCadenceSessionMetrics sourceCadenceMetrics,
        int durationSeconds,
        DiagnosticSessionPreviewSchedulerAnalysis previewScheduler,
        VisualCadenceSessionMetrics visualCadenceMetrics,
        double expectedCaptureFrameRate)
    {
        var isFlashbackScenario = scenarioPlan.UsesFlashbackScenarioWarningPolicy;
        var visualCadenceHealthy = IsVisualCadenceSessionHealthy(visualCadenceMetrics, expectedCaptureFrameRate);
        var sparsePreviewSchedulerDeadlineDropRun = IsSparsePreviewSchedulerDeadlineDropRun(
            previewScheduler.DeadlineDropsDelta,
            previewScheduler.UnderflowsDelta,
            durationSeconds,
            visualCadenceHealthy);
        var sourceWarningCounters = BuildDiagnosticHealthSourceWarningCounters(initialSnapshot, lastSnapshot);
        var sparseSourceCaptureCadenceWarning =
            isFlashbackScenario &&
            IsSparseSourceCaptureCadenceWarningRun(
                diagnosticHealthObservation,
                sourceCadenceMetrics,
                sourceWarningCounters.SourceReaderFramesDroppedDelta,
                sourceWarningCounters.VideoIngestErrorsDelta,
                durationSeconds,
                visualCadenceHealthy);
        var tolerated =
            (scenarioPlan.ToleratesSourceSignalHealthWarning &&
             IsSourceSignalDiagnosticHealthObservation(diagnosticHealthObservation)) ||
            (scenarioPlan.ToleratesFlashbackForceRotateDrainWarning &&
             IsFlashbackForceRotateDrainDiagnosticHealthObservation(diagnosticHealthObservation)) ||
            sparseSourceCaptureCadenceWarning ||
            (isFlashbackScenario &&
             scenarioPlan.IsPreviewCycleScenario &&
             visualCadenceHealthy &&
             IsPreviewSchedulerDiagnosticHealthObservation(diagnosticHealthObservation)) ||
            (isFlashbackScenario &&
             sparsePreviewSchedulerDeadlineDropRun &&
             IsPreviewSchedulerDiagnosticHealthObservation(diagnosticHealthObservation));
        var warningReason =
            IsPreviewSchedulerDiagnosticHealthObservation(diagnosticHealthObservation)
                ? "preview scheduler transition warning tolerated for preview-cycle scenario"
                : IsFlashbackForceRotateDrainDiagnosticHealthObservation(diagnosticHealthObservation)
                    ? "flashback force-rotate drain warning tolerated for flashback scenario"
                    : "source-signal warning tolerated for export reliability scenario";

        return new DiagnosticSessionHealthToleranceVerdict(
            tolerated,
            sparseSourceCaptureCadenceWarning,
            sparsePreviewSchedulerDeadlineDropRun,
            warningReason);
    }
}
