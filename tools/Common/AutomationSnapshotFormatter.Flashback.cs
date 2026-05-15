using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
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
            var encFpsNum = Get(snapshot, "EncoderFrameRateNumerator", "");
            var encFpsDen = Get(snapshot, "EncoderFrameRateDenominator", "");
            var encFpsDetail = !string.IsNullOrWhiteSpace(encFpsNum) &&
                               !string.IsNullOrWhiteSpace(encFpsDen) &&
                               encFpsDen != "0"
                ? $"{encFps} fps ({encFpsNum}/{encFpsDen})"
                : $"{encFps} fps";
            var encBitrate = GetDouble(snapshot, "EncoderTargetBitRate") / 1_000_000.0;
            builder.AppendLine($"Encoder: {codec} {encW}x{encH} @ {encFpsDetail} | Target: {FormatNumber(encBitrate, "0.#")} Mbps");
        }
        var bufferedDurationMs = GetLong(snapshot, "FlashbackBufferedDurationMs");
        var diskBytes = GetLong(snapshot, "FlashbackDiskBytes");
        var diskMb = diskBytes / (1024.0 * 1024.0);
        builder.AppendLine($"Buffer: {FormatNumber(bufferedDurationMs / 1000.0, "F1")}s | Disk: {FormatNumber(diskMb, "F1")} MB | Written: {FormatBytes(GetLong(snapshot, "FlashbackTotalBytesWritten"))} | GPU Encode: {Get(snapshot, "FlashbackGpuEncoding")}");
        builder.AppendLine($"Temp Cache: cache={FormatBytes(GetLong(snapshot, "FlashbackStartupCacheBytes"))} budget={FormatBytes(GetLong(snapshot, "FlashbackStartupCacheBudgetBytes"))} free={FormatBytes(GetLong(snapshot, "FlashbackTempDriveFreeBytes"))} sessions={Get(snapshot, "FlashbackStartupCacheSessionCount")} deleted={Get(snapshot, "FlashbackStartupCacheDeletedSessionCount")} freed={FormatBytes(GetLong(snapshot, "FlashbackStartupCacheFreedBytes"))} overBudget={Get(snapshot, "FlashbackStartupCacheOverBudget")}");
        builder.AppendLine($"Encoded: {Get(snapshot, "FlashbackEncodedFrames")} frames | Dropped: {Get(snapshot, "FlashbackDroppedFrames")} | forceRotate={Get(snapshot, "FlashbackForceRotateActive")} requested={Get(snapshot, "FlashbackForceRotateRequested")} draining={Get(snapshot, "FlashbackForceRotateDraining")} | VQ: {Get(snapshot, "FlashbackVideoQueueDepth")}/{Get(snapshot, "FlashbackVideoQueueCapacity")} max={Get(snapshot, "FlashbackVideoQueueMaxDepth")} AQ: {Get(snapshot, "FlashbackAudioQueueDepth")}/{Get(snapshot, "FlashbackAudioQueueCapacity")}");
        builder.AppendLine($"Flashback Detail: submitted={Get(snapshot, "FlashbackVideoFramesSubmittedToEncoder")} packets={Get(snapshot, "FlashbackVideoEncoderPacketsWritten")} pts={Get(snapshot, "FlashbackVideoEncoderPts")} encoderDrops={Get(snapshot, "FlashbackVideoEncoderDroppedFrames")} seqGaps={Get(snapshot, "FlashbackVideoSequenceGaps")} backendStale={Get(snapshot, "FlashbackBackendSettingsStale")} staleReason={Get(snapshot, "FlashbackBackendSettingsStaleReason", "")} active={Get(snapshot, "FlashbackBackendActiveFormat")}/{Get(snapshot, "FlashbackBackendActivePreset")} requested={Get(snapshot, "FlashbackBackendRequestedFormat")}/{Get(snapshot, "FlashbackBackendRequestedPreset")}");
        builder.AppendLine($"Cleanup: fatal={Get(snapshot, "FatalCleanupInProgress")} flashback={Get(snapshot, "FlashbackCleanupInProgress")}");
        builder.AppendLine($"Flashback Queue Latency: oldest={Get(snapshot, "FlashbackVideoQueueOldestFrameAgeMs")}ms last={Get(snapshot, "FlashbackVideoQueueLastLatencyMs")}ms avg={Get(snapshot, "FlashbackVideoQueueLatencyAvgMs")}ms P95={Get(snapshot, "FlashbackVideoQueueLatencyP95Ms")}ms P99={Get(snapshot, "FlashbackVideoQueueLatencyP99Ms")}ms max={Get(snapshot, "FlashbackVideoQueueLatencyMaxMs")}ms samples={Get(snapshot, "FlashbackVideoQueueLatencySampleCount")} rejected={Get(snapshot, "FlashbackVideoQueueRejectedFrames")} lastReject={Get(snapshot, "FlashbackVideoQueueLastRejectReason", "")}");
        builder.AppendLine($"Flashback Backpressure: total={Get(snapshot, "FlashbackVideoBackpressureWaitMs")}ms events={Get(snapshot, "FlashbackVideoBackpressureEvents")} last={Get(snapshot, "FlashbackVideoBackpressureLastWaitMs")}ms max={Get(snapshot, "FlashbackVideoBackpressureMaxWaitMs")}ms");
        builder.AppendLine($"Flashback Failure: active={Get(snapshot, "FlashbackEncodingFailed")} type={Get(snapshot, "FlashbackEncodingFailureType", "None")} msg={Get(snapshot, "FlashbackEncodingFailureMessage", "")}");
        builder.AppendLine($"Flashback GPU Queue: {Get(snapshot, "FlashbackGpuQueueDepth")}/{Get(snapshot, "FlashbackGpuQueueCapacity")} max={Get(snapshot, "FlashbackGpuQueueMaxDepth")} enq={Get(snapshot, "FlashbackGpuFramesEnqueued")} overloads={Get(snapshot, "FlashbackGpuFramesDropped")} rejected={Get(snapshot, "FlashbackGpuQueueRejectedFrames")} lastReject={Get(snapshot, "FlashbackGpuQueueLastRejectReason", "")}");
        builder.AppendLine($"Playback: {Get(snapshot, "FlashbackPlaybackState")} | Pos: {Get(snapshot, "FlashbackPlaybackPositionMs")}ms | Decoder: {Get(snapshot, "FlashbackDecoderHwAccel")}");
        builder.AppendLine($"Playback Commands: pending={Get(snapshot, "FlashbackPlaybackPendingCommands")}/{Get(snapshot, "FlashbackPlaybackCommandQueueCapacity")} maxPending={Get(snapshot, "FlashbackPlaybackMaxPendingCommands")} lastLatency={Get(snapshot, "FlashbackPlaybackLastCommandQueueLatencyMs")}ms maxLatency={Get(snapshot, "FlashbackPlaybackMaxCommandQueueLatencyMs")}ms maxLatencyCommand={Get(snapshot, "FlashbackPlaybackMaxCommandQueueLatencyCommand")} enq={Get(snapshot, "FlashbackPlaybackCommandsEnqueued")} proc={Get(snapshot, "FlashbackPlaybackCommandsProcessed")} drop={Get(snapshot, "FlashbackPlaybackCommandsDropped")} skip={Get(snapshot, "FlashbackPlaybackCommandsSkippedNotReady")} coalescedScrub={Get(snapshot, "FlashbackPlaybackScrubUpdatesCoalesced")} coalescedSeek={Get(snapshot, "FlashbackPlaybackSeekCommandsCoalesced")} threadAlive={Get(snapshot, "FlashbackPlaybackThreadAlive")} lastQueued={Get(snapshot, "FlashbackPlaybackLastCommandQueued")} lastProcessed={Get(snapshot, "FlashbackPlaybackLastCommandProcessed")} failure={Get(snapshot, "FlashbackPlaybackLastCommandFailure", "")} failureUtc={Get(snapshot, "FlashbackPlaybackLastCommandFailureUtcUnixMs")}");
        builder.AppendLine($"Export: active={Get(snapshot, "FlashbackExportActive")} status={Get(snapshot, "FlashbackExportStatus")} id={Get(snapshot, "FlashbackExportId")} lastResultId={Get(snapshot, "LastExportId")} kind={Get(snapshot, "FlashbackExportFailureKind", "None")} progress={Get(snapshot, "FlashbackExportPercent")}% segments={Get(snapshot, "FlashbackExportSegmentsProcessed")}/{Get(snapshot, "FlashbackExportTotalSegments")} elapsed={Get(snapshot, "FlashbackExportElapsedMs")}ms progressAge={Get(snapshot, "FlashbackExportLastProgressAgeMs")}ms bytes={FormatBytes(GetLong(snapshot, "FlashbackExportOutputBytes"))} throughput={FormatBytes((long)GetDouble(snapshot, "FlashbackExportThroughputBytesPerSec"))}/s in={Get(snapshot, "FlashbackExportInPointMs")}ms out={Get(snapshot, "FlashbackExportOutPointMs")}ms lastProgressUtc={Get(snapshot, "FlashbackExportLastProgressUtcUnixMs")} completedUtc={Get(snapshot, "FlashbackExportCompletedUtcUnixMs")} forceRotateFallbacks={Get(snapshot, "FlashbackExportForceRotateFallbacks")} lastForceRotateFallbackSegments={Get(snapshot, "FlashbackExportLastForceRotateFallbackSegments")} lastForceRotateFallbackUtc={Get(snapshot, "FlashbackExportLastForceRotateFallbackUtcUnixMs")} path={Get(snapshot, "FlashbackExportOutputPath")} msg={Get(snapshot, "FlashbackExportMessage", "")}");
        var playbackFps = double.TryParse(Get(snapshot, "FlashbackPlaybackObservedFps", "0"), NumberStyles.Float, CultureInfo.InvariantCulture, out var fps)
            ? fps
            : 0;
        var playbackAvgMs = double.TryParse(Get(snapshot, "FlashbackPlaybackAvgFrameMs", "0"), NumberStyles.Float, CultureInfo.InvariantCulture, out var avgMs)
            ? avgMs
            : 0;
        var avDrift = double.TryParse(Get(snapshot, "FlashbackAvDriftMs", "0"), NumberStyles.Float, CultureInfo.InvariantCulture, out var drift)
            ? drift
            : 0;
        builder.AppendLine($"Playback Frame Time: avg={FormatNumber(playbackAvgMs, "F2")}ms P95={Get(snapshot, "FlashbackPlaybackP95FrameMs")}ms P99={Get(snapshot, "FlashbackPlaybackP99FrameMs")}ms max={Get(snapshot, "FlashbackPlaybackMaxFrameMs")}ms | Average Rate: {FormatNumber(playbackFps, "F1")} fps | Target: {Get(snapshot, "FlashbackPlaybackTargetFps")} fps | 5% Low: {Get(snapshot, "FlashbackPlaybackFivePercentLowFps")} fps | 1% Low: {Get(snapshot, "FlashbackPlaybackOnePercentLowFps")} fps | Samples: {Get(snapshot, "FlashbackPlaybackCadenceSampleCount")} over {Get(snapshot, "FlashbackPlaybackSampleDurationMs")}ms");
        builder.AppendLine($"Playback Decode: avg={Get(snapshot, "FlashbackPlaybackDecodeAvgMs")}ms P95={Get(snapshot, "FlashbackPlaybackDecodeP95Ms")}ms P99={Get(snapshot, "FlashbackPlaybackDecodeP99Ms")}ms max={Get(snapshot, "FlashbackPlaybackDecodeMaxMs")}ms phase={Get(snapshot, "FlashbackPlaybackMaxDecodePhase", "")} receive={Get(snapshot, "FlashbackPlaybackMaxDecodeReceiveMs")}ms feed={Get(snapshot, "FlashbackPlaybackMaxDecodeFeedMs")}ms read={Get(snapshot, "FlashbackPlaybackMaxDecodeReadMs")}ms send={Get(snapshot, "FlashbackPlaybackMaxDecodeSendMs")}ms audio={Get(snapshot, "FlashbackPlaybackMaxDecodeAudioMs")}ms convert={Get(snapshot, "FlashbackPlaybackMaxDecodeConvertMs")}ms maxPos={Get(snapshot, "FlashbackPlaybackMaxDecodePositionMs")}ms samples={Get(snapshot, "FlashbackPlaybackDecodeSampleCount")} seekCapHits={Get(snapshot, "FlashbackPlaybackSeekForwardDecodeCapHits")} lastSeekCap={Get(snapshot, "FlashbackPlaybackLastSeekHitForwardDecodeCap")}");
        builder.AppendLine($"Playback Frames: total={Get(snapshot, "FlashbackPlaybackFrameCount")} late={Get(snapshot, "FlashbackPlaybackLateFrames")} slow={Get(snapshot, "FlashbackPlaybackSlowFrames")} ({Get(snapshot, "FlashbackPlaybackSlowFramePercent")}%) dropped={Get(snapshot, "FlashbackPlaybackDroppedFrames")} audioMasterDouble={Get(snapshot, "FlashbackPlaybackAudioMasterDelayDoubles")} audioMasterShrink={Get(snapshot, "FlashbackPlaybackAudioMasterDelayShrinks")} audioMasterFallback={Get(snapshot, "FlashbackPlaybackAudioMasterFallbacks")} unavailable={Get(snapshot, "FlashbackPlaybackAudioMasterUnavailableFallbacks")} stale={Get(snapshot, "FlashbackPlaybackAudioMasterStaleFallbacks")} driftOutlier={Get(snapshot, "FlashbackPlaybackAudioMasterDriftOutlierFallbacks")} lastAudioFallback={Get(snapshot, "FlashbackPlaybackAudioMasterLastFallbackReason", "")}/{Get(snapshot, "FlashbackPlaybackAudioMasterLastFallbackClockAgeMs")}ms lastDrop={Get(snapshot, "FlashbackPlaybackLastDropReason", "")} lastDropUtc={Get(snapshot, "FlashbackPlaybackLastDropUtcUnixMs")} submitFailures={Get(snapshot, "FlashbackPlaybackSubmitFailures")} lastSubmitFailure={Get(snapshot, "FlashbackPlaybackLastSubmitFailure", "")} lastSubmitFailureUtc={Get(snapshot, "FlashbackPlaybackLastSubmitFailureUtcUnixMs")}");
        builder.AppendLine($"Playback Stages: switches={Get(snapshot, "FlashbackPlaybackSegmentSwitches")} fmp4Reopens={Get(snapshot, "FlashbackPlaybackFmp4Reopens")} writeHeadWaits={Get(snapshot, "FlashbackPlaybackWriteHeadWaits")} nearLiveSnaps={Get(snapshot, "FlashbackPlaybackNearLiveSnaps")} decodeErrorSnaps={Get(snapshot, "FlashbackPlaybackDecodeErrorSnaps")} lastWriteHeadGap={Get(snapshot, "FlashbackPlaybackLastWriteHeadWaitGapMs")}ms");
        builder.AppendLine($"A/V Drift: {FormatNumber(avDrift, "+0.0;-0.0;0.0")}ms (+ = audio ahead) | Audio buffered={Get(snapshot, "WasapiPlaybackBufferedDurationMs")}ms queue={Get(snapshot, "WasapiPlaybackQueueDurationMs")}ms active={Get(snapshot, "WasapiPlaybackActiveChunkDurationMs")}ms endpoint={Get(snapshot, "WasapiPlaybackEndpointQueuedDurationMs")}ms streamLatency={Get(snapshot, "WasapiPlaybackStreamLatencyMs")}ms | File: {Get(snapshot, "FlashbackFilePath")}");
        builder.AppendLine();
    }
}
