using System.Text.Json;
using Sussudio.Tools;

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
        var effectiveTimeoutMs = responseTimeoutMs
            ?? _responseTimeoutOverrideMs
            ?? AutomationPipeProtocol.GetDefaultResponseTimeout(commandName);

        AutomationPipeCommandResult result;
        try
        {
            result = await AutomationPipeClient.SendCommandWithResultAsync(
                    _pipeName,
                    commandName,
                    payload,
                    AutomationPipeProtocol.DefaultConnectTimeoutMs,
                    effectiveTimeoutMs,
                    includeResponseElement: true)
                .ConfigureAwait(false);
        }
        catch (ArgumentException ex)
        {
            throw new UsageException(ex.Message);
        }

        return result.ResponseElement
            ?? throw new JsonException("Automation pipe returned invalid JSON.");
    }

}
