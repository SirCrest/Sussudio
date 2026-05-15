using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackPreviewCycleScenarios
{
    internal static void RegisterSelectedFlashbackPreviewCycleScenarioTasks(
        DiagnosticSessionScenarioPlan scenarioPlan,
        string outputDirectory,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        CancellationToken cancellationToken)
    {
        if (scenarioPlan.RunFlashbackPreviewCycle)
        {
            backgroundTasks.AddScenario(
                13,
                "flashback-preview-cycle-task",
                RunFlashbackPreviewCycleAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback preview cycle started");
        }

        if (scenarioPlan.RunFlashbackPlaybackPreviewCycle)
        {
            backgroundTasks.AddScenario(
                14,
                "flashback-playback-preview-cycle-task",
                RunFlashbackPlaybackPreviewCycleAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback playback preview cycle started");
        }

        if (scenarioPlan.RunFlashbackRecordingPreviewCycle)
        {
            backgroundTasks.AddScenario(
                15,
                "flashback-recording-preview-cycle-task",
                RunFlashbackRecordingPreviewCycleAsync(
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback recording preview cycle started");
        }
    }
}
