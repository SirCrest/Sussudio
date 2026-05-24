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
        var visualCadenceHealthy =
            IsVisualCadenceHealthy(
                health.ExpectedFrameRate,
                health.VisualCadenceSampleCount,
                health.VisualCadenceChangeObservedFps,
                health.VisualCadenceRepeatFramePercent,
                health.VisualCadenceLongestRepeatRun);

        return TryBuildRealtimeStateDiagnosticEvaluation(health, isPreviewing, isRecording, lanes) ??
               TryBuildRealtimeRecordingDiagnosticEvaluation(captureRuntime, health, isRecording, lanes) ??
               TryBuildRealtimeSourceDiagnosticEvaluation(health, isPreviewing, visualCadenceHealthy, lanes) ??
               TryBuildRealtimeMjpegDiagnosticEvaluation(health, recentMjpeg, lanes) ??
               TryBuildRealtimePreviewDiagnosticEvaluation(
                   health,
                   previewRuntime,
                   visualCadenceHealthy,
                   recentPreviewUnderflows,
                   recentPreviewDeadlineDrops,
                   lanes);
    }

    private static DiagnosticEvaluation? TryBuildRealtimeStateDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        bool isPreviewing,
        bool isRecording,
        DiagnosticEvaluationLanes lanes)
    {
        if (!isPreviewing && !isRecording)
        {
            return new DiagnosticEvaluation(
                "Idle",
                "diagnostic_unavailable",
                "Preview and recording are idle.",
                "Start preview or recording to collect live frame-lane diagnostics.",
                lanes.Source,
                lanes.Decode,
                lanes.Preview,
                lanes.Render,
                lanes.Present,
                lanes.Recording,
                lanes.Audio);
        }

        if (health.CaptureCadenceSampleCount >= 30)
        {
            return null;
        }

        return new DiagnosticEvaluation(
            "WarmingUp",
            "diagnostic_unavailable",
            "Waiting for enough capture cadence samples.",
            lanes.Source,
            lanes.Source,
            lanes.Decode,
            lanes.Preview,
            lanes.Render,
            lanes.Present,
            lanes.Recording,
            lanes.Audio);
    }

    private static DiagnosticEvaluation? TryBuildRealtimeRecordingDiagnosticEvaluation(
        CaptureRuntimeSnapshot captureRuntime,
        CaptureHealthSnapshot health,
        bool isRecording,
        DiagnosticEvaluationLanes lanes)
    {
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
                lanes.Recording,
                lanes.Source,
                lanes.Decode,
                lanes.Preview,
                lanes.Render,
                lanes.Present,
                lanes.Recording,
                lanes.Audio);
        }

        if (string.Equals(captureRuntime.RecordingIntegrityAudioStatus, "Clean", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(captureRuntime.RecordingIntegrityAudioStatus, "Disabled", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(captureRuntime.RecordingIntegrityAudioStatus, "NotStarted", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new DiagnosticEvaluation(
            "Warning",
            "audio",
            "Audio integrity is degraded.",
            lanes.Audio,
            lanes.Source,
            lanes.Decode,
            lanes.Preview,
            lanes.Render,
            lanes.Present,
            lanes.Recording,
            lanes.Audio);
    }

    private static DiagnosticEvaluation? TryBuildRealtimeSourceDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        bool isPreviewing,
        bool visualCadenceHealthy,
        DiagnosticEvaluationLanes lanes)
    {
        var captureOnePercentLowDegraded =
            IsCaptureOnePercentLowDegraded(
                health.ExpectedFrameRate,
                health.CaptureCadenceSampleCount,
                health.CaptureCadenceOnePercentLowFps);

        if (health.CaptureCadenceEstimatedDroppedFrames > 0 ||
            health.CaptureCadenceSevereGapCount > 0 ||
            health.CaptureCadenceEstimatedDropPercent > 0.1)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "source_capture",
                "Source/capture cadence is the likely stutter stage.",
                lanes.Source,
                lanes.Source,
                lanes.Decode,
                lanes.Preview,
                lanes.Render,
                lanes.Present,
                lanes.Recording,
                lanes.Audio);
        }

        if (!captureOnePercentLowDegraded)
        {
            return null;
        }

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
                $"{lanes.Source} | {lanes.Visual}",
                lanes.Source,
                lanes.Decode,
                lanes.Preview,
                lanes.Render,
                lanes.Present,
                lanes.Recording,
                lanes.Audio);
        }

        return new DiagnosticEvaluation(
            "Warning",
            "source_capture",
            "Source/capture 1% low is below target.",
            lanes.Source,
            lanes.Source,
            lanes.Decode,
            lanes.Preview,
            lanes.Render,
            lanes.Present,
            lanes.Recording,
            lanes.Audio);
    }

    private static DiagnosticEvaluation? TryBuildRealtimeMjpegDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        MjpegRecentCounters recentMjpeg,
        DiagnosticEvaluationLanes lanes)
    {
        var mjpegDuplicateCadenceDetected = IsMjpegDuplicateCadenceDetected(health);

        if (mjpegDuplicateCadenceDetected)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "source_signal",
                "Captured HFR MJPEG cadence contains repeated source frames.",
                $"{lanes.MjpegDuplicate} | {lanes.Visual} | {lanes.SourceSignal}",
                lanes.Source,
                lanes.Decode,
                lanes.Preview,
                lanes.Render,
                lanes.Present,
                lanes.Recording,
                lanes.Audio);
        }

        if (recentMjpeg.DecodeFailures <= 0 &&
            recentMjpeg.EmitFailures <= 0 &&
            recentMjpeg.CompressedQueueDrops <= 0 &&
            recentMjpeg.TotalDropped <= 0)
        {
            return null;
        }

        return new DiagnosticEvaluation(
            "Warning",
            "mjpeg_decode",
            "MJPEG decode/reorder is dropping or failing frames.",
            lanes.Decode,
            lanes.Source,
            lanes.Decode,
            lanes.Preview,
            lanes.Render,
            lanes.Present,
            lanes.Recording,
            lanes.Audio);
    }

    private static DiagnosticEvaluation? TryBuildRealtimePreviewDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        PreviewRuntimeSnapshot previewRuntime,
        bool visualCadenceHealthy,
        long recentPreviewUnderflows,
        long recentPreviewDeadlineDrops,
        DiagnosticEvaluationLanes lanes)
    {
        return TryBuildRealtimePreviewSchedulerDiagnosticEvaluation(
                   health,
                   visualCadenceHealthy,
                   recentPreviewUnderflows,
                   recentPreviewDeadlineDrops,
                   lanes) ??
               TryBuildRealtimePreviewRendererDiagnosticEvaluation(lanes) ??
               TryBuildRealtimePreviewPresentDiagnosticEvaluation(
                   previewRuntime,
                   visualCadenceHealthy,
                   lanes);
    }

    private static DiagnosticEvaluation? TryBuildRealtimePreviewRendererDiagnosticEvaluation(
        DiagnosticEvaluationLanes lanes)
    {
        var recentRendererSubmitted = lanes.RecentRendererSubmitted;
        var recentRendererDropPercent = lanes.RecentRendererDropPercent;
        if (recentRendererSubmitted < DiagnosticThresholds.RendererDropWarningMinSamples ||
            recentRendererDropPercent <= DiagnosticThresholds.RendererDropWarningPercent)
        {
            return null;
        }

        return new DiagnosticEvaluation(
            "Warning",
            "renderer",
            "Renderer pacing is the likely preview bottleneck.",
            lanes.Render,
            lanes.Source,
            lanes.Decode,
            lanes.Preview,
            lanes.Render,
            lanes.Present,
            lanes.Recording,
            lanes.Audio);
    }

    private static DiagnosticEvaluation? TryBuildRealtimePreviewSchedulerDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        bool visualCadenceHealthy,
        long recentPreviewUnderflows,
        long recentPreviewDeadlineDrops,
        DiagnosticEvaluationLanes lanes)
    {
        var previewSubmitFailed = string.Equals(
            health.MjpegPreviewJitterLastDropReason,
            "submit-failed",
            StringComparison.OrdinalIgnoreCase);
        if (!previewSubmitFailed &&
            (recentPreviewDeadlineDrops <= 0 || visualCadenceHealthy) &&
            recentPreviewUnderflows <= 3)
        {
            return null;
        }

        return new DiagnosticEvaluation(
            "Warning",
            "preview_scheduler",
            previewSubmitFailed
                ? "Preview scheduler failed to submit frames."
                : "Preview scheduler is skipping stale or missing frames.",
            lanes.Preview,
            lanes.Source,
            lanes.Decode,
            lanes.Preview,
            lanes.Render,
            lanes.Present,
            lanes.Recording,
            lanes.Audio);
    }

    private static DiagnosticEvaluation? TryBuildRealtimePreviewPresentDiagnosticEvaluation(
        PreviewRuntimeSnapshot previewRuntime,
        bool visualCadenceHealthy,
        DiagnosticEvaluationLanes lanes)
    {
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
                lanes.Present,
                lanes.Source,
                lanes.Decode,
                lanes.Preview,
                lanes.Render,
                lanes.Present,
                lanes.Recording,
                lanes.Audio);
        }

        var previewOnePercentLowDegraded =
            IsPreviewOnePercentLowDegraded(
                previewRuntime.DisplayCadenceExpectedIntervalMs,
                previewRuntime.DisplayCadenceSampleCount,
                previewRuntime.DisplayCadenceOnePercentLowFps);
        if (!previewOnePercentLowDegraded)
        {
            return null;
        }

        if (visualCadenceHealthy)
        {
            return new DiagnosticEvaluation(
                "Healthy",
                "none",
                "Present/display 1% low is below target, but sampled visual cadence confirms source-rate output.",
                $"{lanes.Present} | {lanes.Visual}",
                lanes.Source,
                lanes.Decode,
                lanes.Preview,
                lanes.Render,
                lanes.Present,
                lanes.Recording,
                lanes.Audio);
        }

        return new DiagnosticEvaluation(
            "Warning",
            "present_display",
            "Present/display 1% low is below target.",
            lanes.Present,
            lanes.Source,
            lanes.Decode,
            lanes.Preview,
            lanes.Render,
            lanes.Present,
            lanes.Recording,
            lanes.Audio);
    }
}
