using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class CaptureOptionsTool
{
    [McpServerTool(UseStructuredContent = true), Description("Get structured capture options and current selections, including devices, audio inputs, formats, resolutions, frame rates, presets, split encode modes, video formats, and UI-facing automation state.")]
    public static async Task<object> get_capture_options(PipeClient pipeClient)
    {
        var response = await pipeClient.SendCommandAsync("GetCaptureOptions").ConfigureAwait(false);
        if (!IsSuccess(response))
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

    private static bool IsSuccess(JsonElement response)
    {
        return response.ValueKind == JsonValueKind.Object &&
               response.TryGetProperty("Success", out var success) &&
               success.ValueKind == JsonValueKind.True;
    }

    private static object CreateError(JsonElement response)
    {
        return new
        {
            success = false,
            message = ResponseFormatter.Get(response, "Message", "Command failed."),
            errorCode = ResponseFormatter.Get(response, "ErrorCode", string.Empty),
            status = ResponseFormatter.Get(response, "Status", "error")
        };
    }
}
