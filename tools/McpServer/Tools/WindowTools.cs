using System.ComponentModel;
using System.Linq;
using Sussudio.Models;
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
            var armResponse = await pipeClient.SendCommandAsync(AutomationCommandKind.ArmClose, armPayload).ConfigureAwait(false);
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

        var actionResponse = await pipeClient.SendCommandAsync(AutomationCommandKind.WindowAction, actionPayload).ConfigureAwait(false);
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
                AutomationCommandKind.SetFullScreenEnabled,
                "SetFullScreenEnabled",
                new Dictionary<string, object?> { ["enabled"] = enabled })
            .ConfigureAwait(false);
    }

    [McpServerTool, Description("Open the current recordings output folder in Explorer")]
    public static async Task<CallToolResult> open_recordings_folder(PipeClient pipeClient)
    {
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(
                pipeClient,
                AutomationCommandKind.OpenRecordingsFolder,
                "OpenRecordingsFolder")
            .ConfigureAwait(false);
    }

}

[McpServerToolType]
// MCP tools for UI-only settings like stats visibility and window layout.
public static class UiSettingsTools
{
    [McpServerTool, Description("Configure UI-facing settings that matter to automation: show-all compatibility, preview monitoring volume, and stats panel visibility. Only provided parameters are changed.")]
    public static async Task<CallToolResult> configure_ui(
        PipeClient pipeClient,
        [Description("Compatibility setting. Show-all capture options are always enabled; provided values are acknowledged as a no-op.")] bool? showAllCaptureOptions = null,
        [Description("Preview volume percentage from 0 to 100")] double? previewVolumePercent = null,
        [Description("Show or hide the stats panel")] bool? statsVisible = null)
        => await ToolCommandFormatter.ExecuteBatchResultAsync(
                pipeClient,
                "No UI setting changes requested.",
                ToolCommandFormatter.Optional(AutomationCommandKind.SetShowAllCaptureOptions, "SetShowAllCaptureOptions", "enabled", showAllCaptureOptions),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetPreviewVolume, "SetPreviewVolume", "previewVolumePercent", previewVolumePercent),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetStatsVisible, "SetStatsVisible", "visible", statsVisible))
            .ConfigureAwait(false);

    [McpServerTool, Description("Show or hide the settings panel")]
    public static async Task<CallToolResult> configure_settings_panel(
        PipeClient pipeClient,
        [Description("True to show the settings panel, false to hide it")] bool visible)
    {
        var payload = new Dictionary<string, object?> { ["visible"] = visible };
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.SetSettingsVisible, "SetSettingsVisible", payload).ConfigureAwait(false);
    }

    [McpServerTool, Description("Show or hide the frametime graph overlay")]
    public static async Task<CallToolResult> configure_frametime_graph(
        PipeClient pipeClient,
        [Description("True to show the frametime graph, false to hide it")] bool visible)
    {
        var payload = new Dictionary<string, object?> { ["visible"] = visible };
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.SetFrameTimeOverlayVisible, "SetFrameTimeOverlayVisible", payload).ConfigureAwait(false);
    }

    [McpServerTool, Description("Show or hide the Flashback timeline UI")]
    public static async Task<CallToolResult> configure_flashback_timeline(
        PipeClient pipeClient,
        [Description("True to show the Flashback timeline, false to hide it")] bool visible)
    {
        var payload = new Dictionary<string, object?> { ["visible"] = visible };
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.SetFlashbackTimelineVisible, "SetFlashbackTimelineVisible", payload).ConfigureAwait(false);
    }

    [McpServerTool, Description("Show or hide a specific stats section by name")]
    public static async Task<CallToolResult> configure_stats_section(
        PipeClient pipeClient,
        [Description("Section name (e.g. Capture, Audio, Pipeline, Recording, Flashback, Performance, Memory, Preview, Source)")] string section,
        [Description("True to show the section, false to hide it")] bool visible)
    {
        var payload = new Dictionary<string, object?>
        {
            ["section"] = section,
            ["visible"] = visible
        };
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.SetStatsSectionVisible, "SetStatsSectionVisible", payload).ConfigureAwait(false);
    }
}
