using System.IO;
using System.Text.Json;
using Sussudio.Tools;

namespace McpServer;

// MCP-side adapter over the shared automation pipe client. Tool handlers call
// this instead of handling pipe connection and synthetic error shaping directly.
public sealed class PipeClient
{
    private readonly string _pipeName;

    public PipeClient()
        : this(null)
    {
    }

    internal PipeClient(string? pipeName)
    {
        var configuredPipeName = string.IsNullOrWhiteSpace(pipeName)
            ? Environment.GetEnvironmentVariable("SUSSUDIO_AUTOMATION_PIPE")
            : pipeName;
        _pipeName = string.IsNullOrWhiteSpace(configuredPipeName)
            ? AutomationPipeProtocol.DefaultPipeName
            : configuredPipeName;
    }

    public async Task<JsonElement> SendCommandAsync(
        string commandName,
        Dictionary<string, object?>? payload = null,
        int? responseTimeoutMs = null)
    {
        var effectiveResponseTimeoutMs = responseTimeoutMs ?? AutomationPipeProtocol.GetDefaultResponseTimeout(commandName);

        try
        {
            var result = await AutomationPipeClient.SendCommandWithResultAsync(
                    _pipeName,
                    commandName,
                    payload,
                    AutomationPipeProtocol.DefaultConnectTimeoutMs,
                    effectiveResponseTimeoutMs,
                    includeResponseElement: true)
                .ConfigureAwait(false);

            return result.ResponseElement
                ?? throw new JsonException("Automation pipe returned invalid JSON.");
        }
        catch (AutomationPipeConnectException)
        {
            return CreateSyntheticError(
                "Sussudio is not running or not responding. Start the app and try again.",
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
        catch (ArgumentException ex)
        {
            return CreateSyntheticError(ex.Message, "unknown-command");
        }
        catch (JsonException ex)
        {
            return CreateSyntheticError(
                $"Automation pipe returned invalid JSON: {ex.Message}",
                "pipe-invalid-json");
        }
        catch (IOException ex)
        {
            return CreateSyntheticError(
                $"Automation pipe I/O failed ({ex.GetType().Name}): {ex.Message}",
                "pipe-io-error");
        }
        catch (OperationCanceledException ex)
        {
            return CreateSyntheticError(
                $"Automation pipe request canceled: {ex.Message}",
                "pipe-canceled");
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

}
