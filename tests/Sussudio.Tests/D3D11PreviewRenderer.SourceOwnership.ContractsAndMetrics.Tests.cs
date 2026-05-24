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

    internal static Task D3D11PreviewRenderer_NativeInteropLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var nativeInteropText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.NativeInterop.cs")
            .Replace("\r\n", "\n");
        var panelBindingText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.PanelBinding.cs")
            .Replace("\r\n", "\n");
        var dxgiStatisticsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.DxgiFrameStatistics.cs")
            .Replace("\r\n", "\n");

        AssertContains(nativeInteropText, "private interface ISwapChainPanelNative");
        AssertContains(nativeInteropText, "private interface ID3DBlob");
        AssertContains(nativeInteropText, "private static extern int D3DCompileNative(");
        AssertContains(nativeInteropText, "private static extern int DwmFlush()");
        AssertContains(panelBindingText, "WinRT.CastExtensions.As<ISwapChainPanelNative>(_panel)");
        AssertContains(nativeInteropText, "D3DCompileNative(");
        AssertContains(nativeInteropText, "private static byte[] CompileShader(string hlslSource, string entryPoint, string profile)");
        AssertContains(nativeInteropText, "private static string ReadBlobString(IntPtr blobPtr)");
        AssertContains(dxgiStatisticsText, "_ = DwmFlush();");
        AssertDoesNotContain(rootText, "private interface ISwapChainPanelNative");
        AssertDoesNotContain(rootText, "private interface ID3DBlob");
        AssertDoesNotContain(rootText, "D3DCompileNative(");
        AssertDoesNotContain(rootText, "private static extern int DwmFlush()");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_FrameTypesLiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var pendingFrameText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.PendingFrame.cs")
            .Replace("\r\n", "\n");
        var metricTypesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.MetricTypes.cs")
            .Replace("\r\n", "\n");

        AssertContains(pendingFrameText, "private sealed class PendingFrame : IDisposable");
        AssertContains(pendingFrameText, "ArrayPool<byte>.Shared.Return(RawData);");
        AssertContains(pendingFrameText, "FrameLease?.Dispose();");
        AssertContains(metricTypesText, "public readonly record struct PresentCadenceMetrics(");
        AssertContains(metricTypesText, "public readonly record struct CpuStageTimingMetrics(");
        AssertContains(metricTypesText, "public readonly record struct RenderCpuTimingMetrics(");
        AssertContains(metricTypesText, "public readonly record struct PipelineLatencyMetrics(");
        AssertContains(metricTypesText, "public readonly record struct FrameLatencyWaitMetrics(");
        AssertContains(metricTypesText, "public readonly record struct FrameOwnershipMetrics(");
        AssertContains(metricTypesText, "public readonly record struct DxgiFrameStatisticsMetrics(");
        AssertContains(metricTypesText, "private static double[] CopyRecentRing(double[] window, int count, int index, int maxSamples)");
        AssertContains(metricTypesText, "private static CpuStageTimingMetrics SummarizeCpuStageTiming(double[] samples)");
        AssertContains(metricTypesText, "private static double TicksToMs(long ticks)");
        AssertContains(metricTypesText, "private static bool IsValidRenderCpuStageMs(double value)");
        AssertDoesNotContain(rootText, "private sealed class PendingFrame : IDisposable");
        AssertDoesNotContain(rootText, "public readonly record struct PresentCadenceMetrics(");
        AssertDoesNotContain(rootText, "public readonly record struct DxgiFrameStatisticsMetrics(");
        AssertDoesNotContain(pendingFrameText, "public readonly record struct PresentCadenceMetrics(");
        AssertDoesNotContain(metricTypesText, "private sealed class PendingFrame : IDisposable");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_FrameOwnershipLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var metricsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs")
            .Replace("\r\n", "\n");
        var ownershipText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.FrameOwnership.cs")
            .Replace("\r\n", "\n");

        AssertContains(ownershipText, "private long _framesSubmitted;");
        AssertContains(ownershipText, "private long _framesRendered;");
        AssertContains(ownershipText, "private long _framesDropped;");
        AssertContains(ownershipText, "private long _submissionGeneration;");
        AssertContains(ownershipText, "private long _lastSubmittedPreviewPresentId;");
        AssertContains(ownershipText, "private long _lastRenderedSchedulerToPresentTicks;");
        AssertContains(ownershipText, "private long _lastDroppedUtcUnixMs;");
        AssertContains(ownershipText, "private string _submissionGenerationDropReason = \"transition\";");
        AssertContains(ownershipText, "public FrameOwnershipMetrics GetFrameOwnershipMetrics()");
        AssertContains(ownershipText, "private void TrackFrameSubmitted(PendingFrame frame)");
        AssertContains(ownershipText, "private void TrackFramePresented(PendingFrame frame, long presentReturnTick, long estimatedVisibleTick)");
        AssertContains(ownershipText, "private void TrackFrameDropped(PendingFrame frame, string reason)");
        AssertContains(ownershipText, "Interlocked.Exchange(ref _lastRenderedSourcePtsTicks, frame.SourcePtsTicks);");
        AssertContains(ownershipText, "Volatile.Write(ref _lastDropReason, reason);");
        AssertDoesNotContain(rootText, "private long _framesSubmitted;");
        AssertDoesNotContain(rootText, "private long _submissionGeneration;");
        AssertDoesNotContain(metricsText, "public FrameOwnershipMetrics GetFrameOwnershipMetrics()");
        AssertDoesNotContain(metricsText, "private void TrackFrameSubmitted(PendingFrame frame)");
        AssertDoesNotContain(metricsText, "private void TrackFrameDropped(PendingFrame frame, string reason)");

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
        var displayClockText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.DisplayClock.cs")
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
        AssertContains(displayClockText, "private long EstimateVisibleTick(long presentReturnTick)");
        AssertContains(displayClockText, "private long GetEstimatedDisplayFrameIntervalTicks()");
        AssertContains(displayClockText, "public bool TryGetDisplayClock(out PreviewDisplayClockSnapshot snapshot)");
        AssertContains(displayClockText, "new PreviewDisplayClockSnapshot(");
        AssertDoesNotContain(rootText, "private readonly object _dxgiFrameStatisticsLock = new();");
        AssertDoesNotContain(rootText, "private long _dxgiFrameStatisticsPresentCount = -1;");
        AssertDoesNotContain(metricsText, "public DxgiFrameStatisticsMetrics GetDxgiFrameStatisticsMetrics()");
        AssertDoesNotContain(metricsText, "private void TrackDxgiFrameStatistics()");
        AssertDoesNotContain(metricsText, "private long EstimateVisibleTick(long presentReturnTick)");
        AssertDoesNotContain(metricsText, "public bool TryGetDisplayClock(out PreviewDisplayClockSnapshot snapshot)");
        AssertDoesNotContain(dxgiText, "public bool TryGetDisplayClock(out PreviewDisplayClockSnapshot snapshot)");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_SlowFrameDiagnosticsLiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var metricsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs")
            .Replace("\r\n", "\n");
        var diagnosticsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Diagnostics.cs")
            .Replace("\r\n", "\n");
        AssertContains(diagnosticsText, "private readonly object _slowFrameDiagnosticsLock = new();");
        AssertContains(diagnosticsText, "private readonly PreviewSlowFrameDiagnostic[] _slowFrameDiagnostics = new PreviewSlowFrameDiagnostic[64];");
        AssertContains(diagnosticsText, "public PreviewSlowFrameDiagnostic[] GetRecentSlowFrameDiagnostics(int maxEntries = 16)");
        AssertContains(diagnosticsText, "private void RecordSlowFrameDiagnostic(");
        AssertContains(diagnosticsText, "var dxgiSlip = CaptureSlowFrameDxgiSlipSnapshot();");
        AssertContains(diagnosticsText, "DxgiMissedRefreshCount = dxgiSlip.MissedRefreshCount");
        AssertContains(diagnosticsText, "private readonly record struct SlowFrameDxgiSlipSnapshot(");
        AssertContains(diagnosticsText, "private SlowFrameDxgiSlipSnapshot CaptureSlowFrameDxgiSlipSnapshot()");
        AssertContains(diagnosticsText, "frameStatisticsLastSampleFrameCounter == frameStatisticsFrameCounter");
        AssertContains(diagnosticsText, "private static string BuildSlowFrameDiagnosticReason(");
        AssertContains(diagnosticsText, "private static void AppendSlowFrameReason(");
        AssertContains(diagnosticsText, "\"dxgi_refresh_slip\"");
        AssertDoesNotContain(rootText, "private readonly object _slowFrameDiagnosticsLock = new();");
        AssertDoesNotContain(rootText, "new PreviewSlowFrameDiagnostic[64]");
        AssertDoesNotContain(metricsText, "public PreviewSlowFrameDiagnostic[] GetRecentSlowFrameDiagnostics(");
        AssertDoesNotContain(metricsText, "private void RecordSlowFrameDiagnostic(");
        AssertDoesNotContain(metricsText, "private static string BuildSlowFrameDiagnosticReason(");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_MetricTrackingLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var metricsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs")
            .Replace("\r\n", "\n");
        var metricTypesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.MetricTypes.cs")
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
        AssertContains(metricTypesText, "private static double[] CopyRecentRing(double[] window, int count, int index, int maxSamples)");
        AssertContains(metricTypesText, "private static CpuStageTimingMetrics SummarizeCpuStageTiming(double[] samples)");
        AssertContains(metricTypesText, "private static double TicksToMs(long ticks)");
        AssertContains(metricTypesText, "private static bool IsValidRenderCpuStageMs(double value)");
        AssertDoesNotContain(metricsText, "private static double[] CopyRecentRing(double[] window, int count, int index, int maxSamples)");
        AssertDoesNotContain(metricsText, "private static CpuStageTimingMetrics SummarizeCpuStageTiming(double[] samples)");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.PresentCadenceMetrics.cs")),
            "Present cadence metrics folded into renderer metrics owner");
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
