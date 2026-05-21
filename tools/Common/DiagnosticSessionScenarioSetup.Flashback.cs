using System.Text.Json;
using Sussudio.Models;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionScenarioSetup
{
    private static async Task<DiagnosticSessionFlashbackSetupResult> SetupFlashbackStateAsync(
        string scenario,
        DiagnosticSessionScenarioPlan scenarioPlan,
        JsonElement initialSnapshot,
        List<string> actions,
        DiagnosticSessionCommandChannel commandChannel)
    {
        var enabledFlashback = false;
        var disabledFlashback = false;

        if (DiagnosticSessionScenarioCatalog.NeedsFlashback(scenario) && !GetBool(initialSnapshot, "FlashbackActive"))
        {
            await commandChannel.SendAsync(
                    AutomationCommandKind.SetFlashbackEnabled,
                    new Dictionary<string, object?> { ["enabled"] = true },
                    null)
                .ConfigureAwait(false);
            enabledFlashback = true;
            actions.Add("flashback enabled");
        }

        if (scenarioPlan.RunFlashbackExportRejected && GetBool(initialSnapshot, "FlashbackActive"))
        {
            await commandChannel.SendAsync(
                    AutomationCommandKind.SetFlashbackEnabled,
                    new Dictionary<string, object?> { ["enabled"] = false },
                    null)
                .ConfigureAwait(false);
            disabledFlashback = true;
            actions.Add("flashback disabled for rejected export");
        }

        return new DiagnosticSessionFlashbackSetupResult(enabledFlashback, disabledFlashback);
    }
}
