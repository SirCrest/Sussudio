// Tests that keep app-internal service contracts separate from automation wire contracts.
static partial class Program
{
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
}
