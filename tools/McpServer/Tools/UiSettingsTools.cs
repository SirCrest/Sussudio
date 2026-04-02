using System.ComponentModel;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class UiSettingsTools
{
    [McpServerTool, Description("Configure UI-facing settings that matter to automation: show-all capture options, preview monitoring volume, and stats panel visibility. Only provided parameters are changed.")]
    public static async Task<string> configure_ui(
        PipeClient pipeClient,
        [Description("Enable or disable the expanded capture options view")] bool? showAllCaptureOptions = null,
        [Description("Preview volume percentage from 0 to 100")] double? previewVolumePercent = null,
        [Description("Show or hide the stats panel")] bool? statsVisible = null)
    {
        var results = new List<string>();

        if (showAllCaptureOptions.HasValue)
        {
            var payload = new Dictionary<string, object?> { ["enabled"] = showAllCaptureOptions.Value };
            results.Add(await ToolCommandFormatter.ExecuteAndFormatAsync(pipeClient, "SetShowAllCaptureOptions", "SetShowAllCaptureOptions", payload).ConfigureAwait(false));
        }

        if (previewVolumePercent.HasValue)
        {
            var payload = new Dictionary<string, object?> { ["previewVolumePercent"] = previewVolumePercent.Value };
            results.Add(await ToolCommandFormatter.ExecuteAndFormatAsync(pipeClient, "SetPreviewVolume", "SetPreviewVolume", payload).ConfigureAwait(false));
        }

        if (statsVisible.HasValue)
        {
            var payload = new Dictionary<string, object?> { ["visible"] = statsVisible.Value };
            results.Add(await ToolCommandFormatter.ExecuteAndFormatAsync(pipeClient, "SetStatsVisible", "SetStatsVisible", payload).ConfigureAwait(false));
        }

        return results.Count == 0
            ? "No UI setting changes requested."
            : string.Join(Environment.NewLine, results);
    }

    [McpServerTool, Description("Show or hide the settings panel")]
    public static async Task<string> configure_settings_panel(
        PipeClient pipeClient,
        [Description("True to show the settings panel, false to hide it")] bool visible)
    {
        var payload = new Dictionary<string, object?> { ["visible"] = visible };
        return await ToolCommandFormatter.ExecuteAndFormatAsync(pipeClient, "SetSettingsVisible", "SetSettingsVisible", payload).ConfigureAwait(false);
    }

    [McpServerTool, Description("Show or hide a specific stats section by name")]
    public static async Task<string> configure_stats_section(
        PipeClient pipeClient,
        [Description("Section name (e.g. Capture, Audio, Pipeline, Recording, Flashback, Performance, Memory, Preview, Source)")] string section,
        [Description("True to show the section, false to hide it")] bool visible)
    {
        var payload = new Dictionary<string, object?>
        {
            ["section"] = section,
            ["visible"] = visible
        };
        return await ToolCommandFormatter.ExecuteAndFormatAsync(pipeClient, "SetStatsSectionVisible", "SetStatsSectionVisible", payload).ConfigureAwait(false);
    }

}
