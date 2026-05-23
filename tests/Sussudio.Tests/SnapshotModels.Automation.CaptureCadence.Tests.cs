using Xunit;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
    [Fact]
    public void AutomationSnapshot_ExposesCaptureCadenceMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var frameDiagnosticsText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.FrameDiagnostics.cs");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "EstimatedPipelineLatencyMs",
            "ExpectedCaptureFrameRate",
            "CaptureCadenceSampleCount",
            "CaptureCadenceObservedFps",
            "CaptureCadenceP95IntervalMs",
            "CaptureCadenceP99IntervalMs",
            "CaptureCadenceOnePercentLowFps",
            "CaptureCadenceFivePercentLowFps",
            "CaptureCadenceRecentIntervalsMs",
            "CaptureCadenceEstimatedDroppedFrames",
            "CaptureCadenceEstimatedDropPercent");
        AssertContains(frameDiagnosticsText, "public long EstimatedPipelineLatencyMs { get; init; }");
        AssertContains(frameDiagnosticsText, "public double[] CaptureCadenceRecentIntervalsMs { get; init; } = Array.Empty<double>();");
        AssertContains(frameDiagnosticsText, "public int MjpegDecodeSampleCount { get; init; }");
    }
}
