using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackCycleScenarios
{
    internal static void RegisterSelectedFlashbackCycleScenarioTasks(
        DiagnosticSessionScenarioPlan scenarioPlan,
        string outputDirectory,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        CancellationToken cancellationToken)
    {
        if (scenarioPlan.RunFlashbackRestartCycle)
        {
            backgroundTasks.AddScenario(
                4,
                "flashback-restart-cycle-task",
                RunFlashbackRestartCycleAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback restart cycle started");
        }

        if (scenarioPlan.RunFlashbackEncoderCycle)
        {
            backgroundTasks.AddScenario(
                5,
                "flashback-encoder-cycle-task",
                RunFlashbackEncoderCycleAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback encoder cycle started");
        }
    }
}
