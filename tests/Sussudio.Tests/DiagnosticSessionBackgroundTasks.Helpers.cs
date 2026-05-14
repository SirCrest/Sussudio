static partial class Program
{
    private static string ReadDiagnosticSessionBackgroundTasksSource()
    {
        return string.Join(
            "\n",
            new[]
            {
                ReadRepoFile("tools/Common/DiagnosticSessionBackgroundTasks.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionBackgroundTasks.FaultDrain.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionBackgroundTasks.Models.cs")
            }).Replace("\r\n", "\n");
    }
}
