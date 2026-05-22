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

    private static FlashbackPlaybackAudioMasterFlattenedProjection BuildFlashbackPlaybackAudioMasterFlattenedProjection(
        FlashbackPlaybackAudioMasterProjection audioMaster)
        => new()
        {
            DelayDoubles = audioMaster.DelayDoubles,
            DelayShrinks = audioMaster.DelayShrinks,
            Fallbacks = audioMaster.Fallbacks,
            UnavailableFallbacks = audioMaster.UnavailableFallbacks,
            StaleFallbacks = audioMaster.StaleFallbacks,
            DriftOutlierFallbacks = audioMaster.DriftOutlierFallbacks,
            LastFallbackReason = audioMaster.LastFallbackReason,
            LastFallbackDriftMs = audioMaster.LastFallbackDriftMs,
            LastFallbackClockAgeMs = audioMaster.LastFallbackClockAgeMs
        };

    private readonly record struct FlashbackPlaybackAudioMasterFlattenedProjection
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
