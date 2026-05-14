using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionResultArtifacts;
using static Sussudio.Tools.DiagnosticSessionSummaryWriter;

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
        var lastSnapshot = analysis.LastSnapshot;
        var healthStatus = analysis.HealthStatus;
        var likelyStage = analysis.LikelyStage;
        var summary = analysis.Summary;
        var evidence = analysis.Evidence;
        var playbackSessionMetrics = analysis.PlaybackSessionMetrics;
        var playbackResultMetrics = analysis.PlaybackResultMetrics;
        var flashbackExportForceRotateFallbacksAtEnd = analysis.FlashbackExportForceRotateFallbacksAtEnd;
        var flashbackExportForceRotateFallbacksDelta = analysis.FlashbackExportForceRotateFallbacksDelta;
        var flashbackExportLastForceRotateFallbackSegmentsAtEnd = analysis.FlashbackExportLastForceRotateFallbackSegmentsAtEnd;
        var recordingMetrics = analysis.RecordingMetrics;
        var exportMetrics = analysis.ExportMetrics;
        var previewCadenceMetrics = analysis.PreviewCadenceMetrics;
        var previewD3DMetrics = analysis.PreviewD3DMetrics;
        var visualCadenceMetrics = analysis.VisualCadenceMetrics;
        var previewSchedulerDroppedAtEnd = analysis.PreviewSchedulerDroppedAtEnd;
        var previewSchedulerDeadlineDropsAtEnd = analysis.PreviewSchedulerDeadlineDropsAtEnd;
        var previewSchedulerClearedDropsAtEnd = analysis.PreviewSchedulerClearedDropsAtEnd;
        var previewSchedulerUnderflowsAtEnd = analysis.PreviewSchedulerUnderflowsAtEnd;
        var previewSchedulerResumeReprimesAtEnd = analysis.PreviewSchedulerResumeReprimesAtEnd;
        var previewSchedulerDroppedDelta = analysis.PreviewSchedulerDroppedDelta;
        var previewSchedulerDeadlineDropsDelta = analysis.PreviewSchedulerDeadlineDropsDelta;
        var previewSchedulerClearedDropsDelta = analysis.PreviewSchedulerClearedDropsDelta;
        var previewSchedulerUnderflowsDelta = analysis.PreviewSchedulerUnderflowsDelta;
        var previewSchedulerResumeReprimesDelta = analysis.PreviewSchedulerResumeReprimesDelta;
        var previewSchedulerScheduleLateDelta = analysis.PreviewSchedulerScheduleLateDelta;
        var previewSchedulerMaxScheduleLateMsObserved = analysis.PreviewSchedulerMaxScheduleLateMsObserved;
        var diagnosticHealthSucceeded = analysis.DiagnosticHealthSucceeded;
        var flashbackWarningsSucceeded = analysis.FlashbackWarningsSucceeded;
        var processCpuMaxPercentObserved = analysis.ProcessCpuMaxPercentObserved;

        var artifactPaths = await WritePreSummaryAsync(
                request.OutputDirectory,
                request.SessionId,
                samples,
                request.Timeline,
                runState)
            .ConfigureAwait(false);

        var verificationSucceeded = request.Verification.HasValue
            ? GetBool(request.Verification.Value, "Succeeded")
            : (bool?)null;
        var completedUtc = DateTimeOffset.UtcNow;
        var terminalState = runState.GetTerminalState();
        runState.SetStage("summary");
        var result = new DiagnosticSessionResult
        {
            SessionId = request.SessionId,
            Scenario = request.Scenario,
            Success = request.CommandFailureCount == 0 &&
                      runState.TerminalException is null &&
                      diagnosticHealthSucceeded &&
                      (request.PresentMon is null || request.PresentMon.Success) &&
                      (!verificationSucceeded.HasValue || verificationSucceeded.Value) &&
                      flashbackWarningsSucceeded,
            StartedUtc = request.StartedUtc,
            CompletedUtc = completedUtc,
            TerminalState = terminalState,
            LastStage = runState.GetResultLastStage(),
            UnhandledException = runState.TerminalException is null ? null : DiagnosticSessionRunState.FormatTerminalException(runState.TerminalException),
            RunnerProcessId = request.RunnerProcessId,
            DurationSeconds = request.DurationSeconds,
            SampleIntervalMs = request.SampleIntervalMs,
            SampleCount = samples.Count,
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
            FlashbackExportForceRotateFallbacksAtEnd = flashbackExportForceRotateFallbacksAtEnd,
            FlashbackExportForceRotateFallbacksDelta = flashbackExportForceRotateFallbacksDelta,
            FlashbackExportLastForceRotateFallbackSegmentsAtEnd = flashbackExportLastForceRotateFallbackSegmentsAtEnd,
            LastExportIdAtEnd = exportMetrics.LastExportIdAtEnd,
            LastExportSuccessAtEnd = exportMetrics.LastSuccessAtEnd,
            LastExportMessageAtEnd = exportMetrics.LastMessageAtEnd,
            FlashbackExportMaxElapsedMsObserved = exportMetrics.MaxElapsedMsObserved,
            FlashbackExportMaxLastProgressAgeMsObserved = exportMetrics.MaxLastProgressAgeMsObserved,
            FlashbackExportMaxOutputBytesObserved = exportMetrics.MaxOutputBytesObserved,
            FlashbackExportMaxThroughputBytesPerSecObserved = exportMetrics.MaxThroughputBytesPerSecObserved,
            PreviewCadenceOnePercentLowFpsAtEnd = previewCadenceMetrics.OnePercentLowFpsAtEnd,
            PreviewCadenceMinOnePercentLowFpsObserved = previewCadenceMetrics.MinOnePercentLowFpsObserved,
            PreviewSchedulerDroppedAtEnd = previewSchedulerDroppedAtEnd,
            PreviewSchedulerDeadlineDropsAtEnd = previewSchedulerDeadlineDropsAtEnd,
            PreviewSchedulerClearedDropsAtEnd = previewSchedulerClearedDropsAtEnd,
            PreviewSchedulerUnderflowsAtEnd = previewSchedulerUnderflowsAtEnd,
            PreviewSchedulerResumeReprimesAtEnd = previewSchedulerResumeReprimesAtEnd,
            PreviewSchedulerDroppedDelta = previewSchedulerDroppedDelta,
            PreviewSchedulerDeadlineDropsDelta = previewSchedulerDeadlineDropsDelta,
            PreviewSchedulerClearedDropsDelta = previewSchedulerClearedDropsDelta,
            PreviewSchedulerUnderflowsDelta = previewSchedulerUnderflowsDelta,
            PreviewSchedulerResumeReprimesDelta = previewSchedulerResumeReprimesDelta,
            PreviewSchedulerLastDropReasonAtEnd = GetString(lastSnapshot, "MjpegPreviewJitterLastDropReason") ?? string.Empty,
            PreviewSchedulerLastUnderflowReasonAtEnd = GetString(lastSnapshot, "MjpegPreviewJitterLastUnderflowReason") ?? string.Empty,
            PreviewSchedulerLastUnderflowInputAgeMsAtEnd = GetDouble(lastSnapshot, "MjpegPreviewJitterLastUnderflowInputAgeMs"),
            PreviewSchedulerLastUnderflowOutputAgeMsAtEnd = GetDouble(lastSnapshot, "MjpegPreviewJitterLastUnderflowOutputAgeMs"),
            PreviewSchedulerMaxScheduleLateMsObserved = previewSchedulerMaxScheduleLateMsObserved,
            PreviewSchedulerScheduleLateDelta = previewSchedulerScheduleLateDelta,
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
            ProcessCpuMaxPercentObserved = processCpuMaxPercentObserved,
            RecordingVerificationRun = request.Verification.HasValue,
            RecordingVerificationSucceeded = verificationSucceeded,
            RecordingVerificationMessage = request.Verification.HasValue
                ? GetString(request.Verification.Value, "Message") ?? string.Empty
                : null,
            PresentMon = request.PresentMon,
            Actions = request.Actions.ToArray(),
            Warnings = warnings.ToArray()
        };

        return await WriteAsync(result, runState, warnings).ConfigureAwait(false);
    }
}
