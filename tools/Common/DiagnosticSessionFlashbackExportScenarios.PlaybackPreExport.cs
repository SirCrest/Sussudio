using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;
using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackExportScenarios
{
    private static async Task<long> CaptureFlashbackExportPlaybackFrameCountBeforeExportAsync(
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var playbackSnapshotOrNull = await WaitForFlashbackPlaybackStateAsync(
                sendCommandAsync,
                "Playing",
                TimeSpan.FromSeconds(5),
                cancellationToken)
            .ConfigureAwait(false);
        JsonElement playbackSnapshot;
        if (playbackSnapshotOrNull is null)
        {
            warnings.Add("flashback export playback: playback did not report Playing within 5s before export");
            var playbackSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            TryGetSnapshot(playbackSnapshotResponse, out var fallbackPlaybackSnapshot);
            playbackSnapshot = fallbackPlaybackSnapshot;
        }
        else
        {
            playbackSnapshot = playbackSnapshotOrNull.Value;
        }

        var playbackStateBeforeExport = GetString(playbackSnapshot, "FlashbackPlaybackState") ?? "Unknown";
        if (!string.Equals(playbackStateBeforeExport, "Playing", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback export playback: expected Playing before export, got {playbackStateBeforeExport}");
        }

        return GetNullableLong(playbackSnapshot, "FlashbackPlaybackFrameCount") ?? 0;
    }
}
