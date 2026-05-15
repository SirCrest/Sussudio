using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;

namespace Sussudio.Tools;

internal static class DiagnosticSessionFlashbackRejectedExports
{
    internal static async Task RunSelectedRejectedExportScenariosAsync(
        DiagnosticSessionScenarioPlan scenarioPlan,
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (scenarioPlan.RunFlashbackExportRejected)
        {
            await RunFlashbackExportRejectedAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendCommandAsync,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (scenarioPlan.RunFlashbackRecordingExportRejected)
        {
            await RunFlashbackRecordingExportRejectedAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendCommandAsync,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    internal static async Task RunFlashbackExportRejectedAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var exportPath = Path.Combine(outputDirectory, "flashback-rejected-export.mp4");
        var exportResponse = await sendCommandAsync(
                "FlashbackExport",
                new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = exportPath },
                60_000,
                true)
            .ConfigureAwait(false);
        actions.Add("flashback rejected export requested");

        if (AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            warnings.Add("flashback export rejected: export unexpectedly succeeded while Flashback was inactive");
        }

        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        var snapshotResponse = await sendCommandAsync("GetSnapshot", null, null, false).ConfigureAwait(false);
        if (!TryGetSnapshot(snapshotResponse, out var snapshot))
        {
            warnings.Add("flashback export rejected: no snapshot returned after rejected export");
            return;
        }

        var status = GetString(snapshot, "FlashbackExportStatus") ?? string.Empty;
        var message = GetString(snapshot, "FlashbackExportMessage") ?? string.Empty;
        var failureKind = GetString(snapshot, "FlashbackExportFailureKind") ?? string.Empty;
        var lastSuccess = GetString(snapshot, "LastExportSuccess") ?? string.Empty;
        actions.Add($"flashback rejected export observed status={status} kind={failureKind}");
        if (!string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback export rejected: expected Failed status, got {status}");
        }

        if (!string.Equals(failureKind, "BufferInactive", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback export rejected: expected BufferInactive failure kind, got {failureKind}");
        }

        if (!message.Contains("Flashback buffer not active", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback export rejected: unexpected message '{message}'");
        }

        if (!string.Equals(lastSuccess, "false", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback export rejected: expected LastExportSuccess=false, got {lastSuccess}");
        }
    }

    internal static async Task RunFlashbackRecordingExportRejectedAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var readySnapshot = await WaitForFlashbackRecordingReadyAsync(
                (command, payload, timeoutMs) => sendCommandAsync(command, payload, timeoutMs, false),
                TimeSpan.FromSeconds(20),
                cancellationToken)
            .ConfigureAwait(false);
        if (readySnapshot?.ValueKind != JsonValueKind.Object)
        {
            warnings.Add("flashback recording export rejected: Flashback recording backend did not become ready");
            return;
        }

        var exportPath = Path.Combine(outputDirectory, "flashback-recording-rejected-export.mp4");
        var exportResponse = await sendCommandAsync(
                "FlashbackExport",
                new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = exportPath },
                60_000,
                true)
            .ConfigureAwait(false);
        actions.Add("flashback recording rejected export requested");

        if (AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            warnings.Add("flashback recording export rejected: export unexpectedly succeeded while Flashback recording backend was active");
        }

        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        var snapshotResponse = await sendCommandAsync("GetSnapshot", null, null, false).ConfigureAwait(false);
        if (!TryGetSnapshot(snapshotResponse, out var snapshot))
        {
            warnings.Add("flashback recording export rejected: no snapshot returned after rejected export");
            return;
        }

        var status = GetString(snapshot, "FlashbackExportStatus") ?? string.Empty;
        var message = GetString(snapshot, "FlashbackExportMessage") ?? string.Empty;
        var failureKind = GetString(snapshot, "FlashbackExportFailureKind") ?? string.Empty;
        var lastSuccess = GetString(snapshot, "LastExportSuccess") ?? string.Empty;
        actions.Add($"flashback recording rejected export observed status={status} kind={failureKind}");
        if (!string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback recording export rejected: expected Failed status, got {status}");
        }

        if (!string.Equals(failureKind, "UnavailableDuringRecording", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback recording export rejected: expected UnavailableDuringRecording failure kind, got {failureKind}");
        }

        if (!message.Contains("Flashback export is unavailable while Flashback is the active recording backend", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback recording export rejected: unexpected message '{message}'");
        }

        if (!string.Equals(lastSuccess, "false", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback recording export rejected: expected LastExportSuccess=false, got {lastSuccess}");
        }

        if (!GetBool(snapshot, "IsRecording") ||
            !string.Equals(GetString(snapshot, "RecordingBackend"), "Flashback", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("flashback recording export rejected: recording backend changed after rejected export");
        }
    }
}
