using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class PreviewTools
{
    [McpServerTool, Description("Start or stop the live preview")]
    public static async Task<string> control_preview(
        PipeClient pipeClient,
        [Description("True to start preview, false to stop")] bool enabled)
    {
        var payload = new Dictionary<string, object?>
        {
            ["enabled"] = enabled
        };

        var response = await pipeClient.SendCommandAsync("SetPreviewEnabled", payload).ConfigureAwait(false);
        var status = IsSuccess(response) ? "OK" : "ERROR";
        var message = ResponseFormatter.Get(response, "Message", "No message.");
        return $"[{status}] SetPreviewEnabled: {message}";
    }

    private static bool IsSuccess(JsonElement response)
    {
        return response.ValueKind == JsonValueKind.Object &&
               response.TryGetProperty("Success", out var success) &&
               success.ValueKind == JsonValueKind.True;
    }
}
