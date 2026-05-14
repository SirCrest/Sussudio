using System.Globalization;

namespace Sussudio.Tools;

public static partial class DiagnosticSessionResultFormatter
{
    private static string FormatFrameRate(double fps, string friendlyFps, string exactArg)
    {
        var display = !string.IsNullOrWhiteSpace(friendlyFps)
            ? friendlyFps
            : fps > 0
                ? fps.ToString("0.###", CultureInfo.InvariantCulture)
                : "0";
        return !string.IsNullOrWhiteSpace(exactArg)
            ? $"{display}fps ({exactArg})"
            : $"{display}fps";
    }
}
