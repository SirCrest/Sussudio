using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

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
}
