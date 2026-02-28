using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class WaitTools
{
    [McpServerTool, Description("Wait for a condition to be met. Blocks until satisfied or timeout. Conditions: PreviewFramesActive, PreviewRendererHealthy, AudioSignalPresent, RecordingFileGrowing, RecordingStopped, VerificationReady, HdrModeApplied, PerformancePerfectionMet, HdrVerificationReady, AudioFramesFlowing, VideoFramesFlowing")]
    public static async Task<string> wait_for_condition(
        PipeClient pipeClient,
        [Description("Condition name to wait for")] string condition,
        [Description("Timeout in milliseconds (default: 10000)")] int timeoutMs = 10000,
        [Description("Polling interval in milliseconds (default: 250)")] int pollMs = 250)
    {
        var payload = new Dictionary<string, object?>
        {
            ["condition"] = condition,
            ["timeoutMs"] = timeoutMs,
            ["pollMs"] = pollMs
        };

        var responseTimeoutMs = Math.Max(timeoutMs + 5000, PipeClient.ResponseTimeoutMs);
        var response = await pipeClient.SendCommandAsync("WaitForCondition", payload, responseTimeoutMs).ConfigureAwait(false);

        var builder = new StringBuilder();
        builder.AppendLine(IsSuccess(response) ? "Condition result: MET" : "Condition result: NOT MET");
        builder.AppendLine($"Message: {ResponseFormatter.Get(response, "Message", "No message.")}");

        if (response.TryGetProperty("Data", out var data) && data.ValueKind == JsonValueKind.Object)
        {
            builder.AppendLine($"Condition: {ResponseFormatter.Get(data, "condition")}");
            builder.AppendLine($"Met: {ResponseFormatter.Get(data, "met")}");
            builder.AppendLine($"TimeoutMs: {ResponseFormatter.Get(data, "timeoutMs")}");
            builder.AppendLine($"PollMs: {ResponseFormatter.Get(data, "pollMs")}");
        }

        return builder.ToString().TrimEnd();
    }

    private static bool IsSuccess(JsonElement response)
    {
        return response.ValueKind == JsonValueKind.Object &&
               response.TryGetProperty("Success", out var success) &&
               success.ValueKind == JsonValueKind.True;
    }
}
