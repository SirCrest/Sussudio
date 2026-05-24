using System;
using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

// Converts broad automation snapshots into terse console text. The formatter is
// intentionally tolerant of missing JSON properties so old/new app builds can
// still be inspected during live investigations.
internal static partial class AutomationSnapshotFormatter
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

        var builder = new StringBuilder();
        AppendStateSection(builder, snapshot);
        AppendCaptureSettingsSection(builder, snapshot);
        AppendAudioSection(builder, snapshot);
        AppendVideoPipelineSection(builder, snapshot);
        AppendRecordingSection(builder, snapshot);
        if (includeFlashback)
        {
            AppendFlashbackSection(builder, snapshot);
        }

        AppendDiagnosticsSection(builder, snapshot);
        AppendPerformanceSection(builder, snapshot);
        AppendMemorySection(builder, snapshot);
        AppendCaptureCadenceSection(builder, snapshot);
        return builder.ToString().TrimEnd();
    }

    private static void AppendStateSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Sussudio State ==");
        builder.AppendLine($"Status: {Get(snapshot, "SessionState")} | {Get(snapshot, "StatusText")}");
        builder.AppendLine($"Capture Commands: pending={Get(snapshot, "CaptureCommandPendingCommands")} maxPending={Get(snapshot, "CaptureCommandMaxPendingCommands")} oldestAge={Get(snapshot, "CaptureCommandOldestPendingCommandAgeMs")}ms lastLatency={Get(snapshot, "CaptureCommandLastQueueLatencyMs")}ms maxLatency={Get(snapshot, "CaptureCommandMaxQueueLatencyMs")}ms enq={Get(snapshot, "CaptureCommandCommandsEnqueued")} done={Get(snapshot, "CaptureCommandCommandsCompleted")} fail={Get(snapshot, "CaptureCommandCommandsFailed")} cancel={Get(snapshot, "CaptureCommandCommandsCanceled")} coalesced={Get(snapshot, "CaptureCommandCommandsCoalesced")} last={Get(snapshot, "CaptureCommandLastCommand", "None")} outcome={Get(snapshot, "CaptureCommandLastOutcome", "None")} corr={Get(snapshot, "CaptureCommandLastCorrelationId", "")} error={Get(snapshot, "CaptureCommandLastError", "")}");
        builder.AppendLine($"Device: {Get(snapshot, "SelectedDeviceName")} ({Get(snapshot, "SelectedDeviceId")})");
        builder.AppendLine($"Initialized: {Get(snapshot, "IsInitialized")} | Previewing: {Get(snapshot, "IsPreviewing")} | Recording: {Get(snapshot, "IsRecording")}");
        builder.AppendLine();
    }

    private static void AppendCaptureSettingsSection(StringBuilder builder, JsonElement snapshot)
    {
        var frameRateSummary = FormatFrameRateSummary(snapshot);

        builder.AppendLine("== Capture Settings ==");
        builder.AppendLine($"Resolution: {Get(snapshot, "SelectedResolution")} | Frame Rate: {frameRateSummary}");
        builder.AppendLine($"Format: {Get(snapshot, "SelectedRecordingFormat")} | Quality: {Get(snapshot, "SelectedQuality")} | Preset: {Get(snapshot, "SelectedPreset")}");
        builder.AppendLine($"Video Format: {Get(snapshot, "SelectedVideoFormat")} | Split Encode: {Get(snapshot, "SelectedSplitEncodeMode")} | MJPEG Decoders: {Get(snapshot, "MjpegDecoderCount")}");
        builder.AppendLine($"HDR: {Get(snapshot, "IsHdrEnabled")} (Available: {Get(snapshot, "IsHdrAvailable")}, Active: {Get(snapshot, "HdrOutputActive")}, State: {Get(snapshot, "HdrRuntimeState")})");
        builder.AppendLine($"Pipeline: Requested={Get(snapshot, "RequestedPipelineMode")} Active={Get(snapshot, "ActivePipelineMode")} Matched={Get(snapshot, "PipelineModeMatched")}");
        builder.AppendLine($"UI: Preview Volume={Get(snapshot, "PreviewVolumePercent")}% | Stats Visible={Get(snapshot, "IsStatsVisible")}");
        builder.AppendLine();
    }

    private static string FormatFrameRateSummary(JsonElement snapshot)
    {
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

        return string.IsNullOrWhiteSpace(frameRateExactDetail)
            ? $"{frameRateBucket} fps"
            : $"{frameRateBucket} fps ({frameRateExactDetail})";
    }

    private static void AppendAudioSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Audio ==");
        builder.AppendLine($"Enabled: {Get(snapshot, "IsAudioEnabled")} | Preview: {Get(snapshot, "IsAudioPreviewEnabled")} | Custom Input: {Get(snapshot, "IsCustomAudioInputEnabled")}");
        builder.AppendLine($"Peak: {Get(snapshot, "AudioPeak")} | Clipping: {Get(snapshot, "AudioClipping")} | Signal: {Get(snapshot, "AudioSignalPresent")}");
        builder.AppendLine($"Reader: {Get(snapshot, "AudioReaderActive")} | Frames: {Get(snapshot, "AudioFramesArrived")} arrived, {Get(snapshot, "AudioFramesWrittenToSink")} to sink");
        builder.AppendLine();
    }

    private static void AppendVideoPipelineSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Video Pipeline ==");
        builder.AppendLine($"Reader: {Get(snapshot, "VideoReaderActive")} | Ingest: {Get(snapshot, "IngestVideoFramesArrived")} arrived, {Get(snapshot, "IngestVideoFramesWrittenToSink")} to sink");
        builder.AppendLine($"Encoder: {Get(snapshot, "EncoderVideoFramesEnqueued")} enqueued, {Get(snapshot, "EncoderVideoFramesEncoded")} encoded | Queue: {Get(snapshot, "FfmpegVideoQueueDepth")}/{Get(snapshot, "RecordingVideoQueueCapacity")} depth, max={Get(snapshot, "RecordingVideoQueueMaxDepth")} overloads={Get(snapshot, "VideoDropsQueueSaturated")}");
        builder.AppendLine($"Recording Detail: submitted={Get(snapshot, "RecordingVideoFramesSubmittedToEncoder")} packets={Get(snapshot, "RecordingVideoEncoderPacketsWritten")} pts={Get(snapshot, "RecordingVideoEncoderPts")} encoderDrops={Get(snapshot, "RecordingVideoEncoderDroppedFrames")} seqGaps={Get(snapshot, "RecordingVideoSequenceGaps")}");
        builder.AppendLine($"Recording Queue Latency: oldest={Get(snapshot, "RecordingVideoQueueOldestFrameAgeMs")}ms last={Get(snapshot, "RecordingVideoQueueLastLatencyMs")}ms avg={Get(snapshot, "RecordingVideoQueueLatencyAvgMs")}ms P95={Get(snapshot, "RecordingVideoQueueLatencyP95Ms")}ms P99={Get(snapshot, "RecordingVideoQueueLatencyP99Ms")}ms max={Get(snapshot, "RecordingVideoQueueLatencyMaxMs")}ms samples={Get(snapshot, "RecordingVideoQueueLatencySampleCount")}");
        builder.AppendLine($"Recording Backpressure: total={Get(snapshot, "RecordingVideoBackpressureWaitMs")}ms events={Get(snapshot, "RecordingVideoBackpressureEvents")} last={Get(snapshot, "RecordingVideoBackpressureLastWaitMs")}ms max={Get(snapshot, "RecordingVideoBackpressureMaxWaitMs")}ms");
        builder.AppendLine($"Encoder Failure: active={Get(snapshot, "RecordingEncodingFailed")} type={Get(snapshot, "RecordingEncodingFailureType", "None")} msg={Get(snapshot, "RecordingEncodingFailureMessage", "")}");
        builder.AppendLine($"GPU Queue: {Get(snapshot, "RecordingGpuQueueDepth")}/{Get(snapshot, "RecordingGpuQueueCapacity")} max={Get(snapshot, "RecordingGpuQueueMaxDepth")} enq={Get(snapshot, "RecordingGpuFramesEnqueued")} overloads={Get(snapshot, "RecordingGpuFramesDropped")} | CUDA: {Get(snapshot, "RecordingCudaQueueDepth")}/{Get(snapshot, "RecordingCudaQueueCapacity")} max={Get(snapshot, "RecordingCudaQueueMaxDepth")} enq={Get(snapshot, "RecordingCudaFramesEnqueued")} overloads={Get(snapshot, "RecordingCudaFramesDropped")}");
        builder.AppendLine($"Freshness: reader {Get(snapshot, "IngestLastVideoFrameAgeMs")}ms | enqueue {Get(snapshot, "EncoderLastEnqueueAgeMs")}ms | write {Get(snapshot, "EncoderLastWriteAgeMs")}ms");
        builder.AppendLine($"Diagnostics: MemPref={Get(snapshot, "MemoryPreference")} ReqSubtype={Get(snapshot, "VideoRequestedSubtype")} NegSubtype={Get(snapshot, "VideoNegotiatedSubtype")} Errors={Get(snapshot, "VideoIngestErrorCount")}");
        AppendThreadHealthSection(builder, snapshot);
        builder.AppendLine();
    }

    private static void AppendThreadHealthSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine();
        builder.AppendLine("== Thread Health ==");
        AppendSourceReaderThreadHealthLine(builder, snapshot);
        AppendWasapiCaptureThreadHealthLine(builder, snapshot);
        AppendWasapiPlaybackThreadHealthLine(builder, snapshot);
    }

    private static void AppendSourceReaderThreadHealthLine(StringBuilder builder, JsonElement snapshot)
    {
        var sourceReaderLastFrameAgeMs = ComputeTickAgeMs(GetLong(snapshot, "SourceReaderLastFrameTickMs"));
        var sourceReaderOutstanding = Get(snapshot, "SourceReaderReadOutstanding");
        var sourceReaderOutstandingSuffix = string.Equals(sourceReaderOutstanding, "true", StringComparison.OrdinalIgnoreCase)
            ? $" outstandingFor={Get(snapshot, "SourceReaderReadOutstandingMs")}ms"
            : string.Empty;
        builder.AppendLine(
            $"Source Reader: outstanding={sourceReaderOutstanding}{sourceReaderOutstandingSuffix} " +
            $"lastFrame={sourceReaderLastFrameAgeMs}ms ago channelDepth={Get(snapshot, "SourceReaderFrameChannelDepth")}");
    }

    private static void AppendWasapiCaptureThreadHealthLine(StringBuilder builder, JsonElement snapshot)
    {
        var wasapiCaptureLastCallbackAgeMs = ComputeTickAgeMs(GetLong(snapshot, "WasapiCaptureLastCallbackTickMs"));
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
    }

    private static void AppendWasapiPlaybackThreadHealthLine(StringBuilder builder, JsonElement snapshot)
    {
        var wasapiPlaybackLastRenderAgeMs = ComputeTickAgeMs(GetLong(snapshot, "WasapiPlaybackLastRenderTickMs"));
        builder.AppendLine(
            $"WASAPI Playback: callbacks={Get(snapshot, "WasapiPlaybackRenderCallbackCount")} " +
            $"silence={Get(snapshot, "WasapiPlaybackRenderSilenceCount")} " +
            $"queueDepth={Get(snapshot, "WasapiPlaybackQueueDepth")} " +
            $"queueMs={Get(snapshot, "WasapiPlaybackQueueDurationMs")} " +
            $"activeMs={Get(snapshot, "WasapiPlaybackActiveChunkDurationMs")} " +
            $"endpointMs={Get(snapshot, "WasapiPlaybackEndpointQueuedDurationMs")} " +
            $"bufferedMs={Get(snapshot, "WasapiPlaybackBufferedDurationMs")} " +
            $"streamLatencyMs={Get(snapshot, "WasapiPlaybackStreamLatencyMs")} " +
            $"drops={Get(snapshot, "WasapiPlaybackQueueDropCount")} " +
            $"lastCallback={wasapiPlaybackLastRenderAgeMs}ms ago");
    }

    private static void AppendRecordingSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Recording ==");
        builder.AppendLine($"Recording: {Get(snapshot, "IsRecording")} | Output: {Get(snapshot, "OutputPath")}");
        builder.AppendLine($"Time: {Get(snapshot, "RecordingTime")} | Size: {Get(snapshot, "RecordingSizeInfo")} | Bitrate: {Get(snapshot, "RecordingBitrateInfo")}");
        builder.AppendLine($"Backend: {Get(snapshot, "RecordingBackend")} | Audio Path: {Get(snapshot, "AudioPathMode")} | Mux: {Get(snapshot, "MuxResult")}");
        builder.AppendLine($"Integrity: {Get(snapshot, "RecordingIntegrityStatus")} complete={Get(snapshot, "RecordingIntegrityComplete")} backend={Get(snapshot, "RecordingIntegrityBackend")} source={Get(snapshot, "RecordingIntegritySourceFrames")} accepted={Get(snapshot, "RecordingIntegrityAcceptedFrames")} boundaryDrops={Get(snapshot, "RecordingIntegrityPipelineDroppedFrames")} queueDrops={Get(snapshot, "RecordingIntegrityQueueDroppedFrames")} encoderDrops={Get(snapshot, "RecordingIntegrityEncoderDroppedFrames")} seqGaps={Get(snapshot, "RecordingIntegritySequenceGaps")} submitted={Get(snapshot, "RecordingIntegritySubmittedFrames")} encoded={Get(snapshot, "RecordingIntegrityEncodedFrames")} packets={Get(snapshot, "RecordingIntegrityPacketsWritten")} qMax={Get(snapshot, "RecordingIntegrityQueueMaxDepth")} qOldestMs={Get(snapshot, "RecordingIntegrityQueueOldestFrameAgeMs")} backpressure={Get(snapshot, "RecordingIntegrityBackpressureWaitMs")}ms/{Get(snapshot, "RecordingIntegrityBackpressureEvents")} max={Get(snapshot, "RecordingIntegrityBackpressureMaxWaitMs")}ms reason={Get(snapshot, "RecordingIntegrityReason", "")}");
        builder.AppendLine($"Audio Integrity: {Get(snapshot, "RecordingIntegrityAudioStatus")} enabled={Get(snapshot, "RecordingIntegrityAudioEnabled")} active={Get(snapshot, "RecordingIntegrityAudioCaptureActive")} arrived={Get(snapshot, "RecordingIntegrityAudioFramesArrived")} written={Get(snapshot, "RecordingIntegrityAudioFramesWrittenToSink")} encoded={Get(snapshot, "RecordingIntegrityAudioSamplesEncoded")} drops={Get(snapshot, "RecordingIntegrityAudioDropEvents")} disc={Get(snapshot, "RecordingIntegrityAudioDiscontinuities")} tsErr={Get(snapshot, "RecordingIntegrityAudioTimestampErrors")} gaps={Get(snapshot, "RecordingIntegrityAudioCallbackGaps")} drift={Get(snapshot, "RecordingIntegrityAvSyncDriftMs", "N/A")}ms encoderDrift={Get(snapshot, "RecordingIntegrityEncoderAvSyncDriftMs", "N/A")}ms corr={Get(snapshot, "RecordingIntegrityEncoderAvSyncCorrectionSamples", "N/A")}");
        builder.AppendLine($"Last Output: {Get(snapshot, "LastOutputPath")} ({Get(snapshot, "LastOutputSizeBytes")} bytes) Finalize: {Get(snapshot, "LastFinalizeStatus")}");
        builder.AppendLine();
    }

    private static void AppendDiagnosticsSection(StringBuilder builder, JsonElement snapshot)
    {
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
    }

    private static void AppendPerformanceSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Performance ==");
        builder.AppendLine($"Legacy Score: {Get(snapshot, "PerformanceScore")} | Perfection: {Get(snapshot, "PerformancePerfectionMet")}");
        builder.AppendLine($"Legacy Summary: {Get(snapshot, "PerformanceSummary")}");
        builder.AppendLine($"Pipeline Latency: {Get(snapshot, "EstimatedPipelineLatencyMs")}ms (app receive -> estimated visible)");
        builder.AppendLine();
    }

    private static void AppendMemorySection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Memory & GC ==");
        builder.AppendLine($"Process CPU: {Get(snapshot, "ProcessCpuPercent")}% | CPU Time: {Get(snapshot, "ProcessCpuTotalProcessorTimeMs")}ms");
        builder.AppendLine($"Working Set: {Get(snapshot, "MemoryWorkingSetMb")} MB | Private: {Get(snapshot, "MemoryPrivateBytesMb")} MB | Managed Heap: {Get(snapshot, "MemoryManagedHeapMb")} MB");
        builder.AppendLine($"Total Allocated: {Get(snapshot, "MemoryTotalAllocatedMb")} MB | GC Heap: {Get(snapshot, "MemoryGcHeapSizeMb")} MB");
        builder.AppendLine($"GC Collections: Gen0={Get(snapshot, "MemoryGcGen0Collections")} Gen1={Get(snapshot, "MemoryGcGen1Collections")} Gen2={Get(snapshot, "MemoryGcGen2Collections")}");
        builder.AppendLine($"GC Pause: {Get(snapshot, "MemoryGcPauseTimePercent")}% | Fragmentation: {Get(snapshot, "MemoryGcFragmentationPercent")}%");
        builder.AppendLine($"ThreadPool Workers: {Get(snapshot, "ThreadPoolWorkerAvailable")}/{Get(snapshot, "ThreadPoolWorkerMax")} avail | IO: {Get(snapshot, "ThreadPoolIoAvailable")}/{Get(snapshot, "ThreadPoolIoMax")} avail");
        builder.AppendLine();
    }
}
