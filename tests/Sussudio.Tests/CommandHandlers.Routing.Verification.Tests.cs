using System.Threading.Tasks;

static partial class Program
{
    private static async Task SsctlCommandHandlers_RouteVerificationCommands()
    {
        var context = CreateSsctlCommandRoutingContext();

        var verifyPipeName = $"ssctl-verify-profile-{Guid.NewGuid():N}";
        var verifyArguments = new List<string> { "verify", @"C:\captures\clip.mp4", "--profile", "flashback-export" };
        var (verifyExitCode, verifyRequest) = await CaptureSsctlRequestAsync(
                context,
                verifyPipeName,
                verifyArguments)
            .ConfigureAwait(false);

        AssertEqual(0, verifyExitCode, "verify profile exit code");
        AssertEqual(44, verifyRequest.GetProperty("command").GetInt32(), "verify file command id");
        AssertEqual(
            @"C:\captures\clip.mp4",
            verifyRequest.GetProperty("payload").GetProperty("filePath").GetString(),
            "verify file payload path");
        AssertEqual(
            "flashback-export",
            verifyRequest.GetProperty("payload").GetProperty("verificationProfile").GetString(),
            "verify file payload profile");

        var verifyLastPipeName = $"ssctl-verify-last-{Guid.NewGuid():N}";
        var verifyLastArguments = new List<string> { "verify" };
        var (verifyLastExitCode, verifyLastRequest) = await CaptureSsctlRequestAsync(
                context,
                verifyLastPipeName,
                verifyLastArguments)
            .ConfigureAwait(false);

        AssertEqual(0, verifyLastExitCode, "verify last exit code");
        AssertEqual(21, verifyLastRequest.GetProperty("command").GetInt32(), "verify last command id");
    }
}
