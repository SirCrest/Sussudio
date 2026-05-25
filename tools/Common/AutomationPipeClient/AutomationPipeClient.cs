using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Tools;

internal static class AutomationPipeClient
{
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

    internal static async Task<string> SendCommandAsync(
        string pipeName,
        AutomationCommandKind kind,
        object? payload,
        int connectTimeoutMs,
        int responseTimeoutMs,
        string? authToken = null,
        CancellationToken cancellationToken = default)
    {
        var result = await SendCommandWithResultAsync(
                pipeName,
                kind,
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

        return await SendCommandWithResultAsync(
                pipeName,
                commandValue,
                payload,
                connectTimeoutMs,
                responseTimeoutMs,
                authToken,
                includeResponseElement,
                cancellationToken)
            .ConfigureAwait(false);
    }

    internal static Task<AutomationPipeCommandResult> SendCommandWithResultAsync(
        string pipeName,
        AutomationCommandKind kind,
        object? payload,
        int connectTimeoutMs,
        int responseTimeoutMs,
        string? authToken = null,
        bool includeResponseElement = false,
        CancellationToken cancellationToken = default)
        => SendCommandWithResultAsync(
            pipeName,
            (int)kind,
            payload,
            connectTimeoutMs,
            responseTimeoutMs,
            authToken,
            includeResponseElement,
            cancellationToken);

    private static async Task<AutomationPipeCommandResult> SendCommandWithResultAsync(
        string pipeName,
        int commandValue,
        object? payload,
        int connectTimeoutMs,
        int responseTimeoutMs,
        string? authToken,
        bool includeResponseElement,
        CancellationToken cancellationToken)
    {
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

        await ConnectWithClassifiedErrorsAsync(
                client,
                pipeName,
                connectTimeoutMs,
                cancellationToken)
            .ConfigureAwait(false);

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

    private static async Task ConnectWithClassifiedErrorsAsync(
        NamedPipeClientStream client,
        string pipeName,
        int connectTimeoutMs,
        CancellationToken cancellationToken)
    {
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
    }
}

internal static class AutomationCommandTransport
{
    public static async Task<JsonElement> SendCommandAsync(
        string pipeName,
        AutomationCommandKind kind,
        object? payload = null,
        int? responseTimeoutOverrideMs = null,
        int? responseTimeoutMs = null,
        AutomationUnknownCommandHandling unknownCommandHandling = AutomationUnknownCommandHandling.ReturnSyntheticError)
    {
        var effectiveResponseTimeoutMs = responseTimeoutMs
            ?? responseTimeoutOverrideMs
            ?? AutomationPipeProtocol.GetDefaultResponseTimeout(kind);

        try
        {
            var result = await AutomationPipeClient.SendCommandWithResultAsync(
                    pipeName,
                    kind,
                    payload,
                    AutomationPipeProtocol.DefaultConnectTimeoutMs,
                    effectiveResponseTimeoutMs,
                    includeResponseElement: true)
                .ConfigureAwait(false);

            return result.ResponseElement
                ?? throw new JsonException("Automation pipe returned invalid JSON.");
        }
        catch (ArgumentException ex) when (unknownCommandHandling == AutomationUnknownCommandHandling.ReturnSyntheticError)
        {
            return AutomationSyntheticErrorResponse.Create(ex.Message, "unknown-command");
        }
        catch (Exception ex) when (AutomationSyntheticErrorResponse.CanCreateFromException(ex))
        {
            return AutomationSyntheticErrorResponse.Create(ex);
        }
    }

    public static async Task<JsonElement> SendCommandAsync(
        string pipeName,
        string commandName,
        object? payload = null,
        int? responseTimeoutOverrideMs = null,
        int? responseTimeoutMs = null,
        AutomationUnknownCommandHandling unknownCommandHandling = AutomationUnknownCommandHandling.ReturnSyntheticError)
    {
        var effectiveResponseTimeoutMs = responseTimeoutMs
            ?? responseTimeoutOverrideMs
            ?? AutomationPipeProtocol.GetDefaultResponseTimeout(commandName);

        try
        {
            var result = await AutomationPipeClient.SendCommandWithResultAsync(
                    pipeName,
                    commandName,
                    payload,
                    AutomationPipeProtocol.DefaultConnectTimeoutMs,
                    effectiveResponseTimeoutMs,
                    includeResponseElement: true)
                .ConfigureAwait(false);

            return result.ResponseElement
                ?? throw new JsonException("Automation pipe returned invalid JSON.");
        }
        catch (ArgumentException ex) when (unknownCommandHandling == AutomationUnknownCommandHandling.ReturnSyntheticError)
        {
            return AutomationSyntheticErrorResponse.Create(ex.Message, "unknown-command");
        }
        catch (Exception ex) when (AutomationSyntheticErrorResponse.CanCreateFromException(ex))
        {
            return AutomationSyntheticErrorResponse.Create(ex);
        }
    }
}
