static partial class Program
{
    private static string ReadSsctlSnapshotFormatterSource()
    {
        var files = new[]
        {
            "tools/ssctl/Formatters.Snapshot.cs",
            "tools/ssctl/Formatters.Snapshot.AvSync.cs",
            "tools/ssctl/Formatters.Snapshot.Flashback.cs",
            "tools/ssctl/Formatters.Snapshot.Memory.cs",
            "tools/ssctl/Formatters.Snapshot.Mjpeg.cs",
            "tools/ssctl/Formatters.Snapshot.Preview.cs",
            "tools/ssctl/Formatters.Snapshot.ThreadHealth.cs",
            "tools/ssctl/Formatters.Snapshot.Source.cs"
        };
        var parts = new string[files.Length];
        for (var i = 0; i < files.Length; i++)
        {
            parts[i] = ReadRepoFile(files[i]).Replace("\r\n", "\n");
        }

        return string.Join("\n", parts);
    }
}
