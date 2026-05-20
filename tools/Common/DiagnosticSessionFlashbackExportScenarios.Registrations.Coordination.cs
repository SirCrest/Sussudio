using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackExportScenarios
{
    private static void RegisterFlashbackExportCoordinationTasks(
        DiagnosticSessionScenarioPlan scenarioPlan,
        string outputDirectory,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendRawWithConnectRetryAsync,
        CancellationToken cancellationToken)
    {
        if (scenarioPlan.RunFlashbackExportConcurrent)
        {
            backgroundTasks.AddScenario(
                10,
                "flashback-export-concurrent-task",
                RunFlashbackExportConcurrentAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendRawWithConnectRetryAsync,
                    cancellationToken));
            actions.Add("flashback concurrent export started");
        }

        if (scenarioPlan.RunFlashbackDisableDuringExport)
        {
            backgroundTasks.AddScenario(
                11,
                "flashback-disable-during-export-task",
                RunFlashbackDisableDuringExportAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendRawWithConnectRetryAsync,
                    cancellationToken));
            actions.Add("flashback disable during export started");
        }

        if (scenarioPlan.RunFlashbackRotatedExport)
        {
            backgroundTasks.AddScenario(
                12,
                "flashback-rotated-export-task",
                RunFlashbackRotatedExportAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback rotated export started");
        }
    }
}
