using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task D3D11PreviewRenderer_DiagnosticsContract_ExposesSwapChainAndRenderTiming()
    {
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var source = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.FrameTypes.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.FrameOwnership.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.DxgiFrameStatistics.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Submission.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.SlowFrameDiagnostics.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.PendingFrames.cs");
        var renderSource = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Rendering.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderRendering.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.InputResources.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.DeviceLost.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.FrameLatency.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Viewport.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.FrameUpload.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.PanelBinding.cs");
        var captureSource = ReadUnifiedVideoCaptureSource();
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

        var displayClockSnapshotType = RequireType("Sussudio.Services.Preview.PreviewDisplayClockSnapshot");
        foreach (var prop in new[] { "LastPresentTick", "FrameIntervalTicks", "ExpectedFrameIntervalMs", "SampleCount" })
        {
            AssertNotNull(displayClockSnapshotType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"PreviewDisplayClockSnapshot.{prop}");
        }

        var stageTimingType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer+CpuStageTimingMetrics");
        foreach (var prop in new[] { "SampleCount", "AverageMs", "P95Ms", "P99Ms", "MaxMs" })
        {
            AssertNotNull(stageTimingType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"CpuStageTimingMetrics.{prop}");
        }

        var renderTimingType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer+RenderCpuTimingMetrics");
        foreach (var prop in new[] { "InputUpload", "RenderSubmit", "PresentCall", "TotalFrame" })
        {
            AssertNotNull(renderTimingType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"RenderCpuTimingMetrics.{prop}");
        }

        var pipelineLatencyType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer+PipelineLatencyMetrics");
        foreach (var prop in new[] { "SampleCount", "AverageMs", "P95Ms", "P99Ms", "MaxMs" })
        {
            AssertNotNull(pipelineLatencyType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"PipelineLatencyMetrics.{prop}");
        }

        var ownershipMetricsType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer+FrameOwnershipMetrics");
        var previewSinkType = RequireType("Sussudio.Services.Contracts.IPreviewFrameSink");
        var trackingType = RequireType("Sussudio.Services.Contracts.PreviewFrameTracking");
        foreach (var prop in new[] { "SourceSequenceNumber", "PreviewPresentId", "SourcePtsTicks", "ArrivalTick", "SchedulerSubmitTick", "CountForPresentCadence" })
        {
            AssertNotNull(trackingType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"PreviewFrameTracking.{prop}");
        }
        var submitTexture = previewSinkType.GetMethod("SubmitTexture", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("IPreviewFrameSink.SubmitTexture was not found.");
        AssertEqual(true, submitTexture.GetParameters().Any(parameter => parameter.ParameterType == trackingType), "SubmitTexture tracking parameter");
        var submitNv12PlaneTextures = previewSinkType.GetMethod("SubmitNv12PlaneTextures", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("IPreviewFrameSink.SubmitNv12PlaneTextures was not found.");
        AssertEqual(true, submitNv12PlaneTextures.GetParameters().Any(parameter => parameter.ParameterType == trackingType), "SubmitNv12PlaneTextures tracking parameter");
        foreach (var prop in new[]
                 {
                     "LastSubmittedPreviewPresentId",
                     "LastSubmittedSourceSequenceNumber",
                     "LastSubmittedSourcePtsTicks",
                     "LastSubmittedUtcUnixMs",
                     "LastRenderedPreviewPresentId",
                     "LastRenderedSourceSequenceNumber",
                     "LastRenderedSourcePtsTicks",
                     "LastRenderedUtcUnixMs",
                     "LastRenderedSchedulerToPresentMs",
                     "LastRenderedPipelineLatencyMs",
                     "LastDroppedPreviewPresentId",
                     "LastDroppedSourceSequenceNumber",
                     "LastDroppedSourcePtsTicks",
                     "LastDroppedUtcUnixMs",
                     "LastDropReason"
                 })
        {
            AssertNotNull(ownershipMetricsType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"FrameOwnershipMetrics.{prop}");
        }

        var dxgiFrameStatsType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer+DxgiFrameStatisticsMetrics");
        foreach (var prop in new[]
                 {
                     "SampleCount",
                     "SuccessCount",
                     "FailureCount",
                     "LastError",
                     "PresentCount",
                     "PresentRefreshCount",
                     "SyncRefreshCount",
                     "SyncQpcTime",
                     "LastPresentDelta",
                     "LastPresentRefreshDelta",
                     "LastSyncRefreshDelta",
                     "MissedRefreshCount"
                 })
        {
            AssertNotNull(dxgiFrameStatsType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"DxgiFrameStatisticsMetrics.{prop}");
        }

        var previewSnapshotType = RequireType("Sussudio.Models.PreviewRuntimeSnapshot");
        foreach (var prop in new[]
                 {
                     "D3DSwapChainAddress",
                     "D3DPresentSyncInterval",
                     "D3DMaxFrameLatency",
                     "D3DSwapChainBufferCount",
                     "D3DPendingFrameCount",
                     "DisplayCadenceP99IntervalMs",
                     "DisplayCadenceOnePercentLowFps",
                     "D3DCpuTimingSampleCount",
                     "D3DInputUploadCpuP95Ms",
                     "D3DInputUploadCpuP99Ms",
                     "D3DRenderSubmitCpuP95Ms",
                     "D3DRenderSubmitCpuP99Ms",
                     "D3DPresentCallP95Ms",
                     "D3DPresentCallP99Ms",
                     "D3DTotalFrameCpuP95Ms",
                     "D3DTotalFrameCpuP99Ms",
                     "D3DPipelineLatencySampleCount",
                     "D3DPipelineLatencyAvgMs",
                     "D3DPipelineLatencyP95Ms",
                     "D3DPipelineLatencyP99Ms",
                     "D3DPipelineLatencyMaxMs",
                     "D3DFrameLatencyWaitEnabled",
                     "D3DFrameLatencyWaitHandleActive",
                     "D3DFrameLatencyWaitCallCount",
                     "D3DFrameLatencyWaitSignaledCount",
                     "D3DFrameLatencyWaitTimeoutCount",
                     "D3DFrameLatencyWaitUnexpectedResultCount",
                     "D3DFrameLatencyWaitLastResult",
                     "D3DFrameLatencyWaitLastMs",
                     "D3DFrameLatencyWaitSampleCount",
                     "D3DFrameLatencyWaitAvgMs",
                     "D3DFrameLatencyWaitP95Ms",
                     "D3DFrameLatencyWaitP99Ms",
                     "D3DFrameLatencyWaitMaxMs",
                     "D3DFrameStatsSampleCount",
                     "D3DFrameStatsSuccessCount",
                     "D3DFrameStatsFailureCount",
                     "D3DFrameStatsLastError",
                     "D3DFrameStatsPresentCount",
                     "D3DFrameStatsPresentRefreshCount",
                     "D3DFrameStatsSyncRefreshCount",
                     "D3DFrameStatsSyncQpcTime",
                     "D3DFrameStatsLastPresentDelta",
                     "D3DFrameStatsLastPresentRefreshDelta",
                     "D3DFrameStatsLastSyncRefreshDelta",
                     "D3DFrameStatsMissedRefreshCount",
                     "D3DRenderThreadFailureCount",
                     "D3DLastRenderThreadFailureType",
                     "D3DLastRenderThreadFailureMessage",
                     "D3DLastRenderThreadFailureHResult",
                     "D3DLastSubmittedPreviewPresentId",
                     "D3DLastSubmittedSourceSequenceNumber",
                     "D3DLastSubmittedSourcePtsTicks",
                     "D3DLastSubmittedUtcUnixMs",
                     "D3DLastRenderedPreviewPresentId",
                     "D3DLastRenderedSourceSequenceNumber",
                     "D3DLastRenderedSourcePtsTicks",
                     "D3DLastRenderedUtcUnixMs",
                     "D3DLastRenderedSchedulerToPresentMs",
                     "D3DLastRenderedPipelineLatencyMs",
                     "D3DLastDroppedPreviewPresentId",
                     "D3DLastDroppedSourceSequenceNumber",
                     "D3DLastDroppedSourcePtsTicks",
                     "D3DLastDroppedUtcUnixMs",
                     "D3DLastDropReason",
                     "D3DRecentSlowFrames"
                 })
        {
            AssertNotNull(previewSnapshotType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"PreviewRuntimeSnapshot.{prop}");
        }

        var slowFrameDiagnosticType = RequireType("Sussudio.Models.PreviewSlowFrameDiagnostic");
        foreach (var prop in new[]
                 {
                     "PreviewPresentId",
                     "SourceSequenceNumber",
                     "QpcTimestamp",
                     "UtcUnixMs",
                     "PresentIntervalMs",
                     "InputUploadCpuMs",
                     "RenderSubmitCpuMs",
                     "PresentCallMs",
                     "TotalFrameCpuMs",
                     "SchedulerToPresentMs",
                     "PipelineLatencyMs",
                     "ExpectedIntervalMs",
                     "DiagnosticThresholdMs",
                     "WorstOverBudgetMs",
                     "SlowReason",
                     "PendingFrameCount",
                     "DxgiPresentDelta",
                     "DxgiPresentRefreshDelta",
                     "DxgiSyncRefreshDelta",
                     "DxgiMissedRefreshCount"
                 })
        {
            AssertNotNull(slowFrameDiagnosticType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"PreviewSlowFrameDiagnostic.{prop}");
        }

        var automationSnapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        foreach (var prop in new[]
                 {
                     "PreviewD3DSwapChainAddress",
                     "PreviewD3DPresentSyncInterval",
                     "PreviewD3DMaxFrameLatency",
                     "PreviewD3DSwapChainBufferCount",
                     "PreviewD3DPendingFrameCount",
                     "PreviewCadenceP99IntervalMs",
                     "PreviewCadenceOnePercentLowFps",
                     "PreviewD3DCpuTimingSampleCount",
                     "PreviewD3DInputUploadCpuP95Ms",
                     "PreviewD3DInputUploadCpuP99Ms",
                     "PreviewD3DRenderSubmitCpuP95Ms",
                     "PreviewD3DRenderSubmitCpuP99Ms",
                     "PreviewD3DPresentCallP95Ms",
                     "PreviewD3DPresentCallP99Ms",
                     "PreviewD3DTotalFrameCpuP95Ms",
                     "PreviewD3DTotalFrameCpuP99Ms",
                     "PreviewD3DPipelineLatencySampleCount",
                     "PreviewD3DPipelineLatencyAvgMs",
                     "PreviewD3DPipelineLatencyP95Ms",
                     "PreviewD3DPipelineLatencyP99Ms",
                     "PreviewD3DPipelineLatencyMaxMs",
                     "PreviewD3DFrameLatencyWaitEnabled",
                     "PreviewD3DFrameLatencyWaitHandleActive",
                     "PreviewD3DFrameLatencyWaitCallCount",
                     "PreviewD3DFrameLatencyWaitSignaledCount",
                     "PreviewD3DFrameLatencyWaitTimeoutCount",
                     "PreviewD3DFrameLatencyWaitUnexpectedResultCount",
                     "PreviewD3DFrameLatencyWaitLastResult",
                     "PreviewD3DFrameLatencyWaitLastMs",
                     "PreviewD3DFrameLatencyWaitSampleCount",
                     "PreviewD3DFrameLatencyWaitAvgMs",
                     "PreviewD3DFrameLatencyWaitP95Ms",
                     "PreviewD3DFrameLatencyWaitP99Ms",
                     "PreviewD3DFrameLatencyWaitMaxMs",
                     "PreviewD3DFrameStatsSampleCount",
                     "PreviewD3DFrameStatsSuccessCount",
                     "PreviewD3DFrameStatsFailureCount",
                     "PreviewD3DFrameStatsLastError",
                     "PreviewD3DFrameStatsPresentCount",
                     "PreviewD3DFrameStatsPresentRefreshCount",
                     "PreviewD3DFrameStatsSyncRefreshCount",
                     "PreviewD3DFrameStatsSyncQpcTime",
                     "PreviewD3DFrameStatsLastPresentDelta",
                     "PreviewD3DFrameStatsLastPresentRefreshDelta",
                     "PreviewD3DFrameStatsLastSyncRefreshDelta",
                     "PreviewD3DFrameStatsMissedRefreshCount",
                     "PreviewD3DFrameStatsRecentMissedRefreshCount",
                     "PreviewD3DFrameStatsRecentFailureCount",
                     "PreviewD3DLastSubmittedPreviewPresentId",
                     "PreviewD3DLastSubmittedSourceSequenceNumber",
                     "PreviewD3DLastSubmittedSourcePtsTicks",
                     "PreviewD3DLastSubmittedUtcUnixMs",
                     "PreviewD3DLastRenderedPreviewPresentId",
                     "PreviewD3DLastRenderedSourceSequenceNumber",
                     "PreviewD3DLastRenderedSourcePtsTicks",
                     "PreviewD3DLastRenderedUtcUnixMs",
                     "PreviewD3DLastRenderedSchedulerToPresentMs",
                     "PreviewD3DLastRenderedPipelineLatencyMs",
                     "PreviewD3DLastDroppedPreviewPresentId",
                     "PreviewD3DLastDroppedSourceSequenceNumber",
                     "PreviewD3DLastDroppedSourcePtsTicks",
                     "PreviewD3DLastDroppedUtcUnixMs",
                     "PreviewD3DLastDropReason",
                     "PreviewD3DRecentSlowFrames",
                     "PreviewPacingLikelySlowStage",
                     "PreviewPacingSlowStageConfidence",
                     "PreviewPacingSlowStageEvidence",
                     "ProcessCpuPercent",
                     "ProcessCpuTotalProcessorTimeMs"
                 })
        {
            AssertNotNull(automationSnapshotType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"AutomationSnapshot.{prop}");
        }

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
}
