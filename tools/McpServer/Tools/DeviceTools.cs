using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class DeviceTools
{
    [McpServerTool, Description("Select capture device, audio input device, refresh device list, or toggle custom audio input")]
    public static async Task<CallToolResult> configure_device(
        PipeClient pipeClient,
        [Description("Capture device id to select")] string? deviceId = null,
        [Description("Capture device name to select when id is unknown")] string? deviceName = null,
        [Description("Audio input device id to select")] string? audioDeviceId = null,
        [Description("Audio input device name to select when id is unknown")] string? audioDeviceName = null,
        [Description("Refresh the device list before making selections")] bool refresh = false,
        [Description("Enable or disable custom audio input")] bool? customAudioInput = null)
        => await ToolCommandFormatter.ExecuteBatchResultAsync(
                pipeClient,
                "No device configuration changes requested.",
                ToolCommandFormatter.Optional(
                    "RefreshDevices",
                    "RefreshDevices",
                    refresh),
                ToolCommandFormatter.Optional(
                    "SelectDevice",
                    "SelectDevice",
                    !string.IsNullOrWhiteSpace(deviceId) || !string.IsNullOrWhiteSpace(deviceName),
                    new Dictionary<string, object?>
                    {
                        ["deviceId"] = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId,
                        ["deviceName"] = string.IsNullOrWhiteSpace(deviceName) ? null : deviceName
                    }),
                ToolCommandFormatter.Optional(
                    "SelectAudioInputDevice",
                    "SelectAudioInputDevice",
                    !string.IsNullOrWhiteSpace(audioDeviceId) || !string.IsNullOrWhiteSpace(audioDeviceName),
                    new Dictionary<string, object?>
                    {
                        ["deviceId"] = string.IsNullOrWhiteSpace(audioDeviceId) ? null : audioDeviceId,
                        ["deviceName"] = string.IsNullOrWhiteSpace(audioDeviceName) ? null : audioDeviceName
                    }),
                ToolCommandFormatter.Optional(
                    "SetCustomAudioInput",
                    "SetCustomAudioInput",
                    customAudioInput.HasValue,
                    customAudioInput.HasValue ? new Dictionary<string, object?> { ["enabled"] = customAudioInput.Value } : null))
            .ConfigureAwait(false);

}
