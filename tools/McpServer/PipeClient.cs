using System.IO;
using System.Text.Json;
using Sussudio.Models;
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
        catch (ArgumentException ex)
        {
            return AutomationSyntheticErrorResponse.Create(ex.Message, "unknown-command");
        }
        catch (Exception ex) when (AutomationSyntheticErrorResponse.CanCreateFromException(ex))
        {
            return AutomationSyntheticErrorResponse.Create(ex);
        }
    }

    public Task<JsonElement> SendCommandAsync(
        AutomationCommandKind kind,
        Dictionary<string, object?>? payload = null,
        int? responseTimeoutMs = null)
        => SendCommandAsync(AutomationCommandCatalog.Get(kind).Name, payload, responseTimeoutMs);

}
