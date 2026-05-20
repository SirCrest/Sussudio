using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackPreviewCycleScenarios
{
    private static async Task<long> CapturePlaybackPreviewCycleFrameCountBeforeStopAsync(
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var playingSnapshot = await WaitForFlashbackPlaybackStateAsync(
                sendCommandAsync,
                "Playing",
                TimeSpan.FromSeconds(5),
                cancellationToken)
            .ConfigureAwait(false);
        if (playingSnapshot?.ValueKind != JsonValueKind.Object ||
            !string.Equals(GetString(playingSnapshot.Value, "FlashbackPlaybackState"), "Playing", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("flashback playback preview cycle: playback did not report Playing before preview stop");
            return 0;
        }

        var playbackFrameCountBeforeStop = GetNullableLong(playingSnapshot.Value, "FlashbackPlaybackFrameCount") ?? 0;
        if (playbackFrameCountBeforeStop > 0)
        {
            return playbackFrameCountBeforeStop;
        }

        var warmSnapshot = await WaitForFlashbackPlaybackWarmSampleAsync(
                sendCommandAsync,
                playbackFrameCountBeforeStop,
                0.25,
                TimeSpan.FromSeconds(5),
                cancellationToken)
            .ConfigureAwait(false);
        return warmSnapshot?.ValueKind == JsonValueKind.Object
            ? GetNullableLong(warmSnapshot.Value, "FlashbackPlaybackFrameCount") ?? 0
            : 0;
    }
}
