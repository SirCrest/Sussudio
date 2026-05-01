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
        builder.AppendLine($"Capture Commands: pending={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandPendingCommands")} maxPending={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandMaxPendingCommands")} oldestAge={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandOldestPendingCommandAgeMs")}ms lastLatency={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandLastQueueLatencyMs")}ms maxLatency={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandMaxQueueLatencyMs")}ms enq={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandCommandsEnqueued")} done={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandCommandsCompleted")} fail={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandCommandsFailed")} cancel={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandCommandsCanceled")} last={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandLastCommand", "None")} error={AutomationSnapshotFormatter.Get(snapshot, "CaptureCommandLastError", "")}");
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
        builder.AppendLine($"Encoder: {AutomationSnapshotFormatter.Get(snapshot, "EncoderVideoFramesEnqueued")} enqueued, {AutomationSnapshotFormatter.Get(snapshot, "EncoderVideoFramesEncoded")} encoded | Queue: {AutomationSnapshotFormatter.Get(snapshot, "FfmpegVideoQueueDepth")}/{AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueCapacity")} depth, max={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueMaxDepth")} overloads={AutomationSnapshotFormatter.Get(snapshot, "VideoDropsQueueSaturated")}");
        builder.AppendLine($"Recording Detail: submitted={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoFramesSubmittedToEncoder")} packets={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoEncoderPacketsWritten")} pts={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoEncoderPts")} encoderDrops={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoEncoderDroppedFrames")} seqGaps={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoSequenceGaps")}");
        builder.AppendLine($"Recording Queue Latency: oldest={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueOldestFrameAgeMs")}ms last={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueLastLatencyMs")}ms avg={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueLatencyAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueLatencyP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueLatencyMaxMs")}ms samples={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoQueueLatencySampleCount")}");
        builder.AppendLine($"Recording Backpressure: total={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoBackpressureWaitMs")}ms events={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoBackpressureEvents")} last={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoBackpressureLastWaitMs")}ms max={AutomationSnapshotFormatter.Get(snapshot, "RecordingVideoBackpressureMaxWaitMs")}ms");
        builder.AppendLine($"Encoder Failure: active={AutomationSnapshotFormatter.Get(snapshot, "RecordingEncodingFailed")} type={AutomationSnapshotFormatter.Get(snapshot, "RecordingEncodingFailureType", "None")} msg={AutomationSnapshotFormatter.Get(snapshot, "RecordingEncodingFailureMessage", "")}");
        builder.AppendLine($"GPU Queue: {AutomationSnapshotFormatter.Get(snapshot, "RecordingGpuQueueDepth")}/{AutomationSnapshotFormatter.Get(snapshot, "RecordingGpuQueueCapacity")} max={AutomationSnapshotFormatter.Get(snapshot, "RecordingGpuQueueMaxDepth")} enq={AutomationSnapshotFormatter.Get(snapshot, "RecordingGpuFramesEnqueued")} overloads={AutomationSnapshotFormatter.Get(snapshot, "RecordingGpuFramesDropped")} | CUDA: {AutomationSnapshotFormatter.Get(snapshot, "RecordingCudaQueueDepth")}/{AutomationSnapshotFormatter.Get(snapshot, "RecordingCudaQueueCapacity")} max={AutomationSnapshotFormatter.Get(snapshot, "RecordingCudaQueueMaxDepth")} enq={AutomationSnapshotFormatter.Get(snapshot, "RecordingCudaFramesEnqueued")} overloads={AutomationSnapshotFormatter.Get(snapshot, "RecordingCudaFramesDropped")}");
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
            $"levelEvents={AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureAudioLevelEventsFired")} " +
            $"glitches={AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureAudioGlitchCount")} " +
            $"disc={AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureAudioDiscontinuityCount")} " +
            $"tsErr={AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureAudioTimestampErrorCount")} " +
            $"severeGaps={AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureCallbackSevereGapCount")}");
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
        builder.AppendLine($"Integrity: {AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityStatus")} complete={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityComplete")} backend={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityBackend")} source={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegritySourceFrames")} accepted={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAcceptedFrames")} boundaryDrops={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityPipelineDroppedFrames")} queueDrops={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityQueueDroppedFrames")} encoderDrops={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityEncoderDroppedFrames")} seqGaps={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegritySequenceGaps")} submitted={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegritySubmittedFrames")} encoded={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityEncodedFrames")} packets={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityPacketsWritten")} qMax={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityQueueMaxDepth")} qOldestMs={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityQueueOldestFrameAgeMs")} backpressure={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityBackpressureWaitMs")}ms/{AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityBackpressureEvents")} max={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityBackpressureMaxWaitMs")}ms reason={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityReason", "")}");
        builder.AppendLine($"Audio Integrity: {AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioStatus")} enabled={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioEnabled")} active={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioCaptureActive")} arrived={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioFramesArrived")} written={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioFramesWrittenToSink")} encoded={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioSamplesEncoded")} drops={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioDropEvents")} disc={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioDiscontinuities")} tsErr={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioTimestampErrors")} gaps={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAudioCallbackGaps")} drift={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityAvSyncDriftMs", "N/A")}ms encoderDrift={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityEncoderAvSyncDriftMs", "N/A")}ms corr={AutomationSnapshotFormatter.Get(snapshot, "RecordingIntegrityEncoderAvSyncCorrectionSamples", "N/A")}");
        builder.AppendLine($"Last Output: {AutomationSnapshotFormatter.Get(snapshot, "LastOutputPath")} ({AutomationSnapshotFormatter.Get(snapshot, "LastOutputSizeBytes")} bytes) Finalize: {AutomationSnapshotFormatter.Get(snapshot, "LastFinalizeStatus")}");
        builder.AppendLine();
        var flashbackActive = AutomationSnapshotFormatter.Get(snapshot, "FlashbackActive", "false");
        var flashbackFailed = AutomationSnapshotFormatter.Get(snapshot, "FlashbackEncodingFailed", "false");
        if (flashbackActive.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            flashbackFailed.Equals("true", StringComparison.OrdinalIgnoreCase))
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
            builder.AppendLine($"Buffer: {fbDurationMs / 1000.0:F1}s | Disk: {fbDiskMb:F1} MB | Written: {AutomationSnapshotFormatter.FormatBytes(AutomationSnapshotFormatter.GetLong(snapshot, "FlashbackTotalBytesWritten"))} | GPU Encode: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackGpuEncoding")}");
            builder.AppendLine($"Temp Cache: cache={AutomationSnapshotFormatter.FormatBytes(AutomationSnapshotFormatter.GetLong(snapshot, "FlashbackStartupCacheBytes"))} budget={AutomationSnapshotFormatter.FormatBytes(AutomationSnapshotFormatter.GetLong(snapshot, "FlashbackStartupCacheBudgetBytes"))} free={AutomationSnapshotFormatter.FormatBytes(AutomationSnapshotFormatter.GetLong(snapshot, "FlashbackTempDriveFreeBytes"))} sessions={AutomationSnapshotFormatter.Get(snapshot, "FlashbackStartupCacheSessionCount")} deleted={AutomationSnapshotFormatter.Get(snapshot, "FlashbackStartupCacheDeletedSessionCount")} freed={AutomationSnapshotFormatter.FormatBytes(AutomationSnapshotFormatter.GetLong(snapshot, "FlashbackStartupCacheFreedBytes"))} overBudget={AutomationSnapshotFormatter.Get(snapshot, "FlashbackStartupCacheOverBudget")}");
            builder.AppendLine($"Encoded: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackEncodedFrames")} frames | Dropped: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackDroppedFrames")} | forceRotate={AutomationSnapshotFormatter.Get(snapshot, "FlashbackForceRotateActive")} requested={AutomationSnapshotFormatter.Get(snapshot, "FlashbackForceRotateRequested")} draining={AutomationSnapshotFormatter.Get(snapshot, "FlashbackForceRotateDraining")} | VQ: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueDepth")}/{AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueCapacity")} max={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueMaxDepth")} AQ: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackAudioQueueDepth")}");
            builder.AppendLine($"Flashback Detail: submitted={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoFramesSubmittedToEncoder")} packets={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoEncoderPacketsWritten")} pts={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoEncoderPts")} encoderDrops={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoEncoderDroppedFrames")} seqGaps={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoSequenceGaps")}");
            builder.AppendLine($"Cleanup: fatal={AutomationSnapshotFormatter.Get(snapshot, "FatalCleanupInProgress")} flashback={AutomationSnapshotFormatter.Get(snapshot, "FlashbackCleanupInProgress")}");
            builder.AppendLine($"Flashback Queue Latency: oldest={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueOldestFrameAgeMs")}ms last={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueLastLatencyMs")}ms avg={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueLatencyAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueLatencyP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueLatencyMaxMs")}ms samples={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueLatencySampleCount")} rejected={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueRejectedFrames")} lastReject={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueLastRejectReason", "")}");
            builder.AppendLine($"Flashback Backpressure: total={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoBackpressureWaitMs")}ms events={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoBackpressureEvents")} last={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoBackpressureLastWaitMs")}ms max={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoBackpressureMaxWaitMs")}ms");
            builder.AppendLine($"Flashback Failure: active={AutomationSnapshotFormatter.Get(snapshot, "FlashbackEncodingFailed")} type={AutomationSnapshotFormatter.Get(snapshot, "FlashbackEncodingFailureType", "None")} msg={AutomationSnapshotFormatter.Get(snapshot, "FlashbackEncodingFailureMessage", "")}");
            builder.AppendLine($"Flashback GPU Queue: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackGpuQueueDepth")}/{AutomationSnapshotFormatter.Get(snapshot, "FlashbackGpuQueueCapacity")} max={AutomationSnapshotFormatter.Get(snapshot, "FlashbackGpuQueueMaxDepth")} enq={AutomationSnapshotFormatter.Get(snapshot, "FlashbackGpuFramesEnqueued")} overloads={AutomationSnapshotFormatter.Get(snapshot, "FlashbackGpuFramesDropped")} rejected={AutomationSnapshotFormatter.Get(snapshot, "FlashbackGpuQueueRejectedFrames")} lastReject={AutomationSnapshotFormatter.Get(snapshot, "FlashbackGpuQueueLastRejectReason", "")}");
            builder.AppendLine($"Playback: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackState")} | Pos: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackPositionMs")}ms | Decoder: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackDecoderHwAccel")}");
            builder.AppendLine($"Playback Commands: pending={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackPendingCommands")}/{AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackCommandQueueCapacity")} maxPending={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackMaxPendingCommands")} lastLatency={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastCommandQueueLatencyMs")}ms maxLatency={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackMaxCommandQueueLatencyMs")}ms enq={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackCommandsEnqueued")} proc={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackCommandsProcessed")} drop={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackCommandsDropped")} skip={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackCommandsSkippedNotReady")} coalescedScrub={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackScrubUpdatesCoalesced")} threadAlive={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackThreadAlive")} lastQueued={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastCommandQueued")} lastProcessed={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastCommandProcessed")} failure={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastCommandFailure", "")} failureUtc={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastCommandFailureUtcUnixMs")}");
            builder.AppendLine($"Export: active={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportActive")} status={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportStatus")} id={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportId")} kind={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportFailureKind", "None")} progress={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportPercent")}% segments={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportSegmentsProcessed")}/{AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportTotalSegments")} elapsed={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportElapsedMs")}ms progressAge={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportLastProgressAgeMs")}ms bytes={AutomationSnapshotFormatter.FormatBytes(AutomationSnapshotFormatter.GetLong(snapshot, "FlashbackExportOutputBytes"))} throughput={AutomationSnapshotFormatter.FormatBytes((long)AutomationSnapshotFormatter.GetDouble(snapshot, "FlashbackExportThroughputBytesPerSec"))}/s in={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportInPointMs")}ms out={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportOutPointMs")}ms lastProgressUtc={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportLastProgressUtcUnixMs")} completedUtc={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportCompletedUtcUnixMs")} path={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportOutputPath")} msg={AutomationSnapshotFormatter.Get(snapshot, "FlashbackExportMessage", "")}");
            var pbFps = double.TryParse(AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackObservedFps", "0"), out var fps) ? fps : 0;
            var pbAvgMs = double.TryParse(AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackAvgFrameMs", "0"), out var avgMs) ? avgMs : 0;
            var avDrift = double.TryParse(AutomationSnapshotFormatter.Get(snapshot, "FlashbackAvDriftMs", "0"), out var drift) ? drift : 0;
            builder.AppendLine($"Playback Frame Time: avg={pbAvgMs:F2}ms P95={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackP95FrameMs")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackP99FrameMs")}ms max={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackMaxFrameMs")}ms | Average Rate: {pbFps:F1} fps | 1% Low: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackOnePercentLowFps")} fps | Samples: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackCadenceSampleCount")}");
            builder.AppendLine($"Playback Decode: avg={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackDecodeAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackDecodeP95Ms")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackDecodeP99Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackDecodeMaxMs")}ms samples={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackDecodeSampleCount")}");
            builder.AppendLine($"Playback Frames: total={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackFrameCount")} late={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLateFrames")} slow={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackSlowFrames")} ({AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackSlowFramePercent")}%) dropped={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackDroppedFrames")} lastDrop={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastDropReason", "")} lastDropUtc={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastDropUtcUnixMs")} submitFailures={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackSubmitFailures")} lastSubmitFailure={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastSubmitFailure", "")} lastSubmitFailureUtc={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastSubmitFailureUtcUnixMs")}");
            builder.AppendLine($"Playback Stages: switches={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackSegmentSwitches")} fmp4Reopens={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackFmp4Reopens")} writeHeadWaits={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackWriteHeadWaits")} nearLiveSnaps={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackNearLiveSnaps")} decodeErrorSnaps={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackDecodeErrorSnaps")} lastWriteHeadGap={AutomationSnapshotFormatter.Get(snapshot, "FlashbackPlaybackLastWriteHeadWaitGapMs")}ms");
            builder.AppendLine($"A/V Drift: {avDrift:+0.0;-0.0;0.0}ms (+ = audio ahead) | File: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackFilePath")}");
            builder.AppendLine();
        }

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
        builder.AppendLine();
        builder.AppendLine("== Performance ==");
        builder.AppendLine($"Legacy Score: {AutomationSnapshotFormatter.Get(snapshot, "PerformanceScore")} | Perfection: {AutomationSnapshotFormatter.Get(snapshot, "PerformancePerfectionMet")}");
        builder.AppendLine($"Legacy Summary: {AutomationSnapshotFormatter.Get(snapshot, "PerformanceSummary")}");
        builder.AppendLine($"Pipeline Latency: {AutomationSnapshotFormatter.Get(snapshot, "EstimatedPipelineLatencyMs")}ms (source reader -> present)");
        builder.AppendLine();
        builder.AppendLine("== Memory & GC ==");
        builder.AppendLine($"Process CPU: {AutomationSnapshotFormatter.Get(snapshot, "ProcessCpuPercent")}% | CPU Time: {AutomationSnapshotFormatter.Get(snapshot, "ProcessCpuTotalProcessorTimeMs")}ms");
        builder.AppendLine($"Working Set: {AutomationSnapshotFormatter.Get(snapshot, "MemoryWorkingSetMb")} MB | Private: {AutomationSnapshotFormatter.Get(snapshot, "MemoryPrivateBytesMb")} MB | Managed Heap: {AutomationSnapshotFormatter.Get(snapshot, "MemoryManagedHeapMb")} MB");
        builder.AppendLine($"Total Allocated: {AutomationSnapshotFormatter.Get(snapshot, "MemoryTotalAllocatedMb")} MB | GC Heap: {AutomationSnapshotFormatter.Get(snapshot, "MemoryGcHeapSizeMb")} MB");
        builder.AppendLine($"GC Collections: Gen0={AutomationSnapshotFormatter.Get(snapshot, "MemoryGcGen0Collections")} Gen1={AutomationSnapshotFormatter.Get(snapshot, "MemoryGcGen1Collections")} Gen2={AutomationSnapshotFormatter.Get(snapshot, "MemoryGcGen2Collections")}");
        builder.AppendLine($"GC Pause: {AutomationSnapshotFormatter.Get(snapshot, "MemoryGcPauseTimePercent")}% | Fragmentation: {AutomationSnapshotFormatter.Get(snapshot, "MemoryGcFragmentationPercent")}%");
        builder.AppendLine($"ThreadPool Workers: {AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolWorkerAvailable")}/{AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolWorkerMax")} avail | IO: {AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolIoAvailable")}/{AutomationSnapshotFormatter.Get(snapshot, "ThreadPoolIoMax")} avail");
        builder.AppendLine();
        builder.AppendLine("== Capture Cadence ==");
        builder.AppendLine($"Frame Time: target={AutomationSnapshotFormatter.FormatFrameBudgetMs(snapshot, "ExpectedCaptureFrameRate")} avg={AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceAverageIntervalMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceP95IntervalMs")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceP99IntervalMs")}ms max={AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceMaxIntervalMs")}ms | Samples: {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceSampleCount")}");
        builder.AppendLine($"Average Rate: {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceObservedFps")} fps (expected {AutomationSnapshotFormatter.Get(snapshot, "ExpectedCaptureFrameRate")} fps)");
        builder.AppendLine($"1% Low: {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceOnePercentLowFps")} fps");
        builder.AppendLine($"Jitter: {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceJitterStdDevMs")}ms | Gaps: {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceSevereGapCount")} | Est Drops: {AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceEstimatedDroppedFrames")} ({AutomationSnapshotFormatter.Get(snapshot, "CaptureCadenceEstimatedDropPercent")}%)");
        builder.AppendLine($"MJPEG Packet Fingerprint: input={AutomationSnapshotFormatter.Get(snapshot, "MjpegPacketHashInputObservedFps")} fps unique={AutomationSnapshotFormatter.Get(snapshot, "MjpegPacketHashUniqueObservedFps")} fps dup={AutomationSnapshotFormatter.Get(snapshot, "MjpegPacketHashDuplicateFramePercent")}% pattern={AutomationSnapshotFormatter.Get(snapshot, "MjpegPacketHashPattern")} longestDup={AutomationSnapshotFormatter.Get(snapshot, "MjpegPacketHashLongestDuplicateRun")}");
        builder.AppendLine($"Sampled Decoded Crop: changes={AutomationSnapshotFormatter.Get(snapshot, "VisualCadenceChangeObservedFps")} fps output={AutomationSnapshotFormatter.Get(snapshot, "VisualCadenceOutputObservedFps")} fps repeat={AutomationSnapshotFormatter.Get(snapshot, "VisualCadenceRepeatFramePercent")}% avgChangedPx={AutomationSnapshotFormatter.Get(snapshot, "VisualCadenceAverageDelta")} changedPxPct={AutomationSnapshotFormatter.Get(snapshot, "VisualCadenceMotionScore")} confidence={AutomationSnapshotFormatter.Get(snapshot, "VisualCadenceMotionConfidence")}");
        builder.AppendLine($"Sampled Tight Crop: changes={AutomationSnapshotFormatter.Get(snapshot, "VisualCenterCadenceChangeObservedFps")} fps output={AutomationSnapshotFormatter.Get(snapshot, "VisualCenterCadenceOutputObservedFps")} fps repeat={AutomationSnapshotFormatter.Get(snapshot, "VisualCenterCadenceRepeatFramePercent")}% avgChangedPx={AutomationSnapshotFormatter.Get(snapshot, "VisualCenterCadenceAverageDelta")} changedPxPct={AutomationSnapshotFormatter.Get(snapshot, "VisualCenterCadenceMotionScore")} confidence={AutomationSnapshotFormatter.Get(snapshot, "VisualCenterCadenceMotionConfidence")}");
        var mjpegDecodeSamples = AutomationSnapshotFormatter.Get(snapshot, "MjpegDecodeSampleCount", "0");
        var mjpegDecoderCount = AutomationSnapshotFormatter.Get(snapshot, "MjpegDecoderCount", "0");
        var hasCompressedActivity =
            AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedFramesQueued", "0") != "0" ||
            AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedFramesDequeued", "0") != "0" ||
            AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedDropsQueueFull", "0") != "0" ||
            AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedDropsByteBudget", "0") != "0" ||
            AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedDropsDisposed", "0") != "0" ||
            AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedQueueDepth", "0") != "0" ||
            AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedQueueBytes", "0") != "0";
        if (mjpegDecodeSamples != "0" || mjpegDecoderCount != "0" || hasCompressedActivity)
        {
            builder.AppendLine();
            builder.AppendLine("== MJPEG Pipeline Timing ==");
            if (mjpegDecodeSamples != "0")
            {
                builder.AppendLine($"Decode: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegDecodeAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegDecodeP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegDecodeMaxMs")}ms ({mjpegDecodeSamples} samples)");
                builder.AppendLine($"Interop Copy: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegInteropCopyAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegInteropCopyP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegInteropCopyMaxMs")}ms ({AutomationSnapshotFormatter.Get(snapshot, "MjpegInteropCopySampleCount")} samples)");
                builder.AppendLine($"Total Callback: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegCallbackAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegCallbackP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegCallbackMaxMs")}ms ({AutomationSnapshotFormatter.Get(snapshot, "MjpegCallbackSampleCount")} samples)");
            }

            builder.AppendLine($"Decoders: {mjpegDecoderCount} | Decoded={AutomationSnapshotFormatter.Get(snapshot, "MjpegTotalDecoded")} Emitted={AutomationSnapshotFormatter.Get(snapshot, "MjpegTotalEmitted")} Dropped={AutomationSnapshotFormatter.Get(snapshot, "MjpegTotalDropped")}");
            builder.AppendLine(
                $"Compressed Queue: depth={AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedQueueDepth")} bytes={AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedQueueBytes")}/{AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedQueueByteBudget")} " +
                $"queued={AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedFramesQueued")} dequeued={AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedFramesDequeued")} " +
                $"drops(full={AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedDropsQueueFull")}, budget={AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedDropsByteBudget")}, disposed={AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedDropsDisposed")})");
            builder.AppendLine(
                $"MJPEG Drop Reasons: decode={AutomationSnapshotFormatter.Get(snapshot, "MjpegDecodeFailures")} reorderCollision={AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderCollisions")} emit={AutomationSnapshotFormatter.Get(snapshot, "MjpegEmitFailures")}");
            builder.AppendLine($"Reorder: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderMaxMs")}ms ({AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderSampleCount")} samples) | Skips={AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderSkips")} Buffer={AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderBufferDepth")}");
            builder.AppendLine($"Pipeline: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegPipelineAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegPipelineP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegPipelineMaxMs")}ms ({AutomationSnapshotFormatter.Get(snapshot, "MjpegPipelineSampleCount")} samples)");
            if (string.Equals(AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterEnabled", "False"), "True", StringComparison.OrdinalIgnoreCase))
            {
                builder.AppendLine(
                    $"Preview Jitter: target={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterTargetDepth")} depth={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterQueueDepth")}/{AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterMaxDepth")} " +
                    $"queued={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterTotalQueued")} submitted={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterTotalSubmitted")} dropped={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterTotalDropped")} " +
                    $"deadlineDrops={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterDeadlineDropCount")} underflows={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterUnderflowCount")} " +
                    $"target+={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterTargetIncreaseCount")} target-={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterTargetDecreaseCount")}");
                builder.AppendLine(
                    $"Preview Jitter Input: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterInputAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterInputP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterInputMaxMs")}ms ({AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterInputSampleCount")} samples)");
                builder.AppendLine(
                    $"Preview Jitter Output: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterOutputAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterOutputP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterOutputMaxMs")}ms ({AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterOutputSampleCount")} samples)");
                builder.AppendLine(
                    $"Preview Jitter Latency: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLatencyAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLatencyP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLatencyMaxMs")}ms ({AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLatencySampleCount")} samples)");
                builder.AppendLine(
                    $"Preview Jitter Ownership: present={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastSelectedPreviewPresentId")} sourceSeq={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastSelectedSourceSequenceNumber")} " +
                    $"sourceLatency={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastSelectedSourceLatencyMs")}ms lastDropSeq={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastDroppedSourceSequenceNumber")} reason={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastDropReason")}");
            }
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
        else if (rendererMode == "D3D11VideoProcessor" ||
                 rendererMode == "Nv12Shader" ||
                 rendererMode == "HdrShader" ||
                 rendererMode == "HdrPassthrough")
        {
            builder.AppendLine($"D3D Swap Chain: {AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DSwapChainAddress", "N/A")}");
            builder.AppendLine($"D3D Frames: {AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFramesSubmitted")} submitted, {AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFramesRendered")} rendered, {AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFramesDropped")} dropped, pending={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPendingFrameCount")}");
            builder.AppendLine($"Color: input={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DInputColorSpace")} output={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DOutputColorSpace")}");
            builder.AppendLine($"Frame Time: target={AutomationSnapshotFormatter.FormatIntervalMs(snapshot, "PreviewCadenceExpectedIntervalMs")} avg={AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceAverageIntervalMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceP95IntervalMs")}ms max={AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceMaxIntervalMs")}ms");
            builder.AppendLine($"Average Rate: {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceObservedFps")} fps | 1% Low: {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceOnePercentLowFps")} fps");
            builder.AppendLine($"D3D CPU timing: input/upload avg={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DInputUploadCpuAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DInputUploadCpuP95Ms")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DInputUploadCpuP99Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DInputUploadCpuMaxMs")}ms | render-submit avg={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DRenderSubmitCpuAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DRenderSubmitCpuP95Ms")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DRenderSubmitCpuP99Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DRenderSubmitCpuMaxMs")}ms | present-call avg={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPresentCallAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPresentCallP95Ms")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPresentCallP99Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPresentCallMaxMs")}ms | total-frame avg={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DTotalFrameCpuAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DTotalFrameCpuP95Ms")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DTotalFrameCpuP99Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DTotalFrameCpuMaxMs")}ms samples={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DCpuTimingSampleCount")}");
            builder.AppendLine($"D3D frame-latency wait: enabled={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitEnabled")} handle={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitHandleActive")} calls={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitCallCount")} signaled={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitSignaledCount")} timeouts={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitTimeoutCount")} unexpected={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitUnexpectedResultCount")} lastResult={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitLastResult")} last={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitLastMs")}ms avg={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitMaxMs")}ms samples={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitSampleCount")}");
            builder.AppendLine($"D3D DXGI stats: ok={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameStatsSuccessCount")}/{AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameStatsSampleCount")} failures={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameStatsFailureCount")} recentFailures={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameStatsRecentFailureCount")} missedRefresh={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameStatsMissedRefreshCount")} recentMissed={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameStatsRecentMissedRefreshCount")} lastError={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameStatsLastError", "")}");
            builder.AppendLine($"D3D Ownership: submitted present={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastSubmittedPreviewPresentId")} sourceSeq={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastSubmittedSourceSequenceNumber")} | rendered present={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderedPreviewPresentId")} sourceSeq={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderedSourceSequenceNumber")} schedulerToPresent={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderedSchedulerToPresentMs")}ms | lastDrop={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastDropReason")}");
            AutomationSnapshotFormatter.AppendPreviewSlowFrameDiagnostics(builder, snapshot);
        }
        else
        {
            builder.AppendLine($"Frames: {AutomationSnapshotFormatter.Get(snapshot, "PreviewFramesArrived")} arrived, {AutomationSnapshotFormatter.Get(snapshot, "PreviewFramesDisplayed")} displayed, {AutomationSnapshotFormatter.Get(snapshot, "PreviewFramesDropped")} dropped");
            builder.AppendLine($"Average Rate: {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceObservedFps")} fps | 1% Low: {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceOnePercentLowFps")} fps");
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
                CaptureAvgMs = AutomationSnapshotFormatter.GetDouble(item, "CaptureCadenceAverageMs"),
                CaptureP95Ms = AutomationSnapshotFormatter.GetDouble(item, "CaptureCadenceP95Ms"),
                CaptureP99Ms = AutomationSnapshotFormatter.GetDouble(item, "CaptureCadenceP99Ms"),
                CaptureMaxMs = AutomationSnapshotFormatter.GetDouble(item, "CaptureCadenceMaxMs"),
                CaptureOnePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, "CaptureCadenceOnePercentLowFps"),
                PreviewAvgMs = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceAverageMs"),
                PreviewP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceP95Ms"),
                PreviewMaxMs = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceMaxMs"),
                PreviewOnePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceOnePercentLowFps"),
                PreviewSlowPct = AutomationSnapshotFormatter.GetDouble(item, "PreviewCadenceSlowFramePercent"),
                PreviewD3DPending = AutomationSnapshotFormatter.GetInt(item, "PreviewD3DPendingFrameCount"),
                PreviewD3DPresentP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DPresentCallP95Ms"),
                PreviewD3DTotalP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DTotalFrameCpuP95Ms"),
                PreviewD3DFrameLatencyWaitTimeouts = AutomationSnapshotFormatter.GetLong(item, "PreviewD3DFrameLatencyWaitTimeoutCount"),
                PreviewD3DFrameLatencyWaitP95Ms = AutomationSnapshotFormatter.GetDouble(item, "PreviewD3DFrameLatencyWaitP95Ms"),
                PreviewD3DRecentMissed = AutomationSnapshotFormatter.GetLong(item, "PreviewD3DFrameStatsRecentMissedRefreshCount"),
                PreviewD3DRecentFailures = AutomationSnapshotFormatter.GetLong(item, "PreviewD3DFrameStatsRecentFailureCount"),
                LatencyMs = AutomationSnapshotFormatter.GetLong(item, "PipelineLatencyMs"),
                CpuPct = AutomationSnapshotFormatter.GetDouble(item, "ProcessCpuPercent"),
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
        builder.AppendLine("Timestamp                | CapAvg | CapP95 | CapP99 | Cap1% | PrvAvg | PrvP95 | PrvSlow | D3DQ | D3DPrs | D3DTot | D3DMiss | VidQ | VidDrop | LatMs | CPU% | WorkMB | MgdMB  | G0   | G1   | G2   | GC%  | Wkr  | IO");
        builder.AppendLine(new string('-', 200));

        foreach (var entry in entries)
        {
            builder.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0,-24} | {1,6:F1} | {2,6:F1} | {3,6:F1} | {4,5:F1} | {5,6:F1} | {6,6:F1} | {7,7:F1} | {8,4} | {9,6:F1} | {10,6:F1} | {11,7} | {12,4} | {13,7} | {14,5} | {15,5:F1} | {16,6:F1} | {17,6:F1} | {18,4} | {19,4} | {20,4} | {21,4:F1} | {22,4} | {23,4}",
                entry.Timestamp,
                entry.CaptureAvgMs,
                entry.CaptureP95Ms,
                entry.CaptureP99Ms,
                entry.CaptureOnePercentLowFps,
                entry.PreviewAvgMs,
                entry.PreviewP95Ms,
                entry.PreviewSlowPct,
                entry.PreviewD3DPending,
                entry.PreviewD3DPresentP95Ms,
                entry.PreviewD3DTotalP95Ms,
                entry.PreviewD3DRecentMissed,
                entry.VidQueue,
                entry.VidDrops,
                entry.LatencyMs,
                entry.CpuPct,
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
            builder.AppendLine($"Capture Avg:    {first.CaptureAvgMs:F1}ms -> {last.CaptureAvgMs:F1}ms (delta: {last.CaptureAvgMs - first.CaptureAvgMs:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"Capture P95:    {first.CaptureP95Ms:F1}ms -> {last.CaptureP95Ms:F1}ms (delta: {last.CaptureP95Ms - first.CaptureP95Ms:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"Capture P99:    {first.CaptureP99Ms:F1}ms -> {last.CaptureP99Ms:F1}ms (delta: {last.CaptureP99Ms - first.CaptureP99Ms:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"Capture Max:    {first.CaptureMaxMs:F1}ms -> {last.CaptureMaxMs:F1}ms (delta: {last.CaptureMaxMs - first.CaptureMaxMs:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"Preview Avg:    {first.PreviewAvgMs:F1}ms -> {last.PreviewAvgMs:F1}ms (delta: {last.PreviewAvgMs - first.PreviewAvgMs:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"Preview P95:    {first.PreviewP95Ms:F1}ms -> {last.PreviewP95Ms:F1}ms (delta: {last.PreviewP95Ms - first.PreviewP95Ms:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"Preview Max:    {first.PreviewMaxMs:F1}ms -> {last.PreviewMaxMs:F1}ms (delta: {last.PreviewMaxMs - first.PreviewMaxMs:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"Preview 1% Low: {first.PreviewOnePercentLowFps:F1}fps -> {last.PreviewOnePercentLowFps:F1}fps");
            builder.AppendLine($"Preview Slow%:  {first.PreviewSlowPct:F1}% -> {last.PreviewSlowPct:F1}% (delta: {last.PreviewSlowPct - first.PreviewSlowPct:+0.0;-0.0;0.0}%)");
            builder.AppendLine($"D3D Present P95:{first.PreviewD3DPresentP95Ms:F1}ms -> {last.PreviewD3DPresentP95Ms:F1}ms (delta: {last.PreviewD3DPresentP95Ms - first.PreviewD3DPresentP95Ms:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"D3D Total P95:  {first.PreviewD3DTotalP95Ms:F1}ms -> {last.PreviewD3DTotalP95Ms:F1}ms (delta: {last.PreviewD3DTotalP95Ms - first.PreviewD3DTotalP95Ms:+0.0;-0.0;0.0}ms)");
            builder.AppendLine($"D3D Wait P95:   {first.PreviewD3DFrameLatencyWaitP95Ms:F1}ms -> {last.PreviewD3DFrameLatencyWaitP95Ms:F1}ms (timeouts: {first.PreviewD3DFrameLatencyWaitTimeouts} -> {last.PreviewD3DFrameLatencyWaitTimeouts})");
            builder.AppendLine($"D3D Missed:     {first.PreviewD3DRecentMissed} -> {last.PreviewD3DRecentMissed} (latest-window delta: {last.PreviewD3DRecentMissed - first.PreviewD3DRecentMissed:+0;-0;0})");
            builder.AppendLine($"D3D Stat Fails: {first.PreviewD3DRecentFailures} -> {last.PreviewD3DRecentFailures} (latest-window delta: {last.PreviewD3DRecentFailures - first.PreviewD3DRecentFailures:+0;-0;0})");
            builder.AppendLine($"Capture Rate:   {first.CaptureFps:F1}fps -> {last.CaptureFps:F1}fps (derived avg)");
            builder.AppendLine($"Capture 1% Low: {first.CaptureOnePercentLowFps:F1}fps -> {last.CaptureOnePercentLowFps:F1}fps");
            builder.AppendLine($"Preview Rate:   {first.PreviewFps:F1}fps -> {last.PreviewFps:F1}fps (derived avg)");
            builder.AppendLine($"Video Drops:    {first.VidDrops} -> {last.VidDrops} (delta: {last.VidDrops - first.VidDrops:+0;-0;0})");
            builder.AppendLine($"Process CPU:    {first.CpuPct:F1}% -> {last.CpuPct:F1}% (delta: {last.CpuPct - first.CpuPct:+0.0;-0.0;0.0}%)");
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
        builder.AppendLine($"Process CPU: {AutomationSnapshotFormatter.Get(snapshot, "ProcessCpuPercent")}%");
        builder.AppendLine($"Process CPU Time: {AutomationSnapshotFormatter.Get(snapshot, "ProcessCpuTotalProcessorTimeMs")}ms");
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
        public double CaptureAvgMs { get; init; }
        public double CaptureP95Ms { get; init; }
        public double CaptureP99Ms { get; init; }
        public double CaptureMaxMs { get; init; }
        public double CaptureOnePercentLowFps { get; init; }
        public double PreviewAvgMs { get; init; }
        public double PreviewP95Ms { get; init; }
        public double PreviewMaxMs { get; init; }
        public double PreviewOnePercentLowFps { get; init; }
        public double PreviewSlowPct { get; init; }
        public int PreviewD3DPending { get; init; }
        public double PreviewD3DPresentP95Ms { get; init; }
        public double PreviewD3DTotalP95Ms { get; init; }
        public long PreviewD3DFrameLatencyWaitTimeouts { get; init; }
        public double PreviewD3DFrameLatencyWaitP95Ms { get; init; }
        public long PreviewD3DRecentMissed { get; init; }
        public long PreviewD3DRecentFailures { get; init; }
        public long LatencyMs { get; init; }
        public double CpuPct { get; init; }
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
