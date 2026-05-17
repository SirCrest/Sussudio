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

    private readonly record struct FlashbackPlaybackCadenceHealthSnapshotFields(
        int SampleCount,
        double P95FrameMs,
        double P99FrameMs,
        double MaxFrameMs,
        long SlowFrames,
        double SlowFramePercent,
        double OnePercentLowFps,
        double FivePercentLowFps,
        double SampleDurationMs,
        double[] RecentFrameIntervalsMs);
}
