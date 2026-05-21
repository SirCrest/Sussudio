using Xunit;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
    [Fact]
    public void AutomationSnapshot_ExposesMjpegPreviewMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var previewJitterText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.MjpegPreviewJitter.cs");
        var packetHashText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.MjpegPacketHash.cs");

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
        AssertContains(previewJitterText, "public bool MjpegPreviewJitterEnabled { get; init; }");
        AssertContains(previewJitterText, "public string MjpegPreviewJitterLastDropReason { get; init; } = string.Empty;");
        AssertDoesNotContain(previewJitterText, "public int MjpegPacketHashSampleCount { get; init; }");
        AssertContains(packetHashText, "public int MjpegPacketHashSampleCount { get; init; }");
        AssertContains(packetHashText, "public int[] MjpegPacketHashRecentDuplicateFlags { get; init; } = Array.Empty<int>();");
        AssertDoesNotContain(packetHashText, "public int VisualCadenceSampleCount { get; init; }");
    }
}
