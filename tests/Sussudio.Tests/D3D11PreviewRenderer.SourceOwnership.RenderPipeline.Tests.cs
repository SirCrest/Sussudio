using System.Threading.Tasks;

static partial class Program
{
    private static Task D3D11PreviewRenderer_PanelBindingLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var panelBindingText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.PanelBinding.cs")
            .Replace("\r\n", "\n");

        AssertContains(panelBindingText, "private int _swapChainBound;");
        AssertContains(panelBindingText, "private int _compositionTransformDirty;");
        AssertContains(panelBindingText, "private int _panelPixelWidth = 1;");
        AssertContains(panelBindingText, "private double _panelLogicalWidth = 1.0;");
        AssertContains(panelBindingText, "private double _rasterizationScale = 1.0;");
        AssertContains(panelBindingText, "public void OnPanelSizeChanged(double logicalWidth, double logicalHeight, double rasterizationScale)");
        AssertContains(panelBindingText, "private void BindSwapChainToPanel(IDXGISwapChain1 swapChain)");
        AssertContains(panelBindingText, "private void UnbindSwapChainFromPanel()");
        AssertContains(panelBindingText, "private void ApplyCompositionScaleTransform(IDXGISwapChain1 swapChain)");
        AssertContains(panelBindingText, "WinRT.CastExtensions.As<ISwapChainPanelNative>(_panel)");
        AssertContains(panelBindingText, "swapChain2.MatrixTransform");
        AssertDoesNotContain(rootText, "private int _swapChainBound;");
        AssertDoesNotContain(rootText, "private int _compositionTransformDirty;");
        AssertDoesNotContain(resourcesText, "private void BindSwapChainToPanel(IDXGISwapChain1 swapChain)");
        AssertDoesNotContain(resourcesText, "private void UnbindSwapChainFromPanel()");
        AssertDoesNotContain(resourcesText, "private void ApplyCompositionScaleTransform(IDXGISwapChain1 swapChain)");

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_DeviceInitializationLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var deviceInitializationText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.DeviceInitialization.cs")
            .Replace("\r\n", "\n");
        var swapChainInitializationText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.SwapChainInitialization.cs")
            .Replace("\r\n", "\n");

        AssertContains(resourcesText, "private ID3D11Device? _device;");
        AssertContains(resourcesText, "private IDXGISwapChain1? _swapChain;");
        AssertContains(resourcesText, "private ID3D11VideoProcessor? _videoProcessor;");
        AssertContains(deviceInitializationText, "private void InitializeD3D()");
        AssertContains(deviceInitializationText, "private void ConfigureMediaPresentDuration()");
        AssertContains(deviceInitializationText, "var sharedDeviceActive = TryInitializeWithSharedDevice(out var featureLevel);");
        AssertContains(deviceInitializationText, "var (swapChain, pixelWidth, pixelHeight) = InitializeCompositionSwapChain(device);");
        AssertContains(deviceInitializationText, "private void CreateRendererOwnedDevice(out FeatureLevel featureLevel)");
        AssertDoesNotContain(deviceInitializationText, "_factory.CreateSwapChainForComposition(device,");
        AssertContains(swapChainInitializationText, "private (IDXGISwapChain1 SwapChain, int PixelWidth, int PixelHeight) InitializeCompositionSwapChain(ID3D11Device device)");
        AssertContains(swapChainInitializationText, "DXGI.CreateDXGIFactory2(false, out _factory)");
        AssertContains(swapChainInitializationText, "_factory.CreateSwapChainForComposition(device, swapChainDescription, null);");
        AssertContains(swapChainInitializationText, "private void EnsureHdrCapableSwapChainOrFallbackToSdr(");
        AssertContains(swapChainInitializationText, "_swapChain3.CheckColorSpaceSupport(ColorSpaceType.RgbFullG2084NoneP2020)");
        AssertContains(swapChainInitializationText, "private void RecreateSdrCompositionSwapChain(");
        AssertContains(swapChainInitializationText, "Format.B8G8R8A8_UNorm");
        AssertContains(swapChainInitializationText, "_configuredOutputWidth = pixelWidth;");
        AssertContains(resourcesText, "private void EnsurePipeline(int width, int height, bool isHdr, bool useExternalTexture)");
        AssertContains(resourcesText, "private void DisposeProcessorResources()");
        AssertContains(resourcesText, "private void CleanupD3DResources()");
        AssertContains(resourcesText, "DisposeProcessorInputResources();");
        AssertContains(resourcesText, "DisposeNv12ShaderResourceViews();");
        AssertContains(resourcesText, "DisposeInputTextureResources();");
        AssertContains(resourcesText, "DisposeShaderPipelineResources();");
        AssertDoesNotContain(resourcesText, "private void InitializeD3D()");
        AssertDoesNotContain(resourcesText, "private bool TryInitializeWithSharedDevice(");
        AssertDoesNotContain(deviceInitializationText, "private bool TryInitializeWithSharedDevice(");
        AssertDoesNotContain(deviceInitializationText, "CheckColorSpaceSupport(ColorSpaceType.RgbFullG2084NoneP2020)");
        AssertDoesNotContain(resourcesText, "private void CreateRendererOwnedDevice(");
        AssertDoesNotContain(rootText, "private ID3D11Device? _device;");
        AssertDoesNotContain(rootText, "private IDXGISwapChain1? _swapChain;");

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_SharedDeviceLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var deviceInitializationText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.DeviceInitialization.cs")
            .Replace("\r\n", "\n");
        var renderThreadText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderThread.cs")
            .Replace("\r\n", "\n");
        var sharedDeviceText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.SharedDevice.cs")
            .Replace("\r\n", "\n");

        AssertContains(sharedDeviceText, "private ID3D11Device? _sharedDevice;");
        AssertContains(sharedDeviceText, "private int _sharedDeviceResetPending;");
        AssertContains(sharedDeviceText, "private int _sharedDeviceActive;");
        AssertContains(sharedDeviceText, "public void SetSharedDevice(ID3D11Device sharedDevice)");
        AssertContains(sharedDeviceText, "public void RetireSharedDeviceReferenceForReinit()");
        AssertContains(sharedDeviceText, "private bool TryInitializeWithSharedDevice(out FeatureLevel featureLevel)");
        AssertContains(sharedDeviceText, "Marshal.AddRef(sharedDevice.NativePointer);");
        AssertContains(sharedDeviceText, "Interlocked.Exchange(ref _sharedDeviceResetPending, 1);");
        AssertContains(sharedDeviceText, "SignalFrameReady(\"shared_device_reset\");");
        AssertContains(sharedDeviceText, "AccessViolationException");
        AssertContains(deviceInitializationText, "var sharedDeviceActive = TryInitializeWithSharedDevice(out var featureLevel);");
        AssertContains(renderThreadText, "Interlocked.CompareExchange(ref _sharedDeviceResetPending, 0, 1)");
        AssertDoesNotContain(rootText, "public void SetSharedDevice(ID3D11Device sharedDevice)");
        AssertDoesNotContain(rootText, "public void RetireSharedDeviceReferenceForReinit()");
        AssertDoesNotContain(deviceInitializationText, "private bool TryInitializeWithSharedDevice(out FeatureLevel featureLevel)");

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_InputResourcesLiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var inputResourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.InputResources.cs")
            .Replace("\r\n", "\n");

        AssertContains(inputResourcesText, "private ID3D11Texture2D? _inputTexture;");
        AssertContains(inputResourcesText, "private ID3D11Texture2D? _hdrInputTexture;");
        AssertContains(inputResourcesText, "private ID3D11ShaderResourceView? _hdrYPlaneSRV;");
        AssertContains(inputResourcesText, "private bool _hdrPlaneViewsUnavailable;");
        AssertContains(inputResourcesText, "private void EnsureInputResources(int width, int height, bool isHdr)");
        AssertContains(inputResourcesText, "private void EnsureHdrInputResources(int width, int height)");
        AssertContains(inputResourcesText, "private ID3D11ShaderResourceView? CreateHdrPlaneView(Format format, uint planeSlice)");
        AssertContains(inputResourcesText, "private void DisposeProcessorInputResources()");
        AssertContains(inputResourcesText, "private void DisposeInputTextureResources()");
        AssertContains(inputResourcesText, "_inputTexture = _device.CreateTexture2D(inputDescription);");
        AssertContains(inputResourcesText, "_hdrYPlaneSRV = CreateHdrPlaneView(Format.R16_UNorm, planeSlice: 0);");
        AssertDoesNotContain(rootText, "private ID3D11Texture2D? _inputTexture;");
        AssertDoesNotContain(rootText, "private ID3D11ShaderResourceView? _hdrYPlaneSRV;");
        AssertDoesNotContain(resourcesText, "private void EnsureInputResources(int width, int height, bool isHdr)");
        AssertDoesNotContain(resourcesText, "private void EnsureHdrInputResources(int width, int height)");
        AssertDoesNotContain(resourcesText, "private ID3D11ShaderResourceView? CreateHdrPlaneView");
        AssertDoesNotContain(resourcesText, "_inputView?.Dispose();");
        AssertDoesNotContain(resourcesText, "_hdrYPlaneSRV?.Dispose();");
        AssertDoesNotContain(resourcesText, "_stagingTexture?.Dispose();");

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_FrameUploadLivesInFocusedPartial()
    {
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var frameUploadText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.FrameUpload.cs")
            .Replace("\r\n", "\n");

        AssertContains(frameUploadText, "private bool TryResolveInputView(PendingFrame frame, out ID3D11VideoProcessorInputView? inputView, out bool disposeInputView)");
        AssertContains(frameUploadText, "private ID3D11VideoProcessorInputView CreateInputViewFromTexture(ID3D11Texture2D texture, int subresourceIndex)");
        AssertContains(frameUploadText, "private bool _loggedDirectUploadFallback;");
        AssertContains(frameUploadText, "private unsafe bool UploadRawFrameToTexture(");
        AssertContains(frameUploadText, "private unsafe bool TryUpdateRawFrameTexture(");
        AssertContains(frameUploadText, "private unsafe bool UploadRawFrameViaStaging(");
        AssertContains(frameUploadText, "_deviceContext.UpdateSubresource(");
        AssertContains(frameUploadText, "_deviceContext.CopyResource(inputTexture, stagingTexture);");
        AssertDoesNotContain(renderPassesText, "private bool TryResolveInputView(PendingFrame frame, out ID3D11VideoProcessorInputView? inputView, out bool disposeInputView)");
        AssertDoesNotContain(renderPassesText, "private unsafe bool UploadRawFrameViaStaging(");
        AssertDoesNotContain(renderPassesText, "private bool _loggedDirectUploadFallback;");

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_FrameLatencyLivesInFocusedPartial()
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

    private static Task D3D11PreviewRenderer_ViewportHelpersLiveInFocusedPartial()
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

    private static Task D3D11PreviewRenderer_RenderPassesLiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var renderThreadText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderThread.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Lifecycle.cs")
            .Replace("\r\n", "\n");
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var shaderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderPasses.cs")
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
        AssertContains(shaderPassesText, "private void RenderNv12WithShader(PendingFrame frame)");
        AssertContains(shaderPassesText, "private void RenderHdrFrameWithShader(PendingFrame frame, ID3D11PixelShader pixelShader)");
        AssertContains(shaderPassesText, "if (!TryEnterNativeRenderCall())");
        AssertContains(shaderPassesText, "ExitNativeRenderCall();");
        AssertContains(shaderPassesText, "PresentAndTrackFrame(");
        AssertContains(shaderPassesText, "TryEnsureNv12ShaderResources(frame)");
        AssertContains(renderPassesText, "TryResolveInputView(frame, out var inputView, out var disposeInputView)");
        AssertContains(renderPassesText, "D3D11_PREVIEW_HDR_SHADER_FALLBACK");
        AssertContains(lifecycleText, "private bool TryEnterNativeRenderCall()");
        AssertContains(lifecycleText, "private void ExitNativeRenderCall()");
        AssertContains(lifecycleText, "Interlocked.Exchange(ref _inNativeCall, 1);");
        AssertContains(lifecycleText, "Interlocked.Exchange(ref _inNativeCall, 0);");
        AssertContains(renderThreadText, "RenderFrame(frame);");
        AssertDoesNotContain(rootText, "private void RenderFrame(PendingFrame frame)");
        AssertDoesNotContain(renderPassesText, "private void RenderNv12WithShader(PendingFrame frame)");
        AssertDoesNotContain(renderPassesText, "private void RenderHdrFrameWithShader(PendingFrame frame, ID3D11PixelShader pixelShader)");
        AssertDoesNotContain(shaderRenderingText, "private void RenderNv12WithShader(PendingFrame frame)");
        AssertDoesNotContain(shaderRenderingText, "private void RenderHdrFrameWithShader(PendingFrame frame, ID3D11PixelShader pixelShader)");

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_ShaderRenderingLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var shaderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderPasses.cs")
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
        AssertContains(shaderPassesText, "PreviewShaderSources.RendererModeNv12");
        AssertContains(shaderPassesText, "RendererModeHdrPassthrough");
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

    private static Task D3D11PreviewRenderer_ShaderSourcesLiveInFocusedFile()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var shaderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderPasses.cs")
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
        AssertDoesNotContain(renderPassesText, "internal const string HdrTonemapPixel");
        AssertDoesNotContain(shaderPassesText, "internal const string HdrTonemapPixel");
        AssertDoesNotContain(renderPassesText, "BT2020_to_BT709");
        AssertDoesNotContain(shaderPassesText, "BT2020_to_BT709");
        AssertDoesNotContain(shaderSourcesText, "static const float PQ_m1");
        AssertDoesNotContain(shaderSourcesText, "Texture2D<float> yPlane : register(t0);");

        return Task.CompletedTask;
    }
}
