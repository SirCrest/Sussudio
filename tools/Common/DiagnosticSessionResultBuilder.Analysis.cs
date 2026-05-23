using System.Text.Json;
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
