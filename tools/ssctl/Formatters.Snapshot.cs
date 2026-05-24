using System.Text;
using System.Text.Json;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class Formatters
{
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

        var builder = new StringBuilder();
        AppendSnapshotStateSection(builder, snapshot);
        builder.AppendLine();
        AppendSnapshotCaptureSettingsSection(builder, snapshot);
        builder.AppendLine();
        AppendSnapshotAudioSection(builder, snapshot);
        builder.AppendLine();
        AppendSnapshotVideoPipelineSection(builder, snapshot);
        AppendSnapshotThreadHealthSection(builder, snapshot);
        builder.AppendLine();
        AppendSnapshotRecordingSection(builder, snapshot);
        builder.AppendLine();
        AppendSnapshotFlashbackSection(builder, snapshot);
        AppendSnapshotDiagnosticLanesSection(builder, snapshot);
        builder.AppendLine();
        AppendSnapshotPerformanceSection(builder, snapshot);
        builder.AppendLine();
        AppendSnapshotMemorySection(builder, snapshot);
        builder.AppendLine();
        AppendSnapshotCaptureCadenceSection(builder, snapshot);
        AppendSnapshotMjpegTimingSection(builder, snapshot);
        AppendSnapshotAvSyncSection(builder, snapshot);
        AppendSnapshotPreviewSection(builder, snapshot);
        AppendSnapshotSourceSection(builder, snapshot);

        return builder.ToString().TrimEnd();
    }

    private static void AppendSnapshotStateSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Sussudio State ==");
        builder.AppendLine($"Status: {AutomationSnapshotFormatter.Get(snapshot, "SessionState")} | {AutomationSnapshotFormatter.Get(snapshot, "StatusText")}");
        builder.AppendLine($"Capture Commands: pending={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandPendingCommands")} maxPending={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandMaxPendingCommands")} oldestAge={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandOldestPendingCommandAgeMs")}ms lastLatency={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandLastQueueLatencyMs")}ms maxLatency={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandMaxQueueLatencyMs")}ms enq={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandCommandsEnqueued")} done={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandCommandsCompleted")} fail={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandCommandsFailed")} cancel={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandCommandsCanceled")} coalesced={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandCommandsCoalesced")} last={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandLastCommand", "None")} outcome={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandLastOutcome", "None")} corr={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandLastCorrelationId", "")} error={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandLastError", "")}");
        builder.AppendLine($"Device: {AutomationSnapshotFormatter.Get(snapshot, "SelectedDeviceName")} ({AutomationSnapshotFormatter.Get(snapshot, "SelectedDeviceId")})");
        builder.AppendLine($"Initialized: {AutomationSnapshotFormatter.Get(snapshot, "IsInitialized")} | Previewing: {AutomationSnapshotFormatter.Get(snapshot, "IsPreviewing")} | Recording: {AutomationSnapshotFormatter.Get(snapshot, "IsRecording")}");
    }

    private static void AppendSnapshotAudioSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Audio ==");
        builder.AppendLine($"Enabled: {AutomationSnapshotFormatter.Get(snapshot, "IsAudioEnabled")} | Preview: {AutomationSnapshotFormatter.Get(snapshot, "IsAudioPreviewEnabled")} | Custom Input: {AutomationSnapshotFormatter.Get(snapshot, "IsCustomAudioInputEnabled")}");
        builder.AppendLine($"Peak: {AutomationSnapshotFormatter.Get(snapshot, "AudioPeak")} | Clipping: {AutomationSnapshotFormatter.Get(snapshot, "AudioClipping")} | Signal: {AutomationSnapshotFormatter.Get(snapshot, "AudioSignalPresent")}");
        builder.AppendLine($"Reader: {AutomationSnapshotFormatter.Get(snapshot, "AudioReaderActive")} | Frames: {AutomationSnapshotFormatter.Get(snapshot, "AudioFramesArrived")} arrived, {AutomationSnapshotFormatter.Get(snapshot, "AudioFramesWrittenToSink")} to sink");
    }

    private static void AppendSnapshotCaptureSettingsSection(StringBuilder builder, JsonElement snapshot)
    {
        var frameRateSummary = FormatSnapshotFrameRateSummary(snapshot);

        builder.AppendLine("== Capture Settings ==");
        builder.AppendLine($"Resolution: {AutomationSnapshotFormatter.Get(snapshot, "SelectedResolution")} | Frame Rate: {frameRateSummary}");
        builder.AppendLine($"Format: {AutomationSnapshotFormatter.Get(snapshot, "SelectedRecordingFormat")} | Quality: {AutomationSnapshotFormatter.Get(snapshot, "SelectedQuality")} | Preset: {AutomationSnapshotFormatter.Get(snapshot, "SelectedPreset")}");
        builder.AppendLine($"Video Format: {AutomationSnapshotFormatter.Get(snapshot, "SelectedVideoFormat")} | Split Encode: {AutomationSnapshotFormatter.Get(snapshot, "SelectedSplitEncodeMode")} | MJPEG Decoders: {AutomationSnapshotFormatter.Get(snapshot, "MjpegDecoderCount")}");
        builder.AppendLine($"HDR: {AutomationSnapshotFormatter.Get(snapshot, "IsHdrEnabled")} (Available: {AutomationSnapshotFormatter.Get(snapshot, "IsHdrAvailable")}, Active: {AutomationSnapshotFormatter.Get(snapshot, "HdrOutputActive")}, State: {AutomationSnapshotFormatter.Get(snapshot, "HdrRuntimeState")})");
        builder.AppendLine($"Pipeline: Requested={AutomationSnapshotFormatter.Get(snapshot, "RequestedPipelineMode")} Active={AutomationSnapshotFormatter.Get(snapshot, "ActivePipelineMode")} Matched={AutomationSnapshotFormatter.Get(snapshot, "PipelineModeMatched")}");
        builder.AppendLine($"UI: Preview Volume={AutomationSnapshotFormatter.Get(snapshot, "PreviewVolumePercent")}% | Stats Visible={AutomationSnapshotFormatter.Get(snapshot, "IsStatsVisible")}");
    }

    private static string FormatSnapshotFrameRateSummary(JsonElement snapshot)
    {
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
        return string.IsNullOrWhiteSpace(frameRateExactDetail)
            ? $"{frameRateBucket} fps"
            : $"{frameRateBucket} fps ({frameRateExactDetail})";
    }

    private static void AppendSnapshotVideoPipelineSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Video Pipeline ==");
        builder.AppendLine($"Reader: {AutomationSnapshotFormatter.Get(snapshot, "VideoReaderActive")} | Ingest: {AutomationSnapshotFormatter.Get(snapshot, "IngestVideoFramesArrived")} arrived, {AutomationSnapshotFormatter.Get(snapshot, "IngestVideoFramesWrittenToSink")} to sink");
        builder.AppendLine($"Encoder: {AutomationSnapshotFormatter.Get(snapshot, "EncoderVideoFramesEnqueued")} enqueued, {AutomationSnapshotFormatter.Get(snapshot, "EncoderVideoFramesEncoded")} encoded | Queue: {AutomationSnapshotFormatter.Get(snapshot, "FfmpegVideoQueueDepth")}/{AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueCapacity")} depth, max={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueMaxDepth")} overloads={AutomationSnapshotFormatter.Get(snapshot, "VideoDropsQueueSaturated")}");
        builder.AppendLine($"Recording Detail: submitted={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoFramesSubmittedToEncoder")} packets={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoEncoderPacketsWritten")} pts={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoEncoderPts")} encoderDrops={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoEncoderDroppedFrames")} seqGaps={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoSequenceGaps")}");
        builder.AppendLine($"Recording Queue Latency: oldest={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueOldestFrameAgeMs")}ms last={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueLastLatencyMs")}ms avg={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueLatencyAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueLatencyP95Ms")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueLatencyP99Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueLatencyMaxMs")}ms samples={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueLatencySampleCount")}");
        builder.AppendLine($"Recording Backpressure: total={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoBackpressureWaitMs")}ms events={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoBackpressureEvents")} last={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoBackpressureLastWaitMs")}ms max={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoBackpressureMaxWaitMs")}ms");
        builder.AppendLine($"Encoder Failure: active={AutomationSnapshotFormatter.Get(snapshot, "RecordingEncodingFailed")} type={AutomationSnapshotFormatter.Get(snapshot, "RecordingEncodingFailureType", "None")} msg={AutomationSnapshotFormatter.Get(snapshot, "RecordingEncodingFailureMessage", "")}");
        builder.AppendLine($"GPU Queue: {AutomationSnapshotFormatter.Get(snapshot, "RecordingGpuQueueDepth")}/{AutomationSnapshotFormatter.Get(snapshot, "RecordingGpuQueueCapacity")} max={AutomationSnapshotFormatter.Get(snapshot, "RecordingGpuQueueMaxDepth")} enq={AutomationSnapshotFormatter.Get(snapshot, "RecordingGpuFramesEnqueued")} overloads={AutomationSnapshotFormatter.Get(snapshot, "RecordingGpuFramesDropped")} | CUDA: {AutomationSnapshotFormatter.Get(snapshot, "RecordingCudaQueueDepth")}/{AutomationSnapshotFormatter.Get(snapshot, "RecordingCudaQueueCapacity")} max={AutomationSnapshotFormatter.Get(snapshot, "RecordingCudaQueueMaxDepth")} enq={AutomationSnapshotFormatter.Get(snapshot, "RecordingCudaFramesEnqueued")} overloads={AutomationSnapshotFormatter.Get(snapshot, "RecordingCudaFramesDropped")}");
        builder.AppendLine($"Freshness: reader {AutomationSnapshotFormatter.Get(snapshot, "IngestLastVideoFrameAgeMs")}ms | enqueue {AutomationSnapshotFormatter.Get(snapshot, "EncoderLastEnqueueAgeMs")}ms | write {AutomationSnapshotFormatter.Get(snapshot, "EncoderLastWriteAgeMs")}ms");
        builder.AppendLine($"Diagnostics: MemPref={AutomationSnapshotFormatter.Get(snapshot, "MemoryPreference")} ReqSubtype={AutomationSnapshotFormatter.Get(snapshot, "VideoRequestedSubtype")} NegSubtype={AutomationSnapshotFormatter.Get(snapshot, "VideoNegotiatedSubtype")} Errors={AutomationSnapshotFormatter.Get(snapshot, "VideoIngestErrorCount")}");
    }

    private static void AppendSnapshotRecordingSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Recording ==");
        builder.AppendLine($"Recording: {AutomationSnapshotFormatter.Get(snapshot, "IsRecording")} | Output: {AutomationSnapshotFormatter.Get(snapshot, "OutputPath")}");
        builder.AppendLine($"Time: {AutomationSnapshotFormatter.Get(snapshot, "RecordingTime")} | Size: {AutomationSnapshotFormatter.Get(snapshot, "RecordingSizeInfo")} | Bitrate: {AutomationSnapshotFormatter.Get(snapshot, "RecordingBitrateInfo")}");
        builder.AppendLine($"Backend: {AutomationSnapshotFormatter.Get(snapshot, "RecordingBackend")} | Audio Path: {AutomationSnapshotFormatter.Get(snapshot, "AudioPathMode")} | Mux: {AutomationSnapshotFormatter.Get(snapshot, "MuxResult")}");
        builder.AppendLine($"Integrity: {AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityStatus")} complete={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityComplete")} backend={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityBackend")} source={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegritySourceFrames")} accepted={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAcceptedFrames")} boundaryDrops={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityPipelineDroppedFrames")} queueDrops={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityQueueDroppedFrames")} encoderDrops={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityEncoderDroppedFrames")} seqGaps={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegritySequenceGaps")} submitted={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegritySubmittedFrames")} encoded={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityEncodedFrames")} packets={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityPacketsWritten")} qMax={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityQueueMaxDepth")} qOldestMs={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityQueueOldestFrameAgeMs")} backpressure={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityBackpressureWaitMs")}ms/{AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityBackpressureEvents")} max={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityBackpressureMaxWaitMs")}ms reason={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityReason", "")}");
        builder.AppendLine($"Audio Integrity: {AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioStatus")} enabled={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioEnabled")} active={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioCaptureActive")} arrived={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioFramesArrived")} written={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioFramesWrittenToSink")} encoded={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioSamplesEncoded")} drops={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioDropEvents")} disc={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioDiscontinuities")} tsErr={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioTimestampErrors")} gaps={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioCallbackGaps")} drift={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAvSyncDriftMs", "N/A")}ms encoderDrift={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityEncoderAvSyncDriftMs", "N/A")}ms corr={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityEncoderAvSyncCorrectionSamples", "N/A")}");
        builder.AppendLine($"Last Output: {AutomationSnapshotFormatter.Get(snapshot, "LastOutputPath")} ({AutomationSnapshotFormatter.Get(snapshot, "LastOutputSizeBytes")} bytes) Finalize: {AutomationSnapshotFormatter.Get(snapshot, "LastFinalizeStatus")}");
    }

    private static void AppendSnapshotDiagnosticLanesSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Diagnostics ==");
        builder.AppendLine($"Health: {AutomationSnapshotFormatter.Get(snapshot, "DiagnosticHealthStatus")} | Stage: {AutomationSnapshotFormatter.Get(snapshot, "DiagnosticLikelyStage")}");
        builder.AppendLine($"Summary: {AutomationSnapshotFormatter.Get(snapshot, "DiagnosticSummary")}");
        builder.AppendLine($"Evidence: {AutomationSnapshotFormatter.Get(snapshot, "DiagnosticEvidence")}");
        builder.AppendLine("Frame Lanes:");
        builder.AppendLine($"  Source: {AutomationSnapshotFormatter.Get(snapshot, "DiagnosticSourceLane")}");
        builder.AppendLine($"  Decode: {AutomationSnapshotFormatter.Get(snapshot, "DiagnosticDecodeLane")}");
        builder.AppendLine($"  Preview: {AutomationSnapshotFormatter.Get(snapshot, "DiagnosticPreviewLane")}");
        builder.AppendLine($"  Render: {AutomationSnapshotFormatter.Get(snapshot, "DiagnosticRenderLane")}");
        builder.AppendLine($"  Present: {AutomationSnapshotFormatter.Get(snapshot, "DiagnosticPresentLane")}");
        builder.AppendLine($"  Recording: {AutomationSnapshotFormatter.Get(snapshot, "DiagnosticRecordingLane")}");
        builder.AppendLine($"  Audio: {AutomationSnapshotFormatter.Get(snapshot, "DiagnosticAudioLane")}");
    }

    private static void AppendSnapshotPerformanceSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Performance ==");
        builder.AppendLine($"Legacy Score: {AutomationSnapshotFormatter.Get(snapshot, "PerformanceScore")} | Perfection: {AutomationSnapshotFormatter.Get(snapshot, "PerformancePerfectionMet")}");
        builder.AppendLine($"Legacy Summary: {AutomationSnapshotFormatter.Get(snapshot, "PerformanceSummary")}");
        builder.AppendLine($"Pipeline Latency: {AutomationSnapshotFormatter.Get(snapshot, "EstimatedPipelineLatencyMs")}ms (app receive -> estimated visible)");
    }

    private static void AppendSnapshotMemorySection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Memory & GC ==");
        builder.AppendLine($"Process CPU: {AutomationSnapshotFormatter.Get(snapshot, "ProcessCpuPercent")}% | CPU Time: {AutomationSnapshotFormatter.Get(snapshot, "ProcessCpuTotalProcessorTimeMs")}ms");
        builder.AppendLine($"Working Set: {AutomationSnapshotFormatter.Get(snapshot, "MemoryWorkingSetMb")} MB | Private: {AutomationSnapshotFormatter.Get(snapshot, "MemoryPrivateBytesMb")} MB | Managed Heap: {AutomationSnapshotFormatter.Get(snapshot, "MemoryManagedHeapMb")} MB");
        builder.AppendLine($"Total Allocated: {AutomationSnapshotFormatter.Get(snapshot, "MemoryTotalAllocatedMb")} MB | GC Heap: {AutomationSnapshotFormatter.Get(snapshot, "MemoryGcHeapSizeMb")} MB");
        builder.AppendLine($"GC Collections: Gen0={AutomationSnapshotFormatter.Get(snapshot, "MemoryGcGen0Collections")} Gen1={AutomationSnapshotFormatter.Get(snapshot, "MemoryGcGen1Collections")} Gen2={AutomationSnapshotFormatter.Get(snapshot, "MemoryGcGen2Collections")}");
        builder.AppendLine($"GC Pause: {AutomationSnapshotFormatter.Get(snapshot, "MemoryGcPauseTimePercent")}% | Fragmentation: {AutomationSnapshotFormatter.Get(snapshot, "MemoryGcFragmentationPercent")}%");
        builder.AppendLine($"ThreadPool Workers: {AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolWorkerAvailable")}/{AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolWorkerMax")} avail | IO: {AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolIoAvailable")}/{AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolIoMax")} avail");
    }

    private static void AppendSnapshotCaptureCadenceSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Capture Cadence ==");
        builder.AppendLine($"Frame Time: target={AutomationSnapshotFormatter.FormatFrameBudgetMs(snapshot, "ExpectedCaptureFrameRate")} avg={AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceAverageIntervalMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceP95IntervalMs")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceP99IntervalMs")}ms max={AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceMaxIntervalMs")}ms | Samples: {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceSampleCount")} over {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceSampleDurationMs")}ms");
        builder.AppendLine($"Average Rate: {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceObservedFps")} fps (expected {AutomationSnapshotFormatter.Get(snapshot, "ExpectedCaptureFrameRate")} fps)");
        builder.AppendLine($"5% Low: {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceFivePercentLowFps")} fps | 1% Low: {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceOnePercentLowFps")} fps");
        builder.AppendLine($"Jitter: {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceJitterStdDevMs")}ms | Gaps: {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceSevereGapCount")} | Est Drops: {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceEstimatedDroppedFrames")} ({AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceEstimatedDropPercent")}%)");
        builder.AppendLine($"MJPEG Packet Fingerprint: input={AutomationSnapshotFormatter.Get(snapshot, "MjpegPacketHashInputObservedFps")} fps unique={AutomationSnapshotFormatter.Get(snapshot, "MjpegPacketHashUniqueObservedFps")} fps dup={AutomationSnapshotFormatter.Get(snapshot, "MjpegPacketHashDuplicateFramePercent")}% pattern={AutomationSnapshotFormatter.Get(snapshot, "MjpegPacketHashPattern")} longestDup={AutomationSnapshotFormatter.Get(snapshot, "MjpegPacketHashLongestDuplicateRun")}");
        builder.AppendLine($"Sampled Decoded Crop: changes={AutomationSnapshotFormatter.Get(snapshot, "VisualCadenceChangeObservedFps")} fps output={AutomationSnapshotFormatter.Get(snapshot, "VisualCadenceOutputObservedFps")} fps repeat={AutomationSnapshotFormatter.Get(snapshot, "VisualCadenceRepeatFramePercent")}% avgChangedPx={AutomationSnapshotFormatter.Get(snapshot, "VisualCadenceAverageDelta")} changedPxPct={AutomationSnapshotFormatter.Get(snapshot, "VisualCadenceMotionScore")} confidence={AutomationSnapshotFormatter.Get(snapshot, "VisualCadenceMotionConfidence")}");
        builder.AppendLine($"Sampled Tight Crop: changes={AutomationSnapshotFormatter.Get(snapshot, "VisualCenterCadenceChangeObservedFps")} fps output={AutomationSnapshotFormatter.Get(snapshot, "VisualCenterCadenceOutputObservedFps")} fps repeat={AutomationSnapshotFormatter.Get(snapshot, "VisualCenterCadenceRepeatFramePercent")}% avgChangedPx={AutomationSnapshotFormatter.Get(snapshot, "VisualCenterCadenceAverageDelta")} changedPxPct={AutomationSnapshotFormatter.Get(snapshot, "VisualCenterCadenceMotionScore")} confidence={AutomationSnapshotFormatter.Get(snapshot, "VisualCenterCadenceMotionConfidence")}");
    }

    private static void AppendSnapshotAvSyncSection(StringBuilder builder, JsonElement snapshot)
    {
        var avSyncDrift = AutomationSnapshotFormatter.Get(snapshot, "AvSyncCaptureDriftMs", string.Empty);
        var avSyncRate = AutomationSnapshotFormatter.Get(snapshot, "AvSyncCaptureDriftRateMsPerSec", string.Empty);
        var avSyncEncoder = AutomationSnapshotFormatter.Get(snapshot, "AvSyncEncoderDriftMs", string.Empty);
        var avSyncCorr = AutomationSnapshotFormatter.Get(snapshot, "AvSyncEncoderCorrectionSamples", string.Empty);
        if (string.IsNullOrWhiteSpace(avSyncDrift) && string.IsNullOrWhiteSpace(avSyncEncoder))
        {
            return;
        }

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

    private static void AppendSnapshotPreviewSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine();
        builder.AppendLine("== Preview ==");
        var rendererMode = AutomationSnapshotFormatter.Get(snapshot, "PreviewRendererMode");
        builder.AppendLine($"Renderer: {rendererMode} | Startup: {AutomationSnapshotFormatter.Get(snapshot, "PreviewStartupState")} | First Visual: {AutomationSnapshotFormatter.Get(snapshot, "PreviewFirstVisualConfirmed")}");
        if (rendererMode == "GpuMediaSource")
        {
            builder.AppendLine($"GPU Playback: {AutomationSnapshotFormatter.Get(snapshot, "PreviewGpuPlaybackState")} | Video: {AutomationSnapshotFormatter.Get(snapshot, "PreviewGpuNaturalVideoWidth")}x{AutomationSnapshotFormatter.Get(snapshot, "PreviewGpuNaturalVideoHeight")} | Position: {AutomationSnapshotFormatter.Get(snapshot, "PreviewGpuPositionMs")}ms | Events: {AutomationSnapshotFormatter.Get(snapshot, "PreviewGpuPositionEventCount")}");
        }
        else if (IsSnapshotPreviewD3DRendererMode(rendererMode))
        {
            AppendSnapshotPreviewD3DSection(builder, snapshot);
        }
        else
        {
            builder.AppendLine($"Frames: {AutomationSnapshotFormatter.Get(snapshot, "PreviewFramesArrived")} arrived, {AutomationSnapshotFormatter.Get(snapshot, "PreviewFramesDisplayed")} displayed, {AutomationSnapshotFormatter.Get(snapshot, "PreviewFramesDropped")} dropped");
            builder.AppendLine($"Average Rate: {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceObservedFps")} fps | 5% Low: {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceFivePercentLowFps")} fps | 1% Low: {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceOnePercentLowFps")} fps | Samples: {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceSampleCount")} over {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceSampleDurationMs")}ms");
            builder.AppendLine($"Pacing Classifier: stage={AutomationSnapshotFormatter.Get(snapshot, "PreviewPacingLikelySlowStage")} confidence={AutomationSnapshotFormatter.Get(snapshot, "PreviewPacingSlowStageConfidence")} evidence={AutomationSnapshotFormatter.Get(snapshot, "PreviewPacingSlowStageEvidence", "")}");
        }
    }

    private static void AppendSnapshotSourceSection(StringBuilder builder, JsonElement snapshot)
    {
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
    }
}
