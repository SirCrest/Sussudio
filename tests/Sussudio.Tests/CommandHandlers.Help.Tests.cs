using System.IO;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Tools;

static partial class Program
{
    private static Task SsctlHelp_UsesCatalogCliHelpForAutomationCommands()
    {
        var ssctlProgramText = ReadRepoFile("tools/ssctl/Program.cs")
            .Replace("\r\n", "\n");
        var helpWriterText = ReadRepoFile("tools/ssctl/SsctlHelpWriter.cs")
            .Replace("\r\n", "\n");
        var catalogText = ReadRepoFile("Sussudio.Automation.Contracts/AutomationCommandCatalog.cs")
            .Replace("\r\n", "\n");
        var flashbackHandlersText = ReadRepoFile("tools/ssctl/CommandHandlers.Flashback.cs")
            .Replace("\r\n", "\n");
        var ssctlAssembly = LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        var helpWriterType = ssctlAssembly.GetType("Sussudio.Tools.Ssctl.SsctlHelpWriter")
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.SsctlHelpWriter type not found.");
        var writeHelp = RequireNonPublicStaticMethod(helpWriterType, "Write");
        using var writer = new StringWriter();
        writeHelp.Invoke(null, new object[] { writer });
        var helpOutput = writer.ToString().Replace("\r\n", "\n");

        AssertContains(catalogText, "\"flashback export [seconds] [path] [--range] [--force]\"");
        AssertContains(flashbackHandlersText, "ConsumeFlag(context.Rest, \"--force\")");
        AssertContains(ssctlProgramText, "SsctlHelpWriter.Write(Console.Out);");
        AssertDoesNotContain(ssctlProgramText, "AutomationCommandCatalog.Get(kind).CliHelp");
        AssertDoesNotContain(ssctlProgramText, "WriteCatalogHelpLine");
        AssertContains(helpWriterText, "AutomationCommandCatalog.Get(kind).CliHelp");
        AssertContains(helpWriterText, "WriteCatalogHelpLine(writer, AutomationCommandKind.FlashbackExport);");
        AssertContains(helpWriterText, "WriteCatalogHelpLine(writer, AutomationCommandKind.FlashbackGetSegments);");
        AssertContains(helpWriterText, "WriteCatalogHelpLine(writer, AutomationCommandKind.SetFrameTimeOverlayVisible);");
        AssertContains(helpWriterText, "WriteCatalogHelpLine(writer, AutomationCommandKind.SetFlashbackTimelineVisible);");
        AssertContains(helpOutput, "ssctl");
        AssertContains(helpOutput, "Usage:");
        AssertContains(helpOutput, "Flashback:");
        AssertContains(helpOutput, "Flags:");
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
}
