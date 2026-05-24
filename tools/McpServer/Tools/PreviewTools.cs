using System.ComponentModel;
using System.Text.Json;
using Sussudio.Models;
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
                AutomationCommandKind.SetPreviewEnabled,
                "SetPreviewEnabled",
                payload)
            .ConfigureAwait(false);
    }

}

[McpServerToolType]
// MCP tools for starting and stopping user recordings.
public static class RecordingTools
{
    [McpServerTool, Description("Start or stop recording")]
    public static async Task<CallToolResult> control_recording(
        PipeClient pipeClient,
        [Description("True to start recording, false to stop")] bool enabled)
    {
        var payload = new Dictionary<string, object?>
        {
            ["enabled"] = enabled
        };

        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(
                pipeClient,
                AutomationCommandKind.SetRecordingEnabled,
                "SetRecordingEnabled",
                payload)
            .ConfigureAwait(false);
    }

}
