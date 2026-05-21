using System.Globalization;
using System.Text;
using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;

namespace Sussudio.Tools;

public static partial class DiagnosticSessionResultFormatter
{
    private static void AppendCaptureMode(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Capture Mode: " +
            $"selected={FormatOptional(result.SelectedResolutionAtEnd)} @{FormatFrameRate(result.SelectedFrameRateAtEnd, result.SelectedFriendlyFrameRateAtEnd, result.SelectedExactFrameRateArgAtEnd)} " +
            $"format={FormatOptional(result.SelectedVideoFormatAtEnd)} requested={FormatOptional(result.VideoRequestedSubtypeAtEnd)} negotiated={FormatOptional(result.VideoNegotiatedSubtypeAtEnd)} " +
            $"source={result.SourceWidthAtEnd}x{result.SourceHeightAtEnd} @{FormatFrameRate(result.DetectedSourceFrameRateAtEnd, string.Empty, result.DetectedSourceFrameRateArgAtEnd)} " +
            $"hdr={result.SourceIsHdrAtEnd} telemetry={FormatOptional(result.SourceTelemetrySummaryAtEnd)}");
    }

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
