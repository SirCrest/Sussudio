using System.Text.Json;
using Sussudio.Models;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionScenarioSetup
{
    private static async Task<bool> StartPreviewIfNeededAsync(
        string scenario,
        JsonElement initialSnapshot,
        List<string> actions,
        DiagnosticSessionCommandChannel commandChannel,
        Func<string, int, Task> tryWaitAsync)
    {
        if (!DiagnosticSessionScenarioCatalog.NeedsPreview(scenario) || GetBool(initialSnapshot, "IsPreviewing"))
        {
            return false;
        }

        await commandChannel.SendAsync(
                AutomationCommandKind.SetPreviewEnabled,
                new Dictionary<string, object?> { ["enabled"] = true },
                null)
            .ConfigureAwait(false);
        actions.Add("preview started");
        await tryWaitAsync("VideoFramesFlowing", 15_000).ConfigureAwait(false);

        return true;
    }
}
