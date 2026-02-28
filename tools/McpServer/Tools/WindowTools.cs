using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class WindowTools
{
    [McpServerTool, Description("Control the application window: minimize, maximize, restore, or close (close requires arm_close first)")]
    public static async Task<string> window_action(
        PipeClient pipeClient,
        [Description("Window action: minimize, maximize, restore, or close")] string action,
        [Description("Arm window close before sending a close action")] bool armClose = false)
    {
        var results = new List<string>();

        if (armClose)
        {
            var armPayload = new Dictionary<string, object?>
            {
                ["armed"] = true
            };
            results.Add(await ExecuteAndFormatAsync(pipeClient, "ArmClose", "ArmClose", armPayload).ConfigureAwait(false));
        }

        var actionPayload = new Dictionary<string, object?>
        {
            ["action"] = action
        };
        results.Add(await ExecuteAndFormatAsync(pipeClient, "WindowAction", "WindowAction", actionPayload).ConfigureAwait(false));

        return string.Join(Environment.NewLine, results);
    }

    private static async Task<string> ExecuteAndFormatAsync(
        PipeClient pipeClient,
        string commandName,
        string label,
        Dictionary<string, object?> payload)
    {
        var response = await pipeClient.SendCommandAsync(commandName, payload).ConfigureAwait(false);
        var status = IsSuccess(response) ? "OK" : "ERROR";
        var message = ResponseFormatter.Get(response, "Message", "No message.");
        return $"[{status}] {label}: {message}";
    }

    private static bool IsSuccess(JsonElement response)
    {
        return response.ValueKind == JsonValueKind.Object &&
               response.TryGetProperty("Success", out var success) &&
               success.ValueKind == JsonValueKind.True;
    }
}
