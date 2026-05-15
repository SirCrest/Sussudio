static partial class Program
{
    private static string ReadDiagnosticSessionFlashbackCycleScenariosSource()
    {
        var files = new[]
        {
            "tools/Common/DiagnosticSessionFlashbackCycleScenarios.cs",
            "tools/Common/DiagnosticSessionFlashbackCycleScenarios.Registrations.cs"
        };
        var parts = new string[files.Length];
        for (var i = 0; i < files.Length; i++)
        {
            parts[i] = ReadRepoFile(files[i]).Replace("\r\n", "\n");
        }

        return string.Join("\n", parts);
    }
}
