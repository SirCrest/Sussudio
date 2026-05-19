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
        AppendFlashbackEncodingSection(builder, snapshot);
        AppendFlashbackPlaybackStatusSection(builder, snapshot);
        AppendFlashbackExportSection(builder, snapshot);
        AppendFlashbackPlaybackMetricsSection(builder, snapshot);
        builder.AppendLine();
    }

    private static void AppendFlashbackEncodingSection(StringBuilder builder, JsonElement snapshot)
    {
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
    }

    private static void AppendFlashbackExportSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"Export: active={Get(snapshot, "FlashbackExportActive")} status={Get(snapshot, "FlashbackExportStatus")} id={Get(snapshot, "FlashbackExportId")} lastResultId={Get(snapshot, "LastExportId")} kind={Get(snapshot, "FlashbackExportFailureKind", "None")} progress={Get(snapshot, "FlashbackExportPercent")}% segments={Get(snapshot, "FlashbackExportSegmentsProcessed")}/{Get(snapshot, "FlashbackExportTotalSegments")} elapsed={Get(snapshot, "FlashbackExportElapsedMs")}ms progressAge={Get(snapshot, "FlashbackExportLastProgressAgeMs")}ms bytes={FormatBytes(GetLong(snapshot, "FlashbackExportOutputBytes"))} throughput={FormatBytes((long)GetDouble(snapshot, "FlashbackExportThroughputBytesPerSec"))}/s in={Get(snapshot, "FlashbackExportInPointMs")}ms out={Get(snapshot, "FlashbackExportOutPointMs")}ms lastProgressUtc={Get(snapshot, "FlashbackExportLastProgressUtcUnixMs")} completedUtc={Get(snapshot, "FlashbackExportCompletedUtcUnixMs")} forceRotateFallbacks={Get(snapshot, "FlashbackExportForceRotateFallbacks")} lastForceRotateFallbackSegments={Get(snapshot, "FlashbackExportLastForceRotateFallbackSegments")} lastForceRotateFallbackUtc={Get(snapshot, "FlashbackExportLastForceRotateFallbackUtcUnixMs")} path={Get(snapshot, "FlashbackExportOutputPath")} msg={Get(snapshot, "FlashbackExportMessage", "")}");
    }
}
