using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;
using static Sussudio.Tools.DiagnosticSessionFlashbackExports;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;

namespace Sussudio.Tools;

internal static class DiagnosticSessionFlashbackCycleScenarios
{
    internal static void RegisterSelectedFlashbackCycleScenarioTasks(
        DiagnosticSessionScenarioPlan scenarioPlan,
        string outputDirectory,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        CancellationToken cancellationToken)
    {
        if (scenarioPlan.RunFlashbackRestartCycle)
        {
            backgroundTasks.AddScenario(
                4,
                "flashback-restart-cycle-task",
                RunFlashbackRestartCycleAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback restart cycle started");
        }

        if (scenarioPlan.RunFlashbackEncoderCycle)
        {
            backgroundTasks.AddScenario(
                5,
                "flashback-encoder-cycle-task",
                RunFlashbackEncoderCycleAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback encoder cycle started");
        }
    }

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

    internal static async Task RunFlashbackEncoderCycleAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback encoder cycle: Flashback buffer did not become ready before preset change");
            return;
        }

        var beforeResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        if (!TryGetSnapshot(beforeResponse, out var beforeSnapshot))
        {
            warnings.Add("flashback encoder cycle: no initial snapshot returned");
            return;
        }

        var originalPreset = GetString(beforeSnapshot, "SelectedPreset") ?? "P1";
        var cycledPreset = string.Equals(originalPreset, "P1", StringComparison.OrdinalIgnoreCase) ? "P2" : "P1";
        var originalFilePath = GetString(beforeSnapshot, "FlashbackFilePath") ?? string.Empty;

        try
        {
            var setResponse = await sendCommandAsync(
                    "SetPreset",
                    new Dictionary<string, object?> { ["preset"] = cycledPreset },
                    null)
                .ConfigureAwait(false);
            actions.Add($"flashback encoder preset changed to {cycledPreset}");
            if (!AutomationSnapshotFormatter.IsSuccess(setResponse))
            {
                warnings.Add($"flashback encoder cycle: preset change failed - {AutomationSnapshotFormatter.Get(setResponse, "Message", "unknown error")}");
                return;
            }

            if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
            {
                warnings.Add("flashback encoder cycle: Flashback buffer did not become ready after preset change");
                return;
            }

            var afterResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (!TryGetSnapshot(afterResponse, out var afterSnapshot))
            {
                warnings.Add("flashback encoder cycle: no post-cycle snapshot returned");
                return;
            }

            ValidateFlashbackEncoderCycleSnapshot(afterSnapshot, originalFilePath, warnings);

            await VerifyFlashbackEncoderCycleExportAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendCommandAsync)
                .ConfigureAwait(false);
        }
        finally
        {
            await RestoreFlashbackEncoderCyclePresetAsync(
                    actions,
                    warnings,
                    originalPreset,
                    sendCommandAsync,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static async Task<bool> ValidateFlashbackRestartCycleActiveStateAsync(
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
            warnings.Add("flashback restart cycle: Flashback did not report active after restart");
            return false;
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

        return true;
    }

    private static async Task VerifyFlashbackRestartCycleExportAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync)
    {
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

    private static void ValidateFlashbackEncoderCycleSnapshot(
        JsonElement afterSnapshot,
        string originalFilePath,
        List<string> warnings)
    {
        var framesAfter = GetNullableLong(afterSnapshot, "FlashbackEncodedFrames") ?? 0;
        if (framesAfter < 240)
        {
            warnings.Add($"flashback encoder cycle: post-cycle encoder did not reach readiness frame count frames={framesAfter}");
        }

        var afterFilePath = GetString(afterSnapshot, "FlashbackFilePath") ?? string.Empty;
        if (string.Equals(afterFilePath, originalFilePath, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("flashback encoder cycle: Flashback file path did not change after preset cycle");
        }

        if (GetInt(afterSnapshot, "FlashbackPlaybackPendingCommands") > 0 ||
            GetBool(afterSnapshot, "FlashbackPlaybackThreadAlive"))
        {
            warnings.Add(
                "flashback encoder cycle: playback state not clean after preset cycle " +
                $"pending={GetInt(afterSnapshot, "FlashbackPlaybackPendingCommands")} " +
                $"threadAlive={GetBool(afterSnapshot, "FlashbackPlaybackThreadAlive")}");
        }
    }

    private static async Task VerifyFlashbackEncoderCycleExportAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync)
    {
        var exportPath = Path.Combine(outputDirectory, "flashback-encoder-cycle-export.mp4");
        var exportResponse = await sendCommandAsync(
                "FlashbackExport",
                new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = exportPath },
                60_000)
            .ConfigureAwait(false);
        actions.Add("flashback encoder cycle export requested");
        if (!AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            warnings.Add($"flashback encoder cycle: export failed - {AutomationSnapshotFormatter.Get(exportResponse, "Message", "unknown error")}");
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
                $"flashback encoder cycle export verification: {AutomationSnapshotFormatter.Get(verifyResponse, "Message", "verification failed")}");
            return;
        }

        actions.Add("flashback encoder cycle export verified");
    }

    private static async Task RestoreFlashbackEncoderCyclePresetAsync(
        List<string> actions,
        List<string> warnings,
        string originalPreset,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var restoreResponse = await sendCommandAsync(
                "SetPreset",
                new Dictionary<string, object?> { ["preset"] = originalPreset },
                null)
            .ConfigureAwait(false);
        actions.Add($"flashback encoder preset restored to {originalPreset}");
        if (!AutomationSnapshotFormatter.IsSuccess(restoreResponse))
        {
            warnings.Add($"flashback encoder cycle: preset restore failed - {AutomationSnapshotFormatter.Get(restoreResponse, "Message", "unknown error")}");
        }
        else if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback encoder cycle: Flashback buffer did not become ready after preset restore");
        }
    }
}

internal static class DiagnosticSessionFlashbackLifecycleScenarios
{
    internal static void RegisterSelectedFlashbackLifecycleScenarioTask(
        DiagnosticSessionScenarioPlan scenarioPlan,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!scenarioPlan.RunFlashbackLifecycle)
        {
            return;
        }

        backgroundTasks.AddScenario(
            2,
            "flashback-lifecycle-task",
            RunFlashbackLifecycleAsync(
                actions,
                warnings,
                sendCommandAsync,
                cancellationToken));
        actions.Add("flashback lifecycle started");
    }

    internal static async Task RunFlashbackLifecycleAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback lifecycle: Flashback buffer did not become playback-ready within 30s");
            return;
        }

        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "pause" },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback lifecycle pause requested");

        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "seek", ["positionMs"] = 1_000 },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback lifecycle seek requested");

        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "play" },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback lifecycle play requested");

        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        await sendCommandAsync(
                "SetFlashbackEnabled",
                new Dictionary<string, object?> { ["enabled"] = false },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback lifecycle disabled during playback");

        await ValidateFlashbackLifecycleDisabledAsync(
                warnings,
                sendCommandAsync,
                cancellationToken)
            .ConfigureAwait(false);

        await sendCommandAsync(
                "SetFlashbackEnabled",
                new Dictionary<string, object?> { ["enabled"] = true },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback lifecycle re-enabled");

        await ValidateFlashbackLifecycleReenabledAsync(
                warnings,
                sendCommandAsync,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task ValidateFlashbackLifecycleDisabledAsync(
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var disabledSnapshot = await WaitForFlashbackActiveAsync(
                sendCommandAsync,
                expectedActive: false,
                timeout: TimeSpan.FromSeconds(15),
                cancellationToken)
            .ConfigureAwait(false);
        if (disabledSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback lifecycle: Flashback did not report inactive after disable");
            return;
        }

        if (GetBool(disabledSnapshot.Value, "FlashbackPlaybackThreadAlive"))
        {
            warnings.Add("flashback lifecycle: playback worker still alive after disable");
        }

        if (GetInt(disabledSnapshot.Value, "FlashbackPlaybackPendingCommands") > 0)
        {
            warnings.Add(
                "flashback lifecycle: pending commands remained after disable " +
                $"pending={GetInt(disabledSnapshot.Value, "FlashbackPlaybackPendingCommands")}");
        }
    }

    private static async Task ValidateFlashbackLifecycleReenabledAsync(
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var enabledSnapshot = await WaitForFlashbackActiveAsync(
                sendCommandAsync,
                expectedActive: true,
                timeout: TimeSpan.FromSeconds(30),
                cancellationToken)
            .ConfigureAwait(false);
        if (enabledSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback lifecycle: Flashback did not report active after re-enable");
        }
    }
}
