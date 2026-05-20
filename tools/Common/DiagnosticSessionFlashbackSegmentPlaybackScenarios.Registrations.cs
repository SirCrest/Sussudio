using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackSegmentPlaybackScenarios
{
    internal static void RegisterSelectedFlashbackSegmentPlaybackScenarioTask(
        DiagnosticSessionScenarioPlan scenarioPlan,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (!scenarioPlan.RunFlashbackSegmentPlayback)
        {
            return;
        }

        backgroundTasks.AddScenario(
            7,
            "flashback-segment-playback-task",
            RunFlashbackSegmentPlaybackAsync(
                actions,
                warnings,
                sendCommandAsync,
                cancellationToken));
        actions.Add("flashback segment playback started");
    }
}
