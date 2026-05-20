using System.Text.Json;
using Sussudio.Tools;

namespace McpServer.Tools;

public static partial class PerformanceTimelineTools
{
    private static string GetMessage(JsonElement response)
    {
        return AutomationSnapshotFormatter.Get(response, "Message", "Command failed.");
    }

    private static string FormatOptional(string value)
        => string.IsNullOrWhiteSpace(value) ? "none" : value.Trim();

    private static string CompactCell(string value, int maxLength)
    {
        var compact = FormatOptional(value)
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Replace('|', '/');

        return compact.Length <= maxLength ? compact : compact[..Math.Max(0, maxLength - 1)] + "~";
    }
}
