static partial class Program
{
    private static string ReadDiagnosticSessionFlashbackSegmentPlaybackScenariosSource()
    {
        return string.Join(
            "\n",
            new[]
            {
                ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.Validation.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.RecordingAssist.cs")
            }).Replace("\r\n", "\n");
    }
}
