using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class PreviewColorProbeTool
{
    [McpServerTool, Description("Probe the active preview renderer mode, negotiated subtype, and available color metadata. Reports D3D11 input/output color spaces when available; extended MF attributes are shown only when provided by the active pipeline.")]
    public static async Task<string> probe_preview_color(PipeClient pipeClient)
    {
        var response = await pipeClient.SendCommandAsync("ProbePreviewColor").ConfigureAwait(false);
        if (!IsSuccess(response))
        {
            return GetMessage(response);
        }

        if (!response.TryGetProperty("Data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            return "No probe data returned.";
        }

        var builder = new StringBuilder();
        builder.AppendLine("== Preview Color Probe ==");

        var sessionActive = Get(data, "SessionActive");
        builder.AppendLine($"Session Active: {sessionActive}");

        if (string.Equals(sessionActive, "false", StringComparison.OrdinalIgnoreCase))
        {
            builder.AppendLine("No active preview session. Start preview first.");
            return builder.ToString().TrimEnd();
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
                builder.AppendLine($"Diagnosis: Data uses FULL range (0-255). {above235Pct:0.1}% super-white, {below16Pct:0.1}% super-black.");
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

        return builder.ToString().TrimEnd();
    }

    private static bool IsSuccess(JsonElement response)
    {
        return response.ValueKind == JsonValueKind.Object &&
               response.TryGetProperty("Success", out var success) &&
               success.ValueKind == JsonValueKind.True;
    }

    private static string GetMessage(JsonElement response)
    {
        return ResponseFormatter.Get(response, "Message", "Command failed.");
    }

    private static string Get(JsonElement el, string prop, string fallback = "N/A")
    {
        return ResponseFormatter.Get(el, prop, fallback);
    }
}
