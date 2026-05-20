using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewStartupBehaviorContractsTests
{
    public PresentationPreviewStartupBehaviorContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task PreviewStartupWatchdogControllerPreservesTimeoutContracts()
        => global::Program.PreviewStartupWatchdogController_PreservesTimeoutContracts();

    [Fact]
    public Task PreviewStartupWatchdogControllerGatesFailureStopScheduling()
        => global::Program.PreviewStartupWatchdogController_GatesFailureStopScheduling();

    [Fact]
    public Task PreviewStartupSessionControllerPreservesAttemptStateContracts()
        => global::Program.PreviewStartupSessionController_PreservesAttemptStateContracts();

    [Fact]
    public Task PreviewReinitTransitionControllerPreservesTransitionStateContracts()
        => global::Program.PreviewReinitTransitionController_PreservesTransitionStateContracts();

    [Fact]
    public Task PreviewReinitializationWaitsForPendingFlashbackCycle()
        => global::Program.PreviewReinitialization_WaitsForPendingFlashbackCycle();
}
