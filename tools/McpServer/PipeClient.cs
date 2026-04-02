using System.Text.Json;
using ElgatoCapture.Tools;

namespace McpServer;

public sealed class PipeClient
{
    public async Task<JsonElement> SendCommandAsync(
        string commandName,
        Dictionary<string, object?>? payload = null,
        int? responseTimeoutMs = null)
    {
        if (!AutomationPipeProtocol.TryGetCommandValue(commandName, out var commandValue))
        {
            return CreateSyntheticError(
                $"Unknown automation command '{commandName}'.",
                "unknown-command");
        }

        var effectivePayload = payload ?? new Dictionary<string, object?>(StringComparer.Ordinal);
        var effectiveResponseTimeoutMs = responseTimeoutMs ?? AutomationPipeProtocol.GetDefaultResponseTimeout(commandName);

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                var request = AutomationPipeProtocol.CreateRequestEnvelope(commandValue, effectivePayload);

                var requestJson = JsonSerializer.Serialize(request);
                var responseLine = await SendAsync(
                    requestJson,
                    effectiveResponseTimeoutMs).ConfigureAwait(false);

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
            catch (AutomationPipeConnectException)
            {
                return CreateSyntheticError(
                    "ElgatoCapture is not running or not responding. Start the app and try again.",
                    "pipe-connect-failed");
            }
            catch (AutomationPipeResponseTimeoutException ex)
            {
                return CreateSyntheticError(ex.Message, "pipe-response-timeout");
            }
            catch (AutomationPipeProtocolException ex)
            {
                return CreateSyntheticError(ex.Message, "pipe-protocol-error");
            }
            catch (Exception ex)
            {
                return CreateSyntheticError(ex.Message, "pipe-client-error");
            }
        }
    }

    private static JsonElement CreateSyntheticError(string message, string errorCode)
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

        using var responseDocument = JsonDocument.Parse(JsonSerializer.Serialize(response));
        return responseDocument.RootElement.Clone();
    }

    private static async Task<string> SendAsync(string requestJson, int responseTimeoutMs)
    {
        return await AutomationPipeClient.SendRequestAsync(
            AutomationPipeProtocol.DefaultPipeName,
            requestJson,
            AutomationPipeProtocol.DefaultConnectTimeoutMs,
            responseTimeoutMs).ConfigureAwait(false);
    }
}
