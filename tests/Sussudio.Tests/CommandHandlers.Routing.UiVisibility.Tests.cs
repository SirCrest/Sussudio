using System.Threading.Tasks;

static partial class Program
{
    private static async Task SsctlCommandHandlers_RouteUiVisibilityCommands()
    {
        var context = CreateSsctlCommandRoutingContext();

        var statsSectionPipeName = $"ssctl-stats-section-{Guid.NewGuid():N}";
        var statsSectionArguments = new List<string> { "stats", "section", "Preview Cadence", "hide" };
        var (statsSectionExitCode, statsSectionRequest) = await CaptureSsctlRequestAsync(
                context,
                statsSectionPipeName,
                statsSectionArguments)
            .ConfigureAwait(false);

        AssertEqual(0, statsSectionExitCode, "stats section exit code");
        AssertEqual(38, statsSectionRequest.GetProperty("command").GetInt32(), "stats section command id");
        var statsSectionPayload = statsSectionRequest.GetProperty("payload");
        AssertEqual("Preview Cadence", statsSectionPayload.GetProperty("section").GetString(), "stats section name payload");
        AssertEqual(false, statsSectionPayload.GetProperty("visible").GetBoolean(), "stats section visible payload");

        var settingsPipeName = $"ssctl-settings-show-{Guid.NewGuid():N}";
        var settingsArguments = new List<string> { "settings", "show" };
        var (settingsExitCode, settingsRequest) = await CaptureSsctlRequestAsync(
                context,
                settingsPipeName,
                settingsArguments)
            .ConfigureAwait(false);

        AssertEqual(0, settingsExitCode, "settings show exit code");
        AssertEqual(40, settingsRequest.GetProperty("command").GetInt32(), "settings show command id");
        AssertEqual(
            true,
            settingsRequest.GetProperty("payload").GetProperty("visible").GetBoolean(),
            "settings show visible payload");

        var frameTimePipeName = $"ssctl-frametime-hide-{Guid.NewGuid():N}";
        var frameTimeArguments = new List<string> { "frame-time", "hide" };
        var (frameTimeExitCode, frameTimeRequest) = await CaptureSsctlRequestAsync(
                context,
                frameTimePipeName,
                frameTimeArguments)
            .ConfigureAwait(false);

        AssertEqual(0, frameTimeExitCode, "frametime hide exit code");
        AssertEqual(49, frameTimeRequest.GetProperty("command").GetInt32(), "frametime hide command id");
        AssertEqual(
            false,
            frameTimeRequest.GetProperty("payload").GetProperty("visible").GetBoolean(),
            "frametime hide visible payload");
    }
}
