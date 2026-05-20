using System.Text;
using System.Text.Json;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class Formatters
{
    private static void AppendSnapshotFlashbackEncodingStatusSection(StringBuilder builder, JsonElement snapshot)
    {
        var encCodec = AutomationSnapshotFormatter.Get(snapshot, "EncoderCodecName");
        if (!string.IsNullOrEmpty(encCodec))
        {
            var encW = AutomationSnapshotFormatter.Get(snapshot, "EncoderWidth", "0");
            var encH = AutomationSnapshotFormatter.Get(snapshot, "EncoderHeight", "0");
            var encFps = AutomationSnapshotFormatter.Get(snapshot, "EncoderFrameRate", "0");
            var encFpsNum = AutomationSnapshotFormatter.Get(snapshot, "EncoderFrameRateNumerator", "");
            var encFpsDen = AutomationSnapshotFormatter.Get(snapshot, "EncoderFrameRateDenominator", "");
            var encFpsDetail = !string.IsNullOrWhiteSpace(encFpsNum) &&
                               !string.IsNullOrWhiteSpace(encFpsDen) &&
                               encFpsDen != "0"
                ? $"{encFps} fps ({encFpsNum}/{encFpsDen})"
                : $"{encFps} fps";
            var encBr = AutomationSnapshotFormatter.GetDouble(snapshot, "EncoderTargetBitRate") / 1_000_000.0;
            builder.AppendLine($"Encoder: {encCodec} {encW}x{encH} @ {encFpsDetail} | Target: {AutomationSnapshotFormatter.FormatNumber(encBr, "0.#")} Mbps");
        }

        var fbDurationMs = AutomationSnapshotFormatter.GetLong(snapshot, "FlashbackBufferedDurationMs");
        var fbDiskMb = AutomationSnapshotFormatter.GetLong(snapshot, "FlashbackDiskBytes") / (1024.0 * 1024.0);
        builder.AppendLine($"Buffer: {AutomationSnapshotFormatter.FormatNumber(fbDurationMs / 1000.0, "F1")}s | Disk: {AutomationSnapshotFormatter.FormatNumber(fbDiskMb, "F1")} MB | Written: {AutomationSnapshotFormatter.FormatBytes(AutomationSnapshotFormatter.GetLong(snapshot, "FlashbackTotalBytesWritten"))} | GPU Encode: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackGpuEncoding")}");
        builder.AppendLine($"Temp Cache: cache={AutomationSnapshotFormatter.FormatBytes(AutomationSnapshotFormatter.GetLong(snapshot, "FlashbackStartupCacheBytes"))} budget={AutomationSnapshotFormatter.FormatBytes(AutomationSnapshotFormatter.GetLong(snapshot, "FlashbackStartupCacheBudgetBytes"))} free={AutomationSnapshotFormatter.FormatBytes(AutomationSnapshotFormatter.GetLong(snapshot, "FlashbackTempDriveFreeBytes"))} sessions={AutomationSnapshotFormatter.Get(snapshot, "FlashbackStartupCacheSessionCount")} deleted={AutomationSnapshotFormatter.Get(snapshot, "FlashbackStartupCacheDeletedSessionCount")} freed={AutomationSnapshotFormatter.FormatBytes(AutomationSnapshotFormatter.GetLong(snapshot, "FlashbackStartupCacheFreedBytes"))} overBudget={AutomationSnapshotFormatter.Get(snapshot, "FlashbackStartupCacheOverBudget")}");
        builder.AppendLine($"Encoded: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackEncodedFrames")} frames | Dropped: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackDroppedFrames")} | forceRotate={AutomationSnapshotFormatter.Get(snapshot, "FlashbackForceRotateActive")} requested={AutomationSnapshotFormatter.Get(snapshot, "FlashbackForceRotateRequested")} draining={AutomationSnapshotFormatter.Get(snapshot, "FlashbackForceRotateDraining")} | VQ: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueDepth")}/{AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueCapacity")} max={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueMaxDepth")} AQ: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackAudioQueueDepth")}/{AutomationSnapshotFormatter.Get(snapshot, "FlashbackAudioQueueCapacity")}");
        builder.AppendLine($"Flashback Detail: submitted={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoFramesSubmittedToEncoder")} packets={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoEncoderPacketsWritten")} pts={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoEncoderPts")} encoderDrops={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoEncoderDroppedFrames")} seqGaps={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoSequenceGaps")} backendStale={AutomationSnapshotFormatter.Get(snapshot, "FlashbackBackendSettingsStale")} staleReason={AutomationSnapshotFormatter.Get(snapshot, "FlashbackBackendSettingsStaleReason", "")} active={AutomationSnapshotFormatter.Get(snapshot, "FlashbackBackendActiveFormat")}/{AutomationSnapshotFormatter.Get(snapshot, "FlashbackBackendActivePreset")} requested={AutomationSnapshotFormatter.Get(snapshot, "FlashbackBackendRequestedFormat")}/{AutomationSnapshotFormatter.Get(snapshot, "FlashbackBackendRequestedPreset")}");
        builder.AppendLine($"Cleanup: fatal={AutomationSnapshotFormatter.Get(snapshot, "FatalCleanupInProgress")} flashback={AutomationSnapshotFormatter.Get(snapshot, "FlashbackCleanupInProgress")}");
    }
}
