using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackStressScenario
{
    private readonly record struct FlashbackStressWarmPlaybackAudioBaseline(
        long Fallbacks,
        long UnavailableFallbacks,
        long StaleFallbacks,
        long DriftOutlierFallbacks);

    private readonly record struct FlashbackStressWarmPlaybackAudioDeltas(
        long TotalDelta,
        long UnavailableDelta,
        long StaleDelta,
        long DriftOutlierDelta,
        string LastReason);

    private static FlashbackStressWarmPlaybackAudioBaseline CaptureFlashbackStressWarmPlaybackAudioBaseline(
        JsonElement snapshot)
        => new(
            Fallbacks: GetNullableLong(snapshot, "FlashbackPlaybackAudioMasterFallbacks") ?? 0,
            UnavailableFallbacks: GetNullableLong(snapshot, "FlashbackPlaybackAudioMasterUnavailableFallbacks") ?? 0,
            StaleFallbacks: GetNullableLong(snapshot, "FlashbackPlaybackAudioMasterStaleFallbacks") ?? 0,
            DriftOutlierFallbacks: GetNullableLong(snapshot, "FlashbackPlaybackAudioMasterDriftOutlierFallbacks") ?? 0);

    private static FlashbackStressWarmPlaybackAudioDeltas CaptureFlashbackStressWarmPlaybackAudioDeltas(
        JsonElement warmedSnapshot,
        FlashbackStressWarmPlaybackAudioBaseline baseline)
        => new(
            TotalDelta: Math.Max(
                0,
                (GetNullableLong(warmedSnapshot, "FlashbackPlaybackAudioMasterFallbacks") ?? 0) -
                baseline.Fallbacks),
            UnavailableDelta: Math.Max(
                0,
                (GetNullableLong(warmedSnapshot, "FlashbackPlaybackAudioMasterUnavailableFallbacks") ?? 0) -
                baseline.UnavailableFallbacks),
            StaleDelta: Math.Max(
                0,
                (GetNullableLong(warmedSnapshot, "FlashbackPlaybackAudioMasterStaleFallbacks") ?? 0) -
                baseline.StaleFallbacks),
            DriftOutlierDelta: Math.Max(
                0,
                (GetNullableLong(warmedSnapshot, "FlashbackPlaybackAudioMasterDriftOutlierFallbacks") ?? 0) -
                baseline.DriftOutlierFallbacks),
            LastReason: GetString(
                warmedSnapshot,
                "FlashbackPlaybackAudioMasterLastFallbackReason") ?? string.Empty);
}
