using System.Text.Json;

namespace Sussudio.Tools;

internal static class AutomationSyntheticErrorResponse
{
    public static JsonElement Create(string message, string errorCode)
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

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(response));
        return document.RootElement.Clone();
    }
}
