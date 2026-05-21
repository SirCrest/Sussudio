using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PerformanceTimelineFlashbackPlaybackCadenceProjection BuildPerformanceTimelineFlashbackPlaybackCadenceProjection(
        AutomationSnapshot snapshot)
        => new(
            TargetFps: snapshot.FlashbackPlaybackTargetFps,
            ObservedFps: snapshot.FlashbackPlaybackObservedFps,
            P99FrameMs: snapshot.FlashbackPlaybackP99FrameMs,
            MaxFrameMs: snapshot.FlashbackPlaybackMaxFrameMs,
            OnePercentLowFps: snapshot.FlashbackPlaybackOnePercentLowFps,
            FivePercentLowFps: snapshot.FlashbackPlaybackFivePercentLowFps,
            SlowFramePercent: snapshot.FlashbackPlaybackSlowFramePercent,
            DroppedFrames: snapshot.FlashbackPlaybackDroppedFrames);

    private readonly record struct PerformanceTimelineFlashbackPlaybackCadenceProjection(
        double TargetFps,
        double ObservedFps,
        double P99FrameMs,
        double MaxFrameMs,
        double OnePercentLowFps,
        double FivePercentLowFps,
        double SlowFramePercent,
        long DroppedFrames);
}
