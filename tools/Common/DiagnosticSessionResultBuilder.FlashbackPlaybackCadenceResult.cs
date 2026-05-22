namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
    private readonly record struct DiagnosticSessionFlashbackPlaybackCadenceResultProjection(
        double FlashbackPlaybackObservedFpsAtEnd,
        double FlashbackPlaybackMinObservedFpsObserved,
        double FlashbackPlaybackAvgFrameMsAtEnd,
        double FlashbackPlaybackP99FrameMsAtEnd,
        double FlashbackPlaybackMaxFrameMsAtEnd,
        double FlashbackPlaybackMaxP99FrameMsObserved,
        double FlashbackPlaybackMaxFrameMsObserved,
        double FlashbackPlaybackMaxSlowFramePercentObserved,
        long FlashbackPlaybackFrameCountAtEnd,
        long FlashbackPlaybackLateFramesAtEnd,
        long FlashbackPlaybackSlowFramesAtEnd,
        double FlashbackPlaybackSlowFramePercentAtEnd,
        long FlashbackPlaybackDroppedFramesAtEnd,
        long FlashbackPlaybackDroppedFramesDelta);

    private static DiagnosticSessionFlashbackPlaybackCadenceResultProjection BuildFlashbackPlaybackCadenceResultProjection(
        FlashbackPlaybackSessionMetrics playbackSessionMetrics,
        FlashbackPlaybackResultMetrics playbackResultMetrics) =>
        new(
            FlashbackPlaybackObservedFpsAtEnd: playbackResultMetrics.ObservedFpsAtEnd,
            FlashbackPlaybackMinObservedFpsObserved: playbackSessionMetrics.MinObservedFpsObserved,
            FlashbackPlaybackAvgFrameMsAtEnd: playbackResultMetrics.AvgFrameMsAtEnd,
            FlashbackPlaybackP99FrameMsAtEnd: playbackResultMetrics.P99FrameMsAtEnd,
            FlashbackPlaybackMaxFrameMsAtEnd: playbackResultMetrics.MaxFrameMsAtEnd,
            FlashbackPlaybackMaxP99FrameMsObserved: playbackSessionMetrics.MaxP99FrameMsObserved,
            FlashbackPlaybackMaxFrameMsObserved: playbackSessionMetrics.MaxFrameMsObserved,
            FlashbackPlaybackMaxSlowFramePercentObserved: playbackSessionMetrics.MaxSlowFramePercentObserved,
            FlashbackPlaybackFrameCountAtEnd: playbackResultMetrics.FrameCountAtEnd,
            FlashbackPlaybackLateFramesAtEnd: playbackResultMetrics.LateFramesAtEnd,
            FlashbackPlaybackSlowFramesAtEnd: playbackResultMetrics.SlowFramesAtEnd,
            FlashbackPlaybackSlowFramePercentAtEnd: playbackResultMetrics.SlowFramePercentAtEnd,
            FlashbackPlaybackDroppedFramesAtEnd: playbackResultMetrics.DroppedFramesAtEnd,
            FlashbackPlaybackDroppedFramesDelta: playbackSessionMetrics.DroppedFramesDelta);

    private readonly record struct DiagnosticSessionFlashbackPlaybackOnePercentLowResultProjection(
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
        long FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks);

    private static DiagnosticSessionFlashbackPlaybackOnePercentLowResultProjection BuildFlashbackPlaybackOnePercentLowResultProjection(
        FlashbackPlaybackSessionMetrics playbackSessionMetrics,
        FlashbackPlaybackResultMetrics playbackResultMetrics) =>
        new(
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
            FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks: playbackSessionMetrics.MinOnePercentLowAudioMasterFallbacks);

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
