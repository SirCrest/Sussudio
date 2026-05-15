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
        AssertSsctlCommandRequest(fullscreenRequest, "SetFullScreenEnabled", ("enabled", true));

        var windowClosePipeName = $"ssctl-window-close-{Guid.NewGuid():N}";
        var windowCloseArguments = new List<string> { "window", "close" };
        var (windowCloseExitCode, windowCloseRequests) = await CaptureSsctlRequestsAsync(
                context,
                windowClosePipeName,
                expectedCount: 2,
                windowCloseArguments)
            .ConfigureAwait(false);

        AssertEqual(0, windowCloseExitCode, "window close exit code");
        AssertSsctlCommandRequest(windowCloseRequests[0], "ArmClose", ("armed", true));
        AssertSsctlCommandRequest(windowCloseRequests[1], "WindowAction", ("action", "Close"));

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
        AssertSsctlCommandRequest(windowCloseDeniedRequests[0], "ArmClose", ("armed", true));
    }
}
