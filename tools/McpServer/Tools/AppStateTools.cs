using System.ComponentModel;
using System.Text.Json;
using Sussudio.Models;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
// MCP tools for retrieving the current app snapshot and high-level state.
public static class AppStateTools
{
    [McpServerTool, Description("Get the full application state snapshot including device, preview, recording, HDR, audio, and performance status")]
    public static async Task<CallToolResult> get_app_state(PipeClient pipeClient)
    {
        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.GetSnapshot).ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(response))
        {
            return McpToolResultFactory.FromResponse(response, GetMessage(response));
        }

        return McpToolResultFactory.FromResponse(
            response,
            AutomationSnapshotFormatter.FormatSnapshot(response, includeFlashback: true));
    }

    [McpServerTool(UseStructuredContent = true), Description("Get the raw structured application state snapshot for agent consumption.")]
    public static async Task<object> get_app_state_raw(PipeClient pipeClient)
    {
        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.GetSnapshot).ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(response))
        {
            return CreateError(response);
        }

        if (response.TryGetProperty("Snapshot", out var snapshot))
        {
            return snapshot.Clone();
        }

        return new
        {
            success = false,
            message = "Snapshot data was not available."
        };
    }

    private static string GetMessage(JsonElement response)
    {
        return AutomationSnapshotFormatter.Get(response, "Message", "Command failed.");
    }

    private static object CreateError(JsonElement response)
    {
        return new
        {
            success = false,
            message = GetMessage(response),
            errorCode = AutomationSnapshotFormatter.Get(response, "ErrorCode", string.Empty),
            status = AutomationSnapshotFormatter.Get(response, "Status", "error")
        };
    }
}
