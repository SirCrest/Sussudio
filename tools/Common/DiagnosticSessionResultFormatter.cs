using System.Text;

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

    private static void AppendFlashbackSections(StringBuilder builder, DiagnosticSessionResult result)
    {
        AppendFlashbackPlaybackCommands(builder, result);
        AppendFlashbackPlaybackPerformance(builder, result);
        AppendFlashbackPlaybackDecode(builder, result);
        AppendFlashbackPlaybackStages(builder, result);
        AppendFlashbackRecording(builder, result);
        AppendFlashbackExport(builder, result);
    }

    private static void AppendPreviewSections(StringBuilder builder, DiagnosticSessionResult result)
    {
        AppendPreviewScheduler(builder, result);
        AppendPreviewD3DPerformance(builder, result);
        AppendPreviewD3DCpuTiming(builder, result);
        AppendPreviewVisualCadence(builder, result);
    }
}
