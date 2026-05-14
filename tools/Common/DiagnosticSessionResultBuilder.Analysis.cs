using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionCleanupPolicy;
using static Sussudio.Tools.DiagnosticSessionFlashbackMetrics;
using static Sussudio.Tools.DiagnosticSessionFlashbackValidation;
using static Sussudio.Tools.DiagnosticSessionHealthPolicy;
using static Sussudio.Tools.DiagnosticSessionMetrics;
using static Sussudio.Tools.DiagnosticSessionText;

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
        if (playbackResultMetrics.SeekForwardDecodeCapHitsDelta > 0)
        {
            warnings.Add(
                "flashback playback seek forward-decode cap hit during session " +
                $"delta={playbackResultMetrics.SeekForwardDecodeCapHitsDelta} " +
                $"total={playbackResultMetrics.SeekForwardDecodeCapHitsAtEnd}");
        }

        var flashbackExportForceRotateFallbacksAtEnd = GetNullableLong(lastSnapshot, "FlashbackExportForceRotateFallbacks") ?? 0;
        var flashbackExportForceRotateFallbacksDelta = GetCounterDelta(lastSnapshot, initialSnapshot, "FlashbackExportForceRotateFallbacks");
        var flashbackExportLastForceRotateFallbackSegmentsAtEnd = GetInt(lastSnapshot, "FlashbackExportLastForceRotateFallbackSegments");
        if (flashbackExportForceRotateFallbacksDelta > 0)
        {
            warnings.Add(
                "flashback export used force-rotate partial fallback " +
                $"delta={flashbackExportForceRotateFallbacksDelta} total={flashbackExportForceRotateFallbacksAtEnd} " +
                $"segments={flashbackExportLastForceRotateFallbackSegmentsAtEnd}");
        }

        var recordingMetrics = BuildFlashbackRecordingMetrics(initialSnapshot, samples);
        var exportMetrics = BuildFlashbackExportSessionMetrics(initialSnapshot, samples, lastSnapshot);
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
        var previewSchedulerDroppedAtEnd = GetNullableLong(lastSnapshot, "MjpegPreviewJitterTotalDropped") ?? 0;
        var previewSchedulerDeadlineDropsAtEnd = GetNullableLong(lastSnapshot, "MjpegPreviewJitterDeadlineDropCount") ?? 0;
        var previewSchedulerClearedDropsAtEnd = GetNullableLong(lastSnapshot, "MjpegPreviewJitterClearedDropCount") ?? 0;
        var previewSchedulerUnderflowsAtEnd = GetNullableLong(lastSnapshot, "MjpegPreviewJitterUnderflowCount") ?? 0;
        var previewSchedulerResumeReprimesAtEnd = GetNullableLong(lastSnapshot, "MjpegPreviewJitterResumeReprimeCount") ?? 0;
        var previewSchedulerDroppedDelta = GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterTotalDropped");
        var previewSchedulerDeadlineDropsDelta = GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterDeadlineDropCount");
        var previewSchedulerClearedDropsDelta = GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterClearedDropCount");
        var previewSchedulerUnderflowsDelta = GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterUnderflowCount");
        var previewSchedulerResumeReprimesDelta = GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterResumeReprimeCount");
        var previewSchedulerScheduleLateDelta = GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterScheduleLateCount");
        var previewSchedulerMaxScheduleLateMsObserved = samples
            .Select(sample => GetDouble(sample.Snapshot, "MjpegPreviewJitterMaxScheduleLateMs"))
            .Append(GetDouble(lastSnapshot, "MjpegPreviewJitterMaxScheduleLateMs"))
            .DefaultIfEmpty(0)
            .Max();
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
                    previewSchedulerDeadlineDropsDelta,
                    previewSchedulerUnderflowsDelta,
                    request.DurationSeconds,
                    visualCadenceHealthy);
            var toleratesSparseScrubSchedulerTransitions =
                request.ScenarioPlan.ToleratesSparsePreviewSchedulerStressTransitions &&
                IsSparsePreviewSchedulerStressRun(
                    previewSchedulerDeadlineDropsDelta,
                    previewSchedulerUnderflowsDelta,
                    request.DurationSeconds,
                    visualCadenceHealthy);
            ValidateFlashbackPreviewScheduler(
                previewSchedulerDeadlineDropsDelta,
                previewSchedulerUnderflowsDelta,
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

        var diagnosticHealthObservation = BuildSessionDiagnosticHealthObservation(
            samples,
            diagnosticHealthSnapshot,
            isFlashbackScenario);
        var sparseSourceCaptureCadenceWarning =
            isFlashbackScenario &&
            IsSparseSourceCaptureCadenceWarningRun(
                diagnosticHealthObservation,
                sourceCadenceMetrics,
                sourceReaderFramesDroppedDelta,
                videoIngestErrorsDelta,
                request.DurationSeconds,
                IsVisualCadenceSessionHealthy(visualCadenceMetrics, GetDouble(lastSnapshot, "ExpectedCaptureFrameRate")));
        var toleratesFlashbackForceRotateDrainWarning = request.ScenarioPlan.ToleratesFlashbackForceRotateDrainWarning;
        var diagnosticHealthTolerated =
            (toleratesSourceSignalHealthWarning &&
             IsSourceSignalDiagnosticHealthObservation(diagnosticHealthObservation)) ||
            (toleratesFlashbackForceRotateDrainWarning &&
             IsFlashbackForceRotateDrainDiagnosticHealthObservation(diagnosticHealthObservation)) ||
            sparseSourceCaptureCadenceWarning ||
            (isFlashbackScenario &&
             request.ScenarioPlan.IsPreviewCycleScenario &&
             IsVisualCadenceSessionHealthy(visualCadenceMetrics, GetDouble(lastSnapshot, "ExpectedCaptureFrameRate")) &&
             IsPreviewSchedulerDiagnosticHealthObservation(diagnosticHealthObservation)) ||
            (isFlashbackScenario &&
             IsSparsePreviewSchedulerDeadlineDropRun(
                 previewSchedulerDeadlineDropsDelta,
                 previewSchedulerUnderflowsDelta,
                 request.DurationSeconds,
                 IsVisualCadenceSessionHealthy(visualCadenceMetrics, GetDouble(lastSnapshot, "ExpectedCaptureFrameRate"))) &&
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
                 !IsSparsePreviewSchedulerDeadlineDropRun(
                     previewSchedulerDeadlineDropsDelta,
                     previewSchedulerUnderflowsDelta,
                     request.DurationSeconds,
                     IsVisualCadenceSessionHealthy(visualCadenceMetrics, GetDouble(lastSnapshot, "ExpectedCaptureFrameRate"))))
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
            flashbackExportForceRotateFallbacksAtEnd,
            flashbackExportForceRotateFallbacksDelta,
            flashbackExportLastForceRotateFallbackSegmentsAtEnd,
            recordingMetrics,
            exportMetrics,
            previewCadenceMetrics,
            previewD3DMetrics,
            visualCadenceMetrics,
            previewSchedulerDroppedAtEnd,
            previewSchedulerDeadlineDropsAtEnd,
            previewSchedulerClearedDropsAtEnd,
            previewSchedulerUnderflowsAtEnd,
            previewSchedulerResumeReprimesAtEnd,
            previewSchedulerDroppedDelta,
            previewSchedulerDeadlineDropsDelta,
            previewSchedulerClearedDropsDelta,
            previewSchedulerUnderflowsDelta,
            previewSchedulerResumeReprimesDelta,
            previewSchedulerScheduleLateDelta,
            previewSchedulerMaxScheduleLateMsObserved,
            diagnosticHealthSucceeded,
            flashbackWarningsSucceeded,
            processCpuMaxPercentObserved);
    }
}
