using System.Threading.Tasks;

static partial class Program
{
    private static Task D3D11PreviewRenderer_PanelBindingLivesInFocusedPartial()
    {
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var panelBindingText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.PanelBinding.cs")
            .Replace("\r\n", "\n");

        AssertContains(panelBindingText, "private void BindSwapChainToPanel(IDXGISwapChain1 swapChain)");
        AssertContains(panelBindingText, "private void UnbindSwapChainFromPanel()");
        AssertContains(panelBindingText, "private void ApplyCompositionScaleTransform(IDXGISwapChain1 swapChain)");
        AssertContains(panelBindingText, "WinRT.CastExtensions.As<ISwapChainPanelNative>(_panel)");
        AssertContains(panelBindingText, "swapChain2.MatrixTransform");
        AssertDoesNotContain(resourcesText, "private void BindSwapChainToPanel(IDXGISwapChain1 swapChain)");
        AssertDoesNotContain(resourcesText, "private void UnbindSwapChainFromPanel()");
        AssertDoesNotContain(resourcesText, "private void ApplyCompositionScaleTransform(IDXGISwapChain1 swapChain)");

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_DeviceInitializationLivesInFocusedPartial()
    {
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var deviceInitializationText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.DeviceInitialization.cs")
            .Replace("\r\n", "\n");

        AssertContains(deviceInitializationText, "private void InitializeD3D()");
        AssertContains(deviceInitializationText, "private void ConfigureMediaPresentDuration()");
        AssertContains(deviceInitializationText, "var sharedDeviceActive = TryInitializeWithSharedDevice(out var featureLevel);");
        AssertContains(deviceInitializationText, "private void CreateRendererOwnedDevice(out FeatureLevel featureLevel)");
        AssertContains(deviceInitializationText, "_factory.CreateSwapChainForComposition(device, swapChainDescription, null);");
        AssertContains(resourcesText, "private void EnsurePipeline(int width, int height, bool isHdr, bool useExternalTexture)");
        AssertContains(resourcesText, "private void DisposeProcessorResources()");
        AssertContains(resourcesText, "private void CleanupD3DResources()");
        AssertDoesNotContain(resourcesText, "private void InitializeD3D()");
        AssertDoesNotContain(resourcesText, "private bool TryInitializeWithSharedDevice(");
        AssertDoesNotContain(deviceInitializationText, "private bool TryInitializeWithSharedDevice(");
        AssertDoesNotContain(resourcesText, "private void CreateRendererOwnedDevice(");

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
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var inputResourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.InputResources.cs")
            .Replace("\r\n", "\n");

        AssertContains(inputResourcesText, "private void EnsureInputResources(int width, int height, bool isHdr)");
        AssertContains(inputResourcesText, "private void EnsureHdrInputResources(int width, int height)");
        AssertContains(inputResourcesText, "private ID3D11ShaderResourceView? CreateHdrPlaneView(Format format, uint planeSlice)");
        AssertContains(inputResourcesText, "_inputTexture = _device.CreateTexture2D(inputDescription);");
        AssertContains(inputResourcesText, "_hdrYPlaneSRV = CreateHdrPlaneView(Format.R16_UNorm, planeSlice: 0);");
        AssertDoesNotContain(resourcesText, "private void EnsureInputResources(int width, int height, bool isHdr)");
        AssertDoesNotContain(resourcesText, "private void EnsureHdrInputResources(int width, int height)");
        AssertDoesNotContain(resourcesText, "private ID3D11ShaderResourceView? CreateHdrPlaneView");

        return Task.CompletedTask;
    }

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

    private static Task D3D11PreviewRenderer_FrameLatencyLivesInFocusedPartial()
    {
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var renderingText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Rendering.cs")
            .Replace("\r\n", "\n");
        var frameLatencyText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.FrameLatency.cs")
            .Replace("\r\n", "\n");

        AssertContains(frameLatencyText, "private void ConfigureFrameLatencyWaitableObject()");
        AssertContains(frameLatencyText, "private void WaitForFrameLatencySignal()");
        AssertContains(frameLatencyText, "TrackFrameLatencyWait(result, Stopwatch.GetTimestamp() - waitStart);");
        AssertContains(frameLatencyText, "private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);");
        AssertDoesNotContain(resourcesText, "private void WaitForFrameLatencySignal()");
        AssertDoesNotContain(renderingText, "private static extern uint WaitForSingleObject");

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
}
