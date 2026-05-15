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
        AssertEqual(51, manifestRequest.GetProperty("command").GetInt32(), "manifest command id");
    }
}
