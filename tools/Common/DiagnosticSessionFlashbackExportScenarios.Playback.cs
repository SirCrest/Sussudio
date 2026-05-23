using System.Text.Json;
using static Sussudio.Tools.DiagnosticSessionFlashbackExports;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;
using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionMetrics;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackExportScenarios
{
    internal static async Task RunFlashbackExportPlaybackAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback export playback: Flashback buffer did not become playback-ready within 30s");
            return;
        }

        var baselineSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        TryGetSnapshot(baselineSnapshotResponse, out var baselineSnapshot);

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "pause" }, null)
            .ConfigureAwait(false);
        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "seek", ["positionMs"] = 1_000 },
                null)
            .ConfigureAwait(false);
        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "play" }, null)
            .ConfigureAwait(false);
        actions.Add("flashback export playback play requested");

        var playbackFrameCountBeforeExport = await CaptureFlashbackExportPlaybackFrameCountBeforeExportAsync(
                warnings,
                sendCommandAsync,
                cancellationToken)
            .ConfigureAwait(false);

        var exportPath = Path.Combine(outputDirectory, "flashback-export-playback.mp4");
        var exportResponse = await sendCommandAsync(
                "FlashbackExport",
                new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = exportPath },
                60_000)
            .ConfigureAwait(false);
        actions.Add("flashback export during playback requested");
        if (!AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            warnings.Add($"flashback export playback: export failed - {AutomationSnapshotFormatter.Get(exportResponse, "Message", "unknown error")}");
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
                $"flashback export playback verification: {AutomationSnapshotFormatter.Get(verifyResponse, "Message", "verification failed")}");
            return;
        }

        actions.Add("flashback export during playback verified");

        await ValidateFlashbackExportPlaybackAfterExportAsync(
                playbackFrameCountBeforeExport,
                warnings,
                sendCommandAsync)
            .ConfigureAwait(false);

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "go-live" }, null)
            .ConfigureAwait(false);
        actions.Add("flashback export playback go-live requested");

        await ValidateFlashbackExportPlaybackFinalStateAsync(
                baselineSnapshot,
                warnings,
                sendCommandAsync,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<long> CaptureFlashbackExportPlaybackFrameCountBeforeExportAsync(
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var playbackSnapshotOrNull = await WaitForFlashbackPlaybackStateAsync(
                sendCommandAsync,
                "Playing",
                TimeSpan.FromSeconds(5),
                cancellationToken)
            .ConfigureAwait(false);
        JsonElement playbackSnapshot;
        if (playbackSnapshotOrNull is null)
        {
            warnings.Add("flashback export playback: playback did not report Playing within 5s before export");
            var playbackSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            TryGetSnapshot(playbackSnapshotResponse, out var fallbackPlaybackSnapshot);
            playbackSnapshot = fallbackPlaybackSnapshot;
        }
        else
        {
            playbackSnapshot = playbackSnapshotOrNull.Value;
        }

        var playbackStateBeforeExport = GetString(playbackSnapshot, "FlashbackPlaybackState") ?? "Unknown";
        if (!string.Equals(playbackStateBeforeExport, "Playing", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback export playback: expected Playing before export, got {playbackStateBeforeExport}");
        }

        return GetNullableLong(playbackSnapshot, "FlashbackPlaybackFrameCount") ?? 0;
    }

    private static async Task ValidateFlashbackExportPlaybackAfterExportAsync(
        long playbackFrameCountBeforeExport,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync)
    {
        var postExportSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        TryGetSnapshot(postExportSnapshotResponse, out var postExportSnapshot);
        var playbackFrameCountAfterExport = GetNullableLong(postExportSnapshot, "FlashbackPlaybackFrameCount") ?? 0;
        var playbackStateAfterExport = GetString(postExportSnapshot, "FlashbackPlaybackState") ?? "Unknown";
        if (!string.Equals(playbackStateAfterExport, "Playing", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback export playback: expected Playing after export, got {playbackStateAfterExport}");
        }

        if (playbackFrameCountAfterExport <= playbackFrameCountBeforeExport)
        {
            warnings.Add(
                "flashback export playback: playback frame count did not advance during export " +
                $"before={playbackFrameCountBeforeExport} after={playbackFrameCountAfterExport}");
        }
    }

    private static async Task ValidateFlashbackExportPlaybackFinalStateAsync(
        JsonElement baselineSnapshot,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        var finalSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        if (!TryGetSnapshot(finalSnapshotResponse, out var finalSnapshot))
        {
            warnings.Add("flashback export playback: no final snapshot returned");
            return;
        }

        var commandHealth = BuildPlaybackCommandHealth(finalSnapshot, baselineSnapshot);
        var pending = GetInt(finalSnapshot, "FlashbackPlaybackPendingCommands");
        var state = GetString(finalSnapshot, "FlashbackPlaybackState") ?? "Unknown";
        if (commandHealth.NonCoalescedDropped > 0 || commandHealth.Skipped > 0 || commandHealth.SubmitFailures > 0)
        {
            warnings.Add(
                "flashback export playback: " +
                $"dropped={commandHealth.Dropped} nonCoalescedDropped={commandHealth.NonCoalescedDropped} " +
                $"coalescedScrub={commandHealth.CoalescedScrub} coalescedSeek={commandHealth.CoalescedSeek} skipped={commandHealth.Skipped} " +
                $"submitFailures={commandHealth.SubmitFailures}");
        }

        if (pending > 0)
        {
            warnings.Add($"flashback export playback: pending commands remained after go-live pending={pending}");
        }

        if (!string.Equals(state, "Live", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback export playback: playback ended in state {state}");
        }
    }
}
