using System.Globalization;
using System.Text;
using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;

namespace Sussudio.Tools;

public static partial class DiagnosticSessionResultFormatter
{
    public static string Format(DiagnosticSessionResult result)
    {
        var builder = new StringBuilder();
        AppendOverview(builder, result);
        AppendCaptureMode(builder, result);
        AppendRecordingVerification(builder, result);
        AppendPresentMon(builder, result);
        AppendFlashbackSections(builder, result);
        AppendPreviewSections(builder, result);
        AppendProcessPerformance(builder, result);
        AppendArtifacts(builder, result);
        AppendActionsAndWarnings(builder, result);
        return builder.ToString().TrimEnd();
    }

    private static void AppendPresentMon(StringBuilder builder, DiagnosticSessionResult result)
    {
        if (result.PresentMon is not null)
        {
            builder.AppendLine($"PresentMon: {(result.PresentMon.Success ? "PASS" : "FAIL")} | {result.PresentMon.Message}");
        }
    }

    private static void AppendProcessPerformance(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Process Perf: " +
            $"cpuPercentEnd={result.ProcessCpuPercentAtEnd:0.##} " +
            $"cpuPercentMaxObserved={result.ProcessCpuMaxPercentObserved:0.##}");
    }

    private static void AppendCaptureMode(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Capture Mode: " +
            $"selected={FormatOptional(result.SelectedResolutionAtEnd)} @{FormatFrameRate(result.SelectedFrameRateAtEnd, result.SelectedFriendlyFrameRateAtEnd, result.SelectedExactFrameRateArgAtEnd)} " +
            $"format={FormatOptional(result.SelectedVideoFormatAtEnd)} requested={FormatOptional(result.VideoRequestedSubtypeAtEnd)} negotiated={FormatOptional(result.VideoNegotiatedSubtypeAtEnd)} " +
            $"source={result.SourceWidthAtEnd}x{result.SourceHeightAtEnd} @{FormatFrameRate(result.DetectedSourceFrameRateAtEnd, string.Empty, result.DetectedSourceFrameRateArgAtEnd)} " +
            $"hdr={result.SourceIsHdrAtEnd} telemetry={FormatOptional(result.SourceTelemetrySummaryAtEnd)}");
    }

    private static void AppendRecordingVerification(StringBuilder builder, DiagnosticSessionResult result)
    {
        if (result.RecordingVerificationRun)
        {
            var status = result.RecordingVerificationSucceeded == true ? "PASS" : "FAIL";
            builder.AppendLine($"Recording Verification: {status} | {result.RecordingVerificationMessage}");
        }
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
