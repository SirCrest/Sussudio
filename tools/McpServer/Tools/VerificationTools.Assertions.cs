using System.Text.Json;

namespace McpServer.Tools;

public static partial class VerificationTools
{
    private static bool TryParseAssertionArray(string assertions, out JsonElement parsedAssertions, out string? error)
    {
        parsedAssertions = default;
        error = null;

        if (string.IsNullOrWhiteSpace(assertions))
        {
            error = "The assertions parameter must be a JSON array string.";
            return false;
        }

        try
        {
            using var assertionsDocument = JsonDocument.Parse(assertions);
            if (assertionsDocument.RootElement.ValueKind != JsonValueKind.Array)
            {
                error = "The assertions parameter must be a JSON array string.";
                return false;
            }

            parsedAssertions = assertionsDocument.RootElement.Clone();
            return true;
        }
        catch (JsonException ex)
        {
            error = $"Invalid assertions JSON: {ex.Message}";
            return false;
        }
    }
}
