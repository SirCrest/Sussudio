using System.Linq;
using System.Text;
using System.Text.Json;

namespace McpServer.Tools;

public static partial class PreviewFrameCaptureTools
{
    private static void AppendLuminanceHistogram(StringBuilder builder, JsonElement data)
    {
        builder.AppendLine();
        builder.AppendLine("== Luminance Histogram (16 bins) ==");
        if (data.TryGetProperty("LuminanceHistogram", out var histogramElement) &&
            histogramElement.ValueKind == JsonValueKind.Array)
        {
            var bins = new List<int>();
            foreach (var item in histogramElement.EnumerateArray())
            {
                bins.Add(item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out var count)
                    ? count
                    : 0);
            }

            while (bins.Count < 16)
            {
                bins.Add(0);
            }

            var maxBin = bins.Count > 0 ? bins.Max() : 0;
            for (var i = 0; i < 16; i++)
            {
                var start = i * 16;
                var end = start + 15;
                var value = bins[i];
                var barLength = maxBin > 0 ? (int)Math.Round((value / (double)maxBin) * 24.0) : 0;
                var bar = new string('#', Math.Max(0, barLength));
                builder.AppendLine($"{start,3}-{end,3}: {bar} ({value})");
            }
        }
        else
        {
            builder.AppendLine("Histogram unavailable.");
        }
    }
}
