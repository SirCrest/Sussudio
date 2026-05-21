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
}
