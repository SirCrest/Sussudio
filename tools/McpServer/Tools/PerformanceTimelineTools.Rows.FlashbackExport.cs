using System.Text.Json;
using Sussudio.Tools;

namespace McpServer.Tools;

public static partial class PerformanceTimelineTools
{
    private static void PopulateFlashbackExportTimelineRow(JsonElement item, TimelineRow row)
    {
        row.FlashbackExportActive = AutomationSnapshotFormatter.GetBool(item, "FlashbackExportActive");
        row.FlashbackExportStatus = AutomationSnapshotFormatter.Get(item, "FlashbackExportStatus");
        row.FlashbackExportFailureKind = AutomationSnapshotFormatter.Get(item, "FlashbackExportFailureKind");
        row.FlashbackExportElapsedMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackExportElapsedMs");
        row.FlashbackExportLastProgressAgeMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackExportLastProgressAgeMs");
        row.FlashbackExportOutputBytes = AutomationSnapshotFormatter.GetLong(item, "FlashbackExportOutputBytes");
        row.FlashbackExportThroughputBytesPerSec = AutomationSnapshotFormatter.GetDouble(item, "FlashbackExportThroughputBytesPerSec");
        row.FlashbackExportSegmentsProcessed = AutomationSnapshotFormatter.GetInt(item, "FlashbackExportSegmentsProcessed");
        row.FlashbackExportTotalSegments = AutomationSnapshotFormatter.GetInt(item, "FlashbackExportTotalSegments");
        row.FlashbackExportPercent = AutomationSnapshotFormatter.GetDouble(item, "FlashbackExportPercent");
        row.FlashbackExportInPointMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackExportInPointMs");
        row.FlashbackExportOutPointMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackExportOutPointMs");
        row.FlashbackExportMessage = AutomationSnapshotFormatter.Get(item, "FlashbackExportMessage");
    }
}
