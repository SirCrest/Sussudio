using System.Threading.Tasks;

static partial class Program
{
    private static Task D3D11PreviewRenderer_ConfigurationLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var configurationText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Configuration.cs")
            .Replace("\r\n", "\n");

        AssertContains(configurationText, "SUSSUDIO_PREVIEW_PRESENT_SYNC_INTERVAL");
        AssertContains(configurationText, "SUSSUDIO_PREVIEW_DXGI_MAX_FRAME_LATENCY");
        AssertContains(configurationText, "SUSSUDIO_PREVIEW_SWAPCHAIN_BUFFER_COUNT");
        AssertContains(configurationText, "SUSSUDIO_PREVIEW_RENDER_QUEUE_DEPTH");
        AssertContains(configurationText, "SUSSUDIO_PREVIEW_WAITABLE_SWAPCHAIN");
        AssertContains(configurationText, "SUSSUDIO_PREVIEW_DXGI_FRAME_STATS_SAMPLE_INTERVAL");
        AssertContains(configurationText, "SUSSUDIO_PREVIEW_RENDER_MMCSS_TASK\") ?? \"Playback\"");
        AssertContains(configurationText, "SUSSUDIO_PREVIEW_NATIVE_STOP_FENCE_TIMEOUT_MS");
        AssertContains(configurationText, "SUSSUDIO_PREVIEW_RENDER_THREAD_STOP_TIMEOUT_MS");
        AssertDoesNotContain(rootText, "SUSSUDIO_PREVIEW_PRESENT_SYNC_INTERVAL");
        AssertDoesNotContain(rootText, "SUSSUDIO_PREVIEW_RENDER_MMCSS_TASK");

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_NativeInteropLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var nativeInteropText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.NativeInterop.cs")
            .Replace("\r\n", "\n");
        var panelBindingText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.PanelBinding.cs")
            .Replace("\r\n", "\n");
        var shaderSourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderSources.cs")
            .Replace("\r\n", "\n");
        var dxgiStatisticsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.DxgiFrameStatistics.cs")
            .Replace("\r\n", "\n");

        AssertContains(nativeInteropText, "private interface ISwapChainPanelNative");
        AssertContains(nativeInteropText, "private interface ID3DBlob");
        AssertContains(nativeInteropText, "private static extern int D3DCompileNative(");
        AssertContains(nativeInteropText, "private static extern int DwmFlush()");
        AssertContains(panelBindingText, "WinRT.CastExtensions.As<ISwapChainPanelNative>(_panel)");
        AssertContains(shaderSourcesText, "D3DCompileNative(");
        AssertContains(dxgiStatisticsText, "_ = DwmFlush();");
        AssertDoesNotContain(rootText, "private interface ISwapChainPanelNative");
        AssertDoesNotContain(rootText, "private interface ID3DBlob");
        AssertDoesNotContain(rootText, "D3DCompileNative(");
        AssertDoesNotContain(rootText, "private static extern int DwmFlush()");

        return Task.CompletedTask;
    }

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

    private static Task D3D11PreviewRenderer_MetricTrackingLivesInFocusedPartial()
    {
        var metricsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs")
            .Replace("\r\n", "\n");
        var trackingText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.MetricsTracking.cs")
            .Replace("\r\n", "\n");

        AssertContains(metricsText, "public PresentCadenceMetrics GetPresentCadenceMetrics(double expectedIntervalMs)");
        AssertContains(metricsText, "public RenderCpuTimingMetrics GetRenderCpuTimingMetrics()");
        AssertContains(metricsText, "public FrameLatencyWaitMetrics GetFrameLatencyWaitMetrics()");
        AssertContains(trackingText, "private double TrackPresentCadence(bool countSample)");
        AssertContains(trackingText, "private void TrackPipelineLatency(long arrivalTick, long estimatedVisibleTick)");
        AssertContains(trackingText, "private void TrackRenderCpuTiming(");
        AssertContains(trackingText, "private void TrackFrameLatencyWait(uint result, long waitTicks)");
        AssertContains(trackingText, "public void SetExpectedFrameRate(double fps)");
        AssertContains(trackingText, "private void ResetPresentCadence()");
        AssertDoesNotContain(metricsText, "private double TrackPresentCadence(");
        AssertDoesNotContain(metricsText, "private void TrackRenderCpuTiming(");
        AssertDoesNotContain(metricsText, "private void ResetPresentCadence()");

        return Task.CompletedTask;
    }
}
