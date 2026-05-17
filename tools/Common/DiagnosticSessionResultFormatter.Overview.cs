using System.Text;

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
}
