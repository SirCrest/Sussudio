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
        if (!IsSuccess(response))
        {
            return GetMessage(response);
        }

        return ResponseFormatter.FormatSnapshot(response);
    }

    private static bool IsSuccess(JsonElement response)
    {
        return response.ValueKind == JsonValueKind.Object &&
               response.TryGetProperty("Success", out var success) &&
               success.ValueKind == JsonValueKind.True;
    }

    private static string GetMessage(JsonElement response)
    {
        return ResponseFormatter.Get(response, "Message", "Command failed.");
    }
}
