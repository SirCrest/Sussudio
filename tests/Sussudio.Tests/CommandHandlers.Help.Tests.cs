using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Tools;

static partial class Program
{
    internal static Task SsctlHelp_UsesCatalogCliHelpForAutomationCommands()
    {
        var ssctlProgramText = ReadRepoFile("tools/ssctl/Program.cs")
            .Replace("\r\n", "\n");
        var helpWriterText = ReadRepoFile("tools/ssctl/SsctlHelpWriter.cs")
            .Replace("\r\n", "\n");
        var catalogEntriesText = string.Join(
            "\n",
            ReadRepoFile("Sussudio.Automation.Contracts/AutomationCommandCatalog.Entries.cs").Replace("\r\n", "\n"));
        var flashbackHandlersText = string.Join(
            "\n",
            ReadRepoFile("tools/ssctl/CommandHandlers.Flashback.cs").Replace("\r\n", "\n"));
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
