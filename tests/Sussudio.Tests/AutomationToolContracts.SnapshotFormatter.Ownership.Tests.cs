using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationSnapshotFormatter_SourceOwnership_IsSplit()
    {
        var sharedFormatterSource = global::Sussudio.Tests.RuntimeContractSource.ReadAutomationSnapshotFormatterSource();
        var sharedFormatterRootSource = ReadRepoFile("tools/Common/AutomationSnapshotFormatter.cs");
        var sharedFormatterCoreSectionsSource = sharedFormatterRootSource;
        var sharedFormatterAudioSource = sharedFormatterRootSource;
        var sharedFormatterRecordingSource = sharedFormatterRootSource;
        var sharedFormatterProcessResourcesSource = sharedFormatterRootSource;
        var sharedFormatterCaptureSettingsSource = sharedFormatterRootSource;
        var sharedFormatterVideoPipelineSource = sharedFormatterRootSource;
        var sharedFormatterDiagnosticsSource = sharedFormatterRootSource;
        var sharedFormatterCaptureCadenceSource = sharedFormatterRootSource;
        var sharedFormatterAvSyncSource = sharedFormatterCaptureCadenceSource;
        var sharedFormatterSourceSource = sharedFormatterCaptureCadenceSource;
        var sharedFormatterValuesSource = sharedFormatterRootSource;
        var sharedFormatterDisplayValuesSource = sharedFormatterValuesSource;
        var sharedFormatterFlashbackSource = sharedFormatterRootSource;
        var sharedFormatterMjpegTimingSource = sharedFormatterRootSource;
        var sharedFormatterPreviewSource = sharedFormatterCaptureCadenceSource;
        var sharedFormatterPreviewD3DSource = sharedFormatterRootSource;
        var sharedFormatterThreadHealthSource = sharedFormatterVideoPipelineSource;
        AssertContains(sharedFormatterRootSource, "AppendStateSection(builder, snapshot);");
        AssertContains(sharedFormatterRootSource, "AppendCaptureSettingsSection(builder, snapshot);");
        AssertContains(sharedFormatterRootSource, "AppendAudioSection(builder, snapshot);");
        AssertContains(sharedFormatterRootSource, "AppendVideoPipelineSection(builder, snapshot);");
        AssertContains(sharedFormatterRootSource, "AppendRecordingSection(builder, snapshot);");
        AssertContains(sharedFormatterRootSource, "AppendFlashbackSection(builder, snapshot);");
        AssertContains(sharedFormatterRootSource, "AppendDiagnosticsSection(builder, snapshot);");
        AssertContains(sharedFormatterRootSource, "AppendPerformanceSection(builder, snapshot);");
        AssertContains(sharedFormatterRootSource, "AppendMemorySection(builder, snapshot);");
        AssertContains(sharedFormatterRootSource, "AppendCaptureCadenceSection(builder, snapshot);");
        AssertContains(sharedFormatterRootSource, "builder.AppendLine(\"== Sussudio State ==\");");
        AssertContains(sharedFormatterRootSource, "var selectedFriendlyFrameRate = Get(snapshot, \"SelectedFriendlyFrameRate\", string.Empty);");
        AssertContains(sharedFormatterRootSource, "builder.AppendLine(\"== Audio ==\");");
        AssertContains(sharedFormatterRootSource, "builder.AppendLine(\"== Video Pipeline ==\");");
        AssertContains(sharedFormatterRootSource, "builder.AppendLine(\"== Recording ==\");");
        AssertContains(sharedFormatterRootSource, "builder.AppendLine(\"== Diagnostics ==\");");
        AssertContains(sharedFormatterRootSource, "builder.AppendLine(\"== Performance ==\");");
        AssertContains(sharedFormatterRootSource, "builder.AppendLine(\"== Memory & GC ==\");");
        AssertContains(sharedFormatterRootSource, "builder.AppendLine(\"== Capture Cadence ==\");");
        AssertContains(sharedFormatterRootSource, "RecordingIntegrityStatus");
        AssertContains(sharedFormatterRootSource, "ProcessCpuPercent");
        AssertContains(sharedFormatterCoreSectionsSource, "private static void AppendStateSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterCoreSectionsSource, "builder.AppendLine(\"== Sussudio State ==\");");
        AssertContains(sharedFormatterCoreSectionsSource, "CaptureCommandLastCorrelationId");
        AssertContains(sharedFormatterCaptureSettingsSource, "private static void AppendCaptureSettingsSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterCaptureSettingsSource, "private static string FormatFrameRateSummary(JsonElement snapshot)");
        AssertContains(sharedFormatterCaptureSettingsSource, "SelectedFriendlyFrameRate");
        AssertContains(sharedFormatterCoreSectionsSource, "private static void AppendAudioSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterCoreSectionsSource, "AudioFramesWrittenToSink");
        AssertContains(sharedFormatterAudioSource, "private static void AppendAudioSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterAudioSource, "builder.AppendLine(\"== Audio ==\");");
        AssertContains(sharedFormatterAudioSource, "AudioFramesWrittenToSink");
        AssertContains(sharedFormatterVideoPipelineSource, "private static void AppendVideoPipelineSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterVideoPipelineSource, "builder.AppendLine(\"== Video Pipeline ==\");");
        AssertContains(sharedFormatterVideoPipelineSource, "RecordingVideoQueueLatencyP99Ms");
        AssertContains(sharedFormatterVideoPipelineSource, "AppendThreadHealthSection(builder, snapshot);");
        AssertContains(sharedFormatterVideoPipelineSource, "private static void AppendThreadHealthSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterVideoPipelineSource, "builder.AppendLine(\"== Thread Health ==\");");
        AssertContains(sharedFormatterCoreSectionsSource, "private static void AppendRecordingSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterCoreSectionsSource, "RecordingIntegrityStatus");
        AssertContains(sharedFormatterRecordingSource, "private static void AppendRecordingSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterRecordingSource, "builder.AppendLine(\"== Recording ==\");");
        AssertContains(sharedFormatterRecordingSource, "RecordingIntegrityStatus");
        AssertContains(sharedFormatterDiagnosticsSource, "private static void AppendDiagnosticsSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterDiagnosticsSource, "builder.AppendLine(\"== Diagnostics ==\");");
        AssertContains(sharedFormatterDiagnosticsSource, "DiagnosticEvidence");
        AssertContains(sharedFormatterCoreSectionsSource, "private static void AppendPerformanceSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterCoreSectionsSource, "private static void AppendMemorySection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterCoreSectionsSource, "ProcessCpuPercent");
        AssertContains(sharedFormatterProcessResourcesSource, "private static void AppendPerformanceSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterProcessResourcesSource, "Pipeline Latency: {Get(snapshot, \"EstimatedPipelineLatencyMs\")}ms (app receive -> estimated visible)");
        AssertContains(sharedFormatterProcessResourcesSource, "private static void AppendMemorySection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterProcessResourcesSource, "ProcessCpuPercent");
        AssertContains(sharedFormatterProcessResourcesSource, "ThreadPoolWorkerAvailable");
        AssertContains(sharedFormatterCaptureCadenceSource, "private static void AppendCaptureCadenceSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterCaptureCadenceSource, "FormatFrameBudgetMs(snapshot, \"ExpectedCaptureFrameRate\")");
        AssertContains(sharedFormatterCaptureCadenceSource, "MjpegPacketHashInputObservedFps");
        AssertContains(sharedFormatterCaptureCadenceSource, "AppendMjpegTimingSection(builder, snapshot);");
        AssertContains(sharedFormatterCaptureCadenceSource, "AppendAvSyncSection(builder, snapshot);");
        AssertContains(sharedFormatterCaptureCadenceSource, "AppendPreviewSection(builder, snapshot);");
        AssertContains(sharedFormatterCaptureCadenceSource, "AppendSourceSection(builder, snapshot);");
        AssertContains(sharedFormatterValuesSource, "internal static bool IsSuccess(JsonElement response)");
        AssertContains(sharedFormatterValuesSource, "response.TryGetProperty(\"Success\", out var success)");
        AssertContains(sharedFormatterValuesSource, "internal static string Get(JsonElement element, string propertyName, string fallback = \"N/A\")");
        AssertContains(sharedFormatterValuesSource, "internal static bool GetBool(JsonElement element, string propertyName)");
        AssertContains(sharedFormatterValuesSource, "internal static string? GetString(JsonElement element, string propertyName)");
        AssertContains(sharedFormatterValuesSource, "internal static int GetInt(JsonElement element, string propertyName, int fallback = 0)");
        AssertContains(sharedFormatterValuesSource, "internal static double GetDouble(JsonElement element, string propertyName, double fallback = 0.0)");
        AssertContains(sharedFormatterValuesSource, "internal static long GetLong(JsonElement element, string propertyName, long fallback = 0)");
        AssertContains(sharedFormatterValuesSource, "internal static long? GetNullableLong(JsonElement element, string propertyName)");
        AssertContains(sharedFormatterValuesSource, "CultureInfo.InvariantCulture");
        AssertContains(sharedFormatterValuesSource, "internal static string FormatBytes(long bytes)");
        AssertContains(sharedFormatterValuesSource, "internal static long ComputeTickAgeMs(long tickMs)");
        AssertContains(sharedFormatterDisplayValuesSource, "internal static string FormatBytes(long bytes)");
        AssertContains(sharedFormatterDisplayValuesSource, "internal static string FormatIntervalMs(JsonElement element, string propertyName, string fallback = \"N/A\")");
        AssertContains(sharedFormatterDisplayValuesSource, "internal static string FormatFrameBudgetMs(JsonElement element, string fpsPropertyName, string fallback = \"N/A\")");
        AssertContains(sharedFormatterDisplayValuesSource, "internal static string FormatNumber(double value, string format)");
        AssertContains(sharedFormatterDisplayValuesSource, "internal static long ComputeTickAgeMs(long tickMs)");
        AssertContains(sharedFormatterFlashbackSource, "private static void AppendFlashbackSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterFlashbackSource, "var flashbackActive = Get(snapshot, \"FlashbackActive\", \"false\");");
        AssertContains(sharedFormatterFlashbackSource, "AppendFlashbackEncodingSection(builder, snapshot);");
        AssertContains(sharedFormatterFlashbackSource, "AppendFlashbackPlaybackStatusSection(builder, snapshot);");
        AssertContains(sharedFormatterFlashbackSource, "AppendFlashbackExportSection(builder, snapshot);");
        AssertContains(sharedFormatterFlashbackSource, "AppendFlashbackPlaybackMetricsSection(builder, snapshot);");
        AssertContains(sharedFormatterFlashbackSource, "private static void AppendFlashbackEncodingSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterFlashbackSource, "AppendFlashbackEncodingStatusSection(builder, snapshot);");
        AssertContains(sharedFormatterFlashbackSource, "AppendFlashbackEncodingHealthSection(builder, snapshot);");
        AssertContains(sharedFormatterFlashbackSource, "private static void AppendFlashbackEncodingStatusSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterFlashbackSource, "Encoder: {codec}");
        AssertContains(sharedFormatterFlashbackSource, "Temp Cache:");
        AssertContains(sharedFormatterFlashbackSource, "Cleanup:");
        AssertContains(sharedFormatterFlashbackSource, "private static void AppendFlashbackEncodingHealthSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterFlashbackSource, "Flashback Queue Latency:");
        AssertContains(sharedFormatterFlashbackSource, "Flashback Backpressure:");
        AssertContains(sharedFormatterFlashbackSource, "Flashback Failure:");
        AssertContains(sharedFormatterFlashbackSource, "Flashback GPU Queue:");
        AssertContains(sharedFormatterFlashbackSource, "private static void AppendFlashbackExportSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterFlashbackSource, "Export: active=");
        AssertContains(sharedFormatterFlashbackSource, "FlashbackExportThroughputBytesPerSec");
        AssertContains(sharedFormatterFlashbackSource, "forceRotateFallbacks=");
        AssertContains(sharedFormatterFlashbackSource, "private static void AppendFlashbackPlaybackStatusSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterFlashbackSource, "Playback Commands:");
        AssertContains(sharedFormatterFlashbackSource, "private static void AppendFlashbackPlaybackMetricsSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterFlashbackSource, "Playback Decode:");
        AssertContains(sharedFormatterFlashbackSource, "A/V Drift:");
        AssertContains(sharedFormatterRootSource, "builder.AppendLine(\"== Thread Health ==\");");
        AssertContains(sharedFormatterRootSource, "WasapiPlaybackQueueDurationMs");
        AssertContains(sharedFormatterThreadHealthSource, "private static void AppendThreadHealthSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterThreadHealthSource, "builder.AppendLine(\"== Thread Health ==\");");
        AssertContains(sharedFormatterThreadHealthSource, "AppendSourceReaderThreadHealthLine(builder, snapshot);");
        AssertContains(sharedFormatterThreadHealthSource, "AppendWasapiCaptureThreadHealthLine(builder, snapshot);");
        AssertContains(sharedFormatterThreadHealthSource, "AppendWasapiPlaybackThreadHealthLine(builder, snapshot);");
        AssertContains(sharedFormatterThreadHealthSource, "private static void AppendSourceReaderThreadHealthLine(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterThreadHealthSource, "SourceReaderFrameChannelDepth");
        AssertContains(sharedFormatterThreadHealthSource, "private static void AppendWasapiCaptureThreadHealthLine(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterThreadHealthSource, "WasapiCaptureCallbackSevereGapCount");
        AssertContains(sharedFormatterThreadHealthSource, "private static void AppendWasapiPlaybackThreadHealthLine(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterThreadHealthSource, "WasapiPlaybackQueueDurationMs");
        AssertContains(sharedFormatterMjpegTimingSource, "private static void AppendMjpegTimingSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterMjpegTimingSource, "var mjpegDecodeSamples = Get(snapshot, \"MjpegDecodeSampleCount\", \"0\");");
        AssertContains(sharedFormatterMjpegTimingSource, "AppendMjpegDecodeTimingLines(builder, snapshot, mjpegDecodeSamples);");
        AssertContains(sharedFormatterMjpegTimingSource, "AppendMjpegPipelineTimingLines(builder, snapshot, mjpegDecoderCount);");
        AssertContains(sharedFormatterMjpegTimingSource, "AppendMjpegPreviewJitterSection(builder, snapshot);");
        AssertContains(sharedFormatterMjpegTimingSource, "AppendMjpegPerDecoderTimingLines(builder, snapshot);");
        AssertContains(sharedFormatterMjpegTimingSource, "private static void AppendMjpegDecodeTimingLines(StringBuilder builder, JsonElement snapshot, string mjpegDecodeSamples)");
        AssertContains(sharedFormatterMjpegTimingSource, "private static void AppendMjpegPerDecoderTimingLines(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterMjpegTimingSource, "Decode: avg=");
        AssertContains(sharedFormatterMjpegTimingSource, "Decoder[{Get(worker, \"WorkerIndex\", \"?\")}]");
        AssertContains(sharedFormatterMjpegTimingSource, "private static void AppendMjpegPipelineTimingLines(StringBuilder builder, JsonElement snapshot, string mjpegDecoderCount)");
        AssertContains(sharedFormatterMjpegTimingSource, "Compressed Queue:");
        AssertContains(sharedFormatterMjpegTimingSource, "MJPEG Drop Reasons:");
        AssertContains(sharedFormatterMjpegTimingSource, "Pipeline: avg=");
        AssertContains(sharedFormatterMjpegTimingSource, "private static void AppendMjpegPreviewJitterSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterMjpegTimingSource, "Preview Jitter Latency:");
        AssertContains(sharedFormatterMjpegTimingSource, "Preview Jitter Underflow:");
        AssertContains(sharedFormatterCaptureCadenceSource, "AppendAvSyncSection(builder, snapshot);");
        AssertContains(sharedFormatterCaptureCadenceSource, "AppendSourceSection(builder, snapshot);");
        AssertContains(sharedFormatterCaptureCadenceSource, "private static void AppendAvSyncSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterCaptureCadenceSource, "private static void AppendSourceSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterAvSyncSource, "private static void AppendAvSyncSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterAvSyncSource, "var avSyncDrift = Get(snapshot, \"AvSyncCaptureDriftMs\", string.Empty);");
        AssertContains(sharedFormatterPreviewSource, "private static void AppendPreviewSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterPreviewSource, "AppendPreviewD3DSection(builder, snapshot);");
        AssertContains(sharedFormatterPreviewSource, "AppendPreviewSlowFrameDiagnostics(builder, snapshot);");
        AssertContains(sharedFormatterPreviewSource, "D3D CPU timing:");
        AssertContains(sharedFormatterPreviewD3DSource, "private static void AppendPreviewD3DSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterPreviewD3DSource, "private static bool IsPreviewD3DRendererMode(string rendererMode)");
        AssertContains(sharedFormatterPreviewD3DSource, "AppendPreviewD3DCpuTiming(builder, snapshot);");
        AssertContains(sharedFormatterPreviewD3DSource, "AppendPreviewD3DPipelineLatency(builder, snapshot);");
        AssertContains(sharedFormatterPreviewD3DSource, "AppendPreviewD3DFrameLatencyWait(builder, snapshot);");
        AssertContains(sharedFormatterPreviewD3DSource, "AppendPreviewD3DFrameStats(builder, snapshot);");
        AssertContains(sharedFormatterPreviewD3DSource, "AppendPreviewD3DFrameOwnership(builder, snapshot);");
        AssertContains(sharedFormatterPreviewD3DSource, "AppendPreviewSlowFrameDiagnostics(builder, snapshot);");
        AssertOccursBefore(sharedFormatterPreviewD3DSource, "AppendPreviewD3DCpuTiming(builder, snapshot);", "AppendPreviewD3DPipelineLatency(builder, snapshot);");
        AssertOccursBefore(sharedFormatterPreviewD3DSource, "AppendPreviewD3DPipelineLatency(builder, snapshot);", "AppendPreviewD3DFrameLatencyWait(builder, snapshot);");
        AssertOccursBefore(sharedFormatterPreviewD3DSource, "AppendPreviewD3DFrameLatencyWait(builder, snapshot);", "AppendPreviewD3DFrameStats(builder, snapshot);");
        AssertOccursBefore(sharedFormatterPreviewD3DSource, "AppendPreviewD3DFrameStats(builder, snapshot);", "AppendPreviewD3DFrameOwnership(builder, snapshot);");
        AssertOccursBefore(sharedFormatterPreviewD3DSource, "AppendPreviewD3DFrameOwnership(builder, snapshot);", "AppendPreviewSlowFrameDiagnostics(builder, snapshot);");
        AssertContains(sharedFormatterPreviewD3DSource, "private static void AppendPreviewD3DCpuTiming(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterPreviewD3DSource, "D3D CPU timing:");
        AssertContains(sharedFormatterPreviewD3DSource, "private static void AppendPreviewD3DPipelineLatency(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterPreviewD3DSource, "D3D pipeline latency:");
        AssertContains(sharedFormatterPreviewD3DSource, "private static void AppendPreviewD3DFrameLatencyWait(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterPreviewD3DSource, "D3D frame-latency wait:");
        AssertContains(sharedFormatterPreviewD3DSource, "private static void AppendPreviewD3DFrameStats(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterPreviewD3DSource, "D3D DXGI stats:");
        AssertContains(sharedFormatterPreviewD3DSource, "private static void AppendPreviewD3DFrameOwnership(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterPreviewD3DSource, "D3D Ownership:");
        AssertContains(sharedFormatterPreviewD3DSource, "internal static void AppendPreviewSlowFrameDiagnostics(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterPreviewD3DSource, "private static string FormatDiagnosticMs(JsonElement element, string propertyName)");
        AssertContains(sharedFormatterPreviewD3DSource, "D3D Slow Frames:");
        AssertContains(sharedFormatterSourceSource, "private static void AppendSourceSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterSourceSource, "var sourceFrameRate = Get(snapshot, \"DetectedSourceFrameRate\", string.Empty);");
        AssertContains(sharedFormatterSource, "CaptureCommandOldestPendingCommandAgeMs");
        AssertContains(sharedFormatterSource, "CaptureCommandMaxQueueLatencyMs");
        AssertContains(sharedFormatterSource, "CaptureCommandCommandsCoalesced");
        AssertContains(sharedFormatterSource, "CaptureCommandLastOutcome");
        AssertContains(sharedFormatterSource, "CaptureCommandLastCorrelationId");
        AssertContains(sharedFormatterSource, "PreviewD3DInputUploadCpuP99Ms");
        AssertContains(sharedFormatterSource, "PreviewD3DTotalFrameCpuMaxMs");
        AssertContains(sharedFormatterSource, "ProcessCpuPercent");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "AutomationSnapshotFormatter.VideoPipeline.cs")),
            "shared snapshot video-pipeline and thread-health text lives with the root snapshot formatter flow");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "AutomationSnapshotFormatter.Values.cs")),
            "shared snapshot value accessors live with the root snapshot formatter flow");
        foreach (var removedFile in new[]
        {
            "AutomationSnapshotFormatter.Flashback.cs",
            "AutomationSnapshotFormatter.MjpegTiming.cs",
            "AutomationSnapshotFormatter.PreviewD3D.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", removedFile)),
                $"{removedFile} folded into root snapshot formatter");
        }

        return Task.CompletedTask;
    }
}
