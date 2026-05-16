using System.Text.Json;
using Sussudio.Models;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;

namespace McpServer.Tools;

// Shared formatting helper for MCP tools that execute one or more automation
// commands and present concise text results.
internal static class ToolCommandFormatter
{
    internal readonly record struct PendingCommand(
        AutomationCommandKind Kind,
        string Label,
        Dictionary<string, object?>? Payload,
        bool HasValue);

    internal static PendingCommand Optional(AutomationCommandKind kind, string label, string payloadKey, string? value)
        => Optional(kind, label, !string.IsNullOrWhiteSpace(value), new Dictionary<string, object?> { [payloadKey] = value });

    internal static PendingCommand Optional<T>(AutomationCommandKind kind, string label, string payloadKey, T? value)
        where T : struct
        => Optional(kind, label, value.HasValue, value.HasValue ? new Dictionary<string, object?> { [payloadKey] = value.Value } : null);

    internal static PendingCommand Optional(
        AutomationCommandKind kind,
        string label,
        bool hasValue,
        Dictionary<string, object?>? payload = null)
        => new(kind, label, payload, hasValue);

    internal static PendingCommand Optional(AutomationCommandKind kind, string label, bool hasValue)
        => Optional(kind, label, hasValue, payload: null);

    internal static async Task<string> ExecuteAndFormatAsync(
        PipeClient pipeClient,
        AutomationCommandKind kind,
        string label,
        Dictionary<string, object?>? payload = null,
        int? responseTimeoutMs = null)
    {
        var response = await pipeClient.SendCommandAsync(kind, payload, responseTimeoutMs).ConfigureAwait(false);
        return FormatCommandResponse(response, label);
    }

    internal static async Task<CallToolResult> ExecuteAndFormatResultAsync(
        PipeClient pipeClient,
        AutomationCommandKind kind,
        string label,
        Dictionary<string, object?>? payload = null,
        int? responseTimeoutMs = null)
    {
        var response = await pipeClient.SendCommandAsync(kind, payload, responseTimeoutMs).ConfigureAwait(false);
        return McpToolResultFactory.FromResponse(response, FormatCommandResponse(response, label));
    }

    internal static async Task<string> ExecuteBatchAsync(
        PipeClient pipeClient,
        string emptyMessage,
        params PendingCommand[] commands)
    {
        var results = new List<string>();
        foreach (var command in commands)
        {
            if (!command.HasValue)
            {
                continue;
            }

            results.Add(await ExecuteAndFormatAsync(pipeClient, command.Kind, command.Label, command.Payload).ConfigureAwait(false));
        }

        return results.Count == 0
            ? emptyMessage
            : string.Join(Environment.NewLine, results);
    }

    internal static async Task<CallToolResult> ExecuteBatchResultAsync(
        PipeClient pipeClient,
        string emptyMessage,
        params PendingCommand[] commands)
    {
        var results = new List<string>();
        var isError = false;
        foreach (var command in commands)
        {
            if (!command.HasValue)
            {
                continue;
            }

            var response = await pipeClient.SendCommandAsync(command.Kind, command.Payload).ConfigureAwait(false);
            isError |= !AutomationSnapshotFormatter.IsSuccess(response);
            results.Add(FormatCommandResponse(response, command.Label));
        }

        return McpToolResultFactory.FromText(
            results.Count == 0 ? emptyMessage : string.Join(Environment.NewLine, results),
            isError);
    }

    internal static string FormatCommandResponse(JsonElement response, string label)
    {
        var status = AutomationSnapshotFormatter.IsSuccess(response) ? "OK" : "ERROR";
        var message = AutomationSnapshotFormatter.Get(response, "Message", "No message.");
        return $"[{status}] {label}: {message}";
    }
}
