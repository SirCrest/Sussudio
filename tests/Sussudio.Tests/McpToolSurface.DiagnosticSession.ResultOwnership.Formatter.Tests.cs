using System.Threading.Tasks;

static partial class Program
{
    private static Task DiagnosticSessionResultFormatter_OwnsFormattedSummaryText()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var formatterText = ReadDiagnosticSessionResultFormatterSource();
        var overviewText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.Overview.cs")
            .Replace("\r\n", "\n");
        var captureModeText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.CaptureMode.cs")
            .Replace("\r\n", "\n");
        var recordingVerificationText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.RecordingVerification.cs")
            .Replace("\r\n", "\n");
        var presentMonText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.PresentMon.cs")
            .Replace("\r\n", "\n");
        var flashbackRootText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.Flashback.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackCommandsText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.Commands.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackPerformanceText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.Performance.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackDecodeText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.Decode.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackStagesText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.Stages.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackRecording.cs")
            .Replace("\r\n", "\n");
        var flashbackExportText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackExport.cs")
            .Replace("\r\n", "\n");
        var previewRootText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.Preview.cs")
            .Replace("\r\n", "\n");
        var processPerformanceText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.ProcessPerformance.cs")
            .Replace("\r\n", "\n");

        AssertContains(formatterText, "public static partial class DiagnosticSessionResultFormatter");
        AssertContains(formatterText, "public static string Format(DiagnosticSessionResult result)");
        AssertContains(formatterText, "== Diagnostic Session:");
        AssertContains(formatterText, "private static void AppendOverview(");
        AssertContains(overviewText, "private static void AppendOverview(");
        AssertContains(captureModeText, "private static void AppendCaptureMode(");
        AssertContains(captureModeText, "\"Capture Mode: \"");
        AssertContains(recordingVerificationText, "private static void AppendRecordingVerification(");
        AssertContains(recordingVerificationText, "\"Recording Verification: ");
        AssertContains(presentMonText, "private static void AppendPresentMon(");
        AssertContains(presentMonText, "\"PresentMon: ");
        AssertContains(processPerformanceText, "private static void AppendProcessPerformance(");
        AssertContains(processPerformanceText, "\"Process Perf: \"");
        AssertDoesNotContain(overviewText, "\"Capture Mode: \"");
        AssertDoesNotContain(overviewText, "\"Recording Verification: ");
        AssertDoesNotContain(overviewText, "\"PresentMon: ");
        AssertDoesNotContain(overviewText, "\"Process Perf: \"");
        AssertContains(formatterText, "private static void AppendFlashbackSections(");
        AssertContains(formatterText, "private static void AppendPreviewSections(");
        AssertContains(formatterText, "private static void AppendArtifacts(");
        AssertContains(formatterText, "\"Flashback Playback Perf: \"");
        AssertContains(formatterText, "private static string FormatFrameRate(");
        AssertContains(flashbackRootText, "private static void AppendFlashbackSections(");
        AssertContains(flashbackRootText, "AppendFlashbackPlaybackCommands(builder, result);");
        AssertContains(flashbackRootText, "AppendFlashbackRecording(builder, result);");
        AssertContains(flashbackRootText, "AppendFlashbackExport(builder, result);");
        AssertDoesNotContain(flashbackRootText, "\"Flashback Playback Perf: \"");
        AssertContains(flashbackPlaybackCommandsText, "private static void AppendFlashbackPlaybackCommands(");
        AssertContains(flashbackPlaybackCommandsText, "\"Flashback Playback Commands: \"");
        AssertDoesNotContain(flashbackPlaybackCommandsText, "\"Flashback Playback Perf: \"");
        AssertContains(flashbackPlaybackPerformanceText, "private static void AppendFlashbackPlaybackPerformance(");
        AssertContains(flashbackPlaybackPerformanceText, "\"Flashback Playback Perf: \"");
        AssertContains(flashbackPlaybackPerformanceText, "FormatOptional(result.FlashbackPlaybackAudioMasterLastFallbackReasonAtEnd)");
        AssertDoesNotContain(flashbackPlaybackPerformanceText, "\"Flashback Playback Decode: \"");
        AssertContains(flashbackPlaybackDecodeText, "private static void AppendFlashbackPlaybackDecode(");
        AssertContains(flashbackPlaybackDecodeText, "\"Flashback Playback Decode: \"");
        AssertDoesNotContain(flashbackPlaybackDecodeText, "\"Flashback Playback Stages: \"");
        AssertContains(flashbackPlaybackStagesText, "private static void AppendFlashbackPlaybackStages(");
        AssertContains(flashbackPlaybackStagesText, "\"Flashback Playback Stages: \"");
        AssertDoesNotContain(flashbackPlaybackStagesText, "\"Flashback Recording: \"");
        AssertContains(flashbackRecordingText, "private static void AppendFlashbackRecording(");
        AssertContains(flashbackRecordingText, "\"Flashback Recording: \"");
        AssertDoesNotContain(flashbackRecordingText, "\"Flashback Export: \"");
        AssertContains(flashbackExportText, "private static void AppendFlashbackExport(");
        AssertContains(flashbackExportText, "\"Flashback Export: \"");
        AssertContains(previewRootText, "private static void AppendPreviewSections(");
        AssertContains(previewRootText, "AppendPreviewScheduler(builder, result);");
        AssertContains(previewRootText, "AppendPreviewD3DPerformance(builder, result);");
        AssertContains(previewRootText, "AppendPreviewD3DCpuTiming(builder, result);");
        AssertContains(previewRootText, "AppendPreviewVisualCadence(builder, result);");
        AssertContains(previewRootText, "private static void AppendPreviewScheduler(");
        AssertContains(previewRootText, "\"Preview Scheduler: \"");
        AssertContains(previewRootText, "FormatOptional(result.PreviewSchedulerLastUnderflowReasonAtEnd)");
        AssertContains(previewRootText, "private static void AppendPreviewD3DPerformance(");
        AssertContains(previewRootText, "\"Preview D3D Perf: \"");
        AssertContains(previewRootText, "FormatOptional(result.PreviewD3DLatestSlowFrameReason)");
        AssertContains(previewRootText, "private static void AppendPreviewD3DCpuTiming(");
        AssertContains(previewRootText, "\"Preview D3D CPU Timing: \"");
        AssertContains(previewRootText, "private static void AppendPreviewVisualCadence(");
        AssertContains(previewRootText, "\"Preview Visual Cadence: \"");
        AssertContains(runnerText, "return DiagnosticSessionResultFormatter.Format(result);");
        AssertDoesNotContain(runnerText, "== Diagnostic Session:");
        AssertDoesNotContain(runnerText, "\"Flashback Playback Perf: \"");
        AssertDoesNotContain(runnerText, "private static string FormatFrameRate(");

        return Task.CompletedTask;
    }
}
