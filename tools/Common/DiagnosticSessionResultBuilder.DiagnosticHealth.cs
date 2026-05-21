using System.Collections.Generic;
using System.Text.Json;
using static Sussudio.Tools.DiagnosticSessionHealthPolicy;
using static Sussudio.Tools.DiagnosticSessionMetrics;
using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
    private static bool AnalyzeDiagnosticHealth(
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement initialSnapshot,
        JsonElement lastSnapshot,
        JsonElement diagnosticHealthSnapshot,
        DiagnosticSessionScenarioPlan scenarioPlan,
        SourceCadenceSessionMetrics sourceCadenceMetrics,
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
        var tolerance = BuildDiagnosticHealthToleranceVerdict(
            initialSnapshot,
            lastSnapshot,
            diagnosticHealthObservation,
            scenarioPlan,
            sourceCadenceMetrics,
            durationSeconds,
            previewScheduler,
            visualCadenceMetrics,
            expectedCaptureFrameRate);
        var diagnosticHealthSucceeded =
            !IsFailingDiagnosticHealthSeverity(diagnosticHealthObservation.Severity) ||
            tolerance.IsTolerated;
        if (!diagnosticHealthSucceeded)
        {
            warnings.Add(
                "diagnostic health degraded during session: " +
                $"health={diagnosticHealthObservation.HealthStatus} " +
                $"stage={diagnosticHealthObservation.LikelyStage} " +
                $"offsetMs={diagnosticHealthObservation.OffsetMs} " +
                $"evidence={FormatOptional(diagnosticHealthObservation.Evidence)}");
        }
        else if (tolerance.IsTolerated &&
                 !tolerance.SparseSourceCaptureCadenceWarning &&
                 !tolerance.SparsePreviewSchedulerDeadlineDropRun)
        {
            warnings.Add(
                $"diagnostic health {tolerance.WarningReason}: " +
                $"health={diagnosticHealthObservation.HealthStatus} " +
                $"stage={diagnosticHealthObservation.LikelyStage} " +
                $"offsetMs={diagnosticHealthObservation.OffsetMs} " +
                $"evidence={FormatOptional(diagnosticHealthObservation.Evidence)}");
        }

        return diagnosticHealthSucceeded;
    }
}
