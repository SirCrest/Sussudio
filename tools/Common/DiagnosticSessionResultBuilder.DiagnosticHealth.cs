using System.Collections.Generic;
using System.Text.Json;
using static Sussudio.Tools.DiagnosticSessionHealthPolicy;
using static Sussudio.Tools.DiagnosticSessionHealthTolerances;
using static Sussudio.Tools.DiagnosticSessionMetrics;
using static Sussudio.Tools.DiagnosticSessionText;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
    private static bool AnalyzeDiagnosticHealth(
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement diagnosticHealthSnapshot,
        DiagnosticSessionScenarioPlan scenarioPlan,
        SourceCadenceSessionMetrics sourceCadenceMetrics,
        long sourceReaderFramesDroppedDelta,
        long videoIngestErrorsDelta,
        int durationSeconds,
        DiagnosticSessionPreviewSchedulerAnalysis previewScheduler,
        VisualCadenceSessionMetrics visualCadenceMetrics,
        double expectedCaptureFrameRate,
        List<string> warnings)
    {
        var isFlashbackScenario = scenarioPlan.UsesFlashbackScenarioWarningPolicy;
        var diagnosticHealthObservation = BuildSessionDiagnosticHealthObservation(
            samples,
            diagnosticHealthSnapshot,
            isFlashbackScenario);
        var visualCadenceHealthy = IsVisualCadenceSessionHealthy(visualCadenceMetrics, expectedCaptureFrameRate);
        var sparsePreviewSchedulerDeadlineDropRun = IsSparsePreviewSchedulerDeadlineDropRun(
            previewScheduler.DeadlineDropsDelta,
            previewScheduler.UnderflowsDelta,
            durationSeconds,
            visualCadenceHealthy);
        var sparseSourceCaptureCadenceWarning =
            isFlashbackScenario &&
            IsSparseSourceCaptureCadenceWarningRun(
                diagnosticHealthObservation,
                sourceCadenceMetrics,
                sourceReaderFramesDroppedDelta,
                videoIngestErrorsDelta,
                durationSeconds,
                visualCadenceHealthy);
        var diagnosticHealthTolerated =
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
        var diagnosticHealthSucceeded =
            !IsFailingDiagnosticHealthSeverity(diagnosticHealthObservation.Severity) ||
            diagnosticHealthTolerated;
        if (!diagnosticHealthSucceeded)
        {
            warnings.Add(
                "diagnostic health degraded during session: " +
                $"health={diagnosticHealthObservation.HealthStatus} " +
                $"stage={diagnosticHealthObservation.LikelyStage} " +
                $"offsetMs={diagnosticHealthObservation.OffsetMs} " +
                $"evidence={FormatOptional(diagnosticHealthObservation.Evidence)}");
        }
        else if (diagnosticHealthTolerated &&
                 !sparseSourceCaptureCadenceWarning &&
                 !sparsePreviewSchedulerDeadlineDropRun)
        {
            var toleratedReason = IsPreviewSchedulerDiagnosticHealthObservation(diagnosticHealthObservation)
                ? "preview scheduler transition warning tolerated for preview-cycle scenario"
                : IsFlashbackForceRotateDrainDiagnosticHealthObservation(diagnosticHealthObservation)
                    ? "flashback force-rotate drain warning tolerated for flashback scenario"
                    : "source-signal warning tolerated for export reliability scenario";
            warnings.Add(
                $"diagnostic health {toleratedReason}: " +
                $"health={diagnosticHealthObservation.HealthStatus} " +
                $"stage={diagnosticHealthObservation.LikelyStage} " +
                $"offsetMs={diagnosticHealthObservation.OffsetMs} " +
                $"evidence={FormatOptional(diagnosticHealthObservation.Evidence)}");
        }

        return diagnosticHealthSucceeded;
    }
}
