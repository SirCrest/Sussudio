using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static DiagnosticEvaluation? TryBuildRealtimeSourceDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        bool isPreviewing,
        bool visualCadenceHealthy,
        DiagnosticEvaluationLanes lanes)
    {
        var captureOnePercentLowDegraded =
            IsCaptureOnePercentLowDegraded(
                health.ExpectedFrameRate,
                health.CaptureCadenceSampleCount,
                health.CaptureCadenceOnePercentLowFps);

        if (health.CaptureCadenceEstimatedDroppedFrames > 0 ||
            health.CaptureCadenceSevereGapCount > 0 ||
            health.CaptureCadenceEstimatedDropPercent > 0.1)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "source_capture",
                "Source/capture cadence is the likely stutter stage.",
                lanes.Source,
                lanes.Source,
                lanes.Decode,
                lanes.Preview,
                lanes.Render,
                lanes.Present,
                lanes.Recording,
                lanes.Audio);
        }

        if (!captureOnePercentLowDegraded)
        {
            return null;
        }

        if (isPreviewing &&
            visualCadenceHealthy &&
            health.CaptureCadenceEstimatedDroppedFrames <= 0 &&
            health.CaptureCadenceSevereGapCount <= 0 &&
            health.CaptureCadenceEstimatedDropPercent <= 0)
        {
            return new DiagnosticEvaluation(
                "Healthy",
                "none",
                "Source/capture 1% low is below target, but sampled visual cadence confirms source-rate output.",
                $"{lanes.Source} | {lanes.Visual}",
                lanes.Source,
                lanes.Decode,
                lanes.Preview,
                lanes.Render,
                lanes.Present,
                lanes.Recording,
                lanes.Audio);
        }

        return new DiagnosticEvaluation(
            "Warning",
            "source_capture",
            "Source/capture 1% low is below target.",
            lanes.Source,
            lanes.Source,
            lanes.Decode,
            lanes.Preview,
            lanes.Render,
            lanes.Present,
            lanes.Recording,
            lanes.Audio);
    }
}
