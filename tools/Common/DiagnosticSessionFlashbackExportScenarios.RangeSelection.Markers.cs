using System.Text.Json;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackExportScenarios
{
    private static async Task MarkFlashbackSelectionPointAsync(
        int positionMs,
        string action,
        string label,
        string scenarioLabel,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "seek", ["positionMs"] = positionMs },
                null)
            .ConfigureAwait(false);
        if (!await WaitForFlashbackPlaybackPositionAsync(sendCommandAsync, positionMs, TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false))
        {
            warnings.Add($"{scenarioLabel}: playback did not reach {label}-point seek before marking range");
        }

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = action }, null)
            .ConfigureAwait(false);
        actions.Add($"{scenarioLabel} {label} point set positionMs={positionMs}");
    }
}
