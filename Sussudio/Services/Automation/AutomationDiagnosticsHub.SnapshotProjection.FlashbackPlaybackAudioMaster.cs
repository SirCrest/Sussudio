using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackPlaybackAudioMasterProjection BuildFlashbackPlaybackAudioMasterProjection(CaptureHealthSnapshot health)
        => new()
        {
            DelayDoubles = health.FlashbackPlaybackAudioMasterDelayDoubles,
            DelayShrinks = health.FlashbackPlaybackAudioMasterDelayShrinks,
            Fallbacks = health.FlashbackPlaybackAudioMasterFallbacks,
            UnavailableFallbacks = health.FlashbackPlaybackAudioMasterUnavailableFallbacks,
            StaleFallbacks = health.FlashbackPlaybackAudioMasterStaleFallbacks,
            DriftOutlierFallbacks = health.FlashbackPlaybackAudioMasterDriftOutlierFallbacks,
            LastFallbackReason = health.FlashbackPlaybackAudioMasterLastFallbackReason,
            LastFallbackDriftMs = health.FlashbackPlaybackAudioMasterLastFallbackDriftMs,
            LastFallbackClockAgeMs = health.FlashbackPlaybackAudioMasterLastFallbackClockAgeMs
        };

    private readonly record struct FlashbackPlaybackAudioMasterProjection
    {
        public long DelayDoubles { get; init; }
        public long DelayShrinks { get; init; }
        public long Fallbacks { get; init; }
        public long UnavailableFallbacks { get; init; }
        public long StaleFallbacks { get; init; }
        public long DriftOutlierFallbacks { get; init; }
        public string LastFallbackReason { get; init; }
        public double LastFallbackDriftMs { get; init; }
        public double LastFallbackClockAgeMs { get; init; }
    }
}
