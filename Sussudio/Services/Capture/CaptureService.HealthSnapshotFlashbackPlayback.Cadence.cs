using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private static FlashbackPlaybackCadenceHealthSnapshotFields CaptureFlashbackPlaybackCadenceHealthSnapshotFields(
        FlashbackPlaybackController? fbPlayback)
    {
        var playbackCadence = fbPlayback?.GetPlaybackCadenceMetrics() ?? default;
        return new FlashbackPlaybackCadenceHealthSnapshotFields(
            playbackCadence.SampleCount,
            playbackCadence.P95FrameMs,
            playbackCadence.P99FrameMs,
            playbackCadence.MaxFrameMs,
            playbackCadence.SlowFrameCount,
            playbackCadence.SlowFramePercent,
            playbackCadence.OnePercentLowFps,
            playbackCadence.FivePercentLowFps,
            playbackCadence.SampleDurationMs,
            playbackCadence.RecentFrameIntervalsMs);
    }
}
