using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static DiagnosticEvaluation? TryBuildFlashbackRecordingDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        bool isRecording,
        FlashbackRecordingRecentCounters recentFlashbackRecording,
        DiagnosticEvaluationLanes lanes)
    {
        var flashbackForceRotateRejectWithoutDamage =
            health.FlashbackActive &&
            health.FlashbackVideoSequenceGaps > 0 &&
            health.FlashbackVideoQueueRejectedFrames > 0 &&
            health.FlashbackDroppedFrames <= 0 &&
            health.FlashbackVideoEncoderDroppedFrames <= 0 &&
            health.FlashbackGpuFramesDropped <= 0 &&
            recentFlashbackRecording.BackpressureEvents <= 0 &&
            !IsFlashbackRecordingQueueBackedUp(
                health.FlashbackVideoQueueDepth,
                health.FlashbackVideoQueueCapacity,
                health.FlashbackVideoQueueOldestFrameAgeMs) &&
            IsFlashbackForceRotateRejectReason(health.FlashbackVideoQueueLastRejectReason);
        var flashbackRecordingRecentBackpressure =
            recentFlashbackRecording.BackpressureEvents > 0 &&
            health.FlashbackVideoBackpressureLastWaitMs >= FlashbackRecordingBackpressureWarningMs;
        var flashbackRecordingDegraded =
            health.FlashbackActive &&
            (recentFlashbackRecording.DroppedFrames > 0 ||
             recentFlashbackRecording.EncoderDroppedFrames > 0 ||
             (!flashbackForceRotateRejectWithoutDamage &&
              recentFlashbackRecording.SequenceGaps > 0) ||
             recentFlashbackRecording.GpuFramesDropped > 0 ||
             flashbackRecordingRecentBackpressure ||
             IsFlashbackRecordingQueueBackedUp(
                 health.FlashbackVideoQueueDepth,
                 health.FlashbackVideoQueueCapacity,
                 health.FlashbackVideoQueueOldestFrameAgeMs) ||
             IsFlashbackAudioQueueBackedUp(
                 health.FlashbackAudioQueueDepth,
                 health.FlashbackAudioQueueCapacity));
        var flashbackBackendSettingsUnexpectedlyStale =
            health.FlashbackActive &&
            health.FlashbackBackendSettingsStale &&
            !isRecording;
        var flashbackExportRotationGap =
            flashbackForceRotateRejectWithoutDamage &&
            (health.FlashbackExportActive ||
             health.FlashbackForceRotateActive ||
             health.FlashbackForceRotateRequested ||
             health.FlashbackForceRotateDraining);

        if (health.FlashbackEncodingFailed)
        {
            return new DiagnosticEvaluation(
                "Critical",
                "flashback_recording",
                "Flashback encoder has failed.",
                lanes.FlashbackRecording,
                lanes.Source,
                lanes.Decode,
                lanes.Preview,
                lanes.Render,
                lanes.Present,
                lanes.Recording,
                lanes.Audio);
        }

        if (flashbackExportRotationGap)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_export",
                "Flashback export rotation skipped live-edge frames.",
                lanes.FlashbackRecording,
                lanes.Source,
                lanes.Decode,
                lanes.Preview,
                lanes.Render,
                lanes.Present,
                lanes.Recording,
                lanes.Audio);
        }

        if (flashbackBackendSettingsUnexpectedlyStale)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_recording",
                "Flashback backend settings differ from requested settings.",
                lanes.FlashbackRecording,
                lanes.Source,
                lanes.Decode,
                lanes.Preview,
                lanes.Render,
                lanes.Present,
                lanes.Recording,
                lanes.Audio);
        }

        if (!flashbackRecordingDegraded)
        {
            return null;
        }

        return new DiagnosticEvaluation(
            "Warning",
            "flashback_recording",
            "Flashback recording path is dropping or backing up.",
            lanes.FlashbackRecording,
            lanes.Source,
            lanes.Decode,
            lanes.Preview,
            lanes.Render,
            lanes.Present,
            lanes.Recording,
            lanes.Audio);
    }
}
