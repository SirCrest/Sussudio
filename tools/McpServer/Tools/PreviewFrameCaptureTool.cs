using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class PreviewFrameCaptureTool
{
    [McpServerTool, Description("Capture the next rendered preview frame from the D3D11 swap chain back buffer, save it as BMP, and report frame statistics with diagnosis hints.")]
    public static async Task<string> capture_preview_frame(
        PipeClient pipeClient,
        [Description("Optional output BMP path. Defaults to ./temp/preview_capture.bmp")] string? outputPath = null)
    {
        var effectiveOutputPath = string.IsNullOrWhiteSpace(outputPath)
            ? Path.Combine(Environment.CurrentDirectory, "temp", "preview_capture.bmp")
            : outputPath;

        var payload = new Dictionary<string, object?>
        {
            ["outputPath"] = effectiveOutputPath
        };

        var response = await pipeClient.SendCommandAsync("CapturePreviewFrame", payload).ConfigureAwait(false);
        if (!IsSuccess(response))
        {
            return ResponseFormatter.Get(response, "Message", "Command failed.");
        }

        if (!response.TryGetProperty("Data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            return "No frame capture data returned.";
        }

        var builder = new StringBuilder();
        builder.AppendLine("== Preview Frame Capture ==");
        builder.AppendLine($"File: {Get(data, "FilePath")}");
        builder.AppendLine($"Resolution: {Get(data, "CapturedWidth")} x {Get(data, "CapturedHeight")}");
        builder.AppendLine($"Renderer: {Get(data, "RendererMode")}");
        builder.AppendLine();
        builder.AppendLine("== Pixel Summary ==");
        builder.AppendLine($"Average RGB: R={Get(data, "AverageR")} G={Get(data, "AverageG")} B={Get(data, "AverageB")}");
        builder.AppendLine($"Luminance: avg={Get(data, "AverageLuminance")} min={Get(data, "MinLuminance")} max={Get(data, "MaxLuminance")}");
        builder.AppendLine($"Near Black (<16): {Get(data, "NearBlackPercent")}%");
        builder.AppendLine($"Near White (>240): {Get(data, "NearWhitePercent")}%");
        builder.AppendLine($"Pure Black: {Get(data, "PureBlackPercent")}%");
        builder.AppendLine();
        builder.AppendLine("== Framing ==");
        builder.AppendLine($"Letterbox: top={Get(data, "LetterboxTopRows")} bottom={Get(data, "LetterboxBottomRows")} rows");
        builder.AppendLine($"Pillarbox: left={Get(data, "PillarboxLeftCols")} right={Get(data, "PillarboxRightCols")} cols");
        builder.AppendLine($"Content Area: {Get(data, "ContentWidth")} x {Get(data, "ContentHeight")}");
        builder.AppendLine($"Content Aspect Ratio: {Get(data, "ContentAspectRatio")}");
        builder.AppendLine($"Total Pixels: {Get(data, "TotalPixels")}");

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

        var averageLuminance = GetDouble(data, "AverageLuminance");
        var minLuminance = GetDouble(data, "MinLuminance");
        var maxLuminance = GetDouble(data, "MaxLuminance");
        var pureBlackPercent = GetDouble(data, "PureBlackPercent");
        var letterboxTopRows = GetInt(data, "LetterboxTopRows");
        var letterboxBottomRows = GetInt(data, "LetterboxBottomRows");
        var pillarboxLeftCols = GetInt(data, "PillarboxLeftCols");
        var pillarboxRightCols = GetInt(data, "PillarboxRightCols");
        var contentAspectRatio = GetDouble(data, "ContentAspectRatio");
        var contentWidth = GetInt(data, "ContentWidth");
        var contentHeight = GetInt(data, "ContentHeight");

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
                $"LETTERBOXED: top={letterboxTopRows}, bottom={letterboxBottomRows}, estimated source aspect={contentAspectRatio:0.###} ({contentWidth}x{contentHeight}).");
        }
        if (pillarboxLeftCols > 0 || pillarboxRightCols > 0)
        {
            diagnosis.Add(
                $"PILLARBOXED: left={pillarboxLeftCols}, right={pillarboxRightCols}, estimated source aspect={contentAspectRatio:0.###} ({contentWidth}x{contentHeight}).");
        }
        if ((maxLuminance - minLuminance) < 30.0)
        {
            diagnosis.Add("LOW CONTRAST: luminance range is under 30.");
        }

        var near16By9 = IsNear(contentAspectRatio, 16.0 / 9.0, 0.05);
        var near16By10 = IsNear(contentAspectRatio, 16.0 / 10.0, 0.05);
        if (contentAspectRatio > 0 && !near16By9 && !near16By10)
        {
            diagnosis.Add($"ASPECT RATIO ALERT: content aspect {contentAspectRatio:0.###} is not close to 16:9 or 16:10.");
        }

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

        return builder.ToString().TrimEnd();
    }

    private static bool IsSuccess(JsonElement response)
    {
        return response.ValueKind == JsonValueKind.Object &&
               response.TryGetProperty("Success", out var success) &&
               success.ValueKind == JsonValueKind.True;
    }

    private static bool IsNear(double value, double target, double tolerance)
    {
        return Math.Abs(value - target) <= tolerance;
    }

    private static int GetInt(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var numeric))
            {
                return numeric;
            }

            if (value.ValueKind == JsonValueKind.String &&
                int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }

        return 0;
    }

    private static double GetDouble(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var numeric))
            {
                return numeric;
            }

            if (value.ValueKind == JsonValueKind.String &&
                double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }

        return 0.0;
    }

    private static string Get(JsonElement element, string propertyName, string fallback = "N/A")
    {
        return ResponseFormatter.Get(element, propertyName, fallback);
    }
}
