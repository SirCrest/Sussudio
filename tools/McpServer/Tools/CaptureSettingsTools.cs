using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
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
                ToolCommandFormatter.Optional("SetResolution", "SetResolution", "resolution", resolution),
                ToolCommandFormatter.Optional("SetFrameRate", "SetFrameRate", "frameRate", frameRate),
                ToolCommandFormatter.Optional("SetVideoFormat", "SetVideoFormat", "videoFormat", videoFormat),
                ToolCommandFormatter.Optional("SetRecordingFormat", "SetRecordingFormat", "format", format),
                ToolCommandFormatter.Optional("SetQuality", "SetQuality", "quality", quality),
                ToolCommandFormatter.Optional("SetCustomBitrate", "SetCustomBitrate", "bitrateMbps", bitrateMbps),
                ToolCommandFormatter.Optional("SetPreset", "SetPreset", "preset", preset),
                ToolCommandFormatter.Optional("SetSplitEncodeMode", "SetSplitEncodeMode", "splitEncodeMode", splitEncodeMode),
                ToolCommandFormatter.Optional("SetMjpegDecoderCount", "SetMjpegDecoderCount", "decoderCount", mjpegDecoderCount))
            .ConfigureAwait(false);

}
