using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    // ── D3D11PreviewRenderer: ComputeLetterboxRect ──

    private static Task D3D11PreviewRenderer_ComputeLetterboxRect_CalculatesCorrectly()
    {
        var rendererType = RequireType("ElgatoCapture.Services.Preview.D3D11PreviewRenderer");
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
        var rendererType = RequireType("ElgatoCapture.Services.Preview.D3D11PreviewRenderer");

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
        var rendererType = RequireType("ElgatoCapture.Services.Preview.D3D11PreviewRenderer");
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
        var metricsType = RequireType("ElgatoCapture.Services.Preview.D3D11PreviewRenderer+PresentCadenceMetrics");

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
        var rendererType = RequireType("ElgatoCapture.Services.Preview.D3D11PreviewRenderer");
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
        var rendererType = RequireType("ElgatoCapture.Services.Preview.D3D11PreviewRenderer");
        var source = ReadRepoFile("ElgatoCapture/Services/Preview/D3D11PreviewRenderer.cs");
        var renderSource = ReadRepoFile("ElgatoCapture/Services/Preview/D3D11PreviewRenderer.Rendering.cs");
        AssertContains(source, "ELGATOCAPTURE_PREVIEW_RENDER_MMCSS_TASK\") ?? \"Playback\"");
        AssertContains(source, "ELGATOCAPTURE_PREVIEW_DXGI_FRAME_STATS_SAMPLE_INTERVAL");
        AssertContains(source, "private long _dxgiFrameStatisticsFrameCounter;");
        AssertContains(source, "private long _dxgiFrameStatisticsLastSampleFrameCounter;");
        AssertContains(source, "var frameCounter = Interlocked.Increment(ref _dxgiFrameStatisticsFrameCounter);");
        AssertContains(source, "frameCounter % _dxgiFrameStatisticsSampleIntervalFrames != 0");
        AssertContains(source, "_dxgiFrameStatisticsLastSampleFrameCounter = frameCounter;");
        AssertContains(source, "frameStatisticsLastSampleFrameCounter == frameStatisticsFrameCounter");
        AssertContains(source, "private int _pendingFrameCount;");
        AssertContains(source, "public int PendingFrameCount => Math.Max(0, Volatile.Read(ref _pendingFrameCount));");
        AssertContains(source, "var pendingFrameCount = Interlocked.Increment(ref _pendingFrameCount);\n            _pendingFrames.Enqueue(frame);");
        AssertContains(source, "private bool TryDequeuePendingFrame(out PendingFrame frame)");
        AssertContains(source, "DecrementPendingFrameCount();");
        AssertDoesNotContain(source, "_pendingFrames.Count");
        AssertDoesNotContain(source, "_pendingFrames.Enqueue(frame);\n            var pendingFrameCount = Interlocked.Increment(ref _pendingFrameCount);");
        AssertContains(source, "private void TrackFrameDropped(PendingFrame frame, string reason)\n    {\n        Interlocked.Increment(ref _framesDropped);");
        AssertDoesNotContain(source, "TrackFrameDropped(frame, \"renderer-stopped\");\n                frame.Dispose();\n                Interlocked.Increment(ref _framesDropped);");
        AssertDoesNotContain(source, "TrackFrameDropped(oldest, \"renderer-backlog\");\n                    oldest.Dispose();\n                    Interlocked.Increment(ref _framesDropped);");
        AssertDoesNotContain(renderSource, "_pendingFrames.TryDequeue");
        AssertContains(renderSource, "var framesRenderedBefore = Interlocked.Read(ref _framesRendered);");
        AssertContains(renderSource, "if (Interlocked.Read(ref _framesRendered) == framesRenderedBefore)\n                    {\n                        TrackFrameDropped(frame, \"render-skipped\");\n                    }");
        AssertNotNull(rendererType.GetProperty("SwapChainAddress", BindingFlags.Public | BindingFlags.Instance), "D3D11PreviewRenderer.SwapChainAddress");
        AssertNotNull(rendererType.GetMethod("GetRenderCpuTimingMetrics", BindingFlags.Public | BindingFlags.Instance), "D3D11PreviewRenderer.GetRenderCpuTimingMetrics");
        AssertNotNull(rendererType.GetMethod("GetFrameOwnershipMetrics", BindingFlags.Public | BindingFlags.Instance), "D3D11PreviewRenderer.GetFrameOwnershipMetrics");
        AssertNotNull(rendererType.GetMethod("GetDxgiFrameStatisticsMetrics", BindingFlags.Public | BindingFlags.Instance), "D3D11PreviewRenderer.GetDxgiFrameStatisticsMetrics");
        AssertNotNull(rendererType.GetMethod("TryGetDisplayClock", BindingFlags.Public | BindingFlags.Instance), "D3D11PreviewRenderer.TryGetDisplayClock");

        var displayClockSnapshotType = RequireType("ElgatoCapture.Services.Preview.PreviewDisplayClockSnapshot");
        foreach (var prop in new[] { "LastPresentTick", "FrameIntervalTicks", "ExpectedFrameIntervalMs", "SampleCount" })
        {
            AssertNotNull(displayClockSnapshotType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"PreviewDisplayClockSnapshot.{prop}");
        }

        var stageTimingType = RequireType("ElgatoCapture.Services.Preview.D3D11PreviewRenderer+CpuStageTimingMetrics");
        foreach (var prop in new[] { "SampleCount", "AverageMs", "P95Ms", "P99Ms", "MaxMs" })
        {
            AssertNotNull(stageTimingType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"CpuStageTimingMetrics.{prop}");
        }

        var renderTimingType = RequireType("ElgatoCapture.Services.Preview.D3D11PreviewRenderer+RenderCpuTimingMetrics");
        foreach (var prop in new[] { "InputUpload", "RenderSubmit", "PresentCall", "TotalFrame" })
        {
            AssertNotNull(renderTimingType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"RenderCpuTimingMetrics.{prop}");
        }

        var ownershipMetricsType = RequireType("ElgatoCapture.Services.Preview.D3D11PreviewRenderer+FrameOwnershipMetrics");
        foreach (var prop in new[]
                 {
                     "LastSubmittedPreviewPresentId",
                     "LastSubmittedSourceSequenceNumber",
                     "LastSubmittedUtcUnixMs",
                     "LastRenderedPreviewPresentId",
                     "LastRenderedSourceSequenceNumber",
                     "LastRenderedUtcUnixMs",
                     "LastRenderedSchedulerToPresentMs",
                     "LastDroppedPreviewPresentId",
                     "LastDroppedSourceSequenceNumber",
                     "LastDroppedUtcUnixMs",
                     "LastDropReason"
                 })
        {
            AssertNotNull(ownershipMetricsType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"FrameOwnershipMetrics.{prop}");
        }

        var dxgiFrameStatsType = RequireType("ElgatoCapture.Services.Preview.D3D11PreviewRenderer+DxgiFrameStatisticsMetrics");
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

        var previewSnapshotType = RequireType("ElgatoCapture.Models.PreviewRuntimeSnapshot");
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
                     "D3DLastSubmittedUtcUnixMs",
                     "D3DLastRenderedPreviewPresentId",
                     "D3DLastRenderedSourceSequenceNumber",
                     "D3DLastRenderedUtcUnixMs",
                     "D3DLastRenderedSchedulerToPresentMs",
                     "D3DLastDroppedPreviewPresentId",
                     "D3DLastDroppedSourceSequenceNumber",
                     "D3DLastDroppedUtcUnixMs",
                     "D3DLastDropReason",
                     "D3DRecentSlowFrames"
                 })
        {
            AssertNotNull(previewSnapshotType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"PreviewRuntimeSnapshot.{prop}");
        }

        var slowFrameDiagnosticType = RequireType("ElgatoCapture.Models.PreviewSlowFrameDiagnostic");
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

        var automationSnapshotType = RequireType("ElgatoCapture.Models.AutomationSnapshot");
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
                     "PreviewD3DLastSubmittedUtcUnixMs",
                     "PreviewD3DLastRenderedPreviewPresentId",
                     "PreviewD3DLastRenderedSourceSequenceNumber",
                     "PreviewD3DLastRenderedUtcUnixMs",
                     "PreviewD3DLastRenderedSchedulerToPresentMs",
                     "PreviewD3DLastDroppedPreviewPresentId",
                     "PreviewD3DLastDroppedSourceSequenceNumber",
                     "PreviewD3DLastDroppedUtcUnixMs",
                     "PreviewD3DLastDropReason",
                     "PreviewD3DRecentSlowFrames",
                     "ProcessCpuPercent",
                     "ProcessCpuTotalProcessorTimeMs"
                 })
        {
            AssertNotNull(automationSnapshotType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"AutomationSnapshot.{prop}");
        }

        var performanceTimelineEntryType = RequireType("ElgatoCapture.Models.PerformanceTimelineEntry");
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
                     "MjpegPreviewJitterUnderflowCount",
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
                     "PreviewD3DFrameLatencyWaitTimeoutCount",
                     "PreviewD3DFrameLatencyWaitP95Ms",
                     "PreviewD3DFrameLatencyWaitMaxMs",
                     "PreviewD3DFrameStatsRecentMissedRefreshCount",
                     "PreviewD3DFrameStatsRecentFailureCount",
                     "PreviewD3DLastRenderedSchedulerToPresentMs",
                     "PreviewD3DLastDropReason",
                     "FlashbackPlaybackState",
                     "FlashbackPlaybackP99FrameMs",
                     "FlashbackPlaybackDecodeP99Ms",
                     "FlashbackPlaybackPendingCommands",
                     "FlashbackPlaybackSubmitFailures",
                     "FlashbackPlaybackLastDropUtcUnixMs",
                     "FlashbackPlaybackLastDropReason",
                     "FlashbackPlaybackLastSubmitFailureUtcUnixMs",
                     "FlashbackPlaybackLastSubmitFailure",
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
                     "FatalCleanupInProgress",
                     "FlashbackCleanupInProgress",
                     "FlashbackExportActive",
                     "FlashbackExportStatus",
                     "FlashbackExportFailureKind",
                     "FlashbackExportPercent",
                     "FlashbackExportInPointMs",
                     "FlashbackExportOutPointMs",
                     "FlashbackExportMessage",
                     "FlashbackExportThroughputBytesPerSec",
                     "FlashbackExportLastProgressAgeMs",
                     "ProcessCpuPercent"
                 })
        {
            AssertNotNull(performanceTimelineEntryType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance), $"PerformanceTimelineEntry.{prop}");
        }

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_FrameCaptureCancellationClearsPendingRequest()
    {
        var rendererText = ReadRepoFile("ElgatoCapture/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var captureMethod = ExtractTextBetween(
            rendererText,
            "public Task<PreviewFrameCaptureResult> CaptureNextFrameAsync(string outputPath, CancellationToken cancellationToken)",
            "    public void SetSharedDevice");
        var captureServiceText = ReadRepoFile("ElgatoCapture/Services/Capture/CaptureService.cs")
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
}
