using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ElgatoCapture.Tools;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class FlashbackTools
{
    [McpServerTool, Description("Enable or disable the Flashback rolling buffer. Disable it before dedicated LibAv recording verification.")]
    public static async Task<string> flashback_enabled(
        PipeClient pipeClient,
        [Description("True to enable Flashback, false to disable it")] bool enabled)
    {
        return await ToolCommandFormatter.ExecuteAndFormatAsync(
            pipeClient,
            commandName: "SetFlashbackEnabled",
            label: "SetFlashbackEnabled",
            payload: new Dictionary<string, object?> { ["enabled"] = enabled }).ConfigureAwait(false);
    }

    [McpServerTool, Description("Control flashback playback: play, pause, go_live, seek, set_in_point, set_out_point, or clear_in_out_points")]
    public static async Task<string> flashback_action(
        PipeClient pipeClient,
        [Description("Action: play, pause, go_live, seek, set_in_point, set_out_point, clear_in_out_points")] string action,
        [Description("Position in milliseconds (required for seek)")] double? positionMs = null)
    {
        var normalizedAction = action.Replace("_", "-").ToLowerInvariant();

        var payload = new Dictionary<string, object?>
        {
            ["action"] = normalizedAction
        };

        if (positionMs.HasValue)
        {
            if (!double.IsFinite(positionMs.Value) ||
                positionMs.Value < 0 ||
                positionMs.Value > TimeSpan.MaxValue.TotalMilliseconds)
            {
                throw new ArgumentOutOfRangeException(nameof(positionMs), "Flashback positionMs must be finite, non-negative, and within TimeSpan range.");
            }

            payload["positionMs"] = positionMs.Value;
        }

        return await ToolCommandFormatter.ExecuteAndFormatAsync(
            pipeClient,
            commandName: "FlashbackAction",
            label: $"FlashbackAction({normalizedAction})",
            payload: payload).ConfigureAwait(false);
    }

    [McpServerTool, Description("Export flashback buffer to an MP4 file. Exports the most recent N seconds of the rolling buffer.")]
    public static async Task<string> flashback_export(
        PipeClient pipeClient,
        [Description("Number of seconds to export from the buffer (default: 300)")] double seconds = 300,
        [Description("Output file path (default: temp/flashback_export_<timestamp>.mp4)")] string? outputPath = null,
        [Description("True to export the current in/out selection instead of the most recent N seconds")] bool useSelectionRange = false)
    {
        if (!double.IsFinite(seconds) || seconds <= 0 || seconds > TimeSpan.MaxValue.TotalSeconds)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds), "Flashback export seconds must be finite, greater than zero, and within TimeSpan range.");
        }

        outputPath ??= $"temp/flashback_export_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var payload = new Dictionary<string, object?>
        {
            ["seconds"] = seconds,
            ["outputPath"] = outputPath,
            ["useSelectionRange"] = useSelectionRange
        };

        var response = await pipeClient.SendCommandAsync("FlashbackExport", payload, responseTimeoutMs: 60000).ConfigureAwait(false);
        var status = AutomationSnapshotFormatter.IsSuccess(response) ? "OK" : "ERROR";
        var message = AutomationSnapshotFormatter.Get(response, "Message", "No message.");

        var builder = new StringBuilder();
        builder.AppendLine($"[{status}] FlashbackExport: {message}");
        builder.AppendLine(useSelectionRange
            ? $"Requested: selected range -> {outputPath}"
            : $"Requested: {seconds}s -> {outputPath}");

        if (response.TryGetProperty("Data", out var data) && data.ValueKind == JsonValueKind.Object)
        {
            builder.AppendLine($"Data: {data}");
        }

        return builder.ToString().TrimEnd();
    }

    [McpServerTool, Description("List all flashback buffer segments with their file paths, durations, and frame counts")]
    public static async Task<string> flashback_segments(PipeClient pipeClient)
    {
        var response = await pipeClient.SendCommandAsync("FlashbackGetSegments").ConfigureAwait(false);
        var status = AutomationSnapshotFormatter.IsSuccess(response) ? "OK" : "ERROR";
        var message = AutomationSnapshotFormatter.Get(response, "Message", "No message.");

        var builder = new StringBuilder();
        builder.AppendLine($"[{status}] FlashbackGetSegments: {message}");

        if (response.TryGetProperty("Data", out var data) && data.ValueKind == JsonValueKind.Object)
        {
            builder.AppendLine($"Data: {data}");
        }

        return builder.ToString().TrimEnd();
    }
}
