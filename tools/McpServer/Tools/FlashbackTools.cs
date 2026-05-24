using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Sussudio.Models;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
// MCP tools for Flashback timeline playback, export, and backend settings.
public static partial class FlashbackTools
{
    [McpServerTool, Description("Enable or disable the Flashback rolling buffer. Disable it before dedicated LibAv recording verification.")]
    public static async Task<CallToolResult> flashback_enabled(
        PipeClient pipeClient,
        [Description("True to enable Flashback, false to disable it")] bool enabled)
    {
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(
                pipeClient,
                AutomationCommandKind.SetFlashbackEnabled,
                label: "SetFlashbackEnabled",
                payload: new Dictionary<string, object?> { ["enabled"] = enabled })
            .ConfigureAwait(false);
    }

    [McpServerTool, Description("Restart Flashback to apply deferred settings. This clears the current rolling buffer.")]
    public static async Task<CallToolResult> flashback_apply(PipeClient pipeClient)
    {
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(
                pipeClient,
                AutomationCommandKind.RestartFlashback,
                label: "RestartFlashback")
            .ConfigureAwait(false);
    }

    [McpServerTool, Description("List all flashback buffer segments with their file paths, durations, and frame counts")]
    public static async Task<CallToolResult> flashback_segments(PipeClient pipeClient)
    {
        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.FlashbackGetSegments).ConfigureAwait(false);
        var status = AutomationSnapshotFormatter.IsSuccess(response) ? "OK" : "ERROR";
        var message = AutomationSnapshotFormatter.Get(response, "Message", "No message.");

        var builder = new StringBuilder();
        builder.AppendLine($"[{status}] FlashbackGetSegments: {message}");

        if (response.TryGetProperty("Data", out var data) && data.ValueKind == JsonValueKind.Object)
        {
            builder.AppendLine($"Data: {data}");
        }

        return McpToolResultFactory.FromResponse(response, builder.ToString().TrimEnd());
    }

    [McpServerTool, Description("Control flashback playback: play, pause, go_live, seek, begin_scrub, update_scrub, end_scrub, set_in_point, set_out_point, or clear_in_out_points")]
    public static async Task<CallToolResult> flashback_action(
        PipeClient pipeClient,
        [Description("Action: play, pause, go_live, seek, begin_scrub, update_scrub, end_scrub, set_in_point, set_out_point, clear_in_out_points")] string action,
        [Description("Position in milliseconds (required for seek, begin_scrub, and update_scrub; optional for end_scrub)")] double? positionMs = null)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException(
                "Flashback action is required. Expected play, pause, go_live, seek, begin_scrub, update_scrub, end_scrub, set_in_point, set_out_point, or clear_in_out_points.",
                nameof(action));
        }

        var normalizedAction = action.Replace("_", "-").ToLowerInvariant();
        if (normalizedAction is not ("play" or "pause" or "go-live" or "seek" or "begin-scrub" or "update-scrub" or "end-scrub" or "set-in-point" or "set-out-point" or "clear-in-out-points"))
        {
            throw new ArgumentOutOfRangeException(
                nameof(action),
                "Flashback action must be one of: play, pause, go_live, seek, begin_scrub, update_scrub, end_scrub, set_in_point, set_out_point, clear_in_out_points.");
        }

        if ((normalizedAction == "seek" ||
             normalizedAction == "begin-scrub" ||
             normalizedAction == "update-scrub") &&
            !positionMs.HasValue)
        {
            throw new ArgumentException("Flashback seek, begin_scrub, and update_scrub require positionMs.", nameof(positionMs));
        }

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

        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(
                pipeClient,
                AutomationCommandKind.FlashbackAction,
                label: $"FlashbackAction({normalizedAction})",
                payload: payload)
            .ConfigureAwait(false);
    }

    [McpServerTool, Description("Export flashback buffer to an MP4 file. Exports the most recent N seconds of the rolling buffer. Refuses to overwrite an existing destination file unless force=true.")]
    public static async Task<CallToolResult> flashback_export(
        PipeClient pipeClient,
        [Description("Number of seconds to export from the buffer (default: 300)")] double seconds = 300,
        [Description("Output file path (default: temp/flashback_export_<timestamp>.mp4)")] string? outputPath = null,
        [Description("True to export the current in/out selection instead of the most recent N seconds")] bool useSelectionRange = false,
        [Description("True to overwrite an existing file at outputPath. Default false: the export is refused if the destination already exists, preserving any prior take.")] bool force = false)
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
            ["useSelectionRange"] = useSelectionRange,
            ["force"] = force
        };

        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.FlashbackExport, payload).ConfigureAwait(false);
        var status = AutomationSnapshotFormatter.IsSuccess(response) ? "OK" : "ERROR";
        var message = AutomationSnapshotFormatter.Get(response, "Message", "No message.");

        var builder = new StringBuilder();
        builder.AppendLine($"[{status}] FlashbackExport: {message}");
        builder.AppendLine(useSelectionRange
            ? $"Requested: selected range -> {outputPath}"
            : $"Requested: {seconds}s -> {outputPath}");

        if (response.TryGetProperty("Data", out var data) && data.ValueKind == JsonValueKind.Object)
        {
            var failureKind = AutomationSnapshotFormatter.Get(data, "FailureKind", string.Empty);
            if (!string.IsNullOrWhiteSpace(failureKind))
            {
                builder.AppendLine($"FailureKind: {failureKind}");
            }

            builder.AppendLine($"Data: {data}");
        }

        return McpToolResultFactory.FromResponse(response, builder.ToString().TrimEnd());
    }
}
