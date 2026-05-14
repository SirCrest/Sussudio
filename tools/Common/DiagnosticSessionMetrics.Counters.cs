using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionMetrics
{
    internal static long GetCounterDelta(JsonElement snapshot, JsonElement baselineSnapshot, string propertyName)
    {
        var current = GetNullableLong(snapshot, propertyName) ?? 0;
        var baseline = baselineSnapshot.ValueKind == JsonValueKind.Object
            ? GetNullableLong(baselineSnapshot, propertyName) ?? 0
            : 0;
        return Math.Max(0, current - baseline);
    }

    internal static long GetResetAwareCounterDelta(JsonElement snapshot, JsonElement baselineSnapshot, string propertyName)
    {
        var current = GetNullableLong(snapshot, propertyName) ?? 0;
        var baseline = baselineSnapshot.ValueKind == JsonValueKind.Object
            ? GetNullableLong(baselineSnapshot, propertyName) ?? 0
            : 0;
        return current >= baseline ? current - baseline : current;
    }
}
