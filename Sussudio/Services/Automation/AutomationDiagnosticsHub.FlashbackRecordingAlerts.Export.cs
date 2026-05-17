using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private void UpdateFlashbackExportAlerts(
        AutomationSnapshot snapshot,
        long exportLastProgressAgeMs,
        FlashbackRecordingRecentCounters flashbackRecordingRecent,
        bool flashbackRecordingRecentForceRotateGap)
    {
        SetAlertState(
            "flashback-export-stalled",
            snapshot.FlashbackExportActive &&
            exportLastProgressAgeMs >= FlashbackExportStallThresholdMs,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback export has not reported progress for {exportLastProgressAgeMs}ms " +
            $"(id={snapshot.FlashbackExportId}, status={snapshot.FlashbackExportStatus}, " +
            $"progress={snapshot.FlashbackExportPercent:0.##}%, segments={snapshot.FlashbackExportSegmentsProcessed}/{snapshot.FlashbackExportTotalSegments}).",
            "Flashback export progress resumed.",
            throttleMs: 10000);

        SetAlertState(
            "flashback-export-rotation-gap",
            flashbackRecordingRecentForceRotateGap,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback export rotation skipped live-edge frames: recentSeqGaps={flashbackRecordingRecent.SequenceGaps} " +
            $"queueRejects={snapshot.FlashbackVideoQueueRejectedFrames} lastReject={snapshot.FlashbackVideoQueueLastRejectReason} " +
            $"exportStatus={snapshot.FlashbackExportStatus} exportId={snapshot.FlashbackExportId}.",
            "Flashback export rotation is no longer skipping live-edge frames.",
            throttleMs: 5000);
    }
}
