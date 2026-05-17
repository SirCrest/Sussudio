using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackRejectedExports
{
    internal static async Task RunSelectedRejectedExportScenariosAsync(
        DiagnosticSessionScenarioPlan scenarioPlan,
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        if (scenarioPlan.RunFlashbackExportRejected)
        {
            await RunFlashbackExportRejectedAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendCommandAsync,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (scenarioPlan.RunFlashbackRecordingExportRejected)
        {
            await RunFlashbackRecordingExportRejectedAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendCommandAsync,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
