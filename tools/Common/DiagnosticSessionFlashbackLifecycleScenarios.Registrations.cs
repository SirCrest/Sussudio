using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackLifecycleScenarios
{
    internal static void RegisterSelectedFlashbackLifecycleScenarioTask(
        DiagnosticSessionScenarioPlan scenarioPlan,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!scenarioPlan.RunFlashbackLifecycle)
        {
            return;
        }

        backgroundTasks.AddScenario(
            2,
            "flashback-lifecycle-task",
            RunFlashbackLifecycleAsync(
                actions,
                warnings,
                sendCommandAsync,
                cancellationToken));
        actions.Add("flashback lifecycle started");
    }
}
