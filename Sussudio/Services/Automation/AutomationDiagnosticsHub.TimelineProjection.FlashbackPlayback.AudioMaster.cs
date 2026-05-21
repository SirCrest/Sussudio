using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PerformanceTimelineFlashbackPlaybackAudioMasterProjection BuildPerformanceTimelineFlashbackPlaybackAudioMasterProjection(
        AutomationSnapshot snapshot)
        => new(
            DelayDoubles: snapshot.FlashbackPlaybackAudioMasterDelayDoubles,
            DelayShrinks: snapshot.FlashbackPlaybackAudioMasterDelayShrinks,
            Fallbacks: snapshot.FlashbackPlaybackAudioMasterFallbacks,
            UnavailableFallbacks: snapshot.FlashbackPlaybackAudioMasterUnavailableFallbacks,
            StaleFallbacks: snapshot.FlashbackPlaybackAudioMasterStaleFallbacks,
            DriftOutlierFallbacks: snapshot.FlashbackPlaybackAudioMasterDriftOutlierFallbacks,
            LastFallbackReason: snapshot.FlashbackPlaybackAudioMasterLastFallbackReason,
            LastFallbackClockAgeMs: snapshot.FlashbackPlaybackAudioMasterLastFallbackClockAgeMs);

    private readonly record struct PerformanceTimelineFlashbackPlaybackAudioMasterProjection(
        long DelayDoubles,
        long DelayShrinks,
        long Fallbacks,
        long UnavailableFallbacks,
        long StaleFallbacks,
        long DriftOutlierFallbacks,
        string LastFallbackReason,
        double LastFallbackClockAgeMs);
}
