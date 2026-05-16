using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;

namespace Sussudio.Tools;

internal sealed partial class DiagnosticSessionCommandChannel : IDisposable
{
    private readonly Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> _sendCommandAsync;
    private readonly CancellationToken _defaultCancellationToken;
    private readonly List<string> _warnings;
    private readonly SemaphoreSlim _sendGate = new(1, 1);
    private int _failureCount;

    internal DiagnosticSessionCommandChannel(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken defaultCancellationToken,
        List<string> warnings)
    {
        _sendCommandAsync = sendCommandAsync;
        _defaultCancellationToken = defaultCancellationToken;
        _warnings = warnings;
    }

    internal int FailureCount => _failureCount;

    internal void RecordFailure(string warning)
    {
        _failureCount++;
        _warnings.Add(warning);
    }

    internal async Task<JsonElement> SendRawWithConnectRetryAsync(
        string command,
        Dictionary<string, object?>? payload,
        int? responseTimeoutMs)
        => await SendRawWithConnectRetryWithTokenAsync(command, payload, responseTimeoutMs, _defaultCancellationToken).ConfigureAwait(false);

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

    internal async Task<JsonElement> SendAsync(
        string command,
        Dictionary<string, object?>? payload,
        int? responseTimeoutMs)
        => await SendWithTokenAsync(command, payload, responseTimeoutMs, false, _defaultCancellationToken).ConfigureAwait(false);

    internal async Task<JsonElement> SendAsync(
        string command,
        Dictionary<string, object?>? payload,
        int? responseTimeoutMs,
        bool allowFailure)
        => await SendWithTokenAsync(command, payload, responseTimeoutMs, allowFailure, _defaultCancellationToken).ConfigureAwait(false);

    internal async Task<JsonElement> SendWithTokenAsync(
        string command,
        Dictionary<string, object?>? payload,
        int? responseTimeoutMs,
        bool allowFailure,
        CancellationToken commandCancellationToken)
    {
        await _sendGate.WaitAsync(commandCancellationToken).ConfigureAwait(false);
        try
        {
            var response = await SendRawWithConnectRetryWithTokenAsync(command, payload, responseTimeoutMs, commandCancellationToken).ConfigureAwait(false);
            if (!IsSuccess(response) && !allowFailure)
            {
                RecordFailure($"{command}: {Get(response, "Message", "command failed")}");
            }

            return response.Clone();
        }
        finally
        {
            _sendGate.Release();
        }
    }

    public void Dispose()
    {
        _sendGate.Dispose();
    }
}
