using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace Sussudio.Tools;

public readonly record struct AutomationPipeCommandResult(
    string ResponseJson,
    bool StateRead,
    bool Success,
    string? Status,
    int? RetryAfterMs,
    JsonElement? ResponseElement);

public class AutomationPipeException : Exception
{
    public AutomationPipeException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class AutomationPipeConnectException : AutomationPipeException
{
    public AutomationPipeConnectException(string message, string errorCode, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}

public sealed class AutomationPipeResponseTimeoutException : AutomationPipeException
{
    public AutomationPipeResponseTimeoutException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class AutomationPipeProtocolException : AutomationPipeException
{
    public AutomationPipeProtocolException(string message)
        : base(message)
    {
    }
}

public enum AutomationUnknownCommandHandling
{
    ReturnSyntheticError,
    ThrowArgumentException
}

// Tolerant success/status/retry reader for automation response envelopes.
public static class AutomationResponseState
{
    public static bool TryRead(
        JsonElement response,
        out bool success,
        out string? status,
        out int? retryAfterMs)
    {
        success = false;
        status = null;
        retryAfterMs = null;

        if (response.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (response.TryGetProperty("Success", out var successProperty) &&
            successProperty.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            success = successProperty.GetBoolean();
        }

        if (response.TryGetProperty("Status", out var statusProperty) &&
            statusProperty.ValueKind == JsonValueKind.String)
        {
            status = statusProperty.GetString();
        }

        if (response.TryGetProperty("RetryAfterMs", out var retryAfterProperty))
        {
            if (retryAfterProperty.ValueKind == JsonValueKind.Number &&
                retryAfterProperty.TryGetInt32(out var numeric))
            {
                retryAfterMs = numeric;
            }
            else if (retryAfterProperty.ValueKind == JsonValueKind.String &&
                     int.TryParse(
                         retryAfterProperty.GetString(),
                         NumberStyles.Integer,
                         CultureInfo.InvariantCulture,
                         out var parsed))
            {
                retryAfterMs = parsed;
            }
        }

        return true;
    }
}

public static class AutomationSyntheticErrorResponse
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
