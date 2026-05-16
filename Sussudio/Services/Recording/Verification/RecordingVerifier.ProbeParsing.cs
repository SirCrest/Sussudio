using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sussudio.Services.Recording;

public sealed partial class RecordingVerifier
{
    private static Dictionary<string, string> ParseKeyValueOutput(string output)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = rawLine.IndexOf('=');
            if (idx <= 0 || idx >= rawLine.Length - 1)
            {
                continue;
            }

            var key = rawLine[..idx].Trim();
            var value = rawLine[(idx + 1)..].Trim();
            if (key.Length == 0)
            {
                continue;
            }

            values[key] = value;
        }

        return values;
    }

    private static uint? TryParseUInt(string? value)
    {
        return uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static double? TryParseRational(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        var slash = trimmed.IndexOf('/');
        if (slash > 0 && slash < trimmed.Length - 1)
        {
            var numRaw = trimmed[..slash];
            var denRaw = trimmed[(slash + 1)..];
            if (double.TryParse(numRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var numerator) &&
                double.TryParse(denRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var denominator) &&
                Math.Abs(denominator) > double.Epsilon)
            {
                return numerator / denominator;
            }
        }

        if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var direct))
        {
            return direct;
        }

        return null;
    }
}
