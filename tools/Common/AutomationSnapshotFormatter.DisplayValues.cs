using System.Globalization;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
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

    internal static long ComputeTickAgeMs(long tickMs)
    {
        if (tickMs <= 0)
        {
            return -1;
        }

        return Math.Max(0, Environment.TickCount64 - tickMs);
    }
}
