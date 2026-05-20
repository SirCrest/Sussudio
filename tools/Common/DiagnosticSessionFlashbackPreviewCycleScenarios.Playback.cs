using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackPreviewCycleScenarios
{
    internal static async Task RunFlashbackPlaybackPreviewCycleAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback playback preview cycle: Flashback buffer did not become playback-ready within 30s");
            return;
        }

        var playResponse = await sendCommandAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "play", ["positionMs"] = 1000 },
                null)
            .ConfigureAwait(false);
        actions.Add("flashback playback preview cycle playback started");
        if (!AutomationSnapshotFormatter.IsSuccess(playResponse))
        {
            warnings.Add(
                $"flashback playback preview cycle: play command failed - {AutomationSnapshotFormatter.Get(playResponse, "Message", "unknown error")}");
            return;
        }

        var playbackFrameCountBeforeStop = await CapturePlaybackPreviewCycleFrameCountBeforeStopAsync(
                warnings,
                sendCommandAsync,
                cancellationToken)
            .ConfigureAwait(false);

        if (playbackFrameCountBeforeStop <= 0)
        {
            warnings.Add("flashback playback preview cycle: playback did not render frames before preview stop");
            return;
        }

        var stopPreviewResponse = await sendCommandAsync(
                "SetPreviewEnabled",
                new Dictionary<string, object?> { ["enabled"] = false },
                60_000)
            .ConfigureAwait(false);
        actions.Add("flashback playback preview cycle preview stopped during playback");
        if (!AutomationSnapshotFormatter.IsSuccess(stopPreviewResponse))
        {
            warnings.Add(
                $"flashback playback preview cycle: preview stop failed - {AutomationSnapshotFormatter.Get(stopPreviewResponse, "Message", "unknown error")}");
            return;
        }

        if (!await ValidatePlaybackPreviewCycleStoppedAsync(warnings, sendCommandAsync, cancellationToken)
                .ConfigureAwait(false))
        {
            return;
        }

        await VerifyFlashbackPlaybackPreviewCycleExportAsync(
                outputDirectory,
                actions,
                warnings,
                sendCommandAsync)
            .ConfigureAwait(false);

        var startPreviewResponse = await sendCommandAsync(
                "SetPreviewEnabled",
                new Dictionary<string, object?> { ["enabled"] = true },
                60_000)
            .ConfigureAwait(false);
        actions.Add("flashback playback preview cycle preview restarted");
        if (!AutomationSnapshotFormatter.IsSuccess(startPreviewResponse))
        {
            warnings.Add(
                $"flashback playback preview cycle: preview restart failed - {AutomationSnapshotFormatter.Get(startPreviewResponse, "Message", "unknown error")}");
            return;
        }

        await ValidatePlaybackPreviewCycleRestartedAsync(warnings, sendCommandAsync, cancellationToken)
            .ConfigureAwait(false);
    }
}
