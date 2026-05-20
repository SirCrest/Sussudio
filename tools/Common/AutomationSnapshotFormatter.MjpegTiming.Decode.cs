using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendMjpegDecodeTimingLines(StringBuilder builder, JsonElement snapshot, string mjpegDecodeSamples)
    {
        if (mjpegDecodeSamples == "0")
        {
            return;
        }

        builder.AppendLine($"Decode: avg={Get(snapshot, "MjpegDecodeAvgMs")}ms P95={Get(snapshot, "MjpegDecodeP95Ms")}ms max={Get(snapshot, "MjpegDecodeMaxMs")}ms ({mjpegDecodeSamples} samples)");
        builder.AppendLine($"Interop Copy: avg={Get(snapshot, "MjpegInteropCopyAvgMs")}ms P95={Get(snapshot, "MjpegInteropCopyP95Ms")}ms max={Get(snapshot, "MjpegInteropCopyMaxMs")}ms ({Get(snapshot, "MjpegInteropCopySampleCount")} samples)");
        builder.AppendLine($"Total Callback: avg={Get(snapshot, "MjpegCallbackAvgMs")}ms P95={Get(snapshot, "MjpegCallbackP95Ms")}ms max={Get(snapshot, "MjpegCallbackMaxMs")}ms ({Get(snapshot, "MjpegCallbackSampleCount")} samples)");
    }

    private static void AppendMjpegPerDecoderTimingLines(StringBuilder builder, JsonElement snapshot)
    {
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
