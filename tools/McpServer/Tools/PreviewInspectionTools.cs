using System.ComponentModel;
using System.Globalization;
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
public static class PreviewColorProbeTools
{
    [McpServerTool, Description("Probe the active preview renderer mode, negotiated subtype, and available color metadata. Reports D3D11 input/output color spaces when available; extended MF attributes are shown only when provided by the active pipeline.")]
    public static async Task<CallToolResult> probe_preview_color(PipeClient pipeClient)
    {
        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.ProbePreviewColor).ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(response))
        {
            return McpToolResultFactory.FromResponse(response, GetMessage(response));
        }

        if (!response.TryGetProperty("Data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            return McpToolResultFactory.FromText("No probe data returned.", isError: true);
        }

        var builder = new StringBuilder();
        builder.AppendLine("== Preview Color Probe ==");

        var sessionActive = Get(data, "SessionActive");
        builder.AppendLine($"Session Active: {sessionActive}");

        if (string.Equals(sessionActive, "false", StringComparison.OrdinalIgnoreCase))
        {
            builder.AppendLine("No active preview session. Start preview first.");
            return McpToolResultFactory.FromResponse(response, builder.ToString().TrimEnd());
        }

        builder.AppendLine($"Renderer: {Get(data, "RendererMode")}");
        builder.AppendLine($"Format: {Get(data, "NegotiatedSubtype")} {Get(data, "SourceWidth")}x{Get(data, "SourceHeight")} @ {Get(data, "SourceFrameRate")}fps");
        builder.AppendLine();
        var nominalRangeLabel = Get(data, "NominalRangeLabel");
        var transferFunctionLabel = Get(data, "TransferFunctionLabel");
        var videoPrimariesLabel = Get(data, "VideoPrimariesLabel");
        var yuvMatrixLabel = Get(data, "YuvMatrixLabel");
        var hasExtendedMfColor =
            !string.Equals(nominalRangeLabel, "Unknown", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(transferFunctionLabel, "Unknown", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(videoPrimariesLabel, "Unknown", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(yuvMatrixLabel, "Unknown", StringComparison.OrdinalIgnoreCase);
        if (hasExtendedMfColor)
        {
            builder.AppendLine("== Color Attributes ==");
            builder.AppendLine($"Nominal Range: {nominalRangeLabel} (raw={Get(data, "NominalRange")})");
            builder.AppendLine($"Transfer Function: {transferFunctionLabel} (raw={Get(data, "TransferFunction")})");
            builder.AppendLine($"Video Primaries: {videoPrimariesLabel} (raw={Get(data, "VideoPrimaries")})");
            builder.AppendLine($"YUV Matrix: {yuvMatrixLabel} (raw={Get(data, "YuvMatrix")})");
        }
        else
        {
            builder.AppendLine("Extended MF color attributes are unavailable in the active preview path.");
        }

        var d3dInput = Get(data, "D3DInputColorSpace");
        var d3dOutput = Get(data, "D3DOutputColorSpace");
        if (d3dInput != "N/A" && d3dInput != "None")
        {
            builder.AppendLine();
            builder.AppendLine("== D3D11 Video Processor ==");
            builder.AppendLine($"Input Color Space: {d3dInput}");
            builder.AppendLine($"Output Color Space: {d3dOutput}");
        }

        // Luma analysis (only present when ColorCorrectedAdapter is active)
        var lumaSamples = Get(data, "LumaSampleCount");
        if (lumaSamples != "N/A" && lumaSamples != "0")
        {
            builder.AppendLine();
            builder.AppendLine("== Luma (Y Plane) Analysis ==");
            builder.AppendLine($"Range: min={Get(data, "LumaMin")} max={Get(data, "LumaMax")} mean={Get(data, "LumaMean")}");
            builder.AppendLine($"Below 16 (super-black): {Get(data, "LumaBelow16Count")} samples");
            builder.AppendLine($"Above 235 (super-white): {Get(data, "LumaAbove235Count")} samples");
            builder.AppendLine($"Total sampled: {lumaSamples} (every 16th pixel)");

            // Interpretation
            int.TryParse(Get(data, "LumaMin"), out var yMin);
            int.TryParse(Get(data, "LumaMax"), out var yMax);
            int.TryParse(Get(data, "LumaAbove235Count"), out var above235);
            int.TryParse(lumaSamples, out var totalSamples);
            var above235Pct = totalSamples > 0 ? (double)above235 / totalSamples * 100 : 0;
            int.TryParse(Get(data, "LumaBelow16Count"), out var below16);
            var below16Pct = totalSamples > 0 ? (double)below16 / totalSamples * 100 : 0;

            if (yMax > 235 || yMin < 16)
            {
                builder.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "Diagnosis: Data uses FULL range (0-255). {0:0.0}% super-white, {1:0.0}% super-black.",
                    above235Pct,
                    below16Pct));
                builder.AppendLine($"  If MF_MT_VIDEO_NOMINAL_RANGE=Wide(16-235), range mismatch will clip highlights/shadows.");
            }
            else
            {
                builder.AppendLine($"Diagnosis: Data fits within LIMITED range (16-235). No clipping expected.");
            }
        }

        if (data.TryGetProperty("FormatProperties", out var props) && props.ValueKind == JsonValueKind.Object)
        {
            builder.AppendLine();
            builder.AppendLine("== Raw MF Properties ==");
            foreach (var prop in props.EnumerateObject())
            {
                builder.AppendLine($"  {prop.Name} = {prop.Value.GetString() ?? "N/A"}");
            }
        }

        return McpToolResultFactory.FromResponse(response, builder.ToString().TrimEnd());
    }

    private static string GetMessage(JsonElement response)
    {
        return AutomationSnapshotFormatter.Get(response, "Message", "Command failed.");
    }

    private static string Get(JsonElement el, string prop, string fallback = "N/A")
    {
        return AutomationSnapshotFormatter.Get(el, prop, fallback);
    }
}

[McpServerToolType]
// MCP tool for probing the live source signal and capture-card telemetry.
public static class VideoSourceProbeTools
{
    [McpServerTool, Description("Query the live video source's supported formats during preview. Shows P010/NV12 availability, current format, memory preference, and full format table without starting recording.")]
    public static async Task<CallToolResult> probe_video_source(PipeClient pipeClient)
    {
        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.ProbeVideoSource).ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(response))
        {
            return McpToolResultFactory.FromResponse(response, GetMessage(response));
        }

        if (!response.TryGetProperty("Data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            return McpToolResultFactory.FromText("No probe data returned.", isError: true);
        }

        var builder = new StringBuilder();
        builder.AppendLine("== Video Source Probe ==");

        var sessionActive = Get(data, "SessionActive");
        builder.AppendLine($"Session Active: {sessionActive}");

        if (string.Equals(sessionActive, "false", StringComparison.OrdinalIgnoreCase))
        {
            builder.AppendLine("No active ingest session. Start preview first.");
            return McpToolResultFactory.FromResponse(response, builder.ToString().TrimEnd());
        }

        builder.AppendLine($"Memory Preference: {Get(data, "MemoryPreference")}");
        builder.AppendLine($"Current Format: {Get(data, "CurrentSubtype")} {Get(data, "CurrentWidth")}x{Get(data, "CurrentHeight")}@{Get(data, "CurrentFrameRate")}fps");
        builder.AppendLine($"P010 Available: {Get(data, "P010Available")} | NV12 Available: {Get(data, "Nv12Available")}");

        if (data.TryGetProperty("SupportedSubtypes", out var subtypes) && subtypes.ValueKind == JsonValueKind.Array)
        {
            var subtypeList = new List<string>();
            foreach (var s in subtypes.EnumerateArray())
            {
                var val = s.GetString();
                if (!string.IsNullOrWhiteSpace(val))
                {
                    subtypeList.Add(val);
                }
            }
            builder.AppendLine($"Supported Subtypes: {(subtypeList.Count > 0 ? string.Join(", ", subtypeList) : "none")}");
        }

        builder.AppendLine($"Total Format Count: {Get(data, "TotalFormatCount")}");

        if (data.TryGetProperty("Formats", out var formats) && formats.ValueKind == JsonValueKind.Array)
        {
            builder.AppendLine();
            builder.AppendLine("== Format Table ==");
            var index = 0;
            foreach (var fmt in formats.EnumerateArray())
            {
                if (index >= 50)
                {
                    break;
                }

                builder.AppendLine($"  [{index}] {Get(fmt, "Summary")}");
                index++;
            }
        }

        return McpToolResultFactory.FromResponse(response, builder.ToString().TrimEnd());
    }

    private static string GetMessage(JsonElement response)
    {
        return AutomationSnapshotFormatter.Get(response, "Message", "Command failed.");
    }

    private static string Get(JsonElement el, string prop, string fallback = "N/A")
    {
        return AutomationSnapshotFormatter.Get(el, prop, fallback);
    }
}

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

[McpServerToolType]
// MCP tool for capturing the app window as an image artifact.
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

        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.CaptureWindowScreenshot, payload).ConfigureAwait(false);
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
