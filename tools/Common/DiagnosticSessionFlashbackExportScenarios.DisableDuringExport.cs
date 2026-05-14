using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackExports;
using static Sussudio.Tools.DiagnosticSessionFlashbackSegments;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;
using static Sussudio.Tools.DiagnosticSessionMetrics;
using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackExportScenarios
{
    internal static async Task RunFlashbackDisableDuringExportAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback disable during export: Flashback buffer did not become export-ready within 30s");
            return;
        }

        var exportPath = Path.Combine(outputDirectory, "flashback-disable-during-export.mp4");
        var exportTask = sendCommandAsync(
            "FlashbackExport",
            new Dictionary<string, object?> { ["seconds"] = 3, ["outputPath"] = exportPath, ["force"] = true },
            AutomationPipeProtocol.GetDefaultResponseTimeout("FlashbackExport"));

        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        var disableTask = SendCommandWithConnectRetryAsync(
            sendCommandAsync,
            "SetFlashbackEnabled",
            new Dictionary<string, object?> { ["enabled"] = false },
            305_000,
            TimeSpan.FromSeconds(30),
            cancellationToken);
        actions.Add("flashback disable/export requests issued");

        var exportResponse = await exportTask.ConfigureAwait(false);
        var disableResponse = await disableTask.ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            warnings.Add(
                $"flashback disable during export: export failed - {AutomationSnapshotFormatter.Get(exportResponse, "Message", "unknown error")}");
        }

        if (disableResponse is null || !AutomationSnapshotFormatter.IsSuccess(disableResponse.Value))
        {
            var message = disableResponse is null
                ? "no response"
                : AutomationSnapshotFormatter.Get(disableResponse.Value, "Message", "unknown error");
            warnings.Add(
                $"flashback disable during export: disable failed - {message}");
        }

        if (AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            var verifyResponse = await sendCommandAsync(
                    "VerifyFile",
                    CreateFlashbackExportVerifyPayload(exportPath),
                    60_000)
                .ConfigureAwait(false);
            if (!AutomationSnapshotFormatter.IsSuccess(verifyResponse))
            {
                warnings.Add(
                    $"flashback disable during export verification: {AutomationSnapshotFormatter.Get(verifyResponse, "Message", "verification failed")}");
            }
        }

        if (disableResponse.HasValue && AutomationSnapshotFormatter.IsSuccess(disableResponse.Value))
        {
            var inactiveSnapshot = await WaitForFlashbackActiveAsync(
                    sendCommandAsync,
                    expectedActive: false,
                    timeout: TimeSpan.FromSeconds(20),
                    cancellationToken)
                .ConfigureAwait(false);
            if (inactiveSnapshot?.ValueKind != JsonValueKind.Object)
            {
                warnings.Add("flashback disable during export: Flashback did not report inactive after disable");
            }
            else
            {
                if (GetBool(inactiveSnapshot.Value, "FlashbackPlaybackThreadAlive"))
                {
                    warnings.Add("flashback disable during export: playback worker still alive after disable");
                }

                if (GetInt(inactiveSnapshot.Value, "FlashbackPlaybackPendingCommands") > 0)
                {
                    warnings.Add(
                        "flashback disable during export: pending playback commands remained after disable " +
                        $"pending={GetInt(inactiveSnapshot.Value, "FlashbackPlaybackPendingCommands")}");
                }

                actions.Add("flashback disable during export verified");
            }
        }

        var enableResponse = await SendCommandWithConnectRetryAsync(
                sendCommandAsync,
                "SetFlashbackEnabled",
                new Dictionary<string, object?> { ["enabled"] = true },
                305_000,
                TimeSpan.FromSeconds(30),
                cancellationToken)
            .ConfigureAwait(false);
        actions.Add("flashback re-enabled after disable/export");
        if (enableResponse is null || !AutomationSnapshotFormatter.IsSuccess(enableResponse.Value))
        {
            var message = enableResponse is null
                ? "no response"
                : AutomationSnapshotFormatter.Get(enableResponse.Value, "Message", "unknown error");
            warnings.Add(
                $"flashback disable during export: re-enable failed - {message}");
            return;
        }

        var activeSnapshot = await WaitForFlashbackActiveAsync(
                sendCommandAsync,
                expectedActive: true,
                timeout: TimeSpan.FromSeconds(30),
                cancellationToken)
            .ConfigureAwait(false);
        if (activeSnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback disable during export: Flashback did not report active after re-enable");
        }
    }
}
