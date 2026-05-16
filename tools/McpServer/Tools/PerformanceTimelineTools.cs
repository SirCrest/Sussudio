using System.ComponentModel;
using System.Text.Json;
using Sussudio.Models;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
// MCP projection over the in-app performance timeline. The tool formats trends
// and counters for investigation; it does not compute or mutate app state.
public static partial class PerformanceTimelineTools
{
    [McpServerTool, Description("Get a time-series performance timeline showing capture/preview frame times, D3D present CPU timing, DXGI missed refreshes, queue depths, drops, memory, GC, and thread pool metrics over the last ~2 minutes (240 samples at 500ms intervals). Use to identify trends, regressions, stutter, present-call blocking, and GC pressure.")]
    public static async Task<CallToolResult> get_performance_timeline(
        PipeClient pipeClient,
        [Description("Maximum number of timeline entries to return (default: 240, which is ~2 minutes)")] int maxEntries = 240,
        [Description("Target 1% low FPS for preview/playback budget diagnostics (default: 118).")] double targetOnePercentLowFps = 118)
    {
        var payload = new Dictionary<string, object?>
        {
            ["maxEntries"] = maxEntries
        };

        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.GetPerformanceTimeline, payload).ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(response))
        {
            return McpToolResultFactory.FromResponse(response, GetMessage(response));
        }

        if (!response.TryGetProperty("Data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return McpToolResultFactory.FromText(
                "No timeline data available. The app may not have been running long enough to collect samples.",
                isError: true);
        }

        var entries = ReadTimelineRows(data);
        if (entries.Count == 0)
        {
            return McpToolResultFactory.FromText("No timeline entries collected yet.");
        }

        return McpToolResultFactory.FromResponse(response, BuildPerformanceTimelineText(entries, targetOnePercentLowFps));
    }

}
