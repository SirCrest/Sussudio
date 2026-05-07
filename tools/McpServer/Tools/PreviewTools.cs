using System.ComponentModel;
using System.Text.Json;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
// MCP tools for preview start/stop and preview-related toggles.
public static class PreviewTools
{
    [McpServerTool, Description("Start or stop the live preview")]
    public static async Task<CallToolResult> control_preview(
        PipeClient pipeClient,
        [Description("True to start preview, false to stop")] bool enabled)
    {
        var payload = new Dictionary<string, object?>
        {
            ["enabled"] = enabled
        };

        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(
                pipeClient,
                "SetPreviewEnabled",
                "SetPreviewEnabled",
                payload)
            .ConfigureAwait(false);
    }

}
