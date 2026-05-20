namespace McpServer.Tools;

public static partial class PerformanceTimelineTools
{
    private sealed partial class TimelineRow
    {
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
    }
}
