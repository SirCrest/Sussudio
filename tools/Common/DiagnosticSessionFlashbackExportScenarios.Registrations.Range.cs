using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackExportScenarios
{
    private static void RegisterFlashbackRangeExportTasks(
        DiagnosticSessionScenarioPlan scenarioPlan,
        string outputDirectory,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendRawWithConnectRetryAsync,
        CancellationToken cancellationToken)
    {
        if (scenarioPlan.RunFlashbackRangeExport)
        {
            backgroundTasks.AddScenario(
                8,
                "flashback-range-export-task",
                RunFlashbackRangeExportAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback range export started");
        }

        if (scenarioPlan.RunFlashbackRangeExportAudioSwitch)
        {
            backgroundTasks.AddScenario(
                9,
                "flashback-range-export-audio-switch-task",
                RunFlashbackRangeExportAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendRawWithConnectRetryAsync,
                    cancellationToken,
                    scenarioLabel: "flashback range export audio switch",
                    exportFileName: "flashback-range-export-audio-switch.mp4",
                    outPointMs: 15_000,
                    switchAudioDuringExport: true));
            actions.Add("flashback range export audio switch started");
        }
    }
}
