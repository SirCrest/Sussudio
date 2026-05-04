using System.Text.Json;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;

namespace McpServer.Tools;

internal static class McpToolResultFactory
{
    internal static CallToolResult FromResponse(JsonElement response, string text)
        => FromText(text, isError: !AutomationSnapshotFormatter.IsSuccess(response));

    internal static CallToolResult FromText(string text, bool isError = false)
        => new()
        {
            Content = [new TextContentBlock { Text = text }],
            IsError = isError
        };

    internal static string GetMessage(JsonElement response, string fallback = "Command failed.")
        => AutomationSnapshotFormatter.Get(response, "Message", fallback);
}
