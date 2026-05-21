using System.Collections.Generic;
using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionHealthPolicy;
using static Sussudio.Tools.DiagnosticSessionHealthTolerances;
using static Sussudio.Tools.DiagnosticSessionMetrics;
using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
    private readonly record struct DiagnosticSessionHealthSummary(
        JsonElement Snapshot,
        string HealthStatus,
        string LikelyStage,
        string Summary,
        string Evidence);

    private static DiagnosticSessionHealthSummary BuildDiagnosticHealthSummary(
        DiagnosticSessionResultBuildRequest request,
        JsonElement lastSnapshot)
    {
        var diagnosticHealthSnapshot = request.StoppedRecordingForVerification
            ? lastSnapshot
            : request.HealthSnapshot;

        return new DiagnosticSessionHealthSummary(
            Snapshot: diagnosticHealthSnapshot,
            HealthStatus: GetString(diagnosticHealthSnapshot, "DiagnosticHealthStatus") ?? "Unknown",
            LikelyStage: GetString(diagnosticHealthSnapshot, "DiagnosticLikelyStage") ?? "diagnostic_unavailable",
            Summary: GetString(diagnosticHealthSnapshot, "DiagnosticSummary") ?? string.Empty,
            Evidence: GetString(diagnosticHealthSnapshot, "DiagnosticEvidence") ?? string.Empty);
    }

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

    private readonly record struct DiagnosticHealthSourceWarningCounters(
        long SourceReaderFramesDroppedDelta,
        long VideoIngestErrorsDelta);

    private static DiagnosticHealthSourceWarningCounters BuildDiagnosticHealthSourceWarningCounters(
        JsonElement initialSnapshot,
        JsonElement lastSnapshot)
    {
        return new DiagnosticHealthSourceWarningCounters(
            SourceReaderFramesDroppedDelta: GetCounterDelta(lastSnapshot, initialSnapshot, "MfSourceReaderFramesDropped"),
            VideoIngestErrorsDelta: GetCounterDelta(lastSnapshot, initialSnapshot, "VideoIngestErrorCount"));
    }
}
