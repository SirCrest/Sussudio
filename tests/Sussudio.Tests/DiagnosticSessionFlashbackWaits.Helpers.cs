static partial class Program
{
    private static string ReadDiagnosticSessionFlashbackWaitsSource()
    {
        return string.Join(
            "\n",
            new[]
            {
                ReadRepoFile("tools/Common/DiagnosticSessionFlashbackWaits.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionFlashbackWaits.Playback.cs")
            }).Replace("\r\n", "\n");
    }
}
