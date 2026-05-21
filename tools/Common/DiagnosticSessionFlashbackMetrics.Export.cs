using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionMetrics;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackMetrics
{
    internal static FlashbackExportSessionMetrics BuildFlashbackExportSessionMetrics(
        JsonElement initialSnapshot,
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement lastSnapshot)
    {
        var metrics = new FlashbackExportSessionMetrics();
        var baselineExportId = GetNullableLong(initialSnapshot, "FlashbackExportId") ?? 0;
        var baselineExportActive = GetBool(initialSnapshot, "FlashbackExportActive");
        foreach (var sample in samples)
        {
            ObserveExportSnapshot(metrics, sample.Snapshot, baselineExportId, baselineExportActive);
        }

        ObserveExportSnapshot(metrics, lastSnapshot, baselineExportId, baselineExportActive);
        metrics.ForceRotateFallbacksAtEnd = GetNullableLong(lastSnapshot, "FlashbackExportForceRotateFallbacks") ?? 0;
        metrics.ForceRotateFallbacksDelta = GetCounterDelta(
            lastSnapshot,
            initialSnapshot,
            "FlashbackExportForceRotateFallbacks");
        metrics.LastForceRotateFallbackSegmentsAtEnd =
            GetInt(lastSnapshot, "FlashbackExportLastForceRotateFallbackSegments");
        return metrics;
    }
}
