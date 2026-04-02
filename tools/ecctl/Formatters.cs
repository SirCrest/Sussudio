using System.Globalization;
using System.Text;
using System.Text.Json;
using ElgatoCapture.Tools;

namespace EcCtl;

internal static class Formatters
{
    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true
    };

    public static string FormatSnapshot(JsonElement snapshotResponse)
    {
        if (snapshotResponse.ValueKind != JsonValueKind.Object)
        {
            return "Snapshot response was not a JSON object.";
        }

        if (!snapshotResponse.TryGetProperty("Snapshot", out var snapshot) ||
            snapshot.ValueKind != JsonValueKind.Object)
        {
            return AutomationSnapshotFormatter.Get(snapshotResponse, "Message", "Snapshot data not available.");
        }

        var selectedFriendlyFrameRate = AutomationSnapshotFormatter.Get(snapshot, "SelectedFriendlyFrameRate", string.Empty);
        var selectedExactFrameRate = AutomationSnapshotFormatter.Get(snapshot, "SelectedExactFrameRate", string.Empty);
        var selectedExactFrameRateArg = AutomationSnapshotFormatter.Get(snapshot, "SelectedExactFrameRateArg", string.Empty);
        var frameRateBucket = string.IsNullOrWhiteSpace(selectedFriendlyFrameRate)
            ? AutomationSnapshotFormatter.Get(snapshot, "SelectedFrameRate")
            : selectedFriendlyFrameRate;
        var frameRateExactDetail = !string.IsNullOrWhiteSpace(selectedExactFrameRateArg)
            ? $"{selectedExactFrameRate} fps, {selectedExactFrameRateArg}"
            : !string.IsNullOrWhiteSpace(selectedExactFrameRate)
                ? $"{selectedExactFrameRate} fps"
                : string.Empty;
        var frameRateSummary = string.IsNullOrWhiteSpace(frameRateExactDetail)
            ? $"{frameRateBucket} fps"
            : $"{frameRateBucket} fps ({frameRateExactDetail})";

        var builder = new StringBuilder();
        builder.AppendLine("== ElgatoCapture State ==");
        builder.AppendLine($"Status: {AutomationSnapshotFormatter.Get(snapshot, "SessionState")} | {AutomationSnapshotFormatter.Get(snapshot, "StatusText")}");
        builder.AppendLine($"Device: {AutomationSnapshotFormatter.Get(snapshot, "SelectedDeviceName")} ({AutomationSnapshotFormatter.Get(snapshot, "SelectedDeviceId")})");
        builder.AppendLine($"Initialized: {AutomationSnapshotFormatter.Get(snapshot, "IsInitialized")} | Previewing: {AutomationSnapshotFormatter.Get(snapshot, "IsPreviewing")} | Recording: {AutomationSnapshotFormatter.Get(snapshot, "IsRecording")}");
        builder.AppendLine();
        builder.AppendLine("== Capture Settings ==");
        builder.AppendLine($"Resolution: {AutomationSnapshotFormatter.Get(snapshot, "SelectedResolution")} | Frame Rate: {frameRateSummary}");
        builder.AppendLine($"Format: {AutomationSnapshotFormatter.Get(snapshot, "SelectedRecordingFormat")} | Quality: {AutomationSnapshotFormatter.Get(snapshot, "SelectedQuality")} | Preset: {AutomationSnapshotFormatter.Get(snapshot, "SelectedPreset")}");
        builder.AppendLine($"Video Format: {AutomationSnapshotFormatter.Get(snapshot, "SelectedVideoFormat")} | Split Encode: {AutomationSnapshotFormatter.Get(snapshot, "SelectedSplitEncodeMode")} | MJPEG Decoders: {AutomationSnapshotFormatter.Get(snapshot, "MjpegDecoderCount")}");
        builder.AppendLine($"HDR: {AutomationSnapshotFormatter.Get(snapshot, "IsHdrEnabled")} (Available: {AutomationSnapshotFormatter.Get(snapshot, "IsHdrAvailable")}, Active: {AutomationSnapshotFormatter.Get(snapshot, "HdrOutputActive")}, State: {AutomationSnapshotFormatter.Get(snapshot, "HdrRuntimeState")})");
        builder.AppendLine($"Pipeline: Requested={AutomationSnapshotFormatter.Get(snapshot, "RequestedPipelineMode")} Active={AutomationSnapshotFormatter.Get(snapshot, "ActivePipelineMode")} Matched={AutomationSnapshotFormatter.Get(snapshot, "PipelineModeMatched")}");
        builder.AppendLine($"UI: Show All Options={AutomationSnapshotFormatter.Get(snapshot, "ShowAllCaptureOptions")} | Preview Volume={AutomationSnapshotFormatter.Get(snapshot, "PreviewVolumePercent")}% | Stats Visible={AutomationSnapshotFormatter.Get(snapshot, "IsStatsVisible")}");
        builder.AppendLine();
        builder.AppendLine("== Audio ==");
        builder.AppendLine($"Enabled: {AutomationSnapshotFormatter.Get(snapshot, "IsAudioEnabled")} | Preview: {AutomationSnapshotFormatter.Get(snapshot, "IsAudioPreviewEnabled")} | Custom Input: {AutomationSnapshotFormatter.Get(snapshot, "IsCustomAudioInputEnabled")}");
        builder.AppendLine($"Peak: {AutomationSnapshotFormatter.Get(snapshot, "AudioPeak")} | Clipping: {AutomationSnapshotFormatter.Get(snapshot, "AudioClipping")} | Signal: {AutomationSnapshotFormatter.Get(snapshot, "AudioSignalPresent")}");
        builder.AppendLine($"Reader: {AutomationSnapshotFormatter.Get(snapshot, "AudioReaderActive")} | Frames: {AutomationSnapshotFormatter.Get(snapshot, "AudioFramesArrived")} arrived, {AutomationSnapshotFormatter.Get(snapshot, "AudioFramesWrittenToSink")} to sink");
        builder.AppendLine();
        builder.AppendLine("== Video Pipeline ==");
        builder.AppendLine($"Reader: {AutomationSnapshotFormatter.Get(snapshot, "VideoReaderActive")} | Ingest: {AutomationSnapshotFormatter.Get(snapshot, "IngestVideoFramesArrived")} arrived, {AutomationSnapshotFormatter.Get(snapshot, "IngestVideoFramesWrittenToSink")} to sink");
        builder.AppendLine($"Encoder: {AutomationSnapshotFormatter.Get(snapshot, "EncoderVideoFramesEnqueued")} enqueued, {AutomationSnapshotFormatter.Get(snapshot, "EncoderVideoFramesEncoded")} encoded | Queue: {AutomationSnapshotFormatter.Get(snapshot, "FfmpegVideoQueueDepth")} depth, {AutomationSnapshotFormatter.Get(snapshot, "VideoDropsQueueSaturated")} drops");
        builder.AppendLine($"Freshness: reader {AutomationSnapshotFormatter.Get(snapshot, "IngestLastVideoFrameAgeMs")}ms | enqueue {AutomationSnapshotFormatter.Get(snapshot, "EncoderLastEnqueueAgeMs")}ms | write {AutomationSnapshotFormatter.Get(snapshot, "EncoderLastWriteAgeMs")}ms");
        builder.AppendLine($"Diagnostics: MemPref={AutomationSnapshotFormatter.Get(snapshot, "MemoryPreference")} ReqSubtype={AutomationSnapshotFormatter.Get(snapshot, "VideoRequestedSubtype")} NegSubtype={AutomationSnapshotFormatter.Get(snapshot, "VideoNegotiatedSubtype")} Errors={AutomationSnapshotFormatter.Get(snapshot, "VideoIngestErrorCount")}");
        builder.AppendLine();
        builder.AppendLine("== Thread Health ==");
        var sourceReaderLastFrameAgeMs = AutomationSnapshotFormatter.ComputeTickAgeMs(AutomationSnapshotFormatter.GetLong(snapshot, "SourceReaderLastFrameTickMs"));
        var wasapiCaptureLastCallbackAgeMs = AutomationSnapshotFormatter.ComputeTickAgeMs(AutomationSnapshotFormatter.GetLong(snapshot, "WasapiCaptureLastCallbackTickMs"));
        var wasapiPlaybackLastRenderAgeMs = AutomationSnapshotFormatter.ComputeTickAgeMs(AutomationSnapshotFormatter.GetLong(snapshot, "WasapiPlaybackLastRenderTickMs"));
        var sourceReaderOutstanding = AutomationSnapshotFormatter.Get(snapshot, "SourceReaderReadOutstanding");
        var sourceReaderOutstandingSuffix = string.Equals(sourceReaderOutstanding, "true", StringComparison.OrdinalIgnoreCase)
            ? $" outstandingFor={AutomationSnapshotFormatter.Get(snapshot, "SourceReaderReadOutstandingMs")}ms"
            : string.Empty;
        builder.AppendLine(
            $"Source Reader: outstanding={sourceReaderOutstanding}{sourceReaderOutstandingSuffix} " +
            $"lastFrame={sourceReaderLastFrameAgeMs}ms ago channelDepth={AutomationSnapshotFormatter.Get(snapshot, "SourceReaderFrameChannelDepth")}");
        builder.AppendLine(
            $"WASAPI Capture: callbacks={AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureCallbackCount")} " +
            $"interval={AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureCallbackAvgIntervalMs")}ms/avg {AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureCallbackMaxIntervalMs")}ms/max " +
            $"silence={AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureCallbackSilenceCount")} " +
            $"lastCallback={wasapiCaptureLastCallbackAgeMs}ms ago " +
            $"levelEvents={AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureAudioLevelEventsFired")}");
        builder.AppendLine(
            $"WASAPI Playback: callbacks={AutomationSnapshotFormatter.Get(snapshot, "WasapiPlaybackRenderCallbackCount")} " +
            $"silence={AutomationSnapshotFormatter.Get(snapshot, "WasapiPlaybackRenderSilenceCount")} " +
            $"queueDepth={AutomationSnapshotFormatter.Get(snapshot, "WasapiPlaybackQueueDepth")} " +
            $"drops={AutomationSnapshotFormatter.Get(snapshot, "WasapiPlaybackQueueDropCount")} " +
            $"lastCallback={wasapiPlaybackLastRenderAgeMs}ms ago");
        builder.AppendLine();
        builder.AppendLine("== Recording ==");
        builder.AppendLine($"Recording: {AutomationSnapshotFormatter.Get(snapshot, "IsRecording")} | Output: {AutomationSnapshotFormatter.Get(snapshot, "OutputPath")}");
        builder.AppendLine($"Time: {AutomationSnapshotFormatter.Get(snapshot, "RecordingTime")} | Size: {AutomationSnapshotFormatter.Get(snapshot, "RecordingSizeInfo")} | Bitrate: {AutomationSnapshotFormatter.Get(snapshot, "RecordingBitrateInfo")}");
        builder.AppendLine($"Backend: {AutomationSnapshotFormatter.Get(snapshot, "RecordingBackend")} | Audio Path: {AutomationSnapshotFormatter.Get(snapshot, "AudioPathMode")} | Mux: {AutomationSnapshotFormatter.Get(snapshot, "MuxResult")}");
        builder.AppendLine($"Last Output: {AutomationSnapshotFormatter.Get(snapshot, "LastOutputPath")} ({AutomationSnapshotFormatter.Get(snapshot, "LastOutputSizeBytes")} bytes) Finalize: {AutomationSnapshotFormatter.Get(snapshot, "LastFinalizeStatus")}");
        builder.AppendLine();
        var flashbackActive = AutomationSnapshotFormatter.Get(snapshot, "FlashbackActive", "false");
        if (flashbackActive == "True" || flashbackActive == "true")
        {
            builder.AppendLine("== Flashback ==");
            var encCodec = AutomationSnapshotFormatter.Get(snapshot, "EncoderCodecName");
            if (!string.IsNullOrEmpty(encCodec))
            {
                var encW = AutomationSnapshotFormatter.Get(snapshot, "EncoderWidth", "0");
                var encH = AutomationSnapshotFormatter.Get(snapshot, "EncoderHeight", "0");
                var encFps = AutomationSnapshotFormatter.Get(snapshot, "EncoderFrameRate", "0");
                var encBr = uint.TryParse(AutomationSnapshotFormatter.Get(snapshot, "EncoderTargetBitRate", "0"), out var br) ? br / 1_000_000.0 : 0;
                builder.AppendLine($"Encoder: {encCodec} {encW}x{encH} @ {encFps} fps | Target: {encBr:0.#} Mbps");
            }
            var fbDurationMs = long.TryParse(AutomationSnapshotFormatter.Get(snapshot, "FlashbackBufferedDurationMs", "0"), out var durMs) ? durMs : 0;
            var fbDiskMb = long.TryParse(AutomationSnapshotFormatter.Get(snapshot, "FlashbackDiskBytes", "0"), out var diskBytes) ? diskBytes / (1024.0 * 1024.0) : 0;
            builder.AppendLine($"Buffer: {fbDurationMs / 1000.0:F1}s | Disk: {fbDiskMb:F1} MB | GPU Encode: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackGpuEncoding")}");
            builder.AppendLine($"Encoded: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackEncodedFrames")} frames | Dropped: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackDroppedFrames")} | VQ: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueDepth")} AQ: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackAudioQueueDepth")}");
            builder.AppendLine($"Playback: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackState")} | Pos: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackPositionMs")}ms | Decoder: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackDecoderHwAccel")}");
            var pbFps = double.TryParse(AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackObservedFps", "0"), out var fps) ? fps : 0;
            var pbAvgMs = double.TryParse(AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackAvgFrameMs", "0"), out var avgMs) ? avgMs : 0;
            var avDrift = double.TryParse(AutomationSnapshotFormatter.Get(snapshot, "FlashbackAvDriftMs", "0"), out var drift) ? drift : 0;
            builder.AppendLine($"Playback FPS: {pbFps:F1} | AvgFrame: {pbAvgMs:F2}ms | Frames: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackFrameCount")} | Late: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLateFrames")}");
            builder.AppendLine($"A/V Drift: {avDrift:+0.0;-0.0;0.0}ms (+ = audio ahead) | File: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackFilePath")}");
            builder.AppendLine();
        }

        builder.AppendLine("== Performance ==");
        builder.AppendLine($"Score: {AutomationSnapshotFormatter.Get(snapshot, "PerformanceScore")} | Perfection: {AutomationSnapshotFormatter.Get(snapshot, "PerformancePerfectionMet")}");
        builder.AppendLine($"Summary: {AutomationSnapshotFormatter.Get(snapshot, "PerformanceSummary")}");
        builder.AppendLine($"Pipeline Latency: {AutomationSnapshotFormatter.Get(snapshot, "EstimatedPipelineLatencyMs")}ms (source reader -> present)");
        builder.AppendLine();
        builder.AppendLine("== Memory & GC ==");
        builder.AppendLine($"Working Set: {AutomationSnapshotFormatter.Get(snapshot, "MemoryWorkingSetMb")} MB | Private: {AutomationSnapshotFormatter.Get(snapshot, "MemoryPrivateBytesMb")} MB | Managed Heap: {AutomationSnapshotFormatter.Get(snapshot, "MemoryManagedHeapMb")} MB");
        builder.AppendLine($"Total Allocated: {AutomationSnapshotFormatter.Get(snapshot, "MemoryTotalAllocatedMb")} MB | GC Heap: {AutomationSnapshotFormatter.Get(snapshot, "MemoryGcHeapSizeMb")} MB");
        builder.AppendLine($"GC Collections: Gen0={AutomationSnapshotFormatter.Get(snapshot, "MemoryGcGen0Collections")} Gen1={AutomationSnapshotFormatter.Get(snapshot, "MemoryGcGen1Collections")} Gen2={AutomationSnapshotFormatter.Get(snapshot, "MemoryGcGen2Collections")}");
        builder.AppendLine($"GC Pause: {AutomationSnapshotFormatter.Get(snapshot, "MemoryGcPauseTimePercent")}% | Fragmentation: {AutomationSnapshotFormatter.Get(snapshot, "MemoryGcFragmentationPercent")}%");
        builder.AppendLine($"ThreadPool Workers: {AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolWorkerAvailable")}/{AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolWorkerMax")} avail | IO: {AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolIoAvailable")}/{AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolIoMax")} avail");
        builder.AppendLine();
        builder.AppendLine("== Capture Cadence ==");
        builder.AppendLine($"Source: {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceObservedFps")} fps (expected {AutomationSnapshotFormatter.Get(snapshot, "ExpectedCaptureFrameRate")} fps) | Samples: {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceSampleCount")}");
        builder.AppendLine($"Interval: avg={AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceAverageIntervalMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceP95IntervalMs")}ms max={AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceMaxIntervalMs")}ms");
        builder.AppendLine($"Jitter: {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceJitterStdDevMs")}ms | Gaps: {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceSevereGapCount")} | Est Drops: {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceEstimatedDroppedFrames")} ({AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceEstimatedDropPercent")}%)");
        var mjpegDecodeSamples = AutomationSnapshotFormatter.Get(snapshot, "MjpegDecodeSampleCount", "0");
        if (mjpegDecodeSamples != "0")
        {
            builder.AppendLine();
            builder.AppendLine("== MJPEG Pipeline Timing ==");
            builder.AppendLine($"Decode: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegDecodeAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegDecodeP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegDecodeMaxMs")}ms ({mjpegDecodeSamples} samples)");
            builder.AppendLine($"Interop Copy: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegInteropCopyAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegInteropCopyP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegInteropCopyMaxMs")}ms ({AutomationSnapshotFormatter.Get(snapshot, "MjpegInteropCopySampleCount")} samples)");
            builder.AppendLine($"Total Callback: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegCallbackAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegCallbackP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegCallbackMaxMs")}ms ({AutomationSnapshotFormatter.Get(snapshot, "MjpegCallbackSampleCount")} samples)");

            var mjpegDecoderCount = AutomationSnapshotFormatter.Get(snapshot, "MjpegDecoderCount", "0");
            if (mjpegDecoderCount != "0")
            {
                builder.AppendLine($"Decoders: {mjpegDecoderCount} | Decoded={AutomationSnapshotFormatter.Get(snapshot, "MjpegTotalDecoded")} Emitted={AutomationSnapshotFormatter.Get(snapshot, "MjpegTotalEmitted")} Dropped={AutomationSnapshotFormatter.Get(snapshot, "MjpegTotalDropped")}");
                builder.AppendLine($"Reorder: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderMaxMs")}ms ({AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderSampleCount")} samples) | Skips={AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderSkips")} Buffer={AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderBufferDepth")}");
                builder.AppendLine($"Pipeline: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegPipelineAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegPipelineP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegPipelineMaxMs")}ms ({AutomationSnapshotFormatter.Get(snapshot, "MjpegPipelineSampleCount")} samples)");
                if (snapshot.TryGetProperty("MjpegPerDecoder", out var perDecoder) &&
                    perDecoder.ValueKind == JsonValueKind.Array)
                {
                    foreach (var worker in perDecoder.EnumerateArray())
                    {
                        builder.AppendLine(
                            $"Decoder[{AutomationSnapshotFormatter.Get(worker, "WorkerIndex", "?")}]: avg={AutomationSnapshotFormatter.Get(worker, "AvgMs")}ms " +
                            $"P95={AutomationSnapshotFormatter.Get(worker, "P95Ms")}ms max={AutomationSnapshotFormatter.Get(worker, "MaxMs")}ms " +
                            $"({AutomationSnapshotFormatter.Get(worker, "SampleCount")} samples)");
                    }
                }
            }
        }

        var avSyncDrift = AutomationSnapshotFormatter.Get(snapshot, "AvSyncCaptureDriftMs", string.Empty);
        var avSyncRate = AutomationSnapshotFormatter.Get(snapshot, "AvSyncCaptureDriftRateMsPerSec", string.Empty);
        var avSyncEncoder = AutomationSnapshotFormatter.Get(snapshot, "AvSyncEncoderDriftMs", string.Empty);
        var avSyncCorr = AutomationSnapshotFormatter.Get(snapshot, "AvSyncEncoderCorrectionSamples", string.Empty);
        if (!string.IsNullOrWhiteSpace(avSyncDrift) || !string.IsNullOrWhiteSpace(avSyncEncoder))
        {
            builder.AppendLine();
            builder.AppendLine("== AV Sync ==");
            builder.AppendLine(
                $"Capture Drift: {(string.IsNullOrWhiteSpace(avSyncDrift) ? "N/A" : avSyncDrift + "ms")} | " +
                $"Rate: {(string.IsNullOrWhiteSpace(avSyncRate) ? "N/A" : avSyncRate + "ms/s")}");
            if (!string.IsNullOrWhiteSpace(avSyncEncoder))
            {
                builder.AppendLine(
                    $"Encoder Drift: {avSyncEncoder}ms | " +
                    $"Correction Samples: {(string.IsNullOrWhiteSpace(avSyncCorr) ? "N/A" : avSyncCorr)}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("== Preview ==");
        var rendererMode = AutomationSnapshotFormatter.Get(snapshot, "PreviewRendererMode");
        builder.AppendLine($"Renderer: {rendererMode} | Startup: {AutomationSnapshotFormatter.Get(snapshot, "PreviewStartupState")} | First Visual: {AutomationSnapshotFormatter.Get(snapshot, "PreviewFirstVisualConfirmed")}");
        if (rendererMode == "GpuMediaSource")
        {
            builder.AppendLine($"GPU Playback: {AutomationSnapshotFormatter.Get(snapshot, "PreviewGpuPlaybackState")} | Video: {AutomationSnapshotFormatter.Get(snapshot, "PreviewGpuNaturalVideoWidth")}x{AutomationSnapshotFormatter.Get(snapshot, "PreviewGpuNaturalVideoHeight")} | Position: {AutomationSnapshotFormatter.Get(snapshot, "PreviewGpuPositionMs")}ms | Events: {AutomationSnapshotFormatter.Get(snapshot, "PreviewGpuPositionEventCount")}");
        }
        else if (rendererMode == "D3D11VideoProcessor" || rendererMode == "HdrShader")
        {
            builder.AppendLine($"D3D Frames: {AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFramesSubmitted")} submitted, {AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFramesRendered")} rendered, {AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFramesDropped")} dropped");
            builder.AppendLine($"Color: input={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DInputColorSpace")} output={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DOutputColorSpace")}");
            builder.AppendLine($"Cadence: {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceObservedFps")} fps");
        }
        else
        {
            builder.AppendLine($"Frames: {AutomationSnapshotFormatter.Get(snapshot, "PreviewFramesArrived")} arrived, {AutomationSnapshotFormatter.Get(snapshot, "PreviewFramesDisplayed")} displayed, {AutomationSnapshotFormatter.Get(snapshot, "PreviewFramesDropped")} dropped");
            builder.AppendLine($"Cadence: {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceObservedFps")} fps");
        }

        builder.AppendLine();
        builder.AppendLine("== Source ==");
        var sourceFrameRate = AutomationSnapshotFormatter.Get(snapshot, "DetectedSourceFrameRate", string.Empty);
        var sourceFrameRateArg = AutomationSnapshotFormatter.Get(snapshot, "DetectedSourceFrameRateArg", string.Empty);
        var sourceFpsSummary = !string.IsNullOrWhiteSpace(sourceFrameRateArg)
            ? $"{sourceFrameRate}fps ({sourceFrameRateArg})"
            : !string.IsNullOrWhiteSpace(sourceFrameRate)
                ? $"{sourceFrameRate}fps"
                : "N/A";
        builder.AppendLine($"Source: {AutomationSnapshotFormatter.Get(snapshot, "SourceWidth")} x {AutomationSnapshotFormatter.Get(snapshot, "SourceHeight")} @ {sourceFpsSummary} HDR={AutomationSnapshotFormatter.Get(snapshot, "SourceIsHdr")}");
        builder.AppendLine($"Telemetry: {AutomationSnapshotFormatter.Get(snapshot, "SourceTelemetryAvailability")} ({AutomationSnapshotFormatter.Get(snapshot, "SourceTelemetryConfidence")})");

        return builder.ToString().TrimEnd();
    }

    public static string FormatDiagnostics(JsonElement response)
    {
        if (!TryGetData(response, out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return AutomationSnapshotFormatter.Get(response, "Message", "No diagnostics available.");
        }

        var builder = new StringBuilder();
        builder.AppendLine("== Diagnostics ==");
        var count = 0;
        foreach (var item in data.EnumerateArray())
        {
            count++;
            var correlation = AutomationSnapshotFormatter.Get(item, "CorrelationId", string.Empty);
            var correlationSuffix = string.IsNullOrWhiteSpace(correlation) ? string.Empty : $" [{correlation}]";
            builder.AppendLine(
                $"{AutomationSnapshotFormatter.Get(item, "TimestampUtc", "?")} [{AutomationSnapshotFormatter.Get(item, "Severity", "Info")}] [{AutomationSnapshotFormatter.Get(item, "Category", "System")}] {AutomationSnapshotFormatter.Get(item, "Message", string.Empty)}{correlationSuffix}");
        }

        if (count == 0)
        {
            builder.AppendLine("No diagnostic events.");
        }

        return builder.ToString().TrimEnd();
    }

    public static string FormatOptions(JsonElement response)
    {
        if (!TryGetData(response, out var data) || data.ValueKind != JsonValueKind.Object)
        {
            return AutomationSnapshotFormatter.Get(response, "Message", "Capture options not available.");
        }

        var builder = new StringBuilder();
        builder.AppendLine("== Capture Options ==");
        builder.AppendLine($"Selected Device: {AutomationSnapshotFormatter.Get(data, "SelectedDeviceId")}");
        builder.AppendLine($"Selected Audio Input: {AutomationSnapshotFormatter.Get(data, "SelectedAudioInputDeviceId")}");
        builder.AppendLine($"Resolution: {AutomationSnapshotFormatter.Get(data, "SelectedResolution")} | Frame Rate: {AutomationSnapshotFormatter.Get(data, "SelectedFrameRate")}");
        builder.AppendLine($"Format: {AutomationSnapshotFormatter.Get(data, "SelectedRecordingFormat")} | Quality: {AutomationSnapshotFormatter.Get(data, "SelectedQuality")} | Preset: {AutomationSnapshotFormatter.Get(data, "SelectedPreset")}");
        builder.AppendLine($"Split Encode: {AutomationSnapshotFormatter.Get(data, "SelectedSplitEncodeMode")} | Video Format: {AutomationSnapshotFormatter.Get(data, "SelectedVideoFormat")} | MJPEG Decoders: {AutomationSnapshotFormatter.Get(data, "MjpegDecoderCount")}");
        builder.AppendLine($"Show All Options: {AutomationSnapshotFormatter.Get(data, "ShowAllCaptureOptions")} | Preview Volume: {AutomationSnapshotFormatter.Get(data, "PreviewVolumePercent")}% | Stats Visible: {AutomationSnapshotFormatter.Get(data, "IsStatsVisible")}");
        builder.AppendLine();
        AppendNamedOptions(builder, "Devices", data, "Devices", includeId: true);
        builder.AppendLine();
        AppendNamedOptions(builder, "Audio Input Devices", data, "AudioInputDevices", includeId: true);
        builder.AppendLine();
        AppendResolutionOptions(builder, data);
        builder.AppendLine();
        AppendFrameRateOptions(builder, data);
        builder.AppendLine();
        AppendStringOptions(builder, "Recording Formats", data, "RecordingFormats");
        builder.AppendLine();
        AppendStringOptions(builder, "Qualities", data, "Qualities");
        builder.AppendLine();
        AppendStringOptions(builder, "Presets", data, "Presets");
        builder.AppendLine();
        AppendStringOptions(builder, "Split Encode Modes", data, "SplitEncodeModes");
        builder.AppendLine();
        AppendStringOptions(builder, "Video Formats", data, "VideoFormats");
        builder.AppendLine();
        AppendIntOptions(builder, "MJPEG Decoder Counts", data, "MjpegDecoderCounts");
        return builder.ToString().TrimEnd();
    }

    public static string FormatDeviceList(JsonElement response)
    {
        if (!TryGetData(response, out var data) || data.ValueKind != JsonValueKind.Object)
        {
            return AutomationSnapshotFormatter.Get(response, "Message", "Capture options not available.");
        }

        var builder = new StringBuilder();
        builder.AppendLine("== Devices ==");
        AppendNamedOptions(builder, "Capture Devices", data, "Devices", includeId: true);
        builder.AppendLine();
        AppendNamedOptions(builder, "Audio Input Devices", data, "AudioInputDevices", includeId: true);
        return builder.ToString().TrimEnd();
    }

    public static string FormatTimeline(JsonElement response)
    {
        if (!TryGetData(response, out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return AutomationSnapshotFormatter.Get(response, "Message", "No timeline data available.");
        }

        var entries = new List<TimelineRow>();
        foreach (var item in data.EnumerateArray())
        {
            entries.Add(new TimelineRow
            {
                Timestamp = AutomationSnapshotFormatter.Get(item, "TimestampUtc"),
                CaptureFps = AutomationSnapshotFormatter.GetDouble(item, "CaptureFps"),
                PreviewFps = AutomationSnapshotFormatter.GetDouble(item, "PreviewFps"),
                VidQueue = AutomationSnapshotFormatter.GetInt(item, "VideoQueueDepth"),
                VidDrops = AutomationSnapshotFormatter.GetLong(item, "VideoDrops"),
                P95Ms = AutomationSnapshotFormatter.GetDouble(item, "CaptureCadenceP95Ms"),
                LatencyMs = AutomationSnapshotFormatter.GetLong(item, "PipelineLatencyMs"),
                WorkingMb = AutomationSnapshotFormatter.GetDouble(item, "MemoryWorkingSetMb"),
                ManagedMb = AutomationSnapshotFormatter.GetDouble(item, "MemoryManagedHeapMb"),
                Gen0 = AutomationSnapshotFormatter.GetInt(item, "GcGen0Collections"),
                Gen1 = AutomationSnapshotFormatter.GetInt(item, "GcGen1Collections"),
                Gen2 = AutomationSnapshotFormatter.GetInt(item, "GcGen2Collections"),
                GcPause = AutomationSnapshotFormatter.GetDouble(item, "GcPauseTimePercent"),
                Workers = AutomationSnapshotFormatter.GetInt(item, "ThreadPoolWorkerAvailable"),
                IoThreads = AutomationSnapshotFormatter.GetInt(item, "ThreadPoolIoAvailable")
            });
        }

        if (entries.Count == 0)
        {
            return "No timeline entries collected yet.";
        }

        var builder = new StringBuilder();
        builder.AppendLine($"Performance Timeline ({entries.Count} samples)");
        builder.AppendLine();
        builder.AppendLine("Timestamp                | CapFPS | PrvFPS | VidQ | VidDrop |  P95ms | LatMs | WorkMB | MgdMB  | G0   | G1   | G2   | GC%  | Wkr  | IO");
        builder.AppendLine(new string('-', 160));

        foreach (var entry in entries)
        {
            builder.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0,-24} | {1,6:F1} | {2,6:F1} | {3,4} | {4,7} | {5,6:F1} | {6,5} | {7,6:F1} | {8,6:F1} | {9,4} | {10,4} | {11,4} | {12,4:F1} | {13,4} | {14,4}",
                entry.Timestamp,
                entry.CaptureFps,
                entry.PreviewFps,
                entry.VidQueue,
                entry.VidDrops,
                entry.P95Ms,
                entry.LatencyMs,
                entry.WorkingMb,
                entry.ManagedMb,
                entry.Gen0,
                entry.Gen1,
                entry.Gen2,
                entry.GcPause,
                entry.Workers,
                entry.IoThreads));
        }

        if (entries.Count >= 2)
        {
            var first = entries[0];
            var last = entries[^1];
            builder.AppendLine();
            builder.AppendLine("== Trend Summary (first vs last sample) ==");
            builder.AppendLine($"Capture FPS:    {first.CaptureFps:F1} -> {last.CaptureFps:F1} (delta: {last.CaptureFps - first.CaptureFps:+0.0;-0.0;0.0})");
            builder.AppendLine($"Preview FPS:    {first.PreviewFps:F1} -> {last.PreviewFps:F1} (delta: {last.PreviewFps - first.PreviewFps:+0.0;-0.0;0.0})");
            builder.AppendLine($"Video Drops:    {first.VidDrops} -> {last.VidDrops} (delta: {last.VidDrops - first.VidDrops:+0;-0;0})");
            builder.AppendLine($"Capture P95:    {first.P95Ms:F1}ms -> {last.P95Ms:F1}ms (delta: {last.P95Ms - first.P95Ms:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"Working Set:    {first.WorkingMb:F1}MB -> {last.WorkingMb:F1}MB (delta: {last.WorkingMb - first.WorkingMb:+0.0;-0.0;0.0}MB)");
            builder.AppendLine($"Managed Heap:   {first.ManagedMb:F1}MB -> {last.ManagedMb:F1}MB (delta: {last.ManagedMb - first.ManagedMb:+0.0;-0.0;0.0}MB)");
            builder.AppendLine($"GC Gen0:        {first.Gen0} -> {last.Gen0} (delta: {last.Gen0 - first.Gen0:+0;-0;0})");
            builder.AppendLine($"GC Gen2:        {first.Gen2} -> {last.Gen2} (delta: {last.Gen2 - first.Gen2:+0;-0;0})");
            builder.AppendLine($"GC Pause%:      {first.GcPause:F1}% -> {last.GcPause:F1}% (delta: {last.GcPause - first.GcPause:+0.0;-0.0;0.0}%)");
        }

        return builder.ToString().TrimEnd();
    }

    public static string FormatMemory(JsonElement response)
    {
        if (!response.TryGetProperty("Snapshot", out var snapshot) || snapshot.ValueKind != JsonValueKind.Object)
        {
            return AutomationSnapshotFormatter.Get(response, "Message", "Snapshot data not available.");
        }

        var builder = new StringBuilder();
        builder.AppendLine("== Memory & GC ==");
        builder.AppendLine($"Working Set: {AutomationSnapshotFormatter.Get(snapshot, "MemoryWorkingSetMb")} MB");
        builder.AppendLine($"Private Bytes: {AutomationSnapshotFormatter.Get(snapshot, "MemoryPrivateBytesMb")} MB");
        builder.AppendLine($"Managed Heap: {AutomationSnapshotFormatter.Get(snapshot, "MemoryManagedHeapMb")} MB");
        builder.AppendLine($"Total Allocated: {AutomationSnapshotFormatter.Get(snapshot, "MemoryTotalAllocatedMb")} MB");
        builder.AppendLine($"GC Heap Size: {AutomationSnapshotFormatter.Get(snapshot, "MemoryGcHeapSizeMb")} MB");
        builder.AppendLine($"GC Collections: Gen0={AutomationSnapshotFormatter.Get(snapshot, "MemoryGcGen0Collections")} Gen1={AutomationSnapshotFormatter.Get(snapshot, "MemoryGcGen1Collections")} Gen2={AutomationSnapshotFormatter.Get(snapshot, "MemoryGcGen2Collections")}");
        builder.AppendLine($"GC Pause: {AutomationSnapshotFormatter.Get(snapshot, "MemoryGcPauseTimePercent")}% | Fragmentation: {AutomationSnapshotFormatter.Get(snapshot, "MemoryGcFragmentationPercent")}%");
        builder.AppendLine($"ThreadPool Workers: {AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolWorkerAvailable")}/{AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolWorkerMax")} avail");
        builder.AppendLine($"ThreadPool IO: {AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolIoAvailable")}/{AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolIoMax")} avail");
        return builder.ToString().TrimEnd();
    }

    public static string FormatResult(JsonElement response, bool includeData)
    {
        if (!includeData || !TryGetData(response, out var data) || data.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return AutomationSnapshotFormatter.Get(response, "Message", "Command completed.");
        }

        return $"{AutomationSnapshotFormatter.Get(response, "Message", "Command completed.")}{Environment.NewLine}{PrettyJson(data)}";
    }

    public static string PrettyJson(JsonElement element)
        => JsonSerializer.Serialize(element, IndentedJsonOptions);

    private static void AppendNamedOptions(StringBuilder builder, string title, JsonElement data, string propertyName, bool includeId)
    {
        builder.AppendLine($"== {title} ==");
        if (!data.TryGetProperty(propertyName, out var options) || options.ValueKind != JsonValueKind.Array || options.GetArrayLength() == 0)
        {
            builder.AppendLine("None");
            return;
        }

        foreach (var item in options.EnumerateArray())
        {
            var prefix = AutomationSnapshotFormatter.Get(item, "IsSelected", "false").Equals("true", StringComparison.OrdinalIgnoreCase) ? "*" : "-";
            var idSuffix = includeId ? $" ({AutomationSnapshotFormatter.Get(item, "Id")})" : string.Empty;
            builder.AppendLine($"{prefix} {AutomationSnapshotFormatter.Get(item, "Name")}{idSuffix}");
        }
    }

    private static void AppendResolutionOptions(StringBuilder builder, JsonElement data)
    {
        builder.AppendLine("== Resolutions ==");
        if (!data.TryGetProperty("Resolutions", out var options) || options.ValueKind != JsonValueKind.Array || options.GetArrayLength() == 0)
        {
            builder.AppendLine("None");
            return;
        }

        foreach (var item in options.EnumerateArray())
        {
            builder.AppendLine($"{GetSelectionPrefix(item)} {AutomationSnapshotFormatter.Get(item, "Value")} ({AutomationSnapshotFormatter.Get(item, "Width")}x{AutomationSnapshotFormatter.Get(item, "Height")}){GetDisableSuffix(item)}");
        }
    }

    private static void AppendFrameRateOptions(StringBuilder builder, JsonElement data)
    {
        builder.AppendLine("== Frame Rates ==");
        if (!data.TryGetProperty("FrameRates", out var options) || options.ValueKind != JsonValueKind.Array || options.GetArrayLength() == 0)
        {
            builder.AppendLine("None");
            return;
        }

        foreach (var item in options.EnumerateArray())
        {
            builder.AppendLine(
                $"{GetSelectionPrefix(item)} {AutomationSnapshotFormatter.Get(item, "FriendlyValue")} fps ({AutomationSnapshotFormatter.Get(item, "Value")} exact, {AutomationSnapshotFormatter.Get(item, "ExactValueArg")}){GetDisableSuffix(item)}");
        }
    }

    private static void AppendStringOptions(StringBuilder builder, string title, JsonElement data, string propertyName)
    {
        builder.AppendLine($"== {title} ==");
        if (!data.TryGetProperty(propertyName, out var options) || options.ValueKind != JsonValueKind.Array || options.GetArrayLength() == 0)
        {
            builder.AppendLine("None");
            return;
        }

        foreach (var item in options.EnumerateArray())
        {
            builder.AppendLine($"{GetSelectionPrefix(item)} {AutomationSnapshotFormatter.Get(item, "Label", AutomationSnapshotFormatter.Get(item, "Value"))}{GetDisableSuffix(item)}");
        }
    }

    private static void AppendIntOptions(StringBuilder builder, string title, JsonElement data, string propertyName)
    {
        builder.AppendLine($"== {title} ==");
        if (!data.TryGetProperty(propertyName, out var options) || options.ValueKind != JsonValueKind.Array || options.GetArrayLength() == 0)
        {
            builder.AppendLine("None");
            return;
        }

        foreach (var item in options.EnumerateArray())
        {
            builder.AppendLine($"{GetSelectionPrefix(item)} {AutomationSnapshotFormatter.Get(item, "Value")}{GetDisableSuffix(item)}");
        }
    }

    private static string GetSelectionPrefix(JsonElement item)
        => AutomationSnapshotFormatter.Get(item, "IsSelected", "false").Equals("true", StringComparison.OrdinalIgnoreCase) ? "*" : "-";

    private static string GetDisableSuffix(JsonElement item)
    {
        var isEnabled = AutomationSnapshotFormatter.Get(item, "IsEnabled", "true");
        if (isEnabled.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return $" [disabled: {AutomationSnapshotFormatter.Get(item, "DisableReason", "Unavailable")}]";
    }

    private static bool TryGetData(JsonElement response, out JsonElement data)
    {
        if (response.ValueKind == JsonValueKind.Object && response.TryGetProperty("Data", out data))
        {
            return true;
        }

        data = default;
        return false;
    }

    private sealed class TimelineRow
    {
        public string Timestamp { get; init; } = string.Empty;
        public double CaptureFps { get; init; }
        public double PreviewFps { get; init; }
        public int VidQueue { get; init; }
        public long VidDrops { get; init; }
        public double P95Ms { get; init; }
        public long LatencyMs { get; init; }
        public double WorkingMb { get; init; }
        public double ManagedMb { get; init; }
        public int Gen0 { get; init; }
        public int Gen1 { get; init; }
        public int Gen2 { get; init; }
        public double GcPause { get; init; }
        public int Workers { get; init; }
        public int IoThreads { get; init; }
    }
}
