using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private static FlashbackPlaybackDecodeHealthSnapshotFields CaptureFlashbackPlaybackDecodeHealthSnapshotFields(
        FlashbackPlaybackController? fbPlayback)
    {
        var playbackDecode = fbPlayback?.GetPlaybackDecodeMetrics() ?? default;
        return new FlashbackPlaybackDecodeHealthSnapshotFields(
            playbackDecode.SampleCount,
            playbackDecode.AvgMs,
            playbackDecode.P95Ms,
            playbackDecode.P99Ms,
            playbackDecode.MaxMs,
            fbPlayback?.PlaybackMaxDecodePhase ?? string.Empty,
            fbPlayback?.PlaybackMaxDecodeReceiveMs ?? 0,
            fbPlayback?.PlaybackMaxDecodeFeedMs ?? 0,
            fbPlayback?.PlaybackMaxDecodeReadMs ?? 0,
            fbPlayback?.PlaybackMaxDecodeSendMs ?? 0,
            fbPlayback?.PlaybackMaxDecodeAudioMs ?? 0,
            fbPlayback?.PlaybackMaxDecodeConvertMs ?? 0,
            fbPlayback?.PlaybackMaxDecodeUtcUnixMs ?? 0,
            fbPlayback?.PlaybackMaxDecodePositionMs ?? 0);
    }
}
