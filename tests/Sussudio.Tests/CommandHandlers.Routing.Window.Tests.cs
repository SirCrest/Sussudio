using System.Threading.Tasks;

static partial class Program
{
    private static async Task SsctlCommandHandlers_RouteWindowCommands()
    {
        var context = CreateSsctlCommandRoutingContext();

        var fullscreenPipeName = $"ssctl-fullscreen-{Guid.NewGuid():N}";
        var fullscreenArguments = new List<string> { "window", "fullscreen", "on" };
        var (fullscreenExitCode, fullscreenRequest) = await CaptureSsctlRequestAsync(
                context,
                fullscreenPipeName,
                fullscreenArguments)
            .ConfigureAwait(false);

        AssertEqual(0, fullscreenExitCode, "window fullscreen exit code");
        AssertEqual(52, fullscreenRequest.GetProperty("command").GetInt32(), "window fullscreen command id");
        AssertEqual(
            true,
            fullscreenRequest.GetProperty("payload").GetProperty("enabled").GetBoolean(),
            "window fullscreen payload enabled");

        var windowClosePipeName = $"ssctl-window-close-{Guid.NewGuid():N}";
        var windowCloseArguments = new List<string> { "window", "close" };
        var (windowCloseExitCode, windowCloseRequests) = await CaptureSsctlRequestsAsync(
                context,
                windowClosePipeName,
                expectedCount: 2,
                windowCloseArguments)
            .ConfigureAwait(false);

        AssertEqual(0, windowCloseExitCode, "window close exit code");
        AssertEqual(18, windowCloseRequests[0].GetProperty("command").GetInt32(), "window close arm command id");
        AssertEqual(
            true,
            windowCloseRequests[0].GetProperty("payload").GetProperty("armed").GetBoolean(),
            "window close arm payload");
        AssertEqual(19, windowCloseRequests[1].GetProperty("command").GetInt32(), "window close action command id");
        AssertEqual(
            "Close",
            windowCloseRequests[1].GetProperty("payload").GetProperty("action").GetString(),
            "window close action payload");

        var windowCloseDeniedPipeName = $"ssctl-window-close-denied-{Guid.NewGuid():N}";
        var windowCloseDeniedArguments = new List<string> { "window", "close" };
        var (windowCloseDeniedExitCode, windowCloseDeniedRequests) = await CaptureSsctlRequestsAsync(
                context,
                windowCloseDeniedPipeName,
                expectedCount: 1,
                windowCloseDeniedArguments,
                _ => "{\"Success\":false,\"Message\":\"arm denied\"}")
            .ConfigureAwait(false);

        AssertEqual(3, windowCloseDeniedExitCode, "window close denied exit code");
        AssertEqual(18, windowCloseDeniedRequests[0].GetProperty("command").GetInt32(), "window close denied arm command id");
    }
}
