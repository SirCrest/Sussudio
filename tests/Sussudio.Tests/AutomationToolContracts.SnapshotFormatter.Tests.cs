using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
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
}
