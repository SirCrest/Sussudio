using System.Text.Json;
using Sussudio.Tools;

namespace McpServer.Tools;

public static partial class PerformanceTimelineTools
{
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
}
