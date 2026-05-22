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
}
