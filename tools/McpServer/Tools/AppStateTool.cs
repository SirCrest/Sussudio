using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class AppStateTool
{
    [McpServerTool, Description("Get the full application state snapshot including device, preview, recording, HDR, audio, and performance status")]
    public static async Task<string> get_app_state(PipeClient pipeClient)
    {
        var response = await pipeClient.SendCommandAsync("GetSnapshot").ConfigureAwait(false);
        if (!ResponseFormatter.IsSuccess(response))
        {
            return GetMessage(response);
        }

        return ResponseFormatter.FormatSnapshot(response);
    }

    [McpServerTool(UseStructuredContent = true), Description("Get the raw structured application state snapshot for agent consumption.")]
    public static async Task<object> get_app_state_raw(PipeClient pipeClient)
    {
        var response = await pipeClient.SendCommandAsync("GetSnapshot").ConfigureAwait(false);
        if (!ResponseFormatter.IsSuccess(response))
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
        return ResponseFormatter.Get(response, "Message", "Command failed.");
    }

    private static object CreateError(JsonElement response)
    {
        return new
        {
            success = false,
            message = GetMessage(response),
            errorCode = ResponseFormatter.Get(response, "ErrorCode", string.Empty),
            status = ResponseFormatter.Get(response, "Status", "error")
        };
    }
}
