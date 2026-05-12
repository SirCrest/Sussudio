using System;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.Tools;

// Shared pipe client used by ssctl, diagnostic sessions, and smaller smoke
// tools. It centralizes the JSON framing/auth/timeout rules so every external
// harness exercises the same protocol as MCP.
internal static class AutomationPipeClient
{
    internal static Task<string> SendRequestAsync(
        string pipeName,
        string requestJson,
        int connectTimeoutMs,
        int responseTimeoutMs)
        => SendRequestAsync(
            pipeName,
            requestJson,
            connectTimeoutMs,
            responseTimeoutMs,
            CancellationToken.None);

    internal static async Task<string> SendRequestAsync(
        string pipeName,
        string requestJson,
        int connectTimeoutMs,
        int responseTimeoutMs,
        CancellationToken cancellationToken)
    {
        using var client = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        try
        {
            await client.ConnectAsync(connectTimeoutMs, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException ex)
        {
            throw new AutomationPipeConnectException(
                $"Timed out connecting to automation pipe '{pipeName}' after {connectTimeoutMs} ms.",
                "pipe-connect-timeout",
                ex);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new AutomationPipeConnectException(
                $"Access denied connecting to automation pipe '{pipeName}'. The app is running, but this process is not allowed by the pipe security descriptor. Run the client from the same Windows user/elevation/session as the app, or restart the app with {AutomationPipeProtocol.AutomationKeyEnvVar} configured for token-gated fallback security.",
                "pipe-access-denied",
                ex);
        }
        catch (Exception ex)
        {
            throw new AutomationPipeConnectException(
                $"Failed to connect to automation pipe '{pipeName}': {ex.Message}",
                "pipe-connect-failed",
                ex);
        }

        using var writer = new StreamWriter(
            client,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            bufferSize: 4096,
            leaveOpen: true)
        {
            AutoFlush = true
        };

        using var reader = new StreamReader(
            client,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 4096,
            leaveOpen: true);

        await writer.WriteLineAsync(requestJson).WaitAsync(cancellationToken).ConfigureAwait(false);
        string? responseLine;
        try
        {
            responseLine = await reader.ReadLineAsync()
                .WaitAsync(TimeSpan.FromMilliseconds(responseTimeoutMs), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (TimeoutException ex)
        {
            throw new AutomationPipeResponseTimeoutException(
                $"Timed out waiting for automation response after {responseTimeoutMs} ms.",
                ex);
        }

        if (string.IsNullOrWhiteSpace(responseLine))
        {
            throw new AutomationPipeProtocolException("No response received from automation pipe.");
        }

        return responseLine;
    }

    internal static async Task<string> SendCommandAsync(
        string pipeName,
        string commandName,
        object? payload,
        int connectTimeoutMs,
        int responseTimeoutMs,
        string? authToken = null,
        CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithResultAsync(
                pipeName,
                commandName,
                payload,
                connectTimeoutMs,
                responseTimeoutMs,
                authToken,
                includeResponseElement: false,
                cancellationToken)
            .ConfigureAwait(false);
        return result.ResponseJson;
    }

    internal static async Task<AutomationPipeCommandResult> SendCommandWithResultAsync(
        string pipeName,
        string commandName,
        object? payload,
        int connectTimeoutMs,
        int responseTimeoutMs,
        string? authToken = null,
        bool includeResponseElement = false,
        CancellationToken cancellationToken = default)
    {
        var commandValue = AutomationPipeProtocol.ResolveCommand(commandName);

        for (var attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var request = AutomationPipeProtocol.CreateRequestEnvelope(
                commandValue,
                payload,
                authToken: authToken);

            var responseLine = await SendRequestAsync(
                    pipeName,
                    JsonSerializer.Serialize(request),
                    connectTimeoutMs,
                    responseTimeoutMs,
                    cancellationToken)
                .ConfigureAwait(false);

            var stateRead = TryReadResponseState(
                responseLine,
                includeResponseElement,
                out var success,
                out var status,
                out var retryAfterMs,
                out var responseElement);

            if (!stateRead ||
                success ||
                !string.Equals(status, "not_ready", StringComparison.OrdinalIgnoreCase) ||
                attempt >= AutomationPipeProtocol.DefaultNotReadyRetries)
            {
                return new AutomationPipeCommandResult(
                    responseLine,
                    stateRead,
                    success,
                    status,
                    retryAfterMs,
                    responseElement);
            }

            var delayMs = Math.Clamp(
                retryAfterMs ?? AutomationPipeProtocol.DefaultNotReadyDelayMs,
                100,
                30000);
            await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
        }
    }

    internal static bool TryReadResponseState(
        string responseJson,
        out bool success,
        out string? status,
        out int? retryAfterMs)
        => TryReadResponseState(
            responseJson,
            includeResponseElement: false,
            out success,
            out status,
            out retryAfterMs,
            out _);

    private static bool TryReadResponseState(
        string responseJson,
        bool includeResponseElement,
        out bool success,
        out string? status,
        out int? retryAfterMs,
        out JsonElement? responseElement)
    {
        success = false;
        status = null;
        retryAfterMs = null;
        responseElement = null;

        try
        {
            using var responseDocument = JsonDocument.Parse(responseJson);
            var response = responseDocument.RootElement;
            var stateRead = AutomationResponseState.TryRead(
                response,
                out success,
                out status,
                out retryAfterMs);
            if (includeResponseElement)
            {
                responseElement = response.Clone();
            }

            return stateRead;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

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
