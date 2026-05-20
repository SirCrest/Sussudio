using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendMjpegPipelineTimingLines(StringBuilder builder, JsonElement snapshot, string mjpegDecoderCount)
    {
        builder.AppendLine($"Decoders: {mjpegDecoderCount} | Decoded={Get(snapshot, "MjpegTotalDecoded")} Emitted={Get(snapshot, "MjpegTotalEmitted")} Dropped={Get(snapshot, "MjpegTotalDropped")}");
        builder.AppendLine(
            $"Compressed Queue: depth={Get(snapshot, "MjpegCompressedQueueDepth")} bytes={Get(snapshot, "MjpegCompressedQueueBytes")}/{Get(snapshot, "MjpegCompressedQueueByteBudget")} " +
            $"queued={Get(snapshot, "MjpegCompressedFramesQueued")} dequeued={Get(snapshot, "MjpegCompressedFramesDequeued")} " +
            $"drops(full={Get(snapshot, "MjpegCompressedDropsQueueFull")}, budget={Get(snapshot, "MjpegCompressedDropsByteBudget")}, disposed={Get(snapshot, "MjpegCompressedDropsDisposed")})");
        builder.AppendLine(
            $"MJPEG Drop Reasons: decode={Get(snapshot, "MjpegDecodeFailures")} reorderCollision={Get(snapshot, "MjpegReorderCollisions")} emit={Get(snapshot, "MjpegEmitFailures")}");
        builder.AppendLine($"Reorder: avg={Get(snapshot, "MjpegReorderAvgMs")}ms P95={Get(snapshot, "MjpegReorderP95Ms")}ms max={Get(snapshot, "MjpegReorderMaxMs")}ms ({Get(snapshot, "MjpegReorderSampleCount")} samples) | Skips={Get(snapshot, "MjpegReorderSkips")} Buffer={Get(snapshot, "MjpegReorderBufferDepth")}");
        builder.AppendLine($"Pipeline: avg={Get(snapshot, "MjpegPipelineAvgMs")}ms P95={Get(snapshot, "MjpegPipelineP95Ms")}ms max={Get(snapshot, "MjpegPipelineMaxMs")}ms ({Get(snapshot, "MjpegPipelineSampleCount")} samples)");
    }
}
