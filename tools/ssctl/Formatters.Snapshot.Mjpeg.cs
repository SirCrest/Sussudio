using System.Text;
using System.Text.Json;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class Formatters
{
    private static void AppendSnapshotMjpegTimingSection(StringBuilder builder, JsonElement snapshot)
    {
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
        if (mjpegDecodeSamples == "0" && mjpegDecoderCount == "0" && !hasCompressedActivity)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("== MJPEG Pipeline Timing ==");
        AppendSnapshotMjpegDecodeTimingLines(builder, snapshot, mjpegDecodeSamples);
        AppendSnapshotMjpegPipelineTimingLines(builder, snapshot, mjpegDecoderCount);
        AppendSnapshotMjpegPreviewJitterSection(builder, snapshot);
        AppendSnapshotMjpegPerDecoderTimingLines(builder, snapshot);
    }

    private static void AppendSnapshotMjpegDecodeTimingLines(StringBuilder builder, JsonElement snapshot, string mjpegDecodeSamples)
    {
        if (mjpegDecodeSamples == "0")
        {
            return;
        }

        builder.AppendLine($"Decode: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegDecodeAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegDecodeP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegDecodeMaxMs")}ms ({mjpegDecodeSamples} samples)");
        builder.AppendLine($"Interop Copy: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegInteropCopyAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegInteropCopyP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegInteropCopyMaxMs")}ms ({AutomationSnapshotFormatter.Get(snapshot, "MjpegInteropCopySampleCount")} samples)");
        builder.AppendLine($"Total Callback: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegCallbackAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegCallbackP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegCallbackMaxMs")}ms ({AutomationSnapshotFormatter.Get(snapshot, "MjpegCallbackSampleCount")} samples)");
    }

    private static void AppendSnapshotMjpegPipelineTimingLines(StringBuilder builder, JsonElement snapshot, string mjpegDecoderCount)
    {
        builder.AppendLine($"Decoders: {mjpegDecoderCount} | Decoded={AutomationSnapshotFormatter.Get(snapshot, "MjpegTotalDecoded")} Emitted={AutomationSnapshotFormatter.Get(snapshot, "MjpegTotalEmitted")} Dropped={AutomationSnapshotFormatter.Get(snapshot, "MjpegTotalDropped")}");
        builder.AppendLine(
            $"Compressed Queue: depth={AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedQueueDepth")} bytes={AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedQueueBytes")}/{AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedQueueByteBudget")} " +
            $"queued={AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedFramesQueued")} dequeued={AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedFramesDequeued")} " +
            $"drops(full={AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedDropsQueueFull")}, budget={AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedDropsByteBudget")}, disposed={AutomationSnapshotFormatter.Get(snapshot, "MjpegCompressedDropsDisposed")})");
        builder.AppendLine(
            $"MJPEG Drop Reasons: decode={AutomationSnapshotFormatter.Get(snapshot, "MjpegDecodeFailures")} reorderCollision={AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderCollisions")} emit={AutomationSnapshotFormatter.Get(snapshot, "MjpegEmitFailures")}");
        builder.AppendLine($"Reorder: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderMaxMs")}ms ({AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderSampleCount")} samples) | Skips={AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderSkips")} Buffer={AutomationSnapshotFormatter.Get(snapshot, "MjpegReorderBufferDepth")}");
        builder.AppendLine($"Pipeline: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegPipelineAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegPipelineP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegPipelineMaxMs")}ms ({AutomationSnapshotFormatter.Get(snapshot, "MjpegPipelineSampleCount")} samples)");
    }

    private static void AppendSnapshotMjpegPreviewJitterSection(StringBuilder builder, JsonElement snapshot)
    {
        if (!string.Equals(AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterEnabled", "False"), "True", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        builder.AppendLine(
            $"Preview Jitter: target={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterTargetDepth")} depth={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterQueueDepth")}/{AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterMaxDepth")} " +
            $"queued={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterTotalQueued")} submitted={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterTotalSubmitted")} dropped={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterTotalDropped")} " +
            $"clearedDrops={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterClearedDropCount")} deadlineDrops={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterDeadlineDropCount")} underflows={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterUnderflowCount")} resumeReprimes={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterResumeReprimeCount")} " +
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
        builder.AppendLine(
            $"Preview Jitter Underflow: reason={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastUnderflowReason")} " +
            $"queue={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastUnderflowQueueDepth")} " +
            $"inputAge={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastUnderflowInputAgeMs")}ms outputAge={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastUnderflowOutputAgeMs")}ms " +
            $"scheduleLateLast={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastScheduleLateMs")}ms scheduleLateMax={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterMaxScheduleLateMs")}ms count={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterScheduleLateCount")}");
    }

    private static void AppendSnapshotMjpegPerDecoderTimingLines(StringBuilder builder, JsonElement snapshot)
    {
        if (!snapshot.TryGetProperty("MjpegPerDecoder", out var perDecoder) ||
            perDecoder.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var worker in perDecoder.EnumerateArray())
        {
            builder.AppendLine(
                $"Decoder[{AutomationSnapshotFormatter.Get(worker, "WorkerIndex", "?")}]: avg={AutomationSnapshotFormatter.Get(worker, "AvgMs")}ms " +
                $"P95={AutomationSnapshotFormatter.Get(worker, "P95Ms")}ms max={AutomationSnapshotFormatter.Get(worker, "MaxMs")}ms " +
                $"({AutomationSnapshotFormatter.Get(worker, "SampleCount")} samples)");
        }
    }
}
