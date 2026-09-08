using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackMetrics;
using static Sussudio.Tools.DiagnosticSessionFlashbackValidation;
using static Sussudio.Tools.DiagnosticSessionHealthPolicy;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;
using static Sussudio.Tools.DiagnosticSessionResultArtifacts;
using static Sussudio.Tools.DiagnosticSessionMetrics;
using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;

namespace Sussudio.Tools;

internal static class DiagnosticSessionResultBuilder
{
    internal static async Task<DiagnosticSessionResult> BuildAndWriteAsync(
        DiagnosticSessionResultBuildRequest request,
        DiagnosticSessionRunState runState)
    {
        runState.SetStage("result-analysis");
        var analysis = Analyze(request);
        var samples = request.Samples;
        var warnings = request.Warnings;

        var artifactPaths = await WritePreSummaryAsync(
                request.OutputDirectory,
                request.SessionId,
                samples,
                request.Timeline,
                runState)
            .ConfigureAwait(false);

        var completedUtc = DateTimeOffset.UtcNow;
        var terminalState = runState.GetTerminalState();
        runState.SetStage("summary");

        var result = CreateResult(
            request,
            runState,
            analysis,
            artifactPaths,
            completedUtc,
            terminalState);

        return await WriteSummaryAsync(result, runState, warnings).ConfigureAwait(false);
    }

    private static async Task<DiagnosticSessionResult> WriteSummaryAsync(
        DiagnosticSessionResult result,
        DiagnosticSessionRunState runState,
        List<string> warnings)
    {
        var summaryWritten = false;
        try
        {
            await WriteJsonAsync(result.SummaryPath, result, CancellationToken.None).ConfigureAwait(false);
            summaryWritten = true;
        }
        catch (Exception ex)
        {
            runState.RecordTerminalException(ex, "summary-write");
            result.Success = false;
            result.CompletedUtc = DateTimeOffset.UtcNow;
            result.TerminalState = runState.GetTerminalState();
            result.LastStage = runState.GetResultLastStage();
            result.UnhandledException = runState.TerminalException is null ? null : DiagnosticSessionRunState.FormatTerminalException(runState.TerminalException);
            result.Warnings = warnings.ToArray();
        }

        if (summaryWritten)
        {
            runState.SetStage("summary-written");
        }

        return result;
    }

    private static DiagnosticSessionResult CreateResult(
        DiagnosticSessionResultBuildRequest request,
        DiagnosticSessionRunState runState,
        DiagnosticSessionResultAnalysis analysis,
        DiagnosticSessionResultArtifactPaths artifactPaths,
        DateTimeOffset completedUtc,
        string terminalState)
    {
        var lastSnapshot = analysis.LastSnapshot;
        var healthSummary = analysis.HealthSummary;
        var healthStatus = healthSummary.HealthStatus;
        var likelyStage = healthSummary.LikelyStage;
        var summary = healthSummary.Summary;
        var evidence = healthSummary.Evidence;
        var playbackSessionMetrics = analysis.PlaybackSessionMetrics;
        var playbackResultMetrics = analysis.PlaybackResultMetrics;
        var recordingMetrics = analysis.RecordingMetrics;
        var exportMetrics = analysis.ExportMetrics;
        var previewCadenceMetrics = analysis.PreviewCadenceMetrics;
        var previewScheduler = analysis.PreviewScheduler;
        var previewD3DMetrics = analysis.PreviewD3DMetrics;
        var visualCadenceMetrics = analysis.VisualCadenceMetrics;
        var verificationSucceeded = request.Verification.HasValue
            ? GetBool(request.Verification.Value, "Succeeded")
            : (bool?)null;
        var stutterClassification = ClassifyFlashbackPlaybackStutter(playbackSessionMetrics, playbackResultMetrics);

        return new DiagnosticSessionResult
        {
            SessionId = request.SessionId,
            Scenario = request.Scenario,
            Success = DetermineDiagnosticSessionSuccess(request, runState, analysis, verificationSucceeded),
            StartedUtc = request.StartedUtc,
            CompletedUtc = completedUtc,
            TerminalState = terminalState,
            LastStage = runState.GetResultLastStage(),
            UnhandledException = runState.TerminalException is null ? null : DiagnosticSessionRunState.FormatTerminalException(runState.TerminalException),
            RunnerProcessId = request.RunnerProcessId,
            DurationSeconds = request.DurationSeconds,
            SampleIntervalMs = request.SampleIntervalMs,
            SampleCount = request.Samples.Count,
            OutputDirectory = request.OutputDirectory,
            LivePath = request.LivePath,
            SummaryPath = artifactPaths.SummaryPath,
            SamplesPath = artifactPaths.SamplesPath,
            FrameLedgerPath = artifactPaths.FrameLedgerPath,
            TimelinePath = artifactPaths.TimelinePath,
            HealthStatus = healthStatus,
            LikelyStage = likelyStage,
            Summary = summary,
            Evidence = evidence,
            SelectedResolutionAtEnd = GetString(lastSnapshot, "SelectedResolution") ?? string.Empty,
            SelectedFrameRateAtEnd = GetDouble(lastSnapshot, "SelectedFrameRate"),
            SelectedFriendlyFrameRateAtEnd = GetString(lastSnapshot, "SelectedFriendlyFrameRate") ?? string.Empty,
            SelectedExactFrameRateArgAtEnd = GetString(lastSnapshot, "SelectedExactFrameRateArg") ?? string.Empty,
            SelectedVideoFormatAtEnd = GetString(lastSnapshot, "SelectedVideoFormat") ?? string.Empty,
            VideoRequestedSubtypeAtEnd = GetString(lastSnapshot, "VideoRequestedSubtype") ?? string.Empty,
            VideoNegotiatedSubtypeAtEnd = GetString(lastSnapshot, "VideoNegotiatedSubtype") ?? string.Empty,
            SourceWidthAtEnd = (int)(GetNullableLong(lastSnapshot, "SourceWidth") ?? 0),
            SourceHeightAtEnd = (int)(GetNullableLong(lastSnapshot, "SourceHeight") ?? 0),
            DetectedSourceFrameRateAtEnd = GetDouble(lastSnapshot, "DetectedSourceFrameRate"),
            DetectedSourceFrameRateArgAtEnd = GetString(lastSnapshot, "DetectedSourceFrameRateArg") ?? string.Empty,
            SourceIsHdrAtEnd = GetBool(lastSnapshot, "SourceIsHdr"),
            SourceTelemetrySummaryAtEnd = GetString(lastSnapshot, "SourceTelemetrySummaryText") ?? string.Empty,
            FlashbackPlaybackPendingCommandsAtEnd = playbackResultMetrics.PendingCommandsAtEnd,
            FlashbackPlaybackMaxPendingCommandsObserved = playbackResultMetrics.MaxPendingCommandsObserved,
            FlashbackPlaybackMaxCommandQueueLatencyMsObserved = playbackResultMetrics.MaxCommandQueueLatencyMsObserved,
            FlashbackPlaybackMaxCommandQueueLatencyCommandObserved = playbackResultMetrics.MaxCommandQueueLatencyCommandObserved,
            FlashbackPlaybackCommandsDroppedAtEnd = playbackResultMetrics.CommandsDroppedAtEnd,
            FlashbackPlaybackCommandsSkippedNotReadyAtEnd = playbackResultMetrics.CommandsSkippedNotReadyAtEnd,
            FlashbackPlaybackScrubUpdatesCoalescedAtEnd = playbackResultMetrics.ScrubUpdatesCoalescedAtEnd,
            FlashbackPlaybackSeekCommandsCoalescedAtEnd = playbackResultMetrics.SeekCommandsCoalescedAtEnd,
            FlashbackPlaybackLastCommandFailureAtEnd = playbackResultMetrics.LastCommandFailureAtEnd,
            FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd = playbackResultMetrics.LastCommandFailureUtcUnixMsAtEnd,
            FlashbackPlaybackObservedFpsAtEnd = playbackResultMetrics.ObservedFpsAtEnd,
            FlashbackPlaybackMinObservedFpsObserved = playbackSessionMetrics.MinObservedFpsObserved,
            FlashbackPlaybackAvgFrameMsAtEnd = playbackResultMetrics.AvgFrameMsAtEnd,
            FlashbackPlaybackP99FrameMsAtEnd = playbackResultMetrics.P99FrameMsAtEnd,
            FlashbackPlaybackMaxFrameMsAtEnd = playbackResultMetrics.MaxFrameMsAtEnd,
            FlashbackPlaybackOnePercentLowFpsAtEnd = playbackResultMetrics.OnePercentLowFpsAtEnd,
            FlashbackPlaybackMinOnePercentLowFpsObserved = playbackSessionMetrics.MinOnePercentLowFpsObserved,
            FlashbackPlaybackOnePercentLowSampleWindowObserved = playbackSessionMetrics.OnePercentLowSampleWindowObserved,
            FlashbackPlaybackOnePercentLowMinimumFrames = playbackSessionMetrics.MinimumOnePercentLowFrameCount,
            FlashbackPlaybackMaxSessionFrameCountObserved = playbackSessionMetrics.MaxSessionFrameCountObserved,
            FlashbackPlaybackMinOnePercentLowOffsetMs = playbackSessionMetrics.MinOnePercentLowOffsetMs,
            FlashbackPlaybackMinOnePercentLowFrameCount = playbackSessionMetrics.MinOnePercentLowFrameCount,
            FlashbackPlaybackMinOnePercentLowP99FrameMs = playbackSessionMetrics.MinOnePercentLowP99FrameMs,
            FlashbackPlaybackMinOnePercentLowMaxFrameMs = playbackSessionMetrics.MinOnePercentLowMaxFrameMs,
            FlashbackPlaybackMinOnePercentLowDecodeP99Ms = playbackSessionMetrics.MinOnePercentLowDecodeP99Ms,
            FlashbackPlaybackMinOnePercentLowDecodeMaxMs = playbackSessionMetrics.MinOnePercentLowDecodeMaxMs,
            FlashbackPlaybackMinOnePercentLowAvDriftMs = playbackSessionMetrics.MinOnePercentLowAvDriftMs,
            FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks = playbackSessionMetrics.MinOnePercentLowAudioMasterFallbacks,
            FlashbackPlaybackMaxP99FrameMsObserved = playbackSessionMetrics.MaxP99FrameMsObserved,
            FlashbackPlaybackMaxFrameMsObserved = playbackSessionMetrics.MaxFrameMsObserved,
            FlashbackPlaybackMaxSlowFramePercentObserved = playbackSessionMetrics.MaxSlowFramePercentObserved,
            FlashbackPlaybackDecodeAvgMsAtEnd = playbackResultMetrics.DecodeAvgMsAtEnd,
            FlashbackPlaybackDecodeP95MsAtEnd = playbackResultMetrics.DecodeP95MsAtEnd,
            FlashbackPlaybackDecodeP99MsAtEnd = playbackResultMetrics.DecodeP99MsAtEnd,
            FlashbackPlaybackDecodeMaxMsAtEnd = playbackResultMetrics.DecodeMaxMsAtEnd,
            FlashbackPlaybackMaxDecodePhaseAtEnd = playbackResultMetrics.MaxDecodePhaseAtEnd,
            FlashbackPlaybackMaxDecodeReceiveMsAtEnd = playbackResultMetrics.MaxDecodeReceiveMsAtEnd,
            FlashbackPlaybackMaxDecodeFeedMsAtEnd = playbackResultMetrics.MaxDecodeFeedMsAtEnd,
            FlashbackPlaybackMaxDecodeReadMsAtEnd = playbackResultMetrics.MaxDecodeReadMsAtEnd,
            FlashbackPlaybackMaxDecodeSendMsAtEnd = playbackResultMetrics.MaxDecodeSendMsAtEnd,
            FlashbackPlaybackMaxDecodeAudioMsAtEnd = playbackResultMetrics.MaxDecodeAudioMsAtEnd,
            FlashbackPlaybackMaxDecodeConvertMsAtEnd = playbackResultMetrics.MaxDecodeConvertMsAtEnd,
            FlashbackPlaybackMaxDecodeUtcUnixMsAtEnd = playbackResultMetrics.MaxDecodeUtcUnixMsAtEnd,
            FlashbackPlaybackMaxDecodePositionMsAtEnd = playbackResultMetrics.MaxDecodePositionMsAtEnd,
            FlashbackPlaybackMaxDecodeP99MsObserved = playbackSessionMetrics.MaxDecodeP99MsObserved,
            FlashbackPlaybackMaxDecodeMsObserved = playbackSessionMetrics.MaxDecodeMsObserved,
            FlashbackPlaybackMaxDecodePhaseObserved = playbackSessionMetrics.MaxDecodePhaseObserved,
            FlashbackPlaybackMaxDecodeReceiveMsObserved = playbackSessionMetrics.MaxDecodeReceiveMsObserved,
            FlashbackPlaybackMaxDecodeFeedMsObserved = playbackSessionMetrics.MaxDecodeFeedMsObserved,
            FlashbackPlaybackMaxDecodeReadMsObserved = playbackSessionMetrics.MaxDecodeReadMsObserved,
            FlashbackPlaybackMaxDecodeSendMsObserved = playbackSessionMetrics.MaxDecodeSendMsObserved,
            FlashbackPlaybackMaxDecodeAudioMsObserved = playbackSessionMetrics.MaxDecodeAudioMsObserved,
            FlashbackPlaybackMaxDecodeConvertMsObserved = playbackSessionMetrics.MaxDecodeConvertMsObserved,
            FlashbackPlaybackMaxDecodeUtcUnixMsObserved = playbackSessionMetrics.MaxDecodeUtcUnixMsObserved,
            FlashbackPlaybackMaxDecodePositionMsObserved = playbackSessionMetrics.MaxDecodePositionMsObserved,
            FlashbackPlaybackFrameCountAtEnd = playbackResultMetrics.FrameCountAtEnd,
            FlashbackPlaybackLateFramesAtEnd = playbackResultMetrics.LateFramesAtEnd,
            FlashbackPlaybackSlowFramesAtEnd = playbackResultMetrics.SlowFramesAtEnd,
            FlashbackPlaybackSlowFramePercentAtEnd = playbackResultMetrics.SlowFramePercentAtEnd,
            FlashbackPlaybackDroppedFramesAtEnd = playbackResultMetrics.DroppedFramesAtEnd,
            FlashbackPlaybackDroppedFramesDelta = playbackSessionMetrics.DroppedFramesDelta,
            FlashbackPlaybackAudioMasterDelayDoublesAtEnd = playbackResultMetrics.AudioMasterDelayDoublesAtEnd,
            FlashbackPlaybackAudioMasterDelayShrinksAtEnd = playbackResultMetrics.AudioMasterDelayShrinksAtEnd,
            FlashbackPlaybackAudioMasterFallbacksAtEnd = playbackResultMetrics.AudioMasterFallbacksAtEnd,
            FlashbackPlaybackAudioMasterUnavailableFallbacksAtEnd = playbackResultMetrics.AudioMasterUnavailableFallbacksAtEnd,
            FlashbackPlaybackAudioMasterStaleFallbacksAtEnd = playbackResultMetrics.AudioMasterStaleFallbacksAtEnd,
            FlashbackPlaybackAudioMasterDriftOutlierFallbacksAtEnd = playbackResultMetrics.AudioMasterDriftOutlierFallbacksAtEnd,
            FlashbackPlaybackAudioMasterLastFallbackReasonAtEnd = playbackResultMetrics.AudioMasterLastFallbackReasonAtEnd,
            FlashbackPlaybackAudioMasterLastFallbackClockAgeMsAtEnd = playbackResultMetrics.AudioMasterLastFallbackClockAgeMsAtEnd,
            FlashbackPlaybackMaxAudioMasterDelayDoublesObserved = playbackSessionMetrics.MaxAudioMasterDelayDoublesObserved,
            FlashbackPlaybackMaxAudioMasterDelayShrinksObserved = playbackSessionMetrics.MaxAudioMasterDelayShrinksObserved,
            FlashbackPlaybackMaxAudioMasterFallbacksObserved = playbackSessionMetrics.MaxAudioMasterFallbacksObserved,
            FlashbackPlaybackMaxAudioBufferedDurationMsObserved = playbackSessionMetrics.MaxAudioBufferedDurationMsObserved,
            FlashbackPlaybackMaxAudioQueueDurationMsObserved = playbackSessionMetrics.MaxAudioQueueDurationMsObserved,
            FlashbackPlaybackMaxAbsAvDriftMsObserved = playbackSessionMetrics.MaxAbsAvDriftMsObserved,
            FlashbackPlaybackSubmitFailuresAtEnd = playbackResultMetrics.SubmitFailuresAtEnd,
            FlashbackPlaybackSubmitFailuresDelta = playbackSessionMetrics.SubmitFailuresDelta,
            FlashbackPlaybackSegmentSwitchesAtEnd = playbackResultMetrics.SegmentSwitchesAtEnd,
            FlashbackPlaybackFmp4ReopensAtEnd = playbackResultMetrics.Fmp4ReopensAtEnd,
            FlashbackPlaybackWriteHeadWaitsAtEnd = playbackResultMetrics.WriteHeadWaitsAtEnd,
            FlashbackPlaybackNearLiveSnapsAtEnd = playbackResultMetrics.NearLiveSnapsAtEnd,
            FlashbackPlaybackDecodeErrorSnapsAtEnd = playbackResultMetrics.DecodeErrorSnapsAtEnd,
            FlashbackPlaybackLastWriteHeadWaitGapMsAtEnd = playbackResultMetrics.LastWriteHeadWaitGapMsAtEnd,
            FlashbackPlaybackSeekForwardDecodeCapHitsAtEnd = playbackResultMetrics.SeekForwardDecodeCapHitsAtEnd,
            FlashbackPlaybackSeekForwardDecodeCapHitsDelta = playbackResultMetrics.SeekForwardDecodeCapHitsDelta,
            FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd = playbackResultMetrics.LastSeekHitForwardDecodeCapAtEnd,
            FlashbackPlaybackLikelyStutterCause = stutterClassification.Cause,
            FlashbackPlaybackLikelyStutterEvidence = stutterClassification.Evidence,
            FlashbackRecordingBackendObserved = recordingMetrics.BackendObserved,
            FlashbackRecordingFileGrowthObserved = recordingMetrics.FileGrowthObserved,
            FlashbackRecordingVideoFramesSubmittedDelta = recordingMetrics.VideoFramesSubmittedDelta,
            FlashbackRecordingVideoEncoderPacketsWrittenDelta = recordingMetrics.VideoEncoderPacketsWrittenDelta,
            FlashbackRecordingIntegritySequenceGapsAtEnd = recordingMetrics.IntegritySequenceGapsAtEnd,
            FlashbackRecordingIntegrityQueueDroppedFramesAtEnd = recordingMetrics.IntegrityQueueDroppedFramesAtEnd,
            FlashbackRecordingIntegritySequenceGapsDelta = recordingMetrics.IntegritySequenceGapsDelta,
            FlashbackRecordingIntegrityQueueDroppedFramesDelta = recordingMetrics.IntegrityQueueDroppedFramesDelta,
            FlashbackExportObserved = exportMetrics.Observed,
            FlashbackExportActiveAtEnd = exportMetrics.ActiveAtEnd,
            FlashbackExportStatusAtEnd = exportMetrics.StatusAtEnd,
            FlashbackExportMessageAtEnd = exportMetrics.MessageAtEnd,
            FlashbackExportFailureKindAtEnd = exportMetrics.FailureKindAtEnd,
            FlashbackExportOutputPathAtEnd = exportMetrics.OutputPathAtEnd,
            FlashbackExportForceRotateFallbacksAtEnd = exportMetrics.ForceRotateFallbacksAtEnd,
            FlashbackExportForceRotateFallbacksDelta = exportMetrics.ForceRotateFallbacksDelta,
            FlashbackExportLastForceRotateFallbackSegmentsAtEnd = exportMetrics.LastForceRotateFallbackSegmentsAtEnd,
            LastExportIdAtEnd = exportMetrics.LastExportIdAtEnd,
            LastExportSuccessAtEnd = exportMetrics.LastSuccessAtEnd,
            LastExportMessageAtEnd = exportMetrics.LastMessageAtEnd,
            FlashbackExportMaxElapsedMsObserved = exportMetrics.MaxElapsedMsObserved,
            FlashbackExportMaxLastProgressAgeMsObserved = exportMetrics.MaxLastProgressAgeMsObserved,
            FlashbackExportMaxOutputBytesObserved = exportMetrics.MaxOutputBytesObserved,
            FlashbackExportMaxThroughputBytesPerSecObserved = exportMetrics.MaxThroughputBytesPerSecObserved,
            PreviewCadenceOnePercentLowFpsAtEnd = previewCadenceMetrics.OnePercentLowFpsAtEnd,
            PreviewCadenceMinOnePercentLowFpsObserved = previewCadenceMetrics.MinOnePercentLowFpsObserved,
            PreviewSchedulerDroppedAtEnd = previewScheduler.DroppedAtEnd,
            PreviewSchedulerDeadlineDropsAtEnd = previewScheduler.DeadlineDropsAtEnd,
            PreviewSchedulerClearedDropsAtEnd = previewScheduler.ClearedDropsAtEnd,
            PreviewSchedulerUnderflowsAtEnd = previewScheduler.UnderflowsAtEnd,
            PreviewSchedulerResumeReprimesAtEnd = previewScheduler.ResumeReprimesAtEnd,
            PreviewSchedulerDroppedDelta = previewScheduler.DroppedDelta,
            PreviewSchedulerDeadlineDropsDelta = previewScheduler.DeadlineDropsDelta,
            PreviewSchedulerClearedDropsDelta = previewScheduler.ClearedDropsDelta,
            PreviewSchedulerUnderflowsDelta = previewScheduler.UnderflowsDelta,
            PreviewSchedulerResumeReprimesDelta = previewScheduler.ResumeReprimesDelta,
            PreviewSchedulerLastDropReasonAtEnd = previewScheduler.LastDropReasonAtEnd,
            PreviewSchedulerLastUnderflowReasonAtEnd = previewScheduler.LastUnderflowReasonAtEnd,
            PreviewSchedulerLastUnderflowInputAgeMsAtEnd = previewScheduler.LastUnderflowInputAgeMsAtEnd,
            PreviewSchedulerLastUnderflowOutputAgeMsAtEnd = previewScheduler.LastUnderflowOutputAgeMsAtEnd,
            PreviewSchedulerMaxScheduleLateMsObserved = previewScheduler.MaxScheduleLateMsObserved,
            PreviewSchedulerScheduleLateDelta = previewScheduler.ScheduleLateDelta,
            PreviewD3DFrameStatsMissedRefreshDelta = previewD3DMetrics.MissedRefreshDelta,
            PreviewD3DFrameStatsFailureDelta = previewD3DMetrics.StatsFailureDelta,
            PreviewD3DMaxRecentSlowFramesObserved = previewD3DMetrics.MaxRecentSlowFramesObserved,
            PreviewD3DLatestSlowFrameReason = previewD3DMetrics.LatestSlowFrameReason,
            PreviewD3DLatestSlowFrameOverBudgetMs = previewD3DMetrics.LatestSlowFrameOverBudgetMs,
            PreviewD3DLatestSlowFramePresentIntervalMs = previewD3DMetrics.LatestSlowFramePresentIntervalMs,
            PreviewD3DLatestSlowFrameTotalFrameCpuMs = previewD3DMetrics.LatestSlowFrameTotalFrameCpuMs,
            PreviewD3DLatestSlowFramePresentCallMs = previewD3DMetrics.LatestSlowFramePresentCallMs,
            PreviewD3DLatestSlowFramePendingFrameCount = previewD3DMetrics.LatestSlowFramePendingFrameCount,
            PreviewD3DInputUploadCpuP99MsAtEnd = previewD3DMetrics.InputUploadCpuP99MsAtEnd,
            PreviewD3DInputUploadCpuMaxMsObserved = previewD3DMetrics.InputUploadCpuMaxMsObserved,
            PreviewD3DRenderSubmitCpuP99MsAtEnd = previewD3DMetrics.RenderSubmitCpuP99MsAtEnd,
            PreviewD3DRenderSubmitCpuMaxMsObserved = previewD3DMetrics.RenderSubmitCpuMaxMsObserved,
            PreviewD3DPresentCallP99MsAtEnd = previewD3DMetrics.PresentCallP99MsAtEnd,
            PreviewD3DPresentCallMaxMsObserved = previewD3DMetrics.PresentCallMaxMsObserved,
            PreviewD3DTotalFrameCpuP99MsAtEnd = previewD3DMetrics.TotalFrameCpuP99MsAtEnd,
            PreviewD3DTotalFrameCpuMaxMsObserved = previewD3DMetrics.TotalFrameCpuMaxMsObserved,
            VisualCadenceOutputFpsAtEnd = visualCadenceMetrics.OutputFpsAtEnd,
            VisualCadenceChangeFpsAtEnd = visualCadenceMetrics.ChangeFpsAtEnd,
            VisualCadenceMinChangeFpsObserved = visualCadenceMetrics.MinChangeFpsObserved,
            VisualCadenceRepeatPercentAtEnd = visualCadenceMetrics.RepeatPercentAtEnd,
            VisualCadenceMaxRepeatPercentObserved = visualCadenceMetrics.MaxRepeatPercentObserved,
            VisualCadenceRepeatFramesAtEnd = visualCadenceMetrics.RepeatFramesAtEnd,
            VisualCadenceLongestRepeatRunAtEnd = visualCadenceMetrics.LongestRepeatRunAtEnd,
            ProcessCpuPercentAtEnd = GetDouble(lastSnapshot, "ProcessCpuPercent"),
            ProcessCpuMaxPercentObserved = GetProcessCpuMaxPercentObserved(request.Samples, lastSnapshot),
            RecordingVerificationRun = request.Verification.HasValue,
            RecordingVerificationSucceeded = verificationSucceeded,
            RecordingVerificationMessage = request.Verification.HasValue
                ? GetString(request.Verification.Value, "Message") ?? string.Empty
                : null,
            PresentMon = request.PresentMon,
            Actions = request.Actions.ToArray(),
            Warnings = request.Warnings.ToArray()
        };
    }

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
        if (request.ScenarioPlan.Kind == DiagnosticSessionScenarioKind.FlashbackPlayback)
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
            scenarioPlan.ToleratesStrictArtifactDiagnosticHealthWarning,
            scenarioPlan.ToleratesControlOnlyDiagnosticHealthWarning,
            scenarioPlan.IsPreviewCycleScenario,
            scenarioPlan.ToleratesSparsePreviewSchedulerStressTransitions));
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
            IsSnapshotEpochDiagnosticHealthObservation(diagnosticHealthObservation) ||
            sparseSourceCaptureCadenceWarning ||
            (scenarioPlan.ToleratesStrictArtifactDiagnosticHealthWarning &&
             visualCadenceHealthy &&
             IsPresentDisplayDiagnosticHealthObservation(diagnosticHealthObservation)) ||
            (scenarioPlan.ToleratesControlOnlyDiagnosticHealthWarning &&
             IsPresentDisplayDiagnosticHealthObservation(diagnosticHealthObservation)) ||
            (isFlashbackScenario &&
             scenarioPlan.IsPreviewCycleScenario &&
             visualCadenceHealthy &&
             IsPreviewSchedulerDiagnosticHealthObservation(diagnosticHealthObservation)) ||
            (isFlashbackScenario &&
             sparsePreviewSchedulerDeadlineDropRun &&
             IsPreviewSchedulerDiagnosticHealthObservation(diagnosticHealthObservation));
        var warningReason =
            IsSnapshotEpochDiagnosticHealthObservation(diagnosticHealthObservation)
                ? "snapshot epoch consistency warning tolerated"
                : IsPreviewSchedulerDiagnosticHealthObservation(diagnosticHealthObservation)
                    ? scenarioPlan.ToleratesStrictArtifactDiagnosticHealthWarning
                        ? "present/display warning tolerated for strict artifact verification scenario"
                        : "preview scheduler transition warning tolerated for preview-cycle scenario"
                    : IsPresentDisplayDiagnosticHealthObservation(diagnosticHealthObservation)
                        ? scenarioPlan.ToleratesControlOnlyDiagnosticHealthWarning
                            ? "present/display warning tolerated for flashback control scenario"
                            : "present/display warning tolerated for strict artifact verification scenario"
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

    private static double GetProcessCpuMaxPercentObserved(
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement lastSnapshot) =>
        samples
            .Select(sample => GetDouble(sample.Snapshot, "ProcessCpuPercent"))
            .Append(GetDouble(lastSnapshot, "ProcessCpuPercent"))
            .DefaultIfEmpty(0.0)
            .Max();

    private static bool DetermineDiagnosticSessionSuccess(
        DiagnosticSessionResultBuildRequest request,
        DiagnosticSessionRunState runState,
        DiagnosticSessionResultAnalysis analysis,
        bool? verificationSucceeded) =>
        request.CommandFailureCount == 0 &&
        runState.TerminalException is null &&
        (analysis.DiagnosticHealthSucceeded ||
         IsFunctionallyVerifiedRecordingWarning(request, analysis, verificationSucceeded) ||
         IsVisuallyVerifiedPreviewWarning(request, analysis)) &&
        (request.PresentMon is null || request.PresentMon.Success) &&
        (!verificationSucceeded.HasValue || verificationSucceeded.Value) &&
        analysis.FlashbackWarningsSucceeded;

    private static bool IsFunctionallyVerifiedRecordingWarning(
        DiagnosticSessionResultBuildRequest request,
        DiagnosticSessionResultAnalysis analysis,
        bool? verificationSucceeded) =>
        IsStrictArtifactVerificationScenario(request.Scenario) &&
        verificationSucceeded == true &&
        string.Equals(analysis.HealthSummary.HealthStatus, "Warning", StringComparison.OrdinalIgnoreCase);

    private static bool IsStrictArtifactVerificationScenario(string scenario) =>
        string.Equals(scenario, DiagnosticSessionScenarioCatalog.RecordingOnly, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(scenario, DiagnosticSessionScenarioCatalog.FlashbackRangeExport, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(scenario, DiagnosticSessionScenarioCatalog.FlashbackRangeExportAudioSwitch, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(scenario, DiagnosticSessionScenarioCatalog.FlashbackExportPlayback, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(scenario, DiagnosticSessionScenarioCatalog.FlashbackRestartCycle, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(scenario, DiagnosticSessionScenarioCatalog.FlashbackEncoderCycle, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(scenario, DiagnosticSessionScenarioCatalog.FlashbackExportConcurrent, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(scenario, DiagnosticSessionScenarioCatalog.FlashbackDisableDuringExport, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(scenario, DiagnosticSessionScenarioCatalog.FlashbackRotatedExport, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(scenario, DiagnosticSessionScenarioCatalog.FlashbackPreviewCycle, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(scenario, DiagnosticSessionScenarioCatalog.FlashbackPlaybackPreviewCycle, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(scenario, DiagnosticSessionScenarioCatalog.FlashbackRecording, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(scenario, DiagnosticSessionScenarioCatalog.FlashbackRecordingPreviewCycle, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(scenario, DiagnosticSessionScenarioCatalog.FlashbackRecordingSettingsDeferred, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(scenario, DiagnosticSessionScenarioCatalog.FlashbackRecordingExportRejected, StringComparison.OrdinalIgnoreCase);

    private static bool IsVisuallyVerifiedPreviewWarning(
        DiagnosticSessionResultBuildRequest request,
        DiagnosticSessionResultAnalysis analysis)
    {
        if (!string.Equals(request.Scenario, DiagnosticSessionScenarioCatalog.PreviewOnly, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.Scenario, DiagnosticSessionScenarioCatalog.Observe, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.Equals(analysis.HealthSummary.HealthStatus, "Warning", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(analysis.HealthSummary.LikelyStage, "present_display", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var targetFps = GetDouble(analysis.LastSnapshot, "ExpectedCaptureFrameRate");
        if (targetFps <= 0)
        {
            targetFps = GetDouble(analysis.LastSnapshot, "SelectedExactFrameRate");
        }

        return IsVisualCadenceSessionHealthy(analysis.VisualCadenceMetrics, targetFps);
    }

    internal readonly record struct FlashbackPlaybackStutterClassification(
        string Cause,
        string Evidence);

    private static FlashbackPlaybackStutterClassification ClassifyFlashbackPlaybackStutter(
        FlashbackPlaybackSessionMetrics playbackSessionMetrics,
        FlashbackPlaybackResultMetrics playbackResultMetrics)
    {
        var endSnapshot = playbackResultMetrics.EndSnapshot;
        var baselineSnapshot = playbackSessionMetrics.BaselineSnapshot;
        return ClassifyFlashbackPlaybackStutterCause(
            renderSilenceDelta: GetResetAwareCounterDelta(endSnapshot, baselineSnapshot, "WasapiPlaybackRenderSilenceCount"),
            playbackQueueDropDelta: GetResetAwareCounterDelta(endSnapshot, baselineSnapshot, "WasapiPlaybackQueueDropCount"),
            playbackBufferedDurationMsAtEnd: GetDouble(endSnapshot, "WasapiPlaybackBufferedDurationMs"),
            playbackQueueDurationMsAtEnd: GetDouble(endSnapshot, "WasapiPlaybackQueueDurationMs"),
            submitFailuresDelta: playbackSessionMetrics.SubmitFailuresDelta,
            audioMasterStaleFallbacksDelta: GetResetAwareCounterDelta(endSnapshot, baselineSnapshot, "FlashbackPlaybackAudioMasterStaleFallbacks"),
            audioMasterDriftOutlierFallbacksDelta: GetResetAwareCounterDelta(endSnapshot, baselineSnapshot, "FlashbackPlaybackAudioMasterDriftOutlierFallbacks"),
            maxAbsAvDriftMsObserved: playbackSessionMetrics.MaxAbsAvDriftMsObserved,
            pendingCommandsAtEnd: playbackResultMetrics.PendingCommandsAtEnd,
            maxPendingCommandsObserved: playbackResultMetrics.MaxPendingCommandsObserved,
            maxCommandQueueLatencyMsObserved: playbackResultMetrics.MaxCommandQueueLatencyMsObserved,
            commandDropsDelta: GetCounterDelta(endSnapshot, baselineSnapshot, "FlashbackPlaybackCommandsDropped"),
            commandSkippedNotReadyDelta: GetCounterDelta(endSnapshot, baselineSnapshot, "FlashbackPlaybackCommandsSkippedNotReady"),
            segmentSwitchesDelta: GetCounterDelta(endSnapshot, baselineSnapshot, "FlashbackPlaybackSegmentSwitches"),
            fmp4ReopensDelta: GetCounterDelta(endSnapshot, baselineSnapshot, "FlashbackPlaybackFmp4Reopens"),
            writeHeadWaitsDelta: GetCounterDelta(endSnapshot, baselineSnapshot, "FlashbackPlaybackWriteHeadWaits"),
            lastWriteHeadWaitGapMsAtEnd: playbackResultMetrics.LastWriteHeadWaitGapMsAtEnd,
            captureSevereGapDelta: GetResetAwareCounterDelta(endSnapshot, baselineSnapshot, "WasapiCaptureCallbackSevereGapCount"),
            captureMaxCallbackIntervalMsAtEnd: GetDouble(endSnapshot, "WasapiCaptureCallbackMaxIntervalMs"),
            maxDecodeP99MsObserved: playbackSessionMetrics.MaxDecodeP99MsObserved,
            maxDecodeMsObserved: playbackSessionMetrics.MaxDecodeMsObserved,
            maxDecodePhaseObserved: playbackSessionMetrics.MaxDecodePhaseObserved,
            slowFramePercentAtEnd: playbackResultMetrics.SlowFramePercentAtEnd,
            maxSlowFramePercentObserved: playbackSessionMetrics.MaxSlowFramePercentObserved,
            droppedFramesDelta: playbackSessionMetrics.DroppedFramesDelta,
            onePercentLowFpsAtEnd: playbackResultMetrics.OnePercentLowFpsAtEnd,
            minOnePercentLowFpsObserved: playbackSessionMetrics.MinOnePercentLowFpsObserved,
            targetFps: GetFlashbackPlaybackTargetFps(endSnapshot));
    }

    internal static FlashbackPlaybackStutterClassification ClassifyFlashbackPlaybackStutterCause(
        long renderSilenceDelta,
        long playbackQueueDropDelta,
        double playbackBufferedDurationMsAtEnd,
        double playbackQueueDurationMsAtEnd,
        long submitFailuresDelta,
        long audioMasterStaleFallbacksDelta,
        long audioMasterDriftOutlierFallbacksDelta,
        double maxAbsAvDriftMsObserved,
        int pendingCommandsAtEnd,
        int maxPendingCommandsObserved,
        int maxCommandQueueLatencyMsObserved,
        long commandDropsDelta,
        long commandSkippedNotReadyDelta,
        long segmentSwitchesDelta,
        long fmp4ReopensDelta,
        long writeHeadWaitsDelta,
        long lastWriteHeadWaitGapMsAtEnd,
        long captureSevereGapDelta,
        double captureMaxCallbackIntervalMsAtEnd,
        double maxDecodeP99MsObserved,
        double maxDecodeMsObserved,
        string maxDecodePhaseObserved,
        double slowFramePercentAtEnd,
        double maxSlowFramePercentObserved,
        long droppedFramesDelta,
        double onePercentLowFpsAtEnd,
        double minOnePercentLowFpsObserved,
        double targetFps)
    {
        var cadenceStress = HasFlashbackPlaybackCadenceStress(
            slowFramePercentAtEnd,
            maxSlowFramePercentObserved,
            droppedFramesDelta,
            onePercentLowFpsAtEnd,
            minOnePercentLowFpsObserved,
            targetFps);

        if (renderSilenceDelta > 0 ||
            playbackQueueDropDelta > 0 ||
            submitFailuresDelta > 0 ||
            (cadenceStress && playbackBufferedDurationMsAtEnd <= 1.0 && playbackQueueDurationMsAtEnd <= 1.0))
        {
            return new FlashbackPlaybackStutterClassification(
                "render_underrun",
                $"renderSilenceDelta={renderSilenceDelta} queueDropsDelta={playbackQueueDropDelta} submitFailuresDelta={submitFailuresDelta} bufferedMsEnd={playbackBufferedDurationMsAtEnd:0.##} queueMsEnd={playbackQueueDurationMsAtEnd:0.##}");
        }

        if (audioMasterStaleFallbacksDelta > 0)
        {
            return new FlashbackPlaybackStutterClassification(
                "audio_master_stale_clock",
                $"staleFallbacksDelta={audioMasterStaleFallbacksDelta} driftOutlierFallbacksDelta={audioMasterDriftOutlierFallbacksDelta} absAvDriftMsMax={maxAbsAvDriftMsObserved:0.##}");
        }

        const double maxHealthyAvDriftMs = 250.0;
        if (audioMasterDriftOutlierFallbacksDelta > 0 ||
            maxAbsAvDriftMsObserved > maxHealthyAvDriftMs)
        {
            return new FlashbackPlaybackStutterClassification(
                "audio_master_drift_outlier_clock",
                $"driftOutlierFallbacksDelta={audioMasterDriftOutlierFallbacksDelta} absAvDriftMsMax={maxAbsAvDriftMsObserved:0.##} budgetMs={maxHealthyAvDriftMs:0.##}");
        }

        const int commandBacklogPendingThreshold = 4;
        const int commandBacklogLatencyMsThreshold = 250;
        if (pendingCommandsAtEnd > 0 ||
            maxPendingCommandsObserved >= commandBacklogPendingThreshold ||
            maxCommandQueueLatencyMsObserved >= commandBacklogLatencyMsThreshold ||
            commandDropsDelta > 0 ||
            commandSkippedNotReadyDelta > 0)
        {
            return new FlashbackPlaybackStutterClassification(
                "command_backlog",
                $"pendingEnd={pendingCommandsAtEnd} maxPending={maxPendingCommandsObserved} maxLatencyMs={maxCommandQueueLatencyMsObserved} droppedDelta={commandDropsDelta} skippedDelta={commandSkippedNotReadyDelta}");
        }

        if (writeHeadWaitsDelta > 0 ||
            lastWriteHeadWaitGapMsAtEnd > 0 ||
            (cadenceStress && (segmentSwitchesDelta > 0 || fmp4ReopensDelta > 0)))
        {
            return new FlashbackPlaybackStutterClassification(
                "segment_reopen_stall",
                $"segmentSwitchesDelta={segmentSwitchesDelta} fmp4ReopensDelta={fmp4ReopensDelta} writeHeadWaitsDelta={writeHeadWaitsDelta} lastWriteHeadGapMsEnd={lastWriteHeadWaitGapMsAtEnd}");
        }

        const double captureCallbackGapMsThreshold = 100.0;
        if (captureSevereGapDelta > 0 ||
            (cadenceStress && captureMaxCallbackIntervalMsAtEnd >= captureCallbackGapMsThreshold))
        {
            return new FlashbackPlaybackStutterClassification(
                "capture_callback_gap",
                $"captureSevereGapsDelta={captureSevereGapDelta} captureMaxIntervalMsEnd={captureMaxCallbackIntervalMsAtEnd:0.##} thresholdMs={captureCallbackGapMsThreshold:0.##}");
        }

        var frameBudgetMs = targetFps > 0 ? 1000.0 / targetFps : 16.67;
        var decodeP99StallThresholdMs = Math.Max(25.0, frameBudgetMs * 2.0);
        var decodeMaxStallThresholdMs = Math.Max(50.0, frameBudgetMs * 4.0);
        if ((cadenceStress && maxDecodeP99MsObserved >= decodeP99StallThresholdMs) ||
            maxDecodeMsObserved >= decodeMaxStallThresholdMs)
        {
            return new FlashbackPlaybackStutterClassification(
                "decode_stall",
                $"decodeP99MsMax={maxDecodeP99MsObserved:0.##} decodeMaxMsObserved={maxDecodeMsObserved:0.##} phaseObserved={FormatOptional(maxDecodePhaseObserved)} p99ThresholdMs={decodeP99StallThresholdMs:0.##} maxThresholdMs={decodeMaxStallThresholdMs:0.##}");
        }

        if (cadenceStress)
        {
            return new FlashbackPlaybackStutterClassification(
                "unknown",
                $"slowPctEnd={slowFramePercentAtEnd:0.##} slowPctMax={maxSlowFramePercentObserved:0.##} droppedFramesDelta={droppedFramesDelta} onePercentLowEnd={onePercentLowFpsAtEnd:0.##} onePercentLowMin={minOnePercentLowFpsObserved:0.##} targetFps={targetFps:0.##}");
        }

        return new FlashbackPlaybackStutterClassification("none", string.Empty);
    }

    private static bool HasFlashbackPlaybackCadenceStress(
        double slowFramePercentAtEnd,
        double maxSlowFramePercentObserved,
        long droppedFramesDelta,
        double onePercentLowFpsAtEnd,
        double minOnePercentLowFpsObserved,
        double targetFps)
    {
        var onePercentLowFloor = targetFps > 0 ? targetFps * 0.80 : 0.0;
        return droppedFramesDelta > 0 ||
               slowFramePercentAtEnd > 0 ||
               maxSlowFramePercentObserved > 0 ||
               (onePercentLowFloor > 0 &&
                onePercentLowFpsAtEnd > 0 &&
                onePercentLowFpsAtEnd < onePercentLowFloor) ||
               (onePercentLowFloor > 0 &&
                minOnePercentLowFpsObserved > 0 &&
                minOnePercentLowFpsObserved < onePercentLowFloor);
    }

    private static double GetFlashbackPlaybackTargetFps(JsonElement snapshot)
    {
        var targetFps = GetDouble(snapshot, "FlashbackPlaybackTargetFps");
        if (targetFps <= 0)
        {
            targetFps = GetDouble(snapshot, "SelectedExactFrameRate");
        }

        if (targetFps <= 0)
        {
            targetFps = GetDouble(snapshot, "ExpectedCaptureFrameRate");
        }

        return targetFps;
    }


}

internal sealed record DiagnosticSessionResultBuildRequest(
    DiagnosticSessionOptions Options,
    DiagnosticSessionScenarioPlan ScenarioPlan,
    string SessionId,
    string Scenario,
    int DurationSeconds,
    int SampleIntervalMs,
    string OutputDirectory,
    string LivePath,
    DateTimeOffset StartedUtc,
    int RunnerProcessId,
    int CommandFailureCount,
    IReadOnlyList<DiagnosticSessionSample> Samples,
    JsonElement InitialSnapshot,
    JsonElement HealthSnapshot,
    JsonElement? Timeline,
    JsonElement? Verification,
    PresentMonProbeResult? PresentMon,
    bool StartedPreview,
    bool EnabledFlashback,
    bool StartedFlashbackPlayback,
    bool StoppedRecordingForVerification,
    IReadOnlyList<string> Actions,
    List<string> Warnings);

internal static class ToolJsonOptions
{
    internal static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };
}

internal static class DiagnosticSessionResultArtifacts
{
    internal static async Task<DiagnosticSessionResultArtifactPaths> WritePreSummaryAsync(
        string outputDirectory,
        string sessionId,
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement? timeline,
        DiagnosticSessionRunState runState)
    {
        var paths = new DiagnosticSessionResultArtifactPaths(
            SummaryPath: Path.Combine(outputDirectory, "summary.json"),
            SamplesPath: Path.Combine(outputDirectory, "samples.json"),
            FrameLedgerPath: Path.Combine(outputDirectory, "frame-ledger.json"),
            TimelinePath: Path.Combine(outputDirectory, "timeline.json"));

        await runState.WriteArtifactBestEffortAsync("write-samples", paths.SamplesPath, samples).ConfigureAwait(false);
        await runState.WriteArtifactBestEffortAsync("write-frame-ledger", paths.FrameLedgerPath, BuildFrameLedgerTrace(sessionId, samples)).ConfigureAwait(false);
        await runState.WriteArtifactBestEffortAsync("write-timeline", paths.TimelinePath, timeline).ConfigureAwait(false);

        return paths;
    }

    private static object BuildFrameLedgerTrace(string sessionId, IReadOnlyList<DiagnosticSessionSample> samples)
    {
        var events = new List<JsonElement>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var sample in samples)
        {
            if (!sample.Snapshot.TryGetProperty("FrameLedgerRecentEvents", out var recentEvents) ||
                recentEvents.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var item in recentEvents.EnumerateArray())
            {
                var key =
                    $"{Get(item, "SourceSequence")}|{Get(item, "Stage")}|{Get(item, "QpcTimestamp")}";
                if (seen.Add(key))
                {
                    events.Add(item.Clone());
                }
            }
        }

        return new
        {
            SessionId = sessionId,
            SampleCount = samples.Count,
            EventCount = events.Count,
            Events = events
        };
    }
}

internal readonly record struct DiagnosticSessionResultArtifactPaths(
    string SummaryPath,
    string SamplesPath,
    string FrameLedgerPath,
    string TimelinePath);

internal static class DiagnosticSessionJsonArtifacts
{
    internal static JsonElement CreateEmptyJsonObject()
    {
        using var document = JsonDocument.Parse("{}");
        return document.RootElement.Clone();
    }

    internal static async Task WriteJsonAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(value, ToolJsonOptions.Pretty);
        await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
    }
}
