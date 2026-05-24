using System.Threading.Tasks;

static partial class Program
{
    internal static Task SsctlCommandHandlers_SourceOwnership_IsSplit()
    {
        AssertSsctlCommandRoutingTestsUseCommandIdHelper();
        var commandHandlersSource = ReadSsctlCommandHandlersFamilyText();
        var commandHandlersRootSource = ReadRepoFile("tools/ssctl/CommandHandlers.cs");
        AssertContains(commandHandlersRootSource, "private sealed class CommandContext");
        AssertContains(commandHandlersRootSource, "Rest = arguments.Skip(1).ToList();");
        AssertContains(commandHandlersRootSource, "private static async Task<int> HandleSimpleCommandAsync(");
        AssertContains(commandHandlersRootSource, "context.Transport.SendCommandAsync(kind, payload)");
        AssertContains(commandHandlersRootSource, "private static int WriteResponse(JsonElement response, bool json, Func<JsonElement, string> formatter)");
        AssertDoesNotContain(commandHandlersRootSource, "private static Task<int> HandleFlashbackAsync");
        var observabilitySource = ReadRepoFile("tools/ssctl/CommandHandlers.Observability.cs");
        AssertContains(observabilitySource, "HandleAudioRampTraceAsync");
        AssertContains(observabilitySource, "HandleDiagnosticSessionAsync");
        AssertContains(observabilitySource, "HandlePresentMonAsync");
        AssertContains(observabilitySource, "TryResolvePreviewPresentCorrelationAsync");
        AssertContains(observabilitySource, "PresentMonProbe.CreateOptions(");
        AssertContains(observabilitySource, "DiagnosticSessionRunner.RunAsync(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "ssctl", "CommandHandlers.DiagnosticSession.cs")),
            "diagnostic-session command routing lives with ssctl observability commands");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "ssctl", "CommandHandlers.PresentMon.cs")),
            "presentmon command routing lives with ssctl observability commands");
        var captureControlsSource = ReadRepoFile("tools/ssctl/CommandHandlers.CaptureControls.cs");
        AssertContains(captureControlsSource, "HandleSetAsync");
        AssertSsctlCapturePipelineRoutingUsesAutomationCommandKinds();
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "ssctl", "CommandHandlers.Device.cs")), "ssctl device commands stay folded into CaptureControls.cs");
        AssertContains(captureControlsSource, "HandleDeviceAsync");
        AssertContains(captureControlsSource, "AutomationCommandKind.RefreshDevices");
        AssertContains(captureControlsSource, "AutomationCommandKind.GetCaptureOptions");
        AssertContains(captureControlsSource, "AutomationCommandKind.SelectDevice");
        AssertContains(captureControlsSource, "AutomationCommandKind.SelectAudioInputDevice");
        AssertContains(captureControlsSource, "AutomationCommandKind.SetCustomAudioInput");
        AssertDoesNotContain(captureControlsSource, "HandleWindowAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Window.cs"), "HandleWindowAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Window.cs"), "AutomationCommandKind.ArmClose");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Window.cs"), "AutomationCommandKind.WindowAction");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Window.cs"), "AutomationCommandKind.SetFullScreenEnabled");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Window.cs"), "HandleRecordingsAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Window.cs"), "AutomationCommandKind.OpenRecordingsFolder");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Window.cs"), "HandleStatsAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Window.cs"), "HandleSettingsAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Window.cs"), "HandleFrameTimeAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Window.cs"), "AutomationCommandKind.SetStatsVisible");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Window.cs"), "AutomationCommandKind.SetStatsSectionVisible");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Window.cs"), "AutomationCommandKind.SetSettingsVisible");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Window.cs"), "AutomationCommandKind.SetFrameTimeOverlayVisible");
        AssertDoesNotContain(ReadRepoFile("tools/ssctl/CommandHandlers.Window.cs"), "HandleDeviceAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "HandleWaitAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "AutomationCommandKind.WaitForCondition");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "Math.Max(timeoutMs.GetValueOrDefault(0) + 5000, 60000)");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "HandleAssertAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "JsonDocument.Parse(assertionsJson)");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "AutomationCommandKind.AssertSnapshot");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "HandleProbeAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "AutomationCommandKind.ProbeVideoSource");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "AutomationCommandKind.ProbePreviewColor");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "HandleVerifyAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "AutomationCommandKind.VerifyFile");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "AutomationCommandKind.VerifyLastRecording");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "ConsumeFlag(context.Rest, \"--json\")");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "ParseOptionalStringFlag(context.Rest, \"--verification-profile\")");
        AssertDoesNotContain(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "HandleStatsAsync");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "ssctl", "CommandHandlers.UiVisibility.cs")),
            "stats/settings/frame-time visibility commands live with the ssctl window and shell command owner");
        var flashbackRouterSource = ReadRepoFile("tools/ssctl/CommandHandlers.Flashback.cs");
        AssertContains(flashbackRouterSource, "HandleFlashbackAsync");
        AssertContains(flashbackRouterSource, "return HandleFlashbackActionAsync(context, subcommand);");
        AssertContains(flashbackRouterSource, "return HandleFlashbackExportAsync(context);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "ssctl", "CommandHandlers.Flashback.Actions.cs")),
            "Flashback action routing stays folded into the Flashback command owner");
        AssertContains(flashbackRouterSource, "private static Task<int> HandleFlashbackActionAsync(CommandContext context, string subcommand)");
        AssertContains(flashbackRouterSource, "AutomationCommandKind.FlashbackAction");
        AssertContains(flashbackRouterSource, "playPayload[\"positionMs\"] = ParseFlashbackPositionMs(context.Rest[1]);");
        AssertContains(flashbackRouterSource, "[\"action\"] = \"begin-scrub\"");
        AssertContains(flashbackRouterSource, "[\"action\"] = \"clear-in-out-points\"");
        AssertContains(flashbackRouterSource, "private static double ParseFlashbackPositionMs(string value)");
        AssertContains(flashbackRouterSource, "Flashback position must be finite, non-negative, and within TimeSpan range.");
        AssertDoesNotContain(ReadRepoFile("tools/ssctl/CommandHandlers.Values.cs"), "private static double ParseFlashbackPositionMs(string value)");
        AssertContains(flashbackRouterSource, "private static Task<int> HandleFlashbackExportAsync(CommandContext context)");
        AssertContains(flashbackRouterSource, "ConsumeFlag(context.Rest, \"--range\")");
        AssertContains(flashbackRouterSource, "ConsumeFlag(context.Rest, \"--force\")");
        AssertContains(flashbackRouterSource, "? ParseFlashbackExportSeconds(context.Rest[1])");
        AssertContains(flashbackRouterSource, "Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? \".\")");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Arguments.cs"), "private static string JoinRemaining(IReadOnlyList<string> args, int startIndex)");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Arguments.cs"), "private static bool ConsumeFlag(List<string> args, string flag)");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Arguments.cs"), "private static bool LooksLikeJson(string value)");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Arguments.cs"), "private static string PrettyJson<T>(T value)");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Values.cs"), "private static object? ParseAssertionValue(string value)");
        AssertContains(commandHandlersSource, "\"manifest\" => HandleManifestAsync(context)");
        AssertContains(commandHandlersSource, "\"audio-ramp-trace\" => HandleAudioRampTraceAsync(context)");
        AssertContains(commandHandlersSource, "\"recordings\" => HandleRecordingsAsync(context)");
        AssertSsctlFixedAutomationRoutesUseAutomationCommandKinds(commandHandlersSource);
        AssertContains(commandHandlersSource, "playPayload[\"positionMs\"] = ParseFlashbackPositionMs(context.Rest[1]);");
        AssertContains(commandHandlersSource, "return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackAction, playPayload, includeData: true);");
        AssertContains(commandHandlersSource, "ParseOptionalStringFlag(context.Rest, \"--profile\")");
        AssertContains(commandHandlersSource, "payload[\"verificationProfile\"] = verificationProfile;");
        AssertContains(commandHandlersSource, "[\"positionMs\"] = ParseFlashbackPositionMs(RequireWord(context.Rest, 1, \"flashback seek <ms>\"))");
        AssertContains(commandHandlersSource, "[\"action\"] = \"begin-scrub\"");
        AssertContains(commandHandlersSource, "[\"positionMs\"] = ParseFlashbackPositionMs(RequireWord(context.Rest, 1, \"flashback begin-scrub <ms>\"))");
        AssertContains(commandHandlersSource, "[\"action\"] = \"update-scrub\"");
        AssertContains(commandHandlersSource, "[\"positionMs\"] = ParseFlashbackPositionMs(RequireWord(context.Rest, 1, \"flashback update-scrub <ms>\"))");
        AssertContains(commandHandlersSource, "var payload = new Dictionary<string, object?> { [\"action\"] = \"end-scrub\" };");
        AssertContains(commandHandlersSource, "payload[\"positionMs\"] = ParseFlashbackPositionMs(context.Rest[1]);");
        AssertContains(commandHandlersSource, "? ParseFlashbackExportSeconds(context.Rest[1])");
        AssertContains(commandHandlersSource, "private static double ParseFlashbackPositionMs(string value)");
        AssertContains(commandHandlersSource, "Flashback position must be finite, non-negative, and within TimeSpan range.");
        AssertContains(commandHandlersSource, "private static double ParseFlashbackExportSeconds(string value)");
        AssertContains(commandHandlersSource, "Flashback export seconds must be finite, greater than zero, and within TimeSpan range.");
        AssertContains(commandHandlersSource, "assert <json> OR assert <field> <op> <value>");
        AssertContains(commandHandlersSource, "private static object? ParseAssertionValue(string value)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "ssctl", "CommandHandlers" + ".Parsing" + ".cs")),
            "old ssctl parsing grab-bag removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "ssctl", "CommandHandlers.Flags.cs")),
            "ssctl flag helpers live with command argument parsing");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "ssctl", "CommandHandlers.Json.cs")),
            "ssctl JSON helpers live with command argument parsing");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "ssctl", "CommandHandlers.DeviceWindow.cs")),
            "old ssctl device/window grab-bag removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "ssctl", "CommandHandlers.Context.cs")),
            "ssctl command context lives with the root command dispatcher");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "ssctl", "CommandHandlers.Flashback.Export.cs")),
            "Flashback export routing lives with the Flashback command router");

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
        var captureControlsSource = ReadRepoFile("tools/ssctl/CommandHandlers.CaptureControls.cs");

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
        AssertDoesNotContain(rootSource, "private static Task<int> SendSetValueAsync(");
        AssertContains(captureControlsSource, "HandleSimpleCommandAsync(\n            context,\n            kind,");
        AssertContains(rootSource, "context.Transport.SendCommandAsync(kind, payload)");
    }
}
