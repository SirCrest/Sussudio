using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackMetrics
{
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
