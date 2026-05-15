using System.Reflection;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;

// Tests for command-line snapshot and diagnostics formatting.
static partial class Program
{
    private static Task SsctlFormatters_EmitCoreSnapshotSections()
    {
        var assemblyPath = Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll");
        var ssctlAssembly = LoadToolAssembly(assemblyPath);
        var formatterType = ssctlAssembly.GetType("Sussudio.Tools.Ssctl.Formatters")
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.Formatters type not found.");
        var formatSnapshot = formatterType.GetMethod("FormatSnapshot", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.Formatters.FormatSnapshot not found.");

        const string json = """
                            {"Snapshot":{"SessionState":"Ready","StatusText":"Idle","SelectedDeviceName":"Synthetic","SelectedDeviceId":"device-1","IsInitialized":true,"IsPreviewing":true,"IsRecording":false,"SelectedResolution":"3840x2160","SelectedFrameRate":120,"SelectedRecordingFormat":"HEVC","SelectedQuality":"High","SelectedPreset":"P5","SelectedSplitEncodeMode":"Auto","SelectedVideoFormat":"MJPG","ShowAllCaptureOptions":true,"PreviewVolumePercent":42.5,"IsStatsVisible":true,"IsHdrEnabled":false,"IsHdrAvailable":true,"HdrOutputActive":false,"HdrRuntimeState":"Inactive","RequestedPipelineMode":"SDR","ActivePipelineMode":"SDR","PipelineModeMatched":true,"IsAudioEnabled":true,"IsAudioPreviewEnabled":false,"IsCustomAudioInputEnabled":false,"AudioPeak":0,"AudioClipping":false,"AudioSignalPresent":false,"AudioReaderActive":false,"AudioFramesArrived":0,"AudioFramesWrittenToSink":0,"VideoReaderActive":true,"IngestVideoFramesArrived":120,"IngestVideoFramesWrittenToSink":120,"EncoderVideoFramesEnqueued":0,"EncoderVideoFramesEncoded":0,"FfmpegVideoQueueDepth":0,"VideoDropsQueueSaturated":0,"IngestLastVideoFrameAgeMs":5,"EncoderLastEnqueueAgeMs":0,"EncoderLastWriteAgeMs":0,"MemoryPreference":"Gpu","VideoRequestedSubtype":"MJPG","VideoNegotiatedSubtype":"MJPG","VideoIngestErrorCount":0,"SourceReaderReadOutstanding":false,"SourceReaderReadOutstandingMs":0,"SourceReaderLastFrameTickMs":0,"SourceReaderFrameChannelDepth":0,"WasapiCaptureCallbackCount":0,"WasapiCaptureCallbackAvgIntervalMs":0,"WasapiCaptureCallbackMaxIntervalMs":0,"WasapiCaptureCallbackSilenceCount":0,"WasapiCaptureLastCallbackTickMs":0,"WasapiCaptureAudioLevelEventsFired":0,"WasapiPlaybackRenderCallbackCount":0,"WasapiPlaybackRenderSilenceCount":0,"WasapiPlaybackQueueDepth":0,"WasapiPlaybackQueueDropCount":0,"WasapiPlaybackLastRenderTickMs":0,"OutputPath":"","RecordingTime":"00:00:00","RecordingSizeInfo":"0 B","RecordingBitrateInfo":"0 Mbps","RecordingBackend":"None","AudioPathMode":"None","MuxResult":"NotAttempted","LastOutputPath":"","LastOutputSizeBytes":0,"LastFinalizeStatus":"None","FlashbackActive":true,"FlashbackBufferedDurationMs":45000,"FlashbackDiskBytes":104857600,"FlashbackTotalBytesWritten":157286400,"FlashbackGpuEncoding":true,"FlashbackEncodedFrames":900,"FlashbackDroppedFrames":3,"FlashbackVideoQueueDepth":2,"FlashbackAudioQueueDepth":1,"FlashbackPlaybackState":"Paused","FlashbackPlaybackPositionMs":1234,"FlashbackDecoderHwAccel":"D3D11","FlashbackPlaybackObservedFps":59.9,"FlashbackPlaybackAvgFrameMs":16.7,"FlashbackPlaybackFrameCount":300,"FlashbackPlaybackLateFrames":2,"FlashbackPlaybackSubmitFailures":1,"FlashbackAvDriftMs":-1.5,"FlashbackFilePath":"temp/flashback.mp4","PerformanceScore":100,"PerformancePerfectionMet":true,"PerformanceSummary":"OK","EstimatedPipelineLatencyMs":1,"CaptureCadenceObservedFps":120,"ExpectedCaptureFrameRate":120,"CaptureCadenceSampleCount":300,"CaptureCadenceAverageIntervalMs":8.3,"CaptureCadenceP95IntervalMs":8.5,"CaptureCadenceMaxIntervalMs":9.0,"CaptureCadenceJitterStdDevMs":0.1,"CaptureCadenceSevereGapCount":0,"CaptureCadenceEstimatedDroppedFrames":0,"CaptureCadenceEstimatedDropPercent":0,"MjpegDecodeSampleCount":300,"MjpegDecodeAvgMs":2.1,"MjpegDecodeP95Ms":3.4,"MjpegDecodeMaxMs":5.6,"MjpegInteropCopySampleCount":300,"MjpegInteropCopyAvgMs":0.9,"MjpegInteropCopyP95Ms":1.4,"MjpegInteropCopyMaxMs":2.2,"MjpegCallbackSampleCount":300,"MjpegCallbackAvgMs":4.5,"MjpegCallbackP95Ms":6.7,"MjpegCallbackMaxMs":9.1,"MjpegDecoderCount":2,"MjpegReorderSampleCount":300,"MjpegReorderAvgMs":0.4,"MjpegReorderP95Ms":0.8,"MjpegReorderMaxMs":1.2,"MjpegPipelineSampleCount":300,"MjpegPipelineAvgMs":5.1,"MjpegPipelineP95Ms":7.0,"MjpegPipelineMaxMs":9.4,"MjpegTotalDecoded":301,"MjpegTotalEmitted":300,"MjpegTotalDropped":1,"MjpegReorderSkips":2,"MjpegReorderBufferDepth":1,"MjpegPerDecoder":[{"WorkerIndex":0,"SampleCount":150,"AvgMs":2.0,"P95Ms":3.0,"MaxMs":4.0},{"WorkerIndex":1,"SampleCount":151,"AvgMs":2.2,"P95Ms":3.2,"MaxMs":4.2}],"PreviewRendererMode":"D3D11VideoProcessor","PreviewStartupState":"Rendering","PreviewFirstVisualConfirmed":true,"PreviewD3DFramesSubmitted":120,"PreviewD3DFramesRendered":120,"PreviewD3DFramesDropped":0,"PreviewD3DInputColorSpace":"BT.709","PreviewD3DOutputColorSpace":"sRGB","PreviewCadenceObservedFps":120,"DetectedSourceFrameRate":120,"SourceWidth":3840,"SourceHeight":2160,"SourceIsHdr":false,"SourceTelemetryAvailability":"Available","SourceTelemetryConfidence":"High"}}
                            """;
        var jsonWithEncoder = json.Replace(
            "\"FlashbackActive\":true,",
            "\"FlashbackActive\":true,\"EncoderCodecName\":\"hevc_nvenc\",\"EncoderWidth\":3840,\"EncoderHeight\":2160,\"EncoderFrameRate\":120,\"EncoderFrameRateNumerator\":120,\"EncoderFrameRateDenominator\":1,\"EncoderTargetBitRate\":12345678,",
            StringComparison.Ordinal).Replace(
            "\"DetectedSourceFrameRate\":120,",
            "\"AvSyncCaptureDriftMs\":1.5,\"AvSyncCaptureDriftRateMsPerSec\":0.1,\"AvSyncEncoderDriftMs\":-0.5,\"AvSyncEncoderCorrectionSamples\":2,\"DetectedSourceFrameRate\":120,",
            StringComparison.Ordinal);
        using var document = JsonDocument.Parse(jsonWithEncoder);
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        string output;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            output = formatSnapshot.Invoke(null, new object[] { document.RootElement })?.ToString()
                ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.Formatters.FormatSnapshot returned null.");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }

        AssertContains(output, "== Sussudio State ==");
        AssertContains(output, "Capture Commands:");
        AssertContains(output, "== Capture Settings ==");
        AssertContains(output, "== Audio ==");
        AssertContains(output, "== Thread Health ==");
        AssertContains(output, "== Flashback ==");
        AssertContains(output, "== Diagnostics ==");
        AssertContains(output, "Process CPU:");
        AssertContains(output, "Legacy Score:");
        AssertContains(output, "Frame Time:");
        AssertContains(output, "Pipeline Latency: 1ms (app receive -> estimated visible)");
        AssertContains(output, "Average Rate:");
        AssertContains(output, "== MJPEG Pipeline Timing ==");
        AssertContains(output, "== Preview ==");
        AssertContains(output, "== Source ==");
        AssertContains(output, "== AV Sync ==");
        AssertOccursBefore(output, "== Sussudio State ==", "== Capture Settings ==");
        AssertOccursBefore(output, "== Capture Settings ==", "== Audio ==");
        AssertOccursBefore(output, "== Audio ==", "== Video Pipeline ==");
        AssertOccursBefore(output, "== Video Pipeline ==", "== Thread Health ==");
        AssertOccursBefore(output, "== Thread Health ==", "== Recording ==");
        AssertOccursBefore(output, "== Recording ==", "== Flashback ==");
        AssertOccursBefore(output, "== Flashback ==", "== Diagnostics ==");
        AssertOccursBefore(output, "== Diagnostics ==", "== Performance ==");
        AssertOccursBefore(output, "== Performance ==", "== Memory & GC ==");
        AssertOccursBefore(output, "== Memory & GC ==", "== Capture Cadence ==");
        AssertOccursBefore(output, "== Capture Cadence ==", "== MJPEG Pipeline Timing ==");
        AssertOccursBefore(output, "== MJPEG Pipeline Timing ==", "== AV Sync ==");
        AssertOccursBefore(output, "== AV Sync ==", "== Preview ==");
        AssertOccursBefore(output, "== Preview ==", "== Source ==");
        AssertContains(output, "Capture Drift: 1.5ms | Rate: 0.1ms/s");
        AssertContains(output, "Encoder Drift: -0.5ms | Correction Samples: 2");
        AssertContains(output, "Encoder: hevc_nvenc 3840x2160 @ 120 fps (120/1) | Target: 12.3 Mbps");
        AssertContains(output, "Buffer: 45.0s | Disk: 100.0 MB | Written: 150 MB");
        AssertContains(output, "Written: 150 MB");
        AssertContains(output, "submitFailures=1");
        AssertContains(output, "A/V Drift: -1.5ms");
        AssertOccursBefore(output, "Flashback GPU Queue:", "Playback: Paused");
        AssertOccursBefore(output, "Playback Commands:", "Export: active=");
        AssertOccursBefore(output, "Export: active=", "Playback Frame Time:");
        var ssctlFormatterRoot = ReadRepoFile("tools/ssctl/Formatters.cs");
        var ssctlFormatterSource = ReadSsctlSnapshotFormatterSource();
        var ssctlSnapshotRootSource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.cs");
        var ssctlSnapshotAudioSource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.Audio.cs");
        var ssctlSnapshotAvSyncSource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.AvSync.cs");
        var ssctlSnapshotCaptureCadenceSource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.CaptureCadence.cs");
        var ssctlSnapshotCaptureSettingsSource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.CaptureSettings.cs");
        var ssctlSnapshotDiagnosticLanesSource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.DiagnosticLanes.cs");
        var ssctlSnapshotFlashbackSource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.Flashback.cs");
        var ssctlSnapshotFlashbackEncodingSource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.Flashback.Encoding.cs");
        var ssctlSnapshotFlashbackExportSource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.Flashback.Export.cs");
        var ssctlSnapshotFlashbackPlaybackSource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.Flashback.Playback.cs");
        var ssctlSnapshotMemorySource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.Memory.cs");
        var ssctlSnapshotMjpegSource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.Mjpeg.cs");
        var ssctlSnapshotPerformanceSource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.Performance.cs");
        var ssctlSnapshotPreviewSource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.Preview.cs");
        var ssctlSnapshotPreviewD3DSource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.PreviewD3D.cs");
        var ssctlSnapshotRecordingSource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.Recording.cs");
        var ssctlSnapshotStateSource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.State.cs");
        var ssctlSnapshotThreadHealthSource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.ThreadHealth.cs");
        var ssctlSnapshotVideoPipelineSource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.VideoPipeline.cs");
        var ssctlSnapshotSourceSource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.Source.cs");
        AssertDoesNotContain(ssctlFormatterRoot, "public static string FormatSnapshot");
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
        AssertDoesNotContain(ssctlSnapshotRootSource, "builder.AppendLine(\"== Sussudio State ==\");");
        AssertDoesNotContain(ssctlSnapshotRootSource, "var selectedFriendlyFrameRate = AutomationSnapshotFormatter.Get(snapshot, \"SelectedFriendlyFrameRate\", string.Empty);");
        AssertDoesNotContain(ssctlSnapshotRootSource, "builder.AppendLine(\"== Audio ==\");");
        AssertDoesNotContain(ssctlSnapshotRootSource, "builder.AppendLine(\"== Video Pipeline ==\");");
        AssertDoesNotContain(ssctlSnapshotRootSource, "builder.AppendLine(\"== Recording ==\");");
        AssertDoesNotContain(ssctlSnapshotRootSource, "builder.AppendLine(\"== Diagnostics ==\");");
        AssertDoesNotContain(ssctlSnapshotRootSource, "builder.AppendLine(\"== Performance ==\");");
        AssertDoesNotContain(ssctlSnapshotRootSource, "builder.AppendLine(\"== Capture Cadence ==\");");
        AssertDoesNotContain(ssctlSnapshotRootSource, "var flashbackActive = AutomationSnapshotFormatter.Get(snapshot, \"FlashbackActive\", \"false\");");
        AssertDoesNotContain(ssctlSnapshotRootSource, "builder.AppendLine(\"== Memory & GC ==\");");
        AssertDoesNotContain(ssctlSnapshotRootSource, "var mjpegDecodeSamples = AutomationSnapshotFormatter.Get(snapshot, \"MjpegDecodeSampleCount\", \"0\");");
        AssertDoesNotContain(ssctlSnapshotRootSource, "var avSyncDrift = AutomationSnapshotFormatter.Get(snapshot, \"AvSyncCaptureDriftMs\", string.Empty);");
        AssertDoesNotContain(ssctlSnapshotRootSource, "var rendererMode = AutomationSnapshotFormatter.Get(snapshot, \"PreviewRendererMode\");");
        AssertDoesNotContain(ssctlSnapshotRootSource, "builder.AppendLine(\"== Thread Health ==\");");
        AssertDoesNotContain(ssctlSnapshotRootSource, "var sourceReaderLastFrameAgeMs = AutomationSnapshotFormatter.ComputeTickAgeMs");
        AssertDoesNotContain(ssctlSnapshotRootSource, "builder.AppendLine(\"== Source ==\");");
        AssertContains(ssctlSnapshotStateSource, "private static void AppendSnapshotStateSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotStateSource, "builder.AppendLine(\"== Sussudio State ==\");");
        AssertContains(ssctlSnapshotCaptureSettingsSource, "private static void AppendSnapshotCaptureSettingsSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotCaptureSettingsSource, "private static string FormatSnapshotFrameRateSummary(JsonElement snapshot)");
        AssertContains(ssctlSnapshotAudioSource, "private static void AppendSnapshotAudioSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotAudioSource, "builder.AppendLine(\"== Audio ==\");");
        AssertContains(ssctlSnapshotVideoPipelineSource, "private static void AppendSnapshotVideoPipelineSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotVideoPipelineSource, "builder.AppendLine(\"== Video Pipeline ==\");");
        AssertContains(ssctlSnapshotRecordingSource, "private static void AppendSnapshotRecordingSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotRecordingSource, "builder.AppendLine(\"== Recording ==\");");
        AssertContains(ssctlSnapshotDiagnosticLanesSource, "private static void AppendSnapshotDiagnosticLanesSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotDiagnosticLanesSource, "builder.AppendLine(\"== Diagnostics ==\");");
        AssertContains(ssctlSnapshotPerformanceSource, "private static void AppendSnapshotPerformanceSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotPerformanceSource, "builder.AppendLine(\"== Performance ==\");");
        AssertContains(ssctlSnapshotCaptureCadenceSource, "private static void AppendSnapshotCaptureCadenceSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotCaptureCadenceSource, "builder.AppendLine(\"== Capture Cadence ==\");");
        AssertContains(ssctlSnapshotAvSyncSource, "private static void AppendSnapshotAvSyncSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotAvSyncSource, "var avSyncDrift = AutomationSnapshotFormatter.Get(snapshot, \"AvSyncCaptureDriftMs\", string.Empty);");
        AssertContains(ssctlSnapshotAvSyncSource, "builder.AppendLine(\"== AV Sync ==\");");
        AssertContains(ssctlSnapshotFlashbackSource, "private static void AppendSnapshotFlashbackSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotFlashbackSource, "var flashbackActive = AutomationSnapshotFormatter.Get(snapshot, \"FlashbackActive\", \"false\");");
        AssertContains(ssctlSnapshotFlashbackSource, "AppendSnapshotFlashbackEncodingSection(builder, snapshot);");
        AssertContains(ssctlSnapshotFlashbackSource, "AppendSnapshotFlashbackPlaybackStatusSection(builder, snapshot);");
        AssertContains(ssctlSnapshotFlashbackSource, "AppendSnapshotFlashbackExportSection(builder, snapshot);");
        AssertContains(ssctlSnapshotFlashbackSource, "AppendSnapshotFlashbackPlaybackMetricsSection(builder, snapshot);");
        AssertDoesNotContain(ssctlSnapshotFlashbackSource, "Playback Decode:");
        AssertDoesNotContain(ssctlSnapshotFlashbackSource, "Flashback Queue Latency:");
        AssertDoesNotContain(ssctlSnapshotFlashbackSource, "Export: active=");
        AssertContains(ssctlSnapshotFlashbackEncodingSource, "private static void AppendSnapshotFlashbackEncodingSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotFlashbackEncodingSource, "Encoder: {encCodec}");
        AssertContains(ssctlSnapshotFlashbackEncodingSource, "Flashback Queue Latency:");
        AssertContains(ssctlSnapshotFlashbackEncodingSource, "Flashback GPU Queue:");
        AssertContains(ssctlSnapshotFlashbackExportSource, "private static void AppendSnapshotFlashbackExportSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotFlashbackExportSource, "Export: active=");
        AssertContains(ssctlSnapshotFlashbackExportSource, "FlashbackExportThroughputBytesPerSec");
        AssertContains(ssctlSnapshotFlashbackExportSource, "forceRotateFallbacks=");
        AssertContains(ssctlSnapshotFlashbackPlaybackSource, "private static void AppendSnapshotFlashbackPlaybackStatusSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotFlashbackPlaybackSource, "private static void AppendSnapshotFlashbackPlaybackMetricsSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotFlashbackPlaybackSource, "Playback Decode:");
        AssertContains(ssctlSnapshotFlashbackPlaybackSource, "A/V Drift:");
        AssertContains(ssctlSnapshotMemorySource, "private static void AppendSnapshotMemorySection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotMemorySource, "builder.AppendLine(\"== Memory & GC ==\");");
        AssertContains(ssctlSnapshotMjpegSource, "private static void AppendSnapshotMjpegTimingSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotMjpegSource, "var mjpegDecodeSamples = AutomationSnapshotFormatter.Get(snapshot, \"MjpegDecodeSampleCount\", \"0\");");
        AssertContains(ssctlSnapshotPreviewSource, "private static void AppendSnapshotPreviewSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotPreviewSource, "var rendererMode = AutomationSnapshotFormatter.Get(snapshot, \"PreviewRendererMode\");");
        AssertContains(ssctlSnapshotPreviewSource, "AppendSnapshotPreviewD3DSection(builder, snapshot);");
        AssertDoesNotContain(ssctlSnapshotPreviewSource, "D3D CPU timing:");
        AssertContains(ssctlSnapshotPreviewD3DSource, "private static void AppendSnapshotPreviewD3DSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotPreviewD3DSource, "private static bool IsSnapshotPreviewD3DRendererMode(string rendererMode)");
        AssertContains(ssctlSnapshotPreviewD3DSource, "D3D CPU timing:");
        AssertContains(ssctlSnapshotPreviewD3DSource, "AutomationSnapshotFormatter.AppendPreviewSlowFrameDiagnostics(builder, snapshot);");
        AssertContains(ssctlSnapshotThreadHealthSource, "private static void AppendSnapshotThreadHealthSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotThreadHealthSource, "builder.AppendLine(\"== Thread Health ==\");");
        AssertContains(ssctlSnapshotThreadHealthSource, "WasapiPlaybackQueueDropCount");
        AssertContains(ssctlSnapshotSourceSource, "private static void AppendSnapshotSourceSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotSourceSource, "builder.AppendLine(\"== Source ==\");");
        AssertContains(ssctlSnapshotSourceSource, "var sourceFrameRate = AutomationSnapshotFormatter.Get(snapshot, \"DetectedSourceFrameRate\", string.Empty);");
        AssertContains(ReadRepoFile("tools/ssctl/Formatters.Diagnostics.cs"), "public static string FormatDiagnostics");
        AssertContains(ReadRepoFile("tools/ssctl/Formatters.Options.cs"), "public static string FormatOptions");
        var ssctlTimelineRootSource = ReadRepoFile("tools/ssctl/Formatters.Timeline.cs");
        var ssctlTimelineRowsSource = ReadRepoFile("tools/ssctl/Formatters.Timeline.Rows.cs");
        var ssctlTimelineRenderingSource = ReadRepoFile("tools/ssctl/Formatters.Timeline.Rendering.cs");
        var ssctlTimelineSummariesSource = ReadRepoFile("tools/ssctl/Formatters.Timeline.Summaries.cs");
        AssertContains(ssctlTimelineRootSource, "public static string FormatTimeline");
        AssertContains(ssctlTimelineRootSource, "var entries = ReadTimelineRows(data);");
        AssertContains(ssctlTimelineRootSource, "return RenderTimeline(entries);");
        AssertDoesNotContain(ssctlTimelineRootSource, "private sealed class TimelineRow");
        AssertDoesNotContain(ssctlTimelineRootSource, "new StringBuilder()");
        AssertDoesNotContain(ssctlTimelineRootSource, "== Trend Summary");
        AssertContains(ssctlTimelineRowsSource, "private sealed class TimelineRow");
        AssertContains(ssctlTimelineRowsSource, "AutomationSnapshotFormatter.GetDouble(item, \"CaptureFps\")");
        AssertContains(ssctlTimelineRenderingSource, "private static string RenderTimeline(IReadOnlyList<TimelineRow> entries)");
        AssertContains(ssctlTimelineRenderingSource, "Performance Timeline ({entries.Count} samples)");
        AssertContains(ssctlTimelineRenderingSource, "AppendTimelineTrendSummary(builder, entries);");
        AssertContains(ssctlTimelineSummariesSource, "private static void AppendTimelineTrendSummary(StringBuilder builder, IReadOnlyList<TimelineRow> entries)");
        AssertContains(ssctlTimelineSummariesSource, "== Trend Summary (first vs last sample) ==");
        AssertContains(ReadRepoFile("tools/ssctl/Formatters.Memory.cs"), "public static string FormatMemory");
        AssertContains(ReadRepoFile("tools/ssctl/Formatters.Common.cs"), "public static string FormatResult");
        AssertContains(ssctlFormatterSource, "CaptureCommandOldestPendingCommandAgeMs");
        AssertContains(ssctlFormatterSource, "CaptureCommandMaxQueueLatencyMs");
        AssertContains(ssctlFormatterSource, "CaptureCommandCommandsCoalesced");
        AssertContains(ssctlFormatterSource, "CaptureCommandLastOutcome");
        AssertContains(ssctlFormatterSource, "CaptureCommandLastCorrelationId");
        AssertContains(ssctlFormatterSource, "PreviewD3DInputUploadCpuP99Ms");
        AssertContains(ssctlFormatterSource, "PreviewD3DTotalFrameCpuMaxMs");
        AssertContains(ssctlFormatterSource, "ProcessCpuPercent");
        var sharedFormatterSource = ReadAutomationSnapshotFormatterSource();
        var sharedFormatterRootSource = ReadRepoFile("tools/Common/AutomationSnapshotFormatter.cs");
        var sharedFormatterPreviewSource = ReadRepoFile("tools/Common/AutomationSnapshotFormatter.Preview.cs");
        var sharedFormatterPreviewD3DSource = ReadRepoFile("tools/Common/AutomationSnapshotFormatter.PreviewD3D.cs");
        var sharedFormatterThreadHealthSource = ReadRepoFile("tools/Common/AutomationSnapshotFormatter.ThreadHealth.cs");
        AssertContains(sharedFormatterRootSource, "AppendThreadHealthSection(builder, snapshot);");
        AssertDoesNotContain(sharedFormatterRootSource, "builder.AppendLine(\"== Thread Health ==\");");
        AssertDoesNotContain(sharedFormatterRootSource, "WasapiPlaybackQueueDurationMs");
        AssertContains(sharedFormatterThreadHealthSource, "private static void AppendThreadHealthSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterThreadHealthSource, "builder.AppendLine(\"== Thread Health ==\");");
        AssertContains(sharedFormatterThreadHealthSource, "WasapiPlaybackQueueDurationMs");
        AssertContains(sharedFormatterPreviewSource, "private static void AppendPreviewSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterPreviewSource, "AppendPreviewD3DSection(builder, snapshot);");
        AssertDoesNotContain(sharedFormatterPreviewSource, "AppendPreviewSlowFrameDiagnostics(builder, snapshot);");
        AssertDoesNotContain(sharedFormatterPreviewSource, "D3D CPU timing:");
        AssertContains(sharedFormatterPreviewD3DSource, "private static void AppendPreviewD3DSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterPreviewD3DSource, "internal static void AppendPreviewSlowFrameDiagnostics(StringBuilder builder, JsonElement snapshot)");
        AssertContains(sharedFormatterPreviewD3DSource, "private static string FormatDiagnosticMs(JsonElement element, string propertyName)");
        AssertContains(sharedFormatterPreviewD3DSource, "D3D CPU timing:");
        AssertContains(sharedFormatterSource, "CaptureCommandOldestPendingCommandAgeMs");
        AssertContains(sharedFormatterSource, "CaptureCommandMaxQueueLatencyMs");
        AssertContains(sharedFormatterSource, "CaptureCommandCommandsCoalesced");
        AssertContains(sharedFormatterSource, "CaptureCommandLastOutcome");
        AssertContains(sharedFormatterSource, "CaptureCommandLastCorrelationId");
        AssertContains(sharedFormatterSource, "PreviewD3DInputUploadCpuP99Ms");
        AssertContains(sharedFormatterSource, "PreviewD3DTotalFrameCpuMaxMs");
        AssertContains(sharedFormatterSource, "ProcessCpuPercent");

        const string failedFlashbackJson = """
                                          {"Snapshot":{"SessionState":"Error","StatusText":"Flashback failed","SelectedDeviceName":"Synthetic","SelectedDeviceId":"device-1","IsInitialized":true,"IsPreviewing":false,"IsRecording":false,"FlashbackActive":false,"FlashbackEncodingFailed":true,"FlashbackEncodingFailureType":"InvalidOperationException","FlashbackEncodingFailureMessage":"Flashback queue overloaded"}}
                                          """;
        using var failedFlashbackDocument = JsonDocument.Parse(failedFlashbackJson);
        var failedFlashbackOutput = formatSnapshot.Invoke(null, new object[] { failedFlashbackDocument.RootElement })?.ToString()
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.Formatters.FormatSnapshot returned null for failed flashback snapshot.");
        AssertContains(failedFlashbackOutput, "== Flashback ==");
        AssertContains(failedFlashbackOutput, "Flashback Failure: active=true type=InvalidOperationException msg=Flashback queue overloaded");

        return Task.CompletedTask;
    }
}
