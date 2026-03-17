using System.Globalization;
using System.Text;
using System.Text.Json;

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
            return Get(snapshotResponse, "Message", "Snapshot data not available.");
        }

        var selectedFriendlyFrameRate = Get(snapshot, "SelectedFriendlyFrameRate", string.Empty);
        var selectedExactFrameRate = Get(snapshot, "SelectedExactFrameRate", string.Empty);
        var selectedExactFrameRateArg = Get(snapshot, "SelectedExactFrameRateArg", string.Empty);
        var frameRateBucket = string.IsNullOrWhiteSpace(selectedFriendlyFrameRate)
            ? Get(snapshot, "SelectedFrameRate")
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
        builder.AppendLine($"Status: {Get(snapshot, "SessionState")} | {Get(snapshot, "StatusText")}");
        builder.AppendLine($"Device: {Get(snapshot, "SelectedDeviceName")} ({Get(snapshot, "SelectedDeviceId")})");
        builder.AppendLine($"Initialized: {Get(snapshot, "IsInitialized")} | Previewing: {Get(snapshot, "IsPreviewing")} | Recording: {Get(snapshot, "IsRecording")}");
        builder.AppendLine();
        builder.AppendLine("== Capture Settings ==");
        builder.AppendLine($"Resolution: {Get(snapshot, "SelectedResolution")} | Frame Rate: {frameRateSummary}");
        builder.AppendLine($"Format: {Get(snapshot, "SelectedRecordingFormat")} | Quality: {Get(snapshot, "SelectedQuality")} | Preset: {Get(snapshot, "SelectedPreset")}");
        builder.AppendLine($"Video Format: {Get(snapshot, "SelectedVideoFormat")} | Split Encode: {Get(snapshot, "SelectedSplitEncodeMode")} | MJPEG Decoders: {Get(snapshot, "MjpegDecoderCount")}");
        builder.AppendLine($"HDR: {Get(snapshot, "IsHdrEnabled")} (Available: {Get(snapshot, "IsHdrAvailable")}, Active: {Get(snapshot, "HdrOutputActive")}, State: {Get(snapshot, "HdrRuntimeState")})");
        builder.AppendLine($"Pipeline: Requested={Get(snapshot, "RequestedPipelineMode")} Active={Get(snapshot, "ActivePipelineMode")} Matched={Get(snapshot, "PipelineModeMatched")}");
        builder.AppendLine($"UI: Show All Options={Get(snapshot, "ShowAllCaptureOptions")} | Preview Volume={Get(snapshot, "PreviewVolumePercent")}% | Stats Visible={Get(snapshot, "IsStatsVisible")}");
        builder.AppendLine();
        builder.AppendLine("== Audio ==");
        builder.AppendLine($"Enabled: {Get(snapshot, "IsAudioEnabled")} | Preview: {Get(snapshot, "IsAudioPreviewEnabled")} | Custom Input: {Get(snapshot, "IsCustomAudioInputEnabled")}");
        builder.AppendLine($"Peak: {Get(snapshot, "AudioPeak")} | Clipping: {Get(snapshot, "AudioClipping")} | Signal: {Get(snapshot, "AudioSignalPresent")}");
        builder.AppendLine($"Reader: {Get(snapshot, "AudioReaderActive")} | Frames: {Get(snapshot, "AudioFramesArrived")} arrived, {Get(snapshot, "AudioFramesWrittenToSink")} to sink");
        builder.AppendLine();
        builder.AppendLine("== Video Pipeline ==");
        builder.AppendLine($"Reader: {Get(snapshot, "VideoReaderActive")} | Ingest: {Get(snapshot, "IngestVideoFramesArrived")} arrived, {Get(snapshot, "IngestVideoFramesWrittenToSink")} to sink");
        builder.AppendLine($"Encoder: {Get(snapshot, "EncoderVideoFramesEnqueued")} enqueued, {Get(snapshot, "EncoderVideoFramesEncoded")} encoded | Queue: {Get(snapshot, "FfmpegVideoQueueDepth")} depth, {Get(snapshot, "VideoDropsQueueSaturated")} drops");
        builder.AppendLine($"Freshness: reader {Get(snapshot, "IngestLastVideoFrameAgeMs")}ms | enqueue {Get(snapshot, "EncoderLastEnqueueAgeMs")}ms | write {Get(snapshot, "EncoderLastWriteAgeMs")}ms");
        builder.AppendLine($"Diagnostics: MemPref={Get(snapshot, "MemoryPreference")} ReqSubtype={Get(snapshot, "VideoRequestedSubtype")} NegSubtype={Get(snapshot, "VideoNegotiatedSubtype")} Errors={Get(snapshot, "VideoIngestErrorCount")}");
        builder.AppendLine();
        builder.AppendLine("== Thread Health ==");
        var sourceReaderLastFrameAgeMs = FormatTickAgeMs(GetLong(snapshot, "SourceReaderLastFrameTickMs"));
        var wasapiCaptureLastCallbackAgeMs = FormatTickAgeMs(GetLong(snapshot, "WasapiCaptureLastCallbackTickMs"));
        var wasapiPlaybackLastRenderAgeMs = FormatTickAgeMs(GetLong(snapshot, "WasapiPlaybackLastRenderTickMs"));
        var sourceReaderOutstanding = Get(snapshot, "SourceReaderReadOutstanding");
        var sourceReaderOutstandingSuffix = string.Equals(sourceReaderOutstanding, "true", StringComparison.OrdinalIgnoreCase)
            ? $" outstandingFor={Get(snapshot, "SourceReaderReadOutstandingMs")}ms"
            : string.Empty;
        builder.AppendLine(
            $"Source Reader: outstanding={sourceReaderOutstanding}{sourceReaderOutstandingSuffix} " +
            $"lastFrame={sourceReaderLastFrameAgeMs}ms ago channelDepth={Get(snapshot, "SourceReaderFrameChannelDepth")}");
        builder.AppendLine(
            $"WASAPI Capture: callbacks={Get(snapshot, "WasapiCaptureCallbackCount")} " +
            $"interval={Get(snapshot, "WasapiCaptureCallbackAvgIntervalMs")}ms/avg {Get(snapshot, "WasapiCaptureCallbackMaxIntervalMs")}ms/max " +
            $"silence={Get(snapshot, "WasapiCaptureCallbackSilenceCount")} " +
            $"lastCallback={wasapiCaptureLastCallbackAgeMs}ms ago " +
            $"levelEvents={Get(snapshot, "WasapiCaptureAudioLevelEventsFired")}");
        builder.AppendLine(
            $"WASAPI Playback: callbacks={Get(snapshot, "WasapiPlaybackRenderCallbackCount")} " +
            $"silence={Get(snapshot, "WasapiPlaybackRenderSilenceCount")} " +
            $"queueDepth={Get(snapshot, "WasapiPlaybackQueueDepth")} " +
            $"drops={Get(snapshot, "WasapiPlaybackQueueDropCount")} " +
            $"lastCallback={wasapiPlaybackLastRenderAgeMs}ms ago");
        builder.AppendLine();
        builder.AppendLine("== Recording ==");
        builder.AppendLine($"Recording: {Get(snapshot, "IsRecording")} | Output: {Get(snapshot, "OutputPath")}");
        builder.AppendLine($"Time: {Get(snapshot, "RecordingTime")} | Size: {Get(snapshot, "RecordingSizeInfo")} | Bitrate: {Get(snapshot, "RecordingBitrateInfo")}");
        builder.AppendLine($"Backend: {Get(snapshot, "RecordingBackend")} | Audio Path: {Get(snapshot, "AudioPathMode")} | Mux: {Get(snapshot, "MuxResult")}");
        builder.AppendLine($"Last Output: {Get(snapshot, "LastOutputPath")} ({Get(snapshot, "LastOutputSizeBytes")} bytes) Finalize: {Get(snapshot, "LastFinalizeStatus")}");
        builder.AppendLine();
        var flashbackActive = Get(snapshot, "FlashbackActive", "false");
        if (flashbackActive == "True" || flashbackActive == "true")
        {
            builder.AppendLine("== Flashback ==");
            var fbDurationMs = long.TryParse(Get(snapshot, "FlashbackBufferedDurationMs", "0"), out var durMs) ? durMs : 0;
            var fbDiskMb = long.TryParse(Get(snapshot, "FlashbackDiskBytes", "0"), out var diskBytes) ? diskBytes / (1024.0 * 1024.0) : 0;
            builder.AppendLine($"Buffer: {fbDurationMs / 1000.0:F1}s | Disk: {fbDiskMb:F1} MB | GPU Encode: {Get(snapshot, "FlashbackGpuEncoding")}");
            builder.AppendLine($"Encoded: {Get(snapshot, "FlashbackEncodedFrames")} frames | Dropped: {Get(snapshot, "FlashbackDroppedFrames")} | VQ: {Get(snapshot, "FlashbackVideoQueueDepth")} AQ: {Get(snapshot, "FlashbackAudioQueueDepth")}");
            builder.AppendLine($"Playback: {Get(snapshot, "FlashbackPlaybackState")} | Pos: {Get(snapshot, "FlashbackPlaybackPositionMs")}ms | Decoder: {Get(snapshot, "FlashbackDecoderHwAccel")}");
            var pbFps = double.TryParse(Get(snapshot, "FlashbackPlaybackObservedFps", "0"), out var fps) ? fps : 0;
            var pbAvgMs = double.TryParse(Get(snapshot, "FlashbackPlaybackAvgFrameMs", "0"), out var avgMs) ? avgMs : 0;
            var avDrift = double.TryParse(Get(snapshot, "FlashbackAvDriftMs", "0"), out var drift) ? drift : 0;
            builder.AppendLine($"Playback FPS: {pbFps:F1} | AvgFrame: {pbAvgMs:F2}ms | Frames: {Get(snapshot, "FlashbackPlaybackFrameCount")} | Late: {Get(snapshot, "FlashbackPlaybackLateFrames")}");
            builder.AppendLine($"A/V Drift: {avDrift:+0.0;-0.0;0.0}ms (+ = audio ahead) | File: {Get(snapshot, "FlashbackFilePath")}");
            builder.AppendLine();
        }

        builder.AppendLine("== Performance ==");
        builder.AppendLine($"Score: {Get(snapshot, "PerformanceScore")} | Perfection: {Get(snapshot, "PerformancePerfectionMet")}");
        builder.AppendLine($"Summary: {Get(snapshot, "PerformanceSummary")}");
        builder.AppendLine($"Pipeline Latency: {Get(snapshot, "EstimatedPipelineLatencyMs")}ms (source reader -> present)");
        builder.AppendLine();
        builder.AppendLine("== Memory & GC ==");
        builder.AppendLine($"Working Set: {Get(snapshot, "MemoryWorkingSetMb")} MB | Private: {Get(snapshot, "MemoryPrivateBytesMb")} MB | Managed Heap: {Get(snapshot, "MemoryManagedHeapMb")} MB");
        builder.AppendLine($"Total Allocated: {Get(snapshot, "MemoryTotalAllocatedMb")} MB | GC Heap: {Get(snapshot, "MemoryGcHeapSizeMb")} MB");
        builder.AppendLine($"GC Collections: Gen0={Get(snapshot, "MemoryGcGen0Collections")} Gen1={Get(snapshot, "MemoryGcGen1Collections")} Gen2={Get(snapshot, "MemoryGcGen2Collections")}");
        builder.AppendLine($"GC Pause: {Get(snapshot, "MemoryGcPauseTimePercent")}% | Fragmentation: {Get(snapshot, "MemoryGcFragmentationPercent")}%");
        builder.AppendLine($"ThreadPool Workers: {Get(snapshot, "ThreadPoolWorkerAvailable")}/{Get(snapshot, "ThreadPoolWorkerMax")} avail | IO: {Get(snapshot, "ThreadPoolIoAvailable")}/{Get(snapshot, "ThreadPoolIoMax")} avail");
        builder.AppendLine();
        builder.AppendLine("== Capture Cadence ==");
        builder.AppendLine($"Source: {Get(snapshot, "CaptureCadenceObservedFps")} fps (expected {Get(snapshot, "ExpectedCaptureFrameRate")} fps) | Samples: {Get(snapshot, "CaptureCadenceSampleCount")}");
        builder.AppendLine($"Interval: avg={Get(snapshot, "CaptureCadenceAverageIntervalMs")}ms P95={Get(snapshot, "CaptureCadenceP95IntervalMs")}ms max={Get(snapshot, "CaptureCadenceMaxIntervalMs")}ms");
        builder.AppendLine($"Jitter: {Get(snapshot, "CaptureCadenceJitterStdDevMs")}ms | Gaps: {Get(snapshot, "CaptureCadenceSevereGapCount")} | Est Drops: {Get(snapshot, "CaptureCadenceEstimatedDroppedFrames")} ({Get(snapshot, "CaptureCadenceEstimatedDropPercent")}%)");
        var mjpegDecodeSamples = Get(snapshot, "MjpegDecodeSampleCount", "0");
        if (mjpegDecodeSamples != "0")
        {
            builder.AppendLine();
            builder.AppendLine("== MJPEG Pipeline Timing ==");
            builder.AppendLine($"Decode: avg={Get(snapshot, "MjpegDecodeAvgMs")}ms P95={Get(snapshot, "MjpegDecodeP95Ms")}ms max={Get(snapshot, "MjpegDecodeMaxMs")}ms ({mjpegDecodeSamples} samples)");
            builder.AppendLine($"Interop Copy: avg={Get(snapshot, "MjpegInteropCopyAvgMs")}ms P95={Get(snapshot, "MjpegInteropCopyP95Ms")}ms max={Get(snapshot, "MjpegInteropCopyMaxMs")}ms ({Get(snapshot, "MjpegInteropCopySampleCount")} samples)");
            builder.AppendLine($"Total Callback: avg={Get(snapshot, "MjpegCallbackAvgMs")}ms P95={Get(snapshot, "MjpegCallbackP95Ms")}ms max={Get(snapshot, "MjpegCallbackMaxMs")}ms ({Get(snapshot, "MjpegCallbackSampleCount")} samples)");

            var mjpegDecoderCount = Get(snapshot, "MjpegDecoderCount", "0");
            if (mjpegDecoderCount != "0")
            {
                builder.AppendLine($"Decoders: {mjpegDecoderCount} | Decoded={Get(snapshot, "MjpegTotalDecoded")} Emitted={Get(snapshot, "MjpegTotalEmitted")} Dropped={Get(snapshot, "MjpegTotalDropped")}");
                builder.AppendLine($"Reorder: avg={Get(snapshot, "MjpegReorderAvgMs")}ms P95={Get(snapshot, "MjpegReorderP95Ms")}ms max={Get(snapshot, "MjpegReorderMaxMs")}ms ({Get(snapshot, "MjpegReorderSampleCount")} samples) | Skips={Get(snapshot, "MjpegReorderSkips")} Buffer={Get(snapshot, "MjpegReorderBufferDepth")}");
                builder.AppendLine($"Pipeline: avg={Get(snapshot, "MjpegPipelineAvgMs")}ms P95={Get(snapshot, "MjpegPipelineP95Ms")}ms max={Get(snapshot, "MjpegPipelineMaxMs")}ms ({Get(snapshot, "MjpegPipelineSampleCount")} samples)");
                if (snapshot.TryGetProperty("MjpegPerDecoder", out var perDecoder) &&
                    perDecoder.ValueKind == JsonValueKind.Array)
                {
                    foreach (var worker in perDecoder.EnumerateArray())
                    {
                        builder.AppendLine(
                            $"Decoder[{Get(worker, "WorkerIndex", "?")}]: avg={Get(worker, "AvgMs")}ms " +
                            $"P95={Get(worker, "P95Ms")}ms max={Get(worker, "MaxMs")}ms " +
                            $"({Get(worker, "SampleCount")} samples)");
                    }
                }
            }
        }

        var avSyncDrift = Get(snapshot, "AvSyncCaptureDriftMs", string.Empty);
        var avSyncRate = Get(snapshot, "AvSyncCaptureDriftRateMsPerSec", string.Empty);
        var avSyncEncoder = Get(snapshot, "AvSyncEncoderDriftMs", string.Empty);
        var avSyncCorr = Get(snapshot, "AvSyncEncoderCorrectionSamples", string.Empty);
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
        var rendererMode = Get(snapshot, "PreviewRendererMode");
        builder.AppendLine($"Renderer: {rendererMode} | Startup: {Get(snapshot, "PreviewStartupState")} | First Visual: {Get(snapshot, "PreviewFirstVisualConfirmed")}");
        if (rendererMode == "GpuMediaSource")
        {
            builder.AppendLine($"GPU Playback: {Get(snapshot, "PreviewGpuPlaybackState")} | Video: {Get(snapshot, "PreviewGpuNaturalVideoWidth")}x{Get(snapshot, "PreviewGpuNaturalVideoHeight")} | Position: {Get(snapshot, "PreviewGpuPositionMs")}ms | Events: {Get(snapshot, "PreviewGpuPositionEventCount")}");
        }
        else if (rendererMode == "D3D11VideoProcessor" || rendererMode == "HdrShader")
        {
            builder.AppendLine($"D3D Frames: {Get(snapshot, "PreviewD3DFramesSubmitted")} submitted, {Get(snapshot, "PreviewD3DFramesRendered")} rendered, {Get(snapshot, "PreviewD3DFramesDropped")} dropped");
            builder.AppendLine($"Color: input={Get(snapshot, "PreviewD3DInputColorSpace")} output={Get(snapshot, "PreviewD3DOutputColorSpace")}");
            builder.AppendLine($"Cadence: {Get(snapshot, "PreviewCadenceObservedFps")} fps");
        }
        else
        {
            builder.AppendLine($"Frames: {Get(snapshot, "PreviewFramesArrived")} arrived, {Get(snapshot, "PreviewFramesDisplayed")} displayed, {Get(snapshot, "PreviewFramesDropped")} dropped");
            builder.AppendLine($"Cadence: {Get(snapshot, "PreviewCadenceObservedFps")} fps");
        }

        builder.AppendLine();
        builder.AppendLine("== Source ==");
        var sourceFrameRate = Get(snapshot, "DetectedSourceFrameRate", string.Empty);
        var sourceFrameRateArg = Get(snapshot, "DetectedSourceFrameRateArg", string.Empty);
        var sourceFpsSummary = !string.IsNullOrWhiteSpace(sourceFrameRateArg)
            ? $"{sourceFrameRate}fps ({sourceFrameRateArg})"
            : !string.IsNullOrWhiteSpace(sourceFrameRate)
                ? $"{sourceFrameRate}fps"
                : "N/A";
        builder.AppendLine($"Source: {Get(snapshot, "SourceWidth")} x {Get(snapshot, "SourceHeight")} @ {sourceFpsSummary} HDR={Get(snapshot, "SourceIsHdr")}");
        builder.AppendLine($"Telemetry: {Get(snapshot, "SourceTelemetryAvailability")} ({Get(snapshot, "SourceTelemetryConfidence")})");

        return builder.ToString().TrimEnd();
    }

    public static string FormatDiagnostics(JsonElement response)
    {
        if (!TryGetData(response, out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return Get(response, "Message", "No diagnostics available.");
        }

        var builder = new StringBuilder();
        builder.AppendLine("== Diagnostics ==");
        var count = 0;
        foreach (var item in data.EnumerateArray())
        {
            count++;
            var correlation = Get(item, "CorrelationId", string.Empty);
            var correlationSuffix = string.IsNullOrWhiteSpace(correlation) ? string.Empty : $" [{correlation}]";
            builder.AppendLine(
                $"{Get(item, "TimestampUtc", "?")} [{Get(item, "Severity", "Info")}] [{Get(item, "Category", "System")}] {Get(item, "Message", string.Empty)}{correlationSuffix}");
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
            return Get(response, "Message", "Capture options not available.");
        }

        var builder = new StringBuilder();
        builder.AppendLine("== Capture Options ==");
        builder.AppendLine($"Selected Device: {Get(data, "SelectedDeviceId")}");
        builder.AppendLine($"Selected Audio Input: {Get(data, "SelectedAudioInputDeviceId")}");
        builder.AppendLine($"Resolution: {Get(data, "SelectedResolution")} | Frame Rate: {Get(data, "SelectedFrameRate")}");
        builder.AppendLine($"Format: {Get(data, "SelectedRecordingFormat")} | Quality: {Get(data, "SelectedQuality")} | Preset: {Get(data, "SelectedPreset")}");
        builder.AppendLine($"Split Encode: {Get(data, "SelectedSplitEncodeMode")} | Video Format: {Get(data, "SelectedVideoFormat")} | MJPEG Decoders: {Get(data, "MjpegDecoderCount")}");
        builder.AppendLine($"Show All Options: {Get(data, "ShowAllCaptureOptions")} | Preview Volume: {Get(data, "PreviewVolumePercent")}% | Stats Visible: {Get(data, "IsStatsVisible")}");
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
            return Get(response, "Message", "Capture options not available.");
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
            return Get(response, "Message", "No timeline data available.");
        }

        var entries = new List<TimelineRow>();
        foreach (var item in data.EnumerateArray())
        {
            entries.Add(new TimelineRow
            {
                Timestamp = Get(item, "TimestampUtc"),
                CaptureFps = GetDouble(item, "CaptureFps"),
                PreviewFps = GetDouble(item, "PreviewFps"),
                VidQueue = GetInt(item, "VideoQueueDepth"),
                VidDrops = GetLong(item, "VideoDrops"),
                P95Ms = GetDouble(item, "CaptureCadenceP95Ms"),
                LatencyMs = GetLong(item, "PipelineLatencyMs"),
                WorkingMb = GetDouble(item, "MemoryWorkingSetMb"),
                ManagedMb = GetDouble(item, "MemoryManagedHeapMb"),
                Gen0 = GetInt(item, "GcGen0Collections"),
                Gen1 = GetInt(item, "GcGen1Collections"),
                Gen2 = GetInt(item, "GcGen2Collections"),
                GcPause = GetDouble(item, "GcPauseTimePercent"),
                Workers = GetInt(item, "ThreadPoolWorkerAvailable"),
                IoThreads = GetInt(item, "ThreadPoolIoAvailable")
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
            return Get(response, "Message", "Snapshot data not available.");
        }

        var builder = new StringBuilder();
        builder.AppendLine("== Memory & GC ==");
        builder.AppendLine($"Working Set: {Get(snapshot, "MemoryWorkingSetMb")} MB");
        builder.AppendLine($"Private Bytes: {Get(snapshot, "MemoryPrivateBytesMb")} MB");
        builder.AppendLine($"Managed Heap: {Get(snapshot, "MemoryManagedHeapMb")} MB");
        builder.AppendLine($"Total Allocated: {Get(snapshot, "MemoryTotalAllocatedMb")} MB");
        builder.AppendLine($"GC Heap Size: {Get(snapshot, "MemoryGcHeapSizeMb")} MB");
        builder.AppendLine($"GC Collections: Gen0={Get(snapshot, "MemoryGcGen0Collections")} Gen1={Get(snapshot, "MemoryGcGen1Collections")} Gen2={Get(snapshot, "MemoryGcGen2Collections")}");
        builder.AppendLine($"GC Pause: {Get(snapshot, "MemoryGcPauseTimePercent")}% | Fragmentation: {Get(snapshot, "MemoryGcFragmentationPercent")}%");
        builder.AppendLine($"ThreadPool Workers: {Get(snapshot, "ThreadPoolWorkerAvailable")}/{Get(snapshot, "ThreadPoolWorkerMax")} avail");
        builder.AppendLine($"ThreadPool IO: {Get(snapshot, "ThreadPoolIoAvailable")}/{Get(snapshot, "ThreadPoolIoMax")} avail");
        return builder.ToString().TrimEnd();
    }

    public static string FormatResult(JsonElement response, bool includeData)
    {
        if (!includeData || !TryGetData(response, out var data) || data.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return Get(response, "Message", "Command completed.");
        }

        return $"{Get(response, "Message", "Command completed.")}{Environment.NewLine}{PrettyJson(data)}";
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
            var prefix = Get(item, "IsSelected", "false").Equals("true", StringComparison.OrdinalIgnoreCase) ? "*" : "-";
            var idSuffix = includeId ? $" ({Get(item, "Id")})" : string.Empty;
            builder.AppendLine($"{prefix} {Get(item, "Name")}{idSuffix}");
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
            builder.AppendLine($"{GetSelectionPrefix(item)} {Get(item, "Value")} ({Get(item, "Width")}x{Get(item, "Height")}){GetDisableSuffix(item)}");
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
                $"{GetSelectionPrefix(item)} {Get(item, "FriendlyValue")} fps ({Get(item, "Value")} exact, {Get(item, "ExactValueArg")}){GetDisableSuffix(item)}");
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
            builder.AppendLine($"{GetSelectionPrefix(item)} {Get(item, "Label", Get(item, "Value"))}{GetDisableSuffix(item)}");
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
            builder.AppendLine($"{GetSelectionPrefix(item)} {Get(item, "Value")}{GetDisableSuffix(item)}");
        }
    }

    private static string GetSelectionPrefix(JsonElement item)
        => Get(item, "IsSelected", "false").Equals("true", StringComparison.OrdinalIgnoreCase) ? "*" : "-";

    private static string GetDisableSuffix(JsonElement item)
    {
        var isEnabled = Get(item, "IsEnabled", "true");
        if (isEnabled.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return $" [disabled: {Get(item, "DisableReason", "Unavailable")}]";
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

    private static double GetDouble(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object &&
               element.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.Number &&
               value.TryGetDouble(out var result)
            ? result
            : 0;
    }

    private static int GetInt(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object &&
               element.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.Number &&
               value.TryGetInt32(out var result)
            ? result
            : 0;
    }

    private static long GetLong(JsonElement element, string propertyName, long fallback = 0)
    {
        return element.ValueKind == JsonValueKind.Object &&
               element.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.Number &&
               value.TryGetInt64(out var result)
            ? result
            : fallback;
    }

    private static long FormatTickAgeMs(long tickMs)
    {
        if (tickMs <= 0)
        {
            return -1;
        }

        return Math.Max(0, Environment.TickCount64 - tickMs);
    }

    public static string Get(JsonElement element, string propertyName, string fallback = "N/A")
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var value))
        {
            return fallback;
        }

        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return fallback;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? fallback,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.Array => value.GetArrayLength() == 0 ? fallback : value.ToString(),
            JsonValueKind.Object => value.ToString(),
            _ => fallback
        };
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
