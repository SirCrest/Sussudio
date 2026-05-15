using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

public static partial class FlashbackTools
{
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
                commandName: "FlashbackAction",
                label: $"FlashbackAction({normalizedAction})",
                payload: payload)
            .ConfigureAwait(false);
    }
}
