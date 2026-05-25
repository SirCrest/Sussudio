using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task D3D11PreviewRenderer_FrameLatencyLivesWithRenderThread()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var renderThreadText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderThread.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.FrameLatency.cs")),
            "D3D11 waitable frame-latency pacing lives with render-thread execution");
        AssertContains(renderThreadText, "private IntPtr _frameLatencyWaitHandle;");
        AssertContains(renderThreadText, "private void ConfigureFrameLatencyWaitableObject()");
        AssertContains(renderThreadText, "private void WaitForFrameLatencySignal()");
        AssertContains(renderThreadText, "TrackFrameLatencyWait(result, Stopwatch.GetTimestamp() - waitStart);");
        AssertContains(renderThreadText, "private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);");
        AssertDoesNotContain(rootText, "private IntPtr _frameLatencyWaitHandle;");
        AssertDoesNotContain(resourcesText, "private void WaitForFrameLatencySignal()");
        AssertDoesNotContain(renderPassesText, "private static extern uint WaitForSingleObject");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_ViewportHelpersLiveWithRenderPasses()
    {
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Viewport.cs")),
            "D3D11 preview viewport helpers live with render-pass execution");
        AssertContains(renderPassesText, "private Viewport ComputeLetterboxViewport(int sourceWidth, int sourceHeight)");
        AssertContains(renderPassesText, "private void UpdateViewportConstantBuffer(Viewport viewport)");
        AssertContains(renderPassesText, "private static Vortice.RawRect ComputeLetterboxRect(");
        AssertContains(renderPassesText, "MapMode.WriteDiscard");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_RenderPassesLiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var renderThreadText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderThread.cs")
            .Replace("\r\n", "\n");
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var shaderRenderingText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderRendering.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Nv12ShaderPass.cs")),
            "NV12 shader pass folded into render-pass owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.HdrShaderPass.cs")),
            "HDR shader pass folded into render-pass owner");
        AssertContains(renderPassesText, "private bool _loggedHdrShaderFallback;");
        AssertContains(renderPassesText, "private void RenderFrame(PendingFrame frame)");
        AssertContains(renderPassesText, "private void ApplySwapChainColorSpaceIfDirty()");
        AssertContains(renderPassesText, "private void RenderFrameWithVideoProcessor(PendingFrame frame)");
        AssertContains(renderPassesText, "private void RenderNv12WithShader(PendingFrame frame)");
        AssertContains(renderPassesText, "private void RenderHdrFrameWithShader(PendingFrame frame, ID3D11PixelShader pixelShader)");
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
        AssertContains(renderPassesText, "TryEnsureNv12ShaderResources(frame)");
        AssertContains(renderPassesText, "EnsureHdrInputResources(frame.Width, frame.Height)");
        AssertContains(renderPassesText, "TryResolveInputView(frame, out var inputView, out var disposeInputView)");
        AssertContains(renderPassesText, "D3D11_PREVIEW_HDR_SHADER_FALLBACK");
        AssertContains(renderThreadText, "private bool TryEnterNativeRenderCall()");
        AssertContains(renderThreadText, "private void ExitNativeRenderCall()");
        AssertContains(renderThreadText, "Interlocked.Exchange(ref _inNativeCall, 1);");
        AssertContains(renderThreadText, "Interlocked.Exchange(ref _inNativeCall, 0);");
        AssertDoesNotContain(rootText, "private bool TryEnterNativeRenderCall()");
        AssertDoesNotContain(rootText, "private void ExitNativeRenderCall()");
        AssertContains(renderThreadText, "ProcessRenderThreadFrameOrIdle()");
        AssertContains(renderThreadText, "RenderFrame(frame);");
        AssertDoesNotContain(rootText, "private void RenderFrame(PendingFrame frame)");
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
        AssertDoesNotContain(rootText, "private ID3D11VertexShader? _fullscreenVS;");
        AssertDoesNotContain(rootText, "private readonly VideoProcessorStream[] _vpStreamArray = new VideoProcessorStream[1];");
        AssertDoesNotContain(renderPassesText, "private bool TryEnsureNv12ShaderResources(PendingFrame frame)");
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
        var shaderRenderingText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderRendering.cs")
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

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.ShaderCompilation.cs")),
            "shader compilation folded into shader rendering owner");
        AssertContains(shaderRenderingText, "private unsafe void CompileTonemapShaders()");
        AssertContains(shaderRenderingText, "PreviewShaderSources.FullscreenVertex");
        AssertContains(shaderRenderingText, "PreviewShaderSources.HdrTonemapPixel");
        AssertContains(shaderRenderingText, "PreviewShaderSources.HdrPassthroughPixel");
        AssertContains(shaderRenderingText, "PreviewShaderSources.Nv12Pixel");
        AssertContains(shaderRenderingText, "private interface ID3DBlob");
        AssertContains(shaderRenderingText, "private static extern int D3DCompileNative(");
        AssertContains(shaderRenderingText, "private static byte[] CompileShader(string hlslSource, string entryPoint, string profile)");
        AssertContains(shaderRenderingText, "private static byte[] ReadBlobBytes(IntPtr blobPtr)");
        AssertContains(shaderRenderingText, "private static string ReadBlobString(IntPtr blobPtr)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.NativeInterop.cs")),
            "shader compiler interop folded into shader rendering owner");

        AssertDoesNotContain(rootText, "internal const string FullscreenVertex");
        AssertDoesNotContain(rootText, "static const float PQ_m1");
        AssertDoesNotContain(renderPassesText, "internal const string HdrTonemapPixel");
        AssertDoesNotContain(renderPassesText, "BT2020_to_BT709");
        AssertDoesNotContain(shaderRenderingText, "static const float PQ_m1");
        AssertDoesNotContain(shaderRenderingText, "Texture2D<float> yPlane : register(t0);");

        return Task.CompletedTask;
    }
}
