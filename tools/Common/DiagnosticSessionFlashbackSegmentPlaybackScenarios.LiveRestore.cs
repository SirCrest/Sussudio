using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackSegmentPlaybackScenarios
{
    private static async Task ReturnFlashbackSegmentPlaybackLiveAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "go-live" }, null)
            .ConfigureAwait(false);
        actions.Add("flashback segment playback go-live requested");

        var finalSnapshot = await WaitForFlashbackPlaybackStateAsync(
                sendCommandAsync,
                "Live",
                TimeSpan.FromSeconds(3),
                cancellationToken)
            .ConfigureAwait(false);
        if (finalSnapshot?.ValueKind == JsonValueKind.Object)
        {
            var finalState = GetString(finalSnapshot.Value, "FlashbackPlaybackState") ?? "Unknown";
            if (!string.Equals(finalState, "Live", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"flashback segment playback: playback ended in state {finalState}");
            }
        }
    }
}
