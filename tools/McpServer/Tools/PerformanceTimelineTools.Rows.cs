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
