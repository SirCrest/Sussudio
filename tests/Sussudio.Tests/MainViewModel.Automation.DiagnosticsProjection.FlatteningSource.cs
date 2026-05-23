static partial class Program
{
    private static string ReadAutomationSnapshotFlatteningFamilyText()
        => string.Join(
            "\n",
            ReadAutomationSnapshotFlatteningOrchestrationText(),
            ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs"))
            .Replace("\r\n", "\n");

    private static string ReadAutomationSnapshotFlatteningOrchestrationText()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        const string startToken = "private static AutomationSnapshotFlattenedProjectionSet BuildAutomationSnapshotFlattenedProjectionSet(";
        const string endToken = "private SnapshotStatusProjection BuildSnapshotStatusProjection(";
        var startIndex = snapshotProjectionText.IndexOf(startToken, System.StringComparison.Ordinal);
        var endIndex = snapshotProjectionText.IndexOf(endToken, System.StringComparison.Ordinal);
        if (startIndex < 0 || endIndex <= startIndex)
        {
            throw new System.InvalidOperationException("Unable to locate automation snapshot flattening orchestration in the root snapshot projection file.");
        }

        return snapshotProjectionText[startIndex..endIndex];
    }
}
