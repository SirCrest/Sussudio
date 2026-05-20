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
        AppendMjpegDecodeTimingLines(builder, snapshot, mjpegDecodeSamples);
        AppendMjpegPipelineTimingLines(builder, snapshot, mjpegDecoderCount);
        AppendMjpegPreviewJitterSection(builder, snapshot);
        AppendMjpegPerDecoderTimingLines(builder, snapshot);
    }
}
