using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackMetrics
{
    private static FlashbackPlaybackResultCadenceMetrics BuildFlashbackPlaybackResultCadenceMetrics(
        bool observed,
        JsonElement endSnapshot) =>
        new(
            ObservedFpsAtEnd: GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackObservedFps"),
            AvgFrameMsAtEnd: GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackAvgFrameMs"),
            P99FrameMsAtEnd: GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackP99FrameMs"),
            MaxFrameMsAtEnd: GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackMaxFrameMs"),
            OnePercentLowFpsAtEnd: GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackOnePercentLowFps"),
            FrameCountAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackFrameCount"),
            LateFramesAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackLateFrames"),
            SlowFramesAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackSlowFrames"),
            SlowFramePercentAtEnd: GetObservedDouble(observed, endSnapshot, "FlashbackPlaybackSlowFramePercent"),
            DroppedFramesAtEnd: GetObservedLong(observed, endSnapshot, "FlashbackPlaybackDroppedFrames"));
}
