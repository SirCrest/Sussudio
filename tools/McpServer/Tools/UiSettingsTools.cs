using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sussudio.Models;

namespace McpServer.Tools;

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
