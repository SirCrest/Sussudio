static partial class Program
{
    private static string ReadAutomationSnapshotFlatteningFamilyText()
        => string.Join(
            "\n",
            ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.Set.cs"),
            ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs"))
            .Replace("\r\n", "\n");
}
