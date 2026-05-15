using System.Threading.Tasks;

static partial class Program
{
    private static Task DiagnosticSessionResultFormatter_OwnsFormattedSummaryText()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var formatterText = ReadDiagnosticSessionResultFormatterSource();
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
        var previewSchedulerText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.Preview.Scheduler.cs")
            .Replace("\r\n", "\n");
        var previewD3DPerformanceText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.Preview.D3DPerformance.cs")
            .Replace("\r\n", "\n");
        var previewD3DCpuTimingText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.Preview.D3DCpuTiming.cs")
            .Replace("\r\n", "\n");
        var previewVisualCadenceText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.Preview.VisualCadence.cs")
            .Replace("\r\n", "\n");

        AssertContains(formatterText, "public static partial class DiagnosticSessionResultFormatter");
        AssertContains(formatterText, "public static string Format(DiagnosticSessionResult result)");
        AssertContains(formatterText, "== Diagnostic Session:");
        AssertContains(formatterText, "private static void AppendOverview(");
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
        AssertDoesNotContain(previewRootText, "\"Preview Scheduler: \"");
        AssertContains(previewSchedulerText, "private static void AppendPreviewScheduler(");
        AssertContains(previewSchedulerText, "\"Preview Scheduler: \"");
        AssertContains(previewSchedulerText, "FormatOptional(result.PreviewSchedulerLastUnderflowReasonAtEnd)");
        AssertDoesNotContain(previewSchedulerText, "\"Preview D3D Perf: \"");
        AssertContains(previewD3DPerformanceText, "private static void AppendPreviewD3DPerformance(");
        AssertContains(previewD3DPerformanceText, "\"Preview D3D Perf: \"");
        AssertContains(previewD3DPerformanceText, "FormatOptional(result.PreviewD3DLatestSlowFrameReason)");
        AssertDoesNotContain(previewD3DPerformanceText, "\"Preview D3D CPU Timing: \"");
        AssertContains(previewD3DCpuTimingText, "private static void AppendPreviewD3DCpuTiming(");
        AssertContains(previewD3DCpuTimingText, "\"Preview D3D CPU Timing: \"");
        AssertDoesNotContain(previewD3DCpuTimingText, "\"Preview Visual Cadence: \"");
        AssertContains(previewVisualCadenceText, "private static void AppendPreviewVisualCadence(");
        AssertContains(previewVisualCadenceText, "\"Preview Visual Cadence: \"");
        AssertContains(runnerText, "return DiagnosticSessionResultFormatter.Format(result);");
        AssertDoesNotContain(runnerText, "== Diagnostic Session:");
        AssertDoesNotContain(runnerText, "\"Flashback Playback Perf: \"");
        AssertDoesNotContain(runnerText, "private static string FormatFrameRate(");

        return Task.CompletedTask;
    }
}
