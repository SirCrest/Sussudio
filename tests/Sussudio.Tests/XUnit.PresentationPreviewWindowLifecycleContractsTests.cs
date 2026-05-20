using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewWindowLifecycleContractsTests
{
    public PresentationPreviewWindowLifecycleContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task MainWindowNativeBootstrapLivesInFocusedController()
        => global::Program.MainWindowNativeBootstrap_LivesInFocusedController();

    [Fact]
    public Task MainWindowCloseLifecycleAndShutdownCleanupAreSplit()
        => global::Program.MainWindowCloseLifecycleAndShutdownCleanup_AreSplit();

    [Fact]
    public Task MainWindowCloseLifecycleControllersOwnCloseRequestAndAppClosing()
        => global::Program.MainWindowCloseLifecycleControllers_OwnCloseRequestAndAppClosing();

    [Fact]
    public Task MainWindowCloseRecordingFinalizationOwnsRecordingStopPolicy()
        => global::Program.MainWindowCloseRecordingFinalization_OwnsRecordingStopPolicy();

    [Fact]
    public Task MainWindowShutdownCleanupOwnsPostCloseCleanupOrder()
        => global::Program.MainWindowShutdownCleanup_OwnsPostCloseCleanupOrder();
}
