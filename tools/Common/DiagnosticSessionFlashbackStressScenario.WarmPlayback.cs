using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;
using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackStressScenario
{
    private static async Task ValidateFlashbackStressWarmPlaybackAsync(
        JsonElement baselineSnapshot,
        JsonElement? playbackBaselineSnapshot,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var warmBaselineSnapshot = playbackBaselineSnapshot?.ValueKind == JsonValueKind.Object
            ? playbackBaselineSnapshot.Value
            : baselineSnapshot;
        var baselineFrameCount = GetNullableLong(warmBaselineSnapshot, "FlashbackPlaybackFrameCount") ?? 0;
        var baselineAudioFallbacks = CaptureFlashbackStressWarmPlaybackAudioBaseline(warmBaselineSnapshot);
        var warmedPlaybackSnapshot = await WaitForFlashbackPlaybackWarmSampleAsync(
                sendCommandAsync,
                baselineFrameCount,
                FlashbackStressPlaybackWarmSeconds,
                TimeSpan.FromSeconds(15),
                cancellationToken)
            .ConfigureAwait(false);
        if (warmedPlaybackSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add($"flashback stress: playback did not warm for {FlashbackStressPlaybackWarmSeconds:0.#}s before go-live");
            return;
        }

        var warmedFrames = GetNullableLong(warmedPlaybackSnapshot.Value, "FlashbackPlaybackFrameCount") ?? 0;
        var warmedObservedFps = GetDouble(warmedPlaybackSnapshot.Value, "FlashbackPlaybackObservedFps");
        var warmedOnePercentLow = GetDouble(warmedPlaybackSnapshot.Value, "FlashbackPlaybackOnePercentLowFps");
        var warmedTargetFps = GetDouble(warmedPlaybackSnapshot.Value, "FlashbackPlaybackTargetFps");
        if (warmedTargetFps <= 0)
        {
            warmedTargetFps = GetDouble(warmedPlaybackSnapshot.Value, "SelectedExactFrameRate");
        }

        var warmedAudioFallbacks = CaptureFlashbackStressWarmPlaybackAudioDeltas(
            warmedPlaybackSnapshot.Value,
            baselineAudioFallbacks);
        actions.Add(
            $"flashback playback warmed frames={Math.Max(0, warmedFrames - baselineFrameCount)} " +
            $"fps={warmedObservedFps:0.##} onePercentLow={warmedOnePercentLow:0.##} " +
            $"audioFallbackDelta={warmedAudioFallbacks.TotalDelta} " +
            $"unavailableDelta={warmedAudioFallbacks.UnavailableDelta} " +
            $"staleDelta={warmedAudioFallbacks.StaleDelta} " +
            $"driftOutlierDelta={warmedAudioFallbacks.DriftOutlierDelta} " +
            $"lastAudioFallback={FormatOptional(warmedAudioFallbacks.LastReason)}");
        if (warmedTargetFps > 0)
        {
            var observedFloor = warmedTargetFps * 0.95;
            if (warmedObservedFps > 0 && warmedObservedFps < observedFloor)
            {
                warnings.Add($"flashback stress: warmed playback observed FPS below floor fps={warmedObservedFps:0.##} floor={observedFloor:0.##}");
            }

            var onePercentLowFloor = warmedTargetFps * 0.80;
            if (warmedOnePercentLow > 0 && warmedOnePercentLow < onePercentLowFloor)
            {
                warnings.Add($"flashback stress: warmed playback 1% low below floor fps={warmedOnePercentLow:0.##} floor={onePercentLowFloor:0.##}");
            }
        }

        var audioMasterFallbackWarning = ClassifyFlashbackStressAudioMasterFallbackWarning(
            warmedAudioFallbacks.TotalDelta,
            warmedAudioFallbacks.UnavailableDelta,
            warmedAudioFallbacks.StaleDelta,
            warmedAudioFallbacks.DriftOutlierDelta);
        if (audioMasterFallbackWarning is { Length: > 0 })
        {
            warnings.Add(audioMasterFallbackWarning);
        }
    }
}
