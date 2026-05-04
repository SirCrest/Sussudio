using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class DiagnosticsTools
{
    [McpServerTool, Description("Get recent diagnostic events with severity, category, and timestamps")]
    public static async Task<CallToolResult> get_diagnostics(
        PipeClient pipeClient,
        [Description("Maximum number of events to return (default: 50)")] int maxEvents = 50)
    {
        var payload = new Dictionary<string, object?>
        {
            ["maxEvents"] = maxEvents
        };

        var response = await pipeClient.SendCommandAsync("GetDiagnostics", payload).ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(response))
        {
            return McpToolResultFactory.FromResponse(response, GetMessage(response));
        }

        if (!response.TryGetProperty("Data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return McpToolResultFactory.FromText("No diagnostic events were returned.", isError: true);
        }

        var lines = new StringBuilder();
        var count = 0;
        foreach (var item in data.EnumerateArray())
        {
            var timestamp = AutomationSnapshotFormatter.Get(item, "TimestampUtc");
            var severity = AutomationSnapshotFormatter.Get(item, "Severity");
            var category = AutomationSnapshotFormatter.Get(item, "Category");
            var message = AutomationSnapshotFormatter.Get(item, "Message");
            lines.AppendLine($"[{timestamp}] [{severity}] [{category}] {message}");
            count++;
        }

        var text = count == 0
            ? "No diagnostic events were returned."
            : lines.ToString().TrimEnd();
        return McpToolResultFactory.FromResponse(response, text);
    }

    private static string GetMessage(JsonElement response)
    {
        return AutomationSnapshotFormatter.Get(response, "Message", "Command failed.");
    }
}
