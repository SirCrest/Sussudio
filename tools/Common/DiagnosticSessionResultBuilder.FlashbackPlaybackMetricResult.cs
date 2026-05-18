namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
    private static DiagnosticSessionFlashbackPlaybackCommandsResultProjection BuildFlashbackPlaybackCommandsResultProjection(
        FlashbackPlaybackResultMetrics playbackResultMetrics) =>
        new(
            FlashbackPlaybackPendingCommandsAtEnd: playbackResultMetrics.PendingCommandsAtEnd,
            FlashbackPlaybackMaxPendingCommandsObserved: playbackResultMetrics.MaxPendingCommandsObserved,
            FlashbackPlaybackMaxCommandQueueLatencyMsObserved: playbackResultMetrics.MaxCommandQueueLatencyMsObserved,
            FlashbackPlaybackMaxCommandQueueLatencyCommandObserved: playbackResultMetrics.MaxCommandQueueLatencyCommandObserved,
            FlashbackPlaybackCommandsDroppedAtEnd: playbackResultMetrics.CommandsDroppedAtEnd,
            FlashbackPlaybackCommandsSkippedNotReadyAtEnd: playbackResultMetrics.CommandsSkippedNotReadyAtEnd,
            FlashbackPlaybackScrubUpdatesCoalescedAtEnd: playbackResultMetrics.ScrubUpdatesCoalescedAtEnd,
            FlashbackPlaybackSeekCommandsCoalescedAtEnd: playbackResultMetrics.SeekCommandsCoalescedAtEnd,
            FlashbackPlaybackLastCommandFailureAtEnd: playbackResultMetrics.LastCommandFailureAtEnd,
            FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd: playbackResultMetrics.LastCommandFailureUtcUnixMsAtEnd);

    private static DiagnosticSessionFlashbackPlaybackCadenceResultProjection BuildFlashbackPlaybackCadenceResultProjection(
        FlashbackPlaybackSessionMetrics playbackSessionMetrics,
        FlashbackPlaybackResultMetrics playbackResultMetrics) =>
        new(
            FlashbackPlaybackObservedFpsAtEnd: playbackResultMetrics.ObservedFpsAtEnd,
            FlashbackPlaybackMinObservedFpsObserved: playbackSessionMetrics.MinObservedFpsObserved,
            FlashbackPlaybackAvgFrameMsAtEnd: playbackResultMetrics.AvgFrameMsAtEnd,
            FlashbackPlaybackP99FrameMsAtEnd: playbackResultMetrics.P99FrameMsAtEnd,
            FlashbackPlaybackMaxFrameMsAtEnd: playbackResultMetrics.MaxFrameMsAtEnd,
            FlashbackPlaybackOnePercentLowFpsAtEnd: playbackResultMetrics.OnePercentLowFpsAtEnd,
            FlashbackPlaybackMinOnePercentLowFpsObserved: playbackSessionMetrics.MinOnePercentLowFpsObserved,
            FlashbackPlaybackOnePercentLowSampleWindowObserved: playbackSessionMetrics.OnePercentLowSampleWindowObserved,
            FlashbackPlaybackOnePercentLowMinimumFrames: playbackSessionMetrics.MinimumOnePercentLowFrameCount,
            FlashbackPlaybackMaxSessionFrameCountObserved: playbackSessionMetrics.MaxSessionFrameCountObserved,
            FlashbackPlaybackMinOnePercentLowOffsetMs: playbackSessionMetrics.MinOnePercentLowOffsetMs,
            FlashbackPlaybackMinOnePercentLowFrameCount: playbackSessionMetrics.MinOnePercentLowFrameCount,
            FlashbackPlaybackMinOnePercentLowP99FrameMs: playbackSessionMetrics.MinOnePercentLowP99FrameMs,
            FlashbackPlaybackMinOnePercentLowMaxFrameMs: playbackSessionMetrics.MinOnePercentLowMaxFrameMs,
            FlashbackPlaybackMinOnePercentLowDecodeP99Ms: playbackSessionMetrics.MinOnePercentLowDecodeP99Ms,
            FlashbackPlaybackMinOnePercentLowDecodeMaxMs: playbackSessionMetrics.MinOnePercentLowDecodeMaxMs,
            FlashbackPlaybackMinOnePercentLowAvDriftMs: playbackSessionMetrics.MinOnePercentLowAvDriftMs,
            FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks: playbackSessionMetrics.MinOnePercentLowAudioMasterFallbacks,
            FlashbackPlaybackMaxP99FrameMsObserved: playbackSessionMetrics.MaxP99FrameMsObserved,
            FlashbackPlaybackMaxFrameMsObserved: playbackSessionMetrics.MaxFrameMsObserved,
            FlashbackPlaybackMaxSlowFramePercentObserved: playbackSessionMetrics.MaxSlowFramePercentObserved,
            FlashbackPlaybackFrameCountAtEnd: playbackResultMetrics.FrameCountAtEnd,
            FlashbackPlaybackLateFramesAtEnd: playbackResultMetrics.LateFramesAtEnd,
            FlashbackPlaybackSlowFramesAtEnd: playbackResultMetrics.SlowFramesAtEnd,
            FlashbackPlaybackSlowFramePercentAtEnd: playbackResultMetrics.SlowFramePercentAtEnd,
            FlashbackPlaybackDroppedFramesAtEnd: playbackResultMetrics.DroppedFramesAtEnd,
            FlashbackPlaybackDroppedFramesDelta: playbackSessionMetrics.DroppedFramesDelta);

    private static DiagnosticSessionFlashbackPlaybackDecodeResultProjection BuildFlashbackPlaybackDecodeResultProjection(
        FlashbackPlaybackSessionMetrics playbackSessionMetrics,
        FlashbackPlaybackResultMetrics playbackResultMetrics) =>
        new(
            FlashbackPlaybackDecodeAvgMsAtEnd: playbackResultMetrics.DecodeAvgMsAtEnd,
            FlashbackPlaybackDecodeP95MsAtEnd: playbackResultMetrics.DecodeP95MsAtEnd,
            FlashbackPlaybackDecodeP99MsAtEnd: playbackResultMetrics.DecodeP99MsAtEnd,
            FlashbackPlaybackDecodeMaxMsAtEnd: playbackResultMetrics.DecodeMaxMsAtEnd,
            FlashbackPlaybackMaxDecodePhaseAtEnd: playbackResultMetrics.MaxDecodePhaseAtEnd,
            FlashbackPlaybackMaxDecodeReceiveMsAtEnd: playbackResultMetrics.MaxDecodeReceiveMsAtEnd,
            FlashbackPlaybackMaxDecodeFeedMsAtEnd: playbackResultMetrics.MaxDecodeFeedMsAtEnd,
            FlashbackPlaybackMaxDecodeReadMsAtEnd: playbackResultMetrics.MaxDecodeReadMsAtEnd,
            FlashbackPlaybackMaxDecodeSendMsAtEnd: playbackResultMetrics.MaxDecodeSendMsAtEnd,
            FlashbackPlaybackMaxDecodeAudioMsAtEnd: playbackResultMetrics.MaxDecodeAudioMsAtEnd,
            FlashbackPlaybackMaxDecodeConvertMsAtEnd: playbackResultMetrics.MaxDecodeConvertMsAtEnd,
            FlashbackPlaybackMaxDecodeUtcUnixMsAtEnd: playbackResultMetrics.MaxDecodeUtcUnixMsAtEnd,
            FlashbackPlaybackMaxDecodePositionMsAtEnd: playbackResultMetrics.MaxDecodePositionMsAtEnd,
            FlashbackPlaybackMaxDecodeP99MsObserved: playbackSessionMetrics.MaxDecodeP99MsObserved,
            FlashbackPlaybackMaxDecodeMsObserved: playbackSessionMetrics.MaxDecodeMsObserved,
            FlashbackPlaybackMaxDecodePhaseObserved: playbackSessionMetrics.MaxDecodePhaseObserved,
            FlashbackPlaybackMaxDecodeReceiveMsObserved: playbackSessionMetrics.MaxDecodeReceiveMsObserved,
            FlashbackPlaybackMaxDecodeFeedMsObserved: playbackSessionMetrics.MaxDecodeFeedMsObserved,
            FlashbackPlaybackMaxDecodeReadMsObserved: playbackSessionMetrics.MaxDecodeReadMsObserved,
            FlashbackPlaybackMaxDecodeSendMsObserved: playbackSessionMetrics.MaxDecodeSendMsObserved,
            FlashbackPlaybackMaxDecodeAudioMsObserved: playbackSessionMetrics.MaxDecodeAudioMsObserved,
            FlashbackPlaybackMaxDecodeConvertMsObserved: playbackSessionMetrics.MaxDecodeConvertMsObserved,
            FlashbackPlaybackMaxDecodeUtcUnixMsObserved: playbackSessionMetrics.MaxDecodeUtcUnixMsObserved,
            FlashbackPlaybackMaxDecodePositionMsObserved: playbackSessionMetrics.MaxDecodePositionMsObserved);

    private static DiagnosticSessionFlashbackPlaybackAudioMasterResultProjection BuildFlashbackPlaybackAudioMasterResultProjection(
        FlashbackPlaybackSessionMetrics playbackSessionMetrics,
        FlashbackPlaybackResultMetrics playbackResultMetrics) =>
        new(
            FlashbackPlaybackAudioMasterDelayDoublesAtEnd: playbackResultMetrics.AudioMasterDelayDoublesAtEnd,
            FlashbackPlaybackAudioMasterDelayShrinksAtEnd: playbackResultMetrics.AudioMasterDelayShrinksAtEnd,
            FlashbackPlaybackAudioMasterFallbacksAtEnd: playbackResultMetrics.AudioMasterFallbacksAtEnd,
            FlashbackPlaybackAudioMasterUnavailableFallbacksAtEnd: playbackResultMetrics.AudioMasterUnavailableFallbacksAtEnd,
            FlashbackPlaybackAudioMasterStaleFallbacksAtEnd: playbackResultMetrics.AudioMasterStaleFallbacksAtEnd,
            FlashbackPlaybackAudioMasterDriftOutlierFallbacksAtEnd: playbackResultMetrics.AudioMasterDriftOutlierFallbacksAtEnd,
            FlashbackPlaybackAudioMasterLastFallbackReasonAtEnd: playbackResultMetrics.AudioMasterLastFallbackReasonAtEnd,
            FlashbackPlaybackAudioMasterLastFallbackClockAgeMsAtEnd: playbackResultMetrics.AudioMasterLastFallbackClockAgeMsAtEnd,
            FlashbackPlaybackMaxAudioMasterDelayDoublesObserved: playbackSessionMetrics.MaxAudioMasterDelayDoublesObserved,
            FlashbackPlaybackMaxAudioMasterDelayShrinksObserved: playbackSessionMetrics.MaxAudioMasterDelayShrinksObserved,
            FlashbackPlaybackMaxAudioMasterFallbacksObserved: playbackSessionMetrics.MaxAudioMasterFallbacksObserved,
            FlashbackPlaybackMaxAudioBufferedDurationMsObserved: playbackSessionMetrics.MaxAudioBufferedDurationMsObserved,
            FlashbackPlaybackMaxAudioQueueDurationMsObserved: playbackSessionMetrics.MaxAudioQueueDurationMsObserved,
            FlashbackPlaybackMaxAbsAvDriftMsObserved: playbackSessionMetrics.MaxAbsAvDriftMsObserved);

    private static DiagnosticSessionFlashbackPlaybackStagesResultProjection BuildFlashbackPlaybackStagesResultProjection(
        FlashbackPlaybackSessionMetrics playbackSessionMetrics,
        FlashbackPlaybackResultMetrics playbackResultMetrics) =>
        new(
            FlashbackPlaybackSubmitFailuresAtEnd: playbackResultMetrics.SubmitFailuresAtEnd,
            FlashbackPlaybackSubmitFailuresDelta: playbackSessionMetrics.SubmitFailuresDelta,
            FlashbackPlaybackSegmentSwitchesAtEnd: playbackResultMetrics.SegmentSwitchesAtEnd,
            FlashbackPlaybackFmp4ReopensAtEnd: playbackResultMetrics.Fmp4ReopensAtEnd,
            FlashbackPlaybackWriteHeadWaitsAtEnd: playbackResultMetrics.WriteHeadWaitsAtEnd,
            FlashbackPlaybackNearLiveSnapsAtEnd: playbackResultMetrics.NearLiveSnapsAtEnd,
            FlashbackPlaybackDecodeErrorSnapsAtEnd: playbackResultMetrics.DecodeErrorSnapsAtEnd,
            FlashbackPlaybackLastWriteHeadWaitGapMsAtEnd: playbackResultMetrics.LastWriteHeadWaitGapMsAtEnd,
            FlashbackPlaybackSeekForwardDecodeCapHitsAtEnd: playbackResultMetrics.SeekForwardDecodeCapHitsAtEnd,
            FlashbackPlaybackSeekForwardDecodeCapHitsDelta: playbackResultMetrics.SeekForwardDecodeCapHitsDelta,
            FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd: playbackResultMetrics.LastSeekHitForwardDecodeCapAtEnd);
}
