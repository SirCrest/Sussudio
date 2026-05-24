using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackValidation;
using static Sussudio.Tools.DiagnosticSessionHealthTolerances;
using static Sussudio.Tools.DiagnosticSessionFlashbackMetrics;
using static Sussudio.Tools.DiagnosticSessionMetrics;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
    private sealed record DiagnosticSessionResultAnalysis(
        JsonElement LastSnapshot,
        DiagnosticSessionHealthSummary HealthSummary,
        FlashbackPlaybackSessionMetrics PlaybackSessionMetrics,
        FlashbackPlaybackResultMetrics PlaybackResultMetrics,
        FlashbackRecordingSessionMetrics RecordingMetrics,
        FlashbackExportSessionMetrics ExportMetrics,
        PreviewCadenceSessionMetrics PreviewCadenceMetrics,
        PreviewD3DMetrics PreviewD3DMetrics,
        VisualCadenceSessionMetrics VisualCadenceMetrics,
        DiagnosticSessionPreviewSchedulerAnalysis PreviewScheduler,
        bool DiagnosticHealthSucceeded,
        bool FlashbackWarningsSucceeded);

    private readonly record struct DiagnosticSessionAnalysisValidationOutcome(
        bool DiagnosticHealthSucceeded,
        bool FlashbackWarningsSucceeded);

    private static DiagnosticSessionResultAnalysis Analyze(DiagnosticSessionResultBuildRequest request)
    {
        var samples = request.Samples;
        var initialSnapshot = request.InitialSnapshot;
        var lastSnapshot = samples.Count > 0
            ? samples[^1].Snapshot
            : initialSnapshot;
        var healthSnapshot = request.HealthSnapshot;
        var warnings = request.Warnings;

        var healthSummary = BuildDiagnosticHealthSummary(request, lastSnapshot);
        var playbackSessionMetrics = BuildFlashbackPlaybackSessionMetrics(initialSnapshot, samples, lastSnapshot);
        var playbackResultMetrics = BuildFlashbackPlaybackResultMetrics(playbackSessionMetrics);
        AddFlashbackPlaybackAnalysisWarnings(playbackResultMetrics, warnings);

        var exportMetrics = BuildFlashbackExportSessionMetrics(initialSnapshot, samples, lastSnapshot);
        AddFlashbackExportAnalysisWarnings(
            exportMetrics.ForceRotateFallbacksAtEnd,
            exportMetrics.ForceRotateFallbacksDelta,
            exportMetrics.LastForceRotateFallbackSegmentsAtEnd,
            warnings);

        var recordingMetrics = BuildFlashbackRecordingMetrics(initialSnapshot, samples);
        var sourceCadenceMetrics = BuildSourceCadenceSessionMetrics(samples, lastSnapshot);
        var previewCadenceMetrics = BuildPreviewCadenceSessionMetrics(samples, lastSnapshot);
        var previewD3DMetrics = BuildPreviewD3DMetrics(initialSnapshot, lastSnapshot, samples);
        var visualCadenceMetrics = BuildVisualCadenceSessionMetrics(samples, lastSnapshot);
        var previewScheduler = BuildPreviewSchedulerAnalysis(initialSnapshot, lastSnapshot, samples);
        var validationOutcome = ValidateAnalysis(
            request,
            initialSnapshot,
            lastSnapshot,
            healthSnapshot,
            healthSummary.Snapshot,
            playbackSessionMetrics,
            playbackResultMetrics,
            sourceCadenceMetrics,
            previewCadenceMetrics,
            previewD3DMetrics,
            visualCadenceMetrics,
            previewScheduler);

        return new DiagnosticSessionResultAnalysis(
            lastSnapshot,
            healthSummary,
            playbackSessionMetrics,
            playbackResultMetrics,
            recordingMetrics,
            exportMetrics,
            previewCadenceMetrics,
            previewD3DMetrics,
            visualCadenceMetrics,
            previewScheduler,
            validationOutcome.DiagnosticHealthSucceeded,
            validationOutcome.FlashbackWarningsSucceeded);
    }

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

        return new DiagnosticSessionAnalysisValidationOutcome(
            DiagnosticHealthSucceeded: diagnosticHealthSucceeded,
            FlashbackWarningsSucceeded: EvaluateFlashbackWarningsSucceeded(request.ScenarioPlan, warnings));
    }

    private static void ValidateCleanupLifecycleRestored(
        bool leaveRunning,
        bool startedPreview,
        bool enabledFlashback,
        bool startedFlashbackPlayback,
        JsonElement initialSnapshot,
        JsonElement finalSnapshot,
        List<string> warnings)
    {
        if (leaveRunning)
        {
            return;
        }

        if (startedPreview &&
            !GetBool(initialSnapshot, "IsPreviewing") &&
            GetBool(finalSnapshot, "IsPreviewing"))
        {
            warnings.Add("cleanup: preview remained active after restore");
        }

        if (enabledFlashback &&
            !GetBool(initialSnapshot, "FlashbackActive") &&
            GetBool(finalSnapshot, "FlashbackActive"))
        {
            warnings.Add("cleanup: Flashback remained active after restore");
        }

        if (startedFlashbackPlayback)
        {
            var state = GetString(finalSnapshot, "FlashbackPlaybackState") ?? "Unknown";
            if (!string.Equals(state, "Live", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"cleanup: playback did not return live state={state}");
            }
        }
    }

    private static void AddFlashbackPlaybackAnalysisWarnings(
        FlashbackPlaybackResultMetrics playbackResultMetrics,
        List<string> warnings)
    {
        if (playbackResultMetrics.SeekForwardDecodeCapHitsDelta <= 0)
        {
            return;
        }

        warnings.Add(
            "flashback playback seek forward-decode cap hit during session " +
            $"delta={playbackResultMetrics.SeekForwardDecodeCapHitsDelta} " +
            $"total={playbackResultMetrics.SeekForwardDecodeCapHitsAtEnd}");
    }

    private static void AddFlashbackExportAnalysisWarnings(
        long flashbackExportForceRotateFallbacksAtEnd,
        long flashbackExportForceRotateFallbacksDelta,
        int flashbackExportLastForceRotateFallbackSegmentsAtEnd,
        List<string> warnings)
    {
        if (flashbackExportForceRotateFallbacksDelta <= 0)
        {
            return;
        }

        warnings.Add(
            "flashback export used force-rotate partial fallback " +
            $"delta={flashbackExportForceRotateFallbacksDelta} total={flashbackExportForceRotateFallbacksAtEnd} " +
            $"segments={flashbackExportLastForceRotateFallbackSegmentsAtEnd}");
    }

    private static bool EvaluateFlashbackWarningsSucceeded(
        DiagnosticSessionScenarioPlan scenarioPlan,
        List<string> warnings)
    {
        if (!scenarioPlan.UsesFlashbackScenarioWarningPolicy)
        {
            return true;
        }

        return warnings.All(warning => IsToleratedFlashbackScenarioWarning(
            warning,
            scenarioPlan.ToleratesSourceSignalHealthWarning,
            scenarioPlan.ToleratesFlashbackForceRotateDrainWarning,
            scenarioPlan.IsPreviewCycleScenario));
    }
}
