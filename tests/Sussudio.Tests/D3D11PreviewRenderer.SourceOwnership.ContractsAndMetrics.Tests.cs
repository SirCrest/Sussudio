using System.IO;
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
        var panelBindingText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.PanelBinding.cs")
            .Replace("\r\n", "\n");
        var shaderRenderingText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderRendering.cs")
            .Replace("\r\n", "\n");
        var dxgiStatisticsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.DxgiFrameStatistics.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.NativeInterop.cs")),
            "mixed native interop bucket retired into behavior owners");
        AssertContains(panelBindingText, "private interface ISwapChainPanelNative");
        AssertContains(panelBindingText, "WinRT.CastExtensions.As<ISwapChainPanelNative>(_panel)");
        AssertContains(shaderRenderingText, "private interface ID3DBlob");
        AssertContains(shaderRenderingText, "private static extern int D3DCompileNative(");
        AssertContains(shaderRenderingText, "private static byte[] CompileShader(string hlslSource, string entryPoint, string profile)");
        AssertContains(shaderRenderingText, "private static string ReadBlobString(IntPtr blobPtr)");
        AssertContains(dxgiStatisticsText, "private static extern int DwmFlush()");
        AssertContains(dxgiStatisticsText, "_ = DwmFlush();");
        AssertDoesNotContain(rootText, "private interface ISwapChainPanelNative");
        AssertDoesNotContain(rootText, "private interface ID3DBlob");
        AssertDoesNotContain(rootText, "D3DCompileNative(");
        AssertDoesNotContain(rootText, "private static extern int DwmFlush()");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_FrameTypesLiveWithPendingFrameQueue()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var pendingFramesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.PendingFrames.cs")
            .Replace("\r\n", "\n");
        var metricsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs")
            .Replace("\r\n", "\n");

        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.PendingFrame.cs")), "pending-frame lifetime model stays folded into PendingFrames.cs");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.MetricTypes.cs")), "renderer metric model types folded into Metrics.cs");
        AssertContains(pendingFramesText, "private sealed class PendingFrame : IDisposable");
        AssertContains(pendingFramesText, "ArrayPool<byte>.Shared.Return(RawData);");
        AssertContains(pendingFramesText, "FrameLease?.Dispose();");
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
        AssertDoesNotContain(rootText, "private sealed class PendingFrame : IDisposable");
        AssertDoesNotContain(rootText, "public readonly record struct PresentCadenceMetrics(");
        AssertDoesNotContain(rootText, "public readonly record struct DxgiFrameStatisticsMetrics(");
        AssertDoesNotContain(pendingFramesText, "public readonly record struct PresentCadenceMetrics(");
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

    internal static Task D3D11PreviewRenderer_DxgiFrameStatisticsLiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var metricsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs")
            .Replace("\r\n", "\n");
        var dxgiText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.DxgiFrameStatistics.cs")
            .Replace("\r\n", "\n");

        AssertContains(dxgiText, "private readonly object _dxgiFrameStatisticsLock = new();");
        AssertContains(dxgiText, "private long _dxgiFrameStatisticsSampleCount;");
        AssertContains(dxgiText, "private long _dxgiFrameStatisticsMissedRefreshCount;");
        AssertContains(dxgiText, "private long _dxgiFrameStatisticsLastSampleFrameCounter;");
        AssertContains(dxgiText, "private long _dxgiFrameStatisticsPresentCount = -1;");
        AssertContains(dxgiText, "private bool _dxgiFrameStatisticsHasBaseline;");
        AssertContains(dxgiText, "public DxgiFrameStatisticsMetrics GetDxgiFrameStatisticsMetrics()");
        AssertContains(dxgiText, "private void TrackDxgiFrameStatistics()");
        AssertContains(dxgiText, "_ = DwmFlush();");
        AssertContains(dxgiText, "_swapChain.GetFrameStatistics(out var stats)");
        AssertContains(dxgiText, "private long EstimateVisibleTick(long presentReturnTick)");
        AssertContains(dxgiText, "private long GetEstimatedDisplayFrameIntervalTicks()");
        AssertContains(dxgiText, "public bool TryGetDisplayClock(out PreviewDisplayClockSnapshot snapshot)");
        AssertContains(dxgiText, "new PreviewDisplayClockSnapshot(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.DisplayClock.cs")),
            "D3D11 preview display-clock projection lives with DXGI frame statistics");
        AssertDoesNotContain(rootText, "private readonly object _dxgiFrameStatisticsLock = new();");
        AssertDoesNotContain(rootText, "private long _dxgiFrameStatisticsPresentCount = -1;");
        AssertDoesNotContain(metricsText, "public DxgiFrameStatisticsMetrics GetDxgiFrameStatisticsMetrics()");
        AssertDoesNotContain(metricsText, "private void TrackDxgiFrameStatistics()");
        AssertDoesNotContain(metricsText, "private long EstimateVisibleTick(long presentReturnTick)");
        AssertDoesNotContain(metricsText, "public bool TryGetDisplayClock(out PreviewDisplayClockSnapshot snapshot)");

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
}
