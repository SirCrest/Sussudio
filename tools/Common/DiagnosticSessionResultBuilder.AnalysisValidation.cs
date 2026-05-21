using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionCleanupPolicy;
using static Sussudio.Tools.DiagnosticSessionFlashbackValidation;
using static Sussudio.Tools.DiagnosticSessionHealthTolerances;
using static Sussudio.Tools.DiagnosticSessionMetrics;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
    private readonly record struct DiagnosticSessionAnalysisValidationOutcome(
        bool DiagnosticHealthSucceeded,
        bool FlashbackWarningsSucceeded);

    private static DiagnosticSessionAnalysisValidationOutcome ValidateAnalysis(
        DiagnosticSessionResultBuildRequest request,
        JsonElement initialSnapshot,
        JsonElement lastSnapshot,
        JsonElement healthSnapshot,
        JsonElement diagnosticHealthSnapshot,
        FlashbackPlaybackSessionMetrics playbackSessionMetrics,
        FlashbackPlaybackResultMetrics playbackResultMetrics,
        SourceCadenceSessionMetrics sourceCadenceMetrics,
        PreviewCadenceSessionMetrics previewCadenceMetrics,
        PreviewD3DMetrics previewD3DMetrics,
        VisualCadenceSessionMetrics visualCadenceMetrics,
        DiagnosticSessionPreviewSchedulerAnalysis previewScheduler)
    {
        var warnings = request.Warnings;
        if (request.ScenarioPlan.RunFlashbackPlayback)
        {
            ValidateFlashbackPlaybackSession(
                playbackSessionMetrics.Observed ? playbackResultMetrics.EndSnapshot : lastSnapshot,
                playbackSessionMetrics,
                visualCadenceMetrics,
                request.DurationSeconds,
                warnings);
        }

        ValidateCleanupLifecycleRestored(
            request.Options.LeaveRunning,
            request.StartedPreview,
            request.EnabledFlashback,
            request.StartedFlashbackPlayback,
            initialSnapshot,
            healthSnapshot,
            warnings);
        ValidateFlashbackPreviewSchedulerAnalysis(
            request.ScenarioPlan,
            lastSnapshot,
            request.DurationSeconds,
            previewScheduler,
            previewCadenceMetrics,
            visualCadenceMetrics,
            previewD3DMetrics,
            warnings);

        var diagnosticHealthSucceeded = AnalyzeDiagnosticHealth(
            request.Samples,
            initialSnapshot,
            lastSnapshot,
            diagnosticHealthSnapshot,
            request.ScenarioPlan,
            sourceCadenceMetrics,
            request.DurationSeconds,
            previewScheduler,
            visualCadenceMetrics,
            GetDouble(lastSnapshot, "ExpectedCaptureFrameRate"),
            warnings);

        var isFlashbackScenario = request.ScenarioPlan.UsesFlashbackScenarioWarningPolicy;
        var toleratesSourceSignalHealthWarning = request.ScenarioPlan.ToleratesSourceSignalHealthWarning;
        var toleratesFlashbackForceRotateDrainWarning = request.ScenarioPlan.ToleratesFlashbackForceRotateDrainWarning;
        var flashbackWarningsSucceeded = !isFlashbackScenario ||
                                         warnings.All(warning => IsToleratedFlashbackScenarioWarning(
                                             warning,
                                             toleratesSourceSignalHealthWarning,
                                             toleratesFlashbackForceRotateDrainWarning,
                                             request.ScenarioPlan.IsPreviewCycleScenario));

        return new DiagnosticSessionAnalysisValidationOutcome(
            DiagnosticHealthSucceeded: diagnosticHealthSucceeded,
            FlashbackWarningsSucceeded: flashbackWarningsSucceeded);
    }
}
