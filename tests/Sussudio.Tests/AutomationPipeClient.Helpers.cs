static partial class Program
{
    private static string ReadAutomationPipeClientSource()
        => string.Join(
                "\n",
                ReadRepoFile("tools/Common/AutomationPipeClient/AutomationPipeClient.Transport.cs"),
                ReadRepoFile("tools/Common/AutomationPipeClient/AutomationPipeClient.ConnectErrors.cs"),
                ReadRepoFile("tools/Common/AutomationPipeClient/AutomationPipeClient.Commands.cs"),
                ReadRepoFile("tools/Common/AutomationPipeClient/AutomationPipeClient.ResponseState.cs"),
                ReadRepoFile("tools/Common/AutomationPipeClient/AutomationPipeClient.Models.cs"),
                ReadRepoFile("tools/Common/AutomationPipeClient/AutomationSyntheticErrorResponse.cs"))
            .Replace("\r\n", "\n");
}
