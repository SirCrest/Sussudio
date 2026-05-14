static partial class Program
{
    private static string ReadSsctlSnapshotFormatterSource()
    {
        var files = new[]
        {
            "tools/ssctl/Formatters.Snapshot.cs",
            "tools/ssctl/Formatters.Snapshot.Flashback.cs",
            "tools/ssctl/Formatters.Snapshot.Mjpeg.cs"
        };
        var parts = new string[files.Length];
        for (var i = 0; i < files.Length; i++)
        {
            parts[i] = ReadRepoFile(files[i]).Replace("\r\n", "\n");
        }

        return string.Join("\n", parts);
    }
}
