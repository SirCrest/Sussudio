static partial class Program
{
    private static string ReadDiagnosticSessionCleanupActionsSource()
    {
        return string.Join(
            "\n",
            new[]
            {
                ReadRepoFile("tools/Common/DiagnosticSessionCleanupActions.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionCleanupActions.Recording.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionCleanupActions.StateRestore.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionCleanupActions.Models.cs")
            }).Replace("\r\n", "\n");
    }
}
