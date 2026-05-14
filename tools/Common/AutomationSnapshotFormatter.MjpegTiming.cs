using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendMjpegTimingSection(StringBuilder builder, JsonElement snapshot)
    {
        var mjpegDecodeSamples = Get(snapshot, "MjpegDecodeSampleCount", "0");
        var mjpegDecoderCount = Get(snapshot, "MjpegDecoderCount", "0");
        var hasCompressedActivity =
            Get(snapshot, "MjpegCompressedFramesQueued", "0") != "0" ||
            Get(snapshot, "MjpegCompressedFramesDequeued", "0") != "0" ||
            Get(snapshot, "MjpegCompressedDropsQueueFull", "0") != "0" ||
            Get(snapshot, "MjpegCompressedDropsByteBudget", "0") != "0" ||
            Get(snapshot, "MjpegCompressedDropsDisposed", "0") != "0" ||
            Get(snapshot, "MjpegCompressedQueueDepth", "0") != "0" ||
            Get(snapshot, "MjpegCompressedQueueBytes", "0") != "0";
        if (mjpegDecodeSamples == "0" && mjpegDecoderCount == "0" && !hasCompressedActivity)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("== MJPEG Pipeline Timing ==");
        if (mjpegDecodeSamples != "0")
        {
            builder.AppendLine($"Decode: avg={Get(snapshot, "MjpegDecodeAvgMs")}ms P95={Get(snapshot, "MjpegDecodeP95Ms")}ms max={Get(snapshot, "MjpegDecodeMaxMs")}ms ({mjpegDecodeSamples} samples)");
            builder.AppendLine($"Interop Copy: avg={Get(snapshot, "MjpegInteropCopyAvgMs")}ms P95={Get(snapshot, "MjpegInteropCopyP95Ms")}ms max={Get(snapshot, "MjpegInteropCopyMaxMs")}ms ({Get(snapshot, "MjpegInteropCopySampleCount")} samples)");
            builder.AppendLine($"Total Callback: avg={Get(snapshot, "MjpegCallbackAvgMs")}ms P95={Get(snapshot, "MjpegCallbackP95Ms")}ms max={Get(snapshot, "MjpegCallbackMaxMs")}ms ({Get(snapshot, "MjpegCallbackSampleCount")} samples)");
        }

        builder.AppendLine($"Decoders: {mjpegDecoderCount} | Decoded={Get(snapshot, "MjpegTotalDecoded")} Emitted={Get(snapshot, "MjpegTotalEmitted")} Dropped={Get(snapshot, "MjpegTotalDropped")}");
        builder.AppendLine(
            $"Compressed Queue: depth={Get(snapshot, "MjpegCompressedQueueDepth")} bytes={Get(snapshot, "MjpegCompressedQueueBytes")}/{Get(snapshot, "MjpegCompressedQueueByteBudget")} " +
            $"queued={Get(snapshot, "MjpegCompressedFramesQueued")} dequeued={Get(snapshot, "MjpegCompressedFramesDequeued")} " +
            $"drops(full={Get(snapshot, "MjpegCompressedDropsQueueFull")}, budget={Get(snapshot, "MjpegCompressedDropsByteBudget")}, disposed={Get(snapshot, "MjpegCompressedDropsDisposed")})");
        builder.AppendLine(
            $"MJPEG Drop Reasons: decode={Get(snapshot, "MjpegDecodeFailures")} reorderCollision={Get(snapshot, "MjpegReorderCollisions")} emit={Get(snapshot, "MjpegEmitFailures")}");
        builder.AppendLine($"Reorder: avg={Get(snapshot, "MjpegReorderAvgMs")}ms P95={Get(snapshot, "MjpegReorderP95Ms")}ms max={Get(snapshot, "MjpegReorderMaxMs")}ms ({Get(snapshot, "MjpegReorderSampleCount")} samples) | Skips={Get(snapshot, "MjpegReorderSkips")} Buffer={Get(snapshot, "MjpegReorderBufferDepth")}");
        builder.AppendLine($"Pipeline: avg={Get(snapshot, "MjpegPipelineAvgMs")}ms P95={Get(snapshot, "MjpegPipelineP95Ms")}ms max={Get(snapshot, "MjpegPipelineMaxMs")}ms ({Get(snapshot, "MjpegPipelineSampleCount")} samples)");
        if (string.Equals(Get(snapshot, "MjpegPreviewJitterEnabled", "False"), "True", StringComparison.OrdinalIgnoreCase))
        {
            builder.AppendLine(
                $"Preview Jitter: target={Get(snapshot, "MjpegPreviewJitterTargetDepth")} depth={Get(snapshot, "MjpegPreviewJitterQueueDepth")}/{Get(snapshot, "MjpegPreviewJitterMaxDepth")} " +
                $"queued={Get(snapshot, "MjpegPreviewJitterTotalQueued")} submitted={Get(snapshot, "MjpegPreviewJitterTotalSubmitted")} dropped={Get(snapshot, "MjpegPreviewJitterTotalDropped")} " +
                $"clearedDrops={Get(snapshot, "MjpegPreviewJitterClearedDropCount")} deadlineDrops={Get(snapshot, "MjpegPreviewJitterDeadlineDropCount")} underflows={Get(snapshot, "MjpegPreviewJitterUnderflowCount")} resumeReprimes={Get(snapshot, "MjpegPreviewJitterResumeReprimeCount")} " +
                $"target+={Get(snapshot, "MjpegPreviewJitterTargetIncreaseCount")} target-={Get(snapshot, "MjpegPreviewJitterTargetDecreaseCount")}");
            builder.AppendLine(
                $"Preview Jitter Input: avg={Get(snapshot, "MjpegPreviewJitterInputAvgMs")}ms P95={Get(snapshot, "MjpegPreviewJitterInputP95Ms")}ms max={Get(snapshot, "MjpegPreviewJitterInputMaxMs")}ms ({Get(snapshot, "MjpegPreviewJitterInputSampleCount")} samples)");
            builder.AppendLine(
                $"Preview Jitter Output: avg={Get(snapshot, "MjpegPreviewJitterOutputAvgMs")}ms P95={Get(snapshot, "MjpegPreviewJitterOutputP95Ms")}ms max={Get(snapshot, "MjpegPreviewJitterOutputMaxMs")}ms ({Get(snapshot, "MjpegPreviewJitterOutputSampleCount")} samples)");
            builder.AppendLine(
                $"Preview Jitter Latency: avg={Get(snapshot, "MjpegPreviewJitterLatencyAvgMs")}ms P95={Get(snapshot, "MjpegPreviewJitterLatencyP95Ms")}ms max={Get(snapshot, "MjpegPreviewJitterLatencyMaxMs")}ms ({Get(snapshot, "MjpegPreviewJitterLatencySampleCount")} samples)");
            builder.AppendLine(
                $"Preview Jitter Ownership: present={Get(snapshot, "MjpegPreviewJitterLastSelectedPreviewPresentId")} sourceSeq={Get(snapshot, "MjpegPreviewJitterLastSelectedSourceSequenceNumber")} " +
                $"sourceLatency={Get(snapshot, "MjpegPreviewJitterLastSelectedSourceLatencyMs")}ms lastDropSeq={Get(snapshot, "MjpegPreviewJitterLastDroppedSourceSequenceNumber")} reason={Get(snapshot, "MjpegPreviewJitterLastDropReason")}");
            builder.AppendLine(
                $"Preview Jitter Underflow: reason={Get(snapshot, "MjpegPreviewJitterLastUnderflowReason")} " +
                $"queue={Get(snapshot, "MjpegPreviewJitterLastUnderflowQueueDepth")} " +
                $"inputAge={Get(snapshot, "MjpegPreviewJitterLastUnderflowInputAgeMs")}ms outputAge={Get(snapshot, "MjpegPreviewJitterLastUnderflowOutputAgeMs")}ms " +
                $"scheduleLateLast={Get(snapshot, "MjpegPreviewJitterLastScheduleLateMs")}ms scheduleLateMax={Get(snapshot, "MjpegPreviewJitterMaxScheduleLateMs")}ms count={Get(snapshot, "MjpegPreviewJitterScheduleLateCount")}");
        }
        if (!snapshot.TryGetProperty("MjpegPerDecoder", out var perDecoder) ||
            perDecoder.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var worker in perDecoder.EnumerateArray())
        {
            builder.AppendLine(
                $"Decoder[{Get(worker, "WorkerIndex", "?")}]: avg={Get(worker, "AvgMs")}ms " +
                $"P95={Get(worker, "P95Ms")}ms max={Get(worker, "MaxMs")}ms " +
                $"({Get(worker, "SampleCount")} samples)");
        }
    }
}
