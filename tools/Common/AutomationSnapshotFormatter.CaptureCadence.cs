using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendCaptureCadenceSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Capture Cadence ==");
        builder.AppendLine($"Frame Time: target={FormatFrameBudgetMs(snapshot, "ExpectedCaptureFrameRate")} avg={Get(snapshot, "CaptureCadenceAverageIntervalMs")}ms P95={Get(snapshot, "CaptureCadenceP95IntervalMs")}ms P99={Get(snapshot, "CaptureCadenceP99IntervalMs")}ms max={Get(snapshot, "CaptureCadenceMaxIntervalMs")}ms | Samples: {Get(snapshot, "CaptureCadenceSampleCount")} over {Get(snapshot, "CaptureCadenceSampleDurationMs")}ms");
        builder.AppendLine($"Average Rate: {Get(snapshot, "CaptureCadenceObservedFps")} fps (expected {Get(snapshot, "ExpectedCaptureFrameRate")} fps)");
        builder.AppendLine($"5% Low: {Get(snapshot, "CaptureCadenceFivePercentLowFps")} fps | 1% Low: {Get(snapshot, "CaptureCadenceOnePercentLowFps")} fps");
        builder.AppendLine($"Jitter: {Get(snapshot, "CaptureCadenceJitterStdDevMs")}ms | Gaps: {Get(snapshot, "CaptureCadenceSevereGapCount")} | Est Drops: {Get(snapshot, "CaptureCadenceEstimatedDroppedFrames")} ({Get(snapshot, "CaptureCadenceEstimatedDropPercent")}%)");
        builder.AppendLine($"MJPEG Packet Fingerprint: input={Get(snapshot, "MjpegPacketHashInputObservedFps")} fps unique={Get(snapshot, "MjpegPacketHashUniqueObservedFps")} fps dup={Get(snapshot, "MjpegPacketHashDuplicateFramePercent")}% pattern={Get(snapshot, "MjpegPacketHashPattern")} longestDup={Get(snapshot, "MjpegPacketHashLongestDuplicateRun")}");
        builder.AppendLine($"Sampled Decoded Crop: changes={Get(snapshot, "VisualCadenceChangeObservedFps")} fps output={Get(snapshot, "VisualCadenceOutputObservedFps")} fps repeat={Get(snapshot, "VisualCadenceRepeatFramePercent")}% avgChangedPx={Get(snapshot, "VisualCadenceAverageDelta")} changedPxPct={Get(snapshot, "VisualCadenceMotionScore")} confidence={Get(snapshot, "VisualCadenceMotionConfidence")}");
        builder.AppendLine($"Sampled Tight Crop: changes={Get(snapshot, "VisualCenterCadenceChangeObservedFps")} fps output={Get(snapshot, "VisualCenterCadenceOutputObservedFps")} fps repeat={Get(snapshot, "VisualCenterCadenceRepeatFramePercent")}% avgChangedPx={Get(snapshot, "VisualCenterCadenceAverageDelta")} changedPxPct={Get(snapshot, "VisualCenterCadenceMotionScore")} confidence={Get(snapshot, "VisualCenterCadenceMotionConfidence")}");
        AppendMjpegTimingSection(builder, snapshot);
        AppendAvSyncSection(builder, snapshot);
        AppendPreviewSection(builder, snapshot);
        AppendSourceSection(builder, snapshot);
    }
}
