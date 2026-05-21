using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionScenarioSetup
{
    internal static async Task<DiagnosticSessionScenarioSetupResult> RunAsync(
        string scenario,
        DiagnosticSessionScenarioPlan scenarioPlan,
        JsonElement initialSnapshot,
        List<string> actions,
        List<string> warnings,
        DiagnosticSessionCommandChannel commandChannel,
        Func<string, int, Task> tryWaitAsync,
        CancellationToken cancellationToken)
    {
        var flashbackSetup = await SetupFlashbackStateAsync(
                scenario,
                scenarioPlan,
                initialSnapshot,
                actions,
                commandChannel)
            .ConfigureAwait(false);
        var startedPreview = await StartPreviewIfNeededAsync(
                scenario,
                initialSnapshot,
                actions,
                commandChannel,
                tryWaitAsync)
            .ConfigureAwait(false);
        var startedRecording = await StartRecordingIfNeededAsync(
                scenario,
                scenarioPlan,
                initialSnapshot,
                actions,
                warnings,
                commandChannel,
                tryWaitAsync,
                cancellationToken)
            .ConfigureAwait(false);

        return new DiagnosticSessionScenarioSetupResult(
            startedPreview,
            startedRecording,
            flashbackSetup.EnabledFlashback,
            flashbackSetup.DisabledFlashback);
    }
}
