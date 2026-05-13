using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private void UpdateAlerts(AutomationSnapshot snapshot, FlashbackRecordingRecentCounters flashbackRecordingRecent)
    {
        ObserveFlashbackExportCompletion(snapshot);
        var captureOnePercentLowDegraded =
            IsCaptureOnePercentLowDegraded(
                snapshot.ExpectedCaptureFrameRate,
                snapshot.CaptureCadenceSampleCount,
                snapshot.CaptureCadenceOnePercentLowFps);
        var previewOnePercentLowDegraded =
            IsPreviewOnePercentLowDegraded(
                snapshot.PreviewCadenceExpectedIntervalMs,
                snapshot.PreviewCadenceSampleCount,
                snapshot.PreviewCadenceOnePercentLowFps);
        var visualCadenceHealthy =
            IsVisualCadenceHealthy(
                snapshot.SelectedFrameRate,
                snapshot.VisualCadenceSampleCount,
                snapshot.VisualCadenceChangeObservedFps,
                snapshot.VisualCadenceRepeatFramePercent,
                snapshot.VisualCadenceLongestRepeatRun);
        var previewSlowFrameDetail = FormatPreviewSlowFrameAlertDetail(snapshot);

        UpdateSignalAlerts(
            snapshot,
            captureOnePercentLowDegraded,
            previewOnePercentLowDegraded,
            visualCadenceHealthy,
            previewSlowFrameDetail);

        UpdateFlashbackAlerts(snapshot, flashbackRecordingRecent);

        SetAlertState(
            "hdr-parity-mismatch",
            snapshot.LastVerification?.HdrParity is { Requested: true, Verified: false },
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Verification,
            $"HDR parity mismatch: {snapshot.LastVerification?.HdrParity?.Status ?? "Unknown"}",
            "HDR parity mismatch cleared.");

        SetAlertState(
            "pipeline-mode-violation",
            snapshot.IsRecording && !snapshot.PipelineModeMatched,
            DiagnosticsSeverity.Error,
            DiagnosticsCategory.Capture,
            string.IsNullOrWhiteSpace(snapshot.PipelineModeReason)
                ? $"Pipeline mode violation: requested={snapshot.RequestedPipelineMode}, active={snapshot.ActivePipelineMode}."
                : $"Pipeline mode violation: {snapshot.PipelineModeReason}",
            "Pipeline mode contract restored.");

        if (!snapshot.IsRecording && _wasRecording)
        {
            AddEvent(
                DiagnosticsSeverity.Info,
                DiagnosticsCategory.Recording,
                $"Recording stopped with status: {snapshot.LastFinalizeStatus}");
        }

        SetAlertState(
            "performance-perfection-not-met",
            !snapshot.PerformancePerfectionMet,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.System,
            $"Performance below perfection threshold (score={snapshot.PerformanceScore:0.##}): {snapshot.PerformanceSummary}",
            "Performance returned to perfection threshold.",
            throttleMs: 5000);
    }
}
