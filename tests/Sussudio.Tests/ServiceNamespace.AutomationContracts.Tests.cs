using System.Threading.Tasks;

// Tests that prevent app service code from drifting into stale namespaces.
static partial class Program
{
    internal static Task AutomationContracts_SourceOwnership_IsModelAligned()
    {
        var repoRoot = GetRepoRoot();
        var automationContractsProject = Path.Combine(repoRoot, "Sussudio.Automation.Contracts", "Sussudio.Automation.Contracts.csproj");
        AssertEqual(true, File.Exists(automationContractsProject), "Automation contracts project exists");

        foreach (var contractFile in new[]
        {
            "AutomationCommandKind.cs",
            "AutomationCommandCatalog.cs",
            "AutomationCommandCatalog.Entries.cs",
            "AutomationCommandCatalog.Manifest.cs",
            "AutomationCommandCatalog.PathValidation.cs",
            "AutomationPipeProtocol.cs",
            "AutomationPipeClientModels.cs",
            "AutomationPipeSecurityPolicy.cs"
        })
        {
            var contractPath = Path.Combine(repoRoot, "Sussudio.Automation.Contracts", contractFile);
            AssertEqual(true, File.Exists(contractPath), $"{contractFile} contract source exists");
            var expectedNamespace = string.Equals(contractFile, "AutomationCommandKind.cs", StringComparison.Ordinal)
                ? "namespace Sussudio.Models;"
                : "namespace Sussudio.Tools;";
            AssertContains(File.ReadAllText(contractPath), expectedNamespace);
            AssertEqual(
                false,
                File.Exists(Path.Combine(repoRoot, "tools", "Common", contractFile)),
                $"tools/Common must not own {contractFile}");
            AssertEqual(
                false,
                File.Exists(Path.Combine(repoRoot, "tools", "Common", "AutomationPipeClient", contractFile)),
                $"tools/Common/AutomationPipeClient must not own {contractFile}");
            AssertEqual(
                false,
                File.Exists(Path.Combine(repoRoot, "Sussudio", "Models", "Automation", contractFile)),
                $"app project must not own {contractFile}");
        }

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
            0,
            CountCompileInclude(appIncludes, @"..\tools\Common\AutomationPipeProtocol.cs"),
            "app project must not link AutomationPipeProtocol from tools/Common");
        AssertEqual(
            0,
            CountCompileInclude(appIncludes, @"..\tools\Common\AutomationResponseState.cs"),
            "app project must not link AutomationResponseState from tools/Common");
        AssertEqual(
            0,
            CountCompileInclude(appIncludes, @"..\tools\Common\AutomationPipeSecurityPolicy.cs"),
            "app project must not link AutomationPipeSecurityPolicy from tools/Common");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "tools", "Common", "AutomationPipeClient", "AutomationPipeClient.Models.cs")),
            "tools/Common/AutomationPipeClient must not own AutomationPipeClient.Models.cs");
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
                0,
                CountCompileInclude(includes, @"..\Common\AutomationCommandKind.cs"),
                $"{Path.GetFileName(toolProject)} must not link AutomationCommandKind from tools/Common");
            AssertEqual(
                0,
                CountCompileInclude(includes, @"..\Common\AutomationCommandCatalog.cs"),
                $"{Path.GetFileName(toolProject)} must not link AutomationCommandCatalog from tools/Common");
            AssertEqual(
                0,
                CountCompileInclude(includes, @"..\Common\AutomationPipeProtocol.cs"),
                $"{Path.GetFileName(toolProject)} must not link AutomationPipeProtocol from tools/Common");
            AssertEqual(
                0,
                CountCompileInclude(includes, @"..\Common\AutomationResponseState.cs"),
                $"{Path.GetFileName(toolProject)} must not link AutomationResponseState from tools/Common");
            AssertEqual(
                0,
                CountCompileInclude(includes, @"..\Common\AutomationPipeSecurityPolicy.cs"),
                $"{Path.GetFileName(toolProject)} must not link AutomationPipeSecurityPolicy from tools/Common");
            AssertEqual(
                1,
                CountProjectReference(references, @"..\..\Sussudio.Automation.Contracts\Sussudio.Automation.Contracts.csproj"),
                $"{Path.GetFileName(toolProject)} references automation contracts exactly once");
        }

        return Task.CompletedTask;
    }
}
