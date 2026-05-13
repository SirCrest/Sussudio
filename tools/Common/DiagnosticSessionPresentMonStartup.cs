using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
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
        backgroundTasks.SetPresentMon(PresentMonProbe.RunAsync(new PresentMonProbeOptions
        {
            ProcessName = "Sussudio",
            DurationSeconds = Math.Max(1, durationSeconds),
            PresentMonPath = options.PresentMonPath,
            OutputFile = Path.Combine(outputDirectory, "presentmon.csv"),
            ExpectedSwapChainAddress = GetString(correlationSnapshot, "PreviewD3DSwapChainAddress"),
            AppPresentId = GetNullableLong(correlationSnapshot, "PreviewD3DLastRenderedPreviewPresentId"),
            AppSourceSequenceNumber = GetNullableLong(correlationSnapshot, "PreviewD3DLastRenderedSourceSequenceNumber"),
            AppPresentUtcUnixMs = GetNullableLong(correlationSnapshot, "PreviewD3DLastRenderedUtcUnixMs"),
            KeepCsv = true
        }));
        actions.Add("presentmon capture started");
    }
}
