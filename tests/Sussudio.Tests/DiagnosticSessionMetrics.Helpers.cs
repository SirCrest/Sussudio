static partial class Program
{
    private static string ReadDiagnosticSessionMetricsSource()
        => string.Join(
                "\n",
                ReadRepoFile("tools/Common/DiagnosticSessionMetrics.Models.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionMetrics.SourceCadence.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionMetrics.PreviewCadence.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionMetrics.PreviewD3D.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionMetrics.PlaybackCommands.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionMetrics.Counters.cs"))
            .Replace("\r\n", "\n");
}
