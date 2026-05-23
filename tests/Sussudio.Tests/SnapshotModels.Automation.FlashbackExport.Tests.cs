using Xunit;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
    [Fact]
    public void AutomationSnapshot_ExposesFlashbackExportMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var flashbackText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.Flashback.cs");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "FlashbackExportActive",
            "FlashbackExportId",
            "FlashbackExportStatus",
            "FlashbackExportOutputPath",
            "FlashbackExportStartedUtcUnixMs",
            "FlashbackExportLastProgressUtcUnixMs",
            "FlashbackExportCompletedUtcUnixMs",
            "FlashbackExportElapsedMs",
            "FlashbackExportLastProgressAgeMs",
            "FlashbackExportOutputBytes",
            "FlashbackExportThroughputBytesPerSec",
            "FlashbackExportSegmentsProcessed",
            "FlashbackExportTotalSegments",
            "FlashbackExportPercent",
            "FlashbackExportInPointMs",
            "FlashbackExportOutPointMs",
            "FlashbackExportMessage",
            "FlashbackExportFailureKind",
            "FlashbackExportForceRotateFallbacks",
            "FlashbackExportLastForceRotateFallbackUtcUnixMs",
            "FlashbackExportLastForceRotateFallbackSegments",
            "FlashbackExportLastForceRotateFallbackInPointMs",
            "FlashbackExportLastForceRotateFallbackOutPointMs",
            "LastExportId");
        AssertContains(flashbackText, "public bool FlashbackExportActive { get; init; }");
        AssertContains(flashbackText, "public string FlashbackExportStatus { get; init; } = \"NotStarted\";");
        AssertContains(flashbackText, "public string? LastExportMessage { get; init; }");
        AssertContains(flashbackText, "public string FlashbackPlaybackState { get; init; }");
    }
}
