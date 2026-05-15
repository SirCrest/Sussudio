using System.Globalization;

namespace Sussudio.Tools.Ssctl;

internal static partial class CommandHandlers
{
    private static int ParseInt(string value)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new UsageException($"Invalid integer value '{value}'.");
        }

        return parsed;
    }

    private static long ParseLong(string value)
    {
        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new UsageException($"Invalid integer value '{value}'.");
        }

        return parsed;
    }

    private static double ParseDouble(string value)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new UsageException($"Invalid numeric value '{value}'.");
        }

        return parsed;
    }

    private static double ParseFlashbackPositionMs(string value)
    {
        var parsed = ParseDouble(value);
        if (!double.IsFinite(parsed) || parsed < 0 || parsed > TimeSpan.MaxValue.TotalMilliseconds)
        {
            throw new UsageException("Flashback position must be finite, non-negative, and within TimeSpan range.");
        }

        return parsed;
    }

    private static double ParseFlashbackExportSeconds(string value)
    {
        var parsed = ParseDouble(value);
        if (!double.IsFinite(parsed) || parsed <= 0 || parsed > TimeSpan.MaxValue.TotalSeconds)
        {
            throw new UsageException("Flashback export seconds must be finite, greater than zero, and within TimeSpan range.");
        }

        return parsed;
    }

    private static object? ParseAssertionValue(string value)
    {
        if (bool.TryParse(value, out var boolValue))
        {
            return boolValue;
        }

        if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
        {
            return longValue;
        }

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
        {
            return doubleValue;
        }

        return value;
    }

    private static bool ParseOnOff(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "on" or "true" or "1" => true,
            "off" or "false" or "0" => false,
            _ => throw new UsageException($"Invalid boolean value '{value}'. Use on/off, true/false, or 1/0.")
        };
    }

    private static bool ParseShowHide(string value, string usage)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "show" => true,
            "hide" => false,
            _ => throw new UsageException(usage)
        };
    }

    private static string NormalizeRecordingFormat(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "h264" or "h.264" or "avc" => "H.264",
            "hevc" or "h265" or "h.265" => "HEVC",
            "av1" => "AV1",
            _ => value, // pass through as-is for server-side validation
        };
    }

    private static string MapSnapAction(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "left" => "SnapLeft",
            "right" => "SnapRight",
            "top-left" => "SnapTopLeft",
            "top-right" => "SnapTopRight",
            "bottom-left" => "SnapBottomLeft",
            "bottom-right" => "SnapBottomRight",
            _ => throw new UsageException("window snap left|right|top-left|top-right|bottom-left|bottom-right")
        };
    }

    private static string Capitalize(string value)
        => char.ToUpperInvariant(value[0]) + value[1..];
}
