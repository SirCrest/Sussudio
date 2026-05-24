using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Sussudio.Models;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
// MCP tools for capturing the current preview frame to disk for visual checks.
public static class PreviewFrameCaptureTools
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

    private static string BuildPreviewFrameCaptureText(JsonElement data)
    {
        var builder = new StringBuilder();
        builder.AppendLine("== Preview Frame Capture ==");
        builder.AppendLine($"File: {AutomationSnapshotFormatter.Get(data, "FilePath")}");
        builder.AppendLine($"Resolution: {AutomationSnapshotFormatter.Get(data, "CapturedWidth")} x {AutomationSnapshotFormatter.Get(data, "CapturedHeight")}");
        builder.AppendLine($"Renderer: {AutomationSnapshotFormatter.Get(data, "RendererMode")}");
        builder.AppendLine();
        builder.AppendLine("== Pixel Summary ==");
        builder.AppendLine($"Average RGB: R={AutomationSnapshotFormatter.Get(data, "AverageR")} G={AutomationSnapshotFormatter.Get(data, "AverageG")} B={AutomationSnapshotFormatter.Get(data, "AverageB")}");
        builder.AppendLine($"Luminance: avg={AutomationSnapshotFormatter.Get(data, "AverageLuminance")} min={AutomationSnapshotFormatter.Get(data, "MinLuminance")} max={AutomationSnapshotFormatter.Get(data, "MaxLuminance")}");
        builder.AppendLine($"Near Black (<16): {AutomationSnapshotFormatter.Get(data, "NearBlackPercent")}%");
        builder.AppendLine($"Near White (>240): {AutomationSnapshotFormatter.Get(data, "NearWhitePercent")}%");
        builder.AppendLine($"Pure Black: {AutomationSnapshotFormatter.Get(data, "PureBlackPercent")}%");
        builder.AppendLine();
        builder.AppendLine("== Framing ==");
        builder.AppendLine($"Letterbox: top={AutomationSnapshotFormatter.Get(data, "LetterboxTopRows")} bottom={AutomationSnapshotFormatter.Get(data, "LetterboxBottomRows")} rows");
        builder.AppendLine($"Pillarbox: left={AutomationSnapshotFormatter.Get(data, "PillarboxLeftCols")} right={AutomationSnapshotFormatter.Get(data, "PillarboxRightCols")} cols");
        builder.AppendLine($"Content Area: {AutomationSnapshotFormatter.Get(data, "ContentWidth")} x {AutomationSnapshotFormatter.Get(data, "ContentHeight")}");
        builder.AppendLine($"Content Aspect Ratio: {AutomationSnapshotFormatter.Get(data, "ContentAspectRatio")}");
        builder.AppendLine($"Total Pixels: {AutomationSnapshotFormatter.Get(data, "TotalPixels")}");

        AppendLuminanceHistogram(builder, data);
        AppendPreviewFrameCaptureDiagnosis(builder, data);

        return builder.ToString().TrimEnd();
    }

    private static void AppendPreviewFrameCaptureDiagnosis(StringBuilder builder, JsonElement data)
    {
        var diagnosis = BuildPreviewFrameCaptureDiagnosis(data);

        builder.AppendLine();
        builder.AppendLine("== Diagnosis ==");
        if (diagnosis.Count == 0)
        {
            builder.AppendLine("No obvious anomalies detected.");
        }
        else
        {
            foreach (var finding in diagnosis)
            {
                builder.AppendLine($"- {finding}");
            }
        }
    }

    private static void AppendLuminanceHistogram(StringBuilder builder, JsonElement data)
    {
        builder.AppendLine();
        builder.AppendLine("== Luminance Histogram (16 bins) ==");
        if (data.TryGetProperty("LuminanceHistogram", out var histogramElement) &&
            histogramElement.ValueKind == JsonValueKind.Array)
        {
            var bins = new List<int>();
            foreach (var item in histogramElement.EnumerateArray())
            {
                bins.Add(item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out var count)
                    ? count
                    : 0);
            }

            while (bins.Count < 16)
            {
                bins.Add(0);
            }

            var maxBin = bins.Count > 0 ? bins.Max() : 0;
            for (var i = 0; i < 16; i++)
            {
                var start = i * 16;
                var end = start + 15;
                var value = bins[i];
                var barLength = maxBin > 0 ? (int)Math.Round((value / (double)maxBin) * 24.0) : 0;
                var bar = new string('#', Math.Max(0, barLength));
                builder.AppendLine($"{start,3}-{end,3}: {bar} ({value})");
            }
        }
        else
        {
            builder.AppendLine("Histogram unavailable.");
        }
    }

    private static List<string> BuildPreviewFrameCaptureDiagnosis(JsonElement data)
    {
        var averageLuminance = AutomationSnapshotFormatter.GetDouble(data, "AverageLuminance");
        var minLuminance = AutomationSnapshotFormatter.GetDouble(data, "MinLuminance");
        var maxLuminance = AutomationSnapshotFormatter.GetDouble(data, "MaxLuminance");
        var pureBlackPercent = AutomationSnapshotFormatter.GetDouble(data, "PureBlackPercent");
        var letterboxTopRows = AutomationSnapshotFormatter.GetInt(data, "LetterboxTopRows");
        var letterboxBottomRows = AutomationSnapshotFormatter.GetInt(data, "LetterboxBottomRows");
        var pillarboxLeftCols = AutomationSnapshotFormatter.GetInt(data, "PillarboxLeftCols");
        var pillarboxRightCols = AutomationSnapshotFormatter.GetInt(data, "PillarboxRightCols");
        var contentAspectRatio = AutomationSnapshotFormatter.GetDouble(data, "ContentAspectRatio");
        var contentWidth = AutomationSnapshotFormatter.GetInt(data, "ContentWidth");
        var contentHeight = AutomationSnapshotFormatter.GetInt(data, "ContentHeight");

        var diagnosis = new List<string>();
        if (pureBlackPercent > 95.0)
        {
            diagnosis.Add("BLANK FRAME: >95% of pixels are pure black.");
        }
        if (averageLuminance < 30.0)
        {
            diagnosis.Add("VERY DARK: average luminance is below 30.");
        }
        if (averageLuminance > 230.0)
        {
            diagnosis.Add("VERY BRIGHT: average luminance is above 230.");
        }
        if (letterboxTopRows > 0 || letterboxBottomRows > 0)
        {
            diagnosis.Add(
                $"LETTERBOXED: top={letterboxTopRows}, bottom={letterboxBottomRows}, estimated source aspect={FormatAspectRatio(contentAspectRatio)} ({contentWidth}x{contentHeight}).");
        }
        if (pillarboxLeftCols > 0 || pillarboxRightCols > 0)
        {
            diagnosis.Add(
                $"PILLARBOXED: left={pillarboxLeftCols}, right={pillarboxRightCols}, estimated source aspect={FormatAspectRatio(contentAspectRatio)} ({contentWidth}x{contentHeight}).");
        }
        if ((maxLuminance - minLuminance) < 30.0)
        {
            diagnosis.Add("LOW CONTRAST: luminance range is under 30.");
        }

        var near16By9 = IsNear(contentAspectRatio, 16.0 / 9.0, 0.05);
        var near16By10 = IsNear(contentAspectRatio, 16.0 / 10.0, 0.05);
        if (contentAspectRatio > 0 && !near16By9 && !near16By10)
        {
            diagnosis.Add($"ASPECT RATIO ALERT: content aspect {FormatAspectRatio(contentAspectRatio)} is not close to 16:9 or 16:10.");
        }

        return diagnosis;
    }

    private static string FormatAspectRatio(double aspectRatio)
        => AutomationSnapshotFormatter.FormatNumber(aspectRatio, "0.###");

    private static bool IsNear(double value, double target, double tolerance)
    {
        return Math.Abs(value - target) <= tolerance;
    }
}
