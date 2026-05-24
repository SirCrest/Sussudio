using Xunit;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
    [Fact]
    public void AutomationSnapshot_ExposesMjpegPreviewMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var automationSnapshotText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.cs");

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
        AssertContains(automationSnapshotText, "public bool MjpegPreviewJitterEnabled { get; init; }");
        AssertContains(automationSnapshotText, "public string MjpegPreviewJitterLastDropReason { get; init; } = string.Empty;");
        AssertContains(automationSnapshotText, "public int MjpegPacketHashSampleCount { get; init; }");
        AssertContains(automationSnapshotText, "public int[] MjpegPacketHashRecentDuplicateFlags { get; init; } = Array.Empty<int>();");
        AssertContains(automationSnapshotText, "public int VisualCadenceSampleCount { get; init; }");
    }
}
