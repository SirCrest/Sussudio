using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewD3DRenderSetupOwnershipTests
{
    public PresentationPreviewD3DRenderSetupOwnershipTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task PanelBindingLivesInFocusedPartial()
        => global::Program.D3D11PreviewRenderer_PanelBindingLivesInFocusedPartial();

    [Fact]
    public Task SharedDeviceLivesInFocusedPartial()
        => global::Program.D3D11PreviewRenderer_SharedDeviceLivesInFocusedPartial();

    [Fact]
    public Task FrameUploadLivesInFocusedPartial()
        => global::Program.D3D11PreviewRenderer_FrameUploadLivesInFocusedPartial();

    [Fact]
    public Task InputResourcesLiveWithD3DResources()
        => global::Program.D3D11PreviewRenderer_InputResourcesLiveWithD3DResources();

    [Fact]
    public Task DeviceInitializationOwnsSwapChainSetup()
        => global::Program.D3D11PreviewRenderer_DeviceInitializationOwnsSwapChainSetup();
}
