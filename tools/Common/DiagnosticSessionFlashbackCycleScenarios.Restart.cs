using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackCycleScenarios
{
    internal static async Task RunFlashbackRestartCycleAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback restart cycle: Flashback buffer did not become ready before restart");
            return;
        }

        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "pause" },
                null)
            .ConfigureAwait(false);
        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "seek", ["positionMs"] = 750 },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback restart cycle playback primed");

        var restartResponse = await sendCommandAsync("RestartFlashback", null, 305_000).ConfigureAwait(false);
        actions.Add("flashback restart requested");
        if (!AutomationSnapshotFormatter.IsSuccess(restartResponse))
        {
            warnings.Add($"flashback restart cycle: restart failed - {AutomationSnapshotFormatter.Get(restartResponse, "Message", "unknown error")}");
            return;
        }

        if (!await ValidateFlashbackRestartCycleActiveStateAsync(
                    warnings,
                    sendCommandAsync,
                    cancellationToken)
                .ConfigureAwait(false))
        {
            return;
        }

        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback restart cycle: Flashback buffer did not refill after restart");
            return;
        }

        await VerifyFlashbackRestartCycleExportAsync(
                outputDirectory,
                actions,
                warnings,
                sendCommandAsync)
            .ConfigureAwait(false);
    }
}
