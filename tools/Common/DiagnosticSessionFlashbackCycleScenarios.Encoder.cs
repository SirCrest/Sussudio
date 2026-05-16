using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackExports;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackCycleScenarios
{
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
        finally
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
}
