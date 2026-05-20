using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewCaptureSelectionPolicyContractsTests
{
    public PresentationPreviewCaptureSelectionPolicyContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task ModeSelectionStateLivesInFocusedPartial()
        => global::Program.ModeSelectionState_LivesInFocusedPartial();

    [Fact]
    public Task CaptureFormatSelectionPolicyLivesInFocusedHelper()
        => global::Program.CaptureFormatSelectionPolicy_LivesInFocusedHelper();

    [Fact]
    public Task CaptureFormatSelectionPolicyPreservesSelectionBehavior()
        => global::Program.CaptureFormatSelectionPolicy_PreservesSelectionBehavior();

    [Fact]
    public Task RecordingSettingsSelectionPolicyLivesInFocusedHelper()
        => global::Program.RecordingSettingsSelectionPolicy_LivesInFocusedHelper();
}
