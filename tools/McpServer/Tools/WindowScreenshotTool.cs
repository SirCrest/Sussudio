using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class WindowScreenshotTool
{
    [McpServerTool, Description("Capture the entire application window (including UI chrome, margins, letterbox areas, and video preview) as a PNG screenshot. Use .bmp extension for uncompressed BMP.")]
    public static async Task<string> capture_window_screenshot(
        PipeClient pipeClient,
        [Description("Optional output path for the screenshot. Use .png (default) for compressed or .bmp for uncompressed.")] string? outputPath = null)
    {
        var effectiveOutputPath = string.IsNullOrWhiteSpace(outputPath)
            ? Path.Combine(Environment.CurrentDirectory, "temp", "window_screenshot.png")
            : outputPath;

        var payload = new Dictionary<string, object?>
        {
            ["outputPath"] = effectiveOutputPath
        };

        var response = await pipeClient.SendCommandAsync("CaptureWindowScreenshot", payload).ConfigureAwait(false);
        if (!IsSuccess(response))
        {
            return ResponseFormatter.Get(response, "Message", "Screenshot failed.");
        }

        if (!response.TryGetProperty("Data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            return "No screenshot data returned.";
        }

        var filePath = ResponseFormatter.Get(data, "FilePath", "N/A");
        var width = ResponseFormatter.Get(data, "CapturedWidth", "?");
        var height = ResponseFormatter.Get(data, "CapturedHeight", "?");
        var fileSize = ResponseFormatter.Get(data, "FileSizeBytes", "0");

        return $"Window screenshot saved: {filePath} ({width}x{height}, {fileSize} bytes)";
    }

    private static bool IsSuccess(JsonElement response)
    {
        return response.ValueKind == JsonValueKind.Object &&
               response.TryGetProperty("Success", out var success) &&
               success.ValueKind == JsonValueKind.True;
    }
}
