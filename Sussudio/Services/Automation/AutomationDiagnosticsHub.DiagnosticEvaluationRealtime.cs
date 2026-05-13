using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static DiagnosticEvaluation? TryBuildRealtimeDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        CaptureRuntimeSnapshot captureRuntime,
        PreviewRuntimeSnapshot previewRuntime,
        bool isPreviewing,
        bool isRecording,
        MjpegRecentCounters recentMjpeg,
        long recentPreviewUnderflows,
        long recentPreviewDeadlineDrops,
        DiagnosticEvaluationLanes lanes)
    {
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
        var recentRendererSubmitted = lanes.RecentRendererSubmitted;
        var recentRendererDropPercent = lanes.RecentRendererDropPercent;
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

        return null;
    }
}
