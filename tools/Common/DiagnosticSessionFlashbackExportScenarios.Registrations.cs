using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackExportScenarios
{
    internal static void RegisterSelectedFlashbackExportScenarioTasks(
        DiagnosticSessionScenarioPlan scenarioPlan,
        string outputDirectory,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendRawWithConnectRetryAsync,
        CancellationToken cancellationToken)
    {
        RegisterFlashbackExportPlaybackTask(
            scenarioPlan,
            outputDirectory,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            cancellationToken);

        RegisterFlashbackRangeExportTasks(
            scenarioPlan,
            outputDirectory,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            sendRawWithConnectRetryAsync,
            cancellationToken);

        RegisterFlashbackExportCoordinationTasks(
            scenarioPlan,
            outputDirectory,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            sendRawWithConnectRetryAsync,
            cancellationToken);
    }

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
