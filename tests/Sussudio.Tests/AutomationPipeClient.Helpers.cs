static partial class Program
{
    private static string ReadAutomationPipeClientSource()
        => string.Join(
                "\n",
                ReadRepoFile("tools/Common/AutomationPipeClient.cs"),
                ReadRepoFile("tools/Common/AutomationPipeClient.Transport.cs"),
                ReadRepoFile("tools/Common/AutomationPipeClient.Commands.cs"),
                ReadRepoFile("tools/Common/AutomationPipeClient.ResponseState.cs"),
                ReadRepoFile("tools/Common/AutomationPipeClient.Models.cs"))
            .Replace("\r\n", "\n");
}
