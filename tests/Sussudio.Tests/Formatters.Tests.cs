using System.Reflection;
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
        using var document = JsonDocument.Parse(json);
        var output = formatSnapshot.Invoke(null, new object[] { document.RootElement })?.ToString()
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.Formatters.FormatSnapshot returned null.");

        AssertContains(output, "== Sussudio State ==");
        AssertContains(output, "Capture Commands:");
        AssertContains(output, "== Capture Settings ==");
        AssertContains(output, "== Audio ==");
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
        AssertContains(output, "Written: 150 MB");
        AssertContains(output, "submitFailures=1");
        AssertContains(output, "A/V Drift: -1.5ms");
        var ssctlFormatterRoot = ReadRepoFile("tools/ssctl/Formatters.cs");
        var ssctlFormatterSource = ReadSsctlSnapshotFormatterSource();
        var ssctlSnapshotRootSource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.cs");
        var ssctlSnapshotFlashbackSource = ReadRepoFile("tools/ssctl/Formatters.Snapshot.Flashback.cs");
        AssertDoesNotContain(ssctlFormatterRoot, "public static string FormatSnapshot");
        AssertContains(ssctlSnapshotRootSource, "AppendSnapshotFlashbackSection(builder, snapshot);");
        AssertDoesNotContain(ssctlSnapshotRootSource, "var flashbackActive = AutomationSnapshotFormatter.Get(snapshot, \"FlashbackActive\", \"false\");");
        AssertContains(ssctlSnapshotFlashbackSource, "private static void AppendSnapshotFlashbackSection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotFlashbackSource, "var flashbackActive = AutomationSnapshotFormatter.Get(snapshot, \"FlashbackActive\", \"false\");");
        AssertContains(ReadRepoFile("tools/ssctl/Formatters.Diagnostics.cs"), "public static string FormatDiagnostics");
        AssertContains(ReadRepoFile("tools/ssctl/Formatters.Options.cs"), "public static string FormatOptions");
        AssertContains(ReadRepoFile("tools/ssctl/Formatters.Timeline.cs"), "public static string FormatTimeline");
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
