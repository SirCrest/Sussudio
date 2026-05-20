using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackLifecycleScenarios
{
    private static async Task ValidateFlashbackLifecycleDisabledAsync(
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var disabledSnapshot = await WaitForFlashbackActiveAsync(
                sendCommandAsync,
                expectedActive: false,
                timeout: TimeSpan.FromSeconds(15),
                cancellationToken)
            .ConfigureAwait(false);
        if (disabledSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback lifecycle: Flashback did not report inactive after disable");
            return;
        }

        if (GetBool(disabledSnapshot.Value, "FlashbackPlaybackThreadAlive"))
        {
            warnings.Add("flashback lifecycle: playback worker still alive after disable");
        }

        if (GetInt(disabledSnapshot.Value, "FlashbackPlaybackPendingCommands") > 0)
        {
            warnings.Add(
                "flashback lifecycle: pending commands remained after disable " +
                $"pending={GetInt(disabledSnapshot.Value, "FlashbackPlaybackPendingCommands")}");
        }
    }

    private static async Task ValidateFlashbackLifecycleReenabledAsync(
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var enabledSnapshot = await WaitForFlashbackActiveAsync(
                sendCommandAsync,
                expectedActive: true,
                timeout: TimeSpan.FromSeconds(30),
                cancellationToken)
            .ConfigureAwait(false);
        if (enabledSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback lifecycle: Flashback did not report active after re-enable");
        }
    }
}
