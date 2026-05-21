namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
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
}
