using System.IO;
using System.Text.Json;
using Sussudio.Models;
using Sussudio.Tools;

namespace McpServer.Tools;

public static partial class PresentMonTools
{
    private readonly record struct PresentMonCorrelation(
        string? SwapChainAddress,
        long? PresentId,
        long? SourceSequenceNumber,
        long? PresentUtcUnixMs);

    private static async Task<PresentMonCorrelation> TryResolvePreviewPresentCorrelationAsync(PipeClient pipeClient)
    {
        try
        {
            var response = await pipeClient.SendCommandAsync(AutomationCommandKind.GetSnapshot).ConfigureAwait(false);
            if (!AutomationSnapshotFormatter.IsSuccess(response) ||
                !response.TryGetProperty("Snapshot", out var snapshot))
            {
                return default;
            }

            var address = AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DSwapChainAddress", string.Empty);
            return new PresentMonCorrelation(
                string.IsNullOrWhiteSpace(address) ? null : address,
                GetPositiveLong(snapshot, "PreviewD3DLastRenderedPreviewPresentId"),
                GetNonNegativeLong(snapshot, "PreviewD3DLastRenderedSourceSequenceNumber"),
                GetPositiveLong(snapshot, "PreviewD3DLastRenderedUtcUnixMs"));
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

    private static long? GetPositiveLong(JsonElement snapshot, string name)
    {
        var value = AutomationSnapshotFormatter.GetLong(snapshot, name, 0);
        return value > 0 ? value : null;
    }

    private static long? GetNonNegativeLong(JsonElement snapshot, string name)
    {
        var value = AutomationSnapshotFormatter.GetLong(snapshot, name, -1);
        return value >= 0 ? value : null;
    }
}
