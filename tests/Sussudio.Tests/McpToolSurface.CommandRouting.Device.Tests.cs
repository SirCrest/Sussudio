using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task McpDeviceTools_RouteRefreshSelectionsAndCustomAudio()
    {
        var pipeName = NewMcpToolPipeName("device");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var deviceTools = RequireMcpType("McpServer.Tools.DeviceTools");

        var empty = await InvokeMcpToolStringAsync(
            deviceTools,
            "configure_device",
            pipeClient,
            null,
            null,
            null,
            null,
            false,
            null).ConfigureAwait(false);
        AssertEqual("No device configuration changes requested.", empty, "configure_device empty result");

        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 4,
                () => InvokeMcpToolStringAsync(
                    deviceTools,
                    "configure_device",
                    pipeClient,
                    "capture-id",
                    "Capture Name",
                    "audio-id",
                    "Audio Name",
                    true,
                    true))
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "RefreshDevices");
        AssertCommandRequest(
            requests[1],
            "SelectDevice",
            ("deviceId", "capture-id"),
            ("deviceName", "Capture Name"));
        AssertCommandRequest(
            requests[2],
            "SelectAudioInputDevice",
            ("deviceId", "audio-id"),
            ("deviceName", "Audio Name"));
        AssertCommandRequest(requests[3], "SetCustomAudioInput", ("enabled", true));
    }
}
