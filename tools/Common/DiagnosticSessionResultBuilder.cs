using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;
using static Sussudio.Tools.DiagnosticSessionResultArtifacts;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
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
