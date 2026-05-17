using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackStressScenario
{
    internal const int FlashbackStressMaxPlaybackPendingCommands = 4;
    internal const int FlashbackStressMaxPlaybackCommandLatencyMs = 750;
    internal const double FlashbackStressPlaybackWarmSeconds = 10.0;
    internal const long FlashbackStressAudioUnavailableFallbackAllowance = 4;
    internal const int FlashbackScrubStressMaxPlaybackPendingCommands = 20;

    internal static void RegisterSelectedFlashbackStressScenarioTasks(
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
    }
}
