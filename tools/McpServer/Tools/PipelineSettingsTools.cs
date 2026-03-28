using System.ComponentModel;
using System.Text.Json;
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
            results.Add(await ExecuteAndFormatAsync(pipeClient, "SetHdrEnabled", "SetHdrEnabled", payload).ConfigureAwait(false));
        }

        if (trueHdrPreviewEnabled.HasValue)
        {
            var payload = new Dictionary<string, object?> { ["enabled"] = trueHdrPreviewEnabled.Value };
            results.Add(await ExecuteAndFormatAsync(pipeClient, "SetTrueHdrPreviewEnabled", "SetTrueHdrPreviewEnabled", payload).ConfigureAwait(false));
        }

        if (audioEnabled.HasValue)
        {
            var payload = new Dictionary<string, object?> { ["enabled"] = audioEnabled.Value };
            results.Add(await ExecuteAndFormatAsync(pipeClient, "SetAudioEnabled", "SetAudioEnabled", payload).ConfigureAwait(false));
        }

        if (audioPreviewEnabled.HasValue)
        {
            var payload = new Dictionary<string, object?> { ["enabled"] = audioPreviewEnabled.Value };
            results.Add(await ExecuteAndFormatAsync(pipeClient, "SetAudioPreviewEnabled", "SetAudioPreviewEnabled", payload).ConfigureAwait(false));
        }

        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            var payload = new Dictionary<string, object?> { ["outputPath"] = outputPath };
            results.Add(await ExecuteAndFormatAsync(pipeClient, "SetOutputPath", "SetOutputPath", payload).ConfigureAwait(false));
        }

        return results.Count == 0
            ? "No pipeline setting changes requested."
            : string.Join(Environment.NewLine, results);
    }

    private static async Task<string> ExecuteAndFormatAsync(
        PipeClient pipeClient,
        string commandName,
        string label,
        Dictionary<string, object?>? payload = null)
    {
        var response = await pipeClient.SendCommandAsync(commandName, payload).ConfigureAwait(false);
        var status = ResponseFormatter.IsSuccess(response) ? "OK" : "ERROR";
        var message = ResponseFormatter.Get(response, "Message", "No message.");
        return $"[{status}] {label}: {message}";
    }

}
