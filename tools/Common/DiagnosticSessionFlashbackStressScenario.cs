using System.Diagnostics;
using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackExports;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;
using static Sussudio.Tools.DiagnosticSessionMetrics;
using static Sussudio.Tools.DiagnosticSessionText;

namespace Sussudio.Tools;

internal static class DiagnosticSessionFlashbackStressScenario
{
    internal const int FlashbackStressMaxPlaybackPendingCommands = 4;
    internal const int FlashbackStressMaxPlaybackCommandLatencyMs = 750;
    internal const double FlashbackStressPlaybackWarmSeconds = 10.0;
    internal const long FlashbackStressAudioUnavailableFallbackAllowance = 4;
    internal const int FlashbackScrubStressMaxPlaybackPendingCommands = 20;

    internal static async Task RunFlashbackStressAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback stress: Flashback buffer did not become export-ready within 30s");
            return;
        }

        var baselineSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        TryGetSnapshot(baselineSnapshotResponse, out var baselineSnapshot);

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "pause" }, null)
            .ConfigureAwait(false);
        actions.Add("flashback pause requested");

        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "seek", ["positionMs"] = 500 },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback seek requested");

        foreach (var positionMs in new[] { 750, 1_250, 2_000, 3_250, 1_500 })
        {
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            await sendCommandAsync(
                    "FlashbackAction",
                    new Dictionary<string, object?> { ["action"] = "seek", ["positionMs"] = positionMs },
                    null)
                .ConfigureAwait(false);
        }
        actions.Add("flashback scrub burst requested");

        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "play" }, null)
            .ConfigureAwait(false);
        actions.Add("flashback play requested");

        var playbackBaselineSnapshot = await WaitForFlashbackPlaybackStateAsync(
                sendCommandAsync,
                "Playing",
                TimeSpan.FromSeconds(5),
                cancellationToken)
            .ConfigureAwait(false);
        if (playbackBaselineSnapshot?.ValueKind != JsonValueKind.Object ||
            !string.Equals(
                GetString(playbackBaselineSnapshot.Value, "FlashbackPlaybackState"),
                "Playing",
                StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("flashback stress: playback did not enter Playing before warm sample");
        }

        var warmBaselineSnapshot = playbackBaselineSnapshot?.ValueKind == JsonValueKind.Object
            ? playbackBaselineSnapshot.Value
            : baselineSnapshot;
        var baselineFrameCount = GetNullableLong(warmBaselineSnapshot, "FlashbackPlaybackFrameCount") ?? 0;
        var baselineAudioFallbacks = GetNullableLong(warmBaselineSnapshot, "FlashbackPlaybackAudioMasterFallbacks") ?? 0;
        var baselineAudioUnavailableFallbacks = GetNullableLong(warmBaselineSnapshot, "FlashbackPlaybackAudioMasterUnavailableFallbacks") ?? 0;
        var baselineAudioStaleFallbacks = GetNullableLong(warmBaselineSnapshot, "FlashbackPlaybackAudioMasterStaleFallbacks") ?? 0;
        var baselineAudioDriftOutlierFallbacks = GetNullableLong(warmBaselineSnapshot, "FlashbackPlaybackAudioMasterDriftOutlierFallbacks") ?? 0;
        var warmedPlaybackSnapshot = await WaitForFlashbackPlaybackWarmSampleAsync(
                sendCommandAsync,
                baselineFrameCount,
                FlashbackStressPlaybackWarmSeconds,
                TimeSpan.FromSeconds(15),
                cancellationToken)
            .ConfigureAwait(false);
        if (warmedPlaybackSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add($"flashback stress: playback did not warm for {FlashbackStressPlaybackWarmSeconds:0.#}s before go-live");
        }
        else
        {
            var warmedFrames = GetNullableLong(warmedPlaybackSnapshot.Value, "FlashbackPlaybackFrameCount") ?? 0;
            var warmedObservedFps = GetDouble(warmedPlaybackSnapshot.Value, "FlashbackPlaybackObservedFps");
            var warmedOnePercentLow = GetDouble(warmedPlaybackSnapshot.Value, "FlashbackPlaybackOnePercentLowFps");
            var warmedTargetFps = GetDouble(warmedPlaybackSnapshot.Value, "FlashbackPlaybackTargetFps");
            if (warmedTargetFps <= 0)
            {
                warmedTargetFps = GetDouble(warmedPlaybackSnapshot.Value, "SelectedExactFrameRate");
            }

            var warmedAudioFallbacks = GetNullableLong(warmedPlaybackSnapshot.Value, "FlashbackPlaybackAudioMasterFallbacks") ?? 0;
            var warmedAudioFallbackDelta = Math.Max(0, warmedAudioFallbacks - baselineAudioFallbacks);
            var warmedAudioUnavailableDelta = Math.Max(
                0,
                (GetNullableLong(warmedPlaybackSnapshot.Value, "FlashbackPlaybackAudioMasterUnavailableFallbacks") ?? 0) -
                baselineAudioUnavailableFallbacks);
            var warmedAudioStaleDelta = Math.Max(
                0,
                (GetNullableLong(warmedPlaybackSnapshot.Value, "FlashbackPlaybackAudioMasterStaleFallbacks") ?? 0) -
                baselineAudioStaleFallbacks);
            var warmedAudioDriftOutlierDelta = Math.Max(
                0,
                (GetNullableLong(warmedPlaybackSnapshot.Value, "FlashbackPlaybackAudioMasterDriftOutlierFallbacks") ?? 0) -
                baselineAudioDriftOutlierFallbacks);
            var warmedAudioLastFallbackReason = GetString(
                warmedPlaybackSnapshot.Value,
                "FlashbackPlaybackAudioMasterLastFallbackReason") ?? string.Empty;
            actions.Add(
                $"flashback playback warmed frames={Math.Max(0, warmedFrames - baselineFrameCount)} " +
                $"fps={warmedObservedFps:0.##} onePercentLow={warmedOnePercentLow:0.##} " +
                $"audioFallbackDelta={warmedAudioFallbackDelta} " +
                $"unavailableDelta={warmedAudioUnavailableDelta} " +
                $"staleDelta={warmedAudioStaleDelta} " +
                $"driftOutlierDelta={warmedAudioDriftOutlierDelta} " +
                $"lastAudioFallback={FormatOptional(warmedAudioLastFallbackReason)}");
            if (warmedTargetFps > 0)
            {
                var observedFloor = warmedTargetFps * 0.95;
                if (warmedObservedFps > 0 && warmedObservedFps < observedFloor)
                {
                    warnings.Add($"flashback stress: warmed playback observed FPS below floor fps={warmedObservedFps:0.##} floor={observedFloor:0.##}");
                }

                var onePercentLowFloor = warmedTargetFps * 0.80;
                if (warmedOnePercentLow > 0 && warmedOnePercentLow < onePercentLowFloor)
                {
                    warnings.Add($"flashback stress: warmed playback 1% low below floor fps={warmedOnePercentLow:0.##} floor={onePercentLowFloor:0.##}");
                }
            }

            var audioMasterFallbackWarning = ClassifyFlashbackStressAudioMasterFallbackWarning(
                warmedAudioFallbackDelta,
                warmedAudioUnavailableDelta,
                warmedAudioStaleDelta,
                warmedAudioDriftOutlierDelta);
            if (audioMasterFallbackWarning is { Length: > 0 })
            {
                warnings.Add(audioMasterFallbackWarning);
            }
        }

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "go-live" }, null)
            .ConfigureAwait(false);
        actions.Add("flashback go-live requested");

        var exportPath = Path.Combine(outputDirectory, "flashback-stress-export.mp4");
        var exportResponse = await sendCommandAsync(
                "FlashbackExport",
                new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = exportPath },
                60_000)
            .ConfigureAwait(false);
        actions.Add("flashback stress export requested");

        if (AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            await sendCommandAsync(
                    "VerifyFile",
                    CreateFlashbackExportVerifyPayload(exportPath),
                    60_000)
                .ConfigureAwait(false);
            actions.Add("flashback stress export verified");
        }

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

    internal static string? ClassifyFlashbackStressAudioMasterFallbackWarning(
        long totalDelta,
        long unavailableDelta,
        long staleDelta,
        long driftOutlierDelta)
    {
        if (totalDelta <= 0)
        {
            return null;
        }

        if (staleDelta > 0 || driftOutlierDelta > 0)
        {
            return
                "flashback stress: audio-master harmful fallbacks increased during warmed playback " +
                $"staleDelta={staleDelta} driftOutlierDelta={driftOutlierDelta} " +
                $"totalDelta={totalDelta}";
        }

        if (unavailableDelta > FlashbackStressAudioUnavailableFallbackAllowance)
        {
            return
                "flashback stress: audio-master unavailable fallbacks exceeded startup allowance " +
                $"unavailableDelta={unavailableDelta} allowance={FlashbackStressAudioUnavailableFallbackAllowance} " +
                $"totalDelta={totalDelta}";
        }

        if (unavailableDelta <= 0)
        {
            return $"flashback stress: audio-master unclassified fallbacks increased during warmed playback delta={totalDelta}";
        }

        return null;
    }
}
