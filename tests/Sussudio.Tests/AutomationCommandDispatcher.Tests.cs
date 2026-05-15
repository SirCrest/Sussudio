static partial class Program
{
    private static string ReadAutomationCommandDispatcherFamilyText()
    {
        var files = new[]
        {
            "Sussudio/Services/Automation/AutomationCommandDispatcher.cs",
            "Sussudio/Services/Automation/AutomationCommandDispatcher.TrivialHandlers.cs",
            "Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs",
            "Sussudio/Services/Automation/AutomationCommandDispatcher.FlashbackCommands.cs",
            "Sussudio/Services/Automation/AutomationCommandDispatcher.Payload.cs",
            "Sussudio/Services/Automation/AutomationCommandDispatcher.CommandParsing.cs",
            "Sussudio/Services/Automation/AutomationCommandDispatcher.WindowActions.cs",
            "Sussudio/Services/Automation/AutomationCommandDispatcher.WaitConditions.cs",
            "Sussudio/Services/Automation/AutomationCommandDispatcher.Assertions.cs",
            "Sussudio/Services/Automation/AutomationCommandDispatcher.Authorization.cs",
            "Sussudio/Services/Automation/AutomationCommandDispatcher.Responses.cs"
        };

        return string.Join(
            "\n",
            files.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
    }
}
