using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static string BuildSourceLane(CaptureHealthSnapshot health)
    {
        var sourceTarget = health.ExpectedFrameRate > 0
            ? $"{1000.0 / health.ExpectedFrameRate:0.##}ms"
            : "n/a";

        return $"source target={sourceTarget} avg={health.CaptureCadenceAverageIntervalMs:0.##}ms p95={health.CaptureCadenceP95IntervalMs:0.##}ms p99={health.CaptureCadenceP99IntervalMs:0.##}ms max={health.CaptureCadenceMaxIntervalMs:0.##}ms rate={health.CaptureCadenceObservedFps:0.##}/{health.ExpectedFrameRate:0.##}fps 1pctLow={health.CaptureCadenceOnePercentLowFps:0.##}fps gaps={health.CaptureCadenceSevereGapCount} drops={health.CaptureCadenceEstimatedDroppedFrames} ({health.CaptureCadenceEstimatedDropPercent:0.###}%)";
    }

    private static string BuildSourceSignalLane(
        CaptureHealthSnapshot health,
        string sourceLane)
    {
        return $"{sourceLane} | source telemetry {health.SourceWidth ?? 0}x{health.SourceHeight ?? 0}@{(health.SourceFrameRateExact ?? 0):0.##}fps hdr={health.SourceIsHdr?.ToString() ?? "Unknown"} availability={health.SourceTelemetryAvailability}/{health.SourceTelemetryConfidence}";
    }
}
