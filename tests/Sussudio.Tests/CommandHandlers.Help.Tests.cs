using System.Threading.Tasks;

static partial class Program
{
    private static Task SsctlHelp_FlashbackExportIncludesForceFlag()
    {
        var ssctlProgramText = ReadRepoFile("tools/ssctl/Program.cs")
            .Replace("\r\n", "\n");
        var catalogText = ReadRepoFile("Sussudio.Automation.Contracts/AutomationCommandCatalog.cs")
            .Replace("\r\n", "\n");
        var flashbackHandlersText = ReadRepoFile("tools/ssctl/CommandHandlers.Flashback.cs")
            .Replace("\r\n", "\n");

        AssertContains(catalogText, "\"flashback export [seconds] [path] [--range] [--force]\"");
        AssertContains(flashbackHandlersText, "ConsumeFlag(context.Rest, \"--force\")");
        AssertContains(ssctlProgramText, "flashback export [seconds] [path] [--range] [--force]");

        return Task.CompletedTask;
    }
}
