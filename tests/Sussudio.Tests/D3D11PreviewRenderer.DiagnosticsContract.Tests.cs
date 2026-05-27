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
        AssertContains(renderSource, "if (Interlocked.Read(ref _framesRendered) == framesRenderedBefore)\n            {\n                TrackFrameDropped(frame, \"render-skipped\");\n            }");
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

    internal static Task D3D11PreviewRenderer_PresentCadenceMetrics_HasExpectedProperties()
    {
        var metricsType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer+PresentCadenceMetrics");

        var expectedProps = new[]
        {
            "SampleCount", "ObservedFps", "ExpectedIntervalMs", "AverageIntervalMs",
            "P95IntervalMs", "P99IntervalMs", "MaxIntervalMs", "OnePercentLowFps", "JitterStdDevMs", "SlowFrameCount", "SlowFramePercent"
        };

        foreach (var prop in expectedProps)
        {
            var propInfo = metricsType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance);
            AssertNotNull(propInfo, $"PresentCadenceMetrics.{prop}");
        }

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_PresentCadenceSuppression_SkipsSamplesAndResetsBaseline()
    {
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var renderer = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(rendererType);
        SetPrivateField(renderer, "_presentCadenceLock", new object());
        SetPrivateField(renderer, "_presentIntervalWindowMs", new double[8]);

        var getMetrics = rendererType.GetMethod("GetPresentCadenceMetrics", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("GetPresentCadenceMetrics not found.");

        var fakeStepTicks = System.Diagnostics.Stopwatch.Frequency / 60;

        SetPrivateField(renderer, "_lastPresentTick", 0L);
        InvokeNonPublicInstanceMethod(renderer, "TrackPresentCadence", new object?[] { true });

        SetPrivateField(renderer, "_lastPresentTick", System.Diagnostics.Stopwatch.GetTimestamp() - fakeStepTicks);
        var firstInterval = Convert.ToDouble(InvokeNonPublicInstanceMethod(renderer, "TrackPresentCadence", new object?[] { true }));
        AssertEqual(true, firstInterval > 0, "first measured cadence interval is recorded");

        var metrics = getMetrics.Invoke(renderer, new object[] { 8.333 })
            ?? throw new InvalidOperationException("GetPresentCadenceMetrics returned null.");
        AssertEqual(1, Convert.ToInt32(GetPropertyValue(metrics, "SampleCount")), "sample count after first measured interval");

        SetPrivateField(renderer, "_lastPresentTick", System.Diagnostics.Stopwatch.GetTimestamp() - fakeStepTicks);
        var suppressedInterval = Convert.ToDouble(InvokeNonPublicInstanceMethod(renderer, "TrackPresentCadence", new object?[] { false }));
        AssertEqual(0.0, suppressedInterval, "suppressed present does not report interval");
        metrics = getMetrics.Invoke(renderer, new object[] { 8.333 })
            ?? throw new InvalidOperationException("GetPresentCadenceMetrics returned null after suppressed present.");
        AssertEqual(1, Convert.ToInt32(GetPropertyValue(metrics, "SampleCount")), "suppressed present does not add a sample");
        AssertEqual(1L, GetLongPrivateField(renderer, "_presentCadenceBaselinePending"), "suppressed present marks baseline pending");

        SetPrivateField(renderer, "_lastPresentTick", System.Diagnostics.Stopwatch.GetTimestamp() - fakeStepTicks);
        var baselineInterval = Convert.ToDouble(InvokeNonPublicInstanceMethod(renderer, "TrackPresentCadence", new object?[] { true }));
        AssertEqual(0.0, baselineInterval, "first measured present after suppression resets baseline");
        metrics = getMetrics.Invoke(renderer, new object[] { 8.333 })
            ?? throw new InvalidOperationException("GetPresentCadenceMetrics returned null after baseline present.");
        AssertEqual(1, Convert.ToInt32(GetPropertyValue(metrics, "SampleCount")), "baseline reset does not add transition gap sample");
        AssertEqual(0L, GetLongPrivateField(renderer, "_presentCadenceBaselinePending"), "baseline pending flag clears after measured present");

        SetPrivateField(renderer, "_lastPresentTick", System.Diagnostics.Stopwatch.GetTimestamp() - fakeStepTicks);
        var resumedInterval = Convert.ToDouble(InvokeNonPublicInstanceMethod(renderer, "TrackPresentCadence", new object?[] { true }));
        AssertEqual(true, resumedInterval > 0, "second measured present after suppression records interval");
        metrics = getMetrics.Invoke(renderer, new object[] { 8.333 })
            ?? throw new InvalidOperationException("GetPresentCadenceMetrics returned null after resumed present.");
        AssertEqual(2, Convert.ToInt32(GetPropertyValue(metrics, "SampleCount")), "measured cadence resumes after suppression baseline");

        return Task.CompletedTask;
    }

    internal static Task D3D11PreviewRenderer_DiagnosticsContract_PerformanceTimelineExposesExpectedProperties()
    {
        var rootModelText = ReadRepoFile("Sussudio/Models/Automation/PerformanceTimelineEntry.cs");

        AssertContains(rootModelText, "public sealed class PerformanceTimelineEntry");
        AssertContains(rootModelText, "public double PreviewCadenceSlowFramePercent { get; init; }");
        AssertContains(rootModelText, "public string PreviewPacingSlowStageEvidence { get; init; } = string.Empty;");
        AssertContains(rootModelText, "public string FlashbackPlaybackLastCommandFailure { get; init; } = string.Empty;");
        AssertContains(rootModelText, "public double FlashbackExportThroughputBytesPerSec { get; init; }");
        AssertContains(rootModelText, "public double ProcessCpuPercent { get; init; }");
        AssertDoesNotContain(rootModelText, "partial class PerformanceTimelineEntry");

        var performanceTimelineEntryType = RequireType("Sussudio.Models.PerformanceTimelineEntry");
        foreach (var prop in new[]
                 {
                     "PreviewCadenceSlowFramePercent",
                     "PreviewCadenceOnePercentLowFps",
                     "MjpegPreviewJitterEnabled",
                     "MjpegPreviewJitterTargetDepth",
                     "MjpegPreviewJitterMaxDepth",
                     "MjpegPreviewJitterQueueDepth",
                     "MjpegPreviewJitterTotalDropped",
                     "MjpegPreviewJitterDeadlineDropCount",
                     "MjpegPreviewJitterClearedDropCount",
                     "MjpegPreviewJitterUnderflowCount",
                     "MjpegPreviewJitterResumeReprimeCount",
                     "MjpegPreviewJitterLatencyP95Ms",
                     "MjpegPreviewJitterLatencyMaxMs",
                     "MjpegPreviewJitterLastDropReason",
                     "PreviewD3DPendingFrameCount",
                     "PreviewD3DPresentCallP95Ms",
                     "PreviewD3DTotalFrameCpuP95Ms",
                     "PreviewD3DInputUploadCpuP99Ms",
                     "PreviewD3DRenderSubmitCpuP99Ms",
                     "PreviewD3DPresentCallP99Ms",
                     "PreviewD3DTotalFrameCpuP99Ms",
                     "PreviewD3DPipelineLatencyP95Ms",
                     "PreviewD3DPipelineLatencyP99Ms",
                     "PreviewD3DPipelineLatencyMaxMs",
                     "PreviewD3DFrameLatencyWaitTimeoutCount",
                     "PreviewD3DFrameLatencyWaitP95Ms",
                     "PreviewD3DFrameLatencyWaitMaxMs",
                     "PreviewD3DFrameStatsRecentMissedRefreshCount",
                     "PreviewD3DFrameStatsRecentFailureCount",
                     "PreviewD3DLastRenderedSchedulerToPresentMs",
                     "PreviewD3DLastRenderedPipelineLatencyMs",
                     "PreviewD3DLastDropReason",
                     "PreviewPacingLikelySlowStage",
                     "PreviewPacingSlowStageConfidence",
                     "PreviewPacingSlowStageEvidence",
                     "FlashbackPlaybackState",
                     "FlashbackPlaybackP99FrameMs",
                     "FlashbackPlaybackDecodeP99Ms",
                     "FlashbackPlaybackMaxDecodePhase",
                     "FlashbackPlaybackMaxDecodeReceiveMs",
                     "FlashbackPlaybackMaxDecodeFeedMs",
                     "FlashbackPlaybackMaxDecodeReadMs",
                     "FlashbackPlaybackMaxDecodeSendMs",
                     "FlashbackPlaybackMaxDecodeAudioMs",
                     "FlashbackPlaybackMaxDecodeConvertMs",
                     "FlashbackPlaybackPendingCommands",
                     "FlashbackPlaybackSeekCommandsCoalesced",
                     "FlashbackPlaybackSubmitFailures",
                     "FlashbackPlaybackLastDropUtcUnixMs",
                     "FlashbackPlaybackLastDropReason",
                     "FlashbackPlaybackLastSubmitFailureUtcUnixMs",
                     "FlashbackPlaybackLastSubmitFailure",
                     "FlashbackPlaybackAudioMasterDelayDoubles",
                     "FlashbackPlaybackAudioMasterDelayShrinks",
                     "FlashbackPlaybackAudioMasterFallbacks",
                     "FlashbackPlaybackSegmentSwitches",
                     "FlashbackPlaybackFmp4Reopens",
                     "FlashbackPlaybackWriteHeadWaits",
                     "FlashbackPlaybackNearLiveSnaps",
                     "FlashbackPlaybackDecodeErrorSnaps",
                     "FlashbackPlaybackLastWriteHeadWaitGapMs",
                     "FlashbackPlaybackLastCommandFailureUtcUnixMs",
                     "FlashbackPlaybackLastCommandFailure",
                     "FlashbackVideoQueueRejectedFrames",
                     "FlashbackVideoQueueLastRejectReason",
                     "FlashbackGpuQueueRejectedFrames",
                     "FlashbackGpuQueueLastRejectReason",
                     "FlashbackBackendSettingsStale",
                     "FlashbackBackendSettingsStaleReason",
                     "FlashbackBackendActiveFormat",
                     "FlashbackBackendRequestedFormat",
                     "FlashbackBackendActivePreset",
                     "FlashbackBackendRequestedPreset",
                     "FatalCleanupInProgress",
                     "FlashbackCleanupInProgress",
                     "FlashbackExportActive",
                     "FlashbackExportStatus",
                     "FlashbackExportFailureKind",
                     "FlashbackExportPercent",
                     "FlashbackExportInPointMs",
                     "FlashbackExportOutPointMs",
                     "FlashbackExportMessage",
                     "FlashbackExportForceRotateFallbacks",
                     "FlashbackExportLastForceRotateFallbackUtcUnixMs",
                     "FlashbackExportLastForceRotateFallbackSegments",
                     "FlashbackExportLastForceRotateFallbackInPointMs",
                     "FlashbackExportLastForceRotateFallbackOutPointMs",
                     "FlashbackExportThroughputBytesPerSec",
                     "FlashbackExportLastProgressAgeMs",
                     "ProcessCpuPercent"
                 })
        {
            AssertNotNull(performanceTimelineEntryType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"PerformanceTimelineEntry.{prop}");
        }

        return Task.CompletedTask;
    }

    private static D3D11PreviewRendererDiagnosticsContractSources ReadD3D11PreviewRendererDiagnosticsContractSources()
    {
        var source = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Submission.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.PendingFrames.cs");
        var renderSource = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderThread.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderRendering.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.DeviceInitialization.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.FrameUpload.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.PanelBinding.cs");
        var captureSource = ReadUnifiedVideoCaptureSource();

        return new D3D11PreviewRendererDiagnosticsContractSources(
            source,
            renderSource,
            captureSource);
    }
}
