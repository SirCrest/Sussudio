using System.Threading.Tasks;

// Tests that prevent app service code from drifting into stale namespaces.
static partial class Program
{
    private static Task AutomationCommandKind_SourceOwnership_IsModelAligned()
    {
        var repoRoot = GetRepoRoot();
        var automationContractsProject = Path.Combine(repoRoot, "Sussudio.Automation.Contracts", "Sussudio.Automation.Contracts.csproj");
        var automationKindPath = Path.Combine(repoRoot, "Sussudio.Automation.Contracts", "AutomationCommandKind.cs");
        AssertEqual(true, File.Exists(automationContractsProject), "Automation contracts project exists");
        AssertEqual(true, File.Exists(automationKindPath), "AutomationCommandKind contract source exists");
        AssertContains(File.ReadAllText(automationKindPath), "namespace Sussudio.Models;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Models", "Automation", "AutomationCommandKind.cs")),
            "app project no longer owns AutomationCommandKind");

        var appIncludes = ReadCompileIncludes(Path.Combine(repoRoot, "Sussudio", "Sussudio.csproj"));
        var appReferences = ReadProjectReferences(Path.Combine(repoRoot, "Sussudio", "Sussudio.csproj"));
        AssertEqual(
            0,
            CountCompileInclude(appIncludes, @"..\tools\Common\AutomationCommandKind.cs"),
            "app project must not link AutomationCommandKind from tools/Common");
        AssertEqual(
            0,
            CountCompileInclude(appIncludes, @"..\tools\Common\AutomationCommandCatalog.cs"),
            "app project must not link AutomationCommandCatalog from tools/Common");
        AssertEqual(
            1,
            CountProjectReference(appReferences, @"..\Sussudio.Automation.Contracts\Sussudio.Automation.Contracts.csproj"),
            "app project references automation contracts exactly once");

        foreach (var toolProject in new[]
        {
            Path.Combine(repoRoot, "tools", "AutomationClient", "AutomationClient.csproj"),
            Path.Combine(repoRoot, "tools", "ssctl", "ssctl.csproj"),
            Path.Combine(repoRoot, "tools", "McpServer", "McpServer.csproj")
        })
        {
            var includes = ReadCompileIncludes(toolProject);
            var references = ReadProjectReferences(toolProject);
            AssertEqual(
                0,
                CountCompileInclude(includes, @"..\..\Sussudio\Models\Automation\AutomationCommandKind.cs"),
                $"{Path.GetFileName(toolProject)} must not link app-owned AutomationCommandKind source");
            AssertEqual(
                1,
                CountProjectReference(references, @"..\..\Sussudio.Automation.Contracts\Sussudio.Automation.Contracts.csproj"),
                $"{Path.GetFileName(toolProject)} references automation contracts exactly once");
        }

        return Task.CompletedTask;
    }
}
