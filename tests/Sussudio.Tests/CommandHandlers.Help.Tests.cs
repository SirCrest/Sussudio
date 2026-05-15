using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Tools;

static partial class Program
{
    private static Task SsctlHelp_UsesCatalogCliHelpForAutomationCommands()
    {
        var ssctlProgramText = ReadRepoFile("tools/ssctl/Program.cs")
            .Replace("\r\n", "\n");
        var catalogText = ReadRepoFile("Sussudio.Automation.Contracts/AutomationCommandCatalog.cs")
            .Replace("\r\n", "\n");
        var flashbackHandlersText = ReadRepoFile("tools/ssctl/CommandHandlers.Flashback.cs")
            .Replace("\r\n", "\n");

        AssertContains(catalogText, "\"flashback export [seconds] [path] [--range] [--force]\"");
        AssertContains(flashbackHandlersText, "ConsumeFlag(context.Rest, \"--force\")");
        AssertContains(ssctlProgramText, "AutomationCommandCatalog.Get(kind).CliHelp");
        AssertContains(ssctlProgramText, "WriteCatalogHelpLine(AutomationCommandKind.FlashbackExport);");
        AssertContains(ssctlProgramText, "WriteCatalogHelpLine(AutomationCommandKind.FlashbackGetSegments);");
        AssertContains(ssctlProgramText, "WriteCatalogHelpLine(AutomationCommandKind.SetFrameTimeOverlayVisible);");
        AssertContains(ssctlProgramText, "WriteCatalogHelpLine(AutomationCommandKind.SetFlashbackTimelineVisible);");

        AssertEqual("flashback export [seconds] [path] [--range] [--force]",
            AutomationCommandCatalog.Get(AutomationCommandKind.FlashbackExport).CliHelp,
            "catalog Flashback export CLI help");
        AssertEqual("flashback segments",
            AutomationCommandCatalog.Get(AutomationCommandKind.FlashbackGetSegments).CliHelp,
            "catalog Flashback segments CLI help");

        return Task.CompletedTask;
    }
}
