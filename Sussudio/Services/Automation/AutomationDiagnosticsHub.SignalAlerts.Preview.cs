using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private void UpdatePreviewSignalAlerts(
        AutomationSnapshot snapshot,
        bool previewOnePercentLowDegraded,
        bool visualCadenceHealthy,
        string previewSlowFrameDetail)
    {
        SetAlertState(
            "preview-blank",
            snapshot.PreviewBlankSuspected,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Preview,
            "Preview appears active but no frames are being displayed.",
            "Preview blank condition cleared.");

        SetAlertState(
            "preview-stall",
            snapshot.PreviewStalled,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Preview,
            "Preview frame flow appears stalled.",
            "Preview stall condition cleared.");

        var startupTimeoutMs = snapshot.PreviewStartupTimeoutMs > 0 ? snapshot.PreviewStartupTimeoutMs : 2000;
        SetAlertState(
            "preview-startup-timeout",
            snapshot.IsPreviewing &&
            !snapshot.PreviewFirstVisualConfirmed &&
            string.Equals(snapshot.PreviewStartupState, "WaitingForFirstVisual", StringComparison.OrdinalIgnoreCase) &&
            snapshot.PreviewStartupElapsedMs.GetValueOrDefault() >= startupTimeoutMs,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Preview,
            string.IsNullOrWhiteSpace(snapshot.PreviewStartupMissingSignals)
                ? $"Preview startup waiting for first visual beyond {startupTimeoutMs}ms (attempt={snapshot.PreviewAttemptId ?? "none"})."
                : $"Preview startup waiting for first visual beyond {startupTimeoutMs}ms (attempt={snapshot.PreviewAttemptId ?? "none"}, missing={snapshot.PreviewStartupMissingSignals}).",
            "Preview startup visual confirmation recovered.");

        SetAlertState(
            "preview-startup-failed",
            string.Equals(snapshot.PreviewStartupState, "Failed", StringComparison.OrdinalIgnoreCase),
            DiagnosticsSeverity.Error,
            DiagnosticsCategory.Preview,
            string.IsNullOrWhiteSpace(snapshot.PreviewLastFailureReason)
                ? string.IsNullOrWhiteSpace(snapshot.PreviewStartupMissingSignals)
                    ? "Preview startup failed before first visual confirmation."
                    : $"Preview startup failed (missing={snapshot.PreviewStartupMissingSignals})."
                : $"Preview startup failed: {snapshot.PreviewLastFailureReason}",
            "Preview startup failure cleared.");

        SetAlertState(
            "preview-cadence-slow",
            snapshot.PreviewCadenceSampleCount >= 60 && snapshot.PreviewCadenceSlowFramePercent >= 8.0,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Preview,
            $"Preview cadence degraded: slowFrames={snapshot.PreviewCadenceSlowFramePercent:0.##}% " +
            $"p95={snapshot.PreviewCadenceP95IntervalMs:0.##}ms expected={snapshot.PreviewCadenceExpectedIntervalMs:0.##}ms{previewSlowFrameDetail}.",
            "Preview cadence returned to healthy range.");

        SetAlertState(
            "preview-display-low-1pct",
            previewOnePercentLowDegraded && !visualCadenceHealthy,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Preview,
            $"Preview/display 1% low is below target: onePercentLow={snapshot.PreviewCadenceOnePercentLowFps:0.##}fps " +
            $"target={(snapshot.PreviewCadenceExpectedIntervalMs > 0 ? 1000.0 / snapshot.PreviewCadenceExpectedIntervalMs : 0):0.##}fps " +
            $"avg={snapshot.PreviewCadenceObservedFps:0.##}fps p95={snapshot.PreviewCadenceP95IntervalMs:0.##}ms " +
            $"p99={snapshot.PreviewCadenceP99IntervalMs:0.##}ms max={snapshot.PreviewCadenceMaxIntervalMs:0.##}ms{previewSlowFrameDetail}{FormatVisualCadenceAlertDetail(snapshot)}.",
            "Preview/display 1% low returned to target range.",
            throttleMs: 5000);
    }
}
