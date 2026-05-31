using System.IO;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task D3D11PreviewRenderer_ConfigurationLivesWithRendererFacade()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "SUSSUDIO_PREVIEW_PRESENT_SYNC_INTERVAL");
        AssertContains(rootText, "SUSSUDIO_PREVIEW_DXGI_MAX_FRAME_LATENCY");
        AssertContains(rootText, "SUSSUDIO_PREVIEW_SWAPCHAIN_BUFFER_COUNT");
        AssertContains(rootText, "SUSSUDIO_PREVIEW_RENDER_QUEUE_DEPTH");
        AssertContains(rootText, "SUSSUDIO_PREVIEW_WAITABLE_SWAPCHAIN");
        AssertContains(rootText, "SUSSUDIO_PREVIEW_DXGI_FRAME_STATS_SAMPLE_INTERVAL");
        AssertContains(rootText, "SUSSUDIO_PREVIEW_RENDER_MMCSS_TASK\") ?? \"Playback\"");
        AssertContains(rootText, "SUSSUDIO_PREVIEW_NATIVE_STOP_FENCE_TIMEOUT_MS");
        AssertContains(rootText, "SUSSUDIO_PREVIEW_RENDER_THREAD_STOP_TIMEOUT_MS");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Configuration.cs")),
            "D3D11 preview renderer configuration partial");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_NativeInteropLivesWithBehaviorOwners()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var panelBindingText = rootText;
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var metricsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.NativeInterop.cs")),
            "mixed native interop bucket retired into behavior owners");
        AssertContains(panelBindingText, "private interface ISwapChainPanelNative");
        AssertContains(panelBindingText, "WinRT.CastExtensions.As<ISwapChainPanelNative>(_panel)");
        AssertContains(resourcesText, "private interface ID3DBlob");
        AssertContains(resourcesText, "private static extern int D3DCompileNative(");
        AssertContains(resourcesText, "private static byte[] CompileShader(string hlslSource, string entryPoint, string profile)");
        AssertContains(resourcesText, "private static string ReadBlobString(IntPtr blobPtr)");
        AssertContains(metricsText, "private static extern int DwmFlush()");
        AssertContains(metricsText, "_ = DwmFlush();");
        AssertContains(rootText, "private interface ISwapChainPanelNative");
        AssertDoesNotContain(rootText, "private interface ID3DBlob");
        AssertDoesNotContain(rootText, "D3DCompileNative(");
        AssertDoesNotContain(rootText, "private static extern int DwmFlush()");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_FrameTypesLiveWithPendingFrameQueue()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var metricsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs")
            .Replace("\r\n", "\n");

        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.PendingFrame.cs")), "pending-frame lifetime model stays folded into the renderer root");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.PendingFrames.cs")), "pending-frame queue folded into the renderer root");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Submission.cs")), "pending-frame submission folded into the renderer root");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.MetricTypes.cs")), "renderer metric model types folded into Metrics.cs");
        AssertContains(rootText, "private sealed class PendingFrame : IDisposable");
        AssertContains(rootText, "ArrayPool<byte>.Shared.Return(RawData);");
        AssertContains(rootText, "FrameLease?.Dispose();");
        AssertContains(metricsText, "public readonly record struct PresentCadenceMetrics(");
        AssertContains(metricsText, "public readonly record struct CpuStageTimingMetrics(");
        AssertContains(metricsText, "public readonly record struct RenderCpuTimingMetrics(");
        AssertContains(metricsText, "public readonly record struct PipelineLatencyMetrics(");
        AssertContains(metricsText, "public readonly record struct FrameLatencyWaitMetrics(");
        AssertContains(metricsText, "public readonly record struct FrameOwnershipMetrics(");
        AssertContains(metricsText, "public readonly record struct DxgiFrameStatisticsMetrics(");
        AssertContains(metricsText, "private static double[] CopyRecentRing(double[] window, int count, int index, int maxSamples)");
        AssertContains(metricsText, "private static CpuStageTimingMetrics SummarizeCpuStageTiming(double[] samples)");
        AssertContains(metricsText, "private static double TicksToMs(long ticks)");
        AssertContains(metricsText, "private static bool IsValidRenderCpuStageMs(double value)");
        AssertDoesNotContain(rootText, "public readonly record struct PresentCadenceMetrics(");
        AssertDoesNotContain(rootText, "public readonly record struct DxgiFrameStatisticsMetrics(");
        AssertDoesNotContain(metricsText, "private sealed class PendingFrame : IDisposable");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_FrameOwnershipLivesWithMetrics()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var metricsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.FrameOwnership.cs")),
            "frame ownership metrics folded into renderer metrics owner");
        AssertContains(metricsText, "private long _framesSubmitted;");
        AssertContains(metricsText, "private long _framesRendered;");
        AssertContains(metricsText, "private long _framesDropped;");
        AssertContains(metricsText, "private long _submissionGeneration;");
        AssertContains(metricsText, "private long _lastSubmittedPreviewPresentId;");
        AssertContains(metricsText, "private long _lastRenderedSchedulerToPresentTicks;");
        AssertContains(metricsText, "private long _lastDroppedUtcUnixMs;");
        AssertContains(metricsText, "private string _submissionGenerationDropReason = \"transition\";");
        AssertContains(metricsText, "public FrameOwnershipMetrics GetFrameOwnershipMetrics()");
        AssertContains(metricsText, "private void TrackFrameSubmitted(PendingFrame frame)");
        AssertContains(metricsText, "private void TrackFramePresented(PendingFrame frame, long presentReturnTick, long estimatedVisibleTick)");
        AssertContains(metricsText, "private void TrackFrameDropped(PendingFrame frame, string reason)");
        AssertContains(metricsText, "Interlocked.Exchange(ref _lastRenderedSourcePtsTicks, frame.SourcePtsTicks);");
        AssertContains(metricsText, "Volatile.Write(ref _lastDropReason, reason);");
        AssertDoesNotContain(rootText, "private long _framesSubmitted;");
        AssertDoesNotContain(rootText, "private long _submissionGeneration;");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_DxgiFrameStatisticsLiveWithMetrics()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var metricsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.DxgiFrameStatistics.cs")),
            "DXGI frame-statistics partial folded into renderer metrics owner");
        AssertContains(metricsText, "private readonly object _dxgiFrameStatisticsLock = new();");
        AssertContains(metricsText, "private long _dxgiFrameStatisticsSampleCount;");
        AssertContains(metricsText, "private long _dxgiFrameStatisticsMissedRefreshCount;");
        AssertContains(metricsText, "private long _dxgiFrameStatisticsLastSampleFrameCounter;");
        AssertContains(metricsText, "private long _dxgiFrameStatisticsPresentCount = -1;");
        AssertContains(metricsText, "private bool _dxgiFrameStatisticsHasBaseline;");
        AssertContains(metricsText, "public DxgiFrameStatisticsMetrics GetDxgiFrameStatisticsMetrics()");
        AssertContains(metricsText, "private void TrackDxgiFrameStatistics()");
        AssertContains(metricsText, "_ = DwmFlush();");
        AssertContains(metricsText, "_swapChain.GetFrameStatistics(out var stats)");
        AssertContains(metricsText, "private long EstimateVisibleTick(long presentReturnTick)");
        AssertContains(metricsText, "private long GetEstimatedDisplayFrameIntervalTicks()");
        AssertContains(metricsText, "public bool TryGetDisplayClock(out PreviewDisplayClockSnapshot snapshot)");
        AssertContains(metricsText, "new PreviewDisplayClockSnapshot(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.DisplayClock.cs")),
            "D3D11 preview display-clock projection lives with renderer metrics");
        AssertDoesNotContain(rootText, "private readonly object _dxgiFrameStatisticsLock = new();");
        AssertDoesNotContain(rootText, "private long _dxgiFrameStatisticsPresentCount = -1;");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_SlowFrameDiagnosticsLiveWithMetrics()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var metricsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs")
            .Replace("\r\n", "\n");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Diagnostics.cs")),
            "slow-frame diagnostics folded into renderer metrics owner");
        AssertContains(metricsText, "private readonly object _slowFrameDiagnosticsLock = new();");
        AssertContains(metricsText, "private readonly PreviewSlowFrameDiagnostic[] _slowFrameDiagnostics = new PreviewSlowFrameDiagnostic[64];");
        AssertContains(metricsText, "public PreviewSlowFrameDiagnostic[] GetRecentSlowFrameDiagnostics(int maxEntries = 16)");
        AssertContains(metricsText, "private void RecordSlowFrameDiagnostic(");
        AssertContains(metricsText, "var dxgiSlip = CaptureSlowFrameDxgiSlipSnapshot();");
        AssertContains(metricsText, "DxgiMissedRefreshCount = dxgiSlip.MissedRefreshCount");
        AssertContains(metricsText, "private readonly record struct SlowFrameDxgiSlipSnapshot(");
        AssertContains(metricsText, "private SlowFrameDxgiSlipSnapshot CaptureSlowFrameDxgiSlipSnapshot()");
        AssertContains(metricsText, "frameStatisticsLastSampleFrameCounter == frameStatisticsFrameCounter");
        AssertContains(metricsText, "private static string BuildSlowFrameDiagnosticReason(");
        AssertContains(metricsText, "private static void AppendSlowFrameReason(");
        AssertContains(metricsText, "\"dxgi_refresh_slip\"");
        AssertDoesNotContain(rootText, "private readonly object _slowFrameDiagnosticsLock = new();");
        AssertDoesNotContain(rootText, "new PreviewSlowFrameDiagnostic[64]");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_MetricTrackingLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var metricsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs")
            .Replace("\r\n", "\n");

        AssertContains(metricsText, "private readonly object _presentCadenceLock = new();");
        AssertContains(metricsText, "private double[] _presentIntervalWindowMs = new double[1200];");
        AssertContains(metricsText, "public PresentCadenceMetrics GetPresentCadenceMetrics(double expectedIntervalMs)");
        AssertContains(metricsText, "public double[] GetRecentPresentIntervalsMs(int maxSamples)");
        AssertContains(metricsText, "private readonly object _pipelineLatencyLock = new();");
        AssertContains(metricsText, "private double[] _pipelineLatencyWindowMs = new double[1200];");
        AssertContains(metricsText, "private readonly object _renderCpuTimingLock = new();");
        AssertContains(metricsText, "private readonly object _frameLatencyWaitTimingLock = new();");
        AssertContains(metricsText, "private long _frameLatencyWaitCallCount;");
        AssertContains(metricsText, "public RenderCpuTimingMetrics GetRenderCpuTimingMetrics()");
        AssertContains(metricsText, "public FrameLatencyWaitMetrics GetFrameLatencyWaitMetrics()");
        AssertContains(metricsText, "private long _lastPresentTick;");
        AssertContains(metricsText, "private int _presentCadenceBaselinePending;");
        AssertContains(metricsText, "private double TrackPresentCadence(bool countSample)");
        AssertContains(metricsText, "private void TrackPipelineLatency(long arrivalTick, long estimatedVisibleTick)");
        AssertContains(metricsText, "private void TrackRenderCpuTiming(");
        AssertContains(metricsText, "private void TrackFrameLatencyWait(uint result, long waitTicks)");
        AssertContains(metricsText, "public void SetExpectedFrameRate(double fps)");
        AssertContains(metricsText, "private void ResetPresentCadence()");
        AssertContains(metricsText, "var targetSize = Math.Max(600, (int)Math.Ceiling(fps * CadenceWindowSeconds));");
        AssertContains(metricsText, "Array.Clear(_slowFrameDiagnostics, 0, _slowFrameDiagnostics.Length);");
        AssertContains(metricsText, "private static double[] CopyRecentRing(double[] window, int count, int index, int maxSamples)");
        AssertContains(metricsText, "private static CpuStageTimingMetrics SummarizeCpuStageTiming(double[] samples)");
        AssertContains(metricsText, "private static double TicksToMs(long ticks)");
        AssertContains(metricsText, "private static bool IsValidRenderCpuStageMs(double value)");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.PresentCadenceMetrics.cs")),
            "Present cadence metrics folded into renderer metrics owner");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.MetricTypes.cs")),
            "Renderer metric model types folded into renderer metrics owner");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.MetricsTracking.cs")),
            "Metric tracking folded into renderer metrics owner");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.MetricWindows.cs")),
            "Metric window lifecycle folded into renderer metrics owner");
        AssertDoesNotContain(rootText, "private readonly object _presentCadenceLock = new();");
        AssertDoesNotContain(rootText, "private long _lastPresentTick;");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_PanelBindingLivesWithRendererFacade()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var panelBindingText = rootText;

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.PanelBinding.cs")),
            "D3D11 preview panel binding folded into renderer facade");
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
        AssertContains(rootText, "private int _swapChainBound;");
        AssertContains(rootText, "private int _compositionTransformDirty;");
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
        var deviceInitializationText = resourcesText;
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
            "D3D11 preview swap-chain setup folded into D3D resource ownership");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.DeviceInitialization.cs")),
            "D3D11 preview device initialization folded into D3D resource ownership");
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
        AssertContains(resourcesText, "private void InitializeD3D()");
        AssertContains(deviceInitializationText, "private bool TryInitializeWithSharedDevice(");
        AssertContains(deviceInitializationText, "private void HandleDeviceLost(Exception ex)");
        AssertContains(deviceInitializationText, "private static bool IsDeviceLostException(Exception ex)");
        AssertDoesNotContain(rootText, "private ID3D11Device? _device;");
        AssertDoesNotContain(rootText, "private IDXGISwapChain1? _swapChain;");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_SharedDeviceLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var deviceInitializationText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var renderLifecycleText = rootText;
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
        AssertContains(renderLifecycleText, "Interlocked.CompareExchange(ref _sharedDeviceResetPending, 0, 1)");
        AssertContains(renderLifecycleText, "HandlePendingSharedDeviceResetOnRenderThread();");
        AssertContains(renderLifecycleText, "private void HandlePendingSharedDeviceResetOnRenderThread()");
        AssertContains(renderLifecycleText, "TrackFrameDropped(stale, \"shared-device-reset\");");
        AssertContains(renderLifecycleText, "CleanupD3DResources();");
        AssertContains(renderLifecycleText, "InitializeD3D();");
        AssertDoesNotContain(rootText, "public void SetSharedDevice(ID3D11Device sharedDevice)");
        AssertDoesNotContain(rootText, "public void RetireSharedDeviceReferenceForReinit()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.SharedDevice.cs")),
            "shared D3D device lifecycle folded into D3D resource ownership");

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

    internal static Task D3D11PreviewRenderer_RenderPassesOwnInputUpload()
    {
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");

        AssertContains(renderPassesText, "private bool TryResolveInputView(PendingFrame frame, out ID3D11VideoProcessorInputView? inputView, out bool disposeInputView)");
        AssertContains(renderPassesText, "private ID3D11VideoProcessorInputView CreateInputViewFromTexture(ID3D11Texture2D texture, int subresourceIndex)");
        AssertContains(renderPassesText, "inputView = CreateInputViewFromTexture(frame.D3DTexture, frame.D3DSubresourceIndex);");
        AssertContains(renderPassesText, "UploadRawFrameToTexture(frame.RawData, frame.RawDataLength");
        AssertContains(renderPassesText, "private bool _loggedDirectUploadFallback;");
        AssertContains(renderPassesText, "private unsafe bool UploadRawFrameToTexture(");
        AssertContains(renderPassesText, "private unsafe bool TryUpdateRawFrameTexture(");
        AssertContains(renderPassesText, "private unsafe bool UploadRawFrameViaStaging(");
        AssertContains(renderPassesText, "_deviceContext.UpdateSubresource(");
        AssertContains(renderPassesText, "_deviceContext.CopyResource(inputTexture, stagingTexture);");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.RawFrameUpload.cs")),
            "Raw frame upload helpers folded into render-pass owner");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.FrameUpload.cs")),
            "Frame upload helpers folded into render-pass owner");

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

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.FrameLatency.cs")),
            "D3D11 waitable frame-latency pacing lives with render-thread execution");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.RenderThread.cs")),
            "D3D11 render-thread execution and frame-latency pacing are folded into the renderer root");
        AssertContains(rootText, "private IntPtr _frameLatencyWaitHandle;");
        AssertContains(rootText, "private void ConfigureFrameLatencyWaitableObject()");
        AssertContains(rootText, "private void WaitForFrameLatencySignal()");
        AssertContains(rootText, "TrackFrameLatencyWait(result, Stopwatch.GetTimestamp() - waitStart);");
        AssertContains(rootText, "private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);");
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
        var renderLifecycleText = rootText;
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
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
        AssertContains(renderLifecycleText, "private bool TryEnterNativeRenderCall()");
        AssertContains(renderLifecycleText, "private void ExitNativeRenderCall()");
        AssertContains(renderLifecycleText, "Interlocked.Exchange(ref _inNativeCall, 1);");
        AssertContains(renderLifecycleText, "Interlocked.Exchange(ref _inNativeCall, 0);");
        AssertContains(renderLifecycleText, "ProcessRenderThreadFrameOrIdle()");
        AssertContains(renderLifecycleText, "RenderFrame(frame);");
        AssertDoesNotContain(rootText, "private void RenderFrame(PendingFrame frame)");
        AssertDoesNotContain(resourcesText, "private void RenderNv12WithShader(PendingFrame frame)");
        AssertDoesNotContain(resourcesText, "private void RenderHdrFrameWithShader(PendingFrame frame, ID3D11PixelShader pixelShader)");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_ShaderResourcesLiveWithD3DResources()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.ShaderRendering.cs")),
            "shader rendering resources folded into D3D resource owner");
        AssertContains(resourcesText, "private ID3D11VertexShader? _fullscreenVS;");
        AssertContains(resourcesText, "private ID3D11PixelShader? _nv12PS;");
        AssertContains(resourcesText, "private ID3D11PixelShader? _hdrPassthroughPS;");
        AssertContains(resourcesText, "private readonly VideoProcessorStream[] _vpStreamArray = new VideoProcessorStream[1];");
        AssertContains(resourcesText, "private bool TryEnsureNv12ShaderResources(PendingFrame frame)");
        AssertContains(resourcesText, "private void DisposeNv12ShaderResourceViews()");
        AssertContains(resourcesText, "private void DisposeShaderPipelineResources()");
        AssertContains(resourcesText, "private static readonly ID3D11ClassInstance[] EmptyClassInstances");
        AssertContains(renderPassesText, "PreviewShaderSources.RendererModeNv12");
        AssertContains(renderPassesText, "RendererModeHdrPassthrough");
        AssertContains(renderPassesText, "private bool _loggedHdrShaderFallback;");
        AssertDoesNotContain(rootText, "private ID3D11VertexShader? _fullscreenVS;");
        AssertDoesNotContain(rootText, "private readonly VideoProcessorStream[] _vpStreamArray = new VideoProcessorStream[1];");
        AssertDoesNotContain(renderPassesText, "private bool TryEnsureNv12ShaderResources(PendingFrame frame)");
        AssertDoesNotContain(resourcesText, "private bool _loggedHdrShaderFallback;");
        AssertDoesNotContain(resourcesText, "private int _lastNv12IsHdr = -1;");
        AssertContains(resourcesText, "_linearSampler?.Dispose();");
        AssertContains(resourcesText, "_nv12PS?.Dispose();");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_ShaderCompilationLivesInFocusedFiles()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var previewShaderSourcesText = resourcesText;

        AssertContains(previewShaderSourcesText, "internal static class PreviewShaderSources");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "PreviewShaderSources.cs")),
            "preview shader sources live with D3D resource ownership");
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
            "shader compilation folded into D3D resource owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.ShaderRendering.cs")),
            "shader rendering owner folded into D3D resource owner");
        AssertContains(resourcesText, "private unsafe void CompileTonemapShaders()");
        AssertContains(resourcesText, "PreviewShaderSources.FullscreenVertex");
        AssertContains(resourcesText, "PreviewShaderSources.HdrTonemapPixel");
        AssertContains(resourcesText, "PreviewShaderSources.HdrPassthroughPixel");
        AssertContains(resourcesText, "PreviewShaderSources.Nv12Pixel");
        AssertContains(resourcesText, "private interface ID3DBlob");
        AssertContains(resourcesText, "private static extern int D3DCompileNative(");
        AssertContains(resourcesText, "private static byte[] CompileShader(string hlslSource, string entryPoint, string profile)");
        AssertContains(resourcesText, "private static byte[] ReadBlobBytes(IntPtr blobPtr)");
        AssertContains(resourcesText, "private static string ReadBlobString(IntPtr blobPtr)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.NativeInterop.cs")),
            "shader compiler interop folded into D3D resource owner");

        AssertDoesNotContain(rootText, "internal const string FullscreenVertex");
        AssertDoesNotContain(rootText, "static const float PQ_m1");
        AssertDoesNotContain(renderPassesText, "internal const string HdrTonemapPixel");
        AssertDoesNotContain(renderPassesText, "BT2020_to_BT709");

        return Task.CompletedTask;
    }
}
