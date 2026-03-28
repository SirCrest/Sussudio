using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class DiagnosticsTool
{
    [McpServerTool, Description("Get recent diagnostic events with severity, category, and timestamps")]
    public static async Task<string> get_diagnostics(
        PipeClient pipeClient,
        [Description("Maximum number of events to return (default: 50)")] int maxEvents = 50)
    {
        var payload = new Dictionary<string, object?>
        {
            ["maxEvents"] = maxEvents
        };

        var response = await pipeClient.SendCommandAsync("GetDiagnostics", payload).ConfigureAwait(false);
        if (!ResponseFormatter.IsSuccess(response))
        {
            return GetMessage(response);
        }

        if (!response.TryGetProperty("Data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return "No diagnostic events were returned.";
        }

        var lines = new StringBuilder();
        var count = 0;
        foreach (var item in data.EnumerateArray())
        {
            var timestamp = ResponseFormatter.Get(item, "TimestampUtc");
            var severity = ResponseFormatter.Get(item, "Severity");
            var category = ResponseFormatter.Get(item, "Category");
            var message = ResponseFormatter.Get(item, "Message");
            lines.AppendLine($"[{timestamp}] [{severity}] [{category}] {message}");
            count++;
        }

        return count == 0
            ? "No diagnostic events were returned."
            : lines.ToString().TrimEnd();
    }

    private static string GetMessage(JsonElement response)
    {
        return ResponseFormatter.Get(response, "Message", "Command failed.");
    }
}
