using System.Text.Json;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackLifecycleScenarios
{
    internal static async Task RunFlashbackLifecycleAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback lifecycle: Flashback buffer did not become playback-ready within 30s");
            return;
        }

        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "pause" },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback lifecycle pause requested");

        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "seek", ["positionMs"] = 1_000 },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback lifecycle seek requested");

        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "play" },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback lifecycle play requested");

        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        await sendCommandAsync(
                "SetFlashbackEnabled",
                new Dictionary<string, object?> { ["enabled"] = false },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback lifecycle disabled during playback");

        await ValidateFlashbackLifecycleDisabledAsync(
                warnings,
                sendCommandAsync,
                cancellationToken)
            .ConfigureAwait(false);

        await sendCommandAsync(
                "SetFlashbackEnabled",
                new Dictionary<string, object?> { ["enabled"] = true },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback lifecycle re-enabled");

        await ValidateFlashbackLifecycleReenabledAsync(
                warnings,
                sendCommandAsync,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
