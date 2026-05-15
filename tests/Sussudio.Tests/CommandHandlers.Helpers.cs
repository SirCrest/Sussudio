static partial class Program
{
    private static string ReadSsctlCommandHandlersFamilyText()
    {
        var files = new[]
        {
            "tools/ssctl/CommandHandlers.cs",
            "tools/ssctl/CommandHandlers.AutomationFlow.cs",
            "tools/ssctl/CommandHandlers.CaptureControls.cs",
            "tools/ssctl/CommandHandlers.Context.cs",
            "tools/ssctl/CommandHandlers.DeviceWindow.cs",
            "tools/ssctl/CommandHandlers.Flashback.cs",
            "tools/ssctl/CommandHandlers.Observability.cs",
            "tools/ssctl/CommandHandlers.Parsing.cs",
            "tools/ssctl/CommandHandlers.Transport.cs"
        };

        return string.Join(
            "\n",
            files.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
    }
}
