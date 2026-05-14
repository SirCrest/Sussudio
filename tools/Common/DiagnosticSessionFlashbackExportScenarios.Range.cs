using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackExports;
using static Sussudio.Tools.DiagnosticSessionFlashbackSegments;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;
using static Sussudio.Tools.DiagnosticSessionMetrics;
using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackExportScenarios
{
    internal static async Task RunFlashbackRangeExportAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken,
        string scenarioLabel = "flashback range export",
        string exportFileName = "flashback-range-export.mp4",
        int outPointMs = 5_000,
        bool switchAudioDuringExport = false)
    {
        const int liveEdgeSafetyMarginMs = 5_000;
        const int leftEdgeSafetyMarginMs = 10_000;
        var requiredBufferedDurationMs = Math.Max(
            20_000,
            outPointMs + liveEdgeSafetyMarginMs + leftEdgeSafetyMarginMs);
        var requiredEncodedFrames = Math.Max(240, (long)Math.Ceiling(requiredBufferedDurationMs / 1000.0 * 60.0));
        if (!await WaitForFlashbackStressBufferReadyAsync(
                sendCommandAsync,
                cancellationToken,
                requiredBufferedDurationMs,
                requiredEncodedFrames,
                TimeSpan.FromSeconds(45)).ConfigureAwait(false))
        {
            warnings.Add(
                $"{scenarioLabel}: Flashback buffer did not become range-ready " +
                $"within 45s bufferedMs>={requiredBufferedDurationMs} encodedFrames>={requiredEncodedFrames}");
            return;
        }

        var baselineSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        TryGetSnapshot(baselineSnapshotResponse, out var baselineSnapshot);
        var bufferedDurationMs = GetNullableLong(baselineSnapshot, "FlashbackBufferedDurationMs") ?? 0;
        var rangeEndMs = (int)Math.Clamp(bufferedDurationMs - liveEdgeSafetyMarginMs, 0, int.MaxValue);
        var rangeStartMs = Math.Max(0, rangeEndMs - outPointMs);
        if (rangeStartMs < leftEdgeSafetyMarginMs)
        {
            warnings.Add(
                $"{scenarioLabel}: insufficient near-live range headroom " +
                $"bufferedMs={bufferedDurationMs} startMs={rangeStartMs} endMs={rangeEndMs} " +
                $"requiredStartMs>={leftEdgeSafetyMarginMs}");
            return;
        }

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "clear-in-out-points" }, null)
            .ConfigureAwait(false);
        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "pause" }, null)
            .ConfigureAwait(false);
        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "seek", ["positionMs"] = rangeStartMs },
                null)
            .ConfigureAwait(false);
        if (!await WaitForFlashbackPlaybackPositionAsync(sendCommandAsync, rangeStartMs, TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false))
        {
            warnings.Add($"{scenarioLabel}: playback did not reach in-point seek before marking range");
        }

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "set-in-point" }, null)
            .ConfigureAwait(false);
        actions.Add($"{scenarioLabel} in point set positionMs={rangeStartMs}");

        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "seek", ["positionMs"] = rangeEndMs },
                null)
            .ConfigureAwait(false);
        if (!await WaitForFlashbackPlaybackPositionAsync(sendCommandAsync, rangeEndMs, TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false))
        {
            warnings.Add($"{scenarioLabel}: playback did not reach out-point seek before marking range");
        }

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "set-out-point" }, null)
            .ConfigureAwait(false);
        actions.Add($"{scenarioLabel} out point set positionMs={rangeEndMs}");

        var exportPath = Path.Combine(outputDirectory, exportFileName);
        var exportTask = sendCommandAsync(
                "FlashbackExport",
                new Dictionary<string, object?>
                {
                    ["seconds"] = 1,
                    ["outputPath"] = exportPath,
                    ["useSelectionRange"] = true,
                    ["force"] = true
                },
                60_000)
            ;
        Task? audioSwitchTask = null;
        if (switchAudioDuringExport)
        {
            audioSwitchTask = ToggleAudioEnabledDuringFlashbackExportAsync(
                exportTask,
                baselineSnapshot,
                actions,
                warnings,
                sendCommandAsync,
                cancellationToken);
        }

        var exportResponse = await exportTask.ConfigureAwait(false);
        if (audioSwitchTask is not null)
        {
            await audioSwitchTask.ConfigureAwait(false);
        }

        actions.Add($"{scenarioLabel} requested");
        if (!AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            warnings.Add($"{scenarioLabel}: export failed - {AutomationSnapshotFormatter.Get(exportResponse, "Message", "unknown error")}");
            await CleanupFlashbackSelectionAsync(sendCommandAsync).ConfigureAwait(false);
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
                $"{scenarioLabel} verification: {AutomationSnapshotFormatter.Get(verifyResponse, "Message", "verification failed")}");
        }
        else
        {
            actions.Add($"{scenarioLabel} verified");
        }

        var snapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        if (!TryGetSnapshot(snapshotResponse, out var snapshot))
        {
            warnings.Add($"{scenarioLabel}: no snapshot returned after export");
            await CleanupFlashbackSelectionAsync(sendCommandAsync).ConfigureAwait(false);
            return;
        }

        var inPointMs = GetNullableLong(snapshot, "FlashbackExportInPointMs") ?? 0;
        var markedOutPointMs = GetNullableLong(snapshot, "FlashbackExportOutPointMs") ?? 0;
        var exportedDurationMs = markedOutPointMs - inPointMs;
        var expectedDurationMinMs = Math.Max(0, outPointMs - 1_000);
        var expectedDurationMaxMs = outPointMs + 2_000;
        if (exportedDurationMs < expectedDurationMinMs || exportedDurationMs > expectedDurationMaxMs)
        {
            warnings.Add(
                $"{scenarioLabel}: selected export duration outside expected range " +
                $"in={inPointMs} out={markedOutPointMs} duration={exportedDurationMs} " +
                $"expected={expectedDurationMinMs}-{expectedDurationMaxMs}");
        }

        var status = GetString(snapshot, "FlashbackExportStatus") ?? "Unknown";
        if (!string.Equals(status, "Succeeded", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"{scenarioLabel}: expected Succeeded status, got {status}");
        }

        await CleanupFlashbackSelectionAsync(sendCommandAsync).ConfigureAwait(false);
        actions.Add($"{scenarioLabel} cleared range and went live");

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
