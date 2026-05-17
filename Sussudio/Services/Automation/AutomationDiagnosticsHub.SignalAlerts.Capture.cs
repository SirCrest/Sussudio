using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private void UpdateCaptureSignalAlerts(
        AutomationSnapshot snapshot,
        bool captureOnePercentLowDegraded)
    {
        SetAlertState(
            "capture-cadence-drop",
            snapshot.CaptureCadenceSampleCount >= 120 && snapshot.CaptureCadenceEstimatedDropPercent >= 1.0,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Capture,
            $"Capture cadence drop estimate={snapshot.CaptureCadenceEstimatedDropPercent:0.##}% " +
            $"(estDropped={snapshot.CaptureCadenceEstimatedDroppedFrames}, severeGaps={snapshot.CaptureCadenceSevereGapCount}).",
            "Capture cadence drop estimate returned to healthy range.");

        SetAlertState(
            "capture-cadence-low-1pct",
            captureOnePercentLowDegraded,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Capture,
            $"Capture cadence 1% low is below target: onePercentLow={snapshot.CaptureCadenceOnePercentLowFps:0.##}fps " +
            $"target={snapshot.ExpectedCaptureFrameRate:0.##}fps avg={snapshot.CaptureCadenceObservedFps:0.##}fps " +
            $"p95={snapshot.CaptureCadenceP95IntervalMs:0.##}ms p99={snapshot.CaptureCadenceP99IntervalMs:0.##}ms max={snapshot.CaptureCadenceMaxIntervalMs:0.##}ms.",
            "Capture cadence 1% low returned to target range.",
            throttleMs: 5000);
    }
}
