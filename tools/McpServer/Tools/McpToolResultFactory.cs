using System.Text.Json;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;

namespace McpServer.Tools;

// Creates MCP CallToolResult objects from automation responses.
internal static class McpToolResultFactory
{
    internal static CallToolResult FromResponse(JsonElement response, string text)
    {
        var isError = !AutomationSnapshotFormatter.IsSuccess(response);
        if (isError)
        {
            var errorCode = AutomationSnapshotFormatter.Get(response, "ErrorCode", string.Empty);
            if (!string.IsNullOrWhiteSpace(errorCode) &&
                !text.Contains(errorCode, StringComparison.OrdinalIgnoreCase))
            {
                text = $"{text}{Environment.NewLine}ErrorCode: {errorCode}";
            }
        }

        return FromText(text, isError);
    }

    internal static CallToolResult FromText(string text, bool isError = false)
        => new()
        {
            Content = [new TextContentBlock { Text = text }],
            IsError = isError
        };

    internal static string GetMessage(JsonElement response, string fallback = "Command failed.")
        => AutomationSnapshotFormatter.Get(response, "Message", fallback);
}
