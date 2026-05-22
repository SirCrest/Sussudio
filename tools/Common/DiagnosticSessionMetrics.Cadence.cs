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

internal static partial class DiagnosticSessionMetrics
{
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
}
