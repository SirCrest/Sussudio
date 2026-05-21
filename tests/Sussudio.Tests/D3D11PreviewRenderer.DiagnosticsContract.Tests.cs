using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private readonly record struct D3D11PreviewRendererDiagnosticsContractSources(
        string Source,
        string RenderSource,
        string CaptureSource);

    internal static Task D3D11PreviewRenderer_DiagnosticsContract_ExposesSwapChainAndRenderTiming()
    {
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var sources = ReadD3D11PreviewRendererDiagnosticsContractSources();
        var source = sources.Source;
        var renderSource = sources.RenderSource;
        var captureSource = sources.CaptureSource;
        AssertContains(source, "SUSSUDIO_PREVIEW_RENDER_MMCSS_TASK\") ?? \"Playback\"");
        AssertContains(source, "SUSSUDIO_PREVIEW_DXGI_FRAME_STATS_SAMPLE_INTERVAL");
        AssertContains(source, "private long _dxgiFrameStatisticsFrameCounter;");
        AssertContains(source, "private long _dxgiFrameStatisticsLastSampleFrameCounter;");
        AssertContains(source, "public PipelineLatencyMetrics GetPipelineLatencyMetrics()");
        AssertContains(source, "public double GetEstimatedPipelineLatencyMs()\n    {\n        lock (_pipelineLatencyLock)");
        AssertDoesNotContain(source, "public double GetEstimatedPipelineLatencyMs()\n    {\n        return GetPipelineLatencyMetrics().AverageMs;\n    }");
        AssertContains(source, "private long EstimateVisibleTick(long presentReturnTick)");
        AssertContains(renderSource, "var estimatedVisibleTick = EstimateVisibleTick(presentEnd);");
        AssertContains(renderSource, "TrackPipelineLatency(frame.ArrivalTick, estimatedVisibleTick);");
        AssertContains(source, "var sorted = (double[])samples.Clone();");
        AssertContains(source, "Array.Sort(sorted);");
        AssertContains(source, "var frameCounter = Interlocked.Increment(ref _dxgiFrameStatisticsFrameCounter);");
        AssertContains(source, "frameCounter % _dxgiFrameStatisticsSampleIntervalFrames != 0");
        AssertContains(source, "_dxgiFrameStatisticsLastSampleFrameCounter = frameCounter;");
        AssertContains(source, "frameStatisticsLastSampleFrameCounter == frameStatisticsFrameCounter");
        AssertContains(source, "private int _pendingFrameCount;");
        AssertContains(source, "public int PendingFrameCount => Math.Max(0, Volatile.Read(ref _pendingFrameCount));");
        AssertContains(source, "public event Action<string>? RenderThreadFailed;");
        AssertContains(source, "public long RenderThreadFailureCount => Interlocked.Read(ref _renderThreadFailureCount);");
        AssertContains(source, "public string LastRenderThreadFailureMessage => Volatile.Read(ref _lastRenderThreadFailureMessage);");
        AssertContains(renderSource, "NotifyRenderThreadFailed(ex);");
        AssertContains(renderSource, "RenderThreadFailed?.Invoke(reason)");
        AssertContains(source, "IPreviewFrameQueueControl");
        AssertContains(source, "public int DropPendingFrames(string reason)");
        AssertContains(source, "Interlocked.Increment(ref _submissionGeneration);");
        AssertContains(source, "frame.SubmissionGeneration = Interlocked.Read(ref _submissionGeneration);");
        AssertContains(source, "var pendingFrameCount = Interlocked.Increment(ref _pendingFrameCount);\n            _pendingFrames.Enqueue(frame);");
        AssertContains(source, "private void SignalFrameReady(string operation)");
        AssertContains(source, "private void ResetFrameReady(string operation)");
        AssertContains(source, "D3D11_PREVIEW_FRAME_SIGNAL_SKIPPED");
        AssertContains(source, "D3D11_PREVIEW_FRAME_RESET_SKIPPED");
        AssertContains(source, "SignalFrameReady(\"pending_frame\");");
        AssertContains(renderSource, "SignalFrameReady(\"render_loop_drain\");");
        AssertEqual(1, (source + renderSource).Split("_frameReadyEvent.Set();", StringSplitOptions.None).Length - 1, "All D3D frame-ready signals go through SignalFrameReady");
        AssertEqual(1, (source + renderSource).Split("_frameReadyEvent.Reset();", StringSplitOptions.None).Length - 1, "All D3D frame-ready resets go through ResetFrameReady");
        AssertContains(source, "private bool TryDequeuePendingFrame(out PendingFrame frame)");
        AssertContains(source, "DecrementPendingFrameCount();");
        AssertDoesNotContain(source, "_pendingFrames.Count");
        AssertDoesNotContain(source, "_pendingFrames.Enqueue(frame);\n            var pendingFrameCount = Interlocked.Increment(ref _pendingFrameCount);");
        AssertContains(source, "private void TrackFrameDropped(PendingFrame frame, string reason)\n    {\n        Interlocked.Increment(ref _framesDropped);");
        AssertContains(source, "Interlocked.Exchange(ref _lastSubmittedSourcePtsTicks, frame.SourcePtsTicks);");
        AssertContains(source, "Interlocked.Exchange(ref _lastRenderedSourcePtsTicks, frame.SourcePtsTicks);");
        AssertContains(source, "Interlocked.Exchange(ref _lastDroppedSourcePtsTicks, frame.SourcePtsTicks);");
        AssertDoesNotContain(source, "TrackFrameDropped(frame, \"renderer-stopped\");\n                frame.Dispose();\n                Interlocked.Increment(ref _framesDropped);");
        AssertDoesNotContain(source, "TrackFrameDropped(oldest, \"renderer-backlog\");\n                    oldest.Dispose();\n                    Interlocked.Increment(ref _framesDropped);");
        AssertDoesNotContain(renderSource, "_pendingFrames.TryDequeue");
        AssertContains(renderSource, "var framesRenderedBefore = Interlocked.Read(ref _framesRendered);");
        AssertContains(renderSource, "frame.SubmissionGeneration != Interlocked.Read(ref _submissionGeneration)");
        AssertContains(renderSource, "if (Interlocked.Read(ref _framesRendered) == framesRenderedBefore)\n                    {\n                        TrackFrameDropped(frame, \"render-skipped\");\n                    }");
        AssertContains(captureSource, "DropPendingPreviewFrames(\"live-preview-suppressed\")");
        AssertContains(captureSource, "DropPendingPreviewFrames(\"live-preview-resumed\")");
        AssertContains(captureSource, "queueControl.DropPendingFrames(reason)");
        AssertContains(captureSource, "private long _livePreviewPresentId;");
        AssertContains(captureSource, "var previewPresentId = Interlocked.Increment(ref _livePreviewPresentId);");
        AssertContains(captureSource, "SourceSequenceNumber = sourceSequence");
        AssertContains(captureSource, "PreviewPresentId = previewPresentId");
        AssertContains(captureSource, "SchedulerSubmitTick = submitTick");
        AssertNotNull(rendererType.GetProperty("SwapChainAddress", BindingFlags.Public | BindingFlags.Instance), "D3D11PreviewRenderer.SwapChainAddress");
        AssertNotNull(rendererType.GetMethod("DropPendingFrames", BindingFlags.Public | BindingFlags.Instance), "D3D11PreviewRenderer.DropPendingFrames");
        AssertNotNull(rendererType.GetMethod("GetRenderCpuTimingMetrics", BindingFlags.Public | BindingFlags.Instance), "D3D11PreviewRenderer.GetRenderCpuTimingMetrics");
        AssertNotNull(rendererType.GetMethod("GetPipelineLatencyMetrics", BindingFlags.Public | BindingFlags.Instance), "D3D11PreviewRenderer.GetPipelineLatencyMetrics");
        AssertNotNull(rendererType.GetMethod("GetFrameOwnershipMetrics", BindingFlags.Public | BindingFlags.Instance), "D3D11PreviewRenderer.GetFrameOwnershipMetrics");
        AssertNotNull(rendererType.GetMethod("GetDxgiFrameStatisticsMetrics", BindingFlags.Public | BindingFlags.Instance), "D3D11PreviewRenderer.GetDxgiFrameStatisticsMetrics");
        AssertNotNull(rendererType.GetMethod("TryGetDisplayClock", BindingFlags.Public | BindingFlags.Instance), "D3D11PreviewRenderer.TryGetDisplayClock");

        return Task.CompletedTask;
    }

    private static D3D11PreviewRendererDiagnosticsContractSources ReadD3D11PreviewRendererDiagnosticsContractSources()
    {
        var source = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Configuration.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.PendingFrame.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.MetricTypes.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.MetricSampleHelpers.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.FrameOwnership.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.DxgiFrameStatistics.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.DisplayClock.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Submission.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.PresentCadenceMetrics.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.SlowFrameDiagnostics.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.PendingFrames.cs");
        var renderSource = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderThread.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Nv12ShaderPass.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.HdrShaderPass.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Present.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderRendering.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.VideoProcessorPipeline.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.InputResources.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.HdrInputResources.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.DeviceLost.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.FrameLatency.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Viewport.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.FrameUpload.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RawFrameUpload.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.PanelBinding.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.PanelComposition.cs");
        var captureSource = ReadUnifiedVideoCaptureSource();

        return new D3D11PreviewRendererDiagnosticsContractSources(
            source,
            renderSource,
            captureSource);
    }
}
