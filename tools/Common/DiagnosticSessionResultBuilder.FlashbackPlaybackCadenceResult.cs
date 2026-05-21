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
}
