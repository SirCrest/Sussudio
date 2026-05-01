using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace ElgatoCapture.Tools;

internal static class AutomationSnapshotFormatter
{
    internal static string FormatSnapshot(JsonElement snapshotResponse, bool includeFlashback = false)
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
        builder.AppendLine($"Encoder: {Get(snapshot, "EncoderVideoFramesEnqueued")} enqueued, {Get(snapshot, "EncoderVideoFramesEncoded")} encoded | Queue: {Get(snapshot, "FfmpegVideoQueueDepth")}/{Get(snapshot, "RecordingVideoQueueCapacity")} depth, max={Get(snapshot, "RecordingVideoQueueMaxDepth")} overloads={Get(snapshot, "VideoDropsQueueSaturated")}");
        builder.AppendLine($"Recording Detail: submitted={Get(snapshot, "RecordingVideoFramesSubmittedToEncoder")} packets={Get(snapshot, "RecordingVideoEncoderPacketsWritten")} pts={Get(snapshot, "RecordingVideoEncoderPts")} encoderDrops={Get(snapshot, "RecordingVideoEncoderDroppedFrames")} seqGaps={Get(snapshot, "RecordingVideoSequenceGaps")}");
        builder.AppendLine($"Recording Queue Latency: oldest={Get(snapshot, "RecordingVideoQueueOldestFrameAgeMs")}ms last={Get(snapshot, "RecordingVideoQueueLastLatencyMs")}ms avg={Get(snapshot, "RecordingVideoQueueLatencyAvgMs")}ms P95={Get(snapshot, "RecordingVideoQueueLatencyP95Ms")}ms max={Get(snapshot, "RecordingVideoQueueLatencyMaxMs")}ms samples={Get(snapshot, "RecordingVideoQueueLatencySampleCount")}");
        builder.AppendLine($"Recording Backpressure: total={Get(snapshot, "RecordingVideoBackpressureWaitMs")}ms events={Get(snapshot, "RecordingVideoBackpressureEvents")} last={Get(snapshot, "RecordingVideoBackpressureLastWaitMs")}ms max={Get(snapshot, "RecordingVideoBackpressureMaxWaitMs")}ms");
        builder.AppendLine($"Encoder Failure: active={Get(snapshot, "RecordingEncodingFailed")} type={Get(snapshot, "RecordingEncodingFailureType", "None")} msg={Get(snapshot, "RecordingEncodingFailureMessage", "")}");
        builder.AppendLine($"GPU Queue: {Get(snapshot, "RecordingGpuQueueDepth")}/{Get(snapshot, "RecordingGpuQueueCapacity")} max={Get(snapshot, "RecordingGpuQueueMaxDepth")} enq={Get(snapshot, "RecordingGpuFramesEnqueued")} overloads={Get(snapshot, "RecordingGpuFramesDropped")} | CUDA: {Get(snapshot, "RecordingCudaQueueDepth")}/{Get(snapshot, "RecordingCudaQueueCapacity")} max={Get(snapshot, "RecordingCudaQueueMaxDepth")} enq={Get(snapshot, "RecordingCudaFramesEnqueued")} overloads={Get(snapshot, "RecordingCudaFramesDropped")}");
        builder.AppendLine($"Freshness: reader {Get(snapshot, "IngestLastVideoFrameAgeMs")}ms | enqueue {Get(snapshot, "EncoderLastEnqueueAgeMs")}ms | write {Get(snapshot, "EncoderLastWriteAgeMs")}ms");
        builder.AppendLine($"Diagnostics: MemPref={Get(snapshot, "MemoryPreference")} ReqSubtype={Get(snapshot, "VideoRequestedSubtype")} NegSubtype={Get(snapshot, "VideoNegotiatedSubtype")} Errors={Get(snapshot, "VideoIngestErrorCount")}");
        builder.AppendLine();
        builder.AppendLine("== Thread Health ==");
        var sourceReaderLastFrameAgeMs = ComputeTickAgeMs(GetLong(snapshot, "SourceReaderLastFrameTickMs"));
        var wasapiCaptureLastCallbackAgeMs = ComputeTickAgeMs(GetLong(snapshot, "WasapiCaptureLastCallbackTickMs"));
        var wasapiPlaybackLastRenderAgeMs = ComputeTickAgeMs(GetLong(snapshot, "WasapiPlaybackLastRenderTickMs"));
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
            $"levelEvents={Get(snapshot, "WasapiCaptureAudioLevelEventsFired")} " +
            $"glitches={Get(snapshot, "WasapiCaptureAudioGlitchCount")} " +
            $"disc={Get(snapshot, "WasapiCaptureAudioDiscontinuityCount")} " +
            $"tsErr={Get(snapshot, "WasapiCaptureAudioTimestampErrorCount")} " +
            $"severeGaps={Get(snapshot, "WasapiCaptureCallbackSevereGapCount")}");
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
        builder.AppendLine($"Integrity: {Get(snapshot, "RecordingIntegrityStatus")} complete={Get(snapshot, "RecordingIntegrityComplete")} backend={Get(snapshot, "RecordingIntegrityBackend")} source={Get(snapshot, "RecordingIntegritySourceFrames")} accepted={Get(snapshot, "RecordingIntegrityAcceptedFrames")} boundaryDrops={Get(snapshot, "RecordingIntegrityPipelineDroppedFrames")} queueDrops={Get(snapshot, "RecordingIntegrityQueueDroppedFrames")} encoderDrops={Get(snapshot, "RecordingIntegrityEncoderDroppedFrames")} seqGaps={Get(snapshot, "RecordingIntegritySequenceGaps")} submitted={Get(snapshot, "RecordingIntegritySubmittedFrames")} encoded={Get(snapshot, "RecordingIntegrityEncodedFrames")} packets={Get(snapshot, "RecordingIntegrityPacketsWritten")} qMax={Get(snapshot, "RecordingIntegrityQueueMaxDepth")} qOldestMs={Get(snapshot, "RecordingIntegrityQueueOldestFrameAgeMs")} backpressure={Get(snapshot, "RecordingIntegrityBackpressureWaitMs")}ms/{Get(snapshot, "RecordingIntegrityBackpressureEvents")} max={Get(snapshot, "RecordingIntegrityBackpressureMaxWaitMs")}ms reason={Get(snapshot, "RecordingIntegrityReason", "")}");
        builder.AppendLine($"Audio Integrity: {Get(snapshot, "RecordingIntegrityAudioStatus")} enabled={Get(snapshot, "RecordingIntegrityAudioEnabled")} active={Get(snapshot, "RecordingIntegrityAudioCaptureActive")} arrived={Get(snapshot, "RecordingIntegrityAudioFramesArrived")} written={Get(snapshot, "RecordingIntegrityAudioFramesWrittenToSink")} encoded={Get(snapshot, "RecordingIntegrityAudioSamplesEncoded")} drops={Get(snapshot, "RecordingIntegrityAudioDropEvents")} disc={Get(snapshot, "RecordingIntegrityAudioDiscontinuities")} tsErr={Get(snapshot, "RecordingIntegrityAudioTimestampErrors")} gaps={Get(snapshot, "RecordingIntegrityAudioCallbackGaps")} drift={Get(snapshot, "RecordingIntegrityAvSyncDriftMs", "N/A")}ms encoderDrift={Get(snapshot, "RecordingIntegrityEncoderAvSyncDriftMs", "N/A")}ms corr={Get(snapshot, "RecordingIntegrityEncoderAvSyncCorrectionSamples", "N/A")}");
        builder.AppendLine($"Last Output: {Get(snapshot, "LastOutputPath")} ({Get(snapshot, "LastOutputSizeBytes")} bytes) Finalize: {Get(snapshot, "LastFinalizeStatus")}");
        builder.AppendLine();
        if (includeFlashback)
        {
            AppendFlashbackSection(builder, snapshot);
        }

        builder.AppendLine("== Diagnostics ==");
        builder.AppendLine($"Health: {Get(snapshot, "DiagnosticHealthStatus")} | Stage: {Get(snapshot, "DiagnosticLikelyStage")}");
        builder.AppendLine($"Summary: {Get(snapshot, "DiagnosticSummary")}");
        builder.AppendLine($"Evidence: {Get(snapshot, "DiagnosticEvidence")}");
        builder.AppendLine("Frame Lanes:");
        builder.AppendLine($"  Source: {Get(snapshot, "DiagnosticSourceLane")}");
        builder.AppendLine($"  Decode: {Get(snapshot, "DiagnosticDecodeLane")}");
        builder.AppendLine($"  Preview: {Get(snapshot, "DiagnosticPreviewLane")}");
        builder.AppendLine($"  Render: {Get(snapshot, "DiagnosticRenderLane")}");
        builder.AppendLine($"  Present: {Get(snapshot, "DiagnosticPresentLane")}");
        builder.AppendLine($"  Recording: {Get(snapshot, "DiagnosticRecordingLane")}");
        builder.AppendLine($"  Audio: {Get(snapshot, "DiagnosticAudioLane")}");
        builder.AppendLine();
        builder.AppendLine("== Performance ==");
        builder.AppendLine($"Legacy Score: {Get(snapshot, "PerformanceScore")} | Perfection: {Get(snapshot, "PerformancePerfectionMet")}");
        builder.AppendLine($"Legacy Summary: {Get(snapshot, "PerformanceSummary")}");
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
        builder.AppendLine($"Frame Time: target={FormatFrameBudgetMs(snapshot, "ExpectedCaptureFrameRate")} avg={Get(snapshot, "CaptureCadenceAverageIntervalMs")}ms P95={Get(snapshot, "CaptureCadenceP95IntervalMs")}ms P99={Get(snapshot, "CaptureCadenceP99IntervalMs")}ms max={Get(snapshot, "CaptureCadenceMaxIntervalMs")}ms | Samples: {Get(snapshot, "CaptureCadenceSampleCount")}");
        builder.AppendLine($"Average Rate: {Get(snapshot, "CaptureCadenceObservedFps")} fps (expected {Get(snapshot, "ExpectedCaptureFrameRate")} fps)");
        builder.AppendLine($"1% Low: {Get(snapshot, "CaptureCadenceOnePercentLowFps")} fps");
        builder.AppendLine($"Jitter: {Get(snapshot, "CaptureCadenceJitterStdDevMs")}ms | Gaps: {Get(snapshot, "CaptureCadenceSevereGapCount")} | Est Drops: {Get(snapshot, "CaptureCadenceEstimatedDroppedFrames")} ({Get(snapshot, "CaptureCadenceEstimatedDropPercent")}%)");
        builder.AppendLine($"MJPEG Packet Fingerprint: input={Get(snapshot, "MjpegPacketHashInputObservedFps")} fps unique={Get(snapshot, "MjpegPacketHashUniqueObservedFps")} fps dup={Get(snapshot, "MjpegPacketHashDuplicateFramePercent")}% pattern={Get(snapshot, "MjpegPacketHashPattern")} longestDup={Get(snapshot, "MjpegPacketHashLongestDuplicateRun")}");
        builder.AppendLine($"Sampled Decoded Crop: changes={Get(snapshot, "VisualCadenceChangeObservedFps")} fps output={Get(snapshot, "VisualCadenceOutputObservedFps")} fps repeat={Get(snapshot, "VisualCadenceRepeatFramePercent")}% avgChangedPx={Get(snapshot, "VisualCadenceAverageDelta")} changedPxPct={Get(snapshot, "VisualCadenceMotionScore")} confidence={Get(snapshot, "VisualCadenceMotionConfidence")}");
        builder.AppendLine($"Sampled Tight Crop: changes={Get(snapshot, "VisualCenterCadenceChangeObservedFps")} fps output={Get(snapshot, "VisualCenterCadenceOutputObservedFps")} fps repeat={Get(snapshot, "VisualCenterCadenceRepeatFramePercent")}% avgChangedPx={Get(snapshot, "VisualCenterCadenceAverageDelta")} changedPxPct={Get(snapshot, "VisualCenterCadenceMotionScore")} confidence={Get(snapshot, "VisualCenterCadenceMotionConfidence")}");
        AppendMjpegTimingSection(builder, snapshot);
        AppendAvSyncSection(builder, snapshot);
        AppendPreviewSection(builder, snapshot);
        AppendSourceSection(builder, snapshot);
        return builder.ToString().TrimEnd();
    }

    internal static bool IsSuccess(JsonElement response)
    {
        return response.ValueKind == JsonValueKind.Object &&
               response.TryGetProperty("Success", out var success) &&
               success.ValueKind == JsonValueKind.True;
    }

    internal static string Get(JsonElement element, string propertyName, string fallback = "N/A")
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

    internal static int GetInt(JsonElement element, string propertyName, int fallback = 0)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var numeric))
                return numeric;

            if (value.ValueKind == JsonValueKind.String &&
                int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                return parsed;
        }

        return fallback;
    }

    internal static double GetDouble(JsonElement element, string propertyName, double fallback = 0.0)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var numeric))
                return numeric;

            if (value.ValueKind == JsonValueKind.String &&
                double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                return parsed;
        }

        return fallback;
    }

    internal static string FormatFrameBudgetMs(JsonElement element, string fpsPropertyName, string fallback = "N/A")
    {
        var fps = GetDouble(element, fpsPropertyName);
        return fps > 0 ? $"{1000.0 / fps:0.00}ms" : fallback;
    }

    internal static string FormatIntervalMs(JsonElement element, string propertyName, string fallback = "N/A")
    {
        var intervalMs = GetDouble(element, propertyName);
        return intervalMs > 0 ? $"{intervalMs:0.##}ms" : fallback;
    }

    internal static string FormatBytes(long bytes)
    {
        if (bytes < 0)
        {
            return "N/A";
        }

        if (bytes >= 1024L * 1024L * 1024L)
        {
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):0.##} GB";
        }

        if (bytes >= 1024L * 1024L)
        {
            return $"{bytes / (1024.0 * 1024.0):0.##} MB";
        }

        if (bytes >= 1024L)
        {
            return $"{bytes / 1024.0:0.##} KB";
        }

        return $"{bytes} B";
    }

    internal static long GetLong(JsonElement element, string propertyName, long fallback = 0)
    {
        return element.ValueKind == JsonValueKind.Object &&
               element.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.Number &&
               value.TryGetInt64(out var result)
            ? result
            : fallback;
    }

    internal static long? GetNullableLong(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var value) ||
            value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var numeric))
        {
            return numeric;
        }

        return long.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    internal static bool GetBool(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(value.GetString(), out var parsed) && parsed,
            _ => false
        };
    }

    internal static string? GetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var value) ||
            value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : value.ToString();
    }

    internal static long ComputeTickAgeMs(long tickMs)
    {
        if (tickMs <= 0)
        {
            return -1;
        }

        return Math.Max(0, Environment.TickCount64 - tickMs);
    }

    private static void AppendFlashbackSection(StringBuilder builder, JsonElement snapshot)
    {
        var flashbackActive = Get(snapshot, "FlashbackActive", "false");
        var flashbackFailed = Get(snapshot, "FlashbackEncodingFailed", "false");
        if (!flashbackActive.Equals("true", StringComparison.OrdinalIgnoreCase) &&
            !flashbackFailed.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        builder.AppendLine("== Flashback ==");
        var codec = Get(snapshot, "EncoderCodecName");
        if (!string.IsNullOrEmpty(codec))
        {
            var encW = Get(snapshot, "EncoderWidth", "0");
            var encH = Get(snapshot, "EncoderHeight", "0");
            var encFps = Get(snapshot, "EncoderFrameRate", "0");
            var encBitrate = uint.TryParse(Get(snapshot, "EncoderTargetBitRate", "0"), out var br) ? br / 1_000_000.0 : 0;
            builder.AppendLine($"Encoder: {codec} {encW}x{encH} @ {encFps} fps | Target: {encBitrate:0.#} Mbps");
        }
        var bufferedDurationMs = long.TryParse(Get(snapshot, "FlashbackBufferedDurationMs", "0"), out var durationMs)
            ? durationMs
            : 0;
        var diskBytes = long.TryParse(Get(snapshot, "FlashbackDiskBytes", "0"), out var rawDiskBytes)
            ? rawDiskBytes
            : 0;
        var diskMb = diskBytes / (1024.0 * 1024.0);
        builder.AppendLine($"Buffer: {bufferedDurationMs / 1000.0:F1}s | Disk: {diskMb:F1} MB | Written: {FormatBytes(GetLong(snapshot, "FlashbackTotalBytesWritten"))} | GPU Encode: {Get(snapshot, "FlashbackGpuEncoding")}");
        builder.AppendLine($"Temp Cache: cache={FormatBytes(GetLong(snapshot, "FlashbackStartupCacheBytes"))} budget={FormatBytes(GetLong(snapshot, "FlashbackStartupCacheBudgetBytes"))} free={FormatBytes(GetLong(snapshot, "FlashbackTempDriveFreeBytes"))} sessions={Get(snapshot, "FlashbackStartupCacheSessionCount")} deleted={Get(snapshot, "FlashbackStartupCacheDeletedSessionCount")} freed={FormatBytes(GetLong(snapshot, "FlashbackStartupCacheFreedBytes"))} overBudget={Get(snapshot, "FlashbackStartupCacheOverBudget")}");
        builder.AppendLine($"Encoded: {Get(snapshot, "FlashbackEncodedFrames")} frames | Dropped: {Get(snapshot, "FlashbackDroppedFrames")} | VQ: {Get(snapshot, "FlashbackVideoQueueDepth")}/{Get(snapshot, "FlashbackVideoQueueCapacity")} max={Get(snapshot, "FlashbackVideoQueueMaxDepth")} AQ: {Get(snapshot, "FlashbackAudioQueueDepth")}");
        builder.AppendLine($"Flashback Detail: submitted={Get(snapshot, "FlashbackVideoFramesSubmittedToEncoder")} packets={Get(snapshot, "FlashbackVideoEncoderPacketsWritten")} pts={Get(snapshot, "FlashbackVideoEncoderPts")} encoderDrops={Get(snapshot, "FlashbackVideoEncoderDroppedFrames")} seqGaps={Get(snapshot, "FlashbackVideoSequenceGaps")}");
        builder.AppendLine($"Flashback Queue Latency: oldest={Get(snapshot, "FlashbackVideoQueueOldestFrameAgeMs")}ms last={Get(snapshot, "FlashbackVideoQueueLastLatencyMs")}ms avg={Get(snapshot, "FlashbackVideoQueueLatencyAvgMs")}ms P95={Get(snapshot, "FlashbackVideoQueueLatencyP95Ms")}ms max={Get(snapshot, "FlashbackVideoQueueLatencyMaxMs")}ms samples={Get(snapshot, "FlashbackVideoQueueLatencySampleCount")}");
        builder.AppendLine($"Flashback Backpressure: total={Get(snapshot, "FlashbackVideoBackpressureWaitMs")}ms events={Get(snapshot, "FlashbackVideoBackpressureEvents")} last={Get(snapshot, "FlashbackVideoBackpressureLastWaitMs")}ms max={Get(snapshot, "FlashbackVideoBackpressureMaxWaitMs")}ms");
        builder.AppendLine($"Flashback Failure: active={Get(snapshot, "FlashbackEncodingFailed")} type={Get(snapshot, "FlashbackEncodingFailureType", "None")} msg={Get(snapshot, "FlashbackEncodingFailureMessage", "")}");
        builder.AppendLine($"Flashback GPU Queue: {Get(snapshot, "FlashbackGpuQueueDepth")}/{Get(snapshot, "FlashbackGpuQueueCapacity")} max={Get(snapshot, "FlashbackGpuQueueMaxDepth")} enq={Get(snapshot, "FlashbackGpuFramesEnqueued")} overloads={Get(snapshot, "FlashbackGpuFramesDropped")}");
        builder.AppendLine($"Playback: {Get(snapshot, "FlashbackPlaybackState")} | Pos: {Get(snapshot, "FlashbackPlaybackPositionMs")}ms | Decoder: {Get(snapshot, "FlashbackDecoderHwAccel")}");
        builder.AppendLine($"Playback Commands: pending={Get(snapshot, "FlashbackPlaybackPendingCommands")}/{Get(snapshot, "FlashbackPlaybackCommandQueueCapacity")} maxPending={Get(snapshot, "FlashbackPlaybackMaxPendingCommands")} lastLatency={Get(snapshot, "FlashbackPlaybackLastCommandQueueLatencyMs")}ms maxLatency={Get(snapshot, "FlashbackPlaybackMaxCommandQueueLatencyMs")}ms enq={Get(snapshot, "FlashbackPlaybackCommandsEnqueued")} proc={Get(snapshot, "FlashbackPlaybackCommandsProcessed")} drop={Get(snapshot, "FlashbackPlaybackCommandsDropped")} skip={Get(snapshot, "FlashbackPlaybackCommandsSkippedNotReady")} coalescedScrub={Get(snapshot, "FlashbackPlaybackScrubUpdatesCoalesced")} threadAlive={Get(snapshot, "FlashbackPlaybackThreadAlive")} lastQueued={Get(snapshot, "FlashbackPlaybackLastCommandQueued")} lastProcessed={Get(snapshot, "FlashbackPlaybackLastCommandProcessed")} failure={Get(snapshot, "FlashbackPlaybackLastCommandFailure", "")}");
        builder.AppendLine($"Export: active={Get(snapshot, "FlashbackExportActive")} status={Get(snapshot, "FlashbackExportStatus")} id={Get(snapshot, "FlashbackExportId")} progress={Get(snapshot, "FlashbackExportPercent")}% segments={Get(snapshot, "FlashbackExportSegmentsProcessed")}/{Get(snapshot, "FlashbackExportTotalSegments")} elapsed={Get(snapshot, "FlashbackExportElapsedMs")}ms progressAge={Get(snapshot, "FlashbackExportLastProgressAgeMs")}ms bytes={FormatBytes(GetLong(snapshot, "FlashbackExportOutputBytes"))} throughput={FormatBytes((long)GetDouble(snapshot, "FlashbackExportThroughputBytesPerSec"))}/s in={Get(snapshot, "FlashbackExportInPointMs")}ms out={Get(snapshot, "FlashbackExportOutPointMs")}ms lastProgressUtc={Get(snapshot, "FlashbackExportLastProgressUtcUnixMs")} completedUtc={Get(snapshot, "FlashbackExportCompletedUtcUnixMs")} path={Get(snapshot, "FlashbackExportOutputPath")} msg={Get(snapshot, "FlashbackExportMessage", "")}");
        var playbackFps = double.TryParse(Get(snapshot, "FlashbackPlaybackObservedFps", "0"), NumberStyles.Float, CultureInfo.InvariantCulture, out var fps)
            ? fps
            : 0;
        var playbackAvgMs = double.TryParse(Get(snapshot, "FlashbackPlaybackAvgFrameMs", "0"), NumberStyles.Float, CultureInfo.InvariantCulture, out var avgMs)
            ? avgMs
            : 0;
        var avDrift = double.TryParse(Get(snapshot, "FlashbackAvDriftMs", "0"), NumberStyles.Float, CultureInfo.InvariantCulture, out var drift)
            ? drift
            : 0;
        builder.AppendLine($"Playback Frame Time: avg={playbackAvgMs:F2}ms P95={Get(snapshot, "FlashbackPlaybackP95FrameMs")}ms P99={Get(snapshot, "FlashbackPlaybackP99FrameMs")}ms max={Get(snapshot, "FlashbackPlaybackMaxFrameMs")}ms | Average Rate: {playbackFps:F1} fps | 1% Low: {Get(snapshot, "FlashbackPlaybackOnePercentLowFps")} fps | Samples: {Get(snapshot, "FlashbackPlaybackCadenceSampleCount")}");
        builder.AppendLine($"Playback Decode: avg={Get(snapshot, "FlashbackPlaybackDecodeAvgMs")}ms P95={Get(snapshot, "FlashbackPlaybackDecodeP95Ms")}ms P99={Get(snapshot, "FlashbackPlaybackDecodeP99Ms")}ms max={Get(snapshot, "FlashbackPlaybackDecodeMaxMs")}ms samples={Get(snapshot, "FlashbackPlaybackDecodeSampleCount")}");
        builder.AppendLine($"Playback Frames: total={Get(snapshot, "FlashbackPlaybackFrameCount")} late={Get(snapshot, "FlashbackPlaybackLateFrames")} slow={Get(snapshot, "FlashbackPlaybackSlowFrames")} ({Get(snapshot, "FlashbackPlaybackSlowFramePercent")}%) dropped={Get(snapshot, "FlashbackPlaybackDroppedFrames")}");
        builder.AppendLine($"Playback Stages: switches={Get(snapshot, "FlashbackPlaybackSegmentSwitches")} fmp4Reopens={Get(snapshot, "FlashbackPlaybackFmp4Reopens")} writeHeadWaits={Get(snapshot, "FlashbackPlaybackWriteHeadWaits")} nearLiveSnaps={Get(snapshot, "FlashbackPlaybackNearLiveSnaps")} decodeErrorSnaps={Get(snapshot, "FlashbackPlaybackDecodeErrorSnaps")} lastWriteHeadGap={Get(snapshot, "FlashbackPlaybackLastWriteHeadWaitGapMs")}ms");
        builder.AppendLine($"A/V Drift: {avDrift:+0.0;-0.0;0.0}ms (+ = audio ahead) | File: {Get(snapshot, "FlashbackFilePath")}");
        builder.AppendLine();
    }

    private static void AppendMjpegTimingSection(StringBuilder builder, JsonElement snapshot)
    {
        var mjpegDecodeSamples = Get(snapshot, "MjpegDecodeSampleCount", "0");
        var mjpegDecoderCount = Get(snapshot, "MjpegDecoderCount", "0");
        var hasCompressedActivity =
            Get(snapshot, "MjpegCompressedFramesQueued", "0") != "0" ||
            Get(snapshot, "MjpegCompressedFramesDequeued", "0") != "0" ||
            Get(snapshot, "MjpegCompressedDropsQueueFull", "0") != "0" ||
            Get(snapshot, "MjpegCompressedDropsByteBudget", "0") != "0" ||
            Get(snapshot, "MjpegCompressedDropsDisposed", "0") != "0" ||
            Get(snapshot, "MjpegCompressedQueueDepth", "0") != "0" ||
            Get(snapshot, "MjpegCompressedQueueBytes", "0") != "0";
        if (mjpegDecodeSamples == "0" && mjpegDecoderCount == "0" && !hasCompressedActivity)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("== MJPEG Pipeline Timing ==");
        if (mjpegDecodeSamples != "0")
        {
            builder.AppendLine($"Decode: avg={Get(snapshot, "MjpegDecodeAvgMs")}ms P95={Get(snapshot, "MjpegDecodeP95Ms")}ms max={Get(snapshot, "MjpegDecodeMaxMs")}ms ({mjpegDecodeSamples} samples)");
            builder.AppendLine($"Interop Copy: avg={Get(snapshot, "MjpegInteropCopyAvgMs")}ms P95={Get(snapshot, "MjpegInteropCopyP95Ms")}ms max={Get(snapshot, "MjpegInteropCopyMaxMs")}ms ({Get(snapshot, "MjpegInteropCopySampleCount")} samples)");
            builder.AppendLine($"Total Callback: avg={Get(snapshot, "MjpegCallbackAvgMs")}ms P95={Get(snapshot, "MjpegCallbackP95Ms")}ms max={Get(snapshot, "MjpegCallbackMaxMs")}ms ({Get(snapshot, "MjpegCallbackSampleCount")} samples)");
        }

        builder.AppendLine($"Decoders: {mjpegDecoderCount} | Decoded={Get(snapshot, "MjpegTotalDecoded")} Emitted={Get(snapshot, "MjpegTotalEmitted")} Dropped={Get(snapshot, "MjpegTotalDropped")}");
        builder.AppendLine(
            $"Compressed Queue: depth={Get(snapshot, "MjpegCompressedQueueDepth")} bytes={Get(snapshot, "MjpegCompressedQueueBytes")}/{Get(snapshot, "MjpegCompressedQueueByteBudget")} " +
            $"queued={Get(snapshot, "MjpegCompressedFramesQueued")} dequeued={Get(snapshot, "MjpegCompressedFramesDequeued")} " +
            $"drops(full={Get(snapshot, "MjpegCompressedDropsQueueFull")}, budget={Get(snapshot, "MjpegCompressedDropsByteBudget")}, disposed={Get(snapshot, "MjpegCompressedDropsDisposed")})");
        builder.AppendLine(
            $"MJPEG Drop Reasons: decode={Get(snapshot, "MjpegDecodeFailures")} reorderCollision={Get(snapshot, "MjpegReorderCollisions")} emit={Get(snapshot, "MjpegEmitFailures")}");
        builder.AppendLine($"Reorder: avg={Get(snapshot, "MjpegReorderAvgMs")}ms P95={Get(snapshot, "MjpegReorderP95Ms")}ms max={Get(snapshot, "MjpegReorderMaxMs")}ms ({Get(snapshot, "MjpegReorderSampleCount")} samples) | Skips={Get(snapshot, "MjpegReorderSkips")} Buffer={Get(snapshot, "MjpegReorderBufferDepth")}");
        builder.AppendLine($"Pipeline: avg={Get(snapshot, "MjpegPipelineAvgMs")}ms P95={Get(snapshot, "MjpegPipelineP95Ms")}ms max={Get(snapshot, "MjpegPipelineMaxMs")}ms ({Get(snapshot, "MjpegPipelineSampleCount")} samples)");
        if (string.Equals(Get(snapshot, "MjpegPreviewJitterEnabled", "False"), "True", StringComparison.OrdinalIgnoreCase))
        {
            builder.AppendLine(
                $"Preview Jitter: target={Get(snapshot, "MjpegPreviewJitterTargetDepth")} depth={Get(snapshot, "MjpegPreviewJitterQueueDepth")}/{Get(snapshot, "MjpegPreviewJitterMaxDepth")} " +
                $"queued={Get(snapshot, "MjpegPreviewJitterTotalQueued")} submitted={Get(snapshot, "MjpegPreviewJitterTotalSubmitted")} dropped={Get(snapshot, "MjpegPreviewJitterTotalDropped")} " +
                $"deadlineDrops={Get(snapshot, "MjpegPreviewJitterDeadlineDropCount")} underflows={Get(snapshot, "MjpegPreviewJitterUnderflowCount")} " +
                $"target+={Get(snapshot, "MjpegPreviewJitterTargetIncreaseCount")} target-={Get(snapshot, "MjpegPreviewJitterTargetDecreaseCount")}");
            builder.AppendLine(
                $"Preview Jitter Input: avg={Get(snapshot, "MjpegPreviewJitterInputAvgMs")}ms P95={Get(snapshot, "MjpegPreviewJitterInputP95Ms")}ms max={Get(snapshot, "MjpegPreviewJitterInputMaxMs")}ms ({Get(snapshot, "MjpegPreviewJitterInputSampleCount")} samples)");
            builder.AppendLine(
                $"Preview Jitter Output: avg={Get(snapshot, "MjpegPreviewJitterOutputAvgMs")}ms P95={Get(snapshot, "MjpegPreviewJitterOutputP95Ms")}ms max={Get(snapshot, "MjpegPreviewJitterOutputMaxMs")}ms ({Get(snapshot, "MjpegPreviewJitterOutputSampleCount")} samples)");
            builder.AppendLine(
                $"Preview Jitter Latency: avg={Get(snapshot, "MjpegPreviewJitterLatencyAvgMs")}ms P95={Get(snapshot, "MjpegPreviewJitterLatencyP95Ms")}ms max={Get(snapshot, "MjpegPreviewJitterLatencyMaxMs")}ms ({Get(snapshot, "MjpegPreviewJitterLatencySampleCount")} samples)");
            builder.AppendLine(
                $"Preview Jitter Ownership: present={Get(snapshot, "MjpegPreviewJitterLastSelectedPreviewPresentId")} sourceSeq={Get(snapshot, "MjpegPreviewJitterLastSelectedSourceSequenceNumber")} " +
                $"sourceLatency={Get(snapshot, "MjpegPreviewJitterLastSelectedSourceLatencyMs")}ms lastDropSeq={Get(snapshot, "MjpegPreviewJitterLastDroppedSourceSequenceNumber")} reason={Get(snapshot, "MjpegPreviewJitterLastDropReason")}");
        }
        if (!snapshot.TryGetProperty("MjpegPerDecoder", out var perDecoder) ||
            perDecoder.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var worker in perDecoder.EnumerateArray())
        {
            builder.AppendLine(
                $"Decoder[{Get(worker, "WorkerIndex", "?")}]: avg={Get(worker, "AvgMs")}ms " +
                $"P95={Get(worker, "P95Ms")}ms max={Get(worker, "MaxMs")}ms " +
                $"({Get(worker, "SampleCount")} samples)");
        }
    }

    private static void AppendAvSyncSection(StringBuilder builder, JsonElement snapshot)
    {
        var avSyncDrift = Get(snapshot, "AvSyncCaptureDriftMs", string.Empty);
        var avSyncRate = Get(snapshot, "AvSyncCaptureDriftRateMsPerSec", string.Empty);
        var avSyncEncoder = Get(snapshot, "AvSyncEncoderDriftMs", string.Empty);
        var avSyncCorrectionSamples = Get(snapshot, "AvSyncEncoderCorrectionSamples", string.Empty);
        if (string.IsNullOrWhiteSpace(avSyncDrift) && string.IsNullOrWhiteSpace(avSyncEncoder))
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("== AV Sync ==");
        builder.AppendLine(
            $"Capture Drift: {(string.IsNullOrWhiteSpace(avSyncDrift) ? "N/A" : avSyncDrift + "ms")} | " +
            $"Rate: {(string.IsNullOrWhiteSpace(avSyncRate) ? "N/A" : avSyncRate + "ms/s")}");
        if (string.IsNullOrWhiteSpace(avSyncEncoder))
        {
            return;
        }

        builder.AppendLine(
            $"Encoder Drift: {avSyncEncoder}ms | " +
            $"Correction Samples: {(string.IsNullOrWhiteSpace(avSyncCorrectionSamples) ? "N/A" : avSyncCorrectionSamples)}");
    }

    private static void AppendPreviewSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine();
        builder.AppendLine("== Preview ==");
        var rendererMode = Get(snapshot, "PreviewRendererMode");
        builder.AppendLine($"Renderer: {rendererMode} | Startup: {Get(snapshot, "PreviewStartupState")} | First Visual: {Get(snapshot, "PreviewFirstVisualConfirmed")}");
        if (rendererMode == "GpuMediaSource")
        {
            builder.AppendLine($"GPU Playback: {Get(snapshot, "PreviewGpuPlaybackState")} | Video: {Get(snapshot, "PreviewGpuNaturalVideoWidth")}x{Get(snapshot, "PreviewGpuNaturalVideoHeight")} | Position: {Get(snapshot, "PreviewGpuPositionMs")}ms | Events: {Get(snapshot, "PreviewGpuPositionEventCount")}");
            return;
        }

        if (rendererMode == "D3D11VideoProcessor" ||
            rendererMode == "Nv12Shader" ||
            rendererMode == "HdrShader" ||
            rendererMode == "HdrPassthrough")
        {
            builder.AppendLine($"D3D Swap Chain: {Get(snapshot, "PreviewD3DSwapChainAddress", "N/A")}");
            builder.AppendLine($"D3D Frames: {Get(snapshot, "PreviewD3DFramesSubmitted")} submitted, {Get(snapshot, "PreviewD3DFramesRendered")} rendered, {Get(snapshot, "PreviewD3DFramesDropped")} dropped, pending={Get(snapshot, "PreviewD3DPendingFrameCount")}");
            builder.AppendLine($"Color: input={Get(snapshot, "PreviewD3DInputColorSpace")} output={Get(snapshot, "PreviewD3DOutputColorSpace")}");
            builder.AppendLine($"Frame Time: target={FormatIntervalMs(snapshot, "PreviewCadenceExpectedIntervalMs")} avg={Get(snapshot, "PreviewCadenceAverageIntervalMs")}ms P95={Get(snapshot, "PreviewCadenceP95IntervalMs")}ms max={Get(snapshot, "PreviewCadenceMaxIntervalMs")}ms");
            builder.AppendLine($"Average Rate: {Get(snapshot, "PreviewCadenceObservedFps")} fps | 1% Low: {Get(snapshot, "PreviewCadenceOnePercentLowFps")} fps");
            builder.AppendLine($"D3D CPU timing: input/upload avg={Get(snapshot, "PreviewD3DInputUploadCpuAvgMs")}ms P95={Get(snapshot, "PreviewD3DInputUploadCpuP95Ms")}ms P99={Get(snapshot, "PreviewD3DInputUploadCpuP99Ms")}ms max={Get(snapshot, "PreviewD3DInputUploadCpuMaxMs")}ms | render-submit avg={Get(snapshot, "PreviewD3DRenderSubmitCpuAvgMs")}ms P95={Get(snapshot, "PreviewD3DRenderSubmitCpuP95Ms")}ms P99={Get(snapshot, "PreviewD3DRenderSubmitCpuP99Ms")}ms max={Get(snapshot, "PreviewD3DRenderSubmitCpuMaxMs")}ms | present-call avg={Get(snapshot, "PreviewD3DPresentCallAvgMs")}ms P95={Get(snapshot, "PreviewD3DPresentCallP95Ms")}ms P99={Get(snapshot, "PreviewD3DPresentCallP99Ms")}ms max={Get(snapshot, "PreviewD3DPresentCallMaxMs")}ms | total-frame avg={Get(snapshot, "PreviewD3DTotalFrameCpuAvgMs")}ms P95={Get(snapshot, "PreviewD3DTotalFrameCpuP95Ms")}ms P99={Get(snapshot, "PreviewD3DTotalFrameCpuP99Ms")}ms max={Get(snapshot, "PreviewD3DTotalFrameCpuMaxMs")}ms samples={Get(snapshot, "PreviewD3DCpuTimingSampleCount")}");
            builder.AppendLine($"D3D DXGI stats: ok={Get(snapshot, "PreviewD3DFrameStatsSuccessCount")}/{Get(snapshot, "PreviewD3DFrameStatsSampleCount")} failures={Get(snapshot, "PreviewD3DFrameStatsFailureCount")} recentFailures={Get(snapshot, "PreviewD3DFrameStatsRecentFailureCount")} missedRefresh={Get(snapshot, "PreviewD3DFrameStatsMissedRefreshCount")} recentMissed={Get(snapshot, "PreviewD3DFrameStatsRecentMissedRefreshCount")} lastError={Get(snapshot, "PreviewD3DFrameStatsLastError", "")}");
            builder.AppendLine($"D3D Ownership: submitted present={Get(snapshot, "PreviewD3DLastSubmittedPreviewPresentId")} sourceSeq={Get(snapshot, "PreviewD3DLastSubmittedSourceSequenceNumber")} | rendered present={Get(snapshot, "PreviewD3DLastRenderedPreviewPresentId")} sourceSeq={Get(snapshot, "PreviewD3DLastRenderedSourceSequenceNumber")} schedulerToPresent={Get(snapshot, "PreviewD3DLastRenderedSchedulerToPresentMs")}ms | lastDrop={Get(snapshot, "PreviewD3DLastDropReason")}");
            AppendPreviewSlowFrameDiagnostics(builder, snapshot);
            return;
        }

        builder.AppendLine($"Frames: {Get(snapshot, "PreviewFramesArrived")} arrived, {Get(snapshot, "PreviewFramesDisplayed")} displayed, {Get(snapshot, "PreviewFramesDropped")} dropped");
        builder.AppendLine($"Average Rate: {Get(snapshot, "PreviewCadenceObservedFps")} fps | 1% Low: {Get(snapshot, "PreviewCadenceOnePercentLowFps")} fps");
    }

    internal static void AppendPreviewSlowFrameDiagnostics(StringBuilder builder, JsonElement snapshot)
    {
        if (!snapshot.TryGetProperty("PreviewD3DRecentSlowFrames", out var slowFrames) ||
            slowFrames.ValueKind != JsonValueKind.Array ||
            slowFrames.GetArrayLength() <= 0)
        {
            return;
        }

        var lines = new List<string>();
        foreach (var frame in slowFrames.EnumerateArray())
        {
            if (frame.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            lines.Add(
                $"present={Get(frame, "PreviewPresentId")} srcSeq={Get(frame, "SourceSequenceNumber")} " +
                $"reason={Get(frame, "SlowReason")} target={FormatDiagnosticMs(frame, "ExpectedIntervalMs")} " +
                $"over={FormatDiagnosticMs(frame, "WorstOverBudgetMs")} interval={FormatDiagnosticMs(frame, "PresentIntervalMs")} total={FormatDiagnosticMs(frame, "TotalFrameCpuMs")} " +
                $"upload={FormatDiagnosticMs(frame, "InputUploadCpuMs")} render={FormatDiagnosticMs(frame, "RenderSubmitCpuMs")} " +
                $"presentCall={FormatDiagnosticMs(frame, "PresentCallMs")} sched={FormatDiagnosticMs(frame, "SchedulerToPresentMs")} " +
                $"pending={Get(frame, "PendingFrameCount")} dxgiDelta={Get(frame, "DxgiPresentDelta")}/{Get(frame, "DxgiPresentRefreshDelta")}/{Get(frame, "DxgiSyncRefreshDelta")}");
            if (lines.Count >= 3)
            {
                break;
            }
        }

        if (lines.Count > 0)
        {
            builder.AppendLine($"D3D Slow Frames: {string.Join(" | ", lines)}");
        }
    }

    private static string FormatDiagnosticMs(JsonElement element, string propertyName)
    {
        var value = GetDouble(element, propertyName, double.NaN);
        return double.IsFinite(value) ? $"{value:0.00}ms" : "N/A";
    }

    private static void AppendSourceSection(StringBuilder builder, JsonElement snapshot)
    {
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
    }
}
