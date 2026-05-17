using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;
using static Sussudio.Tools.DiagnosticSessionMetrics;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackExportScenarios
{
    private static async Task ValidateFlashbackRangeExportCleanupAsync(
        JsonElement baselineSnapshot,
        string scenarioLabel,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        var finalSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        if (!TryGetSnapshot(finalSnapshotResponse, out var finalSnapshot))
        {
            warnings.Add($"{scenarioLabel}: no final snapshot returned");
            return;
        }

        var pending = GetInt(finalSnapshot, "FlashbackPlaybackPendingCommands");
        var commandHealth = BuildPlaybackCommandHealth(finalSnapshot, baselineSnapshot);
        var state = GetString(finalSnapshot, "FlashbackPlaybackState") ?? "Unknown";
        if (pending > 0)
        {
            warnings.Add($"{scenarioLabel}: pending commands remained after go-live pending={pending}");
        }

        if (commandHealth.NonCoalescedDropped > 0 || commandHealth.Skipped > 0 || commandHealth.SubmitFailures > 0)
        {
            warnings.Add(
                $"{scenarioLabel}: " +
                $"dropped={commandHealth.Dropped} nonCoalescedDropped={commandHealth.NonCoalescedDropped} " +
                $"coalescedScrub={commandHealth.CoalescedScrub} coalescedSeek={commandHealth.CoalescedSeek} skipped={commandHealth.Skipped} " +
                $"submitFailures={commandHealth.SubmitFailures}");
        }

        if (!string.Equals(state, "Live", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"{scenarioLabel}: playback ended in state {state}");
        }
    }
}
