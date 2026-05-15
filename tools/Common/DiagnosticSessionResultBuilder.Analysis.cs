using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionCleanupPolicy;
using static Sussudio.Tools.DiagnosticSessionFlashbackMetrics;
using static Sussudio.Tools.DiagnosticSessionFlashbackValidation;
using static Sussudio.Tools.DiagnosticSessionHealthTolerances;
using static Sussudio.Tools.DiagnosticSessionMetrics;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
    private static DiagnosticSessionResultAnalysis Analyze(DiagnosticSessionResultBuildRequest request)
    {
        var samples = request.Samples;
        var initialSnapshot = request.InitialSnapshot;
        var lastSnapshot = samples.Count > 0
            ? samples[^1].Snapshot
            : initialSnapshot;
        var healthSnapshot = request.HealthSnapshot;
        var warnings = request.Warnings;

        var diagnosticHealthSnapshot = request.StoppedRecordingForVerification
            ? lastSnapshot
            : healthSnapshot;
        var healthStatus = GetString(diagnosticHealthSnapshot, "DiagnosticHealthStatus") ?? "Unknown";
        var likelyStage = GetString(diagnosticHealthSnapshot, "DiagnosticLikelyStage") ?? "diagnostic_unavailable";
        var summary = GetString(diagnosticHealthSnapshot, "DiagnosticSummary") ?? string.Empty;
        var evidence = GetString(diagnosticHealthSnapshot, "DiagnosticEvidence") ?? string.Empty;
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
        if (request.ScenarioPlan.RunFlashbackPlayback)
        {
            ValidateFlashbackPlaybackSession(
                playbackSessionMetrics.Observed ? playbackResultMetrics.EndSnapshot : lastSnapshot,
                playbackSessionMetrics,
                visualCadenceMetrics,
                request.DurationSeconds,
                warnings);
        }

        var sourceReaderFramesDroppedDelta = GetCounterDelta(lastSnapshot, initialSnapshot, "MfSourceReaderFramesDropped");
        var videoIngestErrorsDelta = GetCounterDelta(lastSnapshot, initialSnapshot, "VideoIngestErrorCount");
        var previewScheduler = BuildPreviewSchedulerAnalysis(initialSnapshot, lastSnapshot, samples);
        var isFlashbackScenario = request.ScenarioPlan.UsesFlashbackScenarioWarningPolicy;
        ValidateCleanupLifecycleRestored(
            request.Options.LeaveRunning,
            request.StartedPreview,
            request.EnabledFlashback,
            request.StartedFlashbackPlayback,
            initialSnapshot,
            healthSnapshot,
            warnings);
        var toleratesSourceSignalHealthWarning = request.ScenarioPlan.ToleratesSourceSignalHealthWarning;
        if (isFlashbackScenario)
        {
            var previewTargetFps = GetDouble(lastSnapshot, "ExpectedCaptureFrameRate");
            if (previewTargetFps <= 0)
            {
                previewTargetFps = GetDouble(lastSnapshot, "SelectedExactFrameRate");
            }

            var visualCadenceHealthy = IsVisualCadenceSessionHealthy(visualCadenceMetrics, previewTargetFps);
            var toleratesPreviewCycleSchedulerSettling =
                request.ScenarioPlan.IsPreviewCycleScenario && visualCadenceHealthy;
            var toleratesSparsePreviewSchedulerDeadlineDrops =
                IsSparsePreviewSchedulerDeadlineDropRun(
                    previewScheduler.DeadlineDropsDelta,
                    previewScheduler.UnderflowsDelta,
                    request.DurationSeconds,
                    visualCadenceHealthy);
            var toleratesSparseScrubSchedulerTransitions =
                request.ScenarioPlan.ToleratesSparsePreviewSchedulerStressTransitions &&
                IsSparsePreviewSchedulerStressRun(
                    previewScheduler.DeadlineDropsDelta,
                    previewScheduler.UnderflowsDelta,
                    request.DurationSeconds,
                    visualCadenceHealthy);
            ValidateFlashbackPreviewScheduler(
                previewScheduler.DeadlineDropsDelta,
                previewScheduler.UnderflowsDelta,
                previewD3DMetrics.StatsFailureDelta,
                previewCadenceMetrics,
                visualCadenceMetrics,
                previewD3DMetrics,
                previewTargetFps,
                toleratesPreviewCycleSchedulerSettling ||
                    toleratesSparsePreviewSchedulerDeadlineDrops ||
                    toleratesSparseScrubSchedulerTransitions,
                warnings);
        }

        var toleratesFlashbackForceRotateDrainWarning = request.ScenarioPlan.ToleratesFlashbackForceRotateDrainWarning;
        var diagnosticHealthSucceeded = AnalyzeDiagnosticHealth(
            samples,
            diagnosticHealthSnapshot,
            request.ScenarioPlan,
            sourceCadenceMetrics,
            sourceReaderFramesDroppedDelta,
            videoIngestErrorsDelta,
            request.DurationSeconds,
            previewScheduler,
            visualCadenceMetrics,
            GetDouble(lastSnapshot, "ExpectedCaptureFrameRate"),
            warnings);

        var flashbackWarningsSucceeded = !isFlashbackScenario ||
                                         warnings.All(warning => IsToleratedFlashbackScenarioWarning(
                                             warning,
                                             toleratesSourceSignalHealthWarning,
                                             toleratesFlashbackForceRotateDrainWarning,
                                             request.ScenarioPlan.IsPreviewCycleScenario));

        var processCpuMaxPercentObserved = samples
            .Select(sample => GetDouble(sample.Snapshot, "ProcessCpuPercent"))
            .Append(GetDouble(lastSnapshot, "ProcessCpuPercent"))
            .DefaultIfEmpty(0.0)
            .Max();

        return new DiagnosticSessionResultAnalysis(
            lastSnapshot,
            healthStatus,
            likelyStage,
            summary,
            evidence,
            playbackSessionMetrics,
            playbackResultMetrics,
            recordingMetrics,
            exportMetrics,
            previewCadenceMetrics,
            previewD3DMetrics,
            visualCadenceMetrics,
            previewScheduler,
            diagnosticHealthSucceeded,
            flashbackWarningsSucceeded,
            processCpuMaxPercentObserved);
    }
}
