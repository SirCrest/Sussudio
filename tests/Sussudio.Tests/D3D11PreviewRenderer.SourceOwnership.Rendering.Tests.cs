using System.Threading.Tasks;

static partial class Program
{
    private static Task D3D11PreviewRenderer_FrameUploadLivesInFocusedPartial()
    {
        var renderingText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Rendering.cs")
            .Replace("\r\n", "\n");
        var frameUploadText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.FrameUpload.cs")
            .Replace("\r\n", "\n");

        AssertContains(frameUploadText, "private bool TryResolveInputView(PendingFrame frame, out ID3D11VideoProcessorInputView? inputView, out bool disposeInputView)");
        AssertContains(frameUploadText, "private ID3D11VideoProcessorInputView CreateInputViewFromTexture(ID3D11Texture2D texture, int subresourceIndex)");
        AssertContains(frameUploadText, "private unsafe bool UploadRawFrameToTexture(");
        AssertContains(frameUploadText, "private unsafe bool TryUpdateRawFrameTexture(");
        AssertContains(frameUploadText, "private unsafe bool UploadRawFrameViaStaging(");
        AssertContains(frameUploadText, "_deviceContext.UpdateSubresource(");
        AssertContains(frameUploadText, "_deviceContext.CopyResource(inputTexture, stagingTexture);");
        AssertDoesNotContain(renderingText, "private bool TryResolveInputView(PendingFrame frame, out ID3D11VideoProcessorInputView? inputView, out bool disposeInputView)");
        AssertDoesNotContain(renderingText, "private unsafe bool UploadRawFrameViaStaging(");

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_ShaderRenderingLivesInFocusedPartial()
    {
        var renderingText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Rendering.cs")
            .Replace("\r\n", "\n");
        var shaderRenderingText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderRendering.cs")
            .Replace("\r\n", "\n");

        AssertContains(shaderRenderingText, "private void RenderNv12WithShader(PendingFrame frame)");
        AssertContains(shaderRenderingText, "private void RenderHdrFrameWithShader(PendingFrame frame, ID3D11PixelShader pixelShader)");
        AssertContains(shaderRenderingText, "private bool TryEnsureNv12ShaderResources(PendingFrame frame)");
        AssertContains(shaderRenderingText, "private static readonly ID3D11ClassInstance[] EmptyClassInstances");
        AssertContains(shaderRenderingText, "PreviewShaderSources.RendererModeNv12");
        AssertContains(shaderRenderingText, "RendererModeHdrPassthrough");
        AssertDoesNotContain(renderingText, "private void RenderNv12WithShader(PendingFrame frame)");
        AssertDoesNotContain(renderingText, "private void RenderHdrFrameWithShader(PendingFrame frame, ID3D11PixelShader pixelShader)");
        AssertDoesNotContain(renderingText, "private bool TryEnsureNv12ShaderResources(PendingFrame frame)");

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_ShaderSourcesLiveInFocusedFile()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var renderingText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Rendering.cs")
            .Replace("\r\n", "\n");
        var shaderSourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderSources.cs")
            .Replace("\r\n", "\n");
        var previewShaderSourcesText = ReadRepoFile("Sussudio/Services/Preview/PreviewShaderSources.cs")
            .Replace("\r\n", "\n");

        AssertContains(previewShaderSourcesText, "internal static class PreviewShaderSources");
        AssertContains(previewShaderSourcesText, "internal const string FullscreenVertex");
        AssertContains(previewShaderSourcesText, "internal const string HdrTonemapPixel");
        AssertContains(previewShaderSourcesText, "internal const string HdrPassthroughPixel");
        AssertContains(previewShaderSourcesText, "internal const string Nv12Pixel");
        AssertContains(previewShaderSourcesText, "static const float PQ_m1");
        AssertContains(previewShaderSourcesText, "Texture2D<float> yPlane : register(t0);");
        AssertContains(previewShaderSourcesText, "BT2020_to_BT709");

        AssertContains(shaderSourcesText, "private unsafe void CompileTonemapShaders()");
        AssertContains(shaderSourcesText, "private static byte[] CompileShader(string hlslSource, string entryPoint, string profile)");
        AssertContains(shaderSourcesText, "PreviewShaderSources.FullscreenVertex");
        AssertContains(shaderSourcesText, "PreviewShaderSources.HdrTonemapPixel");
        AssertContains(shaderSourcesText, "PreviewShaderSources.HdrPassthroughPixel");
        AssertContains(shaderSourcesText, "PreviewShaderSources.Nv12Pixel");
        AssertContains(shaderSourcesText, "D3DCompileNative(");

        AssertDoesNotContain(rootText, "internal const string FullscreenVertex");
        AssertDoesNotContain(rootText, "static const float PQ_m1");
        AssertDoesNotContain(renderingText, "internal const string HdrTonemapPixel");
        AssertDoesNotContain(renderingText, "BT2020_to_BT709");
        AssertDoesNotContain(shaderSourcesText, "static const float PQ_m1");
        AssertDoesNotContain(shaderSourcesText, "Texture2D<float> yPlane : register(t0);");

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_ViewportHelpersLiveInFocusedPartial()
    {
        var renderingText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Rendering.cs")
            .Replace("\r\n", "\n");
        var viewportText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Viewport.cs")
            .Replace("\r\n", "\n");

        AssertContains(viewportText, "private Viewport ComputeLetterboxViewport(int sourceWidth, int sourceHeight)");
        AssertContains(viewportText, "private void UpdateViewportConstantBuffer(Viewport viewport)");
        AssertContains(viewportText, "private static Vortice.RawRect ComputeLetterboxRect(");
        AssertContains(viewportText, "MapMode.WriteDiscard");
        AssertDoesNotContain(renderingText, "private Viewport ComputeLetterboxViewport(");
        AssertDoesNotContain(renderingText, "private static Vortice.RawRect ComputeLetterboxRect(");

        return Task.CompletedTask;
    }
}
