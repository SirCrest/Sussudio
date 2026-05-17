using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationCommandDispatcher_IntrospectionCommands_LiveInFocusedPartial()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var introspectionCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.IntrospectionCommands.cs")
            .Replace("\r\n", "\n");

        AssertContains(customCommandsText, "case AutomationCommandKind.GetSnapshot:");
        AssertContains(customCommandsText, "ExecuteGetSnapshotCommandAsync(correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.GetAutomationManifest:");
        AssertContains(customCommandsText, "ExecuteGetAutomationManifestCommand(correlationId)");
        AssertDoesNotContain(customCommandsText, "_diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken)");
        AssertDoesNotContain(customCommandsText, "AutomationCommandCatalog.CreateManifest()");

        AssertContains(introspectionCommandsText, "private async Task<AutomationCommandResponse> ExecuteGetSnapshotCommandAsync(");
        AssertContains(introspectionCommandsText, "_diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken)");
        AssertContains(introspectionCommandsText, "Snapshot retrieved.");
        AssertContains(introspectionCommandsText, "private AutomationCommandResponse ExecuteGetAutomationManifestCommand(string correlationId)");
        AssertContains(introspectionCommandsText, "Automation manifest retrieved.");
        AssertContains(introspectionCommandsText, "AutomationCommandCatalog.CreateManifest()");
        AssertContains(introspectionCommandsText, "includeSnapshot: false");

        return Task.CompletedTask;
    }
}
