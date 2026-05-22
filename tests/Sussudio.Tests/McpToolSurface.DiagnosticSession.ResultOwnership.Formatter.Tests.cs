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

        AssertContains(formatterRootText, "public static class DiagnosticSessionResultFormatter");
        AssertContains(formatterRootText, "public static string Format(DiagnosticSessionResult result)");
        AssertContains(formatterRootText, "AppendOverview(builder, result);");
        AssertContains(formatterRootText, "AppendCaptureMode(builder, result);");
        AssertContains(formatterRootText, "AppendRecordingVerification(builder, result);");
        AssertContains(formatterRootText, "AppendPresentMon(builder, result);");
        AssertContains(formatterRootText, "AppendProcessPerformance(builder, result);");
        AssertContains(formatterRootText, "private static void AppendOverview(");
        AssertContains(formatterRootText, "== Diagnostic Session:");
        AssertContains(formatterRootText, "private static void AppendCaptureMode(");
        AssertContains(formatterRootText, "\"Capture Mode: \"");
        AssertContains(formatterRootText, "private static string FormatFrameRate(");
        AssertContains(formatterRootText, "CultureInfo.InvariantCulture");
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
        AssertContains(formatterRootText, "private static void AppendFlashbackPlaybackCommands(");
        AssertContains(formatterRootText, "\"Flashback Playback Commands: \"");
        AssertContains(formatterRootText, "private static void AppendFlashbackPlaybackStages(");
        AssertContains(formatterRootText, "\"Flashback Playback Stages: \"");
        AssertContains(formatterRootText, "private static void AppendFlashbackRecording(");
        AssertContains(formatterRootText, "\"Flashback Recording: \"");
        AssertContains(formatterRootText, "private static void AppendFlashbackExport(");
        AssertContains(formatterRootText, "\"Flashback Export: \"");
        AssertContains(formatterRootText, "\"Flashback Playback Perf: \"");
        AssertContains(formatterRootText, "private static void AppendPreviewSections(");
        AssertContains(formatterRootText, "AppendPreviewScheduler(builder, result);");
        AssertContains(formatterRootText, "AppendPreviewD3DPerformance(builder, result);");
        AssertContains(formatterRootText, "AppendPreviewD3DCpuTiming(builder, result);");
        AssertContains(formatterRootText, "AppendPreviewVisualCadence(builder, result);");
        AssertContains(formatterRootText, "private static void AppendPreviewScheduler(");
        AssertContains(formatterRootText, "\"Preview Scheduler: \"");
        AssertContains(formatterRootText, "FormatOptional(result.PreviewSchedulerLastUnderflowReasonAtEnd)");
        AssertContains(formatterRootText, "private static void AppendPreviewD3DPerformance(");
        AssertContains(formatterRootText, "\"Preview D3D Perf: \"");
        AssertContains(formatterRootText, "private static void AppendPreviewD3DCpuTiming(");
        AssertContains(formatterRootText, "\"Preview D3D CPU Timing: \"");
        AssertContains(formatterRootText, "private static void AppendPreviewVisualCadence(");
        AssertContains(formatterRootText, "\"Preview Visual Cadence: \"");
        AssertContains(formatterText, "private static void AppendFlashbackSections(");
        AssertContains(formatterText, "private static void AppendPreviewSections(");
        AssertContains(formatterText, "private static void AppendArtifacts(");
        AssertContains(formatterText, "\"Flashback Playback Perf: \"");
        AssertContains(formatterRootText, "FormatOptional(result.FlashbackPlaybackMaxCommandQueueLatencyCommandObserved)");
        AssertContains(formatterRootText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta");
        AssertContains(formatterRootText, "FlashbackRecordingVideoFramesSubmittedDelta");
        AssertContains(formatterRootText, "FlashbackExportForceRotateFallbacksDelta");
        AssertContains(formatterRootText, "FormatBytes(result.FlashbackExportMaxOutputBytesObserved)");
        AssertContains(formatterRootText, "BuildFlashbackPlaybackCadencePerformanceText(result)");
        AssertContains(formatterRootText, "BuildFlashbackPlaybackAudioMasterPerformanceText(result)");
        AssertContains(formatterRootText, "BuildFlashbackPlaybackSubmitPerformanceText(result)");
        AssertContains(formatterRootText, "submitFailuresDelta={result.FlashbackPlaybackSubmitFailuresDelta}");
        AssertContains(formatterRootText, "private static string BuildFlashbackPlaybackCadencePerformanceText(");
        AssertContains(formatterRootText, "BuildFlashbackPlaybackOnePercentLowPerformanceText(result)");
        AssertContains(formatterRootText, "droppedFramesDelta={result.FlashbackPlaybackDroppedFramesDelta}");
        AssertContains(formatterRootText, "private static string BuildFlashbackPlaybackOnePercentLowPerformanceText(");
        AssertContains(formatterRootText, "onePercentLowMinAvDriftMs={result.FlashbackPlaybackMinOnePercentLowAvDriftMs:0.##}");
        AssertContains(formatterRootText, "onePercentLowMinAudioFallbacks={result.FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks}");
        AssertContains(formatterRootText, "private static string BuildFlashbackPlaybackAudioMasterPerformanceText(");
        AssertContains(formatterRootText, "FormatOptional(result.FlashbackPlaybackAudioMasterLastFallbackReasonAtEnd)");
        AssertContains(formatterRootText, "absAvDriftMsMax={result.FlashbackPlaybackMaxAbsAvDriftMsObserved:0.##}");
        AssertContains(formatterRootText, "private static void AppendFlashbackPlaybackDecode(");
        AssertContains(formatterRootText, "\"Flashback Playback Decode: \"");
        AssertContains(formatterRootText, "FormatOptional(result.PreviewD3DLatestSlowFrameReason)");
        AssertContains(formatterRootText, "PreviewD3DInputUploadCpuP99MsAtEnd");
        AssertContains(formatterRootText, "VisualCadenceLongestRepeatRunAtEnd");
        AssertContains(runnerText, "return DiagnosticSessionResultFormatter.Format(result);");
        AssertDoesNotContain(runnerText, "== Diagnostic Session:");
        AssertDoesNotContain(runnerText, "\"Flashback Playback Perf: \"");
        AssertDoesNotContain(runnerText, "private static string FormatFrameRate(");

        return Task.CompletedTask;
    }
}
