namespace McpServer.Tools;

public static partial class PerformanceTimelineTools
{
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
