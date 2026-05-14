using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task D3D11PreviewRenderer_FrameTypesLiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var frameTypesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.FrameTypes.cs")
            .Replace("\r\n", "\n");

        AssertContains(frameTypesText, "private sealed class PendingFrame : IDisposable");
        AssertContains(frameTypesText, "public readonly record struct PresentCadenceMetrics(");
        AssertContains(frameTypesText, "public readonly record struct CpuStageTimingMetrics(");
        AssertContains(frameTypesText, "public readonly record struct RenderCpuTimingMetrics(");
        AssertContains(frameTypesText, "public readonly record struct PipelineLatencyMetrics(");
        AssertContains(frameTypesText, "public readonly record struct FrameLatencyWaitMetrics(");
        AssertContains(frameTypesText, "public readonly record struct FrameOwnershipMetrics(");
        AssertContains(frameTypesText, "public readonly record struct DxgiFrameStatisticsMetrics(");
        AssertDoesNotContain(rootText, "private sealed class PendingFrame : IDisposable");
        AssertDoesNotContain(rootText, "public readonly record struct PresentCadenceMetrics(");
        AssertDoesNotContain(rootText, "public readonly record struct DxgiFrameStatisticsMetrics(");

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_SubmissionLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var submissionText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Submission.cs")
            .Replace("\r\n", "\n");

        AssertContains(submissionText, "public void SubmitRawFrame(");
        AssertContains(submissionText, "public void SubmitRawFrameLease(");
        AssertContains(submissionText, "public void SubmitTexture(");
        AssertContains(submissionText, "public void SubmitNv12PlaneTextures(");
        AssertContains(submissionText, "private void EnqueueNv12Frame(");
        AssertContains(submissionText, "EnqueuePendingFrame(frame);");
        AssertDoesNotContain(rootText, "public void SubmitRawFrame(");
        AssertDoesNotContain(rootText, "public void SubmitRawFrameLease(");
        AssertDoesNotContain(rootText, "public void SubmitTexture(");
        AssertDoesNotContain(rootText, "public void SubmitNv12PlaneTextures(");

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_FrameOwnershipLivesInFocusedPartial()
    {
        var metricsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs")
            .Replace("\r\n", "\n");
        var ownershipText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.FrameOwnership.cs")
            .Replace("\r\n", "\n");

        AssertContains(ownershipText, "public FrameOwnershipMetrics GetFrameOwnershipMetrics()");
        AssertContains(ownershipText, "private void TrackFrameSubmitted(PendingFrame frame)");
        AssertContains(ownershipText, "private void TrackFramePresented(PendingFrame frame, long presentReturnTick, long estimatedVisibleTick)");
        AssertContains(ownershipText, "private void TrackFrameDropped(PendingFrame frame, string reason)");
        AssertContains(ownershipText, "Interlocked.Exchange(ref _lastRenderedSourcePtsTicks, frame.SourcePtsTicks);");
        AssertContains(ownershipText, "Volatile.Write(ref _lastDropReason, reason);");
        AssertDoesNotContain(metricsText, "public FrameOwnershipMetrics GetFrameOwnershipMetrics()");
        AssertDoesNotContain(metricsText, "private void TrackFrameSubmitted(PendingFrame frame)");
        AssertDoesNotContain(metricsText, "private void TrackFrameDropped(PendingFrame frame, string reason)");

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_DxgiFrameStatisticsLiveInFocusedPartial()
    {
        var metricsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs")
            .Replace("\r\n", "\n");
        var dxgiText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.DxgiFrameStatistics.cs")
            .Replace("\r\n", "\n");

        AssertContains(dxgiText, "public DxgiFrameStatisticsMetrics GetDxgiFrameStatisticsMetrics()");
        AssertContains(dxgiText, "private void TrackDxgiFrameStatistics()");
        AssertContains(dxgiText, "private long EstimateVisibleTick(long presentReturnTick)");
        AssertContains(dxgiText, "private long GetEstimatedDisplayFrameIntervalTicks()");
        AssertContains(dxgiText, "public bool TryGetDisplayClock(out PreviewDisplayClockSnapshot snapshot)");
        AssertContains(dxgiText, "_ = DwmFlush();");
        AssertContains(dxgiText, "_swapChain.GetFrameStatistics(out var stats)");
        AssertDoesNotContain(metricsText, "public DxgiFrameStatisticsMetrics GetDxgiFrameStatisticsMetrics()");
        AssertDoesNotContain(metricsText, "private void TrackDxgiFrameStatistics()");
        AssertDoesNotContain(metricsText, "private long EstimateVisibleTick(long presentReturnTick)");
        AssertDoesNotContain(metricsText, "public bool TryGetDisplayClock(out PreviewDisplayClockSnapshot snapshot)");

        return Task.CompletedTask;
    }

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

    private static Task D3D11PreviewRenderer_SlowFrameDiagnosticsLiveInFocusedPartial()
    {
        var metricsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs")
            .Replace("\r\n", "\n");
        var slowFrameDiagnosticsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.SlowFrameDiagnostics.cs")
            .Replace("\r\n", "\n");

        AssertContains(slowFrameDiagnosticsText, "public PreviewSlowFrameDiagnostic[] GetRecentSlowFrameDiagnostics(int maxEntries = 16)");
        AssertContains(slowFrameDiagnosticsText, "private void RecordSlowFrameDiagnostic(");
        AssertContains(slowFrameDiagnosticsText, "private static string BuildSlowFrameDiagnosticReason(");
        AssertContains(slowFrameDiagnosticsText, "private static void AppendSlowFrameReason(");
        AssertContains(slowFrameDiagnosticsText, "DxgiMissedRefreshCount = missedRefreshCount");
        AssertContains(slowFrameDiagnosticsText, "\"dxgi_refresh_slip\"");
        AssertDoesNotContain(metricsText, "public PreviewSlowFrameDiagnostic[] GetRecentSlowFrameDiagnostics(");
        AssertDoesNotContain(metricsText, "private void RecordSlowFrameDiagnostic(");
        AssertDoesNotContain(metricsText, "private static string BuildSlowFrameDiagnosticReason(");

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
