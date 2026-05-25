using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

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
