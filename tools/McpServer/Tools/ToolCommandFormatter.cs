using ElgatoCapture.Tools;
namespace McpServer.Tools;

internal static class ToolCommandFormatter
{
    internal readonly record struct PendingCommand(
        string CommandName,
        string Label,
        Dictionary<string, object?>? Payload,
        bool HasValue);

    internal static PendingCommand Optional(string commandName, string label, string payloadKey, string? value)
        => Optional(commandName, label, !string.IsNullOrWhiteSpace(value), new Dictionary<string, object?> { [payloadKey] = value });

    internal static PendingCommand Optional<T>(string commandName, string label, string payloadKey, T? value)
        where T : struct
        => Optional(commandName, label, value.HasValue, value.HasValue ? new Dictionary<string, object?> { [payloadKey] = value.Value } : null);

    internal static PendingCommand Optional(
        string commandName,
        string label,
        bool hasValue,
        Dictionary<string, object?>? payload = null)
        => new(commandName, label, payload, hasValue);

    internal static async Task<string> ExecuteAndFormatAsync(
        PipeClient pipeClient,
        string commandName,
        string label,
        Dictionary<string, object?>? payload = null,
        int? responseTimeoutMs = null)
    {
        var response = await pipeClient.SendCommandAsync(commandName, payload, responseTimeoutMs).ConfigureAwait(false);
        var status = AutomationSnapshotFormatter.IsSuccess(response) ? "OK" : "ERROR";
        var message = AutomationSnapshotFormatter.Get(response, "Message", "No message.");
        return $"[{status}] {label}: {message}";
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

            results.Add(await ExecuteAndFormatAsync(pipeClient, command.CommandName, command.Label, command.Payload).ConfigureAwait(false));
        }

        return results.Count == 0
            ? emptyMessage
            : string.Join(Environment.NewLine, results);
    }
}
