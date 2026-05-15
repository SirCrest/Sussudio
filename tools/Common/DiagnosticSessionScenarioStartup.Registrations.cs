using System.Text.Json;
using static Sussudio.Tools.DiagnosticSessionFlashbackCycleScenarios;
using static Sussudio.Tools.DiagnosticSessionFlashbackLifecycleScenarios;
using static Sussudio.Tools.DiagnosticSessionFlashbackPreviewCycleScenarios;
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

        if (scenarioPlan.RunFlashbackLifecycle)
        {
            backgroundTasks.AddScenario(
                2,
                "flashback-lifecycle-task",
                RunFlashbackLifecycleAsync(
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback lifecycle started");
        }

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
