using System.Text.Json;
using Sussudio.Tools;

namespace McpServer.Tools;

public static partial class FramePacingVerdictTools
{
    private static IReadOnlyList<TimelineRow> ReadTimeline(JsonElement timelineResponse)
    {
        if (!timelineResponse.TryGetProperty("Data", out var data) ||
            data.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<TimelineRow>();
        }

        var rows = new List<TimelineRow>();
        foreach (var item in data.EnumerateArray())
        {
            rows.Add(new TimelineRow(
                AutomationSnapshotFormatter.GetLong(item, "PreviewD3DFrameStatsRecentMissedRefreshCount"),
                AutomationSnapshotFormatter.GetLong(item, "MjpegPreviewJitterTotalDropped"),
                AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackDroppedFrames")));
        }

        return rows;
    }
}
