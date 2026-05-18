namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
    private readonly record struct DiagnosticSessionFlashbackPlaybackResultProjection(
        DiagnosticSessionFlashbackPlaybackCommandsResultProjection CommandsResult,
        DiagnosticSessionFlashbackPlaybackCadenceResultProjection CadenceResult,
        DiagnosticSessionFlashbackPlaybackDecodeResultProjection DecodeResult,
        DiagnosticSessionFlashbackPlaybackAudioMasterResultProjection AudioMasterResult,
        DiagnosticSessionFlashbackPlaybackStagesResultProjection StagesResult);

    private readonly record struct DiagnosticSessionFlashbackPlaybackCommandsResultProjection(
        int FlashbackPlaybackPendingCommandsAtEnd,
        int FlashbackPlaybackMaxPendingCommandsObserved,
        int FlashbackPlaybackMaxCommandQueueLatencyMsObserved,
        string FlashbackPlaybackMaxCommandQueueLatencyCommandObserved,
        long FlashbackPlaybackCommandsDroppedAtEnd,
        long FlashbackPlaybackCommandsSkippedNotReadyAtEnd,
        long FlashbackPlaybackScrubUpdatesCoalescedAtEnd,
        long FlashbackPlaybackSeekCommandsCoalescedAtEnd,
        string FlashbackPlaybackLastCommandFailureAtEnd,
        long FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd);

    private readonly record struct DiagnosticSessionFlashbackPlaybackCadenceResultProjection(
        double FlashbackPlaybackObservedFpsAtEnd,
        double FlashbackPlaybackMinObservedFpsObserved,
        double FlashbackPlaybackAvgFrameMsAtEnd,
        double FlashbackPlaybackP99FrameMsAtEnd,
        double FlashbackPlaybackMaxFrameMsAtEnd,
        double FlashbackPlaybackOnePercentLowFpsAtEnd,
        double FlashbackPlaybackMinOnePercentLowFpsObserved,
        bool FlashbackPlaybackOnePercentLowSampleWindowObserved,
        long FlashbackPlaybackOnePercentLowMinimumFrames,
        long FlashbackPlaybackMaxSessionFrameCountObserved,
        long FlashbackPlaybackMinOnePercentLowOffsetMs,
        long FlashbackPlaybackMinOnePercentLowFrameCount,
        double FlashbackPlaybackMinOnePercentLowP99FrameMs,
        double FlashbackPlaybackMinOnePercentLowMaxFrameMs,
        double FlashbackPlaybackMinOnePercentLowDecodeP99Ms,
        double FlashbackPlaybackMinOnePercentLowDecodeMaxMs,
        double FlashbackPlaybackMinOnePercentLowAvDriftMs,
        long FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks,
        double FlashbackPlaybackMaxP99FrameMsObserved,
        double FlashbackPlaybackMaxFrameMsObserved,
        double FlashbackPlaybackMaxSlowFramePercentObserved,
        long FlashbackPlaybackFrameCountAtEnd,
        long FlashbackPlaybackLateFramesAtEnd,
        long FlashbackPlaybackSlowFramesAtEnd,
        double FlashbackPlaybackSlowFramePercentAtEnd,
        long FlashbackPlaybackDroppedFramesAtEnd,
        long FlashbackPlaybackDroppedFramesDelta);

    private readonly record struct DiagnosticSessionFlashbackPlaybackDecodeResultProjection(
        double FlashbackPlaybackDecodeAvgMsAtEnd,
        double FlashbackPlaybackDecodeP95MsAtEnd,
        double FlashbackPlaybackDecodeP99MsAtEnd,
        double FlashbackPlaybackDecodeMaxMsAtEnd,
        string FlashbackPlaybackMaxDecodePhaseAtEnd,
        double FlashbackPlaybackMaxDecodeReceiveMsAtEnd,
        double FlashbackPlaybackMaxDecodeFeedMsAtEnd,
        double FlashbackPlaybackMaxDecodeReadMsAtEnd,
        double FlashbackPlaybackMaxDecodeSendMsAtEnd,
        double FlashbackPlaybackMaxDecodeAudioMsAtEnd,
        double FlashbackPlaybackMaxDecodeConvertMsAtEnd,
        long FlashbackPlaybackMaxDecodeUtcUnixMsAtEnd,
        long FlashbackPlaybackMaxDecodePositionMsAtEnd,
        double FlashbackPlaybackMaxDecodeP99MsObserved,
        double FlashbackPlaybackMaxDecodeMsObserved,
        string FlashbackPlaybackMaxDecodePhaseObserved,
        double FlashbackPlaybackMaxDecodeReceiveMsObserved,
        double FlashbackPlaybackMaxDecodeFeedMsObserved,
        double FlashbackPlaybackMaxDecodeReadMsObserved,
        double FlashbackPlaybackMaxDecodeSendMsObserved,
        double FlashbackPlaybackMaxDecodeAudioMsObserved,
        double FlashbackPlaybackMaxDecodeConvertMsObserved,
        long FlashbackPlaybackMaxDecodeUtcUnixMsObserved,
        long FlashbackPlaybackMaxDecodePositionMsObserved);

    private readonly record struct DiagnosticSessionFlashbackPlaybackAudioMasterResultProjection(
        long FlashbackPlaybackAudioMasterDelayDoublesAtEnd,
        long FlashbackPlaybackAudioMasterDelayShrinksAtEnd,
        long FlashbackPlaybackAudioMasterFallbacksAtEnd,
        long FlashbackPlaybackAudioMasterUnavailableFallbacksAtEnd,
        long FlashbackPlaybackAudioMasterStaleFallbacksAtEnd,
        long FlashbackPlaybackAudioMasterDriftOutlierFallbacksAtEnd,
        string FlashbackPlaybackAudioMasterLastFallbackReasonAtEnd,
        double FlashbackPlaybackAudioMasterLastFallbackClockAgeMsAtEnd,
        long FlashbackPlaybackMaxAudioMasterDelayDoublesObserved,
        long FlashbackPlaybackMaxAudioMasterDelayShrinksObserved,
        long FlashbackPlaybackMaxAudioMasterFallbacksObserved,
        double FlashbackPlaybackMaxAudioBufferedDurationMsObserved,
        double FlashbackPlaybackMaxAudioQueueDurationMsObserved,
        double FlashbackPlaybackMaxAbsAvDriftMsObserved);

    private readonly record struct DiagnosticSessionFlashbackPlaybackStagesResultProjection(
        long FlashbackPlaybackSubmitFailuresAtEnd,
        long FlashbackPlaybackSubmitFailuresDelta,
        long FlashbackPlaybackSegmentSwitchesAtEnd,
        long FlashbackPlaybackFmp4ReopensAtEnd,
        long FlashbackPlaybackWriteHeadWaitsAtEnd,
        long FlashbackPlaybackNearLiveSnapsAtEnd,
        long FlashbackPlaybackDecodeErrorSnapsAtEnd,
        long FlashbackPlaybackLastWriteHeadWaitGapMsAtEnd,
        long FlashbackPlaybackSeekForwardDecodeCapHitsAtEnd,
        long FlashbackPlaybackSeekForwardDecodeCapHitsDelta,
        bool FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd);
}
