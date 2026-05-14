using System.Text.Json;

static partial class Program
{
    private static void AssertNoToolSchemaExposesPipeClient(JsonElement tools)
    {
        var checkedCount = 0;
        foreach (var tool in tools.EnumerateArray())
        {
            checkedCount++;
            var toolName = tool.GetProperty("name").GetString() ?? "<unnamed>";
            var inputSchema = tool.GetProperty("inputSchema");
            if (inputSchema.TryGetProperty("properties", out var properties) &&
                properties.TryGetProperty("pipeClient", out _))
            {
                throw new InvalidOperationException($"{toolName} exposes pipeClient in the MCP input schema.");
            }

            if (inputSchema.TryGetProperty("required", out var required))
            {
                foreach (var item in required.EnumerateArray())
                {
                    if (string.Equals(item.GetString(), "pipeClient", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException($"{toolName} requires pipeClient in the MCP input schema.");
                    }
                }
            }
        }

        if (checkedCount == 0)
        {
            throw new InvalidOperationException("MCP host did not list any tools.");
        }
    }

    private static string CompactJsonLine(string json)
    {
        using var document = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(document.RootElement);
    }

    private static void AssertCommandRequest(JsonElement request, string commandName, params (string Key, object? Value)[] expectedPayload)
    {
        AssertEqual(GetExpectedAutomationCommandValue(commandName), request.GetProperty("command").GetInt32(), $"{commandName} command id");
        var payload = request.GetProperty("payload");
        if (expectedPayload.Length == 0)
        {
            if (payload.ValueKind == JsonValueKind.Object && payload.EnumerateObject().Any())
            {
                throw new InvalidOperationException($"{commandName} payload contained unexpected properties.");
            }

            if (payload.ValueKind is not JsonValueKind.Null and not JsonValueKind.Object)
            {
                throw new InvalidOperationException($"{commandName} payload had unexpected kind {payload.ValueKind}.");
            }

            return;
        }

        AssertJsonObjectPropertyNames(payload, expectedPayload.Select(item => item.Key).ToArray());
        foreach (var (key, value) in expectedPayload)
        {
            AssertJsonPropertyEquals(payload, key, value, $"{commandName}.{key}");
        }
    }

    private static void AssertContainsOrdinal(string value, string token)
    {
        if (!value.Contains(token, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Assertion failed: expected '{value}' to contain '{token}' with ordinal casing.");
        }
    }

    private static void AssertJsonObjectPropertyNames(JsonElement element, params string[] expectedPropertyNames)
    {
        AssertEqual(JsonValueKind.Object, element.ValueKind, "JSON object property-name assertion kind");
        var actual = element.EnumerateObject()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var expected = expectedPropertyNames
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        AssertEqual(string.Join(",", expected), string.Join(",", actual), "JSON object property names");
    }

    private static int GetExpectedAutomationCommandValue(string commandName)
    {
        foreach (var (name, value) in ExpectedAutomationCommands())
        {
            if (string.Equals(name, commandName, StringComparison.Ordinal))
            {
                return value;
            }
        }

        throw new InvalidOperationException($"Expected automation command '{commandName}' was not found.");
    }

    private static void AssertJsonPropertyEquals(JsonElement element, string propertyName, object? expected, string fieldName)
    {
        if (!element.TryGetProperty(propertyName, out var actual))
        {
            throw new InvalidOperationException($"Assertion failed for {fieldName}: property was missing.");
        }

        switch (expected)
        {
            case null:
                AssertEqual(JsonValueKind.Null, actual.ValueKind, fieldName);
                break;
            case bool expectedBool:
                AssertEqual(expectedBool, actual.GetBoolean(), fieldName);
                break;
            case int expectedInt:
                AssertEqual(expectedInt, actual.GetInt32(), fieldName);
                break;
            case double expectedDouble:
                AssertEqual(expectedDouble, actual.GetDouble(), fieldName);
                break;
            case string expectedString:
                AssertEqual(expectedString, actual.GetString(), fieldName);
                break;
            default:
                throw new InvalidOperationException($"Unsupported expected JSON value type for {fieldName}: {expected.GetType().FullName}.");
        }
    }
}
