using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewRecordingContractsTests
{
    public PresentationPreviewRecordingContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task RecordingButtonChromeLivesInController()
        => global::Program.RecordingButtonChrome_LivesInController();

    [Fact]
    public Task RecordingStatePresentationLivesInController()
        => global::Program.RecordingStatePresentation_LivesInController();

    [Fact]
    public Task RecordingStatePresentationPolicyPreservesLockoutRules()
        => global::Program.RecordingStatePresentationPolicy_PreservesLockoutRules();

    [Fact]
    public Task RecordingButtonActionLivesInController()
        => global::Program.RecordingButtonAction_LivesInController();
}
