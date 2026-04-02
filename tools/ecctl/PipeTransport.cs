using System.Text.Json;
using ElgatoCapture.Tools;

namespace EcCtl;

internal sealed class PipeTransport
{
    private readonly string _pipeName;
    private readonly int? _responseTimeoutOverrideMs;

    public PipeTransport(string pipeName, int? responseTimeoutOverrideMs = null)
    {
        _pipeName = string.IsNullOrWhiteSpace(pipeName)
            ? AutomationPipeProtocol.DefaultPipeName
            : pipeName;
        _responseTimeoutOverrideMs = responseTimeoutOverrideMs;
    }

    public async Task<JsonElement> SendCommandAsync(
        string commandName,
        Dictionary<string, object?>? payload = null,
        int? responseTimeoutMs = null)
    {
        if (!AutomationPipeProtocol.TryGetCommandValue(commandName, out var commandValue))
        {
            throw new UsageException($"Unknown automation command '{commandName}'.");
        }

        var effectivePayload = payload ?? new Dictionary<string, object?>(StringComparer.Ordinal);
        var effectiveTimeoutMs = responseTimeoutMs
            ?? _responseTimeoutOverrideMs
            ?? AutomationPipeProtocol.GetDefaultResponseTimeout(commandName);

        for (var attempt = 0; ; attempt++)
        {
            var request = AutomationPipeProtocol.CreateRequestEnvelope(commandValue, effectivePayload);

            var requestJson = JsonSerializer.Serialize(request);
            var responseLine = await SendAsync(requestJson, effectiveTimeoutMs).ConfigureAwait(false);

            using var responseDocument = JsonDocument.Parse(responseLine);
            var response = responseDocument.RootElement.Clone();
            if (!AutomationResponseState.TryRead(response, out var success, out var status, out var retryAfterMs))
            {
                return response;
            }

            if (success)
            {
                return response;
            }

            if (!string.Equals(status, "not_ready", StringComparison.OrdinalIgnoreCase) ||
                attempt >= AutomationPipeProtocol.DefaultNotReadyRetries)
            {
                return response;
            }

            var delayMs = Math.Clamp(retryAfterMs ?? AutomationPipeProtocol.DefaultNotReadyDelayMs, 100, 30000);
            await Task.Delay(delayMs).ConfigureAwait(false);
        }
    }

    private async Task<string> SendAsync(string requestJson, int responseTimeoutMs)
    {
        return await AutomationPipeClient.SendRequestAsync(
            _pipeName,
            requestJson,
            AutomationPipeProtocol.DefaultConnectTimeoutMs,
            responseTimeoutMs).ConfigureAwait(false);
    }

}
