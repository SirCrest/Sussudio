using System;

namespace Sussudio.ViewModels;

internal static partial class StatsPresentationBuilder
{
    public static string FormatMs(double value)
    {
        return $"{Sanitize(value):0.00}ms";
    }

    public static double Sanitize(double value)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            return 0;
        }

        return value;
    }

    private static string FormatFps(double value)
    {
        return Sanitize(value).ToString("0.00");
    }

    private static string FormatSourceHdr(bool? isHdr, string? colorimetry)
        => DisplayFormatters.FormatSourceHdr(isHdr, colorimetry);

    private static string FormatFrameBudgetMs(double expectedFps)
    {
        expectedFps = Sanitize(expectedFps);
        return expectedFps > 0 ? $"{1000.0 / expectedFps:0.00}ms" : "\u2014";
    }

    private static string FormatPercent(double value)
    {
        return $"{Sanitize(value):0.0}%";
    }

    private static string FormatScore(double value)
    {
        return Sanitize(value).ToString("0.0");
    }

    private static string FormatCount(long value)
    {
        return Math.Max(0, value).ToString("N0");
    }

    private static string FormatSignedMs(double? value)
    {
        if (!value.HasValue || double.IsNaN(value.Value))
        {
            return "\u2014";
        }

        return value.Value >= 0 ? $"+{value.Value:F1}ms" : $"{value.Value:F1}ms";
    }

    private static string FormatSignedMsPerSec(double? value)
    {
        if (!value.HasValue || double.IsNaN(value.Value))
        {
            return "\u2014";
        }

        return value.Value >= 0 ? $"+{value.Value:F2} ms/s" : $"{value.Value:F2} ms/s";
    }
}
