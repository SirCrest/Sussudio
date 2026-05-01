using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using ElgatoCapture.Tools;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class PerformanceTimelineTools
{
    [McpServerTool, Description("Get a time-series performance timeline showing capture/preview frame times, D3D present CPU timing, DXGI missed refreshes, queue depths, drops, memory, GC, and thread pool metrics over the last ~2 minutes (240 samples at 500ms intervals). Use to identify trends, regressions, stutter, present-call blocking, and GC pressure.")]
    public static async Task<string> get_performance_timeline(
        PipeClient pipeClient,
        [Description("Maximum number of timeline entries to return (default: 240, which is ~2 minutes)")] int maxEntries = 240)
    {
        var payload = new Dictionary<string, object?>
        {
            ["maxEntries"] = maxEntries
        };

        var response = await pipeClient.SendCommandAsync("GetPerformanceTimeline", payload).ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(response))
        {
            return GetMessage(response);
        }

        if (!response.TryGetProperty("Data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return "No timeline data available. The app may not have been running long enough to collect samples.";
        }

        var entries = new List<TimelineRow>();
        foreach (var item in data.EnumerateArray())
        {
            entries.Add(new TimelineRow
            {
                Timestamp = AutomationSnapshotFormatter.Get(item, "TimestampUtc"),
                CaptureFps = AutomationSnapshotFormatter.GetDouble(item, "CaptureFps"),
                PreviewFps = AutomationSnapshotFormatter.GetDouble(item, "PreviewFps"),
                VidQueue = AutomationSnapshotFormatter.GetInt(item, "VideoQueueDepth"),
                VidDrops = AutomationSnapshotFormatter.GetLong(item, "VideoDrops"),
                CaptureAvgMs = AutomationSnapshotFormatter.GetDouble(item, "CaptureCadenceAverageMs"),
                CaptureP95Ms = AutomationSnapshotFormatter.GetDouble(item, "CaptureCadenceP95Ms"),
                CaptureP99Ms = AutomationSnapshotFormatter.GetDouble(item, "CaptureCadenceP99Ms"),
                CaptureMaxMs = AutomationSnapshotFormatter.GetDouble(item, "CaptureCadenceMaxMs"),
                CaptureOnePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, "CaptureCadenceOnePercentLowFps"),
                PreviewAvgMs = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceAverageMs"),
                PreviewP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceP95Ms"),
                PreviewMaxMs = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceMaxMs"),
                PreviewOnePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceOnePercentLowFps"),
                PreviewSlowPct = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceSlowFramePercent"),
                MjpegPreviewJitterEnabled = AutomationSnapshotFormatter.GetBool(item, "MjpegPreviewJitterEnabled"),
                MjpegPreviewJitterTargetDepth = AutomationSnapshotFormatter.GetInt(item, "MjpegPreviewJitterTargetDepth"),
                MjpegPreviewJitterMaxDepth = AutomationSnapshotFormatter.GetInt(item, "MjpegPreviewJitterMaxDepth"),
                MjpegPreviewJitterQueueDepth = AutomationSnapshotFormatter.GetInt(item, "MjpegPreviewJitterQueueDepth"),
                MjpegPreviewJitterTotalDropped = AutomationSnapshotFormatter.GetLong(item, "MjpegPreviewJitterTotalDropped"),
                MjpegPreviewJitterDeadlineDropCount = AutomationSnapshotFormatter.GetLong(item, "MjpegPreviewJitterDeadlineDropCount"),
                MjpegPreviewJitterUnderflowCount = AutomationSnapshotFormatter.GetLong(item, "MjpegPreviewJitterUnderflowCount"),
                MjpegPreviewJitterLatencyP95Ms = AutomationSnapshotFormatter.GetDouble(item, "MjpegPreviewJitterLatencyP95Ms"),
                MjpegPreviewJitterLatencyMaxMs = AutomationSnapshotFormatter.GetDouble(item, "MjpegPreviewJitterLatencyMaxMs"),
                MjpegPreviewJitterLastDropReason = AutomationSnapshotFormatter.Get(item, "MjpegPreviewJitterLastDropReason"),
                PreviewD3DPending = AutomationSnapshotFormatter.GetInt(item, "PreviewD3DPendingFrameCount"),
                PreviewD3DPresentP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DPresentCallP95Ms"),
                PreviewD3DTotalP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DTotalFrameCpuP95Ms"),
                PreviewD3DInputUploadP99Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DInputUploadCpuP99Ms"),
                PreviewD3DRenderSubmitP99Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DRenderSubmitCpuP99Ms"),
                PreviewD3DPresentP99Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DPresentCallP99Ms"),
                PreviewD3DTotalP99Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DTotalFrameCpuP99Ms"),
                PreviewD3DRecentMissed = AutomationSnapshotFormatter.GetLong(item, "PreviewD3DFrameStatsRecentMissedRefreshCount"),
                PreviewD3DRecentFailures = AutomationSnapshotFormatter.GetLong(item, "PreviewD3DFrameStatsRecentFailureCount"),
                PreviewD3DSchedulerToPresentMs = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DLastRenderedSchedulerToPresentMs"),
                PreviewD3DLastDropReason = AutomationSnapshotFormatter.Get(item, "PreviewD3DLastDropReason"),
                FlashbackPlaybackState = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackState"),
                FlashbackPlaybackObservedFps = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackObservedFps"),
                FlashbackPlaybackP99FrameMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackP99FrameMs"),
                FlashbackPlaybackMaxFrameMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackMaxFrameMs"),
                FlashbackPlaybackOnePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackOnePercentLowFps"),
                FlashbackPlaybackSlowFramePercent = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackSlowFramePercent"),
                FlashbackPlaybackDecodeP99Ms = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackDecodeP99Ms"),
                FlashbackPlaybackDecodeMaxMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackDecodeMaxMs"),
                FlashbackPlaybackPendingCommands = AutomationSnapshotFormatter.GetInt(item, "FlashbackPlaybackPendingCommands"),
                FlashbackPlaybackMaxPendingCommands = AutomationSnapshotFormatter.GetInt(item, "FlashbackPlaybackMaxPendingCommands"),
                FlashbackPlaybackMaxCommandQueueLatencyMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackMaxCommandQueueLatencyMs"),
                FlashbackPlaybackSubmitFailures = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackSubmitFailures"),
                FlashbackPlaybackDroppedFrames = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackDroppedFrames"),
                FlashbackPlaybackSegmentSwitches = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackSegmentSwitches"),
                FlashbackPlaybackFmp4Reopens = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackFmp4Reopens"),
                FlashbackPlaybackWriteHeadWaits = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackWriteHeadWaits"),
                FlashbackPlaybackNearLiveSnaps = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackNearLiveSnaps"),
                FlashbackPlaybackDecodeErrorSnaps = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackDecodeErrorSnaps"),
                FlashbackPlaybackLastWriteHeadWaitGapMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackLastWriteHeadWaitGapMs"),
                FlashbackPlaybackLastCommandFailureUtcUnixMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackLastCommandFailureUtcUnixMs"),
                FlashbackPlaybackLastCommandFailure = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackLastCommandFailure"),
                FatalCleanupInProgress = AutomationSnapshotFormatter.GetBool(item, "FatalCleanupInProgress"),
                FlashbackCleanupInProgress = AutomationSnapshotFormatter.GetBool(item, "FlashbackCleanupInProgress"),
                FlashbackExportActive = AutomationSnapshotFormatter.GetBool(item, "FlashbackExportActive"),
                FlashbackExportStatus = AutomationSnapshotFormatter.Get(item, "FlashbackExportStatus"),
                FlashbackExportFailureKind = AutomationSnapshotFormatter.Get(item, "FlashbackExportFailureKind"),
                FlashbackExportElapsedMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackExportElapsedMs"),
                FlashbackExportLastProgressAgeMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackExportLastProgressAgeMs"),
                FlashbackExportOutputBytes = AutomationSnapshotFormatter.GetLong(item, "FlashbackExportOutputBytes"),
                FlashbackExportThroughputBytesPerSec = AutomationSnapshotFormatter.GetDouble(item, "FlashbackExportThroughputBytesPerSec"),
                FlashbackExportSegmentsProcessed = AutomationSnapshotFormatter.GetInt(item, "FlashbackExportSegmentsProcessed"),
                FlashbackExportTotalSegments = AutomationSnapshotFormatter.GetInt(item, "FlashbackExportTotalSegments"),
                FlashbackExportPercent = AutomationSnapshotFormatter.GetDouble(item, "FlashbackExportPercent"),
                FlashbackExportInPointMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackExportInPointMs"),
                FlashbackExportOutPointMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackExportOutPointMs"),
                FlashbackExportMessage = AutomationSnapshotFormatter.Get(item, "FlashbackExportMessage"),
                LatencyMs = AutomationSnapshotFormatter.GetLong(item, "PipelineLatencyMs"),
                WorkingMb = AutomationSnapshotFormatter.GetDouble(item, "MemoryWorkingSetMb"),
                ManagedMb = AutomationSnapshotFormatter.GetDouble(item, "MemoryManagedHeapMb"),
                Gen0 = AutomationSnapshotFormatter.GetInt(item, "GcGen0Collections"),
                Gen1 = AutomationSnapshotFormatter.GetInt(item, "GcGen1Collections"),
                Gen2 = AutomationSnapshotFormatter.GetInt(item, "GcGen2Collections"),
                GcPause = AutomationSnapshotFormatter.GetDouble(item, "GcPauseTimePercent"),
                Workers = AutomationSnapshotFormatter.GetInt(item, "ThreadPoolWorkerAvailable"),
                IoThreads = AutomationSnapshotFormatter.GetInt(item, "ThreadPoolIoAvailable")
            });
        }

        if (entries.Count == 0)
        {
            return "No timeline entries collected yet.";
        }

        var builder = new StringBuilder();
        builder.AppendLine($"Performance Timeline ({entries.Count} samples)");
        builder.AppendLine();
        builder.AppendLine("Timestamp                | CapAvg | CapP95 | CapP99 | Cap1% | PrvAvg | PrvP95 | PrvSlow | JitD  | JitLat | JitDrop | JitUF | JitWhy       | D3DQ | D3DPrs | D3DTot | InP99 | RsP99 | PrP99 | TotP99 | D3DSch | D3DMiss | D3DDrop      | FbState | Fb1%  | FbP99 | FbDec | FbCmd | FbFail | FbStage        | Cln | ExStat  | ExKind | Ex%   | ExMBps | VidQ | VidDrop | LatMs | WorkMB | MgdMB  | G0   | G1   | G2   | GC%  | Wkr  | IO");
        builder.AppendLine(new string('-', 409));

        foreach (var e in entries)
        {
            builder.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0,-24} | {1,6:F1} | {2,6:F1} | {3,6:F1} | {4,5:F1} | {5,6:F1} | {6,6:F1} | {7,7:F1} | {8,-5} | {9,6:F1} | {10,7} | {11,5} | {12,-12} | {13,4} | {14,6:F1} | {15,6:F1} | {16,5:F1} | {17,5:F1} | {18,5:F1} | {19,6:F1} | {20,6:F1} | {21,7} | {22,-12} | {23,-7} | {24,5:F1} | {25,5:F1} | {26,5:F1} | {27,5} | {28,6} | {29,-14} | {30,-3} | {31,-7} | {32,-6} | {33,5:F1} | {34,6:F1} | {35,4} | {36,7} | {37,5} | {38,6:F1} | {39,6:F1} | {40,4} | {41,4} | {42,4} | {43,4:F1} | {44,4} | {45,4}",
                e.Timestamp,
                e.CaptureAvgMs,
                e.CaptureP95Ms,
                e.CaptureP99Ms,
                e.CaptureOnePercentLowFps,
                e.PreviewAvgMs,
                e.PreviewP95Ms,
                e.PreviewSlowPct,
                FormatJitterDepthCell(e),
                e.MjpegPreviewJitterLatencyP95Ms,
                e.MjpegPreviewJitterTotalDropped,
                e.MjpegPreviewJitterUnderflowCount,
                CompactCell(e.MjpegPreviewJitterLastDropReason, 12),
                e.PreviewD3DPending,
                e.PreviewD3DPresentP95Ms,
                e.PreviewD3DTotalP95Ms,
                e.PreviewD3DInputUploadP99Ms,
                e.PreviewD3DRenderSubmitP99Ms,
                e.PreviewD3DPresentP99Ms,
                e.PreviewD3DTotalP99Ms,
                e.PreviewD3DSchedulerToPresentMs,
                e.PreviewD3DRecentMissed,
                CompactCell(e.PreviewD3DLastDropReason, 12),
                CompactCell(e.FlashbackPlaybackState, 7),
                e.FlashbackPlaybackOnePercentLowFps,
                e.FlashbackPlaybackP99FrameMs,
                e.FlashbackPlaybackDecodeP99Ms,
                e.FlashbackPlaybackPendingCommands,
                e.FlashbackPlaybackSubmitFailures,
                FormatFlashbackStageCell(e),
                FormatCleanupCell(e.FatalCleanupInProgress, e.FlashbackCleanupInProgress),
                CompactCell(e.FlashbackExportStatus, 7),
                FormatExportFailureKind(e.FlashbackExportFailureKind),
                e.FlashbackExportPercent,
                e.FlashbackExportThroughputBytesPerSec / (1024.0 * 1024.0),
                e.VidQueue,
                e.VidDrops,
                e.LatencyMs,
                e.WorkingMb,
                e.ManagedMb,
                e.Gen0,
                e.Gen1,
                e.Gen2,
                e.GcPause,
                e.Workers,
                e.IoThreads));
        }

        if (entries.Count >= 2)
        {
            var first = entries[0];
            var last = entries[^1];
            builder.AppendLine();
            builder.AppendLine("== Trend Summary (first vs last sample) ==");
            builder.AppendLine($"Capture Avg:    {first.CaptureAvgMs:F1}ms -> {last.CaptureAvgMs:F1}ms (delta: {last.CaptureAvgMs - first.CaptureAvgMs:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"Capture P95:    {first.CaptureP95Ms:F1}ms -> {last.CaptureP95Ms:F1}ms (delta: {last.CaptureP95Ms - first.CaptureP95Ms:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"Capture P99:    {first.CaptureP99Ms:F1}ms -> {last.CaptureP99Ms:F1}ms (delta: {last.CaptureP99Ms - first.CaptureP99Ms:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"Capture Max:    {first.CaptureMaxMs:F1}ms -> {last.CaptureMaxMs:F1}ms (delta: {last.CaptureMaxMs - first.CaptureMaxMs:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"Preview Avg:    {first.PreviewAvgMs:F1}ms -> {last.PreviewAvgMs:F1}ms (delta: {last.PreviewAvgMs - first.PreviewAvgMs:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"Preview P95:    {first.PreviewP95Ms:F1}ms -> {last.PreviewP95Ms:F1}ms (delta: {last.PreviewP95Ms - first.PreviewP95Ms:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"Preview Max:    {first.PreviewMaxMs:F1}ms -> {last.PreviewMaxMs:F1}ms (delta: {last.PreviewMaxMs - first.PreviewMaxMs:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"Preview 1% Low: {first.PreviewOnePercentLowFps:F1}fps -> {last.PreviewOnePercentLowFps:F1}fps");
            builder.AppendLine($"Preview Slow%:  {first.PreviewSlowPct:F1}% -> {last.PreviewSlowPct:F1}% (delta: {last.PreviewSlowPct - first.PreviewSlowPct:+0.0;-0.0;0.0}%)");
            builder.AppendLine($"Jitter Depth:   {FormatJitterDepthCell(first)} -> {FormatJitterDepthCell(last)} enabled={last.MjpegPreviewJitterEnabled}");
            builder.AppendLine($"Jitter Latency: P95 {first.MjpegPreviewJitterLatencyP95Ms:F1}ms -> {last.MjpegPreviewJitterLatencyP95Ms:F1}ms, max latest={last.MjpegPreviewJitterLatencyMaxMs:F1}ms");
            builder.AppendLine($"Jitter Drops:   total {first.MjpegPreviewJitterTotalDropped} -> {last.MjpegPreviewJitterTotalDropped}, deadline {first.MjpegPreviewJitterDeadlineDropCount} -> {last.MjpegPreviewJitterDeadlineDropCount}, underflows {first.MjpegPreviewJitterUnderflowCount} -> {last.MjpegPreviewJitterUnderflowCount}, lastReason={FormatOptional(last.MjpegPreviewJitterLastDropReason)}");
            builder.AppendLine($"D3D Present P95:{first.PreviewD3DPresentP95Ms:F1}ms -> {last.PreviewD3DPresentP95Ms:F1}ms (delta: {last.PreviewD3DPresentP95Ms - first.PreviewD3DPresentP95Ms:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"D3D Total P95:  {first.PreviewD3DTotalP95Ms:F1}ms -> {last.PreviewD3DTotalP95Ms:F1}ms (delta: {last.PreviewD3DTotalP95Ms - first.PreviewD3DTotalP95Ms:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"D3D Input P99:  {first.PreviewD3DInputUploadP99Ms:F1}ms -> {last.PreviewD3DInputUploadP99Ms:F1}ms (delta: {last.PreviewD3DInputUploadP99Ms - first.PreviewD3DInputUploadP99Ms:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"D3D Render P99: {first.PreviewD3DRenderSubmitP99Ms:F1}ms -> {last.PreviewD3DRenderSubmitP99Ms:F1}ms (delta: {last.PreviewD3DRenderSubmitP99Ms - first.PreviewD3DRenderSubmitP99Ms:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"D3D Present P99:{first.PreviewD3DPresentP99Ms:F1}ms -> {last.PreviewD3DPresentP99Ms:F1}ms (delta: {last.PreviewD3DPresentP99Ms - first.PreviewD3DPresentP99Ms:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"D3D Total P99:  {first.PreviewD3DTotalP99Ms:F1}ms -> {last.PreviewD3DTotalP99Ms:F1}ms (delta: {last.PreviewD3DTotalP99Ms - first.PreviewD3DTotalP99Ms:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"D3D Sched->Prs: {first.PreviewD3DSchedulerToPresentMs:F1}ms -> {last.PreviewD3DSchedulerToPresentMs:F1}ms (latest rendered frame)");
            builder.AppendLine($"D3D Missed:     {first.PreviewD3DRecentMissed} -> {last.PreviewD3DRecentMissed} (latest-window delta: {last.PreviewD3DRecentMissed - first.PreviewD3DRecentMissed:+0;-0;0})");
            builder.AppendLine($"D3D Stat Fails: {first.PreviewD3DRecentFailures} -> {last.PreviewD3DRecentFailures} (latest-window delta: {last.PreviewD3DRecentFailures - first.PreviewD3DRecentFailures:+0;-0;0})");
            builder.AppendLine($"D3D Last Drop:  {FormatOptional(last.PreviewD3DLastDropReason)}");
            builder.AppendLine($"Flashback State:{FormatOptional(first.FlashbackPlaybackState)} -> {FormatOptional(last.FlashbackPlaybackState)}");
            builder.AppendLine($"Flashback 1%Low:{first.FlashbackPlaybackOnePercentLowFps:F1}fps -> {last.FlashbackPlaybackOnePercentLowFps:F1}fps");
            builder.AppendLine($"Flashback P99:  {first.FlashbackPlaybackP99FrameMs:F1}ms -> {last.FlashbackPlaybackP99FrameMs:F1}ms (max latest={last.FlashbackPlaybackMaxFrameMs:F1}ms)");
            builder.AppendLine($"Flashback Decode:{first.FlashbackPlaybackDecodeP99Ms:F1}ms -> {last.FlashbackPlaybackDecodeP99Ms:F1}ms (max latest={last.FlashbackPlaybackDecodeMaxMs:F1}ms)");
            builder.AppendLine($"Flashback Slow%:{first.FlashbackPlaybackSlowFramePercent:F1}% -> {last.FlashbackPlaybackSlowFramePercent:F1}%");
            builder.AppendLine($"Flashback Cmds: pending {first.FlashbackPlaybackPendingCommands} -> {last.FlashbackPlaybackPendingCommands}, maxPending latest={last.FlashbackPlaybackMaxPendingCommands}, maxLatency latest={last.FlashbackPlaybackMaxCommandQueueLatencyMs}ms, failureUtc latest={last.FlashbackPlaybackLastCommandFailureUtcUnixMs}");
            builder.AppendLine($"Flashback Failure: latest={FormatOptional(last.FlashbackPlaybackLastCommandFailure)}");
            builder.AppendLine($"Flashback Drops: submitFailures {first.FlashbackPlaybackSubmitFailures} -> {last.FlashbackPlaybackSubmitFailures}, droppedFrames {first.FlashbackPlaybackDroppedFrames} -> {last.FlashbackPlaybackDroppedFrames}, decodeSnaps {first.FlashbackPlaybackDecodeErrorSnaps} -> {last.FlashbackPlaybackDecodeErrorSnaps}");
            builder.AppendLine($"Flashback Stages: switches {first.FlashbackPlaybackSegmentSwitches} -> {last.FlashbackPlaybackSegmentSwitches}, fmp4Reopens {first.FlashbackPlaybackFmp4Reopens} -> {last.FlashbackPlaybackFmp4Reopens}, writeHeadWaits {first.FlashbackPlaybackWriteHeadWaits} -> {last.FlashbackPlaybackWriteHeadWaits}, nearLiveSnaps {first.FlashbackPlaybackNearLiveSnaps} -> {last.FlashbackPlaybackNearLiveSnaps}, lastWriteHeadGap latest={last.FlashbackPlaybackLastWriteHeadWaitGapMs}ms");
            builder.AppendLine($"Cleanup State:  fatal={last.FatalCleanupInProgress} flashback={last.FlashbackCleanupInProgress}");
            builder.AppendLine($"Export State:    {FormatOptional(first.FlashbackExportStatus)} -> {FormatOptional(last.FlashbackExportStatus)} active={last.FlashbackExportActive} kind={FormatOptional(last.FlashbackExportFailureKind)}");
            builder.AppendLine($"Export Message:  {FormatOptional(last.FlashbackExportMessage)}");
            builder.AppendLine($"Export Progress: {first.FlashbackExportPercent:F1}% -> {last.FlashbackExportPercent:F1}% segments={last.FlashbackExportSegmentsProcessed}/{last.FlashbackExportTotalSegments}");
            builder.AppendLine($"Export Range:    in={last.FlashbackExportInPointMs}ms out={FormatExportOutPoint(last.FlashbackExportOutPointMs)}");
            builder.AppendLine($"Export Output:   {FormatBytes(first.FlashbackExportOutputBytes)} -> {FormatBytes(last.FlashbackExportOutputBytes)} throughput={FormatBytesPerSecond(last.FlashbackExportThroughputBytesPerSec)} elapsed={last.FlashbackExportElapsedMs}ms lastProgressAge={last.FlashbackExportLastProgressAgeMs}ms");
            builder.AppendLine($"Capture Rate:   {first.CaptureFps:F1}fps -> {last.CaptureFps:F1}fps (derived avg)");
            builder.AppendLine($"Capture 1% Low: {first.CaptureOnePercentLowFps:F1}fps -> {last.CaptureOnePercentLowFps:F1}fps");
            builder.AppendLine($"Preview Rate:   {first.PreviewFps:F1}fps -> {last.PreviewFps:F1}fps (derived avg)");
            builder.AppendLine($"Video Drops:    {first.VidDrops} -> {last.VidDrops} (delta: {last.VidDrops - first.VidDrops:+0;-0;0})");
            builder.AppendLine($"Working Set:    {first.WorkingMb:F1}MB -> {last.WorkingMb:F1}MB (delta: {last.WorkingMb - first.WorkingMb:+0.0;-0.0;0.0}MB)");
            builder.AppendLine($"Managed Heap:   {first.ManagedMb:F1}MB -> {last.ManagedMb:F1}MB (delta: {last.ManagedMb - first.ManagedMb:+0.0;-0.0;0.0}MB)");
            builder.AppendLine($"GC Gen0:        {first.Gen0} -> {last.Gen0} (delta: {last.Gen0 - first.Gen0:+0;-0;0})");
            builder.AppendLine($"GC Gen2:        {first.Gen2} -> {last.Gen2} (delta: {last.Gen2 - first.Gen2:+0;-0;0})");
            builder.AppendLine($"GC Pause%:      {first.GcPause:F1}% -> {last.GcPause:F1}% (delta: {last.GcPause - first.GcPause:+0.0;-0.0;0.0}%)");
        }

        return builder.ToString().TrimEnd();
    }

    private static string GetMessage(JsonElement response)
    {
        return AutomationSnapshotFormatter.Get(response, "Message", "Command failed.");
    }

    private static string FormatOptional(string value)
        => string.IsNullOrWhiteSpace(value) ? "none" : value.Trim();

    private static string CompactCell(string value, int maxLength)
    {
        var compact = FormatOptional(value)
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Replace('|', '/');

        return compact.Length <= maxLength ? compact : compact[..Math.Max(0, maxLength - 1)] + "~";
    }

    private static string FormatCleanupCell(bool fatalCleanup, bool flashbackCleanup)
        => fatalCleanup ? "F" : flashbackCleanup ? "B" : "-";

    private static string FormatJitterDepthCell(TimelineRow row)
        => row.MjpegPreviewJitterEnabled
            ? $"{row.MjpegPreviewJitterQueueDepth}/{row.MjpegPreviewJitterTargetDepth}/{row.MjpegPreviewJitterMaxDepth}"
            : "-";

    private static string FormatFlashbackStageCell(TimelineRow row)
        => $"{row.FlashbackPlaybackSegmentSwitches}/{row.FlashbackPlaybackFmp4Reopens}/{row.FlashbackPlaybackWriteHeadWaits}/{row.FlashbackPlaybackNearLiveSnaps}/{row.FlashbackPlaybackLastWriteHeadWaitGapMs}";

    private static string FormatExportFailureKind(string failureKind)
        => CompactCell(string.IsNullOrWhiteSpace(failureKind) ? "-" : failureKind, 6);

    private static string FormatExportOutPoint(long outPointMs)
        => outPointMs < 0 ? "live" : $"{outPointMs}ms";

    private static string FormatBytes(long bytes)
    {
        if (bytes < 0) return "N/A";
        if (bytes >= 1024L * 1024L * 1024L) return $"{bytes / (1024.0 * 1024.0 * 1024.0):0.##}GB";
        if (bytes >= 1024L * 1024L) return $"{bytes / (1024.0 * 1024.0):0.##}MB";
        if (bytes >= 1024L) return $"{bytes / 1024.0:0.##}KB";
        return $"{bytes}B";
    }

    private static string FormatBytesPerSecond(double bytesPerSecond)
        => double.IsFinite(bytesPerSecond) && bytesPerSecond > 0
            ? $"{FormatBytes((long)bytesPerSecond)}/s"
            : "N/A";

    private sealed class TimelineRow
    {
        public string Timestamp { get; init; } = string.Empty;
        public double CaptureFps { get; init; }
        public double PreviewFps { get; init; }
        public int VidQueue { get; init; }
        public long VidDrops { get; init; }
        public double CaptureAvgMs { get; init; }
        public double CaptureP95Ms { get; init; }
        public double CaptureP99Ms { get; init; }
        public double CaptureMaxMs { get; init; }
        public double CaptureOnePercentLowFps { get; init; }
        public double PreviewAvgMs { get; init; }
        public double PreviewP95Ms { get; init; }
        public double PreviewMaxMs { get; init; }
        public double PreviewOnePercentLowFps { get; init; }
        public double PreviewSlowPct { get; init; }
        public bool MjpegPreviewJitterEnabled { get; init; }
        public int MjpegPreviewJitterTargetDepth { get; init; }
        public int MjpegPreviewJitterMaxDepth { get; init; }
        public int MjpegPreviewJitterQueueDepth { get; init; }
        public long MjpegPreviewJitterTotalDropped { get; init; }
        public long MjpegPreviewJitterDeadlineDropCount { get; init; }
        public long MjpegPreviewJitterUnderflowCount { get; init; }
        public double MjpegPreviewJitterLatencyP95Ms { get; init; }
        public double MjpegPreviewJitterLatencyMaxMs { get; init; }
        public string MjpegPreviewJitterLastDropReason { get; init; } = string.Empty;
        public int PreviewD3DPending { get; init; }
        public double PreviewD3DPresentP95Ms { get; init; }
        public double PreviewD3DTotalP95Ms { get; init; }
        public double PreviewD3DInputUploadP99Ms { get; init; }
        public double PreviewD3DRenderSubmitP99Ms { get; init; }
        public double PreviewD3DPresentP99Ms { get; init; }
        public double PreviewD3DTotalP99Ms { get; init; }
        public long PreviewD3DRecentMissed { get; init; }
        public long PreviewD3DRecentFailures { get; init; }
        public double PreviewD3DSchedulerToPresentMs { get; init; }
        public string PreviewD3DLastDropReason { get; init; } = string.Empty;
        public string FlashbackPlaybackState { get; init; } = string.Empty;
        public double FlashbackPlaybackObservedFps { get; init; }
        public double FlashbackPlaybackP99FrameMs { get; init; }
        public double FlashbackPlaybackMaxFrameMs { get; init; }
        public double FlashbackPlaybackOnePercentLowFps { get; init; }
        public double FlashbackPlaybackSlowFramePercent { get; init; }
        public double FlashbackPlaybackDecodeP99Ms { get; init; }
        public double FlashbackPlaybackDecodeMaxMs { get; init; }
        public int FlashbackPlaybackPendingCommands { get; init; }
        public int FlashbackPlaybackMaxPendingCommands { get; init; }
        public long FlashbackPlaybackMaxCommandQueueLatencyMs { get; init; }
        public long FlashbackPlaybackSubmitFailures { get; init; }
        public long FlashbackPlaybackDroppedFrames { get; init; }
        public long FlashbackPlaybackSegmentSwitches { get; init; }
        public long FlashbackPlaybackFmp4Reopens { get; init; }
        public long FlashbackPlaybackWriteHeadWaits { get; init; }
        public long FlashbackPlaybackNearLiveSnaps { get; init; }
        public long FlashbackPlaybackDecodeErrorSnaps { get; init; }
        public long FlashbackPlaybackLastWriteHeadWaitGapMs { get; init; }
        public long FlashbackPlaybackLastCommandFailureUtcUnixMs { get; init; }
        public string FlashbackPlaybackLastCommandFailure { get; init; } = string.Empty;
        public bool FatalCleanupInProgress { get; init; }
        public bool FlashbackCleanupInProgress { get; init; }
        public bool FlashbackExportActive { get; init; }
        public string FlashbackExportStatus { get; init; } = string.Empty;
        public string FlashbackExportFailureKind { get; init; } = string.Empty;
        public long FlashbackExportElapsedMs { get; init; }
        public long FlashbackExportLastProgressAgeMs { get; init; }
        public long FlashbackExportOutputBytes { get; init; }
        public double FlashbackExportThroughputBytesPerSec { get; init; }
        public int FlashbackExportSegmentsProcessed { get; init; }
        public int FlashbackExportTotalSegments { get; init; }
        public double FlashbackExportPercent { get; init; }
        public long FlashbackExportInPointMs { get; init; }
        public long FlashbackExportOutPointMs { get; init; }
        public string FlashbackExportMessage { get; init; } = string.Empty;
        public long LatencyMs { get; init; }
        public double WorkingMb { get; init; }
        public double ManagedMb { get; init; }
        public int Gen0 { get; init; }
        public int Gen1 { get; init; }
        public int Gen2 { get; init; }
        public double GcPause { get; init; }
        public int Workers { get; init; }
        public int IoThreads { get; init; }
    }
}
