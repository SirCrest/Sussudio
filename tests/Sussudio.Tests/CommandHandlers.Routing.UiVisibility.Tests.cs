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
        AssertSsctlCommandRequest(
            statsSectionRequest,
            "SetStatsSectionVisible",
            ("section", "Preview Cadence"),
            ("visible", false));

        var settingsPipeName = $"ssctl-settings-show-{Guid.NewGuid():N}";
        var settingsArguments = new List<string> { "settings", "show" };
        var (settingsExitCode, settingsRequest) = await CaptureSsctlRequestAsync(
                context,
                settingsPipeName,
                settingsArguments)
            .ConfigureAwait(false);

        AssertEqual(0, settingsExitCode, "settings show exit code");
        AssertSsctlCommandRequest(settingsRequest, "SetSettingsVisible", ("visible", true));

        var frameTimePipeName = $"ssctl-frametime-hide-{Guid.NewGuid():N}";
        var frameTimeArguments = new List<string> { "frame-time", "hide" };
        var (frameTimeExitCode, frameTimeRequest) = await CaptureSsctlRequestAsync(
                context,
                frameTimePipeName,
                frameTimeArguments)
            .ConfigureAwait(false);

        AssertEqual(0, frameTimeExitCode, "frametime hide exit code");
        AssertSsctlCommandRequest(frameTimeRequest, "SetFrameTimeOverlayVisible", ("visible", false));
    }
}
