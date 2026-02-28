using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class RecordingTools
{
    [McpServerTool, Description("Start or stop recording")]
    public static async Task<string> control_recording(
        PipeClient pipeClient,
        [Description("True to start recording, false to stop")] bool enabled)
    {
        var payload = new Dictionary<string, object?>
        {
            ["enabled"] = enabled
        };

        var response = await pipeClient.SendCommandAsync("SetRecordingEnabled", payload).ConfigureAwait(false);
        var status = IsSuccess(response) ? "OK" : "ERROR";
        var message = ResponseFormatter.Get(response, "Message", "No message.");
        return $"[{status}] SetRecordingEnabled: {message}";
    }

    private static bool IsSuccess(JsonElement response)
    {
        return response.ValueKind == JsonValueKind.Object &&
               response.TryGetProperty("Success", out var success) &&
               success.ValueKind == JsonValueKind.True;
    }
}
