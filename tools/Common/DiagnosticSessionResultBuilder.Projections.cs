using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
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
