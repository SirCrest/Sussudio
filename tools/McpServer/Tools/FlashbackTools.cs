using System.ComponentModel;
using Sussudio.Models;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
// MCP tools for Flashback timeline playback, export, and backend settings.
public static partial class FlashbackTools
{
    [McpServerTool, Description("Enable or disable the Flashback rolling buffer. Disable it before dedicated LibAv recording verification.")]
    public static async Task<CallToolResult> flashback_enabled(
        PipeClient pipeClient,
        [Description("True to enable Flashback, false to disable it")] bool enabled)
    {
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(
                pipeClient,
                AutomationCommandKind.SetFlashbackEnabled,
                label: "SetFlashbackEnabled",
                payload: new Dictionary<string, object?> { ["enabled"] = enabled })
            .ConfigureAwait(false);
    }

    [McpServerTool, Description("Restart Flashback to apply deferred settings. This clears the current rolling buffer.")]
    public static async Task<CallToolResult> flashback_apply(PipeClient pipeClient)
    {
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(
                pipeClient,
                AutomationCommandKind.RestartFlashback,
                label: "RestartFlashback")
            .ConfigureAwait(false);
    }
}
