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
        var flashbackPlaybackText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackRecording.cs")
            .Replace("\r\n", "\n");
        var flashbackExportText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackExport.cs")
            .Replace("\r\n", "\n");
        var previewSchedulerText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.PreviewScheduler.cs")
            .Replace("\r\n", "\n");
        var previewD3DPerformanceText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.PreviewD3D.Performance.cs")
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
        AssertContains(formatterRootText, "private static void AppendRecordingVerification(");
        AssertContains(formatterRootText, "\"Recording Verification: ");
        AssertContains(formatterRootText, "private static void AppendPresentMon(");
        AssertContains(formatterRootText, "\"PresentMon: ");
        AssertContains(formatterRootText, "private static void AppendProcessPerformance(");
        AssertContains(formatterRootText, "\"Process Perf: \"");
        AssertContains(formatterRootText, "private static void AppendFlashbackSections(");
        AssertContains(formatterRootText, "AppendFlashbackPlaybackCommands(builder, result);");
        AssertContains(formatterRootText, "AppendFlashbackRecording(builder, result);");
        AssertContains(formatterRootText, "AppendFlashbackExport(builder, result);");
        AssertDoesNotContain(formatterRootText, "private static void AppendFlashbackPlaybackCommands(");
        AssertDoesNotContain(formatterRootText, "\"Flashback Playback Commands: \"");
        AssertDoesNotContain(formatterRootText, "private static void AppendFlashbackPlaybackStages(");
        AssertDoesNotContain(formatterRootText, "\"Flashback Playback Stages: \"");
        AssertDoesNotContain(formatterRootText, "private static void AppendFlashbackRecording(");
        AssertDoesNotContain(formatterRootText, "\"Flashback Recording: \"");
        AssertDoesNotContain(formatterRootText, "private static void AppendFlashbackExport(");
        AssertDoesNotContain(formatterRootText, "\"Flashback Export: \"");
        AssertDoesNotContain(formatterRootText, "\"Flashback Playback Perf: \"");
        AssertContains(formatterRootText, "private static void AppendPreviewSections(");
        AssertContains(formatterRootText, "AppendPreviewScheduler(builder, result);");
        AssertContains(formatterRootText, "AppendPreviewD3DPerformance(builder, result);");
        AssertContains(formatterRootText, "AppendPreviewD3DCpuTiming(builder, result);");
        AssertContains(formatterRootText, "AppendPreviewVisualCadence(builder, result);");
        AssertDoesNotContain(formatterRootText, "private static void AppendPreviewScheduler(");
        AssertDoesNotContain(formatterRootText, "\"Preview Scheduler: \"");
        AssertDoesNotContain(formatterRootText, "FormatOptional(result.PreviewSchedulerLastUnderflowReasonAtEnd)");
        AssertDoesNotContain(formatterRootText, "private static void AppendPreviewD3DPerformance(");
        AssertDoesNotContain(formatterRootText, "\"Preview D3D Perf: \"");
        AssertDoesNotContain(formatterRootText, "private static void AppendPreviewD3DCpuTiming(");
        AssertDoesNotContain(formatterRootText, "\"Preview D3D CPU Timing: \"");
        AssertDoesNotContain(formatterRootText, "private static void AppendPreviewVisualCadence(");
        AssertDoesNotContain(formatterRootText, "\"Preview Visual Cadence: \"");
        AssertDoesNotContain(formatterRootText, "private static string FormatFrameRate(");
        AssertContains(overviewText, "private static void AppendOverview(");
        AssertContains(overviewText, "== Diagnostic Session:");
        AssertContains(captureModeText, "private static void AppendCaptureMode(");
        AssertContains(captureModeText, "\"Capture Mode: \"");
        AssertContains(captureModeText, "private static string FormatFrameRate(");
        AssertContains(captureModeText, "CultureInfo.InvariantCulture");
        AssertDoesNotContain(captureModeText, "\"Recording Verification: ");
        AssertDoesNotContain(captureModeText, "\"PresentMon: ");
        AssertDoesNotContain(captureModeText, "\"Process Perf: \"");
        AssertDoesNotContain(overviewText, "\"Capture Mode: \"");
        AssertDoesNotContain(overviewText, "\"Recording Verification: ");
        AssertDoesNotContain(overviewText, "\"PresentMon: ");
        AssertDoesNotContain(overviewText, "\"Process Perf: \"");
        AssertContains(formatterText, "private static void AppendFlashbackSections(");
        AssertContains(formatterText, "private static void AppendPreviewSections(");
        AssertContains(formatterText, "private static void AppendArtifacts(");
        AssertContains(formatterText, "\"Flashback Playback Perf: \"");
        AssertContains(flashbackPlaybackText, "private static void AppendFlashbackPlaybackCommands(");
        AssertContains(flashbackPlaybackText, "\"Flashback Playback Commands: \"");
        AssertContains(flashbackPlaybackText, "FormatOptional(result.FlashbackPlaybackMaxCommandQueueLatencyCommandObserved)");
        AssertContains(flashbackPlaybackText, "private static void AppendFlashbackPlaybackStages(");
        AssertContains(flashbackPlaybackText, "\"Flashback Playback Stages: \"");
        AssertContains(flashbackPlaybackText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta");
        AssertContains(flashbackRecordingText, "private static void AppendFlashbackRecording(");
        AssertContains(flashbackRecordingText, "\"Flashback Recording: \"");
        AssertContains(flashbackRecordingText, "FlashbackRecordingVideoFramesSubmittedDelta");
        AssertContains(flashbackExportText, "private static void AppendFlashbackExport(");
        AssertContains(flashbackExportText, "\"Flashback Export: \"");
        AssertContains(flashbackExportText, "FlashbackExportForceRotateFallbacksDelta");
        AssertContains(flashbackExportText, "FormatBytes(result.FlashbackExportMaxOutputBytesObserved)");
        AssertContains(flashbackPlaybackText, "private static void AppendFlashbackPlaybackPerformance(");
        AssertContains(flashbackPlaybackText, "\"Flashback Playback Perf: \"");
        AssertContains(flashbackPlaybackText, "BuildFlashbackPlaybackCadencePerformanceText(result)");
        AssertContains(flashbackPlaybackText, "BuildFlashbackPlaybackAudioMasterPerformanceText(result)");
        AssertContains(flashbackPlaybackText, "BuildFlashbackPlaybackSubmitPerformanceText(result)");
        AssertContains(flashbackPlaybackText, "submitFailuresDelta={result.FlashbackPlaybackSubmitFailuresDelta}");
        AssertContains(flashbackPlaybackText, "private static string BuildFlashbackPlaybackCadencePerformanceText(");
        AssertContains(flashbackPlaybackText, "BuildFlashbackPlaybackOnePercentLowPerformanceText(result)");
        AssertContains(flashbackPlaybackText, "droppedFramesDelta={result.FlashbackPlaybackDroppedFramesDelta}");
        AssertContains(flashbackPlaybackText, "private static string BuildFlashbackPlaybackOnePercentLowPerformanceText(");
        AssertContains(flashbackPlaybackText, "onePercentLowMinAvDriftMs={result.FlashbackPlaybackMinOnePercentLowAvDriftMs:0.##}");
        AssertContains(flashbackPlaybackText, "onePercentLowMinAudioFallbacks={result.FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks}");
        AssertContains(flashbackPlaybackText, "private static string BuildFlashbackPlaybackAudioMasterPerformanceText(");
        AssertContains(flashbackPlaybackText, "FormatOptional(result.FlashbackPlaybackAudioMasterLastFallbackReasonAtEnd)");
        AssertContains(flashbackPlaybackText, "absAvDriftMsMax={result.FlashbackPlaybackMaxAbsAvDriftMsObserved:0.##}");
        AssertContains(flashbackPlaybackText, "private static void AppendFlashbackPlaybackDecode(");
        AssertContains(flashbackPlaybackText, "\"Flashback Playback Decode: \"");
        AssertContains(previewSchedulerText, "private static void AppendPreviewScheduler(");
        AssertContains(previewSchedulerText, "\"Preview Scheduler: \"");
        AssertContains(previewSchedulerText, "FormatOptional(result.PreviewSchedulerLastUnderflowReasonAtEnd)");
        AssertContains(previewD3DPerformanceText, "private static void AppendPreviewD3DPerformance(");
        AssertContains(previewD3DPerformanceText, "\"Preview D3D Perf: \"");
        AssertContains(previewD3DPerformanceText, "FormatOptional(result.PreviewD3DLatestSlowFrameReason)");
        AssertContains(previewD3DPerformanceText, "private static void AppendPreviewD3DCpuTiming(");
        AssertContains(previewD3DPerformanceText, "\"Preview D3D CPU Timing: \"");
        AssertContains(previewD3DPerformanceText, "PreviewD3DInputUploadCpuP99MsAtEnd");
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
