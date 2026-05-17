using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationCommandDispatcher_UiSettingsCommands_LiveInFocusedPartial()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var uiSettingsCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.UiSettingsCommands.cs")
            .Replace("\r\n", "\n");

        AssertContains(customCommandsText, "case AutomationCommandKind.SetStatsSectionVisible:");
        AssertContains(customCommandsText, "ExecuteSetStatsSectionVisibleCommandAsync(payload, correlationId, cancellationToken)");
        AssertDoesNotContain(customCommandsText, "_viewModel.SetStatsSectionVisibleAsync");
        AssertDoesNotContain(customCommandsText, "Stats section '{section}' {(visible ? \"expanded\" : \"collapsed\")}.");

        AssertContains(uiSettingsCommandsText, "private async Task<AutomationCommandResponse> ExecuteSetStatsSectionVisibleCommandAsync(");
        AssertContains(uiSettingsCommandsText, "var section = RequireString(payload, \"section\");");
        AssertContains(uiSettingsCommandsText, "var visible = RequireBool(payload, \"visible\");");
        AssertContains(uiSettingsCommandsText, "_viewModel.SetStatsSectionVisibleAsync(section, visible, cancellationToken)");
        AssertContains(uiSettingsCommandsText, "Stats section '{section}' {(visible ? \"expanded\" : \"collapsed\")}.");

        return Task.CompletedTask;
    }
}
