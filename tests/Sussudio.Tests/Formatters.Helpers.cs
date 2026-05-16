static partial class Program
{
    private static string ReadSsctlSnapshotFormatterSource()
    {
        var files = new[]
        {
            "tools/ssctl/Formatters.Snapshot.cs",
            "tools/ssctl/Formatters.Snapshot.Audio.cs",
            "tools/ssctl/Formatters.Snapshot.AvSync.cs",
            "tools/ssctl/Formatters.Snapshot.CaptureCadence.cs",
            "tools/ssctl/Formatters.Snapshot.CaptureSettings.cs",
            "tools/ssctl/Formatters.Snapshot.DiagnosticLanes.cs",
            "tools/ssctl/Formatters.Snapshot.Flashback.cs",
            "tools/ssctl/Formatters.Snapshot.Flashback.Encoding.cs",
            "tools/ssctl/Formatters.Snapshot.Flashback.Export.cs",
            "tools/ssctl/Formatters.Snapshot.Flashback.Playback.cs",
            "tools/ssctl/Formatters.Snapshot.Memory.cs",
            "tools/ssctl/Formatters.Snapshot.Mjpeg.cs",
            "tools/ssctl/Formatters.Snapshot.Performance.cs",
            "tools/ssctl/Formatters.Snapshot.Preview.cs",
            "tools/ssctl/Formatters.Snapshot.PreviewD3D.cs",
            "tools/ssctl/Formatters.Snapshot.PreviewD3D.CpuTiming.cs",
            "tools/ssctl/Formatters.Snapshot.PreviewD3D.FrameFlow.cs",
            "tools/ssctl/Formatters.Snapshot.PreviewD3D.FrameLatencyWait.cs",
            "tools/ssctl/Formatters.Snapshot.PreviewD3D.FrameStats.cs",
            "tools/ssctl/Formatters.Snapshot.PreviewD3D.SlowFrames.cs",
            "tools/ssctl/Formatters.Snapshot.Recording.cs",
            "tools/ssctl/Formatters.Snapshot.State.cs",
            "tools/ssctl/Formatters.Snapshot.ThreadHealth.cs",
            "tools/ssctl/Formatters.Snapshot.VideoPipeline.cs",
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
