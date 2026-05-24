namespace McpServer.Tools;

public static partial class PerformanceTimelineTools
{
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
