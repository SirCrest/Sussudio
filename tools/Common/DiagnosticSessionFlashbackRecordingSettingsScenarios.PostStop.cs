using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackRecordingSettingsScenarios
{
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
