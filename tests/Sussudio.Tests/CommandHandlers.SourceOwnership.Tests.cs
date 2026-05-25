using System.Threading.Tasks;

static partial class Program
{
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
}
