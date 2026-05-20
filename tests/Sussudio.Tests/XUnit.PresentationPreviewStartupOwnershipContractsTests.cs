using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewStartupOwnershipContractsTests
{
    public PresentationPreviewStartupOwnershipContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task PreviewStartupSessionAndReinitOwnershipLivesInFocusedControllers()
        => global::Program.PreviewStartupSessionReinitOwnership_LivesInFocusedControllers();

    [Fact]
    public Task PreviewStartupWatchdogOwnershipLivesInFocusedController()
        => global::Program.PreviewStartupWatchdogOwnership_LivesInFocusedController();

    [Fact]
    public Task PreviewStartupSignalOwnershipLivesInFocusedControllers()
        => global::Program.PreviewStartupSignalsOwnership_LivesInFocusedControllers();

    [Fact]
    public Task PreviewStartupLifecycleEventOwnershipLivesInFocusedController()
        => global::Program.PreviewStartupLifecycleEventOwnership_LivesInFocusedController();
}
