using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal sealed class SourceCadenceSessionMetrics
{
    public long MaxSevereGapCountObserved { get; set; }
    public long MaxEstimatedDroppedFramesObserved { get; set; }
    public double MaxDropPercentObserved { get; set; }
}

internal sealed class PreviewCadenceSessionMetrics
{
    public double OnePercentLowFpsAtEnd { get; init; }
    public double MinOnePercentLowFpsObserved { get; set; } = double.PositiveInfinity;
}

internal sealed class VisualCadenceSessionMetrics
{
    public double OutputFpsAtEnd { get; init; }
    public double ChangeFpsAtEnd { get; init; }
    public double MinChangeFpsObserved { get; set; } = double.PositiveInfinity;
    public double RepeatPercentAtEnd { get; init; }
    public double MaxRepeatPercentObserved { get; set; }
    public long RepeatFramesAtEnd { get; init; }
    public long LongestRepeatRunAtEnd { get; init; }
}

internal sealed class PreviewD3DMetrics
{
    public long MissedRefreshDelta { get; init; }
    public long StatsFailureDelta { get; init; }
    public int MaxRecentSlowFramesObserved { get; set; }
    public string LatestSlowFrameReason { get; set; } = string.Empty;
    public double LatestSlowFrameOverBudgetMs { get; set; }
    public double LatestSlowFramePresentIntervalMs { get; set; }
    public double LatestSlowFrameTotalFrameCpuMs { get; set; }
    public double LatestSlowFramePresentCallMs { get; set; }
    public int LatestSlowFramePendingFrameCount { get; set; }
    public double InputUploadCpuP99MsAtEnd { get; init; }
    public double InputUploadCpuMaxMsObserved { get; set; }
    public double RenderSubmitCpuP99MsAtEnd { get; init; }
    public double RenderSubmitCpuMaxMsObserved { get; set; }
    public double PresentCallP99MsAtEnd { get; init; }
    public double PresentCallMaxMsObserved { get; set; }
    public double TotalFrameCpuP99MsAtEnd { get; init; }
    public double TotalFrameCpuMaxMsObserved { get; set; }
}

internal readonly record struct PlaybackCommandHealth(
    long Dropped,
    long Skipped,
    long SubmitFailures,
    long CoalescedScrub,
    long CoalescedSeek,
    long NonCoalescedDropped);

internal static class DiagnosticSessionMetrics
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

    internal static PlaybackCommandHealth BuildPlaybackCommandHealth(JsonElement snapshot, JsonElement baselineSnapshot)
    {
        var dropped = GetCounterDelta(snapshot, baselineSnapshot, "FlashbackPlaybackCommandsDropped");
        var skipped = GetCounterDelta(snapshot, baselineSnapshot, "FlashbackPlaybackCommandsSkippedNotReady");
        var submitFailures = GetCounterDelta(snapshot, baselineSnapshot, "FlashbackPlaybackSubmitFailures");
        var coalescedScrub = GetCounterDelta(snapshot, baselineSnapshot, "FlashbackPlaybackScrubUpdatesCoalesced");
        var coalescedSeek = GetCounterDelta(snapshot, baselineSnapshot, "FlashbackPlaybackSeekCommandsCoalesced");
        return new PlaybackCommandHealth(
            dropped,
            skipped,
            submitFailures,
            coalescedScrub,
            coalescedSeek,
            Math.Max(0, dropped - coalescedScrub));
    }

    internal static SourceCadenceSessionMetrics BuildSourceCadenceSessionMetrics(
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement lastSnapshot)
    {
        var metrics = new SourceCadenceSessionMetrics();
        ObserveSourceCadenceSnapshot(metrics, lastSnapshot);
        foreach (var sample in samples)
        {
            ObserveSourceCadenceSnapshot(metrics, sample.Snapshot);
        }

        return metrics;
    }

    private static void ObserveSourceCadenceSnapshot(SourceCadenceSessionMetrics metrics, JsonElement snapshot)
    {
        metrics.MaxSevereGapCountObserved = Math.Max(
            metrics.MaxSevereGapCountObserved,
            GetNullableLong(snapshot, "CaptureCadenceSevereGapCount") ?? 0);
        metrics.MaxEstimatedDroppedFramesObserved = Math.Max(
            metrics.MaxEstimatedDroppedFramesObserved,
            GetNullableLong(snapshot, "CaptureCadenceEstimatedDroppedFrames") ?? 0);
        metrics.MaxDropPercentObserved = Math.Max(
            metrics.MaxDropPercentObserved,
            GetDouble(snapshot, "CaptureCadenceEstimatedDropPercent"));
    }

    internal static PreviewCadenceSessionMetrics BuildPreviewCadenceSessionMetrics(
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement lastSnapshot)
    {
        var metrics = new PreviewCadenceSessionMetrics
        {
            OnePercentLowFpsAtEnd = GetDouble(lastSnapshot, "PreviewCadenceOnePercentLowFps")
        };
        ObservePreviewCadenceSnapshot(metrics, lastSnapshot);
        foreach (var sample in samples)
        {
            ObservePreviewCadenceSnapshot(metrics, sample.Snapshot);
        }

        if (double.IsPositiveInfinity(metrics.MinOnePercentLowFpsObserved))
        {
            metrics.MinOnePercentLowFpsObserved = 0;
        }

        return metrics;
    }

    private static void ObservePreviewCadenceSnapshot(PreviewCadenceSessionMetrics metrics, JsonElement snapshot)
    {
        var onePercentLow = GetDouble(snapshot, "PreviewCadenceOnePercentLowFps");
        if (onePercentLow > 0)
        {
            metrics.MinOnePercentLowFpsObserved = Math.Min(metrics.MinOnePercentLowFpsObserved, onePercentLow);
        }
    }

    internal static VisualCadenceSessionMetrics BuildVisualCadenceSessionMetrics(
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement lastSnapshot)
    {
        var metrics = new VisualCadenceSessionMetrics
        {
            OutputFpsAtEnd = GetDouble(lastSnapshot, "VisualCadenceOutputObservedFps"),
            ChangeFpsAtEnd = GetDouble(lastSnapshot, "VisualCadenceChangeObservedFps"),
            RepeatPercentAtEnd = GetDouble(lastSnapshot, "VisualCadenceRepeatFramePercent"),
            RepeatFramesAtEnd = GetNullableLong(lastSnapshot, "VisualCadenceRepeatFrameCount") ?? 0,
            LongestRepeatRunAtEnd = GetNullableLong(lastSnapshot, "VisualCadenceLongestRepeatRun") ?? 0
        };
        ObserveVisualCadenceSnapshot(metrics, lastSnapshot);
        foreach (var sample in samples)
        {
            ObserveVisualCadenceSnapshot(metrics, sample.Snapshot);
        }

        if (double.IsPositiveInfinity(metrics.MinChangeFpsObserved))
        {
            metrics.MinChangeFpsObserved = 0;
        }

        return metrics;
    }

    internal static bool IsVisualCadenceSessionHealthy(VisualCadenceSessionMetrics metrics, double targetFps)
        => targetFps > 0 &&
           metrics.MinChangeFpsObserved >= targetFps * 0.98 &&
           metrics.MaxRepeatPercentObserved <= 1.0 &&
           metrics.LongestRepeatRunAtEnd <= 1;

    private static void ObserveVisualCadenceSnapshot(VisualCadenceSessionMetrics metrics, JsonElement snapshot)
    {
        var changeFps = GetDouble(snapshot, "VisualCadenceChangeObservedFps");
        if (changeFps > 0)
        {
            metrics.MinChangeFpsObserved = Math.Min(metrics.MinChangeFpsObserved, changeFps);
        }

        metrics.MaxRepeatPercentObserved = Math.Max(
            metrics.MaxRepeatPercentObserved,
            GetDouble(snapshot, "VisualCadenceRepeatFramePercent"));
    }

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
