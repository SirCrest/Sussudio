using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private void UpdateFlashbackRecordingAlerts(
        AutomationSnapshot snapshot,
        FlashbackRecordingRecentCounters flashbackRecordingRecent)
    {
        var flashbackRecordingRecentBackpressure =
            flashbackRecordingRecent.BackpressureEvents > 0 &&
            snapshot.FlashbackVideoBackpressureLastWaitMs >= FlashbackRecordingBackpressureWarningMs;
        var flashbackRecordingQueueBacklog =
            IsFlashbackRecordingQueueBackedUp(
                snapshot.FlashbackVideoQueueDepth,
                snapshot.FlashbackVideoQueueCapacity,
                snapshot.FlashbackVideoQueueOldestFrameAgeMs);
        var flashbackAudioQueueBacklog =
            IsFlashbackAudioQueueBackedUp(
                snapshot.FlashbackAudioQueueDepth,
                snapshot.FlashbackAudioQueueCapacity);
        var flashbackRecordingRecentForceRotateGap =
            snapshot.FlashbackActive &&
            flashbackRecordingRecent.SequenceGaps > 0 &&
            snapshot.FlashbackVideoQueueRejectedFrames > 0 &&
            IsFlashbackForceRotateRejectReason(snapshot.FlashbackVideoQueueLastRejectReason);
        var exportLastProgressAgeMs = snapshot.FlashbackExportActive
            ? Math.Max(0, snapshot.FlashbackExportLastProgressAgeMs)
            : 0;

        UpdateFlashbackExportAlerts(snapshot, exportLastProgressAgeMs, flashbackRecordingRecent, flashbackRecordingRecentForceRotateGap);
        UpdateFlashbackStorageAlerts(snapshot);
        UpdateFlashbackEncoderAlerts(snapshot);
        UpdateFlashbackRecordingDegradationAlert(
            snapshot,
            flashbackRecordingRecent,
            flashbackRecordingRecentForceRotateGap,
            flashbackRecordingRecentBackpressure,
            flashbackRecordingQueueBacklog,
            flashbackAudioQueueBacklog);
    }

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

    private void UpdateFlashbackStorageAlerts(AutomationSnapshot snapshot)
    {
        SetAlertState(
            "flashback-temp-cache-pressure",
            snapshot.FlashbackActive &&
            (snapshot.FlashbackStartupCacheOverBudget ||
             (snapshot.FlashbackTempDriveFreeBytes >= 0 && snapshot.FlashbackTempDriveFreeBytes < FlashbackTempDriveLowFreeBytes)),
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback temp storage is under pressure: freeBytes={snapshot.FlashbackTempDriveFreeBytes} " +
            $"cacheBytes={snapshot.FlashbackStartupCacheBytes} budgetBytes={snapshot.FlashbackStartupCacheBudgetBytes} " +
            $"sessions={snapshot.FlashbackStartupCacheSessionCount} deleted={snapshot.FlashbackStartupCacheDeletedSessionCount} " +
            $"freedBytes={snapshot.FlashbackStartupCacheFreedBytes} overBudget={snapshot.FlashbackStartupCacheOverBudget}.",
            "Flashback temp storage returned to healthy range.",
            throttleMs: 10000);
    }

    private void UpdateFlashbackEncoderAlerts(AutomationSnapshot snapshot)
    {
        SetAlertState(
            "flashback-encoding-failed",
            snapshot.FlashbackEncodingFailed,
            DiagnosticsSeverity.Error,
            DiagnosticsCategory.Flashback,
            string.IsNullOrWhiteSpace(snapshot.FlashbackEncodingFailureMessage)
                ? $"Flashback encoder failed: type={snapshot.FlashbackEncodingFailureType ?? "Unknown"}."
                : $"Flashback encoder failed: type={snapshot.FlashbackEncodingFailureType ?? "Unknown"} message={snapshot.FlashbackEncodingFailureMessage}.",
            "Flashback encoder failure cleared.",
            throttleMs: 5000);
    }

    private void UpdateFlashbackRecordingDegradationAlert(
        AutomationSnapshot snapshot,
        FlashbackRecordingRecentCounters flashbackRecordingRecent,
        bool flashbackRecordingRecentForceRotateGap,
        bool flashbackRecordingRecentBackpressure,
        bool flashbackRecordingQueueBacklog,
        bool flashbackAudioQueueBacklog)
    {
        SetAlertState(
            "flashback-recording-degraded",
            snapshot.FlashbackActive &&
            (flashbackRecordingRecent.DroppedFrames > 0 ||
             flashbackRecordingRecent.EncoderDroppedFrames > 0 ||
             (flashbackRecordingRecent.SequenceGaps > 0 && !flashbackRecordingRecentForceRotateGap) ||
             flashbackRecordingRecent.GpuFramesDropped > 0 ||
             flashbackRecordingRecentBackpressure ||
             flashbackRecordingQueueBacklog ||
             flashbackAudioQueueBacklog),
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Flashback,
            $"Flashback recording path degraded: recentDropped={flashbackRecordingRecent.DroppedFrames} recentEncoderDrops={flashbackRecordingRecent.EncoderDroppedFrames} " +
            $"recentSeqGaps={flashbackRecordingRecent.SequenceGaps} recentGpuOverloads={flashbackRecordingRecent.GpuFramesDropped} " +
            $"recentBackpressureEvents={flashbackRecordingRecent.BackpressureEvents} " +
            $"totals=dropped:{snapshot.FlashbackDroppedFrames},encoderDrops:{snapshot.FlashbackVideoEncoderDroppedFrames},seqGaps:{snapshot.FlashbackVideoSequenceGaps},gpuOverloads:{snapshot.FlashbackGpuFramesDropped} " +
            $"forceRotate={snapshot.FlashbackForceRotateActive} requested={snapshot.FlashbackForceRotateRequested} draining={snapshot.FlashbackForceRotateDraining} " +
            $"queue={snapshot.FlashbackVideoQueueDepth}/{snapshot.FlashbackVideoQueueCapacity} maxQueue={snapshot.FlashbackVideoQueueMaxDepth} " +
            $"audioQueue={snapshot.FlashbackAudioQueueDepth}/{snapshot.FlashbackAudioQueueCapacity} " +
            $"backpressure={snapshot.FlashbackVideoBackpressureWaitMs}ms/{snapshot.FlashbackVideoBackpressureEvents} last={snapshot.FlashbackVideoBackpressureLastWaitMs}ms max={snapshot.FlashbackVideoBackpressureMaxWaitMs}ms.",
            "Flashback recording path returned to healthy range.",
            throttleMs: 5000);
    }
}
