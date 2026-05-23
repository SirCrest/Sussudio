using Xunit;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
    [Fact]
    public void AutomationSnapshot_ExposesMjpegPreviewMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var frameDiagnosticsText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.FrameDiagnostics.cs");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "MjpegPreviewJitterLastSelectedPreviewPresentId",
            "MjpegPreviewJitterLastSelectedSourceSequenceNumber",
            "MjpegPreviewJitterLastSelectedSourceLatencyMs",
            "MjpegPreviewJitterLastDroppedSourceSequenceNumber",
            "MjpegPreviewJitterClearedDropCount",
            "MjpegPreviewJitterResumeReprimeCount",
            "MjpegPreviewJitterLastDropReason",
            "MjpegPacketHashSampleCount",
            "MjpegPacketHashInputObservedFps",
            "MjpegPacketHashUniqueObservedFps",
            "MjpegPacketHashDuplicateFramePercent",
            "MjpegPacketHashPattern",
            "MjpegPacketHashRecentDuplicateFlags");
        AssertContains(frameDiagnosticsText, "public bool MjpegPreviewJitterEnabled { get; init; }");
        AssertContains(frameDiagnosticsText, "public string MjpegPreviewJitterLastDropReason { get; init; } = string.Empty;");
        AssertContains(frameDiagnosticsText, "public int MjpegPacketHashSampleCount { get; init; }");
        AssertContains(frameDiagnosticsText, "public int[] MjpegPacketHashRecentDuplicateFlags { get; init; } = Array.Empty<int>();");
        AssertContains(frameDiagnosticsText, "public int VisualCadenceSampleCount { get; init; }");
    }
}
