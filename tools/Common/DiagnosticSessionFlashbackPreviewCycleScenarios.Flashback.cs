using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackPreviewCycleScenarios
{
    internal static async Task RunFlashbackPreviewCycleAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback preview cycle: Flashback buffer did not become ready within 30s");
            return;
        }

        var beforeStopResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        TryGetSnapshot(beforeStopResponse, out var beforeStopSnapshot);
        var encodedBeforeStop = GetNullableLong(beforeStopSnapshot, "FlashbackEncodedFrames") ?? 0;

        var stopPreviewResponse = await sendCommandAsync(
                "SetPreviewEnabled",
                new Dictionary<string, object?> { ["enabled"] = false },
                60_000)
            .ConfigureAwait(false);
        actions.Add("flashback preview cycle preview stopped");
        if (!AutomationSnapshotFormatter.IsSuccess(stopPreviewResponse))
        {
            warnings.Add(
                $"flashback preview cycle: preview stop failed - {AutomationSnapshotFormatter.Get(stopPreviewResponse, "Message", "unknown error")}");
            return;
        }

        var previewStoppedSnapshot = await WaitForPreviewActiveAsync(
                sendCommandAsync,
                expectedActive: false,
                timeout: TimeSpan.FromSeconds(15),
                cancellationToken)
            .ConfigureAwait(false);
        if (previewStoppedSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback preview cycle: preview did not report stopped");
            return;
        }

        if (!GetBool(previewStoppedSnapshot.Value, "FlashbackActive"))
        {
            warnings.Add("flashback preview cycle: Flashback became inactive when preview stopped");
            return;
        }

        await Task.Delay(2_000, cancellationToken).ConfigureAwait(false);
        var previewOffSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        if (!TryGetSnapshot(previewOffSnapshotResponse, out var previewOffSnapshot))
        {
            warnings.Add("flashback preview cycle: no preview-off snapshot returned");
            return;
        }

        var encodedPreviewOff = GetNullableLong(previewOffSnapshot, "FlashbackEncodedFrames") ?? 0;
        if (!GetBool(previewOffSnapshot, "FlashbackActive"))
        {
            warnings.Add("flashback preview cycle: Flashback inactive while preview was off");
        }

        if (encodedPreviewOff <= encodedBeforeStop)
        {
            warnings.Add(
                "flashback preview cycle: Flashback frames did not advance while preview was off " +
                $"before={encodedBeforeStop} after={encodedPreviewOff}");
        }

        await VerifyFlashbackPreviewCycleExportAsync(
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
        actions.Add("flashback preview cycle preview restarted");
        if (!AutomationSnapshotFormatter.IsSuccess(startPreviewResponse))
        {
            warnings.Add(
                $"flashback preview cycle: preview restart failed - {AutomationSnapshotFormatter.Get(startPreviewResponse, "Message", "unknown error")}");
            return;
        }

        var previewStartedSnapshot = await WaitForPreviewActiveAsync(
                sendCommandAsync,
                expectedActive: true,
                timeout: TimeSpan.FromSeconds(15),
                cancellationToken)
            .ConfigureAwait(false);
        if (previewStartedSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback preview cycle: preview did not report active after restart");
            return;
        }

        if (!GetBool(previewStartedSnapshot.Value, "FlashbackActive"))
        {
            warnings.Add("flashback preview cycle: Flashback inactive after preview restart");
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
                $"flashback preview cycle: preview frames did not resume - {AutomationSnapshotFormatter.Get(framesFlowingResponse, "Message", "not met")}");
        }
    }
}
