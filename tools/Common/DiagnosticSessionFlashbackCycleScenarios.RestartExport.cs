using System.Text.Json;
using static Sussudio.Tools.DiagnosticSessionFlashbackExports;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackCycleScenarios
{
    private static async Task VerifyFlashbackRestartCycleExportAsync(
        string outputDirectory,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync)
    {
        var exportPath = Path.Combine(outputDirectory, "flashback-restart-cycle-export.mp4");
        var exportResponse = await sendCommandAsync(
                "FlashbackExport",
                new Dictionary<string, object?> { ["seconds"] = 1, ["outputPath"] = exportPath },
                60_000)
            .ConfigureAwait(false);
        actions.Add("flashback restart cycle export requested");
        if (!AutomationSnapshotFormatter.IsSuccess(exportResponse))
        {
            warnings.Add($"flashback restart cycle: export failed - {AutomationSnapshotFormatter.Get(exportResponse, "Message", "unknown error")}");
            return;
        }

        var verifyResponse = await sendCommandAsync(
                "VerifyFile",
                CreateFlashbackExportVerifyPayload(exportPath),
                60_000)
            .ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(verifyResponse))
        {
            warnings.Add(
                $"flashback restart cycle export verification: {AutomationSnapshotFormatter.Get(verifyResponse, "Message", "verification failed")}");
            return;
        }

        actions.Add("flashback restart cycle export verified");
    }
}
