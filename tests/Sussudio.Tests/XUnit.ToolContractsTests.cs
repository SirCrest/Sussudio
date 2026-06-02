using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Tools;
using Xunit;

using System.Diagnostics;
using System.Threading;

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
        var assemblyPath = global::Program.SsctlAssemblyRelativePath;
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
        var assemblyPath = global::Program.SsctlAssemblyRelativePath;
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
        var assembly = ToolFormatterTestAssembly.Load(global::Program.SsctlAssemblyRelativePath);
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

            global::Program.RequireFreshToolAssembly(relativeAssemblyPath, fullPath);
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
    internal static Task McpToolSurface_KeepsCaptureOptionsSeparateFromRawState()
    {
        var automationControlToolsText = ReadRepoFile("tools/McpServer/Tools/AutomationControlTools.cs");
        var captureSettingsToolsText = automationControlToolsText;
        var appStateToolText = ReadRepoFile("tools/McpServer/Tools/AppStateTools.cs");
        var captureOptionsToolText = captureSettingsToolsText;
        var uiSettingsToolText = automationControlToolsText;
        var automationSnapshotText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.cs");

        AssertContains(captureSettingsToolsText, "string? preset = null");
        AssertContains(captureSettingsToolsText, "string? splitEncodeMode = null");
        AssertContains(captureSettingsToolsText, "int? mjpegDecoderCount = null");
        AssertContains(captureSettingsToolsText, "AutomationCommandKind.SetPreset");
        AssertContains(captureSettingsToolsText, "AutomationCommandKind.SetSplitEncodeMode");
        AssertContains(captureSettingsToolsText, "AutomationCommandKind.SetMjpegDecoderCount");

        AssertContains(appStateToolText, "get_app_state_raw");
        AssertContains(appStateToolText, "UseStructuredContent = true");
        AssertDoesNotContain(appStateToolText, "SendCommandAsync(\"GetCaptureOptions\")");
        AssertContains(captureOptionsToolText, "get_capture_options");
        AssertContains(captureOptionsToolText, "AutomationCommandKind.GetCaptureOptions");
        AssertContains(captureOptionsToolText, "UseStructuredContent = true");
        AssertContains(uiSettingsToolText, "configure_ui");
        AssertContains(uiSettingsToolText, "\"SetPreviewVolume\"");
        AssertContains(uiSettingsToolText, "\"SetStatsVisible\"");
        AssertDoesNotContain(automationSnapshotText, " Options { get; init;");

        return Task.CompletedTask;
    }

    internal static Task McpToolSurface_FixedAutomationRoutesUseAutomationCommandKinds()
    {
        var formatterText = ReadRepoFile("tools/McpServer/Tools/ToolCommandFormatter.cs");
        var appStateToolText = ReadRepoFile("tools/McpServer/Tools/AppStateTools.cs");
        var automationControlToolsText = ReadRepoFile("tools/McpServer/Tools/AutomationControlTools.cs");
        var captureSettingsToolsText = automationControlToolsText;
        var captureOptionsToolText = captureSettingsToolsText;
        var deviceToolsText = captureSettingsToolsText;
        var diagnosticsToolsText = appStateToolText;
        var flashbackToolsText = automationControlToolsText;
        var flashbackActionsText = flashbackToolsText;
        var flashbackExportText = flashbackToolsText;
        var framePacingVerdictToolsText = ReadRepoFile("tools/McpServer/Tools/FramePacingVerdictTools.cs");
        var memoryDiagnosticsToolsText = appStateToolText;
        var pipelineSettingsToolsText = captureSettingsToolsText;
        var performanceToolsText = ReadRepoFile("tools/McpServer/Tools/PerformanceTools.cs");
        var performanceTimelineToolsText = performanceToolsText;
        var previewToolsText = automationControlToolsText;
        var previewInspectionToolsText = ReadRepoFile("tools/McpServer/Tools/PreviewInspectionTools.cs");
        var previewColorProbeToolsText = previewInspectionToolsText;
        var recordingToolsText = previewToolsText;
        var presentMonToolsText = performanceToolsText;
        var previewFrameCaptureToolsText = previewInspectionToolsText;
        var verificationToolsText = automationControlToolsText;
        var videoSourceProbeToolsText = previewColorProbeToolsText;
        var windowToolsText = automationControlToolsText;
        var windowScreenshotToolsText = previewFrameCaptureToolsText;
        var waitToolsText = previewToolsText;

        AssertContains(formatterText, "AutomationCommandKind Kind,");
        AssertContains(formatterText, "pipeClient.SendCommandAsync(command.Kind, command.Payload)");
        AssertContains(formatterText, "if (!AutomationSnapshotFormatter.IsSuccess(response))");
        AssertContains(formatterText, "isError = true;\n                break;");
        AssertDoesNotContain(formatterText, "string CommandName");
        AssertDoesNotContain(formatterText, "SendCommandAsync(command.CommandName");
        AssertDoesNotContain(formatterText, "pipeClient.SendCommandAsync(commandName");

        AssertContains(appStateToolText, "SendCommandAsync(AutomationCommandKind.GetSnapshot)");
        AssertDoesNotContain(appStateToolText, "SendCommandAsync(\"GetSnapshot\"");
        AssertContains(captureOptionsToolText, "SendCommandAsync(AutomationCommandKind.GetCaptureOptions)");
        AssertDoesNotContain(captureOptionsToolText, "SendCommandAsync(\"GetCaptureOptions\"");
        AssertContains(diagnosticsToolsText, "SendCommandAsync(AutomationCommandKind.GetDiagnostics, payload)");
        AssertDoesNotContain(diagnosticsToolsText, "SendCommandAsync(\"GetDiagnostics\"");

        foreach (var commandName in new[]
        {
            "SetResolution",
            "SetFrameRate",
            "SetVideoFormat",
            "SetRecordingFormat",
            "SetQuality",
            "SetCustomBitrate",
            "SetPreset",
            "SetSplitEncodeMode",
            "SetMjpegDecoderCount"
        })
        {
            AssertContains(captureSettingsToolsText, $"ToolCommandFormatter.Optional(AutomationCommandKind.{commandName}, \"{commandName}\"");
            AssertDoesNotContain(captureSettingsToolsText, $"ToolCommandFormatter.Optional(\"{commandName}\"");
        }

        foreach (var commandName in new[]
        {
            "RefreshDevices",
            "SelectDevice",
            "SelectAudioInputDevice",
            "SetCustomAudioInput"
        })
        {
            AssertContains(deviceToolsText, $"AutomationCommandKind.{commandName}");
            AssertDoesNotContain(deviceToolsText, $"ToolCommandFormatter.Optional(\"{commandName}\"");
        }

        foreach (var commandName in new[]
        {
            "SetHdrEnabled",
            "SetTrueHdrPreviewEnabled",
            "SetAudioEnabled",
            "SetAudioPreviewEnabled",
            "SetOutputPath"
        })
        {
            AssertContains(pipelineSettingsToolsText, $"ToolCommandFormatter.Optional(AutomationCommandKind.{commandName}, \"{commandName}\"");
            AssertDoesNotContain(pipelineSettingsToolsText, $"ToolCommandFormatter.Optional(\"{commandName}\"");
        }

        foreach (var commandName in new[] { "SetDeviceAudioMode", "SetAnalogAudioGain" })
        {
            AssertContains(pipelineSettingsToolsText, $"ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.{commandName}, \"{commandName}\"");
            AssertDoesNotContain(pipelineSettingsToolsText, $"ExecuteAndFormatResultAsync(pipeClient, \"{commandName}\"");
        }

        AssertContains(previewToolsText, "ExecuteAndFormatResultAsync(\n                pipeClient,\n                AutomationCommandKind.SetPreviewEnabled,\n                \"SetPreviewEnabled\",");
        AssertDoesNotContain(previewToolsText, "ExecuteAndFormatResultAsync(\n                pipeClient,\n                \"SetPreviewEnabled\",");

        AssertContains(recordingToolsText, "ExecuteAndFormatResultAsync(\n                pipeClient,\n                AutomationCommandKind.SetRecordingEnabled,\n                \"SetRecordingEnabled\",");
        AssertDoesNotContain(recordingToolsText, "ExecuteAndFormatResultAsync(\n                pipeClient,\n                \"SetRecordingEnabled\",");

        AssertContains(flashbackToolsText, "AutomationCommandKind.SetFlashbackEnabled");
        AssertContains(flashbackToolsText, "AutomationCommandKind.RestartFlashback");
        AssertDoesNotContain(flashbackToolsText, "commandName: \"SetFlashbackEnabled\"");
        AssertDoesNotContain(flashbackToolsText, "commandName: \"RestartFlashback\"");
        AssertContains(flashbackActionsText, "AutomationCommandKind.FlashbackAction");
        AssertDoesNotContain(flashbackActionsText, "commandName: \"FlashbackAction\"");
        AssertContains(flashbackExportText, "SendCommandAsync(AutomationCommandKind.FlashbackExport, payload)");
        AssertDoesNotContain(flashbackExportText, "SendCommandAsync(\"FlashbackExport\"");
        AssertContains(flashbackToolsText, "SendCommandAsync(AutomationCommandKind.FlashbackGetSegments)");
        AssertDoesNotContain(flashbackToolsText, "SendCommandAsync(\"FlashbackGetSegments\"");
        AssertContains(framePacingVerdictToolsText, "SendCommandAsync(AutomationCommandKind.GetSnapshot)");
        AssertContains(framePacingVerdictToolsText, "SendCommandAsync(AutomationCommandKind.GetPerformanceTimeline, timelinePayload)");
        AssertDoesNotContain(framePacingVerdictToolsText, "SendCommandAsync(\"GetSnapshot\"");
        AssertDoesNotContain(framePacingVerdictToolsText, "SendCommandAsync(\"GetPerformanceTimeline\"");
        AssertContains(memoryDiagnosticsToolsText, "SendCommandAsync(AutomationCommandKind.GetSnapshot)");
        AssertDoesNotContain(memoryDiagnosticsToolsText, "SendCommandAsync(\"GetSnapshot\"");
        AssertContains(performanceTimelineToolsText, "SendCommandAsync(AutomationCommandKind.GetPerformanceTimeline, payload)");
        AssertDoesNotContain(performanceTimelineToolsText, "SendCommandAsync(\"GetPerformanceTimeline\"");
        AssertContains(previewColorProbeToolsText, "SendCommandAsync(AutomationCommandKind.ProbePreviewColor)");
        AssertDoesNotContain(previewColorProbeToolsText, "SendCommandAsync(\"ProbePreviewColor\"");
        AssertContains(previewFrameCaptureToolsText, "SendCommandAsync(AutomationCommandKind.CapturePreviewFrame, payload)");
        AssertDoesNotContain(previewFrameCaptureToolsText, "SendCommandAsync(\"CapturePreviewFrame\", payload)");
        AssertContains(presentMonToolsText, "SendCommandAsync(AutomationCommandKind.GetSnapshot)");
        AssertDoesNotContain(presentMonToolsText, "SendCommandAsync(\"GetSnapshot\"");
        AssertContains(verificationToolsText, "SendCommandAsync(AutomationCommandKind.VerifyLastRecording)");
        AssertContains(verificationToolsText, "SendCommandAsync(AutomationCommandKind.AssertSnapshot, payload)");
        AssertContains(verificationToolsText, "SendCommandAsync(AutomationCommandKind.VerifyFile, payload)");
        AssertDoesNotContain(verificationToolsText, "SendCommandAsync(\"VerifyLastRecording\"");
        AssertDoesNotContain(verificationToolsText, "SendCommandAsync(\"AssertSnapshot\"");
        AssertDoesNotContain(verificationToolsText, "SendCommandAsync(\"VerifyFile\"");
        AssertContains(videoSourceProbeToolsText, "SendCommandAsync(AutomationCommandKind.ProbeVideoSource)");
        AssertDoesNotContain(videoSourceProbeToolsText, "SendCommandAsync(\"ProbeVideoSource\"");
        AssertContains(windowToolsText, "SendCommandAsync(AutomationCommandKind.ArmClose, armPayload)");
        AssertContains(windowToolsText, "var actionId = Guid.NewGuid().ToString(\"N\");");
        AssertContains(windowToolsText, "[\"actionId\"] = actionId");
        AssertContains(windowToolsText, "actionPayload[\"actionId\"] = actionId;");
        AssertContains(windowToolsText, "SendCommandAsync(AutomationCommandKind.WindowAction, actionPayload)");
        AssertContains(windowToolsText, "AutomationCommandKind.SetFullScreenEnabled");
        AssertContains(windowToolsText, "AutomationCommandKind.OpenRecordingsFolder");
        AssertDoesNotContain(windowToolsText, "SendCommandAsync(\"ArmClose\"");
        AssertDoesNotContain(windowToolsText, "SendCommandAsync(\"WindowAction\"");
        AssertDoesNotContain(windowToolsText, "ExecuteAndFormatResultAsync(\n                pipeClient,\n                \"SetFullScreenEnabled\"");
        AssertDoesNotContain(windowToolsText, "ExecuteAndFormatResultAsync(\n                pipeClient,\n                \"OpenRecordingsFolder\"");
        AssertContains(windowScreenshotToolsText, "SendCommandAsync(AutomationCommandKind.CaptureWindowScreenshot, payload)");
        AssertDoesNotContain(windowScreenshotToolsText, "SendCommandAsync(\"CaptureWindowScreenshot\", payload)");
        AssertContains(waitToolsText, "SendCommandAsync(AutomationCommandKind.WaitForCondition, payload, responseTimeoutMs)");
        AssertContains(waitToolsText, "AutomationPipeProtocol.GetDefaultResponseTimeout(AutomationCommandKind.WaitForCondition)");
        AssertDoesNotContain(waitToolsText, "WaitForConditionCommandName");
        AssertDoesNotContain(waitToolsText, "SendCommandAsync(\"WaitForCondition\"");

        return Task.CompletedTask;
    }

    internal static async Task McpDeviceTools_RouteRefreshSelectionsAndCustomAudio()
    {
        var pipeName = NewMcpToolPipeName("device");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var deviceTools = RequireMcpType("McpServer.Tools.DeviceTools");

        var empty = await InvokeMcpToolStringAsync(
            deviceTools,
            "configure_device",
            pipeClient,
            null,
            null,
            null,
            null,
            false,
            null).ConfigureAwait(false);
        AssertEqual("No device configuration changes requested.", empty, "configure_device empty result");

        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 4,
                () => InvokeMcpToolStringAsync(
                    deviceTools,
                    "configure_device",
                    pipeClient,
                    "capture-id",
                    "Capture Name",
                    "audio-id",
                    "Audio Name",
                    true,
                    true))
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "RefreshDevices");
        AssertCommandRequest(
            requests[1],
            "SelectDevice",
            ("deviceId", "capture-id"),
            ("deviceName", "Capture Name"));
        AssertCommandRequest(
            requests[2],
            "SelectAudioInputDevice",
            ("deviceId", "audio-id"),
            ("deviceName", "Audio Name"));
        AssertCommandRequest(requests[3], "SetCustomAudioInput", ("enabled", true));
    }

    internal static async Task McpCaptureSettingsTools_RouteProvidedSettings()
    {
        var pipeName = NewMcpToolPipeName("capture");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var captureSettingsTools = RequireMcpType("McpServer.Tools.CaptureSettingsTools");

        var empty = await InvokeMcpToolStringAsync(
            captureSettingsTools,
            "configure_capture",
            pipeClient,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null).ConfigureAwait(false);
        AssertEqual("No capture setting changes requested.", empty, "configure_capture empty result");

        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 9,
                () => InvokeMcpToolStringAsync(
                    captureSettingsTools,
                    "configure_capture",
                    pipeClient,
                    "3840x2160",
                    59.94d,
                    "MJPG",
                    "Hevc",
                    "High",
                    80d,
                    "P5",
                    "ForcedOn",
                    4))
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "SetResolution", ("resolution", "3840x2160"));
        AssertCommandRequest(requests[1], "SetFrameRate", ("frameRate", 59.94d));
        AssertCommandRequest(requests[2], "SetVideoFormat", ("videoFormat", "MJPG"));
        AssertCommandRequest(requests[3], "SetRecordingFormat", ("format", "Hevc"));
        AssertCommandRequest(requests[4], "SetQuality", ("quality", "High"));
        AssertCommandRequest(requests[5], "SetCustomBitrate", ("bitrateMbps", 80d));
        AssertCommandRequest(requests[6], "SetPreset", ("preset", "P5"));
        AssertCommandRequest(requests[7], "SetSplitEncodeMode", ("splitEncodeMode", "ForcedOn"));
        AssertCommandRequest(requests[8], "SetMjpegDecoderCount", ("decoderCount", 4));
    }

    internal static async Task McpPipelineSettingsTools_RoutePipelineAndAudioCommands()
    {
        var pipeName = NewMcpToolPipeName("pipeline");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var pipelineTools = RequireMcpType("McpServer.Tools.PipelineSettingsTools");

        var empty = await InvokeMcpToolStringAsync(
            pipelineTools,
            "configure_pipeline",
            pipeClient,
            null,
            null,
            null,
            null,
            null).ConfigureAwait(false);
        AssertEqual("No pipeline setting changes requested.", empty, "configure_pipeline empty result");

        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 7,
                async () =>
                {
                    await InvokeMcpToolStringAsync(
                        pipelineTools,
                        "configure_pipeline",
                        pipeClient,
                        true,
                        false,
                        true,
                        false,
                        @"C:\captures").ConfigureAwait(false);
                    await InvokeMcpToolStringAsync(
                        pipelineTools,
                        "configure_audio_mode",
                        pipeClient,
                        "Analog").ConfigureAwait(false);
                    await InvokeMcpToolStringAsync(
                        pipelineTools,
                        "configure_analog_gain",
                        pipeClient,
                        42.5d).ConfigureAwait(false);
                })
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "SetHdrEnabled", ("enabled", true));
        AssertCommandRequest(requests[1], "SetTrueHdrPreviewEnabled", ("enabled", false));
        AssertCommandRequest(requests[2], "SetAudioEnabled", ("enabled", false));
        AssertCommandRequest(requests[3], "SetAudioPreviewEnabled", ("enabled", true));
        AssertCommandRequest(requests[4], "SetOutputPath", ("outputPath", @"C:\captures"));
        AssertCommandRequest(requests[5], "SetDeviceAudioMode", ("mode", "analog"));
        AssertCommandRequest(requests[6], "SetAnalogAudioGain", ("gain", 42.5d));
    }

    internal static async Task McpRecordingTools_RouteRecordingToggle()
    {
        var pipeName = NewMcpToolPipeName("recording");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var recordingTools = RequireMcpType("McpServer.Tools.RecordingTools");

        string successResult = string.Empty;
        string failureResult = string.Empty;
        string missingMessageResult = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 3,
                async () =>
                {
                    successResult = await InvokeMcpToolStringAsync(
                            recordingTools,
                            "control_recording",
                            pipeClient,
                            true)
                        .ConfigureAwait(false);
                    failureResult = await InvokeMcpToolStringAsync(
                            recordingTools,
                            "control_recording",
                            pipeClient,
                            false)
                        .ConfigureAwait(false);
                    missingMessageResult = await InvokeMcpToolStringAsync(
                            recordingTools,
                            "control_recording",
                            pipeClient,
                            false)
                        .ConfigureAwait(false);
                },
                i => i switch
                {
                    0 => "{\"Success\":true,\"Message\":\"recording started\"}",
                    1 => "{\"Success\":false,\"Message\":\"stop failed\"}",
                    _ => "{\"Success\":false}"
                })
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "SetRecordingEnabled", ("enabled", true));
        AssertCommandRequest(requests[1], "SetRecordingEnabled", ("enabled", false));
        AssertCommandRequest(requests[2], "SetRecordingEnabled", ("enabled", false));
        AssertEqual("[OK] SetRecordingEnabled: recording started", successResult, "control_recording formatted success");
        AssertEqual("[ERROR] SetRecordingEnabled: stop failed", failureResult, "control_recording formatted failure");
        AssertEqual("[ERROR] SetRecordingEnabled: No message.", missingMessageResult, "control_recording missing message fallback");
    }

    internal static async Task McpUiSettingsTools_RouteUiCommands()
    {
        var pipeName = NewMcpToolPipeName("ui");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var uiSettingsTools = RequireMcpType("McpServer.Tools.UiSettingsTools");

        var empty = await InvokeMcpToolStringAsync(
            uiSettingsTools,
            "configure_ui",
            pipeClient,
            null,
            null,
            null).ConfigureAwait(false);
        AssertEqual("No UI setting changes requested.", empty, "configure_ui empty result");

        var results = new List<string>();
        string result = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 7,
                async () =>
                {
                    results.Add(await InvokeMcpToolStringAsync(
                            uiSettingsTools,
                            "configure_ui",
                            pipeClient,
                            true,
                            33.5d,
                            false)
                        .ConfigureAwait(false));
                    results.Add(await InvokeMcpToolStringAsync(
                            uiSettingsTools,
                            "configure_settings_panel",
                            pipeClient,
                            true)
                        .ConfigureAwait(false));
                    results.Add(await InvokeMcpToolStringAsync(
                            uiSettingsTools,
                            "configure_frametime_graph",
                            pipeClient,
                            true)
                        .ConfigureAwait(false));
                    results.Add(await InvokeMcpToolStringAsync(
                            uiSettingsTools,
                            "configure_flashback_timeline",
                            pipeClient,
                            false)
                        .ConfigureAwait(false));
                    results.Add(await InvokeMcpToolStringAsync(
                            uiSettingsTools,
                            "configure_stats_section",
                            pipeClient,
                            "Source",
                            false)
                        .ConfigureAwait(false));
                    result = string.Join(Environment.NewLine, results);
                },
                i => $$"""{"Success":true,"Message":"ui command {{i}} ok"}""")
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "SetShowAllCaptureOptions", ("enabled", true));
        AssertCommandRequest(requests[1], "SetPreviewVolume", ("previewVolumePercent", 33.5d));
        AssertCommandRequest(requests[2], "SetStatsVisible", ("visible", false));
        AssertCommandRequest(requests[3], "SetSettingsVisible", ("visible", true));
        AssertCommandRequest(requests[4], "SetFrameTimeOverlayVisible", ("visible", true));
        AssertCommandRequest(requests[5], "SetFlashbackTimelineVisible", ("visible", false));
        AssertCommandRequest(requests[6], "SetStatsSectionVisible", ("section", "Source"), ("visible", false));
        AssertEqual(
            string.Join(
                Environment.NewLine,
                "[OK] SetShowAllCaptureOptions: ui command 0 ok",
                "[OK] SetPreviewVolume: ui command 1 ok",
                "[OK] SetStatsVisible: ui command 2 ok",
                "[OK] SetSettingsVisible: ui command 3 ok",
                "[OK] SetFrameTimeOverlayVisible: ui command 4 ok",
                "[OK] SetFlashbackTimelineVisible: ui command 5 ok",
                "[OK] SetStatsSectionVisible: ui command 6 ok"),
            result,
            "MCP UI command formatted output");
    }

    internal static async Task McpToolCommandFormatter_BatchesPendingCommands()
    {
        var pipeName = NewMcpToolPipeName("formatter");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var formatterType = RequireMcpType("McpServer.Tools.ToolCommandFormatter");
        var optional = formatterType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .SingleOrDefault(method =>
            {
                if (method.Name != "Optional" || method.IsGenericMethodDefinition)
                {
                    return false;
                }

                var parameters = method.GetParameters();
                return parameters.Length == 4 &&
                       parameters[0].ParameterType.FullName == "Sussudio.Models.AutomationCommandKind" &&
                       parameters[1].ParameterType == typeof(string) &&
                       parameters[2].ParameterType == typeof(bool) &&
                       parameters[3].ParameterType == typeof(Dictionary<string, object?>);
            })
            ?? throw new InvalidOperationException("ToolCommandFormatter.Optional overload was not found.");
        var automationCommandKindType = optional.GetParameters()[0].ParameterType;
        var pendingType = optional.ReturnType;
        var executeBatch = formatterType.GetMethod(
                "ExecuteBatchAsync",
                BindingFlags.Static | BindingFlags.NonPublic,
                binder: null,
                types:
                [
                    pipeClient.GetType(),
                    typeof(string),
                    pendingType.MakeArrayType()
                ],
                modifiers: null)
            ?? throw new InvalidOperationException("ToolCommandFormatter.ExecuteBatchAsync was not found.");
        var emptyCommands = Array.CreateInstance(pendingType, 0);
        var emptyResult = await InvokeFormatterBatchAsync(executeBatch, pipeClient, "nothing to do", emptyCommands).ConfigureAwait(false);
        AssertEqual("nothing to do", emptyResult, "ToolCommandFormatter empty batch result");

        var firstPending = optional.Invoke(
            null,
            new object?[]
            {
                Enum.Parse(automationCommandKindType, "SetStatsVisible"),
                "SetStatsVisible",
                true,
                new Dictionary<string, object?> { ["visible"] = true }
            });
        var secondPending = optional.Invoke(
            null,
            new object?[]
            {
                Enum.Parse(automationCommandKindType, "SetSettingsVisible"),
                "SetSettingsVisible",
                true,
                new Dictionary<string, object?> { ["visible"] = false }
            });
        var commands = Array.CreateInstance(pendingType, 2);
        commands.SetValue(firstPending, 0);
        commands.SetValue(secondPending, 1);

        string result = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 2,
                async () =>
                {
                    result = await InvokeFormatterBatchAsync(executeBatch, pipeClient, "nothing to do", commands).ConfigureAwait(false);
                },
                i => i == 0
                    ? "{\"Success\":true,\"Message\":\"stats updated\"}"
                    : "{\"Success\":false,\"Message\":\"settings blocked\"}")
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "SetStatsVisible", ("visible", true));
        AssertCommandRequest(requests[1], "SetSettingsVisible", ("visible", false));
        AssertEqual(
            "[OK] SetStatsVisible: stats updated" + Environment.NewLine + "[ERROR] SetSettingsVisible: settings blocked",
            result,
            "ToolCommandFormatter ordered joined batch result");

        string failFastResult = string.Empty;
        var failFastRequests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 1,
                async () =>
                {
                    failFastResult = await InvokeFormatterBatchAsync(executeBatch, pipeClient, "nothing to do", commands).ConfigureAwait(false);
                },
                _ => "{\"Success\":false,\"Message\":\"stats blocked\"}")
            .ConfigureAwait(false);

        AssertCommandRequest(failFastRequests[0], "SetStatsVisible", ("visible", true));
        AssertEqual(
            "[ERROR] SetStatsVisible: stats blocked",
            failFastResult,
            "ToolCommandFormatter stops batch after first failed mutation");
    }

    internal static async Task McpHostToolSchema_UsesPipeClientAsService()
    {
        var assemblyPath = global::Program.McpServerAssemblyRelativePath;
        LoadToolAssemblyIsolated(assemblyPath);

        using var process = StartMcpServerProcess(
            assemblyPath,
            NewMcpToolPipeName("host-pipe-failure"));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        _ = Task.Run(async () =>
        {
            try
            {
                await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
            }
            catch
            {
            }
        });

        try
        {
            await WriteJsonRpcLineAsync(
                    process,
                    """
                    {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"Sussudio.Tests","version":"1.0"}}}
                    """,
                    cts.Token)
                .ConfigureAwait(false);
            await ReadJsonRpcResponseAsync(process, 1, cts.Token).ConfigureAwait(false);

            await WriteJsonRpcLineAsync(
                    process,
                    """
                    {"jsonrpc":"2.0","method":"notifications/initialized","params":{}}
                    """,
                    cts.Token)
                .ConfigureAwait(false);
            await WriteJsonRpcLineAsync(
                    process,
                    """
                    {"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}
                    """,
                    cts.Token)
                .ConfigureAwait(false);

            using var toolsListDocument = await ReadJsonRpcResponseAsync(process, 2, cts.Token).ConfigureAwait(false);
            var tools = toolsListDocument.RootElement.GetProperty("result").GetProperty("tools");
            AssertNoToolSchemaExposesPipeClient(tools);
        }
        finally
        {
            await StopMcpServerProcessAsync(process).ConfigureAwait(false);
        }
    }

    internal static async Task McpPipeClient_HonorsSussudioAutomationPipeEnvironment()
    {
        var pipeName = NewMcpToolPipeName("env");
        var previousPipeName = Environment.GetEnvironmentVariable("SUSSUDIO_AUTOMATION_PIPE");
        Environment.SetEnvironmentVariable("SUSSUDIO_AUTOMATION_PIPE", pipeName);
        try
        {
            var pipeClient = CreateDefaultMcpPipeClient();
            var appStateTools = RequireMcpType("McpServer.Tools.AppStateTools");

            var requests = await CapturePipeRequestsAsync(
                    pipeName,
                    expectedCount: 1,
                    async () =>
                    {
                        _ = await InvokeMcpToolResultAsync(
                                appStateTools,
                                "get_app_state_raw",
                                pipeClient)
                            .ConfigureAwait(false);
                    },
                    _ => "{\"Success\":true,\"Snapshot\":{\"SessionState\":\"Ready\"}}")
                .ConfigureAwait(false);

            AssertCommandRequest(requests[0], "GetSnapshot");
        }
        finally
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_AUTOMATION_PIPE", previousPipeName);
        }
    }

    internal static async Task McpHostToolInvocation_ReturnsPipeFailureInsteadOfClosingTransport()
    {
        var assemblyPath = global::Program.McpServerAssemblyRelativePath;
        LoadToolAssemblyIsolated(assemblyPath);

        using var process = StartMcpServerProcess(
            assemblyPath,
            NewMcpToolPipeName("host-pipe-failure"));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        _ = Task.Run(async () =>
        {
            try
            {
                await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
            }
            catch
            {
            }
        });

        try
        {
            await WriteJsonRpcLineAsync(
                    process,
                    """
                    {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"Sussudio.Tests","version":"1.0"}}}
                    """,
                    cts.Token)
                .ConfigureAwait(false);
            await ReadJsonRpcResponseAsync(process, 1, cts.Token).ConfigureAwait(false);

            await WriteJsonRpcLineAsync(
                    process,
                    """
                    {"jsonrpc":"2.0","method":"notifications/initialized","params":{}}
                    """,
                    cts.Token)
                .ConfigureAwait(false);
            await WriteJsonRpcLineAsync(
                    process,
                    """
                    {"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"get_app_state","arguments":{}}}
                    """,
                    cts.Token)
                .ConfigureAwait(false);

            using var response = await ReadJsonRpcResponseAsync(process, 2, cts.Token).ConfigureAwait(false);
            var resultElement = response.RootElement.GetProperty("result");
            AssertEqual(true, resultElement.GetProperty("isError").GetBoolean(), "get_app_state pipe failure MCP isError");
            var content = resultElement.GetProperty("content");
            var text = content[0].GetProperty("text").GetString() ?? string.Empty;
            AssertContains(text, "Timed out connecting to automation pipe");
            AssertContains(text, "pipe-connect-timeout");

            await WriteJsonRpcLineAsync(
                    process,
                    """
                    {"jsonrpc":"2.0","id":3,"method":"tools/list","params":{}}
                    """,
                    cts.Token)
                .ConfigureAwait(false);
            using var toolsListResponse = await ReadJsonRpcResponseAsync(process, 3, cts.Token).ConfigureAwait(false);
            AssertEqual(
                true,
                toolsListResponse.RootElement.GetProperty("result").GetProperty("tools").GetArrayLength() > 0,
                "MCP transport remains open after pipe failure");
        }
        finally
        {
            await StopMcpServerProcessAsync(process).ConfigureAwait(false);
        }
    }

    internal static async Task McpVerificationTools_FormatVerificationResponses()
    {
        var verificationTools = RequireMcpType("McpServer.Tools.VerificationTools");

        var blankAssertions = await InvokeMcpToolStringAsync(
            verificationTools,
            "assert_snapshot",
            CreateMcpPipeClient(NewMcpToolPipeName("assert-empty")),
            string.Empty).ConfigureAwait(false);
        AssertEqual("The assertions parameter must be a JSON array string.", blankAssertions, "assert_snapshot blank input");

        var invalidAssertions = await InvokeMcpToolStringAsync(
            verificationTools,
            "assert_snapshot",
            CreateMcpPipeClient(NewMcpToolPipeName("assert-invalid")),
            "{\"field\":\"IsRecording\"}").ConfigureAwait(false);
        AssertEqual("The assertions parameter must be a JSON array string.", invalidAssertions, "assert_snapshot non-array input");

        var recordingResult = string.Empty;
        var fileResult = string.Empty;
        var assertResult = string.Empty;
        var missingRecordingResult = string.Empty;
        var missingFileResult = string.Empty;
        var pipeName = NewMcpToolPipeName("verification");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 5,
                async () =>
                {
                    recordingResult = await InvokeMcpToolStringAsync(
                            verificationTools,
                            "verify_recording",
                            pipeClient)
                        .ConfigureAwait(false);
                    fileResult = await InvokeMcpToolStringAsync(
                            verificationTools,
                            "verify_file",
                            pipeClient,
                            @"C:\captures\clip.mp4",
                            "flashback-export")
                        .ConfigureAwait(false);
                    assertResult = await InvokeMcpToolStringAsync(
                            verificationTools,
                            "assert_snapshot",
                            pipeClient,
                            """[{"field":"IsRecording","op":"eq","value":false}]""")
                        .ConfigureAwait(false);
                    missingRecordingResult = await InvokeMcpToolStringAsync(
                            verificationTools,
                            "verify_recording",
                            pipeClient)
                        .ConfigureAwait(false);
                    missingFileResult = await InvokeMcpToolStringAsync(
                            verificationTools,
                            "verify_file",
                            pipeClient,
                            @"C:\captures\missing.mp4",
                            null)
                        .ConfigureAwait(false);
                },
                i => i switch
                {
                    0 => """
                         {
                           "Success": true,
                           "Message": "last recording verified",
                           "Data": {
                             "Verification": {
                               "OutputPath": "C:\\captures\\latest.mp4",
                               "FileExists": true,
                               "FileSizeBytes": 123456,
                               "VerificationMode": "LastRecording",
                               "DetectedVideoCodec": "hevc",
                               "DetectedPixelFormat": "p010le",
                               "DetectedWidth": 3840,
                               "DetectedHeight": 2160,
                               "DetectedFrameRate": 59.94,
                               "HdrVerificationLevel": "Strict",
                               "HdrMetadataPresent": true,
                               "HdrColorimetryValid": true,
                               "HdrMasteringMetadataPresent": false,
                               "Mismatches": []
                             }
                           }
                         }
                         """,
                    1 => """
                         {
                           "Success": false,
                           "Message": "file mismatch",
                           "Snapshot": {
                             "LastVerification": {
                               "FileExists": true,
                               "FileSizeBytes": 42,
                               "DetectedVideoCodec": "h264",
                               "DetectedPixelFormat": "yuv420p",
                               "DetectedWidth": 1920,
                               "DetectedHeight": 1080,
                               "DetectedFrameRate": 30
                             }
                           }
                         }
                         """,
                    2 => """
                         {
                           "Success": false,
                           "Message": "1 assertion failed",
                           "Data": {
                             "assertions": 1,
                             "passed": false,
                             "failures": ["IsRecording expected false"]
                           }
                         }
                         """,
                    3 => "{\"Success\":true,\"Message\":\"no verification data\"}",
                    _ => "{\"Success\":false,\"Message\":\"file not found\"}"
                })
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "VerifyLastRecording");
        AssertCommandRequest(requests[1], "VerifyFile", ("filePath", @"C:\captures\clip.mp4"), ("verificationProfile", "flashback-export"));
        AssertAutomationCommandId(requests[2], "AssertSnapshot");
        AssertCommandRequest(requests[3], "VerifyLastRecording");
        AssertCommandRequest(requests[4], "VerifyFile", ("filePath", @"C:\captures\missing.mp4"));
        var assertPayload = requests[2].GetProperty("payload");
        AssertJsonObjectPropertyNames(assertPayload, "assertions");
        var assertions = requests[2].GetProperty("payload").GetProperty("assertions");
        AssertEqual(JsonValueKind.Array, assertions.ValueKind, "AssertSnapshot assertions payload kind");
        AssertEqual(1, assertions.GetArrayLength(), "AssertSnapshot assertions payload count");
        var assertion = assertions[0];
        AssertJsonObjectPropertyNames(assertion, "field", "op", "value");
        AssertEqual("IsRecording", assertion.GetProperty("field").GetString(), "AssertSnapshot field payload");
        AssertEqual("eq", assertion.GetProperty("op").GetString(), "AssertSnapshot op payload");
        AssertEqual(JsonValueKind.False, assertion.GetProperty("value").ValueKind, "AssertSnapshot value payload kind");

        AssertEqual(
            """
            == Recording Verification: PASS ==
            Message: last recording verified
            Output: C:\captures\latest.mp4 | Exists: true | Size: 123456 bytes
            Mode: LastRecording | Codec: hevc | Pixel Format: p010le
            Resolution: 3840 x 2160 | FPS: 59.94
            HDR: Level=Strict Metadata=true Colorimetry=true Mastering=false
            Mismatches: None
            """,
            recordingResult.Replace("\r\n", "\n"),
            "verify_recording exact text");
        AssertEqual(
            """
            == File Verification: FAIL ==
            Message: file mismatch
            File: C:\captures\clip.mp4 | Exists: true | Size: 42 bytes
            Codec: h264 | Pixel Format: yuv420p
            Resolution: 1920 x 1080 | FPS: 30
            """,
            fileResult.Replace("\r\n", "\n"),
            "verify_file exact text");
        AssertEqual(
            """
            Snapshot assertions: FAIL
            Message: 1 assertion failed
            Assertions: 1
            Passed: false
            Failures: IsRecording expected false
            """,
            assertResult.Replace("\r\n", "\n"),
            "assert_snapshot exact text");
        AssertEqual("no verification data", missingRecordingResult, "verify_recording missing verification fallback");
        AssertEqual("file not found", missingFileResult, "verify_file missing verification fallback");

        var verificationRootText = ReadRepoFile("tools/McpServer/Tools/AutomationControlTools.cs")
            .Replace("\r\n", "\n");

        AssertContains(verificationRootText, "[McpServerToolType]");
        AssertContains(verificationRootText, "public static class VerificationTools");
        AssertDoesNotContain(verificationRootText, "public static partial class VerificationTools");
        AssertContains(verificationRootText, "public static async Task<CallToolResult> verify_recording");
        AssertContains(verificationRootText, "public static async Task<CallToolResult> assert_snapshot");
        AssertContains(verificationRootText, "public static async Task<CallToolResult> verify_file");
        AssertContains(verificationRootText, "SendCommandAsync(AutomationCommandKind.VerifyLastRecording)");
        AssertContains(verificationRootText, "SendCommandAsync(AutomationCommandKind.AssertSnapshot, payload)");
        AssertContains(verificationRootText, "SendCommandAsync(AutomationCommandKind.VerifyFile, payload)");
        AssertContains(verificationRootText, "TryParseAssertionArray(assertions, out var parsedAssertions, out var parseError)");
        AssertContains(verificationRootText, "BuildRecordingVerificationText(response, verification, message)");
        AssertContains(verificationRootText, "BuildSnapshotAssertionText(response)");
        AssertContains(verificationRootText, "BuildFileVerificationText(filePath, response, verification, message)");

        AssertContains(verificationRootText, "private static bool TryParseAssertionArray(");
        AssertContains(verificationRootText, "string.IsNullOrWhiteSpace(assertions)");
        AssertContains(verificationRootText, "JsonDocument.Parse(assertions)");
        AssertContains(verificationRootText, "RootElement.Clone()");
        AssertContains(verificationRootText, "Invalid assertions JSON: {ex.Message}");
        AssertContains(verificationRootText, "private static bool TryGetVerification(");
        AssertContains(verificationRootText, "response.TryGetProperty(\"Data\", out var data)");
        AssertContains(verificationRootText, "data.TryGetProperty(\"Verification\", out verification)");
        AssertContains(verificationRootText, "response.TryGetProperty(\"Snapshot\", out var snapshot)");
        AssertContains(verificationRootText, "snapshot.TryGetProperty(\"LastVerification\", out verification)");

        AssertContains(verificationRootText, "private static string BuildRecordingVerificationText(");
        AssertContains(verificationRootText, "== Recording Verification: PASS ==");
        AssertContains(verificationRootText, "FormatJsonArrayList(verification, \"Mismatches\", \"Mismatches\")");
        AssertContains(verificationRootText, "private static string BuildSnapshotAssertionText(");
        AssertContains(verificationRootText, "FormatJsonArrayList(failures, \"Failures\")");
        AssertContains(verificationRootText, "\"{label}: None\"");
        AssertContains(verificationRootText, "private static string BuildFileVerificationText(");
        AssertContains(verificationRootText, "== File Verification: PASS ==");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "McpServer", "Tools", "VerificationTools.Formatting.cs")),
            "MCP verification response formatting lives with the verification tool commands");

        AssertMcpCommandRoutingTestsUseCommandIdHelper();
    }

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
        var formatterRootText = ReadRepoFile("tools/Common/DiagnosticSessionResult.cs")
            .Replace("\r\n", "\n");
        var validationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSupport.cs")
            .Replace("\r\n", "\n");

        AssertContains(formatterRootText, "internal static class DiagnosticSessionOptionalTextFormatter");
        AssertContains(formatterRootText, "internal static string FormatOptional(string value)");
        AssertContains(formatterRootText, "string.IsNullOrWhiteSpace(value) ? \"none\" : value");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionOptionalTextFormatter.cs")), "Optional diagnostic text formatting stays folded into DiagnosticSessionResult.cs");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionResultFormatter.cs")), "Diagnostic session result text formatting stays folded into DiagnosticSessionResult.cs");
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
        var formatterRootText = ReadRepoFile("tools/Common/DiagnosticSessionResult.cs")
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
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionResultFormatter.cs")), "DiagnosticSessionResultFormatter lives with the diagnostic result model surface");
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


// MCP performance tool contracts live with the tool xUnit wrappers.
    internal static Task McpPerformanceTimelineTool_ExposesD3DP99StageTiming()
    {
        var sources = ReadMcpPerformanceTimelineSources();

        AssertMcpPerformanceTimelineSourceOwnership(sources);
        AssertMcpPerformanceTimelineRenderingContracts(sources);
        AssertMcpPerformanceTimelineProjectionContracts(sources);

        return Task.CompletedTask;
    }

    private static McpPerformanceTimelineSources ReadMcpPerformanceTimelineSources()
    {
        var renderingSource = ReadRepoFile("tools/McpServer/Tools/PerformanceTools.cs");

        return new McpPerformanceTimelineSources
        {
            RowsSource = renderingSource,
            RenderingSource = renderingSource,
            CombinedSource = renderingSource
        };
    }

    private sealed class McpPerformanceTimelineSources
    {
        public string RowsSource { get; init; } = string.Empty;
        public string RenderingSource { get; init; } = string.Empty;
        public string CombinedSource { get; init; } = string.Empty;
    }

    private static void AssertMcpPerformanceTimelineSourceOwnership(McpPerformanceTimelineSources sources)
    {
        AssertContains(sources.RowsSource, "PopulatePreviewTimelineRow(item, row);");
        AssertContains(sources.RowsSource, "PopulateFlashbackPlaybackTimelineRow(item, row);");
        AssertContains(sources.RowsSource, "PopulateFlashbackExportTimelineRow(item, row);");
        AssertContains(sources.RowsSource, "PopulateSystemTimelineRow(item, row);");
        AssertContains(sources.RowsSource, "private static void PopulatePreviewTimelineRow(JsonElement item, TimelineRow row)");
        AssertContains(sources.RowsSource, "private static void PopulateFlashbackPlaybackTimelineRow(JsonElement item, TimelineRow row)");
        AssertContains(sources.RowsSource, "private static void PopulateFlashbackExportTimelineRow(JsonElement item, TimelineRow row)");
        AssertContains(sources.RowsSource, "private static void PopulateSystemTimelineRow(JsonElement item, TimelineRow row)");
        AssertContains(sources.RowsSource, "MjpegPreviewJitterLatencyP95Ms");
        AssertContains(sources.RowsSource, "FlashbackPlaybackMaxCommandQueueLatencyCommand");
        AssertContains(sources.RowsSource, "FlashbackExportThroughputBytesPerSec");
        AssertContains(sources.RowsSource, "ThreadPoolIoAvailable");
        AssertOccursBefore(sources.RowsSource, "private static void PopulatePreviewTimelineRow", "private static void PopulateFlashbackPlaybackTimelineRow");
        AssertOccursBefore(sources.RowsSource, "private static void PopulateFlashbackPlaybackTimelineRow", "private static void PopulateFlashbackExportTimelineRow");
        AssertOccursBefore(sources.RowsSource, "private static void PopulateFlashbackExportTimelineRow", "private static void PopulateSystemTimelineRow");
        AssertContains(sources.RowsSource, "private sealed class TimelineRow");
        AssertContains(sources.RowsSource, "public double PreviewFivePercentLowFps { get; set; }");
        AssertContains(sources.RowsSource, "public string PreviewPacingSlowStageEvidence { get; set; } = string.Empty;");
        AssertContains(sources.RowsSource, "public string FlashbackPlaybackLastCommandFailure { get; set; } = string.Empty;");
        AssertContains(sources.RowsSource, "public double FlashbackExportThroughputBytesPerSec { get; set; }");
        AssertContains(sources.RowsSource, "public int IoThreads { get; set; }");
        AssertOccursBefore(sources.RowsSource, "private static void PopulateSystemTimelineRow", "private sealed class TimelineRow");
        AssertOccursBefore(sources.RowsSource, "private static List<TimelineRow> ReadTimelineRows", "private static string BuildPerformanceTimelineText");
        AssertOccursBefore(sources.RowsSource, "public double PreviewSlowPct { get; set; }", "public double VisualCadenceChangeObservedFps { get; set; }");
        AssertOccursBefore(sources.RowsSource, "public string PreviewPacingSlowStageEvidence { get; set; } = string.Empty;", "public string FlashbackPlaybackState { get; set; } = string.Empty;");
        AssertOccursBefore(sources.RowsSource, "public bool FlashbackForceRotateDraining { get; set; }", "public bool FlashbackExportActive { get; set; }");
        AssertOccursBefore(sources.RowsSource, "public string FlashbackExportMessage { get; set; } = string.Empty;", "public long LatencyMs { get; set; }");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "McpServer", "Tools", "PerformanceTimelineTools.Rows.cs")),
            "MCP performance timeline row projection lives with the timeline renderer owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "McpServer", "Tools", "PerformanceTimelineTools.Rendering.cs")),
            "MCP timeline rendering lives with the broader performance MCP tool owner");
        AssertContains(sources.RenderingSource, "public static async Task<CallToolResult> get_performance_timeline(");
        AssertContains(sources.RenderingSource, "var entries = ReadTimelineRows(data);");
        AssertContains(sources.RenderingSource, "McpToolResultFactory.FromResponse(response, BuildPerformanceTimelineText(entries, targetOnePercentLowFps))");
        AssertContains(sources.RenderingSource, "BuildPerformanceTimelineText");
        AssertContains(sources.RenderingSource, "AppendTrendSummary");
        AssertContains(sources.RenderingSource, "== Trend Summary");
        AssertContains(sources.RenderingSource, "FormatOptional");
        AssertContains(sources.RenderingSource, "CompactCell");
        AssertContains(sources.RenderingSource, "private static string FormatJitterDepthCell(TimelineRow row)");
        AssertContains(sources.RenderingSource, "private static string FormatD3DP99Bottleneck(TimelineRow row)");
        AssertContains(sources.RenderingSource, "private static string FormatFlashbackStageCell(TimelineRow row)");
        AssertContains(sources.RenderingSource, "private static string FormatExportFailureKind(string failureKind)");
        AssertContains(sources.RenderingSource, "private static string FormatBytesPerSecond(double bytesPerSecond)");
        AssertContains(sources.RenderingSource, "AppendPreviewTrendSummary(builder, first, last);");
        AssertContains(sources.RenderingSource, "AppendFlashbackTrendSummary(builder, first, last);");
        AssertContains(sources.RenderingSource, "private static void AppendPreviewTrendSummary(StringBuilder builder, TimelineRow first, TimelineRow last)");
        AssertContains(sources.RenderingSource, "Preview Slow Stage:");
        AssertContains(sources.RenderingSource, "D3D P99 Bottleneck:");
        AssertContains(sources.RenderingSource, "Jitter Drops:");
        AssertContains(sources.RenderingSource, "private static void AppendFlashbackTrendSummary(StringBuilder builder, TimelineRow first, TimelineRow last)");
        AssertContains(sources.RenderingSource, "Flashback Cmd Counters:");
        AssertContains(sources.RenderingSource, "AppendFlashbackExportTrendSummary(builder, first, last);");
        AssertContains(sources.RenderingSource, "private static void AppendFlashbackExportTrendSummary(StringBuilder builder, TimelineRow first, TimelineRow last)");
        AssertContains(sources.RenderingSource, "Export Output:");
        AssertOccursBefore(sources.RenderingSource, "Cleanup State:", "AppendFlashbackExportTrendSummary(builder, first, last);");
        AssertOccursBefore(sources.RenderingSource, "AppendFlashbackExportTrendSummary(builder, first, last);", "private static void AppendFlashbackExportTrendSummary");
        AssertContains(sources.RenderingSource, "AppendOnePercentLowTargetSummary");
        AssertContains(sources.RenderingSource, "private static void AppendPressureSummary(");
        AssertContains(sources.RenderingSource, "== Pressure Summary ==");
        AssertContains(sources.RenderingSource, "private static int CountOverBudget(");
    }

    private static void AssertMcpPerformanceTimelineRenderingContracts(McpPerformanceTimelineSources sources)
    {
        AssertContains(sources.CombinedSource, "PreviewD3DInputUploadCpuP99Ms");
        AssertContains(sources.CombinedSource, "targetOnePercentLowFps");
        AssertContains(sources.CombinedSource, "== 1% Low Target Summary");
        AssertContains(sources.CombinedSource, "AppendOnePercentLowTargetSummary");
        AssertContains(sources.CombinedSource, "AppendPressureSummary");
        AssertContains(sources.CombinedSource, "misses={belowTarget}/{valid.Length}");
        AssertContains(sources.CombinedSource, "Preview 5% Low:");
        AssertContains(sources.CombinedSource, "Visual Cadence:");
        AssertContains(sources.CombinedSource, "MJPEG Fingerprint:");
        AssertContains(sources.CombinedSource, "Preview P99:");
        AssertContains(sources.CombinedSource, "InP99 | RsP99 | PrP99 | TotP99");
        AssertContains(sources.CombinedSource, "Flashback target:");
        AssertContains(sources.CombinedSource, "lastSubmitFailure");
        AssertContains(sources.CombinedSource, "Flashback Enqueue Rejects");
        AssertContains(sources.CombinedSource, "JitD  | JitLat | JitDrop | JitUF | JitWhy");
        AssertContains(sources.CombinedSource, "FbState | Fb1%  | FbP99 | FbDec | FbCmd | FbFail | FbStage");
        AssertContains(sources.CombinedSource, "FormatFlashbackStageCell");
        AssertContains(sources.CombinedSource, "Cln | ExStat");
        AssertContains(sources.CombinedSource, "ExStat  | ExKind | Ex%");
        AssertContains(sources.CombinedSource, "FormatJitterDepthCell");
        AssertContains(sources.CombinedSource, "FormatExportFailureKind");
        AssertContains(sources.CombinedSource, "Jitter Depth:");
        AssertContains(sources.CombinedSource, "Jitter Latency:");
        AssertContains(sources.CombinedSource, "Jitter Drops:");
        AssertContains(sources.CombinedSource, "D3D Input P99:");
        AssertContains(sources.CombinedSource, "D3D Render P99:");
        AssertContains(sources.CombinedSource, "D3D Present P99:");
        AssertContains(sources.CombinedSource, "D3D Total P99:");
        AssertContains(sources.CombinedSource, "D3D P99 Bottleneck:");
        AssertContains(sources.CombinedSource, "Preview Slow Stage:");
        AssertContains(sources.CombinedSource, "FormatD3DP99Bottleneck");
        AssertContains(sources.CombinedSource, "== Pressure Summary ==");
        AssertContains(sources.CombinedSource, "Preview Pressure:");
        AssertContains(sources.CombinedSource, "overBudgetSamples input=");
        AssertContains(sources.CombinedSource, "dxgiMissedSamples=");
        AssertContains(sources.CombinedSource, "jitterDropsDelta=");
        AssertContains(sources.CombinedSource, "Flashback Pressure:");
        AssertContains(sources.CombinedSource, "decodeOverBudget=");
        AssertContains(sources.CombinedSource, "pendingCmdSamples=");
        AssertContains(sources.CombinedSource, "System Pressure:");
        AssertContains(sources.CombinedSource, "gcPauseSamples=");
        AssertContains(sources.CombinedSource, "CountOverBudget");
        AssertContains(sources.CombinedSource, "NonNegativeDelta");
        AssertContains(sources.CombinedSource, "Flashback P99:");
        AssertContains(sources.CombinedSource, "Flashback Decode:");
        AssertContains(sources.CombinedSource, "phase={FormatOptional(last.FlashbackPlaybackMaxDecodePhase)}");
        AssertContains(sources.CombinedSource, "send={last.FlashbackPlaybackMaxDecodeSendMs:F1}ms");
        AssertContains(sources.CombinedSource, "audio={last.FlashbackPlaybackMaxDecodeAudioMs:F1}ms");
        AssertContains(sources.CombinedSource, "Flashback Cmds:");
        AssertContains(sources.CombinedSource, "maxLatencyCommand={FormatOptional(last.FlashbackPlaybackMaxCommandQueueLatencyCommand)}");
        AssertContains(sources.CombinedSource, "Flashback Cmd Counters:");
        AssertContains(sources.CombinedSource, "lastQueued={FormatOptional(last.FlashbackPlaybackLastCommandQueued)}");
        AssertContains(sources.CombinedSource, "lastProcessed={FormatOptional(last.FlashbackPlaybackLastCommandProcessed)}");
        AssertContains(sources.CombinedSource, "Flashback Failure:");
        AssertContains(sources.CombinedSource, "Flashback Stages:");
        AssertContains(sources.CombinedSource, "failureUtc latest={last.FlashbackPlaybackLastCommandFailureUtcUnixMs}");
        AssertContains(sources.CombinedSource, "Cleanup State:");
        AssertContains(sources.CombinedSource, "forceRotateRequested={last.FlashbackForceRotateRequested}");
        AssertContains(sources.CombinedSource, "forceRotateDraining={last.FlashbackForceRotateDraining}");
        AssertContains(sources.CombinedSource, "kind={FormatOptional(last.FlashbackExportFailureKind)}");
        AssertContains(sources.CombinedSource, "Export Message:");
        AssertContains(sources.CombinedSource, "Export Progress:");
        AssertContains(sources.CombinedSource, "Export Range:");
        AssertContains(sources.CombinedSource, "FormatExportOutPoint");
        AssertContains(sources.CombinedSource, "Export Output:");
    }

    private static void AssertMcpPerformanceTimelineProjectionContracts(McpPerformanceTimelineSources sources)
    {
        var diagnosticsHubSource = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs");
        var entryType = RequireType("Sussudio.Models.PerformanceTimelineEntry");

        AssertContains(sources.CombinedSource, "PreviewP99Ms = AutomationSnapshotFormatter.GetDouble(item, \"PreviewCadenceP99Ms\")");
        AssertContains(sources.CombinedSource, "PreviewFivePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, \"PreviewCadenceFivePercentLowFps\")");
        AssertContains(sources.CombinedSource, "VisualCadenceChangeObservedFps = AutomationSnapshotFormatter.GetDouble(item, \"VisualCadenceChangeObservedFps\")");
        AssertContains(sources.CombinedSource, "MjpegPacketHashUniqueObservedFps = AutomationSnapshotFormatter.GetDouble(item, \"MjpegPacketHashUniqueObservedFps\")");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackFivePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, \"FlashbackPlaybackFivePercentLowFps\")");
        AssertContains(sources.CombinedSource, "PreviewD3DRenderSubmitCpuP99Ms");
        AssertContains(sources.CombinedSource, "PreviewD3DPresentCallP99Ms");
        AssertContains(sources.CombinedSource, "PreviewD3DTotalFrameCpuP99Ms");
        AssertContains(sources.CombinedSource, "PreviewD3DFrameLatencyWaitTimeoutCount");
        AssertContains(sources.CombinedSource, "PreviewD3DFrameLatencyWaitP95Ms");
        AssertContains(sources.CombinedSource, "PreviewD3DFrameLatencyWaitMaxMs");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackP99FrameMs");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackTargetFps");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackDecodeP99Ms");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackPendingCommands");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackCommandsEnqueued");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackCommandsProcessed");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackCommandsDropped");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackCommandsSkippedNotReady");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackScrubUpdatesCoalesced");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackSeekCommandsCoalesced");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackMaxCommandQueueLatencyCommand");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackLastCommandQueued");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackLastCommandProcessed");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackSubmitFailures");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackLastDropUtcUnixMs");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackLastDropReason");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackLastSubmitFailureUtcUnixMs");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackLastSubmitFailure");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackSegmentSwitches");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackFmp4Reopens");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackWriteHeadWaits");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackNearLiveSnaps");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackLastCommandFailureUtcUnixMs");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackLastWriteHeadWaitGapMs");
        AssertContains(sources.CombinedSource, "FlashbackPlaybackLastCommandFailure");
        AssertContains(sources.CombinedSource, "FlashbackVideoQueueRejectedFrames");
        AssertContains(sources.CombinedSource, "FlashbackVideoQueueLastRejectReason");
        AssertContains(sources.CombinedSource, "FlashbackGpuQueueRejectedFrames");
        AssertContains(sources.CombinedSource, "FlashbackGpuQueueLastRejectReason");
        AssertContains(sources.CombinedSource, "FatalCleanupInProgress");
        AssertContains(sources.CombinedSource, "FlashbackCleanupInProgress");
        AssertContains(sources.CombinedSource, "FlashbackForceRotateRequested");
        AssertContains(sources.CombinedSource, "FlashbackForceRotateDraining");
        AssertContains(sources.CombinedSource, "FlashbackExportFailureKind");
        AssertContains(sources.CombinedSource, "FlashbackExportPercent");
        AssertContains(sources.CombinedSource, "FlashbackExportInPointMs");
        AssertContains(sources.CombinedSource, "FlashbackExportOutPointMs");
        AssertContains(sources.CombinedSource, "FlashbackExportMessage");
        AssertContains(sources.CombinedSource, "FlashbackExportThroughputBytesPerSec");
        AssertContains(sources.CombinedSource, "FlashbackExportLastProgressAgeMs");
        AssertContains(sources.CombinedSource, "MjpegPreviewJitterLatencyP95Ms");
        AssertContains(sources.CombinedSource, "MjpegPreviewJitterDeadlineDropCount");
        AssertContains(sources.CombinedSource, "MjpegPreviewJitterClearedDropCount");
        AssertContains(sources.CombinedSource, "MjpegPreviewJitterResumeReprimeCount");
        AssertContains(sources.CombinedSource, "MjpegPreviewJitterLastDropReason");
        AssertContains(sources.CombinedSource, "PreviewPacingLikelySlowStage");

        foreach (var propertyName in new[]
                 {
                     "CaptureCadenceFivePercentLowFps",
                     "PreviewCadenceFivePercentLowFps",
                     "VisualCadenceChangeObservedFps",
                     "VisualCadenceRepeatFramePercent",
                     "VisualCadenceMotionConfidence",
                     "MjpegPacketHashInputObservedFps",
                     "MjpegPacketHashUniqueObservedFps",
                     "MjpegPacketHashDuplicateFramePercent",
                     "PreviewPacingLikelySlowStage",
                     "PreviewPacingSlowStageConfidence",
                     "PreviewPacingSlowStageEvidence",
                     "FlashbackPlaybackFivePercentLowFps",
                     "FlashbackPlaybackCommandsEnqueued",
                     "FlashbackPlaybackCommandsProcessed",
                     "FlashbackPlaybackCommandsDropped",
                     "FlashbackPlaybackCommandsSkippedNotReady",
                     "FlashbackPlaybackScrubUpdatesCoalesced",
                     "FlashbackPlaybackSeekCommandsCoalesced",
                     "FlashbackPlaybackLastCommandQueued",
                     "FlashbackPlaybackLastCommandProcessed",
                     "FlashbackExportPercent",
                     "FlashbackExportThroughputBytesPerSec",
                     "ProcessCpuPercent",
                     "ThreadPoolWorkerAvailable"
                 })
        {
            AssertNotNull(entryType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance), $"PerformanceTimelineEntry.{propertyName}");
            if (propertyName.StartsWith("FlashbackPlayback", StringComparison.Ordinal))
            {
                var projectionName = propertyName["FlashbackPlayback".Length..];
                AssertContains(diagnosticsHubSource, $"{propertyName} = flashbackPlayback.{projectionName}");
                AssertContains(diagnosticsHubSource, $"{projectionName}: snapshot.{propertyName}");
            }
            else if (propertyName.StartsWith("FlashbackExport", StringComparison.Ordinal))
            {
                var projectionName = propertyName["FlashbackExport".Length..];
                AssertContains(diagnosticsHubSource, $"{propertyName} = flashbackExport.{projectionName}");
                AssertContains(diagnosticsHubSource, $"{projectionName}: snapshot.{propertyName}");
            }
            else if (propertyName is "ProcessCpuPercent" or "ThreadPoolWorkerAvailable")
            {
                AssertContains(diagnosticsHubSource, $"{propertyName} = system.{propertyName}");
                AssertContains(diagnosticsHubSource, $"{propertyName}: snapshot.{propertyName}");
            }
            else if (propertyName.StartsWith("PreviewCadence", StringComparison.Ordinal))
            {
                var projectionName = propertyName["Preview".Length..];
                AssertContains(diagnosticsHubSource, $"{propertyName} = preview.{projectionName}");
                AssertContains(diagnosticsHubSource, $"{projectionName}: snapshot.{propertyName.Replace("Ms", "IntervalMs", StringComparison.Ordinal)}");
            }
            else if (propertyName.StartsWith("CaptureCadence", StringComparison.Ordinal))
            {
                AssertContains(diagnosticsHubSource, $"{propertyName} = core.{propertyName}");
                AssertContains(diagnosticsHubSource, $"{propertyName}: snapshot.{propertyName}");
            }
            else if (propertyName.StartsWith("PreviewPacing", StringComparison.Ordinal))
            {
                var projectionName = propertyName["Preview".Length..];
                AssertContains(diagnosticsHubSource, $"{propertyName} = preview.{projectionName}");
                AssertContains(diagnosticsHubSource, $"{projectionName}: snapshot.{propertyName}");
            }
            else if (propertyName.StartsWith("VisualCadence", StringComparison.Ordinal) ||
                     propertyName.StartsWith("MjpegPacketHash", StringComparison.Ordinal))
            {
                AssertContains(diagnosticsHubSource, $"{propertyName} = preview.{propertyName}");
                AssertContains(diagnosticsHubSource, $"{propertyName}: snapshot.{propertyName}");
            }
            else
            {
                AssertContains(diagnosticsHubSource, $"{propertyName} = snapshot.{propertyName}");
            }
        }
    }

    internal static async Task McpPerformanceTimelineTool_RendersFlashbackCommandCounters()
    {
        var pipeName = NewMcpToolPipeName("timeline-counters");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var timelineTools = RequireMcpType("McpServer.Tools.PerformanceTimelineTools");

        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 1,
                async () =>
                {
                    var output = await InvokeMcpToolStringAsync(
                            timelineTools,
                            "get_performance_timeline",
                            pipeClient,
                            2,
                            118d)
                        .ConfigureAwait(false);

                    AssertContains(output, "Flashback Cmd Counters: enqueued 1 -> 9, processed 0 -> 8, dropped 0 -> 2, skippedNotReady 0 -> 1, scrubCoalesced 0 -> 4, seekCoalesced 0 -> 3, lastQueued=Seek, lastProcessed=Pause");
                    AssertContains(output, "cmdDropsDelta=2");
                    AssertContains(output, "Preview Slow Stage: Unknown/None -> CompositorMiss/High evidence=dxgiRecentMissed=4");
                },
                _ => """
                     {
                       "Success": true,
                       "Data": [
                         {
                           "TimestampUtc": "2026-05-04T12:00:00Z",
                           "PreviewPacingLikelySlowStage": "Unknown",
                           "PreviewPacingSlowStageConfidence": "None",
                           "PreviewPacingSlowStageEvidence": "",
                           "FlashbackPlaybackCommandsEnqueued": 1,
                           "FlashbackPlaybackCommandsProcessed": 0,
                           "FlashbackPlaybackCommandsDropped": 0,
                           "FlashbackPlaybackCommandsSkippedNotReady": 0,
                           "FlashbackPlaybackScrubUpdatesCoalesced": 0,
                           "FlashbackPlaybackSeekCommandsCoalesced": 0,
                           "FlashbackPlaybackLastCommandQueued": "Play",
                           "FlashbackPlaybackLastCommandProcessed": "None"
                         },
                         {
                           "TimestampUtc": "2026-05-04T12:00:01Z",
                           "PreviewPacingLikelySlowStage": "CompositorMiss",
                           "PreviewPacingSlowStageConfidence": "High",
                           "PreviewPacingSlowStageEvidence": "dxgiRecentMissed=4",
                           "FlashbackPlaybackPendingCommands": 2,
                           "FlashbackPlaybackCommandsEnqueued": 9,
                           "FlashbackPlaybackCommandsProcessed": 8,
                           "FlashbackPlaybackCommandsDropped": 2,
                           "FlashbackPlaybackCommandsSkippedNotReady": 1,
                           "FlashbackPlaybackScrubUpdatesCoalesced": 4,
                           "FlashbackPlaybackSeekCommandsCoalesced": 3,
                           "FlashbackPlaybackLastCommandQueued": "Seek",
                           "FlashbackPlaybackLastCommandProcessed": "Pause"
                         }
                       ]
                     }
                     """)
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "GetPerformanceTimeline", ("maxEntries", 2));
    }


    internal static async Task McpPresentMonTools_RouteSnapshotCorrelation()
    {
        var presentMonTools = RequireMcpType("McpServer.Tools.PresentMonTools");

        var pipeName = NewMcpToolPipeName("presentmon-text");
        var pipeClient = CreateMcpPipeClient(pipeName);
        string textResult = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 1,
                async () =>
                {
                    textResult = await InvokeMcpToolStringAsync(
                            presentMonTools,
                            "capture_presentmon",
                            pipeClient,
                            5,
                            -1,
                            "NoSuchSussudioProcess",
                            null,
                            null,
                            null,
                            null,
                            null,
                            null,
                            false,
                            true)
                        .ConfigureAwait(false);
                },
                _ => PresentMonSnapshotJson("0xABCDEF", 42, 0, 1700000000000))
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "GetSnapshot");
        AssertEqual("No running process matched pid=-1 name='NoSuchSussudioProcess'.", textResult, "capture_presentmon no-process text");

        var rawPipeName = NewMcpToolPipeName("presentmon-raw");
        var rawPipeClient = CreateMcpPipeClient(rawPipeName);
        object? rawResult = null;
        var rawRequests = await CapturePipeRequestsAsync(
                rawPipeName,
                expectedCount: 1,
                async () =>
                {
                    rawResult = await InvokeMcpToolResultAsync(
                            presentMonTools,
                            "capture_presentmon_raw",
                            rawPipeClient,
                            15,
                            -1,
                            "AnotherMissingProcess",
                            "0xEXPLICIT",
                            99L,
                            1001L,
                            1700000000999L,
                            @"C:\tools\missing-presentmon.exe",
                            @"C:\captures\presentmon.csv",
                            true,
                            false)
                        .ConfigureAwait(false);
                },
                _ => PresentMonSnapshotJson("0xSHOULD_NOT_WIN", 12, 34, 1700000000123))
            .ConfigureAwait(false);

        AssertCommandRequest(rawRequests[0], "GetSnapshot");
        AssertEqual(false, GetBoolProperty(rawResult!, "Success"), "capture_presentmon_raw missing process success");
        AssertEqual("No running process matched pid=-1 name='AnotherMissingProcess'.", GetStringProperty(rawResult!, "Message"), "capture_presentmon_raw no-process message");

        AssertPresentMonOptionsFallbackAndPrecedence();

        var rootText = ReadRepoFile("tools/McpServer/Tools/PerformanceTools.cs")
            .Replace("\r\n", "\n");
        var probeText = ReadRepoFile("tools/Common/PresentMon/PresentMonProbe.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "[McpServerToolType]");
        AssertContains(rootText, "public static class PresentMonTools");
        AssertDoesNotContain(rootText, "public static partial class PresentMonTools");
        AssertContains(rootText, "public static async Task<CallToolResult> capture_presentmon");
        AssertContains(rootText, "public static async Task<object> capture_presentmon_raw");
        AssertContains(rootText, "[McpServerTool(UseStructuredContent = true)");
        AssertContains(rootText, "PresentMonProbe.Format(result)");
        AssertContains(rootText, "PresentMonProbe.RunAsync(PresentMonProbe.CreateOptions(");
        AssertContains(rootText, "correlation: resolved");
        AssertContains(rootText, "private static async Task<PresentMonProbeCorrelation> TryResolvePreviewPresentCorrelationAsync(");
        AssertContains(rootText, "SendCommandAsync(AutomationCommandKind.GetSnapshot)");
        AssertContains(rootText, "return PresentMonProbe.ReadPreviewCorrelation(snapshot);");
        AssertContains(rootText, "catch (JsonException ex)");
        AssertContains(rootText, "catch (IOException ex)");
        AssertDoesNotContain(rootText, "new PresentMonProbeOptions");
        AssertDoesNotContain(rootText, "ExpectedSwapChainAddress =");
        AssertDoesNotContain(rootText, "AppPresentId = appPresentId");
        AssertDoesNotContain(rootText, "SendCommandAsync(\"GetSnapshot\")");
        AssertDoesNotContain(rootText, "GetPositiveLong(");
        AssertDoesNotContain(rootText, "private readonly record struct PresentMonCorrelation(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "McpServer", "Tools", "PresentMonTools.Correlation.cs")),
            "PresentMon snapshot correlation lives with the PresentMon MCP tool");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "McpServer", "Tools", "PresentMonTools.cs")),
            "PresentMon MCP entry points live with the broader performance MCP tool owner");

        AssertContains(probeText, "public readonly record struct PresentMonProbeCorrelation(");
        AssertContains(probeText, "public static PresentMonProbeOptions CreateOptions(");
        AssertContains(probeText, "public static PresentMonProbeCorrelation ReadPreviewCorrelation(JsonElement snapshot)");
    }

    private static void AssertPresentMonOptionsFallbackAndPrecedence()
    {
        var presentMonProbe = RequireMcpType("Sussudio.Tools.PresentMonProbe");
        var createOptions = presentMonProbe.GetMethod("CreateOptions", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("PresentMonProbe.CreateOptions was not found.");
        var correlationType = RequireMcpType("Sussudio.Tools.PresentMonProbeCorrelation");
        var resolved = Activator.CreateInstance(
                correlationType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object?[] { "0xSNAPSHOT", 42L, 0L, 1700000000000L },
                culture: null)
            ?? throw new InvalidOperationException("PresentMonCorrelation could not be created.");

        var fallbackOptions = createOptions.Invoke(null, new object?[]
        {
            5,
            123,
            "Sussudio",
            null,
            null,
            null,
            null,
            null,
            @"C:\tools\PresentMon.exe",
            @"C:\captures\presentmon.csv",
            true,
            false,
            resolved
        }) ?? throw new InvalidOperationException("CreatePresentMonProbeOptions returned null.");

        AssertEqual(5, GetIntProperty(fallbackOptions, "DurationSeconds"), "PresentMon fallback DurationSeconds");
        AssertEqual(123, GetIntProperty(fallbackOptions, "ProcessId"), "PresentMon fallback ProcessId");
        AssertEqual("Sussudio", GetStringProperty(fallbackOptions, "ProcessName"), "PresentMon fallback ProcessName");
        AssertEqual("0xSNAPSHOT", GetStringProperty(fallbackOptions, "ExpectedSwapChainAddress"), "PresentMon fallback swap chain");
        AssertEqual(42L, GetLongProperty(fallbackOptions, "AppPresentId"), "PresentMon fallback present id");
        AssertEqual(0L, GetLongProperty(fallbackOptions, "AppSourceSequenceNumber"), "PresentMon fallback source sequence");
        AssertEqual(1700000000000L, GetLongProperty(fallbackOptions, "AppPresentUtcUnixMs"), "PresentMon fallback present UTC");
        AssertEqual(@"C:\tools\PresentMon.exe", GetStringProperty(fallbackOptions, "PresentMonPath"), "PresentMon fallback path");
        AssertEqual(@"C:\captures\presentmon.csv", GetStringProperty(fallbackOptions, "OutputFile"), "PresentMon fallback output");
        AssertEqual(true, GetBoolProperty(fallbackOptions, "KeepCsv"), "PresentMon fallback keep CSV");
        AssertEqual(false, GetBoolProperty(fallbackOptions, "TrackGpuVideo"), "PresentMon fallback track GPU video");

        var explicitOptions = createOptions.Invoke(null, new object?[]
        {
            15,
            -1,
            "OtherProcess",
            "0xEXPLICIT",
            99L,
            1001L,
            1700000000999L,
            null,
            null,
            null,
            false,
            true,
            resolved
        }) ?? throw new InvalidOperationException("CreatePresentMonProbeOptions returned null for explicit args.");

        AssertEqual(15, GetIntProperty(explicitOptions, "DurationSeconds"), "PresentMon explicit DurationSeconds");
        AssertEqual(-1, GetIntProperty(explicitOptions, "ProcessId"), "PresentMon explicit ProcessId");
        AssertEqual("OtherProcess", GetStringProperty(explicitOptions, "ProcessName"), "PresentMon explicit ProcessName");
        AssertEqual("0xEXPLICIT", GetStringProperty(explicitOptions, "ExpectedSwapChainAddress"), "PresentMon explicit swap chain");
        AssertEqual(99L, GetLongProperty(explicitOptions, "AppPresentId"), "PresentMon explicit present id");
        AssertEqual(1001L, GetLongProperty(explicitOptions, "AppSourceSequenceNumber"), "PresentMon explicit source sequence");
        AssertEqual(1700000000999L, GetLongProperty(explicitOptions, "AppPresentUtcUnixMs"), "PresentMon explicit present UTC");
        AssertEqual(string.Empty, GetStringProperty(explicitOptions, "PresentMonPath"), "PresentMon explicit null path");
        AssertEqual(string.Empty, GetStringProperty(explicitOptions, "OutputFile"), "PresentMon explicit null output");
        AssertEqual(false, GetBoolProperty(explicitOptions, "KeepCsv"), "PresentMon explicit keep CSV");
        AssertEqual(true, GetBoolProperty(explicitOptions, "TrackGpuVideo"), "PresentMon explicit track GPU video");
    }

    private static string PresentMonSnapshotJson(
        string swapChainAddress,
        long presentId,
        long sourceSequenceNumber,
        long presentUtcUnixMs)
    {
        return $$"""
                 {
                   "Success": true,
                   "Snapshot": {
                     "PreviewD3DSwapChainAddress": "{{swapChainAddress}}",
                     "PreviewD3DLastRenderedPreviewPresentId": {{presentId}},
                     "PreviewD3DLastRenderedSourceSequenceNumber": {{sourceSequenceNumber}},
                     "PreviewD3DLastRenderedUtcUnixMs": {{presentUtcUnixMs}}
                   }
                 }
                 """;
    }

    internal static Task McpFramePacingVerdictTool_SourceOwnershipIsSplit()
    {
        var rootSource = ReadRepoFile("tools/McpServer/Tools/FramePacingVerdictTools.cs");

        AssertContains(rootSource, "[McpServerToolType]");
        AssertContains(rootSource, "[McpServerTool, Description(\"Get a compact frame pacing verdict");
        AssertContains(rootSource, "public static async Task<CallToolResult> get_frame_pacing_verdict");
        AssertContains(rootSource, "SendCommandAsync(AutomationCommandKind.GetSnapshot)");
        AssertContains(rootSource, "SendCommandAsync(AutomationCommandKind.GetPerformanceTimeline, timelinePayload)");
        AssertContains(rootSource, "BuildFramePacingVerdictText(");
        AssertContains(rootSource, "private static IReadOnlyList<TimelineRow> ReadTimeline");
        AssertContains(rootSource, "private sealed record TimelineRow");
        AssertContains(rootSource, "PreviewD3DFrameStatsRecentMissedRefreshCount");
        AssertContains(rootSource, "private static FramePacingChannel ReadChannel(");
        AssertContains(rootSource, "private sealed record FramePacingChannel");
        AssertContains(rootSource, "private static double[] GetDoubleArray");
        AssertContains(rootSource, "private static double ResolveTargetFps");
        AssertContains(rootSource, "private static bool IsSampleReady");
        AssertContains(rootSource, "private static bool IsHalfRate");
        AssertContains(rootSource, "private static bool HasHalfRateIntervals");
        AssertContains(rootSource, "private static bool IsHiddenStutter");
        AssertContains(rootSource, "private static string ResolveVerdict");
        AssertContains(rootSource, "private static double Ratio");
        AssertContains(rootSource, "private static string BuildFramePacingVerdictText(");
        AssertContains(rootSource, "new StringBuilder()");
        AssertContains(rootSource, "Verdict: {verdict}");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "McpServer", "Tools", "FramePacingVerdictTools.Timeline.cs")),
            "Frame pacing timeline reader lives with the MCP tool orchestration");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "McpServer", "Tools", "FramePacingVerdictTools.Channels.cs")),
            "Frame pacing channel projection lives with the MCP verdict tool");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "McpServer", "Tools", "FramePacingVerdictTools.Policy.cs")),
            "Frame pacing readiness and verdict policy lives with the MCP verdict tool");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "McpServer", "Tools", "FramePacingVerdictTools.Rendering.cs")),
            "Frame pacing verdict rendering lives with the MCP verdict tool");

        return Task.CompletedTask;
    }

    internal static async Task McpFramePacingVerdictTool_FlagsHalfRatePreviewAndPlayback()
    {
        var pipeName = NewMcpToolPipeName("frame-pacing");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var verdictTools = RequireMcpType("McpServer.Tools.FramePacingVerdictTools");

        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 2,
                async () =>
                {
                    var output = await InvokeMcpToolStringAsync(
                            verdictTools,
                            "get_frame_pacing_verdict",
                            pipeClient,
                            240,
                            30d,
                            120d)
                        .ConfigureAwait(false);

                    AssertContains(output, "Verdict: HalfRatePreviewAndPlaybackSuspected");
                    AssertContains(output, "SampleQuality: Ready");
                    AssertContains(output, "SourceToPreviewRatio: 0.5");
                    AssertContains(output, "SourceToPlaybackRatio: 0.5");
                    AssertContains(output, "HalfRatePreviewSuspected: true");
                    AssertContains(output, "HalfRatePlaybackSuspected: true");
                    AssertContains(output, "VisualChangeFps: 60");
                    AssertContains(output, "MjpegUniqueFps: 60");
                    AssertContains(output, "PreviewDropDelta: 4");
                    AssertContains(output, "PlaybackDropDelta: 2");
                    AssertContains(output, "PreviewPacingLikelySlowStage: VisualDuplicateOrLowMotion");
                    AssertContains(output, "PreviewPacingSlowStageConfidence: Medium");
                    AssertContains(output, "PreviewPacingSlowStageEvidence: synthetic duplicate cadence");
                    var expected = """
                                   Verdict: HalfRatePreviewAndPlaybackSuspected
                                   SampleQuality: Ready
                                   TargetFps: 120
                                   TargetFrameMs: 8.333
                                   MinSampleSeconds: 30
                                   Capture: observed=120 5pct=120 1pct=119 samples=3600 durationMs=30000 ready=true
                                   Preview: observed=60 5pct=60 1pct=58 samples=1800 durationMs=30000 ready=true
                                   Playback: observed=60 5pct=60 1pct=58 samples=1800 durationMs=30000 ready=true
                                   SourceToPreviewRatio: 0.5
                                   SourceToPlaybackRatio: 0.5
                                   HalfRatePreviewSuspected: true
                                   HalfRatePlaybackSuspected: true
                                   HiddenStutterSuspected: false
                                   VisualChangeFps: 60
                                   VisualRepeatPercent: 50
                                   VisualMotionConfidence: High
                                   MjpegInputFps: 120
                                   MjpegUniqueFps: 60
                                   MjpegDuplicatePercent: 50
                                   PreviewPacingLikelySlowStage: VisualDuplicateOrLowMotion
                                   PreviewPacingSlowStageConfidence: Medium
                                   PreviewPacingSlowStageEvidence: synthetic duplicate cadence
                                   TimelineSamples: 2
                                   DxgiMissedRefreshRecentMax: 4
                                   PreviewDropDelta: 4
                                   PlaybackDropDelta: 2
                                   Evidence: captureReady=true previewReady=true playbackReady=true previewHalfRate=true playbackHalfRate=true
                                   """;
                    AssertEqual(NormalizeLineEndings(expected), NormalizeLineEndings(output), "frame pacing verdict text");
                },
                i => i == 0
                    ? """
                      {
                        "Success": true,
                        "Snapshot": {
                          "ExpectedCaptureFrameRate": 120,
                          "CaptureCadenceObservedFps": 120,
                          "CaptureCadenceFivePercentLowFps": 120,
                          "CaptureCadenceOnePercentLowFps": 119,
                          "CaptureCadenceSampleCount": 3600,
                          "CaptureCadenceSampleDurationMs": 30000,
                          "PreviewCadenceObservedFps": 60,
                          "PreviewCadenceFivePercentLowFps": 60,
                          "PreviewCadenceOnePercentLowFps": 58,
                          "PreviewCadenceSampleCount": 1800,
                          "PreviewCadenceSampleDurationMs": 30000,
                          "PreviewCadenceRecentIntervalsMs": [16.67, 16.67, 16.67, 16.67, 16.67, 16.67],
                          "FlashbackPlaybackTargetFps": 120,
                          "FlashbackPlaybackObservedFps": 60,
                          "FlashbackPlaybackFivePercentLowFps": 60,
                          "FlashbackPlaybackOnePercentLowFps": 58,
                          "FlashbackPlaybackCadenceSampleCount": 1800,
                          "FlashbackPlaybackSampleDurationMs": 30000,
                          "FlashbackPlaybackRecentFrameIntervalsMs": [16.67, 16.67, 16.67, 16.67, 16.67, 16.67],
                          "VisualCadenceChangeObservedFps": 60,
                          "VisualCadenceRepeatFramePercent": 50,
                          "VisualCadenceMotionConfidence": "High",
                          "MjpegPacketHashInputObservedFps": 120,
                          "MjpegPacketHashUniqueObservedFps": 60,
                          "MjpegPacketHashDuplicateFramePercent": 50,
                          "PreviewPacingLikelySlowStage": "VisualDuplicateOrLowMotion",
                          "PreviewPacingSlowStageConfidence": "Medium",
                          "PreviewPacingSlowStageEvidence": "synthetic duplicate cadence"
                        }
                      }
                      """
                    : """
                      {
                        "Success": true,
                        "Data": [
                          {
                            "PreviewD3DFrameStatsRecentMissedRefreshCount": 2,
                            "MjpegPreviewJitterTotalDropped": 1,
                            "FlashbackPlaybackDroppedFrames": 0
                          },
                          {
                            "PreviewD3DFrameStatsRecentMissedRefreshCount": 4,
                            "MjpegPreviewJitterTotalDropped": 5,
                            "FlashbackPlaybackDroppedFrames": 2
                          }
                        ]
                      }
                      """)
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "GetSnapshot");
        AssertCommandRequest(requests[1], "GetPerformanceTimeline", ("maxEntries", 240));
    }

    internal static async Task McpFramePacingVerdictTool_FlagsInsufficientSampleDuration()
    {
        var pipeName = NewMcpToolPipeName("frame-pacing-short");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var verdictTools = RequireMcpType("McpServer.Tools.FramePacingVerdictTools");

        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 2,
                async () =>
                {
                    var output = await InvokeMcpToolStringAsync(
                            verdictTools,
                            "get_frame_pacing_verdict",
                            pipeClient,
                            240,
                            30d,
                            120d)
                        .ConfigureAwait(false);

                    AssertContains(output, "Verdict: InsufficientSample");
                    AssertContains(output, "SampleQuality: Insufficient");
                    AssertContains(output, "ready=false");
                },
                i => i == 0
                    ? """
                      {
                        "Success": true,
                        "Snapshot": {
                          "ExpectedCaptureFrameRate": 120,
                          "CaptureCadenceObservedFps": 120,
                          "CaptureCadenceFivePercentLowFps": 120,
                          "CaptureCadenceOnePercentLowFps": 119,
                          "CaptureCadenceSampleCount": 240,
                          "CaptureCadenceSampleDurationMs": 2000,
                          "PreviewCadenceObservedFps": 120,
                          "PreviewCadenceFivePercentLowFps": 120,
                          "PreviewCadenceOnePercentLowFps": 119,
                          "PreviewCadenceSampleCount": 240,
                          "PreviewCadenceSampleDurationMs": 2000
                        }
                      }
                      """
                    : """
                      {
                        "Success": true,
                        "Data": []
                      }
                      """)
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "GetSnapshot");
        AssertCommandRequest(requests[1], "GetPerformanceTimeline", ("maxEntries", 240));
    }


// MCP window and preview tool contracts live with the tool xUnit wrappers.
    internal static async Task McpPreviewTools_RoutePreviewToggle()
    {
        var pipeName = NewMcpToolPipeName("preview");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var previewTools = RequireMcpType("McpServer.Tools.PreviewTools");

        string result = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 1,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            previewTools,
                            "control_preview",
                            pipeClient,
                            true)
                        .ConfigureAwait(false);
                },
                _ => "{\"Success\":true,\"Message\":\"preview started\"}")
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "SetPreviewEnabled", ("enabled", true));
        AssertEqual("[OK] SetPreviewEnabled: preview started", result, "control_preview formatted success");
    }

    internal static async Task McpWindowScreenshotTool_FormatsScreenshotResponses()
    {
        var screenshotTools = RequireMcpType("McpServer.Tools.WindowScreenshotTools");

        var failureText = await InvokeWindowScreenshotAsync(
                screenshotTools,
                @"C:\captures\fail.png",
                "{\"Success\":false,\"Message\":\"window not available\"}")
            .ConfigureAwait(false);
        AssertEqual("window not available", failureText, "capture_window_screenshot failure message");

        var missingDataText = await InvokeWindowScreenshotAsync(
                screenshotTools,
                @"C:\captures\missing.png",
                "{\"Success\":true,\"Message\":\"ok\"}")
            .ConfigureAwait(false);
        AssertEqual("No screenshot data returned.", missingDataText, "capture_window_screenshot missing data");

        var successText = await InvokeWindowScreenshotAsync(
                screenshotTools,
                @"C:\captures\window.png",
                """
                {
                  "Success": true,
                  "Data": {
                    "FilePath": "C:\\captures\\actual-window.png",
                    "CapturedWidth": 1280,
                    "CapturedHeight": 720,
                    "FileSizeBytes": 4096
                  }
                }
                """)
            .ConfigureAwait(false);
        AssertEqual(
            "Window screenshot saved: C:\\captures\\actual-window.png (1280x720, 4096 bytes)",
            successText,
            "capture_window_screenshot formatted success");
    }

    private static async Task<string> InvokeWindowScreenshotAsync(
        Type screenshotTools,
        string outputPath,
        string responseJson)
    {
        var pipeName = NewMcpToolPipeName("screenshot");
        var pipeClient = CreateMcpPipeClient(pipeName);
        string result = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 1,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            screenshotTools,
                            "capture_window_screenshot",
                            pipeClient,
                            outputPath)
                        .ConfigureAwait(false);
                },
                _ => responseJson)
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "CaptureWindowScreenshot", ("outputPath", outputPath));
        return result;
    }

    internal static async Task McpWindowTools_RouteWindowActions()
    {
        var pipeName = NewMcpToolPipeName("window");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var windowTools = RequireMcpType("McpServer.Tools.WindowTools");

        string result = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 9,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            windowTools,
                            "window_action",
                            pipeClient,
                            "snap_top_left",
                            true,
                            null,
                            null,
                            null,
                            null)
                        .ConfigureAwait(false);
                    result += Environment.NewLine + await InvokeMcpToolStringAsync(
                            windowTools,
                            "window_action",
                            pipeClient,
                            "resize",
                            false,
                            null,
                            null,
                            1024,
                            768)
                        .ConfigureAwait(false);
                    result += Environment.NewLine + await InvokeMcpToolStringAsync(
                            windowTools,
                            "window_action",
                            pipeClient,
                            "move",
                            false,
                            42,
                            84,
                            null,
                            null)
                        .ConfigureAwait(false);
                    result += Environment.NewLine + await InvokeMcpToolStringAsync(
                            windowTools,
                            "window_action",
                            pipeClient,
                            "close",
                            true,
                            null,
                            null,
                            null,
                            null)
                        .ConfigureAwait(false);
                    result += Environment.NewLine + await InvokeMcpToolStringAsync(
                            windowTools,
                            "window_action",
                            pipeClient,
                            " close ",
                            true,
                            null,
                            null,
                            null,
                            null)
                        .ConfigureAwait(false);
                    result += Environment.NewLine + await InvokeMcpToolStringAsync(
                            windowTools,
                            "set_full_screen",
                            pipeClient,
                            true)
                        .ConfigureAwait(false);
                    result += Environment.NewLine + await InvokeMcpToolStringAsync(
                            windowTools,
                            "open_recordings_folder",
                            pipeClient)
                        .ConfigureAwait(false);
                },
                i => $$"""{"Success":true,"Message":"window command {{i}} ok"}""")
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "WindowAction", ("action", "SnapTopLeft"));
        AssertCommandRequest(requests[1], "WindowAction", ("action", "Resize"), ("width", 1024), ("height", 768));
        AssertCommandRequest(requests[2], "WindowAction", ("action", "Move"), ("x", 42), ("y", 84));
        AssertWindowCloseActionIdPair(requests[3], requests[4], "mcp close");
        AssertWindowCloseActionIdPair(requests[5], requests[6], "mcp trimmed close");
        AssertCommandRequest(requests[7], "SetFullScreenEnabled", ("enabled", true));
        AssertCommandRequest(requests[8], "OpenRecordingsFolder");
        AssertEqual(
            string.Join(
                Environment.NewLine,
                "[OK] WindowAction: window command 0 ok",
                "[OK] WindowAction: window command 1 ok",
                "[OK] WindowAction: window command 2 ok",
                "[OK] ArmClose: window command 3 ok",
                "[OK] WindowAction: window command 4 ok",
                "[OK] ArmClose: window command 5 ok",
                "[OK] WindowAction: window command 6 ok",
                "[OK] SetFullScreenEnabled: window command 7 ok",
                "[OK] OpenRecordingsFolder: window command 8 ok"),
            result,
            "window_action ordered formatted output");

        var failedArmPipeName = NewMcpToolPipeName("window-close-arm-failed");
        var failedArmPipeClient = CreateMcpPipeClient(failedArmPipeName);
        var failedArmResult = string.Empty;
        var failedArmRequests = await CapturePipeRequestsAsync(
                failedArmPipeName,
                expectedCount: 1,
                async () =>
                {
                    failedArmResult = await InvokeMcpToolStringAsync(
                            windowTools,
                            "window_action",
                            failedArmPipeClient,
                            "close",
                            true,
                            null,
                            null,
                            null,
                            null)
                        .ConfigureAwait(false);
                },
                _ => "{\"Success\":false,\"Message\":\"arm rejected\"}")
            .ConfigureAwait(false);

        AssertAutomationCommandId(failedArmRequests[0], "ArmClose");
        var failedArmPayload = failedArmRequests[0].GetProperty("payload");
        AssertJsonObjectPropertyNames(failedArmPayload, "armed", "actionId");
        AssertJsonPropertyEquals(failedArmPayload, "armed", true, "mcp failed ArmClose.armed");
        AssertEqual(32, failedArmPayload.GetProperty("actionId").GetString()?.Length ?? 0, "mcp failed actionId length");
        AssertEqual("[ERROR] ArmClose: arm rejected", failedArmResult, "window_action stops after failed ArmClose");
    }

    private static void AssertWindowCloseActionIdPair(JsonElement armRequest, JsonElement closeRequest, string scenario)
    {
        AssertAutomationCommandId(armRequest, "ArmClose");
        AssertAutomationCommandId(closeRequest, "WindowAction");

        var armPayload = armRequest.GetProperty("payload");
        var closePayload = closeRequest.GetProperty("payload");
        AssertJsonObjectPropertyNames(armPayload, "armed", "actionId");
        AssertJsonObjectPropertyNames(closePayload, "action", "actionId");
        AssertJsonPropertyEquals(armPayload, "armed", true, $"{scenario} ArmClose.armed");
        AssertJsonPropertyEquals(closePayload, "action", "Close", $"{scenario} WindowAction.action");

        var actionId = armPayload.GetProperty("actionId").GetString();
        AssertEqual(32, actionId?.Length ?? 0, $"{scenario} actionId length");
        AssertEqual(actionId, closePayload.GetProperty("actionId").GetString(), $"{scenario} actionId match");
    }

    internal static Task McpWaitTools_UsesCatalogResponseTimeoutForConditionWaits()
    {
        var waitToolsSource = ReadRepoFile("tools/McpServer/Tools/AutomationControlTools.cs");
        AssertContains(waitToolsSource, "AutomationPipeProtocol.GetDefaultResponseTimeout(AutomationCommandKind.WaitForCondition)");
        AssertContains(waitToolsSource, "SendCommandAsync(AutomationCommandKind.WaitForCondition, payload, responseTimeoutMs)");
        AssertDoesNotContain(waitToolsSource, "WaitForConditionCommandName");
        AssertDoesNotContain(waitToolsSource, "SendCommandAsync(\"WaitForCondition\"");
        AssertDoesNotContain(waitToolsSource, "AutomationPipeProtocol.DefaultResponseTimeoutMs");

        var waitTools = RequireMcpType("McpServer.Tools.WaitTools");
        var timeoutMethod = RequireNonPublicStaticMethod(waitTools, "GetWaitForConditionResponseTimeoutMs");

        AssertEqual(
            Sussudio.Tools.AutomationPipeProtocol.ExtendedResponseTimeoutMs,
            (int)timeoutMethod.Invoke(null, new object[] { 10000 })!,
            "MCP wait default pipe response timeout follows catalog policy");
        AssertEqual(
            65000,
            (int)timeoutMethod.Invoke(null, new object[] { 60000 })!,
            "MCP wait explicit timeout keeps response buffer");
        AssertEqual(
            int.MaxValue,
            (int)timeoutMethod.Invoke(null, new object[] { int.MaxValue })!,
            "MCP wait response timeout saturates on extreme input");

        return Task.CompletedTask;
    }

    internal static async Task McpWaitTools_RouteConditionWaits()
    {
        var pipeName = NewMcpToolPipeName("wait");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var waitTools = RequireMcpType("McpServer.Tools.WaitTools");

        string metResult = string.Empty;
        string notMetResult = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 2,
                async () =>
                {
                    metResult = await InvokeMcpToolStringAsync(
                            waitTools,
                            "wait_for_condition",
                            pipeClient,
                            "PreviewFramesActive",
                            750,
                            50)
                        .ConfigureAwait(false);
                    notMetResult = await InvokeMcpToolStringAsync(
                            waitTools,
                            "wait_for_condition",
                            pipeClient,
                            "RecordingStopped",
                            100,
                            10)
                        .ConfigureAwait(false);
                },
                i => i == 0
                    ? """
                      {
                        "Success": true,
                        "Message": "preview frames flowing",
                        "Data": {
                          "condition": "PreviewFramesActive",
                          "met": true,
                          "timeoutMs": 750,
                          "pollMs": 50
                        }
                      }
                      """
                    : """
                      {
                        "Success": false,
                        "Message": "recording still active",
                        "Data": {
                          "condition": "RecordingStopped",
                          "met": false,
                          "timeoutMs": 250,
                          "pollMs": 25
                        }
                      }
                      """)
            .ConfigureAwait(false);

        AssertCommandRequest(
            requests[0],
            "WaitForCondition",
            ("condition", "PreviewFramesActive"),
            ("timeoutMs", 750),
            ("pollMs", 50));
        AssertCommandRequest(
            requests[1],
            "WaitForCondition",
            ("condition", "RecordingStopped"),
            ("timeoutMs", 100),
            ("pollMs", 10));
        AssertContainsOrdinal(metResult, "Condition result: MET");
        AssertContainsOrdinal(metResult, "Met: true");
        AssertContainsOrdinal(metResult, "Condition: PreviewFramesActive");
        AssertContainsOrdinal(notMetResult, "Condition result: NOT MET");
        AssertContainsOrdinal(notMetResult, "Met: false");
        AssertContainsOrdinal(notMetResult, "TimeoutMs: 250");
        AssertContainsOrdinal(notMetResult, "PollMs: 25");
    }

    internal static async Task McpFlashbackTools_RouteEnableToggle()
    {
        var pipeName = NewMcpToolPipeName("flashback-enabled");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var flashbackTools = RequireMcpType("McpServer.Tools.FlashbackTools");

        string result = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 1,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            flashbackTools,
                            "flashback_enabled",
                            pipeClient,
                            false)
                        .ConfigureAwait(false);
                },
                _ => "{\"Success\":true,\"Message\":\"Flashback disabled.\"}")
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "SetFlashbackEnabled", ("enabled", false));
        AssertEqual("[OK] SetFlashbackEnabled: Flashback disabled.", result, "flashback_enabled formatted success");

        var actionPipeName = NewMcpToolPipeName("flashback-action-scrub");
        var actionPipeClient = CreateMcpPipeClient(actionPipeName);
        var actionRequests = await CapturePipeRequestsAsync(
                actionPipeName,
                expectedCount: 1,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            flashbackTools,
                            "flashback_action",
                            actionPipeClient,
                            "begin_scrub",
                            1234d)
                        .ConfigureAwait(false);
                },
                _ => "{\"Success\":true,\"Message\":\"Flashback scrub begin at 1234ms requested.\"}")
            .ConfigureAwait(false);

        AssertCommandRequest(actionRequests[0], "FlashbackAction", ("action", "begin-scrub"), ("positionMs", 1234d));
        AssertContains(result, "[OK] FlashbackAction(begin-scrub): Flashback scrub begin at 1234ms requested.");

        var applyPipeName = NewMcpToolPipeName("flashback-apply");
        var applyPipeClient = CreateMcpPipeClient(applyPipeName);
        var applyRequests = await CapturePipeRequestsAsync(
                applyPipeName,
                expectedCount: 1,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            flashbackTools,
                            "flashback_apply",
                            applyPipeClient)
                        .ConfigureAwait(false);
                },
                _ => "{\"Success\":true,\"Message\":\"Flashback restarted.\"}")
            .ConfigureAwait(false);

        AssertCommandRequest(applyRequests[0], "RestartFlashback");
        AssertEqual("[OK] RestartFlashback: Flashback restarted.", result, "flashback_apply formatted success");

        var flashbackToolsRootText = ReadRepoFile("tools/McpServer/Tools/AutomationControlTools.cs")
            .Replace("\r\n", "\n");
        var flashbackToolsActionText = flashbackToolsRootText;
        var flashbackToolsExportText = flashbackToolsRootText;
        AssertContains(flashbackToolsRootText, "[McpServerToolType]");
        AssertContains(flashbackToolsRootText, "public static class FlashbackTools");
        AssertDoesNotContain(flashbackToolsRootText, "public static partial class FlashbackTools");
        AssertContains(flashbackToolsRootText, "public static async Task<CallToolResult> flashback_enabled");
        AssertContains(flashbackToolsRootText, "public static async Task<CallToolResult> flashback_apply");
        AssertContains(flashbackToolsRootText, "public static async Task<CallToolResult> flashback_segments");
        AssertContains(flashbackToolsRootText, "FlashbackGetSegments");
        AssertContains(flashbackToolsActionText, "public static async Task<CallToolResult> flashback_action");
        AssertContains(flashbackToolsActionText, "if (string.IsNullOrWhiteSpace(action))");
        AssertContains(flashbackToolsActionText, "Flashback action is required. Expected play, pause, go_live, seek, begin_scrub, update_scrub, end_scrub, set_in_point, set_out_point, or clear_in_out_points.");
        AssertContains(flashbackToolsActionText, "normalizedAction is not (\"play\" or \"pause\" or \"go-live\" or \"seek\" or \"begin-scrub\" or \"update-scrub\" or \"end-scrub\" or \"set-in-point\" or \"set-out-point\" or \"clear-in-out-points\")");
        AssertContains(flashbackToolsActionText, "Flashback action must be one of: play, pause, go_live, seek, begin_scrub, update_scrub, end_scrub, set_in_point, set_out_point, clear_in_out_points.");
        AssertContains(flashbackToolsActionText, "normalizedAction == \"begin-scrub\"");
        AssertContains(flashbackToolsActionText, "normalizedAction == \"update-scrub\"");
        AssertContains(flashbackToolsActionText, "Flashback seek, begin_scrub, and update_scrub require positionMs.");
        AssertContains(flashbackToolsActionText, "if (!double.IsFinite(positionMs.Value) ||\n                positionMs.Value < 0 ||\n                positionMs.Value > TimeSpan.MaxValue.TotalMilliseconds)");
        AssertContains(flashbackToolsActionText, "Flashback positionMs must be finite, non-negative, and within TimeSpan range.");
        AssertContains(flashbackToolsExportText, "public static async Task<CallToolResult> flashback_export");
        AssertContains(flashbackToolsExportText, "if (!double.IsFinite(seconds) || seconds <= 0 || seconds > TimeSpan.MaxValue.TotalSeconds)");
        AssertContains(flashbackToolsExportText, "Flashback export seconds must be finite, greater than zero, and within TimeSpan range.");
        AssertContains(flashbackToolsExportText, "AutomationSnapshotFormatter.Get(data, \"FailureKind\", string.Empty)");
        AssertContains(flashbackToolsExportText, "FailureKind: {failureKind}");

        var exportPipeName = NewMcpToolPipeName("flashback-export-failure-kind");
        var exportPipeClient = CreateMcpPipeClient(exportPipeName);
        var exportRequests = await CapturePipeRequestsAsync(
                exportPipeName,
                expectedCount: 1,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            flashbackTools,
                            "flashback_export",
                            exportPipeClient,
                            1d,
                            "temp/fb-failure-kind.mp4",
                            false,
                            false)
                        .ConfigureAwait(false);
                },
                _ => "{\"Success\":false,\"Message\":\"Flashback buffer not active\",\"Data\":{\"Succeeded\":false,\"OutputPath\":\"temp/fb-failure-kind.mp4\",\"StatusMessage\":\"Flashback buffer not active\",\"FailureKind\":\"BufferInactive\",\"FileSizeBytes\":0}}")
            .ConfigureAwait(false);

        AssertCommandRequest(exportRequests[0], "FlashbackExport", ("seconds", 1d), ("outputPath", "temp/fb-failure-kind.mp4"), ("useSelectionRange", false), ("force", false));
        AssertContains(result, "[ERROR] FlashbackExport: Flashback buffer not active");
        AssertContains(result, "FailureKind: BufferInactive");

        var segmentsPipeName = NewMcpToolPipeName("flashback-segments");
        var segmentsPipeClient = CreateMcpPipeClient(segmentsPipeName);
        var segmentsRequests = await CapturePipeRequestsAsync(
                segmentsPipeName,
                expectedCount: 1,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            flashbackTools,
                            "flashback_segments",
                            segmentsPipeClient)
                        .ConfigureAwait(false);
                },
                _ => "{\"Success\":true,\"Message\":\"1 segment.\",\"Data\":{\"Segments\":[{\"Path\":\"temp/segment-000.mp4\",\"DurationMs\":1000,\"FrameCount\":60}]}}")
            .ConfigureAwait(false);

        AssertCommandRequest(segmentsRequests[0], "FlashbackGetSegments");
        AssertContains(result, "[OK] FlashbackGetSegments: 1 segment.");
        AssertContains(result, "\"FrameCount\":60");
    }

    internal static async Task McpPreviewColorProbeTool_FormatsProbeResponses()
    {
        var previewColorProbeTool = RequireMcpType("McpServer.Tools.PreviewColorProbeTools");

        var failureText = await InvokePreviewColorProbeAsync(
                previewColorProbeTool,
                "{\"Success\":false,\"Message\":\"preview unavailable\"}")
            .ConfigureAwait(false);
        AssertEqual("preview unavailable", failureText, "probe_preview_color failure message");

        var missingDataText = await InvokePreviewColorProbeAsync(
                previewColorProbeTool,
                "{\"Success\":true,\"Message\":\"ok\"}")
            .ConfigureAwait(false);
        AssertEqual("No probe data returned.", missingDataText, "probe_preview_color missing data");

        var inactiveText = await InvokePreviewColorProbeAsync(
                previewColorProbeTool,
                "{\"Success\":true,\"Data\":{\"SessionActive\":false}}")
            .ConfigureAwait(false);
        AssertContains(inactiveText, "== Preview Color Probe ==");
        AssertContainsOrdinal(inactiveText, "Session Active: false");
        AssertContains(inactiveText, "No active preview session. Start preview first.");

        var activeJson = """
                         {
                           "Success": true,
                           "Data": {
                             "SessionActive": true,
                             "RendererMode": "D3D11VideoProcessor",
                             "NegotiatedSubtype": "P010",
                             "SourceWidth": 3840,
                             "SourceHeight": 2160,
                             "SourceFrameRate": 59.94,
                             "NominalRangeLabel": "Full",
                             "NominalRange": 2,
                             "TransferFunctionLabel": "PQ",
                             "TransferFunction": 16,
                             "VideoPrimariesLabel": "BT.2020",
                             "VideoPrimaries": 9,
                             "YuvMatrixLabel": "BT.2020",
                             "YuvMatrix": 9,
                             "D3DInputColorSpace": "BT2020_PQ",
                             "D3DOutputColorSpace": "RGB_Full",
                             "LumaSampleCount": 100,
                             "LumaMin": 0,
                             "LumaMax": 255,
                             "LumaMean": 128.5,
                             "LumaBelow16Count": 5,
                             "LumaAbove235Count": 10,
                             "FormatProperties": {
                               "MF_MT_SUBTYPE": "P010"
                             }
                           }
                         }
                         """;
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        string activeText;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            activeText = await InvokePreviewColorProbeAsync(previewColorProbeTool, activeJson).ConfigureAwait(false);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }

        AssertContains(activeText, "Renderer: D3D11VideoProcessor");
        AssertContains(activeText, "Format: P010 3840x2160 @ 59.94fps");
        AssertContains(activeText, "== Color Attributes ==");
        AssertContains(activeText, "Nominal Range: Full (raw=2)");
        AssertContains(activeText, "== D3D11 Video Processor ==");
        AssertContains(activeText, "== Luma (Y Plane) Analysis ==");
        AssertContains(activeText, "Diagnosis: Data uses FULL range (0-255). 10.0% super-white, 5.0% super-black.");
        AssertContains(activeText, "== Raw MF Properties ==");
        AssertContains(activeText, "MF_MT_SUBTYPE = P010");
    }

    internal static async Task McpVideoSourceProbeTool_FormatsProbeResponses()
    {
        var videoSourceProbeTool = RequireMcpType("McpServer.Tools.VideoSourceProbeTools");

        var failureText = await InvokeVideoSourceProbeAsync(
                videoSourceProbeTool,
                "{\"Success\":false,\"Message\":\"source unavailable\"}")
            .ConfigureAwait(false);
        AssertEqual("source unavailable", failureText, "probe_video_source failure message");

        var missingDataText = await InvokeVideoSourceProbeAsync(
                videoSourceProbeTool,
                "{\"Success\":true,\"Message\":\"ok\"}")
            .ConfigureAwait(false);
        AssertEqual("No probe data returned.", missingDataText, "probe_video_source missing data");

        var inactiveText = await InvokeVideoSourceProbeAsync(
                videoSourceProbeTool,
                "{\"Success\":true,\"Data\":{\"SessionActive\":false}}")
            .ConfigureAwait(false);
        AssertContains(inactiveText, "== Video Source Probe ==");
        AssertContainsOrdinal(inactiveText, "Session Active: false");
        AssertContains(inactiveText, "No active ingest session. Start preview first.");

        var activeJson = """
                         {
                           "Success": true,
                           "Data": {
                             "SessionActive": true,
                             "MemoryPreference": "D3D11",
                             "CurrentSubtype": "P010",
                             "CurrentWidth": 3840,
                             "CurrentHeight": 2160,
                             "CurrentFrameRate": 59.94,
                             "P010Available": true,
                             "Nv12Available": true,
                             "SupportedSubtypes": ["P010", "NV12", ""],
                             "TotalFormatCount": 2,
                             "Formats": [
                               { "Summary": "3840x2160 P010 59.94fps" },
                               { "Summary": "1920x1080 NV12 60fps" }
                             ]
                           }
                         }
                         """;
        var activeText = await InvokeVideoSourceProbeAsync(videoSourceProbeTool, activeJson).ConfigureAwait(false);
        AssertContains(activeText, "Memory Preference: D3D11");
        AssertContains(activeText, "Current Format: P010 3840x2160@59.94fps");
        AssertContainsOrdinal(activeText, "P010 Available: true | NV12 Available: true");
        AssertContains(activeText, "Supported Subtypes: P010, NV12");
        AssertContains(activeText, "Total Format Count: 2");
        AssertContains(activeText, "== Format Table ==");
        AssertContains(activeText, "[0] 3840x2160 P010 59.94fps");
        AssertContains(activeText, "[1] 1920x1080 NV12 60fps");
    }

    private static async Task<string> InvokePreviewColorProbeAsync(Type previewColorProbeTool, string responseJson)
    {
        var pipeName = NewMcpToolPipeName("color");
        var pipeClient = CreateMcpPipeClient(pipeName);
        string result = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 1,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            previewColorProbeTool,
                            "probe_preview_color",
                            pipeClient)
                        .ConfigureAwait(false);
                },
                _ => responseJson)
            .ConfigureAwait(false);
        AssertCommandRequest(requests[0], "ProbePreviewColor");
        return result;
    }

    private static async Task<string> InvokeVideoSourceProbeAsync(Type videoSourceProbeTool, string responseJson)
    {
        var pipeName = NewMcpToolPipeName("source");
        var pipeClient = CreateMcpPipeClient(pipeName);
        string result = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 1,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            videoSourceProbeTool,
                            "probe_video_source",
                            pipeClient)
                        .ConfigureAwait(false);
                },
                _ => responseJson)
            .ConfigureAwait(false);
        AssertCommandRequest(requests[0], "ProbeVideoSource");
        return result;
    }

    internal static async Task McpPreviewFrameCaptureTool_FormatsCaptureResponses()
    {
        var previewFrameCaptureTool = RequireMcpType("McpServer.Tools.PreviewFrameCaptureTools");
        var defaultOutputPath = Path.Combine(Environment.CurrentDirectory, "temp", "preview_capture.bmp");

        var failureText = await InvokePreviewFrameCaptureAsync(
                previewFrameCaptureTool,
                "preview-frame-failure",
                outputPath: null,
                expectedOutputPath: defaultOutputPath,
                responseJson: "{\"Success\":false,\"Message\":\"preview unavailable\"}")
            .ConfigureAwait(false);
        AssertEqual("preview unavailable", failureText, "capture_preview_frame failure message");

        var missingDataText = await InvokePreviewFrameCaptureAsync(
                previewFrameCaptureTool,
                "preview-frame-missing",
                outputPath: @"C:\captures\missing.bmp",
                expectedOutputPath: @"C:\captures\missing.bmp",
                responseJson: "{\"Success\":true,\"Message\":\"ok\"}")
            .ConfigureAwait(false);
        AssertEqual("No frame capture data returned.", missingDataText, "capture_preview_frame missing data");

        var activeJson = """
                         {
                           "Success": true,
                           "Data": {
                             "FilePath": "C:\\captures\\preview.bmp",
                             "CapturedWidth": 640,
                             "CapturedHeight": 360,
                             "RendererMode": "D3D11",
                             "AverageR": 10,
                             "AverageG": 20,
                             "AverageB": 30,
                             "AverageLuminance": 25.5,
                             "MinLuminance": 10,
                             "MaxLuminance": 34,
                             "NearBlackPercent": 12.5,
                             "NearWhitePercent": 0,
                             "PureBlackPercent": 96.5,
                             "LetterboxTopRows": 12,
                             "LetterboxBottomRows": 12,
                             "PillarboxLeftCols": 3,
                             "PillarboxRightCols": 4,
                             "ContentWidth": 640,
                             "ContentHeight": 360,
                             "ContentAspectRatio": 1.333,
                             "TotalPixels": 230400,
                             "LuminanceHistogram": [0, 10, 20, 40, 80, 160, 80, 40, 20, 10, 5, 0, 0, 0, 0, 0]
                           }
                         }
                         """;
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        string activeText;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            activeText = await InvokePreviewFrameCaptureAsync(
                    previewFrameCaptureTool,
                    "preview-frame-active",
                    outputPath: @"C:\captures\preview.bmp",
                    expectedOutputPath: @"C:\captures\preview.bmp",
                    responseJson: activeJson)
                .ConfigureAwait(false);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }

        AssertEqual(
            NormalizeLineEndings(
                """
            == Preview Frame Capture ==
            File: C:\captures\preview.bmp
            Resolution: 640 x 360
            Renderer: D3D11

            == Pixel Summary ==
            Average RGB: R=10 G=20 B=30
            Luminance: avg=25.5 min=10 max=34
            Near Black (<16): 12.5%
            Near White (>240): 0%
            Pure Black: 96.5%

            == Framing ==
            Letterbox: top=12 bottom=12 rows
            Pillarbox: left=3 right=4 cols
            Content Area: 640 x 360
            Content Aspect Ratio: 1.333
            Total Pixels: 230400

            == Luminance Histogram (16 bins) ==
              0- 15:  (0)
             16- 31: ## (10)
             32- 47: ### (20)
             48- 63: ###### (40)
             64- 79: ############ (80)
             80- 95: ######################## (160)
             96-111: ############ (80)
            112-127: ###### (40)
            128-143: ### (20)
            144-159: ## (10)
            160-175: # (5)
            176-191:  (0)
            192-207:  (0)
            208-223:  (0)
            224-239:  (0)
            240-255:  (0)

            == Diagnosis ==
            - BLANK FRAME: >95% of pixels are pure black.
            - VERY DARK: average luminance is below 30.
            - LETTERBOXED: top=12, bottom=12, estimated source aspect=1.333 (640x360).
            - PILLARBOXED: left=3, right=4, estimated source aspect=1.333 (640x360).
            - LOW CONTRAST: luminance range is under 30.
            - ASPECT RATIO ALERT: content aspect 1.333 is not close to 16:9 or 16:10.
            """),
            NormalizeLineEndings(activeText),
            "capture_preview_frame exact report");

        var noAnomalyJson = """
                            {
                              "Success": true,
                              "Data": {
                                "FilePath": "temp/preview_capture.bmp",
                                "CapturedWidth": 1920,
                                "CapturedHeight": 1080,
                                "RendererMode": "D3D11VideoProcessor",
                                "AverageR": 120,
                                "AverageG": 130,
                                "AverageB": 140,
                                "AverageLuminance": 128,
                                "MinLuminance": 0,
                                "MaxLuminance": 255,
                                "NearBlackPercent": 0,
                                "NearWhitePercent": 0,
                                "PureBlackPercent": 0,
                                "LetterboxTopRows": 0,
                                "LetterboxBottomRows": 0,
                                "PillarboxLeftCols": 0,
                                "PillarboxRightCols": 0,
                                "ContentWidth": 1920,
                                "ContentHeight": 1080,
                                "ContentAspectRatio": 1.777,
                                "TotalPixels": 2073600
                              }
                            }
                            """;
        var noAnomalyText = await InvokePreviewFrameCaptureAsync(
                previewFrameCaptureTool,
                "preview-frame-no-anomaly",
                outputPath: "temp/preview_capture.bmp",
                expectedOutputPath: "temp/preview_capture.bmp",
                responseJson: noAnomalyJson)
            .ConfigureAwait(false);
        AssertContains(noAnomalyText, "Histogram unavailable.");
        AssertContains(noAnomalyText, "No obvious anomalies detected.");

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            var frenchCultureText = await InvokePreviewFrameCaptureAsync(
                    previewFrameCaptureTool,
                    "preview-frame-culture",
                    outputPath: "temp/preview_capture_culture.bmp",
                    expectedOutputPath: "temp/preview_capture_culture.bmp",
                    responseJson: activeJson)
                .ConfigureAwait(false);
            AssertContains(frenchCultureText, "estimated source aspect=1.333 (640x360).");
            AssertContains(frenchCultureText, "content aspect 1.333 is not close to 16:9 or 16:10.");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }

        var rootText = ReadRepoFile("tools/McpServer/Tools/PreviewInspectionTools.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "[McpServerToolType]");
        AssertContains(rootText, "public static class PreviewFrameCaptureTools");
        AssertContains(rootText, "public static async Task<CallToolResult> capture_preview_frame");
        AssertContains(rootText, "Path.Combine(Environment.CurrentDirectory, \"temp\", \"preview_capture.bmp\")");
        AssertContains(rootText, "SendCommandAsync(AutomationCommandKind.CapturePreviewFrame, payload)");
        AssertDoesNotContain(rootText, "SendCommandAsync(\"CapturePreviewFrame\", payload)");
        AssertContains(rootText, "BuildPreviewFrameCaptureText(data)");

        AssertContains(rootText, "private static string BuildPreviewFrameCaptureText(");
        AssertContains(rootText, "== Preview Frame Capture ==");
        AssertContains(rootText, "== Pixel Summary ==");
        AssertContains(rootText, "AppendLuminanceHistogram(builder, data)");
        AssertContains(rootText, "AppendPreviewFrameCaptureDiagnosis(builder, data)");
        AssertContains(rootText, "private static void AppendLuminanceHistogram(");
        AssertContains(rootText, "LuminanceHistogram");
        AssertContains(rootText, "while (bins.Count < 16)");
        AssertContains(rootText, "* 24.0");
        AssertContains(rootText, "new string('#', Math.Max(0, barLength))");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "McpServer", "Tools", "PreviewFrameCaptureTools.Histogram.cs")),
            "preview frame histogram rendering lives with the preview frame report renderer");

        AssertContains(rootText, "private static List<string> BuildPreviewFrameCaptureDiagnosis(");
        AssertContains(rootText, "pureBlackPercent > 95.0");
        AssertContains(rootText, "averageLuminance < 30.0");
        AssertContains(rootText, "averageLuminance > 230.0");
        AssertContains(rootText, "(maxLuminance - minLuminance) < 30.0");
        AssertContains(rootText, "private static string FormatAspectRatio(");
        AssertContains(rootText, "AutomationSnapshotFormatter.FormatNumber(aspectRatio, \"0.###\")");
        AssertContains(rootText, "private static bool IsNear(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "McpServer", "Tools", "PreviewFrameCaptureTools.Rendering.cs")),
            "preview frame report rendering lives with the preview frame MCP tool");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "McpServer", "Tools", "PreviewFrameCaptureTools.Diagnosis.cs")),
            "preview frame diagnosis policy lives with the preview frame MCP tool");
    }

    private static async Task<string> InvokePreviewFrameCaptureAsync(
        Type previewFrameCaptureTool,
        string pipeSuffix,
        string? outputPath,
        string expectedOutputPath,
        string responseJson)
    {
        var pipeName = NewMcpToolPipeName(pipeSuffix);
        var pipeClient = CreateMcpPipeClient(pipeName);
        string result = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 1,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            previewFrameCaptureTool,
                            "capture_preview_frame",
                            pipeClient,
                            outputPath)
                        .ConfigureAwait(false);
                },
                _ => responseJson)
            .ConfigureAwait(false);
        AssertCommandRequest(requests[0], "CapturePreviewFrame", ("outputPath", expectedOutputPath));
        return result;
    }


// ssctl command-handler routing contracts live with the tool xUnit wrappers.
    internal static async Task SsctlCommandHandlers_RouteDeviceCommands()
    {
        var context = CreateSsctlCommandRoutingContext();

        var devicePipeName = $"ssctl-device-audio-{Guid.NewGuid():N}";
        var deviceArguments = new List<string> { "device", "audio-select", "Synthetic Mic" };
        var (deviceExitCode, deviceRequest) = await CaptureSsctlRequestAsync(
                context,
                devicePipeName,
                deviceArguments)
            .ConfigureAwait(false);

        AssertEqual(0, deviceExitCode, "device audio-select exit code");
        AssertSsctlCommandRequest(deviceRequest, "SelectAudioInputDevice", ("deviceName", "Synthetic Mic"));

        var deviceRefreshPipeName = $"ssctl-device-refresh-{Guid.NewGuid():N}";
        var deviceRefreshArguments = new List<string> { "device", "refresh" };
        var (deviceRefreshExitCode, deviceRefreshRequest) = await CaptureSsctlRequestAsync(
                context,
                deviceRefreshPipeName,
                deviceRefreshArguments)
            .ConfigureAwait(false);

        AssertEqual(0, deviceRefreshExitCode, "device refresh exit code");
        AssertSsctlCommandRequestHasEmptyPayload(deviceRefreshRequest, "RefreshDevices");

        var deviceListPipeName = $"ssctl-device-list-{Guid.NewGuid():N}";
        var deviceListArguments = new List<string> { "device", "list" };
        var (deviceListExitCode, deviceListRequests) = await CaptureSsctlRequestsAsync(
                context,
                deviceListPipeName,
                expectedCount: 2,
                arguments: deviceListArguments,
                responseFactory: i => i == 0
                    ? "{\"Success\":true,\"Message\":\"refresh ok\"}"
                    : "{\"Success\":true,\"Message\":\"options ok\",\"Data\":{\"Devices\":[],\"AudioInputDevices\":[]}}")
            .ConfigureAwait(false);

        AssertEqual(0, deviceListExitCode, "device list exit code");
        AssertSsctlCommandRequestHasEmptyPayload(deviceListRequests[0], "RefreshDevices");
        AssertSsctlCommandRequestHasEmptyPayload(deviceListRequests[1], "GetCaptureOptions");
    }

    internal static async Task SsctlCommandHandlers_RouteCaptureControlCommands()
    {
        var context = CreateSsctlCommandRoutingContext();

        var previewPipeName = $"ssctl-preview-{Guid.NewGuid():N}";
        var previewArguments = new List<string> { "preview", "start" };
        var (previewExitCode, previewRequest) = await CaptureSsctlRequestAsync(
                context,
                previewPipeName,
                previewArguments)
            .ConfigureAwait(false);

        AssertEqual(0, previewExitCode, "preview start exit code");
        AssertSsctlCommandRequest(previewRequest, "SetPreviewEnabled", ("enabled", true));
    }

    internal static async Task SsctlCommandHandlers_RouteRecordingsCommands()
    {
        var context = CreateSsctlCommandRoutingContext();

        var recordingsPipeName = $"ssctl-recordings-open-{Guid.NewGuid():N}";
        var recordingsArguments = new List<string> { "recordings", "open" };
        var (recordingsExitCode, recordingsRequest) = await CaptureSsctlRequestAsync(
                context,
                recordingsPipeName,
                recordingsArguments)
            .ConfigureAwait(false);

        AssertEqual(0, recordingsExitCode, "recordings open exit code");
        AssertSsctlCommandRequestHasEmptyPayload(recordingsRequest, "OpenRecordingsFolder");
    }

    internal static async Task SsctlCommandHandlers_RouteWindowCommands()
    {
        var context = CreateSsctlCommandRoutingContext();

        var fullscreenPipeName = $"ssctl-fullscreen-{Guid.NewGuid():N}";
        var fullscreenArguments = new List<string> { "window", "fullscreen", "on" };
        var (fullscreenExitCode, fullscreenRequest) = await CaptureSsctlRequestAsync(
                context,
                fullscreenPipeName,
                fullscreenArguments)
            .ConfigureAwait(false);

        AssertEqual(0, fullscreenExitCode, "window fullscreen exit code");
        AssertSsctlCommandRequest(fullscreenRequest, "SetFullScreenEnabled", ("enabled", true));

        var windowClosePipeName = $"ssctl-window-close-{Guid.NewGuid():N}";
        var windowCloseArguments = new List<string> { "window", "close" };
        var (windowCloseExitCode, windowCloseRequests) = await CaptureSsctlRequestsAsync(
                context,
                windowClosePipeName,
                expectedCount: 2,
                windowCloseArguments)
            .ConfigureAwait(false);

        AssertEqual(0, windowCloseExitCode, "window close exit code");
        AssertWindowCloseActionIdPair(windowCloseRequests[0], windowCloseRequests[1], "ssctl close");

        var windowCloseDeniedPipeName = $"ssctl-window-close-denied-{Guid.NewGuid():N}";
        var windowCloseDeniedArguments = new List<string> { "window", "close" };
        var (windowCloseDeniedExitCode, windowCloseDeniedRequests) = await CaptureSsctlRequestsAsync(
                context,
                windowCloseDeniedPipeName,
                expectedCount: 1,
                windowCloseDeniedArguments,
                _ => "{\"Success\":false,\"Message\":\"arm denied\"}")
            .ConfigureAwait(false);

        AssertEqual(3, windowCloseDeniedExitCode, "window close denied exit code");
        AssertAutomationCommandId(windowCloseDeniedRequests[0], "ArmClose");
        var deniedArmPayload = windowCloseDeniedRequests[0].GetProperty("payload");
        AssertJsonObjectPropertyNames(deniedArmPayload, "armed", "actionId");
        AssertJsonPropertyEquals(deniedArmPayload, "armed", true, "ssctl denied ArmClose.armed");
        AssertEqual(32, deniedArmPayload.GetProperty("actionId").GetString()?.Length ?? 0, "ssctl denied actionId length");
    }

    internal static async Task SsctlCommandHandlers_RouteManifestCommand()
    {
        var context = CreateSsctlCommandRoutingContext();

        var manifestPipeName = $"ssctl-manifest-{Guid.NewGuid():N}";
        var manifestArguments = new List<string> { "manifest" };
        var (manifestExitCode, manifestRequest) = await CaptureSsctlRequestAsync(
                context,
                manifestPipeName,
                manifestArguments)
            .ConfigureAwait(false);

        AssertEqual(0, manifestExitCode, "manifest exit code");
        AssertSsctlCommandRequestHasEmptyPayload(manifestRequest, "GetAutomationManifest");
    }

    internal static async Task SsctlCommandHandlers_RouteFlashbackCommands()
    {
        var context = CreateSsctlCommandRoutingContext();

        var flashbackPipeName = $"ssctl-flashback-{Guid.NewGuid():N}";
        var flashbackArguments = new List<string> { "flashback", "off" };
        var (flashbackExitCode, flashbackRequest) = await CaptureSsctlRequestAsync(
                context,
                flashbackPipeName,
                flashbackArguments)
            .ConfigureAwait(false);

        AssertEqual(0, flashbackExitCode, "flashback off exit code");
        AssertSsctlCommandRequest(flashbackRequest, "SetFlashbackEnabled", ("enabled", false));

        var flashbackExportPipeName = $"ssctl-flashback-export-{Guid.NewGuid():N}";
        var flashbackExportOutputPath = Path.Combine("temp", "ssctl flashback export", "export with spaces.mp4");
        var flashbackExportArguments = new List<string>
        {
            "flashback",
            "export",
            "--range",
            "--force",
            "2.5",
            flashbackExportOutputPath,
        };
        var (flashbackExportExitCode, flashbackExportRequest) = await CaptureSsctlRequestAsync(
                context,
                flashbackExportPipeName,
                flashbackExportArguments)
            .ConfigureAwait(false);

        AssertEqual(0, flashbackExportExitCode, "flashback export exit code");
        AssertSsctlCommandRequest(
            flashbackExportRequest,
            "FlashbackExport",
            ("seconds", 2.5d),
            ("outputPath", flashbackExportOutputPath),
            ("useSelectionRange", true),
            ("force", true));
        AssertEqual(
            true,
            Directory.Exists(Path.GetDirectoryName(flashbackExportOutputPath) ?? "."),
            "flashback export parent directory created");

        var flashbackSeekPipeName = $"ssctl-flashback-seek-{Guid.NewGuid():N}";
        var (flashbackSeekExitCode, flashbackSeekRequest) = await CaptureSsctlRequestAsync(
                context,
                flashbackSeekPipeName,
                new List<string> { "flashback", "seek", "1234.5" })
            .ConfigureAwait(false);

        AssertEqual(0, flashbackSeekExitCode, "flashback seek exit code");
        AssertSsctlCommandRequest(
            flashbackSeekRequest,
            "FlashbackAction",
            ("action", "seek"),
            ("positionMs", 1234.5d));

        var flashbackScrubPipeName = $"ssctl-flashback-scrub-{Guid.NewGuid():N}";
        var (flashbackScrubExitCode, flashbackScrubRequest) = await CaptureSsctlRequestAsync(
                context,
                flashbackScrubPipeName,
                new List<string> { "flashback", "begin-scrub", "250" })
            .ConfigureAwait(false);

        AssertEqual(0, flashbackScrubExitCode, "flashback begin-scrub exit code");
        AssertSsctlCommandRequest(
            flashbackScrubRequest,
            "FlashbackAction",
            ("action", "begin-scrub"),
            ("positionMs", 250d));

        var flashbackClearRangePipeName = $"ssctl-flashback-clear-range-{Guid.NewGuid():N}";
        var (flashbackClearRangeExitCode, flashbackClearRangeRequest) = await CaptureSsctlRequestAsync(
                context,
                flashbackClearRangePipeName,
                new List<string> { "flashback", "clear-range" })
            .ConfigureAwait(false);

        AssertEqual(0, flashbackClearRangeExitCode, "flashback clear-range exit code");
        AssertSsctlCommandRequest(
            flashbackClearRangeRequest,
            "FlashbackAction",
            ("action", "clear-in-out-points"));
    }

    internal static async Task SsctlCommandHandlers_RouteObservabilityCommands()
    {
        var context = CreateSsctlCommandRoutingContext();

        var audioRampPipeName = $"ssctl-audio-ramp-trace-{Guid.NewGuid():N}";
        var audioRampArguments = new List<string> { "audio-ramp-trace" };
        var (audioRampExitCode, audioRampRequest) = await CaptureSsctlRequestAsync(
                context,
                audioRampPipeName,
                audioRampArguments)
            .ConfigureAwait(false);

        AssertEqual(0, audioRampExitCode, "audio-ramp-trace exit code");
        AssertSsctlCommandRequestHasEmptyPayload(audioRampRequest, "GetAudioRampTrace");
    }

    internal static async Task SsctlCommandHandlers_RouteAutomationFlowCommands()
    {
        var context = CreateSsctlCommandRoutingContext();

        var assertPipeName = $"ssctl-assert-simple-{Guid.NewGuid():N}";
        var assertArguments = new List<string> { "assert", "IsRecording", "eq", "false" };
        var (assertExitCode, assertRequest) = await CaptureSsctlRequestAsync(
                context,
                assertPipeName,
                assertArguments)
            .ConfigureAwait(false);

        AssertEqual(0, assertExitCode, "assert simple exit code");
        var assertPayload = AssertSsctlCommandRequest(assertRequest, "AssertSnapshot")
            .GetProperty("assertions")[0];
        AssertEqual("IsRecording", assertPayload.GetProperty("field").GetString(), "assert simple field");
        AssertEqual("eq", assertPayload.GetProperty("op").GetString(), "assert simple op");
        AssertEqual(false, assertPayload.GetProperty("value").GetBoolean(), "assert simple value");

        var waitPipeName = $"ssctl-wait-{Guid.NewGuid():N}";
        var waitArguments = new List<string> { "wait", "preview-ready", "--timeout", "12500", "--poll", "250" };
        var (waitExitCode, waitRequest) = await CaptureSsctlRequestAsync(
                context,
                waitPipeName,
                waitArguments)
            .ConfigureAwait(false);

        AssertEqual(0, waitExitCode, "wait exit code");
        AssertSsctlCommandRequest(
            waitRequest,
            "WaitForCondition",
            ("condition", "preview-ready"),
            ("timeoutMs", 12500),
            ("pollMs", 250));

        var probePipeName = $"ssctl-probe-color-{Guid.NewGuid():N}";
        var probeArguments = new List<string> { "probe", "color" };
        var (probeExitCode, probeRequest) = await CaptureSsctlRequestAsync(
                context,
                probePipeName,
                probeArguments)
            .ConfigureAwait(false);

        AssertEqual(0, probeExitCode, "probe color exit code");
        AssertSsctlCommandRequestHasEmptyPayload(probeRequest, "ProbePreviewColor");
    }

    internal static async Task SsctlCommandHandlers_RouteUiVisibilityCommands()
    {
        var context = CreateSsctlCommandRoutingContext();

        var statsSectionPipeName = $"ssctl-stats-section-{Guid.NewGuid():N}";
        var statsSectionArguments = new List<string> { "stats", "section", "Preview Cadence", "hide" };
        var (statsSectionExitCode, statsSectionRequest) = await CaptureSsctlRequestAsync(
                context,
                statsSectionPipeName,
                statsSectionArguments)
            .ConfigureAwait(false);

        AssertEqual(0, statsSectionExitCode, "stats section exit code");
        AssertSsctlCommandRequest(
            statsSectionRequest,
            "SetStatsSectionVisible",
            ("section", "Preview Cadence"),
            ("visible", false));

        var settingsPipeName = $"ssctl-settings-show-{Guid.NewGuid():N}";
        var settingsArguments = new List<string> { "settings", "show" };
        var (settingsExitCode, settingsRequest) = await CaptureSsctlRequestAsync(
                context,
                settingsPipeName,
                settingsArguments)
            .ConfigureAwait(false);

        AssertEqual(0, settingsExitCode, "settings show exit code");
        AssertSsctlCommandRequest(settingsRequest, "SetSettingsVisible", ("visible", true));

        var frameTimePipeName = $"ssctl-frametime-hide-{Guid.NewGuid():N}";
        var frameTimeArguments = new List<string> { "frame-time", "hide" };
        var (frameTimeExitCode, frameTimeRequest) = await CaptureSsctlRequestAsync(
                context,
                frameTimePipeName,
                frameTimeArguments)
            .ConfigureAwait(false);

        AssertEqual(0, frameTimeExitCode, "frametime hide exit code");
        AssertSsctlCommandRequest(frameTimeRequest, "SetFrameTimeOverlayVisible", ("visible", false));
    }

    internal static async Task SsctlCommandHandlers_RouteVerificationCommands()
    {
        var context = CreateSsctlCommandRoutingContext();

        var verifyPipeName = $"ssctl-verify-profile-{Guid.NewGuid():N}";
        var verifyArguments = new List<string> { "verify", @"C:\captures\clip.mp4", "--profile", "flashback-export" };
        var (verifyExitCode, verifyRequest) = await CaptureSsctlRequestAsync(
                context,
                verifyPipeName,
                verifyArguments)
            .ConfigureAwait(false);

        AssertEqual(0, verifyExitCode, "verify profile exit code");
        AssertSsctlCommandRequest(
            verifyRequest,
            "VerifyFile",
            ("filePath", @"C:\captures\clip.mp4"),
            ("verificationProfile", "flashback-export"));

        var verifyLastPipeName = $"ssctl-verify-last-{Guid.NewGuid():N}";
        var verifyLastArguments = new List<string> { "verify" };
        var (verifyLastExitCode, verifyLastRequest) = await CaptureSsctlRequestAsync(
                context,
                verifyLastPipeName,
                verifyLastArguments)
            .ConfigureAwait(false);

        AssertEqual(0, verifyLastExitCode, "verify last exit code");
        AssertSsctlCommandRequestHasEmptyPayload(verifyLastRequest, "VerifyLastRecording");
    }

    internal static Task SsctlCommandHandlers_SourceOwnership_IsConsolidated()
    {
        AssertSsctlCommandRoutingTestsUseCommandIdHelper();
        var commandHandlersSource = ReadSsctlCommandHandlersFamilyText();
        var commandHandlersRootSource = ReadRepoFile("tools/ssctl/CommandHandlers.cs")
            .Replace("\r\n", "\n");

        AssertEqual(commandHandlersRootSource, commandHandlersSource, "ssctl command-handler source family is consolidated into CommandHandlers.cs");
        AssertContains(commandHandlersRootSource, "private sealed class CommandContext");
        AssertContains(commandHandlersRootSource, "Rest = arguments.Skip(1).ToList();");
        AssertContains(commandHandlersRootSource, "RequestCancellationToken = cancellationToken;");
        AssertContains(commandHandlersRootSource, "private static async Task<int> HandleSimpleCommandAsync(");
        AssertContains(commandHandlersRootSource, "context.SendCommandAsync(kind, payload)");
        AssertContains(commandHandlersRootSource, "private static int WriteResponse(JsonElement response, bool json, Func<JsonElement, string> formatter)");
        AssertContains(commandHandlersRootSource, "private static string JoinRemaining(IReadOnlyList<string> args, int startIndex)");
        AssertContains(commandHandlersRootSource, "private static bool ConsumeFlag(List<string> args, string flag)");
        AssertContains(commandHandlersRootSource, "private static bool LooksLikeJson(string value)");
        AssertContains(commandHandlersRootSource, "private static string PrettyJson<T>(T value)");
        AssertContains(commandHandlersRootSource, "private static object? ParseAssertionValue(string value)");

        AssertContains(commandHandlersRootSource, "// Observability command family.");
        AssertContains(commandHandlersRootSource, "HandleAudioRampTraceAsync");
        AssertContains(commandHandlersRootSource, "HandleDiagnosticSessionAsync");
        AssertContains(commandHandlersRootSource, "HandlePresentMonAsync");
        AssertContains(commandHandlersRootSource, "TryResolvePreviewPresentCorrelationAsync");
        AssertContains(commandHandlersRootSource, "PresentMonProbe.CreateOptions(");
        AssertContains(commandHandlersRootSource, "DiagnosticSessionRunner.RunAsync(");

        AssertContains(commandHandlersRootSource, "// CaptureControls command family.");
        AssertContains(commandHandlersRootSource, "HandleSetAsync");
        AssertSsctlCapturePipelineRoutingUsesAutomationCommandKinds();
        AssertContains(commandHandlersRootSource, "HandleDeviceAsync");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.RefreshDevices");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.GetCaptureOptions");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.SelectDevice");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.SelectAudioInputDevice");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.SetCustomAudioInput");

        AssertContains(commandHandlersRootSource, "// Window command family.");
        AssertContains(commandHandlersRootSource, "HandleWindowAsync");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.ArmClose");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.WindowAction");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.SetFullScreenEnabled");
        AssertContains(commandHandlersRootSource, "HandleRecordingsAsync");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.OpenRecordingsFolder");
        AssertContains(commandHandlersRootSource, "HandleStatsAsync");
        AssertContains(commandHandlersRootSource, "HandleSettingsAsync");
        AssertContains(commandHandlersRootSource, "HandleFrameTimeAsync");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.SetStatsVisible");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.SetStatsSectionVisible");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.SetSettingsVisible");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.SetFrameTimeOverlayVisible");

        AssertContains(commandHandlersRootSource, "HandleWaitAsync");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.WaitForCondition");
        AssertContains(commandHandlersRootSource, "Math.Max(timeoutMs.GetValueOrDefault(0) + 5000, 60000)");
        AssertContains(commandHandlersRootSource, "HandleAssertAsync");
        AssertContains(commandHandlersRootSource, "JsonDocument.Parse(assertionsJson)");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.AssertSnapshot");
        AssertContains(commandHandlersRootSource, "HandleProbeAsync");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.ProbeVideoSource");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.ProbePreviewColor");
        AssertContains(commandHandlersRootSource, "HandleVerifyAsync");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.VerifyFile");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.VerifyLastRecording");
        AssertContains(commandHandlersRootSource, "ConsumeFlag(context.Rest, \"--json\")");
        AssertContains(commandHandlersRootSource, "ParseOptionalStringFlag(context.Rest, \"--verification-profile\")");
        AssertContains(commandHandlersRootSource, "var actionId = Guid.NewGuid().ToString(\"N\");");
        AssertContains(commandHandlersRootSource, "[\"actionId\"] = actionId");

        AssertContains(commandHandlersRootSource, "// Flashback command family.");
        AssertContains(commandHandlersRootSource, "HandleFlashbackAsync");
        AssertContains(commandHandlersRootSource, "return HandleFlashbackActionAsync(context, subcommand);");
        AssertContains(commandHandlersRootSource, "return HandleFlashbackExportAsync(context);");
        AssertContains(commandHandlersRootSource, "private static Task<int> HandleFlashbackActionAsync(CommandContext context, string subcommand)");
        AssertContains(commandHandlersRootSource, "AutomationCommandKind.FlashbackAction");
        AssertContains(commandHandlersRootSource, "playPayload[\"positionMs\"] = ParseFlashbackPositionMs(context.Rest[1]);");
        AssertContains(commandHandlersRootSource, "[\"action\"] = \"begin-scrub\"");
        AssertContains(commandHandlersRootSource, "[\"action\"] = \"clear-in-out-points\"");
        AssertContains(commandHandlersRootSource, "private static double ParseFlashbackPositionMs(string value)");
        AssertContains(commandHandlersRootSource, "Flashback position must be finite, non-negative, and within TimeSpan range.");
        AssertContains(commandHandlersRootSource, "private static Task<int> HandleFlashbackExportAsync(CommandContext context)");
        AssertContains(commandHandlersRootSource, "ConsumeFlag(context.Rest, \"--range\")");
        AssertContains(commandHandlersRootSource, "ConsumeFlag(context.Rest, \"--force\")");
        AssertContains(commandHandlersRootSource, "? ParseFlashbackExportSeconds(context.Rest[1])");
        AssertContains(commandHandlersRootSource, "Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? \".\")");

        AssertContains(commandHandlersSource, "\"manifest\" => HandleManifestAsync(context)");
        AssertContains(commandHandlersSource, "\"audio-ramp-trace\" => HandleAudioRampTraceAsync(context)");
        AssertContains(commandHandlersSource, "\"recordings\" => HandleRecordingsAsync(context)");
        AssertSsctlFixedAutomationRoutesUseAutomationCommandKinds(commandHandlersSource);
        AssertContains(commandHandlersSource, "return HandleSimpleCommandAsync(context, Sussudio.Models.AutomationCommandKind.FlashbackAction, playPayload, includeData: true);");
        AssertContains(commandHandlersSource, "ParseOptionalStringFlag(context.Rest, \"--profile\")");
        AssertContains(commandHandlersSource, "payload[\"verificationProfile\"] = verificationProfile;");
        AssertContains(commandHandlersSource, "[\"positionMs\"] = ParseFlashbackPositionMs(RequireWord(context.Rest, 1, \"flashback seek <ms>\"))");
        AssertContains(commandHandlersSource, "[\"positionMs\"] = ParseFlashbackPositionMs(RequireWord(context.Rest, 1, \"flashback begin-scrub <ms>\"))");
        AssertContains(commandHandlersSource, "[\"positionMs\"] = ParseFlashbackPositionMs(RequireWord(context.Rest, 1, \"flashback update-scrub <ms>\"))");
        AssertContains(commandHandlersSource, "var payload = new Dictionary<string, object?> { [\"action\"] = \"end-scrub\" };");
        AssertContains(commandHandlersSource, "payload[\"positionMs\"] = ParseFlashbackPositionMs(context.Rest[1]);");
        AssertContains(commandHandlersSource, "private static double ParseFlashbackExportSeconds(string value)");
        AssertContains(commandHandlersSource, "Flashback export seconds must be finite, greater than zero, and within TimeSpan range.");
        AssertContains(commandHandlersSource, "assert <json> OR assert <field> <op> <value>");

        foreach (var removedFile in new[]
        {
            "CommandHandlers.Observability.cs",
            "CommandHandlers.CaptureControls.cs",
            "CommandHandlers.Window.cs",
            "CommandHandlers.Flashback.cs",
            "CommandHandlers.DiagnosticSession.cs",
            "CommandHandlers.PresentMon.cs",
            "CommandHandlers.Device.cs",
            "CommandHandlers.AutomationFlow.cs",
            "CommandHandlers.UiVisibility.cs",
            "CommandHandlers.Flashback.Actions.cs",
            "CommandHandlers.Parsing.cs",
            "CommandHandlers.Flags.cs",
            "CommandHandlers.Json.cs",
            "CommandHandlers.DeviceWindow.cs",
            "CommandHandlers.Context.cs",
            "CommandHandlers.Arguments.cs",
            "CommandHandlers.Values.cs",
            "CommandHandlers.Flashback.Export.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "tools", "ssctl", removedFile)),
                $"ssctl command-handler implementation stays consolidated in CommandHandlers.cs, not {removedFile}");
        }

        return Task.CompletedTask;
    }

    private static void AssertSsctlFixedAutomationRoutesUseAutomationCommandKinds(string commandHandlersSource)
    {
        AssertContains(
            commandHandlersSource,
            "(command, payload, responseTimeoutMs) => context.SendCommandAsync(command, payload, responseTimeoutMs)");
        AssertDoesNotContain(
            ReadRepoFile("tools/ssctl/CommandHandlers.cs"),
            "private static async Task<int> HandleSimpleCommandAsync(\n        CommandContext context,\n        string commandName,");

        foreach (var commandName in new[]
        {
            "GetSnapshot",
            "GetDiagnostics",
            "RefreshDevices",
            "GetCaptureOptions",
            "GetAutomationManifest",
            "GetAudioRampTrace",
            "GetPerformanceTimeline",
            "SelectDevice",
            "SelectAudioInputDevice",
            "SetCustomAudioInput",
            "ArmClose",
            "WindowAction",
            "SetFullScreenEnabled",
            "OpenRecordingsFolder",
            "WaitForCondition",
            "AssertSnapshot",
            "ProbeVideoSource",
            "ProbePreviewColor",
            "VerifyFile",
            "VerifyLastRecording",
            "SetFlashbackEnabled",
            "SetFlashbackTimelineVisible",
            "RestartFlashback",
            "FlashbackAction",
            "FlashbackExport",
            "FlashbackGetSegments"
        })
        {
            AssertContains(commandHandlersSource, $"AutomationCommandKind.{commandName}");
            AssertDoesNotContain(commandHandlersSource, $"SendCommandAsync(\"{commandName}\"");
            AssertDoesNotContain(commandHandlersSource, $"HandleSimpleCommandAsync(context, \"{commandName}\"");
        }
    }

    private static void AssertSsctlCapturePipelineRoutingUsesAutomationCommandKinds()
    {
        var rootSource = ReadRepoFile("tools/ssctl/CommandHandlers.cs");
        var captureControlsSource = rootSource;

        AssertContains(rootSource, "HandleCaptureAsync(context, AutomationCommandKind.CaptureWindowScreenshot");
        AssertContains(rootSource, "HandleCaptureAsync(context, AutomationCommandKind.CapturePreviewFrame");
        AssertDoesNotContain(rootSource, "HandleCaptureAsync(context, \"CaptureWindowScreenshot\"");
        AssertDoesNotContain(rootSource, "HandleCaptureAsync(context, \"CapturePreviewFrame\"");

        AssertContains(captureControlsSource, "HandleSimpleCommandAsync(\n            context,\n            AutomationCommandKind.SetPreviewEnabled,");
        AssertContains(captureControlsSource, "HandleSimpleCommandAsync(\n            context,\n            AutomationCommandKind.SetRecordingEnabled,");
        AssertContains(captureControlsSource, "private static Task<int> HandleCaptureAsync(CommandContext context, AutomationCommandKind kind, string defaultPath)");
        AssertContains(captureControlsSource, "HandleSimpleCommandAsync(\n            context,\n            kind,");

        foreach (var commandName in new[]
        {
            "SetResolution",
            "SetFrameRate",
            "SetRecordingFormat",
            "SetQuality",
            "SetCustomBitrate",
            "SetPreset",
            "SetSplitEncodeMode",
            "SetVideoFormat",
            "SetMjpegDecoderCount",
            "SetHdrEnabled",
            "SetTrueHdrPreviewEnabled",
            "SetAudioEnabled",
            "SetAudioPreviewEnabled",
            "SetPreviewVolume",
            "SetDeviceAudioMode",
            "SetAnalogAudioGain",
            "SetOutputPath",
            "SetMicrophoneEnabled"
        })
        {
            AssertContains(captureControlsSource, $"SendSetValueAsync(context, AutomationCommandKind.{commandName},");
            AssertDoesNotContain(captureControlsSource, $"SendSetValueAsync(context, \"{commandName}\"");
        }

        AssertContains(captureControlsSource, "private static Task<int> SendSetValueAsync(\n        CommandContext context,\n        AutomationCommandKind kind,");
        AssertContains(captureControlsSource, "HandleSimpleCommandAsync(\n            context,\n            kind,");
        AssertContains(rootSource, "context.SendCommandAsync(kind, payload)");
    }

    internal static Task SsctlHelp_UsesCatalogCliHelpForAutomationCommands()
    {
        var ssctlProgramText = ReadRepoFile("tools/ssctl/Program.cs")
            .Replace("\r\n", "\n");
        var helpWriterText = ssctlProgramText;
        var catalogEntriesText = ReadRepoFile("Sussudio.Automation.Contracts/AutomationCommandCatalog.cs")
            .Replace("\r\n", "\n");
        var flashbackHandlersText = ReadRepoFile("tools/ssctl/CommandHandlers.cs")
            .Replace("\r\n", "\n");
        var ssctlAssembly = LoadToolAssemblyIsolated(global::Program.SsctlAssemblyRelativePath);
        var helpWriterType = ssctlAssembly.GetType("Sussudio.Tools.Ssctl.SsctlHelpWriter")
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.SsctlHelpWriter type not found.");
        var diagnosticSessionOptionsType = ssctlAssembly.GetType("Sussudio.Tools.DiagnosticSessionOptions")
            ?? throw new InvalidOperationException("Sussudio.Tools.DiagnosticSessionOptions type not found.");
        var diagnosticSessionCliUsage = diagnosticSessionOptionsType
            .GetField("CliUsage", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?.GetValue(null) as string
            ?? throw new InvalidOperationException("DiagnosticSessionOptions.CliUsage field not found.");
        var writeHelp = RequireNonPublicStaticMethod(helpWriterType, "Write");
        using var writer = new StringWriter();
        writeHelp.Invoke(null, new object[] { writer });
        var helpOutput = writer.ToString().Replace("\r\n", "\n");

        AssertContains(catalogEntriesText, "\"flashback export [seconds] [path] [--range]\"");
        AssertContains(flashbackHandlersText, "ConsumeFlag(context.Rest, \"--force\")");
        AssertContains(ssctlProgramText, "SsctlHelpWriter.Write(Console.Out);");
        AssertContains(ssctlProgramText, "AutomationCommandCatalog.Get(kind).CliHelp");
        AssertContains(ssctlProgramText, "WriteCatalogHelpLine");
        AssertContains(helpWriterText, "internal static class SsctlHelpWriter");
        AssertContains(helpWriterText, "WriteHeader(writer);");
        AssertContains(helpWriterText, "WriteFlashbackSection(writer);");
        AssertContains(helpWriterText, "WriteFlagsSection(writer);");
        AssertContains(helpWriterText, "AutomationCommandCatalog.Get(kind).CliHelp");
        AssertContains(helpWriterText, "private static void WriteCatalogHelpLine(TextWriter writer, AutomationCommandKind kind, string? suffix = null)");
        AssertContains(helpWriterText, "private static void WriteFlashbackSection(TextWriter writer)");
        AssertContains(helpWriterText, "private static void WriteWaitVerifySection(TextWriter writer)");
        AssertContains(helpWriterText, "WriteCatalogHelpLine(writer, AutomationCommandKind.FlashbackExport);");
        AssertContains(helpWriterText, "WriteCatalogHelpLine(writer, AutomationCommandKind.FlashbackGetSegments);");
        AssertContains(helpWriterText, "WriteCatalogHelpLine(writer, AutomationCommandKind.SetFrameTimeOverlayVisible);");
        AssertContains(helpWriterText, "WriteCatalogHelpLine(writer, AutomationCommandKind.SetFlashbackTimelineVisible);");
        AssertContains(helpWriterText, "DiagnosticSessionOptions.CliUsage");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "ssctl", "SsctlHelpWriter.cs")),
            "ssctl help facade folded into the CLI front-door file");
        AssertContains(helpOutput, "ssctl");
        AssertContains(helpOutput, "Usage:");
        AssertContains(helpOutput, "Flashback:");
        AssertContains(helpOutput, "Flags:");
        AssertEqual(BuildExpectedSsctlHelpOutput(diagnosticSessionCliUsage), helpOutput, "full ssctl help output");
        AssertContains(helpOutput, $"  {AutomationCommandCatalog.Get(AutomationCommandKind.FlashbackExport).CliHelp}");
        AssertContains(helpOutput, $"  {AutomationCommandCatalog.Get(AutomationCommandKind.FlashbackGetSegments).CliHelp}");
        AssertContains(helpOutput, $"  {AutomationCommandCatalog.Get(AutomationCommandKind.SetFrameTimeOverlayVisible).CliHelp}");
        AssertContains(helpOutput, $"  {AutomationCommandCatalog.Get(AutomationCommandKind.SetFlashbackTimelineVisible).CliHelp}");

        AssertEqual("flashback export [seconds] [path] [--range]",
            AutomationCommandCatalog.Get(AutomationCommandKind.FlashbackExport).CliHelp,
            "catalog Flashback export CLI help");
        AssertEqual("flashback segments",
            AutomationCommandCatalog.Get(AutomationCommandKind.FlashbackGetSegments).CliHelp,
            "catalog Flashback segments CLI help");

        return Task.CompletedTask;
    }

    private static string BuildExpectedSsctlHelpOutput(string diagnosticSessionCliUsage)
    {
        static string HelpLine(AutomationCommandKind kind, string? suffix = null)
        {
            var command = AutomationCommandCatalog.Get(kind).CliHelp;
            return string.IsNullOrWhiteSpace(suffix)
                ? $"  {command}"
                : $"  {command} {suffix}";
        }

        var lines = new[]
        {
            "ssctl",
            "Usage:",
            "  ssctl [--json] [--pipe NAME] [--timeout MS] <command>",
            "",
            "Query:",
            HelpLine(AutomationCommandKind.GetSnapshot, "[--json]"),
            HelpLine(AutomationCommandKind.GetDiagnostics, "[--json]"),
            HelpLine(AutomationCommandKind.GetCaptureOptions, "[--json]"),
            HelpLine(AutomationCommandKind.GetAutomationManifest, "[--json]"),
            HelpLine(AutomationCommandKind.GetPerformanceTimeline, "[--json]"),
            "  memory [--json]",
            HelpLine(AutomationCommandKind.GetAudioRampTrace, "[--json]"),
            "  presentmon [--seconds N] [--pid PID|--process NAME] [--swapchain HEX] [--app-present-id N] [--app-source-seq N] [--app-present-utc-ms N] [--capture-start-utc-ms N] [--presentmon PATH] [--output PATH] [--keep-csv] [--json]",
            $"  {diagnosticSessionCliUsage}",
            "",
            "Control:",
            HelpLine(AutomationCommandKind.SetPreviewEnabled),
            HelpLine(AutomationCommandKind.SetRecordingEnabled),
            HelpLine(AutomationCommandKind.CaptureWindowScreenshot),
            HelpLine(AutomationCommandKind.CapturePreviewFrame),
            HelpLine(AutomationCommandKind.OpenRecordingsFolder),
            "",
            "Configure:",
            HelpLine(AutomationCommandKind.SetResolution),
            HelpLine(AutomationCommandKind.SetFrameRate),
            HelpLine(AutomationCommandKind.SetRecordingFormat),
            HelpLine(AutomationCommandKind.SetQuality),
            HelpLine(AutomationCommandKind.SetCustomBitrate),
            HelpLine(AutomationCommandKind.SetPreset),
            HelpLine(AutomationCommandKind.SetSplitEncodeMode),
            HelpLine(AutomationCommandKind.SetVideoFormat),
            HelpLine(AutomationCommandKind.SetMjpegDecoderCount),
            HelpLine(AutomationCommandKind.SetHdrEnabled),
            HelpLine(AutomationCommandKind.SetTrueHdrPreviewEnabled),
            HelpLine(AutomationCommandKind.SetAudioEnabled),
            HelpLine(AutomationCommandKind.SetAudioPreviewEnabled),
            HelpLine(AutomationCommandKind.SetPreviewVolume),
            HelpLine(AutomationCommandKind.SetDeviceAudioMode),
            HelpLine(AutomationCommandKind.SetAnalogAudioGain),
            HelpLine(AutomationCommandKind.SetOutputPath),
            HelpLine(AutomationCommandKind.SetShowAllCaptureOptions),
            HelpLine(AutomationCommandKind.SetMicrophoneEnabled),
            "",
            "Device:",
            HelpLine(AutomationCommandKind.RefreshDevices),
            "  device list",
            HelpLine(AutomationCommandKind.SelectDevice),
            HelpLine(AutomationCommandKind.SelectAudioInputDevice),
            HelpLine(AutomationCommandKind.SetCustomAudioInput),
            "",
            "Flashback:",
            HelpLine(AutomationCommandKind.SetFlashbackEnabled),
            HelpLine(AutomationCommandKind.SetFlashbackTimelineVisible),
            "  flashback play [<ms>]",
            "  flashback pause",
            "  flashback go-live",
            "  flashback seek <ms>",
            "  flashback begin-scrub <ms>",
            "  flashback update-scrub <ms>",
            "  flashback end-scrub [<ms>]",
            "  flashback set-in|set-out|clear-range",
            HelpLine(AutomationCommandKind.FlashbackExport),
            HelpLine(AutomationCommandKind.FlashbackGetSegments),
            HelpLine(AutomationCommandKind.RestartFlashback),
            "",
            "Window:",
            "  window close|minimize|maximize|restore|center",
            HelpLine(AutomationCommandKind.SetFullScreenEnabled),
            "  window snap left|right|top-left|top-right|bottom-left|bottom-right",
            "  window move <x> <y>",
            "  window resize <w> <h>",
            "",
            "Wait / Verify:",
            HelpLine(AutomationCommandKind.WaitForCondition, "[--timeout MS] [--poll MS]"),
            "  verify [path] [--profile NAME|--verification-profile NAME]",
            "  assert <json>|<field> <op> <value>",
            "  probe source|color",
            HelpLine(AutomationCommandKind.SetStatsVisible),
            HelpLine(AutomationCommandKind.SetStatsSectionVisible),
            HelpLine(AutomationCommandKind.SetFrameTimeOverlayVisible),
            HelpLine(AutomationCommandKind.SetSettingsVisible),
            "",
            "Flags:",
            "  --json            Print raw JSON responses where supported",
            "  --pipe NAME       Named pipe (default: SussudioAutomation)",
            "  --timeout MS      Response timeout override for pipe calls",
            "  --verbose         On error, print full stack trace + InnerException chain to stderr",
            "  --help            Show this help",
            "",
        };

        return string.Join('\n', lines);
    }

    private readonly record struct SsctlCommandRoutingContext(Type TransportType, MethodInfo ExecuteAsync);

    private static SsctlCommandRoutingContext CreateSsctlCommandRoutingContext()
    {
        var assemblyPath = global::Program.SsctlAssemblyRelativePath;
        var ssctlAssembly = LoadToolAssemblyIsolated(assemblyPath);
        var transportType = ssctlAssembly.GetType("Sussudio.Tools.Ssctl.PipeTransport")
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.PipeTransport type not found.");
        var commandHandlersType = ssctlAssembly.GetType("Sussudio.Tools.Ssctl.CommandHandlers")
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.CommandHandlers type not found.");
        var executeAsync = commandHandlersType.GetMethod("ExecuteAsync", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.CommandHandlers.ExecuteAsync not found.");

        return new SsctlCommandRoutingContext(transportType, executeAsync);
    }

    private static object CreateSsctlTransport(SsctlCommandRoutingContext context, string pipeName)
        => Activator.CreateInstance(context.TransportType, pipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for ssctl command-handler routing test.");

    private static async Task<(int ExitCode, JsonElement Request)> CaptureSsctlRequestAsync(
        SsctlCommandRoutingContext context,
        string pipeName,
        List<string> arguments)
    {
        var transport = CreateSsctlTransport(context, pipeName);
        var exitCode = -1;
        JsonElement request = await CapturePipeRequestAsync(
                pipeName,
                async () =>
                {
                    var task = context.ExecuteAsync.Invoke(null, new object?[] { transport, arguments, false, CancellationToken.None }) as Task<int>
                        ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                    exitCode = await task.ConfigureAwait(false);
                })
            .ConfigureAwait(false);

        return (exitCode, request);
    }

    private static async Task<(int ExitCode, JsonElement[] Requests)> CaptureSsctlRequestsAsync(
        SsctlCommandRoutingContext context,
        string pipeName,
        int expectedCount,
        List<string> arguments,
        Func<int, string>? responseFactory = null)
    {
        var transport = CreateSsctlTransport(context, pipeName);
        var exitCode = -1;
        JsonElement[] requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount,
                async () =>
                {
                    var task = context.ExecuteAsync.Invoke(null, new object?[] { transport, arguments, false, CancellationToken.None }) as Task<int>
                        ?? throw new InvalidOperationException("CommandHandlers.ExecuteAsync did not return Task<int>.");
                    exitCode = await task.ConfigureAwait(false);
                },
                responseFactory)
            .ConfigureAwait(false);

        return (exitCode, requests);
    }

    private static JsonElement AssertSsctlCommandRequest(
        JsonElement request,
        string commandName,
        params (string Key, object? Value)[] expectedPayload)
    {
        AssertAutomationCommandId(request, commandName);
        var payload = request.GetProperty("payload");
        if (expectedPayload.Length == 0)
        {
            return payload;
        }

        AssertJsonObjectPropertyNames(payload, expectedPayload.Select(item => item.Key).ToArray());
        foreach (var (key, value) in expectedPayload)
        {
            AssertJsonPropertyEquals(payload, key, value, $"{commandName}.{key}");
        }

        return payload;
    }

    private static void AssertSsctlCommandRequestHasEmptyPayload(JsonElement request, string commandName)
    {
        var payload = AssertSsctlCommandRequest(request, commandName);
        if (payload.ValueKind == JsonValueKind.Object && payload.EnumerateObject().Any())
        {
            throw new InvalidOperationException($"{commandName} payload contained unexpected properties.");
        }

        if (payload.ValueKind is not JsonValueKind.Null and not JsonValueKind.Object)
        {
            throw new InvalidOperationException($"{commandName} payload had unexpected kind {payload.ValueKind}.");
        }
    }

    private static void AssertSsctlCommandRoutingTestsUseCommandIdHelper()
    {
        var repoRoot = GetRepoRoot();
        var testRoot = System.IO.Path.Combine(repoRoot, "tests", "Sussudio.Tests");
        foreach (var file in System.IO.Directory.GetFiles(testRoot, "CommandHandlers.Routing*.Tests.cs"))
        {
            var relativePath = System.IO.Path.GetRelativePath(repoRoot, file).Replace('\\', '/');
            var text = System.IO.File.ReadAllText(file).Replace("\r\n", "\n");
            var directCommandReadToken = "GetProperty(\"command\")" + ".GetInt32()";
            if (text.Contains(directCommandReadToken, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"{relativePath} must use AssertSsctlCommandRequest for captured request.command checks.");
            }

            var commandValueBypassToken = "GetExpectedAutomationCommand" + "Value(";
            if (text.Contains(commandValueBypassToken, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"{relativePath} must not bypass AssertSsctlCommandRequest.");
            }
        }
    }

    private static string ReadSsctlCommandHandlersFamilyText()
    {
        var files = new[]
        {
            "tools/ssctl/CommandHandlers.cs",
        };

        return string.Join(
            "\n",
            files.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));
    }

    // Diagnostic-session backing methods live with the tool xUnit wrappers.
    private static string ReadDiagnosticSessionBackgroundTasksSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionRunner.cs");

    private static string ReadDiagnosticSessionCleanupActionsSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionRunner.cs");

    private static string ReadDiagnosticSessionScenarioSetupSource()
        => ReadDiagnosticSessionScenarioStartupSource();

    private static string ReadDiagnosticSessionFlashbackCycleScenariosSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackCycleScenarios.cs");

    private static string ReadDiagnosticSessionFlashbackExportScenariosSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackExportScenarios.cs");

    private static string ReadDiagnosticSessionFlashbackLifecycleScenariosSource()
        => ReadDiagnosticSessionFlashbackCycleScenariosSource();

    private static string ReadDiagnosticSessionFlashbackMetricsSource()
        => ReadDiagnosticSessionMetricsSource();

    private static string ReadDiagnosticSessionFlashbackPreviewCycleScenariosSource()
        => ReadDiagnosticSessionFlashbackCycleScenariosSource();

    private static string ReadDiagnosticSessionFlashbackRecordingSettingsScenariosSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackScenarioTasks.cs");

    private static string ReadDiagnosticSessionFlashbackSegmentPlaybackScenariosSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackScenarioTasks.cs");

    private static string ReadDiagnosticSessionFlashbackSegmentsSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackSupport.cs");

    private static string ReadDiagnosticSessionFlashbackStressScenarioSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackStressScenario.cs");

    private static string ReadDiagnosticSessionFlashbackWaitsSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionFlashbackSupport.cs");

    private static string ReadDiagnosticSessionMetricsSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionMetrics.cs");

    private static string ReadDiagnosticSessionModelsSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionResult.cs");

    private static string ReadDiagnosticSessionResultBuilderSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionResultBuilder.cs");

    private static string ReadDiagnosticSessionResultFormatterSource()
        => ReadDiagnosticSessionModelsSource();

    private static string ReadDiagnosticSessionRunnerSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionRunner.cs",
            "tools/Common/DiagnosticSessionRunContext.cs");

    private static string ReadDiagnosticSessionRunExecutionRootSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunner.cs");

    private static string ReadDiagnosticSessionRunContextSource()
        => ReadNormalizedRepoFile("tools/Common/DiagnosticSessionRunContext.cs");

    private static string ReadDiagnosticSessionRunContextRootSource()
        => ReadDiagnosticSessionRunContextSource();

    private static string ReadDiagnosticSessionRunExecutionScenarioSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionRunner.cs",
            "tools/Common/DiagnosticSessionResult.cs");

    private static string ReadDiagnosticSessionRunExecutionCompletionSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionRunner.cs");

    private static string ReadDiagnosticSessionRunExecutionCompletionRootSource()
        => ReadDiagnosticSessionRunExecutionRootSource();

    private static string ReadDiagnosticSessionRunExecutionCompletionContextSource()
        => ReadDiagnosticSessionRunExecutionRootSource();

    private static string ReadDiagnosticSessionScenarioStartupSource()
        => ReadNormalizedSourceFiles(
            "tools/Common/DiagnosticSessionScenarioCatalog.cs");

    private static string ReadNormalizedSourceFiles(params string[] paths)
    {
        var parts = new string[paths.Length];
        for (var i = 0; i < paths.Length; i++)
        {
            parts[i] = ReadNormalizedRepoFile(paths[i]);
        }

        return string.Join("\n", parts);
    }

    internal static Task DiagnosticSessionHealthPolicy_OwnsHealthTolerances()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var policyText = ReadRepoFile("tools/Common/DiagnosticSessionHealthPolicy.cs")
            .Replace("\r\n", "\n");

        AssertContains(policyText, "internal static class DiagnosticSessionHealthPolicy");
        AssertContains(policyText, "internal readonly record struct DiagnosticHealthObservation");
        AssertContains(policyText, "internal static DiagnosticHealthObservation BuildSessionDiagnosticHealthObservation(");
        AssertContains(policyText, "private static DiagnosticHealthObservation BuildWorstDiagnosticHealthObservationAfterOffset(");
        AssertContains(policyText, "private const double FlashbackDiagnosticWarmupFraction = 0.20;");
        AssertContains(policyText, "internal static bool IsSourceSignalDiagnosticHealthObservation(");
        AssertContains(policyText, "internal static bool IsSourceCaptureDiagnosticHealthObservation(");
        AssertContains(policyText, "internal static bool IsPreviewSchedulerDiagnosticHealthObservation(");
        AssertContains(policyText, "internal static bool IsFlashbackForceRotateDrainDiagnosticHealthObservation(");
        AssertContains(policyText, "internal static bool IsSparseSourceCaptureCadenceWarningRun(");
        AssertContains(policyText, "internal static bool IsSparsePreviewSchedulerDeadlineDropRun(");
        AssertContains(policyText, "internal static bool IsSparsePreviewSchedulerStressRun(");
        AssertContains(policyText, "internal static bool IsToleratedFlashbackScenarioWarning(");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionHealthPolicy;");
        AssertDoesNotContain(builderText, "using static Sussudio.Tools.DiagnosticSessionHealthTolerances;");
        AssertDoesNotContain(runnerText, "private readonly record struct DiagnosticHealthObservation");
        AssertDoesNotContain(runnerText, "private static DiagnosticHealthObservation BuildSessionDiagnosticHealthObservation(");
        AssertDoesNotContain(runnerText, "private static bool IsSparseSourceCaptureCadenceWarningRun(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionHealthTolerances.cs")),
            "diagnostic-session health tolerance classifiers folded into the health policy owner");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionScenarioPlan_OwnsScenarioFlags()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var bootstrapText = ReadDiagnosticSessionRunContextSource();
        var catalogText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioCatalog.cs")
            .Replace("\r\n", "\n");

        AssertContains(catalogText, "internal static class DiagnosticSessionScenarioCatalog");
        AssertDoesNotContain(catalogText, "internal static partial class DiagnosticSessionScenarioCatalog");
        AssertContains(catalogText, "TryGetEntry(normalized, out _)");
        AssertContains(catalogText, "internal const string HelpList =");
        AssertContains(catalogText, "internal const string Description =");
        AssertContains(catalogText, "internal static IReadOnlyList<string> Names => Entries.Select");
        AssertContains(catalogText, "TryGetEntry(scenario, out var entry) && entry.RequiresPreview");
        AssertContains(catalogText, "entry.FlashbackExportVerificationFileName");
        AssertContains(catalogText, "internal static IReadOnlyList<DiagnosticSessionScenarioCatalogEntry> Entries { get; }");
        AssertContains(catalogText, ".. CreateCoreScenarioEntries(),");
        AssertContains(catalogText, ".. CreateFlashbackPlaybackScenarioEntries(),");
        AssertContains(catalogText, ".. CreateFlashbackExportScenarioEntries(),");
        AssertContains(catalogText, ".. CreateFlashbackRecordingScenarioEntries(),");
        AssertContains(catalogText, "CreateCombinedScenarioEntry()");
        AssertContains(catalogText, "internal readonly record struct DiagnosticSessionScenarioCatalogEntry(");
        AssertContains(catalogText, "private static DiagnosticSessionScenarioCatalogEntry[] CreateCoreScenarioEntries()");
        AssertContains(catalogText, "new(Observe)");
        AssertContains(catalogText, "FlashbackExportVerificationFileName: \"flashback-stress-export.mp4\"");
        AssertContains(catalogText, "private static DiagnosticSessionScenarioCatalogEntry[] CreateFlashbackPlaybackScenarioEntries()");
        AssertContains(catalogText, "FlashbackPlayback,");
        AssertContains(catalogText, "DiagnosticSessionScenarioPlan.Create(runFlashbackPlayback: true)");
        AssertContains(catalogText, "DiagnosticSessionScenarioPlan.Create(runFlashbackSegmentPlayback: true)");
        AssertContains(catalogText, "private static DiagnosticSessionScenarioCatalogEntry[] CreateFlashbackExportScenarioEntries()");
        AssertContains(catalogText, "FlashbackExportVerificationFileName: \"flashback-range-export.mp4\"");
        AssertContains(catalogText, "DiagnosticSessionScenarioPlan.Create(runFlashbackPlaybackPreviewCycle: true)");
        AssertContains(catalogText, "private static DiagnosticSessionScenarioCatalogEntry[] CreateFlashbackRecordingScenarioEntries()");
        AssertContains(catalogText, "RequiresRecording: true");
        AssertContains(catalogText, "DiagnosticSessionScenarioPlan.Create(runFlashbackExportRejected: true)");
        AssertContains(catalogText, "private static DiagnosticSessionScenarioCatalogEntry CreateCombinedScenarioEntry()");
        AssertContains(catalogText, "DiagnosticSessionScenarioPlan.Create(runCombined: true)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionScenarioCatalog.Entries.cs")),
            "Diagnostic session scenario entries folded into the catalog owner");
        AssertContains(catalogText, "internal readonly record struct DiagnosticSessionScenarioPlan(");
        AssertContains(catalogText, "internal static DiagnosticSessionScenarioPlan Create(");
        AssertContains(catalogText, "internal static DiagnosticSessionScenarioPlan From(string scenario)");
        AssertContains(catalogText, "DiagnosticSessionScenarioCatalog.TryGetEntry(scenario, out var entry)");
        AssertContains(catalogText, "? entry.Plan");
        AssertContains(catalogText, "internal bool RequiresFlashbackRecordingReadiness");
        AssertContains(catalogText, "internal bool UsesFlashbackScenarioWarningPolicy");
        AssertContains(catalogText, "internal bool ToleratesSourceSignalHealthWarning");
        AssertContains(catalogText, "internal bool ToleratesFlashbackForceRotateDrainWarning");
        AssertContains(catalogText, "internal bool IsPreviewCycleScenario");
        AssertContains(catalogText, "internal bool ToleratesSparsePreviewSchedulerStressTransitions");
        AssertContains(catalogText, "RunFlashbackSegmentPlayback");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionScenarioPlan.cs")),
            "Diagnostic session scenario plan flags live with the catalog that constructs every plan");
        AssertContains(bootstrapText, "var scenarioPlan = DiagnosticSessionScenarioPlan.From(scenario);");
        AssertContains(runnerText, "ScenarioPlan = RunBootstrap.ScenarioPlan;");
        AssertDoesNotContain(runnerText, "scenario == \"flashback-playback\"");
        AssertDoesNotContain(runnerText, "scenario == \"flashback-stress\"");
        AssertDoesNotContain(runnerText, "scenario == \"combined\"");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionScenarioSetup_OwnsInitialMutations()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var setupText = ReadDiagnosticSessionScenarioSetupSource();
        AssertContains(setupText, "internal static class DiagnosticSessionScenarioSetup");
        AssertContains(setupText, "internal static async Task<DiagnosticSessionScenarioSetupResult> RunAsync(");
        AssertContains(setupText, "SetupFlashbackStateAsync(");
        AssertContains(setupText, "StartPreviewIfNeededAsync(");
        AssertContains(setupText, "StartRecordingIfNeededAsync(");
        AssertContains(setupText, "internal readonly record struct DiagnosticSessionScenarioSetupResult(");
        AssertContains(setupText, "private readonly record struct DiagnosticSessionFlashbackSetupResult(");
        AssertContains(setupText, "DiagnosticSessionCommandChannel commandChannel,");
        AssertContains(setupText, "DiagnosticSessionScenarioCatalog.NeedsFlashback(scenario)");
        AssertContains(setupText, "scenarioPlan.RunFlashbackExportRejected");
        AssertContains(setupText, "AutomationCommandKind.SetFlashbackEnabled,");
        AssertContains(setupText, "actions.Add(\"flashback enabled\")");
        AssertContains(setupText, "actions.Add(\"flashback disabled for rejected export\")");
        AssertContains(setupText, "DiagnosticSessionScenarioCatalog.NeedsPreview(scenario)");
        AssertContains(setupText, "AutomationCommandKind.SetPreviewEnabled,");
        AssertContains(setupText, "actions.Add(\"preview started\")");
        AssertContains(setupText, "await tryWaitAsync(\"VideoFramesFlowing\", 15_000)");
        AssertContains(setupText, "DiagnosticSessionScenarioCatalog.NeedsRecording(scenario)");
        AssertContains(setupText, "WaitForFlashbackStressBufferReadyAsync(SendByNameAsync, cancellationToken)");
        AssertContains(setupText, "AutomationCommandKind.SetRecordingEnabled,");
        AssertContains(setupText, "actions.Add(\"recording started\")");
        AssertContains(setupText, "await tryWaitAsync(\"RecordingFileGrowing\", 20_000)");
        AssertContains(runnerText, "DiagnosticSessionScenarioSetup.RunAsync(");
        AssertContains(runnerText, "context.CommandChannel,");
        AssertContains(runnerText, "startedPreview = setupResult.StartedPreview;");
        AssertContains(runnerText, "startedRecording = setupResult.StartedRecording;");
        AssertContains(runnerText, "enabledFlashback = setupResult.EnabledFlashback;");
        AssertContains(runnerText, "disabledFlashback = setupResult.DisabledFlashback;");
        AssertDoesNotContain(runnerText, "actions.Add(\"flashback enabled\")");
        AssertDoesNotContain(runnerText, "actions.Add(\"preview started\")");
        AssertDoesNotContain(runnerText, "actions.Add(\"recording started\")");
        AssertDoesNotContain(runnerText, "WaitForFlashbackStressBufferReadyAsync(");
        AssertDoesNotContain(setupText, "sendAsync(\"SetFlashbackEnabled\"");
        AssertDoesNotContain(setupText, "sendAsync(\"SetPreviewEnabled\"");
        AssertDoesNotContain(setupText, "sendAsync(\"SetRecordingEnabled\"");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionScenarioActivation.cs")),
            "diagnostic-session scenario setup/startup activation folded into the scenario catalog owner");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionBackgroundTasks_OwnTaskDraining()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var cycleScenariosText = ReadDiagnosticSessionFlashbackCycleScenariosSource();
        var exportScenariosText = ReadDiagnosticSessionFlashbackExportScenariosSource();
        var stressScenariosText = ReadDiagnosticSessionFlashbackStressScenarioSource();
        var segmentPlaybackScenariosText = ReadDiagnosticSessionFlashbackSegmentPlaybackScenariosSource();
        var previewCycleScenariosText = ReadDiagnosticSessionFlashbackPreviewCycleScenariosSource();
        var presentMonStartupText = startupText;
        var tasksText = ReadDiagnosticSessionBackgroundTasksSource();

        AssertContains(startupText, "internal static class DiagnosticSessionScenarioStartup");
        AssertDoesNotContain(startupText, "internal static partial class DiagnosticSessionScenarioStartup");
        AssertContains(startupText, "internal static async Task<DiagnosticSessionScenarioStartupResult> StartAsync(");
        AssertContains(startupText, "internal readonly record struct DiagnosticSessionScenarioStartupResult(bool StartedFlashbackPlayback)");
        AssertContains(tasksText, "internal sealed class DiagnosticSessionBackgroundTasks");
        AssertDoesNotContain(tasksText, "internal sealed partial class DiagnosticSessionBackgroundTasks");
        AssertContains(tasksText, "internal readonly record struct DiagnosticSessionBackgroundTaskRegistration(");
        AssertContains(tasksText, "internal readonly record struct DiagnosticSessionBackgroundTaskDrainResult(");
        AssertContains(tasksText, "private readonly List<DiagnosticSessionBackgroundTaskRegistration> _scenarioTasks = [];");
        AssertContains(tasksText, "private Task<PresentMonProbeResult>? _presentMonTask;");
        AssertContains(tasksText, "private Task<FlashbackRecordingSettingsDeferredPresetState>? _recordingSettingsDeferredTask;");
        AssertContains(tasksText, "internal void AddScenario(int awaitOrder, string stage, Task task)");
        AssertContains(tasksText, "internal void SetPresentMon(Task<PresentMonProbeResult> task)");
        AssertContains(tasksText, "internal void SetRecordingSettingsDeferred(Task<FlashbackRecordingSettingsDeferredPresetState> task)");
        AssertContains(tasksText, "internal async Task<FlashbackRecordingSettingsDeferredPresetState> CompleteRegisteredScenarioWorkAsync(");
        AssertContains(tasksText, "internal async Task<PresentMonProbeResult?> CompletePresentMonAsync(");
        AssertContains(tasksText, "internal async Task<DiagnosticSessionBackgroundTaskDrainResult> ObserveAfterFaultAsync(");
        AssertContains(tasksText, "private async Task AwaitScenarioTasksAsync()");
        AssertContains(tasksText, "_scenarioTasks.OrderBy(task => task.AwaitOrder)");
        AssertContains(tasksText, "private async Task<PresentMonProbeResult?> AwaitPresentMonAsync(");
        AssertContains(tasksText, "private async Task<PresentMonProbeResult?> ObservePresentMonAfterFaultAsync(");
        AssertContains(tasksText, "presentmon-task: task still running after diagnostic interruption");
        AssertContains(tasksText, "private async Task<FlashbackRecordingSettingsDeferredPresetState> AwaitRecordingSettingsDeferredAsync(");
        AssertContains(tasksText, "private async Task<FlashbackRecordingSettingsDeferredPresetState> ObserveRecordingSettingsDeferredAfterFaultAsync(");
        AssertContains(tasksText, "flashback-recording-settings-deferred-task: task still running after diagnostic interruption");
        AssertContains(tasksText, "ObservePresentMonAfterFaultAsync(");
        AssertContains(tasksText, "ObserveRecordingSettingsDeferredAfterFaultAsync(");
        AssertContains(tasksText, "private static async Task ObserveTaskAfterFaultAsync(");
        AssertContains(runnerText, "var backgroundTasks = new DiagnosticSessionBackgroundTasks();");
        AssertContains(startupText, "private static void RegisterFlashbackScenarioTasks(");
        AssertContains(startupText, "private static void RegisterDeferredFlashbackRecordingSettingsTask(");
        AssertContains(startupText, "private static async Task<bool> TryStartFlashbackPlaybackAsync(");
        AssertDoesNotContain(startupText, "backgroundTasks.AddScenario(");
        AssertContains(startupText, "StartPresentMonAsync(");
        AssertContains(presentMonStartupText, "backgroundTasks.SetPresentMon(");
        AssertContains(startupText, "backgroundTasks.SetRecordingSettingsDeferred(");
        AssertContains(startupText, "RunFlashbackRecordingSettingsDeferredAsync(");
        AssertContains(startupText, "actions.Add(\"flashback recording settings deferred started\")");
        AssertContains(startupText, "DiagnosticSessionFlashbackCycleScenarios.RegisterSelectedFlashbackCycleScenarioTasks(");
        AssertContains(startupText, "DiagnosticSessionFlashbackStressScenario.RegisterSelectedFlashbackStressScenarioTasks(");
        AssertContains(startupText, "DiagnosticSessionFlashbackSegmentPlaybackScenarios.RegisterSelectedFlashbackSegmentPlaybackScenarioTask(");
        AssertContains(startupText, "DiagnosticSessionFlashbackExportScenarios.RegisterSelectedFlashbackExportScenarioTasks(");
        AssertContains(startupText, "DiagnosticSessionFlashbackPreviewCycleScenarios.RegisterSelectedFlashbackPreviewCycleScenarioTasks(");
        AssertDoesNotContain(startupText, "RunFlashbackStressAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackScrubStressAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackSegmentPlaybackAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackRestartCycleAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackEncoderCycleAsync(");
        AssertContains(cycleScenariosText, "internal static void RegisterSelectedFlashbackCycleScenarioTasks(");
        AssertContains(stressScenariosText, "internal static void RegisterSelectedFlashbackStressScenarioTasks(");
        AssertContains(stressScenariosText, "backgroundTasks.AddScenario(");
        AssertContains(segmentPlaybackScenariosText, "internal static void RegisterSelectedFlashbackSegmentPlaybackScenarioTask(");
        AssertContains(segmentPlaybackScenariosText, "backgroundTasks.AddScenario(");
        AssertDoesNotContain(startupText, "RegisterFlashbackExportPlaybackTask(");
        AssertDoesNotContain(startupText, "RegisterFlashbackRangeExportTasks(");
        AssertDoesNotContain(startupText, "RegisterFlashbackExportCoordinationTasks(");
        AssertDoesNotContain(startupText, "RunFlashbackPreviewCycleAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackPlaybackPreviewCycleAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackRecordingPreviewCycleAsync(");
        AssertContains(exportScenariosText, "internal static void RegisterSelectedFlashbackExportScenarioTasks(");
        AssertContains(exportScenariosText, "private static void RegisterFlashbackExportPlaybackTask(");
        AssertContains(exportScenariosText, "private static void RegisterFlashbackRangeExportTasks(");
        AssertContains(exportScenariosText, "private static void RegisterFlashbackExportCoordinationTasks(");
        AssertContains(exportScenariosText, "RunFlashbackExportPlaybackAsync(");
        AssertContains(exportScenariosText, "RunFlashbackRangeExportAsync(");
        AssertContains(exportScenariosText, "RunFlashbackExportConcurrentAsync(");
        AssertContains(exportScenariosText, "RunFlashbackDisableDuringExportAsync(");
        AssertContains(exportScenariosText, "RunFlashbackRotatedExportAsync(");
        AssertContains(previewCycleScenariosText, "internal static void RegisterSelectedFlashbackPreviewCycleScenarioTasks(");
        AssertDoesNotContain(startupText, "RunFlashbackRangeExportAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackExportConcurrentAsync(");
        AssertContains(runnerText, "DiagnosticSessionScenarioStartup.StartAsync(");
        AssertContains(runnerText, "startedFlashbackPlayback = scenarioStartup.StartedFlashbackPlayback;");
        AssertContains(runnerText, ".CompleteRegisteredScenarioWorkAsync(");
        AssertContains(runnerText, "backgroundTasks.CompletePresentMonAsync(");
        AssertContains(runnerText, "backgroundTasks.ObserveAfterFaultAsync(");
        AssertDoesNotContain(runnerText, "Task? flashbackStressTask");
        AssertDoesNotContain(runnerText, "Task<PresentMonProbeResult>? presentMonTask");
        AssertDoesNotContain(runnerText, "async Task ObserveBackgroundTasksAfterFaultAsync()");
        AssertDoesNotContain(runnerText, "async Task ObserveTaskAfterFaultAsync(Task? task, string stage)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionBackgroundTasks.cs")),
            "diagnostic-session background task drain folded into DiagnosticSessionRunner.cs");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionPresentMonStartup_OwnsPresentMonLaunch()
    {
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var presentMonStartupText = startupText;

        AssertContains(presentMonStartupText, "private static async Task StartPresentMonAsync(");
        AssertContains(presentMonStartupText, "if (!options.IncludePresentMon)");
        AssertContains(presentMonStartupText, "var correlationSnapshotResponse = await sendAsync(\"GetSnapshot\", null, null)");
        AssertContains(presentMonStartupText, "TryGetSnapshot(correlationSnapshotResponse, out var correlationSnapshot)");
        AssertContains(presentMonStartupText, "backgroundTasks.SetPresentMon(PresentMonProbe.RunAsync(PresentMonProbe.CreateOptions(");
        AssertContains(presentMonStartupText, "processName: \"Sussudio\"");
        AssertContains(presentMonStartupText, "outputFile: Path.Combine(outputDirectory, \"presentmon.csv\")");
        AssertContains(presentMonStartupText, "correlation: PresentMonProbe.ReadPreviewCorrelation(correlationSnapshot)");
        AssertContains(presentMonStartupText, "actions.Add(\"presentmon capture started\")");
        AssertDoesNotContain(presentMonStartupText, "new PresentMonProbeOptions");
        AssertDoesNotContain(presentMonStartupText, "PreviewD3DSwapChainAddress");
        AssertContains(startupText, "StartPresentMonAsync(");
        AssertDoesNotContain(startupText, "PresentMonProbe.RunAsync(new PresentMonProbeOptions");
        AssertDoesNotContain(startupText, "PreviewD3DSwapChainAddress");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionSampler_OwnsSampleLoopOrdering()
    {
        var scenarioText = ReadDiagnosticSessionRunExecutionRootSource();

        AssertContains(scenarioText, "private static async Task SampleLoopAsync(");
        AssertContains(scenarioText, "var response = await sendCommandAsync(\"GetSnapshot\", null, null)");
        AssertContains(scenarioText, "samples.Add(new DiagnosticSessionSample");
        AssertContains(scenarioText, "await sampleCheckpointAsync().ConfigureAwait(false);");
        AssertOccursBefore(scenarioText, "samples.Add(new DiagnosticSessionSample", "await sampleCheckpointAsync().ConfigureAwait(false);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionScenarioPhaseRunner.cs")),
            "diagnostic-session scenario phase runner folded into DiagnosticSessionRunner.cs");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionAnalysisValidation_OwnsCleanupRestoreWarnings()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var runAsyncText = ExtractMemberCode(runnerText, "RunAsync");
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var cleanupActionsText = ReadDiagnosticSessionCleanupActionsSource();
        var cleanupText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");

        AssertContains(cleanupActionsText, "internal static class DiagnosticSessionCleanupActions");
        AssertDoesNotContain(cleanupActionsText, "internal static partial class DiagnosticSessionCleanupActions");
        AssertContains(cleanupActionsText, "internal static async Task<DiagnosticSessionCleanupResult> RunAsync(");
        AssertContains(cleanupActionsText, "internal readonly record struct DiagnosticSessionCleanupResult(bool StoppedRecordingForVerification)");
        AssertContains(cleanupActionsText, "StopRecordingForCleanupAsync(");
        AssertContains(cleanupActionsText, "private static async Task<bool> StopRecordingForCleanupAsync(");
        AssertContains(cleanupActionsText, "setStage(\"cleanup-stop-recording\")");
        AssertContains(cleanupActionsText, "recordTerminalException(ex, \"cleanup-stop-recording\")");
        AssertContains(cleanupActionsText, "private static async Task RestoreLiveFlashbackPlaybackAsync(");
        AssertContains(cleanupActionsText, "setStage(\"cleanup-go-live\")");
        AssertContains(cleanupActionsText, "private static async Task StopPreviewIfStartedAsync(");
        AssertContains(cleanupActionsText, "setStage(\"cleanup-stop-preview\")");
        AssertContains(cleanupActionsText, "private static async Task RestoreFlashbackEnabledStateAsync(");
        AssertContains(cleanupActionsText, "setStage(\"cleanup-restore-flashback-off\")");
        AssertContains(cleanupActionsText, "setStage(\"cleanup-restore-flashback-on\")");
        AssertContains(cleanupActionsText, "using Sussudio.Models;");
        AssertContains(cleanupActionsText, "DiagnosticSessionCommandChannel commandChannel,");
        AssertContains(cleanupActionsText, "commandChannel.SendWithTokenAsync(");
        AssertContains(cleanupActionsText, "AutomationCommandKind.SetRecordingEnabled,");
        AssertContains(cleanupActionsText, "AutomationCommandKind.FlashbackAction,");
        AssertContains(cleanupActionsText, "AutomationCommandKind.SetPreviewEnabled,");
        AssertContains(cleanupActionsText, "AutomationCommandKind.SetFlashbackEnabled,");
        AssertContains(cleanupActionsText, "AutomationPipeProtocol.GetDefaultResponseTimeout(AutomationCommandKind.SetFlashbackEnabled)");
        AssertContains(cleanupText, "internal static class DiagnosticSessionResultBuilder");
        AssertContains(cleanupText, "private static void ValidateCleanupLifecycleRestored(");
        AssertContains(cleanupText, "cleanup: preview remained active after restore");
        AssertContains(cleanupText, "cleanup: Flashback remained active after restore");
        AssertContains(cleanupText, "cleanup: playback did not return live state={state}");
        AssertContains(runnerText, "DiagnosticSessionCleanupActions.RunAsync(");
        AssertContains(runnerText, "runContext.CommandChannel,");
        AssertContains(runnerText, "stoppedRecordingForVerification = cleanupResult.StoppedRecordingForVerification;");
        AssertDoesNotContain(builderText, "using static Sussudio.Tools.DiagnosticSessionCleanupPolicy;");
        AssertDoesNotContain(runAsyncText, "setStage(\"cleanup-stop-recording\")");
        AssertDoesNotContain(runAsyncText, "setStage(\"cleanup-go-live\")");
        AssertDoesNotContain(runAsyncText, "setStage(\"cleanup-stop-preview\")");
        AssertDoesNotContain(runAsyncText, "setStage(\"cleanup-restore-flashback-off\")");
        AssertDoesNotContain(runnerText, "private static void ValidateCleanupLifecycleRestored(");
        AssertDoesNotContain(cleanupActionsText, "sendWithTokenAsync(\"SetRecordingEnabled\"");
        AssertDoesNotContain(cleanupActionsText, "sendWithTokenAsync(\"FlashbackAction\"");
        AssertDoesNotContain(cleanupActionsText, "sendWithTokenAsync(\"SetPreviewEnabled\"");
        AssertDoesNotContain(cleanupActionsText, "sendWithTokenAsync(\"SetFlashbackEnabled\"");
        AssertDoesNotContain(cleanupActionsText, "GetDefaultResponseTimeout(\"SetFlashbackEnabled\")");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionRecordingChecks_OwnPostRunRecordingVerification()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var completionText = ExtractMemberCode(runnerText, "RunCompletionPhaseAsync");
        var recordingChecksText = ReadDiagnosticSessionCleanupActionsSource()
            .Replace("\r\n", "\n");
        var recordingVerificationText = recordingChecksText;

        AssertContains(recordingChecksText, "internal static class DiagnosticSessionRecordingChecks");
        AssertContains(recordingChecksText, "internal static async Task<DiagnosticSessionRecordingCheckResult> RunAsync(");
        AssertContains(recordingChecksText, "internal readonly record struct DiagnosticSessionRecordingCheckResult(JsonElement? Verification)");
        AssertContains(recordingChecksText, "setStage(\"settings-deferred-restore\")");
        AssertContains(recordingChecksText, "VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
        AssertContains(recordingChecksText, "RunRecordingVerificationAsync(");
        AssertContains(recordingChecksText, "verification = await RunRecordingVerificationAsync(");
        AssertContains(recordingChecksText, "setStage(\"recording-validation\")");
        AssertContains(recordingChecksText, "ValidateFlashbackRecordingSession(initialSnapshot, samples, warnings)");
        AssertContains(recordingVerificationText, "private static async Task<JsonElement?> RunRecordingVerificationAsync(");
        AssertContains(recordingVerificationText, "DiagnosticSessionScenarioCatalog.TryGetFlashbackExportVerificationPath(");
        AssertContains(recordingVerificationText, "setStage(\"recording-verification\")");
        AssertContains(recordingVerificationText, "var verificationCommand = \"VerifyLastRecording\";");
        AssertContains(recordingVerificationText, "verificationCommand = \"VerifyFile\";");
        AssertContains(recordingVerificationText, "[\"strict\"] = true");
        AssertContains(recordingVerificationText, "[\"verificationProfile\"] = \"flashback-export\"");
        AssertContains(recordingVerificationText, "sendAsync(verificationCommand, verificationPayload, 60_000)");
        AssertContains(recordingVerificationText, "return verificationElement.Clone();");
        AssertContains(recordingVerificationText, "recording verification skipped: scenario does not produce a recording or export artifact");
        AssertContains(recordingVerificationText, "recordTerminalException(ex, \"recording-verification\")");
        AssertContains(runnerText, "DiagnosticSessionRecordingChecks.RunAsync(");
        AssertDoesNotContain(completionText, "SetStage(\"settings-deferred-restore\")");
        AssertContains(recordingChecksText, "var verificationCommand = \"VerifyLastRecording\"");
        AssertDoesNotContain(completionText, "DiagnosticSessionScenarioCatalog.TryGetFlashbackExportVerificationPath(");
        AssertContains(recordingChecksText, "[\"verificationProfile\"] = \"flashback-export\"");
        AssertDoesNotContain(completionText, "ValidateFlashbackRecordingSession(initialSnapshot, samples, warnings)");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionPostRunSnapshots_OwnTimelineAndFinalSnapshot()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var postRunText = ReadDiagnosticSessionRunExecutionRootSource();

        AssertContains(postRunText, "private static async Task<DiagnosticSessionPostRunSnapshotResult> CapturePostRunSnapshotsAsync(");
        AssertContains(postRunText, "internal readonly record struct DiagnosticSessionPostRunSnapshotResult(");
        AssertContains(postRunText, "JsonElement HealthSnapshot,");
        AssertContains(postRunText, "setStage(\"timeline\")");
        AssertContains(postRunText, "\"GetPerformanceTimeline\"");
        AssertContains(postRunText, "new Dictionary<string, object?> { [\"maxEntries\"] = 240 }");
        AssertContains(postRunText, "recordTerminalException(ex, \"timeline\")");
        AssertContains(postRunText, "setStage(\"final-snapshot\")");
        AssertContains(postRunText, "sendAsync(\"GetSnapshot\", null, null)");
        AssertContains(postRunText, "TryGetSnapshot(finalSnapshotResponse, out var finalSnapshot)");
        AssertContains(postRunText, "recordTerminalException(ex, \"final-snapshot\")");
        AssertContains(runnerText, "CapturePostRunSnapshotsAsync(");
        AssertContains(runnerText, "postRunSnapshots.HealthSnapshot");
        AssertContains(runnerText, "postRunSnapshots.Timeline");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionPostRunSnapshots.cs")),
            "post-run timeline and final-snapshot capture lives with the runner completion phase");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionMetrics_OwnsSessionMetricProjection()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var metricsText = ReadDiagnosticSessionMetricsSource();

        AssertContains(metricsText, "internal static class DiagnosticSessionMetrics");
        AssertContains(metricsText, "internal sealed class SourceCadenceSessionMetrics");
        AssertContains(metricsText, "internal sealed class PreviewCadenceSessionMetrics");
        AssertContains(metricsText, "internal sealed class VisualCadenceSessionMetrics");
        AssertContains(metricsText, "internal sealed class PreviewD3DMetrics");
        AssertContains(metricsText, "internal readonly record struct PlaybackCommandHealth(");
        AssertContains(metricsText, "internal static SourceCadenceSessionMetrics BuildSourceCadenceSessionMetrics(");
        AssertContains(metricsText, "internal static PreviewCadenceSessionMetrics BuildPreviewCadenceSessionMetrics(");
        AssertContains(metricsText, "internal static VisualCadenceSessionMetrics BuildVisualCadenceSessionMetrics(");
        AssertContains(metricsText, "private static void ObserveSourceCadenceSnapshot(");
        AssertContains(metricsText, "private static void ObservePreviewCadenceSnapshot(");
        AssertContains(metricsText, "internal static bool IsVisualCadenceSessionHealthy(");
        AssertContains(metricsText, "private static void ObserveVisualCadenceSnapshot(");
        AssertContains(metricsText, "internal static PreviewD3DMetrics BuildPreviewD3DMetrics(");
        AssertContains(metricsText, "CountArrayItems(sample.Snapshot, \"PreviewD3DRecentSlowFrames\")");
        AssertContains(metricsText, "private static void ObservePreviewD3DCpuTiming(PreviewD3DMetrics metrics, JsonElement snapshot)");
        AssertContains(metricsText, "private static void ApplySlowFrame(PreviewD3DMetrics metrics, JsonElement slowFrame)");
        AssertContains(metricsText, "private static bool TryGetLatestSlowFrame(JsonElement snapshot, out JsonElement slowFrame)");
        AssertContains(metricsText, "internal static PlaybackCommandHealth BuildPlaybackCommandHealth(");
        AssertContains(metricsText, "internal static long GetResetAwareCounterDelta(");
        AssertContains(metricsText, "internal static bool IsVisualCadenceSessionHealthy(");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionMetrics;");
        AssertDoesNotContain(runnerText, "private sealed class SourceCadenceSessionMetrics");
        AssertDoesNotContain(runnerText, "private sealed class PreviewD3DMetrics");
        AssertDoesNotContain(runnerText, "private static PlaybackCommandHealth BuildPlaybackCommandHealth(");
        AssertDoesNotContain(runnerText, "private static long GetCounterDelta(");

        return Task.CompletedTask;
    }


    internal static Task DiagnosticSessionRunner_OwnsCompatibilitySurface()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var scenarioText = ReadDiagnosticSessionRunExecutionScenarioSource();

        AssertContains(runnerText, "public static class DiagnosticSessionRunner");
        AssertContains(runnerText, "public static async Task<DiagnosticSessionResult> RunAsync(");
        AssertContains(runnerText, "await runContext.CaptureInitialSnapshotAsync().ConfigureAwait(false);");
        AssertContains(runnerText, "DiagnosticSessionScenarioPhaseRunner.RunAsync(scenarioPhaseContext)");
        AssertContains(runnerText, "DiagnosticSessionCleanupActions.RunAsync(");
        AssertContains(runnerText, "return await RunCompletionPhaseAsync(");
        AssertContains(runnerText, "return DiagnosticSessionResultFormatter.Format(result);");
        AssertContains(runnerText, "private static FileStream AcquireOutputLock(string outputDirectory)");
        AssertDoesNotContain(runnerText, "internal static class DiagnosticSessionRunExecution");
        AssertDoesNotContain(runnerText, "DiagnosticSessionRunExecution.RunAsync(");
        AssertContains(executionText, "DiagnosticSessionScenarioPhaseRunner.RunAsync(scenarioPhaseContext)");
        AssertContains(executionText, "DiagnosticSessionCleanupActions.RunAsync(");
        AssertContains(scenarioText, "DiagnosticSessionScenarioSetup.RunAsync(");
        AssertContains(scenarioText, "SampleLoopAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionInitialSnapshot_OwnsBaselineCapture()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var initialSnapshotText = contextText;

        AssertContains(initialSnapshotText, "internal sealed class DiagnosticSessionRunContext : IDisposable");
        AssertContains(initialSnapshotText, "using Sussudio.Models;");
        AssertContains(initialSnapshotText, "private DiagnosticSessionInitialSnapshotResult CreateUnknownInitialSnapshot()");
        AssertContains(initialSnapshotText, "private async Task<DiagnosticSessionInitialSnapshotResult> CaptureInitialSnapshotCoreAsync()");
        AssertContains(initialSnapshotText, "CreateEmptyJsonObject()");
        AssertContains(initialSnapshotText, "var unknownSnapshot = CreateUnknownInitialSnapshot();");
        AssertContains(initialSnapshotText, "SetStage(\"initial-snapshot\")");
        AssertContains(initialSnapshotText, "CommandChannel.SendAsync(AutomationCommandKind.GetSnapshot, null, null)");
        AssertDoesNotContain(initialSnapshotText, "commandChannel.SendAsync(\"GetSnapshot\", null, null)");
        AssertContains(initialSnapshotText, "TryGetSnapshot(initialResponse, out var initial)");
        AssertContains(initialSnapshotText, "CommandChannel.RecordFailure(\"initial-snapshot: baseline snapshot unavailable; state-mutating scenarios will be skipped\")");
        AssertContains(initialSnapshotText, "RecordTerminalException(ex, \"initial-snapshot\")");
        AssertContains(initialSnapshotText, "await WriteLiveStateBestEffortAsync().ConfigureAwait(false);");
        AssertContains(initialSnapshotText, "internal sealed class DiagnosticSessionInitialSnapshotResult");
        AssertContains(initialSnapshotText, "internal DiagnosticSessionInitialSnapshotResult(JsonElement snapshot, bool known)");
        AssertContains(initialSnapshotText, "internal JsonElement Snapshot { get; }");
        AssertContains(initialSnapshotText, "internal bool Known { get; }");
        AssertContains(contextText, "var unknownSnapshot = CreateUnknownInitialSnapshot();");
        AssertContains(contextText, "internal async Task CaptureInitialSnapshotAsync()");
        AssertContains(contextText, "CaptureInitialSnapshotCoreAsync()");
        AssertContains(contextText, "InitialSnapshot = initialSnapshotResult.Snapshot;");
        AssertContains(contextText, "InitialSnapshotKnown = initialSnapshotResult.Known;");
        AssertContains(runnerText, "await runContext.CaptureInitialSnapshotAsync().ConfigureAwait(false);");
        AssertDoesNotContain(executionText, "CreateEmptyJsonObject()");
        AssertDoesNotContain(executionText, "var initialResponse = await commandChannel.SendAsync(\"GetSnapshot\", null, null)");
        AssertDoesNotContain(executionText, "var initialResponse = await commandChannel.SendAsync(AutomationCommandKind.GetSnapshot, null, null)");
        AssertDoesNotContain(executionText, "TryGetSnapshot(initialResponse, out var initial)");
        AssertDoesNotContain(executionText, "baseline snapshot unavailable; state-mutating scenarios will be skipped");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionPipeRetryPolicy_OwnsConnectRetryClassification()
    {
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var channelText = ReadDiagnosticSessionRunContextSource();
        var retryText = channelText;

        AssertContains(retryText, "internal static class DiagnosticSessionPipeRetryPolicy");
        AssertContains(retryText, "BuildLocalFailureResponse(command, ex.Message)");
        AssertContains(retryText, "\"pipe-connect-failed\"");
        AssertContains(retryText, "\"pipe-connect-timeout\"");
        AssertContains(retryText, "\"pipe-access-denied\"");
        AssertContains(channelText, "using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;");
        AssertContains(channelText, "SendCommandWithConnectRetryAsync(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionPipeRetryPolicy.cs")),
            "diagnostic-session pipe retry policy lives with the run-context command-channel transport owner");
        AssertDoesNotContain(executionText, "using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;");
        AssertDoesNotContain(executionText, "private static bool IsSyntheticPipeConnectFailure(");
        AssertDoesNotContain(executionText, "private static bool IsPermanentPipeConnectFailure(");
        AssertDoesNotContain(executionText, "private static JsonElement BuildLocalFailureResponse(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionCommandChannel_OwnsSerializedCommandSending()
    {
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var channelText = contextText;

        AssertContains(channelText, "internal sealed class DiagnosticSessionCommandChannel : IDisposable");
        AssertContains(channelText, "using Sussudio.Models;");
        AssertContains(channelText, "private readonly SemaphoreSlim _sendGate = new(1, 1);");
        AssertContains(channelText, "internal int FailureCount => _failureCount;");
        AssertContains(channelText, "internal void RecordFailure(string warning)");
        AssertContains(channelText, "private static string CommandName(AutomationCommandKind kind)");
        AssertContains(channelText, "=> AutomationCommandCatalog.Get(kind).Name;");
        AssertContains(channelText, "internal async Task<JsonElement> SendRawWithConnectRetryAsync(");
        AssertContains(channelText, "internal async Task<JsonElement> SendRawWithConnectRetryWithTokenAsync(");
        AssertContains(channelText, "internal async Task<JsonElement> SendWithTokenAsync(");
        AssertContains(channelText, "AutomationCommandKind kind,");
        AssertContains(channelText, "=> await SendRawWithConnectRetryAsync(CommandName(kind), payload, responseTimeoutMs).ConfigureAwait(false);");
        AssertContains(channelText, "=> await SendAsync(CommandName(kind), payload, responseTimeoutMs).ConfigureAwait(false);");
        AssertContains(channelText, "=> await SendWithTokenAsync(CommandName(kind), payload, responseTimeoutMs, allowFailure, commandCancellationToken).ConfigureAwait(false);");
        AssertContains(channelText, "BuildLocalFailureResponse(command, \"no response after connect retry\")");
        AssertContains(channelText, "RecordFailure($\"{command}:");
        AssertContains(channelText, "Get(response, \"Message\", \"command failed\")");
        AssertContains(channelText, "internal async Task TryWaitAsync(string condition, int timeoutMs)");
        AssertContains(channelText, "internal async Task TryWaitWithTokenAsync(");
        AssertContains(channelText, "SendWithTokenAsync(\n                AutomationCommandKind.WaitForCondition,");
        AssertContains(channelText, "AutomationCommandKind.WaitForCondition");
        AssertContains(channelText, "[\"condition\"] = condition");
        AssertContains(channelText, "[\"timeoutMs\"] = timeoutMs");
        AssertContains(channelText, "[\"pollMs\"] = 250");
        AssertContains(channelText, "timeoutMs + 2_000");
        AssertContains(channelText, "$\"wait {condition}: {Get(response, \"Message\", \"not met\")}\"");
        AssertDoesNotContain(channelText, "\"WaitForCondition\"");
        AssertDoesNotContain(channelText, "\"GetSnapshot\"");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionCommandChannel.cs")),
            "diagnostic-session command channel lives with DiagnosticSessionRunContext.cs");
        AssertContains(contextText, "CommandChannel = new DiagnosticSessionCommandChannel(");
        AssertContains(executionText, "context.CommandChannel,");
        AssertContains(executionText, "runContext.CommandChannel,");
        AssertContains(contextText, "CommandChannel.FailureCount");
        AssertDoesNotContain(executionText, "new DiagnosticSessionCommandChannel(");
        AssertDoesNotContain(executionText, "var commandFailureCount = 0;");
        AssertDoesNotContain(executionText, "var commandSendGate = new SemaphoreSlim(1, 1);");
        AssertDoesNotContain(executionText, "async Task<JsonElement> SendAsync(");
        AssertDoesNotContain(executionText, "async Task TryWaitAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionRunExecutionScenario_OwnsScenarioPhase()
    {
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var scenarioText = ReadDiagnosticSessionRunExecutionScenarioSource();
        var phaseRunnerText = ReadDiagnosticSessionRunExecutionRootSource();
        var phaseModelsText = ReadRepoFile("tools/Common/DiagnosticSessionResult.cs")
            .Replace("\r\n", "\n");
        var completionText = phaseRunnerText;
        var backgroundTasksText = ReadDiagnosticSessionBackgroundTasksSource();

        AssertContains(executionText, "DiagnosticSessionScenarioPhaseRunner.RunAsync(scenarioPhaseContext)");
        AssertContains(phaseRunnerText, "internal static class DiagnosticSessionScenarioPhaseRunner");
        AssertContains(phaseModelsText, "internal sealed class DiagnosticSessionScenarioPhaseContext");
        AssertContains(phaseModelsText, "internal sealed record DiagnosticSessionScenarioPhaseResult(");
        AssertContains(phaseModelsText, "internal sealed class DiagnosticSessionScenarioPhaseState");
        AssertContains(scenarioText, "internal sealed class DiagnosticSessionScenarioPhaseContext");
        AssertContains(scenarioText, "internal sealed record DiagnosticSessionScenarioPhaseResult(");
        AssertContains(scenarioText, "internal sealed class DiagnosticSessionScenarioPhaseState");
        AssertContains(scenarioText, "return scenarioPhase.ToResult();");
        AssertContains(scenarioText, "DiagnosticSessionScenarioSetup.RunAsync(");
        AssertContains(scenarioText, "DiagnosticSessionScenarioStartup.StartAsync(");
        AssertContains(phaseRunnerText, "RunSamplingAndCompleteAsync(context, backgroundTasks, scenarioPhase)");
        AssertContains(phaseRunnerText, "private static async Task RunSamplingAndCompleteAsync(");
        AssertContains(phaseRunnerText, "context.SetStage(\"sampling\")");
        AssertContains(phaseRunnerText, "SampleLoopAsync(");
        AssertContains(phaseRunnerText, "CompleteAfterSamplingAsync(");
        AssertContains(completionText, "private static async Task CompleteAfterSamplingAsync(");
        AssertContains(completionText, ".CompleteRegisteredScenarioWorkAsync(scenarioPhase.FlashbackRecordingSettingsDeferredPresetState)");
        AssertContains(completionText, "DiagnosticSessionFlashbackExportScenarios.RunSelectedRejectedExportScenariosAsync(");
        AssertContains(completionText, "backgroundTasks.CompletePresentMonAsync(scenarioPhase.PresentMon, context.Warnings)");
        AssertContains(backgroundTasksText, "internal async Task<FlashbackRecordingSettingsDeferredPresetState> CompleteRegisteredScenarioWorkAsync(");
        AssertContains(backgroundTasksText, "private async Task AwaitScenarioTasksAsync()");
        AssertContains(backgroundTasksText, "private async Task<FlashbackRecordingSettingsDeferredPresetState> AwaitRecordingSettingsDeferredAsync(");
        AssertContains(backgroundTasksText, "internal async Task<PresentMonProbeResult?> CompletePresentMonAsync(");
        AssertContains(scenarioText, "context.RecordTerminalException(ex, context.GetLastStage())");
        AssertContains(scenarioText, "context.ScenarioCancellationSource.Cancel();");
        AssertContains(phaseRunnerText, "DrainAfterFaultAsync(context, backgroundTasks, scenarioPhase)");
        AssertContains(completionText, "private static async Task DrainAfterFaultAsync(");
        AssertContains(completionText, "backgroundTasks.ObserveAfterFaultAsync(");
        AssertDoesNotContain(phaseRunnerText, "internal sealed class DiagnosticSessionScenarioPhaseContext");
        AssertDoesNotContain(phaseRunnerText, "internal sealed record DiagnosticSessionScenarioPhaseResult(");
        AssertDoesNotContain(phaseRunnerText, "internal sealed class DiagnosticSessionScenarioPhaseState");
        AssertContains(contextText, "new DiagnosticSessionScenarioPhaseContext");
        AssertContains(executionText, "var scenarioPhaseContext = runContext.CreateScenarioPhaseContext(options, cancellationToken);");
        AssertContains(executionText, "var scenarioPhase = DiagnosticSessionScenarioPhaseResult.Empty;");
        AssertContains(executionText, "scenarioPhase = await DiagnosticSessionScenarioPhaseRunner.RunAsync(scenarioPhaseContext)");
        AssertContains(executionText, "scenarioPhase.StartedRecording");
        AssertContains(executionText, "scenarioPhase.StartedPreview");
        AssertContains(executionText, "scenarioPhase.EnabledFlashback");
        AssertContains(executionText, "scenarioPhase.DisabledFlashback");
        AssertContains(executionText, "scenarioPhase.StartedFlashbackPlayback");
        AssertContains(contextText, "ScenarioPhase = scenarioPhase,");
        AssertDoesNotContain(scenarioText, "internal required DiagnosticSessionScenarioPhaseState PhaseState");
        AssertDoesNotContain(executionText, "backgroundTasks.AwaitScenarioTasksAsync()");
        AssertDoesNotContain(phaseRunnerText, "backgroundTasks.AwaitScenarioTasksAsync()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionScenarioPhaseRunner.cs")),
            "diagnostic-session scenario phase runner folded into DiagnosticSessionRunner.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionBackgroundTasks.cs")),
            "diagnostic-session background task drain folded into DiagnosticSessionRunner.cs");
        AssertOccursBefore(phaseRunnerText, "DiagnosticSessionScenarioSetup.RunAsync(", "DiagnosticSessionScenarioStartup.StartAsync(");
        AssertOccursBefore(phaseRunnerText, "DiagnosticSessionScenarioStartup.StartAsync(", "RunSamplingAndCompleteAsync(context, backgroundTasks, scenarioPhase)");
        AssertOccursBefore(phaseRunnerText, "context.RecordTerminalException(ex, context.GetLastStage())", "context.ScenarioCancellationSource.Cancel();");
        AssertOccursBefore(phaseRunnerText, "context.ScenarioCancellationSource.Cancel();", "DrainAfterFaultAsync(context, backgroundTasks, scenarioPhase)");
        AssertOccursBefore(phaseRunnerText, "context.SetStage(\"sampling\")", "SampleLoopAsync(");
        AssertOccursBefore(phaseRunnerText, "SampleLoopAsync(", "CompleteAfterSamplingAsync(");
        AssertOccursBefore(backgroundTasksText, "await AwaitScenarioTasksAsync()", "return await AwaitRecordingSettingsDeferredAsync(");
        AssertOccursBefore(completionText, ".CompleteRegisteredScenarioWorkAsync(", "DiagnosticSessionFlashbackExportScenarios.RunSelectedRejectedExportScenariosAsync(");
        AssertOccursBefore(completionText, "DiagnosticSessionFlashbackExportScenarios.RunSelectedRejectedExportScenariosAsync(", "backgroundTasks.CompletePresentMonAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionRunExecutionCompletion_OwnsPostCleanupEvidenceAndResult()
    {
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var completionRootText = ReadDiagnosticSessionRunExecutionCompletionRootSource();
        var completionContextText = ReadDiagnosticSessionRunExecutionCompletionContextSource();
        var recordingChecksText = ReadDiagnosticSessionCleanupActionsSource()
            .Replace("\r\n", "\n");
        var recordingVerificationText = recordingChecksText;
        var postRunText = completionRootText;
        var resultBuilderText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");

        AssertContains(completionRootText, "private static async Task<DiagnosticSessionResult> RunCompletionPhaseAsync(DiagnosticSessionCompletionContext context)");
        AssertContains(completionContextText, "internal sealed class DiagnosticSessionCompletionContext");
        AssertContains(completionRootText, "private static DiagnosticSessionResultBuildRequest CreateResultBuildRequest(");
        AssertContains(completionRootText, "internal sealed class DiagnosticSessionCompletionContext");
        AssertContains(completionRootText, "DiagnosticSessionRecordingChecks.RunAsync(");
        AssertContains(completionRootText, "var verification = recordingCheckResult.Verification;");
        AssertContains(completionRootText, "context.ScenarioPhase.FlashbackRecordingSettingsDeferredPresetState");
        AssertContains(completionRootText, "CapturePostRunSnapshotsAsync(");
        AssertContains(completionRootText, "DiagnosticSessionResultBuilder.BuildAndWriteAsync(");
        AssertContains(completionRootText, "CreateResultBuildRequest(");
        AssertContains(completionRootText, "context.ScenarioPhase.PresentMon");
        AssertContains(completionRootText, "await context.WriteLiveStateBestEffortAsync(result.CompletedUtc, result.TerminalState).ConfigureAwait(false);");
        AssertContains(completionRootText, "postRunSnapshots.HealthSnapshot");
        AssertContains(completionRootText, "postRunSnapshots.Timeline");
        AssertContains(completionRootText, "runBootstrap.RunnerProcessId");
        AssertContains(contextText, "new DiagnosticSessionCompletionContext");
        AssertContains(executionText, "return await RunCompletionPhaseAsync(");
        AssertContains(executionText, "runContext.CreateCompletionContext(options, scenarioPhase, stoppedRecordingForVerification, cancellationToken)");
        AssertContains(executionText, "DiagnosticSessionRecordingChecks.RunAsync(");
        AssertContains(executionText, "CapturePostRunSnapshotsAsync(");
        AssertContains(executionText, "DiagnosticSessionResultBuilder.BuildAndWriteAsync(");
        AssertContains(recordingVerificationText, "setStage(\"recording-verification\")");
        AssertContains(postRunText, "setStage(\"timeline\")");
        AssertContains(postRunText, "setStage(\"final-snapshot\")");
        AssertContains(resultBuilderText, "runState.SetStage(\"summary\")");
        AssertContains(agentMapText, "completion context handoff consumed by the post-cleanup completion phase");
        AssertContains(agentMapText, "post-cleanup evidence/result sequence, result-build");
        AssertContains(cleanupPlanText, "`DiagnosticSessionRunner.cs` owns the completion context handoff");
        AssertContains(cleanupPlanText, "`DiagnosticSessionRunner.cs` owns the post-cleanup evidence/result sequence");
        AssertOccursBefore(completionRootText, "DiagnosticSessionRecordingChecks.RunAsync(", "CapturePostRunSnapshotsAsync(");
        AssertOccursBefore(completionRootText, "CapturePostRunSnapshotsAsync(", "DiagnosticSessionResultBuilder.BuildAndWriteAsync(");
        AssertOccursBefore(completionRootText, "DiagnosticSessionResultBuilder.BuildAndWriteAsync(", "await context.WriteLiveStateBestEffortAsync(result.CompletedUtc, result.TerminalState)");
        AssertOccursBefore(postRunText, "setStage(\"timeline\")", "setStage(\"final-snapshot\")");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionRunState_OwnsTerminalState()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var runStateStart = contextText.IndexOf("internal sealed class DiagnosticSessionRunState", StringComparison.Ordinal);
        var runStateEnd = contextText.IndexOf("internal sealed class DiagnosticSessionInitialSnapshotResult", StringComparison.Ordinal);
        var runStateText = contextText[runStateStart..runStateEnd];

        AssertContains(contextText, "internal sealed class DiagnosticSessionRunState");
        AssertContains(contextText, "internal void SetStage(string stage)");
        AssertContains(contextText, "internal void RecordTerminalException(Exception ex, string stage)");
        AssertContains(contextText, "internal string GetTerminalState()");
        AssertContains(contextText, "internal async Task WriteArtifactBestEffortAsync<T>(");
        AssertContains(contextText, "RunState = new DiagnosticSessionRunState(");
        AssertContains(contextText, "internal void SetStage(string stage)");
        AssertContains(contextText, "internal void RecordTerminalException(Exception ex, string stage)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionRunState.cs")),
            "run state stays folded into DiagnosticSessionRunContext.cs");
        AssertDoesNotContain(runnerText, "var lastStage = \"initializing\";");
        AssertDoesNotContain(runnerText, "Exception? terminalException = null;");
        AssertDoesNotContain(runStateText, "DateTimeOffset.MinValue");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionLiveStateWriter_OwnsBreadcrumbFile()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var liveStateWriterText = contextText;

        AssertContains(liveStateWriterText, "internal sealed class DiagnosticSessionLiveStateWriter");
        AssertContains(liveStateWriterText, "LivePath = Path.Combine(runBootstrap.OutputDirectory, \"session-live.json\");");
        AssertContains(liveStateWriterText, "internal string LivePath { get; }");
        AssertContains(liveStateWriterText, "internal async Task WriteLiveStateBestEffortAsync(");
        AssertContains(liveStateWriterText, "internal async Task WriteSamplingLiveStateBestEffortAsync(");
        AssertContains(liveStateWriterText, "private DateTimeOffset _lastSamplingLiveStateUtc = DateTimeOffset.MinValue;");
        AssertContains(liveStateWriterText, "TerminalState = terminalStateOverride ?? (_runState.TerminalException is null ? \"running\" : _runState.GetTerminalState())");
        AssertContains(liveStateWriterText, "LastStage = terminalStateOverride is null ? _runState.LastStage : _runState.GetResultLastStage()");
        AssertContains(liveStateWriterText, "TimeSpan.FromSeconds(5)");
        AssertContains(liveStateWriterText, "The live-state file is diagnostic breadcrumbs only.");
        AssertContains(contextText, "_liveStateWriter = new DiagnosticSessionLiveStateWriter(RunBootstrap, RunState, Warnings);");
        AssertContains(contextText, "LivePath = _liveStateWriter.LivePath;");
        AssertContains(contextText, "_liveStateWriter.WriteLiveStateBestEffortAsync(");
        AssertContains(contextText, "_liveStateWriter.WriteSamplingLiveStateBestEffortAsync(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionLiveStateWriter.cs")),
            "live-state writer stays folded into DiagnosticSessionRunContext.cs");
        AssertDoesNotContain(runnerText, "var livePath = runState.LivePath;");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionRunContext_OwnsMutableRunInfrastructure()
    {
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");

        AssertContains(contextText, "internal sealed class DiagnosticSessionRunContext : IDisposable");
        AssertContains(contextText, "RunBootstrap = DiagnosticSessionRunBootstrap.Create(options);");
        AssertContains(contextText, "Actions = [];");
        AssertContains(contextText, "Warnings = [];");
        AssertContains(contextText, "Samples = [];");
        AssertContains(contextText, "ScenarioCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(runCancellationToken);");
        AssertContains(contextText, "CommandChannel = new DiagnosticSessionCommandChannel(sendCommandAsync, ScenarioCancellationToken, Warnings);");
        AssertContains(contextText, "InitializeUnknownSnapshotState();");
        AssertContains(contextText, "internal JsonElement InitialSnapshot { get; private set; }");
        AssertContains(contextText, "private void InitializeUnknownSnapshotState()");
        AssertContains(contextText, "InitialSnapshot = unknownSnapshot.Snapshot;");
        AssertContains(contextText, "internal async Task CaptureInitialSnapshotAsync()");
        AssertContains(contextText, "private readonly DiagnosticSessionLiveStateWriter _liveStateWriter;");
        AssertContains(contextText, "internal string LivePath { get; }");
        AssertContains(contextText, "internal async Task WriteLiveStateBestEffortAsync(");
        AssertContains(contextText, "internal async Task WriteSamplingLiveStateBestEffortAsync()");
        AssertContains(contextText, "public void Dispose()");
        AssertContains(contextText, "internal DiagnosticSessionScenarioPhaseContext CreateScenarioPhaseContext(");
        AssertContains(contextText, "internal DiagnosticSessionCompletionContext CreateCompletionContext(");
        AssertContains(contextText, "GetLastStage = () => RunState.LastStage,");
        AssertContains(contextText, "CommandChannel.Dispose();");
        AssertContains(contextText, "ScenarioCancellationSource.Dispose();");

        AssertContains(executionText, "using var runContext = new DiagnosticSessionRunContext(options, sendCommandAsync, cancellationToken);");
        AssertContains(executionText, "using var sessionLock = AcquireOutputLock(runContext.OutputDirectory);");
        AssertContains(executionText, "await runContext.CaptureInitialSnapshotAsync().ConfigureAwait(false);");
        AssertContains(executionText, "var scenarioPhaseContext = runContext.CreateScenarioPhaseContext(options, cancellationToken);");
        AssertContains(executionText, "runContext.CreateCompletionContext(options, scenarioPhase, stoppedRecordingForVerification, cancellationToken)");
        AssertDoesNotContain(executionText, "new DiagnosticSessionRunState(");
        AssertDoesNotContain(executionText, "new DiagnosticSessionCommandChannel(");
        AssertDoesNotContain(executionText, "new DiagnosticSessionLiveStateWriter(");

        AssertContains(agentMapText, "`tools/Common/DiagnosticSessionRunContext.cs` owns diagnostic-session core mutable run infrastructure");
        AssertContains(agentMapText, "initial snapshot state, live-state handoff, run context disposal, scenario/completion context construction");
        AssertContains(cleanupPlanText, "`DiagnosticSessionRunContext.cs`");
        AssertContains(cleanupPlanText, "owns the cohesive mutable per-run context");
        AssertContains(cleanupPlanText, "initial\nsnapshot state and capture, live-state writer handoff, disposal");
        AssertContains(cleanupPlanText, "scenario/completion context construction");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionRunBootstrap_OwnsNormalizedSessionIdentity()
    {
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var bootstrapText = ReadDiagnosticSessionRunContextSource()
            .Replace("\r\n", "\n");

        AssertContains(bootstrapText, "internal readonly record struct DiagnosticSessionRunBootstrap(");
        AssertContains(bootstrapText, "internal static DiagnosticSessionRunBootstrap Create(DiagnosticSessionOptions options)");
        AssertContains(bootstrapText, "var scenario = DiagnosticSessionScenarioCatalog.Normalize(options.Scenario);");
        AssertContains(bootstrapText, "var scenarioPlan = DiagnosticSessionScenarioPlan.From(scenario);");
        AssertContains(bootstrapText, "Math.Clamp(options.DurationSeconds, 0, 24 * 60 * 60)");
        AssertContains(bootstrapText, "Math.Clamp(options.SampleIntervalMs, 100, 60_000)");
        AssertContains(bootstrapText, "DateTimeOffset.UtcNow.ToString(\"yyyyMMdd_HHmmss\", CultureInfo.InvariantCulture)");
        AssertContains(bootstrapText, "Path.Combine(Environment.CurrentDirectory, \"temp\", \"diagnostic-sessions\", sessionId)");
        AssertContains(bootstrapText, "Path.GetFullPath(options.OutputDirectory)");
        AssertContains(bootstrapText, "Directory.CreateDirectory(outputDirectory);");
        AssertContains(bootstrapText, "Environment.ProcessId");
        AssertContains(contextText, "RunBootstrap = DiagnosticSessionRunBootstrap.Create(options);");
        AssertContains(contextText, "ScenarioPlan = RunBootstrap.ScenarioPlan;");
        AssertContains(executionText, "using var sessionLock = AcquireOutputLock(runContext.OutputDirectory);");
        AssertDoesNotContain(executionText, "DiagnosticSessionScenarioCatalog.Normalize(options.Scenario)");
        AssertDoesNotContain(executionText, "Math.Clamp(options.DurationSeconds");
        AssertDoesNotContain(executionText, "Math.Clamp(options.SampleIntervalMs");
        AssertDoesNotContain(executionText, "DateTimeOffset.UtcNow.ToString(\"yyyyMMdd_HHmmss\"");
        AssertDoesNotContain(executionText, "Directory.CreateDirectory(outputDirectory);");
        AssertDoesNotContain(executionText, "var runFlashbackPlayback = scenarioPlan.RunFlashbackPlayback;");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionOutputLock_OwnsExclusiveOutputDirectoryLock()
    {
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();

        AssertContains(executionText, "private static FileStream AcquireOutputLock(string outputDirectory)");
        AssertContains(executionText, "\".sussudio-diag.lock\"");
        AssertContains(executionText, "FileShare.None");
        AssertContains(executionText, "FileOptions.DeleteOnClose");
        AssertContains(executionText, "Another diagnostic session is already running");
        AssertContains(executionText, "using var sessionLock = AcquireOutputLock(runContext.OutputDirectory);");
        AssertDoesNotContain(executionText, "sessionLock.Dispose();");

        return Task.CompletedTask;
    }

    private static Assembly LoadDiagnosticSessionRunnerAssembly()
    {
        return LoadToolAssembly(global::Program.SsctlAssemblyRelativePath);
    }

    private static object CreateDiagnosticSessionOptions(
        Assembly assembly,
        string scenario,
        int durationSeconds,
        int sampleIntervalMs,
        string outputDirectory)
    {
        var optionsType = assembly.GetType("Sussudio.Tools.DiagnosticSessionOptions")
            ?? throw new InvalidOperationException("DiagnosticSessionOptions type was not found.");
        var options = Activator.CreateInstance(optionsType)
            ?? throw new InvalidOperationException("DiagnosticSessionOptions instance could not be created.");

        optionsType.GetProperty("Scenario")!.SetValue(options, scenario);
        optionsType.GetProperty("DurationSeconds")!.SetValue(options, durationSeconds);
        optionsType.GetProperty("SampleIntervalMs")!.SetValue(options, sampleIntervalMs);
        optionsType.GetProperty("OutputDirectory")!.SetValue(options, outputDirectory);
        return options;
    }

    private static async Task<object> RunDiagnosticSessionRunnerAsync(
        Assembly assembly,
        object options,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommand,
        CancellationToken cancellationToken = default)
    {
        var runnerType = assembly.GetType("Sussudio.Tools.DiagnosticSessionRunner")
            ?? throw new InvalidOperationException("DiagnosticSessionRunner type was not found.");
        var runAsync = runnerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method => method.Name == "RunAsync" && method.GetParameters().Length == 3)
            ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync overload was not found.");
        var task = runAsync.Invoke(null, new object?[] { options, sendCommand, cancellationToken }) as Task
            ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync did not return a Task.");

        await task.ConfigureAwait(false);
        return task.GetType().GetProperty("Result")!.GetValue(task)
            ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync returned null.");
    }

    private static JsonElement ParseDiagnosticSessionJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    internal static async Task DiagnosticSessionRunner_UnknownInitialSnapshotFailsWithoutMutatingState()
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-unknown-initial-test-{Guid.NewGuid():N}");
        var commands = new List<string>();

        try
        {
            var assembly = LoadDiagnosticSessionRunnerAssembly();
            var options = CreateDiagnosticSessionOptions(
                assembly,
                "preview-only",
                durationSeconds: 0,
                sampleIntervalMs: 100,
                outputDirectory: outputDirectory);

            Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommand = (command, _, _) =>
            {
                commands.Add(command);
                if (command is "SetPreviewEnabled" or "SetRecordingEnabled" or "SetFlashbackEnabled")
                {
                    throw new InvalidOperationException($"Unexpected state mutation command: {command}");
                }

                return Task.FromResult(ParseDiagnosticSessionJson(command == "GetPerformanceTimeline"
                    ? """
                      {
                        "Success": true,
                        "Data": []
                      }
                      """
                    : """
                      {
                        "Success": true,
                        "Message": "ok"
                      }
                      """));
            };

            var result = await RunDiagnosticSessionRunnerAsync(assembly, options, sendCommand).ConfigureAwait(false);

            AssertEqual(false, GetBoolProperty(result, "Success"), "diagnostic unknown initial result success");
            using var summaryDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(outputDirectory, "summary.json")));
            AssertJsonArrayContains(summaryDocument.RootElement.GetProperty("Warnings"), "skipped state-mutating scenario");
            AssertEqual(false, commands.Contains("SetPreviewEnabled"), "diagnostic unknown initial did not start preview");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
        static void AssertJsonArrayContains(JsonElement array, string token)
        {
            AssertEqual(JsonValueKind.Array, array.ValueKind, "diagnostic warning array kind");
            foreach (var item in array.EnumerateArray())
            {
                if ((item.GetString() ?? string.Empty).Contains(token, StringComparison.Ordinal))
                {
                    return;
                }
            }

            throw new InvalidOperationException($"Assertion failed: expected warning array to contain '{token}'.");
        }
    }

    internal static Task DiagnosticSessionRunner_ToleratesSparseSourceCadenceWarningsOnlyWithoutSourceDrops()
    {
        var assembly = LoadToolAssembly(global::Program.SsctlAssemblyRelativePath);
        var healthPolicyType = assembly.GetType("Sussudio.Tools.DiagnosticSessionHealthPolicy")
            ?? throw new InvalidOperationException("DiagnosticSessionHealthPolicy type was not found.");
        var observationType = assembly.GetType("Sussudio.Tools.DiagnosticHealthObservation")
            ?? throw new InvalidOperationException("DiagnosticHealthObservation type was not found.");
        var sourceMetricsType = assembly.GetType("Sussudio.Tools.SourceCadenceSessionMetrics")
            ?? throw new InvalidOperationException("SourceCadenceSessionMetrics type was not found.");
        var sparseSourceWarning = healthPolicyType.GetMethod(
                "IsSparseSourceCaptureCadenceWarningRun",
                BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Sparse source-cadence classifier was not found.");

        var observation = Activator.CreateInstance(
                observationType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object?[] { "Warning", "source_capture", "source gaps=1 drops=1", 85_727L, 2 },
                culture: null)
            ?? throw new InvalidOperationException("DiagnosticHealthObservation instance could not be created.");
        var metrics = Activator.CreateInstance(sourceMetricsType, nonPublic: true)
            ?? throw new InvalidOperationException("SourceCadenceSessionMetrics instance could not be created.");
        sourceMetricsType.GetProperty("MaxSevereGapCountObserved")!.SetValue(metrics, 1L);
        sourceMetricsType.GetProperty("MaxEstimatedDroppedFramesObserved")!.SetValue(metrics, 1L);
        sourceMetricsType.GetProperty("MaxDropPercentObserved")!.SetValue(metrics, 0.042);

        AssertEqual(
            true,
            (bool)sparseSourceWarning.Invoke(null, new object?[] { observation, metrics, 0L, 0L, 300, true })!,
            "sparse source cadence warning without source counter deltas");
        AssertEqual(
            false,
            (bool)sparseSourceWarning.Invoke(null, new object?[] { observation, metrics, 1L, 0L, 300, true })!,
            "source reader drop delta blocks sparse source cadence tolerance");
        AssertEqual(
            false,
            (bool)sparseSourceWarning.Invoke(null, new object?[] { observation, metrics, 0L, 1L, 300, true })!,
            "video ingest error delta blocks sparse source cadence tolerance");
        AssertEqual(
            false,
            (bool)sparseSourceWarning.Invoke(null, new object?[] { observation, metrics, 0L, 0L, 300, false })!,
            "unhealthy visual cadence blocks sparse source cadence tolerance");

        sourceMetricsType.GetProperty("MaxEstimatedDroppedFramesObserved")!.SetValue(metrics, 3L);
        AssertEqual(
            false,
            (bool)sparseSourceWarning.Invoke(null, new object?[] { observation, metrics, 0L, 0L, 300, true })!,
            "repeated source cadence drops block sparse source cadence tolerance");

        return Task.CompletedTask;
    }

    internal static async Task DiagnosticSessionRunner_RetriesSyntheticPipeConnectFailures()
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-connect-retry-test-{Guid.NewGuid():N}");
        var getSnapshotAttempts = 0;

        try
        {
            var assembly = LoadDiagnosticSessionRunnerAssembly();
            var options = CreateDiagnosticSessionOptions(
                assembly,
                "observe",
                durationSeconds: 0,
                sampleIntervalMs: 100,
                outputDirectory: outputDirectory);

            Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommand = (command, _, _) =>
            {
                if (command == "GetSnapshot")
                {
                    getSnapshotAttempts++;
                    if (getSnapshotAttempts <= 2)
                    {
                        return Task.FromResult(ParseDiagnosticSessionJson("""
                            {
                              "Success": false,
                              "Status": "error",
                              "CommandLifecycle": "failed",
                              "Message": "Sussudio is not running or not responding. Start the app and try again.",
                              "ErrorCode": "pipe-connect-failed"
                            }
                            """));
                    }

                    return Task.FromResult(ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Snapshot": {
                            "IsPreviewing": false,
                            "IsRecording": false,
                            "FlashbackActive": false,
                            "DiagnosticHealthStatus": "Healthy",
                            "DiagnosticLikelyStage": "none",
                            "DiagnosticSummary": "No degraded frame lane detected.",
                            "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                            "FrameLedgerRecentEvents": []
                          }
                        }
                        """));
                }

                if (command == "GetPerformanceTimeline")
                {
                    return Task.FromResult(ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Data": []
                        }
                        """));
                }

                return Task.FromResult(ParseDiagnosticSessionJson("""
                    {
                      "Success": true,
                      "Message": "ok"
                    }
                    """));
            };

            var result = await RunDiagnosticSessionRunnerAsync(assembly, options, sendCommand).ConfigureAwait(false);

            AssertEqual(true, GetBoolProperty(result, "Success"), "diagnostic synthetic connect retry result success");
            AssertEqual(true, getSnapshotAttempts >= 3, "diagnostic synthetic connect failure was retried");

            using var summaryDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(outputDirectory, "summary.json")));
            AssertEqual(true, summaryDocument.RootElement.GetProperty("Success").GetBoolean(), "diagnostic synthetic connect retry summary success");
            AssertEqual("completed", summaryDocument.RootElement.GetProperty("TerminalState").GetString(), "diagnostic synthetic connect retry terminal state");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    internal static async Task DiagnosticSessionRunner_RejectsConcurrentInvocationOnSameOutputDirectory()
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-concurrent-lock-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var lockPath = Path.Combine(outputDirectory, ".sussudio-diag.lock");

        // Simulate a concurrent in-flight diagnostic session by holding the same exclusive
        // lock file the runner uses. A second RunAsync against this OutputDirectory must
        // fail fast with InvalidOperationException rather than corrupt the artifact set.
        FileStream? holderLock = null;
        try
        {
            holderLock = new FileStream(
                lockPath,
                FileMode.OpenOrCreate,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1,
                FileOptions.DeleteOnClose);

            var assembly = LoadDiagnosticSessionRunnerAssembly();
            var runnerType = assembly.GetType("Sussudio.Tools.DiagnosticSessionRunner")
                ?? throw new InvalidOperationException("DiagnosticSessionRunner type was not found.");
            var options = CreateDiagnosticSessionOptions(
                assembly,
                scenario: "observe",
                durationSeconds: 0,
                sampleIntervalMs: 100,
                outputDirectory);

            Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommand = (_, _, _) =>
                Task.FromResult(ParseDiagnosticSessionJson("""
                    {
                      "Success": true,
                      "Message": "should-not-be-called"
                    }
                    """));

            var runAsync = runnerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "RunAsync" && method.GetParameters().Length == 3)
                ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync overload was not found.");

            Exception? captured = null;
            try
            {
                var task = runAsync.Invoke(null, new object?[] { options, sendCommand, CancellationToken.None }) as Task
                    ?? throw new InvalidOperationException("DiagnosticSessionRunner.RunAsync did not return a Task.");
                await task.ConfigureAwait(false);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                captured = ex.InnerException;
            }
            catch (Exception ex)
            {
                captured = ex;
            }

            if (captured is null)
            {
                throw new InvalidOperationException("Assertion failed: expected concurrent invocation to throw, but RunAsync completed.");
            }

            AssertEqual(typeof(InvalidOperationException), captured.GetType(), "diagnostic concurrent invocation exception type");
            AssertContains(captured.Message ?? string.Empty, "Another diagnostic session");

            // Artifacts must NOT have been written; only the lock file should exist.
            AssertEqual(false, File.Exists(Path.Combine(outputDirectory, "summary.json")), "diagnostic concurrent invocation must not write summary");
            AssertEqual(false, File.Exists(Path.Combine(outputDirectory, "session-live.json")), "diagnostic concurrent invocation must not write live state");
        }
        finally
        {
            holderLock?.Dispose();
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    internal static async Task DiagnosticSessionRunner_FinalSnapshotFailureWritesTerminalArtifacts()
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-failure-test-{Guid.NewGuid():N}");
        var getSnapshotCount = 0;

        try
        {
            var assembly = LoadDiagnosticSessionRunnerAssembly();
            var options = CreateDiagnosticSessionOptions(
                assembly,
                scenario: "observe",
                durationSeconds: 0,
                sampleIntervalMs: 100,
                outputDirectory);

            Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommand = (command, _, _) =>
            {
                if (command == "GetSnapshot")
                {
                    getSnapshotCount++;
                    if (getSnapshotCount == 3)
                    {
                        throw new InvalidOperationException("simulated final snapshot failure");
                    }

                    return Task.FromResult(ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Snapshot": {
                            "IsPreviewing": false,
                            "IsRecording": false,
                            "FlashbackActive": false,
                            "DiagnosticHealthStatus": "Healthy",
                            "DiagnosticLikelyStage": "none",
                            "DiagnosticSummary": "No degraded frame lane detected.",
                            "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                            "FrameLedgerRecentEvents": []
                          }
                        }
                        """));
                }

                if (command == "GetPerformanceTimeline")
                {
                    return Task.FromResult(ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Data": []
                        }
                        """));
                }

                return Task.FromResult(ParseDiagnosticSessionJson("""
                    {
                      "Success": true,
                      "Message": "ok"
                    }
                    """));
            };

            var result = await RunDiagnosticSessionRunnerAsync(assembly, options, sendCommand).ConfigureAwait(false);

            AssertEqual(false, GetBoolProperty(result, "Success"), "diagnostic failure result success");
            AssertEqual("failed", GetPropertyValue(result, "TerminalState") as string, "diagnostic failure terminal state");
            AssertEqual("final-snapshot", GetPropertyValue(result, "LastStage") as string, "diagnostic failure last stage");
            AssertContains(GetPropertyValue(result, "UnhandledException") as string ?? string.Empty, "InvalidOperationException");

            var summaryPath = Path.Combine(outputDirectory, "summary.json");
            var livePath = Path.Combine(outputDirectory, "session-live.json");
            AssertEqual(true, File.Exists(summaryPath), "diagnostic failure summary artifact");
            AssertEqual(true, File.Exists(livePath), "diagnostic failure live artifact");

            using var summaryDocument = JsonDocument.Parse(File.ReadAllText(summaryPath));
            AssertEqual(false, summaryDocument.RootElement.GetProperty("Success").GetBoolean(), "diagnostic failure summary success");
            AssertEqual("failed", summaryDocument.RootElement.GetProperty("TerminalState").GetString(), "diagnostic failure summary terminal state");
            AssertEqual("final-snapshot", summaryDocument.RootElement.GetProperty("LastStage").GetString(), "diagnostic failure summary last stage");
            AssertJsonArrayContains(summaryDocument.RootElement.GetProperty("Warnings"), "final-snapshot");

            using var liveDocument = JsonDocument.Parse(File.ReadAllText(livePath));
            AssertEqual("failed", liveDocument.RootElement.GetProperty("TerminalState").GetString(), "diagnostic failure live terminal state");
            AssertEqual("final-snapshot", liveDocument.RootElement.GetProperty("LastStage").GetString(), "diagnostic failure live last stage");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }

        static void AssertJsonArrayContains(JsonElement array, string token)
        {
            AssertEqual(JsonValueKind.Array, array.ValueKind, "diagnostic warning array kind");
            foreach (var item in array.EnumerateArray())
            {
                if ((item.GetString() ?? string.Empty).Contains(token, StringComparison.Ordinal))
                {
                    return;
                }
            }

            throw new InvalidOperationException($"Assertion failed: expected warning array to contain '{token}'.");
        }
    }

    internal static async Task DiagnosticSessionRunner_VerifiesFlashbackExportPlaybackCommandFlow()
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-export-playback-test-{Guid.NewGuid():N}");
        var requests = new List<(string Command, Dictionary<string, object?>? Payload)>();
        var getSnapshotCount = 0;
        var goLiveRequested = false;

        try
        {
            var assembly = LoadDiagnosticSessionRunnerAssembly();
            var options = CreateDiagnosticSessionOptions(
                assembly,
                scenario: "flashback-export-playback",
                durationSeconds: 0,
                sampleIntervalMs: 100,
                outputDirectory);
            options.GetType().GetProperty("LeaveRunning")!.SetValue(options, true);

            Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommand = (command, payload, _) =>
            {
                requests.Add((command, payload));
                if (command == "FlashbackAction" &&
                    string.Equals(GetPayloadString(payload, "action"), "go-live", StringComparison.OrdinalIgnoreCase))
                {
                    goLiveRequested = true;
                }

                return Task.FromResult(command switch
                {
                    "GetSnapshot" => CreateSnapshotResponse(++getSnapshotCount),
                    "GetPerformanceTimeline" => ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Data": []
                        }
                        """),
                    "WaitForCondition" => ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Message": "condition met"
                        }
                        """),
                    "FlashbackExport" => ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Message": "Exported 120 packets from 1 segments"
                        }
                        """),
                    "VerifyFile" => ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Message": "Strict verification passed.",
                          "Data": {
                            "Succeeded": true,
                            "Message": "Strict verification passed."
                          }
                        }
                        """),
                    _ => ParseDiagnosticSessionJson("""
                        {
                          "Success": true,
                          "Message": "ok"
                        }
                        """)
                });
            };

            var result = await RunDiagnosticSessionRunnerAsync(assembly, options, sendCommand).ConfigureAwait(false);

            if (!GetBoolProperty(result, "Success"))
            {
                var warnings = GetPropertyValue(result, "Warnings") as System.Collections.IEnumerable;
                var warningText = warnings == null
                    ? string.Empty
                    : string.Join(" | ", warnings.Cast<object?>().Select(item => item?.ToString() ?? string.Empty));
                throw new InvalidOperationException($"Assertion failed for flashback export playback diagnostic success: warnings={warningText}");
            }

            AssertEqual(true, requests.Any(request => request.Command == "SetFlashbackEnabled" && GetPayloadBool(request.Payload, "enabled") == true), "flashback export playback enabled Flashback");
            AssertEqual(true, requests.Any(request => request.Command == "SetPreviewEnabled" && GetPayloadBool(request.Payload, "enabled") == true), "flashback export playback started preview");
            AssertEqual(true, requests.Any(request => request.Command == "FlashbackAction" && GetPayloadString(request.Payload, "action") == "pause"), "flashback export playback pauses before seek");
            AssertEqual(true, requests.Any(request => request.Command == "FlashbackAction" && GetPayloadString(request.Payload, "action") == "seek" && GetPayloadDouble(request.Payload, "positionMs") == 1000d), "flashback export playback seeks to 1000ms");
            AssertEqual(true, requests.Any(request => request.Command == "FlashbackAction" && GetPayloadString(request.Payload, "action") == "play"), "flashback export playback starts playback");
            AssertEqual(true, requests.Any(request => request.Command == "FlashbackExport" && GetPayloadDouble(request.Payload, "seconds") == 1d), "flashback export playback exports one second");
            AssertEqual(true, requests.Any(request => request.Command == "VerifyFile" && GetPayloadString(request.Payload, "verificationProfile") == "flashback-export"), "flashback export playback verifies export");
            AssertEqual(true, requests.Any(request => request.Command == "FlashbackAction" && GetPayloadString(request.Payload, "action") == "go-live"), "flashback export playback returns live");

            using var summaryDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(outputDirectory, "summary.json")));
            AssertEqual(true, summaryDocument.RootElement.GetProperty("Success").GetBoolean(), "flashback export playback summary success");
            AssertJsonArrayContains(summaryDocument.RootElement.GetProperty("Actions"), "flashback export during playback verified");
            AssertJsonArrayContains(summaryDocument.RootElement.GetProperty("Actions"), "flashback export playback go-live requested");
            AssertEqual(0, summaryDocument.RootElement.GetProperty("Warnings").GetArrayLength(), "flashback export playback summary warning count");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }

        JsonElement CreateSnapshotResponse(int snapshotIndex)
        {
            if (snapshotIndex == 1)
            {
                return ParseDiagnosticSessionJson("""
                    {
                      "Success": true,
                      "Snapshot": {
                        "IsPreviewing": false,
                        "IsRecording": false,
                        "FlashbackActive": false,
                        "FlashbackPlaybackState": "Live",
                        "DiagnosticHealthStatus": "Healthy",
                        "DiagnosticLikelyStage": "none",
                        "DiagnosticSummary": "No degraded frame lane detected.",
                        "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                        "FrameLedgerRecentEvents": []
                      }
                    }
                    """);
            }

            var playbackState = !goLiveRequested && snapshotIndex >= 5 ? "Playing" : "Live";
            var playbackFrames = snapshotIndex <= 4 ? 0 : snapshotIndex * 16;
            return ParseDiagnosticSessionJson($$"""
                {
                  "Success": true,
                  "Snapshot": {
                    "IsPreviewing": true,
                    "IsRecording": false,
                    "FlashbackActive": true,
                    "FlashbackBufferedDurationMs": 12000,
                    "FlashbackEncodedFrames": 360,
                    "FlashbackPlaybackState": "{{playbackState}}",
                    "FlashbackPlaybackFrameCount": {{playbackFrames}},
                    "FlashbackPlaybackPendingCommands": 0,
                    "FlashbackPlaybackCommandsDropped": 0,
                    "FlashbackPlaybackCommandsSkippedNotReady": 0,
                    "FlashbackPlaybackSubmitFailures": 0,
                    "FlashbackPlaybackScrubUpdatesCoalesced": 0,
                    "FlashbackPlaybackSeekCommandsCoalesced": 0,
                    "FlashbackExportActive": false,
                    "FlashbackExportStatus": "Succeeded",
                    "FlashbackExportMessage": "Exported 120 packets from 1 segments",
                    "FlashbackExportOutputPath": "flashback-export-playback.mp4",
                    "ExpectedCaptureFrameRate": 120,
                    "SelectedExactFrameRate": 120,
                    "PreviewCadenceObservedFps": 120,
                    "VisualCadenceChangeFps": 120,
                    "VisualCadenceRepeatPercent": 0,
                    "DiagnosticHealthStatus": "Healthy",
                    "DiagnosticLikelyStage": "none",
                    "DiagnosticSummary": "No degraded frame lane detected.",
                    "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                    "FrameLedgerRecentEvents": []
                  }
                }
                """);
        }

        static string? GetPayloadString(Dictionary<string, object?>? payload, string name)
            => payload != null && payload.TryGetValue(name, out var value) ? value?.ToString() : null;

        static bool? GetPayloadBool(Dictionary<string, object?>? payload, string name)
            => payload != null && payload.TryGetValue(name, out var value) && value is bool boolValue ? boolValue : null;

        static double? GetPayloadDouble(Dictionary<string, object?>? payload, string name)
            => payload != null && payload.TryGetValue(name, out var value) && value is IConvertible convertible
                ? convertible.ToDouble(CultureInfo.InvariantCulture)
                : null;

        static void AssertJsonArrayContains(JsonElement array, string token)
        {
            AssertEqual(JsonValueKind.Array, array.ValueKind, "flashback export playback action array kind");
            foreach (var item in array.EnumerateArray())
            {
                if ((item.GetString() ?? string.Empty).Contains(token, StringComparison.Ordinal))
                {
                    return;
                }
            }

            throw new InvalidOperationException($"Assertion failed: expected array to contain '{token}'.");
        }
    }

    internal static async Task McpDiagnosticSessionTool_RecordsSnapshotArtifacts()
    {
        var diagnosticSessionTools = RequireMcpType("McpServer.Tools.DiagnosticSessionTools");
        var pipeName = NewMcpToolPipeName("diag-session");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-test-{Guid.NewGuid():N}");
        var result = string.Empty;
        object? toolResult = null;

        try
        {
            var requests = await CapturePipeRequestsAsync(
                    pipeName,
                    expectedCount: 4,
                    async () =>
                    {
                        toolResult = await InvokeMcpToolResultAsync(
                                diagnosticSessionTools,
                                "run_diagnostic_session",
                                pipeClient,
                                "observe",
                                0,
                                100,
                                outputDirectory,
                                false,
                                null,
                                false,
                                false)
                            .ConfigureAwait(false);
                        result = GetMcpToolResultText(toolResult);
                    },
                    i => i switch
                    {
                        0 => """
                             {
                               "Success": true,
                               "Snapshot": {
                                 "IsPreviewing": false,
                                 "IsRecording": false,
                                 "FlashbackActive": false,
                                 "DiagnosticHealthStatus": "Idle",
                                 "DiagnosticLikelyStage": "diagnostic_unavailable",
                                 "DiagnosticSummary": "Preview and recording are idle.",
                                 "DiagnosticEvidence": "Start preview or recording to collect live frame-lane diagnostics.",
                                 "PreviewD3DFrameStatsMissedRefreshCount": 4,
                                 "PreviewD3DFrameStatsFailureCount": 1,
                                 "FrameLedgerRecentEvents": []
                               }
                             }
                             """,
                        1 => """
                             {
                               "Success": true,
                               "Snapshot": {
                                 "DiagnosticHealthStatus": "Healthy",
                                 "DiagnosticLikelyStage": "none",
                                 "DiagnosticSummary": "No degraded frame lane detected.",
                                 "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                                 "PreviewD3DFrameStatsMissedRefreshCount": 7,
                                 "PreviewD3DFrameStatsFailureCount": 2,
                                 "PreviewD3DRecentSlowFrames": [
                                   {
                                     "SlowReason": "present_interval",
                                     "WorstOverBudgetMs": 1.5,
                                     "PresentIntervalMs": 9.8,
                                     "TotalFrameCpuMs": 4.2,
                                     "PresentCallMs": 0.7,
                                     "PendingFrameCount": 1
                                   }
                                 ],
                                 "FrameLedgerRecentEvents": [
                                   {
                                     "SourceSequence": 7,
                                     "Stage": "CaptureArrived",
                                     "QpcTimestamp": 123456,
                                     "Accepted": true
                                   }
                                 ]
                               }
                             }
                             """,
                        2 => """
                             {
                               "Success": true,
                               "Data": [
                                 {
                                   "TimestampUtc": "2026-04-26T00:00:00Z",
                                   "PerformanceScore": 100
                                 }
                               ]
                             }
                             """,
                        _ => """
                             {
                               "Success": true,
                               "Snapshot": {
                                 "DiagnosticHealthStatus": "Healthy",
                                 "DiagnosticLikelyStage": "none",
                                 "DiagnosticSummary": "No degraded frame lane detected.",
                                 "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                                 "FrameLedgerRecentEvents": []
                               }
                             }
                             """
                    })
                .ConfigureAwait(false);

            AssertCommandRequest(requests[0], "GetSnapshot");
            AssertCommandRequest(requests[1], "GetSnapshot");
            AssertCommandRequest(requests[2], "GetPerformanceTimeline", ("maxEntries", 240));
            AssertCommandRequest(requests[3], "GetSnapshot");
            AssertEqual(false, GetMcpToolResultIsError(toolResult), "diagnostic session success MCP isError");
            AssertContains(result, "== Diagnostic Session: PASS ==");
            AssertContains(result, "Health: Healthy | Stage: none");
            AssertContains(result, "Preview D3D Perf: onePercentLowFpsEnd=0 onePercentLowFpsMin=0 missedRefreshDelta=3 statsFailureDelta=1 maxRecentSlowFrames=1 latestSlowReason=present_interval overBudgetMs=1.5 presentIntervalMs=9.8 totalFrameCpuMs=4.2 presentCallMs=0.7 pending=1");
            AssertContains(result, "Frame Ledger:");

            var summaryPath = Path.Combine(outputDirectory, "summary.json");
            var livePath = Path.Combine(outputDirectory, "session-live.json");
            var samplesPath = Path.Combine(outputDirectory, "samples.json");
            var frameLedgerPath = Path.Combine(outputDirectory, "frame-ledger.json");
            AssertEqual(true, File.Exists(summaryPath), "diagnostic session summary artifact");
            AssertEqual(true, File.Exists(livePath), "diagnostic session live artifact");
            AssertEqual(true, File.Exists(samplesPath), "diagnostic session samples artifact");
            AssertEqual(true, File.Exists(frameLedgerPath), "diagnostic session frame ledger artifact");
            AssertContains(result, $"Live: {livePath}");

            using var summaryDocument = JsonDocument.Parse(File.ReadAllText(summaryPath));
            AssertEqual("completed", summaryDocument.RootElement.GetProperty("TerminalState").GetString(), "diagnostic session terminal state");
            AssertEqual("summary", summaryDocument.RootElement.GetProperty("LastStage").GetString(), "diagnostic session last stage");
            AssertEqual(true, summaryDocument.RootElement.GetProperty("RunnerProcessId").GetInt32() > 0, "diagnostic session runner pid");
            AssertEqual(livePath, summaryDocument.RootElement.GetProperty("LivePath").GetString(), "diagnostic session live path");

            using var liveDocument = JsonDocument.Parse(File.ReadAllText(livePath));
            AssertEqual("completed", liveDocument.RootElement.GetProperty("TerminalState").GetString(), "diagnostic live terminal state");
            AssertEqual("summary-written", liveDocument.RootElement.GetProperty("LastStage").GetString(), "diagnostic live last stage");
            AssertEqual("Healthy", liveDocument.RootElement.GetProperty("HealthStatus").GetString(), "diagnostic live health status");
            AssertEqual("none", liveDocument.RootElement.GetProperty("LikelyStage").GetString(), "diagnostic live likely stage");
            AssertEqual(0, liveDocument.RootElement.GetProperty("WarningCount").GetInt32(), "diagnostic live warning count");
            AssertEqual(string.Empty, liveDocument.RootElement.GetProperty("LastWarning").GetString(), "diagnostic live last warning");

            using var frameLedgerDocument = JsonDocument.Parse(File.ReadAllText(frameLedgerPath));
            AssertEqual(1, frameLedgerDocument.RootElement.GetProperty("EventCount").GetInt32(), "diagnostic session frame ledger event count");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    internal static async Task McpDiagnosticSessionTool_SurfacesDiagnosticFailureAsToolError()
    {
        var diagnosticSessionTools = RequireMcpType("McpServer.Tools.DiagnosticSessionTools");
        var pipeName = NewMcpToolPipeName("diag-session-failure");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-session-health-test-{Guid.NewGuid():N}");
        object? toolResult = null;
        var result = string.Empty;

        try
        {
            var requests = await CapturePipeRequestsAsync(
                    pipeName,
                    expectedCount: 4,
                    async () =>
                    {
                        toolResult = await InvokeMcpToolResultAsync(
                                diagnosticSessionTools,
                                "run_diagnostic_session",
                                pipeClient,
                                "observe",
                                0,
                                100,
                                outputDirectory,
                                false,
                                null,
                                false,
                                false)
                            .ConfigureAwait(false);
                        result = GetMcpToolResultText(toolResult);
                    },
                    i => i switch
                    {
                        0 => """
                             {
                               "Success": true,
                               "Snapshot": {
                                 "IsPreviewing": true,
                                 "IsRecording": false,
                                 "FlashbackActive": true,
                                 "DiagnosticHealthStatus": "Healthy",
                                 "DiagnosticLikelyStage": "none",
                                 "DiagnosticSummary": "No degraded frame lane detected.",
                                 "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                                 "FrameLedgerRecentEvents": []
                               }
                             }
                             """,
                        1 => """
                             {
                               "Success": true,
                               "Snapshot": {
                                 "IsPreviewing": true,
                                 "IsRecording": false,
                                 "FlashbackActive": true,
                                 "DiagnosticHealthStatus": "Critical",
                                 "DiagnosticLikelyStage": "flashback_playback",
                                 "DiagnosticSummary": "Playback cadence collapsed.",
                                 "DiagnosticEvidence": "1pctLow=5fps target=120fps",
                                 "FrameLedgerRecentEvents": []
                               }
                             }
                             """,
                        2 => """
                             {
                               "Success": true,
                               "Data": []
                             }
                             """,
                        _ => """
                             {
                               "Success": true,
                               "Snapshot": {
                                 "IsPreviewing": true,
                                 "IsRecording": false,
                                 "FlashbackActive": true,
                                 "DiagnosticHealthStatus": "Healthy",
                                 "DiagnosticLikelyStage": "none",
                                 "DiagnosticSummary": "No degraded frame lane detected.",
                                 "DiagnosticEvidence": "All monitored frame lanes are within current thresholds.",
                                 "FrameLedgerRecentEvents": []
                               }
                             }
                             """
                    })
                .ConfigureAwait(false);

            AssertCommandRequest(requests[0], "GetSnapshot");
            AssertCommandRequest(requests[1], "GetSnapshot");
            AssertCommandRequest(requests[2], "GetPerformanceTimeline", ("maxEntries", 240));
            AssertCommandRequest(requests[3], "GetSnapshot");
            AssertEqual(true, GetMcpToolResultIsError(toolResult), "diagnostic session failure MCP isError");
            AssertContains(result, "== Diagnostic Session: FAIL ==");
            AssertContains(result, "diagnostic health degraded during session");
            AssertContains(result, "health=Critical");

            var summaryPath = Path.Combine(outputDirectory, "summary.json");
            var livePath = Path.Combine(outputDirectory, "session-live.json");
            AssertEqual(true, File.Exists(summaryPath), "diagnostic health failure summary artifact");
            AssertEqual(true, File.Exists(livePath), "diagnostic health failure live artifact");

            using var summaryDocument = JsonDocument.Parse(File.ReadAllText(summaryPath));
            AssertEqual(false, summaryDocument.RootElement.GetProperty("Success").GetBoolean(), "diagnostic health failure summary success");
            AssertJsonArrayContains(summaryDocument.RootElement.GetProperty("Warnings"), "diagnostic health degraded during session");

            using var liveDocument = JsonDocument.Parse(File.ReadAllText(livePath));
            AssertEqual("Critical", liveDocument.RootElement.GetProperty("HealthStatus").GetString(), "diagnostic health failure live health");
            AssertEqual("flashback_playback", liveDocument.RootElement.GetProperty("LikelyStage").GetString(), "diagnostic health failure live stage");
            AssertEqual(1, liveDocument.RootElement.GetProperty("WarningCount").GetInt32(), "diagnostic health failure live warning count");
            AssertContains(liveDocument.RootElement.GetProperty("LastWarning").GetString() ?? string.Empty, "health=Critical");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }

        static void AssertJsonArrayContains(JsonElement array, string token)
        {
            AssertEqual(JsonValueKind.Array, array.ValueKind, "diagnostic health warning array kind");
            foreach (var item in array.EnumerateArray())
            {
                if ((item.GetString() ?? string.Empty).Contains(token, StringComparison.Ordinal))
                {
                    return;
                }
            }

            throw new InvalidOperationException($"Assertion failed: expected warning array to contain '{token}'.");
        }
    }

    internal static Task DiagnosticSessionFlashbackValidation_OwnsFlashbackWarningPolicy()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var validationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSupport.cs")
            .Replace("\r\n", "\n");

        AssertContains(validationText, "internal static class DiagnosticSessionFlashbackValidation");
        AssertContains(validationText, "internal static void ValidateFlashbackRecordingSession(");
        AssertContains(validationText, "\"flashback recording: no Flashback video frames submitted to encoder\"");
        AssertContains(validationText, "internal static void ValidateFlashbackPlaybackSession(");
        AssertContains(validationText, "\"flashback playback: no playback frames were observed\"");
        AssertContains(validationText, "\"flashback playback: absolute A/V drift exceeded budget");
        AssertContains(validationText, "internal static void ValidateFlashbackPreviewScheduler(");
        AssertContains(validationText, "\"flashback preview: present/display pressure \"");
        AssertContains(validationText, "latestSlowReason={FormatOptional(previewD3DMetrics.LatestSlowFrameReason)}");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionFlashbackValidation;");
        AssertDoesNotContain(runnerText, "private static void ValidateFlashbackRecordingSession(");
        AssertDoesNotContain(runnerText, "private static void ValidateFlashbackPlaybackSession(");
        AssertDoesNotContain(runnerText, "private static void ValidateFlashbackPreviewScheduler(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionRunner_IgnoresTransientFlashbackWarmupWarnings()
    {
        var assembly = LoadToolAssembly(global::Program.SsctlAssemblyRelativePath);
        var healthPolicyType = assembly.GetType("Sussudio.Tools.DiagnosticSessionHealthPolicy")
            ?? throw new InvalidOperationException("DiagnosticSessionHealthPolicy type was not found.");
        var sampleType = assembly.GetType("Sussudio.Tools.DiagnosticSessionSample")
            ?? throw new InvalidOperationException("DiagnosticSessionSample type was not found.");
        var buildObservation = healthPolicyType.GetMethod(
                "BuildSessionDiagnosticHealthObservation",
                BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("BuildSessionDiagnosticHealthObservation was not found.");

        var samples = CreateDiagnosticSessionSampleList(
            sampleType,
            (1_000, CreateDiagnosticSnapshot("Warning", "flashback_playback", "startup 1% low")),
            (12_000, CreateDiagnosticSnapshot("Healthy", "none", "warmed")));
        var finalSnapshot = CreateDiagnosticSnapshot("Healthy", "none", "final");
        var transientWarningObservation = buildObservation.Invoke(
                null,
                new object?[] { samples, finalSnapshot, true })
            ?? throw new InvalidOperationException("Transient warning observation was null.");
        AssertEqual("Healthy", GetPropertyValue(transientWarningObservation, "HealthStatus") as string, "flashback warmup health status");
        AssertEqual("none", GetPropertyValue(transientWarningObservation, "LikelyStage") as string, "flashback warmup likely stage");

        var criticalSamples = CreateDiagnosticSessionSampleList(
            sampleType,
            (1_000, CreateDiagnosticSnapshot("Critical", "flashback_playback", "startup crash")),
            (12_000, CreateDiagnosticSnapshot("Healthy", "none", "warmed")));
        var criticalObservation = buildObservation.Invoke(
                null,
                new object?[] { criticalSamples, finalSnapshot, true })
            ?? throw new InvalidOperationException("Critical observation was null.");
        AssertEqual("Critical", GetPropertyValue(criticalObservation, "HealthStatus") as string, "flashback critical health status");
        AssertEqual("flashback_playback", GetPropertyValue(criticalObservation, "LikelyStage") as string, "flashback critical likely stage");

        return Task.CompletedTask;

        static object CreateDiagnosticSessionSampleList(Type sampleType, params (long OffsetMs, JsonElement Snapshot)[] values)
        {
            var listType = typeof(List<>).MakeGenericType(sampleType);
            var list = (System.Collections.IList)(Activator.CreateInstance(listType)
                ?? throw new InvalidOperationException("DiagnosticSessionSample list could not be created."));
            foreach (var value in values)
            {
                var sample = Activator.CreateInstance(sampleType)
                    ?? throw new InvalidOperationException("DiagnosticSessionSample instance could not be created.");
                sampleType.GetProperty("OffsetMs")!.SetValue(sample, value.OffsetMs);
                sampleType.GetProperty("TimestampUtc")!.SetValue(sample, DateTimeOffset.UtcNow);
                sampleType.GetProperty("Snapshot")!.SetValue(sample, value.Snapshot);
                list.Add(sample);
            }

            return list;
        }

        static JsonElement CreateDiagnosticSnapshot(string health, string stage, string evidence)
        {
            using var document = JsonDocument.Parse($$"""
                {
                  "DiagnosticHealthStatus": "{{health}}",
                  "DiagnosticLikelyStage": "{{stage}}",
                  "DiagnosticEvidence": "{{evidence}}"
                }
                """);
            return document.RootElement.Clone();
        }
    }

    internal static Task DiagnosticSessionFlashbackWaits_OwnsSnapshotPollingWaits()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var setupText = ReadDiagnosticSessionScenarioSetupSource();
        var waitsText = ReadDiagnosticSessionFlashbackWaitsSource();

        AssertContains(waitsText, "internal static class DiagnosticSessionFlashbackWaits");
        AssertDoesNotContain(waitsText, "internal static partial class DiagnosticSessionFlashbackWaits");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForFlashbackPlaybackBoundaryCrossAsync(");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForFlashbackPlaybackStateAsync(");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertContains(waitsText, "internal static async Task<bool> WaitForFlashbackPlaybackPositionAsync(");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForFlashbackActiveAsync(");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForPreviewActiveAsync(");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForFlashbackRecordingReadyAsync(");
        AssertContains(waitsText, "internal static async Task<bool> WaitForFlashbackStressBufferReadyAsync(");
        AssertContains(waitsText, "FlashbackPlaybackPendingCommands");
        AssertContains(waitsText, "FlashbackPlaybackFrameCount");
        AssertContains(waitsText, "RecordingBackend");
        AssertContains(waitsText, "RecordingFileGrowing");
        AssertContains(waitsText, "FlashbackBufferedDurationMs");
        AssertContains(waitsText, "requiredEncodedFrames");
        AssertContains(waitsText, "string expectedState");
        AssertContains(waitsText, "positionMs >= boundaryMs + 1_500");
        AssertContains(waitsText, "FlashbackPlaybackTargetFps");
        AssertContains(waitsText, "SelectedExactFrameRate");
        AssertContains(waitsText, "Math.Abs(position - targetPositionMs) <= 1_500");
        AssertContains(setupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForFlashbackPlaybackBoundaryCrossAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForFlashbackPlaybackStateAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<bool> WaitForFlashbackPlaybackPositionAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForFlashbackActiveAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForPreviewActiveAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForFlashbackRecordingReadyAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<bool> WaitForFlashbackStressBufferReadyAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackStressScenario_OwnsStressFlow()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var stressText = ReadDiagnosticSessionFlashbackStressScenarioSource();

        AssertContains(stressText, "internal static class DiagnosticSessionFlashbackStressScenario");
        AssertDoesNotContain(stressText, "internal static partial class DiagnosticSessionFlashbackStressScenario");
        AssertContains(stressText, "internal const int FlashbackStressMaxPlaybackPendingCommands = 4;");
        AssertContains(stressText, "internal const int FlashbackStressMaxPlaybackCommandLatencyMs = 750;");
        AssertContains(stressText, "internal const double FlashbackStressPlaybackWarmSeconds = 10.0;");
        AssertContains(stressText, "internal const long FlashbackStressAudioUnavailableFallbackAllowance = 4;");
        AssertContains(stressText, "internal const int FlashbackScrubStressMaxPlaybackPendingCommands = 20;");
        AssertContains(stressText, "internal static async Task RunFlashbackStressAsync(");
        AssertContains(stressText, "ValidateFlashbackStressWarmPlaybackAsync(");
        AssertContains(stressText, "private static async Task VerifyFlashbackStressExportAsync(");
        AssertContains(stressText, "\"flashback-stress-export.mp4\"");
        AssertContains(stressText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(stressText, "flashback stress export verified");
        AssertContains(stressText, "private static async Task ValidateFlashbackStressWarmPlaybackAsync(");
        AssertContains(stressText, "WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertContains(stressText, "\"flashback playback warmed frames=");
        AssertContains(stressText, "private readonly record struct FlashbackStressWarmPlaybackAudioBaseline(");
        AssertContains(stressText, "private readonly record struct FlashbackStressWarmPlaybackAudioDeltas(");
        AssertContains(stressText, "private static FlashbackStressWarmPlaybackAudioBaseline CaptureFlashbackStressWarmPlaybackAudioBaseline(");
        AssertContains(stressText, "private static FlashbackStressWarmPlaybackAudioDeltas CaptureFlashbackStressWarmPlaybackAudioDeltas(");
        AssertContains(stressText, "FlashbackPlaybackAudioMasterUnavailableFallbacks");
        AssertContains(stressText, "FlashbackPlaybackAudioMasterLastFallbackReason");
        AssertContains(stressText, "private static async Task ValidateFlashbackStressCommandDrainAsync(");
        AssertContains(stressText, "BuildPlaybackCommandHealth(lastSnapshot, baselineSnapshot)");
        AssertContains(stressText, "\"flashback stress: playback command queue did not drain within 10s \"");
        AssertContains(stressText, "private readonly record struct FlashbackStressPlaybackDrainResult(");
        AssertContains(stressText, "private static async Task<FlashbackStressPlaybackDrainResult> WaitForFlashbackStressPlaybackCommandDrainAsync(");
        AssertContains(stressText, "GetInt(lastSnapshot, \"FlashbackPlaybackPendingCommands\") == 0");
        AssertContains(stressText, "GetString(lastSnapshot, \"FlashbackPlaybackState\")");
        AssertContains(stressText, "internal static async Task RunFlashbackScrubStressAsync(");
        AssertContains(stressText, "WaitForFlashbackStressBufferReadyAsync(");
        AssertContains(stressText, "new Dictionary<string, object?> { [\"action\"] = \"begin-scrub\", [\"positionMs\"] = 500 }");
        AssertContains(stressText, "private static async Task<int> RunFlashbackScrubStressUpdateBurstAsync(");
        AssertContains(stressText, "new Dictionary<string, object?> { [\"action\"] = \"update-scrub\", [\"positionMs\"] = positions[i] }");
        AssertContains(stressText, "return positions[^1];");
        AssertContains(stressText, "flashback scrub stress: {failedUpdates} update-scrub command(s) failed");
        AssertContains(stressText, "new Dictionary<string, object?> { [\"action\"] = \"end-scrub\", [\"positionMs\"] = finalScrubPositionMs }");
        AssertContains(stressText, "private static async Task ValidateFlashbackScrubStressDrainAsync(");
        AssertContains(stressText, "\"flashback scrub stress: playback did not settle live with an empty queue within 10s \"");
        AssertContains(stressText, "FlashbackScrubStressMaxPlaybackPendingCommands");
        AssertContains(stressText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(stressText, "internal static string? ClassifyFlashbackStressAudioMasterFallbackWarning(");
        AssertContains(stressText, "\"flashback stress: audio-master harmful fallbacks increased during warmed playback \"");
        AssertContains(stressText, "internal static void RegisterSelectedFlashbackStressScenarioTasks(");
        AssertContains(stressText, "1,\n                \"flashback-stress-task\",");
        AssertContains(stressText, "3,\n                \"flashback-scrub-stress-task\",");
        AssertContains(stressText, "RunFlashbackStressAsync(");
        AssertContains(stressText, "RunFlashbackScrubStressAsync(");
        AssertContains(stressText, "sendRawWithConnectRetryAsync");
        AssertContains(stressText, "actions.Add(\"flashback stress started\")");
        AssertContains(stressText, "actions.Add(\"flashback scrub stress started\")");
        AssertContains(startupText, "DiagnosticSessionFlashbackStressScenario.RegisterSelectedFlashbackStressScenarioTasks(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackStressScenario;");
        AssertDoesNotContain(startupText, "RunFlashbackStressAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackScrubStressAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackStressAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackScrubStressAsync(");
        AssertDoesNotContain(runnerText, "private static string? ClassifyFlashbackStressAudioMasterFallbackWarning(");
        AssertDoesNotContain(runnerText, "private const int FlashbackStressMaxPlaybackPendingCommands = 4;");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackStressScenario_ClassifiesAudioMasterFallbacks()
    {
        var assembly = LoadToolAssembly(global::Program.SsctlAssemblyRelativePath);
        var stressScenarioType = assembly.GetType("Sussudio.Tools.DiagnosticSessionFlashbackStressScenario")
            ?? throw new InvalidOperationException("DiagnosticSessionFlashbackStressScenario type was not found.");
        var classify = stressScenarioType.GetMethod(
                "ClassifyFlashbackStressAudioMasterFallbackWarning",
                BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Audio-master fallback classifier was not found.");

        AssertEqual((string?)null, Invoke(0, 0, 0, 0), "no audio-master fallback warning");
        AssertEqual((string?)null, Invoke(4, 4, 0, 0), "startup unavailable fallback allowance");

        var unavailable = Invoke(5, 5, 0, 0)
            ?? throw new InvalidOperationException("Expected unavailable fallback warning.");
        AssertContains(unavailable, "audio-master unavailable fallbacks exceeded startup allowance");
        AssertContains(unavailable, "unavailableDelta=5");
        AssertContains(unavailable, "allowance=4");
        AssertContains(unavailable, "totalDelta=5");

        var stale = Invoke(2, 0, 1, 0)
            ?? throw new InvalidOperationException("Expected stale fallback warning.");
        AssertContains(stale, "audio-master harmful fallbacks increased during warmed playback");
        AssertContains(stale, "staleDelta=1");
        AssertContains(stale, "driftOutlierDelta=0");

        var driftOutlier = Invoke(2, 0, 0, 1)
            ?? throw new InvalidOperationException("Expected drift-outlier fallback warning.");
        AssertContains(driftOutlier, "audio-master harmful fallbacks increased during warmed playback");
        AssertContains(driftOutlier, "staleDelta=0");
        AssertContains(driftOutlier, "driftOutlierDelta=1");

        var unclassified = Invoke(2, 0, 0, 0)
            ?? throw new InvalidOperationException("Expected unclassified fallback warning.");
        AssertContains(unclassified, "audio-master unclassified fallbacks increased during warmed playback");
        AssertContains(unclassified, "delta=2");

        return Task.CompletedTask;

        string? Invoke(long totalDelta, long unavailableDelta, long staleDelta, long driftOutlierDelta)
            => classify.Invoke(null, new object?[] { totalDelta, unavailableDelta, staleDelta, driftOutlierDelta }) as string;
    }

    internal static Task DiagnosticSessionFlashbackExportScenarios_OwnExportFlows()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var scenariosText = ReadDiagnosticSessionFlashbackExportScenariosSource();
        var rootText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExportScenarios.cs")
            .Replace("\r\n", "\n");
        var disableDuringExportText = rootText;
        var playbackText = rootText;
        var rangeText = rootText;
        var scenariosTextWithoutSpaces = scenariosText.Replace(" ", string.Empty);

        AssertContains(scenariosText, "internal static class DiagnosticSessionFlashbackExportScenarios");
        AssertContains(scenariosText, "internal static async Task RunFlashbackExportConcurrentAsync(");
        AssertContains(scenariosText, "\"flashback-concurrent-a.mp4\"");
        AssertContains(scenariosText, "flashback concurrent exports verified");
        AssertContains(scenariosText, "internal static async Task RunFlashbackDisableDuringExportAsync(");
        AssertContains(scenariosText, "\"flashback-disable-during-export.mp4\"");
        AssertContains(scenariosText, "SendCommandWithConnectRetryAsync(");
        AssertContains(disableDuringExportText, "ValidateFlashbackDisableDuringExportFileAsync(");
        AssertContains(disableDuringExportText, "ValidateFlashbackDisabledAfterExportAsync(");
        AssertContains(disableDuringExportText, "ValidateFlashbackReenabledAfterDisableDuringExportAsync(");
        AssertContains(disableDuringExportText, "private static async Task ValidateFlashbackDisableDuringExportFileAsync(");
        AssertContains(disableDuringExportText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(disableDuringExportText, "private static async Task ValidateFlashbackDisabledAfterExportAsync(");
        AssertContains(disableDuringExportText, "flashback disable during export: pending playback commands remained after disable");
        AssertContains(disableDuringExportText, "private static async Task ValidateFlashbackReenabledAfterDisableDuringExportAsync(");
        AssertContains(scenariosText, "internal static async Task RunFlashbackRotatedExportAsync(");
        AssertContains(scenariosText, "TryParseFlashbackExportSegmentCount(exportMessage)");
        AssertContains(scenariosText, "internal static async Task RunFlashbackExportPlaybackAsync(");
        AssertContains(scenariosText, "flashback export during playback verified");
        AssertContains(playbackText, "CaptureFlashbackExportPlaybackFrameCountBeforeExportAsync(");
        AssertContains(playbackText, "ValidateFlashbackExportPlaybackAfterExportAsync(");
        AssertContains(playbackText, "ValidateFlashbackExportPlaybackFinalStateAsync(");
        AssertContains(playbackText, "private static async Task<long> CaptureFlashbackExportPlaybackFrameCountBeforeExportAsync(");
        AssertContains(playbackText, "flashback export playback: expected Playing before export");
        AssertContains(playbackText, "private static async Task ValidateFlashbackExportPlaybackAfterExportAsync(");
        AssertContains(playbackText, "flashback export playback: playback frame count did not advance during export");
        AssertContains(playbackText, "private static async Task ValidateFlashbackExportPlaybackFinalStateAsync(");
        AssertContains(playbackText, "BuildPlaybackCommandHealth(finalSnapshot, baselineSnapshot)");
        AssertContains(playbackText, "flashback export playback: pending commands remained after go-live");
        AssertContains(scenariosText, "internal static async Task RunFlashbackRangeExportAsync(");
        AssertContains(rangeText, "private static async Task<FlashbackSelectionRange?> PrepareFlashbackSelectionRangeAsync(");
        AssertContains(rangeText, "private readonly record struct FlashbackSelectionRange(");
        AssertContains(rangeText, "WaitForFlashbackStressBufferReadyAsync(");
        AssertContains(rangeText, "private static async Task MarkFlashbackSelectionPointAsync(");
        AssertContains(rangeText, "WaitForFlashbackPlaybackPositionAsync(");
        AssertContains(scenariosText, "\"clear-in-out-points\"");
        AssertContains(scenariosText, "\"set-in-point\"");
        AssertContains(scenariosText, "\"set-out-point\"");
        AssertContains(scenariosText, "[\"useSelectionRange\"] = true");
        AssertContains(scenariosText, "private static void ValidateFlashbackRangeExportResult(");
        AssertContains(scenariosText, "private static async Task ValidateFlashbackRangeExportCleanupAsync(");
        AssertContains(rootText, "internal static void RegisterSelectedFlashbackExportScenarioTasks(");
        AssertContains(rootText, "RegisterFlashbackExportPlaybackTask(");
        AssertContains(rootText, "RegisterFlashbackRangeExportTasks(");
        AssertContains(rootText, "RegisterFlashbackExportCoordinationTasks(");
        AssertContains(rootText, "backgroundTasks.AddScenario(");
        AssertContains(rootText, "private static void RegisterFlashbackExportPlaybackTask(");
        AssertContains(rootText, "6,\n            \"flashback-export-playback-task\",");
        AssertContains(rootText, "flashback export playback started");
        AssertContains(rootText, "private static void RegisterFlashbackRangeExportTasks(");
        AssertContains(rootText, "8,\n                \"flashback-range-export-task\",");
        AssertContains(rootText, "9,\n                \"flashback-range-export-audio-switch-task\",");
        AssertContains(rootText, "flashback range export audio switch started");
        AssertContains(rootText, "private static void RegisterFlashbackExportCoordinationTasks(");
        AssertContains(rootText, "10,\n                \"flashback-export-concurrent-task\",");
        AssertContains(rootText, "11,\n                \"flashback-disable-during-export-task\",");
        AssertContains(rootText, "12,\n                \"flashback-rotated-export-task\",");
        AssertContains(rootText, "flashback rotated export started");
        AssertContains(scenariosTextWithoutSpaces, "6,\n\"flashback-export-playback-task\",");
        AssertContains(scenariosTextWithoutSpaces, "8,\n\"flashback-range-export-task\",");
        AssertContains(scenariosTextWithoutSpaces, "9,\n\"flashback-range-export-audio-switch-task\",");
        AssertContains(scenariosTextWithoutSpaces, "10,\n\"flashback-export-concurrent-task\",");
        AssertContains(scenariosTextWithoutSpaces, "11,\n\"flashback-disable-during-export-task\",");
        AssertContains(scenariosTextWithoutSpaces, "12,\n\"flashback-rotated-export-task\",");
        AssertContains(scenariosText, "\"flashback-range-export-task\"");
        AssertContains(scenariosText, "actions.Add(\"flashback concurrent export started\")");
        AssertContains(startupText, "DiagnosticSessionFlashbackExportScenarios.RegisterSelectedFlashbackExportScenarioTasks(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackExportScenarios;");
        AssertDoesNotContain(startupText, "RunFlashbackExportConcurrentAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackDisableDuringExportAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackRotatedExportAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackExportPlaybackAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackRangeExportAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackExportConcurrentAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackDisableDuringExportAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRotatedExportAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackExportPlaybackAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRangeExportAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackExports_OwnsExportHelpers()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var exportScenariosText = ReadDiagnosticSessionFlashbackExportScenariosSource();
        var stressText = ReadDiagnosticSessionFlashbackStressScenarioSource();
        var exportHelpersText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSupport.cs")
            .Replace("\r\n", "\n");

        AssertContains(exportHelpersText, "internal static class DiagnosticSessionFlashbackExports");
        AssertDoesNotContain(exportHelpersText, "internal static partial class DiagnosticSessionFlashbackExports");
        AssertContains(exportHelpersText, "internal static int? TryParseFlashbackExportSegmentCount(");
        AssertContains(exportHelpersText, "const string marker = \" from \";");
        AssertContains(exportHelpersText, "suffix.Contains(\"segment\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(exportHelpersText, "internal static Dictionary<string, object?> CreateFlashbackExportVerifyPayload(string filePath)");
        AssertContains(exportHelpersText, "[\"verificationProfile\"] = \"flashback-export\"");
        AssertContains(exportHelpersText, "internal static async Task CleanupFlashbackSelectionAsync(");
        AssertContains(exportHelpersText, "\"clear-in-out-points\"");
        AssertContains(exportHelpersText, "\"go-live\"");
        AssertContains(exportHelpersText, "internal static async Task ToggleAudioEnabledDuringFlashbackExportAsync(");
        AssertContains(exportHelpersText, "\"SetAudioEnabled\"");
        AssertContains(exportScenariosText, "using static Sussudio.Tools.DiagnosticSessionFlashbackExports;");
        AssertContains(stressText, "using static Sussudio.Tools.DiagnosticSessionFlashbackExports;");
        AssertDoesNotContain(runnerText, "private static int? TryParseFlashbackExportSegmentCount(");
        AssertDoesNotContain(runnerText, "private static Dictionary<string, object?> CreateFlashbackExportVerifyPayload(");
        AssertDoesNotContain(runnerText, "private static async Task ToggleAudioEnabledDuringFlashbackExportAsync(");
        AssertDoesNotContain(runnerText, "private static async Task CleanupFlashbackSelectionAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackSegments_OwnsSegmentWaitsAndParsing()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var exportScenariosText = ReadDiagnosticSessionFlashbackExportScenariosSource();
        var segmentPlaybackText = ReadDiagnosticSessionFlashbackSegmentPlaybackScenariosSource();
        var segmentsText = ReadDiagnosticSessionFlashbackSegmentsSource();

        AssertContains(segmentsText, "internal static class DiagnosticSessionFlashbackSegments");
        AssertDoesNotContain(segmentsText, "internal static partial class DiagnosticSessionFlashbackSegments");
        AssertContains(segmentsText, "internal readonly record struct FlashbackSegmentProbe(");
        AssertContains(segmentsText, "internal readonly record struct FlashbackSegmentPlaybackTarget(");
        AssertContains(segmentsText, "internal static async Task<FlashbackSegmentProbe?> WaitForFlashbackCompletedSegmentAsync(");
        AssertContains(segmentsText, "\"FlashbackGetSegments\"");
        AssertContains(segmentsText, "internal static bool TryGetFlashbackSegments(");
        AssertContains(segmentsText, "internal static async Task<FlashbackSegmentPlaybackTarget?> WaitForFlashbackPlayableCompletedSegmentAsync(");
        AssertContains(segmentsText, "const int requiredHeadroomMs = 8_000;");
        AssertContains(segmentsText, "internal static async Task<bool> WaitForFlashbackSegmentPlaybackHeadroomAsync(");
        AssertContains(segmentsText, "data.TryGetProperty(\"Segments\", out var segmentsElement)");
        AssertContains(segmentsText, "sendCommandAsync(\"FlashbackGetSegments\", null, null)");
        AssertContains(segmentsText, "sendCommandAsync(\"GetSnapshot\", null, null)");
        AssertContains(exportScenariosText, "using static Sussudio.Tools.DiagnosticSessionFlashbackSegments;");
        AssertContains(segmentPlaybackText, "using static Sussudio.Tools.DiagnosticSessionFlashbackSegments;");
        AssertDoesNotContain(runnerText, "private readonly record struct FlashbackSegmentProbe(");
        AssertDoesNotContain(runnerText, "private readonly record struct FlashbackSegmentPlaybackTarget(");
        AssertDoesNotContain(runnerText, "private static async Task<FlashbackSegmentProbe?> WaitForFlashbackCompletedSegmentAsync(");
        AssertDoesNotContain(runnerText, "private static bool TryGetFlashbackSegments(");
        AssertDoesNotContain(runnerText, "private static async Task<FlashbackSegmentPlaybackTarget?> WaitForFlashbackPlayableCompletedSegmentAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<bool> WaitForFlashbackSegmentPlaybackHeadroomAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackCycleScenarios_OwnCycleFlows()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var cyclesText = ReadDiagnosticSessionFlashbackCycleScenariosSource();

        AssertContains(cyclesText, "internal static class DiagnosticSessionFlashbackCycleScenarios");
        AssertDoesNotContain(cyclesText, "internal static partial class DiagnosticSessionFlashbackCycleScenarios");
        AssertContains(cyclesText, "internal static async Task RunFlashbackRestartCycleAsync(");
        AssertContains(cyclesText, "\"RestartFlashback\"");
        AssertContains(cyclesText, "private static async Task<bool> ValidateFlashbackRestartCycleActiveStateAsync(");
        AssertContains(cyclesText, "FlashbackPlaybackThreadAlive");
        AssertContains(cyclesText, "pending playback commands remained after restart");
        AssertContains(cyclesText, "private static async Task VerifyFlashbackRestartCycleExportAsync(");
        AssertContains(cyclesText, "\"flashback-restart-cycle-export.mp4\"");
        AssertContains(cyclesText, "flashback restart cycle export verified");
        AssertContains(cyclesText, "internal static async Task RunFlashbackEncoderCycleAsync(");
        AssertContains(cyclesText, "var cycledPreset = string.Equals(originalPreset, \"P1\", StringComparison.OrdinalIgnoreCase) ? \"P2\" : \"P1\";");
        AssertContains(cyclesText, "ValidateFlashbackEncoderCycleSnapshot(afterSnapshot, originalFilePath, warnings);");
        AssertContains(cyclesText, "private static void ValidateFlashbackEncoderCycleSnapshot(");
        AssertContains(cyclesText, "post-cycle encoder did not reach readiness frame count");
        AssertContains(cyclesText, "playback state not clean after preset cycle");
        AssertContains(cyclesText, "private static async Task VerifyFlashbackEncoderCycleExportAsync(");
        AssertContains(cyclesText, "\"flashback-encoder-cycle-export.mp4\"");
        AssertContains(cyclesText, "flashback encoder cycle export verified");
        AssertContains(cyclesText, "private static async Task RestoreFlashbackEncoderCyclePresetAsync(");
        AssertContains(cyclesText, "flashback encoder preset restored to");
        AssertContains(cyclesText, "Flashback buffer did not become ready after preset restore");
        AssertContains(cyclesText, "internal static void RegisterSelectedFlashbackCycleScenarioTasks(");
        AssertContains(cyclesText, "4,\n                \"flashback-restart-cycle-task\",");
        AssertContains(cyclesText, "5,\n                \"flashback-encoder-cycle-task\",");
        AssertContains(startupText, "DiagnosticSessionFlashbackCycleScenarios.RegisterSelectedFlashbackCycleScenarioTasks(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackCycleScenarios;");
        AssertDoesNotContain(startupText, "RunFlashbackRestartCycleAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackEncoderCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRestartCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackEncoderCycleAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackPreviewCycleScenarios_OwnPreviewCycleFlows()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var cyclesText = ReadDiagnosticSessionFlashbackPreviewCycleScenariosSource();
        var flashbackCycleText = cyclesText;
        var playbackCycleText = flashbackCycleText;
        var recordingCycleText = flashbackCycleText;

        AssertContains(cyclesText, "internal static class DiagnosticSessionFlashbackPreviewCycleScenarios");
        AssertDoesNotContain(cyclesText, "internal static partial class DiagnosticSessionFlashbackPreviewCycleScenarios");
        AssertContains(cyclesText, "internal static async Task RunFlashbackPreviewCycleAsync(");
        AssertContains(flashbackCycleText, "flashback preview cycle preview stopped");
        AssertContains(flashbackCycleText, "CaptureFlashbackPreviewCycleEncodedFramesBeforeStopAsync(");
        AssertContains(flashbackCycleText, "ValidateFlashbackPreviewCycleStoppedAsync(");
        AssertContains(flashbackCycleText, "ValidateFlashbackPreviewCycleRestartedAsync(");
        AssertContains(flashbackCycleText, "private static async Task<long> CaptureFlashbackPreviewCycleEncodedFramesBeforeStopAsync(");
        AssertContains(flashbackCycleText, "private static async Task<bool> ValidateFlashbackPreviewCycleStoppedAsync(");
        AssertContains(flashbackCycleText, "flashback preview cycle: Flashback frames did not advance while preview was off");
        AssertContains(flashbackCycleText, "private static async Task ValidateFlashbackPreviewCycleRestartedAsync(");
        AssertContains(flashbackCycleText, "VideoFramesFlowing");
        AssertContains(flashbackCycleText, "VerifyFlashbackPreviewCycleExportAsync(");
        AssertContains(flashbackCycleText, "private static async Task VerifyFlashbackPreviewCycleExportAsync(");
        AssertContains(flashbackCycleText, "\"flashback-preview-off-export.mp4\"");
        AssertContains(flashbackCycleText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(flashbackCycleText, "flashback preview cycle export verified");
        AssertContains(cyclesText, "internal static async Task RunFlashbackPlaybackPreviewCycleAsync(");
        AssertContains(playbackCycleText, "flashback playback preview cycle preview stopped during playback");
        AssertContains(playbackCycleText, "CapturePlaybackPreviewCycleFrameCountBeforeStopAsync(");
        AssertContains(playbackCycleText, "ValidatePlaybackPreviewCycleStoppedAsync(");
        AssertContains(playbackCycleText, "ValidatePlaybackPreviewCycleRestartedAsync(");
        AssertContains(playbackCycleText, "private static async Task<long> CapturePlaybackPreviewCycleFrameCountBeforeStopAsync(");
        AssertContains(playbackCycleText, "WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertContains(playbackCycleText, "private static async Task<bool> ValidatePlaybackPreviewCycleStoppedAsync(");
        AssertContains(playbackCycleText, "flashback playback preview cycle: playback did not return live after preview stop");
        AssertContains(playbackCycleText, "private static async Task ValidatePlaybackPreviewCycleRestartedAsync(");
        AssertContains(playbackCycleText, "VideoFramesFlowing");
        AssertContains(playbackCycleText, "VerifyFlashbackPlaybackPreviewCycleExportAsync(");
        AssertContains(playbackCycleText, "private static async Task VerifyFlashbackPlaybackPreviewCycleExportAsync(");
        AssertContains(playbackCycleText, "\"flashback-playback-preview-cycle.mp4\"");
        AssertContains(playbackCycleText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(playbackCycleText, "flashback playback preview cycle export verified");
        AssertContains(cyclesText, "internal static async Task RunFlashbackRecordingPreviewCycleAsync(");
        AssertContains(cyclesText, "flashback recording preview cycle preview stopped");
        AssertContains(recordingCycleText, "CaptureRecordingPreviewCycleCountersBeforeStopAsync(");
        AssertContains(recordingCycleText, "ValidateRecordingPreviewCycleStoppedAsync(");
        AssertContains(recordingCycleText, "ValidateRecordingPreviewCycleRestartedAsync(");
        AssertContains(recordingCycleText, "private readonly record struct RecordingPreviewCycleCounters(");
        AssertContains(recordingCycleText, "private static async Task<RecordingPreviewCycleCounters?> CaptureRecordingPreviewCycleCountersBeforeStopAsync(");
        AssertContains(recordingCycleText, "WaitForFlashbackRecordingReadyAsync(");
        AssertContains(recordingCycleText, "WaitForPreviewActiveAsync(");
        AssertContains(recordingCycleText, "private static async Task<bool> ValidateRecordingPreviewCycleStoppedAsync(");
        AssertContains(recordingCycleText, "flashback recording preview cycle: recording counters did not advance while preview was off");
        AssertContains(recordingCycleText, "private static async Task ValidateRecordingPreviewCycleRestartedAsync(");
        AssertContains(recordingCycleText, "VideoFramesFlowing");
        AssertContains(recordingCycleText, "flashback recording preview cycle: preview frames did not resume");
        AssertDoesNotContain(cyclesText, "internal static bool IsPreviewCycleScenario(");
        AssertContains(cyclesText, "internal static void RegisterSelectedFlashbackPreviewCycleScenarioTasks(");
        AssertContains(cyclesText, "13,\n                \"flashback-preview-cycle-task\",");
        AssertContains(cyclesText, "14,\n                \"flashback-playback-preview-cycle-task\",");
        AssertContains(cyclesText, "15,\n                \"flashback-recording-preview-cycle-task\",");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackPreviewCycleScenarios.Playback.cs")),
            "Flashback playback preview-cycle scenario stays with the preview-cycle scenario family");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackPreviewCycleScenarios.Recording.cs")),
            "Flashback recording preview-cycle scenario stays with the preview-cycle scenario family");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackPreviewCycleScenarios.cs")),
            "Flashback preview-cycle scenario family folded into the Flashback cycle scenario owner");
        AssertContains(startupText, "DiagnosticSessionFlashbackPreviewCycleScenarios.RegisterSelectedFlashbackPreviewCycleScenarioTasks(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackPreviewCycleScenarios;");
        AssertDoesNotContain(startupText, "RunFlashbackPreviewCycleAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackPlaybackPreviewCycleAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackRecordingPreviewCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackPreviewCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackPlaybackPreviewCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRecordingPreviewCycleAsync(");
        AssertDoesNotContain(runnerText, "private static bool IsPreviewCycleScenario(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackRejectedExports_OwnRejectionFlows()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var rejectedExportsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExportScenarios.cs")
            .Replace("\r\n", "\n");

        AssertContains(rejectedExportsText, "internal static class DiagnosticSessionFlashbackExportScenarios");
        AssertContains(rejectedExportsText, "internal static async Task RunSelectedRejectedExportScenariosAsync(");
        AssertContains(rejectedExportsText, "private static async Task RunFlashbackExportRejectedAsync(");
        AssertContains(rejectedExportsText, "\"flashback-rejected-export.mp4\"");
        AssertContains(rejectedExportsText, "BufferInactive");
        AssertContains(rejectedExportsText, "Flashback buffer not active");
        AssertContains(rejectedExportsText, "private static async Task RunFlashbackRecordingExportRejectedAsync(");
        AssertContains(rejectedExportsText, "\"flashback-recording-rejected-export.mp4\"");
        AssertContains(rejectedExportsText, "UnavailableDuringRecording");
        AssertContains(rejectedExportsText, "recording backend changed after rejected export");
        var dispatchText = ExtractMemberCode(rejectedExportsText, "RunSelectedRejectedExportScenariosAsync");
        AssertContains(dispatchText, "scenarioPlan.RunFlashbackExportRejected");
        AssertContains(dispatchText, "scenarioPlan.RunFlashbackRecordingExportRejected");
        AssertOccursBefore(dispatchText, "RunFlashbackExportRejectedAsync(", "RunFlashbackRecordingExportRejectedAsync(");
        AssertContains(runnerText, "DiagnosticSessionFlashbackExportScenarios.RunSelectedRejectedExportScenariosAsync(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackRejectedExports.cs")),
            "Flashback rejected-export scenarios stay folded into the export scenario owner");
        AssertDoesNotContain(runnerText, "DiagnosticSessionFlashbackRejectedExports.");
        AssertDoesNotContain(runnerText, "RunFlashbackExportRejectedAsync(");
        AssertDoesNotContain(runnerText, "RunFlashbackRecordingExportRejectedAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackExportRejectedAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRecordingExportRejectedAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackSegmentPlaybackScenarios_OwnSegmentPlaybackFlow()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var segmentPlaybackText = ReadDiagnosticSessionFlashbackSegmentPlaybackScenariosSource();

        AssertContains(segmentPlaybackText, "internal static class DiagnosticSessionFlashbackSegmentPlaybackScenarios");
        AssertDoesNotContain(segmentPlaybackText, "internal static partial class DiagnosticSessionFlashbackSegmentPlaybackScenarios");
        AssertContains(segmentPlaybackText, "internal static async Task RunFlashbackSegmentPlaybackAsync(");
        AssertContains(segmentPlaybackText, "flashback segment playback live headroom established");
        AssertContains(segmentPlaybackText, "flashback segment playback started near boundary");
        AssertContains(segmentPlaybackText, "private static async Task<FlashbackSegmentPlaybackTarget?> AcquireFlashbackSegmentPlaybackTargetAsync(");
        AssertContains(segmentPlaybackText, "WaitForFlashbackPlayableCompletedSegmentAsync(");
        AssertContains(segmentPlaybackText, "no playable completed segment became available after recording-assisted rotation");
        AssertContains(segmentPlaybackText, "private static void ValidateFlashbackSegmentPlaybackSnapshot(");
        AssertContains(segmentPlaybackText, "frameCount >= 180");
        AssertContains(segmentPlaybackText, "playback FPS below source-rate target after warm sample");
        AssertContains(segmentPlaybackText, "flashback segment playback: command queue unhealthy");
        AssertContains(segmentPlaybackText, "private static async Task ReturnFlashbackSegmentPlaybackLiveAsync(");
        AssertContains(segmentPlaybackText, "\"go-live\"");
        AssertContains(segmentPlaybackText, "flashback segment playback go-live requested");
        AssertContains(segmentPlaybackText, "flashback segment playback: playback ended in state");
        AssertContains(segmentPlaybackText, "private static async Task<bool> CreateFlashbackCompletedSegmentViaRecordingAsync(");
        AssertContains(segmentPlaybackText, "recording-assisted rotation started");
        AssertContains(segmentPlaybackText, "private static async Task TryStopRecordingAsync(");
        AssertContains(segmentPlaybackText, "internal static void RegisterSelectedFlashbackSegmentPlaybackScenarioTask(");
        AssertContains(segmentPlaybackText, "scenarioPlan.RunFlashbackSegmentPlayback");
        AssertContains(segmentPlaybackText, "7,\n            \"flashback-segment-playback-task\",");
        AssertContains(segmentPlaybackText, "actions.Add(\"flashback segment playback started\")");
        AssertContains(startupText, "DiagnosticSessionFlashbackSegmentPlaybackScenarios.RegisterSelectedFlashbackSegmentPlaybackScenarioTask(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackSegmentPlaybackScenarios;");
        AssertDoesNotContain(startupText, "RunFlashbackSegmentPlaybackAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackSegmentPlaybackAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<bool> CreateFlashbackCompletedSegmentViaRecordingAsync(");
        AssertDoesNotContain(runnerText, "private static async Task TryStopRecordingAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackRecordingSettingsScenarios_OwnDeferredSettingsFlow()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var recordingChecksText = ReadDiagnosticSessionCleanupActionsSource()
            .Replace("\r\n", "\n");
        var recordingSettingsText = ReadDiagnosticSessionFlashbackRecordingSettingsScenariosSource();

        AssertContains(recordingSettingsText, "internal readonly record struct FlashbackRecordingSettingsDeferredPresetState(");
        AssertContains(recordingSettingsText, "internal static class DiagnosticSessionFlashbackRecordingSettingsScenarios");
        AssertDoesNotContain(recordingSettingsText, "internal static partial class DiagnosticSessionFlashbackRecordingSettingsScenarios");
        AssertContains(recordingSettingsText, "internal static async Task<FlashbackRecordingSettingsDeferredPresetState> RunFlashbackRecordingSettingsDeferredAsync(");
        AssertContains(recordingSettingsText, "flashback recording settings deferred preset changed to");
        AssertContains(recordingSettingsText, "VerifyFlashbackRestartRejectedDuringRecordingAsync(");
        AssertContains(recordingSettingsText, "VerifyFlashbackDisableRejectedDuringRecordingAsync(");
        AssertContains(recordingSettingsText, "VerifyFlashbackRecordingSettingsDeferredStillRecordingAsync(");
        AssertContains(recordingSettingsText, "private static async Task VerifyFlashbackRecordingSettingsCommandRejectedDuringRecordingAsync(");
        AssertContains(recordingSettingsText, "RestartFlashback unexpectedly succeeded during recording");
        AssertContains(recordingSettingsText, "SetFlashbackEnabled(false) unexpectedly succeeded during recording");
        AssertContains(recordingSettingsText, "Flashback recording backend did not remain active after mutations");
        AssertContains(recordingSettingsText, "recording counters did not advance after mutation attempts");
        AssertContains(recordingSettingsText, "internal static async Task VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
        AssertContains(recordingSettingsText, "flashback recording settings deferred post-stop buffer verified");
        AssertContains(recordingSettingsText, "private static async Task RestoreFlashbackRecordingSettingsOriginalPresetAsync(");
        AssertContains(recordingSettingsText, "\"SetPreset\"");
        AssertContains(recordingSettingsText, "flashback recording settings deferred preset restored to");
        AssertContains(recordingSettingsText, "selected preset was not restored");
        AssertContains(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackRecordingSettingsScenarios;");
        AssertContains(startupText, "RunFlashbackRecordingSettingsDeferredAsync(");
        AssertContains(recordingChecksText, "using static Sussudio.Tools.DiagnosticSessionFlashbackRecordingSettingsScenarios;");
        AssertContains(recordingChecksText, "VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<FlashbackRecordingSettingsDeferredPresetState> RunFlashbackRecordingSettingsDeferredAsync(");
        AssertDoesNotContain(runnerText, "private static async Task VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
        AssertDoesNotContain(runnerText, "private readonly record struct FlashbackRecordingSettingsDeferredPresetState(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackLifecycleScenarios_OwnLifecycleFlow()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var lifecycleText = ReadDiagnosticSessionFlashbackLifecycleScenariosSource();

        AssertContains(lifecycleText, "internal static class DiagnosticSessionFlashbackLifecycleScenarios");
        AssertContains(lifecycleText, "internal static void RegisterSelectedFlashbackLifecycleScenarioTask(");
        AssertContains(lifecycleText, "scenarioPlan.RunFlashbackLifecycle");
        AssertContains(lifecycleText, "backgroundTasks.AddScenario(");
        AssertContains(lifecycleText, "2,\n            \"flashback-lifecycle-task\",");
        AssertContains(lifecycleText, "actions.Add(\"flashback lifecycle started\")");
        AssertContains(lifecycleText, "internal static async Task RunFlashbackLifecycleAsync(");
        AssertContains(lifecycleText, "flashback lifecycle pause requested");
        AssertContains(lifecycleText, "flashback lifecycle disabled during playback");
        AssertContains(lifecycleText, "ValidateFlashbackLifecycleDisabledAsync(");
        AssertContains(lifecycleText, "flashback lifecycle re-enabled");
        AssertContains(lifecycleText, "ValidateFlashbackLifecycleReenabledAsync(");
        AssertContains(lifecycleText, "private static async Task ValidateFlashbackLifecycleDisabledAsync(");
        AssertContains(lifecycleText, "flashback lifecycle: playback worker still alive after disable");
        AssertContains(lifecycleText, "flashback lifecycle: pending commands remained after disable");
        AssertContains(lifecycleText, "private static async Task ValidateFlashbackLifecycleReenabledAsync(");
        AssertContains(startupText, "DiagnosticSessionFlashbackLifecycleScenarios.RegisterSelectedFlashbackLifecycleScenarioTask(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackLifecycleScenarios;");
        AssertDoesNotContain(startupText, "RunFlashbackLifecycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackLifecycleAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackMetrics_OwnsFlashbackSessionMetricProjection()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var metricsText = ReadDiagnosticSessionFlashbackMetricsSource();
        var recordingText = metricsText;
        var playbackSessionText = metricsText;
        var playbackObservationText = metricsText;
        var playbackResultText = metricsText;
        var exportText = metricsText;

        AssertContains(metricsText, "internal static class DiagnosticSessionFlashbackMetrics");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackMetrics.cs")), "Flashback metrics stay folded into DiagnosticSessionMetrics.cs");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackMetrics.Recording.cs")), "Flashback recording metrics stay folded into the consolidated metrics owner");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackMetrics.Export.cs")), "Flashback export metrics stay folded into the consolidated metrics owner");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackMetrics.PlaybackObservation.cs")), "Flashback playback observation metrics stay folded into the consolidated metrics owner");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackMetrics.RecordingExport.cs")), "Flashback recording/export metrics stay folded into the consolidated metrics owner");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackMetrics.PlaybackSession.cs")), "Flashback playback session metrics stay folded into the consolidated metrics owner");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackMetrics.PlaybackResult.cs")), "Flashback playback result metrics stay folded into the consolidated metrics owner");
        AssertContains(recordingText, "internal sealed class FlashbackRecordingSessionMetrics");
        AssertContains(playbackSessionText, "internal sealed class FlashbackPlaybackSessionMetrics");
        AssertContains(playbackResultText, "internal sealed class FlashbackPlaybackResultMetrics");
        AssertContains(exportText, "internal sealed class FlashbackExportSessionMetrics");
        AssertContains(playbackSessionText, "public JsonElement BaselineSnapshot { get; init; }");
        AssertContains(playbackSessionText, "public int MaxCommandQueueLatencyMsObserved { get; set; }");
        AssertContains(playbackSessionText, "public double MaxSlowFramePercentObserved { get; set; }");
        AssertContains(playbackSessionText, "public long MinOnePercentLowAudioMasterFallbacks { get; set; }");
        AssertContains(playbackSessionText, "public string MaxDecodePhaseObserved { get; set; } = string.Empty;");
        AssertContains(playbackSessionText, "public double MaxAbsAvDriftMsObserved { get; set; }");
        AssertContains(playbackSessionText, "public long SubmitFailuresDelta { get; set; }");
        AssertContains(playbackResultText, "public JsonElement EndSnapshot { get; init; }");
        AssertContains(playbackResultText, "public int PendingCommandsAtEnd { get; init; }");
        AssertContains(playbackResultText, "public double OnePercentLowFpsAtEnd { get; init; }");
        AssertContains(playbackResultText, "public string MaxDecodePhaseAtEnd { get; init; } = string.Empty;");
        AssertContains(playbackResultText, "public long AudioMasterFallbacksAtEnd { get; init; }");
        AssertContains(playbackResultText, "public long SeekForwardDecodeCapHitsDelta { get; init; }");
        AssertContains(exportText, "public long ForceRotateFallbacksAtEnd { get; set; }");
        AssertContains(metricsText, "internal static FlashbackRecordingSessionMetrics BuildFlashbackRecordingMetrics(");
        AssertContains(playbackSessionText, "internal static FlashbackPlaybackSessionMetrics BuildFlashbackPlaybackSessionMetrics(");
        AssertContains(playbackObservationText, "private static void ObservePlaybackSnapshot(");
        AssertContains(playbackObservationText, "var relevance = BuildPlaybackSnapshotRelevance(");
        AssertContains(playbackObservationText, "private readonly record struct FlashbackPlaybackSnapshotRelevance(");
        AssertContains(playbackObservationText, "private static FlashbackPlaybackSnapshotRelevance BuildPlaybackSnapshotRelevance(");
        AssertContains(playbackObservationText, "private static bool IsPlaybackSnapshotActive(");
        AssertContains(playbackObservationText, "GetInt(snapshot, \"FlashbackPlaybackPendingCommands\") > 0");
        AssertContains(playbackObservationText, "ObservePlaybackOnePercentLow(");
        AssertContains(playbackObservationText, "ObservePlaybackFrameAndDecodeMetrics(metrics, snapshot);");
        AssertContains(playbackObservationText, "ObservePlaybackAudioMasterMetrics(metrics, snapshot);");
        AssertContains(playbackObservationText, "private static void ObservePlaybackOnePercentLow(");
        AssertContains(playbackObservationText, "metrics.OnePercentLowSampleWindowObserved = true;");
        AssertContains(playbackObservationText, "private static void ObservePlaybackFrameAndDecodeMetrics(");
        AssertContains(playbackObservationText, "metrics.MaxDecodePhaseObserved = GetString(snapshot, \"FlashbackPlaybackMaxDecodePhase\") ?? string.Empty;");
        AssertContains(playbackObservationText, "private static void ObservePlaybackAudioMasterMetrics(");
        AssertContains(playbackObservationText, "GetResetAwareCounterDelta(snapshot, metrics.BaselineSnapshot, \"FlashbackPlaybackAudioMasterFallbacks\")");
        AssertContains(playbackResultText, "internal static FlashbackPlaybackResultMetrics BuildFlashbackPlaybackResultMetrics(");
        AssertContains(playbackResultText, "var commands = BuildFlashbackPlaybackResultCommandMetrics(observed, endSnapshot, metrics);");
        AssertContains(playbackResultText, "PendingCommandsAtEnd = commands.PendingCommandsAtEnd,");
        AssertContains(playbackResultText, "private static long GetObservedLong(bool observed, JsonElement snapshot, string propertyName)");
        AssertContains(playbackResultText, "private static double GetObservedDouble(bool observed, JsonElement snapshot, string propertyName)");
        AssertContains(playbackResultText, "private static FlashbackPlaybackResultCommandMetrics BuildFlashbackPlaybackResultCommandMetrics(");
        AssertContains(playbackResultText, "PendingCommandsAtEnd: observed ? GetInt(endSnapshot, \"FlashbackPlaybackPendingCommands\") : 0");
        AssertContains(playbackResultText, "LastCommandFailureAtEnd: observed ? GetString(endSnapshot, \"FlashbackPlaybackLastCommandFailure\") ?? string.Empty : string.Empty");
        AssertContains(playbackResultText, "private static FlashbackPlaybackResultCadenceMetrics BuildFlashbackPlaybackResultCadenceMetrics(");
        AssertContains(playbackResultText, "DroppedFramesAtEnd: GetObservedLong(observed, endSnapshot, \"FlashbackPlaybackDroppedFrames\")");
        AssertContains(playbackResultText, "private static FlashbackPlaybackResultDecodeMetrics BuildFlashbackPlaybackResultDecodeMetrics(");
        AssertContains(playbackResultText, "MaxDecodePhaseAtEnd: observed ? GetString(endSnapshot, \"FlashbackPlaybackMaxDecodePhase\") ?? string.Empty : string.Empty");
        AssertContains(playbackResultText, "private static FlashbackPlaybackResultAudioMasterMetrics BuildFlashbackPlaybackResultAudioMasterMetrics(");
        AssertContains(playbackResultText, "AudioMasterFallbacksAtEnd: GetObservedLong(observed, endSnapshot, \"FlashbackPlaybackAudioMasterFallbacks\")");
        AssertContains(playbackResultText, "private static FlashbackPlaybackResultStageMetrics BuildFlashbackPlaybackResultStageMetrics(");
        AssertContains(playbackResultText, "GetCounterDelta(endSnapshot, metrics.BaselineSnapshot, \"FlashbackPlaybackSeekForwardDecodeCapHits\")");
        AssertContains(playbackResultText, "private readonly record struct FlashbackPlaybackResultCommandMetrics(");
        AssertContains(playbackResultText, "private readonly record struct FlashbackPlaybackResultCadenceMetrics(");
        AssertContains(playbackResultText, "private readonly record struct FlashbackPlaybackResultDecodeMetrics(");
        AssertContains(playbackResultText, "private readonly record struct FlashbackPlaybackResultAudioMasterMetrics(");
        AssertContains(playbackResultText, "private readonly record struct FlashbackPlaybackResultStageMetrics(");
        AssertContains(metricsText, "internal static FlashbackExportSessionMetrics BuildFlashbackExportSessionMetrics(");
        AssertContains(metricsText, "metrics.ForceRotateFallbacksAtEnd = GetNullableLong(lastSnapshot, \"FlashbackExportForceRotateFallbacks\") ?? 0;");
        AssertContains(metricsText, "metrics.ForceRotateFallbacksDelta = GetCounterDelta(");
        AssertContains(metricsText, "metrics.LastForceRotateFallbackSegmentsAtEnd =");
        AssertContains(exportText, "private static void ObserveExportSnapshot(");
        AssertContains(exportText, "var relevantToSession =");
        AssertContains(exportText, "metrics.MaxThroughputBytesPerSecObserved = Math.Max(");
        AssertContains(playbackSessionText, "private static void ObservePlaybackOnePercentLow(");
        AssertContains(playbackSessionText, "private static void ObservePlaybackFrameAndDecodeMetrics(");
        AssertContains(playbackSessionText, "private static void ObservePlaybackAudioMasterMetrics(");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionFlashbackMetrics;");
        AssertContains(builderText, "var playbackResultMetrics = BuildFlashbackPlaybackResultMetrics(playbackSessionMetrics);");
        AssertDoesNotContain(runnerText, "private sealed class FlashbackPlaybackSessionMetrics");
        AssertDoesNotContain(runnerText, "GetString(playbackEndSnapshot,");
        AssertDoesNotContain(runnerText, "private sealed class FlashbackExportSessionMetrics");
        AssertDoesNotContain(runnerText, "private static FlashbackRecordingSessionMetrics BuildFlashbackRecordingMetrics(");
        AssertDoesNotContain(runnerText, "private static bool IsPlaybackSnapshotActive(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackMetrics_ExportForceRotateCountersIgnoreRelevanceGate()
    {
        var assembly = LoadToolAssemblyIsolated(global::Program.SsctlAssemblyRelativePath);
        var metricsType = assembly.GetType("Sussudio.Tools.DiagnosticSessionFlashbackMetrics")
            ?? throw new InvalidOperationException("Sussudio.Tools.DiagnosticSessionFlashbackMetrics was not found.");
        var sampleType = assembly.GetType("Sussudio.Tools.DiagnosticSessionSample")
            ?? throw new InvalidOperationException("Sussudio.Tools.DiagnosticSessionSample was not found.");
        var buildMetrics = metricsType.GetMethod(
            "BuildFlashbackExportSessionMetrics",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BuildFlashbackExportSessionMetrics was not found.");

        using var initialDocument = JsonDocument.Parse(
            """
            {
              "FlashbackExportId": 0,
              "FlashbackExportActive": false,
              "FlashbackExportStatus": "NotStarted",
              "FlashbackExportForceRotateFallbacks": 1,
              "FlashbackExportLastForceRotateFallbackSegments": 0
            }
            """);
        using var lastDocument = JsonDocument.Parse(
            """
            {
              "FlashbackExportId": 0,
              "FlashbackExportActive": false,
              "FlashbackExportStatus": "NotStarted",
              "FlashbackExportForceRotateFallbacks": 3,
              "FlashbackExportLastForceRotateFallbackSegments": 2
            }
            """);

        var samples = Array.CreateInstance(sampleType, 0);
        var metrics = buildMetrics.Invoke(
            null,
            new object?[] { initialDocument.RootElement, samples, lastDocument.RootElement })
            ?? throw new InvalidOperationException("BuildFlashbackExportSessionMetrics returned null.");

        AssertEqual(false, (bool)GetPropertyValue(metrics, "Observed")!, "Non-relevant export remains unobserved");
        AssertEqual(3L, Convert.ToInt64(GetPropertyValue(metrics, "ForceRotateFallbacksAtEnd")), "ForceRotateFallbacksAtEnd");
        AssertEqual(2L, Convert.ToInt64(GetPropertyValue(metrics, "ForceRotateFallbacksDelta")), "ForceRotateFallbacksDelta");
        AssertEqual(2, Convert.ToInt32(GetPropertyValue(metrics, "LastForceRotateFallbackSegmentsAtEnd")), "LastForceRotateFallbackSegmentsAtEnd");

        return Task.CompletedTask;
    }

    // Shared reflection helpers and contract checks for automation tool contract tests.
    internal static Task NvmlSnapshot_ComputedProperties_ConvertUnits()
    {
        var snapshotType = RequireType("Sussudio.Services.Gpu.NvmlSnapshot");
        // Constructor: GpuName, GpuUtil%, MemUtil%, NvdecUtil%, NvencUtil%, PcieTxKB, PcieRxKB,
        //              VramUsedB, VramTotalB, TempC, PowerMw, ClockMHz, MemClockMHz
        var snapshot = Activator.CreateInstance(snapshotType,
            "RTX 4090",        // GpuName
            (uint?)85,         // GpuUtilizationPercent
            (uint?)40,         // GpuMemoryUtilizationPercent
            (uint?)50,         // NvdecUtilizationPercent
            (uint?)75,         // NvencUtilizationPercent
            (uint?)1024,       // PcieTxKBps (1024 KB/s = 1.0 MB/s)
            (uint?)2048,       // PcieRxKBps (2048 KB/s = 2.0 MB/s)
            (ulong?)2147483648,// VramUsedBytes (2 GB)
            (ulong?)25769803776,// VramTotalBytes (24 GB)
            (uint?)65,         // GpuTemperatureC
            (uint?)350000,     // GpuPowerMilliwatts (350W)
            (uint?)2520,       // GpuClockMHz
            (uint?)10501)!;    // GpuMemClockMHz

        var powerW = GetPropertyValue(snapshot, "GpuPowerW");
        AssertEqual(350.0, (double)powerW!, "GpuPowerW");

        var txMB = GetPropertyValue(snapshot, "PcieTxMBps");
        AssertEqual(1.0, (double)txMB!, "PcieTxMBps");

        var rxMB = GetPropertyValue(snapshot, "PcieRxMBps");
        AssertEqual(2.0, (double)rxMB!, "PcieRxMBps");

        var usedMB = GetPropertyValue(snapshot, "VramUsedMB");
        AssertEqual(2048UL, (ulong)usedMB!, "VramUsedMB");

        return Task.CompletedTask;
    }

    internal static Task NvmlMonitor_NativeInteropLivesWithMonitorOwner()
    {
        var monitorText = ReadRepoFile("Sussudio/Services/Gpu/NvmlMonitor.cs");

        AssertContains(monitorText, "public sealed class NvmlMonitor : IDisposable");
        AssertContains(monitorText, "private void Poll(object? state)");
        AssertContains(monitorText, "public NvmlSnapshot? GetLatestSnapshot()");
        AssertContains(monitorText, "TryLoadNativeLibrary()");
        AssertContains(monitorText, "private static unsafe string? GetDeviceName(IntPtr device)");
        AssertContains(monitorText, "private struct NvmlUtilization");
        AssertContains(monitorText, "[DllImport(\"nvml.dll\"");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Gpu", "NvmlMonitor.NativeInterop.cs")),
            "NvmlMonitor.NativeInterop.cs folded into NvmlMonitor.cs");

        return Task.CompletedTask;
    }

    private static Type RequireSharedToolType(string typeName)
    {
        var assembly = LoadToolAssemblyIsolated(global::Program.SsctlAssemblyRelativePath);
        return assembly.GetType(typeName)
               ?? throw new InvalidOperationException($"{typeName} was not found in the shared tool assembly.");
    }

    private static Type RequireAutomationContractType(string typeName)
    {
        var assembly = typeof(Sussudio.Tools.AutomationCommandCatalog).Assembly;
        return assembly.GetType(typeName)
               ?? throw new InvalidOperationException($"{typeName} was not found in the automation contracts assembly.");
    }

    private static T GetConstant<T>(Type type, string name)
    {
        var field = type.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{type.FullName}.{name} was not found.");
        return (T)field.GetRawConstantValue()!;
    }

    private static MethodInfo RequireNonPublicStaticMethod(Type type, string name)
        => type.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
           ?? throw new InvalidOperationException($"{type.FullName}.{name} was not found.");

    private static object[] GetCatalogEntries(Type catalogType)
    {
        var entriesProperty = catalogType.GetProperty("Entries", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("AutomationCommandCatalog.Entries not found.");
        return ((System.Collections.IEnumerable)entriesProperty.GetValue(null)!)
            .Cast<object>()
            .ToArray();
    }

    private static object[] GetMetadataCollection(object metadata, string name)
        => ((System.Collections.IEnumerable)GetMetadataProperty(metadata, name)!)
            .Cast<object>()
            .ToArray();

    private static void AssertPayloadFieldsMatchShape(object entry, string commandName, string payloadShape)
    {
        var expectedFields = ParsePayloadShape(payloadShape);
        var actualFields = GetMetadataCollection(entry, "PayloadFields");
        AssertEqual(expectedFields.Length, actualFields.Length, $"{commandName} typed payload field count");

        for (var i = 0; i < expectedFields.Length; i++)
        {
            var actual = actualFields[i];
            AssertEqual(expectedFields[i].Name, (string)GetMetadataProperty(actual, "Name")!, $"{commandName} payload field {i} name");
            AssertEqual(expectedFields[i].Type, GetMetadataProperty(actual, "Type")!.ToString(), $"{commandName} payload field {i} type");
            AssertEqual(expectedFields[i].Required, (bool)GetMetadataProperty(actual, "Required")!, $"{commandName} payload field {i} required");
        }

        var distinctNames = actualFields
            .Select(field => (string)GetMetadataProperty(field, "Name")!)
            .Distinct(StringComparer.Ordinal)
            .Count();
        AssertEqual(actualFields.Length, distinctNames, $"{commandName} unique typed payload field names");
    }

    private static (string Name, string Type, bool Required)[] ParsePayloadShape(string payloadShape)
    {
        var trimmed = payloadShape.Trim();
        if (string.Equals(trimmed, "{}", StringComparison.Ordinal))
        {
            return Array.Empty<(string Name, string Type, bool Required)>();
        }

        if (!trimmed.StartsWith("{", StringComparison.Ordinal) ||
            !trimmed.EndsWith("}", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unsupported payload shape '{payloadShape}'.");
        }

        var inner = trimmed[1..^1].Trim();
        if (string.IsNullOrWhiteSpace(inner))
        {
            return Array.Empty<(string Name, string Type, bool Required)>();
        }

        return inner
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(fieldShape =>
            {
                var parts = fieldShape.Split(':', 2, StringSplitOptions.TrimEntries);
                if (parts.Length != 2)
                {
                    throw new InvalidOperationException($"Unsupported payload field shape '{fieldShape}'.");
                }

                var rawName = parts[0];
                var required = !rawName.EndsWith("?", StringComparison.Ordinal);
                var name = required ? rawName : rawName[..^1];
                return (name, NormalizePayloadFieldType(parts[1]), required);
            })
            .ToArray();
    }

    private static string NormalizePayloadFieldType(string payloadType)
        => payloadType.Trim().ToLowerInvariant() switch
        {
            "string" => "String",
            "bool" => "Boolean",
            "int" => "Integer",
            "double" => "Number",
            "array" => "Array",
            "object" => "Object",
            _ => throw new InvalidOperationException($"Unsupported payload field type '{payloadType}'.")
        };

    private static void AssertCatalogMetadata(
        Type catalogType,
        Type enumType,
        Type pathPolicyType,
        string commandName,
        int timeoutMs,
        bool requiresReadyDevices,
        string pathPolicy,
        string payloadShapeContains)
    {
        var get = RequireNonPublicStaticMethod(catalogType, "Get");
        var enumValue = Enum.Parse(enumType, commandName);
        var metadata = get.Invoke(null, new[] { enumValue })
            ?? throw new InvalidOperationException($"Catalog metadata for {commandName} was null.");
        AssertEqual(commandName, (string)GetMetadataProperty(metadata, "Name")!, $"{commandName} catalog name");
        AssertEqual(timeoutMs, (int)GetMetadataProperty(metadata, "ResponseTimeoutMs")!, $"{commandName} catalog timeout");
        AssertEqual(requiresReadyDevices, (bool)GetMetadataProperty(metadata, "RequiresReadyDevices")!, $"{commandName} catalog readiness");
        AssertEqual(
            Enum.Parse(pathPolicyType, pathPolicy).ToString(),
            GetMetadataProperty(metadata, "PathPolicy")!.ToString(),
            $"{commandName} catalog path policy");
        AssertContains((string)GetMetadataProperty(metadata, "PayloadShape")!, payloadShapeContains);
    }

    private static object? GetMetadataProperty(object metadata, string name)
        => metadata.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)
               ?.GetValue(metadata)
           ?? throw new InvalidOperationException($"Metadata property '{name}' was not found.");

    private static void AssertNotEmpty(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Assertion failed for {fieldName}: expected non-empty text.");
        }
    }

    private static void AssertThrows<TException>(Action action, string fieldName)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TargetInvocationException ex) when (ex.InnerException is TException)
        {
            return;
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Assertion failed for {fieldName}: expected {typeof(TException).Name}.");
    }

    private static Task AutomationCommandKind_PreservesNumericValuesThroughGetAutomationManifest()
    {
        var enumType = RequireType("Sussudio.Models.AutomationCommandKind");
        var expectedCommands = ExpectedAutomationCommands();
        AssertEqual(expectedCommands.Length, Enum.GetValues(enumType).Length, "AutomationCommandKind value count");

        for (int i = 0; i < expectedCommands.Length; i++)
        {
            var (name, value) = expectedCommands[i];
            var parsed = Enum.Parse(enumType, name);
            AssertEqual(value, Convert.ToInt32(parsed), $"AutomationCommandKind.{name}");
            if (!Enum.IsDefined(enumType, value))
            {
                throw new InvalidOperationException(
                    $"AutomationCommandKind missing sequential value {value}.");
            }
        }

        return Task.CompletedTask;
    }

    internal static (string Name, int Value)[] ExpectedAutomationCommands() =>
    [
        ("Authenticate", 0),
        ("GetSnapshot", 1),
        ("GetDiagnostics", 2),
        ("RefreshDevices", 3),
        ("SelectDevice", 4),
        ("SelectAudioInputDevice", 5),
        ("SetCustomAudioInput", 6),
        ("SetResolution", 7),
        ("SetFrameRate", 8),
        ("SetRecordingFormat", 9),
        ("SetQuality", 10),
        ("SetCustomBitrate", 11),
        ("SetHdrEnabled", 12),
        ("SetAudioEnabled", 13),
        ("SetAudioPreviewEnabled", 14),
        ("SetOutputPath", 15),
        ("SetPreviewEnabled", 16),
        ("SetRecordingEnabled", 17),
        ("ArmClose", 18),
        ("WindowAction", 19),
        ("WaitForCondition", 20),
        ("VerifyLastRecording", 21),
        ("AssertSnapshot", 22),
        ("SetTrueHdrPreviewEnabled", 23),
        ("ProbeVideoSource", 24),
        ("ProbePreviewColor", 25),
        ("CapturePreviewFrame", 26),
        ("CaptureWindowScreenshot", 27),
        ("SetVideoFormat", 28),
        ("GetCaptureOptions", 29),
        ("SetPreset", 30),
        ("SetSplitEncodeMode", 31),
        ("SetMjpegDecoderCount", 32),
        ("SetShowAllCaptureOptions", 33),
        ("SetPreviewVolume", 34),
        ("SetStatsVisible", 35),
        ("SetDeviceAudioMode", 36),
        ("GetPerformanceTimeline", 37),
        ("SetStatsSectionVisible", 38),
        ("SetAnalogAudioGain", 39),
        ("SetSettingsVisible", 40),
        ("FlashbackAction", 41),
        ("FlashbackExport", 42),
        ("FlashbackGetSegments", 43),
        ("VerifyFile", 44),
        ("RestartFlashback", 45),
        ("SetMicrophoneEnabled", 46),
        ("SetFlashbackEnabled", 47),
        ("GetAudioRampTrace", 48),
        ("SetFrameTimeOverlayVisible", 49),
        ("SetFlashbackTimelineVisible", 50),
        ("GetAutomationManifest", 51),
        ("SetFullScreenEnabled", 52),
        ("OpenRecordingsFolder", 53)
    ];

    internal static Task AutomationCommandCatalog_CoversCommandsAndPolicyMetadata()
    {
        var catalogType = RequireAutomationContractType("Sussudio.Tools.AutomationCommandCatalog");
        var catalogText = ReadRepoFile("Sussudio.Automation.Contracts/AutomationCommandCatalog.cs")
            .Replace("\r\n", "\n");
        var catalogEntriesText = catalogText;
        var enumType = RequireAutomationContractType("Sussudio.Models.AutomationCommandKind");
        var pathPolicyType = RequireAutomationContractType("Sussudio.Tools.AutomationCommandPathPolicy");
        var entriesProperty = catalogType.GetProperty("Entries", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("AutomationCommandCatalog.Entries not found.");
        var entries = ((System.Collections.IEnumerable)entriesProperty.GetValue(null)!)
            .Cast<object>()
            .ToArray();
        var enumValues = Enum.GetValues(enumType).Cast<object>().ToArray();
        AssertEqual(enumValues.Length, entries.Length, "AutomationCommandCatalog entry count");

        foreach (var enumValue in enumValues)
        {
            var entry = entries.Single(candidate =>
                Convert.ToInt32(GetMetadataProperty(candidate, "Kind")) == Convert.ToInt32(enumValue));
            var payloadShape = (string)GetMetadataProperty(entry, "PayloadShape")!;
            AssertEqual(enumValue.ToString(), (string)GetMetadataProperty(entry, "Name")!, $"Catalog name for {enumValue}");
            AssertNotEmpty(payloadShape, $"Catalog payload shape for {enumValue}");
            AssertPayloadFieldsMatchShape(entry, enumValue.ToString()!, payloadShape);
            AssertEqual(true, (int)GetMetadataProperty(entry, "ResponseTimeoutMs")! > 0, $"Catalog timeout for {enumValue}");
            AssertNotEmpty((string)GetMetadataProperty(entry, "CliHelp")!, $"Catalog CLI help for {enumValue}");
            var mcpDescription = (string)GetMetadataProperty(entry, "McpDescription")!;
            AssertNotEmpty(mcpDescription, $"Catalog MCP description for {enumValue}");
            AssertEqual(false, mcpDescription == $"Automation command {enumValue}.", $"Catalog explicit MCP description for {enumValue}");
        }

        AssertContains(catalogText, "public static class AutomationCommandCatalog");
        AssertContains(catalogEntriesText, "private static IReadOnlyList<AutomationCommandMetadata> BuildEntries()");
        AssertContains(catalogEntriesText, "RegisterCaptureEntries(entries);");
        AssertContains(catalogEntriesText, "private static void RegisterCaptureEntries(");
        AssertContains(catalogEntriesText, "private static void RegisterFlashbackEntries(");
        AssertContains(catalogEntriesText, "Set(entries, AutomationCommandKind.SetRecordingEnabled");
        AssertContains(catalogEntriesText, "Set(entries, AutomationCommandKind.FlashbackExport");
        AssertContains(catalogText, "public enum AutomationCommandPathPolicy");
        AssertContains(catalogText, "public static AutomationManifest CreateManifest()");
        AssertContains(catalogText, "public static string CreateManifestJson()");
        AssertContains(catalogText, "public static string ValidatePath(");

        AssertCatalogMetadata(
            catalogType,
            enumType,
            pathPolicyType,
            "SetRecordingEnabled",
            timeoutMs: 150000,
            requiresReadyDevices: true,
            pathPolicy: "None",
            payloadShapeContains: "enabled");
        AssertCatalogMetadata(
            catalogType,
            enumType,
            pathPolicyType,
            "FlashbackExport",
            timeoutMs: 305000,
            requiresReadyDevices: false,
            pathPolicy: "WriteFile",
            payloadShapeContains: "outputPath");
        AssertCatalogMetadata(
            catalogType,
            enumType,
            pathPolicyType,
            "VerifyFile",
            timeoutMs: 60000,
            requiresReadyDevices: false,
            pathPolicy: "ReadFile",
            payloadShapeContains: "filePath");
        var verifyEntry = entries.Single(candidate =>
            string.Equals((string)GetMetadataProperty(candidate, "Name")!, "VerifyFile", StringComparison.Ordinal));
        AssertContains((string)GetMetadataProperty(verifyEntry, "CliHelp")!, "--profile");
        AssertCatalogMetadata(
            catalogType,
            enumType,
            pathPolicyType,
            "SetOutputPath",
            timeoutMs: 15000,
            requiresReadyDevices: false,
            pathPolicy: "Directory",
            payloadShapeContains: "outputPath");
        AssertCatalogMetadata(
            catalogType,
            enumType,
            pathPolicyType,
            "SetResolution",
            timeoutMs: 15000,
            requiresReadyDevices: true,
            pathPolicy: "None",
            payloadShapeContains: "resolution");
        AssertCatalogMetadata(
            catalogType,
            enumType,
            pathPolicyType,
            "SetFlashbackEnabled",
            timeoutMs: 305000,
            requiresReadyDevices: false,
            pathPolicy: "None",
            payloadShapeContains: "enabled");
        AssertCatalogMetadata(
            catalogType,
            enumType,
            pathPolicyType,
            "GetAutomationManifest",
            timeoutMs: 15000,
            requiresReadyDevices: false,
            pathPolicy: "None",
            payloadShapeContains: "{}");
        AssertCatalogMetadata(
            catalogType,
            enumType,
            pathPolicyType,
            "SetFullScreenEnabled",
            timeoutMs: 15000,
            requiresReadyDevices: false,
            pathPolicy: "None",
            payloadShapeContains: "enabled");
        AssertCatalogMetadata(
            catalogType,
            enumType,
            pathPolicyType,
            "OpenRecordingsFolder",
            timeoutMs: 15000,
            requiresReadyDevices: false,
            pathPolicy: "None",
            payloadShapeContains: "{}");
        AssertOptionalPayloadField(entries, "ArmClose", "actionId");
        AssertOptionalPayloadField(entries, "WindowAction", "action");
        AssertOptionalPayloadField(entries, "WindowAction", "actionId");
        AssertOptionalPayloadField(entries, "WaitForCondition", "condition");
        var armCloseEntry = entries.Single(candidate =>
            string.Equals((string)GetMetadataProperty(candidate, "Name")!, "ArmClose", StringComparison.Ordinal));
        var windowActionEntry = entries.Single(candidate =>
            string.Equals((string)GetMetadataProperty(candidate, "Name")!, "WindowAction", StringComparison.Ordinal));
        AssertContains((string)GetMetadataProperty(armCloseEntry, "McpDescription")!, "actionId is required");
        AssertContains((string)GetMetadataProperty(windowActionEntry, "McpDescription")!, "Close requires the actionId");

        return Task.CompletedTask;
    }

    private static void AssertOptionalPayloadField(
        object[] entries,
        string commandName,
        string fieldName)
    {
        var entry = entries.Single(candidate =>
            string.Equals((string)GetMetadataProperty(candidate, "Name")!, commandName, StringComparison.Ordinal));
        var fields = GetMetadataCollection(entry, "PayloadFields");
        var field = fields.Single(candidate =>
            string.Equals((string)GetMetadataProperty(candidate, "Name")!, fieldName, StringComparison.Ordinal));
        AssertEqual(false, (bool)GetMetadataProperty(field, "Required")!, $"{commandName}.{fieldName} catalog optional field");
    }

    internal static Task AutomationManifest_CoversCatalogMetadata()
    {
        var catalogType = RequireAutomationContractType("Sussudio.Tools.AutomationCommandCatalog");
        var entries = GetCatalogEntries(catalogType);
        var createManifest = RequireNonPublicStaticMethod(catalogType, "CreateManifest");
        var manifest = createManifest.Invoke(null, Array.Empty<object>())
            ?? throw new InvalidOperationException("AutomationCommandCatalog.CreateManifest returned null.");

        AssertEqual(1, (int)GetMetadataProperty(manifest, "SchemaVersion")!, "Automation manifest schema version");
        var commands = GetMetadataCollection(manifest, "Commands");
        AssertEqual(entries.Length, commands.Length, "Automation manifest command count");

        foreach (var entry in entries)
        {
            var id = Convert.ToInt32(GetMetadataProperty(entry, "Kind"));
            var manifestCommand = commands.Single(command => (int)GetMetadataProperty(command, "Id")! == id);
            AssertEqual(id, (int)GetMetadataProperty(manifestCommand, "Id")!, $"Manifest id for {id}");
            AssertEqual((string)GetMetadataProperty(entry, "Name")!, (string)GetMetadataProperty(manifestCommand, "Name")!, $"Manifest name for {id}");
            AssertEqual((string)GetMetadataProperty(entry, "PayloadShape")!, (string)GetMetadataProperty(manifestCommand, "PayloadShape")!, $"Manifest payload shape for {id}");
            AssertEqual((int)GetMetadataProperty(entry, "ResponseTimeoutMs")!, (int)GetMetadataProperty(manifestCommand, "ResponseTimeoutMs")!, $"Manifest timeout for {id}");
            AssertEqual((bool)GetMetadataProperty(entry, "RequiresReadyDevices")!, (bool)GetMetadataProperty(manifestCommand, "RequiresReadyDevices")!, $"Manifest readiness for {id}");
            AssertEqual(GetMetadataProperty(entry, "PathPolicy")!.ToString(), (string)GetMetadataProperty(manifestCommand, "PathPolicy")!, $"Manifest path policy for {id}");
            AssertEqual((string)GetMetadataProperty(entry, "CliHelp")!, (string)GetMetadataProperty(manifestCommand, "CliHelp")!, $"Manifest CLI help for {id}");
            AssertEqual((string)GetMetadataProperty(entry, "McpDescription")!, (string)GetMetadataProperty(manifestCommand, "McpDescription")!, $"Manifest MCP description for {id}");

            var entryFields = GetMetadataCollection(entry, "PayloadFields");
            var manifestFields = GetMetadataCollection(manifestCommand, "PayloadFields");
            AssertEqual(entryFields.Length, manifestFields.Length, $"Manifest payload field count for {id}");
            for (var i = 0; i < entryFields.Length; i++)
            {
                AssertEqual((string)GetMetadataProperty(entryFields[i], "Name")!, (string)GetMetadataProperty(manifestFields[i], "Name")!, $"Manifest payload field name {id}[{i}]");
                AssertEqual(GetMetadataProperty(entryFields[i], "Type")!.ToString(), (string)GetMetadataProperty(manifestFields[i], "Type")!, $"Manifest payload field type {id}[{i}]");
                AssertEqual((bool)GetMetadataProperty(entryFields[i], "Required")!, (bool)GetMetadataProperty(manifestFields[i], "Required")!, $"Manifest payload field required {id}[{i}]");
            }
        }

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandCatalog_PathBearingCommandsHaveValidationCoverage()
    {
        var catalogType = RequireAutomationContractType("Sussudio.Tools.AutomationCommandCatalog");
        var enumType = RequireAutomationContractType("Sussudio.Models.AutomationCommandKind");
        var entries = GetCatalogEntries(catalogType);
        var validatePath = RequireNonPublicStaticMethod(catalogType, "ValidatePath");
        var expectedPathFields = new Dictionary<string, (string Policy, string FieldName, bool Required)>
        {
            ["SetOutputPath"] = ("Directory", "outputPath", true),
            ["CapturePreviewFrame"] = ("WriteFile", "outputPath", false),
            ["CaptureWindowScreenshot"] = ("WriteFile", "outputPath", false),
            ["FlashbackExport"] = ("WriteFile", "outputPath", true),
            ["VerifyFile"] = ("ReadFile", "filePath", true)
        };

        var pathEntries = entries
            .Where(entry => !string.Equals(GetMetadataProperty(entry, "PathPolicy")!.ToString(), "None", StringComparison.Ordinal))
            .ToArray();
        AssertEqual(expectedPathFields.Count, pathEntries.Length, "Catalog path-policy command count");

        var dispatcherText = ReadAutomationCommandDispatcherFamilyText()
            .Replace("\r\n", "\n");
        foreach (var entry in pathEntries)
        {
            var commandName = (string)GetMetadataProperty(entry, "Name")!;
            if (!expectedPathFields.TryGetValue(commandName, out var expected))
            {
                throw new InvalidOperationException($"Unexpected path-bearing command '{commandName}'.");
            }

            AssertEqual(expected.Policy, GetMetadataProperty(entry, "PathPolicy")!.ToString(), $"{commandName} path policy");
            var fields = GetMetadataCollection(entry, "PayloadFields");
            var pathField = fields.SingleOrDefault(field =>
                string.Equals((string)GetMetadataProperty(field, "Name")!, expected.FieldName, StringComparison.Ordinal));
            if (pathField == null)
            {
                throw new InvalidOperationException($"{commandName} missing typed path payload field '{expected.FieldName}'.");
            }

            AssertEqual(expected.Required, (bool)GetMetadataProperty(pathField, "Required")!, $"{commandName} path field required flag");
            AssertEqual("String", GetMetadataProperty(pathField, "Type")!.ToString(), $"{commandName} path field type");
            AssertRegex(
                dispatcherText,
                $"ValidatePathPayload\\(\\n\\s*AutomationCommandKind\\.{commandName},\\n\\s*\"{expected.FieldName}\"",
                $"{commandName} dispatcher path validation");

            var enumValue = Enum.Parse(enumType, commandName);
            AssertThrows<InvalidOperationException>(
                () => validatePath.Invoke(null, new[] { enumValue, expected.FieldName, string.Empty }),
                $"{commandName} empty path validation");
        }

        return Task.CompletedTask;
    }

    internal static Task AutomationManifest_SerializationIsStable()
    {
        const string ExpectedManifestSha256 = "BACAB32C533218A600BF0458C60D9BBB44ECBC8E64D274E5A5F10C598755C385";
        var catalogType = RequireAutomationContractType("Sussudio.Tools.AutomationCommandCatalog");
        var createManifestJson = RequireNonPublicStaticMethod(catalogType, "CreateManifestJson");
        var first = (string)createManifestJson.Invoke(null, Array.Empty<object>())!;
        var second = (string)createManifestJson.Invoke(null, Array.Empty<object>())!;

        AssertEqual(first, second, "Automation manifest repeated serialization");
        AssertDoesNotContain(first, "Timestamp");
        AssertDoesNotContain(first, "DateTime");
        AssertContains(first, "\"SchemaVersion\":1");
        AssertContains(first, "\"Name\":\"GetAutomationManifest\"");
        AssertContains(first, "\"PayloadFields\"");

        var actualSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(first)));
        AssertEqual(ExpectedManifestSha256, actualSha256, "Automation manifest serialized SHA-256");

        return Task.CompletedTask;
    }

    internal static Task ReliabilityGates_RunToolsAndOfflineHarness()
    {
        var scriptText = ReadRepoFile("tools/reliability-gates.ps1")
            .Replace("\r\n", "\n");
        var diagnosticSessionText = ReadDiagnosticSessionRunnerSource();
        var diagnosticSessionCleanupActionsText = ReadDiagnosticSessionCleanupActionsSource();

        AssertContains(scriptText, "$testProjectPath = Join-Path $repoRoot \"tests\\Sussudio.Tests\\Sussudio.Tests.csproj\"");
        AssertContains(scriptText, "$ssctlProjectPath = Join-Path $repoRoot \"tools\\ssctl\\ssctl.csproj\"");
        AssertContains(scriptText, "$mcpServerProjectPath = Join-Path $repoRoot \"tools\\McpServer\\McpServer.csproj\"");
        AssertContains(scriptText, "$nativeXuProbeProjectPath = Join-Path $repoRoot \"tools\\NativeXuAudioProbe\\NativeXuAudioProbe.csproj\"");
        AssertContains(scriptText, "-t:Rebuild");
        AssertContains(scriptText, "\"run\"");
        AssertContains(scriptText, "--no-build");
        AssertContains(scriptText, "$appAssemblyPath");
        AssertContains(scriptText, "Build, tool, and offline regression gates passed.");
        AssertDoesNotContain(scriptText, "docs/testing/README.md");

        AssertContains(diagnosticSessionCleanupActionsText, "var cleanupTimeoutMs = AutomationPipeProtocol.GetDefaultResponseTimeout(AutomationCommandKind.SetFlashbackEnabled);");
        AssertContains(diagnosticSessionCleanupActionsText, "CreateCleanupCts(TimeSpan.FromMilliseconds(cleanupTimeoutMs))");
        AssertContains(diagnosticSessionCleanupActionsText, "new Dictionary<string, object?> { [\"enabled\"] = false }");
        AssertContains(diagnosticSessionCleanupActionsText, "new Dictionary<string, object?> { [\"enabled\"] = true }");
        return Task.CompletedTask;
    }

    internal static Task AutomationSnapshotFormatter_RendersFlashbackSections_WhenIncluded()
    {
        var formatterType = RequireSharedToolType("Sussudio.Tools.AutomationSnapshotFormatter");
        var formatSnapshot = RequireNonPublicStaticMethod(formatterType, "FormatSnapshot");

        using var snapshotDoc = JsonDocument.Parse(
            """
            {
              "Success": true,
              "Snapshot": {
                "SessionState": "Ready",
                "StatusText": "OK",
                "SelectedDeviceName": "Synthetic",
                "SelectedDeviceId": "dev-1",
                "FlashbackActive": true,
                "EncoderCodecName": "hevc_nvenc",
                "EncoderFrameRate": 120,
                "EncoderFrameRateNumerator": 120,
                "EncoderFrameRateDenominator": 1,
                "EncoderTargetBitRate": 12345678,
                "FlashbackBufferedDurationMs": 120000,
                "FlashbackDiskBytes": 1048576,
                "FlashbackTotalBytesWritten": 2097152,
                "FlashbackTempDriveFreeBytes": 2147483648,
                "FlashbackStartupCacheBudgetBytes": 104857600,
                "FlashbackStartupCacheBytes": 52428800,
                "FlashbackStartupCacheSessionCount": 2,
                "FlashbackStartupCacheDeletedSessionCount": 1,
                "FlashbackStartupCacheFreedBytes": 26214400,
                "FlashbackStartupCacheOverBudget": false,
                "FlashbackBackendSettingsStale": true,
                "FlashbackBackendSettingsStaleReason": "preset:P1->P5",
                "FlashbackBackendActiveFormat": "HevcMp4",
                "FlashbackBackendRequestedFormat": "HevcMp4",
                "FlashbackBackendActivePreset": "P1",
                "FlashbackBackendRequestedPreset": "P5",
                "FlashbackPlaybackCommandQueueCapacity": 256,
                "FlashbackPlaybackPendingCommands": 1,
                "FlashbackPlaybackMaxPendingCommands": 4,
                "FlashbackPlaybackLastCommandQueueLatencyMs": 12,
                "FlashbackPlaybackMaxCommandQueueLatencyMs": 87,
                "FlashbackPlaybackMaxCommandQueueLatencyCommand": "Play",
                "FlashbackPlaybackCommandsEnqueued": 12,
                "FlashbackPlaybackCommandsProcessed": 11,
                "FlashbackPlaybackCommandsDropped": 0,
                "FlashbackPlaybackCommandsSkippedNotReady": 2,
                "FlashbackPlaybackSubmitFailures": 3,
                "FlashbackPlaybackScrubUpdatesCoalesced": 9,
                "FlashbackPlaybackSeekCommandsCoalesced": 5,
                "FlashbackPlaybackThreadAlive": true,
                "FlashbackPlaybackLastCommandQueued": "UpdateScrub",
                "FlashbackPlaybackLastCommandProcessed": "BeginScrub",
                "FlashbackPlaybackLastCommandFailure": "not_ready:Pause",
                "FlashbackPlaybackLastCommandFailureUtcUnixMs": 123456789,
                "FlashbackPlaybackTargetFps": 120,
                "FlashbackPlaybackFivePercentLowFps": 118,
                "FlashbackPlaybackSampleDurationMs": 1000,
                "FlashbackPlaybackDecodeSampleCount": 120,
                "FlashbackPlaybackDecodeAvgMs": 1.25,
                "FlashbackPlaybackDecodeP95Ms": 2.5,
                "FlashbackPlaybackDecodeP99Ms": 3.5,
                "FlashbackPlaybackDecodeMaxMs": 4.5,
                "FlashbackPlaybackMaxDecodePhase": "audio",
                "FlashbackPlaybackMaxDecodeReceiveMs": 0.5,
                "FlashbackPlaybackMaxDecodeFeedMs": 4.0,
                "FlashbackPlaybackMaxDecodeReadMs": 0.75,
                "FlashbackPlaybackMaxDecodeSendMs": 3.5,
                "FlashbackPlaybackMaxDecodeAudioMs": 3.25,
                "FlashbackPlaybackMaxDecodeConvertMs": 0.25,
                "FlashbackPlaybackMaxDecodePositionMs": 2345,
                "FlashbackPlaybackSeekForwardDecodeCapHits": 2,
                "FlashbackPlaybackLastSeekHitForwardDecodeCap": true,
                "FlashbackExportActive": true,
                "FlashbackExportStatus": "Running",
                "FlashbackExportId": 7,
                "FlashbackExportPercent": 37.5,
                "FlashbackExportSegmentsProcessed": 3,
                "FlashbackExportTotalSegments": 8,
                "FlashbackExportInPointMs": 1000,
                "FlashbackExportOutPointMs": 9000,
                "FlashbackExportLastProgressUtcUnixMs": 123456,
                "FlashbackExportCompletedUtcUnixMs": 0,
                "FlashbackExportElapsedMs": 2500,
                "FlashbackExportLastProgressAgeMs": 150,
                "FlashbackExportOutputBytes": 1048576,
                "FlashbackExportThroughputBytesPerSec": 419430.4,
                "FlashbackExportOutputPath": "C:/tmp/flashback.mp4",
                "FlashbackExportMessage": "copying packets",
                "FlashbackExportFailureKind": "NoMediaWritten",
                "FlashbackExportForceRotateFallbacks": 1,
                "FlashbackExportLastForceRotateFallbackUtcUnixMs": 12345,
                "FlashbackExportLastForceRotateFallbackSegments": 2,
                "FlashbackExportLastForceRotateFallbackInPointMs": 1000,
                "FlashbackExportLastForceRotateFallbackOutPointMs": 9000,
                "LastExportId": 7
              }
            }
            """);
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        string formatted;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            formatted = (string)formatSnapshot.Invoke(null, new object[] { snapshotDoc.RootElement, true })!;
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }

        AssertContains(formatted, "== Flashback ==");
        AssertContains(formatted, "Encoder: hevc_nvenc 0x0 @ 120 fps (120/1) | Target: 12.3 Mbps");
        AssertContains(formatted, "Buffer: 120.0s | Disk: 1.0 MB | Written: 2 MB");
        AssertContains(formatted, "Temp Cache: cache=50 MB budget=100 MB free=2 GB sessions=2 deleted=1 freed=25 MB overBudget=false");
        AssertContains(formatted, "backendStale=true staleReason=preset:P1->P5 active=HevcMp4/P1 requested=HevcMp4/P5");
        AssertContains(formatted, "submitFailures=3");
        AssertContains(formatted, "Playback Commands: pending=1/256 maxPending=4 lastLatency=12ms maxLatency=87ms maxLatencyCommand=Play enq=12 proc=11 drop=0 skip=2 coalescedScrub=9 coalescedSeek=5 threadAlive=true lastQueued=UpdateScrub lastProcessed=BeginScrub failure=not_ready:Pause failureUtc=123456789");
        AssertContains(formatted, "Target: 120 fps");
        AssertContains(formatted, "5% Low: 118 fps");
        AssertContains(formatted, "Playback Decode: avg=1.25ms P95=2.5ms P99=3.5ms max=4.5ms phase=audio receive=0.5ms feed=4.0ms read=0.75ms send=3.5ms audio=3.25ms convert=0.25ms maxPos=2345ms samples=120 seekCapHits=2 lastSeekCap=true");
        AssertContains(formatted, "Export: active=true status=Running id=7 lastResultId=7 kind=NoMediaWritten progress=37.5% segments=3/8");
        AssertContains(formatted, "elapsed=2500ms progressAge=150ms bytes=1 MB throughput=409.6 KB/s");
        AssertContains(formatted, "forceRotateFallbacks=1 lastForceRotateFallbackSegments=2 lastForceRotateFallbackUtc=12345");

        var omittedFlashbackFormatted = (string)formatSnapshot.Invoke(null, new object[] { snapshotDoc.RootElement, false })!;
        AssertDoesNotContain(omittedFlashbackFormatted, "== Flashback ==");
        AssertDoesNotContain(omittedFlashbackFormatted, "Playback Commands:");
        AssertDoesNotContain(omittedFlashbackFormatted, "Flashback Failure:");

        using var failedFlashbackDoc = JsonDocument.Parse(
            """
            {
              "Success": true,
              "Snapshot": {
                "SessionState": "Error",
                "StatusText": "Flashback failed",
                "SelectedDeviceName": "Synthetic",
                "SelectedDeviceId": "dev-1",
                "FlashbackActive": false,
                "FlashbackEncodingFailed": true,
                "FlashbackEncodingFailureType": "InvalidOperationException",
                "FlashbackEncodingFailureMessage": "Flashback queue overloaded",
                "FlashbackForceRotateActive": true
              }
            }
            """);
        var failedFlashbackFormatted = (string)formatSnapshot.Invoke(null, new object[] { failedFlashbackDoc.RootElement, true })!;
        AssertContains(failedFlashbackFormatted, "== Flashback ==");
        AssertContains(failedFlashbackFormatted, "forceRotate=true");
        AssertContains(failedFlashbackFormatted, "Flashback Failure: active=true type=InvalidOperationException msg=Flashback queue overloaded");

        return Task.CompletedTask;
    }

    internal static Task AutomationSnapshotFormatter_RendersPreviewD3DSections()
    {
        var formatterType = RequireSharedToolType("Sussudio.Tools.AutomationSnapshotFormatter");
        var formatSnapshot = RequireNonPublicStaticMethod(formatterType, "FormatSnapshot");

        using var snapshotDoc = JsonDocument.Parse(
            """
            {
              "Success": true,
              "Snapshot": {
                "SessionState": "Ready",
                "StatusText": "OK",
                "SelectedDeviceName": "Synthetic",
                "SelectedDeviceId": "dev-1",
                "PreviewRendererMode": "D3D11VideoProcessor",
                "PreviewStartupState": "Rendering",
                "PreviewFirstVisualConfirmed": true,
                "PreviewD3DCpuTimingSampleCount": 120,
                "PreviewD3DInputUploadCpuAvgMs": 0.1,
                "PreviewD3DInputUploadCpuP95Ms": 0.2,
                "PreviewD3DInputUploadCpuP99Ms": 0.3,
                "PreviewD3DInputUploadCpuMaxMs": 0.4,
                "PreviewD3DRenderSubmitCpuAvgMs": 0.5,
                "PreviewD3DRenderSubmitCpuP95Ms": 0.6,
                "PreviewD3DRenderSubmitCpuP99Ms": 0.7,
                "PreviewD3DRenderSubmitCpuMaxMs": 0.8,
                "PreviewD3DPresentCallAvgMs": 0.9,
                "PreviewD3DPresentCallP95Ms": 1.0,
                "PreviewD3DPresentCallP99Ms": 1.1,
                "PreviewD3DPresentCallMaxMs": 1.2,
                "PreviewD3DTotalFrameCpuAvgMs": 1.3,
                "PreviewD3DTotalFrameCpuP95Ms": 1.4,
                "PreviewD3DTotalFrameCpuP99Ms": 1.5,
                "PreviewD3DTotalFrameCpuMaxMs": 1.6,
                "PreviewD3DPipelineLatencySampleCount": 120,
                "PreviewD3DPipelineLatencyAvgMs": 7.8,
                "PreviewD3DPipelineLatencyP95Ms": 8.9,
                "PreviewD3DPipelineLatencyP99Ms": 9.9,
                "PreviewD3DPipelineLatencyMaxMs": 12.3,
                "PreviewD3DLastRenderedPipelineLatencyMs": 8.4,
                "PreviewD3DFrameLatencyWaitEnabled": true,
                "PreviewD3DFrameLatencyWaitHandleActive": true,
                "PreviewD3DFrameLatencyWaitCallCount": 118,
                "PreviewD3DFrameLatencyWaitSignaledCount": 110,
                "PreviewD3DFrameLatencyWaitTimeoutCount": 8,
                "PreviewD3DFrameLatencyWaitUnexpectedResultCount": 0,
                "PreviewD3DFrameLatencyWaitLastResult": 0,
                "PreviewD3DFrameLatencyWaitLastMs": 0.05,
                "PreviewD3DFrameLatencyWaitSampleCount": 118,
                "PreviewD3DFrameLatencyWaitAvgMs": 0.2,
                "PreviewD3DFrameLatencyWaitP95Ms": 0.8,
                "PreviewD3DFrameLatencyWaitP99Ms": 1.4,
                "PreviewD3DFrameLatencyWaitMaxMs": 2.0,
                "PreviewD3DFrameStatsSampleCount": 120,
                "PreviewD3DFrameStatsSuccessCount": 119,
                "PreviewD3DFrameStatsFailureCount": 1,
                "PreviewD3DFrameStatsRecentFailureCount": 1,
                "PreviewD3DFrameStatsMissedRefreshCount": 4,
                "PreviewD3DFrameStatsRecentMissedRefreshCount": 2,
                "PreviewD3DFrameStatsLastError": "DXGI_ERROR_WAS_STILL_DRAWING",
                "PreviewD3DLastSubmittedPreviewPresentId": 41,
                "PreviewD3DLastSubmittedSourceSequenceNumber": 9000,
                "PreviewD3DLastSubmittedSourcePtsTicks": 123456,
                "PreviewD3DLastRenderedPreviewPresentId": 42,
                "PreviewD3DLastRenderedSourceSequenceNumber": 9001,
                "PreviewD3DLastRenderedSourcePtsTicks": 123789,
                "PreviewD3DLastRenderedSchedulerToPresentMs": 7.7,
                "PreviewD3DLastDropReason": "none",
                "PreviewD3DLastDroppedSourcePtsTicks": 0,
                "PreviewD3DRecentSlowFrames": [
                  {
                    "PreviewPresentId": 42,
                    "SourceSequenceNumber": 9001,
                    "PresentIntervalMs": 9.2,
                    "InputUploadCpuMs": 1.1,
                    "RenderSubmitCpuMs": 2.2,
                    "PresentCallMs": 3.3,
                    "TotalFrameCpuMs": 6.6,
                    "SchedulerToPresentMs": 7.7,
                    "PipelineLatencyMs": 8.8,
                    "ExpectedIntervalMs": 8.33,
                    "DiagnosticThresholdMs": 8.5,
                    "WorstOverBudgetMs": 0.87,
                    "SlowReason": "present_interval",
                    "PendingFrameCount": 1,
                    "DxgiPresentDelta": 1,
                    "DxgiPresentRefreshDelta": 2,
                    "DxgiSyncRefreshDelta": 2
                  }
                ],
                "SourceWidth": 3840,
                "SourceHeight": 2160
              }
            }
            """);
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        string formatted;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            formatted = (string)formatSnapshot.Invoke(null, new object[] { snapshotDoc.RootElement, false })!;
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }

        AssertContains(formatted, "== Preview ==");
        AssertContains(formatted, "D3D CPU timing: input/upload avg=0.1ms P95=0.2ms P99=0.3ms max=0.4ms | render-submit avg=0.5ms P95=0.6ms P99=0.7ms max=0.8ms | present-call avg=0.9ms P95=1.0ms P99=1.1ms max=1.2ms | total-frame avg=1.3ms P95=1.4ms P99=1.5ms max=1.6ms samples=120");
        AssertContains(formatted, "D3D pipeline latency: avg=7.8ms P95=8.9ms P99=9.9ms max=12.3ms last=8.4ms samples=120");
        AssertContains(formatted, "D3D frame-latency wait: enabled=true handle=true calls=118 signaled=110 timeouts=8 unexpected=0 lastResult=0 last=0.05ms avg=0.2ms P95=0.8ms max=2.0ms samples=118");
        AssertContains(formatted, "D3D DXGI stats: ok=119/120 failures=1 recentFailures=1 missedRefresh=4 recentMissed=2 lastError=DXGI_ERROR_WAS_STILL_DRAWING");
        AssertContains(formatted, "D3D Ownership: submitted present=41 sourceSeq=9000 pts=123456 | rendered present=42 sourceSeq=9001 pts=123789 schedulerToPresent=7.7ms pipeline=8.4ms | lastDrop=none dropPts=0");
        AssertContains(formatted, "D3D Slow Frames: present=42 srcSeq=9001 reason=present_interval target=8.33ms over=0.87ms interval=9.20ms");
        AssertContains(formatted, "presentCall=3.30ms sched=7.70ms pipeline=8.80ms");
        AssertOccursBefore(formatted, "D3D CPU timing:", "D3D pipeline latency:");
        AssertOccursBefore(formatted, "D3D pipeline latency:", "D3D frame-latency wait:");
        AssertOccursBefore(formatted, "D3D frame-latency wait:", "D3D DXGI stats:");
        AssertOccursBefore(formatted, "D3D DXGI stats:", "D3D Ownership:");
        AssertOccursBefore(formatted, "D3D Ownership:", "D3D Slow Frames:");

        return Task.CompletedTask;
    }

    internal static Task AutomationSnapshotFormatter_FormatsCoreSectionsAndTypedAccessors()
    {
        var formatterType = RequireSharedToolType("Sussudio.Tools.AutomationSnapshotFormatter");
        var isSuccess = RequireNonPublicStaticMethod(formatterType, "IsSuccess");
        var get = RequireNonPublicStaticMethod(formatterType, "Get");
        var getInt = RequireNonPublicStaticMethod(formatterType, "GetInt");
        var getDouble = RequireNonPublicStaticMethod(formatterType, "GetDouble");
        var getLong = RequireNonPublicStaticMethod(formatterType, "GetLong");
        var computeTickAge = RequireNonPublicStaticMethod(formatterType, "ComputeTickAgeMs");
        var formatSnapshot = RequireNonPublicStaticMethod(formatterType, "FormatSnapshot");

        using var accessorsDoc = JsonDocument.Parse(
            "{\"Success\":true,\"Name\":\"Camera\",\"Count\":\"42\",\"Rate\":\"59.94\",\"Bytes\":123456789,\"Items\":[1],\"Empty\":[],\"Missing\":null}");
        var accessors = accessorsDoc.RootElement;
        AssertEqual(true, (bool)isSuccess.Invoke(null, new object[] { accessors })!, "AutomationSnapshotFormatter.IsSuccess true");
        AssertEqual("Camera", (string)get.Invoke(null, new object[] { accessors, "Name", "fallback" })!, "AutomationSnapshotFormatter.Get string");
        AssertEqual("true", (string)get.Invoke(null, new object[] { accessors, "Success", "fallback" })!, "AutomationSnapshotFormatter.Get bool");
        AssertEqual("fallback", (string)get.Invoke(null, new object[] { accessors, "Empty", "fallback" })!, "AutomationSnapshotFormatter.Get empty array fallback");
        AssertEqual("fallback", (string)get.Invoke(null, new object[] { accessors, "Missing", "fallback" })!, "AutomationSnapshotFormatter.Get null fallback");
        AssertEqual(42, (int)getInt.Invoke(null, new object[] { accessors, "Count", 0 })!, "AutomationSnapshotFormatter.GetInt string");
        AssertEqual(59.94d, (double)getDouble.Invoke(null, new object[] { accessors, "Rate", 0d })!, "AutomationSnapshotFormatter.GetDouble string");
        AssertEqual(123456789L, (long)getLong.Invoke(null, new object[] { accessors, "Bytes", 0L })!, "AutomationSnapshotFormatter.GetLong number");
        AssertEqual(-1L, (long)computeTickAge.Invoke(null, new object[] { 0L })!, "AutomationSnapshotFormatter.ComputeTickAgeMs non-positive");

        using var invalidDoc = JsonDocument.Parse("[]");
        AssertEqual(
            "Snapshot response was not a JSON object.",
            (string)formatSnapshot.Invoke(null, new object[] { invalidDoc.RootElement, false })!,
            "AutomationSnapshotFormatter non-object response");

        using var missingSnapshotDoc = JsonDocument.Parse("{\"Message\":\"Snapshot warming up\"}");
        AssertEqual(
            "Snapshot warming up",
            (string)formatSnapshot.Invoke(null, new object[] { missingSnapshotDoc.RootElement, false })!,
            "AutomationSnapshotFormatter missing snapshot message");

        using var snapshotDoc = JsonDocument.Parse(
            """
            {
              "Success": true,
              "Snapshot": {
                "SessionState": "Ready",
                "StatusText": "OK",
                "SelectedDeviceName": "Synthetic",
                "SelectedDeviceId": "dev-1",
                "IsInitialized": true,
                "IsPreviewing": true,
                "IsRecording": false,
                "SelectedResolution": "3840x2160",
                "SelectedFriendlyFrameRate": "59.94",
                "SelectedExactFrameRate": "59.940",
                "SelectedExactFrameRateArg": "60000/1001",
                "SelectedRecordingFormat": "HevcMp4",
                "SelectedQuality": "High",
                "SelectedPreset": "P5",
                "SelectedVideoFormat": "MJPG",
                "SelectedSplitEncodeMode": "Auto",
                "PreviewVolumePercent": 42.5,
                "IsStatsVisible": true,
                "IsHdrEnabled": false,
                "IsHdrAvailable": true,
                "HdrOutputActive": false,
                "HdrRuntimeState": "Inactive",
                "RequestedPipelineMode": "SDR",
                "ActivePipelineMode": "SDR",
                "PipelineModeMatched": true,
                "IsAudioEnabled": true,
                "IsAudioPreviewEnabled": false,
                "IsCustomAudioInputEnabled": false,
                "AudioPeak": 0,
                "AudioClipping": false,
                "AudioSignalPresent": false,
                "AudioReaderActive": false,
                "AudioFramesArrived": 0,
                "AudioFramesWrittenToSink": 0,
                "VideoReaderActive": true,
                "IngestVideoFramesArrived": 120,
                "IngestVideoFramesWrittenToSink": 120,
                "EncoderVideoFramesEnqueued": 0,
                "EncoderVideoFramesEncoded": 0,
                "FfmpegVideoQueueDepth": 0,
                "VideoDropsQueueSaturated": 0,
                "IngestLastVideoFrameAgeMs": 5,
                "EncoderLastEnqueueAgeMs": 0,
                "EncoderLastWriteAgeMs": 0,
                "MemoryPreference": "Gpu",
                "VideoRequestedSubtype": "MJPG",
                "VideoNegotiatedSubtype": "MJPG",
                "VideoIngestErrorCount": 0,
                "SourceReaderReadOutstanding": false,
                "SourceReaderReadOutstandingMs": 0,
                "SourceReaderLastFrameTickMs": 0,
                "SourceReaderFrameChannelDepth": 0,
                "WasapiCaptureCallbackCount": 0,
                "WasapiCaptureCallbackAvgIntervalMs": 0,
                "WasapiCaptureCallbackMaxIntervalMs": 0,
                "WasapiCaptureCallbackSilenceCount": 0,
                "WasapiCaptureLastCallbackTickMs": 0,
                "WasapiCaptureAudioLevelEventsFired": 0,
                "WasapiPlaybackRenderCallbackCount": 0,
                "WasapiPlaybackRenderSilenceCount": 0,
                "WasapiPlaybackQueueDepth": 0,
                "WasapiPlaybackQueueDropCount": 0,
                "WasapiPlaybackLastRenderTickMs": 0,
                "OutputPath": "",
                "RecordingTime": "00:00:00",
                "RecordingSizeInfo": "0 B",
                "RecordingBitrateInfo": "0 Mbps",
                "RecordingBackend": "None",
                "AudioPathMode": "None",
                "MuxResult": "NotAttempted",
                "LastOutputPath": "",
                "LastOutputSizeBytes": 0,
                "LastFinalizeStatus": "None",
                "FlashbackActive": true,
                "FlashbackEncodingFailed": true,
                "DiagnosticHealthStatus": "Healthy",
                "DiagnosticLikelyStage": "None",
                "DiagnosticSummary": "OK",
                "DiagnosticEvidence": "stable",
                "DiagnosticSourceLane": "ok",
                "DiagnosticDecodeLane": "ok",
                "DiagnosticPreviewLane": "ok",
                "DiagnosticRenderLane": "ok",
                "DiagnosticPresentLane": "ok",
                "DiagnosticRecordingLane": "idle",
                "DiagnosticAudioLane": "idle",
                "PerformanceScore": 100,
                "PerformancePerfectionMet": true,
                "PerformanceSummary": "OK",
                "EstimatedPipelineLatencyMs": 1,
                "ProcessCpuPercent": 1.5,
                "ProcessCpuTotalProcessorTimeMs": 1200,
                "MemoryWorkingSetMb": 256,
                "MemoryPrivateBytesMb": 128,
                "MemoryManagedHeapMb": 16,
                "MemoryTotalAllocatedMb": 64,
                "MemoryGcHeapSizeMb": 32,
                "MemoryGcGen0Collections": 1,
                "MemoryGcGen1Collections": 0,
                "MemoryGcGen2Collections": 0,
                "MemoryGcPauseTimePercent": 0,
                "MemoryGcFragmentationPercent": 0,
                "ThreadPoolWorkerAvailable": 32766,
                "ThreadPoolWorkerMax": 32767,
                "ThreadPoolIoAvailable": 1000,
                "ThreadPoolIoMax": 1000,
                "CaptureCadenceObservedFps": 120,
                "ExpectedCaptureFrameRate": 120,
                "CaptureCadenceSampleCount": 300,
                "CaptureCadenceAverageIntervalMs": 8.3,
                "CaptureCadenceP95IntervalMs": 8.5,
                "CaptureCadenceP99IntervalMs": 8.7,
                "CaptureCadenceMaxIntervalMs": 9.0,
                "CaptureCadenceFivePercentLowFps": 119,
                "CaptureCadenceOnePercentLowFps": 118,
                "CaptureCadenceSampleDurationMs": 2500,
                "CaptureCadenceJitterStdDevMs": 0.1,
                "CaptureCadenceSevereGapCount": 0,
                "CaptureCadenceEstimatedDroppedFrames": 0,
                "CaptureCadenceEstimatedDropPercent": 0,
                "MjpegDecodeSampleCount": 1,
                "MjpegDecodeAvgMs": 2.1,
                "MjpegDecodeP95Ms": 3.1,
                "MjpegDecodeMaxMs": 4.1,
                "MjpegDecoderCount": 1,
                "MjpegPerDecoder": [
                  { "WorkerIndex": 0, "AvgMs": 2.1, "P95Ms": 3.1, "MaxMs": 4.1, "SampleCount": 5 }
                ],
                "AvSyncCaptureDriftMs": 1.5,
                "AvSyncCaptureDriftRateMsPerSec": 0.1,
                "AvSyncEncoderDriftMs": -0.5,
                "AvSyncEncoderCorrectionSamples": 2,
                "PreviewRendererMode": "Software",
                "PreviewStartupState": "Rendering",
                "PreviewFirstVisualConfirmed": true,
                "PreviewCadenceObservedFps": 120,
                "DetectedSourceFrameRate": 120,
                "SourceWidth": 3840,
                "SourceHeight": 2160,
                "SourceIsHdr": false,
                "SourceTelemetryAvailability": "Available",
                "SourceTelemetryConfidence": "High"
              }
            }
            """);
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        string formatted;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
            formatted = (string)formatSnapshot.Invoke(null, new object[] { snapshotDoc.RootElement, false })!;
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }

        AssertContains(formatted, "== Sussudio State ==");
        AssertContains(formatted, "Device: Synthetic (dev-1)");
        AssertContains(formatted, "Frame Rate: 59.94 fps (59.940 fps, 60000/1001)");
        AssertContains(formatted, "== Thread Health ==");
        AssertContains(formatted, "WASAPI Playback:");
        AssertContains(formatted, "== Diagnostics ==");
        AssertContains(formatted, "Legacy Score:");
        AssertContains(formatted, "Pipeline Latency: 1ms (app receive -> estimated visible)");
        AssertContains(formatted, "Process CPU: 1.5%");
        AssertContains(formatted, "== MJPEG Pipeline Timing ==");
        AssertContains(formatted, "Decoder[0]: avg=2.1ms");
        AssertContains(formatted, "== AV Sync ==");
        AssertContains(formatted, "Capture Drift: 1.5ms | Rate: 0.1ms/s");
        AssertContains(formatted, "Encoder Drift: -0.5ms | Correction Samples: 2");
        AssertContains(formatted, "== Preview ==");
        AssertContains(formatted, "== Source ==");
        AssertDoesNotContain(formatted, "== Flashback ==");
        AssertDoesNotContain(formatted, "Flashback Failure:");

        AssertOccursBefore(formatted, "== Sussudio State ==", "== Capture Settings ==");
        AssertOccursBefore(formatted, "== Capture Settings ==", "== Audio ==");
        AssertOccursBefore(formatted, "== Audio ==", "== Video Pipeline ==");
        AssertOccursBefore(formatted, "== Video Pipeline ==", "== Thread Health ==");
        AssertOccursBefore(formatted, "== Thread Health ==", "== Recording ==");
        AssertOccursBefore(formatted, "== Recording ==", "== Diagnostics ==");
        AssertOccursBefore(formatted, "== Diagnostics ==", "== Performance ==");
        AssertOccursBefore(formatted, "== Performance ==", "== Memory & GC ==");
        AssertOccursBefore(formatted, "== Memory & GC ==", "== Capture Cadence ==");
        AssertOccursBefore(formatted, "== Capture Cadence ==", "== MJPEG Pipeline Timing ==");
        AssertOccursBefore(formatted, "== MJPEG Pipeline Timing ==", "== AV Sync ==");
        AssertOccursBefore(formatted, "== AV Sync ==", "== Preview ==");
        AssertOccursBefore(formatted, "== Preview ==", "== Source ==");

        return Task.CompletedTask;
    }

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

    internal static Task PresentMonProbe_SourceOwnership_IsUnified()
    {
        static string ReadPresentMonProbeFile(string fileName)
            => ReadRepoFile($"tools/Common/PresentMon/{fileName}").Replace("\r\n", "\n");

        var rootText = ReadPresentMonProbeFile("PresentMonProbe.cs");
        var formatText = rootText;
        var csvText = rootText;

        AssertContains(rootText, "public static class PresentMonProbe");
        AssertDoesNotContain(rootText, "partial class PresentMonProbe");
        AssertContains(rootText, "public static async Task<PresentMonProbeResult> RunAsync(");
        AssertContains(rootText, "var targetProcess = ResolveTargetProcess(options);");
        AssertContains(rootText, "var presentMonPath = ResolvePresentMonPath(options.PresentMonPath);");
        AssertContains(rootText, "var outputPath = ResolveOutputPath(options.OutputFile);");
        AssertContains(rootText, "var arguments = BuildArguments(");
        AssertContains(rootText, "private static string BuildArguments(");
        AssertContains(rootText, "private static string QuoteArgument(");
        AssertContains(rootText, "private static string BuildResultMessage(");
        AssertContains(rootText, "Captured {summary.RawSampleCount} PresentMon frame rows");
        AssertContains(rootText, "expected swap chain {summary.ExpectedSwapChainAddress} was not present");
        AssertContains(rootText, "PresentMon capture did not produce frame rows.");
        AssertContains(rootText, "var run = await RunProcessAsync(");
        AssertContains(rootText, "summary = ParseCsv(outputPath, options.ExpectedSwapChainAddress, options, captureStartUtcUnixMs);");
        AssertContains(rootText, "TryDelete(outputPath);");

        AssertContains(rootText, "public readonly record struct PresentMonProbeCorrelation(");
        AssertContains(rootText, "public static PresentMonProbeOptions CreateOptions(");
        AssertContains(rootText, "ExpectedSwapChainAddress = string.IsNullOrWhiteSpace(swapChainAddress)");
        AssertContains(rootText, "AppPresentId = appPresentId ?? correlation.PresentId");
        AssertContains(rootText, "public static PresentMonProbeCorrelation ReadPreviewCorrelation(JsonElement snapshot)");
        AssertContains(rootText, "PreviewD3DSwapChainAddress");
        AssertContains(rootText, "PreviewD3DLastRenderedPreviewPresentId");
        AssertContains(rootText, "PreviewD3DLastRenderedSourceSequenceNumber");
        AssertContains(rootText, "PreviewD3DLastRenderedUtcUnixMs");
        AssertContains(rootText, "private static long? GetPositiveLong(");
        AssertContains(rootText, "private static long? GetNonNegativeLong(");

        AssertContains(rootText, "public sealed class PresentMonProbeOptions");
        AssertContains(rootText, "public sealed class PresentMonProbeResult");
        AssertContains(rootText, "public sealed class PresentMonCaptureSummary");
        AssertContains(rootText, "public sealed class PresentMonAppCorrelation");
        AssertContains(rootText, "public sealed class PresentMonSwapChainSummary");
        AssertContains(rootText, "public sealed class PresentMonMetricSummary");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Models.cs")),
            "PresentMon public DTOs live with PresentMonProbe.RunAsync and result formatting");

        AssertContains(formatText, "public static string Format(PresentMonProbeResult result)");
        AssertContains(formatText, "private static void AppendSummaryContext(");
        AssertContains(formatText, "private static void AppendMetric(");
        AssertContains(formatText, "private static void AppendAppCorrelation(");
        AssertContains(formatText, "private static void AppendSwapChains(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Format.cs")),
            "PresentMon result formatting lives with PresentMonProbe.RunAsync");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Csv.cs")),
            "PresentMon CSV parsing and aggregation live with PresentMonProbe.RunAsync");

        AssertContains(csvText, "private static PresentMonCaptureSummary ParseCsv(");
        AssertContains(csvText, "var csvRows = ReadCsvRows(path);");
        AssertContains(csvText, "var rows = csvRows.Rows;");
        AssertContains(csvText, "var selectedRows = selectedSwapChain == null");
        AssertContains(csvText, "var swapChains = BuildSwapChainSummaries(rows, selectedSwapChain);");
        AssertContains(csvText, "var warnings = BuildWarnings(");
        AssertContains(csvText, "var appCorrelation = BuildAppCorrelation(");
        AssertContains(csvText, "private static IReadOnlyList<PresentMonSwapChainSummary> BuildSwapChainSummaries(");
        AssertContains(csvText, "private static string? NormalizeSwapChainAddress(");
        AssertContains(csvText, "private static string NormalizeHeader(");
        AssertContains(csvText, "private static double? ReadMetric(");
        AssertContains(csvText, "private static List<string> SplitCsvLine(");
        AssertContains(csvText, "private static PresentMonCsvRows ReadCsvRows(string path)");
        AssertContains(csvText, "private sealed record PresentMonCsvRows(");
        AssertContains(csvText, "private sealed record PresentMonRow(");
        AssertContains(csvText, "private static IReadOnlyDictionary<string, int> BuildCsvHeaderIndex(");
        AssertContains(csvText, "private static PresentMonRow ReadRow(");
        AssertContains(csvText, "rows.Add(ReadRow(rowIndex++, fields, index));");
        AssertContains(csvText, "private static bool HasAnyColumn(");
        AssertContains(csvText, "private static PresentMonAppCorrelation BuildAppCorrelation(");
        AssertContains(csvText, "private static string ClassifyPresentOutcome(");
        AssertContains(csvText, "private static IReadOnlyList<string> BuildWarnings(");
        AssertContains(csvText, "private static PresentMonMetricSummary Summarize(");
        AssertContains(csvText, "private static double Percentile(");

        AssertContains(rootText, "private static Process? ResolveTargetProcess(");
        AssertContains(rootText, "private static string? ResolvePresentMonPath(");
        AssertContains(rootText, "private static string ResolveOutputPath(");
        AssertContains(rootText, "private static async Task<ProcessRun> RunProcessAsync(");
        AssertContains(rootText, "private static async Task<string> TryReadAsync(");
        AssertContains(rootText, "private static void TryKill(");
        AssertContains(rootText, "private static void TryDelete(");
        AssertContains(rootText, "private sealed class ProcessRun");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Paths.cs")),
            "PresentMon path resolution lives with PresentMonProbe.RunAsync");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Process.cs")),
            "PresentMon process supervision lives with PresentMonProbe.RunAsync");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Csv.Rows.cs")),
            "PresentMon CSV row ingestion lives with PresentMonProbe.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Csv.Correlation.cs")),
            "PresentMon CSV app correlation lives with PresentMonProbe.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "PresentMon", "PresentMonProbe.Csv.Summary.cs")),
            "PresentMon CSV warnings and percentile summaries live with PresentMonProbe.cs");

        return Task.CompletedTask;
    }

    internal static Task PresentMonParser_SelectsDominantNonArtifactSwapChain()
    {
        var toolAssembly = LoadToolAssemblyIsolated(global::Program.SsctlAssemblyRelativePath);
        var probeType = toolAssembly.GetType("Sussudio.Tools.PresentMonProbe")
            ?? throw new InvalidOperationException("Sussudio.Tools.PresentMonProbe type not found.");
        var parseCsv = probeType.GetMethod(
                "ParseCsv",
                BindingFlags.Static | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(string) },
                modifiers: null)
            ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv(string) not found.");
        var parseCsvWithExpectedSwapChain = probeType.GetMethod(
                "ParseCsv",
                BindingFlags.Static | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(string), typeof(string) },
                modifiers: null)
            ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv(string,string) not found.");
        var optionsType = toolAssembly.GetType("Sussudio.Tools.PresentMonProbeOptions")
            ?? throw new InvalidOperationException("PresentMonProbeOptions type not found.");
        var parseCsvWithCorrelation = probeType.GetMethod(
                "ParseCsv",
                BindingFlags.Static | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(string), typeof(string), optionsType, typeof(long?) },
                modifiers: null)
            ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv correlation overload not found.");

        var csvPath = Path.Combine(Path.GetTempPath(), $"presentmon_parser_{Guid.NewGuid():N}.csv");
        File.WriteAllText(
            csvPath,
            """
            Application,ProcessID,SwapChainAddress,PresentRuntime,SyncInterval,PresentFlags,AllowsTearing,PresentMode,TimeInMs,MsBetweenPresents,MsBetweenDisplayChange,DisplayedTime,MsUntilDisplayed,MsInPresentAPI,MsCPUBusy,MsGPUBusy,MsGPUTime,DisplayLatency
            Sussudio.exe,1234,0xABC,DXGI,0,0,0,Composed: Flip,0.0000,8.3333,8.3333,NA,16.0000,0.0700,8.2500,2.0000,7.0000,NA
            Sussudio.exe,1234,0xABC,DXGI,0,0,0,Composed: Flip,8.3333,8.3334,8.3334,NA,16.1000,0.0710,8.2600,2.1000,7.1000,NA
            Sussudio.exe,1234,0x0,Other,-1,0,0,Composed: Flip,1000.0000,999.0000,999.0000,NA,16.2000,0.0800,999.0000,2.2000,7.2000,NA
            """);

        try
        {
            var summary = parseCsv.Invoke(null, new object[] { csvPath })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null.");

            AssertEqual(2, GetIntProperty(summary, "SampleCount"), "selected PresentMon sample count");
            AssertEqual(3, GetIntProperty(summary, "RawSampleCount"), "raw PresentMon sample count");
            AssertEqual(1, GetIntProperty(summary, "ExcludedSampleCount"), "excluded PresentMon sample count");
            AssertEqual("0xABC", GetStringProperty(summary, "SelectedSwapChainAddress"), "selected PresentMon swap chain");

            var betweenPresents = GetPropertyValue(summary, "BetweenPresentsMs")
                ?? throw new InvalidOperationException("BetweenPresentsMs was null.");
            AssertNearlyEqual(8.33335, GetDoubleProperty(betweenPresents, "Average"), 0.0001, "selected PresentMon average");
            AssertNearlyEqual(8.3334, GetDoubleProperty(betweenPresents, "Max"), 0.0001, "selected PresentMon max");

            var swapChains = GetPropertyValue(summary, "SwapChains")
                ?? throw new InvalidOperationException("SwapChains was null.");
            AssertEqual(2, GetCountProperty(swapChains), "PresentMon swap chain summary count");

            File.WriteAllText(
                csvPath,
                """
                Application,ProcessID,SwapChainAddress,PresentRuntime,SyncInterval,PresentFlags,AllowsTearing,PresentMode,TimeInMs,MsBetweenPresents,MsBetweenDisplayChange,DisplayedTime,MsUntilDisplayed,MsInPresentAPI,MsCPUBusy,MsGPUBusy,MsGPUTime,DisplayLatency
                Sussudio.exe,1234,0xAAA,DXGI,0,0,0,Composed: Flip,0.0000,99.0000,99.0000,8.3333,16.0000,0.0700,8.2500,2.0000,7.0000,20.0000
                Sussudio.exe,1234,0x0000000000000BBB,DXGI,0,0,0,Composed: Flip,8.3333,8.3333,8.3333,8.3333,16.1000,0.0710,8.2600,2.1000,7.1000,20.1000
                """);

            var expectedSwapChainSummary = parseCsvWithExpectedSwapChain.Invoke(null, new object[] { csvPath, "0xbbb" })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for expected swap-chain CSV.");
            AssertEqual("0xBBB", GetStringProperty(expectedSwapChainSummary, "SelectedSwapChainAddress"), "expected PresentMon selected swap chain");
            AssertEqual(true, GetBoolProperty(expectedSwapChainSummary, "ExpectedSwapChainMatched"), "expected PresentMon swap chain matched");
            var expectedBetweenPresents = GetPropertyValue(expectedSwapChainSummary, "BetweenPresentsMs")
                ?? throw new InvalidOperationException("expected BetweenPresentsMs was null.");
            AssertNearlyEqual(8.3333, GetDoubleProperty(expectedBetweenPresents, "Average"), 0.0001, "expected swap-chain PresentMon average");

            File.WriteAllText(
                csvPath,
                """
                Application,ProcessID,SwapChainAddress,PresentRuntime,SyncInterval,PresentFlags,AllowsTearing,PresentMode,CPUStartTime,FrameTime,CPUBusy,GPUTime,DisplayedTime,MsUntilDisplayed,DisplayLatency
                Sussudio.exe,1234,0xBBB,DXGI,0,0,0,Composed: Flip,90.0000,8.3333,8.2000,6.0000,8.3333,6.0000,12.0000
                Sussudio.exe,1234,0xBBB,DXGI,0,0,0,Composed: Flip,104.0000,8.3333,8.2000,6.0000,NA,20.0000,18.0000
                """);
            var options = Activator.CreateInstance(optionsType)
                ?? throw new InvalidOperationException("Failed to create PresentMonProbeOptions.");
            SetPropertyOrBackingField(options, "AppPresentId", 42L);
            SetPropertyOrBackingField(options, "AppSourceSequenceNumber", 1001L);
            SetPropertyOrBackingField(options, "AppPresentUtcUnixMs", 1105L);
            SetPropertyOrBackingField(options, "CaptureStartUtcUnixMs", 1000L);
            var correlatedSummary = parseCsvWithCorrelation.Invoke(null, new object?[] { csvPath, "0xBBB", options, 1000L })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for correlated CSV.");
            var appCorrelation = GetPropertyValue(correlatedSummary, "AppCorrelation")
                ?? throw new InvalidOperationException("AppCorrelation was null.");
            AssertEqual(true, GetBoolProperty(appCorrelation, "Available"), "PresentMon app correlation available");
            AssertEqual(42L, GetLongProperty(appCorrelation, "AppPresentId"), "PresentMon app present id");
            AssertEqual(1001L, GetLongProperty(appCorrelation, "AppSourceSequenceNumber"), "PresentMon app source sequence");
            AssertEqual(1, GetIntProperty(appCorrelation, "PresentMonRowIndex"), "PresentMon correlated row index");
            AssertNearlyEqual(1.0, GetDoubleProperty(appCorrelation, "DeltaMs"), 0.0001, "PresentMon app correlation delta");
            AssertEqual("SupersededOrNotDisplayed", GetStringProperty(appCorrelation, "Outcome"), "PresentMon app correlation outcome");

            var missingExpectedSwapChainSummary = parseCsvWithExpectedSwapChain.Invoke(null, new object[] { csvPath, "0xCCC" })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for missing expected swap-chain CSV.");
            AssertEqual(0, GetIntProperty(missingExpectedSwapChainSummary, "SampleCount"), "missing expected PresentMon sample count");
            AssertEqual(2, GetIntProperty(missingExpectedSwapChainSummary, "RawSampleCount"), "missing expected raw PresentMon sample count");
            AssertEqual(2, GetIntProperty(missingExpectedSwapChainSummary, "ExcludedSampleCount"), "missing expected excluded PresentMon sample count");
            AssertEqual("0xCCC", GetStringProperty(missingExpectedSwapChainSummary, "ExpectedSwapChainAddress"), "missing expected PresentMon swap chain");
            AssertEqual(false, GetBoolProperty(missingExpectedSwapChainSummary, "ExpectedSwapChainMatched"), "missing expected PresentMon swap chain matched");
            AssertEqual(string.Empty, GetStringProperty(missingExpectedSwapChainSummary, "SelectedSwapChainAddress"), "missing expected selected PresentMon swap chain");

            File.WriteAllText(
                csvPath,
                """
                Application,ProcessID,SwapChainAddress,PresentRuntime,SyncInterval,PresentFlags,AllowsTearing,PresentMode,CPUStartTime,FrameTime,CPUBusy,CPUWait,GPULatency,GPUTime,GPUBusy,GPUWait,VideoBusy,DisplayLatency,DisplayedTime
                Sussudio.exe,1234,0xDEF,DXGI,0,0,0,Composed: Flip,0.0000,9.0000,8.9000,0.1000,3.0000,6.0000,2.0000,4.0000,7.0000,22.0000,8.3333
                Sussudio.exe,1234,0xDEF,DXGI,0,0,0,Composed: Flip,9.0000,7.6666,7.5000,0.1666,3.0000,6.5000,2.5000,4.0000,7.0000,22.5000,8.3334
                """);

            var v2Summary = parseCsv.Invoke(null, new object[] { csvPath })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for v2 CSV.");
            var v2BetweenPresents = GetPropertyValue(v2Summary, "BetweenPresentsMs")
                ?? throw new InvalidOperationException("v2 BetweenPresentsMs was null.");
            var v2CpuBusy = GetPropertyValue(v2Summary, "CpuBusyMs")
                ?? throw new InvalidOperationException("v2 CpuBusyMs was null.");
            var v2GpuBusy = GetPropertyValue(v2Summary, "GpuBusyMs")
                ?? throw new InvalidOperationException("v2 GpuBusyMs was null.");
            var v2GpuTime = GetPropertyValue(v2Summary, "GpuTimeMs")
                ?? throw new InvalidOperationException("v2 GpuTimeMs was null.");
            AssertNearlyEqual(8.3333, GetDoubleProperty(v2BetweenPresents, "Average"), 0.0001, "v2 PresentMon frame time average");
            AssertNearlyEqual(8.2, GetDoubleProperty(v2CpuBusy, "Average"), 0.0001, "v2 PresentMon CPU busy average");
            AssertNearlyEqual(2.25, GetDoubleProperty(v2GpuBusy, "Average"), 0.0001, "v2 PresentMon GPU busy average");
            AssertNearlyEqual(6.25, GetDoubleProperty(v2GpuTime, "Average"), 0.0001, "v2 PresentMon GPU time average");

            File.WriteAllText(
                csvPath,
                """
                Application,ProcessID,SwapChainAddress,PresentRuntime,SyncInterval,PresentFlags,AllowsTearing,PresentMode,TimeInMs,MsBetweenPresents,MsBetweenDisplayChange,DisplayedTime,MsUntilDisplayed,MsInPresentAPI,MsCPUBusy,MsGPUBusy,MsGPUTime,DisplayLatency
                Sussudio.exe,1234,0x0,Other,-1,0,0,Composed: Flip,1000.0000,999.0000,999.0000,NA,16.2000,0.0800,999.0000,2.2000,7.2000,NA
                """);

            var artifactOnlySummary = parseCsv.Invoke(null, new object[] { csvPath })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for artifact-only CSV.");
            AssertEqual(0, GetIntProperty(artifactOnlySummary, "SampleCount"), "artifact-only selected sample count");
            AssertEqual(1, GetIntProperty(artifactOnlySummary, "RawSampleCount"), "artifact-only raw sample count");
            AssertEqual(1, GetIntProperty(artifactOnlySummary, "ExcludedSampleCount"), "artifact-only excluded sample count");
            AssertEqual(string.Empty, GetStringProperty(artifactOnlySummary, "SelectedSwapChainAddress"), "artifact-only selected swap chain");

            File.WriteAllText(csvPath, "   \r\n");
            var emptyHeaderSummary = parseCsv.Invoke(null, new object[] { csvPath })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for empty-header CSV.");
            AssertEqual(0, GetIntProperty(emptyHeaderSummary, "SampleCount"), "empty-header selected sample count");
            AssertEqual(0, GetIntProperty(emptyHeaderSummary, "RawSampleCount"), "empty-header raw sample count");
            AssertEqual(0, GetIntProperty(emptyHeaderSummary, "ExcludedSampleCount"), "empty-header excluded sample count");
            AssertEqual(false, GetBoolProperty(emptyHeaderSummary, "DisplayedTimeColumnPresent"), "empty-header displayed-time column presence");

            File.WriteAllText(
                csvPath,
                """
                Application,ProcessID,SwapChainAddress,PresentRuntime,SyncInterval,AllowsTearing,PresentMode,MsBetweenPresents,MsBetweenPresents,DisplayedTime,MsBetweenDisplayChange
                Sussudio.exe,1234,0xDAD,DXGI,0,0,Composed: Flip,7.0000,99.0000,7.0000,7.0000
                """);
            var duplicateHeaderSummary = parseCsv.Invoke(null, new object[] { csvPath })
                ?? throw new InvalidOperationException("PresentMonProbe.ParseCsv returned null for duplicate-header CSV.");
            var duplicateHeaderBetweenPresents = GetPropertyValue(duplicateHeaderSummary, "BetweenPresentsMs")
                ?? throw new InvalidOperationException("duplicate-header BetweenPresentsMs was null.");
            AssertEqual(1, GetIntProperty(duplicateHeaderSummary, "RawSampleCount"), "duplicate-header raw sample count");
            AssertEqual("0xDAD", GetStringProperty(duplicateHeaderSummary, "SelectedSwapChainAddress"), "duplicate-header selected swap chain");
            AssertNearlyEqual(7.0, GetDoubleProperty(duplicateHeaderBetweenPresents, "Average"), 0.0001, "duplicate header uses first metric occurrence");
        }
        finally
        {
            if (File.Exists(csvPath))
            {
                File.Delete(csvPath);
            }
        }

        return Task.CompletedTask;
    }

    internal static async Task SsctlPipeTransport_ExposesAdvancedAutomationCommandIds()
    {
        var assemblyPath = global::Program.SsctlAssemblyRelativePath;
        var ssctlAssembly = LoadToolAssemblyIsolated(assemblyPath);

        // Verify PipeTransport exposes expected command routing.
        var transportType = ssctlAssembly.GetType("Sussudio.Tools.Ssctl.PipeTransport")
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.PipeTransport type not found.");
        var sendCommandAsync = transportType.GetMethod(
                "SendCommandAsync",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: new[] { typeof(string), typeof(Dictionary<string, object?>), typeof(int?), typeof(CancellationToken) },
                modifiers: null)
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.PipeTransport.SendCommandAsync not found.");

        var pipeName = $"ssctl-pipe-transport-{Guid.NewGuid():N}";
        var transport = Activator.CreateInstance(transportType, pipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for transport test.");
        var request = await CapturePipeRequestAsync(
            pipeName,
            async () =>
            {
                var task = sendCommandAsync.Invoke(
                    transport,
                    new object?[]
                    {
                        "SetPreviewVolume",
                        new Dictionary<string, object?> { ["previewVolumePercent"] = 55.5 },
                        null,
                        CancellationToken.None
                    }) as Task
                    ?? throw new InvalidOperationException("PipeTransport.SendCommandAsync did not return a Task.");
                await task.ConfigureAwait(false);
            }).ConfigureAwait(false);

        AssertEqual(34, request.GetProperty("command").GetInt32(), "PipeTransport SetPreviewVolume command id");
        AssertEqual(55.5, request.GetProperty("payload").GetProperty("previewVolumePercent").GetDouble(), "PipeTransport preview volume payload");

        JsonElement response = default;
        var responsePipeName = $"ssctl-pipe-response-{Guid.NewGuid():N}";
        var responseTransport = Activator.CreateInstance(transportType, responsePipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for response test.");
        var responseRequests = await CapturePipeRequestsAsync(
                responsePipeName,
                expectedCount: 1,
                async () =>
                {
                    response = await InvokePipeTransportSendCommandAsync(
                            sendCommandAsync,
                            responseTransport,
                            "GetSnapshot",
                            null,
                            null)
                        .ConfigureAwait(false);
                },
                _ => """
                     {
                       "Success": true,
                       "Message": "snapshot ready",
                       "Data": {
                         "value": 123
                       }
                     }
                     """)
            .ConfigureAwait(false);
        AssertEqual(1, responseRequests[0].GetProperty("command").GetInt32(), "PipeTransport GetSnapshot command id");
        AssertEqual("snapshot ready", response.GetProperty("Message").GetString(), "PipeTransport parsed response message");
        AssertEqual(123, response.GetProperty("Data").GetProperty("value").GetInt32(), "PipeTransport parsed response data");

        JsonElement retryResponse = default;
        var retryPipeName = $"ssctl-pipe-retry-{Guid.NewGuid():N}";
        var retryTransport = Activator.CreateInstance(transportType, retryPipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for retry test.");
        var retryRequests = await CapturePipeRequestsAsync(
                retryPipeName,
                expectedCount: 2,
                async () =>
                {
                    retryResponse = await InvokePipeTransportSendCommandAsync(
                            sendCommandAsync,
                            retryTransport,
                            "GetSnapshot",
                            null,
                            null)
                        .ConfigureAwait(false);
                },
                i => i == 0
                    ? """
                      {
                        "Success": false,
                        "Status": "not_ready",
                        "RetryAfterMs": 100,
                        "Message": "snapshot not ready"
                      }
                      """
                    : """
                      {
                        "Success": true,
                        "Message": "snapshot ready after retry",
                        "Data": {
                          "attempt": 2
                        }
                      }
                      """)
            .ConfigureAwait(false);
        AssertEqual(1, retryRequests[0].GetProperty("command").GetInt32(), "PipeTransport retry first command id");
        AssertEqual(1, retryRequests[1].GetProperty("command").GetInt32(), "PipeTransport retry second command id");
        AssertEqual("snapshot ready after retry", retryResponse.GetProperty("Message").GetString(), "PipeTransport retry final message");
        AssertEqual(2, retryResponse.GetProperty("Data").GetProperty("attempt").GetInt32(), "PipeTransport retry final data");

        JsonElement invalidJsonResponse = default;
        var invalidPipeName = $"ssctl-pipe-invalid-{Guid.NewGuid():N}";
        var invalidTransport = Activator.CreateInstance(transportType, invalidPipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for invalid JSON test.");
        var invalidRequest = await CapturePipeRequestWithRawResponseAsync(
                invalidPipeName,
                async () =>
                {
                    invalidJsonResponse = await InvokePipeTransportSendCommandAsync(
                            sendCommandAsync,
                            invalidTransport,
                            "GetSnapshot",
                            null,
                            null)
                        .ConfigureAwait(false);
                },
                "not-json")
            .ConfigureAwait(false);
        AssertEqual(1, invalidRequest.GetProperty("command").GetInt32(), "PipeTransport invalid JSON request command id");
        AssertEqual(false, invalidJsonResponse.GetProperty("Success").GetBoolean(), "PipeTransport invalid JSON response Success=false");
        AssertEqual("pipe-invalid-json", invalidJsonResponse.GetProperty("ErrorCode").GetString(), "PipeTransport invalid JSON response ErrorCode");
        var invalidJsonMessage = invalidJsonResponse.GetProperty("Message").GetString() ?? "";
        AssertEqual(
            true,
            invalidJsonMessage.Contains("invalid JSON", StringComparison.OrdinalIgnoreCase) || invalidJsonMessage.Contains("pipe-invalid-json", StringComparison.OrdinalIgnoreCase),
            $"PipeTransport invalid JSON response Message should mention invalid JSON, got: {invalidJsonMessage}");

        var usageTransport = Activator.CreateInstance(transportType, $"ssctl-pipe-usage-{Guid.NewGuid():N}", (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for usage test.");
        Exception? usageException = null;
        try
        {
            await InvokePipeTransportSendCommandAsync(
                    sendCommandAsync,
                    usageTransport,
                    "DefinitelyNotACommand",
                    null,
                    null)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            usageException = ex;
        }

        AssertEqual("Sussudio.Tools.Ssctl.UsageException", usageException?.GetType().FullName, "PipeTransport unknown command exception type");
    }

    internal static Task KsAudioNodeProbe_SourceOwnership_IsConsolidated()
    {
        var programText = ReadRepoFile("tools/KsAudioNodeProbe/Program.cs");
        var scanWorkflowsText = ReadRepoFile("tools/KsAudioNodeProbe/Program.ScanWorkflows.cs");

        AssertContains(programText, "using static KsAudioNodeProbeNative;");
        AssertContains(programText, "KsAudioNodeProbeScanWorkflows.RunSetAndHold(handle)");
        AssertContains(programText, "KsAudioNodeProbeScanWorkflows.RunFullProbe(handle)");
        AssertContains(programText, "static class KsAudioNodeProbeNative");
        AssertContains(programText, "private const uint IoctlKsProperty = 0x002F0003;");
        AssertContains(programText, "private const int ErrorMoreData = 234;");
        AssertContains(programText, "public static List<string> EnumerateKsInterfaces");
        AssertContains(programText, "private static extern bool DeviceIoControl");
        AssertContains(programText, "private struct KsProperty");
        AssertContains(programText, "private struct SP_DEVICE_INTERFACE_DETAIL_DATA");
        AssertDoesNotContain(programText, "var anyHit = false");
        AssertDoesNotContain(programText, "== Extended node tests ==");
        AssertDoesNotContain(programText, "== ADC volume probe ==");
        AssertContains(scanWorkflowsText, "static class KsAudioNodeProbeScanWorkflows");
        AssertDoesNotContain(scanWorkflowsText, "static partial class KsAudioNodeProbeScanWorkflows");
        AssertContains(scanWorkflowsText, "public static int RunSetAndHold(SafeFileHandle handle)");
        AssertContains(scanWorkflowsText, "public static void RunFullProbe(SafeFileHandle handle)");
        AssertContains(scanWorkflowsText, "private static void EnumerateTopologyNodes(SafeFileHandle handle)");
        AssertContains(scanWorkflowsText, "private static void RunBruteForceNodePropertyScan(SafeFileHandle handle)");
        AssertContains(scanWorkflowsText, "private static void RunExtendedNodeTests(SafeFileHandle handle)");
        AssertContains(scanWorkflowsText, "private static void RunExtendedSetTest(");
        AssertContains(scanWorkflowsText, "private static void RunAdcVolumeProbe(SafeFileHandle handle)");
        AssertContains(scanWorkflowsText, "private static void RunMuxProbe(SafeFileHandle handle)");
        AssertContains(scanWorkflowsText, "private static void RunMuteProbe(SafeFileHandle handle)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "KsAudioNodeProbe", "Program.ScanWorkflows.Extended.cs")),
            "KS audio node scan workflow probes live with the main scan workflow owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "KsAudioNodeProbe", "Program.NativeInterop.cs")),
            "KS audio node probe private interop declarations live with the command entry point");

        return Task.CompletedTask;
    }

    internal static Task EgavdsAudioProbe_SourceOwnership_IsConsolidated()
    {
        var programText = ReadRepoFile("tools/EgavdsAudioProbe/Program.cs");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");

        AssertContains(programText, "static class EgavdsProbe");
        AssertDoesNotContain(programText, "static partial class EgavdsProbe");
        AssertContains(programText, "static string? FindElgato4KXDevicePath()");
        AssertContains(programText, "EGAVDS_SetAudioInputSelection(handleRef, targetInput)");
        AssertContains(programText, "EGAVDS_SetLineInAudioGain(handleRef, setGain.Value)");
        AssertContains(programText, "private const string DLL = \"EGAVDeviceSupport\"");
        AssertContains(programText, "private static void RegisterSwigCallbacks()");
        AssertContains(programText, "SWIGRegisterExceptionCallbacks_EGAVDS");
        AssertContains(programText, "private static extern int EGAVDS_OpenDevice");
        AssertContains(programText, "private static extern bool SetupDiEnumDeviceInterfaces");
        AssertContains(programText, "private struct SP_DEVICE_INTERFACE_DATA");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "EgavdsAudioProbe", "Program.NativeInterop.cs")),
            "EGAVDS probe private interop declarations live with the probe command flow");
        AssertContains(agentMapText, "`tools/EgavdsAudioProbe/Program.cs` owns EGAVDS audio probe command flow,");
        AssertDoesNotContain(agentMapText, "`Program.NativeInterop.cs` owns EGAVDS");
        AssertDoesNotContain(cleanupPlanText, "`tools/EgavdsAudioProbe/Program.NativeInterop.cs`");

        return Task.CompletedTask;
    }

    private static async Task<JsonElement> InvokePipeTransportSendCommandAsync(
        MethodInfo sendCommandAsync,
        object transport,
        string commandName,
        Dictionary<string, object?>? payload,
        int? responseTimeoutMs)
    {
        var task = sendCommandAsync.Invoke(
                transport,
                new object?[]
                {
                    commandName,
                    payload,
                    responseTimeoutMs,
                    CancellationToken.None
                }) as Task<JsonElement>
            ?? throw new InvalidOperationException("PipeTransport.SendCommandAsync did not return Task<JsonElement>.");
        return await task.ConfigureAwait(false);
    }

    private static async Task<JsonElement> CapturePipeRequestWithRawResponseAsync(
        string pipeName,
        Func<Task> clientAction,
        string rawResponseLine)
    {
        var clientTask = Task.Run(clientAction);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        string requestLine;
        {
            using var serverPipe = new System.IO.Pipes.NamedPipeServerStream(
                pipeName,
                System.IO.Pipes.PipeDirection.InOut,
                1,
                System.IO.Pipes.PipeTransmissionMode.Byte,
                System.IO.Pipes.PipeOptions.Asynchronous);

            var connectTask = serverPipe.WaitForConnectionAsync(cts.Token);
            if (await Task.WhenAny(connectTask, clientTask).ConfigureAwait(false) == clientTask)
            {
                await clientTask.ConfigureAwait(false);
                throw new InvalidOperationException("Expected raw-response pipe request, but the client completed before connecting.");
            }

            await connectTask.ConfigureAwait(false);
            using var reader = new StreamReader(serverPipe, leaveOpen: true);
            var readTask = reader.ReadLineAsync().WaitAsync(cts.Token);
            if (await Task.WhenAny(readTask, clientTask).ConfigureAwait(false) == clientTask)
            {
                await clientTask.ConfigureAwait(false);
                throw new InvalidOperationException("Expected raw-response pipe payload, but the client completed before sending one.");
            }

            try
            {
                requestLine = await readTask.ConfigureAwait(false)
                    ?? throw new InvalidOperationException("No request received on raw-response pipe.");
            }
            catch (OperationCanceledException ex)
            {
                throw new TimeoutException("Timed out waiting for raw-response pipe payload.", ex);
            }

            using var writer = new StreamWriter(serverPipe, leaveOpen: true) { AutoFlush = true };
            await writer.WriteLineAsync(rawResponseLine)
                .WaitAsync(cts.Token)
                .ConfigureAwait(false);
        }

        await EnsureNoUnexpectedPipeRequestAsync(pipeName, 1, 1, clientTask, cts.Token)
            .ConfigureAwait(false);

        using var document = JsonDocument.Parse(requestLine);
        return document.RootElement.Clone();
    }
}
namespace Sussudio.Tests
{
public sealed class AutomationToolContractsProtocolXunitTests
{
    [Fact]
    public void SendAutomationCommand_HelperTracksAutomationContractsInputs()
    {
        var scriptText = RuntimeContractSource.ReadRepoFile("tools/send-automation-command.ps1")
            .Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Contains("Get-AutomationClientInputWriteTimeUtc", scriptText);
        Assert.Contains("(Join-Path $PSScriptRoot \"AutomationClient\")", scriptText);
        Assert.Contains("(Join-Path $PSScriptRoot \"Common\")", scriptText);
        Assert.Contains("(Join-Path $repoRoot \"Sussudio.Automation.Contracts\")", scriptText);
        Assert.Contains("$_.Extension -in @(\".cs\", \".csproj\", \".props\", \".targets\")", scriptText);
        Assert.Contains("$_.FullName -notmatch \"\\\\(bin|obj)\\\\\"", scriptText);
        Assert.DoesNotContain("Sussudio\\Models\\AutomationCommandKind.cs", scriptText);
        Assert.DoesNotContain("Models\\AutomationCommandKind.cs", scriptText);
    }

    [Fact]
    public void ContractTests_LoadActiveFreshBuildArtifacts()
    {
        var harnessText = RuntimeContractSource.ReadRepoFile("tests/Sussudio.Tests/HarnessCore.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var toolContractsText = RuntimeContractSource.ReadRepoFile("tests/Sussudio.Tests/XUnit.ToolContractsTests.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var recordingContractsText = RuntimeContractSource.ReadRepoFile("tests/Sussudio.Tests/XUnit.RecordingContractsTests.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var coreRuntimeContractsText = RuntimeContractSource.ReadRepoFile("tests/Sussudio.Tests/XUnit.CoreRuntimeContractsTests.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var architectureGuardrailsText = RuntimeContractSource.ReadRepoFile("tests/Sussudio.Tests/ArchitectureGuardrails.Tests.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Contains("SUSSUDIO_TEST_CONFIGURATION", harnessText);
        Assert.Contains("internal static string ActiveTestConfiguration", harnessText);
        Assert.Contains("InferConfigurationFromOutputPath(AppContext.BaseDirectory)", harnessText);
        Assert.Contains("internal static string SussudioAppAssemblyRelativePath", harnessText);
        Assert.Contains("internal static string SsctlAssemblyRelativePath", harnessText);
        Assert.Contains("internal static string McpServerAssemblyRelativePath", harnessText);
        Assert.Contains("internal static string NativeXuAudioProbeAssemblyRelativePath", harnessText);
        Assert.Contains("RequireFreshSussudioAssembly(assemblyPath);", harnessText);
        Assert.Contains("internal static void RequireFreshToolAssembly(string relativeAssemblyPath, string fullPath)", harnessText);
        Assert.Contains("GetNewestSussudioInputWriteTimeUtc()", harnessText);
        Assert.Contains("GetNewestToolInputWriteTimeUtc(relativeAssemblyPath)", harnessText);
        Assert.Contains("Directory.EnumerateFiles(root, \"*.props\")", harnessText);
        Assert.Contains("Directory.EnumerateFiles(root, \"*.targets\")", harnessText);
        Assert.Equal(2, CountOccurrences(harnessText, ".Concat(new[] { root })"));
        Assert.Contains("var contractsInputDirectories = EnumerateToolInputDirectories(Path.Combine(root, \"Sussudio.Automation.Contracts\"));", harnessText);
        Assert.Contains("var linkedCompileInputs = EnumerateToolProjectCompileIncludes(projectDirectory).ToArray();", harnessText);
        Assert.Contains(".Concat(contractsInputDirectories)", harnessText);
        Assert.Contains(".Concat(EnumerateExistingCompileIncludeDirectories(linkedCompileInputs))", harnessText);
        Assert.Contains(".Concat(linkedCompileInputs)", harnessText);
        Assert.Contains("private static IEnumerable<string> EnumerateExistingCompileIncludeDirectories(IEnumerable<string> compileIncludes)", harnessText);
        Assert.Contains("Path.Combine(root, \"Sussudio.Automation.Contracts\")", harnessText);
        Assert.Contains("-c {ActiveTestConfiguration}", harnessText);

        Assert.Contains("global::Program.SussudioAppAssemblyRelativePath", recordingContractsText);
        Assert.Contains("global::Program.RequireFreshSussudioAssembly(path);", recordingContractsText);
        Assert.Contains("global::Program.RequireFreshToolAssembly(relativeAssemblyPath, fullPath);", toolContractsText);
        Assert.Contains("global::Program.SsctlAssemblyRelativePath", toolContractsText);
        Assert.Contains("global::Program.McpServerAssemblyRelativePath", toolContractsText);
        Assert.Contains("global::Program.NativeXuAudioProbeAssemblyRelativePath", architectureGuardrailsText);
        Assert.Contains("LoadToolAssembly(global::Program.SsctlAssemblyRelativePath)", coreRuntimeContractsText);

        foreach (var relativePath in Directory.GetFiles(
                     Path.Combine(RuntimeContractSource.GetRepoRoot(), "tests", "Sussudio.Tests"),
                     "*.cs"))
        {
            var text = File.ReadAllText(relativePath).Replace("\r\n", "\n", StringComparison.Ordinal);
            var directToolDebugArtifactPattern =
                "Path\\.Combine\\(\\s*\"tools\"[\\s\\S]*?\"bin\"\\s*,\\s*" + "\"Debug\"";
            Assert.False(
                System.Text.RegularExpressions.Regex.IsMatch(text, directToolDebugArtifactPattern),
                $"{relativePath} must not load tool assemblies from a hard-coded Debug artifact path.");
            Assert.DoesNotContain("Path.Combine(\"tools\", \"ssctl\", \"bin\", " + "\"Debug\"", text);
            Assert.DoesNotContain("Path.Combine(\"tools\", \"McpServer\", \"bin\", " + "\"Debug\"", text);
            Assert.DoesNotContain("Path.Combine(\"tools\", \"NativeXuAudioProbe\", \"bin\", " + "\"Debug\"", text);
            Assert.DoesNotContain("Path.Combine(\"tools\", \"AutomationClient\", \"bin\", " + "\"Debug\"", text);
            Assert.DoesNotContain("Sussudio/bin/x64/" + "Debug", text);
            Assert.DoesNotContain("Sussudio\\bin\\x64\\" + "Debug", text);
        }
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    [Fact]
    public void CliCancellationTokens_FlowIntoAutomationPipeTransport()
    {
        var ssctlProgramText = RuntimeContractSource.ReadRepoFile("tools/ssctl/Program.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var ssctlCommandHandlersText = RuntimeContractSource.ReadRepoFile("tools/ssctl/CommandHandlers.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var automationClientText = RuntimeContractSource.ReadRepoFile("tools/AutomationClient/Program.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var sharedClientText = RuntimeContractSource.ReadAutomationPipeClientSource();

        Assert.Contains("CommandHandlers.ExecuteAsync(\n                transport,\n                options.Arguments,\n                options.Json,\n                cts.Token)", ssctlProgramText);
        Assert.Contains("CancellationToken cancellationToken = default", ssctlCommandHandlersText);
        Assert.Contains("RequestCancellationToken = cancellationToken;", ssctlCommandHandlersText);
        Assert.Contains("=> Transport.SendCommandAsync(commandName, payload, responseTimeoutMs, RequestCancellationToken);", ssctlCommandHandlersText);
        Assert.Contains("=> Transport.SendCommandAsync(kind, payload, responseTimeoutMs, RequestCancellationToken);", ssctlCommandHandlersText);
        Assert.Contains("cancellationToken: cancellationToken", ssctlCommandHandlersText);
        Assert.DoesNotContain("context.Transport.SendCommandAsync", ssctlCommandHandlersText);
        Assert.Contains("options.AuthToken,\n                    cancellationToken: cts.Token)", automationClientText);
        Assert.Contains("CancellationToken cancellationToken = default", sharedClientText);
        Assert.Contains("cancellationToken: cancellationToken", sharedClientText);
    }

    [Fact]
    public void AutomationClient_UsesCatalogTimeoutPolicy_ForRecordingAndFlashbackCommands()
    {
        var protocolText = RuntimeContractSource.ReadRepoFile("Sussudio.Automation.Contracts/AutomationPipeProtocol.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var catalogEntriesText = RuntimeContractSource.ReadRepoFile("Sussudio.Automation.Contracts/AutomationCommandCatalog.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var clientText = RuntimeContractSource.ReadRepoFile("tools/AutomationClient/Program.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var pipeClientText = RuntimeContractSource.ReadAutomationPipeClientSource();

        Assert.Contains("public const int DefaultResponseTimeoutMs = 15000;", protocolText);
        Assert.Contains("public const int ExtendedResponseTimeoutMs = 60000;", protocolText);
        Assert.Contains("public const int RecordingResponseTimeoutMs = 150000;", protocolText);
        Assert.Contains("public const int FlashbackMutationResponseTimeoutMs = 305000;", protocolText);
        Assert.Contains("commandName = ResolveCanonicalCommandName(commandName);", protocolText);
        Assert.Contains("AutomationCommandCatalog.TryGet(commandName, out var metadata)", protocolText);
        Assert.Contains("? metadata.ResponseTimeoutMs", protocolText);
        Assert.Contains("AutomationCommandKind.SetRecordingEnabled", catalogEntriesText);
        Assert.Contains("AutomationPipeProtocol.RecordingResponseTimeoutMs", catalogEntriesText);
        Assert.Contains("AutomationCommandKind.FlashbackExport", catalogEntriesText);
        Assert.Contains("AutomationPipeProtocol.FlashbackMutationResponseTimeoutMs", catalogEntriesText);
        Assert.DoesNotContain("AlignResponseTimeoutWithServerRequest", protocolText);
        Assert.DoesNotContain("AlignResponseTimeoutWithServerRequest", pipeClientText);
        Assert.Contains("AutomationPipeProtocol.TryGetCommandName(commandValue, out var canonicalCommandName)", clientText);
        Assert.Contains("AutomationPipeProtocol.GetDefaultResponseTimeout(timeoutCommandName)", clientText);
        Assert.Contains("public int? ResponseTimeoutMs { get; set; }", clientText);

        foreach (var acceptedName in new[] { "SetRecordingEnabled", "setrecordingenabled", "set-recording-enabled", "17" })
        {
            Assert.Equal(150000, AutomationPipeProtocol.GetDefaultResponseTimeout(acceptedName));
        }

        Assert.Equal(15000, AutomationPipeProtocol.GetDefaultResponseTimeout("GetSnapshot"));
        Assert.Equal(305000, AutomationPipeProtocol.GetDefaultResponseTimeout("FlashbackExport"));

        foreach (var acceptedName in new[] { "SetFlashbackEnabled", "set-flashback-enabled", "RestartFlashback" })
        {
            Assert.Equal(305000, AutomationPipeProtocol.GetDefaultResponseTimeout(acceptedName));
        }
    }

    [Fact]
    public void AutomationClient_StaysAlignedWithAdvancedMcpCommandMap()
    {
        var protocolText = RuntimeContractSource.ReadRepoFile("Sussudio.Automation.Contracts/AutomationPipeProtocol.cs");
        var scriptText = RuntimeContractSource.ReadRepoFile("tools/send-automation-command.ps1");

        foreach (var (kind, ordinal) in new[]
        {
            (AutomationCommandKind.GetCaptureOptions, 29),
            (AutomationCommandKind.SetPreset, 30),
            (AutomationCommandKind.SetSplitEncodeMode, 31),
            (AutomationCommandKind.SetMjpegDecoderCount, 32),
            (AutomationCommandKind.SetShowAllCaptureOptions, 33),
            (AutomationCommandKind.SetPreviewVolume, 34),
            (AutomationCommandKind.SetStatsVisible, 35)
        })
        {
            Assert.Equal(ordinal, (int)kind);
            Assert.Equal(ordinal, AutomationPipeProtocol.ResolveCommand(kind.ToString()));
        }

        Assert.Contains("Enum.GetValues<AutomationCommandKind>()", protocolText);

        Assert.Contains("AutomationClient\\AutomationClient.csproj", scriptText);
        Assert.Contains("Get-AutomationClientInputWriteTimeUtc", scriptText);
        Assert.Contains("Test-AutomationClientBuildFresh", scriptText);
        Assert.Contains("AutomationClient build failed with exit code $LASTEXITCODE.", scriptText);
        Assert.Contains("AutomationClient build output is stale after rebuild", scriptText);
        Assert.Contains("$_.FullName -notmatch \"\\\\(bin|obj)\\\\\"", scriptText);
        Assert.Contains("\"--command\", $Command", scriptText);
        Assert.Contains("$payloadBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($PayloadJson))", scriptText);
        Assert.Contains("\"--payload-base64\", $payloadBase64", scriptText);
        Assert.Contains("[int]$ResponseTimeoutMs = 0", scriptText);
        Assert.Contains("\"--response-timeout-ms\", $ResponseTimeoutMs", scriptText);
        Assert.DoesNotContain("function Resolve-AutomationCommand", scriptText);
    }

    [Fact]
    public void PipeClient_UsesSharedProtocol_ForCommandResolution()
    {
        var pipeClientText = RuntimeContractSource.ReadRepoFile("tools/McpServer/Program.cs");

        Assert.False(
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "tools", "McpServer", "PipeClient.cs")),
            "MCP PipeClient should stay with the host bootstrap owner instead of returning as a tiny adapter file.");
        Assert.Contains("AutomationPipeProtocol", pipeClientText);
        Assert.DoesNotContain("CommandMap = new", pipeClientText);
    }

    [Fact]
    public void UiAutomationAdapters_UseEnumCommands_WithoutChangingLabelsOrWireNames()
    {
        Assert.False(
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "tools", "ssctl", "PipeTransport.cs")),
            "ssctl PipeTransport should stay with the command-handler surface instead of returning as a tiny adapter file.");
        var ssctlPipeText = RuntimeContractSource.ReadRepoFile("tools/ssctl/CommandHandlers.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var ssctlTransportText = RuntimeContractSource.ReadRepoFile("tools/ssctl/CommandHandlers.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var ssctlUiText = ssctlTransportText;
        var ssctlFlashbackText = ssctlTransportText;
        var mcpPipeText = RuntimeContractSource.ReadRepoFile("tools/McpServer/Program.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var formatterText = RuntimeContractSource.ReadRepoFile("tools/McpServer/Tools/ToolCommandFormatter.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var uiSettingsToolsText = RuntimeContractSource.ReadRepoFile("tools/McpServer/Tools/AutomationControlTools.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Contains("SendCommandAsync(\n        AutomationCommandKind kind,", ssctlPipeText);
        Assert.Contains("AutomationCommandTransport.SendCommandAsync(\n            _pipeName,\n            kind,", ssctlPipeText);
        Assert.DoesNotContain("AutomationCommandCatalog.Get(kind).Name", ssctlPipeText);
        Assert.Contains("HandleSimpleCommandAsync(\n        CommandContext context,\n        AutomationCommandKind kind,", ssctlTransportText);
        Assert.Contains("SendCommandAsync(\n            AutomationCommandKind kind,", mcpPipeText);
        Assert.Contains("AutomationCommandTransport.SendCommandAsync(\n                _pipeName,\n                kind,", mcpPipeText);
        Assert.DoesNotContain("AutomationCommandCatalog.Get(kind).Name", mcpPipeText);
        Assert.Contains("Optional(AutomationCommandKind kind, string label,", formatterText);
        Assert.Contains("ExecuteAndFormatResultAsync(\n        PipeClient pipeClient,\n        AutomationCommandKind kind,", formatterText);
        Assert.Contains("pipeClient.SendCommandAsync(kind, payload, responseTimeoutMs)", formatterText);

        Assert.Contains("AutomationCommandKind.SetStatsVisible", ssctlUiText);
        Assert.Contains("AutomationCommandKind.SetStatsSectionVisible", ssctlUiText);
        Assert.Contains("AutomationCommandKind.SetSettingsVisible", ssctlUiText);
        Assert.Contains("AutomationCommandKind.SetFrameTimeOverlayVisible", ssctlUiText);
        Assert.Contains("AutomationCommandKind.SetFlashbackTimelineVisible", ssctlFlashbackText);
        Assert.DoesNotContain("\"SetStatsVisible\"", ssctlUiText);
        Assert.DoesNotContain("\"SetStatsSectionVisible\"", ssctlUiText);
        Assert.DoesNotContain("\"SetSettingsVisible\"", ssctlUiText);
        Assert.DoesNotContain("\"SetFrameTimeOverlayVisible\"", ssctlUiText);
        Assert.DoesNotContain("\"SetFlashbackTimelineVisible\"", ssctlFlashbackText);

        Assert.Contains("ToolCommandFormatter.Optional(AutomationCommandKind.SetStatsVisible, \"SetStatsVisible\"", uiSettingsToolsText);
        Assert.Contains("ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.SetSettingsVisible, \"SetSettingsVisible\"", uiSettingsToolsText);
        Assert.Contains("ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.SetFrameTimeOverlayVisible, \"SetFrameTimeOverlayVisible\"", uiSettingsToolsText);
        Assert.Contains("ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.SetFlashbackTimelineVisible, \"SetFlashbackTimelineVisible\"", uiSettingsToolsText);
        Assert.Contains("ExecuteAndFormatResultAsync(pipeClient, AutomationCommandKind.SetStatsSectionVisible, \"SetStatsSectionVisible\"", uiSettingsToolsText);
    }

    [Fact]
    public void AutomationClient_UsesSharedProtocol_ForCommandResolution()
    {
        var entryText = RuntimeContractSource.ReadRepoFile("tools/AutomationClient/Program.cs");
        var clientText = entryText;

        Assert.Contains("AutomationPipeProtocol", clientText);
        Assert.Contains("var options = ParseArgs(args);", entryText);
        Assert.Contains("var payload = BuildPayload(options);", entryText);
        Assert.Contains("public int? ResponseTimeoutMs { get; set; }", entryText);
        Assert.Contains("private static Options ParseArgs(string[] args)", entryText);
        Assert.Contains("private static void WriteHelp()", entryText);
        Assert.Contains("--payload-base64", entryText);
        Assert.Contains("private static object BuildPayload(Options options)", entryText);
        Assert.Contains("Convert.FromBase64String(options.PayloadBase64)", entryText);
        Assert.False(
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "tools", "AutomationClient", "Program.Arguments.cs")),
            "AutomationClient argument parsing should stay with the low-level client entrypoint.");
        Assert.False(
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "tools", "AutomationClient", "Program.Payload.cs")),
            "AutomationClient payload construction should stay with the low-level client entrypoint.");
        Assert.DoesNotContain("CommandMap = new", clientText);
    }

    [Fact]
    public void AutomationPipeConnectFailures_AreClassifiedForCliAndMcp()
    {
        var sharedClientText = RuntimeContractSource.ReadAutomationPipeClientSource();
        var pipeClientText = sharedClientText;
        Assert.False(
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "tools", "ssctl", "PipeTransport.cs")),
            "ssctl PipeTransport should stay with the command-handler surface instead of returning as a tiny adapter file.");
        Assert.False(
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "tools", "Common", "AutomationPipeClient", "AutomationPipeClient.cs")),
            "AutomationPipeClient transport is folded into Sussudio.Automation.Contracts/AutomationPipeProtocol.cs");
        var ssctlPipeText = RuntimeContractSource.ReadRepoFile("tools/ssctl/CommandHandlers.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var mcpPipeText = RuntimeContractSource.ReadRepoFile("tools/McpServer/Program.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var diagnosticSessionText = RuntimeContractSource.ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var diagnosticSessionCommandChannelText = RuntimeContractSource.ReadRepoFile("tools/Common/DiagnosticSessionRunContext.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var diagnosticSessionPipeRetryText = diagnosticSessionCommandChannelText;
        var automationPipeProtocolText = RuntimeContractSource.ReadRepoFile("Sussudio.Automation.Contracts/AutomationPipeProtocol.cs")
            .Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Contains("internal static class AutomationPipeClient", sharedClientText);
        Assert.DoesNotContain("internal static partial class AutomationPipeClient", sharedClientText);
        Assert.Contains("internal static async Task<string> SendRequestAsync(", sharedClientText);
        Assert.Contains("internal static async Task<AutomationPipeCommandResult> SendCommandWithResultAsync(", sharedClientText);
        Assert.Contains("AutomationCommandKind kind", sharedClientText);
        Assert.Contains("=> SendCommandWithResultAsync(\n            pipeName,\n            (int)kind,", sharedClientText);
        Assert.Contains("internal static bool TryReadResponseState(", sharedClientText);
        Assert.Contains("AutomationResponseState.TryRead(", sharedClientText);
        Assert.DoesNotContain("internal static class AutomationResponseState", sharedClientText);
        Assert.Contains("public static class AutomationResponseState", automationPipeProtocolText);
        Assert.Contains("public static bool TryRead(", automationPipeProtocolText);
        Assert.DoesNotContain("internal readonly record struct AutomationPipeCommandResult(", sharedClientText);
        Assert.Contains("public readonly record struct AutomationPipeCommandResult(", automationPipeProtocolText);
        Assert.Contains("public class AutomationPipeException : Exception", automationPipeProtocolText);
        Assert.Contains("public sealed class AutomationPipeConnectException : AutomationPipeException", automationPipeProtocolText);
        Assert.Contains("ConnectWithClassifiedErrorsAsync(", pipeClientText);
        Assert.Contains("await writer.WriteLineAsync(requestJson)", pipeClientText);
        Assert.Contains("private static async Task ConnectWithClassifiedErrorsAsync(", pipeClientText);
        Assert.Contains("await client.ConnectAsync(connectTimeoutMs, cancellationToken).ConfigureAwait(false);", pipeClientText);
        Assert.Contains("catch (TimeoutException ex)", pipeClientText);
        Assert.Contains("\"pipe-connect-timeout\"", pipeClientText);
        Assert.Contains("catch (OperationCanceledException)\n        {\n            throw;\n        }", pipeClientText);
        Assert.Contains("catch (UnauthorizedAccessException ex)", pipeClientText);
        Assert.Contains("\"pipe-access-denied\"", pipeClientText);
        Assert.Contains("AutomationPipeProtocol.AutomationKeyEnvVar", pipeClientText);
        Assert.Contains("catch (Exception ex)", pipeClientText);
        Assert.Contains("\"pipe-connect-failed\"", pipeClientText);
        Assert.Contains("public string ErrorCode { get; }", automationPipeProtocolText);

        Assert.Contains("AutomationCommandTransport.SendCommandAsync(", ssctlPipeText);
        Assert.Contains("kind,", ssctlPipeText);
        Assert.Contains("unknownCommandHandling: AutomationUnknownCommandHandling.ThrowArgumentException", ssctlPipeText);
        Assert.Contains("throw new UsageException(ex.Message);", ssctlPipeText);
        Assert.DoesNotContain("AutomationPipeClient.SendCommandWithResultAsync", ssctlPipeText);
        Assert.DoesNotContain("catch (AutomationPipeConnectException ex)", ssctlPipeText);
        Assert.DoesNotContain("AutomationSyntheticErrorResponse.Create(ex.Message, ex.ErrorCode)", ssctlPipeText);
        Assert.DoesNotContain("private static JsonElement CreateSyntheticError", ssctlPipeText);
        Assert.DoesNotContain("Sussudio is not running or not responding. Start the app and try again.", ssctlPipeText);

        Assert.Contains("AutomationCommandTransport.SendCommandAsync(", mcpPipeText);
        Assert.Contains("kind,", mcpPipeText);
        Assert.Contains("unknownCommandHandling: AutomationUnknownCommandHandling.ReturnSyntheticError", mcpPipeText);
        Assert.DoesNotContain("AutomationPipeClient.SendCommandWithResultAsync", mcpPipeText);
        Assert.DoesNotContain("catch (AutomationPipeConnectException ex)", mcpPipeText);
        Assert.DoesNotContain("AutomationSyntheticErrorResponse.Create(ex.Message, ex.ErrorCode)", mcpPipeText);
        Assert.DoesNotContain("private static JsonElement CreateSyntheticError", mcpPipeText);
        Assert.DoesNotContain("Sussudio is not running or not responding. Start the app and try again.", mcpPipeText);
        Assert.Contains("internal static class AutomationCommandTransport", sharedClientText);
        Assert.DoesNotContain("internal enum AutomationUnknownCommandHandling", sharedClientText);
        Assert.Contains("public enum AutomationUnknownCommandHandling", automationPipeProtocolText);
        Assert.Contains("ReturnSyntheticError", automationPipeProtocolText);
        Assert.Contains("ThrowArgumentException", automationPipeProtocolText);
        Assert.Contains("AutomationPipeProtocol.GetDefaultResponseTimeout(kind)", sharedClientText);
        Assert.Contains("AutomationSyntheticErrorResponse.Create(ex.Message, \"unknown-command\")", sharedClientText);
        Assert.Contains("catch (Exception ex) when (AutomationSyntheticErrorResponse.CanCreateFromException(ex))", sharedClientText);
        Assert.Contains("AutomationSyntheticErrorResponse.Create(ex)", sharedClientText);
        Assert.DoesNotContain("internal static class AutomationSyntheticErrorResponse", sharedClientText);
        Assert.Contains("public static class AutomationSyntheticErrorResponse", automationPipeProtocolText);
        Assert.Contains("[\"CommandLifecycle\"] = \"failed\"", automationPipeProtocolText);
        Assert.Contains("[\"Snapshot\"] = null", automationPipeProtocolText);
        Assert.Contains("public static bool CanCreateFromException(Exception exception)", automationPipeProtocolText);
        Assert.Contains("public static JsonElement Create(Exception exception)", automationPipeProtocolText);
        Assert.Contains("AutomationPipeConnectException ex => Create(ex.Message, ex.ErrorCode)", automationPipeProtocolText);
        Assert.Contains("AutomationPipeResponseTimeoutException ex => Create(ex.Message, \"pipe-response-timeout\")", automationPipeProtocolText);
        Assert.Contains("AutomationPipeProtocolException ex => Create(ex.Message, \"pipe-protocol-error\")", automationPipeProtocolText);
        Assert.Contains("\"pipe-invalid-json\"", automationPipeProtocolText);
        Assert.Contains("\"pipe-io-error\"", automationPipeProtocolText);
        Assert.Contains("\"pipe-canceled\"", automationPipeProtocolText);

        Assert.Contains("using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;", diagnosticSessionCommandChannelText);
        Assert.Contains("SendCommandWithConnectRetryAsync(", diagnosticSessionCommandChannelText);
        Assert.DoesNotContain("using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;", diagnosticSessionText);
        Assert.False(
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "tools", "Common", "DiagnosticSessionCommandChannel.cs")),
            "diagnostic-session command channel should stay with the run-context infrastructure owner.");
        Assert.Contains("internal static class DiagnosticSessionPipeRetryPolicy", diagnosticSessionPipeRetryText);
        Assert.Contains("internal static async Task<JsonElement?> SendCommandWithConnectRetryAsync(", diagnosticSessionPipeRetryText);
        Assert.Contains("\"pipe-connect-failed\"", diagnosticSessionPipeRetryText);
        Assert.Contains("\"pipe-connect-timeout\"", diagnosticSessionPipeRetryText);
        Assert.Contains("IsPermanentPipeConnectFailure(ex.ErrorCode)", diagnosticSessionPipeRetryText);
        Assert.Contains("\"pipe-access-denied\"", diagnosticSessionPipeRetryText);
        Assert.False(
            File.Exists(Path.Combine(RuntimeContractSource.GetRepoRoot(), "tools", "Common", "DiagnosticSessionPipeRetryPolicy.cs")),
            "diagnostic-session pipe retry policy should stay with the command channel transport owner.");
        Assert.DoesNotContain("private static async Task<JsonElement?> SendCommandWithConnectRetryAsync(", diagnosticSessionText);
    }

    [Fact]
    public void AutomationSyntheticErrorResponse_CreatesStableErrorEnvelope()
    {
        var response = AutomationSyntheticErrorResponse.Create("boom", "pipe-boom");

        Assert.False(response.GetProperty("Success").GetBoolean());
        Assert.Equal("error", response.GetProperty("Status").GetString());
        Assert.Equal("failed", response.GetProperty("CommandLifecycle").GetString());
        Assert.Equal("boom", response.GetProperty("Message").GetString());
        Assert.Equal("pipe-boom", response.GetProperty("ErrorCode").GetString());
        Assert.Equal(JsonValueKind.Null, response.GetProperty("RetryAfterMs").ValueKind);
        Assert.Equal(JsonValueKind.Null, response.GetProperty("ElapsedMs").ValueKind);
        Assert.Equal(JsonValueKind.Null, response.GetProperty("Data").ValueKind);
        Assert.Equal(JsonValueKind.Null, response.GetProperty("Snapshot").ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(response.GetProperty("CorrelationId").GetString()));
        Assert.Equal(JsonValueKind.String, response.GetProperty("TimestampUtc").ValueKind);
    }

    [Fact]
    public void AutomationResponseState_ParsesStatusAndRetryContracts()
    {
        var responseStateType = RequireSharedToolType("Sussudio.Tools.AutomationResponseState");
        var tryRead = RequireNonPublicStaticMethod(responseStateType, "TryRead");

        AssertResponseState(
            tryRead,
            "{\"Success\":true,\"Status\":\"ready\",\"RetryAfterMs\":250}",
            expectedRead: true,
            expectedSuccess: true,
            expectedStatus: "ready",
            expectedRetryAfterMs: 250,
            "numeric retry");
        AssertResponseState(
            tryRead,
            "{\"Success\":false,\"RetryAfterMs\":\"500\"}",
            expectedRead: true,
            expectedSuccess: false,
            expectedStatus: null,
            expectedRetryAfterMs: 500,
            "string retry");
        AssertResponseState(
            tryRead,
            "{\"Success\":\"true\",\"Status\":42,\"RetryAfterMs\":\"soon\"}",
            expectedRead: true,
            expectedSuccess: false,
            expectedStatus: null,
            expectedRetryAfterMs: null,
            "malformed values");
        AssertResponseState(
            tryRead,
            "[]",
            expectedRead: false,
            expectedSuccess: false,
            expectedStatus: null,
            expectedRetryAfterMs: null,
            "non-object response");
    }

    private static string ReadDiagnosticSessionRunnerSource()
        => string.Join(
            "\n",
            Directory.GetFiles(Path.Combine(FindRepoRoot(), "tools", "Common"), "DiagnosticSessionRunner*.cs")
                .Concat(Directory.GetFiles(Path.Combine(FindRepoRoot(), "tools", "Common"), "DiagnosticSessionRun*.cs"))
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(path => File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal)));

    private static Type RequireSharedToolType(string typeName)
    {
        var assembly = ToolFormatterTestAssembly.Load(global::Program.SsctlAssemblyRelativePath);
        var type = assembly.GetType(typeName);
        if (type != null)
        {
            return type;
        }

        var assemblyDirectory = Path.GetDirectoryName(assembly.Location)
                                ?? throw new InvalidOperationException("Shared tool assembly directory was not found.");
        foreach (var reference in assembly.GetReferencedAssemblies())
        {
            var referencePath = Path.Combine(assemblyDirectory, $"{reference.Name}.dll");
            if (!File.Exists(referencePath))
            {
                continue;
            }

            var referenceAssembly = Assembly.LoadFrom(referencePath);
            type = referenceAssembly.GetType(typeName);
            if (type != null)
            {
                return type;
            }
        }

        throw new InvalidOperationException($"{typeName} was not found in the shared tool assembly or its references.");
    }

    private static MethodInfo RequireNonPublicStaticMethod(Type type, string name)
        => type.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
           ?? throw new InvalidOperationException($"{type.FullName}.{name} was not found.");

    private static void AssertResponseState(
        MethodInfo tryRead,
        string json,
        bool expectedRead,
        bool expectedSuccess,
        string? expectedStatus,
        int? expectedRetryAfterMs,
        string fieldName)
    {
        using var document = JsonDocument.Parse(json);
        var args = new object?[] { document.RootElement, null, null, null };
        Assert.Equal(expectedRead, (bool)tryRead.Invoke(null, args)!);
        Assert.Equal(expectedSuccess, (bool)args[1]!);
        Assert.Equal(expectedStatus, (string?)args[2]);
        var actualRetryAfterMs = args[3] is null ? (int?)null : Convert.ToInt32(args[3]);
        Assert.Equal(expectedRetryAfterMs, actualRetryAfterMs);
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
}
}
