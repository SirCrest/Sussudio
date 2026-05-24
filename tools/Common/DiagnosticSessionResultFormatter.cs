using System.Globalization;
using System.Text;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;

namespace Sussudio.Tools;

public static class DiagnosticSessionResultFormatter
{
    public static string Format(DiagnosticSessionResult result)
    {
        var builder = new StringBuilder();
        AppendOverview(builder, result);
        AppendCaptureMode(builder, result);
        AppendRecordingVerification(builder, result);
        AppendPresentMon(builder, result);
        AppendFlashbackSections(builder, result);
        AppendPreviewSections(builder, result);
        AppendProcessPerformance(builder, result);
        AppendArtifacts(builder, result);
        AppendActionsAndWarnings(builder, result);
        return builder.ToString().TrimEnd();
    }

    private static void AppendOverview(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine($"== Diagnostic Session: {(result.Success ? "PASS" : "FAIL")} ==");
        builder.AppendLine($"Scenario: {result.Scenario} | Duration: {result.DurationSeconds}s | Samples: {result.SampleCount} @ {result.SampleIntervalMs}ms");
        builder.AppendLine($"Terminal: {result.TerminalState} | LastStage: {result.LastStage} | RunnerPid: {result.RunnerProcessId}");
        if (!string.IsNullOrWhiteSpace(result.UnhandledException))
        {
            builder.AppendLine($"Terminal Exception: {result.UnhandledException}");
        }

        builder.AppendLine($"Health: {result.HealthStatus} | Stage: {result.LikelyStage}");
        if (!string.IsNullOrWhiteSpace(result.Summary))
        {
            builder.AppendLine($"Summary: {result.Summary}");
        }

        if (!string.IsNullOrWhiteSpace(result.Evidence))
        {
            builder.AppendLine($"Evidence: {result.Evidence}");
        }
    }

    private static void AppendCaptureMode(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Capture Mode: " +
            $"selected={FormatOptional(result.SelectedResolutionAtEnd)} @{FormatFrameRate(result.SelectedFrameRateAtEnd, result.SelectedFriendlyFrameRateAtEnd, result.SelectedExactFrameRateArgAtEnd)} " +
            $"format={FormatOptional(result.SelectedVideoFormatAtEnd)} requested={FormatOptional(result.VideoRequestedSubtypeAtEnd)} negotiated={FormatOptional(result.VideoNegotiatedSubtypeAtEnd)} " +
            $"source={result.SourceWidthAtEnd}x{result.SourceHeightAtEnd} @{FormatFrameRate(result.DetectedSourceFrameRateAtEnd, string.Empty, result.DetectedSourceFrameRateArgAtEnd)} " +
            $"hdr={result.SourceIsHdrAtEnd} telemetry={FormatOptional(result.SourceTelemetrySummaryAtEnd)}");
    }

    private static string FormatFrameRate(double fps, string friendlyFps, string exactArg)
    {
        var display = !string.IsNullOrWhiteSpace(friendlyFps)
            ? friendlyFps
            : fps > 0
                ? fps.ToString("0.###", CultureInfo.InvariantCulture)
                : "0";
        return !string.IsNullOrWhiteSpace(exactArg)
            ? $"{display}fps ({exactArg})"
            : $"{display}fps";
    }

    private static void AppendRecordingVerification(StringBuilder builder, DiagnosticSessionResult result)
    {
        if (result.RecordingVerificationRun)
        {
            var status = result.RecordingVerificationSucceeded == true ? "PASS" : "FAIL";
            builder.AppendLine($"Recording Verification: {status} | {result.RecordingVerificationMessage}");
        }
    }

    private static void AppendPresentMon(StringBuilder builder, DiagnosticSessionResult result)
    {
        if (result.PresentMon is not null)
        {
            builder.AppendLine($"PresentMon: {(result.PresentMon.Success ? "PASS" : "FAIL")} | {result.PresentMon.Message}");
        }
    }

    private static void AppendFlashbackSections(StringBuilder builder, DiagnosticSessionResult result)
    {
        AppendFlashbackPlaybackCommands(builder, result);
        AppendFlashbackPlaybackPerformance(builder, result);
        AppendFlashbackPlaybackDecode(builder, result);
        AppendFlashbackPlaybackStages(builder, result);
        AppendFlashbackRecording(builder, result);
        AppendFlashbackExport(builder, result);
    }

    private static void AppendFlashbackPlaybackCommands(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Flashback Playback Commands: " +
            $"pendingEnd={result.FlashbackPlaybackPendingCommandsAtEnd} " +
            $"maxPending={result.FlashbackPlaybackMaxPendingCommandsObserved} " +
            $"maxLatencyMs={result.FlashbackPlaybackMaxCommandQueueLatencyMsObserved} " +
            $"maxLatencyCommand={FormatOptional(result.FlashbackPlaybackMaxCommandQueueLatencyCommandObserved)} " +
            $"droppedEnd={result.FlashbackPlaybackCommandsDroppedAtEnd} " +
            $"skippedEnd={result.FlashbackPlaybackCommandsSkippedNotReadyAtEnd} " +
            $"coalescedScrubEnd={result.FlashbackPlaybackScrubUpdatesCoalescedAtEnd} " +
            $"coalescedSeekEnd={result.FlashbackPlaybackSeekCommandsCoalescedAtEnd} " +
            $"failureEnd={FormatOptional(result.FlashbackPlaybackLastCommandFailureAtEnd)} " +
            $"failureUtcEnd={result.FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd}");
    }

    private static void AppendFlashbackPlaybackPerformance(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Flashback Playback Perf: " +
            BuildFlashbackPlaybackCadencePerformanceText(result) + " " +
            BuildFlashbackPlaybackAudioMasterPerformanceText(result) + " " +
            BuildFlashbackPlaybackSubmitPerformanceText(result));
    }

    private static string BuildFlashbackPlaybackSubmitPerformanceText(DiagnosticSessionResult result)
        => $"submitFailuresEnd={result.FlashbackPlaybackSubmitFailuresAtEnd} " +
           $"submitFailuresDelta={result.FlashbackPlaybackSubmitFailuresDelta}";

    private static string BuildFlashbackPlaybackCadencePerformanceText(DiagnosticSessionResult result)
        =>
            $"fpsEnd={result.FlashbackPlaybackObservedFpsAtEnd:0.##} " +
            $"fpsMin={result.FlashbackPlaybackMinObservedFpsObserved:0.##} " +
            $"avgFrameMsEnd={result.FlashbackPlaybackAvgFrameMsAtEnd:0.##} " +
            $"p99FrameMsEnd={result.FlashbackPlaybackP99FrameMsAtEnd:0.##} " +
            $"maxFrameMsEnd={result.FlashbackPlaybackMaxFrameMsAtEnd:0.##} " +
            BuildFlashbackPlaybackOnePercentLowPerformanceText(result) + " " +
            $"p99FrameMsMax={result.FlashbackPlaybackMaxP99FrameMsObserved:0.##} " +
            $"maxFrameMsObserved={result.FlashbackPlaybackMaxFrameMsObserved:0.##} " +
            $"framesEnd={result.FlashbackPlaybackFrameCountAtEnd} " +
            $"lateEnd={result.FlashbackPlaybackLateFramesAtEnd} " +
            $"slowEnd={result.FlashbackPlaybackSlowFramesAtEnd} " +
            $"slowPctEnd={result.FlashbackPlaybackSlowFramePercentAtEnd:0.##} " +
            $"slowPctMax={result.FlashbackPlaybackMaxSlowFramePercentObserved:0.##} " +
            $"droppedFramesEnd={result.FlashbackPlaybackDroppedFramesAtEnd} " +
            $"droppedFramesDelta={result.FlashbackPlaybackDroppedFramesDelta}";

    private static string BuildFlashbackPlaybackOnePercentLowPerformanceText(DiagnosticSessionResult result)
        =>
            $"onePercentLowFpsEnd={result.FlashbackPlaybackOnePercentLowFpsAtEnd:0.##} " +
            $"onePercentLowFpsMin={result.FlashbackPlaybackMinOnePercentLowFpsObserved:0.##} " +
            $"onePercentLowWindow={result.FlashbackPlaybackOnePercentLowSampleWindowObserved} " +
            $"onePercentLowMinRequiredFrames={result.FlashbackPlaybackOnePercentLowMinimumFrames} " +
            $"onePercentLowMaxSessionFrames={result.FlashbackPlaybackMaxSessionFrameCountObserved} " +
            $"onePercentLowMinOffsetMs={result.FlashbackPlaybackMinOnePercentLowOffsetMs} " +
            $"onePercentLowMinFrames={result.FlashbackPlaybackMinOnePercentLowFrameCount} " +
            $"onePercentLowMinP99FrameMs={result.FlashbackPlaybackMinOnePercentLowP99FrameMs:0.##} " +
            $"onePercentLowMinMaxFrameMs={result.FlashbackPlaybackMinOnePercentLowMaxFrameMs:0.##} " +
            $"onePercentLowMinDecodeP99Ms={result.FlashbackPlaybackMinOnePercentLowDecodeP99Ms:0.##} " +
            $"onePercentLowMinDecodeMaxMs={result.FlashbackPlaybackMinOnePercentLowDecodeMaxMs:0.##} " +
            $"onePercentLowMinAvDriftMs={result.FlashbackPlaybackMinOnePercentLowAvDriftMs:0.##} " +
            $"onePercentLowMinAudioFallbacks={result.FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks}";

    private static string BuildFlashbackPlaybackAudioMasterPerformanceText(DiagnosticSessionResult result)
        =>
            $"audioMasterDoubleEnd={result.FlashbackPlaybackAudioMasterDelayDoublesAtEnd} " +
            $"audioMasterDoubleMax={result.FlashbackPlaybackMaxAudioMasterDelayDoublesObserved} " +
            $"audioMasterShrinkEnd={result.FlashbackPlaybackAudioMasterDelayShrinksAtEnd} " +
            $"audioMasterShrinkMax={result.FlashbackPlaybackMaxAudioMasterDelayShrinksObserved} " +
            $"audioMasterFallbackEnd={result.FlashbackPlaybackAudioMasterFallbacksAtEnd} " +
            $"audioMasterFallbackMax={result.FlashbackPlaybackMaxAudioMasterFallbacksObserved} " +
            $"audioMasterUnavailableEnd={result.FlashbackPlaybackAudioMasterUnavailableFallbacksAtEnd} " +
            $"audioMasterStaleEnd={result.FlashbackPlaybackAudioMasterStaleFallbacksAtEnd} " +
            $"audioMasterDriftOutlierEnd={result.FlashbackPlaybackAudioMasterDriftOutlierFallbacksAtEnd} " +
            $"audioMasterLastFallback={FormatOptional(result.FlashbackPlaybackAudioMasterLastFallbackReasonAtEnd)} " +
            $"audioMasterLastFallbackAgeMs={result.FlashbackPlaybackAudioMasterLastFallbackClockAgeMsAtEnd:0.##} " +
            $"audioBufferedMsMax={result.FlashbackPlaybackMaxAudioBufferedDurationMsObserved:0.##} " +
            $"audioQueueMsMax={result.FlashbackPlaybackMaxAudioQueueDurationMsObserved:0.##} " +
            $"absAvDriftMsMax={result.FlashbackPlaybackMaxAbsAvDriftMsObserved:0.##}";

    private static void AppendFlashbackPlaybackDecode(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Flashback Playback Decode: " +
            $"avgMsEnd={result.FlashbackPlaybackDecodeAvgMsAtEnd:0.##} " +
            $"p95MsEnd={result.FlashbackPlaybackDecodeP95MsAtEnd:0.##} " +
            $"p99MsEnd={result.FlashbackPlaybackDecodeP99MsAtEnd:0.##} " +
            $"maxMsEnd={result.FlashbackPlaybackDecodeMaxMsAtEnd:0.##} " +
            $"phaseEnd={result.FlashbackPlaybackMaxDecodePhaseAtEnd} " +
            $"receiveMsEnd={result.FlashbackPlaybackMaxDecodeReceiveMsAtEnd:0.##} " +
            $"feedMsEnd={result.FlashbackPlaybackMaxDecodeFeedMsAtEnd:0.##} " +
            $"readMsEnd={result.FlashbackPlaybackMaxDecodeReadMsAtEnd:0.##} " +
            $"sendMsEnd={result.FlashbackPlaybackMaxDecodeSendMsAtEnd:0.##} " +
            $"audioMsEnd={result.FlashbackPlaybackMaxDecodeAudioMsAtEnd:0.##} " +
            $"convertMsEnd={result.FlashbackPlaybackMaxDecodeConvertMsAtEnd:0.##} " +
            $"maxPosEnd={result.FlashbackPlaybackMaxDecodePositionMsAtEnd}ms " +
            $"p99MsMax={result.FlashbackPlaybackMaxDecodeP99MsObserved:0.##} " +
            $"maxMsObserved={result.FlashbackPlaybackMaxDecodeMsObserved:0.##} " +
            $"phaseObserved={result.FlashbackPlaybackMaxDecodePhaseObserved} " +
            $"receiveMsObserved={result.FlashbackPlaybackMaxDecodeReceiveMsObserved:0.##} " +
            $"feedMsObserved={result.FlashbackPlaybackMaxDecodeFeedMsObserved:0.##} " +
            $"readMsObserved={result.FlashbackPlaybackMaxDecodeReadMsObserved:0.##} " +
            $"sendMsObserved={result.FlashbackPlaybackMaxDecodeSendMsObserved:0.##} " +
            $"audioMsObserved={result.FlashbackPlaybackMaxDecodeAudioMsObserved:0.##} " +
            $"convertMsObserved={result.FlashbackPlaybackMaxDecodeConvertMsObserved:0.##} " +
            $"maxPosObserved={result.FlashbackPlaybackMaxDecodePositionMsObserved}ms");
    }

    private static void AppendFlashbackPlaybackStages(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Flashback Playback Stages: " +
            $"switchesEnd={result.FlashbackPlaybackSegmentSwitchesAtEnd} " +
            $"fmp4ReopensEnd={result.FlashbackPlaybackFmp4ReopensAtEnd} " +
            $"writeHeadWaitsEnd={result.FlashbackPlaybackWriteHeadWaitsAtEnd} " +
            $"nearLiveSnapsEnd={result.FlashbackPlaybackNearLiveSnapsAtEnd} " +
            $"decodeErrorSnapsEnd={result.FlashbackPlaybackDecodeErrorSnapsAtEnd} " +
            $"seekCapHitsEnd={result.FlashbackPlaybackSeekForwardDecodeCapHitsAtEnd} " +
            $"seekCapHitsDelta={result.FlashbackPlaybackSeekForwardDecodeCapHitsDelta} " +
            $"lastSeekCapEnd={result.FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd} " +
            $"lastWriteHeadGapMsEnd={result.FlashbackPlaybackLastWriteHeadWaitGapMsAtEnd}");
    }

    private static void AppendFlashbackRecording(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Flashback Recording: " +
            $"backendObserved={result.FlashbackRecordingBackendObserved} " +
            $"fileGrowthObserved={result.FlashbackRecordingFileGrowthObserved} " +
            $"submittedDelta={result.FlashbackRecordingVideoFramesSubmittedDelta} " +
            $"packetsDelta={result.FlashbackRecordingVideoEncoderPacketsWrittenDelta} " +
            $"seqGapsEnd={result.FlashbackRecordingIntegritySequenceGapsAtEnd} " +
            $"seqGapsDelta={result.FlashbackRecordingIntegritySequenceGapsDelta} " +
            $"queueDropsEnd={result.FlashbackRecordingIntegrityQueueDroppedFramesAtEnd} " +
            $"queueDropsDelta={result.FlashbackRecordingIntegrityQueueDroppedFramesDelta}");
    }

    private static void AppendFlashbackExport(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Flashback Export: " +
            $"observed={result.FlashbackExportObserved} " +
            $"activeEnd={result.FlashbackExportActiveAtEnd} " +
            $"statusEnd={FormatOptional(result.FlashbackExportStatusAtEnd)} " +
            $"failureKindEnd={FormatOptional(result.FlashbackExportFailureKindAtEnd)} " +
            $"messageEnd={FormatOptional(result.FlashbackExportMessageAtEnd)} " +
            $"forceRotateFallbacksEnd={result.FlashbackExportForceRotateFallbacksAtEnd} " +
            $"forceRotateFallbacksDelta={result.FlashbackExportForceRotateFallbacksDelta} " +
            $"lastForceRotateFallbackSegmentsEnd={result.FlashbackExportLastForceRotateFallbackSegmentsAtEnd} " +
            $"lastResultIdEnd={result.LastExportIdAtEnd} " +
            $"lastSuccessEnd={FormatOptional(result.LastExportSuccessAtEnd)} " +
            $"lastMessageEnd={FormatOptional(result.LastExportMessageAtEnd)} " +
            $"pathEnd={FormatOptional(result.FlashbackExportOutputPathAtEnd)} " +
            $"maxElapsedMs={result.FlashbackExportMaxElapsedMsObserved} " +
            $"maxProgressAgeMs={result.FlashbackExportMaxLastProgressAgeMsObserved} " +
            $"maxBytes={FormatBytes(result.FlashbackExportMaxOutputBytesObserved)} " +
            $"maxThroughput={FormatBytes((long)result.FlashbackExportMaxThroughputBytesPerSecObserved)}/s");
    }

    private static void AppendPreviewSections(StringBuilder builder, DiagnosticSessionResult result)
    {
        AppendPreviewScheduler(builder, result);
        AppendPreviewD3DPerformance(builder, result);
        AppendPreviewD3DCpuTiming(builder, result);
        AppendPreviewVisualCadence(builder, result);
    }

    private static void AppendPreviewScheduler(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Preview Scheduler: " +
            $"droppedEnd={result.PreviewSchedulerDroppedAtEnd} " +
            $"droppedDelta={result.PreviewSchedulerDroppedDelta} " +
            $"clearedDropsEnd={result.PreviewSchedulerClearedDropsAtEnd} " +
            $"clearedDropsDelta={result.PreviewSchedulerClearedDropsDelta} " +
            $"deadlineDropsEnd={result.PreviewSchedulerDeadlineDropsAtEnd} " +
            $"deadlineDropsDelta={result.PreviewSchedulerDeadlineDropsDelta} " +
            $"underflowsEnd={result.PreviewSchedulerUnderflowsAtEnd} " +
            $"underflowsDelta={result.PreviewSchedulerUnderflowsDelta} " +
            $"resumeReprimesEnd={result.PreviewSchedulerResumeReprimesAtEnd} " +
            $"resumeReprimesDelta={result.PreviewSchedulerResumeReprimesDelta} " +
            $"lastUnderflowReasonEnd={FormatOptional(result.PreviewSchedulerLastUnderflowReasonAtEnd)} " +
            $"lastUnderflowInputAgeMsEnd={result.PreviewSchedulerLastUnderflowInputAgeMsAtEnd:0.##} " +
            $"lastUnderflowOutputAgeMsEnd={result.PreviewSchedulerLastUnderflowOutputAgeMsAtEnd:0.##} " +
            $"scheduleLateMaxMsObserved={result.PreviewSchedulerMaxScheduleLateMsObserved:0.##} " +
            $"scheduleLateDelta={result.PreviewSchedulerScheduleLateDelta} " +
            $"lastDropReasonEnd={FormatOptional(result.PreviewSchedulerLastDropReasonAtEnd)}");
    }

    private static void AppendPreviewD3DPerformance(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Preview D3D Perf: " +
            $"onePercentLowFpsEnd={result.PreviewCadenceOnePercentLowFpsAtEnd:0.##} " +
            $"onePercentLowFpsMin={result.PreviewCadenceMinOnePercentLowFpsObserved:0.##} " +
            $"missedRefreshDelta={result.PreviewD3DFrameStatsMissedRefreshDelta} " +
            $"statsFailureDelta={result.PreviewD3DFrameStatsFailureDelta} " +
            $"maxRecentSlowFrames={result.PreviewD3DMaxRecentSlowFramesObserved} " +
            $"latestSlowReason={FormatOptional(result.PreviewD3DLatestSlowFrameReason)} " +
            $"overBudgetMs={result.PreviewD3DLatestSlowFrameOverBudgetMs:0.##} " +
            $"presentIntervalMs={result.PreviewD3DLatestSlowFramePresentIntervalMs:0.##} " +
            $"totalFrameCpuMs={result.PreviewD3DLatestSlowFrameTotalFrameCpuMs:0.##} " +
            $"presentCallMs={result.PreviewD3DLatestSlowFramePresentCallMs:0.##} " +
            $"pending={result.PreviewD3DLatestSlowFramePendingFrameCount}");
    }

    private static void AppendPreviewD3DCpuTiming(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Preview D3D CPU Timing: " +
            $"inputUploadP99End={result.PreviewD3DInputUploadCpuP99MsAtEnd:0.##} " +
            $"inputUploadMaxObserved={result.PreviewD3DInputUploadCpuMaxMsObserved:0.##} " +
            $"renderSubmitP99End={result.PreviewD3DRenderSubmitCpuP99MsAtEnd:0.##} " +
            $"renderSubmitMaxObserved={result.PreviewD3DRenderSubmitCpuMaxMsObserved:0.##} " +
            $"presentCallP99End={result.PreviewD3DPresentCallP99MsAtEnd:0.##} " +
            $"presentCallMaxObserved={result.PreviewD3DPresentCallMaxMsObserved:0.##} " +
            $"totalFrameP99End={result.PreviewD3DTotalFrameCpuP99MsAtEnd:0.##} " +
            $"totalFrameMaxObserved={result.PreviewD3DTotalFrameCpuMaxMsObserved:0.##}");
    }

    private static void AppendPreviewVisualCadence(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Preview Visual Cadence: " +
            $"outputFpsEnd={result.VisualCadenceOutputFpsAtEnd:0.##} " +
            $"changeFpsEnd={result.VisualCadenceChangeFpsAtEnd:0.##} " +
            $"changeFpsMin={result.VisualCadenceMinChangeFpsObserved:0.##} " +
            $"repeatPctEnd={result.VisualCadenceRepeatPercentAtEnd:0.###} " +
            $"repeatPctMax={result.VisualCadenceMaxRepeatPercentObserved:0.###} " +
            $"repeatFramesEnd={result.VisualCadenceRepeatFramesAtEnd} " +
            $"longestRepeatRunEnd={result.VisualCadenceLongestRepeatRunAtEnd}");
    }

    private static void AppendProcessPerformance(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Process Perf: " +
            $"cpuPercentEnd={result.ProcessCpuPercentAtEnd:0.##} " +
            $"cpuPercentMaxObserved={result.ProcessCpuMaxPercentObserved:0.##}");
    }

    private static void AppendArtifacts(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine($"Artifacts: {result.OutputDirectory}");
        builder.AppendLine($"  Live: {result.LivePath}");
        builder.AppendLine($"  Summary: {result.SummaryPath}");
        builder.AppendLine($"  Samples: {result.SamplesPath}");
        builder.AppendLine($"  Frame Ledger: {result.FrameLedgerPath}");
        builder.AppendLine($"  Timeline: {result.TimelinePath}");
    }

    private static void AppendActionsAndWarnings(StringBuilder builder, DiagnosticSessionResult result)
    {
        if (result.Actions.Length > 0)
        {
            builder.AppendLine($"Actions: {string.Join(", ", result.Actions)}");
        }

        if (result.Warnings.Length > 0)
        {
            builder.AppendLine("Warnings:");
            foreach (var warning in result.Warnings)
            {
                builder.AppendLine($"  {warning}");
            }
        }
    }
}

internal static class DiagnosticSessionOptionalTextFormatter
{
    internal static string FormatOptional(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "none" : value;
    }
}
