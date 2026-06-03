using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using Sussudio.Models;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
// MCP projection over the in-app performance timeline. The tool formats trends
// and counters for investigation; it does not compute or mutate app state.
public static class PerformanceTimelineTools
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

        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.GetPerformanceTimeline, payload).ConfigureAwait(false);
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

        return McpToolResultFactory.FromResponse(response, BuildPerformanceTimelineText(entries, targetOnePercentLowFps));
    }

    private static List<TimelineRow> ReadTimelineRows(JsonElement data)
    {
        var entries = new List<TimelineRow>();
        foreach (var item in data.EnumerateArray())
        {
            var row = new TimelineRow
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
                CaptureFivePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, "CaptureCadenceFivePercentLowFps"),
                PreviewAvgMs = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceAverageMs"),
                PreviewP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceP95Ms"),
                PreviewP99Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceP99Ms"),
                PreviewMaxMs = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceMaxMs"),
                PreviewOnePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceOnePercentLowFps"),
                PreviewFivePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceFivePercentLowFps"),
                PreviewSlowPct = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceSlowFramePercent"),
            };

            PopulatePreviewTimelineRow(item, row);
            PopulateFlashbackPlaybackTimelineRow(item, row);
            PopulateFlashbackExportTimelineRow(item, row);
            PopulateSystemTimelineRow(item, row);
            entries.Add(row);
        }

        return entries;
    }

    private static void PopulatePreviewTimelineRow(JsonElement item, TimelineRow row)
    {
        row.VisualCadenceChangeObservedFps = AutomationSnapshotFormatter.GetDouble(item, "VisualCadenceChangeObservedFps");
        row.VisualCadenceRepeatFramePercent = AutomationSnapshotFormatter.GetDouble(item, "VisualCadenceRepeatFramePercent");
        row.VisualCadenceMotionConfidence = AutomationSnapshotFormatter.Get(item, "VisualCadenceMotionConfidence");
        row.MjpegPacketHashInputObservedFps = AutomationSnapshotFormatter.GetDouble(item, "MjpegPacketHashInputObservedFps");
        row.MjpegPacketHashUniqueObservedFps = AutomationSnapshotFormatter.GetDouble(item, "MjpegPacketHashUniqueObservedFps");
        row.MjpegPacketHashDuplicateFramePercent = AutomationSnapshotFormatter.GetDouble(item, "MjpegPacketHashDuplicateFramePercent");
        row.MjpegPreviewJitterEnabled = AutomationSnapshotFormatter.GetBool(item, "MjpegPreviewJitterEnabled");
        row.MjpegPreviewJitterTargetDepth = AutomationSnapshotFormatter.GetInt(item, "MjpegPreviewJitterTargetDepth");
        row.MjpegPreviewJitterMaxDepth = AutomationSnapshotFormatter.GetInt(item, "MjpegPreviewJitterMaxDepth");
        row.MjpegPreviewJitterQueueDepth = AutomationSnapshotFormatter.GetInt(item, "MjpegPreviewJitterQueueDepth");
        row.MjpegPreviewJitterTotalDropped = AutomationSnapshotFormatter.GetLong(item, "MjpegPreviewJitterTotalDropped");
        row.MjpegPreviewJitterDeadlineDropCount = AutomationSnapshotFormatter.GetLong(item, "MjpegPreviewJitterDeadlineDropCount");
        row.MjpegPreviewJitterClearedDropCount = AutomationSnapshotFormatter.GetLong(item, "MjpegPreviewJitterClearedDropCount");
        row.MjpegPreviewJitterUnderflowCount = AutomationSnapshotFormatter.GetLong(item, "MjpegPreviewJitterUnderflowCount");
        row.MjpegPreviewJitterResumeReprimeCount = AutomationSnapshotFormatter.GetLong(item, "MjpegPreviewJitterResumeReprimeCount");
        row.MjpegPreviewJitterLatencyP95Ms = AutomationSnapshotFormatter.GetDouble(item, "MjpegPreviewJitterLatencyP95Ms");
        row.MjpegPreviewJitterLatencyMaxMs = AutomationSnapshotFormatter.GetDouble(item, "MjpegPreviewJitterLatencyMaxMs");
        row.MjpegPreviewJitterLastDropReason = AutomationSnapshotFormatter.Get(item, "MjpegPreviewJitterLastDropReason");
        row.MjpegPreviewJitterLastUnderflowReason = AutomationSnapshotFormatter.Get(item, "MjpegPreviewJitterLastUnderflowReason");
        row.MjpegPreviewJitterLastUnderflowInputAgeMs = AutomationSnapshotFormatter.GetDouble(item, "MjpegPreviewJitterLastUnderflowInputAgeMs");
        row.MjpegPreviewJitterLastUnderflowOutputAgeMs = AutomationSnapshotFormatter.GetDouble(item, "MjpegPreviewJitterLastUnderflowOutputAgeMs");
        row.MjpegPreviewJitterMaxScheduleLateMs = AutomationSnapshotFormatter.GetDouble(item, "MjpegPreviewJitterMaxScheduleLateMs");
        row.MjpegPreviewJitterScheduleLateCount = AutomationSnapshotFormatter.GetLong(item, "MjpegPreviewJitterScheduleLateCount");
        row.PreviewD3DPending = AutomationSnapshotFormatter.GetInt(item, "PreviewD3DPendingFrameCount");
        row.PreviewD3DPresentP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DPresentCallP95Ms");
        row.PreviewD3DTotalP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DTotalFrameCpuP95Ms");
        row.PreviewD3DInputUploadP99Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DInputUploadCpuP99Ms");
        row.PreviewD3DRenderSubmitP99Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DRenderSubmitCpuP99Ms");
        row.PreviewD3DPresentP99Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DPresentCallP99Ms");
        row.PreviewD3DTotalP99Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DTotalFrameCpuP99Ms");
        row.PreviewD3DPipelineP99Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DPipelineLatencyP99Ms");
        row.PreviewD3DPipelineMaxMs = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DPipelineLatencyMaxMs");
        row.PreviewD3DFrameLatencyWaitTimeouts = AutomationSnapshotFormatter.GetLong(item, "PreviewD3DFrameLatencyWaitTimeoutCount");
        row.PreviewD3DFrameLatencyWaitP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DFrameLatencyWaitP95Ms");
        row.PreviewD3DFrameLatencyWaitMaxMs = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DFrameLatencyWaitMaxMs");
        row.PreviewD3DRecentMissed = AutomationSnapshotFormatter.GetLong(item, "PreviewD3DFrameStatsRecentMissedRefreshCount");
        row.PreviewD3DRecentFailures = AutomationSnapshotFormatter.GetLong(item, "PreviewD3DFrameStatsRecentFailureCount");
        row.PreviewD3DSchedulerToPresentMs = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DLastRenderedSchedulerToPresentMs");
        row.PreviewD3DLastPipelineLatencyMs = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DLastRenderedPipelineLatencyMs");
        row.PreviewD3DLastDropReason = AutomationSnapshotFormatter.Get(item, "PreviewD3DLastDropReason");
        row.PreviewPacingLikelySlowStage = AutomationSnapshotFormatter.Get(item, "PreviewPacingLikelySlowStage");
        row.PreviewPacingSlowStageConfidence = AutomationSnapshotFormatter.Get(item, "PreviewPacingSlowStageConfidence");
        row.PreviewPacingSlowStageEvidence = AutomationSnapshotFormatter.Get(item, "PreviewPacingSlowStageEvidence");
    }

    private static void PopulateFlashbackPlaybackTimelineRow(JsonElement item, TimelineRow row)
    {
        row.FlashbackPlaybackState = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackState");
        row.FlashbackPlaybackTargetFps = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackTargetFps");
        row.FlashbackPlaybackObservedFps = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackObservedFps");
        row.FlashbackPlaybackP99FrameMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackP99FrameMs");
        row.FlashbackPlaybackMaxFrameMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackMaxFrameMs");
        row.FlashbackPlaybackOnePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackOnePercentLowFps");
        row.FlashbackPlaybackFivePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackFivePercentLowFps");
        row.FlashbackPlaybackSlowFramePercent = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackSlowFramePercent");
        row.FlashbackPlaybackDecodeP99Ms = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackDecodeP99Ms");
        row.FlashbackPlaybackDecodeMaxMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackDecodeMaxMs");
        row.FlashbackPlaybackMaxDecodePhase = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackMaxDecodePhase");
        row.FlashbackPlaybackMaxDecodeReceiveMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackMaxDecodeReceiveMs");
        row.FlashbackPlaybackMaxDecodeFeedMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackMaxDecodeFeedMs");
        row.FlashbackPlaybackMaxDecodeReadMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackMaxDecodeReadMs");
        row.FlashbackPlaybackMaxDecodeSendMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackMaxDecodeSendMs");
        row.FlashbackPlaybackMaxDecodeAudioMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackMaxDecodeAudioMs");
        row.FlashbackPlaybackMaxDecodeConvertMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackMaxDecodeConvertMs");
        row.FlashbackPlaybackPendingCommands = AutomationSnapshotFormatter.GetInt(item, "FlashbackPlaybackPendingCommands");
        row.FlashbackPlaybackMaxPendingCommands = AutomationSnapshotFormatter.GetInt(item, "FlashbackPlaybackMaxPendingCommands");
        row.FlashbackPlaybackMaxCommandQueueLatencyMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackMaxCommandQueueLatencyMs");
        row.FlashbackPlaybackMaxCommandQueueLatencyCommand = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackMaxCommandQueueLatencyCommand");
        row.FlashbackPlaybackCommandsEnqueued = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackCommandsEnqueued");
        row.FlashbackPlaybackCommandsProcessed = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackCommandsProcessed");
        row.FlashbackPlaybackCommandsDropped = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackCommandsDropped");
        row.FlashbackPlaybackCommandsSkippedNotReady = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackCommandsSkippedNotReady");
        row.FlashbackPlaybackScrubUpdatesCoalesced = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackScrubUpdatesCoalesced");
        row.FlashbackPlaybackSeekCommandsCoalesced = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackSeekCommandsCoalesced");
        row.FlashbackPlaybackLastCommandQueued = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackLastCommandQueued");
        row.FlashbackPlaybackLastCommandProcessed = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackLastCommandProcessed");
        row.FlashbackPlaybackSubmitFailures = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackSubmitFailures");
        row.FlashbackPlaybackLastDropUtcUnixMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackLastDropUtcUnixMs");
        row.FlashbackPlaybackLastDropReason = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackLastDropReason");
        row.FlashbackPlaybackLastSubmitFailureUtcUnixMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackLastSubmitFailureUtcUnixMs");
        row.FlashbackPlaybackLastSubmitFailure = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackLastSubmitFailure");
        row.FlashbackPlaybackDroppedFrames = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackDroppedFrames");
        row.FlashbackPlaybackAudioMasterUnavailableFallbacks = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackAudioMasterUnavailableFallbacks");
        row.FlashbackPlaybackAudioMasterStaleFallbacks = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackAudioMasterStaleFallbacks");
        row.FlashbackPlaybackAudioMasterDriftOutlierFallbacks = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackAudioMasterDriftOutlierFallbacks");
        row.FlashbackPlaybackAudioMasterLastFallbackReason = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackAudioMasterLastFallbackReason");
        row.FlashbackPlaybackAudioMasterLastFallbackClockAgeMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackAudioMasterLastFallbackClockAgeMs");
        row.FlashbackPlaybackSegmentSwitches = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackSegmentSwitches");
        row.FlashbackPlaybackFmp4Reopens = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackFmp4Reopens");
        row.FlashbackPlaybackWriteHeadWaits = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackWriteHeadWaits");
        row.FlashbackPlaybackNearLiveSnaps = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackNearLiveSnaps");
        row.FlashbackPlaybackDecodeErrorSnaps = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackDecodeErrorSnaps");
        row.FlashbackPlaybackLastWriteHeadWaitGapMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackLastWriteHeadWaitGapMs");
        row.FlashbackPlaybackLastCommandFailureUtcUnixMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackLastCommandFailureUtcUnixMs");
        row.FlashbackPlaybackLastCommandFailure = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackLastCommandFailure");
        row.FlashbackVideoQueueRejectedFrames = AutomationSnapshotFormatter.GetLong(item, "FlashbackVideoQueueRejectedFrames");
        row.FlashbackVideoQueueLastRejectReason = AutomationSnapshotFormatter.Get(item, "FlashbackVideoQueueLastRejectReason");
        row.FlashbackGpuQueueRejectedFrames = AutomationSnapshotFormatter.GetLong(item, "FlashbackGpuQueueRejectedFrames");
        row.FlashbackGpuQueueLastRejectReason = AutomationSnapshotFormatter.Get(item, "FlashbackGpuQueueLastRejectReason");
        row.FatalCleanupInProgress = AutomationSnapshotFormatter.GetBool(item, "FatalCleanupInProgress");
        row.FlashbackCleanupInProgress = AutomationSnapshotFormatter.GetBool(item, "FlashbackCleanupInProgress");
        row.FlashbackForceRotateRequested = AutomationSnapshotFormatter.GetBool(item, "FlashbackForceRotateRequested");
        row.FlashbackForceRotateDraining = AutomationSnapshotFormatter.GetBool(item, "FlashbackForceRotateDraining");
    }

    private static void PopulateFlashbackExportTimelineRow(JsonElement item, TimelineRow row)
    {
        row.FlashbackExportActive = AutomationSnapshotFormatter.GetBool(item, "FlashbackExportActive");
        row.FlashbackExportStatus = AutomationSnapshotFormatter.Get(item, "FlashbackExportStatus");
        row.FlashbackExportFailureKind = AutomationSnapshotFormatter.Get(item, "FlashbackExportFailureKind");
        row.FlashbackExportElapsedMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackExportElapsedMs");
        row.FlashbackExportLastProgressAgeMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackExportLastProgressAgeMs");
        row.FlashbackExportOutputBytes = AutomationSnapshotFormatter.GetLong(item, "FlashbackExportOutputBytes");
        row.FlashbackExportThroughputBytesPerSec = AutomationSnapshotFormatter.GetDouble(item, "FlashbackExportThroughputBytesPerSec");
        row.FlashbackExportSegmentsProcessed = AutomationSnapshotFormatter.GetInt(item, "FlashbackExportSegmentsProcessed");
        row.FlashbackExportTotalSegments = AutomationSnapshotFormatter.GetInt(item, "FlashbackExportTotalSegments");
        row.FlashbackExportPercent = AutomationSnapshotFormatter.GetDouble(item, "FlashbackExportPercent");
        row.FlashbackExportInPointMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackExportInPointMs");
        row.FlashbackExportOutPointMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackExportOutPointMs");
        row.FlashbackExportMessage = AutomationSnapshotFormatter.Get(item, "FlashbackExportMessage");
    }

    private static void PopulateSystemTimelineRow(JsonElement item, TimelineRow row)
    {
        row.LatencyMs = AutomationSnapshotFormatter.GetLong(item, "PipelineLatencyMs");
        row.WorkingMb = AutomationSnapshotFormatter.GetDouble(item, "MemoryWorkingSetMb");
        row.ManagedMb = AutomationSnapshotFormatter.GetDouble(item, "MemoryManagedHeapMb");
        row.Gen0 = AutomationSnapshotFormatter.GetInt(item, "GcGen0Collections");
        row.Gen1 = AutomationSnapshotFormatter.GetInt(item, "GcGen1Collections");
        row.Gen2 = AutomationSnapshotFormatter.GetInt(item, "GcGen2Collections");
        row.GcPause = AutomationSnapshotFormatter.GetDouble(item, "GcPauseTimePercent");
        row.Workers = AutomationSnapshotFormatter.GetInt(item, "ThreadPoolWorkerAvailable");
        row.IoThreads = AutomationSnapshotFormatter.GetInt(item, "ThreadPoolIoAvailable");
    }

    private static string BuildPerformanceTimelineText(IReadOnlyList<TimelineRow> entries, double targetOnePercentLowFps)
    {
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

        AppendTrendSummary(builder, entries, targetOnePercentLowFps);

        return builder.ToString().TrimEnd();
    }

    private static void AppendTrendSummary(StringBuilder builder, IReadOnlyList<TimelineRow> entries, double targetOnePercentLowFps)
    {
        if (entries.Count < 2)
        {
            return;
        }

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
        AppendPreviewTrendSummary(builder, first, last);
        AppendFlashbackTrendSummary(builder, first, last);
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

    private static void AppendPreviewTrendSummary(StringBuilder builder, TimelineRow first, TimelineRow last)
    {
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
    }

    private static void AppendFlashbackTrendSummary(StringBuilder builder, TimelineRow first, TimelineRow last)
    {
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
        AppendFlashbackExportTrendSummary(builder, first, last);
    }

    private static void AppendFlashbackExportTrendSummary(StringBuilder builder, TimelineRow first, TimelineRow last)
    {
        builder.AppendLine($"Export State:    {FormatOptional(first.FlashbackExportStatus)} -> {FormatOptional(last.FlashbackExportStatus)} active={last.FlashbackExportActive} kind={FormatOptional(last.FlashbackExportFailureKind)}");
        builder.AppendLine($"Export Message:  {FormatOptional(last.FlashbackExportMessage)}");
        builder.AppendLine($"Export Progress: {first.FlashbackExportPercent:F1}% -> {last.FlashbackExportPercent:F1}% segments={last.FlashbackExportSegmentsProcessed}/{last.FlashbackExportTotalSegments}");
        builder.AppendLine($"Export Range:    in={last.FlashbackExportInPointMs}ms out={FormatExportOutPoint(last.FlashbackExportOutPointMs)}");
        builder.AppendLine($"Export Output:   {FormatBytes(first.FlashbackExportOutputBytes)} -> {FormatBytes(last.FlashbackExportOutputBytes)} throughput={FormatBytesPerSecond(last.FlashbackExportThroughputBytesPerSec)} elapsed={last.FlashbackExportElapsedMs}ms lastProgressAge={last.FlashbackExportLastProgressAgeMs}ms");
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

    private static string FormatJitterDepthCell(TimelineRow row)
        => row.MjpegPreviewJitterEnabled
            ? $"{row.MjpegPreviewJitterQueueDepth}/{row.MjpegPreviewJitterTargetDepth}/{row.MjpegPreviewJitterMaxDepth}"
            : "-";

    private static string FormatD3DP99Bottleneck(TimelineRow row)
    {
        var stages = new[]
        {
            ("input", row.PreviewD3DInputUploadP99Ms),
            ("render", row.PreviewD3DRenderSubmitP99Ms),
            ("present", row.PreviewD3DPresentP99Ms),
            ("wait", row.PreviewD3DFrameLatencyWaitP95Ms)
        };

        var dominant = stages
            .Where(stage => double.IsFinite(stage.Item2) && stage.Item2 > 0)
            .OrderByDescending(stage => stage.Item2)
            .FirstOrDefault();
        if (string.IsNullOrWhiteSpace(dominant.Item1))
        {
            return "none";
        }

        var namedTotal = stages
            .Where(stage => double.IsFinite(stage.Item2) && stage.Item2 > 0)
            .Sum(stage => stage.Item2);
        if (row.PreviewD3DTotalP99Ms > 0 &&
            row.PreviewD3DTotalP99Ms > namedTotal * 1.25)
        {
            return $"other({row.PreviewD3DTotalP99Ms:0.0}ms)";
        }

        return $"{dominant.Item1}({dominant.Item2:0.0}ms)";
    }

    private static string FormatCleanupCell(bool fatalCleanup, bool flashbackCleanup, bool forceRotateRequested, bool forceRotateDraining)
        => fatalCleanup ? "F" : flashbackCleanup ? "B" : forceRotateDraining ? "D" : forceRotateRequested ? "R" : "-";

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

    private static void AppendOnePercentLowTargetSummary(
        StringBuilder builder,
        IReadOnlyList<TimelineRow> entries,
        double targetOnePercentLowFps,
        double targetFrameBudgetMs)
    {
        AppendChannelTargetSummary(
            builder,
            "Preview",
            entries,
            static row => row.PreviewOnePercentLowFps,
            static row => row.PreviewP99Ms,
            static row => row.PreviewP95Ms,
            static row => FormatD3DP99Bottleneck(row),
            targetOnePercentLowFps,
            targetFrameBudgetMs);
        AppendChannelTargetSummary(
            builder,
            "Capture",
            entries,
            static row => row.CaptureOnePercentLowFps,
            static row => row.CaptureP95Ms,
            static row => row.CaptureP99Ms,
            static row => "capture",
            targetOnePercentLowFps,
            targetFrameBudgetMs);
        AppendChannelTargetSummary(
            builder,
            "Flashback",
            entries,
            static row => row.FlashbackPlaybackOnePercentLowFps,
            static row => row.FlashbackPlaybackP99FrameMs,
            static row => row.FlashbackPlaybackDecodeP99Ms,
            static row => FormatFlashbackStageCell(row),
            targetOnePercentLowFps,
            targetFrameBudgetMs);
    }

    private static void AppendChannelTargetSummary(
        StringBuilder builder,
        string label,
        IReadOnlyList<TimelineRow> entries,
        Func<TimelineRow, double> onePercentLowSelector,
        Func<TimelineRow, double> primaryMsSelector,
        Func<TimelineRow, double> secondaryMsSelector,
        Func<TimelineRow, string> clueSelector,
        double targetOnePercentLowFps,
        double targetFrameBudgetMs)
    {
        var valid = entries
            .Where(row => IsPositiveFinite(onePercentLowSelector(row)))
            .ToArray();
        if (valid.Length == 0)
        {
            builder.AppendLine($"{label}: no 1% low samples yet.");
            return;
        }

        var belowTarget = valid.Count(row => onePercentLowSelector(row) < targetOnePercentLowFps);
        var worst = valid.OrderBy(onePercentLowSelector).First();
        var latest = valid[^1];
        var targetMissPercent = belowTarget * 100.0 / valid.Length;
        var latestPrimaryOverBudgetMs = targetFrameBudgetMs > 0
            ? Math.Max(0, primaryMsSelector(latest) - targetFrameBudgetMs)
            : 0;

        builder.AppendLine(string.Create(
            CultureInfo.InvariantCulture,
            $"{label}: latest={onePercentLowSelector(latest):0.##}fps worst={onePercentLowSelector(worst):0.##}fps misses={belowTarget}/{valid.Length} ({targetMissPercent:0.#}%) latestPrimary={primaryMsSelector(latest):0.##}ms overBudget={latestPrimaryOverBudgetMs:0.##}ms secondary={secondaryMsSelector(latest):0.##}ms clue={clueSelector(latest)} worstAt={worst.Timestamp}"));
    }

    private static long NonNegativeDelta(long latest, long first)
        => latest >= first ? latest - first : 0;

    private static bool IsPositiveFinite(double value)
        => double.IsFinite(value) && value > 0;

    private static void AppendPressureSummary(
        StringBuilder builder,
        IReadOnlyList<TimelineRow> entries,
        double targetFrameBudgetMs)
    {
        if (entries.Count == 0 || targetFrameBudgetMs <= 0)
        {
            return;
        }

        var first = entries[0];
        var last = entries[^1];
        builder.AppendLine();
        builder.AppendLine("== Pressure Summary ==");
        builder.AppendLine(
            "Preview Pressure: " +
            $"overBudgetSamples input={CountOverBudget(entries, static row => row.PreviewD3DInputUploadP99Ms, targetFrameBudgetMs)} " +
            $"render={CountOverBudget(entries, static row => row.PreviewD3DRenderSubmitP99Ms, targetFrameBudgetMs)} " +
            $"present={CountOverBudget(entries, static row => row.PreviewD3DPresentP99Ms, targetFrameBudgetMs)} " +
            $"total={CountOverBudget(entries, static row => row.PreviewD3DTotalP99Ms, targetFrameBudgetMs)} " +
            $"wait={CountOverBudget(entries, static row => row.PreviewD3DFrameLatencyWaitP95Ms, targetFrameBudgetMs)} " +
            $"dxgiMissedSamples={CountWhere(entries, static row => row.PreviewD3DRecentMissed > 0)} " +
            $"dxgiFailureSamples={CountWhere(entries, static row => row.PreviewD3DRecentFailures > 0)} " +
            $"jitterDropsDelta={NonNegativeDelta(last.MjpegPreviewJitterTotalDropped, first.MjpegPreviewJitterTotalDropped)} " +
            $"clearedDropsDelta={NonNegativeDelta(last.MjpegPreviewJitterClearedDropCount, first.MjpegPreviewJitterClearedDropCount)} " +
            $"deadlineDropsDelta={NonNegativeDelta(last.MjpegPreviewJitterDeadlineDropCount, first.MjpegPreviewJitterDeadlineDropCount)} " +
            $"underflowsDelta={NonNegativeDelta(last.MjpegPreviewJitterUnderflowCount, first.MjpegPreviewJitterUnderflowCount)} " +
            $"resumeReprimesDelta={NonNegativeDelta(last.MjpegPreviewJitterResumeReprimeCount, first.MjpegPreviewJitterResumeReprimeCount)} " +
            $"visualChangeLatest={last.VisualCadenceChangeObservedFps:0.##}fps " +
            $"mjpegUniqueLatest={last.MjpegPacketHashUniqueObservedFps:0.##}fps");

        builder.AppendLine(
            "Flashback Pressure: " +
            $"p99OverBudget={CountOverBudget(entries, static row => row.FlashbackPlaybackP99FrameMs, targetFrameBudgetMs)} " +
            $"decodeOverBudget={CountOverBudget(entries, static row => row.FlashbackPlaybackDecodeP99Ms, targetFrameBudgetMs)} " +
            $"pendingCmdSamples={CountWhere(entries, static row => row.FlashbackPlaybackPendingCommands > 0)} " +
            $"cmdDropsDelta={NonNegativeDelta(last.FlashbackPlaybackCommandsDropped, first.FlashbackPlaybackCommandsDropped)} " +
            $"submitFailuresDelta={NonNegativeDelta(last.FlashbackPlaybackSubmitFailures, first.FlashbackPlaybackSubmitFailures)} " +
            $"droppedFramesDelta={NonNegativeDelta(last.FlashbackPlaybackDroppedFrames, first.FlashbackPlaybackDroppedFrames)} " +
            $"writeHeadWaitsDelta={NonNegativeDelta(last.FlashbackPlaybackWriteHeadWaits, first.FlashbackPlaybackWriteHeadWaits)} " +
            $"forceRotateSamples={CountWhere(entries, static row => row.FlashbackForceRotateRequested || row.FlashbackForceRotateDraining)}");

        builder.AppendLine(
            "System Pressure: " +
            $"videoDropsDelta={NonNegativeDelta(last.VidDrops, first.VidDrops)} " +
            $"gcGen2Delta={NonNegativeDelta(last.Gen2, first.Gen2)} " +
            $"gcPauseSamples={CountWhere(entries, static row => row.GcPause > 0)} " +
            $"lowWorkerSamples={CountWhere(entries, static row => row.Workers > 0 && row.Workers < 16)} " +
            $"managedHeapDeltaMb={(last.ManagedMb - first.ManagedMb):+0.0;-0.0;0.0} " +
            $"workingSetDeltaMb={(last.WorkingMb - first.WorkingMb):+0.0;-0.0;0.0}");
    }

    private static int CountOverBudget(
        IReadOnlyList<TimelineRow> entries,
        Func<TimelineRow, double> selector,
        double targetFrameBudgetMs)
        => CountWhere(entries, row => IsPositiveFinite(selector(row)) && selector(row) > targetFrameBudgetMs);

    private static int CountWhere(IReadOnlyList<TimelineRow> entries, Func<TimelineRow, bool> predicate)
    {
        var count = 0;
        foreach (var entry in entries)
        {
            if (predicate(entry))
            {
                count++;
            }
        }

        return count;
    }

    private sealed class TimelineRow
    {
        public string Timestamp { get; set; } = string.Empty;
        public double CaptureFps { get; set; }
        public double PreviewFps { get; set; }
        public int VidQueue { get; set; }
        public long VidDrops { get; set; }
        public double CaptureAvgMs { get; set; }
        public double CaptureP95Ms { get; set; }
        public double CaptureP99Ms { get; set; }
        public double CaptureMaxMs { get; set; }
        public double CaptureOnePercentLowFps { get; set; }
        public double CaptureFivePercentLowFps { get; set; }
        public double PreviewAvgMs { get; set; }
        public double PreviewP95Ms { get; set; }
        public double PreviewP99Ms { get; set; }
        public double PreviewMaxMs { get; set; }
        public double PreviewOnePercentLowFps { get; set; }
        public double PreviewFivePercentLowFps { get; set; }
        public double PreviewSlowPct { get; set; }
        public double VisualCadenceChangeObservedFps { get; set; }
        public double VisualCadenceRepeatFramePercent { get; set; }
        public string VisualCadenceMotionConfidence { get; set; } = string.Empty;
        public double MjpegPacketHashInputObservedFps { get; set; }
        public double MjpegPacketHashUniqueObservedFps { get; set; }
        public double MjpegPacketHashDuplicateFramePercent { get; set; }
        public bool MjpegPreviewJitterEnabled { get; set; }
        public int MjpegPreviewJitterTargetDepth { get; set; }
        public int MjpegPreviewJitterMaxDepth { get; set; }
        public int MjpegPreviewJitterQueueDepth { get; set; }
        public long MjpegPreviewJitterTotalDropped { get; set; }
        public long MjpegPreviewJitterDeadlineDropCount { get; set; }
        public long MjpegPreviewJitterClearedDropCount { get; set; }
        public long MjpegPreviewJitterUnderflowCount { get; set; }
        public long MjpegPreviewJitterResumeReprimeCount { get; set; }
        public double MjpegPreviewJitterLatencyP95Ms { get; set; }
        public double MjpegPreviewJitterLatencyMaxMs { get; set; }
        public string MjpegPreviewJitterLastDropReason { get; set; } = string.Empty;
        public string MjpegPreviewJitterLastUnderflowReason { get; set; } = string.Empty;
        public double MjpegPreviewJitterLastUnderflowInputAgeMs { get; set; }
        public double MjpegPreviewJitterLastUnderflowOutputAgeMs { get; set; }
        public double MjpegPreviewJitterMaxScheduleLateMs { get; set; }
        public long MjpegPreviewJitterScheduleLateCount { get; set; }
        public int PreviewD3DPending { get; set; }
        public double PreviewD3DPresentP95Ms { get; set; }
        public double PreviewD3DTotalP95Ms { get; set; }
        public double PreviewD3DInputUploadP99Ms { get; set; }
        public double PreviewD3DRenderSubmitP99Ms { get; set; }
        public double PreviewD3DPresentP99Ms { get; set; }
        public double PreviewD3DTotalP99Ms { get; set; }
        public double PreviewD3DPipelineP99Ms { get; set; }
        public double PreviewD3DPipelineMaxMs { get; set; }
        public long PreviewD3DFrameLatencyWaitTimeouts { get; set; }
        public double PreviewD3DFrameLatencyWaitP95Ms { get; set; }
        public double PreviewD3DFrameLatencyWaitMaxMs { get; set; }
        public long PreviewD3DRecentMissed { get; set; }
        public long PreviewD3DRecentFailures { get; set; }
        public double PreviewD3DSchedulerToPresentMs { get; set; }
        public double PreviewD3DLastPipelineLatencyMs { get; set; }
        public string PreviewD3DLastDropReason { get; set; } = string.Empty;
        public string PreviewPacingLikelySlowStage { get; set; } = string.Empty;
        public string PreviewPacingSlowStageConfidence { get; set; } = string.Empty;
        public string PreviewPacingSlowStageEvidence { get; set; } = string.Empty;
        public string FlashbackPlaybackState { get; set; } = string.Empty;
        public double FlashbackPlaybackTargetFps { get; set; }
        public double FlashbackPlaybackObservedFps { get; set; }
        public double FlashbackPlaybackP99FrameMs { get; set; }
        public double FlashbackPlaybackMaxFrameMs { get; set; }
        public double FlashbackPlaybackOnePercentLowFps { get; set; }
        public double FlashbackPlaybackFivePercentLowFps { get; set; }
        public double FlashbackPlaybackSlowFramePercent { get; set; }
        public double FlashbackPlaybackDecodeP99Ms { get; set; }
        public double FlashbackPlaybackDecodeMaxMs { get; set; }
        public string FlashbackPlaybackMaxDecodePhase { get; set; } = string.Empty;
        public double FlashbackPlaybackMaxDecodeReceiveMs { get; set; }
        public double FlashbackPlaybackMaxDecodeFeedMs { get; set; }
        public double FlashbackPlaybackMaxDecodeReadMs { get; set; }
        public double FlashbackPlaybackMaxDecodeSendMs { get; set; }
        public double FlashbackPlaybackMaxDecodeAudioMs { get; set; }
        public double FlashbackPlaybackMaxDecodeConvertMs { get; set; }
        public int FlashbackPlaybackPendingCommands { get; set; }
        public int FlashbackPlaybackMaxPendingCommands { get; set; }
        public long FlashbackPlaybackMaxCommandQueueLatencyMs { get; set; }
        public string FlashbackPlaybackMaxCommandQueueLatencyCommand { get; set; } = string.Empty;
        public long FlashbackPlaybackCommandsEnqueued { get; set; }
        public long FlashbackPlaybackCommandsProcessed { get; set; }
        public long FlashbackPlaybackCommandsDropped { get; set; }
        public long FlashbackPlaybackCommandsSkippedNotReady { get; set; }
        public long FlashbackPlaybackScrubUpdatesCoalesced { get; set; }
        public long FlashbackPlaybackSeekCommandsCoalesced { get; set; }
        public string FlashbackPlaybackLastCommandQueued { get; set; } = string.Empty;
        public string FlashbackPlaybackLastCommandProcessed { get; set; } = string.Empty;
        public long FlashbackPlaybackSubmitFailures { get; set; }
        public long FlashbackPlaybackLastDropUtcUnixMs { get; set; }
        public string FlashbackPlaybackLastDropReason { get; set; } = string.Empty;
        public long FlashbackPlaybackLastSubmitFailureUtcUnixMs { get; set; }
        public string FlashbackPlaybackLastSubmitFailure { get; set; } = string.Empty;
        public long FlashbackPlaybackDroppedFrames { get; set; }
        public long FlashbackPlaybackAudioMasterUnavailableFallbacks { get; set; }
        public long FlashbackPlaybackAudioMasterStaleFallbacks { get; set; }
        public long FlashbackPlaybackAudioMasterDriftOutlierFallbacks { get; set; }
        public string FlashbackPlaybackAudioMasterLastFallbackReason { get; set; } = string.Empty;
        public double FlashbackPlaybackAudioMasterLastFallbackClockAgeMs { get; set; }
        public long FlashbackPlaybackSegmentSwitches { get; set; }
        public long FlashbackPlaybackFmp4Reopens { get; set; }
        public long FlashbackPlaybackWriteHeadWaits { get; set; }
        public long FlashbackPlaybackNearLiveSnaps { get; set; }
        public long FlashbackPlaybackDecodeErrorSnaps { get; set; }
        public long FlashbackPlaybackLastWriteHeadWaitGapMs { get; set; }
        public long FlashbackPlaybackLastCommandFailureUtcUnixMs { get; set; }
        public string FlashbackPlaybackLastCommandFailure { get; set; } = string.Empty;
        public long FlashbackVideoQueueRejectedFrames { get; set; }
        public string FlashbackVideoQueueLastRejectReason { get; set; } = string.Empty;
        public long FlashbackGpuQueueRejectedFrames { get; set; }
        public string FlashbackGpuQueueLastRejectReason { get; set; } = string.Empty;
        public bool FatalCleanupInProgress { get; set; }
        public bool FlashbackCleanupInProgress { get; set; }
        public bool FlashbackForceRotateRequested { get; set; }
        public bool FlashbackForceRotateDraining { get; set; }
        public bool FlashbackExportActive { get; set; }
        public string FlashbackExportStatus { get; set; } = string.Empty;
        public string FlashbackExportFailureKind { get; set; } = string.Empty;
        public long FlashbackExportElapsedMs { get; set; }
        public long FlashbackExportLastProgressAgeMs { get; set; }
        public long FlashbackExportOutputBytes { get; set; }
        public double FlashbackExportThroughputBytesPerSec { get; set; }
        public int FlashbackExportSegmentsProcessed { get; set; }
        public int FlashbackExportTotalSegments { get; set; }
        public double FlashbackExportPercent { get; set; }
        public long FlashbackExportInPointMs { get; set; }
        public long FlashbackExportOutPointMs { get; set; }
        public string FlashbackExportMessage { get; set; } = string.Empty;
        public long LatencyMs { get; set; }
        public double WorkingMb { get; set; }
        public double ManagedMb { get; set; }
        public int Gen0 { get; set; }
        public int Gen1 { get; set; }
        public int Gen2 { get; set; }
        public double GcPause { get; set; }
        public int Workers { get; set; }
        public int IoThreads { get; set; }
    }
}

[McpServerToolType]
// MCP wrapper for PresentMon capture and parsed OS presentation metrics.
public static class PresentMonTools
{
    [McpServerTool, Description("Capture OS-level present/frame pacing metrics for Sussudio using the PresentMon console executable.")]
    public static async Task<CallToolResult> capture_presentmon(
        PipeClient pipeClient,
        [Description("Capture duration in seconds. Defaults to 10; clamped to 1-300.")] int seconds = 10,
        [Description("Optional target process id. Defaults to the newest Sussudio process.")] int? processId = null,
        [Description("Optional process name when processId is not provided. Defaults to Sussudio.")] string processName = "Sussudio",
        [Description("Optional expected DXGI swap-chain address, usually PreviewD3DSwapChainAddress from get_app_state_raw.")] string? swapChainAddress = null,
        [Description("Optional app-side D3D preview present id to correlate with PresentMon.")] long? appPresentId = null,
        [Description("Optional app-side decoded source sequence number for the correlated present.")] long? appSourceSequenceNumber = null,
        [Description("Optional UTC Unix milliseconds for the app-side Present return.")] long? appPresentUtcUnixMs = null,
        [Description("Optional path to PresentMon.exe / PresentMon-*-x64.exe. Env vars SUSSUDIO_PRESENTMON_PATH or PRESENTMON_PATH also work.")] string? presentMonPath = null,
        [Description("Optional CSV output path. The CSV is deleted unless keepCsv is true.")] string? outputPath = null,
        [Description("Keep the raw PresentMon CSV and return its path.")] bool keepCsv = false,
        [Description("Ask PresentMon to track GPU video engine metrics when supported.")] bool trackGpuVideo = true)
    {
        var resolved = await TryResolvePreviewPresentCorrelationAsync(pipeClient).ConfigureAwait(false);
        var result = await PresentMonProbe.RunAsync(PresentMonProbe.CreateOptions(
            seconds,
            processId,
            processName,
            swapChainAddress,
            appPresentId,
            appSourceSequenceNumber,
            appPresentUtcUnixMs,
            presentMonPath: presentMonPath,
            outputFile: outputPath,
            keepCsv: keepCsv,
            trackGpuVideo: trackGpuVideo,
            correlation: resolved))
            .ConfigureAwait(false);

        return McpToolResultFactory.FromText(PresentMonProbe.Format(result));
    }

    [McpServerTool(UseStructuredContent = true), Description("Capture raw structured PresentMon frame pacing summary for Sussudio.")]
    public static async Task<object> capture_presentmon_raw(
        PipeClient pipeClient,
        [Description("Capture duration in seconds. Defaults to 10; clamped to 1-300.")] int seconds = 10,
        [Description("Optional target process id. Defaults to the newest Sussudio process.")] int? processId = null,
        [Description("Optional process name when processId is not provided. Defaults to Sussudio.")] string processName = "Sussudio",
        [Description("Optional expected DXGI swap-chain address, usually PreviewD3DSwapChainAddress from get_app_state_raw.")] string? swapChainAddress = null,
        [Description("Optional app-side D3D preview present id to correlate with PresentMon.")] long? appPresentId = null,
        [Description("Optional app-side decoded source sequence number for the correlated present.")] long? appSourceSequenceNumber = null,
        [Description("Optional UTC Unix milliseconds for the app-side Present return.")] long? appPresentUtcUnixMs = null,
        [Description("Optional path to PresentMon.exe / PresentMon-*-x64.exe. Env vars SUSSUDIO_PRESENTMON_PATH or PRESENTMON_PATH also work.")] string? presentMonPath = null,
        [Description("Optional CSV output path. The CSV is deleted unless keepCsv is true.")] string? outputPath = null,
        [Description("Keep the raw PresentMon CSV and return its path.")] bool keepCsv = false,
        [Description("Ask PresentMon to track GPU video engine metrics when supported.")] bool trackGpuVideo = true)
    {
        var resolved = await TryResolvePreviewPresentCorrelationAsync(pipeClient).ConfigureAwait(false);
        return await PresentMonProbe.RunAsync(PresentMonProbe.CreateOptions(
            seconds,
            processId,
            processName,
            swapChainAddress,
            appPresentId,
            appSourceSequenceNumber,
            appPresentUtcUnixMs,
            presentMonPath: presentMonPath,
            outputFile: outputPath,
            keepCsv: keepCsv,
            trackGpuVideo: trackGpuVideo,
            correlation: resolved))
            .ConfigureAwait(false);
    }

    private static async Task<PresentMonProbeCorrelation> TryResolvePreviewPresentCorrelationAsync(PipeClient pipeClient)
    {
        try
        {
            var response = await pipeClient.SendCommandAsync(AutomationCommandKind.GetSnapshot).ConfigureAwait(false);
            if (!AutomationSnapshotFormatter.IsSuccess(response) ||
                !response.TryGetProperty("Snapshot", out var snapshot))
            {
                return default;
            }

            return PresentMonProbe.ReadPreviewCorrelation(snapshot);
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Trace.TraceWarning($"GetExpectedSwapChainAsync: malformed snapshot JSON: {ex.Message}");
            return default;
        }
        catch (IOException ex)
        {
            System.Diagnostics.Trace.TraceWarning($"GetExpectedSwapChainAsync: pipe IO failure: {ex.Message}");
            return default;
        }
    }
}
