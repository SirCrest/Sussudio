using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class CaptureSettingsTools
{
    [McpServerTool, Description("Configure capture settings: resolution, frame rate, video format override, recording format, quality, custom bitrate, preset, split encode mode, and MJPEG decoder count. Only provided parameters are changed.")]
    public static async Task<string> configure_capture(
        PipeClient pipeClient,
        [Description("Recording resolution, for example 3840x2160")] string? resolution = null,
        [Description("Frame rate in fps, for example 60")] double? frameRate = null,
        [Description("Video format override, for example Auto, MJPG, NV12, or P010")] string? videoFormat = null,
        [Description("Recording format, for example Hevc")] string? format = null,
        [Description("Quality preset, for example High")] string? quality = null,
        [Description("Custom bitrate in Mbps")] double? bitrateMbps = null,
        [Description("Encoder preset, for example P5 or Quality")] string? preset = null,
        [Description("Split encode mode, for example Auto or ForcedOn")] string? splitEncodeMode = null,
        [Description("Number of MJPEG decoders to use for CPU MJPEG mode")] int? mjpegDecoderCount = null)
    {
        var results = new List<string>();

        if (!string.IsNullOrWhiteSpace(resolution))
        {
            var payload = new Dictionary<string, object?> { ["resolution"] = resolution };
            results.Add(await ExecuteAndFormatAsync(pipeClient, "SetResolution", "SetResolution", payload).ConfigureAwait(false));
        }

        if (frameRate.HasValue)
        {
            var payload = new Dictionary<string, object?> { ["frameRate"] = frameRate.Value };
            results.Add(await ExecuteAndFormatAsync(pipeClient, "SetFrameRate", "SetFrameRate", payload).ConfigureAwait(false));
        }

        if (!string.IsNullOrWhiteSpace(videoFormat))
        {
            var payload = new Dictionary<string, object?> { ["videoFormat"] = videoFormat };
            results.Add(await ExecuteAndFormatAsync(pipeClient, "SetVideoFormat", "SetVideoFormat", payload).ConfigureAwait(false));
        }

        if (!string.IsNullOrWhiteSpace(format))
        {
            var payload = new Dictionary<string, object?> { ["format"] = format };
            results.Add(await ExecuteAndFormatAsync(pipeClient, "SetRecordingFormat", "SetRecordingFormat", payload).ConfigureAwait(false));
        }

        if (!string.IsNullOrWhiteSpace(quality))
        {
            var payload = new Dictionary<string, object?> { ["quality"] = quality };
            results.Add(await ExecuteAndFormatAsync(pipeClient, "SetQuality", "SetQuality", payload).ConfigureAwait(false));
        }

        if (bitrateMbps.HasValue)
        {
            var payload = new Dictionary<string, object?> { ["bitrateMbps"] = bitrateMbps.Value };
            results.Add(await ExecuteAndFormatAsync(pipeClient, "SetCustomBitrate", "SetCustomBitrate", payload).ConfigureAwait(false));
        }

        if (!string.IsNullOrWhiteSpace(preset))
        {
            var payload = new Dictionary<string, object?> { ["preset"] = preset };
            results.Add(await ExecuteAndFormatAsync(pipeClient, "SetPreset", "SetPreset", payload).ConfigureAwait(false));
        }

        if (!string.IsNullOrWhiteSpace(splitEncodeMode))
        {
            var payload = new Dictionary<string, object?> { ["splitEncodeMode"] = splitEncodeMode };
            results.Add(await ExecuteAndFormatAsync(pipeClient, "SetSplitEncodeMode", "SetSplitEncodeMode", payload).ConfigureAwait(false));
        }

        if (mjpegDecoderCount.HasValue)
        {
            var payload = new Dictionary<string, object?> { ["decoderCount"] = mjpegDecoderCount.Value };
            results.Add(await ExecuteAndFormatAsync(pipeClient, "SetMjpegDecoderCount", "SetMjpegDecoderCount", payload).ConfigureAwait(false));
        }

        return results.Count == 0
            ? "No capture setting changes requested."
            : string.Join(Environment.NewLine, results);
    }

    private static async Task<string> ExecuteAndFormatAsync(
        PipeClient pipeClient,
        string commandName,
        string label,
        Dictionary<string, object?>? payload = null)
    {
        var response = await pipeClient.SendCommandAsync(commandName, payload).ConfigureAwait(false);
        var status = IsSuccess(response) ? "OK" : "ERROR";
        var message = ResponseFormatter.Get(response, "Message", "No message.");
        return $"[{status}] {label}: {message}";
    }

    private static bool IsSuccess(JsonElement response)
    {
        return response.ValueKind == JsonValueKind.Object &&
               response.TryGetProperty("Success", out var success) &&
               success.ValueKind == JsonValueKind.True;
    }
}
