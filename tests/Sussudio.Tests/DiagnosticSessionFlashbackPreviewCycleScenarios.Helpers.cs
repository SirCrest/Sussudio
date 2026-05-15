static partial class Program
{
    private static string ReadDiagnosticSessionFlashbackPreviewCycleScenariosSource()
    {
        var files = new[]
        {
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Registrations.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Flashback.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Playback.cs",
            "tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Recording.cs"
        };
        var parts = new string[files.Length];
        for (var i = 0; i < files.Length; i++)
        {
            parts[i] = ReadRepoFile(files[i]).Replace("\r\n", "\n");
        }

        return string.Join("\n", parts);
    }
}
