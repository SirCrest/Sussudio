using System.Globalization;
using System.Text;
using System.Text.Json;

// AutomationClient payload construction stays isolated from command resolution
// so wire shape changes remain easy to spot in low-level client diffs.
internal static partial class Program
{
    private static object BuildPayload(Options options)
    {
        if (!string.IsNullOrWhiteSpace(options.PayloadBase64))
        {
            if (options.PayloadKv.Count > 0 ||
                !string.Equals(options.PayloadJson, "{}", StringComparison.Ordinal))
            {
                throw new ArgumentException("Use only one of --payload, --payload-base64, or --payload-kv.");
            }

            var payloadBytes = Convert.FromBase64String(options.PayloadBase64);
            var payloadJson = Encoding.UTF8.GetString(payloadBytes);
            using var decodedPayloadDocument = JsonDocument.Parse(string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson);
            return decodedPayloadDocument.RootElement.Clone();
        }

        if (options.PayloadKv.Count > 0)
        {
            if (!string.Equals(options.PayloadJson, "{}", StringComparison.Ordinal))
            {
                throw new ArgumentException("Use either --payload or --payload-kv, not both.");
            }

            var payload = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var entry in options.PayloadKv)
            {
                var separatorIndex = entry.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    throw new ArgumentException($"Invalid --payload-kv entry '{entry}'. Expected key=value.");
                }

                var key = entry.Substring(0, separatorIndex).Trim();
                var rawValue = entry[(separatorIndex + 1)..].Trim();
                if (string.IsNullOrWhiteSpace(key))
                {
                    throw new ArgumentException($"Invalid --payload-kv entry '{entry}'. Key is empty.");
                }

                payload[key] = ParsePayloadValue(rawValue);
            }

            return payload;
        }

        using var payloadDocument = JsonDocument.Parse(string.IsNullOrWhiteSpace(options.PayloadJson) ? "{}" : options.PayloadJson);
        return payloadDocument.RootElement.Clone();
    }

    private static object? ParsePayloadValue(string rawValue)
    {
        if (string.Equals(rawValue, "null", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if ((rawValue.StartsWith("\"", StringComparison.Ordinal) && rawValue.EndsWith("\"", StringComparison.Ordinal)) ||
            (rawValue.StartsWith("'", StringComparison.Ordinal) && rawValue.EndsWith("'", StringComparison.Ordinal)))
        {
            rawValue = rawValue[1..^1];
        }

        if (bool.TryParse(rawValue, out var boolValue))
        {
            return boolValue;
        }

        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
        {
            return intValue;
        }

        if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
        {
            return doubleValue;
        }

        if ((rawValue.StartsWith("{", StringComparison.Ordinal) && rawValue.EndsWith("}", StringComparison.Ordinal)) ||
            (rawValue.StartsWith("[", StringComparison.Ordinal) && rawValue.EndsWith("]", StringComparison.Ordinal)))
        {
            using var jsonValueDocument = JsonDocument.Parse(rawValue);
            return jsonValueDocument.RootElement.Clone();
        }

        return rawValue;
    }
}
