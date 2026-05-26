using System.IO;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task D3D11PreviewRenderer_PanelBindingLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var panelBindingText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.PanelBinding.cs")
            .Replace("\r\n", "\n");

        AssertContains(panelBindingText, "private int _swapChainBound;");
        AssertContains(panelBindingText, "private void BindSwapChainToPanel(IDXGISwapChain1 swapChain)");
        AssertContains(panelBindingText, "private void UnbindSwapChainFromPanel()");
        AssertContains(panelBindingText, "WinRT.CastExtensions.As<ISwapChainPanelNative>(_panel)");
        AssertContains(panelBindingText, "private int _compositionTransformDirty;");
        AssertContains(panelBindingText, "private int _panelPixelWidth = 1;");
        AssertContains(panelBindingText, "private double _panelLogicalWidth = 1.0;");
        AssertContains(panelBindingText, "private double _rasterizationScale = 1.0;");
        AssertContains(panelBindingText, "public void OnPanelSizeChanged(double logicalWidth, double logicalHeight, double rasterizationScale)");
        AssertContains(panelBindingText, "private void ApplyCompositionScaleTransform(IDXGISwapChain1 swapChain)");
        AssertContains(panelBindingText, "swapChain2.MatrixTransform");
        AssertDoesNotContain(rootText, "private int _swapChainBound;");
        AssertDoesNotContain(rootText, "private int _compositionTransformDirty;");
        AssertDoesNotContain(resourcesText, "private void BindSwapChainToPanel(IDXGISwapChain1 swapChain)");
        AssertDoesNotContain(resourcesText, "private void UnbindSwapChainFromPanel()");
        AssertDoesNotContain(resourcesText, "private void ApplyCompositionScaleTransform(IDXGISwapChain1 swapChain)");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_DeviceInitializationOwnsSwapChainSetup()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var deviceInitializationText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.DeviceInitialization.cs")
            .Replace("\r\n", "\n");
        var videoProcessorPipelineText = resourcesText;

        AssertContains(resourcesText, "private ID3D11Device? _device;");
        AssertContains(resourcesText, "private IDXGISwapChain1? _swapChain;");
        AssertContains(resourcesText, "private ID3D11VideoProcessor? _videoProcessor;");
        AssertContains(deviceInitializationText, "private void InitializeD3D()");
        AssertContains(deviceInitializationText, "private void ConfigureMediaPresentDuration()");
        AssertContains(deviceInitializationText, "var sharedDeviceActive = TryInitializeWithSharedDevice(out var featureLevel);");
        AssertContains(deviceInitializationText, "var (swapChain, pixelWidth, pixelHeight) = InitializeCompositionSwapChain(device);");
        AssertContains(deviceInitializationText, "private void CreateRendererOwnedDevice(out FeatureLevel featureLevel)");
        AssertContains(deviceInitializationText, "private (IDXGISwapChain1 SwapChain, int PixelWidth, int PixelHeight) InitializeCompositionSwapChain(ID3D11Device device)");
        AssertContains(deviceInitializationText, "DXGI.CreateDXGIFactory2(false, out _factory)");
        AssertContains(deviceInitializationText, "_factory.CreateSwapChainForComposition(device, swapChainDescription, null);");
        AssertContains(deviceInitializationText, "private void EnsureHdrCapableSwapChainOrFallbackToSdr(");
        AssertContains(deviceInitializationText, "_swapChain3.CheckColorSpaceSupport(ColorSpaceType.RgbFullG2084NoneP2020)");
        AssertContains(deviceInitializationText, "private void RecreateSdrCompositionSwapChain(");
        AssertContains(deviceInitializationText, "Format.B8G8R8A8_UNorm");
        AssertContains(deviceInitializationText, "_configuredOutputWidth = pixelWidth;");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.SwapChainInitialization.cs")),
            "D3D11 preview swap-chain setup folded into device initialization owner");
        AssertContains(videoProcessorPipelineText, "private void EnsurePipeline(int width, int height, bool isHdr, bool useExternalTexture)");
        AssertContains(videoProcessorPipelineText, "private void EnsureSwapChainRTV()");
        AssertContains(videoProcessorPipelineText, "private void RecreateOutputView()");
        AssertContains(videoProcessorPipelineText, "private void ApplyColorSpaces(bool isHdr)");
        AssertContains(videoProcessorPipelineText, "private void DisposeProcessorResources()");
        AssertContains(videoProcessorPipelineText, "DisposeProcessorInputResources();");
        AssertContains(videoProcessorPipelineText, "DisposeNv12ShaderResourceViews();");
        AssertContains(videoProcessorPipelineText, "new VideoProcessorContentDescription");
        AssertContains(videoProcessorPipelineText, "RecreateOutputView();");
        AssertContains(videoProcessorPipelineText, "ApplyColorSpaces(isHdr);");
        AssertContains(videoProcessorPipelineText, "_videoDevice.CreateVideoProcessorOutputView(");
        AssertContains(videoProcessorPipelineText, "_videoContext1.VideoProcessorSetStreamColorSpace1(");
        AssertContains(videoProcessorPipelineText, "D3D11 preview color space input=");
        AssertContains(resourcesText, "private void CleanupD3DResources()");
        AssertContains(resourcesText, "DisposeInputTextureResources();");
        AssertContains(resourcesText, "DisposeShaderPipelineResources();");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.VideoProcessorPipeline.cs")),
            "VideoProcessor setup and output-view resources live with D3D resource ownership");
        AssertDoesNotContain(resourcesText, "private void InitializeD3D()");
        AssertContains(deviceInitializationText, "private bool TryInitializeWithSharedDevice(");
        AssertContains(deviceInitializationText, "private void HandleDeviceLost(Exception ex)");
        AssertContains(deviceInitializationText, "private static bool IsDeviceLostException(Exception ex)");
        AssertDoesNotContain(resourcesText, "private bool TryInitializeWithSharedDevice(");
        AssertDoesNotContain(resourcesText, "private void HandleDeviceLost(Exception ex)");
        AssertDoesNotContain(resourcesText, "private void CreateRendererOwnedDevice(");
        AssertDoesNotContain(rootText, "private ID3D11Device? _device;");
        AssertDoesNotContain(rootText, "private IDXGISwapChain1? _swapChain;");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_SharedDeviceLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var deviceInitializationText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.DeviceInitialization.cs")
            .Replace("\r\n", "\n");
        var renderThreadText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderThread.cs")
            .Replace("\r\n", "\n");
        var sharedDeviceText = deviceInitializationText;

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
        AssertContains(renderThreadText, "HandlePendingSharedDeviceResetOnRenderThread();");
        AssertContains(renderThreadText, "private void HandlePendingSharedDeviceResetOnRenderThread()");
        AssertContains(renderThreadText, "TrackFrameDropped(stale, \"shared-device-reset\");");
        AssertContains(renderThreadText, "CleanupD3DResources();");
        AssertContains(renderThreadText, "InitializeD3D();");
        AssertDoesNotContain(rootText, "public void SetSharedDevice(ID3D11Device sharedDevice)");
        AssertDoesNotContain(rootText, "public void RetireSharedDeviceReferenceForReinit()");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_InputResourcesLiveWithD3DResources()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var videoProcessorPipelineText = resourcesText;
        var inputResourcesText = resourcesText;
        var hdrInputResourcesText = resourcesText;

        AssertContains(inputResourcesText, "private ID3D11Texture2D? _inputTexture;");
        AssertContains(inputResourcesText, "private void EnsureInputResources(int width, int height, bool isHdr)");
        AssertContains(inputResourcesText, "private void DisposeProcessorInputResources()");
        AssertContains(inputResourcesText, "private void DisposeInputTextureResources()");
        AssertContains(inputResourcesText, "_inputTexture = _device.CreateTexture2D(inputDescription);");
        AssertContains(videoProcessorPipelineText, "DisposeHdrInputResources();");
        AssertContains(hdrInputResourcesText, "private ID3D11Texture2D? _hdrInputTexture;");
        AssertContains(hdrInputResourcesText, "private ID3D11ShaderResourceView? _hdrYPlaneSRV;");
        AssertContains(hdrInputResourcesText, "private bool _hdrPlaneViewsUnavailable;");
        AssertContains(hdrInputResourcesText, "private void EnsureHdrInputResources(int width, int height)");
        AssertContains(hdrInputResourcesText, "private ID3D11ShaderResourceView? CreateHdrPlaneView(Format format, uint planeSlice)");
        AssertContains(hdrInputResourcesText, "private void DisposeHdrInputResources()");
        AssertContains(hdrInputResourcesText, "_hdrYPlaneSRV = CreateHdrPlaneView(Format.R16_UNorm, planeSlice: 0);");
        AssertDoesNotContain(rootText, "private ID3D11Texture2D? _inputTexture;");
        AssertDoesNotContain(rootText, "private ID3D11ShaderResourceView? _hdrYPlaneSRV;");
        AssertDoesNotContain(rootText, "private void EnsureInputResources(int width, int height, bool isHdr)");
        AssertDoesNotContain(rootText, "private void EnsureHdrInputResources(int width, int height)");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_FrameUploadLivesInFocusedPartial()
    {
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var frameUploadText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.FrameUpload.cs")
            .Replace("\r\n", "\n");

        AssertContains(frameUploadText, "private bool TryResolveInputView(PendingFrame frame, out ID3D11VideoProcessorInputView? inputView, out bool disposeInputView)");
        AssertContains(frameUploadText, "private ID3D11VideoProcessorInputView CreateInputViewFromTexture(ID3D11Texture2D texture, int subresourceIndex)");
        AssertContains(frameUploadText, "inputView = CreateInputViewFromTexture(frame.D3DTexture, frame.D3DSubresourceIndex);");
        AssertContains(frameUploadText, "UploadRawFrameToTexture(frame.RawData, frame.RawDataLength");
        AssertContains(frameUploadText, "private bool _loggedDirectUploadFallback;");
        AssertContains(frameUploadText, "private unsafe bool UploadRawFrameToTexture(");
        AssertContains(frameUploadText, "private unsafe bool TryUpdateRawFrameTexture(");
        AssertContains(frameUploadText, "private unsafe bool UploadRawFrameViaStaging(");
        AssertContains(frameUploadText, "_deviceContext.UpdateSubresource(");
        AssertContains(frameUploadText, "_deviceContext.CopyResource(inputTexture, stagingTexture);");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.RawFrameUpload.cs")),
            "Raw frame upload helpers folded into frame upload owner");
        AssertDoesNotContain(renderPassesText, "private bool TryResolveInputView(PendingFrame frame, out ID3D11VideoProcessorInputView? inputView, out bool disposeInputView)");
        AssertDoesNotContain(renderPassesText, "private unsafe bool UploadRawFrameViaStaging(");
        AssertDoesNotContain(renderPassesText, "private bool _loggedDirectUploadFallback;");

        return Task.CompletedTask;
    }

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

    internal static Task D3D11PreviewRenderer_ComputeLetterboxRect_CalculatesCorrectly()
    {
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var method = rendererType.GetMethod("ComputeLetterboxRect",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ComputeLetterboxRect not found.");

        var result1 = method.Invoke(null, new object[] { 1920, 1080, 1920, 1080 })!;
        var resultType = result1.GetType();
        var left1 = (int)resultType.GetField("Left")!.GetValue(result1)!;
        var top1 = (int)resultType.GetField("Top")!.GetValue(result1)!;
        var right1 = (int)resultType.GetField("Right")!.GetValue(result1)!;
        var bottom1 = (int)resultType.GetField("Bottom")!.GetValue(result1)!;
        AssertEqual(0, left1, "Same aspect: left=0");
        AssertEqual(0, top1, "Same aspect: top=0");
        AssertEqual(1920, right1, "Same aspect: right=1920");
        AssertEqual(1080, bottom1, "Same aspect: bottom=1080");

        var result2 = method.Invoke(null, new object[] { 1920, 1080, 1024, 768 })!;
        var top2 = (int)resultType.GetField("Top")!.GetValue(result2)!;
        var left2 = (int)resultType.GetField("Left")!.GetValue(result2)!;
        AssertEqual(true, top2 > 0, "16:9 into 4:3 should letterbox (top > 0)");
        AssertEqual(0, left2, "16:9 into 4:3 should not pillarbox");

        var result3 = method.Invoke(null, new object[] { 1024, 768, 1920, 1080 })!;
        var left3 = (int)resultType.GetField("Left")!.GetValue(result3)!;
        var top3 = (int)resultType.GetField("Top")!.GetValue(result3)!;
        AssertEqual(true, left3 > 0, "4:3 into 16:9 should pillarbox (left > 0)");
        AssertEqual(0, top3, "4:3 into 16:9 should not letterbox");

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
        var previewShaderSourcesText = shaderRenderingText;

        AssertContains(previewShaderSourcesText, "internal static class PreviewShaderSources");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "PreviewShaderSources.cs")),
            "preview shader sources live with shader rendering ownership");
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

        return Task.CompletedTask;
    }
}
