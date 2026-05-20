using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewLaunchStartupContractsTests
{
    public PresentationPreviewLaunchStartupContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task SplashLoadingPhrasesLiveInController()
        => global::Program.SplashLoadingPhrases_LiveInController();

    [Fact]
    public Task SplashLoadingPhrasePacingPolicyPreservesIntervalBands()
        => global::Program.SplashLoadingPhrasePacingPolicy_PreservesIntervalBands();

    [Fact]
    public Task LaunchEntranceAnimationLivesInController()
        => global::Program.LaunchEntranceAnimation_LivesInController();

    [Fact]
    public Task MainWindowStartupHostingLivesInStartupPartial()
        => global::Program.MainWindowStartupHosting_LivesInStartupPartial();
}
