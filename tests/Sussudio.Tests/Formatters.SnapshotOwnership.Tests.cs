using System.Threading.Tasks;

static partial class Program
{
    internal static Task SsctlFormatters_SnapshotSourceOwnership_IsSplit()
    {
        var ssctlFormatterCommonSource = ReadRepoFile("tools/ssctl/Formatters.Common.cs");
        var ssctlFormatterSource = global::Sussudio.Tests.RuntimeContractSource.ReadSsctlSnapshotFormatterSource();
        var ssctlSnapshotRootSource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.cs");
        var ssctlSnapshotFlashbackSource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.Flashback.cs");
        var ssctlSnapshotMjpegSource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.Mjpeg.cs");
        AssertDoesNotContain(ssctlFormatterCommonSource, "public static string FormatSnapshot");
        AssertContains(ssctlSnapshotRootSource, "AppendSnapshotStateSection(builder, snapshot);");
        AssertContains(ssctlSnapshotRootSource, "AppendSnapshotCaptureSettingsSection(builder, snapshot);");
        AssertContains(ssctlSnapshotRootSource, "AppendSnapshotAudioSection(builder, snapshot);");
        AssertContains(ssctlSnapshotRootSource, "AppendSnapshotVideoPipelineSection(builder, snapshot);");
        AssertContains(ssctlSnapshotRootSource, "AppendSnapshotFlashbackSection(builder, snapshot);");
        AssertContains(ssctlSnapshotRootSource, "AppendSnapshotDiagnosticLanesSection(builder, snapshot);");
        AssertContains(ssctlSnapshotRootSource, "AppendSnapshotPerformanceSection(builder, snapshot);");
        AssertContains(ssctlSnapshotRootSource, "AppendSnapshotMemorySection(builder, snapshot);");
        AssertContains(ssctlSnapshotRootSource, "AppendSnapshotCaptureCadenceSection(builder, snapshot);");
        AssertContains(ssctlSnapshotRootSource, "AppendSnapshotMjpegTimingSection(builder, snapshot);");
        AssertContains(ssctlSnapshotRootSource, "AppendSnapshotAvSyncSection(builder, snapshot);");
        AssertContains(ssctlSnapshotRootSource, "AppendSnapshotPreviewSection(builder, snapshot);");
        AssertContains(ssctlSnapshotRootSource, "AppendSnapshotThreadHealthSection(builder, snapshot);");
        AssertContains(ssctlSnapshotRootSource, "AppendSnapshotSourceSection(builder, snapshot);");
        AssertContains(ssctlSnapshotRootSource, "private static void AppendSnapshotStateSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotRootSource, "builder.AppendLine(\"== Sussudio State ==\");");
        AssertContains(ssctlSnapshotRootSource, "private static void AppendSnapshotAudioSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotRootSource, "builder.AppendLine(\"== Audio ==\");");
        AssertContains(ssctlSnapshotRootSource, "private static void AppendSnapshotCaptureSettingsSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotRootSource, "private static string FormatSnapshotFrameRateSummary(JsonElement snapshot)");
        AssertContains(ssctlSnapshotRootSource, "var selectedFriendlyFrameRate = AutomationSnapshotFormatter.Get(snapshot, \"SelectedFriendlyFrameRate\", string.Empty);");
        AssertContains(ssctlSnapshotRootSource, "private static void AppendSnapshotVideoPipelineSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotRootSource, "builder.AppendLine(\"== Video Pipeline ==\");");
        AssertContains(ssctlSnapshotRootSource, "private static void AppendSnapshotRecordingSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotRootSource, "builder.AppendLine(\"== Recording ==\");");
        AssertContains(ssctlSnapshotRootSource, "RecordingIntegrityStatus");
        AssertContains(ssctlSnapshotRootSource, "private static void AppendSnapshotDiagnosticLanesSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotRootSource, "builder.AppendLine(\"== Diagnostics ==\");");
        AssertContains(ssctlSnapshotRootSource, "private static void AppendSnapshotPerformanceSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotRootSource, "builder.AppendLine(\"== Performance ==\");");
        AssertDoesNotContain(ssctlSnapshotRootSource, "var flashbackActive = AutomationSnapshotFormatter.Get(snapshot, \"FlashbackActive\", \"false\");");
        AssertContains(ssctlSnapshotRootSource, "private static void AppendSnapshotMemorySection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotRootSource, "builder.AppendLine(\"== Memory & GC ==\");");
        AssertContains(ssctlSnapshotRootSource, "ProcessCpuPercent");
        AssertDoesNotContain(ssctlSnapshotRootSource, "var mjpegDecodeSamples = AutomationSnapshotFormatter.Get(snapshot, \"MjpegDecodeSampleCount\", \"0\");");
        AssertContains(ssctlSnapshotRootSource, "private static void AppendSnapshotCaptureCadenceSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotRootSource, "builder.AppendLine(\"== Capture Cadence ==\");");
        AssertContains(ssctlSnapshotRootSource, "private static void AppendSnapshotAvSyncSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotRootSource, "var avSyncDrift = AutomationSnapshotFormatter.Get(snapshot, \"AvSyncCaptureDriftMs\", string.Empty);");
        AssertContains(ssctlSnapshotRootSource, "builder.AppendLine(\"== AV Sync ==\");");
        AssertContains(ssctlSnapshotRootSource, "private static void AppendSnapshotPreviewSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotRootSource, "var rendererMode = AutomationSnapshotFormatter.Get(snapshot, \"PreviewRendererMode\");");
        AssertContains(ssctlSnapshotRootSource, "AppendSnapshotPreviewD3DSection(builder, snapshot);");
        AssertContains(ssctlSnapshotRootSource, "private static void AppendSnapshotPreviewD3DSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotRootSource, "private static bool IsSnapshotPreviewD3DRendererMode(string rendererMode)");
        AssertContains(ssctlSnapshotRootSource, "AppendSnapshotPreviewD3DCpuTiming(builder, snapshot);");
        AssertContains(ssctlSnapshotRootSource, "AppendSnapshotPreviewD3DPipelineLatency(builder, snapshot);");
        AssertContains(ssctlSnapshotRootSource, "AppendSnapshotPreviewD3DFrameLatencyWait(builder, snapshot);");
        AssertContains(ssctlSnapshotRootSource, "AppendSnapshotPreviewD3DFrameStats(builder, snapshot);");
        AssertContains(ssctlSnapshotRootSource, "AppendSnapshotPreviewD3DFrameOwnership(builder, snapshot);");
        AssertContains(ssctlSnapshotRootSource, "AppendSnapshotPreviewSlowFrameDiagnostics(builder, snapshot);");
        AssertOccursBefore(ssctlSnapshotRootSource, "AppendSnapshotPreviewD3DCpuTiming(builder, snapshot);", "AppendSnapshotPreviewD3DPipelineLatency(builder, snapshot);");
        AssertOccursBefore(ssctlSnapshotRootSource, "AppendSnapshotPreviewD3DPipelineLatency(builder, snapshot);", "AppendSnapshotPreviewD3DFrameLatencyWait(builder, snapshot);");
        AssertOccursBefore(ssctlSnapshotRootSource, "AppendSnapshotPreviewD3DFrameLatencyWait(builder, snapshot);", "AppendSnapshotPreviewD3DFrameStats(builder, snapshot);");
        AssertOccursBefore(ssctlSnapshotRootSource, "AppendSnapshotPreviewD3DFrameStats(builder, snapshot);", "AppendSnapshotPreviewD3DFrameOwnership(builder, snapshot);");
        AssertOccursBefore(ssctlSnapshotRootSource, "AppendSnapshotPreviewD3DFrameOwnership(builder, snapshot);", "AppendSnapshotPreviewSlowFrameDiagnostics(builder, snapshot);");
        AssertContains(ssctlSnapshotRootSource, "private static void AppendSnapshotPreviewD3DCpuTiming(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotRootSource, "D3D CPU timing:");
        AssertContains(ssctlSnapshotRootSource, "private static void AppendSnapshotPreviewD3DPipelineLatency(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotRootSource, "D3D pipeline latency:");
        AssertContains(ssctlSnapshotRootSource, "private static void AppendSnapshotPreviewD3DFrameLatencyWait(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotRootSource, "D3D frame-latency wait:");
        AssertContains(ssctlSnapshotRootSource, "private static void AppendSnapshotPreviewD3DFrameOwnership(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotRootSource, "D3D Ownership:");
        AssertContains(ssctlSnapshotRootSource, "private static void AppendSnapshotPreviewD3DFrameStats(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotRootSource, "D3D DXGI stats:");
        AssertContains(ssctlSnapshotRootSource, "private static void AppendSnapshotPreviewSlowFrameDiagnostics(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotRootSource, "AutomationSnapshotFormatter.AppendPreviewSlowFrameDiagnostics(builder, snapshot);");
        AssertContains(ssctlSnapshotRootSource, "private static void AppendSnapshotSourceSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotRootSource, "builder.AppendLine(\"== Source ==\");");
        AssertContains(ssctlSnapshotRootSource, "var sourceFrameRate = AutomationSnapshotFormatter.Get(snapshot, \"DetectedSourceFrameRate\", string.Empty);");
        AssertContains(ssctlSnapshotRootSource, "private static void AppendSnapshotThreadHealthSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotRootSource, "builder.AppendLine(\"== Thread Health ==\");");
        AssertContains(ssctlSnapshotRootSource, "AppendSnapshotSourceReaderThreadHealthLine(builder, snapshot);");
        AssertContains(ssctlSnapshotRootSource, "AppendSnapshotWasapiCaptureThreadHealthLine(builder, snapshot);");
        AssertContains(ssctlSnapshotRootSource, "AppendSnapshotWasapiPlaybackThreadHealthLine(builder, snapshot);");
        AssertContains(ssctlSnapshotRootSource, "private static void AppendSnapshotSourceReaderThreadHealthLine(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotRootSource, "SourceReaderFrameChannelDepth");
        AssertContains(ssctlSnapshotRootSource, "private static void AppendSnapshotWasapiCaptureThreadHealthLine(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotRootSource, "WasapiCaptureCallbackSevereGapCount");
        AssertContains(ssctlSnapshotRootSource, "private static void AppendSnapshotWasapiPlaybackThreadHealthLine(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotRootSource, "WasapiPlaybackQueueDropCount");
        AssertContains(ssctlSnapshotFlashbackSource, "private static void AppendSnapshotFlashbackSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotFlashbackSource, "var flashbackActive = AutomationSnapshotFormatter.Get(snapshot, \"FlashbackActive\", \"false\");");
        AssertContains(ssctlSnapshotFlashbackSource, "AppendSnapshotFlashbackEncodingSection(builder, snapshot);");
        AssertContains(ssctlSnapshotFlashbackSource, "AppendSnapshotFlashbackPlaybackStatusSection(builder, snapshot);");
        AssertContains(ssctlSnapshotFlashbackSource, "AppendSnapshotFlashbackExportSection(builder, snapshot);");
        AssertContains(ssctlSnapshotFlashbackSource, "AppendSnapshotFlashbackPlaybackMetricsSection(builder, snapshot);");
        AssertContains(ssctlSnapshotFlashbackSource, "private static void AppendSnapshotFlashbackEncodingSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotFlashbackSource, "AppendSnapshotFlashbackEncodingStatusSection(builder, snapshot);");
        AssertContains(ssctlSnapshotFlashbackSource, "AppendSnapshotFlashbackEncodingHealthSection(builder, snapshot);");
        AssertContains(ssctlSnapshotFlashbackSource, "private static void AppendSnapshotFlashbackEncodingStatusSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotFlashbackSource, "Encoder: {encCodec}");
        AssertContains(ssctlSnapshotFlashbackSource, "Temp Cache:");
        AssertContains(ssctlSnapshotFlashbackSource, "Cleanup:");
        AssertContains(ssctlSnapshotFlashbackSource, "private static void AppendSnapshotFlashbackEncodingHealthSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotFlashbackSource, "Flashback Queue Latency:");
        AssertContains(ssctlSnapshotFlashbackSource, "Flashback Backpressure:");
        AssertContains(ssctlSnapshotFlashbackSource, "Flashback Failure:");
        AssertContains(ssctlSnapshotFlashbackSource, "Flashback GPU Queue:");
        AssertContains(ssctlSnapshotFlashbackSource, "private static void AppendSnapshotFlashbackExportSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotFlashbackSource, "Export: active=");
        AssertContains(ssctlSnapshotFlashbackSource, "FlashbackExportThroughputBytesPerSec");
        AssertContains(ssctlSnapshotFlashbackSource, "forceRotateFallbacks=");
        AssertContains(ssctlSnapshotFlashbackSource, "private static void AppendSnapshotFlashbackPlaybackStatusSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotFlashbackSource, "Playback Commands:");
        AssertContains(ssctlSnapshotFlashbackSource, "private static void AppendSnapshotFlashbackPlaybackMetricsSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotFlashbackSource, "Playback Decode:");
        AssertContains(ssctlSnapshotFlashbackSource, "A/V Drift:");
        AssertContains(ssctlSnapshotMjpegSource, "private static void AppendSnapshotMjpegTimingSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotMjpegSource, "var mjpegDecodeSamples = AutomationSnapshotFormatter.Get(snapshot, \"MjpegDecodeSampleCount\", \"0\");");
        AssertContains(ssctlSnapshotMjpegSource, "AppendSnapshotMjpegDecodeTimingLines(builder, snapshot, mjpegDecodeSamples);");
        AssertContains(ssctlSnapshotMjpegSource, "AppendSnapshotMjpegPipelineTimingLines(builder, snapshot, mjpegDecoderCount);");
        AssertContains(ssctlSnapshotMjpegSource, "AppendSnapshotMjpegPreviewJitterSection(builder, snapshot);");
        AssertContains(ssctlSnapshotMjpegSource, "AppendSnapshotMjpegPerDecoderTimingLines(builder, snapshot);");
        AssertContains(ssctlSnapshotMjpegSource, "private static void AppendSnapshotMjpegDecodeTimingLines(StringBuilder builder, JsonElement snapshot, string mjpegDecodeSamples)");
        AssertContains(ssctlSnapshotMjpegSource, "private static void AppendSnapshotMjpegPipelineTimingLines(StringBuilder builder, JsonElement snapshot, string mjpegDecoderCount)");
        AssertContains(ssctlSnapshotMjpegSource, "private static void AppendSnapshotMjpegPreviewJitterSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotMjpegSource, "private static void AppendSnapshotMjpegPerDecoderTimingLines(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotMjpegSource, "Decode: avg=");
        AssertContains(ssctlSnapshotMjpegSource, "Compressed Queue:");
        AssertContains(ssctlSnapshotMjpegSource, "MJPEG Drop Reasons:");
        AssertContains(ssctlSnapshotMjpegSource, "Pipeline: avg=");
        AssertContains(ssctlSnapshotMjpegSource, "Decoder[{AutomationSnapshotFormatter.Get(worker, \"WorkerIndex\", \"?\")}]");
        AssertContains(ssctlSnapshotMjpegSource, "Preview Jitter Latency:");
        AssertContains(ssctlSnapshotMjpegSource, "Preview Jitter Underflow:");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "ssctl", "Formatters.Snapshot.PreviewD3D.cs")),
            "ssctl D3D preview snapshot text lives with the preview routing owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "ssctl", "Formatters.Snapshot.ThreadHealth.cs")),
            "ssctl thread-health snapshot text lives with the root formatter flow");
        AssertContains(ssctlFormatterCommonSource, "public static string FormatDiagnostics");
        AssertContains(ReadRepoFile("tools/ssctl/Formatters.Options.cs"), "public static string FormatOptions");
        var ssctlTimelineRootSource = ReadRepoFile("tools/ssctl/Formatters.Timeline.cs");
        AssertContains(ssctlTimelineRootSource, "public static string FormatTimeline");
        AssertContains(ssctlTimelineRootSource, "var entries = ReadTimelineRows(data);");
        AssertContains(ssctlTimelineRootSource, "return RenderTimeline(entries);");
        AssertContains(ssctlTimelineRootSource, "private sealed class TimelineRow");
        AssertContains(ssctlTimelineRootSource, "AutomationSnapshotFormatter.GetDouble(item, \"CaptureFps\")");
        AssertContains(ssctlTimelineRootSource, "private static string RenderTimeline(IReadOnlyList<TimelineRow> entries)");
        AssertContains(ssctlTimelineRootSource, "Performance Timeline ({entries.Count} samples)");
        AssertContains(ssctlTimelineRootSource, "AppendTimelineTrendSummary(builder, entries);");
        AssertContains(ssctlTimelineRootSource, "private static void AppendTimelineTrendSummary(StringBuilder builder, IReadOnlyList<TimelineRow> entries)");
        AssertContains(ssctlTimelineRootSource, "== Trend Summary (first vs last sample) ==");
        AssertContains(ssctlFormatterCommonSource, "public static string FormatMemory");
        AssertContains(ssctlFormatterCommonSource, "public static string FormatResult");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "ssctl", "Formatters.Diagnostics.cs")),
            "ssctl diagnostic-event output lives with the root formatter helpers");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "ssctl", "Formatters.Memory.cs")),
            "ssctl standalone memory output lives with the root formatter helpers");
        AssertContains(ssctlFormatterSource, "CaptureCommandOldestPendingCommandAgeMs");
        AssertContains(ssctlFormatterSource, "CaptureCommandMaxQueueLatencyMs");
        AssertContains(ssctlFormatterSource, "CaptureCommandCommandsCoalesced");
        AssertContains(ssctlFormatterSource, "CaptureCommandLastOutcome");
        AssertContains(ssctlFormatterSource, "CaptureCommandLastCorrelationId");
        AssertContains(ssctlFormatterSource, "PreviewD3DInputUploadCpuP99Ms");
        AssertContains(ssctlFormatterSource, "PreviewD3DTotalFrameCpuMaxMs");
        AssertContains(ssctlFormatterSource, "ProcessCpuPercent");

        return Task.CompletedTask;
    }
}
