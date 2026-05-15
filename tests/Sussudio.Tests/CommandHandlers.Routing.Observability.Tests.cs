using System.Threading.Tasks;

static partial class Program
{
    private static async Task SsctlCommandHandlers_RouteObservabilityCommands()
    {
        var context = CreateSsctlCommandRoutingContext();

        var audioRampPipeName = $"ssctl-audio-ramp-trace-{Guid.NewGuid():N}";
        var audioRampArguments = new List<string> { "audio-ramp-trace" };
        var (audioRampExitCode, audioRampRequest) = await CaptureSsctlRequestAsync(
                context,
                audioRampPipeName,
                audioRampArguments)
            .ConfigureAwait(false);

        AssertEqual(0, audioRampExitCode, "audio-ramp-trace exit code");
        AssertEqual(48, audioRampRequest.GetProperty("command").GetInt32(), "audio-ramp-trace command id");
    }
}
