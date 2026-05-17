using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;

namespace Sussudio.Tools;

internal readonly record struct FlashbackRecordingSettingsDeferredPresetState(
    string? OriginalPreset,
    string? DeferredPreset);

internal static partial class DiagnosticSessionFlashbackRecordingSettingsScenarios
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

        var restartResponse = await sendCommandAsync(
                "RestartFlashback",
                null,
                null,
                true)
            .ConfigureAwait(false);
        actions.Add("flashback recording settings deferred restart rejection requested");
        if (AutomationSnapshotFormatter.IsSuccess(restartResponse))
        {
            warnings.Add("flashback recording settings deferred: RestartFlashback unexpectedly succeeded during recording");
        }
        else
        {
            var message = AutomationSnapshotFormatter.Get(restartResponse, "Message", string.Empty);
            if (!message.Contains("recording", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"flashback recording settings deferred: restart rejection message did not mention recording - {message}");
            }
        }

        var disableResponse = await sendCommandAsync(
                "SetFlashbackEnabled",
                new Dictionary<string, object?> { ["enabled"] = false },
                305_000,
                true)
            .ConfigureAwait(false);
        actions.Add("flashback recording settings deferred disable rejection requested");
        if (AutomationSnapshotFormatter.IsSuccess(disableResponse))
        {
            warnings.Add("flashback recording settings deferred: SetFlashbackEnabled(false) unexpectedly succeeded during recording");
        }
        else
        {
            var message = AutomationSnapshotFormatter.Get(disableResponse, "Message", string.Empty);
            if (!message.Contains("recording", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"flashback recording settings deferred: disable rejection message did not mention recording - {message}");
            }
        }

        await Task.Delay(2_000, cancellationToken).ConfigureAwait(false);
        var afterResponse = await sendCommandAsync("GetSnapshot", null, null, false).ConfigureAwait(false);
        if (!TryGetSnapshot(afterResponse, out var afterSnapshot))
        {
            warnings.Add("flashback recording settings deferred: no post-mutation recording snapshot returned");
            return presetState;
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

        return presetState;
    }
}
