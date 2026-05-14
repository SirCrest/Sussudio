using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionMetrics
{
    internal static PreviewD3DMetrics BuildPreviewD3DMetrics(
        JsonElement initialSnapshot,
        JsonElement lastSnapshot,
        IReadOnlyList<DiagnosticSessionSample> samples)
    {
        var missedRefreshStart = GetNullableLong(initialSnapshot, "PreviewD3DFrameStatsMissedRefreshCount") ?? 0;
        var missedRefreshEnd = GetNullableLong(lastSnapshot, "PreviewD3DFrameStatsMissedRefreshCount") ?? 0;
        var failureStart = GetNullableLong(initialSnapshot, "PreviewD3DFrameStatsFailureCount") ?? 0;
        var failureEnd = GetNullableLong(lastSnapshot, "PreviewD3DFrameStatsFailureCount") ?? 0;
        var metrics = new PreviewD3DMetrics
        {
            MissedRefreshDelta = Math.Max(0, missedRefreshEnd - missedRefreshStart),
            StatsFailureDelta = Math.Max(0, failureEnd - failureStart),
            InputUploadCpuP99MsAtEnd = GetDouble(lastSnapshot, "PreviewD3DInputUploadCpuP99Ms"),
            RenderSubmitCpuP99MsAtEnd = GetDouble(lastSnapshot, "PreviewD3DRenderSubmitCpuP99Ms"),
            PresentCallP99MsAtEnd = GetDouble(lastSnapshot, "PreviewD3DPresentCallP99Ms"),
            TotalFrameCpuP99MsAtEnd = GetDouble(lastSnapshot, "PreviewD3DTotalFrameCpuP99Ms")
        };

        foreach (var sample in samples)
        {
            ObservePreviewD3DCpuTiming(metrics, sample.Snapshot);
            metrics.MaxRecentSlowFramesObserved = Math.Max(
                metrics.MaxRecentSlowFramesObserved,
                CountArrayItems(sample.Snapshot, "PreviewD3DRecentSlowFrames"));
            if (TryGetLatestSlowFrame(sample.Snapshot, out var slowFrame))
            {
                ApplySlowFrame(metrics, slowFrame);
            }
        }

        metrics.MaxRecentSlowFramesObserved = Math.Max(
            metrics.MaxRecentSlowFramesObserved,
            CountArrayItems(lastSnapshot, "PreviewD3DRecentSlowFrames"));
        ObservePreviewD3DCpuTiming(metrics, lastSnapshot);
        if (TryGetLatestSlowFrame(lastSnapshot, out var lastSlowFrame))
        {
            ApplySlowFrame(metrics, lastSlowFrame);
        }

        return metrics;
    }

    private static void ApplySlowFrame(PreviewD3DMetrics metrics, JsonElement slowFrame)
    {
        metrics.LatestSlowFrameReason = GetSlowFrameReason(slowFrame);
        metrics.LatestSlowFrameOverBudgetMs = GetDouble(slowFrame, "WorstOverBudgetMs");
        metrics.LatestSlowFramePresentIntervalMs = GetDouble(slowFrame, "PresentIntervalMs");
        metrics.LatestSlowFrameTotalFrameCpuMs = GetDouble(slowFrame, "TotalFrameCpuMs");
        metrics.LatestSlowFramePresentCallMs = GetDouble(slowFrame, "PresentCallMs");
        metrics.LatestSlowFramePendingFrameCount = GetInt(slowFrame, "PendingFrameCount");
    }

    private static void ObservePreviewD3DCpuTiming(PreviewD3DMetrics metrics, JsonElement snapshot)
    {
        metrics.InputUploadCpuMaxMsObserved = Math.Max(
            metrics.InputUploadCpuMaxMsObserved,
            GetDouble(snapshot, "PreviewD3DInputUploadCpuMaxMs"));
        metrics.RenderSubmitCpuMaxMsObserved = Math.Max(
            metrics.RenderSubmitCpuMaxMsObserved,
            GetDouble(snapshot, "PreviewD3DRenderSubmitCpuMaxMs"));
        metrics.PresentCallMaxMsObserved = Math.Max(
            metrics.PresentCallMaxMsObserved,
            GetDouble(snapshot, "PreviewD3DPresentCallMaxMs"));
        metrics.TotalFrameCpuMaxMsObserved = Math.Max(
            metrics.TotalFrameCpuMaxMsObserved,
            GetDouble(snapshot, "PreviewD3DTotalFrameCpuMaxMs"));
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
