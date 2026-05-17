using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationCommandDispatcher_WindowCommands_LiveInFocusedPartial()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var windowCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.WindowCommands.cs")
            .Replace("\r\n", "\n");
        var windowActionsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.WindowActions.cs")
            .Replace("\r\n", "\n");

        AssertContains(customCommandsText, "case AutomationCommandKind.SetFullScreenEnabled:");
        AssertContains(customCommandsText, "ExecuteSetFullScreenEnabledCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.OpenRecordingsFolder:");
        AssertContains(customCommandsText, "ExecuteOpenRecordingsFolderCommandAsync(correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.ArmClose:");
        AssertContains(customCommandsText, "ExecuteArmCloseCommand(payload, correlationId)");
        AssertContains(customCommandsText, "case AutomationCommandKind.WindowAction:");
        AssertContains(customCommandsText, "ExecuteWindowActionCommandAsync(payload, correlationId, cancellationToken)");

        AssertDoesNotContain(customCommandsText, "_windowControl.SetFullScreenEnabledAsync");
        AssertDoesNotContain(customCommandsText, "_windowControl.OpenRecordingsFolderAsync");
        AssertDoesNotContain(customCommandsText, "_closeArmed");
        AssertDoesNotContain(customCommandsText, "window-close-not-armed");

        AssertContains(windowCommandsText, "_windowControl.SetFullScreenEnabledAsync(enabled, cancellationToken)");
        AssertContains(windowCommandsText, "Full screen {(enabled ? \"enter\" : \"exit\")} requested.");
        AssertContains(windowCommandsText, "_windowControl.OpenRecordingsFolderAsync(cancellationToken)");
        AssertContains(windowCommandsText, "Recordings folder open requested.");
        AssertContains(windowCommandsText, "var armed = GetBool(payload, \"armed\") ?? true;");
        AssertContains(windowCommandsText, "_closeArmed = armed;");
        AssertContains(windowCommandsText, "Window close arm state requested: {(armed ? \"armed\" : \"disarmed\")}.");
        AssertContains(windowCommandsText, "if (action == AutomationWindowAction.Close)");
        AssertContains(windowCommandsText, "window-close-not-armed");
        AssertContains(windowCommandsText, "await ExecuteWindowActionAsync(action, cancellationToken).ConfigureAwait(false);");
        AssertContains(windowCommandsText, "await ExecuteWindowActionAsync(action, cancellationToken, payload).ConfigureAwait(false);");

        AssertContains(windowActionsText, "private async Task ExecuteWindowActionAsync(");
        AssertDoesNotContain(windowActionsText, "Window close is disallowed until ArmClose is requested.");

        return Task.CompletedTask;
    }
}
