using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationCommandDispatcher_CustomCommands_OwnStatsSectionVisibilityCommand()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");

        AssertContains(customCommandsText, "case AutomationCommandKind.SetStatsSectionVisible:");
        AssertContains(customCommandsText, "ExecuteSetStatsSectionVisibleCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "private async Task<AutomationCommandResponse> ExecuteSetStatsSectionVisibleCommandAsync(");
        AssertContains(customCommandsText, "var section = RequireString(payload, \"section\");");
        AssertContains(customCommandsText, "var visible = RequireBool(payload, \"visible\");");
        AssertContains(customCommandsText, "_viewModel.SetStatsSectionVisibleAsync(section, visible, cancellationToken)");
        AssertContains(customCommandsText, "Stats section '{section}' {(visible ? \"expanded\" : \"collapsed\")}.");

        return Task.CompletedTask;
    }
}
