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
}
