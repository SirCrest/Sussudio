using Xunit;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
    [Fact]
    public void AutomationSnapshot_ExposesVisualCadenceMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var visualCadenceText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.VisualCadence.cs");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "VisualCadenceSampleCount",
            "VisualCadenceChangeObservedFps",
            "VisualCadenceRepeatFramePercent",
            "VisualCadenceMotionConfidence",
            "VisualCadenceRecentChangeIntervalsMs",
            "VisualCenterCadenceSampleCount",
            "VisualCenterCadenceChangeObservedFps",
            "VisualCenterCadenceRepeatFramePercent",
            "VisualCenterCadenceMotionConfidence",
            "VisualCenterCadenceRecentChangeIntervalsMs");
        AssertContains(visualCadenceText, "public int VisualCadenceSampleCount { get; init; }");
        AssertContains(visualCadenceText, "public double[] VisualCenterCadenceRecentChangeIntervalsMs { get; init; } = Array.Empty<double>();");
        AssertDoesNotContain(visualCadenceText, "public MjpegDecoderAutomationSnapshot[] MjpegPerDecoder");
    }
}
