using System;
using System.Text.Json;

namespace Sussudio.Tools;

internal readonly record struct AutomationPipeCommandResult(
    string ResponseJson,
    bool StateRead,
    bool Success,
    string? Status,
    int? RetryAfterMs,
    JsonElement? ResponseElement);

internal class AutomationPipeException : Exception
{
    public AutomationPipeException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

internal sealed class AutomationPipeConnectException : AutomationPipeException
{
    public AutomationPipeConnectException(string message, string errorCode, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}

internal sealed class AutomationPipeResponseTimeoutException : AutomationPipeException
{
    public AutomationPipeResponseTimeoutException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

internal sealed class AutomationPipeProtocolException : AutomationPipeException
{
    public AutomationPipeProtocolException(string message)
        : base(message)
    {
    }
}
