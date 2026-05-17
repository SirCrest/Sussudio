using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Tools;

internal static partial class AutomationPipeClient
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
}
