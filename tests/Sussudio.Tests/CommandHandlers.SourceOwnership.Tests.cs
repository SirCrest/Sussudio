using System.Threading.Tasks;

static partial class Program
{
    private static Task SsctlCommandHandlers_SourceOwnership_IsSplit()
    {
        var commandHandlersSource = ReadSsctlCommandHandlersFamilyText();
        var commandHandlersRootSource = ReadRepoFile("tools/ssctl/CommandHandlers.cs");
        AssertDoesNotContain(commandHandlersRootSource, "private static Task<int> HandleFlashbackAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Observability.cs"), "HandleAudioRampTraceAsync");
        AssertDoesNotContain(ReadRepoFile("tools/ssctl/CommandHandlers.Observability.cs"), "HandleDiagnosticSessionAsync");
        AssertDoesNotContain(ReadRepoFile("tools/ssctl/CommandHandlers.Observability.cs"), "HandlePresentMonAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.DiagnosticSession.cs"), "HandleDiagnosticSessionAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.PresentMon.cs"), "HandlePresentMonAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.PresentMon.cs"), "TryResolvePreviewSwapChainAddressAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.CaptureControls.cs"), "HandleSetAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Device.cs"), "HandleDeviceAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Device.cs"), "\"RefreshDevices\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Device.cs"), "\"GetCaptureOptions\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Device.cs"), "\"SelectDevice\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Device.cs"), "\"SelectAudioInputDevice\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Device.cs"), "\"SetCustomAudioInput\"");
        AssertDoesNotContain(ReadRepoFile("tools/ssctl/CommandHandlers.Device.cs"), "HandleWindowAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Window.cs"), "HandleWindowAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Window.cs"), "\"ArmClose\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Window.cs"), "\"WindowAction\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Window.cs"), "\"SetFullScreenEnabled\"");
        AssertDoesNotContain(ReadRepoFile("tools/ssctl/CommandHandlers.Window.cs"), "HandleDeviceAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Recordings.cs"), "HandleRecordingsAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Recordings.cs"), "\"OpenRecordingsFolder\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "HandleWaitAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "\"WaitForCondition\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "Math.Max(timeoutMs.GetValueOrDefault(0) + 5000, 60000)");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "HandleAssertAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "JsonDocument.Parse(assertionsJson)");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "\"AssertSnapshot\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "HandleProbeAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "\"ProbeVideoSource\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "\"ProbePreviewColor\"");
        AssertDoesNotContain(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "HandleStatsAsync");
        AssertDoesNotContain(ReadRepoFile("tools/ssctl/CommandHandlers.AutomationFlow.cs"), "HandleVerifyAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.UiVisibility.cs"), "HandleStatsAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.UiVisibility.cs"), "HandleSettingsAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.UiVisibility.cs"), "HandleFrameTimeAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.UiVisibility.cs"), "\"SetStatsVisible\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.UiVisibility.cs"), "\"SetStatsSectionVisible\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.UiVisibility.cs"), "\"SetSettingsVisible\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.UiVisibility.cs"), "\"SetFrameTimeOverlayVisible\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Verification.cs"), "HandleVerifyAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Verification.cs"), "\"VerifyFile\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Verification.cs"), "\"VerifyLastRecording\"");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Verification.cs"), "ConsumeFlag(context.Rest, \"--json\")");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Verification.cs"), "ParseOptionalStringFlag(context.Rest, \"--verification-profile\")");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Flashback.cs"), "HandleFlashbackAsync");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Flashback.cs"), "return HandleFlashbackExportAsync(context);");
        AssertDoesNotContain(ReadRepoFile("tools/ssctl/CommandHandlers.Flashback.cs"), "ParseFlashbackExportSeconds(context.Rest[1])");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Flashback.Export.cs"), "private static Task<int> HandleFlashbackExportAsync(CommandContext context)");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Flashback.Export.cs"), "ConsumeFlag(context.Rest, \"--range\")");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Flashback.Export.cs"), "ConsumeFlag(context.Rest, \"--force\")");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Flashback.Export.cs"), "? ParseFlashbackExportSeconds(context.Rest[1])");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Flashback.Export.cs"), "Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? \".\")");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Flags.cs"), "private static bool ConsumeFlag(List<string> args, string flag)");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Arguments.cs"), "private static string JoinRemaining(IReadOnlyList<string> args, int startIndex)");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Json.cs"), "private static bool LooksLikeJson(string value)");
        AssertContains(ReadRepoFile("tools/ssctl/CommandHandlers.Values.cs"), "private static object? ParseAssertionValue(string value)");
        AssertContains(commandHandlersSource, "\"manifest\" => HandleManifestAsync(context)");
        AssertContains(commandHandlersSource, "\"audio-ramp-trace\" => HandleAudioRampTraceAsync(context)");
        AssertContains(commandHandlersSource, "\"recordings\" => HandleRecordingsAsync(context)");
        AssertContains(commandHandlersSource, "context.Transport.SendCommandAsync(\"GetAutomationManifest\")");
        AssertContains(commandHandlersSource, "context.Transport.SendCommandAsync(\"GetAudioRampTrace\")");
        AssertContains(commandHandlersSource, "\"RefreshDevices\",");
        AssertContains(commandHandlersSource, "\"SetFullScreenEnabled\",");
        AssertContains(commandHandlersSource, "\"OpenRecordingsFolder\"");
        AssertContains(commandHandlersSource, "playPayload[\"positionMs\"] = ParseFlashbackPositionMs(context.Rest[1]);");
        AssertContains(commandHandlersSource, "return HandleSimpleCommandAsync(context, \"FlashbackAction\", playPayload, includeData: true);");
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
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "ssctl", "CommandHandlers.DeviceWindow.cs")),
            "old ssctl device/window grab-bag removed");

        return Task.CompletedTask;
    }
}
