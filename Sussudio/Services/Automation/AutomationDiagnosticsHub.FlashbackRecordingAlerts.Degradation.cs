using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
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
