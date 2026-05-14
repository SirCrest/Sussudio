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
                CaptureFivePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, "CaptureCadenceFivePercentLowFps"),
                PreviewAvgMs = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceAverageMs"),
                PreviewP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceP95Ms"),
                PreviewP99Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceP99Ms"),
                PreviewMaxMs = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceMaxMs"),
                PreviewOnePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceOnePercentLowFps"),
                PreviewFivePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceFivePercentLowFps"),
                PreviewSlowPct = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceSlowFramePercent"),
                VisualCadenceChangeObservedFps = AutomationSnapshotFormatter.GetDouble(item, "VisualCadenceChangeObservedFps"),
                VisualCadenceRepeatFramePercent = AutomationSnapshotFormatter.GetDouble(item, "VisualCadenceRepeatFramePercent"),
                VisualCadenceMotionConfidence = AutomationSnapshotFormatter.Get(item, "VisualCadenceMotionConfidence"),
                MjpegPacketHashInputObservedFps = AutomationSnapshotFormatter.GetDouble(item, "MjpegPacketHashInputObservedFps"),
                MjpegPacketHashUniqueObservedFps = AutomationSnapshotFormatter.GetDouble(item, "MjpegPacketHashUniqueObservedFps"),
                MjpegPacketHashDuplicateFramePercent = AutomationSnapshotFormatter.GetDouble(item, "MjpegPacketHashDuplicateFramePercent"),
                MjpegPreviewJitterEnabled = AutomationSnapshotFormatter.GetBool(item, "MjpegPreviewJitterEnabled"),
                MjpegPreviewJitterTargetDepth = AutomationSnapshotFormatter.GetInt(item, "MjpegPreviewJitterTargetDepth"),
                MjpegPreviewJitterMaxDepth = AutomationSnapshotFormatter.GetInt(item, "MjpegPreviewJitterMaxDepth"),
                MjpegPreviewJitterQueueDepth = AutomationSnapshotFormatter.GetInt(item, "MjpegPreviewJitterQueueDepth"),
                MjpegPreviewJitterTotalDropped = AutomationSnapshotFormatter.GetLong(item, "MjpegPreviewJitterTotalDropped"),
                MjpegPreviewJitterDeadlineDropCount = AutomationSnapshotFormatter.GetLong(item, "MjpegPreviewJitterDeadlineDropCount"),
                MjpegPreviewJitterClearedDropCount = AutomationSnapshotFormatter.GetLong(item, "MjpegPreviewJitterClearedDropCount"),
                MjpegPreviewJitterUnderflowCount = AutomationSnapshotFormatter.GetLong(item, "MjpegPreviewJitterUnderflowCount"),
                MjpegPreviewJitterResumeReprimeCount = AutomationSnapshotFormatter.GetLong(item, "MjpegPreviewJitterResumeReprimeCount"),
                MjpegPreviewJitterLatencyP95Ms = AutomationSnapshotFormatter.GetDouble(item, "MjpegPreviewJitterLatencyP95Ms"),
                MjpegPreviewJitterLatencyMaxMs = AutomationSnapshotFormatter.GetDouble(item, "MjpegPreviewJitterLatencyMaxMs"),
                MjpegPreviewJitterLastDropReason = AutomationSnapshotFormatter.Get(item, "MjpegPreviewJitterLastDropReason"),
                MjpegPreviewJitterLastUnderflowReason = AutomationSnapshotFormatter.Get(item, "MjpegPreviewJitterLastUnderflowReason"),
                MjpegPreviewJitterLastUnderflowInputAgeMs = AutomationSnapshotFormatter.GetDouble(item, "MjpegPreviewJitterLastUnderflowInputAgeMs"),
                MjpegPreviewJitterLastUnderflowOutputAgeMs = AutomationSnapshotFormatter.GetDouble(item, "MjpegPreviewJitterLastUnderflowOutputAgeMs"),
                MjpegPreviewJitterMaxScheduleLateMs = AutomationSnapshotFormatter.GetDouble(item, "MjpegPreviewJitterMaxScheduleLateMs"),
                MjpegPreviewJitterScheduleLateCount = AutomationSnapshotFormatter.GetLong(item, "MjpegPreviewJitterScheduleLateCount"),
                PreviewD3DPending = AutomationSnapshotFormatter.GetInt(item, "PreviewD3DPendingFrameCount"),
                PreviewD3DPresentP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DPresentCallP95Ms"),
                PreviewD3DTotalP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DTotalFrameCpuP95Ms"),
                PreviewD3DInputUploadP99Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DInputUploadCpuP99Ms"),
                PreviewD3DRenderSubmitP99Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DRenderSubmitCpuP99Ms"),
                PreviewD3DPresentP99Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DPresentCallP99Ms"),
                PreviewD3DTotalP99Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DTotalFrameCpuP99Ms"),
                PreviewD3DPipelineP99Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DPipelineLatencyP99Ms"),
                PreviewD3DPipelineMaxMs = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DPipelineLatencyMaxMs"),
                PreviewD3DFrameLatencyWaitTimeouts = AutomationSnapshotFormatter.GetLong(item, "PreviewD3DFrameLatencyWaitTimeoutCount"),
                PreviewD3DFrameLatencyWaitP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DFrameLatencyWaitP95Ms"),
                PreviewD3DFrameLatencyWaitMaxMs = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DFrameLatencyWaitMaxMs"),
                PreviewD3DRecentMissed = AutomationSnapshotFormatter.GetLong(item, "PreviewD3DFrameStatsRecentMissedRefreshCount"),
                PreviewD3DRecentFailures = AutomationSnapshotFormatter.GetLong(item, "PreviewD3DFrameStatsRecentFailureCount"),
                PreviewD3DSchedulerToPresentMs = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DLastRenderedSchedulerToPresentMs"),
                PreviewD3DLastPipelineLatencyMs = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DLastRenderedPipelineLatencyMs"),
                PreviewD3DLastDropReason = AutomationSnapshotFormatter.Get(item, "PreviewD3DLastDropReason"),
                PreviewPacingLikelySlowStage = AutomationSnapshotFormatter.Get(item, "PreviewPacingLikelySlowStage"),
                PreviewPacingSlowStageConfidence = AutomationSnapshotFormatter.Get(item, "PreviewPacingSlowStageConfidence"),
                PreviewPacingSlowStageEvidence = AutomationSnapshotFormatter.Get(item, "PreviewPacingSlowStageEvidence"),
                FlashbackPlaybackState = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackState"),
                FlashbackPlaybackTargetFps = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackTargetFps"),
                FlashbackPlaybackObservedFps = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackObservedFps"),
                FlashbackPlaybackP99FrameMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackP99FrameMs"),
                FlashbackPlaybackMaxFrameMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackMaxFrameMs"),
                FlashbackPlaybackOnePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackOnePercentLowFps"),
                FlashbackPlaybackFivePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackFivePercentLowFps"),
                FlashbackPlaybackSlowFramePercent = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackSlowFramePercent"),
                FlashbackPlaybackDecodeP99Ms = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackDecodeP99Ms"),
                FlashbackPlaybackDecodeMaxMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackDecodeMaxMs"),
                FlashbackPlaybackMaxDecodePhase = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackMaxDecodePhase"),
                FlashbackPlaybackMaxDecodeReceiveMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackMaxDecodeReceiveMs"),
                FlashbackPlaybackMaxDecodeFeedMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackMaxDecodeFeedMs"),
                FlashbackPlaybackMaxDecodeReadMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackMaxDecodeReadMs"),
                FlashbackPlaybackMaxDecodeSendMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackMaxDecodeSendMs"),
                FlashbackPlaybackMaxDecodeAudioMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackMaxDecodeAudioMs"),
                FlashbackPlaybackMaxDecodeConvertMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackMaxDecodeConvertMs"),
                FlashbackPlaybackPendingCommands = AutomationSnapshotFormatter.GetInt(item, "FlashbackPlaybackPendingCommands"),
                FlashbackPlaybackMaxPendingCommands = AutomationSnapshotFormatter.GetInt(item, "FlashbackPlaybackMaxPendingCommands"),
                FlashbackPlaybackMaxCommandQueueLatencyMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackMaxCommandQueueLatencyMs"),
                FlashbackPlaybackMaxCommandQueueLatencyCommand = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackMaxCommandQueueLatencyCommand"),
                FlashbackPlaybackCommandsEnqueued = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackCommandsEnqueued"),
                FlashbackPlaybackCommandsProcessed = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackCommandsProcessed"),
                FlashbackPlaybackCommandsDropped = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackCommandsDropped"),
                FlashbackPlaybackCommandsSkippedNotReady = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackCommandsSkippedNotReady"),
                FlashbackPlaybackScrubUpdatesCoalesced = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackScrubUpdatesCoalesced"),
                FlashbackPlaybackSeekCommandsCoalesced = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackSeekCommandsCoalesced"),
                FlashbackPlaybackLastCommandQueued = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackLastCommandQueued"),
                FlashbackPlaybackLastCommandProcessed = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackLastCommandProcessed"),
                FlashbackPlaybackSubmitFailures = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackSubmitFailures"),
                FlashbackPlaybackLastDropUtcUnixMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackLastDropUtcUnixMs"),
                FlashbackPlaybackLastDropReason = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackLastDropReason"),
                FlashbackPlaybackLastSubmitFailureUtcUnixMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackLastSubmitFailureUtcUnixMs"),
                FlashbackPlaybackLastSubmitFailure = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackLastSubmitFailure"),
                FlashbackPlaybackDroppedFrames = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackDroppedFrames"),
                FlashbackPlaybackAudioMasterUnavailableFallbacks = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackAudioMasterUnavailableFallbacks"),
                FlashbackPlaybackAudioMasterStaleFallbacks = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackAudioMasterStaleFallbacks"),
                FlashbackPlaybackAudioMasterDriftOutlierFallbacks = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackAudioMasterDriftOutlierFallbacks"),
                FlashbackPlaybackAudioMasterLastFallbackReason = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackAudioMasterLastFallbackReason"),
                FlashbackPlaybackAudioMasterLastFallbackClockAgeMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackAudioMasterLastFallbackClockAgeMs"),
                FlashbackPlaybackSegmentSwitches = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackSegmentSwitches"),
                FlashbackPlaybackFmp4Reopens = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackFmp4Reopens"),
                FlashbackPlaybackWriteHeadWaits = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackWriteHeadWaits"),
                FlashbackPlaybackNearLiveSnaps = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackNearLiveSnaps"),
                FlashbackPlaybackDecodeErrorSnaps = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackDecodeErrorSnaps"),
                FlashbackPlaybackLastWriteHeadWaitGapMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackLastWriteHeadWaitGapMs"),
                FlashbackPlaybackLastCommandFailureUtcUnixMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackLastCommandFailureUtcUnixMs"),
                FlashbackPlaybackLastCommandFailure = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackLastCommandFailure"),
                FlashbackVideoQueueRejectedFrames = AutomationSnapshotFormatter.GetLong(item, "FlashbackVideoQueueRejectedFrames"),
                FlashbackVideoQueueLastRejectReason = AutomationSnapshotFormatter.Get(item, "FlashbackVideoQueueLastRejectReason"),
                FlashbackGpuQueueRejectedFrames = AutomationSnapshotFormatter.GetLong(item, "FlashbackGpuQueueRejectedFrames"),
                FlashbackGpuQueueLastRejectReason = AutomationSnapshotFormatter.Get(item, "FlashbackGpuQueueLastRejectReason"),
                FatalCleanupInProgress = AutomationSnapshotFormatter.GetBool(item, "FatalCleanupInProgress"),
                FlashbackCleanupInProgress = AutomationSnapshotFormatter.GetBool(item, "FlashbackCleanupInProgress"),
                FlashbackForceRotateRequested = AutomationSnapshotFormatter.GetBool(item, "FlashbackForceRotateRequested"),
                FlashbackForceRotateDraining = AutomationSnapshotFormatter.GetBool(item, "FlashbackForceRotateDraining"),
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

        return entries;
    }

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
        public double CaptureFivePercentLowFps { get; init; }
        public double PreviewAvgMs { get; init; }
        public double PreviewP95Ms { get; init; }
        public double PreviewP99Ms { get; init; }
        public double PreviewMaxMs { get; init; }
        public double PreviewOnePercentLowFps { get; init; }
        public double PreviewFivePercentLowFps { get; init; }
        public double PreviewSlowPct { get; init; }
        public double VisualCadenceChangeObservedFps { get; init; }
        public double VisualCadenceRepeatFramePercent { get; init; }
        public string VisualCadenceMotionConfidence { get; init; } = string.Empty;
        public double MjpegPacketHashInputObservedFps { get; init; }
        public double MjpegPacketHashUniqueObservedFps { get; init; }
        public double MjpegPacketHashDuplicateFramePercent { get; init; }
        public bool MjpegPreviewJitterEnabled { get; init; }
        public int MjpegPreviewJitterTargetDepth { get; init; }
        public int MjpegPreviewJitterMaxDepth { get; init; }
        public int MjpegPreviewJitterQueueDepth { get; init; }
        public long MjpegPreviewJitterTotalDropped { get; init; }
        public long MjpegPreviewJitterDeadlineDropCount { get; init; }
        public long MjpegPreviewJitterClearedDropCount { get; init; }
        public long MjpegPreviewJitterUnderflowCount { get; init; }
        public long MjpegPreviewJitterResumeReprimeCount { get; init; }
        public double MjpegPreviewJitterLatencyP95Ms { get; init; }
        public double MjpegPreviewJitterLatencyMaxMs { get; init; }
        public string MjpegPreviewJitterLastDropReason { get; init; } = string.Empty;
        public string MjpegPreviewJitterLastUnderflowReason { get; init; } = string.Empty;
        public double MjpegPreviewJitterLastUnderflowInputAgeMs { get; init; }
        public double MjpegPreviewJitterLastUnderflowOutputAgeMs { get; init; }
        public double MjpegPreviewJitterMaxScheduleLateMs { get; init; }
        public long MjpegPreviewJitterScheduleLateCount { get; init; }
        public int PreviewD3DPending { get; init; }
        public double PreviewD3DPresentP95Ms { get; init; }
        public double PreviewD3DTotalP95Ms { get; init; }
        public double PreviewD3DInputUploadP99Ms { get; init; }
        public double PreviewD3DRenderSubmitP99Ms { get; init; }
        public double PreviewD3DPresentP99Ms { get; init; }
        public double PreviewD3DTotalP99Ms { get; init; }
        public double PreviewD3DPipelineP99Ms { get; init; }
        public double PreviewD3DPipelineMaxMs { get; init; }
        public long PreviewD3DFrameLatencyWaitTimeouts { get; init; }
        public double PreviewD3DFrameLatencyWaitP95Ms { get; init; }
        public double PreviewD3DFrameLatencyWaitMaxMs { get; init; }
        public long PreviewD3DRecentMissed { get; init; }
        public long PreviewD3DRecentFailures { get; init; }
        public double PreviewD3DSchedulerToPresentMs { get; init; }
        public double PreviewD3DLastPipelineLatencyMs { get; init; }
        public string PreviewD3DLastDropReason { get; init; } = string.Empty;
        public string PreviewPacingLikelySlowStage { get; init; } = string.Empty;
        public string PreviewPacingSlowStageConfidence { get; init; } = string.Empty;
        public string PreviewPacingSlowStageEvidence { get; init; } = string.Empty;
        public string FlashbackPlaybackState { get; init; } = string.Empty;
        public double FlashbackPlaybackTargetFps { get; init; }
        public double FlashbackPlaybackObservedFps { get; init; }
        public double FlashbackPlaybackP99FrameMs { get; init; }
        public double FlashbackPlaybackMaxFrameMs { get; init; }
        public double FlashbackPlaybackOnePercentLowFps { get; init; }
        public double FlashbackPlaybackFivePercentLowFps { get; init; }
        public double FlashbackPlaybackSlowFramePercent { get; init; }
        public double FlashbackPlaybackDecodeP99Ms { get; init; }
        public double FlashbackPlaybackDecodeMaxMs { get; init; }
        public string FlashbackPlaybackMaxDecodePhase { get; init; } = string.Empty;
        public double FlashbackPlaybackMaxDecodeReceiveMs { get; init; }
        public double FlashbackPlaybackMaxDecodeFeedMs { get; init; }
        public double FlashbackPlaybackMaxDecodeReadMs { get; init; }
        public double FlashbackPlaybackMaxDecodeSendMs { get; init; }
        public double FlashbackPlaybackMaxDecodeAudioMs { get; init; }
        public double FlashbackPlaybackMaxDecodeConvertMs { get; init; }
        public int FlashbackPlaybackPendingCommands { get; init; }
        public int FlashbackPlaybackMaxPendingCommands { get; init; }
        public long FlashbackPlaybackMaxCommandQueueLatencyMs { get; init; }
        public string FlashbackPlaybackMaxCommandQueueLatencyCommand { get; init; } = string.Empty;
        public long FlashbackPlaybackCommandsEnqueued { get; init; }
        public long FlashbackPlaybackCommandsProcessed { get; init; }
        public long FlashbackPlaybackCommandsDropped { get; init; }
        public long FlashbackPlaybackCommandsSkippedNotReady { get; init; }
        public long FlashbackPlaybackScrubUpdatesCoalesced { get; init; }
        public long FlashbackPlaybackSeekCommandsCoalesced { get; init; }
        public string FlashbackPlaybackLastCommandQueued { get; init; } = string.Empty;
        public string FlashbackPlaybackLastCommandProcessed { get; init; } = string.Empty;
        public long FlashbackPlaybackSubmitFailures { get; init; }
        public long FlashbackPlaybackLastDropUtcUnixMs { get; init; }
        public string FlashbackPlaybackLastDropReason { get; init; } = string.Empty;
        public long FlashbackPlaybackLastSubmitFailureUtcUnixMs { get; init; }
        public string FlashbackPlaybackLastSubmitFailure { get; init; } = string.Empty;
        public long FlashbackPlaybackDroppedFrames { get; init; }
        public long FlashbackPlaybackAudioMasterUnavailableFallbacks { get; init; }
        public long FlashbackPlaybackAudioMasterStaleFallbacks { get; init; }
        public long FlashbackPlaybackAudioMasterDriftOutlierFallbacks { get; init; }
        public string FlashbackPlaybackAudioMasterLastFallbackReason { get; init; } = string.Empty;
        public double FlashbackPlaybackAudioMasterLastFallbackClockAgeMs { get; init; }
        public long FlashbackPlaybackSegmentSwitches { get; init; }
        public long FlashbackPlaybackFmp4Reopens { get; init; }
        public long FlashbackPlaybackWriteHeadWaits { get; init; }
        public long FlashbackPlaybackNearLiveSnaps { get; init; }
        public long FlashbackPlaybackDecodeErrorSnaps { get; init; }
        public long FlashbackPlaybackLastWriteHeadWaitGapMs { get; init; }
        public long FlashbackPlaybackLastCommandFailureUtcUnixMs { get; init; }
        public string FlashbackPlaybackLastCommandFailure { get; init; } = string.Empty;
        public long FlashbackVideoQueueRejectedFrames { get; init; }
        public string FlashbackVideoQueueLastRejectReason { get; init; } = string.Empty;
        public long FlashbackGpuQueueRejectedFrames { get; init; }
        public string FlashbackGpuQueueLastRejectReason { get; init; } = string.Empty;
        public bool FatalCleanupInProgress { get; init; }
        public bool FlashbackCleanupInProgress { get; init; }
        public bool FlashbackForceRotateRequested { get; init; }
        public bool FlashbackForceRotateDraining { get; init; }
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
