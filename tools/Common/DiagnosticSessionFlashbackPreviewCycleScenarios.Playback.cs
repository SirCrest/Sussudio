using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackExports;
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

    private static async Task<long> CapturePlaybackPreviewCycleFrameCountBeforeStopAsync(
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var playingSnapshot = await WaitForFlashbackPlaybackStateAsync(
                sendCommandAsync,
                "Playing",
                TimeSpan.FromSeconds(5),
                cancellationToken)
            .ConfigureAwait(false);
        if (playingSnapshot?.ValueKind != JsonValueKind.Object ||
            !string.Equals(GetString(playingSnapshot.Value, "FlashbackPlaybackState"), "Playing", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("flashback playback preview cycle: playback did not report Playing before preview stop");
            return 0;
        }

        var playbackFrameCountBeforeStop = GetNullableLong(playingSnapshot.Value, "FlashbackPlaybackFrameCount") ?? 0;
        if (playbackFrameCountBeforeStop > 0)
        {
            return playbackFrameCountBeforeStop;
        }

        var warmSnapshot = await WaitForFlashbackPlaybackWarmSampleAsync(
                sendCommandAsync,
                playbackFrameCountBeforeStop,
                0.25,
                TimeSpan.FromSeconds(5),
                cancellationToken)
            .ConfigureAwait(false);
        return warmSnapshot?.ValueKind == JsonValueKind.Object
            ? GetNullableLong(warmSnapshot.Value, "FlashbackPlaybackFrameCount") ?? 0
            : 0;
    }

    private static async Task<bool> ValidatePlaybackPreviewCycleStoppedAsync(
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var previewStoppedSnapshot = await WaitForPreviewActiveAsync(
                sendCommandAsync,
                expectedActive: false,
                timeout: TimeSpan.FromSeconds(15),
                cancellationToken)
            .ConfigureAwait(false);
        if (previewStoppedSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback playback preview cycle: preview did not report stopped");
            return false;
        }

        if (!GetBool(previewStoppedSnapshot.Value, "FlashbackActive"))
        {
            warnings.Add("flashback playback preview cycle: Flashback became inactive when preview stopped");
            return false;
        }

        var playbackStateAfterStop = GetString(previewStoppedSnapshot.Value, "FlashbackPlaybackState") ?? "Unknown";
        if (!string.Equals(playbackStateAfterStop, "Live", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback playback preview cycle: playback did not return live after preview stop state={playbackStateAfterStop}");
        }

        return true;
    }

    private static async Task VerifyFlashbackPlaybackPreviewCycleExportAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync)
    {
        var exportPath = Path.Combine(outputDirectory, "flashback-playback-preview-cycle.mp4");
        var exportResponse = await sendCommandAsync(
                "FlashbackExport",
                new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = exportPath },
                60_000)
            .ConfigureAwait(false);
        actions.Add("flashback playback preview cycle export while preview off requested");
        if (!AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            warnings.Add(
                $"flashback playback preview cycle: export while preview off failed - {AutomationSnapshotFormatter.Get(exportResponse, "Message", "unknown error")}");
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
                $"flashback playback preview cycle export verification: {AutomationSnapshotFormatter.Get(verifyResponse, "Message", "verification failed")}");
        }
        else
        {
            actions.Add("flashback playback preview cycle export verified");
        }
    }

    private static async Task ValidatePlaybackPreviewCycleRestartedAsync(
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var previewStartedSnapshot = await WaitForPreviewActiveAsync(
                sendCommandAsync,
                expectedActive: true,
                timeout: TimeSpan.FromSeconds(15),
                cancellationToken)
            .ConfigureAwait(false);
        if (previewStartedSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback playback preview cycle: preview did not report active after restart");
            return;
        }

        var framesFlowingResponse = await sendCommandAsync(
                "WaitForCondition",
                new Dictionary<string, object?>
                {
                    ["condition"] = "VideoFramesFlowing",
                    ["timeoutMs"] = 15_000,
                    ["pollMs"] = 250
                },
                17_000)
            .ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(framesFlowingResponse))
        {
            warnings.Add(
                $"flashback playback preview cycle: preview frames did not resume - {AutomationSnapshotFormatter.Get(framesFlowingResponse, "Message", "not met")}");
        }
    }
}
