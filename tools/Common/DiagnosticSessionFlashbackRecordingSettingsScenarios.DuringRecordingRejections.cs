using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackRecordingSettingsScenarios
{
    private static async Task VerifyFlashbackRestartRejectedDuringRecordingAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendCommandAsync)
    {
        await VerifyFlashbackRecordingSettingsCommandRejectedDuringRecordingAsync(
                actions,
                warnings,
                "RestartFlashback",
                null,
                null,
                "flashback recording settings deferred restart rejection requested",
                "flashback recording settings deferred: RestartFlashback unexpectedly succeeded during recording",
                "flashback recording settings deferred: restart rejection message did not mention recording",
                sendCommandAsync)
            .ConfigureAwait(false);
    }

    private static async Task VerifyFlashbackDisableRejectedDuringRecordingAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendCommandAsync)
    {
        await VerifyFlashbackRecordingSettingsCommandRejectedDuringRecordingAsync(
                actions,
                warnings,
                "SetFlashbackEnabled",
                new Dictionary<string, object?> { ["enabled"] = false },
                305_000,
                "flashback recording settings deferred disable rejection requested",
                "flashback recording settings deferred: SetFlashbackEnabled(false) unexpectedly succeeded during recording",
                "flashback recording settings deferred: disable rejection message did not mention recording",
                sendCommandAsync)
            .ConfigureAwait(false);
    }

    private static async Task VerifyFlashbackRecordingSettingsCommandRejectedDuringRecordingAsync(
        List<string> actions,
        List<string> warnings,
        string commandName,
        Dictionary<string, object?>? payload,
        int? timeoutMs,
        string requestedAction,
        string unexpectedSuccessWarning,
        string messageWarningPrefix,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendCommandAsync)
    {
        var response = await sendCommandAsync(
                commandName,
                payload,
                timeoutMs,
                true)
            .ConfigureAwait(false);
        actions.Add(requestedAction);

        if (AutomationSnapshotFormatter.IsSuccess(response))
        {
            warnings.Add(unexpectedSuccessWarning);
            return;
        }

        var message = AutomationSnapshotFormatter.Get(response, "Message", string.Empty);
        if (!message.Contains("recording", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"{messageWarningPrefix} - {message}");
        }
    }
}
