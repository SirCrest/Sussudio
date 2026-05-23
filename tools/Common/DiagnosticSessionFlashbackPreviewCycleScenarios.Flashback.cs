using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;
using static Sussudio.Tools.DiagnosticSessionFlashbackExports;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;

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

        var encodedBeforeStop = await CaptureFlashbackPreviewCycleEncodedFramesBeforeStopAsync(sendCommandAsync)
            .ConfigureAwait(false);

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

        if (!await ValidateFlashbackPreviewCycleStoppedAsync(
                    encodedBeforeStop,
                    warnings,
                    sendCommandAsync,
                    cancellationToken)
                .ConfigureAwait(false))
        {
            return;
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

        await ValidateFlashbackPreviewCycleRestartedAsync(warnings, sendCommandAsync, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<long> CaptureFlashbackPreviewCycleEncodedFramesBeforeStopAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync)
    {
        var beforeStopResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        TryGetSnapshot(beforeStopResponse, out var beforeStopSnapshot);
        return GetNullableLong(beforeStopSnapshot, "FlashbackEncodedFrames") ?? 0;
    }

    private static async Task<bool> ValidateFlashbackPreviewCycleStoppedAsync(
        long encodedBeforeStop,
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
            warnings.Add("flashback preview cycle: preview did not report stopped");
            return false;
        }

        if (!GetBool(previewStoppedSnapshot.Value, "FlashbackActive"))
        {
            warnings.Add("flashback preview cycle: Flashback became inactive when preview stopped");
            return false;
        }

        await Task.Delay(2_000, cancellationToken).ConfigureAwait(false);
        var previewOffSnapshotResponse = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
        if (!TryGetSnapshot(previewOffSnapshotResponse, out var previewOffSnapshot))
        {
            warnings.Add("flashback preview cycle: no preview-off snapshot returned");
            return false;
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

        return true;
    }

    private static async Task VerifyFlashbackPreviewCycleExportAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync)
    {
        var exportPath = Path.Combine(outputDirectory, "flashback-preview-off-export.mp4");
        var exportResponse = await sendCommandAsync(
                "FlashbackExport",
                new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = exportPath },
                60_000)
            .ConfigureAwait(false);
        actions.Add("flashback preview cycle export while preview off requested");
        if (!AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            warnings.Add(
                $"flashback preview cycle: export while preview off failed - {AutomationSnapshotFormatter.Get(exportResponse, "Message", "unknown error")}");
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
                $"flashback preview cycle export verification: {AutomationSnapshotFormatter.Get(verifyResponse, "Message", "verification failed")}");
        }
        else
        {
            actions.Add("flashback preview cycle export verified");
        }
    }

    private static async Task ValidateFlashbackPreviewCycleRestartedAsync(
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
