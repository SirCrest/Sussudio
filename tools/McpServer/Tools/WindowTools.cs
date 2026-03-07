using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class WindowTools
{
    [McpServerTool, Description("Control the application window: minimize, maximize, restore, close (requires arm_close), snap_left, snap_right, snap_top_left, snap_top_right, snap_bottom_left, snap_bottom_right, center, move (requires x,y), resize (requires width,height)")]
    public static async Task<string> window_action(
        PipeClient pipeClient,
        [Description("Window action: minimize, maximize, restore, close, snap_left, snap_right, snap_top_left, snap_top_right, snap_bottom_left, snap_bottom_right, center, move, resize")] string action,
        [Description("Arm window close before sending a close action")] bool armClose = false,
        [Description("X position in pixels (required for move)")] int? x = null,
        [Description("Y position in pixels (required for move)")] int? y = null,
        [Description("Width in pixels (required for resize)")] int? width = null,
        [Description("Height in pixels (required for resize)")] int? height = null)
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

        // Normalize snake_case to PascalCase for enum parsing (e.g. snap_left -> SnapLeft)
        var normalizedAction = string.Join("", action.Split('_').Select(p =>
            p.Length > 0 ? char.ToUpperInvariant(p[0]) + p.Substring(1).ToLowerInvariant() : p));

        var actionPayload = new Dictionary<string, object?>
        {
            ["action"] = normalizedAction
        };

        if (x.HasValue) actionPayload["x"] = x.Value;
        if (y.HasValue) actionPayload["y"] = y.Value;
        if (width.HasValue) actionPayload["width"] = width.Value;
        if (height.HasValue) actionPayload["height"] = height.Value;

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
