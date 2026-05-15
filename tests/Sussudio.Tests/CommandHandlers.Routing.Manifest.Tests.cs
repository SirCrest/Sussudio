using System.Threading.Tasks;

static partial class Program
{
    private static async Task SsctlCommandHandlers_RouteManifestCommand()
    {
        var context = CreateSsctlCommandRoutingContext();

        var manifestPipeName = $"ssctl-manifest-{Guid.NewGuid():N}";
        var manifestArguments = new List<string> { "manifest" };
        var (manifestExitCode, manifestRequest) = await CaptureSsctlRequestAsync(
                context,
                manifestPipeName,
                manifestArguments)
            .ConfigureAwait(false);

        AssertEqual(0, manifestExitCode, "manifest exit code");
        AssertSsctlCommandRequestHasEmptyPayload(manifestRequest, "GetAutomationManifest");
    }
}
