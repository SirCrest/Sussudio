static partial class Program
{
    private static string ReadDiagnosticSessionFlashbackExportScenariosSource()
    {
        var files = new[]
        {
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.Concurrent.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.DisableDuringExport.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.Registrations.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.Rotated.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.Playback.cs",
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.Range.cs"
        };
        var parts = new string[files.Length];
        for (var i = 0; i < files.Length; i++)
        {
            parts[i] = ReadRepoFile(files[i]).Replace("\r\n", "\n");
        }

        return string.Join("\n", parts);
    }
}
