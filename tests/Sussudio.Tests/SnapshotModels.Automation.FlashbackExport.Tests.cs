using Xunit;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
    [Fact]
    public void AutomationSnapshot_ExposesFlashbackExportMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");

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
    }
}
