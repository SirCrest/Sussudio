using System.Text;
using static Sussudio.Tools.DiagnosticSessionText;

namespace Sussudio.Tools;

public static partial class DiagnosticSessionResultFormatter
{
    private static void AppendOverview(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine($"== Diagnostic Session: {(result.Success ? "PASS" : "FAIL")} ==");
        builder.AppendLine($"Scenario: {result.Scenario} | Duration: {result.DurationSeconds}s | Samples: {result.SampleCount} @ {result.SampleIntervalMs}ms");
        builder.AppendLine($"Terminal: {result.TerminalState} | LastStage: {result.LastStage} | RunnerPid: {result.RunnerProcessId}");
        if (!string.IsNullOrWhiteSpace(result.UnhandledException))
        {
            builder.AppendLine($"Terminal Exception: {result.UnhandledException}");
        }

        builder.AppendLine($"Health: {result.HealthStatus} | Stage: {result.LikelyStage}");
        if (!string.IsNullOrWhiteSpace(result.Summary))
        {
            builder.AppendLine($"Summary: {result.Summary}");
        }

        if (!string.IsNullOrWhiteSpace(result.Evidence))
        {
            builder.AppendLine($"Evidence: {result.Evidence}");
        }
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
}
