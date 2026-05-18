using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionMetrics;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackMetrics
{
    internal static FlashbackPlaybackSessionMetrics BuildFlashbackPlaybackSessionMetrics(
        JsonElement initialSnapshot,
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement lastSnapshot)
    {
        var metrics = new FlashbackPlaybackSessionMetrics { BaselineSnapshot = initialSnapshot };
        var baselinePlaybackActive = IsPlaybackSnapshotActive(initialSnapshot);
        var baselineFrameCount = GetNullableLong(initialSnapshot, "FlashbackPlaybackFrameCount") ?? 0;
        var baselineCommandsEnqueued = GetNullableLong(initialSnapshot, "FlashbackPlaybackCommandsEnqueued") ?? 0;
        var baselineCommandsProcessed = GetNullableLong(initialSnapshot, "FlashbackPlaybackCommandsProcessed") ?? 0;
        foreach (var sample in samples)
        {
            ObservePlaybackSnapshot(
                metrics,
                sample.Snapshot,
                sample.OffsetMs,
                baselineFrameCount,
                baselineCommandsEnqueued,
                baselineCommandsProcessed,
                baselinePlaybackActive);
        }

        ObservePlaybackSnapshot(
            metrics,
            lastSnapshot,
            samples.Count > 0 ? samples[^1].OffsetMs : 0,
            baselineFrameCount,
            baselineCommandsEnqueued,
            baselineCommandsProcessed,
            baselinePlaybackActive);

        if (double.IsPositiveInfinity(metrics.MinOnePercentLowFpsObserved))
        {
            metrics.MinOnePercentLowFpsObserved = 0;
        }

        if (double.IsPositiveInfinity(metrics.MinObservedFpsObserved))
        {
            metrics.MinObservedFpsObserved = 0;
        }

        if (metrics.Observed)
        {
            metrics.DroppedFramesDelta = GetResetAwareCounterDelta(
                metrics.EndSnapshot,
                initialSnapshot,
                "FlashbackPlaybackDroppedFrames");
            metrics.SubmitFailuresDelta = GetResetAwareCounterDelta(
                metrics.EndSnapshot,
                initialSnapshot,
                "FlashbackPlaybackSubmitFailures");
        }

        return metrics;
    }
}
