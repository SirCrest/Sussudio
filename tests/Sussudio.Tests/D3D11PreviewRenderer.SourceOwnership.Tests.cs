using System.Diagnostics;
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
    internal static Task D3D11PreviewRenderer_SubmissionLivesWithRendererRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var submissionText = rootText;
        var nv12SubmissionText = rootText;

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Submission.cs")),
            "D3D11 preview submission folded into the renderer root lifecycle owner");
        AssertContains(nv12SubmissionText, "private bool _loggedNv12ShaderMissing;");
        AssertContains(nv12SubmissionText, "private int _lastNv12IsHdr = -1;");
        AssertContains(submissionText, "private readonly ManualResetEventSlim _frameReadyEvent = new(false);");
        AssertContains(submissionText, "private readonly ConcurrentQueue<PendingFrame> _pendingFrames = new();");
        AssertContains(submissionText, "private sealed class PendingFrame : IDisposable");
        AssertContains(submissionText, "FrameLease?.Dispose();");
        AssertContains(submissionText, "private int _pendingFrameCount;");
        AssertContains(submissionText, "public void SubmitRawFrame(");
        AssertContains(submissionText, "public void SubmitRawFrameLease(");
        AssertContains(submissionText, "public void SubmitTexture(");
        AssertContains(submissionText, "public void SubmitNv12PlaneTextures(");
        AssertContains(submissionText, "private void EnqueueNv12Frame(");
        AssertContains(submissionText, "EnqueuePendingFrame(frame);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Nv12Submission.cs")),
            "NV12 texture submission folded into the D3D11 preview renderer root");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.PendingFrames.cs")),
            "pending-frame queue folded into the D3D11 preview renderer root");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_PublicLifecycleLivesInRendererRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Lifecycle.cs")),
            "D3D11 preview public lifecycle is consolidated into the renderer root facade");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.RenderThread.cs")),
            "D3D11 preview render thread lifecycle is consolidated into the renderer root facade");
        AssertContains(rootText, "private readonly object _lifecycleLock = new();");
        AssertContains(rootText, "private Thread? _renderThread;");
        AssertContains(rootText, "private int _disposed;");
        AssertContains(rootText, "private double _startupFps = 60.0;");
        AssertContains(rootText, "public void Start(int width, int height, double fps, bool isHdr)");
        AssertContains(rootText, "public void Dispose()");
        AssertContains(rootText, "private int _stopRequested;");
        AssertContains(rootText, "private int _inNativeCall;");
        AssertContains(rootText, "public void StopRenderThread()");
        AssertContains(rootText, "public void Stop()");
        AssertContains(rootText, "private void WaitForNativeCallToDrainOrThrow(string operation)");
        AssertContains(rootText, "WaitForNativeCallToDrainOrThrow(\"stop\");");
        AssertContains(rootText, "FailPendingFrameCapture(\"Preview renderer stopped before frame capture completed.\");");
        AssertContains(rootText, "WinRT.CastExtensions.As<ISwapChainPanelNative>(_panel)");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_ScreenshotEncodingLivesWithScreenshotCapture()
    {
        var captureText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var previewScreenshotCaptureText = ReadRepoFile("Sussudio/Services/Preview/PreviewScreenshotCapture.cs")
            .Replace("\r\n", "\n");
        var previewPngEncoderText = previewScreenshotCaptureText;

        AssertContains(captureText, "private void TryCaptureFrameBeforePresent(string rendererMode)");
        AssertContains(captureText, "public Task<PreviewFrameCaptureResult> CaptureNextFrameAsync(string outputPath, CancellationToken cancellationToken)");
        AssertContains(captureText, "private const int FrameCaptureTimeoutMs = 5000;");
        AssertContains(captureText, "private TaskCompletionSource<PreviewFrameCaptureResult>? _frameCaptureRequest;");
        AssertContains(captureText, "private void FailPendingFrameCapture(string message)");
        AssertContains(captureText, "if (IsPngFrameCaptureCompletionInProgress())");
        AssertContains(captureText, "EnsureFrameCaptureStagingTexture(backBufferDescription, width, height)");
        AssertContains(captureText, "BeginPngFrameCaptureCompletion(");
        AssertContains(captureText, "TryBeginPngFrameCaptureCompletion()");
        AssertContains(captureText, "EndPngFrameCaptureCompletion();");
        AssertContains(captureText, "LogFrameCaptureResult(captureResult);");
        AssertContains(captureText, "LogFrameCaptureFailure(ex, rendererMode);");
        AssertContains(captureText, "PreviewScreenshotCapture.CopyMappedFrameToBuffer(");
        AssertContains(captureText, "PreviewScreenshotCapture.CaptureMappedFrameToBmp(");
        AssertContains(captureText, "private void BeginPngFrameCaptureCompletion(");
        AssertContains(captureText, "private int _frameCaptureEncodeInProgress;");
        AssertContains(captureText, "private bool IsPngFrameCaptureCompletionInProgress()");
        AssertContains(captureText, "private bool TryBeginPngFrameCaptureCompletion()");
        AssertContains(captureText, "private void EndPngFrameCaptureCompletion()");
        AssertContains(captureText, "PreviewScreenshotCapture.CaptureFrameBufferTo16BitPng(");
        AssertContains(captureText, "Interlocked.Exchange(ref _frameCaptureEncodeInProgress, 0);");
        AssertContains(captureText, "private static PreviewFrameCaptureResult CreateFrameCaptureError(");
        AssertContains(captureText, "private static void LogFrameCaptureResult(PreviewFrameCaptureResult captureResult)");
        AssertContains(captureText, "private static void LogFrameCaptureFailure(Exception ex, string rendererMode)");
        AssertContains(captureText, "LuminanceHistogram = new int[16]");
        AssertContains(resourcesText, "DisposeFrameCaptureStagingResources();");
        AssertContains(previewScreenshotCaptureText, "internal static PreviewFrameCaptureResult CaptureMappedFrameToBmp(");
        AssertContains(previewScreenshotCaptureText, "internal static PreviewFrameCaptureResult CaptureFrameBufferTo16BitPng(");
        AssertContains(previewScreenshotCaptureText, "internal static byte[] CopyMappedFrameToBuffer(");
        AssertContains(previewScreenshotCaptureText, "private sealed class PreviewScreenshotPixelAnalysis");
        AssertContains(previewScreenshotCaptureText, "analysis.AnalyzePixel(");
        AssertContains(previewScreenshotCaptureText, "private static void WriteBitmapHeaders(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.ScreenshotEncoding.cs")),
            "renderer screenshot encoding partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.ScreenshotCapture.cs")),
            "renderer screenshot capture folded into the render-pass present transaction owner");
        AssertDoesNotContain(captureText, "private static PreviewFrameCaptureResult CaptureMappedFrameToBmp(");
        AssertDoesNotContain(captureText, "private static void WriteBitmapHeaders(");
        AssertDoesNotContain(resourcesText, "_captureStagingTexture?.Dispose();");
        AssertContains(previewScreenshotCaptureText, "PreviewPng16Encoder.WriteCompressedRgb16Png(");
        AssertContains(previewScreenshotCaptureText, "internal static class PreviewScreenshotCapture");
        AssertContains(previewPngEncoderText, "internal static class PreviewPng16Encoder");
        AssertContains(previewPngEncoderText, "internal static void WriteCompressedRgb16Png(");
        AssertContains(previewPngEncoderText, "internal static uint[] InitPngCrc32Table()");
        AssertContains(previewPngEncoderText, "private static void WritePngChunk(");
        AssertContains(previewPngEncoderText, "private static uint UpdatePngCrc32(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "PreviewPng16Encoder.cs")),
            "16-bit PNG encoder folded into PreviewScreenshotCapture.cs");
        AssertContains(captureText, "private ID3D11Texture2D? _captureStagingTexture;");
        AssertContains(captureText, "private ID3D11Texture2D EnsureFrameCaptureStagingTexture(");
        AssertContains(captureText, "_captureStagingTexture = _device!.CreateTexture2D(");
        AssertContains(captureText, "private void DisposeFrameCaptureStagingResources()");
        AssertContains(captureText, "_captureStagingTexture?.Dispose();");
        AssertContains(resourcesText, "DisposeFrameCaptureStagingResources();");
        AssertDoesNotContain(resourcesText, "_captureStagingTexture?.Dispose();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "PreviewScreenshotCapture.Png.cs")),
            "preview PNG capture is consolidated into PreviewScreenshotCapture.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "PreviewScreenshotCapture.Bmp.cs")),
            "preview BMP capture is consolidated into PreviewScreenshotCapture.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.ScreenshotStaging.cs")),
            "renderer screenshot staging is consolidated into D3D11PreviewRenderer.RenderPasses.cs");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_BlackEdgeCounting_WorksCorrectly()
    {
        var captureType = RequireType("Sussudio.Services.Preview.PreviewScreenshotCapture");

        var leadingMethod = captureType.GetMethod("CountLeadingBlackEdges",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("CountLeadingBlackEdges not found.");
        var trailingMethod = captureType.GetMethod("CountTrailingBlackEdges",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("CountTrailingBlackEdges not found.");

        var values1 = new[] { true, true, false, true, false };
        AssertEqual(2, (int)leadingMethod.Invoke(null, new object[] { values1 })!, "Leading: 2 black edges");
        AssertEqual(0, (int)trailingMethod.Invoke(null, new object[] { values1 })!, "Trailing: 0 black edges");

        var values2 = new[] { false, false, true, true, true };
        AssertEqual(0, (int)leadingMethod.Invoke(null, new object[] { values2 })!, "Leading: 0");
        AssertEqual(3, (int)trailingMethod.Invoke(null, new object[] { values2 })!, "Trailing: 3");

        var allTrue = new[] { true, true, true, true, true };
        AssertEqual(5, (int)leadingMethod.Invoke(null, new object[] { allTrue })!, "All true leading");
        AssertEqual(5, (int)trailingMethod.Invoke(null, new object[] { allTrue })!, "All true trailing");

        var allFalse = new[] { false, false, false };
        AssertEqual(0, (int)leadingMethod.Invoke(null, new object[] { allFalse })!, "All false leading");
        AssertEqual(0, (int)trailingMethod.Invoke(null, new object[] { allFalse })!, "All false trailing");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_InitPngCrc32Table_Generates256Entries()
    {
        var encoderType = RequireType("Sussudio.Services.Preview.PreviewPng16Encoder");
        var method = encoderType.GetMethod("InitPngCrc32Table",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("InitPngCrc32Table not found.");

        var table = (uint[])method.Invoke(null, null)!;
        AssertEqual(256, table.Length, "CRC32 table has 256 entries");
        AssertEqual(0u, table[0], "CRC32 table[0] = 0");

        var unique = new HashSet<uint>(table);
        AssertEqual(256, unique.Count, "All 256 entries are unique");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_PreviewPngCapture_Writes16BitRgbPng()
    {
        var captureType = RequireType("Sussudio.Services.Preview.PreviewScreenshotCapture");
        var method = captureType.GetMethod(
            "CaptureFrameBufferTo16BitPng",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new InvalidOperationException("CaptureFrameBufferTo16BitPng not found.");

        var outputRoot = Path.Combine(Path.GetTempPath(), "sussudio-preview-png-test-" + Guid.NewGuid().ToString("N"));
        var outputPath = Path.Combine(outputRoot, "preview", "frame.png");
        try
        {
            var format = ParseEnum("Vortice.DXGI.Format", "B8G8R8A8_UNorm");
            var result = method.Invoke(
                null,
                new object[]
                {
                    new byte[] { 0x30, 0x20, 0x10, 0xFF },
                    4,
                    1,
                    1,
                    outputPath,
                    "UnitTest",
                    format
                })
                ?? throw new InvalidOperationException("CaptureFrameBufferTo16BitPng returned null.");

            AssertEqual(true, GetBoolProperty(result, "Succeeded"), "PNG capture succeeded");
            AssertEqual(1, GetIntProperty(result, "CapturedWidth"), "PNG captured width");
            AssertEqual(1, GetIntProperty(result, "CapturedHeight"), "PNG captured height");
            AssertEqual(outputPath, GetStringProperty(result, "FilePath"), "PNG output path");

            var bytes = File.ReadAllBytes(outputPath);
            AssertEqual(137, (int)bytes[0], "PNG signature byte 0");
            AssertEqual(80, (int)bytes[1], "PNG signature byte 1");
            AssertEqual(78, (int)bytes[2], "PNG signature byte 2");
            AssertEqual(71, (int)bytes[3], "PNG signature byte 3");
            AssertEqual((byte)'I', bytes[12], "PNG IHDR I");
            AssertEqual((byte)'H', bytes[13], "PNG IHDR H");
            AssertEqual((byte)'D', bytes[14], "PNG IHDR D");
            AssertEqual((byte)'R', bytes[15], "PNG IHDR R");
            AssertEqual(1, (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19], "PNG IHDR width");
            AssertEqual(1, (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23], "PNG IHDR height");
            AssertEqual(16, (int)bytes[24], "PNG bit depth");
            AssertEqual(2, (int)bytes[25], "PNG color type");
        }
        finally
        {
            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, recursive: true);
            }
        }

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_RenderThreadLivesInRendererRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var diagnosticsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.RenderThread.cs")),
            "D3D11 preview render-thread orchestration is folded into the renderer root");
        AssertContains(rootText, "private void RenderThreadMain()");
        AssertContains(rootText, "MmcssThreadRegistration.TryRegister");
        AssertContains(rootText, "_frameReadyEvent.Wait");
        AssertContains(rootText, "HandlePendingSharedDeviceResetOnRenderThread();");
        AssertContains(rootText, "TryApplyPendingCompositionTransformOnRenderThread(out var skipFrameDispatch)");
        AssertContains(rootText, "if (skipFrameDispatch)");
        AssertContains(rootText, "ProcessRenderThreadFrameOrIdle()");
        AssertContains(rootText, "CleanupRenderThreadExit();");
        AssertContains(rootText, "NotifyRenderThreadFailed(ex);");
        AssertContains(rootText, "private void HandlePendingSharedDeviceResetOnRenderThread()");
        AssertContains(rootText, "TrackFrameDropped(stale, \"shared-device-reset\");");
        AssertContains(rootText, "UnbindSwapChainFromPanel();");
        AssertContains(rootText, "InitializeD3D();");
        AssertContains(rootText, "private bool TryApplyPendingCompositionTransformOnRenderThread(out bool skipFrameDispatch)");
        AssertContains(rootText, "skipFrameDispatch = true;");
        AssertContains(rootText, "if (Volatile.Read(ref _stopRequested) != 0)");
        AssertContains(rootText, "ApplyCompositionScaleTransform(swapChain);");
        AssertContains(rootText, "HandleDeviceLost(ex);");
        AssertContains(rootText, "private bool ProcessRenderThreadFrameOrIdle()");
        AssertContains(rootText, "WaitForFrameLatencySignal();");
        AssertContains(rootText, "RenderFrame(frame);");
        AssertContains(rootText, "TrackFrameDropped(frame, \"render-failed\");");
        AssertContains(rootText, "SignalFrameReady(\"render_loop_drain\");");
        AssertContains(rootText, "private void CleanupRenderThreadExit()");
        AssertContains(rootText, "TrackFrameDropped(stale, \"renderer-exit\");");
        AssertContains(rootText, "FailPendingFrameCapture(\"Render thread exited before frame capture completed.\");");
        AssertDoesNotContain(agentMapText, "D3D11PreviewRenderer.RenderThread.cs");
        AssertContains(agentMapText, "D3D11PreviewRenderer.cs");
        AssertContains(agentMapText, "shared-device reset consumption/rebind");
        AssertContains(agentMapText, "queued-frame render dispatch");
        AssertDoesNotContain(cleanupPlanText, "D3D11PreviewRenderer.RenderThread.cs");
        AssertContains(cleanupPlanText, "D3D11PreviewRenderer.cs");
        AssertContains(cleanupPlanText, "shared-device reset/rebind consumption");
        AssertContains(cleanupPlanText, "pending-frame render dispatch");
        AssertContains(diagnosticsText, "private string _lastRenderThreadFailureType = string.Empty;");
        AssertContains(diagnosticsText, "private long _renderThreadFailureCount;");
        AssertContains(diagnosticsText, "private void NotifyRenderThreadFailed(Exception ex)");
        AssertContains(diagnosticsText, "RenderThreadFailed?.Invoke(reason)");
        AssertContains(diagnosticsText, "private int _firstFrameRaised;");
        AssertContains(diagnosticsText, "private void ResetFirstFrameNotification()");
        AssertContains(diagnosticsText, "private void NotifyFirstFrameRendered(string message)");
        AssertContains(diagnosticsText, "FirstFrameRendered?.Invoke()");
        AssertContains(rootText, "ResetFirstFrameNotification();");
        AssertContains(renderPassesText, "NotifyFirstFrameRendered(firstFrameMessage);");
        var waitIndex = rootText.IndexOf("WaitForFrameLatencySignal();", StringComparison.Ordinal);
        var renderIndex = rootText.IndexOf("RenderFrame(frame);", StringComparison.Ordinal);
        if (waitIndex < 0 || renderIndex < 0 || waitIndex > renderIndex)
        {
            throw new InvalidOperationException("Render thread must wait for frame-latency signal before rendering the frame.");
        }

        AssertDoesNotContain(rootText, "private int _firstFrameRaised;");
        AssertDoesNotContain(rootText, "private string _lastRenderThreadFailureType = string.Empty;");
        AssertDoesNotContain(renderPassesText, "private void RenderThreadMain()");
        AssertDoesNotContain(renderPassesText, "private void NotifyRenderThreadFailed(Exception ex)");
        AssertDoesNotContain(renderPassesText, "FirstFrameRendered?.Invoke()");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_IsDeviceLostException_ClassifiesCorrectly()
    {
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var method = rendererType.GetMethod(
            "IsDeviceLostException",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("IsDeviceLostException not found.");

        var regularEx = new InvalidOperationException("test");
        AssertEqual(false, (bool)method.Invoke(null, new object[] { regularEx })!, "Regular exception is not device lost");

        var deviceRemovedEx = new System.Runtime.InteropServices.COMException("Device removed", unchecked((int)0x887A0005));
        AssertEqual(true, (bool)method.Invoke(null, new object[] { deviceRemovedEx })!, "DeviceRemoved COMException is device lost");

        var deviceResetEx = new System.Runtime.InteropServices.COMException("Device reset", unchecked((int)0x887A0007));
        AssertEqual(true, (bool)method.Invoke(null, new object[] { deviceResetEx })!, "DeviceReset COMException is device lost");

        var otherComEx = new System.Runtime.InteropServices.COMException("Other", unchecked((int)0x80004005));
        AssertEqual(false, (bool)method.Invoke(null, new object[] { otherComEx })!, "Other COMException is not device lost");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_DeviceLostRecoveryLivesInFocusedPartial()
    {
        var resourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var deviceInitializationText = resourcesText;

        AssertContains(deviceInitializationText, "private void HandleDeviceLost(Exception ex)");
        AssertContains(deviceInitializationText, "private static bool IsDeviceLostException(Exception ex)");
        AssertContains(deviceInitializationText, "TrackFrameDropped(stalePending, \"device-lost\");");
        AssertContains(deviceInitializationText, "ResultCode.DeviceRemoved");
        AssertContains(deviceInitializationText, "unchecked((int)0x887A0005)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.DeviceLost.cs")),
            "D3D11 preview device-lost recovery folded into D3D resource ownership");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_PresentAccountingLivesWithRenderPasses()
    {
        var renderPassesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Present.cs")),
            "D3D11 preview present/accounting lives with render-pass execution");
        AssertContains(renderPassesText, "private void PresentAndTrackFrame(");
        AssertContains(renderPassesText, "TryCaptureFrameBeforePresent(rendererMode);");
        AssertContains(renderPassesText, "var presentResult = swapChain.Present((uint)_presentSyncInterval, PresentFlags.None);");
        AssertContains(renderPassesText, "TrackPresentCadence(frame.CountForPresentCadence);");
        AssertContains(renderPassesText, "var estimatedVisibleTick = EstimateVisibleTick(presentEnd);");
        AssertContains(renderPassesText, "RecordSlowFrameDiagnostic(frame, presentIntervalMs, inputUploadTicks, renderTicks, presentTicks, totalTicks, presentEnd, estimatedVisibleTick);");
        var captureIndex = renderPassesText.IndexOf("TryCaptureFrameBeforePresent(rendererMode);", StringComparison.Ordinal);
        var presentIndex = renderPassesText.IndexOf("var presentResult = swapChain.Present((uint)_presentSyncInterval, PresentFlags.None);", StringComparison.Ordinal);
        if (captureIndex < 0 || presentIndex < 0 || captureIndex > presentIndex)
        {
            throw new InvalidOperationException("Present transaction must capture screenshots before swap-chain Present.");
        }

        return Task.CompletedTask;
    }


    internal static Task D3D11PreviewRenderer_DropPendingFrames_DrainsQueueAndMarksGeneration()
    {
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var pendingFrameType = rendererType.GetNestedType("PendingFrame", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("PendingFrame nested type not found.");
        var queueType = typeof(System.Collections.Concurrent.ConcurrentQueue<>).MakeGenericType(pendingFrameType);
        var renderer = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(rendererType);
        SetPrivateField(renderer, "_lifecycleLock", new object());
        SetPrivateField(renderer, "_pendingFrames", Activator.CreateInstance(queueType));
        SetPrivateField(renderer, "_frameReadyEvent", new System.Threading.ManualResetEventSlim(false));
        SetPrivateField(renderer, "_renderThread", System.Threading.Thread.CurrentThread);
        SetPrivateField(renderer, "_maxPendingFrames", 4);

        InvokeNonPublicInstanceMethod(
            renderer,
            "EnqueuePendingFrame",
            new[] { CreateRawPendingD3DFrame(pendingFrameType, 101L, 1001L) });
        InvokeNonPublicInstanceMethod(
            renderer,
            "EnqueuePendingFrame",
            new[] { CreateRawPendingD3DFrame(pendingFrameType, 102L, 1002L) });

        AssertEqual(2, Convert.ToInt32(GetPropertyValue(renderer, "PendingFrameCount")), "pending frame count before drain");
        AssertEqual(2L, Convert.ToInt64(GetPropertyValue(renderer, "FramesSubmitted")), "frames submitted before drain");
        AssertEqual(0L, Convert.ToInt64(GetPropertyValue(renderer, "FramesDropped")), "frames dropped before drain");

        var dropMethod = rendererType.GetMethod("DropPendingFrames", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("DropPendingFrames method not found.");
        var dropped = Convert.ToInt32(dropMethod.Invoke(renderer, new object[] { "flashback-go-live" }));

        AssertEqual(2, dropped, "pending frames drained");
        AssertEqual(0, Convert.ToInt32(GetPropertyValue(renderer, "PendingFrameCount")), "pending frame count after drain");
        AssertEqual(2L, Convert.ToInt64(GetPropertyValue(renderer, "FramesDropped")), "frames dropped after drain");
        AssertEqual(1L, GetLongPrivateField(renderer, "_submissionGeneration"), "submission generation after drain");
        AssertEqual("flashback-go-live", GetStringPrivateField(renderer, "_submissionGenerationDropReason"), "submission generation reason");

        var ownership = rendererType.GetMethod("GetFrameOwnershipMetrics", BindingFlags.Public | BindingFlags.Instance)!
            .Invoke(renderer, Array.Empty<object>())
            ?? throw new InvalidOperationException("GetFrameOwnershipMetrics returned null.");
        AssertEqual("flashback-go-live", GetPropertyValue(ownership, "LastDropReason") as string, "last D3D drop reason");
        AssertEqual(1002L, Convert.ToInt64(GetPropertyValue(ownership, "LastDroppedPreviewPresentId")), "last dropped preview present id");
        AssertEqual(102L, Convert.ToInt64(GetPropertyValue(ownership, "LastDroppedSourceSequenceNumber")), "last dropped source sequence");

        var staleFrame = CreateRawPendingD3DFrame(pendingFrameType, 103L, 1003L);
        pendingFrameType.GetProperty("SubmissionGeneration", BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(staleFrame, 0L);
        var staleGeneration = Convert.ToInt64(pendingFrameType.GetProperty("SubmissionGeneration")!.GetValue(staleFrame));
        AssertEqual(true, staleGeneration != GetLongPrivateField(renderer, "_submissionGeneration"), "stale frame generation is rejected by render loop contract");
        ((IDisposable)staleFrame).Dispose();

        return Task.CompletedTask;

        static object CreateRawPendingD3DFrame(Type pendingFrameType, long sourceSequenceNumber, long previewPresentId)
        {
            var constructor = pendingFrameType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .Single(ctor => ctor.GetParameters().Any(parameter => parameter.Name == "rawData"));
            var args = constructor.GetParameters()
                .Select(parameter =>
                {
                    if (string.Equals(parameter.Name, "rawData", StringComparison.Ordinal))
                    {
                        return null;
                    }

                    if (string.Equals(parameter.Name, "rawDataLength", StringComparison.Ordinal))
                    {
                        return 0;
                    }

                    if (string.Equals(parameter.Name, "width", StringComparison.Ordinal) ||
                        string.Equals(parameter.Name, "height", StringComparison.Ordinal))
                    {
                        return 16;
                    }

                    if (string.Equals(parameter.Name, "isHdr", StringComparison.Ordinal))
                    {
                        return false;
                    }

                    if (string.Equals(parameter.Name, "arrivalTick", StringComparison.Ordinal) ||
                        string.Equals(parameter.Name, "schedulerSubmitTick", StringComparison.Ordinal))
                    {
                        return Stopwatch.GetTimestamp();
                    }

                    if (string.Equals(parameter.Name, "sourceSequenceNumber", StringComparison.Ordinal))
                    {
                        return sourceSequenceNumber;
                    }

                    if (string.Equals(parameter.Name, "previewPresentId", StringComparison.Ordinal))
                    {
                        return previewPresentId;
                    }

                    return parameter.ParameterType.IsValueType
                        ? Activator.CreateInstance(parameter.ParameterType)
                        : null;
                })
                .ToArray();
            return constructor.Invoke(args)
                   ?? throw new InvalidOperationException("PendingFrame constructor returned null.");
        }
    }

    internal static Task D3D11PreviewRenderer_FrameCaptureCancellationClearsPendingRequest()
    {
        var rendererText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            .Replace("\r\n", "\n");
        var captureMethod = ExtractTextBetween(
            rendererText,
            "public Task<PreviewFrameCaptureResult> CaptureNextFrameAsync(string outputPath, CancellationToken cancellationToken)",
            "    private void TryCaptureFrameBeforePresent");
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Snapshots.cs")
                .Replace("\r\n", "\n");

        AssertContains(captureMethod, "if (cancellationToken.IsCancellationRequested)");
        AssertContains(captureMethod, "Preview frame capture canceled.");
        AssertContains(captureMethod, "CancellationTokenRegistration cancellationRegistration = default;");
        AssertContains(captureMethod, "cancellationToken.Register(");
        AssertContains(captureMethod, "Interlocked.CompareExchange(ref renderer._frameCaptureRequest, null, request)");
        AssertContains(captureMethod, "Interlocked.Exchange(ref renderer._frameCaptureOutputPath, null);");
        AssertContains(captureMethod, "PREVIEW_FRAME_CAPTURE_CANCELED");
        AssertContains(captureMethod, "_ = request.Task.ContinueWith(");
        AssertContains(captureServiceText, "return await d3dSink.CaptureNextFrameAsync(outputPath, cancellationToken).ConfigureAwait(false);");
        AssertContains(captureServiceText, "while (_isVideoPreviewActive && !cancellationToken.IsCancellationRequested)");
        AssertDoesNotContain(captureServiceText, "cancellationToken.ThrowIfCancellationRequested();\n        return d3dSink.CaptureNextFrameAsync(outputPath);");

        return Task.CompletedTask;
    }

    internal static Task SharedD3DDeviceManager_DuplicatesReferencesUnderLifecycleLock()
    {
        var managerType = RequireType("Sussudio.Services.Preview.SharedD3DDeviceManager");
        AssertNotNull(
            managerType.GetMethod("TryCreateDeviceReference", BindingFlags.Public | BindingFlags.Instance),
            "SharedD3DDeviceManager.TryCreateDeviceReference");

        var managerText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            .Replace("\r\n", "\n");
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceRecordingFinalizationSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
                .Replace("\r\n", "\n");
        var duplicateMethod = ExtractTextBetween(
            managerText,
            "public bool TryCreateDeviceReference",
            "\n    public void Dispose()");
        var disposeMethod = ExtractTextBetween(
            managerText,
            "public void Dispose()",
            "\n    private void Initialize()");
        var applyMethod = ExtractTextBetween(
            captureServiceText,
            "private void TryApplySharedPreviewDevice",
            "\n    private async Task DisposeTransientRecordingBackendAsync");

        AssertContains(managerText, "private readonly object _sync = new();");
        AssertContains(duplicateMethod, "lock (_sync)");
        AssertContains(duplicateMethod, "if (Volatile.Read(ref _disposed) != 0)");
        AssertContains(duplicateMethod, "var nativePointer = currentDevice.NativePointer;");
        AssertContains(duplicateMethod, "Marshal.AddRef(nativePointer);");
        AssertContains(duplicateMethod, "device = new ID3D11Device(nativePointer);");
        AssertContains(disposeMethod, "lock (_sync)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "SharedD3DDeviceManager.cs")),
            "shared D3D device manager lives with D3D resource ownership");
        AssertContains(applyMethod, "d3dManager.TryCreateDeviceReference(out var sharedDevice, out var reason)");
        AssertContains(applyMethod, "UNIFIED_VIDEO_SHARED_DEVICE_APPLY_SKIP reason={reason}");
        AssertContains(applyMethod, "sharedDevice.Dispose();");
        AssertDoesNotContain(applyMethod, "capture.D3DManager?.Device");

        return Task.CompletedTask;
    }
}
