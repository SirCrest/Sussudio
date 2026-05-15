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

    private static Task D3D11PreviewRenderer_LifecycleLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Lifecycle.cs")
            .Replace("\r\n", "\n");

        AssertContains(lifecycleText, "public void Start(int width, int height, double fps, bool isHdr)");
        AssertContains(lifecycleText, "public void StopRenderThread()");
        AssertContains(lifecycleText, "public void Stop()");
        AssertContains(lifecycleText, "private void WaitForNativeCallToDrainOrThrow(string operation)");
        AssertContains(lifecycleText, "public void Dispose()");
        AssertContains(lifecycleText, "WaitForNativeCallToDrainOrThrow(\"stop\");");
        AssertContains(lifecycleText, "FailPendingFrameCapture(\"Preview renderer stopped before frame capture completed.\");");
        AssertDoesNotContain(rootText, "public void Start(int width, int height, double fps, bool isHdr)");
        AssertDoesNotContain(rootText, "public void StopRenderThread()");
        AssertDoesNotContain(rootText, "private void WaitForNativeCallToDrainOrThrow(string operation)");

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
        AssertContains(deviceInitializationText, "private bool TryInitializeWithSharedDevice(out FeatureLevel featureLevel)");
        AssertContains(deviceInitializationText, "private void CreateRendererOwnedDevice(out FeatureLevel featureLevel)");
        AssertContains(deviceInitializationText, "_factory.CreateSwapChainForComposition(device, swapChainDescription, null);");
        AssertContains(resourcesText, "private void EnsurePipeline(int width, int height, bool isHdr, bool useExternalTexture)");
        AssertContains(resourcesText, "private void DisposeProcessorResources()");
        AssertContains(resourcesText, "private void CleanupD3DResources()");
        AssertDoesNotContain(resourcesText, "private void InitializeD3D()");
        AssertDoesNotContain(resourcesText, "private bool TryInitializeWithSharedDevice(");
        AssertDoesNotContain(resourcesText, "private void CreateRendererOwnedDevice(");

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_ScreenshotEncodingLivesInFocusedPartial()
    {
        var captureText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ScreenshotCapture.cs")
            .Replace("\r\n", "\n");
        var encodingText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ScreenshotEncoding.cs")
            .Replace("\r\n", "\n");

        AssertContains(captureText, "private void TryCaptureFrameBeforePresent(string rendererMode)");
        AssertContains(captureText, "PreviewScreenshotCapture.CaptureFrameBufferTo16BitPng(");
        AssertContains(captureText, "private void FailPendingFrameCapture(string message)");
        AssertContains(encodingText, "private static PreviewFrameCaptureResult CaptureMappedFrameToBmp(");
        AssertContains(encodingText, "private static byte[] CopyMappedFrameToBuffer(");
        AssertContains(encodingText, "private static void WriteBitmapHeaders(");
        AssertContains(encodingText, "private static PreviewFrameCaptureResult CreateFrameCaptureError(");
        AssertDoesNotContain(captureText, "private static PreviewFrameCaptureResult CaptureMappedFrameToBmp(");
        AssertDoesNotContain(captureText, "private static void WriteBitmapHeaders(");

        return Task.CompletedTask;
    }
}
