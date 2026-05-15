static partial class Program
{
    private static string ReadSsctlCommandHandlersFamilyText()
    {
        var files = new[]
        {
            "tools/ssctl/CommandHandlers.cs",
            "tools/ssctl/CommandHandlers.Arguments.cs",
            "tools/ssctl/CommandHandlers.AutomationFlow.cs",
            "tools/ssctl/CommandHandlers.CaptureControls.cs",
            "tools/ssctl/CommandHandlers.Context.cs",
            "tools/ssctl/CommandHandlers.Device.cs",
            "tools/ssctl/CommandHandlers.DiagnosticSession.cs",
            "tools/ssctl/CommandHandlers.Flashback.cs",
            "tools/ssctl/CommandHandlers.Flashback.Export.cs",
            "tools/ssctl/CommandHandlers.Flags.cs",
            "tools/ssctl/CommandHandlers.Json.cs",
            "tools/ssctl/CommandHandlers.Observability.cs",
            "tools/ssctl/CommandHandlers.PresentMon.cs",
            "tools/ssctl/CommandHandlers.Recordings.cs",
            "tools/ssctl/CommandHandlers.Transport.cs",
            "tools/ssctl/CommandHandlers.Values.cs",
            "tools/ssctl/CommandHandlers.Window.cs",
        };

        return string.Join(
            "\n",
            files.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
    }
}
