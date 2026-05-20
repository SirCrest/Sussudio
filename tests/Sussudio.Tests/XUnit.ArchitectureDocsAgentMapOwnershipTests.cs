using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class ArchitectureDocsAgentMapOwnershipTests
{
    [Fact]
    public Task AgentMapFileReferencesResolve()
        => global::Program.ArchitectureAgentMap_FileReferencesResolve();

    [Fact]
    public Task AgentMapTestOwnerPathsUseCodeSpansAndResolve()
        => global::Program.ArchitectureAgentMap_TestOwnerPathsUseCodeSpansAndResolve();

    [Fact]
    public Task AgentMapCoversArchitectureDocsTestFamily()
        => global::Program.ArchitectureAgentMap_CoversArchitectureDocsTestFamily();

    [Fact]
    public Task AgentMapHasUniqueToolsCommonOwnershipEntries()
        => global::Program.ArchitectureAgentMap_ToolsCommonOwnershipEntriesAreUnique();

    [Fact]
    public Task TestProjectDoesNotKeepEmptyPartialMarkerShells()
        => global::Program.TestProject_DoesNotKeepEmptyPartialMarkerShells();

    [Fact]
    public Task AgentMapCoversAutomationConsumerChecklist()
        => global::Program.ArchitectureAgentMap_CoversAutomationConsumerChecklist();

    [Fact]
    public Task AgentMapCoversUiPresentationOwnershipFiles()
        => global::Program.ArchitectureAgentMap_CoversUiPresentationOwnershipFiles();

    [Fact]
    public Task AgentMapCoversCaptureRuntimeOwnershipFiles()
        => global::Program.ArchitectureAgentMap_CoversCaptureRuntimeOwnershipFiles();

    [Fact]
    public Task AgentMapMapsFlashbackPreviewStartupToResourceOwner()
        => global::Program.ArchitectureAgentMap_MapsFlashbackPreviewStartupToResourceOwner();

    [Fact]
    public Task AgentMapCoversToolAutomationPartialFamiliesWithExactPaths()
        => global::Program.ArchitectureAgentMap_CoversToolAutomationPartialFamiliesWithExactPaths();
}
