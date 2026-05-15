using System.Threading.Tasks;

static partial class Program
{
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
}
