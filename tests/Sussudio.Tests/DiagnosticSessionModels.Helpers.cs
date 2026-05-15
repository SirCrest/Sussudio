static partial class Program
{
    private static string ReadDiagnosticSessionModelsSource()
    {
        return string.Join(
            "\n",
            new[]
            {
                ReadRepoFile("tools/Common/DiagnosticSessionOptions.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResult.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResult.Capture.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResult.FlashbackPlayback.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResult.FlashbackRecording.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResult.FlashbackExport.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResult.Preview.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResult.Overview.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionSample.cs")
            }).Replace("\r\n", "\n");
    }
}
