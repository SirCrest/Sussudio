using System;
using System.Collections.Generic;
using System.Text.Json;
using Sussudio.Models;

namespace Sussudio.Tools.Ssctl;

internal static partial class CommandHandlers
{
    private static async Task<int> HandleSimpleCommandAsync(
        CommandContext context,
        AutomationCommandKind kind,
        Dictionary<string, object?>? payload = null,
        bool includeData = false)
    {
        var response = await context.Transport.SendCommandAsync(kind, payload).ConfigureAwait(false);
        return WriteResponse(response, context.GlobalJson, value => Formatters.FormatResult(value, includeData));
    }

    private static int WriteResponse(JsonElement response, bool json, Func<JsonElement, string> formatter)
    {
        Console.WriteLine(json ? Formatters.PrettyJson(response) : formatter(response));
        return IsSuccess(response) ? 0 : 3;
    }

    private static bool IsSuccess(JsonElement response)
        => response.ValueKind == JsonValueKind.Object &&
           response.TryGetProperty("Success", out var success) &&
           success.ValueKind == JsonValueKind.True;
}
