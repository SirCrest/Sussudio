using System.Text;

namespace Sussudio.Tools;

public static partial class DiagnosticSessionResultFormatter
{
    private static void AppendArtifacts(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine($"Artifacts: {result.OutputDirectory}");
        builder.AppendLine($"  Live: {result.LivePath}");
        builder.AppendLine($"  Summary: {result.SummaryPath}");
        builder.AppendLine($"  Samples: {result.SamplesPath}");
        builder.AppendLine($"  Frame Ledger: {result.FrameLedgerPath}");
        builder.AppendLine($"  Timeline: {result.TimelinePath}");
    }

    private static void AppendActionsAndWarnings(StringBuilder builder, DiagnosticSessionResult result)
    {
        if (result.Actions.Length > 0)
        {
            builder.AppendLine($"Actions: {string.Join(", ", result.Actions)}");
        }

        if (result.Warnings.Length > 0)
        {
            builder.AppendLine("Warnings:");
            foreach (var warning in result.Warnings)
            {
                builder.AppendLine($"  {warning}");
            }
        }
    }
}
