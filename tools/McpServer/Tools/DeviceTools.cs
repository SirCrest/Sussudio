using System.ComponentModel;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class DeviceTools
{
    [McpServerTool, Description("Select capture device, audio input device, refresh device list, or toggle custom audio input")]
    public static async Task<string> configure_device(
        PipeClient pipeClient,
        [Description("Capture device id to select")] string? deviceId = null,
        [Description("Capture device name to select when id is unknown")] string? deviceName = null,
        [Description("Audio input device id to select")] string? audioDeviceId = null,
        [Description("Audio input device name to select when id is unknown")] string? audioDeviceName = null,
        [Description("Refresh the device list before making selections")] bool refresh = false,
        [Description("Enable or disable custom audio input")] bool? customAudioInput = null)
    {
        var results = new List<string>();

        if (refresh)
        {
            results.Add(await ToolCommandFormatter.ExecuteAndFormatAsync(
                pipeClient,
                commandName: "RefreshDevices",
                label: "RefreshDevices").ConfigureAwait(false));
        }

        if (!string.IsNullOrWhiteSpace(deviceId) || !string.IsNullOrWhiteSpace(deviceName))
        {
            var payload = new Dictionary<string, object?>
            {
                ["deviceId"] = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId,
                ["deviceName"] = string.IsNullOrWhiteSpace(deviceName) ? null : deviceName
            };
            results.Add(await ToolCommandFormatter.ExecuteAndFormatAsync(
                pipeClient,
                commandName: "SelectDevice",
                label: "SelectDevice",
                payload: payload).ConfigureAwait(false));
        }

        if (!string.IsNullOrWhiteSpace(audioDeviceId) || !string.IsNullOrWhiteSpace(audioDeviceName))
        {
            var payload = new Dictionary<string, object?>
            {
                ["deviceId"] = string.IsNullOrWhiteSpace(audioDeviceId) ? null : audioDeviceId,
                ["deviceName"] = string.IsNullOrWhiteSpace(audioDeviceName) ? null : audioDeviceName
            };
            results.Add(await ToolCommandFormatter.ExecuteAndFormatAsync(
                pipeClient,
                commandName: "SelectAudioInputDevice",
                label: "SelectAudioInputDevice",
                payload: payload).ConfigureAwait(false));
        }

        if (customAudioInput.HasValue)
        {
            var payload = new Dictionary<string, object?>
            {
                ["enabled"] = customAudioInput.Value
            };
            results.Add(await ToolCommandFormatter.ExecuteAndFormatAsync(
                pipeClient,
                commandName: "SetCustomAudioInput",
                label: "SetCustomAudioInput",
                payload: payload).ConfigureAwait(false));
        }

        return results.Count == 0
            ? "No device configuration changes requested."
            : string.Join(Environment.NewLine, results);
    }

}
