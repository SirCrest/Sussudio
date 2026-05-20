using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewVisualShellContractsTests
{
    public PresentationPreviewVisualShellContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task ControlBarHoverAnimationsLiveInController()
        => global::Program.ControlBarHoverAnimations_LiveInController();

    [Fact]
    public Task ShellElevationSetupLivesInController()
        => global::Program.ShellElevationSetup_LivesInController();

    [Fact]
    public Task PreviewTransitionAnimationsLiveInController()
        => global::Program.PreviewTransitionAnimations_LiveInController();

    [Fact]
    public Task PreviewStartupOverlayLivesInController()
        => global::Program.PreviewStartupOverlay_LivesInController();

    [Fact]
    public Task PreviewFadeInRevealLivesInController()
        => global::Program.PreviewFadeInReveal_LivesInController();
}
