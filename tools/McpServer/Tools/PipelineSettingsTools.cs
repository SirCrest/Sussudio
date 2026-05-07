using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

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
                ToolCommandFormatter.Optional("SetHdrEnabled", "SetHdrEnabled", "enabled", hdrEnabled),
                ToolCommandFormatter.Optional("SetTrueHdrPreviewEnabled", "SetTrueHdrPreviewEnabled", "enabled", trueHdrPreviewEnabled),
                ToolCommandFormatter.Optional("SetAudioEnabled", "SetAudioEnabled", "enabled", audioEnabled),
                ToolCommandFormatter.Optional("SetAudioPreviewEnabled", "SetAudioPreviewEnabled", "enabled", audioPreviewEnabled),
                ToolCommandFormatter.Optional("SetOutputPath", "SetOutputPath", "outputPath", outputPath))
            .ConfigureAwait(false);

    [McpServerTool, Description("Set device audio mode to HDMI or analog")]
    public static async Task<CallToolResult> configure_audio_mode(
        PipeClient pipeClient,
        [Description("Audio mode: hdmi or analog")] string mode)
    {
        var payload = new Dictionary<string, object?> { ["mode"] = mode.ToLowerInvariant() };
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(pipeClient, "SetDeviceAudioMode", "SetDeviceAudioMode", payload).ConfigureAwait(false);
    }

    [McpServerTool, Description("Set analog audio input gain (0-100%)")]
    public static async Task<CallToolResult> configure_analog_gain(
        PipeClient pipeClient,
        [Description("Gain value as a percentage (0-100)")] double gainPercent)
    {
        var payload = new Dictionary<string, object?> { ["gain"] = gainPercent };
        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(pipeClient, "SetAnalogAudioGain", "SetAnalogAudioGain", payload).ConfigureAwait(false);
    }

}
