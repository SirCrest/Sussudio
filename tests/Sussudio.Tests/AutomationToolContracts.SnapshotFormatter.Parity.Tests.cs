using System;
using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static Task SsctlFormatters_SnapshotFields_AlignWithMcpResponseFormatter()
    {
        var mcpText = ReadAutomationSnapshotFormatterSource();
        var ssctlText = ReadSsctlSnapshotFormatterSource();

        var mcpFields = ExtractSnapshotFields(mcpText);
        var ssctlFields = ExtractSnapshotFields(ssctlText);

        if (mcpFields.Count == 0)
            throw new InvalidOperationException("Failed to extract any snapshot fields from AutomationSnapshotFormatter.");
        if (ssctlFields.Count == 0)
            throw new InvalidOperationException("Failed to extract any snapshot fields from ssctl Formatters.");

        var missingInSsctl = new List<string>();
        foreach (var field in mcpFields)
        {
            if (!ssctlFields.Contains(field))
                missingInSsctl.Add(field);
        }

        if (missingInSsctl.Count > 0)
        {
            throw new InvalidOperationException(
                $"AutomationSnapshotFormatter references {missingInSsctl.Count} snapshot field(s) " +
                $"missing from ssctl Formatters: {string.Join(", ", missingInSsctl)}");
        }

        return Task.CompletedTask;
    }
}
