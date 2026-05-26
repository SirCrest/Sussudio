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

public sealed class SsctlCommandHandlerContractsTests
{
    [Fact]
    public Task RoutesDeviceCommands()
        => global::Program.SsctlCommandHandlers_RouteDeviceCommands();

    [Fact]
    public Task RoutesCaptureControlCommands()
        => global::Program.SsctlCommandHandlers_RouteCaptureControlCommands();

    [Fact]
    public Task RoutesRecordingsCommands()
        => global::Program.SsctlCommandHandlers_RouteRecordingsCommands();

    [Fact]
    public Task RoutesFlashbackCommands()
        => global::Program.SsctlCommandHandlers_RouteFlashbackCommands();

    [Fact]
    public Task RoutesWindowCommands()
        => global::Program.SsctlCommandHandlers_RouteWindowCommands();

    [Fact]
    public Task RoutesManifestCommand()
        => global::Program.SsctlCommandHandlers_RouteManifestCommand();

    [Fact]
    public Task RoutesObservabilityCommands()
        => global::Program.SsctlCommandHandlers_RouteObservabilityCommands();

    [Fact]
    public Task RoutesAutomationFlowCommands()
        => global::Program.SsctlCommandHandlers_RouteAutomationFlowCommands();

    [Fact]
    public Task RoutesUiVisibilityCommands()
        => global::Program.SsctlCommandHandlers_RouteUiVisibilityCommands();

    [Fact]
    public Task RoutesVerificationCommands()
        => global::Program.SsctlCommandHandlers_RouteVerificationCommands();

    [Fact]
    public Task SourceOwnershipIsConsolidated()
        => global::Program.SsctlCommandHandlers_SourceOwnership_IsConsolidated();

    [Fact]
    public Task HelpUsesCatalogCliHelpForAutomationCommands()
        => global::Program.SsctlHelp_UsesCatalogCliHelpForAutomationCommands();
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
    public Task KsAudioNodeProbeSourceOwnershipIsConsolidated()
        => global::Program.KsAudioNodeProbe_SourceOwnership_IsConsolidated();

    [Fact]
    public Task EgavdsAudioProbeSourceOwnershipIsConsolidated()
        => global::Program.EgavdsAudioProbe_SourceOwnership_IsConsolidated();
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
    public async Task NvmlMonitorNativeInteropLivesWithMonitorOwner()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
        await global::Program.NvmlMonitor_NativeInteropLivesWithMonitorOwner();
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
