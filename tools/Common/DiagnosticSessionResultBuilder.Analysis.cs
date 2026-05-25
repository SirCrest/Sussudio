using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackValidation;
using static Sussudio.Tools.DiagnosticSessionHealthPolicy;
using static Sussudio.Tools.DiagnosticSessionFlashbackMetrics;
using static Sussudio.Tools.DiagnosticSessionMetrics;
using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;

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

    private readonly record struct DiagnosticSessionHealthSummary(
        JsonElement Snapshot,
        string HealthStatus,
        string LikelyStage,
        string Summary,
        string Evidence);

    private readonly record struct DiagnosticSessionHealthToleranceVerdict(
        bool IsTolerated,
        bool SparseSourceCaptureCadenceWarning,
        bool SparsePreviewSchedulerDeadlineDropRun,
        string WarningReason);

    private readonly record struct DiagnosticHealthSourceWarningCounters(
        long SourceReaderFramesDroppedDelta,
        long VideoIngestErrorsDelta);

    private readonly record struct DiagnosticSessionPreviewSchedulerAnalysis(
        long DroppedAtEnd,
        long DeadlineDropsAtEnd,
        long ClearedDropsAtEnd,
        long UnderflowsAtEnd,
        long ResumeReprimesAtEnd,
        long DroppedDelta,
        long DeadlineDropsDelta,
        long ClearedDropsDelta,
        long UnderflowsDelta,
        long ResumeReprimesDelta,
        long ScheduleLateDelta,
        double MaxScheduleLateMsObserved,
        string LastDropReasonAtEnd,
        string LastUnderflowReasonAtEnd,
        double LastUnderflowInputAgeMsAtEnd,
        double LastUnderflowOutputAgeMsAtEnd);

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

    private static DiagnosticSessionPreviewSchedulerAnalysis BuildPreviewSchedulerAnalysis(
        JsonElement initialSnapshot,
        JsonElement lastSnapshot,
        IReadOnlyList<DiagnosticSessionSample> samples)
    {
        return new DiagnosticSessionPreviewSchedulerAnalysis(
            DroppedAtEnd: GetNullableLong(lastSnapshot, "MjpegPreviewJitterTotalDropped") ?? 0,
            DeadlineDropsAtEnd: GetNullableLong(lastSnapshot, "MjpegPreviewJitterDeadlineDropCount") ?? 0,
            ClearedDropsAtEnd: GetNullableLong(lastSnapshot, "MjpegPreviewJitterClearedDropCount") ?? 0,
            UnderflowsAtEnd: GetNullableLong(lastSnapshot, "MjpegPreviewJitterUnderflowCount") ?? 0,
            ResumeReprimesAtEnd: GetNullableLong(lastSnapshot, "MjpegPreviewJitterResumeReprimeCount") ?? 0,
            DroppedDelta: GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterTotalDropped"),
            DeadlineDropsDelta: GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterDeadlineDropCount"),
            ClearedDropsDelta: GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterClearedDropCount"),
            UnderflowsDelta: GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterUnderflowCount"),
            ResumeReprimesDelta: GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterResumeReprimeCount"),
            ScheduleLateDelta: GetCounterDelta(lastSnapshot, initialSnapshot, "MjpegPreviewJitterScheduleLateCount"),
            MaxScheduleLateMsObserved: samples
                .Select(sample => GetDouble(sample.Snapshot, "MjpegPreviewJitterMaxScheduleLateMs"))
                .Append(GetDouble(lastSnapshot, "MjpegPreviewJitterMaxScheduleLateMs"))
                .DefaultIfEmpty(0)
                .Max(),
            LastDropReasonAtEnd: GetString(lastSnapshot, "MjpegPreviewJitterLastDropReason") ?? string.Empty,
            LastUnderflowReasonAtEnd: GetString(lastSnapshot, "MjpegPreviewJitterLastUnderflowReason") ?? string.Empty,
            LastUnderflowInputAgeMsAtEnd: GetDouble(lastSnapshot, "MjpegPreviewJitterLastUnderflowInputAgeMs"),
            LastUnderflowOutputAgeMsAtEnd: GetDouble(lastSnapshot, "MjpegPreviewJitterLastUnderflowOutputAgeMs"));
    }

    private static void ValidateFlashbackPreviewSchedulerAnalysis(
        DiagnosticSessionScenarioPlan scenarioPlan,
        JsonElement lastSnapshot,
        int durationSeconds,
        DiagnosticSessionPreviewSchedulerAnalysis previewScheduler,
        PreviewCadenceSessionMetrics previewCadenceMetrics,
        VisualCadenceSessionMetrics visualCadenceMetrics,
        PreviewD3DMetrics previewD3DMetrics,
        List<string> warnings)
    {
        if (!scenarioPlan.UsesFlashbackScenarioWarningPolicy)
        {
            return;
        }

        var previewTargetFps = GetDouble(lastSnapshot, "ExpectedCaptureFrameRate");
        if (previewTargetFps <= 0)
        {
            previewTargetFps = GetDouble(lastSnapshot, "SelectedExactFrameRate");
        }

        var visualCadenceHealthy = IsVisualCadenceSessionHealthy(visualCadenceMetrics, previewTargetFps);
        var toleratesPreviewCycleSchedulerSettling =
            scenarioPlan.IsPreviewCycleScenario && visualCadenceHealthy;
        var toleratesSparsePreviewSchedulerDeadlineDrops =
            IsSparsePreviewSchedulerDeadlineDropRun(
                previewScheduler.DeadlineDropsDelta,
                previewScheduler.UnderflowsDelta,
                durationSeconds,
                visualCadenceHealthy);
        var toleratesSparseScrubSchedulerTransitions =
            scenarioPlan.ToleratesSparsePreviewSchedulerStressTransitions &&
            IsSparsePreviewSchedulerStressRun(
                previewScheduler.DeadlineDropsDelta,
                previewScheduler.UnderflowsDelta,
                durationSeconds,
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

    private static DiagnosticHealthSourceWarningCounters BuildDiagnosticHealthSourceWarningCounters(
        JsonElement initialSnapshot,
        JsonElement lastSnapshot)
    {
        return new DiagnosticHealthSourceWarningCounters(
            SourceReaderFramesDroppedDelta: GetCounterDelta(lastSnapshot, initialSnapshot, "MfSourceReaderFramesDropped"),
            VideoIngestErrorsDelta: GetCounterDelta(lastSnapshot, initialSnapshot, "VideoIngestErrorCount"));
    }
}
