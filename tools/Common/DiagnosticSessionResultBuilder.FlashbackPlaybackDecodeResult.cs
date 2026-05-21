namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
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
}
