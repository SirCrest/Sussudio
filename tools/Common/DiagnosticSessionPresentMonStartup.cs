using System.Text.Json;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;

namespace Sussudio.Tools;

internal static class DiagnosticSessionPresentMonStartup
{
    internal static async Task StartAsync(
        DiagnosticSessionOptions options,
        int durationSeconds,
        string outputDirectory,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync)
    {
        if (!options.IncludePresentMon)
        {
            return;
        }

        var correlationSnapshotResponse = await sendAsync("GetSnapshot", null, null).ConfigureAwait(false);
        TryGetSnapshot(correlationSnapshotResponse, out var correlationSnapshot);
        backgroundTasks.SetPresentMon(PresentMonProbe.RunAsync(PresentMonProbe.CreateOptions(
            durationSeconds: Math.Max(1, durationSeconds),
            processName: "Sussudio",
            presentMonPath: options.PresentMonPath,
            outputFile: Path.Combine(outputDirectory, "presentmon.csv"),
            keepCsv: true,
            correlation: PresentMonProbe.ReadPreviewCorrelation(correlationSnapshot))));
        actions.Add("presentmon capture started");
    }
}
