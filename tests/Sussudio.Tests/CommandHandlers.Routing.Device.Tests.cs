using System.Threading.Tasks;

static partial class Program
{
    private static async Task SsctlCommandHandlers_RouteDeviceCommands()
    {
        var context = CreateSsctlCommandRoutingContext();

        var devicePipeName = $"ssctl-device-audio-{Guid.NewGuid():N}";
        var deviceArguments = new List<string> { "device", "audio-select", "Synthetic Mic" };
        var (deviceExitCode, deviceRequest) = await CaptureSsctlRequestAsync(
                context,
                devicePipeName,
                deviceArguments)
            .ConfigureAwait(false);

        AssertEqual(0, deviceExitCode, "device audio-select exit code");
        AssertEqual(5, deviceRequest.GetProperty("command").GetInt32(), "device audio-select command id");
        // Auth token is null when not configured via env var.
        AssertEqual(
            "Synthetic Mic",
            deviceRequest.GetProperty("payload").GetProperty("deviceName").GetString(),
            "device audio-select payload key");

        var deviceRefreshPipeName = $"ssctl-device-refresh-{Guid.NewGuid():N}";
        var deviceRefreshArguments = new List<string> { "device", "refresh" };
        var (deviceRefreshExitCode, deviceRefreshRequest) = await CaptureSsctlRequestAsync(
                context,
                deviceRefreshPipeName,
                deviceRefreshArguments)
            .ConfigureAwait(false);

        AssertEqual(0, deviceRefreshExitCode, "device refresh exit code");
        AssertEqual(3, deviceRefreshRequest.GetProperty("command").GetInt32(), "device refresh command id");

        var deviceListPipeName = $"ssctl-device-list-{Guid.NewGuid():N}";
        var deviceListArguments = new List<string> { "device", "list" };
        var (deviceListExitCode, deviceListRequests) = await CaptureSsctlRequestsAsync(
                context,
                deviceListPipeName,
                expectedCount: 2,
                arguments: deviceListArguments,
                responseFactory: i => i == 0
                    ? "{\"Success\":true,\"Message\":\"refresh ok\"}"
                    : "{\"Success\":true,\"Message\":\"options ok\",\"Data\":{\"Devices\":[],\"AudioInputDevices\":[]}}")
            .ConfigureAwait(false);

        AssertEqual(0, deviceListExitCode, "device list exit code");
        AssertEqual(3, deviceListRequests[0].GetProperty("command").GetInt32(), "device list refresh command id");
        AssertEqual(29, deviceListRequests[1].GetProperty("command").GetInt32(), "device list options command id");
    }
}
