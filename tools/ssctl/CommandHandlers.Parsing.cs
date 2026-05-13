using System.Globalization;
using System.Text.Json;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class CommandHandlers
{
    private static bool ConsumeFlag(List<string> args, string flag)
    {
        var index = args.FindIndex(arg => string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return false;
        }

        args.RemoveAt(index);
        return true;
    }

    private static int? ParseOptionalIntFlag(List<string> args, string flag)
    {
        var index = args.FindIndex(arg => string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return null;
        }

        if (index + 1 >= args.Count)
        {
            throw new UsageException($"Missing value for {flag}.");
        }

        var value = ParseInt(args[index + 1]);
        args.RemoveAt(index + 1);
        args.RemoveAt(index);
        return value;
    }

    private static string? ParseOptionalStringFlag(List<string> args, string flag)
    {
        var index = args.FindIndex(arg => string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return null;
        }

        if (index + 1 >= args.Count)
        {
            throw new UsageException($"Missing value for {flag}.");
        }

        var value = args[index + 1];
        args.RemoveAt(index + 1);
        args.RemoveAt(index);
        return value;
    }

    private static long? ParseOptionalLongFlag(List<string> args, string flag)
    {
        var index = args.FindIndex(arg => string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return null;
        }

        if (index + 1 >= args.Count)
        {
            throw new UsageException($"Missing value for {flag}.");
        }

        var value = ParseLong(args[index + 1]);
        args.RemoveAt(index + 1);
        args.RemoveAt(index);
        return value;
    }

    private static string PrettyJson<T>(T value)
        => JsonSerializer.Serialize(value, JsonOptions.Pretty);

    private static string RequireWord(IReadOnlyList<string> args, int index, string usage)
    {
        if (index >= args.Count || string.IsNullOrWhiteSpace(args[index]))
        {
            throw new UsageException(usage);
        }

        return args[index];
    }

    private static void EnsureArgCount(IReadOnlyList<string> args, int expected, string usage)
    {
        if (args.Count != expected)
        {
            throw new UsageException(usage);
        }
    }

    private static void EnsureNoArgs(IReadOnlyList<string> args, string usage)
    {
        if (args.Count != 0)
        {
            throw new UsageException(usage);
        }
    }

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

    private static bool LooksLikeJson(string value)
    {
        var trimmed = value.TrimStart();
        return trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal);
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

    private static string JoinRemaining(IReadOnlyList<string> args, int startIndex)
    {
        if (startIndex >= args.Count)
        {
            throw new UsageException("Missing required value.");
        }

        return JoinRange(args, startIndex, args.Count);
    }

    private static string JoinRange(IReadOnlyList<string> args, int startIndex, int endExclusive)
        => string.Join(" ", args.Skip(startIndex).Take(endExclusive - startIndex));

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
