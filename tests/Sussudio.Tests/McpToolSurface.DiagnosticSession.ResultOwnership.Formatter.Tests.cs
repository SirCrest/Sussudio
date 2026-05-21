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
        var flashbackPlaybackCommandsText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.Commands.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackCadenceText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.Cadence.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackOnePercentLowText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.OnePercentLow.cs")
            .Replace("\r\n", "\n");
        var flashbackRecordingText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackRecording.cs")
            .Replace("\r\n", "\n");
        var flashbackExportText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackExport.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackPerformanceText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.Performance.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackAudioMasterText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.AudioMaster.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackDecodeText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.Decode.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackStagesText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.Stages.cs")
            .Replace("\r\n", "\n");
        var previewRootText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.Preview.cs")
            .Replace("\r\n", "\n");
        var previewSchedulerText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.PreviewScheduler.cs")
            .Replace("\r\n", "\n");
        var previewD3DPerformanceText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.PreviewD3D.Performance.cs")
            .Replace("\r\n", "\n");
        var previewD3DCpuTimingText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.PreviewD3D.CpuTiming.cs")
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
        AssertDoesNotContain(flashbackRootText, "private static void AppendFlashbackPlaybackCommands(");
        AssertDoesNotContain(flashbackRootText, "\"Flashback Playback Commands: \"");
        AssertDoesNotContain(flashbackRootText, "private static void AppendFlashbackPlaybackStages(");
        AssertDoesNotContain(flashbackRootText, "\"Flashback Playback Stages: \"");
        AssertDoesNotContain(flashbackRootText, "private static void AppendFlashbackRecording(");
        AssertDoesNotContain(flashbackRootText, "\"Flashback Recording: \"");
        AssertDoesNotContain(flashbackRootText, "private static void AppendFlashbackExport(");
        AssertDoesNotContain(flashbackRootText, "\"Flashback Export: \"");
        AssertDoesNotContain(flashbackRootText, "\"Flashback Playback Perf: \"");
        AssertContains(flashbackPlaybackCommandsText, "private static void AppendFlashbackPlaybackCommands(");
        AssertContains(flashbackPlaybackCommandsText, "\"Flashback Playback Commands: \"");
        AssertContains(flashbackPlaybackCommandsText, "FormatOptional(result.FlashbackPlaybackMaxCommandQueueLatencyCommandObserved)");
        AssertContains(flashbackPlaybackStagesText, "private static void AppendFlashbackPlaybackStages(");
        AssertContains(flashbackPlaybackStagesText, "\"Flashback Playback Stages: \"");
        AssertContains(flashbackPlaybackStagesText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta");
        AssertContains(flashbackRecordingText, "private static void AppendFlashbackRecording(");
        AssertContains(flashbackRecordingText, "\"Flashback Recording: \"");
        AssertContains(flashbackRecordingText, "FlashbackRecordingVideoFramesSubmittedDelta");
        AssertContains(flashbackExportText, "private static void AppendFlashbackExport(");
        AssertContains(flashbackExportText, "\"Flashback Export: \"");
        AssertContains(flashbackExportText, "FlashbackExportForceRotateFallbacksDelta");
        AssertContains(flashbackExportText, "FormatBytes(result.FlashbackExportMaxOutputBytesObserved)");
        AssertContains(flashbackPlaybackPerformanceText, "private static void AppendFlashbackPlaybackPerformance(");
        AssertContains(flashbackPlaybackPerformanceText, "\"Flashback Playback Perf: \"");
        AssertContains(flashbackPlaybackPerformanceText, "BuildFlashbackPlaybackCadencePerformanceText(result)");
        AssertContains(flashbackPlaybackPerformanceText, "BuildFlashbackPlaybackAudioMasterPerformanceText(result)");
        AssertContains(flashbackPlaybackPerformanceText, "BuildFlashbackPlaybackSubmitPerformanceText(result)");
        AssertContains(flashbackPlaybackPerformanceText, "submitFailuresDelta={result.FlashbackPlaybackSubmitFailuresDelta}");
        AssertContains(flashbackPlaybackCadenceText, "private static string BuildFlashbackPlaybackCadencePerformanceText(");
        AssertContains(flashbackPlaybackCadenceText, "BuildFlashbackPlaybackOnePercentLowPerformanceText(result)");
        AssertContains(flashbackPlaybackCadenceText, "droppedFramesDelta={result.FlashbackPlaybackDroppedFramesDelta}");
        AssertContains(flashbackPlaybackOnePercentLowText, "private static string BuildFlashbackPlaybackOnePercentLowPerformanceText(");
        AssertContains(flashbackPlaybackOnePercentLowText, "onePercentLowMinAvDriftMs={result.FlashbackPlaybackMinOnePercentLowAvDriftMs:0.##}");
        AssertContains(flashbackPlaybackOnePercentLowText, "onePercentLowMinAudioFallbacks={result.FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks}");
        AssertContains(flashbackPlaybackAudioMasterText, "private static string BuildFlashbackPlaybackAudioMasterPerformanceText(");
        AssertContains(flashbackPlaybackAudioMasterText, "FormatOptional(result.FlashbackPlaybackAudioMasterLastFallbackReasonAtEnd)");
        AssertContains(flashbackPlaybackAudioMasterText, "absAvDriftMsMax={result.FlashbackPlaybackMaxAbsAvDriftMsObserved:0.##}");
        AssertDoesNotContain(flashbackPlaybackPerformanceText, "FlashbackPlaybackObservedFpsAtEnd");
        AssertDoesNotContain(flashbackPlaybackPerformanceText, "FlashbackPlaybackAudioMasterLastFallbackReasonAtEnd");
        AssertDoesNotContain(flashbackPlaybackCadenceText, "onePercentLowMinAvDriftMs={result.FlashbackPlaybackMinOnePercentLowAvDriftMs:0.##}");
        AssertDoesNotContain(flashbackPlaybackOnePercentLowText, "droppedFramesDelta={result.FlashbackPlaybackDroppedFramesDelta}");
        AssertDoesNotContain(flashbackPlaybackCadenceText, "AudioMasterLastFallback");
        AssertDoesNotContain(flashbackPlaybackAudioMasterText, "ObservedFpsAtEnd");
        AssertDoesNotContain(flashbackPlaybackPerformanceText, "\"Flashback Playback Decode: \"");
        AssertContains(flashbackPlaybackDecodeText, "private static void AppendFlashbackPlaybackDecode(");
        AssertContains(flashbackPlaybackDecodeText, "\"Flashback Playback Decode: \"");
        AssertDoesNotContain(flashbackPlaybackDecodeText, "\"Flashback Playback Stages: \"");
        AssertContains(previewRootText, "private static void AppendPreviewSections(");
        AssertContains(previewRootText, "AppendPreviewScheduler(builder, result);");
        AssertContains(previewRootText, "AppendPreviewD3DPerformance(builder, result);");
        AssertContains(previewRootText, "AppendPreviewD3DCpuTiming(builder, result);");
        AssertContains(previewRootText, "AppendPreviewVisualCadence(builder, result);");
        AssertDoesNotContain(previewRootText, "private static void AppendPreviewScheduler(");
        AssertDoesNotContain(previewRootText, "\"Preview Scheduler: \"");
        AssertDoesNotContain(previewRootText, "FormatOptional(result.PreviewSchedulerLastUnderflowReasonAtEnd)");
        AssertDoesNotContain(previewRootText, "private static void AppendPreviewD3DPerformance(");
        AssertDoesNotContain(previewRootText, "\"Preview D3D Perf: \"");
        AssertDoesNotContain(previewRootText, "private static void AppendPreviewD3DCpuTiming(");
        AssertDoesNotContain(previewRootText, "\"Preview D3D CPU Timing: \"");
        AssertDoesNotContain(previewRootText, "private static void AppendPreviewVisualCadence(");
        AssertDoesNotContain(previewRootText, "\"Preview Visual Cadence: \"");
        AssertContains(previewSchedulerText, "private static void AppendPreviewScheduler(");
        AssertContains(previewSchedulerText, "\"Preview Scheduler: \"");
        AssertContains(previewSchedulerText, "FormatOptional(result.PreviewSchedulerLastUnderflowReasonAtEnd)");
        AssertContains(previewD3DPerformanceText, "private static void AppendPreviewD3DPerformance(");
        AssertContains(previewD3DPerformanceText, "\"Preview D3D Perf: \"");
        AssertContains(previewD3DPerformanceText, "FormatOptional(result.PreviewD3DLatestSlowFrameReason)");
        AssertDoesNotContain(previewD3DPerformanceText, "\"Preview D3D CPU Timing: \"");
        AssertContains(previewD3DCpuTimingText, "private static void AppendPreviewD3DCpuTiming(");
        AssertContains(previewD3DCpuTimingText, "\"Preview D3D CPU Timing: \"");
        AssertDoesNotContain(previewD3DCpuTimingText, "\"Preview D3D Perf: \"");
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
