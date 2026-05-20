using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewCaptureSelectionContractsTests
{
    public PresentationPreviewCaptureSelectionContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task CaptureSelectionBindingSyncLivesInController()
        => global::Program.CaptureSelectionBindingSync_LivesInController();

    [Fact]
    public Task CaptureSelectionBindingPropertyRouterLivesInController()
        => global::Program.CaptureSelectionBindingPropertyRouter_LivesInController();

    [Fact]
    public Task CaptureSelectionBindingCollectionSyncLivesInControllerPartial()
        => global::Program.CaptureSelectionBindingCollectionSync_LivesInControllerPartial();

    [Fact]
    public Task CaptureSelectionBindingSelectionOwnersLiveInFocusedPartials()
        => global::Program.CaptureSelectionBindingSelectionOwners_LiveInFocusedPartials();

    [Fact]
    public Task CaptureSelectionBindingDeviceAudioProjectionLivesInFocusedPartial()
        => global::Program.CaptureSelectionBindingDeviceAudioProjection_LivesInFocusedPartial();

    [Fact]
    public Task CaptureComboBoxSelectionNormalizerPreservesSelectionFallbacks()
        => global::Program.CaptureComboBoxSelectionNormalizer_PreservesSelectionFallbacks();
}
