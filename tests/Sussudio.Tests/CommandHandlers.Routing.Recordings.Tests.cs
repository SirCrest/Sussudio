using System.Threading.Tasks;

static partial class Program
{
    private static async Task SsctlCommandHandlers_RouteRecordingsCommands()
    {
        var context = CreateSsctlCommandRoutingContext();

        var recordingsPipeName = $"ssctl-recordings-open-{Guid.NewGuid():N}";
        var recordingsArguments = new List<string> { "recordings", "open" };
        var (recordingsExitCode, recordingsRequest) = await CaptureSsctlRequestAsync(
                context,
                recordingsPipeName,
                recordingsArguments)
            .ConfigureAwait(false);

        AssertEqual(0, recordingsExitCode, "recordings open exit code");
        AssertEqual(53, recordingsRequest.GetProperty("command").GetInt32(), "recordings open command id");
    }
}
