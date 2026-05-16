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
        var effectiveTimeoutMs = responseTimeoutMs
            ?? _responseTimeoutOverrideMs
            ?? AutomationPipeProtocol.GetDefaultResponseTimeout(commandName);

        try
        {
            var result = await AutomationPipeClient.SendCommandWithResultAsync(
                    _pipeName,
                    commandName,
                    payload,
                    AutomationPipeProtocol.DefaultConnectTimeoutMs,
                    effectiveTimeoutMs,
                    includeResponseElement: true)
                .ConfigureAwait(false);

            return result.ResponseElement
                ?? AutomationSyntheticErrorResponse.Create(
                    "Automation pipe returned invalid JSON.",
                    "pipe-invalid-json");
        }
        catch (ArgumentException ex)
        {
            // Unknown command name: surface as a usage error so the CLI prints
            // help rather than a structured failure result. Only non-transport
            // exception class that should propagate.
            throw new UsageException(ex.Message);
        }
        catch (AutomationPipeConnectException ex)
        {
            return AutomationSyntheticErrorResponse.Create(ex.Message, ex.ErrorCode);
        }
        catch (AutomationPipeResponseTimeoutException ex)
        {
            return AutomationSyntheticErrorResponse.Create(ex.Message, "pipe-response-timeout");
        }
        catch (AutomationPipeProtocolException ex)
        {
            return AutomationSyntheticErrorResponse.Create(ex.Message, "pipe-protocol-error");
        }
        catch (JsonException ex)
        {
            return AutomationSyntheticErrorResponse.Create(
                $"Automation pipe returned invalid JSON: {ex.Message}",
                "pipe-invalid-json");
        }
        catch (IOException ex)
        {
            return AutomationSyntheticErrorResponse.Create(
                $"Automation pipe I/O failed ({ex.GetType().Name}): {ex.Message}",
                "pipe-io-error");
        }
        catch (OperationCanceledException ex)
        {
            return AutomationSyntheticErrorResponse.Create(
                $"Automation pipe request canceled: {ex.Message}",
                "pipe-canceled");
        }
    }

    public Task<JsonElement> SendCommandAsync(
        AutomationCommandKind kind,
        Dictionary<string, object?>? payload = null,
        int? responseTimeoutMs = null)
        => SendCommandAsync(AutomationCommandCatalog.Get(kind).Name, payload, responseTimeoutMs);

}
