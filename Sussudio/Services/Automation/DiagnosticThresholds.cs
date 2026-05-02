using System;

namespace Sussudio.Services.Automation;

internal static class DiagnosticThresholds
{
    public const int RendererDropWarningMinSamples = 120;
    public const double RendererDropWarningPercent = 0.25;

    public static double CalculatePercent(long numerator, long denominator)
    {
        return denominator > 0
            ? Math.Max(0, numerator) / (double)denominator * 100.0
            : 0.0;
    }
}
