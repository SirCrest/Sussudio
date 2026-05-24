using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackExports;
using static Sussudio.Tools.DiagnosticSessionFlashbackSegments;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;
using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackExportScenarios
{
    internal static async Task RunFlashbackExportConcurrentAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback concurrent export: Flashback buffer did not become export-ready within 30s");
            return;
        }

        var exportPathA = Path.Combine(outputDirectory, "flashback-concurrent-a.mp4");
        var exportPathB = Path.Combine(outputDirectory, "flashback-concurrent-b.mp4");
        // Diagnostic runs may execute against the same output directory across sessions;
        // pass force=true so the destination-exists guard does not break the diagnostic.
        var exportPayloadA = new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = exportPathA, ["force"] = true };
        var exportPayloadB = new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = exportPathB, ["force"] = true };

        var exportTimeoutMs = AutomationPipeProtocol.GetDefaultResponseTimeout("FlashbackExport");
        var exportTaskA = sendCommandAsync("FlashbackExport", exportPayloadA, exportTimeoutMs);
        var exportTaskB = sendCommandAsync("FlashbackExport", exportPayloadB, exportTimeoutMs);
        actions.Add("flashback concurrent export requests issued");

        var exportResponses = await Task.WhenAll(exportTaskA, exportTaskB).ConfigureAwait(false);
        for (var i = 0; i < exportResponses.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = exportResponses[i];
            var path = i == 0 ? exportPathA : exportPathB;
            var label = i == 0 ? "a" : "b";
            if (!AutomationSnapshotFormatter.IsSuccess(response))
            {
                warnings.Add(
                    $"flashback concurrent export {label}: {AutomationSnapshotFormatter.Get(response, "Message", "export failed")}");
                continue;
            }

            var verifyResponse = await sendCommandAsync(
                    "VerifyFile",
                    CreateFlashbackExportVerifyPayload(path),
                    60_000)
                .ConfigureAwait(false);
            if (!AutomationSnapshotFormatter.IsSuccess(verifyResponse))
            {
                warnings.Add(
                    $"flashback concurrent export {label} verification: {AutomationSnapshotFormatter.Get(verifyResponse, "Message", "verification failed")}");
            }
        }

        actions.Add("flashback concurrent exports verified");
    }

    internal static async Task RunFlashbackRotatedExportAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback rotated export: Flashback buffer did not become export-ready within 30s");
            return;
        }

        var completedSegment = await WaitForFlashbackCompletedSegmentAsync(
                sendCommandAsync,
                TimeSpan.FromSeconds(210),
                cancellationToken)
            .ConfigureAwait(false);
        if (completedSegment is null)
        {
            warnings.Add("flashback rotated export: no completed segment observed within 210s");
            return;
        }

        actions.Add(
            "flashback rotated segment observed " +
            $"seq={completedSegment.Value.SequenceNumber} " +
            $"startMs={completedSegment.Value.StartPtsMs} " +
            $"endMs={completedSegment.Value.EndPtsMs}");

        var exportPath = Path.Combine(outputDirectory, "flashback-rotated-export.mp4");
        var exportResponse = await sendCommandAsync(
                "FlashbackExport",
                new Dictionary<string, object?> { ["seconds"] = 180, ["outputPath"] = exportPath },
                300_000)
            .ConfigureAwait(false);
        actions.Add("flashback rotated export requested");

        var exportMessage = AutomationSnapshotFormatter.Get(exportResponse, "Message", string.Empty);
        if (!AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            warnings.Add(
                $"flashback rotated export: export failed - {AutomationSnapshotFormatter.Get(exportResponse, "Message", "unknown error")}");
            return;
        }

        var exportedSegments = TryParseFlashbackExportSegmentCount(exportMessage);
        if (exportedSegments is null or < 2)
        {
            warnings.Add($"flashback rotated export: expected multi-segment export, got '{exportMessage}'");
        }

        var verifyResponse = await sendCommandAsync(
                "VerifyFile",
                CreateFlashbackExportVerifyPayload(exportPath),
                120_000)
            .ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(verifyResponse))
        {
            warnings.Add(
                $"flashback rotated export verification: {AutomationSnapshotFormatter.Get(verifyResponse, "Message", "verification failed")}");
            return;
        }

        actions.Add("flashback rotated export verified");
    }

    internal static void RegisterSelectedFlashbackExportScenarioTasks(
        DiagnosticSessionScenarioPlan scenarioPlan,
        string outputDirectory,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendRawWithConnectRetryAsync,
        CancellationToken cancellationToken)
    {
        RegisterFlashbackExportPlaybackTask(
            scenarioPlan,
            outputDirectory,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            cancellationToken);

        RegisterFlashbackRangeExportTasks(
            scenarioPlan,
            outputDirectory,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            sendRawWithConnectRetryAsync,
            cancellationToken);

        RegisterFlashbackExportCoordinationTasks(
            scenarioPlan,
            outputDirectory,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            sendRawWithConnectRetryAsync,
            cancellationToken);
    }

    private static void RegisterFlashbackExportPlaybackTask(
        DiagnosticSessionScenarioPlan scenarioPlan,
        string outputDirectory,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        CancellationToken cancellationToken)
    {
        if (!scenarioPlan.RunFlashbackExportPlayback)
        {
            return;
        }

        backgroundTasks.AddScenario(
            6,
            "flashback-export-playback-task",
            RunFlashbackExportPlaybackAsync(
                outputDirectory,
                actions,
                warnings,
                sendAsync,
                cancellationToken));
        actions.Add("flashback export playback started");
    }

    private static void RegisterFlashbackRangeExportTasks(
        DiagnosticSessionScenarioPlan scenarioPlan,
        string outputDirectory,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendRawWithConnectRetryAsync,
        CancellationToken cancellationToken)
    {
        if (scenarioPlan.RunFlashbackRangeExport)
        {
            backgroundTasks.AddScenario(
                8,
                "flashback-range-export-task",
                RunFlashbackRangeExportAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback range export started");
        }

        if (scenarioPlan.RunFlashbackRangeExportAudioSwitch)
        {
            backgroundTasks.AddScenario(
                9,
                "flashback-range-export-audio-switch-task",
                RunFlashbackRangeExportAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendRawWithConnectRetryAsync,
                    cancellationToken,
                    scenarioLabel: "flashback range export audio switch",
                    exportFileName: "flashback-range-export-audio-switch.mp4",
                    outPointMs: 15_000,
                    switchAudioDuringExport: true));
            actions.Add("flashback range export audio switch started");
        }
    }

    private static void RegisterFlashbackExportCoordinationTasks(
        DiagnosticSessionScenarioPlan scenarioPlan,
        string outputDirectory,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendRawWithConnectRetryAsync,
        CancellationToken cancellationToken)
    {
        if (scenarioPlan.RunFlashbackExportConcurrent)
        {
            backgroundTasks.AddScenario(
                10,
                "flashback-export-concurrent-task",
                RunFlashbackExportConcurrentAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendRawWithConnectRetryAsync,
                    cancellationToken));
            actions.Add("flashback concurrent export started");
        }

        if (scenarioPlan.RunFlashbackDisableDuringExport)
        {
            backgroundTasks.AddScenario(
                11,
                "flashback-disable-during-export-task",
                RunFlashbackDisableDuringExportAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendRawWithConnectRetryAsync,
                    cancellationToken));
            actions.Add("flashback disable during export started");
        }

        if (scenarioPlan.RunFlashbackRotatedExport)
        {
            backgroundTasks.AddScenario(
                12,
                "flashback-rotated-export-task",
                RunFlashbackRotatedExportAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback rotated export started");
        }
    }
}
