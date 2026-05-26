using System.Threading.Tasks;

// Tests that prevent app service code from drifting into stale namespaces.
static partial class Program
{
    internal static Task ServiceNamespaces_FollowServiceFolders()
    {
        var repoRoot = GetRepoRoot();
        AssertServiceNamespaceFolderRules(repoRoot);
        AssertServiceNamespaceNativeXuProbeOwnership(repoRoot);
        AssertServiceNamespaceSourceOwnership(repoRoot);
        AssertServiceContractsBoundaryOwnership(repoRoot);

        return Task.CompletedTask;
    }

    private static void AssertServiceNamespaceFolderRules(string repoRoot)
    {
        var servicesRoot = Path.Combine(GetRepoRoot(), "Sussudio", "Services");
        var rootFiles = EnumerateSourceFiles(servicesRoot, SearchOption.TopDirectoryOnly).ToArray();
        AssertEqual(0, rootFiles.Length, "Services root C# file count");

        foreach (var file in EnumerateSourceFiles(servicesRoot, SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(servicesRoot, file);
            var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (parts.Length < 2)
            {
                throw new InvalidOperationException($"Service file must live in a domain folder: {relative}");
            }

            var expectedNamespace = $"namespace Sussudio.Services.{parts[0]};";
            var code = StripCSharpCommentsAndLiterals(File.ReadAllText(file));
            if (!code.Contains(expectedNamespace, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"{relative} must declare {expectedNamespace}");
            }

            AssertDoesNotContain(code, "namespace Sussudio.Services;");
        }

        foreach (var file in EnumerateSourceFiles(Path.Combine(repoRoot, "Sussudio"), SearchOption.AllDirectories))
        {
            var code = StripCSharpCommentsAndLiterals(File.ReadAllText(file));
            if (RootServicesUsingRegex.IsMatch(code))
            {
                throw new InvalidOperationException($"{Path.GetRelativePath(repoRoot, file)} imports the flat Services namespace.");
            }
        }
    }

    private static void AssertServiceContractsBoundaryOwnership(string repoRoot)
    {
        var serviceContractFiles = new[]
        {
            "Sussudio/Services/Contracts/ServiceInterfaces.cs",
            "Sussudio/Services/Contracts/ISourceSignalTelemetryProvider.cs",
            "Sussudio/Services/Contracts/RecordingContracts.cs",
            "Sussudio/Services/Contracts/PooledVideoFrame.cs"
        };

        foreach (var relativePath in serviceContractFiles)
        {
            var source = ReadRepoFile(relativePath);
            AssertContains(source, "namespace Sussudio.Services.Contracts;");
            AssertDoesNotContain(source, "namespace Sussudio.Tools;");
            AssertDoesNotContain(source, "Sussudio.Automation.Contracts");
        }

        var automationContractsProject = Path.Combine(
            repoRoot,
            "Sussudio.Automation.Contracts",
            "Sussudio.Automation.Contracts.csproj");
        var automationReferences = ReadProjectReferences(automationContractsProject);
        AssertEqual(
            0,
            CountProjectReference(automationReferences, @"..\Sussudio\Sussudio.csproj"),
            "automation contracts must not reference the app project");

        var automationContractSources = Directory
            .EnumerateFiles(Path.Combine(repoRoot, "Sussudio.Automation.Contracts"), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(File.ReadAllText)
            .Select(StripCSharpCommentsAndLiterals);
        foreach (var source in automationContractSources)
        {
            AssertDoesNotContain(source, "Sussudio.Services.Contracts");
            AssertDoesNotContain(source, "Sussudio/Services/Contracts");
            AssertDoesNotContain(source, @"Sussudio\Services\Contracts");
        }

        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        foreach (var relativePath in serviceContractFiles)
        {
            AssertContains(agentMapText, "`" + relativePath + "`");
        }

        var pooledVideoFrameText = ReadRepoFile("Sussudio/Services/Contracts/PooledVideoFrame.cs");
        AssertContains(pooledVideoFrameText, "internal sealed class PooledVideoFrameLease : IDisposable");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Contracts", "PooledVideoFrameLease.cs")),
            "pooled-frame leases live with the pooled frame owner");

        var serviceInterfacesText = ReadRepoFile("Sussudio/Services/Contracts/ServiceInterfaces.cs");
        AssertContains(serviceInterfacesText, "public interface IAutomationWindowControl");
        AssertContains(serviceInterfacesText, "internal interface IPreviewFrameSink");
        var sourceTelemetryProviderText = ReadRepoFile("Sussudio/Services/Contracts/ISourceSignalTelemetryProvider.cs");
        AssertContains(sourceTelemetryProviderText, "public interface ISourceSignalTelemetryProvider");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Contracts", "AutomationInterfaces.cs")),
            "automation service interfaces live with ServiceInterfaces");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Contracts", "IPreviewFrameSink.cs")),
            "preview sink service interface lives with ServiceInterfaces");

        AssertContains(agentMapText, "separate from `Sussudio.Automation.Contracts` wire/protocol contracts");
    }

    internal static Task AutomationContracts_SourceOwnership_IsModelAligned()
    {
        var repoRoot = GetRepoRoot();
        var automationContractsProject = Path.Combine(repoRoot, "Sussudio.Automation.Contracts", "Sussudio.Automation.Contracts.csproj");
        AssertEqual(true, File.Exists(automationContractsProject), "Automation contracts project exists");

        foreach (var contractFile in new[]
        {
            "AutomationCommandKind.cs",
            "AutomationCommandCatalog.cs",
            "AutomationPipeProtocol.cs",
            "AutomationPipeClientModels.cs"
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
        var protocolText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio.Automation.Contracts", "AutomationPipeProtocol.cs"));
        AssertContains(protocolText, "public static class AutomationPipeSecurityPolicy");
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
