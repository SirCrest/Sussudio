using Xunit;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
    [Fact]
    public void AutomationSnapshot_ExposesVisualCadenceMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var frameDiagnosticsText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.FrameDiagnostics.cs");

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
        AssertContains(frameDiagnosticsText, "public int VisualCadenceSampleCount { get; init; }");
        AssertContains(frameDiagnosticsText, "public double[] VisualCenterCadenceRecentChangeIntervalsMs { get; init; } = Array.Empty<double>();");
        AssertContains(frameDiagnosticsText, "public MjpegDecoderAutomationSnapshot[] MjpegPerDecoder");
    }
}
