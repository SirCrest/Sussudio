using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewD3DRenderPipelineOwnershipTests
{
    public PresentationPreviewD3DRenderPipelineOwnershipTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task RenderPassesLiveInFocusedPartial()
        => global::Program.D3D11PreviewRenderer_RenderPassesLiveInFocusedPartial();

    [Fact]
    public Task ShaderRenderingLivesInFocusedPartial()
        => global::Program.D3D11PreviewRenderer_ShaderRenderingLivesInFocusedPartial();

    [Fact]
    public Task ShaderCompilationLivesInFocusedFiles()
        => global::Program.D3D11PreviewRenderer_ShaderCompilationLivesInFocusedFiles();

    [Fact]
    public Task FrameLatencyLivesInFocusedPartial()
        => global::Program.D3D11PreviewRenderer_FrameLatencyLivesInFocusedPartial();

    [Fact]
    public Task RenderThreadLivesInFocusedPartial()
        => global::Program.D3D11PreviewRenderer_RenderThreadLivesInFocusedPartial();

    [Fact]
    public Task PresentAccountingLivesWithRenderPasses()
        => global::Program.D3D11PreviewRenderer_PresentAccountingLivesWithRenderPasses();

    [Fact]
    public Task ViewportHelpersLiveInFocusedPartial()
        => global::Program.D3D11PreviewRenderer_ViewportHelpersLiveInFocusedPartial();

    [Fact]
    public Task ScreenshotEncodingLivesWithScreenshotCapture()
        => global::Program.D3D11PreviewRenderer_ScreenshotEncodingLivesWithScreenshotCapture();
}
