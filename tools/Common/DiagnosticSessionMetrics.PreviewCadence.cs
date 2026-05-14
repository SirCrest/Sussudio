using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionMetrics
{
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

    private static void ObservePreviewCadenceSnapshot(PreviewCadenceSessionMetrics metrics, JsonElement snapshot)
    {
        var onePercentLow = GetDouble(snapshot, "PreviewCadenceOnePercentLowFps");
        if (onePercentLow > 0)
        {
            metrics.MinOnePercentLowFpsObserved = Math.Min(metrics.MinOnePercentLowFpsObserved, onePercentLow);
        }
    }

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
