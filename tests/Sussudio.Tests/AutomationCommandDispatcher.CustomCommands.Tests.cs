using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationCommandDispatcher_UiSettingsCommands_OwnUiSettingsApplication()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var uiSettingsCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.UiSettingsCommands.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(customCommandsText, "case AutomationCommandKind.SetStatsSectionVisible:");
        AssertContains(uiSettingsCommandsText, "private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler> UiSettingsHandlers");
        AssertContains(uiSettingsCommandsText, "[AutomationCommandKind.SetShowAllCaptureOptions]");
        AssertContains(uiSettingsCommandsText, "[AutomationCommandKind.SetPreviewVolume]");
        AssertContains(uiSettingsCommandsText, "[AutomationCommandKind.SetStatsVisible]");
        AssertContains(uiSettingsCommandsText, "[AutomationCommandKind.SetSettingsVisible]");
        AssertContains(uiSettingsCommandsText, "[AutomationCommandKind.SetFrameTimeOverlayVisible]");
        AssertContains(uiSettingsCommandsText, "[AutomationCommandKind.SetFlashbackTimelineVisible]");
        AssertContains(uiSettingsCommandsText, "if (command == AutomationCommandKind.SetStatsSectionVisible)");
        AssertContains(uiSettingsCommandsText, "ExecuteSetStatsSectionVisibleCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(uiSettingsCommandsText, "private async Task<AutomationCommandResponse> ExecuteSetStatsSectionVisibleCommandAsync(");
        AssertContains(uiSettingsCommandsText, "var section = RequireString(payload, \"section\");");
        AssertContains(uiSettingsCommandsText, "var visible = RequireBool(payload, \"visible\");");
        AssertContains(uiSettingsCommandsText, "_viewModel.SetStatsSectionVisibleAsync(section, visible, cancellationToken)");
        AssertContains(uiSettingsCommandsText, "Stats section '{section}' {(visible ? \"expanded\" : \"collapsed\")}.");

        return Task.CompletedTask;
    }
}
