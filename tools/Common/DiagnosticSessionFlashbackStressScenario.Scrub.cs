using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackStressScenario
{
    internal static async Task RunFlashbackScrubStressAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback scrub stress: Flashback buffer did not become playback-ready within 30s");
            return;
        }

        var baselineSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        TryGetSnapshot(baselineSnapshotResponse, out var baselineSnapshot);

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "pause" }, null)
            .ConfigureAwait(false);
        actions.Add("flashback scrub stress pause requested");

        var beginResponse = await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "begin-scrub", ["positionMs"] = 500 },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback scrub stress begin requested");
        if (!AutomationSnapshotFormatter.IsSuccess(beginResponse))
        {
            warnings.Add($"flashback scrub stress: begin-scrub failed - {AutomationSnapshotFormatter.Get(beginResponse, "Message", "unknown error")}");
            return;
        }

        var scrubbingSnapshot = await WaitForFlashbackPlaybackStateAsync(
                sendCommandAsync,
                "Scrubbing",
                TimeSpan.FromSeconds(5),
                cancellationToken)
            .ConfigureAwait(false);
        if (scrubbingSnapshot is null)
        {
            warnings.Add("flashback scrub stress: playback did not report Scrubbing within 5s");
        }

        var positions = new[]
        {
            250, 500, 750, 1_000, 1_250, 1_500, 1_750, 2_000,
            2_250, 2_500, 2_750, 3_000, 2_400, 1_800, 1_200, 600
        };
        var updateTasks = new Task<JsonElement>[positions.Length];
        for (var i = 0; i < positions.Length; i++)
        {
            updateTasks[i] = sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "update-scrub", ["positionMs"] = positions[i] },
                null);
        }

        var updateResponses = await Task.WhenAll(updateTasks).ConfigureAwait(false);
        actions.Add("flashback scrub stress update burst requested");
        var failedUpdates = 0;
        foreach (var response in updateResponses)
        {
            if (!AutomationSnapshotFormatter.IsSuccess(response))
            {
                failedUpdates++;
            }
        }

        if (failedUpdates > 0)
        {
            warnings.Add($"flashback scrub stress: {failedUpdates} update-scrub command(s) failed");
        }

        var endResponse = await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "end-scrub", ["positionMs"] = positions[^1] },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback scrub stress end requested");
        if (!AutomationSnapshotFormatter.IsSuccess(endResponse))
        {
            warnings.Add($"flashback scrub stress: end-scrub failed - {AutomationSnapshotFormatter.Get(endResponse, "Message", "unknown error")}");
            return;
        }

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "play" }, null)
            .ConfigureAwait(false);
        actions.Add("flashback scrub stress play requested");

        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "go-live" }, null)
            .ConfigureAwait(false);
        actions.Add("flashback scrub stress go-live requested");

        await ValidateFlashbackScrubStressDrainAsync(
                warnings,
                sendCommandAsync,
                baselineSnapshot,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
