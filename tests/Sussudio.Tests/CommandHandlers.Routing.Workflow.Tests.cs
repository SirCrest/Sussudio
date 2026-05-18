using System.Threading.Tasks;

static partial class Program
{
    private static async Task SsctlCommandHandlers_RouteAutomationFlowCommands()
    {
        var context = CreateSsctlCommandRoutingContext();

        var assertPipeName = $"ssctl-assert-simple-{Guid.NewGuid():N}";
        var assertArguments = new List<string> { "assert", "IsRecording", "eq", "false" };
        var (assertExitCode, assertRequest) = await CaptureSsctlRequestAsync(
                context,
                assertPipeName,
                assertArguments)
            .ConfigureAwait(false);

        AssertEqual(0, assertExitCode, "assert simple exit code");
        var assertPayload = AssertSsctlCommandRequest(assertRequest, "AssertSnapshot")
            .GetProperty("assertions")[0];
        AssertEqual("IsRecording", assertPayload.GetProperty("field").GetString(), "assert simple field");
        AssertEqual("eq", assertPayload.GetProperty("op").GetString(), "assert simple op");
        AssertEqual(false, assertPayload.GetProperty("value").GetBoolean(), "assert simple value");

        var waitPipeName = $"ssctl-wait-{Guid.NewGuid():N}";
        var waitArguments = new List<string> { "wait", "preview-ready", "--timeout", "12500", "--poll", "250" };
        var (waitExitCode, waitRequest) = await CaptureSsctlRequestAsync(
                context,
                waitPipeName,
                waitArguments)
            .ConfigureAwait(false);

        AssertEqual(0, waitExitCode, "wait exit code");
        AssertSsctlCommandRequest(
            waitRequest,
            "WaitForCondition",
            ("condition", "preview-ready"),
            ("timeoutMs", 12500),
            ("pollMs", 250));

        var probePipeName = $"ssctl-probe-color-{Guid.NewGuid():N}";
        var probeArguments = new List<string> { "probe", "color" };
        var (probeExitCode, probeRequest) = await CaptureSsctlRequestAsync(
                context,
                probePipeName,
                probeArguments)
            .ConfigureAwait(false);

        AssertEqual(0, probeExitCode, "probe color exit code");
        AssertSsctlCommandRequestHasEmptyPayload(probeRequest, "ProbePreviewColor");
    }

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
        AssertSsctlCommandRequest(
            verifyRequest,
            "VerifyFile",
            ("filePath", @"C:\captures\clip.mp4"),
            ("verificationProfile", "flashback-export"));

        var verifyLastPipeName = $"ssctl-verify-last-{Guid.NewGuid():N}";
        var verifyLastArguments = new List<string> { "verify" };
        var (verifyLastExitCode, verifyLastRequest) = await CaptureSsctlRequestAsync(
                context,
                verifyLastPipeName,
                verifyLastArguments)
            .ConfigureAwait(false);

        AssertEqual(0, verifyLastExitCode, "verify last exit code");
        AssertSsctlCommandRequestHasEmptyPayload(verifyLastRequest, "VerifyLastRecording");
    }
}
