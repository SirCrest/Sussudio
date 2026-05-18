using System.IO;
using System.Text.Json;
using Sussudio.Models;
using Sussudio.Tools;

namespace McpServer.Tools;

public static partial class PresentMonTools
{
    private static async Task<PresentMonProbeCorrelation> TryResolvePreviewPresentCorrelationAsync(PipeClient pipeClient)
    {
        try
        {
            var response = await pipeClient.SendCommandAsync(AutomationCommandKind.GetSnapshot).ConfigureAwait(false);
            if (!AutomationSnapshotFormatter.IsSuccess(response) ||
                !response.TryGetProperty("Snapshot", out var snapshot))
            {
                return default;
            }

            return PresentMonProbe.ReadPreviewCorrelation(snapshot);
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Trace.TraceWarning($"GetExpectedSwapChainAsync: malformed snapshot JSON: {ex.Message}");
            return default;
        }
        catch (IOException ex)
        {
            System.Diagnostics.Trace.TraceWarning($"GetExpectedSwapChainAsync: pipe IO failure: {ex.Message}");
            return default;
        }
    }
}
