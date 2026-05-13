using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static DiagnosticEvaluation BuildDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        CaptureRuntimeSnapshot captureRuntime,
        PreviewRuntimeSnapshot previewRuntime,
        bool isPreviewing,
        bool isRecording,
        PerformanceEvaluation performance,
        MjpegRecentCounters recentMjpeg,
        long recentPreviewUnderflows,
        long recentPreviewDeadlineDrops,
        D3DRendererRecentCounters recentRenderer,
        long recentD3DMissedRefreshes,
        long recentD3DStatsFailures,
        FlashbackRecordingRecentCounters recentFlashbackRecording)
    {
        var nowUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var playbackCommandQueueAgeMs =
            health.FlashbackPlaybackPendingCommands > 0 &&
            health.FlashbackPlaybackLastCommandQueuedUtcUnixMs > 0 &&
            health.FlashbackPlaybackLastCommandQueuedUtcUnixMs > health.FlashbackPlaybackLastCommandProcessedUtcUnixMs
                ? Math.Max(0, nowUnixMs - health.FlashbackPlaybackLastCommandQueuedUtcUnixMs)
                : 0;
        var playbackCommandFailureAgeMs = health.FlashbackPlaybackLastCommandFailureUtcUnixMs > 0
            ? Math.Max(0, nowUnixMs - health.FlashbackPlaybackLastCommandFailureUtcUnixMs)
            : 0;
        var playbackCommandFailure = string.IsNullOrWhiteSpace(health.FlashbackPlaybackLastCommandFailure)
            ? "None"
            : health.FlashbackPlaybackLastCommandFailure;
        var playbackCommandFailedRecently =
            playbackCommandFailureAgeMs > 0 &&
            playbackCommandFailureAgeMs <= FlashbackPlaybackCommandFailureRecentMs;
        var playbackTargetFps = ResolveFlashbackPlaybackTargetFps(
            health.FlashbackPlaybackTargetFps,
            health.ExpectedFrameRate);
        var lanes = BuildDiagnosticEvaluationLanes(
            health,
            captureRuntime,
            previewRuntime,
            recentMjpeg,
            recentPreviewUnderflows,
            recentPreviewDeadlineDrops,
            recentRenderer,
            recentD3DMissedRefreshes,
            recentD3DStatsFailures,
            playbackTargetFps,
            playbackCommandQueueAgeMs,
            playbackCommandFailureAgeMs,
            playbackCommandFailure);
        var sourceLane = lanes.Source;
        var decodeLane = lanes.Decode;
        var previewLane = lanes.Preview;
        var renderLane = lanes.Render;
        var presentLane = lanes.Present;
        var visualLane = lanes.Visual;
        var mjpegDuplicateLane = lanes.MjpegDuplicate;
        var sourceSignalLane = lanes.SourceSignal;
        var recordingLane = lanes.Recording;
        var audioLane = lanes.Audio;
        var flashbackRecordingLane = lanes.FlashbackRecording;
        var exportLane = lanes.Export;
        var tempCacheLane = lanes.TempCache;
        var playbackCommandLane = lanes.PlaybackCommand;
        var playbackPerfLane = lanes.PlaybackPerf;
        var recentRendererSubmitted = lanes.RecentRendererSubmitted;
        var recentRendererDropPercent = lanes.RecentRendererDropPercent;
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
        var captureOnePercentLowDegraded =
            IsCaptureOnePercentLowDegraded(
                health.ExpectedFrameRate,
                health.CaptureCadenceSampleCount,
                health.CaptureCadenceOnePercentLowFps);
        var previewOnePercentLowDegraded =
            IsPreviewOnePercentLowDegraded(
                previewRuntime.DisplayCadenceExpectedIntervalMs,
                previewRuntime.DisplayCadenceSampleCount,
                previewRuntime.DisplayCadenceOnePercentLowFps);
        var visualCadenceHealthy =
            IsVisualCadenceHealthy(
                health.ExpectedFrameRate,
                health.VisualCadenceSampleCount,
                health.VisualCadenceChangeObservedFps,
                health.VisualCadenceRepeatFramePercent,
                health.VisualCadenceLongestRepeatRun);
        var mjpegDuplicateCadenceDetected = IsMjpegDuplicateCadenceDetected(health);
        var flashbackTempPressure =
            health.FlashbackActive &&
            (health.FlashbackStartupCacheOverBudget ||
             (health.FlashbackTempDriveFreeBytes >= 0 && health.FlashbackTempDriveFreeBytes < FlashbackTempDriveLowFreeBytes));
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
        var exportLastProgressAgeMs = health.FlashbackExportActive
            ? Math.Max(0, health.FlashbackExportLastProgressAgeMs)
            : 0;

        if (flashbackTempPressure)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_storage",
                "Flashback temp storage is under pressure.",
                tempCacheLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (health.FlashbackEncodingFailed)
        {
            return new DiagnosticEvaluation(
                "Critical",
                "flashback_recording",
                "Flashback encoder has failed.",
                flashbackRecordingLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (flashbackExportRotationGap)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_export",
                "Flashback export rotation skipped live-edge frames.",
                flashbackRecordingLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (flashbackBackendSettingsUnexpectedlyStale)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_recording",
                "Flashback backend settings differ from requested settings.",
                flashbackRecordingLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (flashbackRecordingDegraded)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_recording",
                "Flashback recording path is dropping or backing up.",
                flashbackRecordingLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (health.FlashbackExportActive)
        {
            if (exportLastProgressAgeMs >= FlashbackExportStallThresholdMs)
            {
                return new DiagnosticEvaluation(
                    "Warning",
                    "flashback_export",
                    "Flashback export progress is stalled.",
                    $"{exportLane} progressAgeMs={exportLastProgressAgeMs}",
                    sourceLane,
                    decodeLane,
                    previewLane,
                    renderLane,
                    presentLane,
                    recordingLane,
                    audioLane);
            }

            return new DiagnosticEvaluation(
                "Busy",
                "flashback_export",
                "Flashback export is running.",
                exportLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (playbackCommandQueueAgeMs >= FlashbackPlaybackCommandStallThresholdMs)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_playback",
                "Flashback playback command queue is stalled.",
                playbackCommandLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (playbackCommandFailedRecently)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_playback",
                "Flashback playback command failed recently.",
                playbackCommandLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (playbackSlow)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_playback",
                "Flashback playback is below target rate.",
                playbackPerfLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (playbackFrametimeDegraded)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_playback",
                "Flashback playback frametime is below target.",
                playbackPerfLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (health.FlashbackPlaybackSubmitFailures > 0)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_playback",
                "Flashback playback frame submission failed.",
                playbackPerfLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (!isPreviewing && !isRecording)
        {
            return new DiagnosticEvaluation(
                "Idle",
                "diagnostic_unavailable",
                "Preview and recording are idle.",
                "Start preview or recording to collect live frame-lane diagnostics.",
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (health.CaptureCadenceSampleCount < 30)
        {
            return new DiagnosticEvaluation(
                "WarmingUp",
                "diagnostic_unavailable",
                "Waiting for enough capture cadence samples.",
                sourceLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        var recordingIntegrityIncomplete =
            string.Equals(captureRuntime.RecordingIntegrityStatus, "Incomplete", StringComparison.OrdinalIgnoreCase);
        var recordingIntegrityFailed =
            health.RecordingEncodingFailed ||
            (recordingIntegrityIncomplete && !isRecording);

        if (recordingIntegrityFailed)
        {
            return new DiagnosticEvaluation(
                "Critical",
                "recording",
                "Recording integrity is the likely failure point.",
                recordingLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (!string.Equals(captureRuntime.RecordingIntegrityAudioStatus, "Clean", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(captureRuntime.RecordingIntegrityAudioStatus, "Disabled", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(captureRuntime.RecordingIntegrityAudioStatus, "NotStarted", StringComparison.OrdinalIgnoreCase))
        {
            return new DiagnosticEvaluation(
                "Warning",
                "audio",
                "Audio integrity is degraded.",
                audioLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (health.CaptureCadenceEstimatedDroppedFrames > 0 ||
            health.CaptureCadenceSevereGapCount > 0 ||
            health.CaptureCadenceEstimatedDropPercent > 0.1)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "source_capture",
                "Source/capture cadence is the likely stutter stage.",
                sourceLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (captureOnePercentLowDegraded)
        {
            if (isPreviewing &&
                visualCadenceHealthy &&
                health.CaptureCadenceEstimatedDroppedFrames <= 0 &&
                health.CaptureCadenceSevereGapCount <= 0 &&
                health.CaptureCadenceEstimatedDropPercent <= 0)
            {
                return new DiagnosticEvaluation(
                    "Healthy",
                    "none",
                    "Source/capture 1% low is below target, but sampled visual cadence confirms source-rate output.",
                    $"{sourceLane} | {visualLane}",
                    sourceLane,
                    decodeLane,
                    previewLane,
                    renderLane,
                    presentLane,
                    recordingLane,
                    audioLane);
            }

            return new DiagnosticEvaluation(
                "Warning",
                "source_capture",
                "Source/capture 1% low is below target.",
                sourceLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (mjpegDuplicateCadenceDetected)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "source_signal",
                "Captured HFR MJPEG cadence contains repeated source frames.",
                $"{mjpegDuplicateLane} | {visualLane} | {sourceSignalLane}",
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (recentMjpeg.DecodeFailures > 0 ||
            recentMjpeg.EmitFailures > 0 ||
            recentMjpeg.CompressedQueueDrops > 0 ||
            recentMjpeg.TotalDropped > 0)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "mjpeg_decode",
                "MJPEG decode/reorder is dropping or failing frames.",
                decodeLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        var previewSubmitFailed = string.Equals(
            health.MjpegPreviewJitterLastDropReason,
            "submit-failed",
            StringComparison.OrdinalIgnoreCase);
        if (previewSubmitFailed ||
            (recentPreviewDeadlineDrops > 0 && !visualCadenceHealthy) ||
            recentPreviewUnderflows > 3)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "preview_scheduler",
                previewSubmitFailed
                    ? "Preview scheduler failed to submit frames."
                    : "Preview scheduler is skipping stale or missing frames.",
                previewLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (recentRendererSubmitted >= DiagnosticThresholds.RendererDropWarningMinSamples &&
            recentRendererDropPercent > DiagnosticThresholds.RendererDropWarningPercent)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "renderer",
                "Renderer pacing is the likely preview bottleneck.",
                renderLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        var presentCadenceOverBudget =
            previewRuntime.DisplayCadenceExpectedIntervalMs > 0 &&
            previewRuntime.DisplayCadenceP95IntervalMs > previewRuntime.DisplayCadenceExpectedIntervalMs * 1.5;
        var unsyncedPresentCallSlow =
            previewRuntime.D3DPresentSyncInterval == 0 &&
            previewRuntime.D3DPresentCallP95Ms > 4.0;
        if (presentCadenceOverBudget ||
            unsyncedPresentCallSlow)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "present_display",
                "Present/display cadence is the likely preview bottleneck.",
                presentLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (previewOnePercentLowDegraded)
        {
            if (visualCadenceHealthy)
            {
                return new DiagnosticEvaluation(
                    "Healthy",
                    "none",
                    "Present/display 1% low is below target, but sampled visual cadence confirms source-rate output.",
                    $"{presentLane} | {visualLane}",
                    sourceLane,
                    decodeLane,
                    previewLane,
                    renderLane,
                    presentLane,
                    recordingLane,
                    audioLane);
            }

            return new DiagnosticEvaluation(
                "Warning",
                "present_display",
                "Present/display 1% low is below target.",
                presentLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        var summary = performance.PerfectionMet
            ? "No degraded frame lane detected."
            : performance.Summary;
        return new DiagnosticEvaluation(
            performance.PerfectionMet ? "Healthy" : "Warning",
            performance.PerfectionMet ? "none" : "mixed",
            summary,
            performance.PerfectionMet ? "All monitored frame lanes are within current thresholds." : performance.Summary,
            sourceLane,
            decodeLane,
            previewLane,
            renderLane,
            presentLane,
            recordingLane,
            audioLane);
    }
}
