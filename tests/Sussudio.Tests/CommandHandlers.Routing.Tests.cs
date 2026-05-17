using System.Threading.Tasks;

static partial class Program
{
    private static async Task SsctlCommandHandlers_RouteDeviceCommands()
    {
        var context = CreateSsctlCommandRoutingContext();

        var devicePipeName = $"ssctl-device-audio-{Guid.NewGuid():N}";
        var deviceArguments = new List<string> { "device", "audio-select", "Synthetic Mic" };
        var (deviceExitCode, deviceRequest) = await CaptureSsctlRequestAsync(
                context,
                devicePipeName,
                deviceArguments)
            .ConfigureAwait(false);

        AssertEqual(0, deviceExitCode, "device audio-select exit code");
        AssertSsctlCommandRequest(deviceRequest, "SelectAudioInputDevice", ("deviceName", "Synthetic Mic"));

        var deviceRefreshPipeName = $"ssctl-device-refresh-{Guid.NewGuid():N}";
        var deviceRefreshArguments = new List<string> { "device", "refresh" };
        var (deviceRefreshExitCode, deviceRefreshRequest) = await CaptureSsctlRequestAsync(
                context,
                deviceRefreshPipeName,
                deviceRefreshArguments)
            .ConfigureAwait(false);

        AssertEqual(0, deviceRefreshExitCode, "device refresh exit code");
        AssertSsctlCommandRequestHasEmptyPayload(deviceRefreshRequest, "RefreshDevices");

        var deviceListPipeName = $"ssctl-device-list-{Guid.NewGuid():N}";
        var deviceListArguments = new List<string> { "device", "list" };
        var (deviceListExitCode, deviceListRequests) = await CaptureSsctlRequestsAsync(
                context,
                deviceListPipeName,
                expectedCount: 2,
                arguments: deviceListArguments,
                responseFactory: i => i == 0
                    ? "{\"Success\":true,\"Message\":\"refresh ok\"}"
                    : "{\"Success\":true,\"Message\":\"options ok\",\"Data\":{\"Devices\":[],\"AudioInputDevices\":[]}}")
            .ConfigureAwait(false);

        AssertEqual(0, deviceListExitCode, "device list exit code");
        AssertSsctlCommandRequestHasEmptyPayload(deviceListRequests[0], "RefreshDevices");
        AssertSsctlCommandRequestHasEmptyPayload(deviceListRequests[1], "GetCaptureOptions");
    }

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
        AssertSsctlCommandRequest(previewRequest, "SetPreviewEnabled", ("enabled", true));
    }

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
        AssertSsctlCommandRequestHasEmptyPayload(recordingsRequest, "OpenRecordingsFolder");
    }

    private static async Task SsctlCommandHandlers_RouteFlashbackCommands()
    {
        var context = CreateSsctlCommandRoutingContext();

        var flashbackPipeName = $"ssctl-flashback-{Guid.NewGuid():N}";
        var flashbackArguments = new List<string> { "flashback", "off" };
        var (flashbackExitCode, flashbackRequest) = await CaptureSsctlRequestAsync(
                context,
                flashbackPipeName,
                flashbackArguments)
            .ConfigureAwait(false);

        AssertEqual(0, flashbackExitCode, "flashback off exit code");
        AssertSsctlCommandRequest(flashbackRequest, "SetFlashbackEnabled", ("enabled", false));

        var flashbackExportPipeName = $"ssctl-flashback-export-{Guid.NewGuid():N}";
        var flashbackExportOutputPath = Path.Combine("temp", "ssctl flashback export", "export with spaces.mp4");
        var flashbackExportArguments = new List<string>
        {
            "flashback",
            "export",
            "--range",
            "--force",
            "2.5",
            flashbackExportOutputPath,
        };
        var (flashbackExportExitCode, flashbackExportRequest) = await CaptureSsctlRequestAsync(
                context,
                flashbackExportPipeName,
                flashbackExportArguments)
            .ConfigureAwait(false);

        AssertEqual(0, flashbackExportExitCode, "flashback export exit code");
        AssertSsctlCommandRequest(
            flashbackExportRequest,
            "FlashbackExport",
            ("seconds", 2.5d),
            ("outputPath", flashbackExportOutputPath),
            ("useSelectionRange", true),
            ("force", true));
        AssertEqual(
            true,
            Directory.Exists(Path.GetDirectoryName(flashbackExportOutputPath) ?? "."),
            "flashback export parent directory created");

        var flashbackSeekPipeName = $"ssctl-flashback-seek-{Guid.NewGuid():N}";
        var (flashbackSeekExitCode, flashbackSeekRequest) = await CaptureSsctlRequestAsync(
                context,
                flashbackSeekPipeName,
                new List<string> { "flashback", "seek", "1234.5" })
            .ConfigureAwait(false);

        AssertEqual(0, flashbackSeekExitCode, "flashback seek exit code");
        AssertSsctlCommandRequest(
            flashbackSeekRequest,
            "FlashbackAction",
            ("action", "seek"),
            ("positionMs", 1234.5d));

        var flashbackScrubPipeName = $"ssctl-flashback-scrub-{Guid.NewGuid():N}";
        var (flashbackScrubExitCode, flashbackScrubRequest) = await CaptureSsctlRequestAsync(
                context,
                flashbackScrubPipeName,
                new List<string> { "flashback", "begin-scrub", "250" })
            .ConfigureAwait(false);

        AssertEqual(0, flashbackScrubExitCode, "flashback begin-scrub exit code");
        AssertSsctlCommandRequest(
            flashbackScrubRequest,
            "FlashbackAction",
            ("action", "begin-scrub"),
            ("positionMs", 250d));

        var flashbackClearRangePipeName = $"ssctl-flashback-clear-range-{Guid.NewGuid():N}";
        var (flashbackClearRangeExitCode, flashbackClearRangeRequest) = await CaptureSsctlRequestAsync(
                context,
                flashbackClearRangePipeName,
                new List<string> { "flashback", "clear-range" })
            .ConfigureAwait(false);

        AssertEqual(0, flashbackClearRangeExitCode, "flashback clear-range exit code");
        AssertSsctlCommandRequest(
            flashbackClearRangeRequest,
            "FlashbackAction",
            ("action", "clear-in-out-points"));
    }

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
        AssertSsctlCommandRequestHasEmptyPayload(audioRampRequest, "GetAudioRampTrace");
    }

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
