namespace McpServer.Tools;

public static partial class PerformanceTimelineTools
{
    private sealed partial class TimelineRow
    {
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
    }
}
