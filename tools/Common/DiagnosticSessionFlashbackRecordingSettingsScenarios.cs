using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;

namespace Sussudio.Tools;

internal readonly record struct FlashbackRecordingSettingsDeferredPresetState(
    string? OriginalPreset,
    string? DeferredPreset);

internal static class DiagnosticSessionFlashbackRecordingSettingsScenarios
{
    internal static async Task<FlashbackRecordingSettingsDeferredPresetState> RunFlashbackRecordingSettingsDeferredAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var recordingReadySnapshot = await WaitForFlashbackRecordingReadyAsync(
                (command, payload, timeoutMs) => sendCommandAsync(command, payload, timeoutMs, false),
                TimeSpan.FromSeconds(20),
                cancellationToken)
            .ConfigureAwait(false);
        if (recordingReadySnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback recording settings deferred: Flashback recording backend did not become ready");
            return default;
        }

        var originalPreset = GetString(recordingReadySnapshot.Value, "SelectedPreset") ?? "P1";
        var cycledPreset = string.Equals(originalPreset, "P1", StringComparison.OrdinalIgnoreCase) ? "P2" : "P1";
        var presetState = new FlashbackRecordingSettingsDeferredPresetState(originalPreset, cycledPreset);
        var originalFilePath = GetString(recordingReadySnapshot.Value, "FlashbackFilePath") ?? string.Empty;
        var submittedBefore = GetNullableLong(recordingReadySnapshot.Value, "FlashbackVideoFramesSubmittedToEncoder") ?? 0;
        var packetsBefore = GetNullableLong(recordingReadySnapshot.Value, "FlashbackVideoEncoderPacketsWritten") ?? 0;

        var presetResponse = await sendCommandAsync(
                "SetPreset",
                new Dictionary<string, object?> { ["preset"] = cycledPreset },
                null,
                false)
            .ConfigureAwait(false);
        actions.Add($"flashback recording settings deferred preset changed to {cycledPreset}");
        if (!AutomationSnapshotFormatter.IsSuccess(presetResponse))
        {
            warnings.Add(
                $"flashback recording settings deferred: preset change failed - {AutomationSnapshotFormatter.Get(presetResponse, "Message", "unknown error")}");
            return presetState;
        }

        await VerifyFlashbackRestartRejectedDuringRecordingAsync(
                actions,
                warnings,
                sendCommandAsync)
            .ConfigureAwait(false);

        await VerifyFlashbackDisableRejectedDuringRecordingAsync(
                actions,
                warnings,
                sendCommandAsync)
            .ConfigureAwait(false);

        await VerifyFlashbackRecordingSettingsDeferredStillRecordingAsync(
                warnings,
                originalFilePath,
                submittedBefore,
                packetsBefore,
                sendCommandAsync,
                cancellationToken)
            .ConfigureAwait(false);

        return presetState;
    }

    internal static async Task VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(
        List<string> actions,
        List<string> warnings,
        FlashbackRecordingSettingsDeferredPresetState presetState,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(presetState.DeferredPreset))
        {
            warnings.Add("flashback recording settings deferred: no expected preset was captured for post-stop verification");
            return;
        }

        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback recording settings deferred: Flashback buffer did not become ready after recording stop");
            return;
        }

        var snapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        if (!TryGetSnapshot(snapshotResponse, out var snapshot))
        {
            warnings.Add("flashback recording settings deferred: no post-stop snapshot returned");
            return;
        }

        if (!string.Equals(GetString(snapshot, "SelectedPreset"), presetState.DeferredPreset, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(
                "flashback recording settings deferred: selected preset was not preserved after stop " +
                $"expected={presetState.DeferredPreset} actual={GetString(snapshot, "SelectedPreset") ?? "<null>"}");
        }

        if (!GetBool(snapshot, "FlashbackActive"))
        {
            warnings.Add("flashback recording settings deferred: Flashback inactive after recording stop");
            return;
        }

        if (GetNullableLong(snapshot, "FlashbackEncodedFrames") is not > 0)
        {
            warnings.Add("flashback recording settings deferred: post-stop Flashback encoder did not produce frames");
        }

        actions.Add("flashback recording settings deferred post-stop buffer verified");

        await RestoreFlashbackRecordingSettingsOriginalPresetAsync(
                actions,
                warnings,
                presetState,
                sendCommandAsync,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task VerifyFlashbackRestartRejectedDuringRecordingAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendCommandAsync)
    {
        await VerifyFlashbackRecordingSettingsCommandRejectedDuringRecordingAsync(
                actions,
                warnings,
                "RestartFlashback",
                null,
                null,
                "flashback recording settings deferred restart rejection requested",
                "flashback recording settings deferred: RestartFlashback unexpectedly succeeded during recording",
                "flashback recording settings deferred: restart rejection message did not mention recording",
                sendCommandAsync)
            .ConfigureAwait(false);
    }

    private static async Task VerifyFlashbackDisableRejectedDuringRecordingAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendCommandAsync)
    {
        await VerifyFlashbackRecordingSettingsCommandRejectedDuringRecordingAsync(
                actions,
                warnings,
                "SetFlashbackEnabled",
                new Dictionary<string, object?> { ["enabled"] = false },
                305_000,
                "flashback recording settings deferred disable rejection requested",
                "flashback recording settings deferred: SetFlashbackEnabled(false) unexpectedly succeeded during recording",
                "flashback recording settings deferred: disable rejection message did not mention recording",
                sendCommandAsync)
            .ConfigureAwait(false);
    }

    private static async Task VerifyFlashbackRecordingSettingsCommandRejectedDuringRecordingAsync(
        List<string> actions,
        List<string> warnings,
        string commandName,
        Dictionary<string, object?>? payload,
        int? timeoutMs,
        string requestedAction,
        string unexpectedSuccessWarning,
        string messageWarningPrefix,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendCommandAsync)
    {
        var response = await sendCommandAsync(
                commandName,
                payload,
                timeoutMs,
                true)
            .ConfigureAwait(false);
        actions.Add(requestedAction);

        if (AutomationSnapshotFormatter.IsSuccess(response))
        {
            warnings.Add(unexpectedSuccessWarning);
            return;
        }

        var message = AutomationSnapshotFormatter.Get(response, "Message", string.Empty);
        if (!message.Contains("recording", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"{messageWarningPrefix} - {message}");
        }
    }

    private static async Task VerifyFlashbackRecordingSettingsDeferredStillRecordingAsync(
        List<string> warnings,
        string originalFilePath,
        long submittedBefore,
        long packetsBefore,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        await Task.Delay(2_000, cancellationToken).ConfigureAwait(false);
        var afterResponse = await sendCommandAsync("GetSnapshot", null, null, false).ConfigureAwait(false);
        if (!TryGetSnapshot(afterResponse, out var afterSnapshot))
        {
            warnings.Add("flashback recording settings deferred: no post-mutation recording snapshot returned");
            return;
        }

        if (!GetBool(afterSnapshot, "IsRecording") ||
            !string.Equals(GetString(afterSnapshot, "RecordingBackend"), "Flashback", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("flashback recording settings deferred: Flashback recording backend did not remain active after mutations");
        }

        var afterFilePath = GetString(afterSnapshot, "FlashbackFilePath") ?? string.Empty;
        if (!string.Equals(afterFilePath, originalFilePath, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("flashback recording settings deferred: Flashback file path changed during recording settings deferral");
        }

        var submittedAfter = GetNullableLong(afterSnapshot, "FlashbackVideoFramesSubmittedToEncoder") ?? 0;
        var packetsAfter = GetNullableLong(afterSnapshot, "FlashbackVideoEncoderPacketsWritten") ?? 0;
        if (submittedAfter <= submittedBefore || packetsAfter <= packetsBefore)
        {
            warnings.Add(
                "flashback recording settings deferred: recording counters did not advance after mutation attempts " +
                $"submitted={submittedBefore}->{submittedAfter} packets={packetsBefore}->{packetsAfter}");
        }
    }

    private static async Task RestoreFlashbackRecordingSettingsOriginalPresetAsync(
        List<string> actions,
        List<string> warnings,
        FlashbackRecordingSettingsDeferredPresetState presetState,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(presetState.OriginalPreset) ||
            string.Equals(presetState.OriginalPreset, presetState.DeferredPreset, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var restoreResponse = await sendCommandAsync(
                "SetPreset",
                new Dictionary<string, object?> { ["preset"] = presetState.OriginalPreset },
                null)
            .ConfigureAwait(false);
        actions.Add($"flashback recording settings deferred preset restored to {presetState.OriginalPreset}");
        if (!AutomationSnapshotFormatter.IsSuccess(restoreResponse))
        {
            warnings.Add(
                $"flashback recording settings deferred: preset restore failed - {AutomationSnapshotFormatter.Get(restoreResponse, "Message", "unknown error")}");
            return;
        }

        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback recording settings deferred: Flashback buffer did not become ready after preset restore");
            return;
        }

        var restoredSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        if (!TryGetSnapshot(restoredSnapshotResponse, out var restoredSnapshot))
        {
            warnings.Add("flashback recording settings deferred: no post-restore snapshot returned");
            return;
        }

        if (!string.Equals(GetString(restoredSnapshot, "SelectedPreset"), presetState.OriginalPreset, StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(
                "flashback recording settings deferred: selected preset was not restored " +
                $"expected={presetState.OriginalPreset} actual={GetString(restoredSnapshot, "SelectedPreset") ?? "<null>"}");
        }
    }
}
