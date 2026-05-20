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
        AppendMjpegPreviewJitterSection(builder, snapshot);
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
