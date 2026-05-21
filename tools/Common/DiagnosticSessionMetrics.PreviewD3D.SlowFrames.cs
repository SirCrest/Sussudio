using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionMetrics
{
    private static void ApplySlowFrame(PreviewD3DMetrics metrics, JsonElement slowFrame)
    {
        metrics.LatestSlowFrameReason = GetSlowFrameReason(slowFrame);
        metrics.LatestSlowFrameOverBudgetMs = GetDouble(slowFrame, "WorstOverBudgetMs");
        metrics.LatestSlowFramePresentIntervalMs = GetDouble(slowFrame, "PresentIntervalMs");
        metrics.LatestSlowFrameTotalFrameCpuMs = GetDouble(slowFrame, "TotalFrameCpuMs");
        metrics.LatestSlowFramePresentCallMs = GetDouble(slowFrame, "PresentCallMs");
        metrics.LatestSlowFramePendingFrameCount = GetInt(slowFrame, "PendingFrameCount");
    }

    private static string GetSlowFrameReason(JsonElement slowFrame)
        => GetString(slowFrame, "SlowReason") ?? GetString(slowFrame, "Reason") ?? string.Empty;

    private static int CountArrayItems(JsonElement snapshot, string propertyName)
    {
        return snapshot.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.GetArrayLength()
            : 0;
    }

    private static bool TryGetLatestSlowFrame(JsonElement snapshot, out JsonElement slowFrame)
    {
        if (snapshot.TryGetProperty("PreviewD3DRecentSlowFrames", out var frames) &&
            frames.ValueKind == JsonValueKind.Array &&
            frames.GetArrayLength() > 0)
        {
            slowFrame = frames.EnumerateArray().Last().Clone();
            return true;
        }

        slowFrame = default;
        return false;
    }
}
