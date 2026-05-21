using System.Text.Json;
using Sussudio.Models;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionScenarioSetup
{
    private static async Task<bool> StartRecordingIfNeededAsync(
        string scenario,
        DiagnosticSessionScenarioPlan scenarioPlan,
        JsonElement initialSnapshot,
        List<string> actions,
        List<string> warnings,
        DiagnosticSessionCommandChannel commandChannel,
        Func<string, int, Task> tryWaitAsync,
        CancellationToken cancellationToken)
    {
        if (!DiagnosticSessionScenarioCatalog.NeedsRecording(scenario) || GetBool(initialSnapshot, "IsRecording"))
        {
            return false;
        }

        Task<JsonElement> SendByNameAsync(string command, Dictionary<string, object?>? payload, int? timeoutMs)
            => commandChannel.SendAsync(command, payload, timeoutMs);

        if (scenarioPlan.RequiresFlashbackRecordingReadiness &&
            !await WaitForFlashbackStressBufferReadyAsync(SendByNameAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback recording: Flashback buffer did not become recording-ready within 30s");
        }

        await commandChannel.SendAsync(
                AutomationCommandKind.SetRecordingEnabled,
                new Dictionary<string, object?> { ["enabled"] = true },
                null)
            .ConfigureAwait(false);
        actions.Add("recording started");
        await tryWaitAsync("RecordingFileGrowing", 20_000).ConfigureAwait(false);

        return true;
    }
}
