using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
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
}
