using System.Text.Json;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackSegmentPlaybackScenarios
{
    private static async Task<bool> CreateFlashbackCompletedSegmentViaRecordingAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var startResponse = await sendCommandAsync(
                "SetRecordingEnabled",
                new Dictionary<string, object?> { ["enabled"] = true },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback segment playback recording-assisted rotation started");
        if (!AutomationSnapshotFormatter.IsSuccess(startResponse))
        {
            warnings.Add(
                $"flashback segment playback: recording-assisted start failed - {AutomationSnapshotFormatter.Get(startResponse, "Message", "unknown error")}");
            return false;
        }

        var readySnapshot = await WaitForFlashbackRecordingReadyAsync(
                sendCommandAsync,
                TimeSpan.FromSeconds(20),
                cancellationToken)
            .ConfigureAwait(false);
        if (readySnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback segment playback: recording-assisted Flashback backend did not become ready");
            await TryStopRecordingAsync(sendCommandAsync).ConfigureAwait(false);
            return false;
        }

        await Task.Delay(2_000, cancellationToken).ConfigureAwait(false);

        var stopResponse = await sendCommandAsync(
                "SetRecordingEnabled",
                new Dictionary<string, object?> { ["enabled"] = false },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback segment playback recording-assisted rotation stopped");
        if (!AutomationSnapshotFormatter.IsSuccess(stopResponse))
        {
            warnings.Add(
                $"flashback segment playback: recording-assisted stop failed - {AutomationSnapshotFormatter.Get(stopResponse, "Message", "unknown error")}");
            return false;
        }

        var stoppedResponse = await sendCommandAsync(
                "WaitForCondition",
                new Dictionary<string, object?>
                {
                    ["condition"] = "RecordingStopped",
                    ["timeoutMs"] = 30_000,
                    ["pollMs"] = 250
                },
                32_000)
            .ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(stoppedResponse))
        {
            warnings.Add(
                $"flashback segment playback: recording-assisted stop did not settle - {AutomationSnapshotFormatter.Get(stoppedResponse, "Message", "not met")}");
            return false;
        }

        return true;
    }

    private static async Task TryStopRecordingAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync)
    {
        try
        {
            await sendCommandAsync(
                    "SetRecordingEnabled",
                    new Dictionary<string, object?> { ["enabled"] = false },
                    null)
                .ConfigureAwait(false);
        }
        catch
        {
            // Best-effort cleanup for diagnostics; the caller records the primary warning.
        }
    }
}
