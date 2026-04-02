using System.ComponentModel;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class PipelineSettingsTools
{
    [McpServerTool, Description("Configure pipeline settings: HDR, audio capture, audio preview, true HDR preview, and output path. Only provided parameters are changed.")]
    public static async Task<string> configure_pipeline(
        PipeClient pipeClient,
        [Description("Enable or disable HDR")] bool? hdrEnabled = null,
        [Description("Enable or disable audio capture")] bool? audioEnabled = null,
        [Description("Enable or disable audio preview")] bool? audioPreviewEnabled = null,
        [Description("Enable or disable true HDR preview (GPU HDR tone-mapping). Must stop preview first.")] bool? trueHdrPreviewEnabled = null,
        [Description("Output folder path for recordings")] string? outputPath = null)
    {
        var results = new List<string>();

        if (hdrEnabled.HasValue)
        {
            var payload = new Dictionary<string, object?> { ["enabled"] = hdrEnabled.Value };
            results.Add(await ToolCommandFormatter.ExecuteAndFormatAsync(pipeClient, "SetHdrEnabled", "SetHdrEnabled", payload).ConfigureAwait(false));
        }

        if (trueHdrPreviewEnabled.HasValue)
        {
            var payload = new Dictionary<string, object?> { ["enabled"] = trueHdrPreviewEnabled.Value };
            results.Add(await ToolCommandFormatter.ExecuteAndFormatAsync(pipeClient, "SetTrueHdrPreviewEnabled", "SetTrueHdrPreviewEnabled", payload).ConfigureAwait(false));
        }

        if (audioEnabled.HasValue)
        {
            var payload = new Dictionary<string, object?> { ["enabled"] = audioEnabled.Value };
            results.Add(await ToolCommandFormatter.ExecuteAndFormatAsync(pipeClient, "SetAudioEnabled", "SetAudioEnabled", payload).ConfigureAwait(false));
        }

        if (audioPreviewEnabled.HasValue)
        {
            var payload = new Dictionary<string, object?> { ["enabled"] = audioPreviewEnabled.Value };
            results.Add(await ToolCommandFormatter.ExecuteAndFormatAsync(pipeClient, "SetAudioPreviewEnabled", "SetAudioPreviewEnabled", payload).ConfigureAwait(false));
        }

        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            var payload = new Dictionary<string, object?> { ["outputPath"] = outputPath };
            results.Add(await ToolCommandFormatter.ExecuteAndFormatAsync(pipeClient, "SetOutputPath", "SetOutputPath", payload).ConfigureAwait(false));
        }

        return results.Count == 0
            ? "No pipeline setting changes requested."
            : string.Join(Environment.NewLine, results);
    }

    [McpServerTool, Description("Set device audio mode to HDMI or analog")]
    public static async Task<string> configure_audio_mode(
        PipeClient pipeClient,
        [Description("Audio mode: hdmi or analog")] string mode)
    {
        var payload = new Dictionary<string, object?> { ["mode"] = mode.ToLowerInvariant() };
        return await ToolCommandFormatter.ExecuteAndFormatAsync(pipeClient, "SetDeviceAudioMode", "SetDeviceAudioMode", payload).ConfigureAwait(false);
    }

    [McpServerTool, Description("Set analog audio input gain (0-100%)")]
    public static async Task<string> configure_analog_gain(
        PipeClient pipeClient,
        [Description("Gain value as a percentage (0-100)")] double gainPercent)
    {
        var payload = new Dictionary<string, object?> { ["gain"] = gainPercent };
        return await ToolCommandFormatter.ExecuteAndFormatAsync(pipeClient, "SetAnalogAudioGain", "SetAnalogAudioGain", payload).ConfigureAwait(false);
    }

}
