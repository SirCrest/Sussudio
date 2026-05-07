using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
// MCP tools for UI-only settings like stats visibility and window layout.
public static class UiSettingsTools
{
    [McpServerTool, Description("Configure UI-facing settings that matter to automation: show-all capture options, preview monitoring volume, and stats panel visibility. Only provided parameters are changed.")]
    public static async Task<CallToolResult> configure_ui(
        PipeClient pipeClient,
        [Description("Enable or disable the expanded capture options view")] bool? showAllCaptureOptions = null,
        [Description("Preview volume percentage from 0 to 100")] double? previewVolumePercent = null,
        [Description("Show or hide the stats panel")] bool? statsVisible = null)
        => await ToolCommandFormatter.ExecuteBatchResultAsync(
                pipeClient,
                "No UI setting changes requested.",
                ToolCommandFormatter.Optional("SetShowAllCaptureOptions", "SetShowAllCaptureOptions", "enabled", showAllCaptureOptions),
                ToolCommandFormatter.Optional("SetPreviewVolume", "SetPreviewVolume", "previewVolumePercent", previewVolumePercent),
                ToolCommandFormatter.Optional("SetStatsVisible", "SetStatsVisible", "visible", statsVisible))
            .ConfigureAwait(false);

    [McpServerTool, Description("Show or hide the settings panel")]
    public static async Task<CallToolResult> configure_settings_panel(
        PipeClient pipeClient,
        [Description("True to show the settings panel, false to hide it")] bool visible)
    {
        var payload = new Dictionary<string, object?> { ["visible"] = visible };
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(pipeClient, "SetSettingsVisible", "SetSettingsVisible", payload).ConfigureAwait(false);
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
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(pipeClient, "SetStatsSectionVisible", "SetStatsSectionVisible", payload).ConfigureAwait(false);
    }

}
