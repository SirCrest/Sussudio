using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackMetrics
{
    private static void ObserveExportSnapshot(
        FlashbackExportSessionMetrics metrics,
        JsonElement snapshot,
        long baselineExportId,
        bool baselineExportActive)
    {
        var exportId = GetNullableLong(snapshot, "FlashbackExportId") ?? 0;
        var status = GetString(snapshot, "FlashbackExportStatus") ?? string.Empty;
        var active = GetBool(snapshot, "FlashbackExportActive");
        var relevantToSession =
            active ||
            exportId > baselineExportId ||
            baselineExportActive && exportId == baselineExportId ||
            baselineExportId <= 0 &&
            !string.IsNullOrWhiteSpace(status) &&
            !string.Equals(status, "NotStarted", StringComparison.OrdinalIgnoreCase);
        if (!relevantToSession)
        {
            return;
        }

        metrics.Observed = true;
        metrics.ActiveAtEnd = active;
        metrics.StatusAtEnd = status;
        metrics.MessageAtEnd = GetString(snapshot, "FlashbackExportMessage") ?? string.Empty;
        metrics.FailureKindAtEnd = GetString(snapshot, "FlashbackExportFailureKind") ?? string.Empty;
        metrics.OutputPathAtEnd = GetString(snapshot, "FlashbackExportOutputPath") ?? string.Empty;
        var lastExportId = GetNullableLong(snapshot, "LastExportId") ?? 0;
        metrics.LastExportIdAtEnd = lastExportId;
        if (!active && exportId > 0 && lastExportId == exportId)
        {
            metrics.LastSuccessAtEnd = GetString(snapshot, "LastExportSuccess") ?? string.Empty;
            metrics.LastMessageAtEnd = GetString(snapshot, "LastExportMessage") ?? string.Empty;
        }
        else
        {
            metrics.LastSuccessAtEnd = string.Empty;
            metrics.LastMessageAtEnd = string.Empty;
        }

        metrics.MaxElapsedMsObserved = Math.Max(
            metrics.MaxElapsedMsObserved,
            GetNullableLong(snapshot, "FlashbackExportElapsedMs") ?? 0);
        metrics.MaxLastProgressAgeMsObserved = Math.Max(
            metrics.MaxLastProgressAgeMsObserved,
            GetNullableLong(snapshot, "FlashbackExportLastProgressAgeMs") ?? 0);
        metrics.MaxOutputBytesObserved = Math.Max(
            metrics.MaxOutputBytesObserved,
            GetNullableLong(snapshot, "FlashbackExportOutputBytes") ?? 0);
        metrics.MaxThroughputBytesPerSecObserved = Math.Max(
            metrics.MaxThroughputBytesPerSecObserved,
            GetDouble(snapshot, "FlashbackExportThroughputBytesPerSec"));
    }
}
