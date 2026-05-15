namespace Sussudio.Tools;

public sealed partial class DiagnosticSessionResult
{
    public int FlashbackPlaybackPendingCommandsAtEnd { get; init; }
    public int FlashbackPlaybackMaxPendingCommandsObserved { get; init; }
    public int FlashbackPlaybackMaxCommandQueueLatencyMsObserved { get; init; }
    public string FlashbackPlaybackMaxCommandQueueLatencyCommandObserved { get; init; } = string.Empty;
    public long FlashbackPlaybackCommandsDroppedAtEnd { get; init; }
    public long FlashbackPlaybackCommandsSkippedNotReadyAtEnd { get; init; }
    public long FlashbackPlaybackScrubUpdatesCoalescedAtEnd { get; init; }
    public long FlashbackPlaybackSeekCommandsCoalescedAtEnd { get; init; }
    public string FlashbackPlaybackLastCommandFailureAtEnd { get; init; } = string.Empty;
    public long FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd { get; init; }
    public double FlashbackPlaybackObservedFpsAtEnd { get; init; }
    public double FlashbackPlaybackMinObservedFpsObserved { get; init; }
    public double FlashbackPlaybackAvgFrameMsAtEnd { get; init; }
    public double FlashbackPlaybackP99FrameMsAtEnd { get; init; }
    public double FlashbackPlaybackMaxFrameMsAtEnd { get; init; }
    public double FlashbackPlaybackOnePercentLowFpsAtEnd { get; init; }
    public double FlashbackPlaybackMinOnePercentLowFpsObserved { get; init; }
    public bool FlashbackPlaybackOnePercentLowSampleWindowObserved { get; init; }
    public long FlashbackPlaybackOnePercentLowMinimumFrames { get; init; }
    public long FlashbackPlaybackMaxSessionFrameCountObserved { get; init; }
    public long FlashbackPlaybackMinOnePercentLowOffsetMs { get; init; }
    public long FlashbackPlaybackMinOnePercentLowFrameCount { get; init; }
    public double FlashbackPlaybackMinOnePercentLowP99FrameMs { get; init; }
    public double FlashbackPlaybackMinOnePercentLowMaxFrameMs { get; init; }
    public double FlashbackPlaybackMinOnePercentLowDecodeP99Ms { get; init; }
    public double FlashbackPlaybackMinOnePercentLowDecodeMaxMs { get; init; }
    public double FlashbackPlaybackMinOnePercentLowAvDriftMs { get; init; }
    public long FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks { get; init; }
    public double FlashbackPlaybackMaxP99FrameMsObserved { get; init; }
    public double FlashbackPlaybackMaxFrameMsObserved { get; init; }
    public double FlashbackPlaybackMaxSlowFramePercentObserved { get; init; }
    public double FlashbackPlaybackDecodeAvgMsAtEnd { get; init; }
    public double FlashbackPlaybackDecodeP95MsAtEnd { get; init; }
    public double FlashbackPlaybackDecodeP99MsAtEnd { get; init; }
    public double FlashbackPlaybackDecodeMaxMsAtEnd { get; init; }
    public string FlashbackPlaybackMaxDecodePhaseAtEnd { get; init; } = string.Empty;
    public double FlashbackPlaybackMaxDecodeReceiveMsAtEnd { get; init; }
    public double FlashbackPlaybackMaxDecodeFeedMsAtEnd { get; init; }
    public double FlashbackPlaybackMaxDecodeReadMsAtEnd { get; init; }
    public double FlashbackPlaybackMaxDecodeSendMsAtEnd { get; init; }
    public double FlashbackPlaybackMaxDecodeAudioMsAtEnd { get; init; }
    public double FlashbackPlaybackMaxDecodeConvertMsAtEnd { get; init; }
    public long FlashbackPlaybackMaxDecodeUtcUnixMsAtEnd { get; init; }
    public long FlashbackPlaybackMaxDecodePositionMsAtEnd { get; init; }
    public double FlashbackPlaybackMaxDecodeP99MsObserved { get; init; }
    public double FlashbackPlaybackMaxDecodeMsObserved { get; init; }
    public string FlashbackPlaybackMaxDecodePhaseObserved { get; init; } = string.Empty;
    public double FlashbackPlaybackMaxDecodeReceiveMsObserved { get; init; }
    public double FlashbackPlaybackMaxDecodeFeedMsObserved { get; init; }
    public double FlashbackPlaybackMaxDecodeReadMsObserved { get; init; }
    public double FlashbackPlaybackMaxDecodeSendMsObserved { get; init; }
    public double FlashbackPlaybackMaxDecodeAudioMsObserved { get; init; }
    public double FlashbackPlaybackMaxDecodeConvertMsObserved { get; init; }
    public long FlashbackPlaybackMaxDecodeUtcUnixMsObserved { get; init; }
    public long FlashbackPlaybackMaxDecodePositionMsObserved { get; init; }
    public long FlashbackPlaybackFrameCountAtEnd { get; init; }
    public long FlashbackPlaybackLateFramesAtEnd { get; init; }
    public long FlashbackPlaybackSlowFramesAtEnd { get; init; }
    public double FlashbackPlaybackSlowFramePercentAtEnd { get; init; }
    public long FlashbackPlaybackDroppedFramesAtEnd { get; init; }
    public long FlashbackPlaybackDroppedFramesDelta { get; init; }
    public long FlashbackPlaybackAudioMasterDelayDoublesAtEnd { get; init; }
    public long FlashbackPlaybackAudioMasterDelayShrinksAtEnd { get; init; }
    public long FlashbackPlaybackAudioMasterFallbacksAtEnd { get; init; }
    public long FlashbackPlaybackAudioMasterUnavailableFallbacksAtEnd { get; init; }
    public long FlashbackPlaybackAudioMasterStaleFallbacksAtEnd { get; init; }
    public long FlashbackPlaybackAudioMasterDriftOutlierFallbacksAtEnd { get; init; }
    public string FlashbackPlaybackAudioMasterLastFallbackReasonAtEnd { get; init; } = string.Empty;
    public double FlashbackPlaybackAudioMasterLastFallbackClockAgeMsAtEnd { get; init; }
    public long FlashbackPlaybackMaxAudioMasterDelayDoublesObserved { get; init; }
    public long FlashbackPlaybackMaxAudioMasterDelayShrinksObserved { get; init; }
    public long FlashbackPlaybackMaxAudioMasterFallbacksObserved { get; init; }
    public double FlashbackPlaybackMaxAudioBufferedDurationMsObserved { get; init; }
    public double FlashbackPlaybackMaxAudioQueueDurationMsObserved { get; init; }
    public double FlashbackPlaybackMaxAbsAvDriftMsObserved { get; init; }
    public long FlashbackPlaybackSubmitFailuresAtEnd { get; init; }
    public long FlashbackPlaybackSubmitFailuresDelta { get; init; }
    public long FlashbackPlaybackSegmentSwitchesAtEnd { get; init; }
    public long FlashbackPlaybackFmp4ReopensAtEnd { get; init; }
    public long FlashbackPlaybackWriteHeadWaitsAtEnd { get; init; }
    public long FlashbackPlaybackNearLiveSnapsAtEnd { get; init; }
    public long FlashbackPlaybackDecodeErrorSnapsAtEnd { get; init; }
    public long FlashbackPlaybackLastWriteHeadWaitGapMsAtEnd { get; init; }
    public long FlashbackPlaybackSeekForwardDecodeCapHitsAtEnd { get; init; }
    public long FlashbackPlaybackSeekForwardDecodeCapHitsDelta { get; init; }
    public bool FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd { get; init; }
}
