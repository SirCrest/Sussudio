static partial class Program
{
    private static string ReadDiagnosticSessionFlashbackMetricsSource()
    {
        var files = new[]
        {
            "tools/Common/DiagnosticSessionFlashbackMetrics.Models.cs",
            "tools/Common/DiagnosticSessionFlashbackMetrics.Recording.cs",
            "tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackSession.cs",
            "tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackResult.cs",
            "tools/Common/DiagnosticSessionFlashbackMetrics.Export.cs"
        };
        var parts = new string[files.Length];
        for (var i = 0; i < files.Length; i++)
        {
            parts[i] = ReadRepoFile(files[i]).Replace("\r\n", "\n");
        }

        return string.Join("\n", parts);
    }
}
