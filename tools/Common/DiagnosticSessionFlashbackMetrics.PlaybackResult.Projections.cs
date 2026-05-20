namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackMetrics
{
    private readonly record struct FlashbackPlaybackResultCommandMetrics(
        int PendingCommandsAtEnd,
        int MaxPendingCommandsObserved,
        int MaxCommandQueueLatencyMsObserved,
        string MaxCommandQueueLatencyCommandObserved,
        long CommandsDroppedAtEnd,
        long CommandsSkippedNotReadyAtEnd,
        long ScrubUpdatesCoalescedAtEnd,
        long SeekCommandsCoalescedAtEnd,
        string LastCommandFailureAtEnd,
        long LastCommandFailureUtcUnixMsAtEnd);

    private readonly record struct FlashbackPlaybackResultCadenceMetrics(
        double ObservedFpsAtEnd,
        double AvgFrameMsAtEnd,
        double P99FrameMsAtEnd,
        double MaxFrameMsAtEnd,
        double OnePercentLowFpsAtEnd,
        long FrameCountAtEnd,
        long LateFramesAtEnd,
        long SlowFramesAtEnd,
        double SlowFramePercentAtEnd,
        long DroppedFramesAtEnd);

    private readonly record struct FlashbackPlaybackResultDecodeMetrics(
        double DecodeAvgMsAtEnd,
        double DecodeP95MsAtEnd,
        double DecodeP99MsAtEnd,
        double DecodeMaxMsAtEnd,
        string MaxDecodePhaseAtEnd,
        double MaxDecodeReceiveMsAtEnd,
        double MaxDecodeFeedMsAtEnd,
        double MaxDecodeReadMsAtEnd,
        double MaxDecodeSendMsAtEnd,
        double MaxDecodeAudioMsAtEnd,
        double MaxDecodeConvertMsAtEnd,
        long MaxDecodeUtcUnixMsAtEnd,
        long MaxDecodePositionMsAtEnd);

    private readonly record struct FlashbackPlaybackResultAudioMasterMetrics(
        long AudioMasterDelayDoublesAtEnd,
        long AudioMasterDelayShrinksAtEnd,
        long AudioMasterFallbacksAtEnd,
        long AudioMasterUnavailableFallbacksAtEnd,
        long AudioMasterStaleFallbacksAtEnd,
        long AudioMasterDriftOutlierFallbacksAtEnd,
        string AudioMasterLastFallbackReasonAtEnd,
        double AudioMasterLastFallbackClockAgeMsAtEnd);

    private readonly record struct FlashbackPlaybackResultStageMetrics(
        long SubmitFailuresAtEnd,
        long SegmentSwitchesAtEnd,
        long Fmp4ReopensAtEnd,
        long WriteHeadWaitsAtEnd,
        long NearLiveSnapsAtEnd,
        long DecodeErrorSnapsAtEnd,
        long LastWriteHeadWaitGapMsAtEnd,
        long SeekForwardDecodeCapHitsAtEnd,
        long SeekForwardDecodeCapHitsDelta,
        bool LastSeekHitForwardDecodeCapAtEnd);
}
