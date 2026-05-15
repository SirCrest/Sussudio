using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

public static partial class FlashbackTools
{
    [McpServerTool, Description("List all flashback buffer segments with their file paths, durations, and frame counts")]
    public static async Task<CallToolResult> flashback_segments(PipeClient pipeClient)
    {
        var response = await pipeClient.SendCommandAsync("FlashbackGetSegments").ConfigureAwait(false);
        var status = AutomationSnapshotFormatter.IsSuccess(response) ? "OK" : "ERROR";
        var message = AutomationSnapshotFormatter.Get(response, "Message", "No message.");

        var builder = new StringBuilder();
        builder.AppendLine($"[{status}] FlashbackGetSegments: {message}");

        if (response.TryGetProperty("Data", out var data) && data.ValueKind == JsonValueKind.Object)
        {
            builder.AppendLine($"Data: {data}");
        }

        return McpToolResultFactory.FromResponse(response, builder.ToString().TrimEnd());
    }
}
