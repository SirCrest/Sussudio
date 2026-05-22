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
}
