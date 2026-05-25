using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static DiagnosticEvaluation? TryBuildFlashbackDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        bool isRecording,
        FlashbackRecordingRecentCounters recentFlashbackRecording,
        DiagnosticEvaluationLanes lanes,
        double playbackTargetFps,
        long playbackCommandQueueAgeMs,
        bool playbackCommandFailedRecently)
    {
        return TryBuildFlashbackStorageDiagnosticEvaluation(health, lanes) ??
               TryBuildFlashbackRecordingDiagnosticEvaluation(health, isRecording, recentFlashbackRecording, lanes) ??
               TryBuildFlashbackExportDiagnosticEvaluation(health, lanes) ??
               TryBuildFlashbackPlaybackDiagnosticEvaluation(
                   health,
                   lanes,
                   playbackTargetFps,
                   playbackCommandQueueAgeMs,
                   playbackCommandFailedRecently);
    }

    private static DiagnosticEvaluation? TryBuildFlashbackStorageDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        DiagnosticEvaluationLanes lanes)
    {
        var flashbackTempPressure =
            health.FlashbackActive &&
            (health.FlashbackStartupCacheOverBudget ||
             (health.FlashbackTempDriveFreeBytes >= 0 && health.FlashbackTempDriveFreeBytes < FlashbackTempDriveLowFreeBytes));

        if (!flashbackTempPressure)
        {
            return null;
        }

        return new DiagnosticEvaluation(
            "Warning",
            "flashback_storage",
            "Flashback temp storage is under pressure.",
            lanes.TempCache,
            lanes.Source,
            lanes.Decode,
            lanes.Preview,
            lanes.Render,
            lanes.Present,
            lanes.Recording,
            lanes.Audio);
    }

    private static DiagnosticEvaluation? TryBuildFlashbackExportDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        DiagnosticEvaluationLanes lanes)
    {
        if (!health.FlashbackExportActive)
        {
            return null;
        }

        var exportLastProgressAgeMs = Math.Max(0, health.FlashbackExportLastProgressAgeMs);
        if (exportLastProgressAgeMs >= FlashbackExportStallThresholdMs)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_export",
                "Flashback export progress is stalled.",
                $"{lanes.Export} progressAgeMs={exportLastProgressAgeMs}",
                lanes.Source,
                lanes.Decode,
                lanes.Preview,
                lanes.Render,
                lanes.Present,
                lanes.Recording,
                lanes.Audio);
        }

        return new DiagnosticEvaluation(
            "Busy",
            "flashback_export",
            "Flashback export is running.",
            lanes.Export,
            lanes.Source,
            lanes.Decode,
            lanes.Preview,
            lanes.Render,
            lanes.Present,
            lanes.Recording,
            lanes.Audio);
    }

    private static DiagnosticEvaluation? TryBuildFlashbackRecordingDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        bool isRecording,
        FlashbackRecordingRecentCounters recentFlashbackRecording,
        DiagnosticEvaluationLanes lanes)
    {
        var conditions = BuildFlashbackRecordingDiagnosticConditions(
            health,
            isRecording,
            recentFlashbackRecording);

        return TryBuildFlashbackEncoderFailureDiagnosticEvaluation(health, lanes) ??
               TryBuildFlashbackExportRotationDiagnosticEvaluation(conditions, lanes) ??
               TryBuildFlashbackBackendSettingsDiagnosticEvaluation(conditions, lanes) ??
               TryBuildFlashbackRecordingDegradationDiagnosticEvaluation(conditions, lanes);
    }

    private static DiagnosticEvaluation? TryBuildFlashbackEncoderFailureDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        DiagnosticEvaluationLanes lanes)
    {
        if (!health.FlashbackEncodingFailed)
        {
            return null;
        }

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

    private static FlashbackRecordingDiagnosticConditions BuildFlashbackRecordingDiagnosticConditions(
        CaptureHealthSnapshot health,
        bool isRecording,
        FlashbackRecordingRecentCounters recentFlashbackRecording)
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

        return new FlashbackRecordingDiagnosticConditions(
            flashbackExportRotationGap,
            flashbackBackendSettingsUnexpectedlyStale,
            flashbackRecordingDegraded);
    }

    private static DiagnosticEvaluation? TryBuildFlashbackExportRotationDiagnosticEvaluation(
        FlashbackRecordingDiagnosticConditions conditions,
        DiagnosticEvaluationLanes lanes)
    {
        if (!conditions.ExportRotationGap)
        {
            return null;
        }

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

    private static DiagnosticEvaluation? TryBuildFlashbackBackendSettingsDiagnosticEvaluation(
        FlashbackRecordingDiagnosticConditions conditions,
        DiagnosticEvaluationLanes lanes)
    {
        if (!conditions.BackendSettingsUnexpectedlyStale)
        {
            return null;
        }

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

    private static DiagnosticEvaluation? TryBuildFlashbackRecordingDegradationDiagnosticEvaluation(
        FlashbackRecordingDiagnosticConditions conditions,
        DiagnosticEvaluationLanes lanes)
    {
        if (!conditions.RecordingDegraded)
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

    private static DiagnosticEvaluation? TryBuildFlashbackPlaybackDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        DiagnosticEvaluationLanes lanes,
        double playbackTargetFps,
        long playbackCommandQueueAgeMs,
        bool playbackCommandFailedRecently)
    {
        var playbackSlow =
            string.Equals(health.FlashbackPlaybackState, "Playing", StringComparison.OrdinalIgnoreCase) &&
            playbackTargetFps > 0 &&
            health.FlashbackPlaybackFrameCount >= FlashbackPlaybackMinFramesForPerfAlert &&
            health.FlashbackPlaybackObservedFps > 0 &&
            health.FlashbackPlaybackObservedFps < playbackTargetFps * FlashbackPlaybackSlowFpsRatio;
        var playbackFrametimeDegraded =
            IsFlashbackPlaybackFrametimeDegraded(
                health.FlashbackPlaybackState,
                playbackTargetFps,
                health.FlashbackPlaybackFrameCount,
                health.FlashbackPlaybackCadenceSampleCount,
                health.FlashbackPlaybackOnePercentLowFps);

        if (playbackCommandQueueAgeMs >= FlashbackPlaybackCommandStallThresholdMs)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_playback",
                "Flashback playback command queue is stalled.",
                lanes.PlaybackCommand,
                lanes.Source,
                lanes.Decode,
                lanes.Preview,
                lanes.Render,
                lanes.Present,
                lanes.Recording,
                lanes.Audio);
        }

        if (playbackCommandFailedRecently)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_playback",
                "Flashback playback command failed recently.",
                lanes.PlaybackCommand,
                lanes.Source,
                lanes.Decode,
                lanes.Preview,
                lanes.Render,
                lanes.Present,
                lanes.Recording,
                lanes.Audio);
        }

        if (playbackSlow)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_playback",
                "Flashback playback is below target rate.",
                lanes.PlaybackPerf,
                lanes.Source,
                lanes.Decode,
                lanes.Preview,
                lanes.Render,
                lanes.Present,
                lanes.Recording,
                lanes.Audio);
        }

        if (playbackFrametimeDegraded)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_playback",
                "Flashback playback frametime is below target.",
                lanes.PlaybackPerf,
                lanes.Source,
                lanes.Decode,
                lanes.Preview,
                lanes.Render,
                lanes.Present,
                lanes.Recording,
                lanes.Audio);
        }

        if (health.FlashbackPlaybackSubmitFailures <= 0)
        {
            return null;
        }

        return new DiagnosticEvaluation(
            "Warning",
            "flashback_playback",
            "Flashback playback frame submission failed.",
            lanes.PlaybackPerf,
            lanes.Source,
            lanes.Decode,
            lanes.Preview,
            lanes.Render,
            lanes.Present,
            lanes.Recording,
            lanes.Audio);
    }

    private readonly record struct FlashbackRecordingDiagnosticConditions(
        bool ExportRotationGap,
        bool BackendSettingsUnexpectedlyStale,
        bool RecordingDegraded);
}
