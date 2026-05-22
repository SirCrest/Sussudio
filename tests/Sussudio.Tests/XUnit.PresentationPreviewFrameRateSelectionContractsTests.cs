using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewFrameRateSelectionContractsTests
{
    public PresentationPreviewFrameRateSelectionContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task SourceFilteredFrameRatesAreAlwaysUnlocked()
        => global::Program.SourceFilteredFrameRatesAreAlwaysUnlocked();

    [Fact]
    public Task FrameRateSourceFilterPolicyLivesInFocusedHelper()
        => global::Program.FrameRateSourceFilterPolicy_LivesInFocusedHelper();

    [Fact]
    public Task FrameRateAutoSelectionPolicyLivesInFocusedHelper()
        => global::Program.FrameRateAutoSelectionPolicy_LivesInFocusedHelper();

    [Fact]
    public Task FrameRateAutoSelectionPolicyPreservesSelectionBehavior()
        => global::Program.FrameRateAutoSelectionPolicy_PreservesSelectionBehavior();

    [Fact]
    public Task FrameRateTimingPolicyLivesInFocusedPartial()
        => global::Program.FrameRateTimingPolicy_LivesInFocusedPartial();

    [Fact]
    public Task FrameRateTimingPolicyPreservesPureTimingBehavior()
        => global::Program.FrameRateTimingPolicy_PreservesPureTimingBehavior();
}
