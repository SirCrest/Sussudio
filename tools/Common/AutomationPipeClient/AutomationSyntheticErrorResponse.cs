using System.Text.Json;

namespace Sussudio.Tools;

internal static class AutomationSyntheticErrorResponse
{
    public static JsonElement Create(string message, string errorCode)
    {
        var response = new Dictionary<string, object?>
        {
            ["Success"] = false,
            ["CorrelationId"] = Guid.NewGuid().ToString("N"),
            ["TimestampUtc"] = DateTimeOffset.UtcNow,
            ["Status"] = "error",
            ["CommandLifecycle"] = "failed",
            ["RetryAfterMs"] = null,
            ["ElapsedMs"] = null,
            ["Message"] = string.IsNullOrWhiteSpace(message) ? "Unknown pipe client error." : message,
            ["ErrorCode"] = errorCode,
            ["Data"] = null,
            ["Snapshot"] = null
        };

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(response));
        return document.RootElement.Clone();
    }

    public static bool CanCreateFromException(Exception exception)
        => exception is AutomationPipeConnectException ||
            exception is AutomationPipeResponseTimeoutException ||
            exception is AutomationPipeProtocolException ||
            exception is JsonException ||
            exception is IOException ||
            exception is OperationCanceledException;

    public static JsonElement Create(Exception exception)
        => exception switch
        {
            AutomationPipeConnectException ex => Create(ex.Message, ex.ErrorCode),
            AutomationPipeResponseTimeoutException ex => Create(ex.Message, "pipe-response-timeout"),
            AutomationPipeProtocolException ex => Create(ex.Message, "pipe-protocol-error"),
            JsonException ex => Create(
                $"Automation pipe returned invalid JSON: {ex.Message}",
                "pipe-invalid-json"),
            IOException ex => Create(
                $"Automation pipe I/O failed ({ex.GetType().Name}): {ex.Message}",
                "pipe-io-error"),
            OperationCanceledException ex => Create(
                $"Automation pipe request canceled: {ex.Message}",
                "pipe-canceled"),
            _ => throw new ArgumentException(
                $"Exception type '{exception.GetType().FullName}' cannot be converted to a synthetic automation error response.",
                nameof(exception))
        };
}
