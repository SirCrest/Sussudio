using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests
{

public sealed class AutomationSnapshotFormatterContractsTests
{
    [Fact]
    public Task FormatsCoreSectionsAndTypedAccessors()
        => global::Program.AutomationSnapshotFormatter_FormatsCoreSectionsAndTypedAccessors();

    [Fact]
    public Task RendersFlashbackSectionsWhenIncluded()
        => global::Program.AutomationSnapshotFormatter_RendersFlashbackSections_WhenIncluded();

    [Fact]
    public Task RendersPreviewD3DSections()
        => global::Program.AutomationSnapshotFormatter_RendersPreviewD3DSections();

    [Fact]
    public Task SourceOwnershipIsSplit()
        => global::Program.AutomationSnapshotFormatter_SourceOwnership_IsSplit();
}

public sealed class SsctlFormatterContractsTests
{
    [Fact]
    public Task EmitsCoreSnapshotSections()
    {
        var assemblyPath = Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll");
        var ssctlAssembly = ToolFormatterTestAssembly.Load(assemblyPath);
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

    [Fact]
    public Task TimelineOutputPreservesTableAndSummary()
    {
        var assemblyPath = Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll");
        var ssctlAssembly = ToolFormatterTestAssembly.Load(assemblyPath);
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

    [Fact]
    public Task SourceOwnershipIsUnified()
    {
        var ssctlFormatterSource = global::Sussudio.Tests.RuntimeContractSource.ReadSsctlSnapshotFormatterSource();
        var ssctlSnapshotRootSource = ssctlFormatterSource;
        var ssctlSnapshotFlashbackSource = ssctlSnapshotRootSource;
        var ssctlSnapshotMjpegSource = ssctlSnapshotRootSource;
        AssertContains(ssctlFormatterSource, "internal static class Formatters");
        AssertDoesNotContain(ssctlFormatterSource, "partial class Formatters");
        AssertEqual(
            false,
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "tools", "ssctl", "Formatters.Common.cs")),
            "ssctl shared result helpers live with the unified formatter owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "tools", "ssctl", "Formatters.Snapshot.cs")),
            "ssctl snapshot text lives with the unified formatter owner");
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
        AssertContains(ssctlSnapshotRootSource, "var flashbackActive = AutomationSnapshotFormatter.Get(snapshot, \"FlashbackActive\", \"false\");");
        AssertContains(ssctlSnapshotRootSource, "private static void AppendSnapshotMemorySection(StringBuilder builder, JsonElement snapshot)");
        AssertContains(ssctlSnapshotRootSource, "builder.AppendLine(\"== Memory & GC ==\");");
        AssertContains(ssctlSnapshotRootSource, "ProcessCpuPercent");
        AssertContains(ssctlSnapshotRootSource, "var mjpegDecodeSamples = AutomationSnapshotFormatter.Get(snapshot, \"MjpegDecodeSampleCount\", \"0\");");
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
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "tools", "ssctl", "Formatters.Snapshot.PreviewD3D.cs")),
            "ssctl D3D preview snapshot text lives with the preview routing owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "tools", "ssctl", "Formatters.Snapshot.ThreadHealth.cs")),
            "ssctl thread-health snapshot text lives with the root formatter flow");
        AssertContains(ssctlFormatterSource, "public static string FormatDiagnostics");
        AssertContains(ssctlFormatterSource, "public static string FormatOptions");
        AssertContains(ssctlFormatterSource, "public static string FormatDeviceList");
        AssertEqual(
            false,
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "tools", "ssctl", "Formatters.Options.cs")),
            "ssctl capture option and device-list output lives with the root formatter helpers");
        AssertContains(ssctlFormatterSource, "public static string FormatTimeline");
        AssertContains(ssctlFormatterSource, "var entries = ReadTimelineRows(data);");
        AssertContains(ssctlFormatterSource, "return RenderTimeline(entries);");
        AssertContains(ssctlFormatterSource, "private sealed class TimelineRow");
        AssertContains(ssctlFormatterSource, "AutomationSnapshotFormatter.GetDouble(item, \"CaptureFps\")");
        AssertContains(ssctlFormatterSource, "private static string RenderTimeline(IReadOnlyList<TimelineRow> entries)");
        AssertContains(ssctlFormatterSource, "Performance Timeline ({entries.Count} samples)");
        AssertContains(ssctlFormatterSource, "AppendTimelineTrendSummary(builder, entries);");
        AssertContains(ssctlFormatterSource, "private static void AppendTimelineTrendSummary(StringBuilder builder, IReadOnlyList<TimelineRow> entries)");
        AssertContains(ssctlFormatterSource, "== Trend Summary (first vs last sample) ==");
        AssertEqual(
            false,
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "tools", "ssctl", "Formatters.Timeline.cs")),
            "ssctl timeline table and trend output lives with the root formatter helpers");
        AssertContains(ssctlFormatterSource, "public static string FormatMemory");
        AssertContains(ssctlFormatterSource, "public static string FormatResult");
        AssertEqual(
            false,
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "tools", "ssctl", "Formatters.Diagnostics.cs")),
            "ssctl diagnostic-event output lives with the root formatter helpers");
        AssertEqual(
            false,
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "tools", "ssctl", "Formatters.Memory.cs")),
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

    private static void AssertContains(string value, string token)
    {
        var normalizedValue = NormalizeLineEndings(value);
        var normalizedToken = NormalizeLineEndings(token);
        Assert.True(
            normalizedValue.IndexOf(normalizedToken, StringComparison.OrdinalIgnoreCase) >= 0,
            $"Expected value to contain '{token}'.");
    }

    private static void AssertDoesNotContain(string value, string token)
    {
        var normalizedValue = NormalizeLineEndings(value);
        var normalizedToken = NormalizeLineEndings(token);
        Assert.True(
            normalizedValue.IndexOf(normalizedToken, StringComparison.OrdinalIgnoreCase) < 0,
            $"Expected value not to contain '{token}'.");
    }

    private static void AssertEqual<T>(T expected, T actual, string fieldName)
    {
        Assert.True(
            EqualityComparer<T>.Default.Equals(expected, actual),
            $"Assertion failed for {fieldName}: expected '{expected}', actual '{actual}'.");
    }

    private static void AssertOccursBefore(string value, string earlierToken, string laterToken)
    {
        var normalizedValue = NormalizeLineEndings(value);
        var normalizedEarlierToken = NormalizeLineEndings(earlierToken);
        var normalizedLaterToken = NormalizeLineEndings(laterToken);
        var earlier = normalizedValue.IndexOf(normalizedEarlierToken, StringComparison.Ordinal);
        var later = normalizedValue.IndexOf(normalizedLaterToken, StringComparison.Ordinal);
        Assert.True(
            earlier >= 0 && later >= 0 && earlier < later,
            $"Expected '{earlierToken}' to occur before '{laterToken}'.");
    }

    private static string NormalizeLineEndings(string value)
        => value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
}

public sealed class ToolFormatterContractsTests
{
    [Fact]
    public void ResponseFormatter_IsSuccess_ParsesSuccessAndFailureJson()
    {
        var formatterType = RequireSharedToolType("Sussudio.Tools.AutomationSnapshotFormatter");
        var isSuccess = RequireStaticMethod(formatterType, "IsSuccess");

        using (var docTrue = JsonDocument.Parse("{\"Success\": true, \"Message\": \"ok\"}"))
        {
            Assert.True((bool)isSuccess.Invoke(null, new object[] { docTrue.RootElement })!);
        }

        using (var docFalse = JsonDocument.Parse("{\"Success\": false, \"Message\": \"fail\"}"))
        {
            Assert.False((bool)isSuccess.Invoke(null, new object[] { docFalse.RootElement })!);
        }

        using (var docMissing = JsonDocument.Parse("{\"Message\": \"no success field\"}"))
        {
            Assert.False((bool)isSuccess.Invoke(null, new object[] { docMissing.RootElement })!);
        }
    }

    [Fact]
    public void ResponseFormatter_Get_HandlesAllJsonValueKinds()
    {
        var formatterType = RequireSharedToolType("Sussudio.Tools.AutomationSnapshotFormatter");
        var get = RequireStaticMethod(formatterType, "Get");

        const string json = """
                            {
                                "str": "hello",
                                "num": 42,
                                "boolTrue": true,
                                "boolFalse": false,
                                "nullVal": null,
                                "emptyArr": [],
                                "nonEmptyArr": [1, 2],
                                "obj": { "nested": true },
                                "emptyStr": ""
                            }
                            """;

        using var doc = JsonDocument.Parse(json);
        var el = doc.RootElement;

        Assert.Equal("hello", (string)get.Invoke(null, new object[] { el, "str", "N/A" })!);
        Assert.Equal("42", (string)get.Invoke(null, new object[] { el, "num", "N/A" })!);
        Assert.Equal("true", (string)get.Invoke(null, new object[] { el, "boolTrue", "N/A" })!);
        Assert.Equal("false", (string)get.Invoke(null, new object[] { el, "boolFalse", "N/A" })!);
        Assert.Equal("N/A", (string)get.Invoke(null, new object[] { el, "nullVal", "N/A" })!);
        Assert.Equal("N/A", (string)get.Invoke(null, new object[] { el, "nonExistent", "N/A" })!);
        Assert.Equal("custom", (string)get.Invoke(null, new object[] { el, "nonExistent", "custom" })!);
        Assert.Equal("N/A", (string)get.Invoke(null, new object[] { el, "emptyArr", "N/A" })!);
        Assert.Equal(string.Empty, (string)get.Invoke(null, new object[] { el, "emptyStr", "N/A" })!);
    }

    [Fact]
    public void SharedFormatter_RendersMjpegTimingSection_WhenFieldsExist()
    {
        var formatterType = RequireSharedToolType("Sussudio.Tools.AutomationSnapshotFormatter");
        var formatSnapshot = RequireStaticMethod(formatterType, "FormatSnapshot");

        const string json = """
                            {"Snapshot":{"SessionState":"Ready","StatusText":"Idle","SelectedDeviceName":"Synthetic","SelectedDeviceId":"device-1","IsInitialized":true,"IsPreviewing":true,"IsRecording":false,"SelectedResolution":"3840x2160","SelectedFrameRate":120,"SelectedRecordingFormat":"HEVC","SelectedQuality":"High","SelectedPreset":"P5","SelectedSplitEncodeMode":"Auto","SelectedVideoFormat":"MJPG","PreviewVolumePercent":42.5,"IsStatsVisible":true,"IsHdrEnabled":false,"IsHdrAvailable":true,"HdrOutputActive":false,"HdrRuntimeState":"Inactive","RequestedPipelineMode":"SDR","ActivePipelineMode":"SDR","PipelineModeMatched":true,"IsAudioEnabled":true,"IsAudioPreviewEnabled":false,"IsCustomAudioInputEnabled":false,"AudioPeak":0,"AudioClipping":false,"AudioSignalPresent":false,"AudioReaderActive":false,"AudioFramesArrived":0,"AudioFramesWrittenToSink":0,"VideoReaderActive":true,"IngestVideoFramesArrived":120,"IngestVideoFramesWrittenToSink":120,"EncoderVideoFramesEnqueued":0,"EncoderVideoFramesEncoded":0,"FfmpegVideoQueueDepth":0,"VideoDropsQueueSaturated":0,"IngestLastVideoFrameAgeMs":5,"EncoderLastEnqueueAgeMs":0,"EncoderLastWriteAgeMs":0,"MemoryPreference":"Gpu","VideoRequestedSubtype":"MJPG","VideoNegotiatedSubtype":"MJPG","VideoIngestErrorCount":0,"SourceReaderReadOutstanding":false,"SourceReaderReadOutstandingMs":0,"SourceReaderLastFrameTickMs":0,"SourceReaderFrameChannelDepth":0,"WasapiCaptureCallbackCount":0,"WasapiCaptureCallbackAvgIntervalMs":0,"WasapiCaptureCallbackMaxIntervalMs":0,"WasapiCaptureCallbackSilenceCount":0,"WasapiCaptureLastCallbackTickMs":0,"WasapiCaptureAudioLevelEventsFired":0,"WasapiPlaybackRenderCallbackCount":0,"WasapiPlaybackRenderSilenceCount":0,"WasapiPlaybackQueueDepth":0,"WasapiPlaybackQueueDropCount":0,"WasapiPlaybackLastRenderTickMs":0,"OutputPath":"","RecordingTime":"00:00:00","RecordingSizeInfo":"0 B","RecordingBitrateInfo":"0 Mbps","RecordingBackend":"None","AudioPathMode":"None","MuxResult":"NotAttempted","LastOutputPath":"","LastOutputSizeBytes":0,"LastFinalizeStatus":"None","PerformanceScore":100,"PerformancePerfectionMet":true,"PerformanceSummary":"OK","EstimatedPipelineLatencyMs":1,"CaptureCadenceObservedFps":120,"ExpectedCaptureFrameRate":120,"CaptureCadenceSampleCount":300,"CaptureCadenceAverageIntervalMs":8.3,"CaptureCadenceP95IntervalMs":8.5,"CaptureCadenceMaxIntervalMs":9.0,"CaptureCadenceJitterStdDevMs":0.1,"CaptureCadenceSevereGapCount":0,"CaptureCadenceEstimatedDroppedFrames":0,"CaptureCadenceEstimatedDropPercent":0,"MjpegDecodeSampleCount":300,"MjpegDecodeAvgMs":2.1,"MjpegDecodeP95Ms":3.4,"MjpegDecodeMaxMs":5.6,"MjpegInteropCopySampleCount":300,"MjpegInteropCopyAvgMs":0.9,"MjpegInteropCopyP95Ms":1.4,"MjpegInteropCopyMaxMs":2.2,"MjpegCallbackSampleCount":300,"MjpegCallbackAvgMs":4.5,"MjpegCallbackP95Ms":6.7,"MjpegCallbackMaxMs":9.1,"MjpegDecoderCount":2,"MjpegReorderSampleCount":300,"MjpegReorderAvgMs":0.4,"MjpegReorderP95Ms":0.8,"MjpegReorderMaxMs":1.2,"MjpegPipelineSampleCount":300,"MjpegPipelineAvgMs":5.1,"MjpegPipelineP95Ms":7.0,"MjpegPipelineMaxMs":9.4,"MjpegTotalDecoded":301,"MjpegTotalEmitted":300,"MjpegTotalDropped":1,"MjpegReorderSkips":2,"MjpegReorderBufferDepth":1,"MjpegPerDecoder":[{"WorkerIndex":0,"SampleCount":150,"AvgMs":2.0,"P95Ms":3.0,"MaxMs":4.0},{"WorkerIndex":1,"SampleCount":151,"AvgMs":2.2,"P95Ms":3.2,"MaxMs":4.2}],"PreviewRendererMode":"D3D11VideoProcessor","PreviewStartupState":"Rendering","PreviewFirstVisualConfirmed":true,"PreviewD3DFramesSubmitted":120,"PreviewD3DFramesRendered":120,"PreviewD3DFramesDropped":0,"PreviewD3DInputColorSpace":"BT.709","PreviewD3DOutputColorSpace":"sRGB","PreviewCadenceObservedFps":120,"PreviewPacingLikelySlowStage":"MjpegDecode","PreviewPacingSlowStageConfidence":"Medium","PreviewPacingSlowStageEvidence":"decode p95 over budget","DetectedSourceFrameRate":120,"SourceWidth":3840,"SourceHeight":2160,"SourceIsHdr":false,"SourceTelemetryAvailability":"Available","SourceTelemetryConfidence":"High"}}
                            """;
        using var document = JsonDocument.Parse(json);
        var output = formatSnapshot.Invoke(null, new object[] { document.RootElement, false })?.ToString()
            ?? throw new InvalidOperationException("AutomationSnapshotFormatter.FormatSnapshot returned null.");

        Assert.Contains("== MJPEG Pipeline Timing ==", output);
        Assert.Contains("Preset: P5", output);
        Assert.Contains("Video Format: MJPG | Split Encode: Auto | MJPEG Decoders: 2", output);
        Assert.Contains("UI: Preview Volume=42.5% | Stats Visible=true", output);
        Assert.Contains("Decode: avg=2.1ms", output);
        Assert.Contains("Interop Copy: avg=0.9ms", output);
        Assert.Contains("Total Callback: avg=4.5ms", output);
        Assert.Contains("Decoders: 2 | Decoded=301 Emitted=300 Dropped=1", output);
        Assert.Contains("Reorder: avg=0.4ms", output);
        Assert.Contains("Pipeline: avg=5.1ms", output);
        Assert.Contains("== Diagnostics ==", output);
        Assert.Contains("Legacy Score:", output);
        Assert.Contains("Pacing Classifier: stage=MjpegDecode confidence=Medium evidence=decode p95 over budget", output);
        Assert.Contains("Frame Time:", output);
        Assert.Contains("Average Rate:", output);
        Assert.Contains("Decoder[0]: avg=2.0ms", output);
        Assert.Contains("Decoder[1]: avg=2.2ms", output);
    }

    [Fact]
    public void SsctlFormatters_SnapshotFields_AlignWithMcpResponseFormatter()
    {
        var mcpFields = ExtractSnapshotFields(RuntimeContractSource.ReadAutomationSnapshotFormatterSource());
        var ssctlFields = ExtractSnapshotFields(RuntimeContractSource.ReadSsctlSnapshotFormatterSource());

        Assert.NotEmpty(mcpFields);
        Assert.NotEmpty(ssctlFields);

        var missingInSsctl = new List<string>();
        foreach (var field in mcpFields)
        {
            if (!ssctlFields.Contains(field))
            {
                missingInSsctl.Add(field);
            }
        }

        Assert.True(
            missingInSsctl.Count == 0,
            $"AutomationSnapshotFormatter references {missingInSsctl.Count} snapshot field(s) missing from ssctl Formatters: {string.Join(", ", missingInSsctl)}");
    }

    private static Type RequireSharedToolType(string typeName)
    {
        var assembly = ToolFormatterTestAssembly.Load(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        return assembly.GetType(typeName)
               ?? throw new InvalidOperationException($"{typeName} was not found in the shared tool assembly.");
    }

    private static MethodInfo RequireStaticMethod(Type type, string name)
        => type.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
           ?? throw new InvalidOperationException($"{type.FullName}.{name} was not found.");

    private static HashSet<string> ExtractSnapshotFields(string sourceText)
    {
        var fields = new HashSet<string>(StringComparer.Ordinal);
        foreach (var callPrefix in new[]
        {
            "Get(snapshot,",
            "GetInt(snapshot,",
            "GetDouble(snapshot,",
            "GetLong(snapshot,",
            "GetNullableLong(snapshot,",
            "GetBool(snapshot,",
            "GetString(snapshot,",
            "FormatFrameBudgetMs(snapshot,",
            "FormatIntervalMs(snapshot,"
        })
        {
            ExtractSnapshotFieldsFromCalls(sourceText, callPrefix, fields);
        }

        return fields;
    }

    private static void ExtractSnapshotFieldsFromCalls(string sourceText, string callPrefix, HashSet<string> fields)
    {
        var index = 0;
        while (index < sourceText.Length)
        {
            var callIdx = sourceText.IndexOf(callPrefix, index, StringComparison.Ordinal);
            if (callIdx < 0)
            {
                break;
            }

            var afterComma = callIdx + callPrefix.Length;
            var quoteIdx = sourceText.IndexOf('"', afterComma);
            if (quoteIdx < 0 || quoteIdx - afterComma > 10)
            {
                index = afterComma;
                continue;
            }

            var endQuoteIdx = sourceText.IndexOf('"', quoteIdx + 1);
            if (endQuoteIdx < 0)
            {
                index = quoteIdx + 1;
                continue;
            }

            var fieldName = sourceText.Substring(quoteIdx + 1, endQuoteIdx - quoteIdx - 1);
            if (fieldName.Length > 0)
            {
                fields.Add(fieldName);
            }

            index = endQuoteIdx + 1;
        }
    }
}

public sealed class SsctlCommandHandlerContractsTests
{
    [Fact]
    public Task RoutesDeviceCommands()
        => global::Program.SsctlCommandHandlers_RouteDeviceCommands();

    [Fact]
    public Task RoutesCaptureControlCommands()
        => global::Program.SsctlCommandHandlers_RouteCaptureControlCommands();

    [Fact]
    public Task RoutesRecordingsCommands()
        => global::Program.SsctlCommandHandlers_RouteRecordingsCommands();

    [Fact]
    public Task RoutesFlashbackCommands()
        => global::Program.SsctlCommandHandlers_RouteFlashbackCommands();

    [Fact]
    public Task RoutesWindowCommands()
        => global::Program.SsctlCommandHandlers_RouteWindowCommands();

    [Fact]
    public Task RoutesManifestCommand()
        => global::Program.SsctlCommandHandlers_RouteManifestCommand();

    [Fact]
    public Task RoutesObservabilityCommands()
        => global::Program.SsctlCommandHandlers_RouteObservabilityCommands();

    [Fact]
    public Task RoutesAutomationFlowCommands()
        => global::Program.SsctlCommandHandlers_RouteAutomationFlowCommands();

    [Fact]
    public Task RoutesUiVisibilityCommands()
        => global::Program.SsctlCommandHandlers_RouteUiVisibilityCommands();

    [Fact]
    public Task RoutesVerificationCommands()
        => global::Program.SsctlCommandHandlers_RouteVerificationCommands();

    [Fact]
    public Task SourceOwnershipIsConsolidated()
        => global::Program.SsctlCommandHandlers_SourceOwnership_IsConsolidated();

    [Fact]
    public Task HelpUsesCatalogCliHelpForAutomationCommands()
        => global::Program.SsctlHelp_UsesCatalogCliHelpForAutomationCommands();
}

public sealed class ToolProbeContractsTests
{
    [Fact]
    public Task PresentMonParserSelectsDominantNonArtifactSwapChain()
        => global::Program.PresentMonParser_SelectsDominantNonArtifactSwapChain();

    [Fact]
    public Task PresentMonProbeSourceOwnershipIsUnified()
        => global::Program.PresentMonProbe_SourceOwnership_IsUnified();

    [Fact]
    public Task SsctlPipeTransportExposesAdvancedAutomationCommandIds()
        => global::Program.SsctlPipeTransport_ExposesAdvancedAutomationCommandIds();

    [Fact]
    public Task KsAudioNodeProbeSourceOwnershipIsConsolidated()
        => global::Program.KsAudioNodeProbe_SourceOwnership_IsConsolidated();

    [Fact]
    public Task EgavdsAudioProbeSourceOwnershipIsConsolidated()
        => global::Program.EgavdsAudioProbe_SourceOwnership_IsConsolidated();
}

public sealed class ToolModelContractsTests
{
    [Fact]
    public async Task NvmlSnapshotComputedPropertiesConvertUnits()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
        await global::Program.NvmlSnapshot_ComputedProperties_ConvertUnits();
    }

    [Fact]
    public async Task NvmlMonitorNativeInteropLivesWithMonitorOwner()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
        await global::Program.NvmlMonitor_NativeInteropLivesWithMonitorOwner();
    }

    [Fact]
    public async Task CaptureSessionSnapshotDefaultState()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
        await global::Program.CaptureSessionSnapshot_DefaultState();
    }
}

public sealed class NativeToolProbeContractsTests
{
    [Fact]
    public Task RtkI2cProbeGuardsUnsafeNativePaths()
        => global::Program.RtkI2cProbe_GuardsUnsafeNativePaths();
}

internal static class ToolFormatterTestAssembly
{
    private static readonly Dictionary<string, Assembly> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object CacheLock = new();

    public static Assembly Load(string relativeAssemblyPath)
    {
        var repoRoot = FindRepoRoot();
        var fullPath = Path.GetFullPath(Path.Combine(repoRoot, relativeAssemblyPath));
        lock (CacheLock)
        {
            if (Cache.TryGetValue(fullPath, out var cached))
            {
                return cached;
            }

            if (!File.Exists(fullPath))
            {
                throw new InvalidOperationException($"Required tool assembly was not found: {relativeAssemblyPath}.");
            }

            var loadContext = new ToolFormatterTestAssemblyLoadContext(fullPath);
            var assembly = loadContext.LoadFromAssemblyPath(fullPath);
            Cache[fullPath] = assembly;
            return assembly;
        }
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(Environment.CurrentDirectory);
        while (directory != null)
        {
            var gitPath = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Environment.CurrentDirectory;
    }

    private sealed class ToolFormatterTestAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public ToolFormatterTestAssemblyLoadContext(string mainAssemblyToLoadPath)
            : base(isCollectible: false)
        {
            _resolver = new AssemblyDependencyResolver(mainAssemblyToLoadPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
        }
    }
}

public sealed class McpToolSurfaceContractsTests
{
    [Fact]
    public Task RawAppStateKeepsCaptureOptionsSeparate()
        => global::Program.McpToolSurface_KeepsCaptureOptionsSeparateFromRawState();

    [Fact]
    public Task FixedAutomationRoutesUseAutomationCommandKinds()
        => global::Program.McpToolSurface_FixedAutomationRoutesUseAutomationCommandKinds();

    [Fact]
    public Task HostToolSchemaUsesPipeClientAsService()
        => global::Program.McpHostToolSchema_UsesPipeClientAsService();

    [Fact]
    public Task PipeClientHonorsSussudioAutomationPipeEnvironment()
        => global::Program.McpPipeClient_HonorsSussudioAutomationPipeEnvironment();

    [Fact]
    public Task HostToolInvocationReturnsPipeFailures()
        => global::Program.McpHostToolInvocation_ReturnsPipeFailureInsteadOfClosingTransport();

    [Fact]
    public Task CaptureSettingsToolsRouteProvidedSettings()
        => global::Program.McpCaptureSettingsTools_RouteProvidedSettings();

    [Fact]
    public Task RecordingToolsRouteRecordingToggle()
        => global::Program.McpRecordingTools_RouteRecordingToggle();

    [Fact]
    public Task FlashbackToolsRouteEnableToggle()
        => global::Program.McpFlashbackTools_RouteEnableToggle();

    [Fact]
    public Task ToolCommandFormatterBatchesPendingCommands()
        => global::Program.McpToolCommandFormatter_BatchesPendingCommands();

    [Fact]
    public Task DeviceToolsRouteRefreshSelectionsAndCustomAudio()
        => global::Program.McpDeviceTools_RouteRefreshSelectionsAndCustomAudio();

    [Fact]
    public Task PipelineSettingsToolsRoutePipelineAndAudioCommands()
        => global::Program.McpPipelineSettingsTools_RoutePipelineAndAudioCommands();

    [Fact]
    public Task UiSettingsToolsRouteUiCommands()
        => global::Program.McpUiSettingsTools_RouteUiCommands();

    [Fact]
    public Task VerificationToolsFormatVerificationResponses()
        => global::Program.McpVerificationTools_FormatVerificationResponses();

    [Fact]
    public Task DiagnosticSessionToolRecordsSnapshotArtifacts()
        => global::Program.McpDiagnosticSessionTool_RecordsSnapshotArtifacts();

    [Fact]
    public Task DiagnosticSessionToolSurfacesDiagnosticFailures()
        => global::Program.McpDiagnosticSessionTool_SurfacesDiagnosticFailureAsToolError();
}

public sealed class McpPerformanceToolContractsTests
{
    [Fact]
    public Task PresentMonToolsRouteSnapshotCorrelation()
        => global::Program.McpPresentMonTools_RouteSnapshotCorrelation();

    [Fact]
    public Task PerformanceTimelineToolExposesD3DP99StageTiming()
        => global::Program.McpPerformanceTimelineTool_ExposesD3DP99StageTiming();

    [Fact]
    public Task PerformanceTimelineToolRendersFlashbackCommandCounters()
        => global::Program.McpPerformanceTimelineTool_RendersFlashbackCommandCounters();

    [Fact]
    public Task FramePacingVerdictToolFlagsHalfRatePreviewAndPlayback()
        => global::Program.McpFramePacingVerdictTool_FlagsHalfRatePreviewAndPlayback();

    [Fact]
    public Task FramePacingVerdictToolFlagsInsufficientSampleDuration()
        => global::Program.McpFramePacingVerdictTool_FlagsInsufficientSampleDuration();

    [Fact]
    public Task FramePacingVerdictToolSourceOwnershipIsSplit()
        => global::Program.McpFramePacingVerdictTool_SourceOwnershipIsSplit();
}

public sealed class McpWindowPreviewToolContractsTests
{
    [Fact]
    public Task WaitToolsUseCatalogResponseTimeoutForConditionWaits()
        => global::Program.McpWaitTools_UsesCatalogResponseTimeoutForConditionWaits();

    [Fact]
    public Task WaitToolsRouteConditionWaits()
        => global::Program.McpWaitTools_RouteConditionWaits();

    [Fact]
    public Task WindowScreenshotToolFormatsScreenshotResponses()
        => global::Program.McpWindowScreenshotTool_FormatsScreenshotResponses();

    [Fact]
    public Task PreviewFrameCaptureToolFormatsFrameReports()
        => global::Program.McpPreviewFrameCaptureTool_FormatsCaptureResponses();

    [Fact]
    public Task WindowToolsRouteWindowActions()
        => global::Program.McpWindowTools_RouteWindowActions();

    [Fact]
    public Task PreviewToolsRoutePreviewToggle()
        => global::Program.McpPreviewTools_RoutePreviewToggle();

    [Fact]
    public Task PreviewColorProbeToolFormatsProbeResponses()
        => global::Program.McpPreviewColorProbeTool_FormatsProbeResponses();

    [Fact]
    public Task VideoSourceProbeToolFormatsProbeResponses()
        => global::Program.McpVideoSourceProbeTool_FormatsProbeResponses();
}

public sealed class McpDiagnosticSessionCommandRunContextContractsTests
{
    [Fact]
    public Task PipeRetryPolicyOwnsConnectRetryClassification()
        => global::Program.DiagnosticSessionPipeRetryPolicy_OwnsConnectRetryClassification();

    [Fact]
    public Task CommandChannelOwnsSerializedCommandSending()
        => global::Program.DiagnosticSessionCommandChannel_OwnsSerializedCommandSending();

    [Fact]
    public Task JsonArtifactsOwnJsonWritingAndResponseExtractionSplit()
        => global::Program.DiagnosticSessionJsonArtifacts_OwnsJsonWritingAndResponseExtractionSplit();

    [Fact]
    public Task RunStateOwnsTerminalState()
        => global::Program.DiagnosticSessionRunState_OwnsTerminalState();

    [Fact]
    public Task LiveStateWriterOwnsBreadcrumbFile()
        => global::Program.DiagnosticSessionLiveStateWriter_OwnsBreadcrumbFile();

    [Fact]
    public Task RunContextOwnsMutableRunInfrastructure()
        => global::Program.DiagnosticSessionRunContext_OwnsMutableRunInfrastructure();

    [Fact]
    public Task RunBootstrapOwnsNormalizedSessionIdentity()
        => global::Program.DiagnosticSessionRunBootstrap_OwnsNormalizedSessionIdentity();

    [Fact]
    public Task OutputLockOwnsExclusiveOutputDirectoryLock()
        => global::Program.DiagnosticSessionOutputLock_OwnsExclusiveOutputDirectoryLock();
}

public sealed class McpDiagnosticSessionCoreContractsTests
{
    [Fact]
    public Task SamplerOwnsSampleLoopOrdering()
        => global::Program.DiagnosticSessionSampler_OwnsSampleLoopOrdering();

    [Fact]
    public Task MetricsOwnSessionMetricProjection()
        => global::Program.DiagnosticSessionMetrics_OwnsSessionMetricProjection();

    [Fact]
    public Task HealthPolicyOwnsHealthTolerances()
        => global::Program.DiagnosticSessionHealthPolicy_OwnsHealthTolerances();
}

public sealed class McpDiagnosticSessionFlashbackContractsTests
{
    [Fact]
    public Task FlashbackCycleScenariosOwnCycleFlows()
        => global::Program.DiagnosticSessionFlashbackCycleScenarios_OwnCycleFlows();

    [Fact]
    public Task FlashbackMetricsOwnSessionMetricProjection()
        => global::Program.DiagnosticSessionFlashbackMetrics_OwnsFlashbackSessionMetricProjection();

    [Fact]
    public Task FlashbackMetricsExportForceRotateCountersIgnoreRelevanceGate()
        => global::Program.DiagnosticSessionFlashbackMetrics_ExportForceRotateCountersIgnoreRelevanceGate();

    [Fact]
    public Task FlashbackPreviewCycleScenariosOwnPreviewCycleFlows()
        => global::Program.DiagnosticSessionFlashbackPreviewCycleScenarios_OwnPreviewCycleFlows();

    [Fact]
    public Task FlashbackRejectedExportsOwnRejectionFlows()
        => global::Program.DiagnosticSessionFlashbackRejectedExports_OwnRejectionFlows();

    [Fact]
    public Task FlashbackRecordingSettingsScenariosOwnDeferredSettingsFlow()
        => global::Program.DiagnosticSessionFlashbackRecordingSettingsScenarios_OwnDeferredSettingsFlow();

    [Fact]
    public Task FlashbackLifecycleScenariosOwnLifecycleFlow()
        => global::Program.DiagnosticSessionFlashbackLifecycleScenarios_OwnLifecycleFlow();

    [Fact]
    public Task FlashbackSegmentPlaybackScenariosOwnSegmentPlaybackFlow()
        => global::Program.DiagnosticSessionFlashbackSegmentPlaybackScenarios_OwnSegmentPlaybackFlow();

    [Fact]
    public Task FlashbackExportScenariosOwnExportFlows()
        => global::Program.DiagnosticSessionFlashbackExportScenarios_OwnExportFlows();

    [Fact]
    public Task FlashbackExportsOwnExportHelpers()
        => global::Program.DiagnosticSessionFlashbackExports_OwnsExportHelpers();

    [Fact]
    public Task FlashbackSegmentsOwnSegmentWaitsAndParsing()
        => global::Program.DiagnosticSessionFlashbackSegments_OwnsSegmentWaitsAndParsing();

    [Fact]
    public Task FlashbackStressScenarioOwnsStressFlow()
        => global::Program.DiagnosticSessionFlashbackStressScenario_OwnsStressFlow();

    [Fact]
    public Task FlashbackWaitsOwnSnapshotPollingWaits()
        => global::Program.DiagnosticSessionFlashbackWaits_OwnsSnapshotPollingWaits();

    [Fact]
    public Task FlashbackValidationOwnWarningPolicy()
        => global::Program.DiagnosticSessionFlashbackValidation_OwnsFlashbackWarningPolicy();

    [Fact]
    public Task FlashbackStressScenarioClassifiesAudioMasterFallbacks()
        => global::Program.DiagnosticSessionFlashbackStressScenario_ClassifiesAudioMasterFallbacks();
}

public sealed class McpDiagnosticSessionInfrastructureContractsTests
{
    [Fact]
    public Task RunnerWritesTerminalArtifactsOnFinalSnapshotFailure()
        => global::Program.DiagnosticSessionRunner_FinalSnapshotFailureWritesTerminalArtifacts();

    [Fact]
    public Task ModelsAreSplitFromRunnerBehavior()
        => global::Program.DiagnosticSessionModels_AreSplitFromRunnerBehavior();

    [Fact]
    public Task InitialSnapshotOwnsBaselineCapture()
        => global::Program.DiagnosticSessionInitialSnapshot_OwnsBaselineCapture();

    [Fact]
    public Task RunnerOwnsCompatibilitySurface()
        => global::Program.DiagnosticSessionRunner_OwnsCompatibilitySurface();
}

public sealed class McpDiagnosticSessionResultSurfaceContractsTests
{
    [Fact]
    public Task ResultFormatterOwnsFormattedSummaryText()
        => global::Program.DiagnosticSessionResultFormatter_OwnsFormattedSummaryText();

    [Fact]
    public Task ResultBuilderOwnsSummaryConstruction()
        => global::Program.DiagnosticSessionResultBuilder_OwnsSummaryConstruction();

    [Fact]
    public Task ResultBuilderDiagnosticHealthVerdictLivesWithAnalysis()
        => global::Program.DiagnosticSessionResultBuilder_DiagnosticHealthVerdictLivesWithAnalysis();

    [Fact]
    public Task ResultBuilderOwnsSummaryWriteFailures()
        => global::Program.DiagnosticSessionResultBuilder_OwnsSummaryWriteFailures();

    [Fact]
    public Task ResultArtifactsOwnPreSummaryWrites()
        => global::Program.DiagnosticSessionResultArtifacts_OwnPreSummaryWrites();

    [Fact]
    public Task OptionalTextFormatterOwnsSharedFormattingHelpers()
        => global::Program.DiagnosticSessionOptionalTextFormatter_OwnsSharedFormattingHelpers();
}

public sealed class McpDiagnosticSessionRunnerBehaviorContractsTests
{
    [Fact]
    public Task VerifiesFlashbackExportPlaybackCommandFlow()
        => global::Program.DiagnosticSessionRunner_VerifiesFlashbackExportPlaybackCommandFlow();

    [Fact]
    public Task IgnoresTransientFlashbackWarmupWarnings()
        => global::Program.DiagnosticSessionRunner_IgnoresTransientFlashbackWarmupWarnings();

    [Fact]
    public Task ToleratesSparseSourceCadenceWarningsOnlyWithoutSourceDrops()
        => global::Program.DiagnosticSessionRunner_ToleratesSparseSourceCadenceWarningsOnlyWithoutSourceDrops();

    [Fact]
    public Task UnknownInitialSnapshotFailsWithoutMutatingState()
        => global::Program.DiagnosticSessionRunner_UnknownInitialSnapshotFailsWithoutMutatingState();

    [Fact]
    public Task RetriesSyntheticPipeConnectFailures()
        => global::Program.DiagnosticSessionRunner_RetriesSyntheticPipeConnectFailures();

    [Fact]
    public Task RejectsConcurrentInvocationOnSameOutputDirectory()
        => global::Program.DiagnosticSessionRunner_RejectsConcurrentInvocationOnSameOutputDirectory();
}

public sealed class McpDiagnosticSessionScenarioExecutionContractsTests
{
    [Fact]
    public Task RunExecutionScenarioOwnsScenarioPhase()
        => global::Program.DiagnosticSessionRunExecutionScenario_OwnsScenarioPhase();

    [Fact]
    public Task RunExecutionCompletionOwnsPostCleanupEvidenceAndResult()
        => global::Program.DiagnosticSessionRunExecutionCompletion_OwnsPostCleanupEvidenceAndResult();

    [Fact]
    public Task ScenarioPlanOwnsScenarioFlags()
        => global::Program.DiagnosticSessionScenarioPlan_OwnsScenarioFlags();

    [Fact]
    public Task ScenarioSetupOwnsInitialMutations()
        => global::Program.DiagnosticSessionScenarioSetup_OwnsInitialMutations();

    [Fact]
    public Task BackgroundTasksOwnTaskDraining()
        => global::Program.DiagnosticSessionBackgroundTasks_OwnTaskDraining();

    [Fact]
    public Task PresentMonStartupOwnsPresentMonLaunch()
        => global::Program.DiagnosticSessionPresentMonStartup_OwnsPresentMonLaunch();

    [Fact]
    public Task CleanupPolicyOwnsRestoreWarnings()
        => global::Program.DiagnosticSessionAnalysisValidation_OwnsCleanupRestoreWarnings();

    [Fact]
    public Task RecordingChecksOwnPostRunRecordingVerification()
        => global::Program.DiagnosticSessionRecordingChecks_OwnPostRunRecordingVerification();

    [Fact]
    public Task PostRunSnapshotsOwnTimelineAndFinalSnapshot()
        => global::Program.DiagnosticSessionPostRunSnapshots_OwnTimelineAndFinalSnapshot();
}

}

// Diagnostic-session result surface contracts live with the tool xUnit wrappers.
static partial class Program
{
    private static string ReadDiagnosticSessionResultBuilderAnalysisSource()
    {
        var builderText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        return ExtractTextBetween(
            builderText,
            "private sealed record DiagnosticSessionResultAnalysis(",
            "private readonly record struct DiagnosticSessionOverviewResultProjection(");
    }

    internal static Task DiagnosticSessionModels_AreSplitFromRunnerBehavior()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var modelText = ReadDiagnosticSessionModelsSource();
        var resultText = ReadRepoFile("tools/Common/DiagnosticSessionResult.cs");

        AssertContains(modelText, "public sealed class DiagnosticSessionOptions");
        AssertContains(modelText, "public sealed class DiagnosticSessionResult");
        AssertDoesNotContain(modelText, "public sealed partial class DiagnosticSessionResult");
        AssertContains(resultText, "public string SessionId { get; init; } = string.Empty;");
        AssertContains(resultText, "public string[] Warnings { get; set; } = Array.Empty<string>();");
        AssertContains(resultText, "// End-of-run overview.");
        AssertContains(resultText, "public double ProcessCpuPercentAtEnd { get; init; }");
        AssertContains(resultText, "public PresentMonProbeResult? PresentMon { get; init; }");
        AssertContains(resultText, "// Capture/source summary.");
        AssertContains(resultText, "public string SelectedResolutionAtEnd { get; init; } = string.Empty;");
        AssertContains(resultText, "public string SourceTelemetrySummaryAtEnd { get; init; } = string.Empty;");
        AssertContains(resultText, "// Flashback playback command queue summary.");
        AssertContains(resultText, "public int FlashbackPlaybackPendingCommandsAtEnd { get; init; }");
        AssertContains(resultText, "// Flashback playback cadence and frame-delivery summary.");
        AssertContains(resultText, "public double FlashbackPlaybackObservedFpsAtEnd { get; init; }");
        AssertContains(resultText, "// Flashback playback 1% low sample-window summary.");
        AssertContains(resultText, "public double FlashbackPlaybackOnePercentLowFpsAtEnd { get; init; }");
        AssertContains(resultText, "// Flashback playback decode timing summary.");
        AssertContains(resultText, "public double FlashbackPlaybackDecodeP99MsAtEnd { get; init; }");
        AssertContains(resultText, "// Flashback playback audio-master summary.");
        AssertContains(resultText, "public long FlashbackPlaybackAudioMasterFallbacksAtEnd { get; init; }");
        AssertContains(resultText, "// Flashback playback stage and seek summary.");
        AssertContains(resultText, "public long FlashbackPlaybackSubmitFailuresAtEnd { get; init; }");
        AssertContains(resultText, "public bool FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd { get; init; }");
        AssertContains(resultText, "// Flashback recording summary.");
        AssertContains(resultText, "public bool FlashbackRecordingBackendObserved { get; init; }");
        AssertContains(resultText, "// Flashback export summary.");
        AssertContains(resultText, "public string FlashbackExportStatusAtEnd { get; init; } = string.Empty;");
        AssertContains(resultText, "// Preview cadence summary.");
        AssertContains(resultText, "public double PreviewCadenceOnePercentLowFpsAtEnd { get; init; }");
        AssertContains(resultText, "// Preview visual-cadence summary.");
        AssertContains(resultText, "public double VisualCadenceOutputFpsAtEnd { get; init; }");
        AssertContains(resultText, "// Preview scheduler and jitter-buffer summary.");
        AssertContains(resultText, "public long PreviewSchedulerDroppedAtEnd { get; init; }");
        AssertContains(resultText, "// Preview D3D frame-stat and CPU timing summary.");
        AssertContains(resultText, "public double PreviewD3DInputUploadCpuP99MsAtEnd { get; init; }");
        AssertContains(modelText, "public sealed class DiagnosticSessionSample");
        AssertContains(modelText, "public string TerminalState { get; set; }");
        AssertContains(modelText, "public JsonElement Snapshot { get; init; }");
        AssertContains(runnerText, "public static class DiagnosticSessionRunner");
        AssertContains(runnerText, "public static async Task<DiagnosticSessionResult> RunAsync(");
        AssertContains(runnerText, "private static async Task<DiagnosticSessionResult> RunCompletionPhaseAsync(");
        AssertDoesNotContain(runnerText, "public sealed class DiagnosticSessionResult");
        AssertDoesNotContain(runnerText, "public sealed class DiagnosticSessionOptions");
        AssertDoesNotContain(runnerText, "public sealed class DiagnosticSessionSample");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionOptionalTextFormatter_OwnsSharedFormattingHelpers()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var formatterText = ReadDiagnosticSessionResultFormatterSource();
        var formatterRootText = ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.cs")
            .Replace("\r\n", "\n");
        var validationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSupport.cs")
            .Replace("\r\n", "\n");

        AssertContains(formatterRootText, "internal static class DiagnosticSessionOptionalTextFormatter");
        AssertContains(formatterRootText, "internal static string FormatOptional(string value)");
        AssertContains(formatterRootText, "string.IsNullOrWhiteSpace(value) ? \"none\" : value");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionOptionalTextFormatter.cs")), "Optional diagnostic text formatting stays folded into DiagnosticSessionResultFormatter.cs");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;");
        AssertContains(formatterText, "using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;");
        AssertContains(validationText, "using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;");
        AssertDoesNotContain(runnerText, "private static string FormatOptional(");
        AssertDoesNotContain(validationText, "private static string FormatOptional(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionJsonArtifacts_OwnsJsonWritingAndResponseExtractionSplit()
    {
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var initialSnapshotText = ReadDiagnosticSessionRunContextSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var responseJsonText = initialSnapshotText;

        AssertContains(builderText, "internal static class DiagnosticSessionJsonArtifacts");
        AssertContains(builderText, "internal static JsonElement CreateEmptyJsonObject()");
        AssertContains(builderText, "internal static async Task WriteJsonAsync<T>(");
        AssertContains(responseJsonText, "internal static class DiagnosticSessionAutomationResponseJson");
        AssertContains(responseJsonText, "internal static bool TryGetSnapshot(");
        AssertContains(responseJsonText, "internal static bool TryGetVerification(");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionResultArtifacts.cs")), "Result artifact helpers stay folded into DiagnosticSessionResultBuilder.cs");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionAutomationResponseJson.cs")), "Automation response JSON helpers stay folded into DiagnosticSessionRunContext.cs");
        AssertContains(initialSnapshotText, "using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;");
        AssertContains(initialSnapshotText, "using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;");
        AssertDoesNotContain(builderText, "TryGetSnapshot(");
        AssertDoesNotContain(builderText, "TryGetVerification(");
        AssertDoesNotContain(executionText, "using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;");
        AssertContains(executionText, "using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;");
        AssertDoesNotContain(executionText, "private static async Task WriteJsonAsync<T>(");
        AssertDoesNotContain(executionText, "private static bool TryGetSnapshot(");
        AssertDoesNotContain(executionText, "private static bool TryGetVerification(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionResultBuilder_OwnsSummaryWriteFailures()
    {
        var builderText = ReadDiagnosticSessionResultBuilderSource();

        AssertContains(builderText, "private static async Task<DiagnosticSessionResult> WriteSummaryAsync(");
        AssertContains(builderText, "await WriteJsonAsync(result.SummaryPath, result, CancellationToken.None)");
        AssertContains(builderText, "runState.RecordTerminalException(ex, \"summary-write\")");
        AssertContains(builderText, "result.Success = false;");
        AssertContains(builderText, "result.CompletedUtc = DateTimeOffset.UtcNow;");
        AssertContains(builderText, "result.TerminalState = runState.GetTerminalState();");
        AssertContains(builderText, "result.LastStage = runState.GetResultLastStage();");
        AssertContains(builderText, "result.Warnings = warnings.ToArray();");
        AssertContains(builderText, "runState.SetStage(\"summary-written\")");
        AssertContains(builderText, "WriteSummaryAsync(result, runState, warnings)");
        AssertDoesNotContain(builderText, "using static Sussudio.Tools.DiagnosticSessionSummaryWriter;");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionResultArtifacts_OwnPreSummaryWrites()
    {
        var builderText = ReadDiagnosticSessionResultBuilderSource();

        AssertContains(builderText, "internal static class DiagnosticSessionResultArtifacts");
        AssertContains(builderText, "internal static async Task<DiagnosticSessionResultArtifactPaths> WritePreSummaryAsync(");
        AssertContains(builderText, "internal readonly record struct DiagnosticSessionResultArtifactPaths(");
        AssertContains(builderText, "SummaryPath: Path.Combine(outputDirectory, \"summary.json\")");
        AssertContains(builderText, "SamplesPath: Path.Combine(outputDirectory, \"samples.json\")");
        AssertContains(builderText, "FrameLedgerPath: Path.Combine(outputDirectory, \"frame-ledger.json\")");
        AssertContains(builderText, "TimelinePath: Path.Combine(outputDirectory, \"timeline.json\")");
        AssertContains(builderText, "private static object BuildFrameLedgerTrace(");
        AssertContains(builderText, "using static Sussudio.Tools.AutomationSnapshotFormatter;");
        AssertContains(builderText, "runState.WriteArtifactBestEffortAsync(\"write-samples\", paths.SamplesPath, samples)");
        AssertContains(builderText, "runState.WriteArtifactBestEffortAsync(\"write-frame-ledger\", paths.FrameLedgerPath, BuildFrameLedgerTrace(sessionId, samples))");
        AssertContains(builderText, "runState.WriteArtifactBestEffortAsync(\"write-timeline\", paths.TimelinePath, timeline)");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionResultArtifacts;");
        AssertContains(builderText, "WritePreSummaryAsync(");
        AssertDoesNotContain(builderText, "Path.Combine(request.OutputDirectory, \"samples.json\")");
        AssertDoesNotContain(builderText, "BuildFrameLedgerTrace(request.SessionId, samples)");

        return Task.CompletedTask;
    }


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

internal static Task DiagnosticSessionResultBuilder_OwnsSummaryConstruction()
    {
        AssertDiagnosticSessionResultBuilderCoreOwnership();
        AssertDiagnosticSessionResultBuilderPreviewSchedulerOwnership();
        AssertDiagnosticSessionResultBuilderOverviewAndCaptureProjectionOwnership();
        AssertDiagnosticSessionResultBuilderFlashbackProjectionOwnership();
        AssertDiagnosticSessionResultBuilderPreviewProjectionOwnership();
        AssertDiagnosticSessionResultBuilderAnalysisWarningsOwnership();
        AssertDiagnosticSessionResultBuilderSummaryArtifactHandoffOwnership();

        return Task.CompletedTask;
    }

    private static void AssertDiagnosticSessionResultBuilderPreviewProjectionOwnership()
    {
        var builderText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var flatteningText = ExtractMemberCode(builderText, "FlattenResultProjectionSet");
        var projectionSetText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var previewResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var previewD3DResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var analysisText = ReadDiagnosticSessionResultBuilderAnalysisSource();

        AssertContains(builderText, "return FlattenResultProjectionSet(");
        AssertContains(flatteningText, "private static DiagnosticSessionResult FlattenResultProjectionSet(");
        AssertContains(projectionSetText, "Preview: BuildPreviewResultProjection(analysis)");
        AssertContains(projectionSetText, "PreviewScheduler: BuildPreviewSchedulerResultProjection(analysis)");
        AssertContains(projectionSetText, "PreviewD3D: BuildPreviewD3DResultProjection(analysis)");
        AssertContains(projectionSetText, "PreviewVisualCadence: BuildPreviewVisualCadenceResultProjection(analysis)");
        AssertContains(flatteningText, "var previewResult = resultProjections.Preview;");
        AssertContains(flatteningText, "var previewSchedulerResult = resultProjections.PreviewScheduler;");
        AssertContains(flatteningText, "var previewD3DResult = resultProjections.PreviewD3D;");
        AssertContains(flatteningText, "var previewVisualCadenceResult = resultProjections.PreviewVisualCadence;");
        AssertContains(previewResultText, "private readonly record struct DiagnosticSessionPreviewResultProjection(");
        AssertContains(previewResultText, "private static DiagnosticSessionPreviewResultProjection BuildPreviewResultProjection(");
        AssertContains(previewResultText, "private readonly record struct DiagnosticSessionPreviewSchedulerResultProjection(");
        AssertContains(previewResultText, "private static DiagnosticSessionPreviewSchedulerResultProjection BuildPreviewSchedulerResultProjection(");
        AssertContains(previewD3DResultText, "private readonly record struct DiagnosticSessionPreviewD3DResultProjection(");
        AssertContains(previewD3DResultText, "private static DiagnosticSessionPreviewD3DResultProjection BuildPreviewD3DResultProjection(");
        AssertContains(previewD3DResultText, "var previewD3DMetrics = analysis.PreviewD3DMetrics;");
        AssertContains(previewResultText, "private readonly record struct DiagnosticSessionPreviewVisualCadenceResultProjection(");
        AssertContains(previewResultText, "private static DiagnosticSessionPreviewVisualCadenceResultProjection BuildPreviewVisualCadenceResultProjection(");
        AssertContains(previewResultText, "var visualCadenceMetrics = analysis.VisualCadenceMetrics;");
        AssertContains(previewResultText, "PreviewSchedulerLastDropReasonAtEnd: previewScheduler.LastDropReasonAtEnd");
        AssertContains(previewD3DResultText, "PreviewD3DInputUploadCpuP99MsAtEnd: previewD3DMetrics.InputUploadCpuP99MsAtEnd");
        AssertContains(previewD3DResultText, "PreviewD3DTotalFrameCpuMaxMsObserved: previewD3DMetrics.TotalFrameCpuMaxMsObserved");
        AssertContains(previewResultText, "VisualCadenceOutputFpsAtEnd: visualCadenceMetrics.OutputFpsAtEnd");
        AssertContains(previewResultText, "VisualCadenceLongestRepeatRunAtEnd: visualCadenceMetrics.LongestRepeatRunAtEnd");
        AssertContains(flatteningText, "PreviewD3DInputUploadCpuP99MsAtEnd = previewD3DResult.PreviewD3DInputUploadCpuP99MsAtEnd,");
        AssertContains(flatteningText, "PreviewSchedulerDroppedAtEnd = previewSchedulerResult.PreviewSchedulerDroppedAtEnd,");
        AssertContains(flatteningText, "VisualCadenceOutputFpsAtEnd = previewVisualCadenceResult.VisualCadenceOutputFpsAtEnd,");
        AssertDoesNotContain(flatteningText, "GetString(lastSnapshot, \"MjpegPreviewJitterLastDropReason\")");
        AssertDoesNotContain(analysisText, "private static DiagnosticSessionPreviewSchedulerResultProjection BuildPreviewSchedulerResultProjection(");
        AssertDoesNotContain(analysisText, "PreviewD3DInputUploadCpuP99MsAtEnd");
        AssertDoesNotContain(flatteningText, "PreviewD3DInputUploadCpuP99MsAtEnd = previewResult");
        AssertDoesNotContain(flatteningText, "PreviewD3DInputUploadCpuP99MsAtEnd = previewD3DMetrics");
        AssertDoesNotContain(flatteningText, "VisualCadenceOutputFpsAtEnd = previewResult");
        AssertDoesNotContain(flatteningText, "VisualCadenceOutputFpsAtEnd = visualCadenceMetrics");
    }

    private static void AssertDiagnosticSessionResultBuilderAnalysisWarningsOwnership()
    {
        var analysisText = ReadDiagnosticSessionResultBuilderAnalysisSource();

        AssertContains(analysisText, "AddFlashbackPlaybackAnalysisWarnings(playbackResultMetrics, warnings);");
        AssertContains(analysisText, "AddFlashbackExportAnalysisWarnings(");
        AssertContains(analysisText, "ValidateFlashbackPreviewSchedulerAnalysis(");
        AssertContains(analysisText, "exportMetrics.ForceRotateFallbacksAtEnd,");
        AssertContains(analysisText, "exportMetrics.ForceRotateFallbacksDelta,");
        AssertContains(analysisText, "exportMetrics.LastForceRotateFallbackSegmentsAtEnd,");
        AssertContains(analysisText, "var toleratesPreviewCycleSchedulerSettling =");
        AssertContains(analysisText, "var toleratesSparsePreviewSchedulerDeadlineDrops =");
        AssertContains(analysisText, "var toleratesSparseScrubSchedulerTransitions =");
        AssertDoesNotContain(analysisText, "var flashbackExportForceRotateFallbacksAtEnd =");
        AssertDoesNotContain(analysisText, "FlashbackExportForceRotateFallbacksAtEnd =");
        AssertContains(analysisText, "private static void AddFlashbackPlaybackAnalysisWarnings(");
        AssertContains(analysisText, "private static void AddFlashbackExportAnalysisWarnings(");
        AssertContains(analysisText, "EvaluateFlashbackWarningsSucceeded(request.ScenarioPlan, warnings)");
        AssertContains(analysisText, "private static bool EvaluateFlashbackWarningsSucceeded(");
        AssertContains(analysisText, "scenarioPlan.UsesFlashbackScenarioWarningPolicy");
        AssertContains(analysisText, "IsToleratedFlashbackScenarioWarning(");
        AssertContains(analysisText, "scenarioPlan.ToleratesSourceSignalHealthWarning");
        AssertContains(analysisText, "scenarioPlan.ToleratesFlashbackForceRotateDrainWarning");
        AssertContains(analysisText, "flashback playback seek forward-decode cap hit during session");
        AssertContains(analysisText, "flashback export used force-rotate partial fallback");
        AssertContains(analysisText, "playbackResultMetrics.SeekForwardDecodeCapHitsDelta <= 0");
        AssertContains(analysisText, "flashbackExportForceRotateFallbacksDelta <= 0");
    }

    internal static Task DiagnosticSessionResultBuilder_DiagnosticHealthVerdictLivesWithAnalysis()
    {
        var analysisText = ReadDiagnosticSessionResultBuilderAnalysisSource();
        var healthText = analysisText;

        AssertContains(analysisText, "var validationOutcome = ValidateAnalysis(");
        AssertContains(analysisText, "var diagnosticHealthSucceeded = AnalyzeDiagnosticHealth(");
        AssertContains(healthText, "private readonly record struct DiagnosticSessionHealthSummary(");
        AssertContains(healthText, "private static DiagnosticSessionHealthSummary BuildDiagnosticHealthSummary(");
        AssertContains(healthText, "request.StoppedRecordingForVerification");
        AssertContains(healthText, "GetString(diagnosticHealthSnapshot, \"DiagnosticHealthStatus\") ?? \"Unknown\"");
        AssertContains(healthText, "private static bool AnalyzeDiagnosticHealth(");
        AssertContains(healthText, "private readonly record struct DiagnosticHealthSourceWarningCounters(");
        AssertContains(healthText, "private static DiagnosticHealthSourceWarningCounters BuildDiagnosticHealthSourceWarningCounters(");
        AssertContains(healthText, "SourceReaderFramesDroppedDelta: GetCounterDelta(lastSnapshot, initialSnapshot, \"MfSourceReaderFramesDropped\")");
        AssertContains(healthText, "VideoIngestErrorsDelta: GetCounterDelta(lastSnapshot, initialSnapshot, \"VideoIngestErrorCount\")");
        AssertContains(healthText, "BuildDiagnosticHealthToleranceVerdict(");
        AssertContains(healthText, "private readonly record struct DiagnosticSessionHealthToleranceVerdict(");
        AssertContains(healthText, "private static DiagnosticSessionHealthToleranceVerdict BuildDiagnosticHealthToleranceVerdict(");
        AssertContains(healthText, "sourceWarningCounters.SourceReaderFramesDroppedDelta");
        AssertContains(healthText, "sourceWarningCounters.VideoIngestErrorsDelta");
        AssertContains(healthText, "BuildSessionDiagnosticHealthObservation(");
        AssertContains(healthText, "IsSparseSourceCaptureCadenceWarningRun(");
        AssertContains(healthText, "IsSparsePreviewSchedulerDeadlineDropRun(");
        AssertContains(healthText, "IsPreviewSchedulerDiagnosticHealthObservation(diagnosticHealthObservation)");
        AssertContains(healthText, "diagnostic health degraded during session");
        AssertContains(healthText, "diagnostic health {tolerance.WarningReason}:");
        AssertContains(healthText, "flashback force-rotate drain warning tolerated for flashback scenario");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionResultBuilder.DiagnosticHealth.cs")),
            "diagnostic health verdict helpers folded into analysis owner");
        AssertDoesNotContain(analysisText, "diagnostic health {toleratedReason}:");

        return Task.CompletedTask;
    }

    private static void AssertDiagnosticSessionResultBuilderSummaryArtifactHandoffOwnership()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var runExecutionText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var completionText = runExecutionText;
        var builderText = ReadDiagnosticSessionResultBuilderSource();

        AssertContains(builderText, "var artifactPaths = await WritePreSummaryAsync(");
        AssertContains(builderText, "SummaryPath = artifactPaths.SummaryPath");
        AssertContains(builderText, "SamplesPath = artifactPaths.SamplesPath");
        AssertContains(builderText, "FrameLedgerPath = artifactPaths.FrameLedgerPath");
        AssertContains(builderText, "TimelinePath = artifactPaths.TimelinePath");
        AssertContains(builderText, "runState.SetStage(\"summary\")");
        AssertContains(builderText, "return await WriteSummaryAsync(result, runState, warnings).ConfigureAwait(false);");
        AssertContains(completionText, "DiagnosticSessionResultBuilder.BuildAndWriteAsync(");
        AssertContains(completionText, "CreateResultBuildRequest(");
        AssertContains(runExecutionText, "RunCompletionPhaseAsync(");
        AssertContains(runExecutionText, "DiagnosticSessionResultBuilder.BuildAndWriteAsync(");
        AssertContains(runExecutionText, "new DiagnosticSessionResultBuildRequest(");
        AssertContains(completionText, "private static DiagnosticSessionResultBuildRequest CreateResultBuildRequest(");
        AssertContains(completionText, "return new DiagnosticSessionResultBuildRequest(");
        AssertContains(completionText, "runBootstrap.ScenarioPlan");
        AssertContains(completionText, "postRunSnapshots.HealthSnapshot");
        AssertDoesNotContain(runnerText, "SetStage(\"result-analysis\")");
        AssertDoesNotContain(runnerText, "var result = new DiagnosticSessionResult");
        AssertDoesNotContain(runnerText, "WriteArtifactBestEffortAsync(\"write-samples\"");
        AssertDoesNotContain(runnerText, "RecordTerminalException(ex, \"summary-write\")");
    }

    private static void AssertDiagnosticSessionResultBuilderCoreOwnership()
    {
        var builderText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var flatteningText = ExtractMemberCode(builderText, "FlattenResultProjectionSet");
        var projectionSetText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var resultBuildRequestText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var analysisText = ReadDiagnosticSessionResultBuilderAnalysisSource();
        var diagnosticHealthText = analysisText;

        AssertContains(builderText, "internal static class DiagnosticSessionResultBuilder");
        AssertContains(builderText, "internal static async Task<DiagnosticSessionResult> BuildAndWriteAsync(");
        AssertContains(builderText, "private static DiagnosticSessionResult CreateResult(");
        AssertContains(flatteningText, "private static DiagnosticSessionResult FlattenResultProjectionSet(");
        AssertContains(projectionSetText, "private static DiagnosticSessionResultProjectionSet BuildResultProjectionSet(");
        AssertContains(projectionSetText, "private readonly record struct DiagnosticSessionResultProjectionSet(");
        AssertContains(analysisText, "private static DiagnosticSessionPreviewSchedulerAnalysis BuildPreviewSchedulerAnalysis(");
        AssertContains(analysisText, "private readonly record struct DiagnosticSessionPreviewSchedulerAnalysis(");
        AssertContains(resultBuildRequestText, "internal sealed record DiagnosticSessionResultBuildRequest(");
        AssertContains(analysisText, "private sealed record DiagnosticSessionResultAnalysis(");
        AssertContains(projectionSetText, "DiagnosticSessionOverviewResultProjection Overview,");
        AssertContains(projectionSetText, "DiagnosticSessionPreviewVisualCadenceResultProjection PreviewVisualCadence");
        AssertContains(builderText, "runState.SetStage(\"result-analysis\")");
        AssertContains(builderText, "return FlattenResultProjectionSet(");
        AssertContains(flatteningText, "return new DiagnosticSessionResult\n        {");
        AssertContains(builderText, "var resultProjections = BuildResultProjectionSet(request, runState, analysis);");
        AssertContains(analysisText, "var healthSummary = BuildDiagnosticHealthSummary(request, lastSnapshot);");
        AssertContains(analysisText, "healthSummary.Snapshot,");
        AssertContains(analysisText, "healthSummary,");
        AssertContains(analysisText, "var previewScheduler = BuildPreviewSchedulerAnalysis(initialSnapshot, lastSnapshot, samples);");
        AssertContains(analysisText, "var validationOutcome = ValidateAnalysis(");
        AssertContains(diagnosticHealthText, "private readonly record struct DiagnosticSessionHealthSummary(");
        AssertContains(diagnosticHealthText, "private static DiagnosticSessionHealthSummary BuildDiagnosticHealthSummary(");
        AssertContains(diagnosticHealthText, "private readonly record struct DiagnosticHealthSourceWarningCounters(");
        AssertContains(diagnosticHealthText, "private static DiagnosticHealthSourceWarningCounters BuildDiagnosticHealthSourceWarningCounters(");
        AssertContains(diagnosticHealthText, "var tolerance = BuildDiagnosticHealthToleranceVerdict(");
        AssertContains(diagnosticHealthText, "tolerance.IsTolerated");
        AssertContains(diagnosticHealthText, "tolerance.WarningReason");
        AssertContains(diagnosticHealthText, "private readonly record struct DiagnosticSessionHealthToleranceVerdict(");
        AssertContains(diagnosticHealthText, "private static DiagnosticSessionHealthToleranceVerdict BuildDiagnosticHealthToleranceVerdict(");
        AssertContains(diagnosticHealthText, "var sourceWarningCounters = BuildDiagnosticHealthSourceWarningCounters(initialSnapshot, lastSnapshot);");
        AssertContains(diagnosticHealthText, "IsSparseSourceCaptureCadenceWarningRun(");
        AssertContains(diagnosticHealthText, "IsSparsePreviewSchedulerDeadlineDropRun(");
        AssertContains(analysisText, "private readonly record struct DiagnosticSessionAnalysisValidationOutcome(");
        AssertContains(analysisText, "private static DiagnosticSessionAnalysisValidationOutcome ValidateAnalysis(");
        AssertContains(analysisText, "ValidateFlashbackPlaybackSession(");
        AssertContains(analysisText, "ValidateCleanupLifecycleRestored(");
        AssertContains(analysisText, "ValidateFlashbackPreviewSchedulerAnalysis(");
        AssertContains(analysisText, "AnalyzeDiagnosticHealth(");
        AssertContains(analysisText, "EvaluateFlashbackWarningsSucceeded(request.ScenarioPlan, warnings)");
        AssertContains(analysisText, "private static bool EvaluateFlashbackWarningsSucceeded(");
        AssertContains(analysisText, "IsToleratedFlashbackScenarioWarning(");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionResultBuilder.Analysis.cs")),
            "diagnostic-session analysis folded into DiagnosticSessionResultBuilder.cs");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionResultBuilder.DiagnosticHealth.cs")),
            "diagnostic health verdict helpers folded into DiagnosticSessionResultBuilder.cs");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionResultBuilder.FlashbackPlaybackResult.cs")),
            "Flashback playback result projection folded into DiagnosticSessionResultBuilder.cs");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionResultBuilder.Flattening.cs")),
            "final result DTO flattening folded into DiagnosticSessionResultBuilder.cs");
        AssertDoesNotContain(flatteningText, "private static DiagnosticSessionResultProjectionSet BuildResultProjectionSet(");
        AssertContains(builderText, "private static DiagnosticSessionResultProjectionSet BuildResultProjectionSet(");
        AssertContains(builderText, "private readonly record struct DiagnosticSessionResultProjectionSet(");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionResultBuilder.Projections.cs")),
            "result projection set folded into DiagnosticSessionResultBuilder.cs");
        AssertContains(builderText, "return new DiagnosticSessionResult\n        {");
        AssertContains(analysisText, "IsToleratedFlashbackScenarioWarning(");
    }

    private static void AssertDiagnosticSessionResultBuilderPreviewSchedulerOwnership()
    {
        var analysisText = ReadDiagnosticSessionResultBuilderAnalysisSource();
        var previewResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");

        AssertContains(analysisText, "previewScheduler,");
        AssertContains(analysisText, "private readonly record struct DiagnosticSessionPreviewSchedulerAnalysis(");
        AssertContains(analysisText, "string LastDropReasonAtEnd,");
        AssertContains(analysisText, "string LastUnderflowReasonAtEnd,");
        AssertContains(analysisText, "double LastUnderflowInputAgeMsAtEnd,");
        AssertContains(analysisText, "double LastUnderflowOutputAgeMsAtEnd");
        AssertContains(analysisText, "LastDropReasonAtEnd: GetString(lastSnapshot, \"MjpegPreviewJitterLastDropReason\") ?? string.Empty");
        AssertContains(analysisText, "LastUnderflowReasonAtEnd: GetString(lastSnapshot, \"MjpegPreviewJitterLastUnderflowReason\") ?? string.Empty");
        AssertContains(analysisText, "LastUnderflowInputAgeMsAtEnd: GetDouble(lastSnapshot, \"MjpegPreviewJitterLastUnderflowInputAgeMs\")");
        AssertContains(analysisText, "LastUnderflowOutputAgeMsAtEnd: GetDouble(lastSnapshot, \"MjpegPreviewJitterLastUnderflowOutputAgeMs\")");
        AssertContains(analysisText, "private static void ValidateFlashbackPreviewSchedulerAnalysis(");
        AssertContains(analysisText, "var previewTargetFps = GetDouble(lastSnapshot, \"ExpectedCaptureFrameRate\");");
        AssertContains(analysisText, "previewTargetFps = GetDouble(lastSnapshot, \"SelectedExactFrameRate\");");
        AssertContains(analysisText, "var toleratesPreviewCycleSchedulerSettling =");
        AssertContains(analysisText, "scenarioPlan.IsPreviewCycleScenario && visualCadenceHealthy");
        AssertContains(analysisText, "var toleratesSparsePreviewSchedulerDeadlineDrops =");
        AssertContains(analysisText, "IsSparsePreviewSchedulerDeadlineDropRun(");
        AssertContains(analysisText, "var toleratesSparseScrubSchedulerTransitions =");
        AssertContains(analysisText, "scenarioPlan.ToleratesSparsePreviewSchedulerStressTransitions &&");
        AssertContains(analysisText, "IsSparsePreviewSchedulerStressRun(");
        AssertContains(analysisText, "ValidateFlashbackPreviewScheduler(");
        AssertContains(analysisText, "DiagnosticSessionPreviewSchedulerAnalysis PreviewScheduler,");
        AssertContains(previewResultText, "private readonly record struct DiagnosticSessionPreviewSchedulerResultProjection(");
        AssertContains(previewResultText, "private static DiagnosticSessionPreviewSchedulerResultProjection BuildPreviewSchedulerResultProjection(");
        AssertContains(previewResultText, "var previewScheduler = analysis.PreviewScheduler;");
        AssertContains(previewResultText, "PreviewSchedulerDroppedAtEnd: previewScheduler.DroppedAtEnd");
        AssertContains(previewResultText, "PreviewSchedulerScheduleLateDelta: previewScheduler.ScheduleLateDelta");
        AssertContains(previewResultText, "PreviewSchedulerLastDropReasonAtEnd: previewScheduler.LastDropReasonAtEnd");
        AssertContains(previewResultText, "PreviewSchedulerLastUnderflowReasonAtEnd: previewScheduler.LastUnderflowReasonAtEnd");
        AssertContains(previewResultText, "PreviewSchedulerLastUnderflowInputAgeMsAtEnd: previewScheduler.LastUnderflowInputAgeMsAtEnd");
        AssertContains(previewResultText, "PreviewSchedulerLastUnderflowOutputAgeMsAtEnd: previewScheduler.LastUnderflowOutputAgeMsAtEnd");
        AssertDoesNotContain(analysisText, "long PreviewSchedulerDroppedAtEnd");
        AssertDoesNotContain(analysisText, "double PreviewSchedulerMaxScheduleLateMsObserved");
        AssertDoesNotContain(analysisText, "var previewSchedulerDroppedAtEnd =");
        AssertDoesNotContain(analysisText, "var previewSchedulerMaxScheduleLateMsObserved = samples");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionResultBuilder.PreviewScheduler.cs")),
            "preview scheduler analysis folded into DiagnosticSessionResultBuilder.cs");
    }

    private static void AssertDiagnosticSessionResultBuilderOverviewAndCaptureProjectionOwnership()
    {
        var builderText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var flatteningText = ExtractMemberCode(builderText, "FlattenResultProjectionSet");
        var projectionSetText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var analysisText = ReadDiagnosticSessionResultBuilderAnalysisSource();
        var overviewResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var captureResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");

        AssertContains(builderText, "var resultProjections = BuildResultProjectionSet(request, runState, analysis);");
        AssertContains(builderText, "return FlattenResultProjectionSet(");
        AssertContains(builderText, "return new DiagnosticSessionResult\n        {");
        AssertContains(flatteningText, "private static DiagnosticSessionResult FlattenResultProjectionSet(");
        AssertContains(projectionSetText, "Overview: BuildOverviewResultProjection(request, runState, analysis)");
        AssertContains(flatteningText, "var overviewResult = resultProjections.Overview;");
        AssertContains(flatteningText, "Success = overviewResult.Success,");
        AssertContains(overviewResultText, "private readonly record struct DiagnosticSessionOverviewResultProjection(");
        AssertContains(overviewResultText, "private static DiagnosticSessionOverviewResultProjection BuildOverviewResultProjection(");
        AssertContains(overviewResultText, "var verificationSucceeded = request.Verification.HasValue");
        AssertContains(overviewResultText, "Success: DetermineDiagnosticSessionSuccess(request, runState, analysis, verificationSucceeded)");
        AssertContains(overviewResultText, "ProcessCpuPercentAtEnd: GetDouble(lastSnapshot, \"ProcessCpuPercent\")");
        AssertContains(overviewResultText, "var processCpuMaxPercentObserved = GetProcessCpuMaxPercentObserved(request.Samples, lastSnapshot);");
        AssertContains(overviewResultText, "ProcessCpuMaxPercentObserved: processCpuMaxPercentObserved");
        AssertContains(overviewResultText, "private static double GetProcessCpuMaxPercentObserved(");
        AssertContains(overviewResultText, ".Select(sample => GetDouble(sample.Snapshot, \"ProcessCpuPercent\"))");
        AssertContains(overviewResultText, ".Append(GetDouble(lastSnapshot, \"ProcessCpuPercent\"))");
        AssertContains(overviewResultText, "RecordingVerificationMessage: request.Verification.HasValue");
        AssertContains(overviewResultText, "PresentMon: request.PresentMon");
        AssertContains(overviewResultText, "private static bool DetermineDiagnosticSessionSuccess(");
        AssertContains(overviewResultText, "request.CommandFailureCount == 0 &&");
        AssertContains(overviewResultText, "runState.TerminalException is null &&");
        AssertContains(overviewResultText, "analysis.DiagnosticHealthSucceeded &&");
        AssertContains(overviewResultText, "(request.PresentMon is null || request.PresentMon.Success) &&");
        AssertContains(overviewResultText, "(!verificationSucceeded.HasValue || verificationSucceeded.Value) &&");
        AssertContains(overviewResultText, "analysis.FlashbackWarningsSucceeded");
        AssertDoesNotContain(flatteningText, "request.CommandFailureCount == 0 &&");
        AssertDoesNotContain(flatteningText, "ProcessCpuPercentAtEnd = GetDouble(lastSnapshot");
        AssertDoesNotContain(flatteningText, "RecordingVerificationMessage = request.Verification.HasValue");
        AssertDoesNotContain(analysisText, "ProcessCpuMaxPercentObserved");
        AssertDoesNotContain(overviewResultText, "analysis.ProcessCpuMaxPercentObserved");
        AssertContains(projectionSetText, "Capture: BuildCaptureResultProjection(analysis)");
        AssertContains(flatteningText, "var captureResult = resultProjections.Capture;");
        AssertContains(captureResultText, "private readonly record struct DiagnosticSessionCaptureResultProjection(");
        AssertContains(captureResultText, "private static DiagnosticSessionCaptureResultProjection BuildCaptureResultProjection(");
        AssertContains(captureResultText, "SelectedResolutionAtEnd: GetString(lastSnapshot, \"SelectedResolution\") ?? string.Empty");
        AssertContains(captureResultText, "SourceWidthAtEnd: (int)(GetNullableLong(lastSnapshot, \"SourceWidth\") ?? 0)");
        AssertContains(captureResultText, "SourceTelemetrySummaryAtEnd: GetString(lastSnapshot, \"SourceTelemetrySummaryText\") ?? string.Empty");
        AssertDoesNotContain(flatteningText, "SelectedResolutionAtEnd = GetString(lastSnapshot");
        AssertDoesNotContain(flatteningText, "SourceWidthAtEnd = (int)(GetNullableLong");
        AssertDoesNotContain(flatteningText, "SourceTelemetrySummaryAtEnd = GetString(lastSnapshot");
    }

    private static void AssertDiagnosticSessionResultBuilderFlashbackProjectionOwnership()
    {
        var builderText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var flatteningText = ExtractMemberCode(builderText, "FlattenResultProjectionSet");
        var projectionSetText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackResultText = projectionSetText;
        var flashbackRecordingResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var flashbackExportResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");

        AssertContains(builderText, "return FlattenResultProjectionSet(");
        AssertContains(flatteningText, "private static DiagnosticSessionResult FlattenResultProjectionSet(");
        AssertContains(projectionSetText, "FlashbackPlayback: BuildFlashbackPlaybackResultProjection(analysis)");
        AssertContains(flatteningText, "var flashbackPlaybackResult = resultProjections.FlashbackPlayback;");
        AssertContains(flatteningText, "var flashbackPlaybackCommandsResult = flashbackPlaybackResult.CommandsResult;");
        AssertContains(flatteningText, "var flashbackPlaybackCadenceResult = flashbackPlaybackResult.CadenceResult;");
        AssertContains(flatteningText, "var flashbackPlaybackOnePercentLowResult = flashbackPlaybackResult.OnePercentLowResult;");
        AssertContains(flatteningText, "var flashbackPlaybackDecodeResult = flashbackPlaybackResult.DecodeResult;");
        AssertContains(flatteningText, "var flashbackPlaybackAudioMasterResult = flashbackPlaybackResult.AudioMasterResult;");
        AssertContains(flatteningText, "var flashbackPlaybackStagesResult = flashbackPlaybackResult.StagesResult;");
        AssertContains(flashbackPlaybackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackResultProjection(");
        AssertContains(flashbackPlaybackResultText, "private static DiagnosticSessionFlashbackPlaybackResultProjection BuildFlashbackPlaybackResultProjection(");
        AssertContains(flashbackPlaybackResultText, "CommandsResult: commandsResult");
        AssertContains(flashbackPlaybackResultText, "CadenceResult: cadenceResult");
        AssertContains(flashbackPlaybackResultText, "OnePercentLowResult: onePercentLowResult");
        AssertContains(flashbackPlaybackResultText, "DecodeResult: decodeResult");
        AssertContains(flashbackPlaybackResultText, "AudioMasterResult: audioMasterResult");
        AssertContains(flashbackPlaybackResultText, "StagesResult: stagesResult");
        AssertContains(flashbackPlaybackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackCommandsResultProjection(");
        AssertContains(flashbackPlaybackResultText, "private static DiagnosticSessionFlashbackPlaybackCommandsResultProjection BuildFlashbackPlaybackCommandsResultProjection(");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackPendingCommandsAtEnd: playbackResultMetrics.PendingCommandsAtEnd");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackMaxCommandQueueLatencyCommandObserved: playbackResultMetrics.MaxCommandQueueLatencyCommandObserved");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackLastCommandFailureAtEnd: playbackResultMetrics.LastCommandFailureAtEnd");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd: playbackResultMetrics.LastCommandFailureUtcUnixMsAtEnd");
        AssertContains(flashbackPlaybackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackCadenceResultProjection(");
        AssertContains(flashbackPlaybackResultText, "private static DiagnosticSessionFlashbackPlaybackCadenceResultProjection BuildFlashbackPlaybackCadenceResultProjection(");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackDroppedFramesDelta: playbackSessionMetrics.DroppedFramesDelta");
        AssertContains(flashbackPlaybackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackOnePercentLowResultProjection(");
        AssertContains(flashbackPlaybackResultText, "private static DiagnosticSessionFlashbackPlaybackOnePercentLowResultProjection BuildFlashbackPlaybackOnePercentLowResultProjection(");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackMinOnePercentLowFpsObserved: playbackSessionMetrics.MinOnePercentLowFpsObserved");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackMinOnePercentLowDecodeP99Ms: playbackSessionMetrics.MinOnePercentLowDecodeP99Ms");
        AssertContains(flashbackPlaybackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackDecodeResultProjection(");
        AssertContains(flashbackPlaybackResultText, "private static DiagnosticSessionFlashbackPlaybackDecodeResultProjection BuildFlashbackPlaybackDecodeResultProjection(");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackMaxDecodePhaseAtEnd: playbackResultMetrics.MaxDecodePhaseAtEnd");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackMaxDecodeP99MsObserved: playbackSessionMetrics.MaxDecodeP99MsObserved");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackMaxDecodePositionMsObserved: playbackSessionMetrics.MaxDecodePositionMsObserved");
        AssertContains(flashbackPlaybackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackAudioMasterResultProjection(");
        AssertContains(flashbackPlaybackResultText, "private static DiagnosticSessionFlashbackPlaybackAudioMasterResultProjection BuildFlashbackPlaybackAudioMasterResultProjection(");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackAudioMasterFallbacksAtEnd: playbackResultMetrics.AudioMasterFallbacksAtEnd");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackMaxAudioMasterFallbacksObserved: playbackSessionMetrics.MaxAudioMasterFallbacksObserved");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackMaxAbsAvDriftMsObserved: playbackSessionMetrics.MaxAbsAvDriftMsObserved");
        AssertContains(flashbackPlaybackResultText, "private readonly record struct DiagnosticSessionFlashbackPlaybackStagesResultProjection(");
        AssertContains(flashbackPlaybackResultText, "private static DiagnosticSessionFlashbackPlaybackStagesResultProjection BuildFlashbackPlaybackStagesResultProjection(");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackSubmitFailuresDelta: playbackSessionMetrics.SubmitFailuresDelta");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta: playbackResultMetrics.SeekForwardDecodeCapHitsDelta");
        AssertContains(flashbackPlaybackResultText, "FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd: playbackResultMetrics.LastSeekHitForwardDecodeCapAtEnd");
        AssertContains(builderText, "private readonly record struct DiagnosticSessionFlashbackPlaybackResultProjection(");
        AssertContains(flatteningText, "FlashbackPlaybackPendingCommandsAtEnd = flashbackPlaybackCommandsResult.FlashbackPlaybackPendingCommandsAtEnd,");
        AssertContains(flatteningText, "FlashbackPlaybackDroppedFramesDelta = flashbackPlaybackCadenceResult.FlashbackPlaybackDroppedFramesDelta,");
        AssertContains(flatteningText, "FlashbackPlaybackMinOnePercentLowFpsObserved = flashbackPlaybackOnePercentLowResult.FlashbackPlaybackMinOnePercentLowFpsObserved,");
        AssertContains(flatteningText, "FlashbackPlaybackMaxDecodePhaseAtEnd = flashbackPlaybackDecodeResult.FlashbackPlaybackMaxDecodePhaseAtEnd,");
        AssertContains(flatteningText, "FlashbackPlaybackAudioMasterFallbacksAtEnd = flashbackPlaybackAudioMasterResult.FlashbackPlaybackAudioMasterFallbacksAtEnd,");
        AssertContains(flatteningText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta = flashbackPlaybackStagesResult.FlashbackPlaybackSeekForwardDecodeCapHitsDelta,");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackPendingCommandsAtEnd: playbackResultMetrics.PendingCommandsAtEnd");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackMinOnePercentLowFpsObserved: playbackSessionMetrics.MinOnePercentLowFpsObserved");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackMaxDecodePhaseAtEnd: playbackResultMetrics.MaxDecodePhaseAtEnd");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackAudioMasterFallbacksAtEnd: playbackResultMetrics.AudioMasterFallbacksAtEnd");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta: playbackResultMetrics.SeekForwardDecodeCapHitsDelta");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionResultBuilder.FlashbackPlaybackResult.cs")),
            "Flashback playback result projection folded into DiagnosticSessionResultBuilder.cs");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackPendingCommandsAtEnd = playbackResultMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackMinOnePercentLowFpsObserved = playbackSessionMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackMinOnePercentLowFpsObserved = flashbackPlaybackCadenceResult");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackMaxDecodePhaseAtEnd = playbackResultMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackAudioMasterFallbacksAtEnd = playbackResultMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta = playbackResultMetrics");
        AssertContains(projectionSetText, "FlashbackRecording: BuildFlashbackRecordingResultProjection(analysis)");
        AssertContains(flatteningText, "var flashbackRecordingResult = resultProjections.FlashbackRecording;");
        AssertContains(flashbackRecordingResultText, "private readonly record struct DiagnosticSessionFlashbackRecordingResultProjection(");
        AssertContains(flashbackRecordingResultText, "private static DiagnosticSessionFlashbackRecordingResultProjection BuildFlashbackRecordingResultProjection(");
        AssertContains(flashbackRecordingResultText, "FlashbackRecordingBackendObserved: recordingMetrics.BackendObserved");
        AssertContains(flashbackRecordingResultText, "FlashbackRecordingIntegrityQueueDroppedFramesDelta: recordingMetrics.IntegrityQueueDroppedFramesDelta");
        AssertDoesNotContain(flatteningText, "FlashbackRecordingBackendObserved = recordingMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackRecordingIntegrityQueueDroppedFramesDelta = recordingMetrics");
        AssertContains(projectionSetText, "FlashbackExport: BuildFlashbackExportResultProjection(analysis)");
        AssertContains(flatteningText, "var flashbackExportResult = resultProjections.FlashbackExport;");
        AssertContains(flashbackExportResultText, "private readonly record struct DiagnosticSessionFlashbackExportResultProjection(");
        AssertContains(flashbackExportResultText, "private static DiagnosticSessionFlashbackExportResultProjection BuildFlashbackExportResultProjection(");
        AssertContains(flashbackExportResultText, "FlashbackExportObserved: exportMetrics.Observed");
        AssertContains(flashbackExportResultText, "FlashbackExportForceRotateFallbacksDelta: exportMetrics.ForceRotateFallbacksDelta");
        AssertContains(flashbackExportResultText, "LastExportSuccessAtEnd: exportMetrics.LastSuccessAtEnd");
        AssertContains(flashbackExportResultText, "FlashbackExportMaxThroughputBytesPerSecObserved: exportMetrics.MaxThroughputBytesPerSecObserved");
        AssertDoesNotContain(flatteningText, "FlashbackExportObserved = exportMetrics");
        AssertDoesNotContain(flatteningText, "FlashbackExportForceRotateFallbacksAtEnd = flashbackExportForceRotateFallbacksAtEnd");
        AssertDoesNotContain(flatteningText, "LastExportSuccessAtEnd = exportMetrics");
    }
}
