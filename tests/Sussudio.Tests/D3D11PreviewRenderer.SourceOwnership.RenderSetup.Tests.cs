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
        var panelCompositionText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.PanelComposition.cs")
            .Replace("\r\n", "\n");

        AssertContains(panelBindingText, "private int _swapChainBound;");
        AssertContains(panelBindingText, "private void BindSwapChainToPanel(IDXGISwapChain1 swapChain)");
        AssertContains(panelBindingText, "private void UnbindSwapChainFromPanel()");
        AssertContains(panelBindingText, "WinRT.CastExtensions.As<ISwapChainPanelNative>(_panel)");
        AssertContains(panelCompositionText, "private int _compositionTransformDirty;");
        AssertContains(panelCompositionText, "private int _panelPixelWidth = 1;");
        AssertContains(panelCompositionText, "private double _panelLogicalWidth = 1.0;");
        AssertContains(panelCompositionText, "private double _rasterizationScale = 1.0;");
        AssertContains(panelCompositionText, "public void OnPanelSizeChanged(double logicalWidth, double logicalHeight, double rasterizationScale)");
        AssertContains(panelCompositionText, "private void ApplyCompositionScaleTransform(IDXGISwapChain1 swapChain)");
        AssertContains(panelCompositionText, "swapChain2.MatrixTransform");
        AssertDoesNotContain(panelBindingText, "public void OnPanelSizeChanged(");
        AssertDoesNotContain(panelBindingText, "private void ApplyCompositionScaleTransform(IDXGISwapChain1 swapChain)");
        AssertDoesNotContain(rootText, "private int _swapChainBound;");
        AssertDoesNotContain(rootText, "private int _compositionTransformDirty;");
        AssertDoesNotContain(resourcesText, "private void BindSwapChainToPanel(IDXGISwapChain1 swapChain)");
        AssertDoesNotContain(resourcesText, "private void UnbindSwapChainFromPanel()");
        AssertDoesNotContain(resourcesText, "private void ApplyCompositionScaleTransform(IDXGISwapChain1 swapChain)");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_DeviceInitializationLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var deviceInitializationText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.DeviceInitialization.cs")
            .Replace("\r\n", "\n");
        var swapChainInitializationText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.SwapChainInitialization.cs")
            .Replace("\r\n", "\n");
        var videoProcessorPipelineText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.VideoProcessorPipeline.cs")
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
        AssertDoesNotContain(resourcesText, "private void EnsurePipeline(int width, int height, bool isHdr, bool useExternalTexture)");
        AssertDoesNotContain(resourcesText, "private void EnsureSwapChainRTV()");
        AssertDoesNotContain(resourcesText, "private void RecreateOutputView()");
        AssertDoesNotContain(resourcesText, "private void ApplyColorSpaces(bool isHdr)");
        AssertDoesNotContain(resourcesText, "private void InitializeD3D()");
        AssertDoesNotContain(resourcesText, "private bool TryInitializeWithSharedDevice(");
        AssertDoesNotContain(deviceInitializationText, "private bool TryInitializeWithSharedDevice(");
        AssertDoesNotContain(deviceInitializationText, "CheckColorSpaceSupport(ColorSpaceType.RgbFullG2084NoneP2020)");
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
        AssertContains(renderThreadText, "HandlePendingSharedDeviceResetOnRenderThread();");
        AssertContains(renderThreadText, "private void HandlePendingSharedDeviceResetOnRenderThread()");
        AssertContains(renderThreadText, "TrackFrameDropped(stale, \"shared-device-reset\");");
        AssertContains(renderThreadText, "CleanupD3DResources();");
        AssertContains(renderThreadText, "InitializeD3D();");
        AssertDoesNotContain(rootText, "public void SetSharedDevice(ID3D11Device sharedDevice)");
        AssertDoesNotContain(rootText, "public void RetireSharedDeviceReferenceForReinit()");
        AssertDoesNotContain(deviceInitializationText, "private bool TryInitializeWithSharedDevice(out FeatureLevel featureLevel)");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_InputResourcesLiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var videoProcessorPipelineText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.VideoProcessorPipeline.cs")
            .Replace("\r\n", "\n");
        var inputResourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.InputResources.cs")
            .Replace("\r\n", "\n");
        var hdrInputResourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.HdrInputResources.cs")
            .Replace("\r\n", "\n");

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
        AssertDoesNotContain(resourcesText, "private void EnsureInputResources(int width, int height, bool isHdr)");
        AssertDoesNotContain(resourcesText, "private void EnsureHdrInputResources(int width, int height)");
        AssertDoesNotContain(resourcesText, "private ID3D11ShaderResourceView? CreateHdrPlaneView");
        AssertDoesNotContain(inputResourcesText, "private void EnsureHdrInputResources(int width, int height)");
        AssertDoesNotContain(inputResourcesText, "private ID3D11ShaderResourceView? CreateHdrPlaneView");
        AssertDoesNotContain(inputResourcesText, "_hdrYPlaneSRV?.Dispose();");
        AssertDoesNotContain(resourcesText, "_inputView?.Dispose();");
        AssertDoesNotContain(resourcesText, "_hdrYPlaneSRV?.Dispose();");
        AssertDoesNotContain(resourcesText, "_stagingTexture?.Dispose();");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_FrameUploadLivesInFocusedPartial()
    {
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var frameUploadText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.FrameUpload.cs")
            .Replace("\r\n", "\n");
        var rawFrameUploadText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RawFrameUpload.cs")
            .Replace("\r\n", "\n");

        AssertContains(frameUploadText, "private bool TryResolveInputView(PendingFrame frame, out ID3D11VideoProcessorInputView? inputView, out bool disposeInputView)");
        AssertContains(frameUploadText, "private ID3D11VideoProcessorInputView CreateInputViewFromTexture(ID3D11Texture2D texture, int subresourceIndex)");
        AssertContains(frameUploadText, "inputView = CreateInputViewFromTexture(frame.D3DTexture, frame.D3DSubresourceIndex);");
        AssertContains(frameUploadText, "UploadRawFrameToTexture(frame.RawData, frame.RawDataLength");
        AssertContains(rawFrameUploadText, "private bool _loggedDirectUploadFallback;");
        AssertContains(rawFrameUploadText, "private unsafe bool UploadRawFrameToTexture(");
        AssertContains(rawFrameUploadText, "private unsafe bool TryUpdateRawFrameTexture(");
        AssertContains(rawFrameUploadText, "private unsafe bool UploadRawFrameViaStaging(");
        AssertContains(rawFrameUploadText, "_deviceContext.UpdateSubresource(");
        AssertContains(rawFrameUploadText, "_deviceContext.CopyResource(inputTexture, stagingTexture);");
        AssertDoesNotContain(frameUploadText, "private bool _loggedDirectUploadFallback;");
        AssertDoesNotContain(frameUploadText, "private unsafe bool TryUpdateRawFrameTexture(");
        AssertDoesNotContain(frameUploadText, "private unsafe bool UploadRawFrameViaStaging(");
        AssertDoesNotContain(renderPassesText, "private bool TryResolveInputView(PendingFrame frame, out ID3D11VideoProcessorInputView? inputView, out bool disposeInputView)");
        AssertDoesNotContain(renderPassesText, "private unsafe bool UploadRawFrameViaStaging(");
        AssertDoesNotContain(renderPassesText, "private bool _loggedDirectUploadFallback;");

        return Task.CompletedTask;
    }

}
