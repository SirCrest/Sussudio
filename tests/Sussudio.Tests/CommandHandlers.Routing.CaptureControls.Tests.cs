using System.Threading.Tasks;

static partial class Program
{
    private static async Task SsctlCommandHandlers_RouteCaptureControlCommands()
    {
        var context = CreateSsctlCommandRoutingContext();

        var previewPipeName = $"ssctl-preview-{Guid.NewGuid():N}";
        var previewArguments = new List<string> { "preview", "start" };
        var (previewExitCode, previewRequest) = await CaptureSsctlRequestAsync(
                context,
                previewPipeName,
                previewArguments)
            .ConfigureAwait(false);

        AssertEqual(0, previewExitCode, "preview start exit code");
        AssertEqual(16, previewRequest.GetProperty("command").GetInt32(), "preview start command id");
        AssertEqual(
            true,
            previewRequest.GetProperty("payload").GetProperty("enabled").GetBoolean(),
            "preview start payload enabled");
    }
}
