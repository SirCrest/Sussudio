static partial class Program
{
    private static string ReadAutomationSnapshotFormatterSource()
    {
        var files = new[]
        {
            "tools/Common/AutomationSnapshotFormatter.cs",
            "tools/Common/AutomationSnapshotFormatter.Values.cs",
            "tools/Common/AutomationSnapshotFormatter.DisplayValues.cs",
            "tools/Common/AutomationSnapshotFormatter.Flashback.cs",
            "tools/Common/AutomationSnapshotFormatter.Flashback.Encoding.cs",
            "tools/Common/AutomationSnapshotFormatter.Flashback.Playback.cs",
            "tools/Common/AutomationSnapshotFormatter.Flashback.Export.cs",
            "tools/Common/AutomationSnapshotFormatter.MjpegTiming.cs",
            "tools/Common/AutomationSnapshotFormatter.AvSync.cs",
            "tools/Common/AutomationSnapshotFormatter.Preview.cs",
            "tools/Common/AutomationSnapshotFormatter.PreviewD3D.cs",
            "tools/Common/AutomationSnapshotFormatter.ThreadHealth.cs",
            "tools/Common/AutomationSnapshotFormatter.Source.cs"
        };
        var parts = new string[files.Length];
        for (var i = 0; i < files.Length; i++)
        {
            parts[i] = ReadRepoFile(files[i]).Replace("\r\n", "\n");
        }

        return string.Join("\n", parts);
    }
}
