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

        if (IsPreviewD3DRendererMode(rendererMode))
        {
            AppendPreviewD3DSection(builder, snapshot);
            return;
        }

        builder.AppendLine($"Frames: {Get(snapshot, "PreviewFramesArrived")} arrived, {Get(snapshot, "PreviewFramesDisplayed")} displayed, {Get(snapshot, "PreviewFramesDropped")} dropped");
        builder.AppendLine($"Average Rate: {Get(snapshot, "PreviewCadenceObservedFps")} fps | 5% Low: {Get(snapshot, "PreviewCadenceFivePercentLowFps")} fps | 1% Low: {Get(snapshot, "PreviewCadenceOnePercentLowFps")} fps | Samples: {Get(snapshot, "PreviewCadenceSampleCount")} over {Get(snapshot, "PreviewCadenceSampleDurationMs")}ms");
        builder.AppendLine($"Pacing Classifier: stage={Get(snapshot, "PreviewPacingLikelySlowStage")} confidence={Get(snapshot, "PreviewPacingSlowStageConfidence")} evidence={Get(snapshot, "PreviewPacingSlowStageEvidence", "")}");
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
