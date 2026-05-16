using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackExports;
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

        var activeSnapshot = await WaitForFlashbackActiveAsync(
                sendCommandAsync,
                expectedActive: true,
                timeout: TimeSpan.FromSeconds(30),
                cancellationToken)
            .ConfigureAwait(false);
        if (activeSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback restart cycle: Flashback did not report active after restart");
            return;
        }

        if (GetBool(activeSnapshot.Value, "FlashbackPlaybackThreadAlive"))
        {
            warnings.Add("flashback restart cycle: playback worker still alive after restart");
        }

        if (GetInt(activeSnapshot.Value, "FlashbackPlaybackPendingCommands") > 0)
        {
            warnings.Add(
                "flashback restart cycle: pending playback commands remained after restart " +
                $"pending={GetInt(activeSnapshot.Value, "FlashbackPlaybackPendingCommands")}");
        }

        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback restart cycle: Flashback buffer did not refill after restart");
            return;
        }

        var exportPath = Path.Combine(outputDirectory, "flashback-restart-cycle-export.mp4");
        var exportResponse = await sendCommandAsync(
                "FlashbackExport",
                new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = exportPath },
                60_000)
            .ConfigureAwait(false);
        actions.Add("flashback restart cycle export requested");
        if (!AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            warnings.Add($"flashback restart cycle: export failed - {AutomationSnapshotFormatter.Get(exportResponse, "Message", "unknown error")}");
            return;
        }

        var verifyResponse = await sendCommandAsync(
                "VerifyFile",
                CreateFlashbackExportVerifyPayload(exportPath),
                60_000)
            .ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(verifyResponse))
        {
            warnings.Add(
                $"flashback restart cycle export verification: {AutomationSnapshotFormatter.Get(verifyResponse, "Message", "verification failed")}");
            return;
        }

        actions.Add("flashback restart cycle export verified");
    }
}
