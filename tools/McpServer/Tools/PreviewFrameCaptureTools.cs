using System.ComponentModel;
using System.IO;
using System.Text.Json;
using Sussudio.Models;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
// MCP tools for capturing the current preview frame to disk for visual checks.
public static partial class PreviewFrameCaptureTools
{
    [McpServerTool, Description("Capture the next rendered preview frame from the D3D11 swap chain back buffer, save it as BMP or 16-bit RGB PNG, and report frame statistics with diagnosis hints.")]
    public static async Task<CallToolResult> capture_preview_frame(
        PipeClient pipeClient,
        [Description("Optional output path. Use .png for 16-bit RGB capture; other paths use BMP. Defaults to ./temp/preview_capture.bmp")] string? outputPath = null)
    {
        var effectiveOutputPath = string.IsNullOrWhiteSpace(outputPath)
            ? Path.Combine(Environment.CurrentDirectory, "temp", "preview_capture.bmp")
            : outputPath;

        var payload = new Dictionary<string, object?>
        {
            ["outputPath"] = effectiveOutputPath
        };

        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.CapturePreviewFrame, payload).ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(response))
        {
            return McpToolResultFactory.FromResponse(response, AutomationSnapshotFormatter.Get(response, "Message", "Command failed."));
        }

        if (!response.TryGetProperty("Data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            return McpToolResultFactory.FromText("No frame capture data returned.", isError: true);
        }

        return McpToolResultFactory.FromResponse(response, BuildPreviewFrameCaptureText(data));
    }
}
