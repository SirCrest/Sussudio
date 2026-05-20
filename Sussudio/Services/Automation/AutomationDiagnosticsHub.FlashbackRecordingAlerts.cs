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
}
