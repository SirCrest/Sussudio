using System.Threading.Tasks;

static partial class Program
{
    internal static Task D3D11PreviewRenderer_FrameLatencyLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var frameLatencyText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.FrameLatency.cs")
            .Replace("\r\n", "\n");

        AssertContains(frameLatencyText, "private IntPtr _frameLatencyWaitHandle;");
        AssertContains(frameLatencyText, "private void ConfigureFrameLatencyWaitableObject()");
        AssertContains(frameLatencyText, "private void WaitForFrameLatencySignal()");
        AssertContains(frameLatencyText, "TrackFrameLatencyWait(result, Stopwatch.GetTimestamp() - waitStart);");
        AssertContains(frameLatencyText, "private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);");
        AssertDoesNotContain(rootText, "private IntPtr _frameLatencyWaitHandle;");
        AssertDoesNotContain(resourcesText, "private void WaitForFrameLatencySignal()");
        AssertDoesNotContain(renderPassesText, "private static extern uint WaitForSingleObject");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_ViewportHelpersLiveInFocusedPartial()
    {
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var viewportText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Viewport.cs")
            .Replace("\r\n", "\n");

        AssertContains(viewportText, "private Viewport ComputeLetterboxViewport(int sourceWidth, int sourceHeight)");
        AssertContains(viewportText, "private void UpdateViewportConstantBuffer(Viewport viewport)");
        AssertContains(viewportText, "private static Vortice.RawRect ComputeLetterboxRect(");
        AssertContains(viewportText, "MapMode.WriteDiscard");
        AssertDoesNotContain(renderPassesText, "private Viewport ComputeLetterboxViewport(");
        AssertDoesNotContain(renderPassesText, "private static Vortice.RawRect ComputeLetterboxRect(");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_RenderPassesLiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var renderThreadText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderThread.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Lifecycle.cs")
            .Replace("\r\n", "\n");
        var stopLifecycleText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.StopLifecycle.cs")
            .Replace("\r\n", "\n");
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var nv12ShaderPassText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Nv12ShaderPass.cs")
            .Replace("\r\n", "\n");
        var hdrShaderPassText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.HdrShaderPass.cs")
            .Replace("\r\n", "\n");
        var shaderRenderingText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderRendering.cs")
            .Replace("\r\n", "\n");

        AssertContains(renderPassesText, "private bool _loggedHdrShaderFallback;");
        AssertContains(renderPassesText, "private void RenderFrame(PendingFrame frame)");
        AssertContains(renderPassesText, "private void ApplySwapChainColorSpaceIfDirty()");
        AssertContains(renderPassesText, "private void RenderFrameWithVideoProcessor(PendingFrame frame)");
        AssertContains(renderPassesText, "RenderNv12WithShader(frame);");
        AssertContains(renderPassesText, "RenderHdrFrameWithShader(frame, _hdrPassthroughPS!);");
        AssertContains(renderPassesText, "RenderHdrFrameWithShader(frame, _hdrTonemapPS);");
        AssertContains(renderPassesText, "RenderFrameWithVideoProcessor(frame);");
        AssertContains(renderPassesText, "Volatile.Write(ref _rendererMode, PreviewShaderSources.RendererModeNv12);");
        AssertContains(renderPassesText, "Volatile.Write(ref _rendererMode, RendererModeHdrPassthrough);");
        AssertContains(renderPassesText, "Volatile.Write(ref _rendererMode, PreviewShaderSources.RendererModeHdr);");
        AssertContains(renderPassesText, "Volatile.Write(ref _rendererMode, RendererModeVideoProcessor);");
        AssertContains(renderPassesText, "if (!TryEnterNativeRenderCall())");
        AssertContains(renderPassesText, "ExitNativeRenderCall();");
        AssertContains(renderPassesText, "PresentAndTrackFrame(");
        AssertContains(nv12ShaderPassText, "private void RenderNv12WithShader(PendingFrame frame)");
        AssertContains(nv12ShaderPassText, "if (!TryEnterNativeRenderCall())");
        AssertContains(nv12ShaderPassText, "ExitNativeRenderCall();");
        AssertContains(nv12ShaderPassText, "PresentAndTrackFrame(");
        AssertContains(nv12ShaderPassText, "TryEnsureNv12ShaderResources(frame)");
        AssertContains(nv12ShaderPassText, "PreviewShaderSources.RendererModeNv12");
        AssertContains(hdrShaderPassText, "private void RenderHdrFrameWithShader(PendingFrame frame, ID3D11PixelShader pixelShader)");
        AssertContains(hdrShaderPassText, "if (!TryEnterNativeRenderCall())");
        AssertContains(hdrShaderPassText, "ExitNativeRenderCall();");
        AssertContains(hdrShaderPassText, "PresentAndTrackFrame(");
        AssertContains(hdrShaderPassText, "EnsureHdrInputResources(frame.Width, frame.Height)");
        AssertContains(hdrShaderPassText, "RendererModeHdrPassthrough");
        AssertContains(renderPassesText, "TryResolveInputView(frame, out var inputView, out var disposeInputView)");
        AssertContains(renderPassesText, "D3D11_PREVIEW_HDR_SHADER_FALLBACK");
        AssertContains(stopLifecycleText, "private bool TryEnterNativeRenderCall()");
        AssertContains(stopLifecycleText, "private void ExitNativeRenderCall()");
        AssertContains(stopLifecycleText, "Interlocked.Exchange(ref _inNativeCall, 1);");
        AssertContains(stopLifecycleText, "Interlocked.Exchange(ref _inNativeCall, 0);");
        AssertDoesNotContain(lifecycleText, "private bool TryEnterNativeRenderCall()");
        AssertDoesNotContain(lifecycleText, "private void ExitNativeRenderCall()");
        AssertContains(renderThreadText, "RenderFrame(frame);");
        AssertDoesNotContain(rootText, "private void RenderFrame(PendingFrame frame)");
        AssertDoesNotContain(renderPassesText, "private void RenderNv12WithShader(PendingFrame frame)");
        AssertDoesNotContain(renderPassesText, "private void RenderHdrFrameWithShader(PendingFrame frame, ID3D11PixelShader pixelShader)");
        AssertDoesNotContain(shaderRenderingText, "private void RenderNv12WithShader(PendingFrame frame)");
        AssertDoesNotContain(shaderRenderingText, "private void RenderHdrFrameWithShader(PendingFrame frame, ID3D11PixelShader pixelShader)");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_ShaderRenderingLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var nv12ShaderPassText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Nv12ShaderPass.cs")
            .Replace("\r\n", "\n");
        var hdrShaderPassText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.HdrShaderPass.cs")
            .Replace("\r\n", "\n");
        var shaderRenderingText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderRendering.cs")
            .Replace("\r\n", "\n");
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");

        AssertContains(shaderRenderingText, "private ID3D11VertexShader? _fullscreenVS;");
        AssertContains(shaderRenderingText, "private ID3D11PixelShader? _nv12PS;");
        AssertContains(shaderRenderingText, "private ID3D11PixelShader? _hdrPassthroughPS;");
        AssertContains(shaderRenderingText, "private readonly VideoProcessorStream[] _vpStreamArray = new VideoProcessorStream[1];");
        AssertContains(shaderRenderingText, "private bool TryEnsureNv12ShaderResources(PendingFrame frame)");
        AssertContains(shaderRenderingText, "private void DisposeNv12ShaderResourceViews()");
        AssertContains(shaderRenderingText, "private void DisposeShaderPipelineResources()");
        AssertContains(shaderRenderingText, "private static readonly ID3D11ClassInstance[] EmptyClassInstances");
        AssertContains(renderPassesText, "PreviewShaderSources.RendererModeNv12");
        AssertContains(renderPassesText, "RendererModeHdrPassthrough");
        AssertContains(renderPassesText, "private bool _loggedHdrShaderFallback;");
        AssertContains(nv12ShaderPassText, "PreviewShaderSources.RendererModeNv12");
        AssertContains(hdrShaderPassText, "RendererModeHdrPassthrough");
        AssertDoesNotContain(rootText, "private ID3D11VertexShader? _fullscreenVS;");
        AssertDoesNotContain(rootText, "private readonly VideoProcessorStream[] _vpStreamArray = new VideoProcessorStream[1];");
        AssertDoesNotContain(renderPassesText, "private bool TryEnsureNv12ShaderResources(PendingFrame frame)");
        AssertDoesNotContain(renderPassesText, "private void RenderNv12WithShader(PendingFrame frame)");
        AssertDoesNotContain(shaderRenderingText, "private bool _loggedHdrShaderFallback;");
        AssertDoesNotContain(shaderRenderingText, "private int _lastNv12IsHdr = -1;");
        AssertDoesNotContain(resourcesText, "_linearSampler?.Dispose();");
        AssertDoesNotContain(resourcesText, "_nv12PS?.Dispose();");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_ShaderCompilationLivesInFocusedFiles()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var shaderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Nv12ShaderPass.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.HdrShaderPass.cs")
                .Replace("\r\n", "\n");
        var shaderCompilationText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderCompilation.cs")
            .Replace("\r\n", "\n");
        var shaderBlobInteropText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderBlobInterop.cs")
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

        AssertContains(shaderCompilationText, "private unsafe void CompileTonemapShaders()");
        AssertContains(shaderCompilationText, "PreviewShaderSources.FullscreenVertex");
        AssertContains(shaderCompilationText, "PreviewShaderSources.HdrTonemapPixel");
        AssertContains(shaderCompilationText, "PreviewShaderSources.HdrPassthroughPixel");
        AssertContains(shaderCompilationText, "PreviewShaderSources.Nv12Pixel");
        AssertContains(shaderBlobInteropText, "private static byte[] CompileShader(string hlslSource, string entryPoint, string profile)");
        AssertContains(shaderBlobInteropText, "private static byte[] ReadBlobBytes(IntPtr blobPtr)");
        AssertContains(shaderBlobInteropText, "private static string ReadBlobString(IntPtr blobPtr)");
        AssertContains(shaderBlobInteropText, "D3DCompileNative(");

        AssertDoesNotContain(rootText, "internal const string FullscreenVertex");
        AssertDoesNotContain(rootText, "static const float PQ_m1");
        AssertDoesNotContain(renderPassesText, "internal const string HdrTonemapPixel");
        AssertDoesNotContain(shaderPassesText, "internal const string HdrTonemapPixel");
        AssertDoesNotContain(renderPassesText, "BT2020_to_BT709");
        AssertDoesNotContain(shaderPassesText, "BT2020_to_BT709");
        AssertDoesNotContain(shaderCompilationText, "D3DCompileNative(");
        AssertDoesNotContain(shaderCompilationText, "static const float PQ_m1");
        AssertDoesNotContain(shaderCompilationText, "Texture2D<float> yPlane : register(t0);");
        AssertDoesNotContain(shaderBlobInteropText, "PreviewShaderSources.FullscreenVertex");

        return Task.CompletedTask;
    }
}
