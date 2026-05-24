using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sussudio.Models;
using Sussudio.Tools;

namespace McpServer.Tools;

[McpServerToolType]
// MCP tools for changing capture settings such as resolution, FPS, HDR, codec,
// bitrate, and decoder count.
public static class CaptureSettingsTools
{
    [McpServerTool, Description("Configure capture settings: resolution, frame rate, video format override, recording format, quality, custom bitrate, preset, split encode mode, and MJPEG decoder count. Only provided parameters are changed.")]
    public static async Task<CallToolResult> configure_capture(
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
        => await ToolCommandFormatter.ExecuteBatchResultAsync(
                pipeClient,
                "No capture setting changes requested.",
                ToolCommandFormatter.Optional(AutomationCommandKind.SetResolution, "SetResolution", "resolution", resolution),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetFrameRate, "SetFrameRate", "frameRate", frameRate),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetVideoFormat, "SetVideoFormat", "videoFormat", videoFormat),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetRecordingFormat, "SetRecordingFormat", "format", format),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetQuality, "SetQuality", "quality", quality),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetCustomBitrate, "SetCustomBitrate", "bitrateMbps", bitrateMbps),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetPreset, "SetPreset", "preset", preset),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetSplitEncodeMode, "SetSplitEncodeMode", "splitEncodeMode", splitEncodeMode),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetMjpegDecoderCount, "SetMjpegDecoderCount", "decoderCount", mjpegDecoderCount))
            .ConfigureAwait(false);

}

[McpServerToolType]
// MCP tools for reading selectable device, format, codec, and UI options.
public static class CaptureOptionsTools
{
    [McpServerTool(UseStructuredContent = true), Description("Get structured capture options and current selections, including devices, audio inputs, formats, resolutions, frame rates, presets, split encode modes, video formats, and UI-facing automation state.")]
    public static async Task<object> get_capture_options(PipeClient pipeClient)
    {
        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.GetCaptureOptions).ConfigureAwait(false);
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

[McpServerToolType]
// MCP tools for pipeline/debug knobs that affect capture and preview behavior.
public static class PipelineSettingsTools
{
    [McpServerTool, Description("Configure pipeline settings: HDR, audio capture, audio preview, true HDR preview, and output path. Only provided parameters are changed.")]
    public static async Task<CallToolResult> configure_pipeline(
        PipeClient pipeClient,
        [Description("Enable or disable HDR")] bool? hdrEnabled = null,
        [Description("Enable or disable audio capture")] bool? audioEnabled = null,
        [Description("Enable or disable audio preview")] bool? audioPreviewEnabled = null,
        [Description("Enable or disable true HDR preview (GPU HDR tone-mapping). Must stop preview first.")] bool? trueHdrPreviewEnabled = null,
        [Description("Output folder path for recordings")] string? outputPath = null)
        => await ToolCommandFormatter.ExecuteBatchResultAsync(
                pipeClient,
                "No pipeline setting changes requested.",
                ToolCommandFormatter.Optional(AutomationCommandKind.SetHdrEnabled, "SetHdrEnabled", "enabled", hdrEnabled),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetTrueHdrPreviewEnabled, "SetTrueHdrPreviewEnabled", "enabled", trueHdrPreviewEnabled),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetAudioEnabled, "SetAudioEnabled", "enabled", audioEnabled),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetAudioPreviewEnabled, "SetAudioPreviewEnabled", "enabled", audioPreviewEnabled),
                ToolCommandFormatter.Optional(AutomationCommandKind.SetOutputPath, "SetOutputPath", "outputPath", outputPath))
            .ConfigureAwait(false);

    [McpServerTool, Description("Set device audio mode to HDMI or analog")]
    public static async Task<CallToolResult> configure_audio_mode(
        PipeClient pipeClient,
        [Description("Audio mode: hdmi or analog")] string mode)
    {
        var payload = new Dictionary<string, object?> { ["mode"] = mode.ToLowerInvariant() };
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.SetDeviceAudioMode, "SetDeviceAudioMode", payload).ConfigureAwait(false);
    }

    [McpServerTool, Description("Set analog audio input gain (0-100%)")]
    public static async Task<CallToolResult> configure_analog_gain(
        PipeClient pipeClient,
        [Description("Gain value as a percentage (0-100)")] double gainPercent)
    {
        var payload = new Dictionary<string, object?> { ["gain"] = gainPercent };
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.SetAnalogAudioGain, "SetAnalogAudioGain", payload).ConfigureAwait(false);
    }

}
