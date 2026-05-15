using System.Globalization;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    internal static bool IsSuccess(JsonElement response)
    {
        return response.ValueKind == JsonValueKind.Object &&
               response.TryGetProperty("Success", out var success) &&
               success.ValueKind == JsonValueKind.True;
    }

    internal static string Get(JsonElement element, string propertyName, string fallback = "N/A")
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var value))
        {
            return fallback;
        }

        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return fallback;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? fallback,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.Array => value.GetArrayLength() == 0 ? fallback : value.ToString(),
            JsonValueKind.Object => value.ToString(),
            _ => fallback
        };
    }

    internal static int GetInt(JsonElement element, string propertyName, int fallback = 0)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var numeric))
                return numeric;

            if (value.ValueKind == JsonValueKind.String &&
                int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                return parsed;
        }

        return fallback;
    }

    internal static double GetDouble(JsonElement element, string propertyName, double fallback = 0.0)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var numeric))
                return numeric;

            if (value.ValueKind == JsonValueKind.String &&
                double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                return parsed;
        }

        return fallback;
    }

    internal static string FormatFrameBudgetMs(JsonElement element, string fpsPropertyName, string fallback = "N/A")
    {
        var fps = GetDouble(element, fpsPropertyName);
        return fps > 0 ? $"{FormatNumber(1000.0 / fps, "0.00")}ms" : fallback;
    }

    internal static string FormatIntervalMs(JsonElement element, string propertyName, string fallback = "N/A")
    {
        var intervalMs = GetDouble(element, propertyName);
        return intervalMs > 0 ? $"{FormatNumber(intervalMs, "0.##")}ms" : fallback;
    }

    internal static string FormatBytes(long bytes)
    {
        if (bytes < 0)
        {
            return "N/A";
        }

        if (bytes >= 1024L * 1024L * 1024L)
        {
            return $"{FormatNumber(bytes / (1024.0 * 1024.0 * 1024.0), "0.##")} GB";
        }

        if (bytes >= 1024L * 1024L)
        {
            return $"{FormatNumber(bytes / (1024.0 * 1024.0), "0.##")} MB";
        }

        if (bytes >= 1024L)
        {
            return $"{FormatNumber(bytes / 1024.0, "0.##")} KB";
        }

        return $"{bytes} B";
    }

    internal static string FormatNumber(double value, string format)
        => value.ToString(format, CultureInfo.InvariantCulture);

    internal static long GetLong(JsonElement element, string propertyName, long fallback = 0)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var numeric))
            {
                return numeric;
            }

            if (value.ValueKind == JsonValueKind.String &&
                long.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }

        return fallback;
    }

    internal static long? GetNullableLong(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var value) ||
            value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var numeric))
        {
            return numeric;
        }

        return long.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    internal static bool GetBool(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(value.GetString(), out var parsed) && parsed,
            _ => false
        };
    }

    internal static string? GetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var value) ||
            value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : value.ToString();
    }

    internal static long ComputeTickAgeMs(long tickMs)
    {
        if (tickMs <= 0)
        {
            return -1;
        }

        return Math.Max(0, Environment.TickCount64 - tickMs);
    }
}
