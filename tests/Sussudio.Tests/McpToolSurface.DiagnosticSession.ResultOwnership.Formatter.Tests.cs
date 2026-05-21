using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticSessionResultFormatter_OwnsFormattedSummaryText()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var formatterText = ReadDiagnosticSessionResultFormatterSource();
        var formatterRootText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.cs")
            .Replace("\r\n", "\n");
        var overviewText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.Overview.cs")
            .Replace("\r\n", "\n");
        var captureModeText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.CaptureMode.cs")
            .Replace("\r\n", "\n");
        var recordingVerificationText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.RecordingVerification.cs")
            .Replace("\r\n", "\n");
        var presentMonText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.PresentMon.cs")
            .Replace("\r\n", "\n");
        var processPerformanceText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.ProcessPerformance.cs")
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

        AssertContains(formatterRootText, "public static partial class DiagnosticSessionResultFormatter");
        AssertContains(formatterRootText, "public static string Format(DiagnosticSessionResult result)");
        AssertContains(formatterRootText, "AppendOverview(builder, result);");
        AssertContains(formatterRootText, "AppendCaptureMode(builder, result);");
        AssertContains(formatterRootText, "AppendRecordingVerification(builder, result);");
        AssertContains(formatterRootText, "AppendPresentMon(builder, result);");
        AssertContains(formatterRootText, "AppendProcessPerformance(builder, result);");
        AssertDoesNotContain(formatterRootText, "== Diagnostic Session:");
        AssertDoesNotContain(formatterRootText, "private static void AppendCaptureMode(");
        AssertDoesNotContain(formatterRootText, "private static void AppendRecordingVerification(");
        AssertDoesNotContain(formatterRootText, "private static void AppendPresentMon(");
        AssertDoesNotContain(formatterRootText, "private static void AppendProcessPerformance(");
        AssertDoesNotContain(formatterRootText, "private static string FormatFrameRate(");
        AssertContains(overviewText, "private static void AppendOverview(");
        AssertContains(overviewText, "== Diagnostic Session:");
        AssertContains(captureModeText, "private static void AppendCaptureMode(");
        AssertContains(captureModeText, "\"Capture Mode: \"");
        AssertContains(captureModeText, "private static string FormatFrameRate(");
        AssertContains(captureModeText, "CultureInfo.InvariantCulture");
        AssertContains(recordingVerificationText, "private static void AppendRecordingVerification(");
        AssertContains(recordingVerificationText, "\"Recording Verification: ");
        AssertContains(presentMonText, "private static void AppendPresentMon(");
        AssertContains(presentMonText, "\"PresentMon: ");
        AssertContains(processPerformanceText, "private static void AppendProcessPerformance(");
        AssertContains(processPerformanceText, "\"Process Perf: \"");
        AssertDoesNotContain(captureModeText, "\"Recording Verification: ");
        AssertDoesNotContain(captureModeText, "\"PresentMon: ");
        AssertDoesNotContain(captureModeText, "\"Process Perf: \"");
        AssertDoesNotContain(recordingVerificationText, "\"Capture Mode: \"");
        AssertDoesNotContain(recordingVerificationText, "\"PresentMon: ");
        AssertDoesNotContain(recordingVerificationText, "\"Process Perf: \"");
        AssertDoesNotContain(presentMonText, "\"Capture Mode: \"");
        AssertDoesNotContain(presentMonText, "\"Recording Verification: ");
        AssertDoesNotContain(presentMonText, "\"Process Perf: \"");
        AssertDoesNotContain(processPerformanceText, "\"Capture Mode: \"");
        AssertDoesNotContain(processPerformanceText, "\"Recording Verification: ");
        AssertDoesNotContain(processPerformanceText, "\"PresentMon: ");
        AssertDoesNotContain(overviewText, "\"Capture Mode: \"");
        AssertDoesNotContain(overviewText, "\"Recording Verification: ");
        AssertDoesNotContain(overviewText, "\"PresentMon: ");
        AssertDoesNotContain(overviewText, "\"Process Perf: \"");
        AssertContains(formatterText, "private static void AppendFlashbackSections(");
        AssertContains(formatterText, "private static void AppendPreviewSections(");
        AssertContains(formatterText, "private static void AppendArtifacts(");
        AssertContains(formatterText, "\"Flashback Playback Perf: \"");
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
