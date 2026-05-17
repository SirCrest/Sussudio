using System.Text.Json;
using Sussudio.Models;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

// ssctl transport wrapper that applies command-specific timeouts and converts
// all transport and protocol errors into the same structured {Success:false}
// envelope shape that MCP's PipeClient.SendCommandAsync produces. This ensures
// exit code 3 (structured server/transport failure) is the single non-zero
// exit code for any automation error, and ssctl scripts can be ported to MCP
// without changing error-handling paths. Only UsageException (unknown command)
// still propagates — it surfaces a help message rather than a structured result.
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
        try
        {
            return await AutomationCommandTransport.SendCommandAsync(
                    _pipeName,
                    commandName,
                    payload,
                    responseTimeoutOverrideMs: _responseTimeoutOverrideMs,
                    responseTimeoutMs: responseTimeoutMs,
                    unknownCommandHandling: AutomationUnknownCommandHandling.ThrowArgumentException)
                .ConfigureAwait(false);
        }
        catch (ArgumentException ex)
        {
            // Unknown command name: surface as a usage error so the CLI prints
            // help rather than a structured failure result. Only non-transport
            // exception class that should propagate.
            throw new UsageException(ex.Message);
        }
    }

    public Task<JsonElement> SendCommandAsync(
        AutomationCommandKind kind,
        Dictionary<string, object?>? payload = null,
        int? responseTimeoutMs = null)
        => AutomationCommandTransport.SendCommandAsync(
            _pipeName,
            kind,
            payload,
            responseTimeoutOverrideMs: _responseTimeoutOverrideMs,
            responseTimeoutMs: responseTimeoutMs,
            unknownCommandHandling: AutomationUnknownCommandHandling.ThrowArgumentException);

}
