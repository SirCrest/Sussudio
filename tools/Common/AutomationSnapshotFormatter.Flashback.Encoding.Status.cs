using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendFlashbackEncodingStatusSection(StringBuilder builder, JsonElement snapshot)
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
    }
}
