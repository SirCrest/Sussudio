using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackPreviewCycleScenarios
{
    internal static async Task RunFlashbackRecordingPreviewCycleAsync(
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var countersBeforeStop = await CaptureRecordingPreviewCycleCountersBeforeStopAsync(
                warnings,
                sendCommandAsync,
                cancellationToken)
            .ConfigureAwait(false);
        if (countersBeforeStop is null)
        {
            return;
        }

        var stopPreviewResponse = await sendCommandAsync(
                "SetPreviewEnabled",
                new Dictionary<string, object?> { ["enabled"] = false },
                60_000)
            .ConfigureAwait(false);
        actions.Add("flashback recording preview cycle preview stopped");
        if (!AutomationSnapshotFormatter.IsSuccess(stopPreviewResponse))
        {
            warnings.Add(
                $"flashback recording preview cycle: preview stop failed - {AutomationSnapshotFormatter.Get(stopPreviewResponse, "Message", "unknown error")}");
            return;
        }

        if (!await ValidateRecordingPreviewCycleStoppedAsync(
                    countersBeforeStop.Value,
                    warnings,
                    sendCommandAsync,
                    cancellationToken)
                .ConfigureAwait(false))
        {
            return;
        }

        var startPreviewResponse = await sendCommandAsync(
                "SetPreviewEnabled",
                new Dictionary<string, object?> { ["enabled"] = true },
                60_000)
            .ConfigureAwait(false);
        actions.Add("flashback recording preview cycle preview restarted");
        if (!AutomationSnapshotFormatter.IsSuccess(startPreviewResponse))
        {
            warnings.Add(
                $"flashback recording preview cycle: preview restart failed - {AutomationSnapshotFormatter.Get(startPreviewResponse, "Message", "unknown error")}");
            return;
        }

        await ValidateRecordingPreviewCycleRestartedAsync(warnings, sendCommandAsync, cancellationToken)
            .ConfigureAwait(false);
    }
}
