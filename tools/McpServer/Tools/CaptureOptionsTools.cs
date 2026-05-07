using System.ComponentModel;
using System.Text.Json;
using Sussudio.Tools;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
// MCP tools for reading selectable device, format, codec, and UI options.
public static class CaptureOptionsTools
{
    [McpServerTool(UseStructuredContent = true), Description("Get structured capture options and current selections, including devices, audio inputs, formats, resolutions, frame rates, presets, split encode modes, video formats, and UI-facing automation state.")]
    public static async Task<object> get_capture_options(PipeClient pipeClient)
    {
        var response = await pipeClient.SendCommandAsync("GetCaptureOptions").ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(response))
        {
            return CreateError(response);
        }

        if (response.TryGetProperty("Data", out var data))
        {
            return data.Clone();
        }

        return new
        {
            success = false,
            message = "Capture options data was not available."
        };
    }

    private static object CreateError(JsonElement response)
    {
        return new
        {
            success = false,
            message = AutomationSnapshotFormatter.Get(response, "Message", "Command failed."),
            errorCode = AutomationSnapshotFormatter.Get(response, "ErrorCode", string.Empty),
            status = AutomationSnapshotFormatter.Get(response, "Status", "error")
        };
    }
}
