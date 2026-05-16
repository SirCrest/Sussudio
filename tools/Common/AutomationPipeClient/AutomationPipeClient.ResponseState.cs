using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationPipeClient
{
    internal static bool TryReadResponseState(
        string responseJson,
        out bool success,
        out string? status,
        out int? retryAfterMs)
        => TryReadResponseState(
            responseJson,
            includeResponseElement: false,
            out success,
            out status,
            out retryAfterMs,
            out _);

    private static bool TryReadResponseState(
        string responseJson,
        bool includeResponseElement,
        out bool success,
        out string? status,
        out int? retryAfterMs,
        out JsonElement? responseElement)
    {
        success = false;
        status = null;
        retryAfterMs = null;
        responseElement = null;

        try
        {
            using var responseDocument = JsonDocument.Parse(responseJson);
            var response = responseDocument.RootElement;
            var stateRead = AutomationResponseState.TryRead(
                response,
                out success,
                out status,
                out retryAfterMs);
            if (includeResponseElement)
            {
                responseElement = response.Clone();
            }

            return stateRead;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
