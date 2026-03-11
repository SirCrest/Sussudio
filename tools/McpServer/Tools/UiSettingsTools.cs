using System.ComponentModel;
using System.Text.Json;
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
            results.Add(await ExecuteAndFormatAsync(pipeClient, "SetShowAllCaptureOptions", "SetShowAllCaptureOptions", payload).ConfigureAwait(false));
        }

        if (previewVolumePercent.HasValue)
        {
            var payload = new Dictionary<string, object?> { ["previewVolumePercent"] = previewVolumePercent.Value };
            results.Add(await ExecuteAndFormatAsync(pipeClient, "SetPreviewVolume", "SetPreviewVolume", payload).ConfigureAwait(false));
        }

        if (statsVisible.HasValue)
        {
            var payload = new Dictionary<string, object?> { ["visible"] = statsVisible.Value };
            results.Add(await ExecuteAndFormatAsync(pipeClient, "SetStatsVisible", "SetStatsVisible", payload).ConfigureAwait(false));
        }

        return results.Count == 0
            ? "No UI setting changes requested."
            : string.Join(Environment.NewLine, results);
    }

    private static async Task<string> ExecuteAndFormatAsync(
        PipeClient pipeClient,
        string commandName,
        string label,
        Dictionary<string, object?>? payload = null)
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
