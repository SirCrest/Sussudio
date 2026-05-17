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
