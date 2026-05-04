using System.ComponentModel;
using System.Text.Json;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class WindowScreenshotTools
{
    [McpServerTool, Description("Capture the entire application window (including UI chrome, margins, letterbox areas, and video preview) as a PNG screenshot. Use .bmp extension for uncompressed BMP.")]
    public static async Task<CallToolResult> capture_window_screenshot(
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
        if (!AutomationSnapshotFormatter.IsSuccess(response))
        {
            return McpToolResultFactory.FromResponse(response, AutomationSnapshotFormatter.Get(response, "Message", "Screenshot failed."));
        }

        if (!response.TryGetProperty("Data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            return McpToolResultFactory.FromText("No screenshot data returned.", isError: true);
        }

        var filePath = AutomationSnapshotFormatter.Get(data, "FilePath", "N/A");
        var width = AutomationSnapshotFormatter.Get(data, "CapturedWidth", "?");
        var height = AutomationSnapshotFormatter.Get(data, "CapturedHeight", "?");
        var fileSize = AutomationSnapshotFormatter.Get(data, "FileSizeBytes", "0");

        return McpToolResultFactory.FromResponse(
            response,
            $"Window screenshot saved: {filePath} ({width}x{height}, {fileSize} bytes)");
    }

}
