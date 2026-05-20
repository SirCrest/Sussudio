using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackExportScenarios
{
    private static void RegisterFlashbackExportPlaybackTask(
        DiagnosticSessionScenarioPlan scenarioPlan,
        string outputDirectory,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        CancellationToken cancellationToken)
    {
        if (!scenarioPlan.RunFlashbackExportPlayback)
        {
            return;
        }

        backgroundTasks.AddScenario(
            6,
            "flashback-export-playback-task",
            RunFlashbackExportPlaybackAsync(
                outputDirectory,
                actions,
                warnings,
                sendAsync,
                cancellationToken));
        actions.Add("flashback export playback started");
    }
}
