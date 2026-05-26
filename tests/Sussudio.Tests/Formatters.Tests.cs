using System.Reflection;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;

// Tests for command-line snapshot and diagnostics formatting.
static partial class Program
{
    internal static Task SsctlFormatters_EmitCoreSnapshotSections()
    {
        var assemblyPath = Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll");
        var ssctlAssembly = LoadToolAssemblyIsolated(assemblyPath);
        var formatterType = ssctlAssembly.GetType("Sussudio.Tools.Ssctl.Formatters")
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.Formatters type not found.");
        var formatSnapshot = formatterType.GetMethod("FormatSnapshot", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.Formatters.FormatSnapshot not found.");

        const string json = """
                            {"Snapshot":{"SessionState":"Ready","StatusText":"Idle","SelectedDeviceName":"Synthetic","SelectedDeviceId":"device-1","IsInitialized":true,"IsPreviewing":true,"IsRecording":false,"SelectedResolution":"3840x2160","SelectedFrameRate":120,"SelectedRecordingFormat":"HEVC","SelectedQuality":"High","SelectedPreset":"P5","SelectedSplitEncodeMode":"Auto","SelectedVideoFormat":"MJPG","PreviewVolumePercent":42.5,"IsStatsVisible":true,"IsHdrEnabled":false,"IsHdrAvailable":true,"HdrOutputActive":false,"HdrRuntimeState":"Inactive","RequestedPipelineMode":"SDR","ActivePipelineMode":"SDR","PipelineModeMatched":true,"IsAudioEnabled":true,"IsAudioPreviewEnabled":false,"IsCustomAudioInputEnabled":false,"AudioPeak":0,"AudioClipping":false,"AudioSignalPresent":false,"AudioReaderActive":false,"AudioFramesArrived":0,"AudioFramesWrittenToSink":0,"VideoReaderActive":true,"IngestVideoFramesArrived":120,"IngestVideoFramesWrittenToSink":120,"EncoderVideoFramesEnqueued":0,"EncoderVideoFramesEncoded":0,"FfmpegVideoQueueDepth":0,"VideoDropsQueueSaturated":0,"IngestLastVideoFrameAgeMs":5,"EncoderLastEnqueueAgeMs":0,"EncoderLastWriteAgeMs":0,"MemoryPreference":"Gpu","VideoRequestedSubtype":"MJPG","VideoNegotiatedSubtype":"MJPG","VideoIngestErrorCount":0,"SourceReaderReadOutstanding":false,"SourceReaderReadOutstandingMs":0,"SourceReaderLastFrameTickMs":0,"SourceReaderFrameChannelDepth":0,"WasapiCaptureCallbackCount":0,"WasapiCaptureCallbackAvgIntervalMs":0,"WasapiCaptureCallbackMaxIntervalMs":0,"WasapiCaptureCallbackSilenceCount":0,"WasapiCaptureLastCallbackTickMs":0,"WasapiCaptureAudioLevelEventsFired":0,"WasapiPlaybackRenderCallbackCount":0,"WasapiPlaybackRenderSilenceCount":0,"WasapiPlaybackQueueDepth":0,"WasapiPlaybackQueueDropCount":0,"WasapiPlaybackLastRenderTickMs":0,"OutputPath":"","RecordingTime":"00:00:00","RecordingSizeInfo":"0 B","RecordingBitrateInfo":"0 Mbps","RecordingBackend":"None","AudioPathMode":"None","MuxResult":"NotAttempted","LastOutputPath":"","LastOutputSizeBytes":0,"LastFinalizeStatus":"None","FlashbackActive":true,"FlashbackBufferedDurationMs":45000,"FlashbackDiskBytes":104857600,"FlashbackTotalBytesWritten":157286400,"FlashbackGpuEncoding":true,"FlashbackEncodedFrames":900,"FlashbackDroppedFrames":3,"FlashbackVideoQueueDepth":2,"FlashbackAudioQueueDepth":1,"FlashbackPlaybackState":"Paused","FlashbackPlaybackPositionMs":1234,"FlashbackDecoderHwAccel":"D3D11","FlashbackPlaybackObservedFps":59.9,"FlashbackPlaybackAvgFrameMs":16.7,"FlashbackPlaybackFrameCount":300,"FlashbackPlaybackLateFrames":2,"FlashbackPlaybackSubmitFailures":1,"FlashbackAvDriftMs":-1.5,"FlashbackFilePath":"temp/flashback.mp4","PerformanceScore":100,"PerformancePerfectionMet":true,"PerformanceSummary":"OK","EstimatedPipelineLatencyMs":1,"CaptureCadenceObservedFps":120,"ExpectedCaptureFrameRate":120,"CaptureCadenceSampleCount":300,"CaptureCadenceAverageIntervalMs":8.3,"CaptureCadenceP95IntervalMs":8.5,"CaptureCadenceMaxIntervalMs":9.0,"CaptureCadenceJitterStdDevMs":0.1,"CaptureCadenceSevereGapCount":0,"CaptureCadenceEstimatedDroppedFrames":0,"CaptureCadenceEstimatedDropPercent":0,"MjpegDecodeSampleCount":300,"MjpegDecodeAvgMs":2.1,"MjpegDecodeP95Ms":3.4,"MjpegDecodeMaxMs":5.6,"MjpegInteropCopySampleCount":300,"MjpegInteropCopyAvgMs":0.9,"MjpegInteropCopyP95Ms":1.4,"MjpegInteropCopyMaxMs":2.2,"MjpegCallbackSampleCount":300,"MjpegCallbackAvgMs":4.5,"MjpegCallbackP95Ms":6.7,"MjpegCallbackMaxMs":9.1,"MjpegDecoderCount":2,"MjpegReorderSampleCount":300,"MjpegReorderAvgMs":0.4,"MjpegReorderP95Ms":0.8,"MjpegReorderMaxMs":1.2,"MjpegPipelineSampleCount":300,"MjpegPipelineAvgMs":5.1,"MjpegPipelineP95Ms":7.0,"MjpegPipelineMaxMs":9.4,"MjpegTotalDecoded":301,"MjpegTotalEmitted":300,"MjpegTotalDropped":1,"MjpegReorderSkips":2,"MjpegReorderBufferDepth":1,"MjpegPerDecoder":[{"WorkerIndex":0,"SampleCount":150,"AvgMs":2.0,"P95Ms":3.0,"MaxMs":4.0},{"WorkerIndex":1,"SampleCount":151,"AvgMs":2.2,"P95Ms":3.2,"MaxMs":4.2}],"PreviewRendererMode":"D3D11VideoProcessor","PreviewStartupState":"Rendering","PreviewFirstVisualConfirmed":true,"PreviewD3DFramesSubmitted":120,"PreviewD3DFramesRendered":120,"PreviewD3DFramesDropped":0,"PreviewD3DInputColorSpace":"BT.709","PreviewD3DOutputColorSpace":"sRGB","PreviewCadenceObservedFps":120,"DetectedSourceFrameRate":120,"SourceWidth":3840,"SourceHeight":2160,"SourceIsHdr":false,"SourceTelemetryAvailability":"Available","SourceTelemetryConfidence":"High"}}
                            """;
        var jsonWithEncoder = json.Replace(
            "\"FlashbackActive\":true,",
            "\"FlashbackActive\":true,\"EncoderCodecName\":\"hevc_nvenc\",\"EncoderWidth\":3840,\"EncoderHeight\":2160,\"EncoderFrameRate\":120,\"EncoderFrameRateNumerator\":120,\"EncoderFrameRateDenominator\":1,\"EncoderTargetBitRate\":12345678,",
            StringComparison.Ordinal).Replace(
            "\"DetectedSourceFrameRate\":120,",
            "\"AvSyncCaptureDriftMs\":1.5,\"AvSyncCaptureDriftRateMsPerSec\":0.1,\"AvSyncEncoderDriftMs\":-0.5,\"AvSyncEncoderCorrectionSamples\":2,\"DetectedSourceFrameRate\":120,",
            StringComparison.Ordinal);
        const string d3dSnapshotFields = """
                                         "PreviewD3DCpuTimingSampleCount":120,
                                         "PreviewD3DInputUploadCpuAvgMs":0.1,
                                         "PreviewD3DInputUploadCpuP95Ms":0.2,
                                         "PreviewD3DInputUploadCpuP99Ms":0.3,
                                         "PreviewD3DInputUploadCpuMaxMs":0.4,
                                         "PreviewD3DRenderSubmitCpuAvgMs":0.5,
                                         "PreviewD3DRenderSubmitCpuP95Ms":0.6,
                                         "PreviewD3DRenderSubmitCpuP99Ms":0.7,
                                         "PreviewD3DRenderSubmitCpuMaxMs":0.8,
                                         "PreviewD3DPresentCallAvgMs":0.9,
                                         "PreviewD3DPresentCallP95Ms":1.0,
                                         "PreviewD3DPresentCallP99Ms":1.1,
                                         "PreviewD3DPresentCallMaxMs":1.2,
                                         "PreviewD3DTotalFrameCpuAvgMs":1.3,
                                         "PreviewD3DTotalFrameCpuP95Ms":1.4,
                                         "PreviewD3DTotalFrameCpuP99Ms":1.5,
                                         "PreviewD3DTotalFrameCpuMaxMs":1.6,
                                         "PreviewD3DPipelineLatencySampleCount":120,
                                         "PreviewD3DPipelineLatencyAvgMs":7.8,
                                         "PreviewD3DPipelineLatencyP95Ms":8.9,
                                         "PreviewD3DPipelineLatencyP99Ms":9.9,
                                         "PreviewD3DPipelineLatencyMaxMs":12.3,
                                         "PreviewD3DLastRenderedPipelineLatencyMs":8.4,
                                         "PreviewD3DFrameLatencyWaitEnabled":true,
                                         "PreviewD3DFrameLatencyWaitHandleActive":true,
                                         "PreviewD3DFrameLatencyWaitCallCount":118,
                                         "PreviewD3DFrameLatencyWaitSignaledCount":110,
                                         "PreviewD3DFrameLatencyWaitTimeoutCount":8,
                                         "PreviewD3DFrameLatencyWaitUnexpectedResultCount":0,
                                         "PreviewD3DFrameLatencyWaitLastResult":0,
                                         "PreviewD3DFrameLatencyWaitLastMs":0.05,
                                         "PreviewD3DFrameLatencyWaitSampleCount":118,
                                         "PreviewD3DFrameLatencyWaitAvgMs":0.2,
                                         "PreviewD3DFrameLatencyWaitP95Ms":0.8,
                                         "PreviewD3DFrameLatencyWaitP99Ms":1.4,
                                         "PreviewD3DFrameLatencyWaitMaxMs":2.0,
                                         "PreviewD3DFrameStatsSampleCount":120,
                                         "PreviewD3DFrameStatsSuccessCount":119,
                                         "PreviewD3DFrameStatsFailureCount":1,
                                         "PreviewD3DFrameStatsRecentFailureCount":1,
                                         "PreviewD3DFrameStatsMissedRefreshCount":4,
                                         "PreviewD3DFrameStatsRecentMissedRefreshCount":2,
                                         "PreviewD3DFrameStatsLastError":"DXGI_ERROR_WAS_STILL_DRAWING",
                                         "PreviewD3DLastSubmittedPreviewPresentId":41,
                                         "PreviewD3DLastSubmittedSourceSequenceNumber":9000,
                                         "PreviewD3DLastSubmittedSourcePtsTicks":123456,
                                         "PreviewD3DLastRenderedPreviewPresentId":42,
                                         "PreviewD3DLastRenderedSourceSequenceNumber":9001,
                                         "PreviewD3DLastRenderedSourcePtsTicks":123789,
                                         "PreviewD3DLastRenderedSchedulerToPresentMs":7.7,
                                         "PreviewD3DLastDropReason":"none",
                                         "PreviewD3DLastDroppedSourcePtsTicks":0,
                                         "PreviewD3DRecentSlowFrames":[{"PreviewPresentId":42,"SourceSequenceNumber":9001,"PresentIntervalMs":9.2,"InputUploadCpuMs":1.1,"RenderSubmitCpuMs":2.2,"PresentCallMs":3.3,"TotalFrameCpuMs":6.6,"SchedulerToPresentMs":7.7,"PipelineLatencyMs":8.8,"ExpectedIntervalMs":8.33,"DiagnosticThresholdMs":8.5,"WorstOverBudgetMs":0.87,"SlowReason":"present_interval","PendingFrameCount":1,"DxgiPresentDelta":1,"DxgiPresentRefreshDelta":2,"DxgiSyncRefreshDelta":2}],
                                         """;
        var jsonWithD3D = jsonWithEncoder.Replace(
            "\"PreviewFirstVisualConfirmed\":true,",
            "\"PreviewFirstVisualConfirmed\":true," + d3dSnapshotFields,
            StringComparison.Ordinal);
        using var document = JsonDocument.Parse(jsonWithD3D);
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
        AssertContains(output, "D3D CPU timing: input/upload avg=0.1ms P95=0.2ms P99=0.3ms max=0.4ms | render-submit avg=0.5ms P95=0.6ms P99=0.7ms max=0.8ms | present-call avg=0.9ms P95=1.0ms P99=1.1ms max=1.2ms | total-frame avg=1.3ms P95=1.4ms P99=1.5ms max=1.6ms samples=120");
        AssertContains(output, "D3D pipeline latency: avg=7.8ms P95=8.9ms P99=9.9ms max=12.3ms last=8.4ms samples=120");
        AssertContains(output, "D3D frame-latency wait: enabled=true handle=true calls=118 signaled=110 timeouts=8 unexpected=0 lastResult=0 last=0.05ms avg=0.2ms P95=0.8ms max=2.0ms samples=118");
        AssertContains(output, "D3D DXGI stats: ok=119/120 failures=1 recentFailures=1 missedRefresh=4 recentMissed=2 lastError=DXGI_ERROR_WAS_STILL_DRAWING");
        AssertContains(output, "D3D Ownership: submitted present=41 sourceSeq=9000 pts=123456 | rendered present=42 sourceSeq=9001 pts=123789 schedulerToPresent=7.7ms pipeline=8.4ms | lastDrop=none dropPts=0");
        AssertContains(output, "D3D Slow Frames: present=42 srcSeq=9001 reason=present_interval target=8.33ms over=0.87ms interval=9.20ms");
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
        AssertOccursBefore(output, "D3D CPU timing:", "D3D pipeline latency:");
        AssertOccursBefore(output, "D3D pipeline latency:", "D3D frame-latency wait:");
        AssertOccursBefore(output, "D3D frame-latency wait:", "D3D DXGI stats:");
        AssertOccursBefore(output, "D3D DXGI stats:", "D3D Ownership:");
        AssertOccursBefore(output, "D3D Ownership:", "D3D Slow Frames:");
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

    internal static Task SsctlFormatters_TimelineOutputPreservesTableAndSummary()
    {
        var assemblyPath = Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll");
        var ssctlAssembly = LoadToolAssemblyIsolated(assemblyPath);
        var formatterType = ssctlAssembly.GetType("Sussudio.Tools.Ssctl.Formatters")
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.Formatters type not found.");
        var formatTimeline = formatterType.GetMethod("FormatTimeline", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.Formatters.FormatTimeline not found.");

        const string json = """
                            {
                              "Data": [
                                {
                                  "TimestampUtc": "2026-05-15T00:00:00Z",
                                  "CaptureFps": 119.5,
                                  "PreviewFps": 118.2,
                                  "VideoQueueDepth": 1,
                                  "VideoDrops": 2,
                                  "CaptureCadenceAverageMs": 8.0,
                                  "CaptureCadenceP95Ms": 8.4,
                                  "CaptureCadenceP99Ms": 8.8,
                                  "CaptureCadenceMaxMs": 10.0,
                                  "CaptureCadenceOnePercentLowFps": 112.0,
                                  "PreviewCadenceAverageMs": 8.1,
                                  "PreviewCadenceP95Ms": 8.6,
                                  "PreviewCadenceMaxMs": 11.0,
                                  "PreviewCadenceOnePercentLowFps": 110.0,
                                  "PreviewCadenceSlowFramePercent": 0.5,
                                  "PreviewD3DPendingFrameCount": 0,
                                  "PreviewD3DPresentCallP95Ms": 0.6,
                                  "PreviewD3DTotalFrameCpuP95Ms": 1.5,
                                  "PreviewD3DPipelineLatencyP95Ms": 2.5,
                                  "PreviewD3DFrameLatencyWaitTimeoutCount": 0,
                                  "PreviewD3DFrameLatencyWaitP95Ms": 0.2,
                                  "PreviewD3DFrameStatsRecentMissedRefreshCount": 0,
                                  "PreviewD3DFrameStatsRecentFailureCount": 0,
                                  "PipelineLatencyMs": 3,
                                  "ProcessCpuPercent": 7.5,
                                  "MemoryWorkingSetMb": 200.0,
                                  "MemoryManagedHeapMb": 40.0,
                                  "GcGen0Collections": 1,
                                  "GcGen1Collections": 0,
                                  "GcGen2Collections": 0,
                                  "GcPauseTimePercent": 0.1,
                                  "ThreadPoolWorkerAvailable": 32760,
                                  "ThreadPoolIoAvailable": 1000
                                },
                                {
                                  "TimestampUtc": "2026-05-15T00:00:01Z",
                                  "CaptureFps": 118.0,
                                  "PreviewFps": 117.0,
                                  "VideoQueueDepth": 2,
                                  "VideoDrops": 5,
                                  "CaptureCadenceAverageMs": 8.5,
                                  "CaptureCadenceP95Ms": 9.0,
                                  "CaptureCadenceP99Ms": 9.5,
                                  "CaptureCadenceMaxMs": 12.0,
                                  "CaptureCadenceOnePercentLowFps": 108.0,
                                  "PreviewCadenceAverageMs": 8.8,
                                  "PreviewCadenceP95Ms": 9.2,
                                  "PreviewCadenceMaxMs": 13.0,
                                  "PreviewCadenceOnePercentLowFps": 105.0,
                                  "PreviewCadenceSlowFramePercent": 1.5,
                                  "PreviewD3DPendingFrameCount": 1,
                                  "PreviewD3DPresentCallP95Ms": 0.8,
                                  "PreviewD3DTotalFrameCpuP95Ms": 1.8,
                                  "PreviewD3DPipelineLatencyP95Ms": 2.9,
                                  "PreviewD3DFrameLatencyWaitTimeoutCount": 1,
                                  "PreviewD3DFrameLatencyWaitP95Ms": 0.4,
                                  "PreviewD3DFrameStatsRecentMissedRefreshCount": 2,
                                  "PreviewD3DFrameStatsRecentFailureCount": 1,
                                  "PipelineLatencyMs": 4,
                                  "ProcessCpuPercent": 8.5,
                                  "MemoryWorkingSetMb": 205.0,
                                  "MemoryManagedHeapMb": 42.0,
                                  "GcGen0Collections": 3,
                                  "GcGen1Collections": 1,
                                  "GcGen2Collections": 1,
                                  "GcPauseTimePercent": 0.2,
                                  "ThreadPoolWorkerAvailable": 32759,
                                  "ThreadPoolIoAvailable": 999
                                }
                              ]
                            }
                            """;

        using var document = JsonDocument.Parse(json);
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        string output;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            output = formatTimeline.Invoke(null, new object[] { document.RootElement })?.ToString()
                ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.Formatters.FormatTimeline returned null.");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }

        AssertContains(output, "Performance Timeline (2 samples)");
        AssertContains(output, "Timestamp                | CapAvg | CapP95");
        AssertContains(output, "2026-05-15T00:00:00Z");
        AssertContains(output, "== Trend Summary (first vs last sample) ==");
        AssertContains(output, "Capture Avg:    8.0ms -> 8.5ms (delta: +0.5ms)");
        AssertContains(output, "Video Drops:    2 -> 5 (delta: +3)");
        AssertContains(output, "Working Set:    200.0MB -> 205.0MB (delta: +5.0MB)");

        return Task.CompletedTask;
    }
}
