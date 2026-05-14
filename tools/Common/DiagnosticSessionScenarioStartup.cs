using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionScenarioStartup
{
    internal static async Task<DiagnosticSessionScenarioStartupResult> StartAsync(
        DiagnosticSessionOptions options,
        DiagnosticSessionScenarioPlan scenarioPlan,
        int durationSeconds,
        string outputDirectory,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendRawWithConnectRetryAsync,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendAsyncWithFailurePolicy,
        CancellationToken cancellationToken)
    {
        await DiagnosticSessionPresentMonStartup.StartAsync(
                options,
                durationSeconds,
                outputDirectory,
                backgroundTasks,
                actions,
                sendAsync)
            .ConfigureAwait(false);

        RegisterFlashbackScenarioTasks(
            scenarioPlan,
            outputDirectory,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            sendRawWithConnectRetryAsync,
            cancellationToken);

        RegisterDeferredFlashbackRecordingSettingsTask(
            scenarioPlan,
            backgroundTasks,
            actions,
            warnings,
            sendAsyncWithFailurePolicy,
            cancellationToken);

        var startedFlashbackPlayback = await TryStartFlashbackPlaybackAsync(
                scenarioPlan,
                actions,
                warnings,
                sendAsync,
                cancellationToken)
            .ConfigureAwait(false);

        return new DiagnosticSessionScenarioStartupResult(startedFlashbackPlayback);
    }
}

internal readonly record struct DiagnosticSessionScenarioStartupResult(bool StartedFlashbackPlayback);
