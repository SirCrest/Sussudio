using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class AutomationSnapshotFormatterContractsTests
{
    [Fact]
    public Task FormatsCoreSectionsAndTypedAccessors()
        => global::Program.AutomationSnapshotFormatter_FormatsCoreSectionsAndTypedAccessors();

    [Fact]
    public Task RendersFlashbackSectionsWhenIncluded()
        => global::Program.AutomationSnapshotFormatter_RendersFlashbackSections_WhenIncluded();

    [Fact]
    public Task RendersPreviewD3DSections()
        => global::Program.AutomationSnapshotFormatter_RendersPreviewD3DSections();

    [Fact]
    public Task SourceOwnershipIsSplit()
        => global::Program.AutomationSnapshotFormatter_SourceOwnership_IsSplit();
}

public sealed class SsctlFormatterContractsTests
{
    [Fact]
    public Task EmitsCoreSnapshotSections()
        => global::Program.SsctlFormatters_EmitCoreSnapshotSections();

    [Fact]
    public Task SnapshotSourceOwnershipIsSplit()
        => global::Program.SsctlFormatters_SnapshotSourceOwnership_IsSplit();

    [Fact]
    public Task TimelineOutputPreservesTableAndSummary()
        => global::Program.SsctlFormatters_TimelineOutputPreservesTableAndSummary();
}

public sealed class ToolProbeContractsTests
{
    [Fact]
    public Task PresentMonParserSelectsDominantNonArtifactSwapChain()
        => global::Program.PresentMonParser_SelectsDominantNonArtifactSwapChain();

    [Fact]
    public Task PresentMonProbeSourceOwnershipIsSplit()
        => global::Program.PresentMonProbe_SourceOwnership_IsSplit();

    [Fact]
    public Task SsctlPipeTransportExposesAdvancedAutomationCommandIds()
        => global::Program.SsctlPipeTransport_ExposesAdvancedAutomationCommandIds();

    [Fact]
    public Task KsAudioNodeProbeSourceOwnershipIsSplit()
        => global::Program.KsAudioNodeProbe_SourceOwnership_IsSplit();

    [Fact]
    public Task EgavdsAudioProbeSourceOwnershipIsSplit()
        => global::Program.EgavdsAudioProbe_SourceOwnership_IsSplit();
}

public sealed class ToolModelContractsTests
{
    [Fact]
    public async Task NvmlSnapshotComputedPropertiesConvertUnits()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
        await global::Program.NvmlSnapshot_ComputedProperties_ConvertUnits();
    }

    [Fact]
    public async Task CaptureSessionSnapshotDefaultState()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
        await global::Program.CaptureSessionSnapshot_DefaultState();
    }
}

public sealed class NativeToolProbeContractsTests
{
    [Fact]
    public Task RtkI2cProbeGuardsUnsafeNativePaths()
        => global::Program.RtkI2cProbe_GuardsUnsafeNativePaths();
}
