using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;
using static Sussudio.Tools.DiagnosticSessionMetrics;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackStressScenario
{
    private static async Task ValidateFlashbackStressCommandDrainAsync(
        JsonElement baselineSnapshot,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var drainResult = await WaitForFlashbackStressPlaybackCommandDrainAsync(
                sendCommandAsync,
                cancellationToken)
            .ConfigureAwait(false);
        var lastSnapshot = drainResult.Snapshot;

        if (!drainResult.Drained)
        {
            warnings.Add(
                "flashback stress: playback command queue did not drain within 10s " +
                $"pending={GetInt(lastSnapshot, "FlashbackPlaybackPendingCommands")} " +
                $"maxPending={GetInt(lastSnapshot, "FlashbackPlaybackMaxPendingCommands")} " +
                $"lastLatencyMs={GetInt(lastSnapshot, "FlashbackPlaybackLastCommandQueueLatencyMs")} " +
                $"maxLatencyMs={GetInt(lastSnapshot, "FlashbackPlaybackMaxCommandQueueLatencyMs")}");
        }

        if (lastSnapshot.ValueKind == JsonValueKind.Object)
        {
            var commandHealth = BuildPlaybackCommandHealth(lastSnapshot, baselineSnapshot);
            var state = GetString(lastSnapshot, "FlashbackPlaybackState") ?? "Unknown";
            var maxPending = GetInt(lastSnapshot, "FlashbackPlaybackMaxPendingCommands");
            var maxLatencyMs = GetInt(lastSnapshot, "FlashbackPlaybackMaxCommandQueueLatencyMs");
            if (commandHealth.NonCoalescedDropped > 0 || commandHealth.Skipped > 0 || commandHealth.SubmitFailures > 0)
            {
                warnings.Add(
                    "flashback stress: " +
                    $"dropped={commandHealth.Dropped} nonCoalescedDropped={commandHealth.NonCoalescedDropped} " +
                    $"coalescedScrub={commandHealth.CoalescedScrub} coalescedSeek={commandHealth.CoalescedSeek} skipped={commandHealth.Skipped} " +
                    $"submitFailures={commandHealth.SubmitFailures}");
            }

            if (maxPending > FlashbackStressMaxPlaybackPendingCommands ||
                maxLatencyMs > FlashbackStressMaxPlaybackCommandLatencyMs)
            {
                warnings.Add(
                    "flashback stress: playback command latency exceeded threshold " +
                    $"maxPending={maxPending}/{FlashbackStressMaxPlaybackPendingCommands} " +
                    $"maxLatencyMs={maxLatencyMs}/{FlashbackStressMaxPlaybackCommandLatencyMs}");
            }

            if (!string.Equals(state, "Live", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"flashback stress: playback ended in state {state}");
            }
        }
    }
}
