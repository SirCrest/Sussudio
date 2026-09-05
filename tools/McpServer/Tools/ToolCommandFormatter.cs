using System.Text.Json;
using Sussudio.Models;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;

namespace McpServer.Tools;

// Shared formatting helper for MCP tools that execute one or more automation
// commands and present concise text results.
internal static class ToolCommandFormatter
{
    // Detail is for the rare command whose display name carries an argument
    // (FlashbackAction names the action it ran); everything else takes its label
    // from the command kind, which is the only spelling the wire format has.
    internal readonly record struct PendingCommand(
        AutomationCommandKind Kind,
        Dictionary<string, object?>? Payload,
        bool HasValue,
        string? Detail = null);

    internal static PendingCommand Optional(AutomationCommandKind kind, string payloadKey, string? value)
        => Optional(kind, !string.IsNullOrWhiteSpace(value), new Dictionary<string, object?> { [payloadKey] = value });

    internal static PendingCommand Optional<T>(AutomationCommandKind kind, string payloadKey, T? value)
        where T : struct
        => Optional(kind, value.HasValue, value.HasValue ? new Dictionary<string, object?> { [payloadKey] = value.Value } : null);

    internal static PendingCommand Optional(
        AutomationCommandKind kind,
        bool hasValue,
        Dictionary<string, object?>? payload = null,
        string? detail = null)
        => new(kind, payload, hasValue, detail);

    internal static PendingCommand Optional(AutomationCommandKind kind, bool hasValue)
        => Optional(kind, hasValue, payload: null);

    internal static async Task<string> ExecuteAndFormatAsync(
        PipeClient pipeClient,
        AutomationCommandKind kind,
        Dictionary<string, object?>? payload = null,
        int? responseTimeoutMs = null,
        string? detail = null)
    {
        var response = await pipeClient.SendCommandAsync(kind, payload, responseTimeoutMs).ConfigureAwait(false);
        return FormatCommandResponse(response, kind, detail);
    }

    internal static async Task<CallToolResult> ExecuteAndFormatResultAsync(
        PipeClient pipeClient,
        AutomationCommandKind kind,
        Dictionary<string, object?>? payload = null,
        int? responseTimeoutMs = null,
        string? detail = null)
    {
        var response = await pipeClient.SendCommandAsync(kind, payload, responseTimeoutMs).ConfigureAwait(false);
        return McpToolResultFactory.FromResponse(response, FormatCommandResponse(response, kind, detail));
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

            var response = await pipeClient.SendCommandAsync(command.Kind, command.Payload).ConfigureAwait(false);
            results.Add(FormatCommandResponse(response, command.Kind, command.Detail));
            if (!AutomationSnapshotFormatter.IsSuccess(response))
            {
                break;
            }
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
            results.Add(FormatCommandResponse(response, command.Kind, command.Detail));
            if (!AutomationSnapshotFormatter.IsSuccess(response))
            {
                isError = true;
                break;
            }
        }

        return McpToolResultFactory.FromText(
            results.Count == 0 ? emptyMessage : string.Join(Environment.NewLine, results),
            isError);
    }

    internal static string FormatCommandResponse(JsonElement response, AutomationCommandKind kind, string? detail = null)
    {
        var status = AutomationSnapshotFormatter.IsSuccess(response) ? "OK" : "ERROR";
        var message = AutomationSnapshotFormatter.Get(response, "Message", "No message.");
        var label = detail is null ? kind.ToString() : $"{kind}({detail})";
        return $"[{status}] {label}: {message}";
    }
}

// Creates MCP CallToolResult objects from automation responses.
internal static class McpToolResultFactory
{
    internal static CallToolResult FromStructuredResponse(JsonElement response, string propertyName, string missingMessage)
    {
        if (!AutomationSnapshotFormatter.IsSuccess(response))
        {
            return FromResponse(response, GetMessage(response));
        }

        if (!response.TryGetProperty(propertyName, out var payload) || payload.ValueKind != JsonValueKind.Object)
        {
            var message = GetMessage(response, string.Empty);
            var errorCode = AutomationSnapshotFormatter.Get(response, "ErrorCode", string.Empty);
            var text = missingMessage;
            if (!string.IsNullOrWhiteSpace(message)) text += $"{Environment.NewLine}{message}";
            if (!string.IsNullOrWhiteSpace(errorCode)) text += $"{Environment.NewLine}ErrorCode: {errorCode}";
            return FromText(text, isError: true);
        }

        var json = payload.GetRawText();
        var result = FromText(json);
        result.StructuredContent = payload.Clone();
        return result;
    }

    internal static CallToolResult FromResponse(JsonElement response, string text)
    {
        var isError = !AutomationSnapshotFormatter.IsSuccess(response);
        if (isError)
        {
            var errorCode = AutomationSnapshotFormatter.Get(response, "ErrorCode", string.Empty);
            if (!string.IsNullOrWhiteSpace(errorCode) &&
                !text.Contains(errorCode, StringComparison.OrdinalIgnoreCase))
            {
                text = $"{text}{Environment.NewLine}ErrorCode: {errorCode}";
            }
        }

        return FromText(text, isError);
    }

    internal static CallToolResult FromText(string text, bool isError = false)
        => new()
        {
            Content = [new TextContentBlock { Text = text }],
            IsError = isError
        };

    internal static string GetMessage(JsonElement response, string fallback = "Command failed.")
        => AutomationSnapshotFormatter.Get(response, "Message", fallback);
}
