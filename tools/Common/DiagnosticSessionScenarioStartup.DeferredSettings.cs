using System.Text.Json;
using static Sussudio.Tools.DiagnosticSessionFlashbackRecordingSettingsScenarios;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionScenarioStartup
{
    private static void RegisterDeferredFlashbackRecordingSettingsTask(
        DiagnosticSessionScenarioPlan scenarioPlan,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendAsyncWithFailurePolicy,
        CancellationToken cancellationToken)
    {
        if (!scenarioPlan.RunFlashbackRecordingSettingsDeferred)
        {
            return;
        }

        backgroundTasks.SetRecordingSettingsDeferred(RunFlashbackRecordingSettingsDeferredAsync(
            actions,
            warnings,
            sendAsyncWithFailurePolicy,
            cancellationToken));
        actions.Add("flashback recording settings deferred started");
    }
}
