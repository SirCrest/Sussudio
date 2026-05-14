static partial class Program
{
    private static string ReadDiagnosticSessionFlashbackStressScenarioSource()
    {
        var files = new[]
        {
            "tools/Common/DiagnosticSessionFlashbackStressScenario.cs",
            "tools/Common/DiagnosticSessionFlashbackStressScenario.Stress.cs",
            "tools/Common/DiagnosticSessionFlashbackStressScenario.Scrub.cs",
            "tools/Common/DiagnosticSessionFlashbackStressScenario.AudioMaster.cs"
        };
        var parts = new string[files.Length];
        for (var i = 0; i < files.Length; i++)
        {
            parts[i] = ReadRepoFile(files[i]).Replace("\r\n", "\n");
        }

        return string.Join("\n", parts);
    }
}
