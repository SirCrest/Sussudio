using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    // ── D3D11PreviewRenderer: ComputeLetterboxRect ──

    private static Task D3D11PreviewRenderer_ComputeLetterboxRect_CalculatesCorrectly()
    {
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var method = rendererType.GetMethod("ComputeLetterboxRect",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ComputeLetterboxRect not found.");

        // 16:9 source into 16:9 dest → no letterbox, fills completely
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

        // 16:9 source into 4:3 dest → letterboxed (bars top/bottom)
        var result2 = method.Invoke(null, new object[] { 1920, 1080, 1024, 768 })!;
        var top2 = (int)resultType.GetField("Top")!.GetValue(result2)!;
        var bottom2 = (int)resultType.GetField("Bottom")!.GetValue(result2)!;
        var left2 = (int)resultType.GetField("Left")!.GetValue(result2)!;
        // Wider source → letterbox (top > 0, centered)
        AssertEqual(true, top2 > 0, "16:9 into 4:3 should letterbox (top > 0)");
        AssertEqual(0, left2, "16:9 into 4:3 should not pillarbox");

        // 4:3 source into 16:9 dest → pillarboxed (bars left/right)
        var result3 = method.Invoke(null, new object[] { 1024, 768, 1920, 1080 })!;
        var left3 = (int)resultType.GetField("Left")!.GetValue(result3)!;
        var top3 = (int)resultType.GetField("Top")!.GetValue(result3)!;
        AssertEqual(true, left3 > 0, "4:3 into 16:9 should pillarbox (left > 0)");
        AssertEqual(0, top3, "4:3 into 16:9 should not letterbox");

        return Task.CompletedTask;
    }

    // ── D3D11PreviewRenderer: CountLeadingBlackEdges / CountTrailingBlackEdges ──

    private static Task D3D11PreviewRenderer_BlackEdgeCounting_WorksCorrectly()
    {
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");

        var leadingMethod = rendererType.GetMethod("CountLeadingBlackEdges",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CountLeadingBlackEdges not found.");
        var trailingMethod = rendererType.GetMethod("CountTrailingBlackEdges",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CountTrailingBlackEdges not found.");

        // [true, true, false, true, false] → leading = 2, trailing = 0
        var values1 = new[] { true, true, false, true, false };
        AssertEqual(2, (int)leadingMethod.Invoke(null, new object[] { values1 })!, "Leading: 2 black edges");
        AssertEqual(0, (int)trailingMethod.Invoke(null, new object[] { values1 })!, "Trailing: 0 black edges");

        // [false, false, true, true, true] → leading = 0, trailing = 3
        var values2 = new[] { false, false, true, true, true };
        AssertEqual(0, (int)leadingMethod.Invoke(null, new object[] { values2 })!, "Leading: 0");
        AssertEqual(3, (int)trailingMethod.Invoke(null, new object[] { values2 })!, "Trailing: 3");

        // All true → leading = 5, trailing = 5
        var allTrue = new[] { true, true, true, true, true };
        AssertEqual(5, (int)leadingMethod.Invoke(null, new object[] { allTrue })!, "All true leading");
        AssertEqual(5, (int)trailingMethod.Invoke(null, new object[] { allTrue })!, "All true trailing");

        // All false → leading = 0, trailing = 0
        var allFalse = new[] { false, false, false };
        AssertEqual(0, (int)leadingMethod.Invoke(null, new object[] { allFalse })!, "All false leading");
        AssertEqual(0, (int)trailingMethod.Invoke(null, new object[] { allFalse })!, "All false trailing");

        return Task.CompletedTask;
    }

    // ── D3D11PreviewRenderer: IsDeviceLostException ──

    private static Task D3D11PreviewRenderer_IsDeviceLostException_ClassifiesCorrectly()
    {
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var method = rendererType.GetMethod("IsDeviceLostException",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("IsDeviceLostException not found.");

        // Regular exception → false
        var regularEx = new InvalidOperationException("test");
        AssertEqual(false, (bool)method.Invoke(null, new object[] { regularEx })!, "Regular exception is not device lost");

        // COMException with DeviceRemoved HRESULT → true
        var deviceRemovedEx = new System.Runtime.InteropServices.COMException("Device removed", unchecked((int)0x887A0005));
        AssertEqual(true, (bool)method.Invoke(null, new object[] { deviceRemovedEx })!, "DeviceRemoved COMException is device lost");

        // COMException with DeviceReset HRESULT → true
        var deviceResetEx = new System.Runtime.InteropServices.COMException("Device reset", unchecked((int)0x887A0007));
        AssertEqual(true, (bool)method.Invoke(null, new object[] { deviceResetEx })!, "DeviceReset COMException is device lost");

        // COMException with other HRESULT → false
        var otherComEx = new System.Runtime.InteropServices.COMException("Other", unchecked((int)0x80004005));
        AssertEqual(false, (bool)method.Invoke(null, new object[] { otherComEx })!, "Other COMException is not device lost");

        return Task.CompletedTask;
    }

    // ── D3D11PreviewRenderer: PresentCadenceMetrics struct shape ──

    private static Task D3D11PreviewRenderer_PresentCadenceMetrics_HasExpectedProperties()
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

    // ── D3D11PreviewRenderer: InitPngCrc32Table ──

    private static Task D3D11PreviewRenderer_InitPngCrc32Table_Generates256Entries()
    {
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var method = rendererType.GetMethod("InitPngCrc32Table",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("InitPngCrc32Table not found.");

        var table = (uint[])method.Invoke(null, null)!;
        AssertEqual(256, table.Length, "CRC32 table has 256 entries");

        // Entry 0 should be 0 (no bits set → no XOR)
        AssertEqual(0u, table[0], "CRC32 table[0] = 0");

        // All entries should be unique (well-formed CRC table)
        var unique = new HashSet<uint>(table);
        AssertEqual(256, unique.Count, "All 256 entries are unique");

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_DiagnosticsContract_ExposesSwapChainAndRenderTiming()
    {
        var rendererType = RequireType("Sussudio.Services.Preview.D3D11PreviewRenderer");
        var source = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs");
        var renderSource = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Rendering.cs");
        var captureSource = ReadRepoFile("Sussudio/Services/Capture/UnifiedVideoCapture.cs");
        AssertContains(source, "SUSSUDIO_PREVIEW_RENDER_MMCSS_TASK\") ?? \"Playback\"");
        AssertContains(source, "SUSSUDIO_PREVIEW_DXGI_FRAME_STATS_SAMPLE_INTERVAL");
        AssertContains(source, "private long _dxgiFrameStatisticsFrameCounter;");
        AssertContains(source, "private long _dxgiFrameStatisticsLastSampleFrameCounter;");
        AssertContains(source, "public PipelineLatencyMetrics GetPipelineLatencyMetrics()");
        AssertContains(source, "public double GetEstimatedPipelineLatencyMs()\n    {\n        lock (_pipelineLatencyLock)");
        AssertDoesNotContain(source, "public double GetEstimatedPipelineLatencyMs()\n    {\n        return GetPipelineLatencyMetrics().AverageMs;\n    }");
        AssertContains(source, "var sorted = (double[])samples.Clone();");
        AssertContains(source, "Array.Sort(sorted);");
        AssertContains(source, "var frameCounter = Interlocked.Increment(ref _dxgiFrameStatisticsFrameCounter);");
        AssertContains(source, "frameCounter % _dxgiFrameStatisticsSampleIntervalFrames != 0");
        AssertContains(source, "_dxgiFrameStatisticsLastSampleFrameCounter = frameCounter;");
        AssertContains(source, "frameStatisticsLastSampleFrameCounter == frameStatisticsFrameCounter");
        AssertContains(source, "private int _pendingFrameCount;");
        AssertContains(source, "public int PendingFrameCount => Math.Max(0, Volatile.Read(ref _pendingFrameCount));");
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
        var previewSinkType = RequireType("Sussudio.Services.Preview.IPreviewFrameSink");
        var submitTexture = previewSinkType.GetMethod("SubmitTexture", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("IPreviewFrameSink.SubmitTexture was not found.");
        AssertEqual(true, submitTexture.GetParameters().Any(parameter => parameter.Name == "sourceSequenceNumber"), "SubmitTexture source identity parameter");
        AssertEqual(true, submitTexture.GetParameters().Any(parameter => parameter.Name == "previewPresentId"), "SubmitTexture present identity parameter");
        AssertEqual(true, submitTexture.GetParameters().Any(parameter => parameter.Name == "sourcePtsTicks"), "SubmitTexture PTS identity parameter");
        var submitNv12PlaneTextures = previewSinkType.GetMethod("SubmitNv12PlaneTextures", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("IPreviewFrameSink.SubmitNv12PlaneTextures was not found.");
        AssertEqual(true, submitNv12PlaneTextures.GetParameters().Any(parameter => parameter.Name == "sourceSequenceNumber"), "SubmitNv12PlaneTextures source identity parameter");
        AssertEqual(true, submitNv12PlaneTextures.GetParameters().Any(parameter => parameter.Name == "previewPresentId"), "SubmitNv12PlaneTextures present identity parameter");
        AssertEqual(true, submitNv12PlaneTextures.GetParameters().Any(parameter => parameter.Name == "sourcePtsTicks"), "SubmitNv12PlaneTextures PTS identity parameter");
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

    private static Task D3D11PreviewRenderer_DropPendingFrames_DrainsQueueAndMarksGeneration()
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

    private static Task D3D11PreviewRenderer_FrameCaptureCancellationClearsPendingRequest()
    {
        var rendererText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var captureMethod = ExtractTextBetween(
            rendererText,
            "public Task<PreviewFrameCaptureResult> CaptureNextFrameAsync(string outputPath, CancellationToken cancellationToken)",
            "    public void SetSharedDevice");
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");

        AssertContains(captureMethod, "if (cancellationToken.IsCancellationRequested)");
        AssertContains(captureMethod, "Preview frame capture canceled.");
        AssertContains(captureMethod, "CancellationTokenRegistration cancellationRegistration = default;");
        AssertContains(captureMethod, "cancellationToken.Register(");
        AssertContains(captureMethod, "Interlocked.CompareExchange(ref renderer._frameCaptureRequest, null, request)");
        AssertContains(captureMethod, "Interlocked.Exchange(ref renderer._frameCaptureOutputPath, null);");
        AssertContains(captureMethod, "PREVIEW_FRAME_CAPTURE_CANCELED");
        AssertContains(captureMethod, "_ = request.Task.ContinueWith(");
        AssertContains(captureServiceText, "return d3dSink.CaptureNextFrameAsync(outputPath, cancellationToken);");
        AssertDoesNotContain(captureServiceText, "cancellationToken.ThrowIfCancellationRequested();\n        return d3dSink.CaptureNextFrameAsync(outputPath);");

        return Task.CompletedTask;
    }

    private static Task SharedD3DDeviceManager_DuplicatesReferencesUnderLifecycleLock()
    {
        var managerType = RequireType("Sussudio.Services.Preview.SharedD3DDeviceManager");
        AssertNotNull(
            managerType.GetMethod("TryCreateDeviceReference", BindingFlags.Public | BindingFlags.Instance),
            "SharedD3DDeviceManager.TryCreateDeviceReference");

        var managerText = ReadRepoFile("Sussudio/Services/Preview/SharedD3DDeviceManager.cs")
            .Replace("\r\n", "\n");
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
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
        AssertContains(applyMethod, "d3dManager.TryCreateDeviceReference(out var sharedDevice, out var reason)");
        AssertContains(applyMethod, "UNIFIED_VIDEO_SHARED_DEVICE_APPLY_SKIP reason={reason}");
        AssertContains(applyMethod, "sharedDevice.Dispose();");
        AssertDoesNotContain(applyMethod, "capture.D3DManager?.Device");

        return Task.CompletedTask;
    }
}
