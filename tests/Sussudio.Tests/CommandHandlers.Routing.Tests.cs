using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Tools;

static partial class Program
{
    internal static async Task SsctlCommandHandlers_RouteDeviceCommands()
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

    internal static async Task SsctlCommandHandlers_RouteCaptureControlCommands()
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

    internal static async Task SsctlCommandHandlers_RouteRecordingsCommands()
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

    internal static async Task SsctlCommandHandlers_RouteWindowCommands()
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

    internal static async Task SsctlCommandHandlers_RouteManifestCommand()
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

    internal static async Task SsctlCommandHandlers_RouteFlashbackCommands()
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

    internal static async Task SsctlCommandHandlers_RouteObservabilityCommands()
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

    internal static async Task SsctlCommandHandlers_RouteAutomationFlowCommands()
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

    internal static async Task SsctlCommandHandlers_RouteUiVisibilityCommands()
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

    internal static async Task SsctlCommandHandlers_RouteVerificationCommands()
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

    internal static Task SsctlCommandHandlers_SourceOwnership_IsConsolidated()
    {
        AssertSsctlCommandRoutingTestsUseCommandIdHelper();
        var commandHandlersSource = ReadSsctlCommandHandlersFamilyText();
        var commandHandlersRootSource = ReadRepoFile("tools/ssctl/CommandHandlers.cs")
            .Replace("\r\n", "\n");

        AssertEqual(commandHandlersRootSource, commandHandlersSource, "ssctl command-handler source family is consolidated into CommandHandlers.cs");
        AssertContains(commandHandlersRootSource, "private sealed class CommandContext");
        AssertContains(commandHandlersRootSource, "Rest = arguments.Skip(1).ToList();");
        AssertContains(commandHandlersRootSource, "private static async Task<int> HandleSimpleCommandAsync(");
        AssertContains(commandHandlersRootSource, "context.Transport.SendCommandAsync(kind, payload)");
        AssertContains(commandHandlersRootSource, "private static int WriteResponse(JsonElement response, bool json, Func<JsonElement, string> formatter)");
        AssertContains(commandHandlersRootSource, "private static string JoinRemaining(IReadOnlyList<string> args, int startIndex)");
        AssertContains(commandHandlersRootSource, "private static bool ConsumeFlag(List<string> args, string flag)");
        AssertContains(commandHandlersRootSource, "private static bool LooksLikeJson(string value)");
        AssertContains(commandHandlersRootSource, "private static string PrettyJson<T>(T value)");
        AssertContains(commandHandlersRootSource, "private static object? ParseAssertionValue(string value)");

        AssertContains(commandHandlersRootSource, "// Observability command family.");
        AssertContains(commandHandlersRootSource, "HandleAudioRampTraceAsync");
        AssertContains(commandHandlersRootSource, "HandleDiagnosticSessionAsync");
        AssertContains(commandHandlersRootSource, "HandlePresentMonAsync");
        AssertContains(commandHandlersRootSource, "TryResolvePreviewPresentCorrelationAsync");
        AssertContains(commandHandlersRootSource, "PresentMonProbe.CreateOptions(");
        AssertContains(commandHandlersRootSource, "DiagnosticSessionRunner.RunAsync(");

        AssertContains(commandHandlersRootSource, "// CaptureControls command family.");
        AssertContains(commandHandlersRootSource, "HandleSetAsync");
        AssertSsctlCapturePipelineRoutingUsesAutomationCommandKinds();
        AssertContains(commandHandlersRootSource, "HandleDeviceAsync");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.RefreshDevices");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.GetCaptureOptions");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.SelectDevice");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.SelectAudioInputDevice");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.SetCustomAudioInput");

        AssertContains(commandHandlersRootSource, "// Window command family.");
        AssertContains(commandHandlersRootSource, "HandleWindowAsync");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.ArmClose");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.WindowAction");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.SetFullScreenEnabled");
        AssertContains(commandHandlersRootSource, "HandleRecordingsAsync");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.OpenRecordingsFolder");
        AssertContains(commandHandlersRootSource, "HandleStatsAsync");
        AssertContains(commandHandlersRootSource, "HandleSettingsAsync");
        AssertContains(commandHandlersRootSource, "HandleFrameTimeAsync");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.SetStatsVisible");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.SetStatsSectionVisible");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.SetSettingsVisible");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.SetFrameTimeOverlayVisible");

        AssertContains(commandHandlersRootSource, "HandleWaitAsync");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.WaitForCondition");
        AssertContains(commandHandlersRootSource, "Math.Max(timeoutMs.GetValueOrDefault(0) + 5000, 60000)");
        AssertContains(commandHandlersRootSource, "HandleAssertAsync");
        AssertContains(commandHandlersRootSource, "JsonDocument.Parse(assertionsJson)");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.AssertSnapshot");
        AssertContains(commandHandlersRootSource, "HandleProbeAsync");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.ProbeVideoSource");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.ProbePreviewColor");
        AssertContains(commandHandlersRootSource, "HandleVerifyAsync");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.VerifyFile");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.VerifyLastRecording");
        AssertContains(commandHandlersRootSource, "ConsumeFlag(context.Rest, \"--json\")");
        AssertContains(commandHandlersRootSource, "ParseOptionalStringFlag(context.Rest, \"--verification-profile\")");

        AssertContains(commandHandlersRootSource, "// Flashback command family.");
        AssertContains(commandHandlersRootSource, "HandleFlashbackAsync");
        AssertContains(commandHandlersRootSource, "return HandleFlashbackActionAsync(context, subcommand);");
        AssertContains(commandHandlersRootSource, "return HandleFlashbackExportAsync(context);");
        AssertContains(commandHandlersRootSource, "private static Task<int> HandleFlashbackActionAsync(CommandContext context, string subcommand)");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.FlashbackAction");
        AssertContains(commandHandlersRootSource, "playPayload[\"positionMs\"] = ParseFlashbackPositionMs(context.Rest[1]);");
        AssertContains(commandHandlersRootSource, "[\"action\"] = \"begin-scrub\"");
        AssertContains(commandHandlersRootSource, "[\"action\"] = \"clear-in-out-points\"");
        AssertContains(commandHandlersRootSource, "private static double ParseFlashbackPositionMs(string value)");
        AssertContains(commandHandlersRootSource, "Flashback position must be finite, non-negative, and within TimeSpan range.");
        AssertContains(commandHandlersRootSource, "private static Task<int> HandleFlashbackExportAsync(CommandContext context)");
        AssertContains(commandHandlersRootSource, "ConsumeFlag(context.Rest, \"--range\")");
        AssertContains(commandHandlersRootSource, "ConsumeFlag(context.Rest, \"--force\")");
        AssertContains(commandHandlersRootSource, "? ParseFlashbackExportSeconds(context.Rest[1])");
        AssertContains(commandHandlersRootSource, "Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? \".\")");

        AssertContains(commandHandlersSource, "\"manifest\" => HandleManifestAsync(context)");
        AssertContains(commandHandlersSource, "\"audio-ramp-trace\" => HandleAudioRampTraceAsync(context)");
        AssertContains(commandHandlersSource, "\"recordings\" => HandleRecordingsAsync(context)");
        AssertSsctlFixedAutomationRoutesUseAutomationCommandKinds(commandHandlersSource);
        AssertContains(commandHandlersSource, "return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackAction, playPayload, includeData: true);");
        AssertContains(commandHandlersSource, "ParseOptionalStringFlag(context.Rest, \"--profile\")");
        AssertContains(commandHandlersSource, "payload[\"verificationProfile\"] = verificationProfile;");
        AssertContains(commandHandlersSource, "[\"positionMs\"] = ParseFlashbackPositionMs(RequireWord(context.Rest, 1, \"flashback seek <ms>\"))");
        AssertContains(commandHandlersSource, "[\"positionMs\"] = ParseFlashbackPositionMs(RequireWord(context.Rest, 1, \"flashback begin-scrub <ms>\"))");
        AssertContains(commandHandlersSource, "[\"positionMs\"] = ParseFlashbackPositionMs(RequireWord(context.Rest, 1, \"flashback update-scrub <ms>\"))");
        AssertContains(commandHandlersSource, "var payload = new Dictionary<string, object?> { [\"action\"] = \"end-scrub\" };");
        AssertContains(commandHandlersSource, "payload[\"positionMs\"] = ParseFlashbackPositionMs(context.Rest[1]);");
        AssertContains(commandHandlersSource, "private static double ParseFlashbackExportSeconds(string value)");
        AssertContains(commandHandlersSource, "Flashback export seconds must be finite, greater than zero, and within TimeSpan range.");
        AssertContains(commandHandlersSource, "assert <json> OR assert <field> <op> <value>");

        foreach (var removedFile in new[]
        {
            "CommandHandlers.Observability.cs",
            "CommandHandlers.CaptureControls.cs",
            "CommandHandlers.Window.cs",
            "CommandHandlers.Flashback.cs",
            "CommandHandlers.DiagnosticSession.cs",
            "CommandHandlers.PresentMon.cs",
            "CommandHandlers.Device.cs",
            "CommandHandlers.AutomationFlow.cs",
            "CommandHandlers.UiVisibility.cs",
            "CommandHandlers.Flashback.Actions.cs",
            "CommandHandlers.Parsing.cs",
            "CommandHandlers.Flags.cs",
            "CommandHandlers.Json.cs",
            "CommandHandlers.DeviceWindow.cs",
            "CommandHandlers.Context.cs",
            "CommandHandlers.Arguments.cs",
            "CommandHandlers.Values.cs",
            "CommandHandlers.Flashback.Export.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "tools", "ssctl", removedFile)),
                $"ssctl command-handler implementation stays consolidated in CommandHandlers.cs, not {removedFile}");
        }

        return Task.CompletedTask;
    }

    private static void AssertSsctlFixedAutomationRoutesUseAutomationCommandKinds(string commandHandlersSource)
    {
        AssertContains(
            commandHandlersSource,
            "(command, payload, responseTimeoutMs) => context.Transport.SendCommandAsync(command, payload, responseTimeoutMs)");
        AssertDoesNotContain(
            ReadRepoFile("tools/ssctl/CommandHandlers.cs"),
            "private static async Task<int> HandleSimpleCommandAsync(\n        CommandContext context,\n        string commandName,");

        foreach (var commandName in new[]
        {
            "GetSnapshot",
            "GetDiagnostics",
            "RefreshDevices",
            "GetCaptureOptions",
            "GetAutomationManifest",
            "GetAudioRampTrace",
            "GetPerformanceTimeline",
            "SelectDevice",
            "SelectAudioInputDevice",
            "SetCustomAudioInput",
            "ArmClose",
            "WindowAction",
            "SetFullScreenEnabled",
            "OpenRecordingsFolder",
            "WaitForCondition",
            "AssertSnapshot",
            "ProbeVideoSource",
            "ProbePreviewColor",
            "VerifyFile",
            "VerifyLastRecording",
            "SetFlashbackEnabled",
            "SetFlashbackTimelineVisible",
            "RestartFlashback",
            "FlashbackAction",
            "FlashbackExport",
            "FlashbackGetSegments"
        })
        {
            AssertContains(commandHandlersSource, $"AutomationCommandKind.{commandName}");
            AssertDoesNotContain(commandHandlersSource, $"SendCommandAsync(\"{commandName}\"");
            AssertDoesNotContain(commandHandlersSource, $"HandleSimpleCommandAsync(context, \"{commandName}\"");
        }
    }

    private static void AssertSsctlCapturePipelineRoutingUsesAutomationCommandKinds()
    {
        var rootSource = ReadRepoFile("tools/ssctl/CommandHandlers.cs");
        var captureControlsSource = rootSource;

        AssertContains(rootSource, "HandleCaptureAsync(context, AutomationCommandKind.CaptureWindowScreenshot");
        AssertContains(rootSource, "HandleCaptureAsync(context, AutomationCommandKind.CapturePreviewFrame");
        AssertDoesNotContain(rootSource, "HandleCaptureAsync(context, \"CaptureWindowScreenshot\"");
        AssertDoesNotContain(rootSource, "HandleCaptureAsync(context, \"CapturePreviewFrame\"");

        AssertContains(captureControlsSource, "HandleSimpleCommandAsync(\n            context,\n            AutomationCommandKind.SetPreviewEnabled,");
        AssertContains(captureControlsSource, "HandleSimpleCommandAsync(\n            context,\n            AutomationCommandKind.SetRecordingEnabled,");
        AssertContains(captureControlsSource, "private static Task<int> HandleCaptureAsync(CommandContext context, AutomationCommandKind kind, string defaultPath)");
        AssertContains(captureControlsSource, "HandleSimpleCommandAsync(\n            context,\n            kind,");

        foreach (var commandName in new[]
        {
            "SetResolution",
            "SetFrameRate",
            "SetRecordingFormat",
            "SetQuality",
            "SetCustomBitrate",
            "SetPreset",
            "SetSplitEncodeMode",
            "SetVideoFormat",
            "SetMjpegDecoderCount",
            "SetHdrEnabled",
            "SetTrueHdrPreviewEnabled",
            "SetAudioEnabled",
            "SetAudioPreviewEnabled",
            "SetPreviewVolume",
            "SetDeviceAudioMode",
            "SetAnalogAudioGain",
            "SetOutputPath",
            "SetMicrophoneEnabled"
        })
        {
            AssertContains(captureControlsSource, $"SendSetValueAsync(context, AutomationCommandKind.{commandName},");
            AssertDoesNotContain(captureControlsSource, $"SendSetValueAsync(context, \"{commandName}\"");
        }

        AssertContains(captureControlsSource, "private static Task<int> SendSetValueAsync(\n        CommandContext context,\n        AutomationCommandKind kind,");
        AssertContains(captureControlsSource, "HandleSimpleCommandAsync(\n            context,\n            kind,");
        AssertContains(rootSource, "context.Transport.SendCommandAsync(kind, payload)");
    }

    internal static Task SsctlHelp_UsesCatalogCliHelpForAutomationCommands()
    {
        var ssctlProgramText = ReadRepoFile("tools/ssctl/Program.cs")
            .Replace("\r\n", "\n");
        var helpWriterText = ssctlProgramText;
        var catalogEntriesText = ReadRepoFile("Sussudio.Automation.Contracts/AutomationCommandCatalog.cs")
            .Replace("\r\n", "\n");
        var flashbackHandlersText = ReadRepoFile("tools/ssctl/CommandHandlers.cs")
            .Replace("\r\n", "\n");
        var ssctlAssembly = LoadToolAssemblyIsolated(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        var helpWriterType = ssctlAssembly.GetType("Sussudio.Tools.Ssctl.SsctlHelpWriter")
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.SsctlHelpWriter type not found.");
        var diagnosticSessionOptionsType = ssctlAssembly.GetType("Sussudio.Tools.DiagnosticSessionOptions")
            ?? throw new InvalidOperationException("Sussudio.Tools.DiagnosticSessionOptions type not found.");
        var diagnosticSessionCliUsage = diagnosticSessionOptionsType
            .GetField("CliUsage", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?.GetValue(null) as string
            ?? throw new InvalidOperationException("DiagnosticSessionOptions.CliUsage field not found.");
        var writeHelp = RequireNonPublicStaticMethod(helpWriterType, "Write");
        using var writer = new StringWriter();
        writeHelp.Invoke(null, new object[] { writer });
        var helpOutput = writer.ToString().Replace("\r\n", "\n");

        AssertContains(catalogEntriesText, "\"flashback export [seconds] [path] [--range] [--force]\"");
        AssertContains(flashbackHandlersText, "ConsumeFlag(context.Rest, \"--force\")");
        AssertContains(ssctlProgramText, "SsctlHelpWriter.Write(Console.Out);");
        AssertContains(ssctlProgramText, "AutomationCommandCatalog.Get(kind).CliHelp");
        AssertContains(ssctlProgramText, "WriteCatalogHelpLine");
        AssertContains(helpWriterText, "internal static class SsctlHelpWriter");
        AssertContains(helpWriterText, "WriteHeader(writer);");
        AssertContains(helpWriterText, "WriteFlashbackSection(writer);");
        AssertContains(helpWriterText, "WriteFlagsSection(writer);");
        AssertContains(helpWriterText, "AutomationCommandCatalog.Get(kind).CliHelp");
        AssertContains(helpWriterText, "private static void WriteCatalogHelpLine(TextWriter writer, AutomationCommandKind kind, string? suffix = null)");
        AssertContains(helpWriterText, "private static void WriteFlashbackSection(TextWriter writer)");
        AssertContains(helpWriterText, "private static void WriteWaitVerifySection(TextWriter writer)");
        AssertContains(helpWriterText, "WriteCatalogHelpLine(writer, AutomationCommandKind.FlashbackExport);");
        AssertContains(helpWriterText, "WriteCatalogHelpLine(writer, AutomationCommandKind.FlashbackGetSegments);");
        AssertContains(helpWriterText, "WriteCatalogHelpLine(writer, AutomationCommandKind.SetFrameTimeOverlayVisible);");
        AssertContains(helpWriterText, "WriteCatalogHelpLine(writer, AutomationCommandKind.SetFlashbackTimelineVisible);");
        AssertContains(helpWriterText, "DiagnosticSessionOptions.CliUsage");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "ssctl", "SsctlHelpWriter.cs")),
            "ssctl help facade folded into the CLI front-door file");
        AssertContains(helpOutput, "ssctl");
        AssertContains(helpOutput, "Usage:");
        AssertContains(helpOutput, "Flashback:");
        AssertContains(helpOutput, "Flags:");
        AssertEqual(BuildExpectedSsctlHelpOutput(diagnosticSessionCliUsage), helpOutput, "full ssctl help output");
        AssertContains(helpOutput, $"  {AutomationCommandCatalog.Get(AutomationCommandKind.FlashbackExport).CliHelp}");
        AssertContains(helpOutput, $"  {AutomationCommandCatalog.Get(AutomationCommandKind.FlashbackGetSegments).CliHelp}");
        AssertContains(helpOutput, $"  {AutomationCommandCatalog.Get(AutomationCommandKind.SetFrameTimeOverlayVisible).CliHelp}");
        AssertContains(helpOutput, $"  {AutomationCommandCatalog.Get(AutomationCommandKind.SetFlashbackTimelineVisible).CliHelp}");

        AssertEqual("flashback export [seconds] [path] [--range] [--force]",
            AutomationCommandCatalog.Get(AutomationCommandKind.FlashbackExport).CliHelp,
            "catalog Flashback export CLI help");
        AssertEqual("flashback segments",
            AutomationCommandCatalog.Get(AutomationCommandKind.FlashbackGetSegments).CliHelp,
            "catalog Flashback segments CLI help");

        return Task.CompletedTask;
    }

    private static string BuildExpectedSsctlHelpOutput(string diagnosticSessionCliUsage)
    {
        static string HelpLine(AutomationCommandKind kind, string? suffix = null)
        {
            var command = AutomationCommandCatalog.Get(kind).CliHelp;
            return string.IsNullOrWhiteSpace(suffix)
                ? $"  {command}"
                : $"  {command} {suffix}";
        }

        var lines = new[]
        {
            "ssctl",
            "Usage:",
            "  ssctl [--json] [--pipe NAME] [--timeout MS] <command>",
            "",
            "Query:",
            HelpLine(AutomationCommandKind.GetSnapshot, "[--json]"),
            HelpLine(AutomationCommandKind.GetDiagnostics, "[--json]"),
            HelpLine(AutomationCommandKind.GetCaptureOptions, "[--json]"),
            HelpLine(AutomationCommandKind.GetAutomationManifest, "[--json]"),
            HelpLine(AutomationCommandKind.GetPerformanceTimeline, "[--json]"),
            "  memory [--json]",
            HelpLine(AutomationCommandKind.GetAudioRampTrace, "[--json]"),
            "  presentmon [--seconds N] [--pid PID|--process NAME] [--swapchain HEX] [--app-present-id N] [--app-source-seq N] [--app-present-utc-ms N] [--capture-start-utc-ms N] [--presentmon PATH] [--output PATH] [--keep-csv] [--json]",
            $"  {diagnosticSessionCliUsage}",
            "",
            "Control:",
            HelpLine(AutomationCommandKind.SetPreviewEnabled),
            HelpLine(AutomationCommandKind.SetRecordingEnabled),
            HelpLine(AutomationCommandKind.CaptureWindowScreenshot),
            HelpLine(AutomationCommandKind.CapturePreviewFrame),
            HelpLine(AutomationCommandKind.OpenRecordingsFolder),
            "",
            "Configure:",
            HelpLine(AutomationCommandKind.SetResolution),
            HelpLine(AutomationCommandKind.SetFrameRate),
            HelpLine(AutomationCommandKind.SetRecordingFormat),
            HelpLine(AutomationCommandKind.SetQuality),
            HelpLine(AutomationCommandKind.SetCustomBitrate),
            HelpLine(AutomationCommandKind.SetPreset),
            HelpLine(AutomationCommandKind.SetSplitEncodeMode),
            HelpLine(AutomationCommandKind.SetVideoFormat),
            HelpLine(AutomationCommandKind.SetMjpegDecoderCount),
            HelpLine(AutomationCommandKind.SetHdrEnabled),
            HelpLine(AutomationCommandKind.SetTrueHdrPreviewEnabled),
            HelpLine(AutomationCommandKind.SetAudioEnabled),
            HelpLine(AutomationCommandKind.SetAudioPreviewEnabled),
            HelpLine(AutomationCommandKind.SetPreviewVolume),
            HelpLine(AutomationCommandKind.SetDeviceAudioMode),
            HelpLine(AutomationCommandKind.SetAnalogAudioGain),
            HelpLine(AutomationCommandKind.SetOutputPath),
            HelpLine(AutomationCommandKind.SetShowAllCaptureOptions),
            HelpLine(AutomationCommandKind.SetMicrophoneEnabled),
            "",
            "Device:",
            HelpLine(AutomationCommandKind.RefreshDevices),
            "  device list",
            HelpLine(AutomationCommandKind.SelectDevice),
            HelpLine(AutomationCommandKind.SelectAudioInputDevice),
            HelpLine(AutomationCommandKind.SetCustomAudioInput),
            "",
            "Flashback:",
            HelpLine(AutomationCommandKind.SetFlashbackEnabled),
            HelpLine(AutomationCommandKind.SetFlashbackTimelineVisible),
            "  flashback play [<ms>]",
            "  flashback pause",
            "  flashback go-live",
            "  flashback seek <ms>",
            "  flashback begin-scrub <ms>",
            "  flashback update-scrub <ms>",
            "  flashback end-scrub [<ms>]",
            "  flashback set-in|set-out|clear-range",
            HelpLine(AutomationCommandKind.FlashbackExport),
            HelpLine(AutomationCommandKind.FlashbackGetSegments),
            HelpLine(AutomationCommandKind.RestartFlashback),
            "",
            "Window:",
            "  window close|minimize|maximize|restore|center",
            HelpLine(AutomationCommandKind.SetFullScreenEnabled),
            "  window snap left|right|top-left|top-right|bottom-left|bottom-right",
            "  window move <x> <y>",
            "  window resize <w> <h>",
            "",
            "Wait / Verify:",
            HelpLine(AutomationCommandKind.WaitForCondition, "[--timeout MS] [--poll MS]"),
            "  verify [path] [--profile NAME|--verification-profile NAME]",
            "  assert <json>|<field> <op> <value>",
            "  probe source|color",
            HelpLine(AutomationCommandKind.SetStatsVisible),
            HelpLine(AutomationCommandKind.SetStatsSectionVisible),
            HelpLine(AutomationCommandKind.SetFrameTimeOverlayVisible),
            HelpLine(AutomationCommandKind.SetSettingsVisible),
            "",
            "Flags:",
            "  --json            Print raw JSON responses where supported",
            "  --pipe NAME       Named pipe (default: SussudioAutomation)",
            "  --timeout MS      Response timeout override for pipe calls",
            "  --verbose         On error, print full stack trace + InnerException chain to stderr",
            "  --help            Show this help",
            "",
        };

        return string.Join('\n', lines);
    }

    private readonly record struct SsctlCommandRoutingContext(Type TransportType, MethodInfo ExecuteAsync);

    private static SsctlCommandRoutingContext CreateSsctlCommandRoutingContext()
    {
        var assemblyPath = Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll");
        var ssctlAssembly = LoadToolAssemblyIsolated(assemblyPath);
        var transportType = ssctlAssembly.GetType("Sussudio.Tools.Ssctl.PipeTransport")
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.PipeTransport type not found.");
        var commandHandlersType = ssctlAssembly.GetType("Sussudio.Tools.Ssctl.CommandHandlers")
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.CommandHandlers type not found.");
        var executeAsync = commandHandlersType.GetMethod("ExecuteAsync", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.CommandHandlers.ExecuteAsync not found.");

        return new SsctlCommandRoutingContext(transportType, executeAsync);
    }

    private static object CreateSsctlTransport(SsctlCommandRoutingContext context, string pipeName)
        => Activator.CreateInstance(context.TransportType, pipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for ssctl command-handler routing test.");

    private static async Task<(int ExitCode, JsonElement Request)> CaptureSsctlRequestAsync(
        SsctlCommandRoutingContext context,
        string pipeName,
        List<string> arguments)
    {
        var transport = CreateSsctlTransport(context, pipeName);
        var exitCode = -1;
        JsonElement request = await CapturePipeRequestAsync(
                pipeName,
                async () =>
                {
                    var task = context.ExecuteAsync.Invoke(null, new object?[] { transport, arguments, false }) as Task<int>
                        ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                    exitCode = await task.ConfigureAwait(false);
                })
            .ConfigureAwait(false);

        return (exitCode, request);
    }

    private static async Task<(int ExitCode, JsonElement[] Requests)> CaptureSsctlRequestsAsync(
        SsctlCommandRoutingContext context,
        string pipeName,
        int expectedCount,
        List<string> arguments,
        Func<int, string>? responseFactory = null)
    {
        var transport = CreateSsctlTransport(context, pipeName);
        var exitCode = -1;
        JsonElement[] requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount,
                async () =>
                {
                    var task = context.ExecuteAsync.Invoke(null, new object?[] { transport, arguments, false }) as Task<int>
                        ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                    exitCode = await task.ConfigureAwait(false);
                },
                responseFactory)
            .ConfigureAwait(false);

        return (exitCode, requests);
    }

    private static JsonElement AssertSsctlCommandRequest(
        JsonElement request,
        string commandName,
        params (string Key, object? Value)[] expectedPayload)
    {
        AssertAutomationCommandId(request, commandName);
        var payload = request.GetProperty("payload");
        if (expectedPayload.Length == 0)
        {
            return payload;
        }

        AssertJsonObjectPropertyNames(payload, expectedPayload.Select(item => item.Key).ToArray());
        foreach (var (key, value) in expectedPayload)
        {
            AssertJsonPropertyEquals(payload, key, value, $"{commandName}.{key}");
        }

        return payload;
    }

    private static void AssertSsctlCommandRequestHasEmptyPayload(JsonElement request, string commandName)
    {
        var payload = AssertSsctlCommandRequest(request, commandName);
        if (payload.ValueKind == JsonValueKind.Object && payload.EnumerateObject().Any())
        {
            throw new InvalidOperationException($"{commandName} payload contained unexpected properties.");
        }

        if (payload.ValueKind is not JsonValueKind.Null and not JsonValueKind.Object)
        {
            throw new InvalidOperationException($"{commandName} payload had unexpected kind {payload.ValueKind}.");
        }
    }

    private static void AssertSsctlCommandRoutingTestsUseCommandIdHelper()
    {
        var repoRoot = GetRepoRoot();
        var testRoot = System.IO.Path.Combine(repoRoot, "tests", "Sussudio.Tests");
        foreach (var file in System.IO.Directory.GetFiles(testRoot, "CommandHandlers.Routing*.Tests.cs"))
        {
            var relativePath = System.IO.Path.GetRelativePath(repoRoot, file).Replace('\\', '/');
            var text = System.IO.File.ReadAllText(file).Replace("\r\n", "\n");
            var directCommandReadToken = "GetProperty(\"command\")" + ".GetInt32()";
            if (text.Contains(directCommandReadToken, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"{relativePath} must use AssertSsctlCommandRequest for captured request.command checks.");
            }

            var commandValueBypassToken = "GetExpectedAutomationCommand" + "Value(";
            if (text.Contains(commandValueBypassToken, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"{relativePath} must not bypass AssertSsctlCommandRequest.");
            }
        }
    }

    private static string ReadSsctlCommandHandlersFamilyText()
    {
        var files = new[]
        {
            "tools/ssctl/CommandHandlers.cs",
        };

        return string.Join(
            "\n",
            files.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
    }
}
