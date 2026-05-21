using System.Text.Json;
using Sussudio.Models;
using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;

namespace Sussudio.Tools;

internal sealed partial class DiagnosticSessionCommandChannel
{
    internal async Task<JsonElement> SendRawWithConnectRetryAsync(
        AutomationCommandKind kind,
        Dictionary<string, object?>? payload,
        int? responseTimeoutMs)
        => await SendRawWithConnectRetryAsync(CommandName(kind), payload, responseTimeoutMs).ConfigureAwait(false);

    internal async Task<JsonElement> SendRawWithConnectRetryAsync(
        string command,
        Dictionary<string, object?>? payload,
        int? responseTimeoutMs)
        => await SendRawWithConnectRetryWithTokenAsync(command, payload, responseTimeoutMs, _defaultCancellationToken).ConfigureAwait(false);

    internal async Task<JsonElement> SendRawWithConnectRetryWithTokenAsync(
        AutomationCommandKind kind,
        Dictionary<string, object?>? payload,
        int? responseTimeoutMs,
        CancellationToken commandCancellationToken)
        => await SendRawWithConnectRetryWithTokenAsync(CommandName(kind), payload, responseTimeoutMs, commandCancellationToken).ConfigureAwait(false);

    internal async Task<JsonElement> SendRawWithConnectRetryWithTokenAsync(
        string command,
        Dictionary<string, object?>? payload,
        int? responseTimeoutMs,
        CancellationToken commandCancellationToken)
    {
        var response = await SendCommandWithConnectRetryAsync(
                _sendCommandAsync,
                command,
                payload,
                responseTimeoutMs,
                TimeSpan.FromSeconds(30),
                commandCancellationToken)
            .ConfigureAwait(false);
        return response ?? BuildLocalFailureResponse(command, "no response after connect retry");
    }
}
