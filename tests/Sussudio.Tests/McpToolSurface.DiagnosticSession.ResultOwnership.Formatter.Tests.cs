using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticSessionResultFormatter_OwnsFormattedSummaryText()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var formatterText = ReadDiagnosticSessionResultFormatterSource();
        var overviewText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.Overview.cs")
            .Replace("\r\n", "\n");
        var flashbackRootText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.Flashback.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackRecording.cs")
            .Replace("\r\n", "\n");
        var flashbackExportText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackExport.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackPerformanceText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.Performance.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackDecodeText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.Decode.cs")
            .Replace("\r\n", "\n");
        var previewRootText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.Preview.cs")
            .Replace("\r\n", "\n");
        var previewD3DText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.PreviewD3D.cs")
            .Replace("\r\n", "\n");
        var previewVisualCadenceText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.PreviewVisualCadence.cs")
            .Replace("\r\n", "\n");

        AssertContains(formatterText, "public static partial class DiagnosticSessionResultFormatter");
        AssertContains(formatterText, "public static string Format(DiagnosticSessionResult result)");
        AssertContains(formatterText, "== Diagnostic Session:");
        AssertContains(formatterText, "private static void AppendOverview(");
        AssertContains(overviewText, "private static void AppendOverview(");
        AssertContains(formatterText, "private static void AppendCaptureMode(");
        AssertContains(formatterText, "\"Capture Mode: \"");
        AssertContains(formatterText, "private static void AppendRecordingVerification(");
        AssertContains(formatterText, "\"Recording Verification: ");
        AssertContains(formatterText, "private static void AppendPresentMon(");
        AssertContains(formatterText, "\"PresentMon: ");
        AssertContains(formatterText, "private static void AppendProcessPerformance(");
        AssertContains(formatterText, "\"Process Perf: \"");
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
        AssertContains(flashbackRootText, "private static void AppendFlashbackPlaybackCommands(");
        AssertContains(flashbackRootText, "\"Flashback Playback Commands: \"");
        AssertContains(flashbackRootText, "private static void AppendFlashbackPlaybackStages(");
        AssertContains(flashbackRootText, "\"Flashback Playback Stages: \"");
        AssertDoesNotContain(flashbackRootText, "private static void AppendFlashbackRecording(");
        AssertDoesNotContain(flashbackRootText, "\"Flashback Recording: \"");
        AssertDoesNotContain(flashbackRootText, "private static void AppendFlashbackExport(");
        AssertDoesNotContain(flashbackRootText, "\"Flashback Export: \"");
        AssertDoesNotContain(flashbackRootText, "\"Flashback Playback Perf: \"");
        AssertContains(flashbackRecordingText, "private static void AppendFlashbackRecording(");
        AssertContains(flashbackRecordingText, "\"Flashback Recording: \"");
        AssertContains(flashbackRecordingText, "FlashbackRecordingVideoFramesSubmittedDelta");
        AssertContains(flashbackExportText, "private static void AppendFlashbackExport(");
        AssertContains(flashbackExportText, "\"Flashback Export: \"");
        AssertContains(flashbackExportText, "FlashbackExportForceRotateFallbacksDelta");
        AssertContains(flashbackExportText, "FormatBytes(result.FlashbackExportMaxOutputBytesObserved)");
        AssertContains(flashbackPlaybackPerformanceText, "private static void AppendFlashbackPlaybackPerformance(");
        AssertContains(flashbackPlaybackPerformanceText, "\"Flashback Playback Perf: \"");
        AssertContains(flashbackPlaybackPerformanceText, "FormatOptional(result.FlashbackPlaybackAudioMasterLastFallbackReasonAtEnd)");
        AssertDoesNotContain(flashbackPlaybackPerformanceText, "\"Flashback Playback Decode: \"");
        AssertContains(flashbackPlaybackDecodeText, "private static void AppendFlashbackPlaybackDecode(");
        AssertContains(flashbackPlaybackDecodeText, "\"Flashback Playback Decode: \"");
        AssertDoesNotContain(flashbackPlaybackDecodeText, "\"Flashback Playback Stages: \"");
        AssertContains(previewRootText, "private static void AppendPreviewSections(");
        AssertContains(previewRootText, "AppendPreviewScheduler(builder, result);");
        AssertContains(previewRootText, "AppendPreviewD3DPerformance(builder, result);");
        AssertContains(previewRootText, "AppendPreviewD3DCpuTiming(builder, result);");
        AssertContains(previewRootText, "AppendPreviewVisualCadence(builder, result);");
        AssertContains(previewRootText, "private static void AppendPreviewScheduler(");
        AssertContains(previewRootText, "\"Preview Scheduler: \"");
        AssertContains(previewRootText, "FormatOptional(result.PreviewSchedulerLastUnderflowReasonAtEnd)");
        AssertDoesNotContain(previewRootText, "private static void AppendPreviewD3DPerformance(");
        AssertDoesNotContain(previewRootText, "\"Preview D3D Perf: \"");
        AssertDoesNotContain(previewRootText, "private static void AppendPreviewD3DCpuTiming(");
        AssertDoesNotContain(previewRootText, "\"Preview D3D CPU Timing: \"");
        AssertDoesNotContain(previewRootText, "private static void AppendPreviewVisualCadence(");
        AssertDoesNotContain(previewRootText, "\"Preview Visual Cadence: \"");
        AssertContains(previewD3DText, "private static void AppendPreviewD3DPerformance(");
        AssertContains(previewD3DText, "\"Preview D3D Perf: \"");
        AssertContains(previewD3DText, "FormatOptional(result.PreviewD3DLatestSlowFrameReason)");
        AssertContains(previewD3DText, "private static void AppendPreviewD3DCpuTiming(");
        AssertContains(previewD3DText, "\"Preview D3D CPU Timing: \"");
        AssertContains(previewVisualCadenceText, "private static void AppendPreviewVisualCadence(");
        AssertContains(previewVisualCadenceText, "\"Preview Visual Cadence: \"");
        AssertContains(previewVisualCadenceText, "VisualCadenceLongestRepeatRunAtEnd");
        AssertContains(runnerText, "return DiagnosticSessionResultFormatter.Format(result);");
        AssertDoesNotContain(runnerText, "== Diagnostic Session:");
        AssertDoesNotContain(runnerText, "\"Flashback Playback Perf: \"");
        AssertDoesNotContain(runnerText, "private static string FormatFrameRate(");

        return Task.CompletedTask;
    }
}
