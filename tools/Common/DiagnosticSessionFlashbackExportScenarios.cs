using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackExports;
using static Sussudio.Tools.DiagnosticSessionFlashbackSegments;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;
using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;
using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;
using static Sussudio.Tools.DiagnosticSessionMetrics;

namespace Sussudio.Tools;

internal static class DiagnosticSessionFlashbackExportScenarios
{
    internal static async Task RunFlashbackExportConcurrentAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback concurrent export: Flashback buffer did not become export-ready within 30s");
            return;
        }

        var exportPathA = Path.Combine(outputDirectory, "flashback-concurrent-a.mp4");
        var exportPathB = Path.Combine(outputDirectory, "flashback-concurrent-b.mp4");
        // Diagnostic runs may execute against the same output directory across sessions;
        // pass force=true so the destination-exists guard does not break the diagnostic.
        var exportPayloadA = new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = exportPathA, ["force"] = true };
        var exportPayloadB = new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = exportPathB, ["force"] = true };

        var exportTimeoutMs = AutomationPipeProtocol.GetDefaultResponseTimeout("FlashbackExport");
        var exportTaskA = sendCommandAsync("FlashbackExport", exportPayloadA, exportTimeoutMs);
        var exportTaskB = sendCommandAsync("FlashbackExport", exportPayloadB, exportTimeoutMs);
        actions.Add("flashback concurrent export requests issued");

        var exportResponses = await Task.WhenAll(exportTaskA, exportTaskB).ConfigureAwait(false);
        for (var i = 0; i < exportResponses.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = exportResponses[i];
            var path = i == 0 ? exportPathA : exportPathB;
            var label = i == 0 ? "a" : "b";
            if (!AutomationSnapshotFormatter.IsSuccess(response))
            {
                warnings.Add(
                    $"flashback concurrent export {label}: {AutomationSnapshotFormatter.Get(response, "Message", "export failed")}");
                continue;
            }

            var verifyResponse = await sendCommandAsync(
                    "VerifyFile",
                    CreateFlashbackExportVerifyPayload(path),
                    60_000)
                .ConfigureAwait(false);
            if (!AutomationSnapshotFormatter.IsSuccess(verifyResponse))
            {
                warnings.Add(
                    $"flashback concurrent export {label} verification: {AutomationSnapshotFormatter.Get(verifyResponse, "Message", "verification failed")}");
            }
        }

        actions.Add("flashback concurrent exports verified");
    }

    internal static async Task RunFlashbackRotatedExportAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback rotated export: Flashback buffer did not become export-ready within 30s");
            return;
        }

        var completedSegment = await WaitForFlashbackCompletedSegmentAsync(
                sendCommandAsync,
                TimeSpan.FromSeconds(210),
                cancellationToken)
            .ConfigureAwait(false);
        if (completedSegment is null)
        {
            warnings.Add("flashback rotated export: no completed segment observed within 210s");
            return;
        }

        actions.Add(
            "flashback rotated segment observed " +
            $"seq={completedSegment.Value.SequenceNumber} " +
            $"startMs={completedSegment.Value.StartPtsMs} " +
            $"endMs={completedSegment.Value.EndPtsMs}");

        var exportPath = Path.Combine(outputDirectory, "flashback-rotated-export.mp4");
        var exportResponse = await sendCommandAsync(
                "FlashbackExport",
                new Dictionary<string, object?> { ["seconds"] = 180, ["outputPath"] = exportPath },
                300_000)
            .ConfigureAwait(false);
        actions.Add("flashback rotated export requested");

        var exportMessage = AutomationSnapshotFormatter.Get(exportResponse, "Message", string.Empty);
        if (!AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            warnings.Add(
                $"flashback rotated export: export failed - {AutomationSnapshotFormatter.Get(exportResponse, "Message", "unknown error")}");
            return;
        }

        var exportedSegments = TryParseFlashbackExportSegmentCount(exportMessage);
        if (exportedSegments is null or < 2)
        {
            warnings.Add($"flashback rotated export: expected multi-segment export, got '{exportMessage}'");
        }

        var verifyResponse = await sendCommandAsync(
                "VerifyFile",
                CreateFlashbackExportVerifyPayload(exportPath),
                120_000)
            .ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(verifyResponse))
        {
            warnings.Add(
                $"flashback rotated export verification: {AutomationSnapshotFormatter.Get(verifyResponse, "Message", "verification failed")}");
            return;
        }

        actions.Add("flashback rotated export verified");
    }

    internal static void RegisterSelectedFlashbackExportScenarioTasks(
        DiagnosticSessionScenarioPlan scenarioPlan,
        string outputDirectory,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendRawWithConnectRetryAsync,
        CancellationToken cancellationToken)
    {
        RegisterFlashbackExportPlaybackTask(
            scenarioPlan,
            outputDirectory,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            cancellationToken);

        RegisterFlashbackRangeExportTasks(
            scenarioPlan,
            outputDirectory,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            sendRawWithConnectRetryAsync,
            cancellationToken);

        RegisterFlashbackExportCoordinationTasks(
            scenarioPlan,
            outputDirectory,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            sendRawWithConnectRetryAsync,
            cancellationToken);
    }

    private static void RegisterFlashbackExportPlaybackTask(
        DiagnosticSessionScenarioPlan scenarioPlan,
        string outputDirectory,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        CancellationToken cancellationToken)
    {
        if (!scenarioPlan.RunFlashbackExportPlayback)
        {
            return;
        }

        backgroundTasks.AddScenario(
            6,
            "flashback-export-playback-task",
            RunFlashbackExportPlaybackAsync(
                outputDirectory,
                actions,
                warnings,
                sendAsync,
                cancellationToken));
        actions.Add("flashback export playback started");
    }

    private static void RegisterFlashbackRangeExportTasks(
        DiagnosticSessionScenarioPlan scenarioPlan,
        string outputDirectory,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendRawWithConnectRetryAsync,
        CancellationToken cancellationToken)
    {
        if (scenarioPlan.RunFlashbackRangeExport)
        {
            backgroundTasks.AddScenario(
                8,
                "flashback-range-export-task",
                RunFlashbackRangeExportAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback range export started");
        }

        if (scenarioPlan.RunFlashbackRangeExportAudioSwitch)
        {
            backgroundTasks.AddScenario(
                9,
                "flashback-range-export-audio-switch-task",
                RunFlashbackRangeExportAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendRawWithConnectRetryAsync,
                    cancellationToken,
                    scenarioLabel: "flashback range export audio switch",
                    exportFileName: "flashback-range-export-audio-switch.mp4",
                    outPointMs: 15_000,
                    switchAudioDuringExport: true));
            actions.Add("flashback range export audio switch started");
        }
    }

    private static void RegisterFlashbackExportCoordinationTasks(
        DiagnosticSessionScenarioPlan scenarioPlan,
        string outputDirectory,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendRawWithConnectRetryAsync,
        CancellationToken cancellationToken)
    {
        if (scenarioPlan.RunFlashbackExportConcurrent)
        {
            backgroundTasks.AddScenario(
                10,
                "flashback-export-concurrent-task",
                RunFlashbackExportConcurrentAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendRawWithConnectRetryAsync,
                    cancellationToken));
            actions.Add("flashback concurrent export started");
        }

        if (scenarioPlan.RunFlashbackDisableDuringExport)
        {
            backgroundTasks.AddScenario(
                11,
                "flashback-disable-during-export-task",
                RunFlashbackDisableDuringExportAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendRawWithConnectRetryAsync,
                    cancellationToken));
            actions.Add("flashback disable during export started");
        }

        if (scenarioPlan.RunFlashbackRotatedExport)
        {
            backgroundTasks.AddScenario(
                12,
                "flashback-rotated-export-task",
                RunFlashbackRotatedExportAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback rotated export started");
        }
    }

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
        var selection = await PrepareFlashbackSelectionRangeAsync(
                outPointMs,
                scenarioLabel,
                actions,
                warnings,
                sendCommandAsync,
                cancellationToken)
            .ConfigureAwait(false);
        if (selection is null)
        {
            return;
        }

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
                selection.Value.BaselineSnapshot,
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

        ValidateFlashbackRangeExportResult(snapshot, outPointMs, scenarioLabel, warnings);

        await CleanupFlashbackSelectionAsync(sendCommandAsync).ConfigureAwait(false);
        actions.Add($"{scenarioLabel} cleared range and went live");

        await ValidateFlashbackRangeExportCleanupAsync(
                selection.Value.BaselineSnapshot,
                scenarioLabel,
                warnings,
                sendCommandAsync,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private readonly record struct FlashbackSelectionRange(
        JsonElement BaselineSnapshot,
        int RangeStartMs,
        int RangeEndMs);

    private static async Task<FlashbackSelectionRange?> PrepareFlashbackSelectionRangeAsync(
        int outPointMs,
        string scenarioLabel,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
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
            return null;
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
            return null;
        }

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "clear-in-out-points" }, null)
            .ConfigureAwait(false);
        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "pause" }, null)
            .ConfigureAwait(false);
        await MarkFlashbackSelectionPointAsync(
                rangeStartMs,
                "set-in-point",
                "in",
                scenarioLabel,
                actions,
                warnings,
                sendCommandAsync,
                cancellationToken)
            .ConfigureAwait(false);
        await MarkFlashbackSelectionPointAsync(
                rangeEndMs,
                "set-out-point",
                "out",
                scenarioLabel,
                actions,
                warnings,
                sendCommandAsync,
                cancellationToken)
            .ConfigureAwait(false);

        return new FlashbackSelectionRange(baselineSnapshot, rangeStartMs, rangeEndMs);
    }

    private static async Task MarkFlashbackSelectionPointAsync(
        int positionMs,
        string action,
        string label,
        string scenarioLabel,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "seek", ["positionMs"] = positionMs },
                null)
            .ConfigureAwait(false);
        if (!await WaitForFlashbackPlaybackPositionAsync(sendCommandAsync, positionMs, TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false))
        {
            warnings.Add($"{scenarioLabel}: playback did not reach {label}-point seek before marking range");
        }

        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = action }, null)
            .ConfigureAwait(false);
        actions.Add($"{scenarioLabel} {label} point set positionMs={positionMs}");
    }

    private static void ValidateFlashbackRangeExportResult(
        JsonElement snapshot,
        int outPointMs,
        string scenarioLabel,
        List<string> warnings)
    {
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
    }

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

    internal static async Task RunFlashbackDisableDuringExportAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback disable during export: Flashback buffer did not become export-ready within 30s");
            return;
        }

        var exportPath = Path.Combine(outputDirectory, "flashback-disable-during-export.mp4");
        var exportTask = sendCommandAsync(
            "FlashbackExport",
            new Dictionary<string, object?> { ["seconds"] = 3, ["outputPath"] = exportPath, ["force"] = true },
            AutomationPipeProtocol.GetDefaultResponseTimeout("FlashbackExport"));

        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        var disableTask = SendCommandWithConnectRetryAsync(
            sendCommandAsync,
            "SetFlashbackEnabled",
            new Dictionary<string, object?> { ["enabled"] = false },
            305_000,
            TimeSpan.FromSeconds(30),
            cancellationToken);
        actions.Add("flashback disable/export requests issued");

        var exportResponse = await exportTask.ConfigureAwait(false);
        var disableResponse = await disableTask.ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            warnings.Add(
                $"flashback disable during export: export failed - {AutomationSnapshotFormatter.Get(exportResponse, "Message", "unknown error")}");
        }

        if (disableResponse is null || !AutomationSnapshotFormatter.IsSuccess(disableResponse.Value))
        {
            var message = disableResponse is null
                ? "no response"
                : AutomationSnapshotFormatter.Get(disableResponse.Value, "Message", "unknown error");
            warnings.Add(
                $"flashback disable during export: disable failed - {message}");
        }

        if (AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            await ValidateFlashbackDisableDuringExportFileAsync(exportPath, warnings, sendCommandAsync)
                .ConfigureAwait(false);
        }

        if (disableResponse.HasValue && AutomationSnapshotFormatter.IsSuccess(disableResponse.Value))
        {
            await ValidateFlashbackDisabledAfterExportAsync(warnings, actions, sendCommandAsync, cancellationToken)
                .ConfigureAwait(false);
        }

        var enableResponse = await SendCommandWithConnectRetryAsync(
                sendCommandAsync,
                "SetFlashbackEnabled",
                new Dictionary<string, object?> { ["enabled"] = true },
                305_000,
                TimeSpan.FromSeconds(30),
                cancellationToken)
            .ConfigureAwait(false);
        actions.Add("flashback re-enabled after disable/export");
        if (enableResponse is null || !AutomationSnapshotFormatter.IsSuccess(enableResponse.Value))
        {
            var message = enableResponse is null
                ? "no response"
                : AutomationSnapshotFormatter.Get(enableResponse.Value, "Message", "unknown error");
            warnings.Add(
                $"flashback disable during export: re-enable failed - {message}");
            return;
        }

        await ValidateFlashbackReenabledAfterDisableDuringExportAsync(warnings, sendCommandAsync, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task ValidateFlashbackDisableDuringExportFileAsync(
        string exportPath,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync)
    {
        var verifyResponse = await sendCommandAsync(
                "VerifyFile",
                CreateFlashbackExportVerifyPayload(exportPath),
                60_000)
            .ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(verifyResponse))
        {
            warnings.Add(
                $"flashback disable during export verification: {AutomationSnapshotFormatter.Get(verifyResponse, "Message", "verification failed")}");
        }
    }

    private static async Task ValidateFlashbackDisabledAfterExportAsync(
        List<string> warnings,
        List<string> actions,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var inactiveSnapshot = await WaitForFlashbackActiveAsync(
                sendCommandAsync,
                expectedActive: false,
                timeout: TimeSpan.FromSeconds(20),
                cancellationToken)
            .ConfigureAwait(false);
        if (inactiveSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback disable during export: Flashback did not report inactive after disable");
            return;
        }

        if (GetBool(inactiveSnapshot.Value, "FlashbackPlaybackThreadAlive"))
        {
            warnings.Add("flashback disable during export: playback worker still alive after disable");
        }

        if (GetInt(inactiveSnapshot.Value, "FlashbackPlaybackPendingCommands") > 0)
        {
            warnings.Add(
                "flashback disable during export: pending playback commands remained after disable " +
                $"pending={GetInt(inactiveSnapshot.Value, "FlashbackPlaybackPendingCommands")}");
        }

        actions.Add("flashback disable during export verified");
    }

    private static async Task ValidateFlashbackReenabledAfterDisableDuringExportAsync(
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var activeSnapshot = await WaitForFlashbackActiveAsync(
                sendCommandAsync,
                expectedActive: true,
                timeout: TimeSpan.FromSeconds(30),
                cancellationToken)
            .ConfigureAwait(false);
        if (activeSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback disable during export: Flashback did not report active after re-enable");
        }
    }
}
