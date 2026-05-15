static partial class Program
{
    private static string ReadDiagnosticSessionFlashbackRecordingSettingsScenariosSource()
    {
        var files = new[]
        {
            "tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.cs",
            "tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.Models.cs",
            "tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.DuringRecording.cs",
            "tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.PostStop.cs"
        };
        var parts = new string[files.Length];
        for (var i = 0; i < files.Length; i++)
        {
            parts[i] = ReadRepoFile(files[i]).Replace("\r\n", "\n");
        }

        return string.Join("\n", parts);
    }
}
