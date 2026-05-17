static partial class Program
{
    private static string ReadDiagnosticSessionResultFormatterSource()
        => string.Join(
                "\n",
                ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.Overview.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.CaptureMode.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.RecordingVerification.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.PresentMon.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.Flashback.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.Commands.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.Performance.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.Decode.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.Stages.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackRecording.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackExport.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.Preview.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.Preview.Scheduler.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.Preview.D3DPerformance.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.Preview.D3DCpuTiming.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.Preview.VisualCadence.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.ProcessPerformance.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.Artifacts.cs"),
                ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.Helpers.cs"))
            .Replace("\r\n", "\n");
}
