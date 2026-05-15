using System.Text.Json;
using static Sussudio.Tools.DiagnosticSessionFlashbackSegmentPlaybackScenarios;
using static Sussudio.Tools.DiagnosticSessionFlashbackStressScenario;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionScenarioStartup
{
    private static void RegisterFlashbackScenarioTasks(
        DiagnosticSessionScenarioPlan scenarioPlan,
        string outputDirectory,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendRawWithConnectRetryAsync,
        CancellationToken cancellationToken)
    {
        if (scenarioPlan.RunFlashbackStress)
        {
            backgroundTasks.AddScenario(
                1,
                "flashback-stress-task",
                RunFlashbackStressAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback stress started");
        }

        if (scenarioPlan.RunFlashbackScrubStress)
        {
            backgroundTasks.AddScenario(
                3,
                "flashback-scrub-stress-task",
                RunFlashbackScrubStressAsync(
                    actions,
                    warnings,
                    sendRawWithConnectRetryAsync,
                    cancellationToken));
            actions.Add("flashback scrub stress started");
        }

        DiagnosticSessionFlashbackCycleScenarios.RegisterSelectedFlashbackCycleScenarioTasks(
            scenarioPlan,
            outputDirectory,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            cancellationToken);

        if (scenarioPlan.RunFlashbackSegmentPlayback)
        {
            backgroundTasks.AddScenario(
                7,
                "flashback-segment-playback-task",
                RunFlashbackSegmentPlaybackAsync(
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback segment playback started");
        }

        DiagnosticSessionFlashbackExportScenarios.RegisterSelectedFlashbackExportScenarioTasks(
            scenarioPlan,
            outputDirectory,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            sendRawWithConnectRetryAsync,
            cancellationToken);

        DiagnosticSessionFlashbackLifecycleScenarios.RegisterSelectedFlashbackLifecycleScenarioTask(
            scenarioPlan,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            cancellationToken);

        DiagnosticSessionFlashbackPreviewCycleScenarios.RegisterSelectedFlashbackPreviewCycleScenarioTasks(
            scenarioPlan,
            outputDirectory,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            cancellationToken);
    }
}
