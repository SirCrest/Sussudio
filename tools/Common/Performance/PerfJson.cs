using System.Text.Json;

namespace Sussudio.Tools.Performance;

// Tolerant readers for automation responses and perf reports. The wire format is PascalCase today; matching property
// names case-insensitively keeps these tools working if a producer ever switches to camelCase, and a missing or
// differently shaped field reads as "absent" instead of throwing, because experiment statistics must degrade to a
// warning rather than abort a run that has already changed the app.
internal static class PerfJson
{
    public static bool TryGet(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    public static JsonElement? Get(JsonElement element, string name)
        => TryGet(element, name, out var value) ? value : null;

    public static string? GetString(JsonElement element, string name)
        => TryGet(element, name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    public static bool? GetBool(JsonElement element, string name)
        => TryGet(element, name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;

    /// <summary>A finite number, or null when the property is absent, not a number, or not finite.</summary>
    public static double? GetDouble(JsonElement element, string name)
    {
        if (TryGet(element, name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number)
            && double.IsFinite(number))
        {
            return number;
        }

        return null;
    }

    public static IEnumerable<JsonElement> GetArray(JsonElement element, string name)
        => TryGet(element, name, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray()
            : Enumerable.Empty<JsonElement>();
}
