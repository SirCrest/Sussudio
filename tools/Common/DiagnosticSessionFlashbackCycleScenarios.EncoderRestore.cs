using System.Text.Json;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackCycleScenarios
{
    private static async Task RestoreFlashbackEncoderCyclePresetAsync(
        List<string> actions,
        List<string> warnings,
        string originalPreset,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var restoreResponse = await sendCommandAsync(
                "SetPreset",
                new Dictionary<string, object?> { ["preset"] = originalPreset },
                null)
            .ConfigureAwait(false);
        actions.Add($"flashback encoder preset restored to {originalPreset}");
        if (!AutomationSnapshotFormatter.IsSuccess(restoreResponse))
        {
            warnings.Add($"flashback encoder cycle: preset restore failed - {AutomationSnapshotFormatter.Get(restoreResponse, "Message", "unknown error")}");
        }
        else if (!await WaitForFlashbackStressBufferReadyAsync(sendCommandAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback encoder cycle: Flashback buffer did not become ready after preset restore");
        }
    }
}
