using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private static FlashbackPlaybackAudioMasterHealthSnapshotFields CaptureFlashbackPlaybackAudioMasterHealthSnapshotFields(
        FlashbackPlaybackController? fbPlayback)
        => new(
            fbPlayback?.PlaybackAudioMasterDelayDoubles ?? 0,
            fbPlayback?.PlaybackAudioMasterDelayShrinks ?? 0,
            fbPlayback?.PlaybackAudioMasterFallbacks ?? 0,
            fbPlayback?.PlaybackAudioMasterUnavailableFallbacks ?? 0,
            fbPlayback?.PlaybackAudioMasterStaleFallbacks ?? 0,
            fbPlayback?.PlaybackAudioMasterDriftOutlierFallbacks ?? 0,
            fbPlayback?.PlaybackAudioMasterLastFallbackReason ?? string.Empty,
            fbPlayback?.PlaybackAudioMasterLastFallbackDriftMs ?? 0,
            fbPlayback?.PlaybackAudioMasterLastFallbackClockAgeMs ?? 0);

    private readonly record struct FlashbackPlaybackAudioMasterHealthSnapshotFields(
        long DelayDoubles,
        long DelayShrinks,
        long Fallbacks,
        long UnavailableFallbacks,
        long StaleFallbacks,
        long DriftOutlierFallbacks,
        string LastFallbackReason,
        double LastFallbackDriftMs,
        double LastFallbackClockAgeMs);
}
