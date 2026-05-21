using Xunit;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
    [Fact]
    public void AutomationSnapshot_ExposesCaptureCadenceMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var captureCadenceText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.CaptureCadence.cs");

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
        AssertContains(captureCadenceText, "public long EstimatedPipelineLatencyMs { get; init; }");
        AssertContains(captureCadenceText, "public double[] CaptureCadenceRecentIntervalsMs { get; init; } = Array.Empty<double>();");
        AssertDoesNotContain(captureCadenceText, "public int MjpegDecodeSampleCount { get; init; }");
    }
}
