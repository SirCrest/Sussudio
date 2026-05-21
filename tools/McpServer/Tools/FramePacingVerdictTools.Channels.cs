using System.Globalization;
using System.Text.Json;
using Sussudio.Tools;

namespace McpServer.Tools;

public static partial class FramePacingVerdictTools
{
    private sealed record FramePacingChannel(
        double ObservedFps,
        double FivePercentLowFps,
        double OnePercentLowFps,
        int SampleCount,
        double SampleDurationMs,
        double[] IntervalsMs);

    private static FramePacingChannel ReadChannel(
        JsonElement snapshot,
        string observedFpsProperty,
        string fivePercentLowFpsProperty,
        string onePercentLowFpsProperty,
        string sampleCountProperty,
        string sampleDurationMsProperty,
        string intervalsProperty)
    {
        return new FramePacingChannel(
            AutomationSnapshotFormatter.GetDouble(snapshot, observedFpsProperty),
            AutomationSnapshotFormatter.GetDouble(snapshot, fivePercentLowFpsProperty),
            AutomationSnapshotFormatter.GetDouble(snapshot, onePercentLowFpsProperty),
            AutomationSnapshotFormatter.GetInt(snapshot, sampleCountProperty),
            AutomationSnapshotFormatter.GetDouble(snapshot, sampleDurationMsProperty),
            GetDoubleArray(snapshot, intervalsProperty));
    }

    private static double[] GetDoubleArray(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var array) ||
            array.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<double>();
        }

        var values = new List<double>();
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Number && item.TryGetDouble(out var numeric))
            {
                values.Add(numeric);
            }
            else if (item.ValueKind == JsonValueKind.String &&
                     double.TryParse(item.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                values.Add(parsed);
            }
        }

        return values.ToArray();
    }
}
