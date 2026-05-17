using System.Text.Json;
using Sussudio.Models;

namespace Sussudio.Tools;

internal enum AutomationUnknownCommandHandling
{
    ReturnSyntheticError,
    ThrowArgumentException
}

internal static class AutomationCommandTransport
{
    public static async Task<JsonElement> SendCommandAsync(
        string pipeName,
        AutomationCommandKind kind,
        object? payload = null,
        int? responseTimeoutOverrideMs = null,
        int? responseTimeoutMs = null,
        AutomationUnknownCommandHandling unknownCommandHandling = AutomationUnknownCommandHandling.ReturnSyntheticError)
    {
        var effectiveResponseTimeoutMs = responseTimeoutMs
            ?? responseTimeoutOverrideMs
            ?? AutomationPipeProtocol.GetDefaultResponseTimeout(kind);

        try
        {
            var result = await AutomationPipeClient.SendCommandWithResultAsync(
                    pipeName,
                    kind,
                    payload,
                    AutomationPipeProtocol.DefaultConnectTimeoutMs,
                    effectiveResponseTimeoutMs,
                    includeResponseElement: true)
                .ConfigureAwait(false);

            return result.ResponseElement
                ?? throw new JsonException("Automation pipe returned invalid JSON.");
        }
        catch (ArgumentException ex) when (unknownCommandHandling == AutomationUnknownCommandHandling.ReturnSyntheticError)
        {
            return AutomationSyntheticErrorResponse.Create(ex.Message, "unknown-command");
        }
        catch (Exception ex) when (AutomationSyntheticErrorResponse.CanCreateFromException(ex))
        {
            return AutomationSyntheticErrorResponse.Create(ex);
        }
    }

    public static async Task<JsonElement> SendCommandAsync(
        string pipeName,
        string commandName,
        object? payload = null,
        int? responseTimeoutOverrideMs = null,
        int? responseTimeoutMs = null,
        AutomationUnknownCommandHandling unknownCommandHandling = AutomationUnknownCommandHandling.ReturnSyntheticError)
    {
        var effectiveResponseTimeoutMs = responseTimeoutMs
            ?? responseTimeoutOverrideMs
            ?? AutomationPipeProtocol.GetDefaultResponseTimeout(commandName);

        try
        {
            var result = await AutomationPipeClient.SendCommandWithResultAsync(
                    pipeName,
                    commandName,
                    payload,
                    AutomationPipeProtocol.DefaultConnectTimeoutMs,
                    effectiveResponseTimeoutMs,
                    includeResponseElement: true)
                .ConfigureAwait(false);

            return result.ResponseElement
                ?? throw new JsonException("Automation pipe returned invalid JSON.");
        }
        catch (ArgumentException ex) when (unknownCommandHandling == AutomationUnknownCommandHandling.ReturnSyntheticError)
        {
            return AutomationSyntheticErrorResponse.Create(ex.Message, "unknown-command");
        }
        catch (Exception ex) when (AutomationSyntheticErrorResponse.CanCreateFromException(ex))
        {
            return AutomationSyntheticErrorResponse.Create(ex);
        }
    }
}
