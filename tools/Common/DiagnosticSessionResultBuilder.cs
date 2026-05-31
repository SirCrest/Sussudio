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
        var resultProjections = BuildResultProjectionSet(request, runState, analysis);

        return FlattenResultProjectionSet(
            request,
            runState,
            analysis,
            resultProjections,
            artifactPaths,
            completedUtc,
            terminalState);
    }


    private static DiagnosticSessionResult FlattenResultProjectionSet(
        DiagnosticSessionResultBuildRequest request,
        DiagnosticSessionRunState runState,
        DiagnosticSessionResultAnalysis analysis,
        DiagnosticSessionResultProjectionSet resultProjections,
        DiagnosticSessionResultArtifactPaths artifactPaths,
        DateTimeOffset completedUtc,
        string terminalState)
    {
        var healthSummary = analysis.HealthSummary;
        var healthStatus = healthSummary.HealthStatus;
        var likelyStage = healthSummary.LikelyStage;
        var summary = healthSummary.Summary;
        var evidence = healthSummary.Evidence;
        var overviewResult = resultProjections.Overview;
        var captureResult = resultProjections.Capture;
        var flashbackPlaybackResult = resultProjections.FlashbackPlayback;
        var flashbackPlaybackCommandsResult = flashbackPlaybackResult.CommandsResult;
        var flashbackPlaybackCadenceResult = flashbackPlaybackResult.CadenceResult;
        var flashbackPlaybackOnePercentLowResult = flashbackPlaybackResult.OnePercentLowResult;
        var flashbackPlaybackDecodeResult = flashbackPlaybackResult.DecodeResult;
        var flashbackPlaybackAudioMasterResult = flashbackPlaybackResult.AudioMasterResult;
        var flashbackPlaybackStagesResult = flashbackPlaybackResult.StagesResult;
        var flashbackRecordingResult = resultProjections.FlashbackRecording;
        var flashbackExportResult = resultProjections.FlashbackExport;
        var previewResult = resultProjections.Preview;
        var previewSchedulerResult = resultProjections.PreviewScheduler;
        var previewD3DResult = resultProjections.PreviewD3D;
        var previewVisualCadenceResult = resultProjections.PreviewVisualCadence;

        return new DiagnosticSessionResult
        {
            SessionId = request.SessionId,
            Scenario = request.Scenario,
            Success = overviewResult.Success,
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
            SelectedResolutionAtEnd = captureResult.SelectedResolutionAtEnd,
            SelectedFrameRateAtEnd = captureResult.SelectedFrameRateAtEnd,
            SelectedFriendlyFrameRateAtEnd = captureResult.SelectedFriendlyFrameRateAtEnd,
            SelectedExactFrameRateArgAtEnd = captureResult.SelectedExactFrameRateArgAtEnd,
            SelectedVideoFormatAtEnd = captureResult.SelectedVideoFormatAtEnd,
            VideoRequestedSubtypeAtEnd = captureResult.VideoRequestedSubtypeAtEnd,
            VideoNegotiatedSubtypeAtEnd = captureResult.VideoNegotiatedSubtypeAtEnd,
            SourceWidthAtEnd = captureResult.SourceWidthAtEnd,
            SourceHeightAtEnd = captureResult.SourceHeightAtEnd,
            DetectedSourceFrameRateAtEnd = captureResult.DetectedSourceFrameRateAtEnd,
            DetectedSourceFrameRateArgAtEnd = captureResult.DetectedSourceFrameRateArgAtEnd,
            SourceIsHdrAtEnd = captureResult.SourceIsHdrAtEnd,
            SourceTelemetrySummaryAtEnd = captureResult.SourceTelemetrySummaryAtEnd,
            FlashbackPlaybackPendingCommandsAtEnd = flashbackPlaybackCommandsResult.FlashbackPlaybackPendingCommandsAtEnd,
            FlashbackPlaybackMaxPendingCommandsObserved = flashbackPlaybackCommandsResult.FlashbackPlaybackMaxPendingCommandsObserved,
            FlashbackPlaybackMaxCommandQueueLatencyMsObserved = flashbackPlaybackCommandsResult.FlashbackPlaybackMaxCommandQueueLatencyMsObserved,
            FlashbackPlaybackMaxCommandQueueLatencyCommandObserved = flashbackPlaybackCommandsResult.FlashbackPlaybackMaxCommandQueueLatencyCommandObserved,
            FlashbackPlaybackCommandsDroppedAtEnd = flashbackPlaybackCommandsResult.FlashbackPlaybackCommandsDroppedAtEnd,
            FlashbackPlaybackCommandsSkippedNotReadyAtEnd = flashbackPlaybackCommandsResult.FlashbackPlaybackCommandsSkippedNotReadyAtEnd,
            FlashbackPlaybackScrubUpdatesCoalescedAtEnd = flashbackPlaybackCommandsResult.FlashbackPlaybackScrubUpdatesCoalescedAtEnd,
            FlashbackPlaybackSeekCommandsCoalescedAtEnd = flashbackPlaybackCommandsResult.FlashbackPlaybackSeekCommandsCoalescedAtEnd,
            FlashbackPlaybackLastCommandFailureAtEnd = flashbackPlaybackCommandsResult.FlashbackPlaybackLastCommandFailureAtEnd,
            FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd = flashbackPlaybackCommandsResult.FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd,
            FlashbackPlaybackObservedFpsAtEnd = flashbackPlaybackCadenceResult.FlashbackPlaybackObservedFpsAtEnd,
            FlashbackPlaybackMinObservedFpsObserved = flashbackPlaybackCadenceResult.FlashbackPlaybackMinObservedFpsObserved,
            FlashbackPlaybackAvgFrameMsAtEnd = flashbackPlaybackCadenceResult.FlashbackPlaybackAvgFrameMsAtEnd,
            FlashbackPlaybackP99FrameMsAtEnd = flashbackPlaybackCadenceResult.FlashbackPlaybackP99FrameMsAtEnd,
            FlashbackPlaybackMaxFrameMsAtEnd = flashbackPlaybackCadenceResult.FlashbackPlaybackMaxFrameMsAtEnd,
            FlashbackPlaybackOnePercentLowFpsAtEnd = flashbackPlaybackOnePercentLowResult.FlashbackPlaybackOnePercentLowFpsAtEnd,
            FlashbackPlaybackMinOnePercentLowFpsObserved = flashbackPlaybackOnePercentLowResult.FlashbackPlaybackMinOnePercentLowFpsObserved,
            FlashbackPlaybackOnePercentLowSampleWindowObserved = flashbackPlaybackOnePercentLowResult.FlashbackPlaybackOnePercentLowSampleWindowObserved,
            FlashbackPlaybackOnePercentLowMinimumFrames = flashbackPlaybackOnePercentLowResult.FlashbackPlaybackOnePercentLowMinimumFrames,
            FlashbackPlaybackMaxSessionFrameCountObserved = flashbackPlaybackOnePercentLowResult.FlashbackPlaybackMaxSessionFrameCountObserved,
            FlashbackPlaybackMinOnePercentLowOffsetMs = flashbackPlaybackOnePercentLowResult.FlashbackPlaybackMinOnePercentLowOffsetMs,
            FlashbackPlaybackMinOnePercentLowFrameCount = flashbackPlaybackOnePercentLowResult.FlashbackPlaybackMinOnePercentLowFrameCount,
            FlashbackPlaybackMinOnePercentLowP99FrameMs = flashbackPlaybackOnePercentLowResult.FlashbackPlaybackMinOnePercentLowP99FrameMs,
            FlashbackPlaybackMinOnePercentLowMaxFrameMs = flashbackPlaybackOnePercentLowResult.FlashbackPlaybackMinOnePercentLowMaxFrameMs,
            FlashbackPlaybackMinOnePercentLowDecodeP99Ms = flashbackPlaybackOnePercentLowResult.FlashbackPlaybackMinOnePercentLowDecodeP99Ms,
            FlashbackPlaybackMinOnePercentLowDecodeMaxMs = flashbackPlaybackOnePercentLowResult.FlashbackPlaybackMinOnePercentLowDecodeMaxMs,
            FlashbackPlaybackMinOnePercentLowAvDriftMs = flashbackPlaybackOnePercentLowResult.FlashbackPlaybackMinOnePercentLowAvDriftMs,
            FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks = flashbackPlaybackOnePercentLowResult.FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks,
            FlashbackPlaybackMaxP99FrameMsObserved = flashbackPlaybackCadenceResult.FlashbackPlaybackMaxP99FrameMsObserved,
            FlashbackPlaybackMaxFrameMsObserved = flashbackPlaybackCadenceResult.FlashbackPlaybackMaxFrameMsObserved,
            FlashbackPlaybackMaxSlowFramePercentObserved = flashbackPlaybackCadenceResult.FlashbackPlaybackMaxSlowFramePercentObserved,
            FlashbackPlaybackDecodeAvgMsAtEnd = flashbackPlaybackDecodeResult.FlashbackPlaybackDecodeAvgMsAtEnd,
            FlashbackPlaybackDecodeP95MsAtEnd = flashbackPlaybackDecodeResult.FlashbackPlaybackDecodeP95MsAtEnd,
            FlashbackPlaybackDecodeP99MsAtEnd = flashbackPlaybackDecodeResult.FlashbackPlaybackDecodeP99MsAtEnd,
            FlashbackPlaybackDecodeMaxMsAtEnd = flashbackPlaybackDecodeResult.FlashbackPlaybackDecodeMaxMsAtEnd,
            FlashbackPlaybackMaxDecodePhaseAtEnd = flashbackPlaybackDecodeResult.FlashbackPlaybackMaxDecodePhaseAtEnd,
            FlashbackPlaybackMaxDecodeReceiveMsAtEnd = flashbackPlaybackDecodeResult.FlashbackPlaybackMaxDecodeReceiveMsAtEnd,
            FlashbackPlaybackMaxDecodeFeedMsAtEnd = flashbackPlaybackDecodeResult.FlashbackPlaybackMaxDecodeFeedMsAtEnd,
            FlashbackPlaybackMaxDecodeReadMsAtEnd = flashbackPlaybackDecodeResult.FlashbackPlaybackMaxDecodeReadMsAtEnd,
            FlashbackPlaybackMaxDecodeSendMsAtEnd = flashbackPlaybackDecodeResult.FlashbackPlaybackMaxDecodeSendMsAtEnd,
            FlashbackPlaybackMaxDecodeAudioMsAtEnd = flashbackPlaybackDecodeResult.FlashbackPlaybackMaxDecodeAudioMsAtEnd,
            FlashbackPlaybackMaxDecodeConvertMsAtEnd = flashbackPlaybackDecodeResult.FlashbackPlaybackMaxDecodeConvertMsAtEnd,
            FlashbackPlaybackMaxDecodeUtcUnixMsAtEnd = flashbackPlaybackDecodeResult.FlashbackPlaybackMaxDecodeUtcUnixMsAtEnd,
            FlashbackPlaybackMaxDecodePositionMsAtEnd = flashbackPlaybackDecodeResult.FlashbackPlaybackMaxDecodePositionMsAtEnd,
            FlashbackPlaybackMaxDecodeP99MsObserved = flashbackPlaybackDecodeResult.FlashbackPlaybackMaxDecodeP99MsObserved,
            FlashbackPlaybackMaxDecodeMsObserved = flashbackPlaybackDecodeResult.FlashbackPlaybackMaxDecodeMsObserved,
            FlashbackPlaybackMaxDecodePhaseObserved = flashbackPlaybackDecodeResult.FlashbackPlaybackMaxDecodePhaseObserved,
            FlashbackPlaybackMaxDecodeReceiveMsObserved = flashbackPlaybackDecodeResult.FlashbackPlaybackMaxDecodeReceiveMsObserved,
            FlashbackPlaybackMaxDecodeFeedMsObserved = flashbackPlaybackDecodeResult.FlashbackPlaybackMaxDecodeFeedMsObserved,
            FlashbackPlaybackMaxDecodeReadMsObserved = flashbackPlaybackDecodeResult.FlashbackPlaybackMaxDecodeReadMsObserved,
            FlashbackPlaybackMaxDecodeSendMsObserved = flashbackPlaybackDecodeResult.FlashbackPlaybackMaxDecodeSendMsObserved,
            FlashbackPlaybackMaxDecodeAudioMsObserved = flashbackPlaybackDecodeResult.FlashbackPlaybackMaxDecodeAudioMsObserved,
            FlashbackPlaybackMaxDecodeConvertMsObserved = flashbackPlaybackDecodeResult.FlashbackPlaybackMaxDecodeConvertMsObserved,
            FlashbackPlaybackMaxDecodeUtcUnixMsObserved = flashbackPlaybackDecodeResult.FlashbackPlaybackMaxDecodeUtcUnixMsObserved,
            FlashbackPlaybackMaxDecodePositionMsObserved = flashbackPlaybackDecodeResult.FlashbackPlaybackMaxDecodePositionMsObserved,
            FlashbackPlaybackFrameCountAtEnd = flashbackPlaybackCadenceResult.FlashbackPlaybackFrameCountAtEnd,
            FlashbackPlaybackLateFramesAtEnd = flashbackPlaybackCadenceResult.FlashbackPlaybackLateFramesAtEnd,
            FlashbackPlaybackSlowFramesAtEnd = flashbackPlaybackCadenceResult.FlashbackPlaybackSlowFramesAtEnd,
            FlashbackPlaybackSlowFramePercentAtEnd = flashbackPlaybackCadenceResult.FlashbackPlaybackSlowFramePercentAtEnd,
            FlashbackPlaybackDroppedFramesAtEnd = flashbackPlaybackCadenceResult.FlashbackPlaybackDroppedFramesAtEnd,
            FlashbackPlaybackDroppedFramesDelta = flashbackPlaybackCadenceResult.FlashbackPlaybackDroppedFramesDelta,
            FlashbackPlaybackAudioMasterDelayDoublesAtEnd = flashbackPlaybackAudioMasterResult.FlashbackPlaybackAudioMasterDelayDoublesAtEnd,
            FlashbackPlaybackAudioMasterDelayShrinksAtEnd = flashbackPlaybackAudioMasterResult.FlashbackPlaybackAudioMasterDelayShrinksAtEnd,
            FlashbackPlaybackAudioMasterFallbacksAtEnd = flashbackPlaybackAudioMasterResult.FlashbackPlaybackAudioMasterFallbacksAtEnd,
            FlashbackPlaybackAudioMasterUnavailableFallbacksAtEnd = flashbackPlaybackAudioMasterResult.FlashbackPlaybackAudioMasterUnavailableFallbacksAtEnd,
            FlashbackPlaybackAudioMasterStaleFallbacksAtEnd = flashbackPlaybackAudioMasterResult.FlashbackPlaybackAudioMasterStaleFallbacksAtEnd,
            FlashbackPlaybackAudioMasterDriftOutlierFallbacksAtEnd = flashbackPlaybackAudioMasterResult.FlashbackPlaybackAudioMasterDriftOutlierFallbacksAtEnd,
            FlashbackPlaybackAudioMasterLastFallbackReasonAtEnd = flashbackPlaybackAudioMasterResult.FlashbackPlaybackAudioMasterLastFallbackReasonAtEnd,
            FlashbackPlaybackAudioMasterLastFallbackClockAgeMsAtEnd = flashbackPlaybackAudioMasterResult.FlashbackPlaybackAudioMasterLastFallbackClockAgeMsAtEnd,
            FlashbackPlaybackMaxAudioMasterDelayDoublesObserved = flashbackPlaybackAudioMasterResult.FlashbackPlaybackMaxAudioMasterDelayDoublesObserved,
            FlashbackPlaybackMaxAudioMasterDelayShrinksObserved = flashbackPlaybackAudioMasterResult.FlashbackPlaybackMaxAudioMasterDelayShrinksObserved,
            FlashbackPlaybackMaxAudioMasterFallbacksObserved = flashbackPlaybackAudioMasterResult.FlashbackPlaybackMaxAudioMasterFallbacksObserved,
            FlashbackPlaybackMaxAudioBufferedDurationMsObserved = flashbackPlaybackAudioMasterResult.FlashbackPlaybackMaxAudioBufferedDurationMsObserved,
            FlashbackPlaybackMaxAudioQueueDurationMsObserved = flashbackPlaybackAudioMasterResult.FlashbackPlaybackMaxAudioQueueDurationMsObserved,
            FlashbackPlaybackMaxAbsAvDriftMsObserved = flashbackPlaybackAudioMasterResult.FlashbackPlaybackMaxAbsAvDriftMsObserved,
            FlashbackPlaybackSubmitFailuresAtEnd = flashbackPlaybackStagesResult.FlashbackPlaybackSubmitFailuresAtEnd,
            FlashbackPlaybackSubmitFailuresDelta = flashbackPlaybackStagesResult.FlashbackPlaybackSubmitFailuresDelta,
            FlashbackPlaybackSegmentSwitchesAtEnd = flashbackPlaybackStagesResult.FlashbackPlaybackSegmentSwitchesAtEnd,
            FlashbackPlaybackFmp4ReopensAtEnd = flashbackPlaybackStagesResult.FlashbackPlaybackFmp4ReopensAtEnd,
            FlashbackPlaybackWriteHeadWaitsAtEnd = flashbackPlaybackStagesResult.FlashbackPlaybackWriteHeadWaitsAtEnd,
            FlashbackPlaybackNearLiveSnapsAtEnd = flashbackPlaybackStagesResult.FlashbackPlaybackNearLiveSnapsAtEnd,
            FlashbackPlaybackDecodeErrorSnapsAtEnd = flashbackPlaybackStagesResult.FlashbackPlaybackDecodeErrorSnapsAtEnd,
            FlashbackPlaybackLastWriteHeadWaitGapMsAtEnd = flashbackPlaybackStagesResult.FlashbackPlaybackLastWriteHeadWaitGapMsAtEnd,
            FlashbackPlaybackSeekForwardDecodeCapHitsAtEnd = flashbackPlaybackStagesResult.FlashbackPlaybackSeekForwardDecodeCapHitsAtEnd,
            FlashbackPlaybackSeekForwardDecodeCapHitsDelta = flashbackPlaybackStagesResult.FlashbackPlaybackSeekForwardDecodeCapHitsDelta,
            FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd = flashbackPlaybackStagesResult.FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd,
            FlashbackRecordingBackendObserved = flashbackRecordingResult.FlashbackRecordingBackendObserved,
            FlashbackRecordingFileGrowthObserved = flashbackRecordingResult.FlashbackRecordingFileGrowthObserved,
            FlashbackRecordingVideoFramesSubmittedDelta = flashbackRecordingResult.FlashbackRecordingVideoFramesSubmittedDelta,
            FlashbackRecordingVideoEncoderPacketsWrittenDelta = flashbackRecordingResult.FlashbackRecordingVideoEncoderPacketsWrittenDelta,
            FlashbackRecordingIntegritySequenceGapsAtEnd = flashbackRecordingResult.FlashbackRecordingIntegritySequenceGapsAtEnd,
            FlashbackRecordingIntegrityQueueDroppedFramesAtEnd = flashbackRecordingResult.FlashbackRecordingIntegrityQueueDroppedFramesAtEnd,
            FlashbackRecordingIntegritySequenceGapsDelta = flashbackRecordingResult.FlashbackRecordingIntegritySequenceGapsDelta,
            FlashbackRecordingIntegrityQueueDroppedFramesDelta = flashbackRecordingResult.FlashbackRecordingIntegrityQueueDroppedFramesDelta,
            FlashbackExportObserved = flashbackExportResult.FlashbackExportObserved,
            FlashbackExportActiveAtEnd = flashbackExportResult.FlashbackExportActiveAtEnd,
            FlashbackExportStatusAtEnd = flashbackExportResult.FlashbackExportStatusAtEnd,
            FlashbackExportMessageAtEnd = flashbackExportResult.FlashbackExportMessageAtEnd,
            FlashbackExportFailureKindAtEnd = flashbackExportResult.FlashbackExportFailureKindAtEnd,
            FlashbackExportOutputPathAtEnd = flashbackExportResult.FlashbackExportOutputPathAtEnd,
            FlashbackExportForceRotateFallbacksAtEnd = flashbackExportResult.FlashbackExportForceRotateFallbacksAtEnd,
            FlashbackExportForceRotateFallbacksDelta = flashbackExportResult.FlashbackExportForceRotateFallbacksDelta,
            FlashbackExportLastForceRotateFallbackSegmentsAtEnd = flashbackExportResult.FlashbackExportLastForceRotateFallbackSegmentsAtEnd,
            LastExportIdAtEnd = flashbackExportResult.LastExportIdAtEnd,
            LastExportSuccessAtEnd = flashbackExportResult.LastExportSuccessAtEnd,
            LastExportMessageAtEnd = flashbackExportResult.LastExportMessageAtEnd,
            FlashbackExportMaxElapsedMsObserved = flashbackExportResult.FlashbackExportMaxElapsedMsObserved,
            FlashbackExportMaxLastProgressAgeMsObserved = flashbackExportResult.FlashbackExportMaxLastProgressAgeMsObserved,
            FlashbackExportMaxOutputBytesObserved = flashbackExportResult.FlashbackExportMaxOutputBytesObserved,
            FlashbackExportMaxThroughputBytesPerSecObserved = flashbackExportResult.FlashbackExportMaxThroughputBytesPerSecObserved,
            PreviewCadenceOnePercentLowFpsAtEnd = previewResult.PreviewCadenceOnePercentLowFpsAtEnd,
            PreviewCadenceMinOnePercentLowFpsObserved = previewResult.PreviewCadenceMinOnePercentLowFpsObserved,
            PreviewSchedulerDroppedAtEnd = previewSchedulerResult.PreviewSchedulerDroppedAtEnd,
            PreviewSchedulerDeadlineDropsAtEnd = previewSchedulerResult.PreviewSchedulerDeadlineDropsAtEnd,
            PreviewSchedulerClearedDropsAtEnd = previewSchedulerResult.PreviewSchedulerClearedDropsAtEnd,
            PreviewSchedulerUnderflowsAtEnd = previewSchedulerResult.PreviewSchedulerUnderflowsAtEnd,
            PreviewSchedulerResumeReprimesAtEnd = previewSchedulerResult.PreviewSchedulerResumeReprimesAtEnd,
            PreviewSchedulerDroppedDelta = previewSchedulerResult.PreviewSchedulerDroppedDelta,
            PreviewSchedulerDeadlineDropsDelta = previewSchedulerResult.PreviewSchedulerDeadlineDropsDelta,
            PreviewSchedulerClearedDropsDelta = previewSchedulerResult.PreviewSchedulerClearedDropsDelta,
            PreviewSchedulerUnderflowsDelta = previewSchedulerResult.PreviewSchedulerUnderflowsDelta,
            PreviewSchedulerResumeReprimesDelta = previewSchedulerResult.PreviewSchedulerResumeReprimesDelta,
            PreviewSchedulerLastDropReasonAtEnd = previewSchedulerResult.PreviewSchedulerLastDropReasonAtEnd,
            PreviewSchedulerLastUnderflowReasonAtEnd = previewSchedulerResult.PreviewSchedulerLastUnderflowReasonAtEnd,
            PreviewSchedulerLastUnderflowInputAgeMsAtEnd = previewSchedulerResult.PreviewSchedulerLastUnderflowInputAgeMsAtEnd,
            PreviewSchedulerLastUnderflowOutputAgeMsAtEnd = previewSchedulerResult.PreviewSchedulerLastUnderflowOutputAgeMsAtEnd,
            PreviewSchedulerMaxScheduleLateMsObserved = previewSchedulerResult.PreviewSchedulerMaxScheduleLateMsObserved,
            PreviewSchedulerScheduleLateDelta = previewSchedulerResult.PreviewSchedulerScheduleLateDelta,
            PreviewD3DFrameStatsMissedRefreshDelta = previewD3DResult.PreviewD3DFrameStatsMissedRefreshDelta,
            PreviewD3DFrameStatsFailureDelta = previewD3DResult.PreviewD3DFrameStatsFailureDelta,
            PreviewD3DMaxRecentSlowFramesObserved = previewD3DResult.PreviewD3DMaxRecentSlowFramesObserved,
            PreviewD3DLatestSlowFrameReason = previewD3DResult.PreviewD3DLatestSlowFrameReason,
            PreviewD3DLatestSlowFrameOverBudgetMs = previewD3DResult.PreviewD3DLatestSlowFrameOverBudgetMs,
            PreviewD3DLatestSlowFramePresentIntervalMs = previewD3DResult.PreviewD3DLatestSlowFramePresentIntervalMs,
            PreviewD3DLatestSlowFrameTotalFrameCpuMs = previewD3DResult.PreviewD3DLatestSlowFrameTotalFrameCpuMs,
            PreviewD3DLatestSlowFramePresentCallMs = previewD3DResult.PreviewD3DLatestSlowFramePresentCallMs,
            PreviewD3DLatestSlowFramePendingFrameCount = previewD3DResult.PreviewD3DLatestSlowFramePendingFrameCount,
            PreviewD3DInputUploadCpuP99MsAtEnd = previewD3DResult.PreviewD3DInputUploadCpuP99MsAtEnd,
            PreviewD3DInputUploadCpuMaxMsObserved = previewD3DResult.PreviewD3DInputUploadCpuMaxMsObserved,
            PreviewD3DRenderSubmitCpuP99MsAtEnd = previewD3DResult.PreviewD3DRenderSubmitCpuP99MsAtEnd,
            PreviewD3DRenderSubmitCpuMaxMsObserved = previewD3DResult.PreviewD3DRenderSubmitCpuMaxMsObserved,
            PreviewD3DPresentCallP99MsAtEnd = previewD3DResult.PreviewD3DPresentCallP99MsAtEnd,
            PreviewD3DPresentCallMaxMsObserved = previewD3DResult.PreviewD3DPresentCallMaxMsObserved,
            PreviewD3DTotalFrameCpuP99MsAtEnd = previewD3DResult.PreviewD3DTotalFrameCpuP99MsAtEnd,
            PreviewD3DTotalFrameCpuMaxMsObserved = previewD3DResult.PreviewD3DTotalFrameCpuMaxMsObserved,
            VisualCadenceOutputFpsAtEnd = previewVisualCadenceResult.VisualCadenceOutputFpsAtEnd,
            VisualCadenceChangeFpsAtEnd = previewVisualCadenceResult.VisualCadenceChangeFpsAtEnd,
            VisualCadenceMinChangeFpsObserved = previewVisualCadenceResult.VisualCadenceMinChangeFpsObserved,
            VisualCadenceRepeatPercentAtEnd = previewVisualCadenceResult.VisualCadenceRepeatPercentAtEnd,
            VisualCadenceMaxRepeatPercentObserved = previewVisualCadenceResult.VisualCadenceMaxRepeatPercentObserved,
            VisualCadenceRepeatFramesAtEnd = previewVisualCadenceResult.VisualCadenceRepeatFramesAtEnd,
            VisualCadenceLongestRepeatRunAtEnd = previewVisualCadenceResult.VisualCadenceLongestRepeatRunAtEnd,
            ProcessCpuPercentAtEnd = overviewResult.ProcessCpuPercentAtEnd,
            ProcessCpuMaxPercentObserved = overviewResult.ProcessCpuMaxPercentObserved,
            RecordingVerificationRun = overviewResult.RecordingVerificationRun,
            RecordingVerificationSucceeded = overviewResult.RecordingVerificationSucceeded,
            RecordingVerificationMessage = overviewResult.RecordingVerificationMessage,
            PresentMon = overviewResult.PresentMon,
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

    private readonly record struct DiagnosticSessionOverviewResultProjection(
        bool Success,
        double ProcessCpuPercentAtEnd,
        double ProcessCpuMaxPercentObserved,
        bool RecordingVerificationRun,
        bool? RecordingVerificationSucceeded,
        string? RecordingVerificationMessage,
        PresentMonProbeResult? PresentMon);

    private readonly record struct DiagnosticSessionCaptureResultProjection(
        string SelectedResolutionAtEnd,
        double SelectedFrameRateAtEnd,
        string SelectedFriendlyFrameRateAtEnd,
        string SelectedExactFrameRateArgAtEnd,
        string SelectedVideoFormatAtEnd,
        string VideoRequestedSubtypeAtEnd,
        string VideoNegotiatedSubtypeAtEnd,
        int SourceWidthAtEnd,
        int SourceHeightAtEnd,
        double DetectedSourceFrameRateAtEnd,
        string DetectedSourceFrameRateArgAtEnd,
        bool SourceIsHdrAtEnd,
        string SourceTelemetrySummaryAtEnd);

    private readonly record struct DiagnosticSessionFlashbackRecordingResultProjection(
        bool FlashbackRecordingBackendObserved,
        bool FlashbackRecordingFileGrowthObserved,
        long FlashbackRecordingVideoFramesSubmittedDelta,
        long FlashbackRecordingVideoEncoderPacketsWrittenDelta,
        long FlashbackRecordingIntegritySequenceGapsAtEnd,
        long FlashbackRecordingIntegrityQueueDroppedFramesAtEnd,
        long FlashbackRecordingIntegritySequenceGapsDelta,
        long FlashbackRecordingIntegrityQueueDroppedFramesDelta);

    private readonly record struct DiagnosticSessionFlashbackExportResultProjection(
        bool FlashbackExportObserved,
        bool FlashbackExportActiveAtEnd,
        string FlashbackExportStatusAtEnd,
        string FlashbackExportMessageAtEnd,
        string FlashbackExportFailureKindAtEnd,
        string FlashbackExportOutputPathAtEnd,
        long FlashbackExportForceRotateFallbacksAtEnd,
        long FlashbackExportForceRotateFallbacksDelta,
        int FlashbackExportLastForceRotateFallbackSegmentsAtEnd,
        long LastExportIdAtEnd,
        string LastExportSuccessAtEnd,
        string LastExportMessageAtEnd,
        long FlashbackExportMaxElapsedMsObserved,
        long FlashbackExportMaxLastProgressAgeMsObserved,
        long FlashbackExportMaxOutputBytesObserved,
        double FlashbackExportMaxThroughputBytesPerSecObserved);

    private readonly record struct DiagnosticSessionPreviewResultProjection(
        double PreviewCadenceOnePercentLowFpsAtEnd,
        double PreviewCadenceMinOnePercentLowFpsObserved);

    private readonly record struct DiagnosticSessionPreviewSchedulerResultProjection(
        long PreviewSchedulerDroppedAtEnd,
        long PreviewSchedulerDeadlineDropsAtEnd,
        long PreviewSchedulerClearedDropsAtEnd,
        long PreviewSchedulerUnderflowsAtEnd,
        long PreviewSchedulerResumeReprimesAtEnd,
        long PreviewSchedulerDroppedDelta,
        long PreviewSchedulerDeadlineDropsDelta,
        long PreviewSchedulerClearedDropsDelta,
        long PreviewSchedulerUnderflowsDelta,
        long PreviewSchedulerResumeReprimesDelta,
        string PreviewSchedulerLastDropReasonAtEnd,
        string PreviewSchedulerLastUnderflowReasonAtEnd,
        double PreviewSchedulerLastUnderflowInputAgeMsAtEnd,
        double PreviewSchedulerLastUnderflowOutputAgeMsAtEnd,
        double PreviewSchedulerMaxScheduleLateMsObserved,
        long PreviewSchedulerScheduleLateDelta);

    private readonly record struct DiagnosticSessionPreviewD3DResultProjection(
        long PreviewD3DFrameStatsMissedRefreshDelta,
        long PreviewD3DFrameStatsFailureDelta,
        int PreviewD3DMaxRecentSlowFramesObserved,
        string PreviewD3DLatestSlowFrameReason,
        double PreviewD3DLatestSlowFrameOverBudgetMs,
        double PreviewD3DLatestSlowFramePresentIntervalMs,
        double PreviewD3DLatestSlowFrameTotalFrameCpuMs,
        double PreviewD3DLatestSlowFramePresentCallMs,
        int PreviewD3DLatestSlowFramePendingFrameCount,
        double PreviewD3DInputUploadCpuP99MsAtEnd,
        double PreviewD3DInputUploadCpuMaxMsObserved,
        double PreviewD3DRenderSubmitCpuP99MsAtEnd,
        double PreviewD3DRenderSubmitCpuMaxMsObserved,
        double PreviewD3DPresentCallP99MsAtEnd,
        double PreviewD3DPresentCallMaxMsObserved,
        double PreviewD3DTotalFrameCpuP99MsAtEnd,
        double PreviewD3DTotalFrameCpuMaxMsObserved);

    private readonly record struct DiagnosticSessionPreviewVisualCadenceResultProjection(
        double VisualCadenceOutputFpsAtEnd,
        double VisualCadenceChangeFpsAtEnd,
        double VisualCadenceMinChangeFpsObserved,
        double VisualCadenceRepeatPercentAtEnd,
        double VisualCadenceMaxRepeatPercentObserved,
        long VisualCadenceRepeatFramesAtEnd,
        long VisualCadenceLongestRepeatRunAtEnd);

    private readonly record struct DiagnosticSessionResultProjectionSet(
        DiagnosticSessionOverviewResultProjection Overview,
        DiagnosticSessionCaptureResultProjection Capture,
        DiagnosticSessionFlashbackPlaybackResultProjection FlashbackPlayback,
        DiagnosticSessionFlashbackRecordingResultProjection FlashbackRecording,
        DiagnosticSessionFlashbackExportResultProjection FlashbackExport,
        DiagnosticSessionPreviewResultProjection Preview,
        DiagnosticSessionPreviewSchedulerResultProjection PreviewScheduler,
        DiagnosticSessionPreviewD3DResultProjection PreviewD3D,
        DiagnosticSessionPreviewVisualCadenceResultProjection PreviewVisualCadence);

    private static DiagnosticSessionResultProjectionSet BuildResultProjectionSet(
        DiagnosticSessionResultBuildRequest request,
        DiagnosticSessionRunState runState,
        DiagnosticSessionResultAnalysis analysis)
    {
        return new DiagnosticSessionResultProjectionSet(
            Overview: BuildOverviewResultProjection(request, runState, analysis),
            Capture: BuildCaptureResultProjection(analysis),
            FlashbackPlayback: BuildFlashbackPlaybackResultProjection(analysis),
            FlashbackRecording: BuildFlashbackRecordingResultProjection(analysis),
            FlashbackExport: BuildFlashbackExportResultProjection(analysis),
            Preview: BuildPreviewResultProjection(analysis),
            PreviewScheduler: BuildPreviewSchedulerResultProjection(analysis),
            PreviewD3D: BuildPreviewD3DResultProjection(analysis),
            PreviewVisualCadence: BuildPreviewVisualCadenceResultProjection(analysis));
    }

    private static DiagnosticSessionOverviewResultProjection BuildOverviewResultProjection(
        DiagnosticSessionResultBuildRequest request,
        DiagnosticSessionRunState runState,
        DiagnosticSessionResultAnalysis analysis)
    {
        var lastSnapshot = analysis.LastSnapshot;
        var verificationSucceeded = request.Verification.HasValue
            ? GetBool(request.Verification.Value, "Succeeded")
            : (bool?)null;
        var processCpuMaxPercentObserved = GetProcessCpuMaxPercentObserved(request.Samples, lastSnapshot);

        return new DiagnosticSessionOverviewResultProjection(
            Success: DetermineDiagnosticSessionSuccess(request, runState, analysis, verificationSucceeded),
            ProcessCpuPercentAtEnd: GetDouble(lastSnapshot, "ProcessCpuPercent"),
            ProcessCpuMaxPercentObserved: processCpuMaxPercentObserved,
            RecordingVerificationRun: request.Verification.HasValue,
            RecordingVerificationSucceeded: verificationSucceeded,
            RecordingVerificationMessage: request.Verification.HasValue
                ? GetString(request.Verification.Value, "Message") ?? string.Empty
                : null,
            PresentMon: request.PresentMon);
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
        analysis.DiagnosticHealthSucceeded &&
        (request.PresentMon is null || request.PresentMon.Success) &&
        (!verificationSucceeded.HasValue || verificationSucceeded.Value) &&
        analysis.FlashbackWarningsSucceeded;

    private static DiagnosticSessionCaptureResultProjection BuildCaptureResultProjection(
        DiagnosticSessionResultAnalysis analysis)
    {
        var lastSnapshot = analysis.LastSnapshot;

        return new DiagnosticSessionCaptureResultProjection(
            SelectedResolutionAtEnd: GetString(lastSnapshot, "SelectedResolution") ?? string.Empty,
            SelectedFrameRateAtEnd: GetDouble(lastSnapshot, "SelectedFrameRate"),
            SelectedFriendlyFrameRateAtEnd: GetString(lastSnapshot, "SelectedFriendlyFrameRate") ?? string.Empty,
            SelectedExactFrameRateArgAtEnd: GetString(lastSnapshot, "SelectedExactFrameRateArg") ?? string.Empty,
            SelectedVideoFormatAtEnd: GetString(lastSnapshot, "SelectedVideoFormat") ?? string.Empty,
            VideoRequestedSubtypeAtEnd: GetString(lastSnapshot, "VideoRequestedSubtype") ?? string.Empty,
            VideoNegotiatedSubtypeAtEnd: GetString(lastSnapshot, "VideoNegotiatedSubtype") ?? string.Empty,
            SourceWidthAtEnd: (int)(GetNullableLong(lastSnapshot, "SourceWidth") ?? 0),
            SourceHeightAtEnd: (int)(GetNullableLong(lastSnapshot, "SourceHeight") ?? 0),
            DetectedSourceFrameRateAtEnd: GetDouble(lastSnapshot, "DetectedSourceFrameRate"),
            DetectedSourceFrameRateArgAtEnd: GetString(lastSnapshot, "DetectedSourceFrameRateArg") ?? string.Empty,
            SourceIsHdrAtEnd: GetBool(lastSnapshot, "SourceIsHdr"),
            SourceTelemetrySummaryAtEnd: GetString(lastSnapshot, "SourceTelemetrySummaryText") ?? string.Empty);
    }

    private static DiagnosticSessionFlashbackRecordingResultProjection BuildFlashbackRecordingResultProjection(
        DiagnosticSessionResultAnalysis analysis)
    {
        var recordingMetrics = analysis.RecordingMetrics;

        return new DiagnosticSessionFlashbackRecordingResultProjection(
            FlashbackRecordingBackendObserved: recordingMetrics.BackendObserved,
            FlashbackRecordingFileGrowthObserved: recordingMetrics.FileGrowthObserved,
            FlashbackRecordingVideoFramesSubmittedDelta: recordingMetrics.VideoFramesSubmittedDelta,
            FlashbackRecordingVideoEncoderPacketsWrittenDelta: recordingMetrics.VideoEncoderPacketsWrittenDelta,
            FlashbackRecordingIntegritySequenceGapsAtEnd: recordingMetrics.IntegritySequenceGapsAtEnd,
            FlashbackRecordingIntegrityQueueDroppedFramesAtEnd: recordingMetrics.IntegrityQueueDroppedFramesAtEnd,
            FlashbackRecordingIntegritySequenceGapsDelta: recordingMetrics.IntegritySequenceGapsDelta,
            FlashbackRecordingIntegrityQueueDroppedFramesDelta: recordingMetrics.IntegrityQueueDroppedFramesDelta);
    }

    private static DiagnosticSessionFlashbackExportResultProjection BuildFlashbackExportResultProjection(
        DiagnosticSessionResultAnalysis analysis)
    {
        var exportMetrics = analysis.ExportMetrics;

        return new DiagnosticSessionFlashbackExportResultProjection(
            FlashbackExportObserved: exportMetrics.Observed,
            FlashbackExportActiveAtEnd: exportMetrics.ActiveAtEnd,
            FlashbackExportStatusAtEnd: exportMetrics.StatusAtEnd,
            FlashbackExportMessageAtEnd: exportMetrics.MessageAtEnd,
            FlashbackExportFailureKindAtEnd: exportMetrics.FailureKindAtEnd,
            FlashbackExportOutputPathAtEnd: exportMetrics.OutputPathAtEnd,
            FlashbackExportForceRotateFallbacksAtEnd: exportMetrics.ForceRotateFallbacksAtEnd,
            FlashbackExportForceRotateFallbacksDelta: exportMetrics.ForceRotateFallbacksDelta,
            FlashbackExportLastForceRotateFallbackSegmentsAtEnd: exportMetrics.LastForceRotateFallbackSegmentsAtEnd,
            LastExportIdAtEnd: exportMetrics.LastExportIdAtEnd,
            LastExportSuccessAtEnd: exportMetrics.LastSuccessAtEnd,
            LastExportMessageAtEnd: exportMetrics.LastMessageAtEnd,
            FlashbackExportMaxElapsedMsObserved: exportMetrics.MaxElapsedMsObserved,
            FlashbackExportMaxLastProgressAgeMsObserved: exportMetrics.MaxLastProgressAgeMsObserved,
            FlashbackExportMaxOutputBytesObserved: exportMetrics.MaxOutputBytesObserved,
            FlashbackExportMaxThroughputBytesPerSecObserved: exportMetrics.MaxThroughputBytesPerSecObserved);
    }

    private static DiagnosticSessionPreviewResultProjection BuildPreviewResultProjection(
        DiagnosticSessionResultAnalysis analysis)
    {
        var previewCadenceMetrics = analysis.PreviewCadenceMetrics;

        return new DiagnosticSessionPreviewResultProjection(
            PreviewCadenceOnePercentLowFpsAtEnd: previewCadenceMetrics.OnePercentLowFpsAtEnd,
            PreviewCadenceMinOnePercentLowFpsObserved: previewCadenceMetrics.MinOnePercentLowFpsObserved);
    }

    private static DiagnosticSessionPreviewSchedulerResultProjection BuildPreviewSchedulerResultProjection(
        DiagnosticSessionResultAnalysis analysis)
    {
        var previewScheduler = analysis.PreviewScheduler;

        return new DiagnosticSessionPreviewSchedulerResultProjection(
            PreviewSchedulerDroppedAtEnd: previewScheduler.DroppedAtEnd,
            PreviewSchedulerDeadlineDropsAtEnd: previewScheduler.DeadlineDropsAtEnd,
            PreviewSchedulerClearedDropsAtEnd: previewScheduler.ClearedDropsAtEnd,
            PreviewSchedulerUnderflowsAtEnd: previewScheduler.UnderflowsAtEnd,
            PreviewSchedulerResumeReprimesAtEnd: previewScheduler.ResumeReprimesAtEnd,
            PreviewSchedulerDroppedDelta: previewScheduler.DroppedDelta,
            PreviewSchedulerDeadlineDropsDelta: previewScheduler.DeadlineDropsDelta,
            PreviewSchedulerClearedDropsDelta: previewScheduler.ClearedDropsDelta,
            PreviewSchedulerUnderflowsDelta: previewScheduler.UnderflowsDelta,
            PreviewSchedulerResumeReprimesDelta: previewScheduler.ResumeReprimesDelta,
            PreviewSchedulerLastDropReasonAtEnd: previewScheduler.LastDropReasonAtEnd,
            PreviewSchedulerLastUnderflowReasonAtEnd: previewScheduler.LastUnderflowReasonAtEnd,
            PreviewSchedulerLastUnderflowInputAgeMsAtEnd: previewScheduler.LastUnderflowInputAgeMsAtEnd,
            PreviewSchedulerLastUnderflowOutputAgeMsAtEnd: previewScheduler.LastUnderflowOutputAgeMsAtEnd,
            PreviewSchedulerMaxScheduleLateMsObserved: previewScheduler.MaxScheduleLateMsObserved,
            PreviewSchedulerScheduleLateDelta: previewScheduler.ScheduleLateDelta);
    }

    private static DiagnosticSessionPreviewD3DResultProjection BuildPreviewD3DResultProjection(
        DiagnosticSessionResultAnalysis analysis)
    {
        var previewD3DMetrics = analysis.PreviewD3DMetrics;

        return new DiagnosticSessionPreviewD3DResultProjection(
            PreviewD3DFrameStatsMissedRefreshDelta: previewD3DMetrics.MissedRefreshDelta,
            PreviewD3DFrameStatsFailureDelta: previewD3DMetrics.StatsFailureDelta,
            PreviewD3DMaxRecentSlowFramesObserved: previewD3DMetrics.MaxRecentSlowFramesObserved,
            PreviewD3DLatestSlowFrameReason: previewD3DMetrics.LatestSlowFrameReason,
            PreviewD3DLatestSlowFrameOverBudgetMs: previewD3DMetrics.LatestSlowFrameOverBudgetMs,
            PreviewD3DLatestSlowFramePresentIntervalMs: previewD3DMetrics.LatestSlowFramePresentIntervalMs,
            PreviewD3DLatestSlowFrameTotalFrameCpuMs: previewD3DMetrics.LatestSlowFrameTotalFrameCpuMs,
            PreviewD3DLatestSlowFramePresentCallMs: previewD3DMetrics.LatestSlowFramePresentCallMs,
            PreviewD3DLatestSlowFramePendingFrameCount: previewD3DMetrics.LatestSlowFramePendingFrameCount,
            PreviewD3DInputUploadCpuP99MsAtEnd: previewD3DMetrics.InputUploadCpuP99MsAtEnd,
            PreviewD3DInputUploadCpuMaxMsObserved: previewD3DMetrics.InputUploadCpuMaxMsObserved,
            PreviewD3DRenderSubmitCpuP99MsAtEnd: previewD3DMetrics.RenderSubmitCpuP99MsAtEnd,
            PreviewD3DRenderSubmitCpuMaxMsObserved: previewD3DMetrics.RenderSubmitCpuMaxMsObserved,
            PreviewD3DPresentCallP99MsAtEnd: previewD3DMetrics.PresentCallP99MsAtEnd,
            PreviewD3DPresentCallMaxMsObserved: previewD3DMetrics.PresentCallMaxMsObserved,
            PreviewD3DTotalFrameCpuP99MsAtEnd: previewD3DMetrics.TotalFrameCpuP99MsAtEnd,
            PreviewD3DTotalFrameCpuMaxMsObserved: previewD3DMetrics.TotalFrameCpuMaxMsObserved);
    }

    private static DiagnosticSessionPreviewVisualCadenceResultProjection BuildPreviewVisualCadenceResultProjection(
        DiagnosticSessionResultAnalysis analysis)
    {
        var visualCadenceMetrics = analysis.VisualCadenceMetrics;

        return new DiagnosticSessionPreviewVisualCadenceResultProjection(
            VisualCadenceOutputFpsAtEnd: visualCadenceMetrics.OutputFpsAtEnd,
            VisualCadenceChangeFpsAtEnd: visualCadenceMetrics.ChangeFpsAtEnd,
            VisualCadenceMinChangeFpsObserved: visualCadenceMetrics.MinChangeFpsObserved,
            VisualCadenceRepeatPercentAtEnd: visualCadenceMetrics.RepeatPercentAtEnd,
            VisualCadenceMaxRepeatPercentObserved: visualCadenceMetrics.MaxRepeatPercentObserved,
            VisualCadenceRepeatFramesAtEnd: visualCadenceMetrics.RepeatFramesAtEnd,
            VisualCadenceLongestRepeatRunAtEnd: visualCadenceMetrics.LongestRepeatRunAtEnd);
    }

    private readonly record struct DiagnosticSessionFlashbackPlaybackResultProjection(
        DiagnosticSessionFlashbackPlaybackCommandsResultProjection CommandsResult,
        DiagnosticSessionFlashbackPlaybackCadenceResultProjection CadenceResult,
        DiagnosticSessionFlashbackPlaybackOnePercentLowResultProjection OnePercentLowResult,
        DiagnosticSessionFlashbackPlaybackDecodeResultProjection DecodeResult,
        DiagnosticSessionFlashbackPlaybackAudioMasterResultProjection AudioMasterResult,
        DiagnosticSessionFlashbackPlaybackStagesResultProjection StagesResult);

    private static DiagnosticSessionFlashbackPlaybackResultProjection BuildFlashbackPlaybackResultProjection(
        DiagnosticSessionResultAnalysis analysis)
    {
        var playbackSessionMetrics = analysis.PlaybackSessionMetrics;
        var playbackResultMetrics = analysis.PlaybackResultMetrics;
        var commandsResult = BuildFlashbackPlaybackCommandsResultProjection(playbackResultMetrics);
        var cadenceResult = BuildFlashbackPlaybackCadenceResultProjection(playbackSessionMetrics, playbackResultMetrics);
        var onePercentLowResult = BuildFlashbackPlaybackOnePercentLowResultProjection(playbackSessionMetrics, playbackResultMetrics);
        var decodeResult = BuildFlashbackPlaybackDecodeResultProjection(playbackSessionMetrics, playbackResultMetrics);
        var audioMasterResult = BuildFlashbackPlaybackAudioMasterResultProjection(playbackSessionMetrics, playbackResultMetrics);
        var stagesResult = BuildFlashbackPlaybackStagesResultProjection(playbackSessionMetrics, playbackResultMetrics);

        return new DiagnosticSessionFlashbackPlaybackResultProjection(
            CommandsResult: commandsResult,
            CadenceResult: cadenceResult,
            OnePercentLowResult: onePercentLowResult,
            DecodeResult: decodeResult,
            AudioMasterResult: audioMasterResult,
            StagesResult: stagesResult);
    }

    private readonly record struct DiagnosticSessionFlashbackPlaybackCommandsResultProjection(
        int FlashbackPlaybackPendingCommandsAtEnd,
        int FlashbackPlaybackMaxPendingCommandsObserved,
        int FlashbackPlaybackMaxCommandQueueLatencyMsObserved,
        string FlashbackPlaybackMaxCommandQueueLatencyCommandObserved,
        long FlashbackPlaybackCommandsDroppedAtEnd,
        long FlashbackPlaybackCommandsSkippedNotReadyAtEnd,
        long FlashbackPlaybackScrubUpdatesCoalescedAtEnd,
        long FlashbackPlaybackSeekCommandsCoalescedAtEnd,
        string FlashbackPlaybackLastCommandFailureAtEnd,
        long FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd);

    private static DiagnosticSessionFlashbackPlaybackCommandsResultProjection BuildFlashbackPlaybackCommandsResultProjection(
        FlashbackPlaybackResultMetrics playbackResultMetrics) =>
        new(
            FlashbackPlaybackPendingCommandsAtEnd: playbackResultMetrics.PendingCommandsAtEnd,
            FlashbackPlaybackMaxPendingCommandsObserved: playbackResultMetrics.MaxPendingCommandsObserved,
            FlashbackPlaybackMaxCommandQueueLatencyMsObserved: playbackResultMetrics.MaxCommandQueueLatencyMsObserved,
            FlashbackPlaybackMaxCommandQueueLatencyCommandObserved: playbackResultMetrics.MaxCommandQueueLatencyCommandObserved,
            FlashbackPlaybackCommandsDroppedAtEnd: playbackResultMetrics.CommandsDroppedAtEnd,
            FlashbackPlaybackCommandsSkippedNotReadyAtEnd: playbackResultMetrics.CommandsSkippedNotReadyAtEnd,
            FlashbackPlaybackScrubUpdatesCoalescedAtEnd: playbackResultMetrics.ScrubUpdatesCoalescedAtEnd,
            FlashbackPlaybackSeekCommandsCoalescedAtEnd: playbackResultMetrics.SeekCommandsCoalescedAtEnd,
            FlashbackPlaybackLastCommandFailureAtEnd: playbackResultMetrics.LastCommandFailureAtEnd,
            FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd: playbackResultMetrics.LastCommandFailureUtcUnixMsAtEnd);

    private readonly record struct DiagnosticSessionFlashbackPlaybackCadenceResultProjection(
        double FlashbackPlaybackObservedFpsAtEnd,
        double FlashbackPlaybackMinObservedFpsObserved,
        double FlashbackPlaybackAvgFrameMsAtEnd,
        double FlashbackPlaybackP99FrameMsAtEnd,
        double FlashbackPlaybackMaxFrameMsAtEnd,
        double FlashbackPlaybackMaxP99FrameMsObserved,
        double FlashbackPlaybackMaxFrameMsObserved,
        double FlashbackPlaybackMaxSlowFramePercentObserved,
        long FlashbackPlaybackFrameCountAtEnd,
        long FlashbackPlaybackLateFramesAtEnd,
        long FlashbackPlaybackSlowFramesAtEnd,
        double FlashbackPlaybackSlowFramePercentAtEnd,
        long FlashbackPlaybackDroppedFramesAtEnd,
        long FlashbackPlaybackDroppedFramesDelta);

    private static DiagnosticSessionFlashbackPlaybackCadenceResultProjection BuildFlashbackPlaybackCadenceResultProjection(
        FlashbackPlaybackSessionMetrics playbackSessionMetrics,
        FlashbackPlaybackResultMetrics playbackResultMetrics) =>
        new(
            FlashbackPlaybackObservedFpsAtEnd: playbackResultMetrics.ObservedFpsAtEnd,
            FlashbackPlaybackMinObservedFpsObserved: playbackSessionMetrics.MinObservedFpsObserved,
            FlashbackPlaybackAvgFrameMsAtEnd: playbackResultMetrics.AvgFrameMsAtEnd,
            FlashbackPlaybackP99FrameMsAtEnd: playbackResultMetrics.P99FrameMsAtEnd,
            FlashbackPlaybackMaxFrameMsAtEnd: playbackResultMetrics.MaxFrameMsAtEnd,
            FlashbackPlaybackMaxP99FrameMsObserved: playbackSessionMetrics.MaxP99FrameMsObserved,
            FlashbackPlaybackMaxFrameMsObserved: playbackSessionMetrics.MaxFrameMsObserved,
            FlashbackPlaybackMaxSlowFramePercentObserved: playbackSessionMetrics.MaxSlowFramePercentObserved,
            FlashbackPlaybackFrameCountAtEnd: playbackResultMetrics.FrameCountAtEnd,
            FlashbackPlaybackLateFramesAtEnd: playbackResultMetrics.LateFramesAtEnd,
            FlashbackPlaybackSlowFramesAtEnd: playbackResultMetrics.SlowFramesAtEnd,
            FlashbackPlaybackSlowFramePercentAtEnd: playbackResultMetrics.SlowFramePercentAtEnd,
            FlashbackPlaybackDroppedFramesAtEnd: playbackResultMetrics.DroppedFramesAtEnd,
            FlashbackPlaybackDroppedFramesDelta: playbackSessionMetrics.DroppedFramesDelta);

    private readonly record struct DiagnosticSessionFlashbackPlaybackOnePercentLowResultProjection(
        double FlashbackPlaybackOnePercentLowFpsAtEnd,
        double FlashbackPlaybackMinOnePercentLowFpsObserved,
        bool FlashbackPlaybackOnePercentLowSampleWindowObserved,
        long FlashbackPlaybackOnePercentLowMinimumFrames,
        long FlashbackPlaybackMaxSessionFrameCountObserved,
        long FlashbackPlaybackMinOnePercentLowOffsetMs,
        long FlashbackPlaybackMinOnePercentLowFrameCount,
        double FlashbackPlaybackMinOnePercentLowP99FrameMs,
        double FlashbackPlaybackMinOnePercentLowMaxFrameMs,
        double FlashbackPlaybackMinOnePercentLowDecodeP99Ms,
        double FlashbackPlaybackMinOnePercentLowDecodeMaxMs,
        double FlashbackPlaybackMinOnePercentLowAvDriftMs,
        long FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks);

    private static DiagnosticSessionFlashbackPlaybackOnePercentLowResultProjection BuildFlashbackPlaybackOnePercentLowResultProjection(
        FlashbackPlaybackSessionMetrics playbackSessionMetrics,
        FlashbackPlaybackResultMetrics playbackResultMetrics) =>
        new(
            FlashbackPlaybackOnePercentLowFpsAtEnd: playbackResultMetrics.OnePercentLowFpsAtEnd,
            FlashbackPlaybackMinOnePercentLowFpsObserved: playbackSessionMetrics.MinOnePercentLowFpsObserved,
            FlashbackPlaybackOnePercentLowSampleWindowObserved: playbackSessionMetrics.OnePercentLowSampleWindowObserved,
            FlashbackPlaybackOnePercentLowMinimumFrames: playbackSessionMetrics.MinimumOnePercentLowFrameCount,
            FlashbackPlaybackMaxSessionFrameCountObserved: playbackSessionMetrics.MaxSessionFrameCountObserved,
            FlashbackPlaybackMinOnePercentLowOffsetMs: playbackSessionMetrics.MinOnePercentLowOffsetMs,
            FlashbackPlaybackMinOnePercentLowFrameCount: playbackSessionMetrics.MinOnePercentLowFrameCount,
            FlashbackPlaybackMinOnePercentLowP99FrameMs: playbackSessionMetrics.MinOnePercentLowP99FrameMs,
            FlashbackPlaybackMinOnePercentLowMaxFrameMs: playbackSessionMetrics.MinOnePercentLowMaxFrameMs,
            FlashbackPlaybackMinOnePercentLowDecodeP99Ms: playbackSessionMetrics.MinOnePercentLowDecodeP99Ms,
            FlashbackPlaybackMinOnePercentLowDecodeMaxMs: playbackSessionMetrics.MinOnePercentLowDecodeMaxMs,
            FlashbackPlaybackMinOnePercentLowAvDriftMs: playbackSessionMetrics.MinOnePercentLowAvDriftMs,
            FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks: playbackSessionMetrics.MinOnePercentLowAudioMasterFallbacks);

    private readonly record struct DiagnosticSessionFlashbackPlaybackAudioMasterResultProjection(
        long FlashbackPlaybackAudioMasterDelayDoublesAtEnd,
        long FlashbackPlaybackAudioMasterDelayShrinksAtEnd,
        long FlashbackPlaybackAudioMasterFallbacksAtEnd,
        long FlashbackPlaybackAudioMasterUnavailableFallbacksAtEnd,
        long FlashbackPlaybackAudioMasterStaleFallbacksAtEnd,
        long FlashbackPlaybackAudioMasterDriftOutlierFallbacksAtEnd,
        string FlashbackPlaybackAudioMasterLastFallbackReasonAtEnd,
        double FlashbackPlaybackAudioMasterLastFallbackClockAgeMsAtEnd,
        long FlashbackPlaybackMaxAudioMasterDelayDoublesObserved,
        long FlashbackPlaybackMaxAudioMasterDelayShrinksObserved,
        long FlashbackPlaybackMaxAudioMasterFallbacksObserved,
        double FlashbackPlaybackMaxAudioBufferedDurationMsObserved,
        double FlashbackPlaybackMaxAudioQueueDurationMsObserved,
        double FlashbackPlaybackMaxAbsAvDriftMsObserved);

    private static DiagnosticSessionFlashbackPlaybackAudioMasterResultProjection BuildFlashbackPlaybackAudioMasterResultProjection(
        FlashbackPlaybackSessionMetrics playbackSessionMetrics,
        FlashbackPlaybackResultMetrics playbackResultMetrics) =>
        new(
            FlashbackPlaybackAudioMasterDelayDoublesAtEnd: playbackResultMetrics.AudioMasterDelayDoublesAtEnd,
            FlashbackPlaybackAudioMasterDelayShrinksAtEnd: playbackResultMetrics.AudioMasterDelayShrinksAtEnd,
            FlashbackPlaybackAudioMasterFallbacksAtEnd: playbackResultMetrics.AudioMasterFallbacksAtEnd,
            FlashbackPlaybackAudioMasterUnavailableFallbacksAtEnd: playbackResultMetrics.AudioMasterUnavailableFallbacksAtEnd,
            FlashbackPlaybackAudioMasterStaleFallbacksAtEnd: playbackResultMetrics.AudioMasterStaleFallbacksAtEnd,
            FlashbackPlaybackAudioMasterDriftOutlierFallbacksAtEnd: playbackResultMetrics.AudioMasterDriftOutlierFallbacksAtEnd,
            FlashbackPlaybackAudioMasterLastFallbackReasonAtEnd: playbackResultMetrics.AudioMasterLastFallbackReasonAtEnd,
            FlashbackPlaybackAudioMasterLastFallbackClockAgeMsAtEnd: playbackResultMetrics.AudioMasterLastFallbackClockAgeMsAtEnd,
            FlashbackPlaybackMaxAudioMasterDelayDoublesObserved: playbackSessionMetrics.MaxAudioMasterDelayDoublesObserved,
            FlashbackPlaybackMaxAudioMasterDelayShrinksObserved: playbackSessionMetrics.MaxAudioMasterDelayShrinksObserved,
            FlashbackPlaybackMaxAudioMasterFallbacksObserved: playbackSessionMetrics.MaxAudioMasterFallbacksObserved,
            FlashbackPlaybackMaxAudioBufferedDurationMsObserved: playbackSessionMetrics.MaxAudioBufferedDurationMsObserved,
            FlashbackPlaybackMaxAudioQueueDurationMsObserved: playbackSessionMetrics.MaxAudioQueueDurationMsObserved,
            FlashbackPlaybackMaxAbsAvDriftMsObserved: playbackSessionMetrics.MaxAbsAvDriftMsObserved);

    private readonly record struct DiagnosticSessionFlashbackPlaybackDecodeResultProjection(
        double FlashbackPlaybackDecodeAvgMsAtEnd,
        double FlashbackPlaybackDecodeP95MsAtEnd,
        double FlashbackPlaybackDecodeP99MsAtEnd,
        double FlashbackPlaybackDecodeMaxMsAtEnd,
        string FlashbackPlaybackMaxDecodePhaseAtEnd,
        double FlashbackPlaybackMaxDecodeReceiveMsAtEnd,
        double FlashbackPlaybackMaxDecodeFeedMsAtEnd,
        double FlashbackPlaybackMaxDecodeReadMsAtEnd,
        double FlashbackPlaybackMaxDecodeSendMsAtEnd,
        double FlashbackPlaybackMaxDecodeAudioMsAtEnd,
        double FlashbackPlaybackMaxDecodeConvertMsAtEnd,
        long FlashbackPlaybackMaxDecodeUtcUnixMsAtEnd,
        long FlashbackPlaybackMaxDecodePositionMsAtEnd,
        double FlashbackPlaybackMaxDecodeP99MsObserved,
        double FlashbackPlaybackMaxDecodeMsObserved,
        string FlashbackPlaybackMaxDecodePhaseObserved,
        double FlashbackPlaybackMaxDecodeReceiveMsObserved,
        double FlashbackPlaybackMaxDecodeFeedMsObserved,
        double FlashbackPlaybackMaxDecodeReadMsObserved,
        double FlashbackPlaybackMaxDecodeSendMsObserved,
        double FlashbackPlaybackMaxDecodeAudioMsObserved,
        double FlashbackPlaybackMaxDecodeConvertMsObserved,
        long FlashbackPlaybackMaxDecodeUtcUnixMsObserved,
        long FlashbackPlaybackMaxDecodePositionMsObserved);

    private static DiagnosticSessionFlashbackPlaybackDecodeResultProjection BuildFlashbackPlaybackDecodeResultProjection(
        FlashbackPlaybackSessionMetrics playbackSessionMetrics,
        FlashbackPlaybackResultMetrics playbackResultMetrics) =>
        new(
            FlashbackPlaybackDecodeAvgMsAtEnd: playbackResultMetrics.DecodeAvgMsAtEnd,
            FlashbackPlaybackDecodeP95MsAtEnd: playbackResultMetrics.DecodeP95MsAtEnd,
            FlashbackPlaybackDecodeP99MsAtEnd: playbackResultMetrics.DecodeP99MsAtEnd,
            FlashbackPlaybackDecodeMaxMsAtEnd: playbackResultMetrics.DecodeMaxMsAtEnd,
            FlashbackPlaybackMaxDecodePhaseAtEnd: playbackResultMetrics.MaxDecodePhaseAtEnd,
            FlashbackPlaybackMaxDecodeReceiveMsAtEnd: playbackResultMetrics.MaxDecodeReceiveMsAtEnd,
            FlashbackPlaybackMaxDecodeFeedMsAtEnd: playbackResultMetrics.MaxDecodeFeedMsAtEnd,
            FlashbackPlaybackMaxDecodeReadMsAtEnd: playbackResultMetrics.MaxDecodeReadMsAtEnd,
            FlashbackPlaybackMaxDecodeSendMsAtEnd: playbackResultMetrics.MaxDecodeSendMsAtEnd,
            FlashbackPlaybackMaxDecodeAudioMsAtEnd: playbackResultMetrics.MaxDecodeAudioMsAtEnd,
            FlashbackPlaybackMaxDecodeConvertMsAtEnd: playbackResultMetrics.MaxDecodeConvertMsAtEnd,
            FlashbackPlaybackMaxDecodeUtcUnixMsAtEnd: playbackResultMetrics.MaxDecodeUtcUnixMsAtEnd,
            FlashbackPlaybackMaxDecodePositionMsAtEnd: playbackResultMetrics.MaxDecodePositionMsAtEnd,
            FlashbackPlaybackMaxDecodeP99MsObserved: playbackSessionMetrics.MaxDecodeP99MsObserved,
            FlashbackPlaybackMaxDecodeMsObserved: playbackSessionMetrics.MaxDecodeMsObserved,
            FlashbackPlaybackMaxDecodePhaseObserved: playbackSessionMetrics.MaxDecodePhaseObserved,
            FlashbackPlaybackMaxDecodeReceiveMsObserved: playbackSessionMetrics.MaxDecodeReceiveMsObserved,
            FlashbackPlaybackMaxDecodeFeedMsObserved: playbackSessionMetrics.MaxDecodeFeedMsObserved,
            FlashbackPlaybackMaxDecodeReadMsObserved: playbackSessionMetrics.MaxDecodeReadMsObserved,
            FlashbackPlaybackMaxDecodeSendMsObserved: playbackSessionMetrics.MaxDecodeSendMsObserved,
            FlashbackPlaybackMaxDecodeAudioMsObserved: playbackSessionMetrics.MaxDecodeAudioMsObserved,
            FlashbackPlaybackMaxDecodeConvertMsObserved: playbackSessionMetrics.MaxDecodeConvertMsObserved,
            FlashbackPlaybackMaxDecodeUtcUnixMsObserved: playbackSessionMetrics.MaxDecodeUtcUnixMsObserved,
            FlashbackPlaybackMaxDecodePositionMsObserved: playbackSessionMetrics.MaxDecodePositionMsObserved);

    private readonly record struct DiagnosticSessionFlashbackPlaybackStagesResultProjection(
        long FlashbackPlaybackSubmitFailuresAtEnd,
        long FlashbackPlaybackSubmitFailuresDelta,
        long FlashbackPlaybackSegmentSwitchesAtEnd,
        long FlashbackPlaybackFmp4ReopensAtEnd,
        long FlashbackPlaybackWriteHeadWaitsAtEnd,
        long FlashbackPlaybackNearLiveSnapsAtEnd,
        long FlashbackPlaybackDecodeErrorSnapsAtEnd,
        long FlashbackPlaybackLastWriteHeadWaitGapMsAtEnd,
        long FlashbackPlaybackSeekForwardDecodeCapHitsAtEnd,
        long FlashbackPlaybackSeekForwardDecodeCapHitsDelta,
        bool FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd);

    private static DiagnosticSessionFlashbackPlaybackStagesResultProjection BuildFlashbackPlaybackStagesResultProjection(
        FlashbackPlaybackSessionMetrics playbackSessionMetrics,
        FlashbackPlaybackResultMetrics playbackResultMetrics) =>
        new(
            FlashbackPlaybackSubmitFailuresAtEnd: playbackResultMetrics.SubmitFailuresAtEnd,
            FlashbackPlaybackSubmitFailuresDelta: playbackSessionMetrics.SubmitFailuresDelta,
            FlashbackPlaybackSegmentSwitchesAtEnd: playbackResultMetrics.SegmentSwitchesAtEnd,
            FlashbackPlaybackFmp4ReopensAtEnd: playbackResultMetrics.Fmp4ReopensAtEnd,
            FlashbackPlaybackWriteHeadWaitsAtEnd: playbackResultMetrics.WriteHeadWaitsAtEnd,
            FlashbackPlaybackNearLiveSnapsAtEnd: playbackResultMetrics.NearLiveSnapsAtEnd,
            FlashbackPlaybackDecodeErrorSnapsAtEnd: playbackResultMetrics.DecodeErrorSnapsAtEnd,
            FlashbackPlaybackLastWriteHeadWaitGapMsAtEnd: playbackResultMetrics.LastWriteHeadWaitGapMsAtEnd,
            FlashbackPlaybackSeekForwardDecodeCapHitsAtEnd: playbackResultMetrics.SeekForwardDecodeCapHitsAtEnd,
            FlashbackPlaybackSeekForwardDecodeCapHitsDelta: playbackResultMetrics.SeekForwardDecodeCapHitsDelta,
            FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd: playbackResultMetrics.LastSeekHitForwardDecodeCapAtEnd);
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
