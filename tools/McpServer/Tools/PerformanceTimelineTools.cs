using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
// MCP projection over the in-app performance timeline. The tool formats trends
// and counters for investigation; it does not compute or mutate app state.
public static partial class PerformanceTimelineTools
{
    [McpServerTool, Description("Get a time-series performance timeline showing capture/preview frame times, D3D present CPU timing, DXGI missed refreshes, queue depths, drops, memory, GC, and thread pool metrics over the last ~2 minutes (240 samples at 500ms intervals). Use to identify trends, regressions, stutter, present-call blocking, and GC pressure.")]
    public static async Task<CallToolResult> get_performance_timeline(
        PipeClient pipeClient,
        [Description("Maximum number of timeline entries to return (default: 240, which is ~2 minutes)")] int maxEntries = 240,
        [Description("Target 1% low FPS for preview/playback budget diagnostics (default: 118).")] double targetOnePercentLowFps = 118)
    {
        var payload = new Dictionary<string, object?>
        {
            ["maxEntries"] = maxEntries
        };

        var response = await pipeClient.SendCommandAsync("GetPerformanceTimeline", payload).ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(response))
        {
            return McpToolResultFactory.FromResponse(response, GetMessage(response));
        }

        if (!response.TryGetProperty("Data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return McpToolResultFactory.FromText(
                "No timeline data available. The app may not have been running long enough to collect samples.",
                isError: true);
        }

        var entries = ReadTimelineRows(data);
        if (entries.Count == 0)
        {
            return McpToolResultFactory.FromText("No timeline entries collected yet.");
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
                CompactCell(string.IsNullOrWhiteSpace(e.MjpegPreviewJitterLastUnderflowReason)
                    ? e.MjpegPreviewJitterLastDropReason
                    : e.MjpegPreviewJitterLastUnderflowReason, 12),
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
                FormatCleanupCell(e.FatalCleanupInProgress, e.FlashbackCleanupInProgress, e.FlashbackForceRotateRequested, e.FlashbackForceRotateDraining),
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
            var targetFrameBudgetMs = targetOnePercentLowFps > 0
                ? 1000.0 / targetOnePercentLowFps
                : 0;
            builder.AppendLine();
            builder.AppendLine("== Trend Summary (first vs last sample) ==");
            builder.AppendLine($"Capture Avg:    {first.CaptureAvgMs:F1}ms -> {last.CaptureAvgMs:F1}ms (delta: {last.CaptureAvgMs - first.CaptureAvgMs:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"Capture P95:    {first.CaptureP95Ms:F1}ms -> {last.CaptureP95Ms:F1}ms (delta: {last.CaptureP95Ms - first.CaptureP95Ms:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"Capture P99:    {first.CaptureP99Ms:F1}ms -> {last.CaptureP99Ms:F1}ms (delta: {last.CaptureP99Ms - first.CaptureP99Ms:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"Capture Max:    {first.CaptureMaxMs:F1}ms -> {last.CaptureMaxMs:F1}ms (delta: {last.CaptureMaxMs - first.CaptureMaxMs:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"Preview Avg:    {first.PreviewAvgMs:F1}ms -> {last.PreviewAvgMs:F1}ms (delta: {last.PreviewAvgMs - first.PreviewAvgMs:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"Preview P95:    {first.PreviewP95Ms:F1}ms -> {last.PreviewP95Ms:F1}ms (delta: {last.PreviewP95Ms - first.PreviewP95Ms:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"Preview P99:    {first.PreviewP99Ms:F1}ms -> {last.PreviewP99Ms:F1}ms (delta: {last.PreviewP99Ms - first.PreviewP99Ms:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"Preview Max:    {first.PreviewMaxMs:F1}ms -> {last.PreviewMaxMs:F1}ms (delta: {last.PreviewMaxMs - first.PreviewMaxMs:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"Preview 5% Low: {first.PreviewFivePercentLowFps:F1}fps -> {last.PreviewFivePercentLowFps:F1}fps");
            builder.AppendLine($"Preview 1% Low: {first.PreviewOnePercentLowFps:F1}fps -> {last.PreviewOnePercentLowFps:F1}fps");
            builder.AppendLine($"Preview Slow%:  {first.PreviewSlowPct:F1}% -> {last.PreviewSlowPct:F1}% (delta: {last.PreviewSlowPct - first.PreviewSlowPct:+0.0;-0.0;0.0}%)");
            builder.AppendLine($"Visual Cadence: changes {first.VisualCadenceChangeObservedFps:F1}fps -> {last.VisualCadenceChangeObservedFps:F1}fps, repeat {first.VisualCadenceRepeatFramePercent:F1}% -> {last.VisualCadenceRepeatFramePercent:F1}%, confidence={FormatOptional(last.VisualCadenceMotionConfidence)}");
            builder.AppendLine($"MJPEG Fingerprint: input {first.MjpegPacketHashInputObservedFps:F1}fps -> {last.MjpegPacketHashInputObservedFps:F1}fps, unique {first.MjpegPacketHashUniqueObservedFps:F1}fps -> {last.MjpegPacketHashUniqueObservedFps:F1}fps, dup {first.MjpegPacketHashDuplicateFramePercent:F1}% -> {last.MjpegPacketHashDuplicateFramePercent:F1}%");
            builder.AppendLine($"Jitter Depth:   {FormatJitterDepthCell(first)} -> {FormatJitterDepthCell(last)} enabled={last.MjpegPreviewJitterEnabled}");
            builder.AppendLine($"Jitter Latency: P95 {first.MjpegPreviewJitterLatencyP95Ms:F1}ms -> {last.MjpegPreviewJitterLatencyP95Ms:F1}ms, max latest={last.MjpegPreviewJitterLatencyMaxMs:F1}ms");
            builder.AppendLine($"Jitter Drops:   total {first.MjpegPreviewJitterTotalDropped} -> {last.MjpegPreviewJitterTotalDropped}, cleared {first.MjpegPreviewJitterClearedDropCount} -> {last.MjpegPreviewJitterClearedDropCount}, deadline {first.MjpegPreviewJitterDeadlineDropCount} -> {last.MjpegPreviewJitterDeadlineDropCount}, underflows {first.MjpegPreviewJitterUnderflowCount} -> {last.MjpegPreviewJitterUnderflowCount}, resumeReprimes {first.MjpegPreviewJitterResumeReprimeCount} -> {last.MjpegPreviewJitterResumeReprimeCount}, lastReason={FormatOptional(last.MjpegPreviewJitterLastDropReason)}");
            builder.AppendLine($"Jitter Underflow: reason={FormatOptional(last.MjpegPreviewJitterLastUnderflowReason)} inputAge={last.MjpegPreviewJitterLastUnderflowInputAgeMs:F1}ms outputAge={last.MjpegPreviewJitterLastUnderflowOutputAgeMs:F1}ms scheduleLateMax={last.MjpegPreviewJitterMaxScheduleLateMs:F1}ms lateCountDelta={NonNegativeDelta(last.MjpegPreviewJitterScheduleLateCount, first.MjpegPreviewJitterScheduleLateCount)}");
            builder.AppendLine($"D3D Present P95:{first.PreviewD3DPresentP95Ms:F1}ms -> {last.PreviewD3DPresentP95Ms:F1}ms (delta: {last.PreviewD3DPresentP95Ms - first.PreviewD3DPresentP95Ms:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"D3D Total P95:  {first.PreviewD3DTotalP95Ms:F1}ms -> {last.PreviewD3DTotalP95Ms:F1}ms (delta: {last.PreviewD3DTotalP95Ms - first.PreviewD3DTotalP95Ms:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"D3D Input P99:  {first.PreviewD3DInputUploadP99Ms:F1}ms -> {last.PreviewD3DInputUploadP99Ms:F1}ms (delta: {last.PreviewD3DInputUploadP99Ms - first.PreviewD3DInputUploadP99Ms:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"D3D Render P99: {first.PreviewD3DRenderSubmitP99Ms:F1}ms -> {last.PreviewD3DRenderSubmitP99Ms:F1}ms (delta: {last.PreviewD3DRenderSubmitP99Ms - first.PreviewD3DRenderSubmitP99Ms:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"D3D Present P99:{first.PreviewD3DPresentP99Ms:F1}ms -> {last.PreviewD3DPresentP99Ms:F1}ms (delta: {last.PreviewD3DPresentP99Ms - first.PreviewD3DPresentP99Ms:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"D3D Total P99:  {first.PreviewD3DTotalP99Ms:F1}ms -> {last.PreviewD3DTotalP99Ms:F1}ms (delta: {last.PreviewD3DTotalP99Ms - first.PreviewD3DTotalP99Ms:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"D3D Pipe P99:   {first.PreviewD3DPipelineP99Ms:F1}ms -> {last.PreviewD3DPipelineP99Ms:F1}ms (max latest={last.PreviewD3DPipelineMaxMs:F1}ms)");
            builder.AppendLine($"D3D P99 Bottleneck: {FormatD3DP99Bottleneck(first)} -> {FormatD3DP99Bottleneck(last)}");
            builder.AppendLine($"D3D Wait P95:   {first.PreviewD3DFrameLatencyWaitP95Ms:F1}ms -> {last.PreviewD3DFrameLatencyWaitP95Ms:F1}ms (timeouts: {first.PreviewD3DFrameLatencyWaitTimeouts} -> {last.PreviewD3DFrameLatencyWaitTimeouts}, max latest={last.PreviewD3DFrameLatencyWaitMaxMs:F1}ms)");
            builder.AppendLine($"D3D Sched->Prs: {first.PreviewD3DSchedulerToPresentMs:F1}ms -> {last.PreviewD3DSchedulerToPresentMs:F1}ms (latest rendered frame)");
            builder.AppendLine($"D3D Last Pipe:  {first.PreviewD3DLastPipelineLatencyMs:F1}ms -> {last.PreviewD3DLastPipelineLatencyMs:F1}ms (latest rendered frame)");
            builder.AppendLine($"D3D Missed:     {first.PreviewD3DRecentMissed} -> {last.PreviewD3DRecentMissed} (latest-window delta: {last.PreviewD3DRecentMissed - first.PreviewD3DRecentMissed:+0;-0;0})");
            builder.AppendLine($"D3D Stat Fails: {first.PreviewD3DRecentFailures} -> {last.PreviewD3DRecentFailures} (latest-window delta: {last.PreviewD3DRecentFailures - first.PreviewD3DRecentFailures:+0;-0;0})");
            builder.AppendLine($"D3D Last Drop:  {FormatOptional(last.PreviewD3DLastDropReason)}");
            builder.AppendLine($"Preview Slow Stage: {FormatOptional(first.PreviewPacingLikelySlowStage)}/{FormatOptional(first.PreviewPacingSlowStageConfidence)} -> {FormatOptional(last.PreviewPacingLikelySlowStage)}/{FormatOptional(last.PreviewPacingSlowStageConfidence)} evidence={FormatOptional(last.PreviewPacingSlowStageEvidence)}");
            builder.AppendLine($"Flashback State:{FormatOptional(first.FlashbackPlaybackState)} -> {FormatOptional(last.FlashbackPlaybackState)}");
            builder.AppendLine($"Flashback target:{first.FlashbackPlaybackTargetFps:F1}fps -> {last.FlashbackPlaybackTargetFps:F1}fps observed:{first.FlashbackPlaybackObservedFps:F1}fps -> {last.FlashbackPlaybackObservedFps:F1}fps 5%Low:{first.FlashbackPlaybackFivePercentLowFps:F1}fps -> {last.FlashbackPlaybackFivePercentLowFps:F1}fps 1%Low:{first.FlashbackPlaybackOnePercentLowFps:F1}fps -> {last.FlashbackPlaybackOnePercentLowFps:F1}fps");
            builder.AppendLine($"Flashback P99:  {first.FlashbackPlaybackP99FrameMs:F1}ms -> {last.FlashbackPlaybackP99FrameMs:F1}ms (max latest={last.FlashbackPlaybackMaxFrameMs:F1}ms)");
            builder.AppendLine($"Flashback Decode:{first.FlashbackPlaybackDecodeP99Ms:F1}ms -> {last.FlashbackPlaybackDecodeP99Ms:F1}ms (max latest={last.FlashbackPlaybackDecodeMaxMs:F1}ms phase={FormatOptional(last.FlashbackPlaybackMaxDecodePhase)} receive={last.FlashbackPlaybackMaxDecodeReceiveMs:F1}ms feed={last.FlashbackPlaybackMaxDecodeFeedMs:F1}ms read={last.FlashbackPlaybackMaxDecodeReadMs:F1}ms send={last.FlashbackPlaybackMaxDecodeSendMs:F1}ms audio={last.FlashbackPlaybackMaxDecodeAudioMs:F1}ms convert={last.FlashbackPlaybackMaxDecodeConvertMs:F1}ms)");
            builder.AppendLine($"Flashback AudioMaster: unavailable={first.FlashbackPlaybackAudioMasterUnavailableFallbacks}->{last.FlashbackPlaybackAudioMasterUnavailableFallbacks} stale={first.FlashbackPlaybackAudioMasterStaleFallbacks}->{last.FlashbackPlaybackAudioMasterStaleFallbacks} driftOutlier={first.FlashbackPlaybackAudioMasterDriftOutlierFallbacks}->{last.FlashbackPlaybackAudioMasterDriftOutlierFallbacks} last={FormatOptional(last.FlashbackPlaybackAudioMasterLastFallbackReason)} age={last.FlashbackPlaybackAudioMasterLastFallbackClockAgeMs:F1}ms");
            builder.AppendLine($"Flashback Slow%:{first.FlashbackPlaybackSlowFramePercent:F1}% -> {last.FlashbackPlaybackSlowFramePercent:F1}%");
            builder.AppendLine($"Flashback Cmds: pending {first.FlashbackPlaybackPendingCommands} -> {last.FlashbackPlaybackPendingCommands}, maxPending latest={last.FlashbackPlaybackMaxPendingCommands}, maxLatency latest={last.FlashbackPlaybackMaxCommandQueueLatencyMs}ms maxLatencyCommand={FormatOptional(last.FlashbackPlaybackMaxCommandQueueLatencyCommand)}, failureUtc latest={last.FlashbackPlaybackLastCommandFailureUtcUnixMs}");
            builder.AppendLine($"Flashback Cmd Counters: enqueued {first.FlashbackPlaybackCommandsEnqueued} -> {last.FlashbackPlaybackCommandsEnqueued}, processed {first.FlashbackPlaybackCommandsProcessed} -> {last.FlashbackPlaybackCommandsProcessed}, dropped {first.FlashbackPlaybackCommandsDropped} -> {last.FlashbackPlaybackCommandsDropped}, skippedNotReady {first.FlashbackPlaybackCommandsSkippedNotReady} -> {last.FlashbackPlaybackCommandsSkippedNotReady}, scrubCoalesced {first.FlashbackPlaybackScrubUpdatesCoalesced} -> {last.FlashbackPlaybackScrubUpdatesCoalesced}, seekCoalesced {first.FlashbackPlaybackSeekCommandsCoalesced} -> {last.FlashbackPlaybackSeekCommandsCoalesced}, lastQueued={FormatOptional(last.FlashbackPlaybackLastCommandQueued)}, lastProcessed={FormatOptional(last.FlashbackPlaybackLastCommandProcessed)}");
            builder.AppendLine($"Flashback Failure: latest={FormatOptional(last.FlashbackPlaybackLastCommandFailure)}");
            builder.AppendLine($"Flashback Drops: submitFailures {first.FlashbackPlaybackSubmitFailures} -> {last.FlashbackPlaybackSubmitFailures}, lastSubmitFailure={FormatOptional(last.FlashbackPlaybackLastSubmitFailure)} failureUtc latest={last.FlashbackPlaybackLastSubmitFailureUtcUnixMs}, droppedFrames {first.FlashbackPlaybackDroppedFrames} -> {last.FlashbackPlaybackDroppedFrames}, lastDrop={FormatOptional(last.FlashbackPlaybackLastDropReason)} dropUtc latest={last.FlashbackPlaybackLastDropUtcUnixMs}, decodeSnaps {first.FlashbackPlaybackDecodeErrorSnaps} -> {last.FlashbackPlaybackDecodeErrorSnaps}");
            builder.AppendLine($"Flashback Enqueue Rejects: video {first.FlashbackVideoQueueRejectedFrames} -> {last.FlashbackVideoQueueRejectedFrames} last={FormatOptional(last.FlashbackVideoQueueLastRejectReason)}, gpu {first.FlashbackGpuQueueRejectedFrames} -> {last.FlashbackGpuQueueRejectedFrames} last={FormatOptional(last.FlashbackGpuQueueLastRejectReason)}");
            builder.AppendLine($"Flashback Stages: switches {first.FlashbackPlaybackSegmentSwitches} -> {last.FlashbackPlaybackSegmentSwitches}, fmp4Reopens {first.FlashbackPlaybackFmp4Reopens} -> {last.FlashbackPlaybackFmp4Reopens}, writeHeadWaits {first.FlashbackPlaybackWriteHeadWaits} -> {last.FlashbackPlaybackWriteHeadWaits}, nearLiveSnaps {first.FlashbackPlaybackNearLiveSnaps} -> {last.FlashbackPlaybackNearLiveSnaps}, lastWriteHeadGap latest={last.FlashbackPlaybackLastWriteHeadWaitGapMs}ms");
            builder.AppendLine($"Cleanup State:  fatal={last.FatalCleanupInProgress} flashback={last.FlashbackCleanupInProgress} forceRotateRequested={last.FlashbackForceRotateRequested} forceRotateDraining={last.FlashbackForceRotateDraining}");
            builder.AppendLine($"Export State:    {FormatOptional(first.FlashbackExportStatus)} -> {FormatOptional(last.FlashbackExportStatus)} active={last.FlashbackExportActive} kind={FormatOptional(last.FlashbackExportFailureKind)}");
            builder.AppendLine($"Export Message:  {FormatOptional(last.FlashbackExportMessage)}");
            builder.AppendLine($"Export Progress: {first.FlashbackExportPercent:F1}% -> {last.FlashbackExportPercent:F1}% segments={last.FlashbackExportSegmentsProcessed}/{last.FlashbackExportTotalSegments}");
            builder.AppendLine($"Export Range:    in={last.FlashbackExportInPointMs}ms out={FormatExportOutPoint(last.FlashbackExportOutPointMs)}");
            builder.AppendLine($"Export Output:   {FormatBytes(first.FlashbackExportOutputBytes)} -> {FormatBytes(last.FlashbackExportOutputBytes)} throughput={FormatBytesPerSecond(last.FlashbackExportThroughputBytesPerSec)} elapsed={last.FlashbackExportElapsedMs}ms lastProgressAge={last.FlashbackExportLastProgressAgeMs}ms");
            builder.AppendLine($"Capture Rate:   {first.CaptureFps:F1}fps -> {last.CaptureFps:F1}fps (derived avg)");
            builder.AppendLine($"Capture 5% Low: {first.CaptureFivePercentLowFps:F1}fps -> {last.CaptureFivePercentLowFps:F1}fps");
            builder.AppendLine($"Capture 1% Low: {first.CaptureOnePercentLowFps:F1}fps -> {last.CaptureOnePercentLowFps:F1}fps");
            builder.AppendLine($"Preview Rate:   {first.PreviewFps:F1}fps -> {last.PreviewFps:F1}fps (derived avg)");
            builder.AppendLine($"Video Drops:    {first.VidDrops} -> {last.VidDrops} (delta: {last.VidDrops - first.VidDrops:+0;-0;0})");
            builder.AppendLine($"Working Set:    {first.WorkingMb:F1}MB -> {last.WorkingMb:F1}MB (delta: {last.WorkingMb - first.WorkingMb:+0.0;-0.0;0.0}MB)");
            builder.AppendLine($"Managed Heap:   {first.ManagedMb:F1}MB -> {last.ManagedMb:F1}MB (delta: {last.ManagedMb - first.ManagedMb:+0.0;-0.0;0.0}MB)");
            builder.AppendLine($"GC Gen0:        {first.Gen0} -> {last.Gen0} (delta: {last.Gen0 - first.Gen0:+0;-0;0})");
            builder.AppendLine($"GC Gen2:        {first.Gen2} -> {last.Gen2} (delta: {last.Gen2 - first.Gen2:+0;-0;0})");
            builder.AppendLine($"GC Pause%:      {first.GcPause:F1}% -> {last.GcPause:F1}% (delta: {last.GcPause - first.GcPause:+0.0;-0.0;0.0}%)");

            if (targetOnePercentLowFps > 0)
            {
                builder.AppendLine();
                builder.AppendLine($"== 1% Low Target Summary ({targetOnePercentLowFps:0.##}fps, budget {targetFrameBudgetMs:0.###}ms) ==");
                AppendOnePercentLowTargetSummary(builder, entries, targetOnePercentLowFps, targetFrameBudgetMs);
                AppendPressureSummary(builder, entries, targetFrameBudgetMs);
            }
        }

        return McpToolResultFactory.FromResponse(response, builder.ToString().TrimEnd());
    }

}
