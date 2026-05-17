using System.Diagnostics;
using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;
using static Sussudio.Tools.DiagnosticSessionMetrics;
using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackStressScenario
{
    private static async Task ValidateFlashbackScrubStressDrainAsync(
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        JsonElement baselineSnapshot,
        CancellationToken cancellationToken)
    {
        var drained = false;
        JsonElement lastSnapshot = default;
        var waitStarted = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(waitStarted) < TimeSpan.FromSeconds(10))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var snapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(snapshotResponse, out lastSnapshot) &&
                GetInt(lastSnapshot, "FlashbackPlaybackPendingCommands") == 0 &&
                string.Equals(
                    GetString(lastSnapshot, "FlashbackPlaybackState"),
                    "Live",
                    StringComparison.OrdinalIgnoreCase))
            {
                drained = true;
                break;
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }

        if (!drained)
        {
            warnings.Add(
                "flashback scrub stress: playback did not settle live with an empty queue within 10s " +
                $"pending={GetInt(lastSnapshot, "FlashbackPlaybackPendingCommands")} " +
                $"state={GetString(lastSnapshot, "FlashbackPlaybackState") ?? "Unknown"} " +
                $"threadAlive={GetBool(lastSnapshot, "FlashbackPlaybackThreadAlive")} " +
                $"maxPending={GetInt(lastSnapshot, "FlashbackPlaybackMaxPendingCommands")} " +
                $"lastLatencyMs={GetInt(lastSnapshot, "FlashbackPlaybackLastCommandQueueLatencyMs")} " +
                $"maxLatencyMs={GetInt(lastSnapshot, "FlashbackPlaybackMaxCommandQueueLatencyMs")} " +
                $"maxLatencyCommand={FormatOptional(GetString(lastSnapshot, "FlashbackPlaybackMaxCommandQueueLatencyCommand") ?? string.Empty)}");
            return;
        }

        var commandHealth = BuildPlaybackCommandHealth(lastSnapshot, baselineSnapshot);
        var state = GetString(lastSnapshot, "FlashbackPlaybackState") ?? "Unknown";
        var maxPending = GetInt(lastSnapshot, "FlashbackPlaybackMaxPendingCommands");
        var maxLatencyMs = GetInt(lastSnapshot, "FlashbackPlaybackMaxCommandQueueLatencyMs");
        var maxLatencyCommand = GetString(lastSnapshot, "FlashbackPlaybackMaxCommandQueueLatencyCommand") ?? string.Empty;

        if (commandHealth.NonCoalescedDropped > 0 || commandHealth.Skipped > 0 || commandHealth.SubmitFailures > 0)
        {
            warnings.Add(
                "flashback scrub stress: " +
                $"dropped={commandHealth.Dropped} nonCoalescedDropped={commandHealth.NonCoalescedDropped} " +
                $"coalescedScrub={commandHealth.CoalescedScrub} coalescedSeek={commandHealth.CoalescedSeek} skipped={commandHealth.Skipped} " +
                $"submitFailures={commandHealth.SubmitFailures}");
        }

        if (maxPending > FlashbackScrubStressMaxPlaybackPendingCommands ||
            maxLatencyMs > FlashbackStressMaxPlaybackCommandLatencyMs)
        {
            warnings.Add(
                "flashback scrub stress: playback command latency exceeded threshold " +
                $"maxPending={maxPending}/{FlashbackScrubStressMaxPlaybackPendingCommands} " +
                $"maxLatencyMs={maxLatencyMs}/{FlashbackStressMaxPlaybackCommandLatencyMs} " +
                $"maxLatencyCommand={FormatOptional(maxLatencyCommand)}");
        }

        if (!string.Equals(state, "Live", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback scrub stress: playback ended in state {state}");
        }
    }
}
