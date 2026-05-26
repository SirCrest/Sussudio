using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Tools;

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

    internal static Task SsctlHelp_UsesCatalogCliHelpForAutomationCommands()
    {
        var ssctlProgramText = ReadRepoFile("tools/ssctl/Program.cs")
            .Replace("\r\n", "\n");
        var helpWriterText = ReadRepoFile("tools/ssctl/SsctlHelpWriter.cs")
            .Replace("\r\n", "\n");
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
        AssertDoesNotContain(ssctlProgramText, "AutomationCommandCatalog.Get(kind).CliHelp");
        AssertDoesNotContain(ssctlProgramText, "WriteCatalogHelpLine");
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
}
