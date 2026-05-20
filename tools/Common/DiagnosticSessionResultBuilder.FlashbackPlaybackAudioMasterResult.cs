namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
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
}
