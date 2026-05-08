using System.ComponentModel;
using System.Linq;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class WindowTools
{
    [McpServerTool, Description("Control the application window: minimize, maximize, restore, close (requires arm_close), snap_left, snap_right, snap_top_left, snap_top_right, snap_bottom_left, snap_bottom_right, center, move (requires x,y), resize (requires width,height)")]
    public static async Task<CallToolResult> window_action(
        PipeClient pipeClient,
        [Description("Window action: minimize, maximize, restore, close, snap_left, snap_right, snap_top_left, snap_top_right, snap_bottom_left, snap_bottom_right, center, move, resize")] string action,
        [Description("Arm window close before sending a close action")] bool armClose = false,
        [Description("X position in pixels (required for move)")] int? x = null,
        [Description("Y position in pixels (required for move)")] int? y = null,
        [Description("Width in pixels (required for resize)")] int? width = null,
        [Description("Height in pixels (required for resize)")] int? height = null)
    {
        var results = new List<string>();

        // Normalize snake_case to PascalCase for enum parsing (e.g. snap_left -> SnapLeft)
        var normalizedAction = string.Join("", action.Trim().Split('_').Select(p =>
            p.Length > 0 ? char.ToUpperInvariant(p[0]) + p.Substring(1).ToLowerInvariant() : p));

        if (armClose && string.Equals(normalizedAction, "Close", StringComparison.Ordinal))
        {
            var armPayload = new Dictionary<string, object?>
            {
                ["armed"] = true
            };
            var armResponse = await pipeClient.SendCommandAsync("ArmClose", armPayload).ConfigureAwait(false);
            results.Add(ToolCommandFormatter.FormatCommandResponse(armResponse, "ArmClose"));
            if (!Sussudio.Tools.AutomationSnapshotFormatter.IsSuccess(armResponse))
            {
                return McpToolResultFactory.FromText(string.Join(Environment.NewLine, results), isError: true);
            }
        }

        var actionPayload = new Dictionary<string, object?>
        {
            ["action"] = normalizedAction
        };

        if (x.HasValue) actionPayload["x"] = x.Value;
        if (y.HasValue) actionPayload["y"] = y.Value;
        if (width.HasValue) actionPayload["width"] = width.Value;
        if (height.HasValue) actionPayload["height"] = height.Value;

        var actionResponse = await pipeClient.SendCommandAsync("WindowAction", actionPayload).ConfigureAwait(false);
        results.Add(ToolCommandFormatter.FormatCommandResponse(actionResponse, "WindowAction"));

        return McpToolResultFactory.FromResponse(actionResponse, string.Join(Environment.NewLine, results));
    }

    [McpServerTool, Description("Enter or exit full-screen mode")]
    public static async Task<CallToolResult> set_full_screen(
        PipeClient pipeClient,
        [Description("True to enter full-screen mode, false to exit")] bool enabled)
    {
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(
                pipeClient,
                "SetFullScreenEnabled",
                "SetFullScreenEnabled",
                new Dictionary<string, object?> { ["enabled"] = enabled })
            .ConfigureAwait(false);
    }

    [McpServerTool, Description("Open the current recordings output folder in Explorer")]
    public static async Task<CallToolResult> open_recordings_folder(PipeClient pipeClient)
    {
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(
                pipeClient,
                "OpenRecordingsFolder",
                "OpenRecordingsFolder")
            .ConfigureAwait(false);
    }

}
