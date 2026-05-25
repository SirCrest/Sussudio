using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Sussudio.Models;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
// MCP tools for preview start/stop and preview-related toggles.
public static class PreviewTools
{
    [McpServerTool, Description("Start or stop the live preview")]
    public static async Task<CallToolResult> control_preview(
        PipeClient pipeClient,
        [Description("True to start preview, false to stop")] bool enabled)
    {
        var payload = new Dictionary<string, object?>
        {
            ["enabled"] = enabled
        };

        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(
                pipeClient,
                AutomationCommandKind.SetPreviewEnabled,
                "SetPreviewEnabled",
                payload)
            .ConfigureAwait(false);
    }

}

[McpServerToolType]
// MCP tools for starting and stopping user recordings.
public static class RecordingTools
{
    [McpServerTool, Description("Start or stop recording")]
    public static async Task<CallToolResult> control_recording(
        PipeClient pipeClient,
        [Description("True to start recording, false to stop")] bool enabled)
    {
        var payload = new Dictionary<string, object?>
        {
            ["enabled"] = enabled
        };

        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(
                pipeClient,
                AutomationCommandKind.SetRecordingEnabled,
                "SetRecordingEnabled",
                payload)
            .ConfigureAwait(false);
    }

}

[McpServerToolType]
// MCP wait helper for polling automation conditions until the app reaches a
// requested observable state.
public static class WaitTools
{
    private const int ResponseTimeoutBufferMs = 5000;

    [McpServerTool, Description("Wait for a condition to be met. Blocks until satisfied or timeout. Conditions: PreviewFramesActive, PreviewRendererHealthy, AudioSignalPresent, RecordingFileGrowing, RecordingStopped, VerificationReady, HdrModeApplied, PerformancePerfectionMet, HdrVerificationReady, AudioFramesFlowing, VideoFramesFlowing")]
    public static async Task<CallToolResult> wait_for_condition(
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

        var responseTimeoutMs = GetWaitForConditionResponseTimeoutMs(timeoutMs);
        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.WaitForCondition, payload, responseTimeoutMs).ConfigureAwait(false);

        var builder = new StringBuilder();
        builder.AppendLine(AutomationSnapshotFormatter.IsSuccess(response) ? "Condition result: MET" : "Condition result: NOT MET");
        builder.AppendLine($"Message: {AutomationSnapshotFormatter.Get(response, "Message", "No message.")}");

        if (response.TryGetProperty("Data", out var data) && data.ValueKind == JsonValueKind.Object)
        {
            builder.AppendLine($"Condition: {AutomationSnapshotFormatter.Get(data, "condition")}");
            builder.AppendLine($"Met: {AutomationSnapshotFormatter.Get(data, "met")}");
            builder.AppendLine($"TimeoutMs: {AutomationSnapshotFormatter.Get(data, "timeoutMs")}");
            builder.AppendLine($"PollMs: {AutomationSnapshotFormatter.Get(data, "pollMs")}");
        }

        return McpToolResultFactory.FromResponse(response, builder.ToString().TrimEnd());
    }

    internal static int GetWaitForConditionResponseTimeoutMs(int timeoutMs)
    {
        var requestedResponseTimeoutMs = (long)timeoutMs + ResponseTimeoutBufferMs;
        var catalogResponseTimeoutMs = AutomationPipeProtocol.GetDefaultResponseTimeout(AutomationCommandKind.WaitForCondition);
        var responseTimeoutMs = Math.Max(requestedResponseTimeoutMs, catalogResponseTimeoutMs);
        return responseTimeoutMs > int.MaxValue
            ? int.MaxValue
            : (int)responseTimeoutMs;
    }

}
